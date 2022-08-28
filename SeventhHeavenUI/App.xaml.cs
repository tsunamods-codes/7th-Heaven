using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
//using SeventhHeaven.Classes.WCF;
using SeventhHeaven.ViewModels;
using SeventhHeaven.Windows;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SeventhHeavenUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public const string uniqueAppGuid = "F73958FA-160F-4185-AE8F-CF5B7EA89494";

        public static Mutex uniqueMutex;

        private bool hasShownErrorWindow = false;

        public App()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            uniqueMutex = new Mutex(true, uniqueAppGuid, out bool gotMutex);
            GC.KeepAlive(App.uniqueMutex);

            //TODO: Add support for .NET 6.0
            //if (SingleInstance.IsFirstInstance(uniqueAppGuid, true))
            //{
            //    SingleInstance.OnSecondInstanceStarted += SingleInstance_OnSecondInstanceStarted;
            //}
            //else
            //{
            //    // second instance so notify first instance
            //    SingleInstance.NotifyFirstInstance(uniqueAppGuid);
            //}

            if (!gotMutex)
            {
                App.Current.Shutdown(); // only one instance of the app opened at a time so shutdown
                return;
            }
        }

        //TODO: Add support for .NET 6.0
        //private void SingleInstance_OnSecondInstanceStarted(object sender, SecondInstanceStartedEventArgs e)
        //{
        //    // e.Args[0] = path to 7th Heaven .exe
        //    ProcessCommandLineArgs(e.Args);
        //}

        internal static void ProcessCommandLineArgs(string[] args, bool closeAfterProcessing = false)
        {
            bool hasLaunchCommand = args.Any(s => s.Equals("/LAUNCH", StringComparison.InvariantCultureIgnoreCase));

            foreach (string parm in args)
            {
                if (parm.StartsWith("iros://", StringComparison.InvariantCultureIgnoreCase))
                {
                    // check if its a catalog url or a mod download url
                    if (parm.StartsWith("iros://mod/"))
                    {

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Mod newMod = new Mod()
                            {
                                Name = Path.GetFileName(parm),
                                ID = Guid.NewGuid(),
                                LatestVersion = new ModVersion()
                                {
                                    Links = new System.Collections.Generic.List<string>() { parm.Replace("iros://mod/", "iros://") },
                                    Version = 1,
                                },
                            };

                            Install.DownloadAndInstall(newMod);

                            MainWindow window = App.Current.MainWindow as MainWindow;
                            window.ViewModel.SelectedTabIndex = 1;
                        });
                    }
                    else
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            MainWindow window = App.Current.MainWindow as MainWindow;
                            window.ViewModel.AddIrosUrlToSubscriptions(parm);
                        });
                    }

                }
                else if (parm.Equals("/MINI", StringComparison.InvariantCultureIgnoreCase))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        App.Current.MainWindow.WindowState = WindowState.Minimized;
                    });
                }
                else if (parm.StartsWith("/PROFILE:", StringComparison.InvariantCultureIgnoreCase))
                {
                    try
                    {
                        string profileToLoad = Path.Combine(Sys.PathToProfiles, $"{parm.Substring(9)}.xml");
                        Profile profile = Util.Deserialize<Profile>(profileToLoad);

                        Sys.Settings.CurrentProfile = parm.Substring(9);
                        Sys.ActiveProfile = profile;

                        Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.LoadedProfile)} {Sys.Settings.CurrentProfile}"));
                        Sys.ActiveProfile.RemoveDeletedItems(doWarn: true);

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            MainWindow window = App.Current.MainWindow as MainWindow;
                            window.ViewModel.RefreshProfile();
                        });
                    }
                    catch (Exception e)
                    {
                        Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.CouldNotLoadProfile)} {parm.Substring(9)}", WMessageLogLevel.Error, e));
                    }
                }
                else if (parm.Equals("/LAUNCH", StringComparison.InvariantCultureIgnoreCase))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow window = App.Current.MainWindow as MainWindow;
                        window.ViewModel.LaunchGame();
                    });
                }
                else if (parm.Equals("/QUIT", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (hasLaunchCommand)
                    {
                        bool isGameLaunching = false;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            MainWindow window = App.Current.MainWindow as MainWindow;
                            isGameLaunching = window.ViewModel.IsGameLaunching;
                        });

                        while (isGameLaunching || GameLauncher.IsFF7Running())
                        {
                            Thread.Sleep(10000); // sleep 10 seconds and check again until ff7 not running

                            if (isGameLaunching)
                            {
                                // only need to invoke this until game launcher is finished launching the game
                                App.Current.Dispatcher.Invoke(() =>
                                {
                                    MainWindow window = App.Current.MainWindow as MainWindow;
                                    isGameLaunching = window.ViewModel.IsGameLaunching;
                                });
                            }
                        }

                        // wait for ff7 to close before closing down 7th Heaven
                    }

                    ShutdownApp();
                }
                else if (parm.StartsWith("/OPENIRO:", StringComparison.InvariantCultureIgnoreCase))
                {
                    string irofile = null;
                    string irofilenoext = null;

                    try
                    {
                        irofile = parm.Substring(9);
                        irofilenoext = Path.GetFileNameWithoutExtension(irofile);
                        Logger.Info("Importing IRO from Windows " + irofile);

                        ModImporter.ImportMod(irofile, ModImporter.ParseNameFromFileOrFolder(irofilenoext), true, false);
                    }
                    catch (Exception ex)
                    {
                        Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.FailedToImportMod)} {irofilenoext}: {ex.Message}", true) { LoggedException = ex });
                        continue;
                    }

                    Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.AutoImportedMod)} {irofilenoext}", true));
                }
                else if (parm.StartsWith("/OPENIROP:", StringComparison.InvariantCultureIgnoreCase))
                {
                    string iropFile = null;
                    var importer = new ModImporter();


                    try
                    {
                        iropFile = parm.Substring(10);
                        Sys.Message(new WMessage($"Applying patch file {iropFile}", true));

                        importer.ImportProgressChanged += ModPatchImportProgressChanged;
                        bool didPatch = importer.ImportModPatch(iropFile);

                        if (didPatch)
                        {
                            Sys.Message(new WMessage($"Applied patch {iropFile}", true));
                        }
                        else
                        {
                            Sys.Message(new WMessage($"Failed to apply patch {iropFile}. Check applog for details.", true));
                        }
                    }
                    catch (Exception ex)
                    {
                        Sys.Message(new WMessage($"Failed to apply patch {iropFile}: {ex.Message}", true) { LoggedException = ex });
                        continue;
                    }
                    finally
                    {
                        importer.ImportProgressChanged -= ModPatchImportProgressChanged;
                    }
                }
                else if (parm.StartsWith("/PACKIRO:", StringComparison.InvariantCultureIgnoreCase))
                {
                    string pathToFolder;

                    try
                    {
                        pathToFolder = parm.Substring(9);
                        string folderName = new DirectoryInfo(pathToFolder).Name;
                        string parentDir = new DirectoryInfo(pathToFolder).Parent.FullName;

                        PackIroViewModel packViewModel = new PackIroViewModel()
                        {
                            PathToSourceFolder = pathToFolder,
                            PathToOutputFile = Path.Combine(parentDir, $"{folderName}.iro"),
                        };

                        Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.PackingIntoIro), folderName), true));

                        Task packTask = packViewModel.PackIro();
                        packTask.ContinueWith((result) =>
                        {
                            Sys.Message(new WMessage(packViewModel.StatusText, true));

                            if (closeAfterProcessing)
                            {
                                ShutdownApp();
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToPackIntoIro), WMessageLogLevel.Error, e) { IsImportant = true });
                    }
                }
                else if (parm.StartsWith("/UNPACKIRO:", StringComparison.InvariantCultureIgnoreCase))
                {
                    string pathToIro;

                    try
                    {
                        pathToIro = parm.Substring(11);
                        string fileName = Path.GetFileNameWithoutExtension(pathToIro);
                        string pathToDir = Path.GetDirectoryName(pathToIro);

                        UnpackIroViewModel unpackViewModel = new UnpackIroViewModel()
                        {
                            PathToIroFile = pathToIro,
                            PathToOutputFolder = Path.Combine(pathToDir, fileName),
                        };

                        Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.UnpackingIroIntoSubfolder), fileName), true));

                        Task unpackTask = unpackViewModel.UnpackIro();
                        unpackTask.ContinueWith((result) =>
                        {
                            Sys.Message(new WMessage(unpackViewModel.StatusText, true));

                            if (closeAfterProcessing)
                            {
                                ShutdownApp();
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnpackIro), WMessageLogLevel.Error, e) { IsImportant = true });
                    }
                }
            }
        }

        private static void ModPatchImportProgressChanged(string message, double percentComplete)
        {
            Logger.Info(message);
        }

        internal static void ShutdownApp()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                App.Current.Shutdown();
            });
        }

        public static Version GetAppVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            return new Version(fileVersionInfo.ProductVersion);
        }

        public static bool IsRunningAppAsAdministrator()
        {
            // reference: https://stackoverflow.com/questions/11660184/c-sharp-check-if-run-as-administrator
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal static string GetAppName()
        {
            foreach (object item in System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(false))
            {
                if (item is System.Reflection.AssemblyTitleAttribute)
                {
                    return (item as System.Reflection.AssemblyTitleAttribute).Title;
                }
            }

            return "7th Heaven"; // default if can't find for some reason
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Enable Visual styles for Winform applications to support plugins that uses Winforms as a UI
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // check if default language saved in app settings; otherwise detect language from thread
            string defaultLang = Sys.Settings.AppLanguage;

            if (string.IsNullOrWhiteSpace(defaultLang))
            {
                defaultLang = GetCultureFromCurrentThread();
            }

            SetLanguageDictionary(defaultLang);
            SetupExceptionHandling();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Dispatcher.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"! Unhandled exception ({source})";
            Logger.Error(message);
            Logger.Error(exception);

            if (!hasShownErrorWindow)
            {
                hasShownErrorWindow = true;
                UnhandledErrorWindow.Show(exception);
            }
        }

        public static void ForceUpdateUI()
        {
            DispatcherFrame frame = new DispatcherFrame();

            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
        }

        public static string GetCultureFromCurrentThread()
        {
            return Thread.CurrentThread.CurrentCulture.ToString();
        }

        internal void SetLanguageDictionary(string cultureCode)
        {
            ResourceDictionary dict = new ResourceDictionary();

            if (string.IsNullOrWhiteSpace(cultureCode) || cultureCode.Length < 2) cultureCode = "en";

            if (cultureCode == "pt-BR")
            {
                dict.Source = new Uri("Resources\\Languages\\StringResources.br.xaml", UriKind.Relative);
            }
            else
            {
                cultureCode = cultureCode.Substring(0, 2);

                switch (cultureCode)
                {
                    case "en":
                        dict.Source = new Uri("Resources\\StringResources.xaml", UriKind.Relative);
                        break;
                    case "fr":
                        dict.Source = new Uri("Resources\\Languages\\StringResources.fr.xaml", UriKind.Relative);
                        break;
                    case "es":
                        dict.Source = new Uri("Resources\\Languages\\StringResources.es.xaml", UriKind.Relative);
                        break;
                    case "de":
                        dict.Source = new Uri("Resources\\Languages\\StringResources.de.xaml", UriKind.Relative);
                        break;
                    case "gr":
                        dict.Source = new Uri("Resources\\Languages\\StringResources.gr.xaml", UriKind.Relative);
                        break;
                    case "it":
                        dict.Source = new Uri("Resources\\Languages\\StringResources.it.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("Resources\\StringResources.xaml", UriKind.Relative);
                        cultureCode = "en";
                        break;
                }
            }

            Sys.Settings.AppLanguage = cultureCode;

            this.Resources.MergedDictionaries.RemoveAt(1); // remove the default string resources dictionary (second in merged dictionary in App.xaml)
            this.Resources.MergedDictionaries.Add(dict);
        }

        public static string GetAppLanguage()
        {
            try
            {
                string defaultLang = Sys.Settings.AppLanguage;

                if (string.IsNullOrWhiteSpace(defaultLang))
                {
                    defaultLang = "en";
                }

                return defaultLang;
            }
            catch (Exception)
            {
                return "en";
            }
        }
    }
}
