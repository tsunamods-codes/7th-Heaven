using AppCore;
using AppWrapper;
using AsmResolver;
using Iros;
using Iros.Workshop;
using Microsoft.Win32;
using AppUI.Windows;
using AppUI.ViewModels;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Profile = Iros.Workshop.Profile;
using AsmResolver.PE.File;
using System.Windows.Shell;
using AsmResolver.PE.File.Headers;

namespace AppUI.Classes
{
    internal enum GraphicsRenderer
    {
        SoftwareRenderer = 0,
        D3DHardwareAccelerated = 1,
        CustomDriver = 3
    }

    /// <summary>
    /// Responsibile for the entire process that happens for launching the game
    /// </summary>
    public class GameLauncher
    {
        #region Data Members and Properties

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static GameLauncher _instance;

        public static GameLauncher Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameLauncher();

                return _instance;
            }
        }

        private Dictionary<string, Process> _alsoLaunchProcesses = new Dictionary<string, Process>(StringComparer.InvariantCultureIgnoreCase);
        private Dictionary<AppWrapper.ProgramInfo, Process> _sideLoadProcesses = new Dictionary<AppWrapper.ProgramInfo, Process>();

        private ControllerInterceptor _controllerInterceptor;

        public delegate void OnProgressChanged(string message);
        public event OnProgressChanged ProgressChanged;

        public delegate void OnLaunchCompleted(bool wasSuccessful);
        public event OnLaunchCompleted LaunchCompleted;


        public string DriveLetter { get; set; }
        public bool DidMountVirtualDisc { get; set; }

        #endregion

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_FORCEMINIMIZE = 11;
        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static Process ff7Proc;

        public static async Task<bool> LaunchGame(bool varDump, bool debug, bool launchWithNoMods = false)
        {
            bool runAsVanilla = false, didDisableReunion = false;
            string vanillaMsg = "";
            RuntimeProfile runtimeProfile = null;

            MainWindowViewModel.SaveActiveProfile();
            Sys.Save();

            GameConverter converter = new GameConverter(Path.GetDirectoryName(Sys.Settings.FF7Exe));
            converter.MessageSent += GameConverter_MessageSent;

            // Check for DEP
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckForSystemDEPStatus));
            switch (AppWrapper.Win32.GetSystemDEPPolicy())
            {
                case AppWrapper.Win32.DEP_SYSTEM_POLICY_TYPE.DEPPolicyAlwaysOn:
                case AppWrapper.Win32.DEP_SYSTEM_POLICY_TYPE.DEPPolicyOptOut:
                    if (MessageDialogWindow.Show(ResourceHelper.Get(StringKey.DoYouWantToDisableDEP), ResourceHelper.Get(StringKey.DEPDetected), MessageBoxButton.YesNo, MessageBoxImage.Warning).Result == MessageBoxResult.Yes)
                    {
                        string fileName = Path.Combine(Sys.PathToTempFolder, "fixdep.bat");

                        System.IO.File.WriteAllText(
                            fileName,
                            $@"@echo off
@bcdedit /set nx OptIn
"
                        );

                        try
                        {
                            // Execute temp batch script with admin privileges
                            ProcessStartInfo startInfo = new ProcessStartInfo(fileName)
                            {
                                CreateNoWindow = false,
                                UseShellExecute = true,
                                Verb = "runas"
                            };

                            // Launch process, wait and then save exit code
                            using (Process temp = Process.Start(startInfo))
                            {
                                temp.WaitForExit();
                            }

                            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SystemDEPDisabledPleaseReboot), NLog.LogLevel.Info);
                        }
                        catch (Exception)
                        {
                            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SomethingWentWrongWhileDisablingDEP), NLog.LogLevel.Error);
                        }
                    }
                    else
                    {
                        Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CannotContinueWithDEPEnabled), NLog.LogLevel.Warn);
                    }
                    return false;
            }

            //
            // Ensure FFNx is installed correctly
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingLatestGameDriverIsInstalled));
            if (!FFNxDriverUpdater.IsAlreadyInstalled())
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SomethingWentWrongTryingToDetectGameDriver), NLog.LogLevel.Error);
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingFf7IsNotRunning));
            if (IsFF7Running())
            {
                string title = ResourceHelper.Get(StringKey.Ff7IsAlreadyRunning);
                string message = ResourceHelper.Get(StringKey.Ff7IsAlreadyRunningDoYouWantToForceClose);

                Instance.RaiseProgressChanged($"\t{title}", NLog.LogLevel.Warn);

                var result = MessageDialogWindow.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result.Result == MessageBoxResult.Yes)
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ForceClosingAllInstancesFf7)}", NLog.LogLevel.Info);

                    if (!ForceKillFF7())
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCloseProcess)} {Path.GetFileName(Sys.Settings.FF7Exe)}. {ResourceHelper.Get(StringKey.Aborting)}", NLog.LogLevel.Error);
                        return false;
                    }
                }
            }

            Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.CheckingFf7ExeExistsAt)} {Sys.Settings.FF7Exe} ...");
            if (!File.Exists(Sys.Settings.FF7Exe))
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FileNotFoundAborting)}", NLog.LogLevel.Error);
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.Ff7ExeNotFoundYouMayNeedToConfigure));
                return false;
            }

            //
            // GAME SANITY CHECK - Make sure the user has at least run once the Steam game
            //
            if (Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
            {
                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.CheckingFF7SteamInstalledCorrectly)}...");
                if (!Path.Exists(GameConverter.GetSteamFF7UserPath()) || Directory.EnumerateDirectories(GameConverter.GetSteamFF7UserPath(), "user_*").Count() <= 0)
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FF7SteamNotInstalledCorrectly)}", NLog.LogLevel.Warn);
                    var result = MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FF7SteamNotInstalledCorrectly), "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (result.Result == MessageBoxResult.OK)
                    {
                        runAsVanilla = true;
                        goto LaunchGame;
                    }
                }
            }

            //
            // GAME CONVERTER - Make sure game is ready for mods
            //
            FFNxDriverUpdater.CleanupUnnecessaryFiles();
            ReShadeUpdater.Cleanup();

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingInstalledGameIsCompatible));
            if (converter.IsGamePirated())
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ErrorCodeYarr), NLog.LogLevel.Error);
                Logger.Info(FileUtils.ListAllFiles(converter.InstallPath));
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CreatingMissingRequiredDirectories));
            converter.CreateMissingDirectories();

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingEnglishGameFilesExist));
            if (!converter.IsEnglishGameInstalled())
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ErrorOnlyEnglishLanguageSupported), NLog.LogLevel.Error);
                return false;
            }

            if (Sys.Settings.FF7InstalledVersion == FF7Version.Original98)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingGameIsMaxInstall));
                if (!converter.VerifyFullInstallation())
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.YourFf7InstallationFolderIsMissingCriticalFiles), NLog.LogLevel.Error);
                    return false;
                }
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingMusicFilesExist));
            converter.AllMusicFilesExist();

            string backupFolderPath = Path.Combine(converter.InstallPath, GameConverter.BackupFolderName, DateTime.Now.ToString("yyyyMMddHHmmss"));

            if (Sys.Settings.FF7InstalledVersion == FF7Version.Original98)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingFf7Exe));
                if (new FileInfo(Sys.Settings.FF7Exe).Name.Equals("ff7.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    // only compare exes are different if ff7.exe set in Settings (and not something like ff7_bc.exe)
                    if (converter.IsExeDifferent())
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Ff7ExeDetectedToBeDifferent)}");
                        if (converter.BackupExe(backupFolderPath))
                        {
                            bool didCopy = converter.CopyFF7ExeToGame();
                            if (!didCopy)
                            {
                                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCopyFf7Exe)}", NLog.LogLevel.Error);
                                return false;
                            }
                        }
                        else
                        {
                            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCreateBackupOfFf7Exe)}", NLog.LogLevel.Error);
                            return false;
                        }
                    }
                }

                if (converter.IsConfigExeDifferent())
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Ff7ConfigExeDetectedToBeMissingOrDifferent)}");
                    if (converter.BackupFF7ConfigExe(backupFolderPath))
                    {
                        bool didCopy = converter.CopyFF7ConfigExeToGame();
                        if (!didCopy)
                        {
                            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCopyFf7ConfigExe)}", NLog.LogLevel.Error);
                            return false;
                        }
                    }
                    else
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCreateBackupOfFf7ConfigExe)}", NLog.LogLevel.Error);
                        return false;
                    }
                }
            }

            // Auto-patch for 4GB support
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.App4GBPatchRequired));
            PEFile file = PEFile.FromFile(Sys.Settings.FF7Exe);
            if (!file.FileHeader.Characteristics.HasFlag(AsmResolver.PE.File.Headers.Characteristics.LargeAddressAware))
            {
                converter.BackupExe(backupFolderPath);
                file.FileHeader.Characteristics |= Characteristics.LargeAddressAware;
                file.Write(Sys.Settings.FF7Exe);
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.App4GBPatchApplied));
            }

            //
            // Copy the new launcher to the game path
            //
            if (Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VerifyingFf7LauncherExe));

                string src = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string dest = Path.GetDirectoryName(Sys.Settings.FF7Exe);
                File.Copy(Path.Combine(src, "AppLauncher.exe"), Path.Combine(dest, "FF7_Launcher.exe"), true);
            }

            //
            // Get Drive Letter and update registry
            //
            if (Sys.Settings.FF7InstalledVersion == FF7Version.Original98)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.LookingForGameDisc));
                Instance.DriveLetter = GetDriveLetter();

                if (!string.IsNullOrEmpty(Instance.DriveLetter))
                {
                    Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.FoundGameDiscAt)} {Instance.DriveLetter} ...");
                }
                else
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.FailedToFindGameDisc), NLog.LogLevel.Error);
                    return false;
                }

                //
                // Update Registry with new launch settings
                //
                Instance.SetRegistryValues();
            }

            //
            // GAME SHOULD BE FULLY 'CONVERTED' AND READY TO LAUNCH FOR MODS AT THIS POINT
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingAProfileIsActive));
            if (Sys.ActiveProfile == null)
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ActiveProfileNotFound)}", NLog.LogLevel.Error);
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CreateAProfileFirstAndTryAgain));
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingModCompatibilityRequirements));
            if (!SanityCheckCompatibility())
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedModCompatibilityCheck)}", NLog.LogLevel.Error);
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingModConstraintsForCompatibility));
            if (!SanityCheckSettings())
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedModConstraintCheck)}", NLog.LogLevel.Error);
                return false;
            }

            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingModLoadOrderRequirements));
            if (!VerifyOrdering())
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedModLoadOrderCheck)}", NLog.LogLevel.Error);
                return false;
            }

            //
            // Copy input.cfg to FF7
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CopyingFf7InputCfgToFf7Path));
            bool didCopyCfg = CopyKeyboardInputCfg();

            // Copy Reshade
            ReShadeUpdater.Install();

            //
            // Determine if game will be ran as 'vanilla' with mods so don't have to inject with AppLoader
            //
            if (launchWithNoMods)
            {
                vanillaMsg = ResourceHelper.Get(StringKey.UserRequestedToPlayWithNoModsLaunchingGameAsVanilla);
                runAsVanilla = true;
            }
            else if (Sys.ActiveProfile.ActiveItems.Count == 0)
            {
                vanillaMsg = ResourceHelper.Get(StringKey.NoModsActivatedLaunchingGameAsVanilla);
                runAsVanilla = true;
            }

            if (!runAsVanilla)
            {
                //
                // Create Runtime Profile for Active Mods
                //
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CreatingRuntimeProfile));
                runtimeProfile = CreateRuntimeProfile();

                if (runtimeProfile == null)
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCreateRuntimeProfileForActiveMods)}", NLog.LogLevel.Error);
                    return false;
                }

                //
                // Copy 7thWrapper* dlls to FF7
                //
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CopyingEasyHookToFf7PathIfNotFoundOrOlder));
                Copy7thWrapperDlls();

                //
                // Inherit FFNx Config keys from each mod
                //
                Sys.FFNxConfig.Reload();
                Sys.FFNxConfig.Backup(true);
                Sys.FFNxConfig.OverrideInternalKeys(debug && runtimeProfile != null);
                foreach (RuntimeMod mod in runtimeProfile.Mods)
                {
                    foreach(FFNxFlag flag in mod.FFNxConfig)
                    {
                        bool addConfig = true;

                        if (flag.Attributes.Count > 0)
                        {
                            foreach (var attr in flag.Attributes)
                            {
                                foreach(Iros.Workshop.ProfileItem item in Sys.ActiveProfile.ActiveItems)
                                {
                                    foreach(ProfileSetting setting in item.Settings)
                                    {
                                        if (setting.ID == attr.Key && setting.Value != attr.Value) addConfig = false;
                                    }
                                }
                            }
                        }

                        if (addConfig)
                        {
                            if (Sys.FFNxConfig.HasKey(flag.Key))
                            {
                                if (flag.Values.Count > 0)
                                {
                                    Sys.FFNxConfig.Set(flag.Key, flag.Values);
                                }
                                else
                                {
                                    Sys.FFNxConfig.Set(flag.Key, flag.Value);
                                }
                            }
                        }
                    }
                }
                Sys.FFNxConfig.Save();
            }

            //
            // Setup log file if debugging
            //
            if (debug && runtimeProfile != null)
            {
                runtimeProfile.Options |= RuntimeOptions.DetailedLog;
                runtimeProfile.LogFile = Path.Combine(Path.GetDirectoryName(Sys.Settings.FF7Exe), "log.txt");

                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.DebugLoggingSetToTrueDetailedLoggingWillBeWrittenTo)} {runtimeProfile.LogFile} ...");
            }

            //
            // Check/Disable Reunion Mod
            //
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.CheckingIfReunionModIsInstalled));
            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Found)}: {IsReunionModInstalled()}");

            if (IsReunionModInstalled() && Sys.Settings.GameLaunchSettings.DisableReunionOnLaunch)
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.DisablingReunionMod));
                EnableOrDisableReunionMod(doEnable: false);
                didDisableReunion = true;
            }

            //
            // Initialize the controller input interceptor
            //
            try
            {
                if (Sys.Settings.GameLaunchSettings.EnableGamepadPolling)
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.BeginningToPollForGamePadInput));

                    if (Instance._controllerInterceptor == null)
                    {
                        Instance._controllerInterceptor = new ControllerInterceptor();
                    }

                    await Instance._controllerInterceptor.PollForGamepadInput().ContinueWith((result) =>
                    {
                        if (result.IsFaulted)
                        {
                            Logger.Error(result.Exception);
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ReceivedUnknownError)}: {e.Message} ...", NLog.LogLevel.Warn);
            }

        LaunchGame:
            // start FF7 proc as normal and return true when running the game as vanilla
            if (runAsVanilla)
            {
                Instance.RaiseProgressChanged(vanillaMsg);
                await LaunchFF7Exe();

                if (didDisableReunion)
                {
                    await Task.Factory.StartNew(() =>
                    {
                        System.Threading.Thread.Sleep(5000); // wait 5 seconds before renaming the dll so the game and gl driver can fully initialize
                        Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ReenablingReunionMod));
                        EnableOrDisableReunionMod(doEnable: true);
                    });
                }

                return true;
            }

            //
            // Attempt to Create FF7 Proc and Inject with AppLoader
            //
            try
            {
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.LaunchingAdditionalProgramsToRunIfAny));
                Instance.LaunchAdditionalProgramsToRunPrior();

                RuntimeParams parms = new RuntimeParams
                {
                    ProfileFile = Path.Combine(Path.GetDirectoryName(Sys.Settings.FF7Exe), ".7thWrapperProfile")
                };

                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.WritingTemporaryRuntimeProfileFileTo)} {parms.ProfileFile} ...");

                using (FileStream fs = new FileStream(parms.ProfileFile, FileMode.Create))
                    Util.SerializeBinary(runtimeProfile, fs);

                // attempt to launch the game a few times in the case of an ApplicationException that can be thrown by AppLoader it seems randomly at times
                // ... The error tends to go away the second time trying but we will try multiple times before failing
                bool didInject = false;

                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.AttemptingToInjectWithEasyHook));

                while (!didInject)
                {
                    try
                    {
                        await LaunchFF7Exe();
                        didInject = true;
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            ff7Proc.Kill();
                        }catch { }
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ReceivedUnknownError)}: {e.Message} ...", NLog.LogLevel.Warn);
                    }
                }

                if (!didInject)
                {
                    Sys.FFNxConfig.RestoreBackup(true);
                    return false;
                }


                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.GettingFf7Proc));

                if (ff7Proc == null)
                {
                    Sys.FFNxConfig.RestoreBackup(true);

                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToGetFf7Proc)}", NLog.LogLevel.Error);
                    return false;
                }

                ff7Proc.EnableRaisingEvents = true;

                if (debug)
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.DebugLoggingSetToTrueWiringUpLogFile));
                    ff7Proc.Exited += (o, e) =>
                    {
                        try
                        {
                            DebugLogger.CloseLogFile();

                            ProcessStartInfo debugTxtProc = new ProcessStartInfo()
                            {
                                WorkingDirectory = Path.GetDirectoryName(runtimeProfile.LogFile),
                                FileName = runtimeProfile.LogFile,
                                UseShellExecute = true
                            };
                            Process.Start(debugTxtProc);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.FailedToStartProcessForDebugLog)}: {ex.Message}", NLog.LogLevel.Error);
                        }
                    };
                }

                //
                // Start Turbolog for Variable Dump
                //
                if (varDump)
                {
                    Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.VariableDumpSetToTrueStartingTurboLog));
                    StartTurboLogForVariableDump(runtimeProfile);
                }

                /// sideload programs for mods before starting FF7 because FF7 losing focus while initializing can cause the intro movies to stop playing
                /// ... Thus we load programs first so they don't steal window focus
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.StartingProgramsForMods));
                foreach (RuntimeMod mod in runtimeProfile.Mods)
                {
                    Instance.LaunchProgramsForMod(mod);
                }

                // wire up process to stop plugins and side processes when proc has exited
                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.SettingUpFf7ExeToStopPluginsAndModPrograms));
                ff7Proc.Exited += (o, e) =>
                {
                    if (!IsFF7Running() && Instance._controllerInterceptor != null)
                    {
                        // stop polling for input once all ff7 procs are closed (could be multiple instances open)
                        Instance._controllerInterceptor.PollingInput = false;
                    }

                    // wrapped in try/catch so an unhandled exception when exiting the game does not crash 7H
                    try
                    {
                        Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.StoppingOtherProgramsForMods));
                        Instance.StopAllSideProcessesForMods();

                        // ensure Reunion is re-enabled when ff7 process exits in case it failed above for any reason
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(Sys.Settings.FF7Exe), "Reunion.dll.bak")))
                        {
                            EnableOrDisableReunionMod(doEnable: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    // Did the game crash? If yes, generate a report
                    if (ff7Proc.ExitCode < 0)
                    {
                        Logger.Error($"FF7.exe Crashed while exiting with code 0x{ff7Proc.ExitCode.ToString("X8")}. Generating report...");
                        Instance.CollectCrashReport();
                    }

                    // Restore FFNx config after the game is closed
                    Sys.FFNxConfig.RestoreBackup(true);
                };
            

                // ensure ff7 window is active at end of launching
                if (ff7Proc.MainWindowHandle != IntPtr.Zero)
                {
                    int secondsToWait = 120;
                    // setting the ff7 proc as the foreground window makes it the active window and thus can start processing mods (this will usually cause a 'Not Responding...' window when loading a lot of mods)
                    SetForegroundWindow(ff7Proc.MainWindowHandle);
                    Instance.RaiseProgressChanged(string.Format(ResourceHelper.Get(StringKey.WaitingForFf7ExeToRespond), secondsToWait));

                    // after setting as active window, wait to ensure the window loads all mods and becomes responsive
                    DateTime start = DateTime.Now;
                    while (ff7Proc.Responding == false)
                    {
                        TimeSpan elapsed = DateTime.Now.Subtract(start);
                        if (elapsed.TotalSeconds > secondsToWait)
                            break;

                        ff7Proc.Refresh();
                    }

                    ff7Proc.Exited += (object sender, EventArgs e) => {
                        if (didDisableReunion)
                        {
                            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ReenablingReunionMod));
                            EnableOrDisableReunionMod(doEnable: true);
                        }
                    };

                    SetForegroundWindow(ff7Proc.MainWindowHandle); // activate window again
                }

                return true;
            }
            catch (Exception e)
            {
                Sys.FFNxConfig.RestoreBackup(true);

                Logger.Error(e);

                Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ExceptionOccurredWhileTryingToStart), NLog.LogLevel.Error);
                MessageDialogWindow.Show(e.ToString(), ResourceHelper.Get(StringKey.ErrorFailedToStartGame), MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
            finally
            {
                converter.MessageSent -= GameConverter_MessageSent;
            }
        }

        private static void GameConverter_MessageSent(string message, NLog.LogLevel logLevel)
        {
            Instance.RaiseProgressChanged(message, logLevel);
        }

        internal static RuntimeProfile CreateRuntimeProfile()
        {
            List<RuntimeMod> runtimeMods = null;

            try
            {
                runtimeMods = Sys.ActiveProfile.ActiveItems.Select(i => i.GetRuntime(Sys._context))
                                                           .Where(i => i != null)
                                                           .ToList();
            }
            catch (Exception)
            {
                throw;
            }


            RuntimeProfile runtimeProfiles = new RuntimeProfile()
            {
                MonitorPaths = new List<string>() {
                    Sys.PathToFF7Data,
                    Sys.PathToFF7Textures,
                },
                ModPath = Sys.Settings.LibraryLocation,
                FF7Path = Sys.InstallPath,
                gameFiles = Directory.GetFiles(Sys.InstallPath, "*.*", SearchOption.AllDirectories),
                Mods = runtimeMods
            };

            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.AddingPathsToMonitor)}");
            runtimeProfiles.MonitorPaths.AddRange(Sys.Settings.ExtraFolders.Where(s => s.Length > 0).Select(s => Path.Combine(Sys.InstallPath, s)));
            return runtimeProfiles;
        }

        private static void Copy7thWrapperDlls()
        {
            string src = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string dest = Path.GetDirectoryName(Sys.Settings.FF7Exe);

            File.Copy(Path.Combine(src, "AppProxy.runtimeconfig.json"), Path.Combine(dest, "AppProxy.runtimeconfig.json"), true);
            File.Copy(Path.Combine(src, "AppProxy.dll"), Path.Combine(dest, "AppProxy.dll"), true);
            File.Copy(Path.Combine(src, "SharpCompress.dll"), Path.Combine(dest, "SharpCompress.dll"), true);
            File.Copy(Path.Combine(src, "AppWrapper.dll"), Path.Combine(dest, "AppWrapper.dll"), true);
            File.Copy(Path.Combine(src, "AppLoader.dll"), Path.Combine(dest, "dinput.dll"), true);
            File.Copy(Path.Combine(src, "AppLoader.pdb"), Path.Combine(dest, "AppLoader.pdb"), true);
            File.Copy(Path.Combine(src, "nethost.dll"), Path.Combine(dest, "nethost.dll"), true);
            File.Copy(Path.Combine(src, "System.Runtime.Serialization.Formatters.dll"), Path.Combine(dest, "System.Runtime.Serialization.Formatters.dll"), true);
        }

        private static void Delete7thWrapperDlls()
        {
            string dest = Path.GetDirectoryName(Sys.Settings.FF7Exe);

            string workshopDir = Path.Combine(dest, "7thWorkshop");
            if (Directory.Exists(workshopDir)) Directory.Delete(workshopDir, true);

            File.Delete(Path.Combine(dest, "AppProxy.runtimeconfig.json"));
            File.Delete(Path.Combine(dest, "AppProxy.dll"));
            File.Delete(Path.Combine(dest, "SharpCompress.dll"));
            File.Delete(Path.Combine(dest, "AppWrapper.dll"));
            File.Delete(Path.Combine(dest, "dinput.dll"));
            File.Delete(Path.Combine(dest, "AppLoader.pdb"));
            File.Delete(Path.Combine(dest, "nethost.dll"));
            File.Delete(Path.Combine(dest, "System.Runtime.Serialization.Formatters.dll"));
        }

        private static void StartTurboLogForVariableDump(RuntimeProfile runtimeProfiles)
        {
            string turboLogProcName = Path.Combine(Sys._7HFolder, "TurBoLog.exe");

            // remove from dictionary (and stop other turbolog exe) if exists
            if (Instance._alsoLaunchProcesses.ContainsKey(turboLogProcName))
            {
                if (!Instance._alsoLaunchProcesses[turboLogProcName].HasExited)
                {
                    Instance._alsoLaunchProcesses[turboLogProcName].Kill();
                }
                Instance._alsoLaunchProcesses.Remove(turboLogProcName);
            }

            runtimeProfiles.MonitorVars = Sys._context.VarAliases.Select(kv => new Tuple<string, string>(kv.Key, kv.Value)).ToList();

            ProcessStartInfo psi = new ProcessStartInfo(turboLogProcName)
            {
                WorkingDirectory = Path.GetDirectoryName(Sys.Settings.FF7Exe),
                UseShellExecute = true,
            };
            Process aproc = Process.Start(psi);

            Instance._alsoLaunchProcesses.Add(turboLogProcName, aproc);
            aproc.EnableRaisingEvents = true;
            aproc.Exited += (o, e) => Instance._alsoLaunchProcesses.Remove(turboLogProcName);
        }

        internal static async Task<Process> waitForProcess(string name)
        {
            Process ret = null;

            do
            {
                if (Process.GetProcessesByName(name) is [{ HasExited: false } process, ..])
                    ret = process;
                else
                    await Task.Delay(5000);
            } while (ret == null);

            return ret;
        }

        internal static async Task<bool> waitForProcessExit(string name)
        {
            Process ret;
            do
            {
                if (Process.GetProcessesByName(name) is [{ HasExited: true } process, ..])
                {
                    ret = process;
                    await Task.Delay(5000);
                }
                else
                    ret = null;
            } while (ret != null);

            return true;
        }

        /// <summary>
        /// Launches FF7.exe without loading any mods.
        /// </summary>
        internal static async Task<bool> LaunchFF7Exe()
        {
            try
            {
                var isSteamVersion = Sys.Settings.FF7InstalledVersion == FF7Version.Steam;

                if (Sys.Settings.HasOption(GeneralOptions.LaunchExeDirectly) || !isSteamVersion)
                {
                    var exeDir = Path.GetDirectoryName(Sys.Settings.FF7Exe);

                    if (isSteamVersion)
                    {
                        var steamAppIdPath = Path.Join(exeDir, "steam_appid.txt");
                        if(!File.Exists(steamAppIdPath))
                        {
                            //Write steam_appid.txt to the exe directory, which prevents the game from re-opening through Steam
                            await File.WriteAllTextAsync(steamAppIdPath, "39140");
                        }
                    }
                    // Start game directly
                    ProcessStartInfo startInfo = new ProcessStartInfo(Sys.Settings.FF7Exe)
                    {
                        WorkingDirectory = exeDir,
                        UseShellExecute = true,
                    };
                    ff7Proc = Process.Start(startInfo);
                }
                else
                {
                    // Start game via Steam
                    ProcessStartInfo startInfo = new ProcessStartInfo(GameConverter.GetSteamExePath())
                    {
                        WorkingDirectory = GameConverter.GetSteamPath(),
                        UseShellExecute = true,
                        Arguments = "-applaunch 39140"
                    };
                    Process.Start(startInfo);

                    // Wait for game process
                    Process game = await waitForProcess(Path.GetFileNameWithoutExtension(Sys.Settings.FF7Exe));
                    if (game != null)
                    {
                        ff7Proc = game;
                    }
                }

                ff7Proc.EnableRaisingEvents = true;
                ff7Proc.Exited += (o, e) =>
                {
                    try
                    {
                        if (!IsFF7Running() && Instance._controllerInterceptor != null)
                        {
                            // stop polling for input once all ff7 procs are closed (could be multiple instances open)
                            Instance._controllerInterceptor.PollingInput = false;
                        }

                        // ensure Reunion is re-enabled when ff7 process exits in case it failed above for any reason
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(Sys.Settings.FF7Exe), "Reunion.dll.bak")))
                        {
                            EnableOrDisableReunionMod(doEnable: true);
                        }

                        // cleanup
                        Delete7thWrapperDlls();
                        ReShadeUpdater.Cleanup();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                };

                return true;
            }
            catch (Exception ex)
            {
                Instance.RaiseProgressChanged($"{ResourceHelper.Get(StringKey.AnExceptionOccurredTryingToStartFf7At)} {Sys.Settings.FF7Exe} ...", NLog.LogLevel.Error);
                Logger.Error(ex);
                return false;
            }
        }

        internal static bool SanityCheckSettings()
        {
            List<string> changes = new List<string>();
            foreach (var constraint in GetConstraints())
            {
                if (!constraint.Verify(out string msg))
                {
                    Logger.Warn(msg);
                    MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ThereWasAnErrorVerifyingModConstraintSeeTheFollowingDetails), msg, ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (msg != null)
                {
                    changes.Add(msg);
                }
            }

            if (changes.Any())
            {
                Logger.Warn($"{ResourceHelper.Get(StringKey.TheFollowingSettingsHaveBeenChangedToMakeTheseModsCompatible)}\n{String.Join("\n", changes)}");
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.TheFollowingSettingsHaveBeenChangedToMakeTheseModsCompatible), String.Join("\n", changes), ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return true;
        }

        internal static bool SanityCheckCompatibility()
        {
            if (Sys.Settings.HasOption(GeneralOptions.BypassCompatibility))
            {
                return true; // user has 'ignore compatibility restrictions' set in general settings so just return true
            }

            List<InstalledItem> activeInstalledMods = Sys.ActiveProfile.ActiveItems.Select(pi => Sys.Library.GetItem(pi.ModID)).ToList();
            var vars = new List<AppWrapper.Variable>();

            foreach (InstalledItem item in activeInstalledMods)
            {
                ModInfo info = item.GetModInfo();
                var mod = Sys.Library.GetItem(item.ModID);
                string location = System.IO.Path.Combine(Sys.Settings.LibraryLocation, mod.LatestInstalled.InstalledLocation);

                if (mod.LatestInstalled.InstalledLocation.EndsWith(".iro"))
                {
                    using (var arc = new AppWrapper.IrosArc(location))
                    {
                        if (arc.HasFile("mod.xml"))
                        {
                            var doc = new System.Xml.XmlDocument();
                            doc.Load(arc.GetData("mod.xml"));
                            foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/Variable"))
                            {
                                var currentVar = new AppWrapper.Variable() { Name = xmlNode.Attributes.GetNamedItem("Name").Value, Value = item.ModID.ToString() };
                                var test = vars.Find(var => var.Name == currentVar.Name);
                                if (test == null){
                                    vars.Add(currentVar);
                                }
                                else
                                {
                                    //@todo create StringKey resource for unknown incompatible mod
                                    MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleAnotherModVariableUsed), item.CachedDetails.Name, Sys.Library.GetItem(new Guid(test.Value)).CachedDetails.Name, currentVar.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                                    return false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    string mfile = System.IO.Path.Combine(location, "mod.xml");
                    if (System.IO.File.Exists(mfile))
                    {
                        var doc = new System.Xml.XmlDocument();
                        doc.Load(mfile);
                        foreach (XmlNode xmlNode in doc.SelectNodes("/ModInfo/Variable"))
                        {
                            var currentVar = new AppWrapper.Variable() { Name = xmlNode.Attributes.GetNamedItem("Name").Value, Value = item.ModID.ToString() };
                            var test = vars.Find(var => var.Name == currentVar.Name);
                            if (test == null)
                            {
                                vars.Add(currentVar);
                            }
                            else
                            {
                                MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleAnotherModVariableUsed), item.CachedDetails.Name, Sys.Library.GetItem(new Guid(test.Value)).CachedDetails.Name, currentVar.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                                return false;
                            }
                        }
                    }

                }

                if (info == null)
                {
                    continue;
                }

                foreach (var req in info.Compatibility.Requires)
                {
                    var rInst = activeInstalledMods.Find(i => i.ModID.Equals(req.ModID));
                    if (rInst == null)
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModRequiresYouToActivateAsWell), item.CachedDetails.Name, req.Description), ResourceHelper.Get(StringKey.MissingRequiredActiveMods));
                        return false;
                    }
                    else if (req.Versions.Any() && !req.Versions.Contains(rInst.LatestInstalled.VersionDetails.Version))
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModRequiresYouToActivateButYouDoNotHaveCompatibleVersionInstalled), item.CachedDetails.Name, rInst.CachedDetails.Name), ResourceHelper.Get(StringKey.UnsupportedModVersion));
                        return false;
                    }
                }

                foreach (var forbid in info.Compatibility.Forbids)
                {
                    var rInst = activeInstalledMods.Find(i => i.ModID.Equals(forbid.ModID));
                    if (rInst == null)
                    {
                        continue; //good!
                    }

                    if (forbid.Versions.Any() && forbid.Versions.Contains(rInst.LatestInstalled.VersionDetails.Version))
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleWithTheVersionYouHaveInstalled), item.CachedDetails.Name, rInst.CachedDetails.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                        return false;
                    }
                    else
                    {
                        MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ModIsNotCompatibleWithYouWillNeedToDisableIt), item.CachedDetails.Name, rInst.CachedDetails.Name), ResourceHelper.Get(StringKey.IncompatibleMod));
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool VerifyOrdering()
        {
            var details = Sys.ActiveProfile
                             .ActiveItems
                             .Select(i => Sys.Library.GetItem(i.ModID))
                             .Select(ii => new { Mod = ii, Info = ii.GetModInfo() })
                             .ToDictionary(a => a.Mod.ModID, a => a);

            List<string> problems = new List<string>();

            foreach (int i in Enumerable.Range(0, Sys.ActiveProfile.ActiveItems.Count))
            {
                Iros.Workshop.ProfileItem mod = Sys.ActiveProfile.ActiveItems[i];
                var info = details[mod.ModID].Info;

                if (info == null)
                {
                    continue;
                }

                foreach (Guid after in info.OrderAfter)
                {
                    if (Sys.ActiveProfile.ActiveItems.Skip(i).Any(pi => pi.ModID.Equals(after)))
                    {
                        problems.Add(string.Format(ResourceHelper.Get(StringKey.ModIsMeantToComeBelowModInTheLoadOrder), details[mod.ModID].Mod.CachedDetails.Name, details[after].Mod.CachedDetails.Name));
                    }
                }

                foreach (Guid before in info.OrderBefore)
                {
                    if (Sys.ActiveProfile.ActiveItems.Take(i).Any(pi => pi.ModID.Equals(before)))
                    {
                        problems.Add(string.Format(ResourceHelper.Get(StringKey.ModIsMeantToComeAboveModInTheLoadOrder), details[mod.ModID].Mod.CachedDetails.Name, details[before].Mod.CachedDetails.Name));
                    }
                }
            }

            if (problems.Any())
            {
                if (MessageDialogWindow.Show(ResourceHelper.Get(StringKey.TheFollowingModsWillNotWorkProperlyInTheCurrentOrder), String.Join("\n", problems), ResourceHelper.Get(StringKey.LoadOrderIncompatible), MessageBoxButton.YesNo).Result != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        internal static List<Constraint> GetConstraints()
        {
            List<Constraint> constraints = new List<Constraint>();
            foreach (Iros.Workshop.ProfileItem pItem in Sys.ActiveProfile.ActiveItems)
            {
                InstalledItem inst = Sys.Library.GetItem(pItem.ModID);
                ModInfo info = inst.GetModInfo();

                if (info == null)
                {
                    continue;
                }

                foreach (var cSetting in info.Compatibility.Settings)
                {
                    if (!String.IsNullOrWhiteSpace(cSetting.MyID))
                    {
                        var setting = pItem.Settings.Find(s => s.ID.Equals(cSetting.MyID, StringComparison.InvariantCultureIgnoreCase));
                        if ((setting == null) || (setting.Value != cSetting.MyValue)) continue;
                    }

                    Iros.Workshop.ProfileItem oItem = Sys.ActiveProfile.ActiveItems.Find(i => i.ModID.Equals(cSetting.ModID));
                    if (oItem == null) continue;

                    InstalledItem oInst = Sys.Library.GetItem(cSetting.ModID);
                    Constraint ct = constraints.Find(c => c.ModID.Equals(cSetting.ModID) && c.Setting.Equals(cSetting.TheirID, StringComparison.InvariantCultureIgnoreCase));
                    if (ct == null)
                    {
                        ct = new Constraint() { ModID = cSetting.ModID, Setting = cSetting.TheirID };
                        constraints.Add(ct);
                    }

                    if (!ct.ParticipatingMods.ContainsKey(inst.CachedDetails.ID))
                    {
                        ct.ParticipatingMods.Add(inst.CachedDetails.ID, inst.CachedDetails.Name);
                    }

                    if (cSetting.Require.HasValue)
                    {
                        ct.Require.Add(cSetting.Require.Value);
                    }

                    foreach (var f in cSetting.Forbid)
                    {
                        ct.Forbid.Add(f);
                    }
                }

                foreach (var setting in info.Options)
                {
                    Constraint ct = constraints.Find(c => c.ModID.Equals(pItem.ModID) && c.Setting.Equals(setting.ID, StringComparison.InvariantCultureIgnoreCase));
                    if (ct == null)
                    {
                        ct = new Constraint() { ModID = pItem.ModID, Setting = setting.ID };
                        constraints.Add(ct);
                    }
                    ct.Option = setting;
                }

            }

            return constraints;
        }

        public static bool IsReunionModInstalled()
        {
            string installPath = Path.GetDirectoryName(Sys.Settings.FF7Exe);


            if (!Directory.Exists(installPath))
            {
                return false;
            }

            return Directory.GetFiles(installPath).Any(s => new FileInfo(s).Name.Equals("ddraw.dll", StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool EnableOrDisableReunionMod(bool doEnable)
        {
            string installPath = Path.GetDirectoryName(Sys.Settings.FF7Exe);

            if (!Directory.Exists(installPath))
            {
                return true;
            }

            try
            {
                string pathToDll = Path.Combine(installPath, "ddraw.dll");
                string backupName = Path.Combine(installPath, "Reunion.dll.bak");

                // disable Reunion by renaming ddraw.dll to Reunion.dll.bak
                if (!doEnable)
                {
                    if (File.Exists(pathToDll))
                    {
                        File.Move(pathToDll, backupName);
                        return true;
                    }
                    else
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.CouldNotFindDdrawDllAt)} {pathToDll}", NLog.LogLevel.Warn);
                        return false;
                    }
                }
                else
                {
                    if (File.Exists(backupName))
                    {
                        File.Move(backupName, pathToDll);
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Renamed)} {backupName} -> {pathToDll}");
                        return true;
                    }
                    else
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.CouldNotFindReunionDllBakAt)} {backupName}", NLog.LogLevel.Warn);
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Scans all drives looking for the drive labeled "FF7DISC1", "FF7DISC2", or "FF7DISC3" and returns the corresponding drive letter.
        /// If not found returns empty string. Returns the drive letter for <paramref name="labelToFind"/> if not null
        /// </summary>
        public static string GetDriveLetter(string labelToFind = null)
        {
            List<string> labels = null;
            if (string.IsNullOrWhiteSpace(labelToFind))
            {
                labels = new List<string>() { "FF7DISC1", "FF7DISC2", "FF7DISC3" };
            }
            else
            {
                labels = new List<string>() { labelToFind };
            }

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && labels.Any(s => s.Equals(drive.VolumeLabel, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return drive.Name;
                }
            }

            return "";
        }

        /// <summary>
        /// Scans all drives looking for the drive labeled "FF7DISC1", "FF7DISC2", or "FF7DISC3" and returns the corresponding drive letters that have the matching label.
        /// If not found returns empty string. Returns the drive letter for <paramref name="labelToFind"/> if not null
        /// </summary>
        public static List<string> GetDriveLetters(string labelToFind = null)
        {
            List<string> labels = null;
            if (string.IsNullOrWhiteSpace(labelToFind))
            {
                labels = new List<string>() { "FF7DISC1", "FF7DISC2", "FF7DISC3" };
            }
            else
            {
                labels = new List<string>() { labelToFind };
            }

            List<string> drivesFound = new List<string>();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && labels.Any(s => s.Equals(drive.VolumeLabel, StringComparison.InvariantCultureIgnoreCase)))
                {
                    drivesFound.Add(drive.Name);
                }
            }

            return drivesFound;
        }

        /// <summary>
        /// Updates Registry with new values from <see cref="Sys.Settings.GameLaunchSettings"/>
        /// </summary>
        public void SetRegistryValues()
        {
            Instance.RaiseProgressChanged(ResourceHelper.Get(StringKey.ApplyingChangedValuesToRegistry));

            RegistryHelper.BeginTransaction();

            string ff7KeyPath = $"{RegistryHelper.GetKeyPath(FF7RegKey.SquareSoftKeyPath)}\\Final Fantasy VII";
            string virtualStorePath = $"{RegistryHelper.GetKeyPath(FF7RegKey.VirtualStoreKeyPath)}\\Final Fantasy VII";

            string installPath = Path.GetDirectoryName(Sys.Settings.FF7Exe);
            string pathToData = Path.Combine(installPath, @"data\");
            string pathToMovies = Sys.PathToFF7Movies;

            if (installPath != null && !installPath.EndsWith(@"\"))
            {
                installPath += @"\";
            }

            if (pathToMovies != null && !pathToMovies.EndsWith(@"\"))
            {
                pathToMovies += @"\";
            }


            // Add registry key values for paths and drive letter
            SetValueIfChanged(ff7KeyPath, "AppPath", installPath);
            SetValueIfChanged(virtualStorePath, "AppPath", installPath);

            SetValueIfChanged(ff7KeyPath, "DataPath", pathToData);
            SetValueIfChanged(virtualStorePath, "DataPath", pathToData);

            SetValueIfChanged(ff7KeyPath, "MoviePath", pathToMovies);
            SetValueIfChanged(virtualStorePath, "MoviePath", pathToMovies);

            // setting the drive letter may not happen if auto update disc path is not set
            if (Sys.Settings.GameLaunchSettings.AutoUpdateDiscPath && !string.IsNullOrWhiteSpace(DriveLetter))
            {
                SetValueIfChanged(ff7KeyPath, "DataDrive", DriveLetter);
                SetValueIfChanged(virtualStorePath, "DataDrive", DriveLetter);
            }

            SetValueIfChanged(ff7KeyPath, "DiskNo", 0, RegistryValueKind.DWord);
            SetValueIfChanged(virtualStorePath, "DiskNo", 0, RegistryValueKind.DWord);

            SetValueIfChanged(ff7KeyPath, "FullInstall", 1, RegistryValueKind.DWord);
            SetValueIfChanged(virtualStorePath, "FullInstall", 1, RegistryValueKind.DWord);


            if (Environment.Is64BitOperatingSystem)
            {
                SetValueIfChanged(RegistryHelper.FF7AppKeyPath64Bit, "Path", installPath.TrimEnd('\\'));
                SetValueIfChanged(RegistryHelper.FF7AppKeyPath32Bit, "Path", installPath.TrimEnd('\\'));
            }
            else
            {
                SetValueIfChanged(RegistryHelper.FF7AppKeyPath32Bit, "Path", installPath.TrimEnd('\\'));
            }


            // Add registry key values for Graphics
            string graphicsKeyPath = $"{ff7KeyPath}\\1.00\\Graphics";
            string graphicsVirtualKeyPath = $"{virtualStorePath}\\1.00\\Graphics";

            SetValueIfChanged(graphicsKeyPath, "Driver", (int)GraphicsRenderer.CustomDriver, RegistryValueKind.DWord);
            SetValueIfChanged(graphicsVirtualKeyPath, "Driver", (int)GraphicsRenderer.CustomDriver, RegistryValueKind.DWord);

            SetValueIfChanged(graphicsKeyPath, "DriverPath", "FFNx.dll");
            SetValueIfChanged(graphicsVirtualKeyPath, "DriverPath", "FFNx.dll");

            SetValueIfChanged(graphicsKeyPath, "Mode", 2, RegistryValueKind.DWord);
            SetValueIfChanged(graphicsVirtualKeyPath, "Mode", 2, RegistryValueKind.DWord);

            SetValueIfChanged(graphicsKeyPath, "Options", 0, RegistryValueKind.DWord);
            SetValueIfChanged(graphicsVirtualKeyPath, "Options", 0, RegistryValueKind.DWord);

            Guid emptyGuidBytes = Guid.Empty;

            SetValueIfChanged(graphicsKeyPath, "DD_GUID", emptyGuidBytes, RegistryValueKind.Binary);
            SetValueIfChanged(graphicsVirtualKeyPath, "DD_GUID", emptyGuidBytes, RegistryValueKind.Binary);

            // Add registry key values for MIDI
            string midiKeyPath = $"{ff7KeyPath}\\1.00\\MIDI";
            string midiVirtualKeyPath = $"{virtualStorePath}\\1.00\\MIDI";

            // Add registry key values for default MIDI Device if missing from registry
            if (RegistryHelper.GetValue(midiKeyPath, "MIDI_DeviceID", null) == null || RegistryHelper.GetValue(midiVirtualKeyPath, "MIDI_DeviceID", null) == null)
            {
                SetValueIfChanged(midiKeyPath, "MIDI_DeviceID", 0x00000000, RegistryValueKind.DWord);
                SetValueIfChanged(midiVirtualKeyPath, "MIDI_DeviceID", 0x00000000, RegistryValueKind.DWord);
            }

            SetValueIfChanged(midiKeyPath, "MIDI_data", Sys.Settings.GameLaunchSettings.SelectedMidiData);
            SetValueIfChanged(midiVirtualKeyPath, "MIDI_data", Sys.Settings.GameLaunchSettings.SelectedMidiData);

            if (Sys.Settings.GameLaunchSettings.LogarithmicVolumeControl)
            {
                SetValueIfChanged(midiKeyPath, "Options", 0x00000001, RegistryValueKind.DWord);
                SetValueIfChanged(midiVirtualKeyPath, "Options", 0x00000001, RegistryValueKind.DWord);
            }
            else
            {
                SetValueIfChanged(midiKeyPath, "Options", 0x00000000, RegistryValueKind.DWord);
                SetValueIfChanged(midiVirtualKeyPath, "Options", 0x00000000, RegistryValueKind.DWord);
            }

            // Add registry key values for Sound
            string soundKeyPath = $"{ff7KeyPath}\\1.00\\Sound";
            string soundVirtualKeyPath = $"{virtualStorePath}\\1.00\\Sound";

            Guid soundGuidBytes = Sys.Settings.GameLaunchSettings.SelectedSoundDevice;

            SetValueIfChanged(soundKeyPath, "Sound_GUID", soundGuidBytes, RegistryValueKind.Binary);
            SetValueIfChanged(soundVirtualKeyPath, "Sound_GUID", soundGuidBytes, RegistryValueKind.Binary);

            if (Sys.Settings.GameLaunchSettings.ReverseSpeakers)
            {
                SetValueIfChanged(soundKeyPath, "Options", 0x00000001, RegistryValueKind.DWord);
                SetValueIfChanged(soundVirtualKeyPath, "Options", 0x00000001, RegistryValueKind.DWord);
            }
            else
            {
                SetValueIfChanged(soundKeyPath, "Options", 0x00000000, RegistryValueKind.DWord);
                SetValueIfChanged(soundVirtualKeyPath, "Options", 0x00000000, RegistryValueKind.DWord);
            }

            // Add registry key values for Sound/Music volume if missing from registry (can happen on fresh install)
            if (RegistryHelper.GetValue(soundKeyPath, "SFXVolume", null) == null || RegistryHelper.GetValue(soundVirtualKeyPath, "SFXVolume", null) == null)
            {
                SetValueIfChanged(soundKeyPath, "SFXVolume", 100, RegistryValueKind.DWord);
                SetValueIfChanged(soundVirtualKeyPath, "SFXVolume", 100, RegistryValueKind.DWord);
            }

            if (RegistryHelper.GetValue(midiKeyPath, "MusicVolume", null) == null || RegistryHelper.GetValue(midiVirtualKeyPath, "MusicVolume", null) == null)
            {
                SetValueIfChanged(midiKeyPath, "MusicVolume", 100, RegistryValueKind.DWord);
                SetValueIfChanged(midiVirtualKeyPath, "MusicVolume", 100, RegistryValueKind.DWord);
            }

            RegistryHelper.CommitTransaction();
        }

        /// <summary>
        /// Update Registry with new value if it has changed from the current value in the Registry.
        /// Logs the changed value.
        /// </summary>
        private void SetValueIfChanged(string regKeyPath, string regValueName, object newValue, RegistryValueKind valueKind = RegistryValueKind.String)
        {
            if (RegistryHelper.SetValueIfChanged(regKeyPath, regValueName, newValue, valueKind))
            {
                string valueFormatted = newValue?.ToString(); // used to display the object value correctly in the log i.e. for a byte[] convert it to readable string

                if (newValue is byte[])
                {
                    valueFormatted = newValue == null ? "" : BitConverter.ToString(newValue as byte[]);
                }

                Instance.RaiseProgressChanged($"\t {regKeyPath}::{regValueName} = {valueFormatted}");
            }
        }

        public static bool CopyKeyboardInputCfg()
        {
            Directory.CreateDirectory(Sys.PathToControlsFolder);
            string pathToCfg = Path.Combine(Sys.PathToControlsFolder, Sys.Settings.GameLaunchSettings.InGameConfigOption);

            Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.UsingControlConfigurationFile)} {Sys.Settings.GameLaunchSettings.InGameConfigOption} ...");
            if (!File.Exists(pathToCfg))
            {
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.InputCfgFileNotFoundAt)} {pathToCfg}", NLog.LogLevel.Warn);
                return false;
            }

            try
            {
                string targetPath = Path.Combine(
                    Sys.Settings.FF7InstalledVersion == FF7Version.Steam ? GameConverter.GetSteamFF7UserPath() : Path.GetDirectoryName(Sys.Settings.FF7Exe),
                    "ff7input.cfg"
                );

                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.Copying)} {pathToCfg} -> {targetPath} ...");
                File.Copy(pathToCfg, targetPath, true);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.FailedToCopyCfgFile)}: {e.Message}", NLog.LogLevel.Error);
                return false;
            }

            return true;
        }

        public static bool IsFF7Running()
        {
            bool ret = false;

            string fileName = Path.GetFileNameWithoutExtension(Sys.Settings.FF7Exe);
            ret = Process.GetProcessesByName(fileName).Length > 0;

            if (!ret && Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
            {
                ret = Process.GetProcessesByName("FF7_Launcher").Length > 0;
            }

            return ret;
        }

        private static bool ForceKillFF7()
        {
            string fileName = Path.GetFileNameWithoutExtension(Sys.Settings.FF7Exe);

            try
            {
                foreach (Process item in Process.GetProcessesByName(fileName))
                {
                    item.Kill();
                }

                if (Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
                {
                    foreach (Process item in Process.GetProcessesByName("FF7_Launcher"))
                    {
                        item.Kill();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }


        /// <summary>
        /// Kills any currently running process found in <see cref="_sideLoadProcesses"/>
        /// </summary>
        private void StopAllSideProcessesForMods()
        {
            foreach (var valuePair in _sideLoadProcesses.ToList())
            {
                AppWrapper.ProgramInfo info = valuePair.Key;
                Process sideProc = valuePair.Value;
                string procName = sideProc.ProcessName;

                if (!sideProc.HasExited)
                {
                    sideProc.Kill();
                }

                // Kill all instances with same process name if necessary
                if (info.CloseAllInstances)
                {
                    foreach (Process otherProc in Process.GetProcessesByName(procName))
                    {
                        if (!otherProc.HasExited)
                            otherProc.Kill();
                    }
                }
            }
        }

        private void CollectCrashReport()
        {
            // Cleanup any old report older than 1 day

            DirectoryInfo dir = new DirectoryInfo(Sys.PathToCrashReports);
            FileInfo[] files = dir.GetFiles().Where(file => file.CreationTime < DateTime.Now.AddDays(-1)).ToArray();
            foreach (FileInfo file in files) file.Delete();

            // Create the new report

            var zipOutPath = Path.Combine(Sys.PathToCrashReports, $"7thCrashReport-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.zip");

            // Flush logs before saving in order to obtain any possible leftover in memory
            Logger.Factory.Flush(0);

            using (var archive = ZipArchive.Create())
            {
                // === FF7 files ===
                if (Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
                {
                    var di = new DirectoryInfo(GameConverter.GetSteamFF7UserPath());
                    foreach (FileInfo fi in di.GetFiles("*.ff7", SearchOption.AllDirectories))
                    {
                        archive.AddEntry(Path.Combine("save", Path.GetFileName(fi.FullName)), fi.FullName);
                    }
                }
                else
                {
                    var savePath = Path.Combine(Sys.InstallPath, "save");
                    if (Directory.Exists(savePath))
                    {
                        var saveFiles = Directory.GetFiles(savePath);
                        foreach (var file in saveFiles)
                        {
                            archive.AddEntry(Path.Combine("save", Path.GetFileName(file)), file);
                        }
                    }
                }

                // === FFNx files ===
                if (File.Exists(Sys.PathToFFNxLog)) archive.AddEntry("FFNx.log", Sys.PathToFFNxLog);
                archive.AddEntry("FFNx.toml", Sys.PathToFFNxToml);

                // === App files ===
                archive.AddEntry("AppLoader.log", Path.Combine(Sys.InstallPath, "AppLoader.log"));
                archive.AddEntry("applog.txt", File.Open(Sys.PathToApplog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                archive.AddEntry("settings.xml", Sys.PathToSettings);

                string rt = Path.Combine(Sys.PathToTempFolder, "registry_transaction.bat");
                if (File.Exists(rt)) archive.AddEntry(Path.GetFileName(rt), rt);

                // Convert profile.xml to profile.txt
                Profile currentProfile = Util.Deserialize<Profile>(Sys.PathToCurrentProfileFile);
                IEnumerable<string> profileDetails = currentProfile.GetDetails();
                File.WriteAllLines(Path.Combine(Sys.PathToTempFolder, "profile.txt"), profileDetails);
                archive.AddEntry("profile.txt", Path.Combine(Sys.PathToTempFolder, "profile.txt"));

                // =================================================================================================

                archive.SaveTo(zipOutPath, CompressionType.Deflate);
            }
        }

        internal void LaunchProgramsForMod(RuntimeMod mod)
        {
            if (!mod.LoadPrograms.Any())
            {
                return;
            }

            mod.Startup();

            foreach (var program in mod.GetLoadPrograms())
            {
                if (!_sideLoadProcesses.ContainsKey(program))
                {
                    Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.StartingProgram)}: {program.PathToProgram}");
                    ProcessStartInfo psi = new ProcessStartInfo()
                    {
                        WorkingDirectory = Path.GetDirectoryName(program.PathToProgram),
                        FileName = program.PathToProgram,
                        Arguments = program.ProgramArgs,
                        UseShellExecute = true,
                    };
                    Process aproc = Process.Start(psi);

                    aproc.EnableRaisingEvents = true;
                    aproc.Exited += (_o, _e) => _sideLoadProcesses.Remove(program);

                    _sideLoadProcesses.Add(program, aproc);
                    Instance.RaiseProgressChanged($"\t\t{ResourceHelper.Get(StringKey.Started)}");

                    IntPtr mainWindowHandle = IntPtr.Zero;

                    if (program.WaitForWindowToShow)
                    {
                        Instance.RaiseProgressChanged($"\t\t{ResourceHelper.Get(StringKey.WaitingForProgramToInitializeAndShow)}");

                        DateTime startTime = DateTime.Now;

                        while (aproc.MainWindowHandle == IntPtr.Zero && mainWindowHandle == IntPtr.Zero)
                        {
                            if (program.WaitTimeOutInSeconds != 0 && DateTime.Now.Subtract(startTime).TotalSeconds > program.WaitTimeOutInSeconds)
                            {
                                Instance.RaiseProgressChanged($"\t\t{string.Format(ResourceHelper.Get(StringKey.TimeoutReachedWaitingForProgramToShow), program.WaitTimeOutInSeconds)}", NLog.LogLevel.Warn);
                                break;
                            }

                            aproc.Refresh();

                            if (!string.IsNullOrWhiteSpace(program.WindowTitle))
                            {
                                // attempt to find main window handle based on the Window Title of the process
                                // ... necessary because some programs (Speed Hack) will open multiple processes and the Main Window Handle needs to be found since it is different from the original process started
                                mainWindowHandle = FindWindow(null, program.WindowTitle);
                            }

                        };
                    }

                    // force the process to become minimized
                    if (aproc.MainWindowHandle != IntPtr.Zero || mainWindowHandle != IntPtr.Zero)
                    {
                        Logger.Info($"\t\t{ResourceHelper.Get(StringKey.ForceMinimizingProgram)}");
                        IntPtr handleToMinimize = aproc.MainWindowHandle != IntPtr.Zero ? aproc.MainWindowHandle : mainWindowHandle;

                        System.Threading.Thread.Sleep(1500); // add a small delay to ensure the program is showing in taskbar before force minimizing;
                        ShowWindow(handleToMinimize.ToInt32(), SW_FORCEMINIMIZE);
                    }
                }
                else
                {
                    if (!_sideLoadProcesses[program].HasExited)
                    {
                        Instance.RaiseProgressChanged($"\t{ResourceHelper.Get(StringKey.ProgramAlreadyRunning)}: {program.PathToProgram}", NLog.LogLevel.Warn);
                    }
                }
            }
        }


        /// <summary>
        /// Starts the processes with the specified arguments that are set in <see cref="Sys.Settings.ProgramsToLaunchPrior"/>.
        /// </summary>
        internal void LaunchAdditionalProgramsToRunPrior()
        {
            // launch other processes set in settings
            foreach (ProgramLaunchInfo al in Sys.Settings.ProgramsToLaunchPrior.Where(s => !String.IsNullOrWhiteSpace(s.PathToProgram)))
            {
                try
                {
                    if (!_alsoLaunchProcesses.ContainsKey(al.PathToProgram))
                    {
                        ProcessStartInfo psi = new ProcessStartInfo()
                        {
                            WorkingDirectory = Path.GetDirectoryName(al.PathToProgram),
                            FileName = al.PathToProgram,
                            Arguments = al.ProgramArgs,
                            UseShellExecute = true,
                        };
                        Process aproc = Process.Start(psi);

                        _alsoLaunchProcesses.Add(al.PathToProgram, aproc);
                        aproc.EnableRaisingEvents = true;
                        aproc.Exited += (_o, _e) => _alsoLaunchProcesses.Remove(al.PathToProgram);
                    }
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    Instance.RaiseProgressChanged($"\t{string.Format(ResourceHelper.Get(StringKey.FailedToStartAdditionalProgram), al.PathToProgram)}", NLog.LogLevel.Warn);
                }
            }
        }

        internal void RaiseProgressChanged(string messageToLog, NLog.LogLevel logLevel = null)
        {
            if (logLevel == null)
            {
                logLevel = NLog.LogLevel.Info;
            }

            Logger.Log(logLevel, messageToLog);
            ProgressChanged?.Invoke(messageToLog);
        }

        internal void RaiseLaunchCompleted(bool didLaunch)
        {
            LaunchCompleted?.Invoke(didLaunch);
        }



    }
}
