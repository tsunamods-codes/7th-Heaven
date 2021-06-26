using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using Microsoft.Win32;
using SeventhHeaven.Classes;
using SeventhHeaven.Windows;
using SeventhHeavenUI;
using SeventhHeavenUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SeventhHeaven.ViewModels
{
    public class GeneralSettingsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _fF7ExePathInput;
        private string _moviesPathInput;
        private string _texturesPathInput;
        private string _libraryPathInput;
        private bool _autoUpdateModsByDefault;
        private bool _activateInstalledModsAuto;
        private bool _importLibraryFolderAuto;
        private bool _checkForUpdatesAuto;
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

        public string MoviesPathInput
        {
            get
            {
                return _moviesPathInput;
            }
            set
            {
                _moviesPathInput = value;
                NotifyPropertyChanged();
            }
        }

        public string TexturesPathInput
        {
            get
            {
                return _texturesPathInput;
            }
            set
            {
                _texturesPathInput = value;
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
            MoviesPathInput = settings.MovieFolder;
            TexturesPathInput = settings.AaliFolder;

            FFNxUpdateChannel = settings.FFNxUpdateChannel;
            AppUpdateChannel = settings.AppUpdateChannel;

            AutoUpdateModsByDefault = settings.HasOption(GeneralOptions.AutoUpdateMods);
            ActivateInstalledModsAuto = settings.HasOption(GeneralOptions.AutoActiveNewMods);
            ImportLibraryFolderAuto = settings.HasOption(GeneralOptions.AutoImportMods);
            CheckForUpdatesAuto = settings.HasOption(GeneralOptions.CheckForUpdates);
            BypassCompatibilityLocks = settings.HasOption(GeneralOptions.BypassCompatibility);
            OpenIrosLinks = settings.HasOption(GeneralOptions.OpenIrosLinksWith7H);
            OpenModFilesWith7H = settings.HasOption(GeneralOptions.OpenModFilesWith7H);
            WarnAboutModCode = settings.HasOption(GeneralOptions.WarnAboutModCode);
            ShowContextMenuInExplorer = settings.HasOption(GeneralOptions.Show7HInFileExplorerContextMenu);
        }

        public static void AutoDetectSystemPaths(Settings settings)
        {
            if (string.IsNullOrEmpty(settings.FF7Exe) || !File.Exists(settings.FF7Exe))
            {
                Logger.Info("FF7 Exe path is empty or ff7.exe is missing. Auto detecting paths ...");

                string registry_path = @"HKEY_LOCAL_MACHINE\SOFTWARE\Square Soft, Inc.\Final Fantasy VII";
                string ff7 = null;
                FF7Version foundVersion = FF7Version.Unknown;

                try
                {
                    // first try to detect 1998 game or a "converted" game from the old 7H game converter
                    ff7 = (string)Registry.GetValue(registry_path, "AppPath", null);
                    foundVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.Original98 : FF7Version.Unknown;

                    if (!Directory.Exists(ff7))
                    {
                        Logger.Warn($"Deleting invalid 'AppPath' registry key since path does not exist: {ff7}");
                        RegistryHelper.DeleteValueFromKey(registry_path, "AppPath"); // delete old paths set 
                        RegistryHelper.DeleteValueFromKey(registry_path, "DataPath"); // delete old paths set 
                        RegistryHelper.DeleteValueFromKey(registry_path, "MoviePath"); // delete old paths set 
                        foundVersion = FF7Version.Unknown; // set back to Unknown to check other registry keys
                    }


                    if (foundVersion == FF7Version.Unknown)
                    {
                        // next check Steam registry keys and then Re-Release registry keys for installation path
                        ff7 = GameConverter.GetInstallLocation(FF7Version.Steam);
                        foundVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.Steam : FF7Version.Unknown;


                        if (foundVersion == FF7Version.Unknown)
                        {
                            ff7 = GameConverter.GetInstallLocation(FF7Version.ReRelease);
                            foundVersion = !string.IsNullOrWhiteSpace(ff7) ? FF7Version.ReRelease : FF7Version.Unknown;
                        }
                    }

                    string versionStr = foundVersion == FF7Version.Original98 ? $"{foundVersion.ToString()} (or Game Converted)" : foundVersion.ToString();

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

                if (foundVersion != FF7Version.Unknown)
                {
                    settings.SetPathsFromInstallationPath(ff7);

                    // copy ff7.exe to install path if not found since Steam & Re-Release installation does not provide a ff7.exe
                    if (!File.Exists(settings.FF7Exe) && Path.GetFileName(settings.FF7Exe).Equals("ff7.exe", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Logger.Info($"Copying ff7.exe from {Sys.PathToPatchedExeFolder} to {settings.FF7Exe}");

                        try
                        {
                            File.Copy(Path.Combine(Sys.PathToPatchedExeFolder, "ff7.exe"), settings.FF7Exe, true);
                            Logger.Info($"\tcopied succesfully.");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                }
                else
                {
                    Logger.Warn("Auto detect paths failed - could not get ff7.exe path from Windows Registry.");
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

            Sys.Settings.FF7Exe = FF7ExePathInput;
            Sys.Settings.LibraryLocation = LibraryPathInput;
            Sys.Settings.MovieFolder = MoviesPathInput;
            Sys.Settings.AaliFolder = TexturesPathInput;
            Sys.Settings.FFNxUpdateChannel = FFNxUpdateChannel;
            Sys.Settings.AppUpdateChannel = AppUpdateChannel;


            Sys.Settings.Options = GetUpdatedOptions();

            ApplyOptions();

            Directory.CreateDirectory(Sys.Settings.LibraryLocation);

            Sys.Message(new WMessage(ResourceHelper.Get(StringKey.GeneralSettingsHaveBeenUpdated)));

            return true;
        }

        /// <summary>
        /// applies various options based on what is enabled e.g. updating registry to associate files
        /// </summary>
        private void ApplyOptions()
        {
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

            if (string.IsNullOrWhiteSpace(TexturesPathInput))
            {
                validationMessage = ResourceHelper.Get(StringKey.MissingTexturesPath);
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(MoviesPathInput))
            {
                validationMessage = ResourceHelper.Get(StringKey.MissingMoviePath);
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
        private static bool AssociateIroFilesWith7H(RegistryKey key)
        {
            string app = System.Reflection.Assembly.GetExecutingAssembly().Location;

            //Create Prog_ID in Registry so we can associate file types
            var progid = key.CreateSubKey("7thHeaven");
            if (progid == null) return false;

            var icon = progid.CreateSubKey("DefaultIcon");
            var shell = progid.CreateSubKey("shell");
            var open = shell.CreateSubKey("open");
            var command = open.CreateSubKey("command");
            progid.SetValue(String.Empty, "7th Heaven Mod File");
            icon.SetValue(String.Empty, "\"" + app + "\"");
            command.SetValue(String.Empty, "\"" + app + "\" /OPENIRO:\"%1\"");

            //Associate .iro mod files with 7H's Prog_ID- .IRO extension
            var iroext = key.CreateSubKey(".iro");
            if (iroext == null) return false;

            iroext.SetValue(String.Empty, "7thHeaven");

            // create registry keys to assocaite .irop files
            var iropExt = key.CreateSubKey(".irop");

            if (iropExt != null)
            {
                icon = iropExt.CreateSubKey("DefaultIcon");
                shell = iropExt.CreateSubKey("shell");
                open = shell.CreateSubKey("open");
                command = open.CreateSubKey("command");

                iropExt.SetValue(String.Empty, "7th Heaven Mod Patch");
                icon.SetValue(String.Empty, "\"" + app + "\"");
                command.SetValue(String.Empty, "\"" + app + "\" /OPENIROP:\"%1\"");
            }

            //Refresh Shell/Explorer so icon cache updates
            //do this now because we don't care so much about assoc. URL if it fails
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            return true;
        }

        /// <summary>
        /// Deletes Registry keys/values (if they exist) to unassociate .iro mod files with 7H
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        private static bool RemoveIroFileAssociationFromRegistry(RegistryKey key)
        {
            try
            {
                List<string> subkeys = key.GetSubKeyNames().Where(k => k == "7thHeaven" || k == ".iro" || k == ".irop").ToList();
                bool deletedKeys = false;

                if (subkeys.Contains("7thHeaven"))
                {
                    var progKey = key.OpenSubKey("7thHeaven", true);
                    string[] subKeys = progKey.GetSubKeyNames();

                    if (subKeys.Any(k => k == "shell"))
                    {
                        var shell = progKey.OpenSubKey("shell", true);
                        if (shell.GetSubKeyNames().Any(k => k == "open"))
                        {
                            shell.DeleteSubKeyTree("open");
                            deletedKeys = true;
                        }
                    }

                    if (subKeys.Any(k => k == ".iro"))
                    {
                        progKey.DeleteSubKeyTree(".iro");
                        deletedKeys = true;
                    }

                    if (subKeys.Any(k => k == "DefaultIcon"))
                    {
                        progKey.DeleteSubKeyTree("DefaultIcon");
                        deletedKeys = true;
                    }
                }

                if (subkeys.Contains(".iro"))
                {
                    key.DeleteSubKeyTree(".iro");
                    deletedKeys = true;
                }


                if (subkeys.Contains(".irop"))
                {
                    key.DeleteSubKeyTree(".irop");
                    deletedKeys = true;
                }

                if (deletedKeys)
                {
                    //Refresh Shell/Explorer so icon cache updates
                    //do this now because we don't care so much about assoc. URL if it fails
                    SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Warn(e); // could be error thrown if already deleted
                return false;
            }
        }

        /// <summary>
        /// Update Registry to asssociate iros:// URL with 7H
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateIrosUrlWith7H(RegistryKey key)
        {
            string app = System.Reflection.Assembly.GetExecutingAssembly().Location;

            var iros = key.CreateSubKey("iros");
            if (iros == null) return false;

            iros.SetValue(String.Empty, "7H Catalog Subscription");
            iros.SetValue("URL Protocol", String.Empty);


            var icon = iros.CreateSubKey("DefaultIcon");
            icon.SetValue(String.Empty, "\"" + app + "\"");

            var shell = iros.CreateSubKey("shell");
            var open = shell.CreateSubKey("open");
            var command = open.CreateSubKey("command");
            command.SetValue(String.Empty, "\"" + app + "\" \"%1\"");

            //Refresh Shell/Explorer so icon cache updates
            //do this now because we don't care so much about assoc. URL if it fails
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            return true;
        }

        /// <summary>
        /// Deletes Registry key/values (if they exist) to unasssociate iros:// URL with 7H
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        /// <returns></returns>
        private static bool RemoveIrosUrlAssociationFromRegistry(RegistryKey key)
        {
            try
            {
                List<string> subkeys = key.GetSubKeyNames().Where(k => k == "iros").ToList();

                if (subkeys.Contains("iros"))
                {
                    key.DeleteSubKeyTree("iros");

                    //Refresh Shell/Explorer so icon cache updates
                    //do this now because we don't care so much about assoc. URL if it fails
                    SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Warn(e); // could be error thrown if already deleted
                return false;
            }
        }

        /// <summary>
        /// Update Registry to add 7th Heaven Context menu options to Windows File Explorer
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool AssociateFileExplorerContextMenuWith7H(RegistryKey key)
        {
            string app = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // create registry keys for 'Pack IRO' for folders
            var dir = key.CreateSubKey("Directory");
            if (dir == null) return false;

            var shell = dir.CreateSubKey("shell");
            var name = shell.CreateSubKey("Pack into IRO");
            var command = name.CreateSubKey("command");

            command.SetValue(String.Empty, "\"" + app + "\" \"/PACKIRO:%1\"");
            name.SetValue("Icon", "\"" + app + "\"");

            // create registry keys for 'Unpack IRO' for files
            var progid = key.CreateSubKey("7thHeaven");
            if (progid == null) return false;

            var iroShell = progid.CreateSubKey("shell");
            var unpackCmdName = iroShell.CreateSubKey("Unpack IRO");
            var unpackCommand = unpackCmdName.CreateSubKey("command");

            unpackCommand.SetValue(String.Empty, "\"" + app + "\" \"/UNPACKIRO:%1\"");
            unpackCmdName.SetValue("Icon", "\"" + app + "\"");

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            return true;
        }

        /// <summary>
        /// Deletes Registry key/values (if they exist) to unasssociate 7th heaven context menu options from Windows File Explorer
        /// </summary>
        /// <param name="key"> could be HKEY_CLASSES_ROOT or HKEY_CURRENT_USER/Software/Classes </param>
        /// <returns></returns>
        private static bool RemoveContextMenuAssociationFromRegistry(RegistryKey key)
        {
            try
            {
                List<string> subkeys = key.GetSubKeyNames().Where(k => k == "Directory" || k == "7thHeaven").ToList();

                if (subkeys.Contains("Directory"))
                {
                    var dirKey = key.OpenSubKey("Directory", true);

                    if (dirKey.GetSubKeyNames().Any(k => k == "shell"))
                    {
                        var shell = dirKey.OpenSubKey("shell", true);
                        if (shell.GetSubKeyNames().Any(k => k == "Pack into IRO"))
                        {
                            shell.DeleteSubKeyTree("Pack into IRO");
                        }
                    }

                    //Refresh Shell/Explorer so icon cache updates
                    SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                }

                if (subkeys.Contains("7thHeaven"))
                {
                    var dirKey = key.OpenSubKey("7thHeaven", true);

                    if (dirKey.GetSubKeyNames().Any(k => k == "shell"))
                    {
                        var shell = dirKey.OpenSubKey("shell", true);
                        if (shell.GetSubKeyNames().Any(k => k == "Unpack IRO"))
                        {
                            shell.DeleteSubKeyTree("Unpack IRO");
                        }
                    }

                    //Refresh Shell/Explorer so icon cache updates
                    SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Warn(e); // could be error thrown if already deleted
                return false;
            }
        }


        internal static bool AssociateIroFilesWith7H()
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot;
                bool global = AssociateIroFilesWith7H(key);
                if (!global)
                {
                    key = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                    global = AssociateIroFilesWith7H(key);
                }

                return global;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRegisterIroModFilesWith7thHeaven)));
                return false;
            }


        }

        internal static bool RemoveIroFileAssociationFromRegistry()
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot;
                bool global = RemoveIroFileAssociationFromRegistry(key);
                if (!global)
                {
                    key = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                    global = RemoveIroFileAssociationFromRegistry(key);
                }

                return global;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnregisterIroModFilesWith7thHeaven)));
                return false;
            }
        }

        internal static bool RemoveIrosUrlAssociationFromRegistry()
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot;
                bool global = RemoveIrosUrlAssociationFromRegistry(key);
                if (!global)
                {
                    key = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                    global = RemoveIrosUrlAssociationFromRegistry(key);
                }

                return global;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToUnregisterIrosLinksWith7thHeaven)));
                return false;
            }
        }

        internal static bool AssociateFileExplorerContextMenuWith7H()
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot;
                bool global = AssociateFileExplorerContextMenuWith7H(key);
                if (!global)
                {
                    key = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                    global = AssociateFileExplorerContextMenuWith7H(key);
                }
                return global;
            }
            catch (Exception e)
            {
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToCreate7thHeavenContextMenuEntries), WMessageLogLevel.Error, e));
                return false;
            }
        }

        internal static bool RemoveFileExplorerContextMenuAssociationWith7H()
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot;
                bool global = RemoveContextMenuAssociationFromRegistry(key);
                if (!global)
                {
                    key = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                    global = RemoveContextMenuAssociationFromRegistry(key);
                }
                return global;
            }
            catch (Exception e)
            {
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRemove7thHeavenContextMenuEntries), WMessageLogLevel.Error, e));
                return false;
            }
        }

        internal static bool AssociateIrosUrlWith7H()
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot;
                bool global = AssociateIrosUrlWith7H(key);
                if (!global)
                {
                    key = Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                    global = AssociateIrosUrlWith7H(key);
                }
                return global;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToRegisterIrosLinksWith7thHeaven)));
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
