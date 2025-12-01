using AppCore;
using Iros;
using Iros.Workshop;
using Microsoft.Win32;
using AppUI.Classes;
using AppUI.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace AppUI.ViewModels
{
    public class GeneralSettingsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _fF7ExePathInput;
        private string _libraryPathInput;
        private bool _autoSortModsByDefault;
        private bool _autoUpdateModsByDefault;
        private bool _activateInstalledModsAuto;
        private bool _importLibraryFolderAuto;
        private bool _checkForUpdatesAuto;
        private bool _launchExeDirectly;
        private bool _bypassCompatibilityLocks;
        private bool _openIrosLinks;
        private bool _openModFilesWith7H;
        private bool _warnAboutModCode;
        private bool _showContextMenuInExplorer;

        private ObservableCollection<SubscriptionSettingViewModel> _subscriptionList;
        private string _newUrlText;
        private string _newNameText;
        private bool _isSubscriptionPopupOpen;
        private bool _isResolvingName;
        private string _subscriptionNameHintText;
        private bool _subscriptionNameTextBoxIsEnabled;
        private ObservableCollection<string> _extraFolderList;
        private string _statusMessage;

        private FFNxUpdateChannelOptions _ffnxUpdateChannel;
        private AppUpdateChannelOptions _appUpdateChannel;

        public delegate void OnListDataChanged();

        /// <summary>
        /// Event raised when data is changed (added/edited/removed) from <see cref="SubscriptionList"/>
        /// </summary>
        public event OnListDataChanged ListDataChanged;

        public string FF7ExePathInput
        {
            get
            {
                return _fF7ExePathInput;
            }
            set
            {
                _fF7ExePathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string LibraryPathInput
        {
            get
            {
                return _libraryPathInput;
            }
            set
            {
                _libraryPathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                NotifyPropertyChanged();
            }
        }

        public FFNxUpdateChannelOptions FFNxUpdateChannel
        {
            get
            {
                return _ffnxUpdateChannel;
            }
            set
            {
                _ffnxUpdateChannel = value;
                NotifyPropertyChanged();
            }
        }

        public AppUpdateChannelOptions AppUpdateChannel
        {
            get
            {
                return _appUpdateChannel;
            }
            set
            {
                _appUpdateChannel = value;
                NotifyPropertyChanged();
            }
        }

        public bool AutoSortModsByDefault
        {
            get
            {
                return _autoSortModsByDefault;
            }
            set
            {
                _autoSortModsByDefault = value;
                NotifyPropertyChanged();
            }
        }

        public bool AutoUpdateModsByDefault
        {
            get
            {
                return _autoUpdateModsByDefault;
            }
            set
            {
                _autoUpdateModsByDefault = value;
                NotifyPropertyChanged();
            }
        }

        public bool ActivateInstalledModsAuto
        {
            get
            {
                return _activateInstalledModsAuto;
            }
            set
            {
                _activateInstalledModsAuto = value;
                NotifyPropertyChanged();
            }
        }

        public bool ImportLibraryFolderAuto
        {
            get
            {
                return _importLibraryFolderAuto;
            }
            set
            {
                _importLibraryFolderAuto = value;
                NotifyPropertyChanged();
            }
        }

        public bool CheckForUpdatesAuto
        {
            get
            {
                return _checkForUpdatesAuto;
            }
            set
            {
                _checkForUpdatesAuto = value;
                NotifyPropertyChanged();
            }
        }

        public bool LaunchExeDirectly
        {
            get
            {
                return _launchExeDirectly;
            }
            set
            {
                _launchExeDirectly = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility LaunchExeDirectlyVisibility
        {
            get
            {
                if(Sys.Settings.FF7InstalledVersion == FF7Version.Steam || LaunchExeDirectly)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }


        public bool BypassCompatibilityLocks
        {
            get
            {
                return _bypassCompatibilityLocks;
            }
            set
            {
                _bypassCompatibilityLocks = value;
                NotifyPropertyChanged();
            }
        }

        public bool OpenIrosLinks
        {
            get
            {
                return _openIrosLinks;
            }
            set
            {
                _openIrosLinks = value;
                NotifyPropertyChanged();
            }
        }

        public bool OpenModFilesWith7H
        {
            get
            {
                return _openModFilesWith7H;
            }
            set
            {
                _openModFilesWith7H = value;
                NotifyPropertyChanged();
            }
        }

        public bool WarnAboutModCode
        {
            get
            {
                return _warnAboutModCode;
            }
            set
            {
                _warnAboutModCode = value;
                NotifyPropertyChanged();
            }
        }

        public bool ShowContextMenuInExplorer
        {
            get
            {
                return _showContextMenuInExplorer;
            }
            set
            {
                _showContextMenuInExplorer = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<string> ExtraFolderList
        {
            get
            {
                if (_extraFolderList == null)
                    _extraFolderList = new ObservableCollection<string>();

                return _extraFolderList;
            }
            set
            {
                _extraFolderList = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsSubscriptionPopupOpen
        {
            get
            {
                return _isSubscriptionPopupOpen;
            }
            set
            {
                _isSubscriptionPopupOpen = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<SubscriptionSettingViewModel> SubscriptionList
        {
            get
            {
                if (_subscriptionList == null)
                    _subscriptionList = new ObservableCollection<SubscriptionSettingViewModel>();

                return _subscriptionList;
            }
            set
            {
                _subscriptionList = value;
                NotifyPropertyChanged();
            }
        }

        public bool SubscriptionsChanged { get; set; }

        private bool IsEditingSubscription { get; set; }

        public string SubscriptionNameHintText
        {
            get
            {
                return _subscriptionNameHintText;
            }
            set
            {
                _subscriptionNameHintText = value;
                NotifyPropertyChanged();
            }
        }

        public bool SubscriptionNameTextBoxIsEnabled
        {
            get
            {
                return _subscriptionNameTextBoxIsEnabled;
            }
            set
            {
                _subscriptionNameTextBoxIsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsResolvingName
        {
            get
            {
                return _isResolvingName;
            }
            set
            {
                _isResolvingName = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotResolvingName));
            }
        }

        public bool IsNotResolvingName
        {
            get
            {
                return !IsResolvingName;
            }
        }

        public string NewUrlText
        {
            get
            {
                return _newUrlText;
            }
            set
            {
                _newUrlText = value.Trim(new char[] { '\n', ' ', '\r' });
                NotifyPropertyChanged();
            }
        }

        public string NewNameText
        {
            get
            {
                return _newNameText;
            }
            set
            {
                _newNameText = value.Trim(new char[] { '\n', ' ', '\r' });
                NotifyPropertyChanged();
            }
        }

        public bool HasChangedInstalledModUpdateTypes { get; set; }

        public GeneralSettingsViewModel()
        {
            NewUrlText = "";
            NewNameText = "";
            SubscriptionsChanged = false;
            IsResolvingName = false;
            SubscriptionNameTextBoxIsEnabled = true;
            SubscriptionNameHintText = ResourceHelper.Get(StringKey.EnterNameForCatalog);
            HasChangedInstalledModUpdateTypes = false;
        }

        internal void ResetToDefaults()
        {
            LoadSettings(Settings.UseDefaultSettings());
        }

        internal void LoadSettings(Settings settings)
        {
            AutoDetectSystemPaths(settings);

            SubscriptionList = new ObservableCollection<SubscriptionSettingViewModel>(settings.Subscriptions.Select(s => new SubscriptionSettingViewModel(s.Url, s.Name)));
            ExtraFolderList = new ObservableCollection<string>(settings.ExtraFolders.ToList());

            FF7ExePathInput = settings.FF7Exe;
            LibraryPathInput = settings.LibraryLocation;

            FFNxUpdateChannel = settings.FFNxUpdateChannel;
            AppUpdateChannel = settings.AppUpdateChannel;

            AutoSortModsByDefault = settings.HasOption(GeneralOptions.AutoSortMods);
            AutoUpdateModsByDefault = settings.HasOption(GeneralOptions.AutoUpdateMods);
            ActivateInstalledModsAuto = settings.HasOption(GeneralOptions.AutoActiveNewMods);
            ImportLibraryFolderAuto = settings.HasOption(GeneralOptions.AutoImportMods);
            CheckForUpdatesAuto = settings.HasOption(GeneralOptions.CheckForUpdates);
            BypassCompatibilityLocks = settings.HasOption(GeneralOptions.BypassCompatibility);
            OpenIrosLinks = settings.HasOption(GeneralOptions.OpenIrosLinksWith7H);
            OpenModFilesWith7H = settings.HasOption(GeneralOptions.OpenModFilesWith7H);
            WarnAboutModCode = settings.HasOption(GeneralOptions.WarnAboutModCode);
            ShowContextMenuInExplorer = settings.HasOption(GeneralOptions.Show7HInFileExplorerContextMenu);

            LaunchExeDirectly = settings.HasOption(GeneralOptions.LaunchExeDirectly);

            NotifyPropertyChanged(nameof(LaunchExeDirectlyVisibility));
        }

        public static void AutoDetectSystemPaths(Settings settings)
        {
            string ff7 = null;

            if (string.IsNullOrEmpty(settings.FF7Exe) || !File.Exists(settings.FF7Exe))
            {
                Logger.Info("FF7 Exe path is empty or ff7.exe is missing. Auto detecting paths ...");

                try
                {
                    // Reset state
                    Sys.Settings.FF7InstalledVersion = FF7Version.Unknown;

                    // First try to autodetect the Steam installation if any
                    ff7 = GameConverter.GetInstallLocation(FF7Version.Steam);
                    Sys.Settings.FF7InstalledVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.Steam : FF7Version.Unknown;

                    // If no Steam version detected, attempt to detect the Eidos release
                    if (Sys.Settings.FF7InstalledVersion == FF7Version.Unknown)
                    {
                        ff7 = GameConverter.GetInstallLocation(FF7Version.ReRelease);
                        // Return the Steam version as both use the same logic to run from the App perspective
                        Sys.Settings.FF7InstalledVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.Steam : FF7Version.Unknown;
                    }

                    // Finally as a last attempt try to autodetect the 1998 release
                    if (Sys.Settings.FF7InstalledVersion == FF7Version.Unknown)
                    {
                        // Try to detect 1998 game or a "converted" game from the old 7H game converter
                        string registry_path = $"{RegistryHelper.GetKeyPath(FF7RegKey.SquareSoftKeyPath)}\\Final Fantasy VII";
                        ff7 = (string)Registry.GetValue(registry_path, "AppPath", null);
                        Sys.Settings.FF7InstalledVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.Original98 : FF7Version.Unknown;

                        if (!Directory.Exists(ff7))
                        {
                            Logger.Warn($"Deleting invalid 'AppPath' registry key since path does not exist: {ff7}");
                            RegistryHelper.DeleteValueFromKey(registry_path, "AppPath"); // delete old paths set 
                            RegistryHelper.DeleteValueFromKey(registry_path, "DataPath"); // delete old paths set 
                            RegistryHelper.DeleteValueFromKey(registry_path, "MoviePath"); // delete old paths set 
                            Sys.Settings.FF7InstalledVersion = FF7Version.Unknown; // set back to Unknown to check other registry keys
                        }
                    }

                    string versionStr = Sys.Settings.FF7InstalledVersion == FF7Version.Original98 ? $"{Sys.Settings.FF7InstalledVersion.ToString()} (or Game Converted)" : Sys.Settings.FF7InstalledVersion.ToString();

                    Logger.Info($"FF7Version Detected: {versionStr} with installation path: {ff7}");

                    if (!Directory.Exists(ff7))
                    {
                        Logger.Warn("Found installation path does not exist. Ignoring...");
                        return;
                    }

                }
                catch
                {
                    // could fail if game not installed
                }

                if (Sys.Settings.FF7InstalledVersion != FF7Version.Unknown)
                    settings.SetPathsFromInstallationPath(ff7);
                else
                    Logger.Warn("Could not detect the path to any FF7 installed copy.");
            }
            // User has given a ff7 exe path, try to guess which version it is
            else
            {
                if (settings.FF7Exe.ToLower().EndsWith("ff7_en.exe"))
                {
                    string ff7Launcher = Path.Combine(Path.GetDirectoryName(settings.FF7Exe), "FF7_Launcher.exe");

                    // Since both Steam and ReRelease share the same way to launch, prefer the Steam codepath
                    if (File.Exists(ff7Launcher)) Sys.Settings.FF7InstalledVersion = FF7Version.Steam;
                }
                else if(settings.FF7Exe.ToLower().EndsWith("ff7.exe"))
                {
                    string ff7path = Path.GetDirectoryName(settings.FF7Exe);
                    bool isRunningInWine = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINELOADER"));

                    // Detect if the installation is a previously Steam converted one
                    if (
                        !isRunningInWine && // are we on Windows?
                        (
                            (Directory.Exists(Path.Combine(ff7path, "music/vgmstream")) && Directory.EnumerateFiles(Path.Combine(ff7path, "music/vgmstream"), "*.ogg").Any()) || // did it inherit the original soundtrack?
                            Path.Exists(Path.Combine(ff7path, "ff7_en.exe")) || // did it inherit the original Steam exe?
                            Path.Exists(Path.Combine(ff7path, "firewall_entry.vdf")) || // did it inherit misc Steam files?
                            Path.Exists(Path.Combine(ff7path, "AF3DN.P")) // did it inherit offical Steam driver?
                        )
                    )
                    {
                        // Clearly not a genuine 1998 installation, try to convert it to Steam
                        Logger.Info("Previously Steam converted game detected. Attempting to switch back to the native Steam edition...");

                        // Try to autodetect the Steam installation if any
                        ff7 = GameConverter.GetInstallLocation(FF7Version.Steam);
                        Sys.Settings.FF7InstalledVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.Steam : FF7Version.Unknown;

                        if (Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
                            settings.SetPathsFromInstallationPath(ff7);
                        else
                        {
                            MessageDialogWindow.Show("Looks like you're using a previously converted Steam to 1998 Game Copy. It is suggested to install a fresh copy of the game from Steam and relaunch this app. The application will now close.", "This installation is now unsupported", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                            Application.Current.Shutdown();
                        }
                    }
                    else
                        // No previously converted edition detected, looks like a genuine 1998 edition
                        Sys.Settings.FF7InstalledVersion = FF7Version.Original98;
                }
            }
        }

        internal bool SaveSettings()
        {
            if (!ValidateSettings())
            {
                return false;
            }

            Sys.Settings.Subscriptions = GetUpdatedSubscriptions();
            Sys.Settings.ExtraFolders = ExtraFolderList.Distinct().ToList();

            // ensure required folders are always in ExtraFolders list
            if (!Sys.Settings.ExtraFolders.Contains("direct", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("direct");
            }

            if (!Sys.Settings.ExtraFolders.Contains("music", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("music");
            }

            if (!Sys.Settings.ExtraFolders.Contains("sfx", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("sfx");
            }

            if (!Sys.Settings.ExtraFolders.Contains("voice", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("voice");
            }

            if (!Sys.Settings.ExtraFolders.Contains("ambient", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("ambient");
            }

            if (!Sys.Settings.ExtraFolders.Contains("widescreen", StringComparer.InvariantCultureIgnoreCase))
            {
                Sys.Settings.ExtraFolders.Add("widescreen");
            }

            Sys.Settings.FF7Exe = FF7ExePathInput;
            Sys.Settings.LibraryLocation = LibraryPathInput;
            Sys.Settings.FFNxUpdateChannel = FFNxUpdateChannel;
            Sys.Settings.AppUpdateChannel = AppUpdateChannel;

            Sys.Settings.Options = GetUpdatedOptions();

            ApplyOptions();

            Directory.CreateDirectory(Sys.Settings.LibraryLocation);

            Sys.Message(new WMessage(ResourceHelper.Get(StringKey.GeneralSettingsHaveBeenUpdated)));

            if (!FFNxDriverUpdater.IsAlreadyInstalled())
            {
                try
                {
                    FFNxDriverUpdater updater = new FFNxDriverUpdater();

                    Sys.Message(new WMessage($"Downloading and extracting the latest FFNx {Sys.Settings.FFNxUpdateChannel} version to {Sys.InstallPath}..."));
                    updater.DownloadAndExtractLatestVersion(Sys.Settings.FFNxUpdateChannel);
                }
                catch (Exception ex)
                {
                    Sys.Message(new WMessage($"Something went wrong while attempting to install FFNx. See logs."));
                    Logger.Error(ex);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// applies various options based on what is enabled e.g. updating registry to associate files
        /// </summary>
        private void ApplyOptions()
        {
            RegistryHelper.BeginTransaction();

            // ensure direct and music folder exist since they are defaults and can not be removed
            foreach (string folder in Settings.UseDefaultSettings().ExtraFolders)
            {
                string pathToFf7 = Path.GetDirectoryName(Sys.Settings.FF7Exe);
                Directory.CreateDirectory(Path.Combine(pathToFf7, folder));
            }

            if (Sys.Settings.HasOption(GeneralOptions.OpenIrosLinksWith7H))
            {
                AssociateIrosUrlWith7H();
            }
            else
            {
                RemoveIrosUrlAssociationFromRegistry();
            }

            if (Sys.Settings.HasOption(GeneralOptions.OpenModFilesWith7H))
            {
                AssociateIroFilesWith7H();
            }
            else
            {
                RemoveIroFileAssociationFromRegistry();
            }

            if (Sys.Settings.HasOption(GeneralOptions.Show7HInFileExplorerContextMenu))
            {
                AssociateFileExplorerContextMenuWith7H();
            }
            else
            {
                RemoveFileExplorerContextMenuAssociationWith7H();
            }

            RegistryHelper.CommitTransaction();

            // ensure only applying option if it has changed
            if (Sys.Settings.HasOption(GeneralOptions.AutoUpdateMods) && Sys.Library.DefaultUpdate == UpdateType.Notify)
            {
                HasChangedInstalledModUpdateTypes = true;
                Sys.Library.DefaultUpdate = UpdateType.Install;

                foreach (InstalledItem item in Sys.Library.Items)
                {
                    item.UpdateType = UpdateType.Install;
                }

            }
            else if (!Sys.Settings.HasOption(GeneralOptions.AutoUpdateMods) && Sys.Library.DefaultUpdate == UpdateType.Install)
            {
                HasChangedInstalledModUpdateTypes = true;
                Sys.Library.DefaultUpdate = UpdateType.Notify;

                foreach (InstalledItem item in Sys.Library.Items)
                {
                    item.UpdateType = UpdateType.Notify;
                }
            }
        }

        /// <summary>
        /// returns list of options currently set to true.
        /// </summary>
        private List<GeneralOptions> GetUpdatedOptions()
        {
            List<GeneralOptions> newOptions = new List<GeneralOptions>();

            if (AutoSortModsByDefault)
                newOptions.Add(GeneralOptions.AutoSortMods);

            if (AutoUpdateModsByDefault)
                newOptions.Add(GeneralOptions.AutoUpdateMods);

            if (ActivateInstalledModsAuto)
                newOptions.Add(GeneralOptions.AutoActiveNewMods);

            if (ImportLibraryFolderAuto)
                newOptions.Add(GeneralOptions.AutoImportMods);

            if (CheckForUpdatesAuto)
                newOptions.Add(GeneralOptions.CheckForUpdates);

            if (BypassCompatibilityLocks)
                newOptions.Add(GeneralOptions.BypassCompatibility);

            if (OpenIrosLinks)
                newOptions.Add(GeneralOptions.OpenIrosLinksWith7H);

            if (OpenModFilesWith7H)
                newOptions.Add(GeneralOptions.OpenModFilesWith7H);

            if (WarnAboutModCode)
                newOptions.Add(GeneralOptions.WarnAboutModCode);

            if (ShowContextMenuInExplorer)
                newOptions.Add(GeneralOptions.Show7HInFileExplorerContextMenu);

            if (LaunchExeDirectly)
                newOptions.Add(GeneralOptions.LaunchExeDirectly);


            return newOptions;
        }

        /// <summary>
        /// Returns list of Subscriptions based on the current input in <see cref="SubscriptionList"/>
        /// </summary>
        private List<Subscription> GetUpdatedSubscriptions()
        {
            List<Subscription> updatedSubscriptions = new List<Subscription>();

            foreach (SubscriptionSettingViewModel item in SubscriptionList.ToList())
            {
                var existingSub = Sys.Settings.Subscriptions.FirstOrDefault(s => s.Url == item.Url);

                if (existingSub == null)
                {
                    existingSub = new Subscription() { Name = item.Name, Url = item.Url };
                }
                else
                {
                    existingSub.Name = item.Name;
                }

                updatedSubscriptions.Add(existingSub);
            }

            return updatedSubscriptions;
        }

        private bool ValidateSettings(bool showMessage = true)
        {
            string validationMessage = "";
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(FF7ExePathInput))
            {
                validationMessage = ResourceHelper.Get(StringKey.MissingFf7ExePath);
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LibraryPathInput))
            {
                validationMessage = ResourceHelper.Get(StringKey.MissingLibraryPath);
                isValid = false;
            }

            if ((Path.GetDirectoryName(FF7ExePathInput) == LibraryPathInput) || Sys._7HFolder == LibraryPathInput)
            {
                validationMessage = ResourceHelper.Get(StringKey.LibraryPathCannotBeGameOrApp);
                isValid = false;
            }

            if (showMessage && !isValid)
            {
                MessageDialogWindow.Show(validationMessage, ResourceHelper.Get(StringKey.SettingsNotValid), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return isValid;
        }

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        /// <summary>
        /// Update Registry to associate .iro mod files with 7H
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateIroFilesWith7H()
        {
            try
            {
                //Associate .iro mod files with 7H's Prog_ID- .IRO extension
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.iro", "", $"7thHeavenIRO");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.iro\\DefaultIcon", "", $"\"{Sys._7HExe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.iro\\shell\\open\\command", "", $"\"{Sys._7HExe}\" /OPENIRO:\"%1\"");

                // create registry keys to assocaite .irop files
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.irop", "", $"7thHeavenIROP");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.irop\\DefaultIcon", "", $"\"{Sys._7HExe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\.irop\\shell\\open\\command", "", $"\"{Sys._7HExe}\" /OPENIROP:\"%1\"");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRegisterIroModFilesWith7thHeaven)));
                return false;
            }
        }

        /// <summary>
        /// Deletes Registry keys/values (if they exist) to unassociate .iro mod files with 7H
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        private static bool RemoveIroFileAssociationFromRegistry()
        {
            try
            {
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\.iro");
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\.irop");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnregisterIroModFilesWith7thHeaven)));
                return false;
            }
        }

        /// <summary>
        /// Update Registry to asssociate iros:// URL with 7H
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateIrosUrlWith7H()
        {
            try
            {
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iros", "", $"7H Catalog Subscription");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iros", "URL Protocol", $"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iros\\DefaultIcon", "", $"\"{Sys._7HExe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\iros\\shell\\open\\command", "", $"\"{Sys._7HExe}\" \"%1\"");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRegisterIrosLinksWith7thHeaven)));
                return false;
            }
        }

        /// <summary>
        /// Deletes Registry key/values (if they exist) to unasssociate iros:// URL with 7H
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        /// <returns></returns>
        private static bool RemoveIrosUrlAssociationFromRegistry()
        {
            try
            {
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\iros");

                //Refresh Shell/Explorer so icon cache updates
                //do this now because we don't care so much about assoc. URL if it fails
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnregisterIrosLinksWith7thHeaven)));
                return false;
            }
        }

        /// <summary>
        /// Update Registry to add Context menu options to Windows File Explorer
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateFileExplorerContextMenuWith7H()
        {
            try
            {
                // create registry keys for 'Pack IRO' for folders
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\Directory\\shell\\Pack into IRO", "Icon", $"\"{Sys._7HExe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\Directory\\shell\\Pack into IRO\\command", "", $"\"{Sys._7HExe}\" /PACKIRO:\"%1\"");

                // create registry keys for 'Unpack IRO' for files
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\7thHeavenIRO\\shell\\Unpack IRO", "Icon", $"\"{Sys._7HExe}\"");
                RegistryHelper.SetValueIfChanged("HKEY_CLASSES_ROOT\\7thHeavenIRO\\shell\\Unpack IRO\\command", "", $"\"{Sys._7HExe}\" /UNPACKIRO:\"%1\"");

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToCreate7thHeavenContextMenuEntries), WMessageLogLevel.Error, e));
                return false;
            }
        }

        /// <summary>
        /// Deletes Registry key/values (if they exist) to unasssociate context menu options from Windows File Explorer
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        /// <returns></returns>
        private static bool RemoveFileExplorerContextMenuAssociationWith7H()
        {
            try
            {
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\Directory\\shell\\Pack into IRO");
                RegistryHelper.DeleteKey("HKEY_CLASSES_ROOT\\7thHeavenIRO\\shell\\Unpack IRO");

                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRemove7thHeavenContextMenuEntries), WMessageLogLevel.Error, e));
                return false;
            }
        }

        internal void EditSelectedSubscription(SubscriptionSettingViewModel selected)
        {
            IsEditingSubscription = true;
            IsSubscriptionPopupOpen = true;
            NewUrlText = selected.Url;
            NewNameText = selected.Name ?? "";
        }

        internal void AddNewSubscription()
        {
            IsEditingSubscription = false;
            SubscriptionNameTextBoxIsEnabled = false;
            SubscriptionNameHintText = ResourceHelper.Get(StringKey.CatalogNameWillAutoResolveOnSave);
            IsSubscriptionPopupOpen = true;
            string clipboardContent = "";

            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                clipboardContent = Clipboard.GetText(TextDataFormat.Text);
            }

            if (!string.IsNullOrWhiteSpace(clipboardContent) && clipboardContent.StartsWith("iros://"))
            {
                NewUrlText = clipboardContent;
            }
        }

        /// <summary>
        /// Adds or Edits subscription and closes subscription popup
        /// </summary>
        internal bool SaveSubscription()
        {
            if (!NewUrlText.StartsWith("iros://"))
            {
                StatusMessage = ResourceHelper.Get(StringKey.UrlMustBeInIrosFormat);
                return false;
            }

            if (!SubscriptionList.Any(s => s.Url == NewUrlText))
            {
                IsResolvingName = true;
                SubscriptionNameHintText = ResourceHelper.Get(StringKey.ResolvingCatalogName);
                ResolveCatalogNameFromUrl(NewUrlText, resolvedName =>
                {
                    NewNameText = resolvedName;
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SubscriptionList.Add(new SubscriptionSettingViewModel(NewUrlText, NewNameText));
                        CloseSubscriptionPopup();
                        IsResolvingName = false;
                        ListDataChanged?.Invoke();
                    });
                });
            }
            else if (IsEditingSubscription)
            {
                SubscriptionSettingViewModel toEdit = SubscriptionList.FirstOrDefault(s => s.Url == NewUrlText);
                toEdit.Name = NewNameText;
                CloseSubscriptionPopup();
                ListDataChanged?.Invoke();
            }
            else
            {
                // if user is trying to add a url that already exists in list then just close popup
                CloseSubscriptionPopup();
                return true;
            }

            SubscriptionsChanged = true;
            return true;
        }

        internal void CloseSubscriptionPopup()
        {
            IsEditingSubscription = false;
            IsSubscriptionPopupOpen = false;
            NewUrlText = "";
            NewNameText = "";
            SubscriptionNameTextBoxIsEnabled = true;
            SubscriptionNameHintText = ResourceHelper.Get(StringKey.EnterNameForCatalog);
        }

        internal void MoveSelectedSubscription(SubscriptionSettingViewModel selected, int toAdd)
        {
            int currentIndex = SubscriptionList.IndexOf(selected);

            if (currentIndex < 0)
            {
                // not found in  list
                return;
            }

            int newIndex = currentIndex + toAdd;

            if (newIndex == currentIndex || newIndex < 0 || newIndex >= SubscriptionList.Count)
            {
                return;
            }

            SubscriptionList.Move(currentIndex, newIndex);
            SubscriptionsChanged = true;
        }

        internal void RemoveSelectedSubscription(SubscriptionSettingViewModel selected)
        {
            SubscriptionsChanged = true;
            SubscriptionList.Remove(selected);
            ListDataChanged?.Invoke();
        }

        /// <summary>
        /// Downloads catalog.xml to temp file and gets Name of the catalog. 
        /// resolved name gets passed to delegate method that is called after download
        /// </summary>
        /// <param name="catalogUrl"></param>
        /// <param name="callback"></param>
        internal static void ResolveCatalogNameFromUrl(string catalogUrl, Action<string> callback)
        {
            string name = "";

            string uniqueFileName = $"cattemp{Path.GetRandomFileName()}.xml"; // save temp catalog update to unique filename so multiple catalog updates can download async
            string path = Path.Combine(Sys.PathToTempFolder, uniqueFileName);

            Action onCancel = () =>
            {
                // delete temp file on cancel if it exists
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                callback(name);
            };

            Action<Exception> onError = ex =>
            {
                callback("");
            };

            Install.InstallProcedureCallback downloadCallback = new Install.InstallProcedureCallback(e =>
            {
                bool success = (e.Error == null && e.Cancelled == false);

                if (success)
                {
                    try
                    {
                        Catalog c = Util.Deserialize<Catalog>(path);
                        name = c.Name ?? "";

                        // delete temp file if it exists
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to deserialize catalog - {ex.Message}");
                    }
                }

                callback(name);
            });
            downloadCallback.Error = onError;

            DownloadItem download = new DownloadItem()
            {
                Links = new List<string>() { catalogUrl },
                SaveFilePath = path,
                Category = DownloadCategory.Catalog,
                ItemName = $"{ResourceHelper.Get(StringKey.ResolvingCatalogNameFor)} {catalogUrl}",
                IProc = downloadCallback,
                OnCancel = onCancel
            };

            Sys.Downloads.AddToDownloadQueue(download);
        }

        internal void AddExtraFolder()
        {
            string initialDir = File.Exists(FF7ExePathInput) ? Path.GetDirectoryName(FF7ExePathInput) : "";
            string pathToFolder = FileDialogHelper.BrowseForFolder("", initialDir);

            if (!string.IsNullOrWhiteSpace(pathToFolder))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(pathToFolder);
                string folderName = dirInfo.Name.ToLower();

                if (!ExtraFolderList.Contains(folderName))
                {
                    ExtraFolderList.Add(folderName);
                }
            }
        }

        internal void MoveSelectedFolder(string selected, int toAdd)
        {
            int currentIndex = ExtraFolderList.IndexOf(selected);

            if (currentIndex < 0)
            {
                // not found in  list
                return;
            }

            int newIndex = currentIndex + toAdd;

            if (newIndex == currentIndex || newIndex < 0 || newIndex >= ExtraFolderList.Count)
            {
                return;
            }

            ExtraFolderList.Move(currentIndex, newIndex);
        }
    }
}
