using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
using SeventhHeaven.Classes.Themes;
using SeventhHeaven.ViewModels;
using SeventhHeaven.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SeventhHeavenUI.ViewModels
{
    enum TabIndex
    {
        MyMods,
        BrowseCatalog
    }

    public class MainWindowViewModel : ViewModelBase
    {
        #region Data Members And Properties

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal string ShowAllText
        {
            get => ResourceHelper.Get(StringKey.SelectAll);
        }
        internal string UnknownText
        {
            get => ResourceHelper.Get(StringKey.Unknown);
        }

        public List<string> AppHints = new List<string>()
        {
            ResourceHelper.Get(StringKey.AppHint1),
            ResourceHelper.Get(StringKey.AppHint2),
            ResourceHelper.Get(StringKey.AppHint3),
            ResourceHelper.Get(StringKey.AppHint4),
            ResourceHelper.Get(StringKey.AppHint5),
            ResourceHelper.Get(StringKey.AppHint6),
            ResourceHelper.Get(StringKey.AppHint7),
            ResourceHelper.Get(StringKey.AppHint8),
            ResourceHelper.Get(StringKey.AppHint9),
            ResourceHelper.Get(StringKey.AppHint10),
            ResourceHelper.Get(StringKey.AppHint11),
            ResourceHelper.Get(StringKey.AppHint12),
            ResourceHelper.Get(StringKey.AppHint13),
            ResourceHelper.Get(StringKey.AppHint14),
            ResourceHelper.Get(StringKey.AppHint15),
            ResourceHelper.Get(StringKey.AppHint16),
            ResourceHelper.Get(StringKey.AppHint17),
            ResourceHelper.Get(StringKey.AppHint18),
            ResourceHelper.Get(StringKey.AppHint19),
            ResourceHelper.Get(StringKey.AppHint20),
            ResourceHelper.Get(StringKey.AppHint21),
            ResourceHelper.Get(StringKey.AppHint22),
            ResourceHelper.Get(StringKey.AppHint23),
            ResourceHelper.Get(StringKey.AppHint24),
            ResourceHelper.Get(StringKey.AppHint25),
            ResourceHelper.Get(StringKey.AppHint26),
            ResourceHelper.Get(StringKey.AppHint27),
            ResourceHelper.Get(StringKey.AppHint28),
            ResourceHelper.Get(StringKey.AppHint29),
            ResourceHelper.Get(StringKey.AppHint30),
            ResourceHelper.Get(StringKey.AppHint31),
            ResourceHelper.Get(StringKey.AppHint32),
            ResourceHelper.Get(StringKey.AppHint33),
            ResourceHelper.Get(StringKey.AppHint34),
            ResourceHelper.Get(StringKey.AppHint35),
            ResourceHelper.Get(StringKey.AppHint36),
            ResourceHelper.Get(StringKey.AppHint37),
            ResourceHelper.Get(StringKey.AppHint38),
            ResourceHelper.Get(StringKey.AppHint39),
            ResourceHelper.Get(StringKey.AppHint40),
            ResourceHelper.Get(StringKey.AppHint41),
            ResourceHelper.Get(StringKey.AppHint42),
            ResourceHelper.Get(StringKey.AppHint43)
        };

        private string _catFile;
        private string _statusMessage;
        private string _currentProfile;
        private string _searchText;
        private int _selectedTabIndex;
        private List<FilterItemViewModel> _availableFilters;

        private Mod _previewMod;
        private string _previewModAuthor;
        private string _previewModName;
        private string _previewModVersion;
        private string _previewModReleaseDate;
        private string _previewModReleaseNotes;
        private string _previewModCategory;
        private string _previewModDescription;
        private string _previewModLink;
        private Uri _previewModImageSource;
        private bool _previewModHasReadMe;
        private bool _previewIsNotifyAboutUpdatesChecked;
        private bool _previewIsAutoUpdateModsChecked;
        private bool _previewIgnoreModUpdatesChecked;
        private Visibility _modUpdateMenuVisibility;


        public MyModsViewModel MyMods { get; set; }

        public CatalogViewModel CatalogMods { get; set; }

        private Visibility _loadingGifVisibility;
        private bool _isFlashingStatus;
        private Visibility _noImageTextVisibility;
        private bool _isGameLaunching;
        private bool _isPlayToggleButtonEnabled;

        public string WindowTitle
        {
            get
            {
                return $"{App.GetAppName()} v{App.GetAppVersion().ToString()} - {ResourceHelper.Get(StringKey.ModManagerForFinalFantasy7)} [{CurrentProfile}]";
            }
        }

        public string SearchText
        {
            get
            {
                return _searchText;
            }
            set
            {
                _searchText = value;
                NotifyPropertyChanged();
            }
        }

        public int SelectedTabIndex
        {
            get
            {
                return _selectedTabIndex;
            }
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(ReadmeVisibility));

                    ReloadAvailableFilters(recheckFilters: false);

                    if ((TabIndex)_selectedTabIndex == TabIndex.MyMods)
                    {
                        if (CatalogMods.PreviousSearchText != MyMods.PreviousSearchText || MyMods.HasPreviousCategoriesOrTags)
                        {
                            SearchText = "";
                            DoSearch();
                        }

                        ModUpdateMenuVisibility = Visibility.Visible;
                        UpdateModPreviewInfo(MyMods.GetSelectedMod());
                    }
                    else
                    {
                        if (MyMods.PreviousSearchText != CatalogMods.PreviousSearchText || CatalogMods.HasPreviousCategoriesOrTags)
                        {
                            SearchText = "";
                            DoSearch();
                        }

                        ModUpdateMenuVisibility = Visibility.Collapsed;
                        UpdateModPreviewInfo(CatalogMods.GetSelectedMod());
                    }
                }
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
                Logger.Info(_statusMessage);
                NotifyPropertyChanged();
            }
        }

        public string CurrentProfile
        {
            get
            {
                return _currentProfile;
            }
            set
            {
                _currentProfile = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(WindowTitle));
            }
        }

        public string PreviewModAuthor
        {
            get
            {
                return _previewModAuthor;
            }
            set
            {
                _previewModAuthor = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModName
        {
            get
            {
                return _previewModName;
            }
            set
            {
                _previewModName = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModVersion
        {
            get
            {
                return _previewModVersion;
            }
            set
            {
                _previewModVersion = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModReleaseDate
        {
            get
            {
                return _previewModReleaseDate;
            }
            set
            {
                _previewModReleaseDate = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModReleaseNotes
        {
            get
            {
                return _previewModReleaseNotes;
            }
            set
            {
                _previewModReleaseNotes = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModCategory
        {
            get
            {
                return _previewModCategory;
            }
            set
            {
                _previewModCategory = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModDescription
        {
            get
            {
                return _previewModDescription;
            }
            set
            {
                _previewModDescription = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewModLink
        {
            get
            {
                return _previewModLink;
            }
            set
            {
                _previewModLink = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(PreviewModHasLink));
            }
        }

        public bool PreviewModHasLink
        {
            get
            {
                return !string.IsNullOrEmpty(PreviewModLink);
            }
        }

        public bool PreviewModHasReadMe
        {
            get
            {
                return _previewModHasReadMe;
            }
            set
            {
                _previewModHasReadMe = value;
                NotifyPropertyChanged();
            }
        }

        public bool PreviewIsNotifyAboutUpdatesChecked
        {
            get
            {
                return _previewIsNotifyAboutUpdatesChecked;
            }
            set
            {
                if (_previewIsNotifyAboutUpdatesChecked != value)
                {
                    _previewIsNotifyAboutUpdatesChecked = value;
                    NotifyPropertyChanged();
                    ChangeUpdateModTypeForSelectedMod();
                }
            }
        }

        public bool PreviewIsAutoUpdateModsChecked
        {
            get
            {
                return _previewIsAutoUpdateModsChecked;
            }
            set
            {
                if (_previewIsAutoUpdateModsChecked != value)
                {
                    _previewIsAutoUpdateModsChecked = value;
                    NotifyPropertyChanged();
                    ChangeUpdateModTypeForSelectedMod();
                }
            }
        }

        public bool PreviewIgnoreModUpdatesChecked
        {
            get
            {
                return _previewIgnoreModUpdatesChecked;
            }
            set
            {
                if (_previewIgnoreModUpdatesChecked != value)
                {
                    _previewIgnoreModUpdatesChecked = value;
                    NotifyPropertyChanged();
                    ChangeUpdateModTypeForSelectedMod();
                }
            }
        }

        public Visibility ModUpdateMenuVisibility
        {
            get
            {
                return _modUpdateMenuVisibility;
            }
            set
            {
                if (_modUpdateMenuVisibility != value)
                {
                    _modUpdateMenuVisibility = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string UpdateModButtonText
        {
            get
            {
                var selected = MyMods.GetSelectedMod();

                if (selected?.InstallInfo != null)
                {
                    if (selected.InstallInfo.IsUpdateAvailable)
                    {
                        ModStatus status = Sys.GetStatus(selected.InstallInfo.ModID);
                        if (status != ModStatus.Downloading && status != ModStatus.Updating)
                        {
                            return ResourceHelper.Get(StringKey.UpdateAvailable);
                        }
                        else
                        {
                            return ResourceHelper.Get(StringKey.UpdateDownloading);
                        }
                    }
                    else
                    {
                        switch (selected.InstallInfo.UpdateType)
                        {
                            case UpdateType.Notify:
                                return ResourceHelper.Get(StringKey.NoUpdates);

                            case UpdateType.Ignore:
                                return ResourceHelper.Get(StringKey.UpdatesIgnored);

                            case UpdateType.Install:
                                return ResourceHelper.Get(StringKey.AutoUpdate);
                        }
                    }
                }

                return "";
            }
        }

        public bool IsUpdateModButtonEnabled
        {
            get
            {
                var selected = MyMods.GetSelectedMod();

                if (selected?.InstallInfo != null && selected.InstallInfo.IsUpdateAvailable)
                {
                    ModStatus status = Sys.GetStatus(selected.InstallInfo.ModID);
                    if (status != ModStatus.Downloading && status != ModStatus.Updating)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Uri PreviewModImageSource
        {
            get
            {
                return _previewModImageSource;
            }
            set
            {
                if (_previewModImageSource == null || !_previewModImageSource.Equals(value))
                {
                    _previewModImageSource = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<FilterItemViewModel> AvailableFilters
        {
            get
            {
                if (_availableFilters == null)
                    _availableFilters = new List<FilterItemViewModel>();

                return _availableFilters;
            }
            set
            {
                _availableFilters = value;
                NotifyPropertyChanged();
            }
        }

        public List<FilterItemViewModel> CheckedCategories
        {
            get
            {
                return AvailableFilters.Where(c => c.FilterType == FilterItemType.Category && c.IsChecked && c.Name != ShowAllText).ToList();
            }
        }

        public List<FilterItemViewModel> CheckedTags
        {
            get
            {
                return AvailableFilters.Where(c => c.FilterType == FilterItemType.Tag && c.IsChecked && c.Name != ShowAllText).ToList();
            }
        }

        public Visibility LoadingGifVisibility
        {
            get
            {
                return _loadingGifVisibility;
            }
            set
            {
                if (_loadingGifVisibility != value)
                {
                    _loadingGifVisibility = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Visibility NoImageTextVisibility
        {
            get
            {
                return _noImageTextVisibility;
            }
            set
            {
                if (_noImageTextVisibility != value)
                {
                    _noImageTextVisibility = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsFlashingStatus
        {
            get
            {
                return _isFlashingStatus;
            }
            set
            {
                _isFlashingStatus = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility ReadmeVisibility
        {
            get
            {
                if ((TabIndex)SelectedTabIndex == TabIndex.BrowseCatalog)
                    return Visibility.Hidden;

                return Visibility.Visible;
            }
        }

        public bool IsPlayButtonEnabled
        {
            get
            {
                return !IsGameLaunching;
            }
        }

        public bool IsPlayToggleButtonEnabled
        {
            get
            {
                return !IsGameLaunching && _isPlayToggleButtonEnabled;
            }
            set
            {
                _isPlayToggleButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsGameLaunching
        {
            get
            {
                return _isGameLaunching;
            }
            private set
            {
                _isGameLaunching = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsPlayButtonEnabled));
                NotifyPropertyChanged(nameof(IsPlayToggleButtonEnabled));
            }
        }


        #endregion

        private UpdateChecker updater = new UpdateChecker();
        
        public MainWindowViewModel()
        {
            SearchText = "";
            IsGameLaunching = false;
            IsPlayToggleButtonEnabled = true;

            MyMods = new MyModsViewModel();
            MyMods.SelectedModChanged += ModsViewModel_SelectedModChanged;

            CatalogMods = new CatalogViewModel();
            CatalogMods.SelectedModChanged += CatalogViewModel_SelectedModChanged;

            LoadingGifVisibility = Visibility.Hidden;
        }


        /// <summary>
        /// Initializes the view model and initializes other files/profiles for use with 7th Heaven.
        /// This should be called first to ensure the app is fully initialized
        /// </summary>
        public void InitViewModel()
        {
            Sys.MessageReceived += Sys_MessageReceived;
            Sys.StatusChanged += new EventHandler<ModStatusEventArgs>(Sys_StatusChanged);

            // Set the Downloads Interface so Sys can use Download methods defined in the CatalogViewModel
            Sys.Downloads = CatalogMods;

            GeneralSettingsViewModel.AutoDetectSystemPaths(Sys.Settings);

            LoadCatalogXmlFile();

            Sys.InitLoaderContext();


            ThemeSettingsViewModel.LoadThemeFromFile();
            ITheme themeSettings = ThemeSettingsViewModel.GetThemeSettingsFromFile();

            if (!string.IsNullOrEmpty(themeSettings.BackgroundImageBase64))
            {
                try
                {
                    byte[] imageBytes = Convert.FromBase64String(themeSettings.BackgroundImageBase64);
                    UpdateBackgroundImage(imageBytes);
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    UpdateBackgroundImage(null);
                }
            }

            InitActiveProfile();

            // check if the settings window should be showed (on first start or if errors in settings)
            bool showSettings = false;
            if (Sys.Settings.IsFirstStart)
            {
                Sys.Settings.IsFirstStart = false;
                showSettings = true;
            }
            else
            {
                IEnumerable<string> errors = Sys.Settings.VerifySettings();
                if (errors.Any())
                {
                    string msg = $"{ResourceHelper.Get(StringKey.FollowingErrorsFoundInConfiguration)}\n" + string.Join("\n", errors);
                    Logger.Warn(msg);
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.ErrorsFoundInGeneralSettingsViewAppLog)));
                    showSettings = true;
                }
            }

            // if new setting SelectedMidiData is missing, reset Game Launch Settings to defaults
            if (String.IsNullOrWhiteSpace(Sys.Settings.GameLaunchSettings.SelectedMidiData))
            {
                Sys.Settings.GameLaunchSettings = LaunchSettings.DefaultSettings();
            }

            if (showSettings)
            {
                InitActiveProfile(); // On upgrade installs, my mods is blank 1st run until profile reloaded.
                ShowGeneralSettingsWindow();
            }

            Sys.TryAutoImportModsAsync();


            CatalogMods.RefreshListRequested += CatalogList_RefreshRequested;
            MyMods.RefreshListRequested += ModList_RefreshRequested;


            CatalogMods.ReloadModList();
            CatalogMods.CheckForCatalogUpdatesAsync();


            ReloadAvailableFilters();

            // this deletes any temp images that were extracted from IRO archives
            // ... the temp images are used for the configure mod window
            // ... the app does not release the file lock on the images at runtime of the app so 
            // ... this will ensure the images from last app session are deleted
            ConfigureModViewModel.DeleteTempConfigModFolder();

            Sys.AppVersion = App.GetAppVersion();
            StatusMessage = $"{App.GetAppName()} v{Sys.AppVersion.ToString()} {ResourceHelper.Get(StringKey.StartedClickHereToViewAppLog)}  |  {ResourceHelper.Get(StringKey.HintLabel)} {GetRandomHint()}";

            MyMods.ScanForModUpdates();


            // enable ds4 support on init - starts the service that treats ds4 controlers as xbox360/xinput device
            if (Sys.Settings.GameLaunchSettings.EnablePs4ControllerService)
            {
                Logger.Info(ResourceHelper.Get(StringKey.StartingPS4ControllerService));
                DS4ControllerService.Instance.StartService();
            }

            // Ensure temp directory exists
            Directory.CreateDirectory(Sys.PathToTempFolder);

            if (Sys.Settings.HasOption(GeneralOptions.CheckForUpdates))
            {
                Task.Factory.StartNew(() =>
                {
                    System.Threading.Thread.Sleep(5000); // wait 5 seconds after init before checking for update to let UI render
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.CheckingForUpdates), WMessageLogLevel.LogOnly));
                    updater.CheckForUpdates(Sys.Settings.AppUpdateChannel);
                });
            }
        }

        private void CatalogList_RefreshRequested()
        {
            SearchText = "";
            ReloadAvailableFilters(recheckFilters: false);
        }

        /// <summary>
        /// Clears the search text and applied filters.
        /// Triggered when my mods or catalog mods list is refreshed from button click or when category changed for an installed mod
        /// </summary>
        private void ModList_RefreshRequested(bool beforeRefresh)
        {
            if (beforeRefresh)
            {
                if (MyMods.GetSelectedMod() != null)
                {
                    string relativePath = MyMods.GetSelectedMod()?.InstallInfo?.LatestInstalled?.InstalledLocation ?? "";
                    string pathToSelectedMod = Path.Combine(Sys.Settings.LibraryLocation, relativePath);
                    if (Directory.Exists(pathToSelectedMod) && File.Exists(Path.Combine(pathToSelectedMod, "mod.xml")))
                    {
                        // Set the preview image to null before doing a refresh because the mod.xml will be re-parsed and the image cache may get updated with a new image
                        // ... setting to null will prevent IOException due to file being in use
                        SetPreviewImage(null);
                    }
                }
            }
            else
            {
                if (MyMods.GetSelectedMod() != null)
                {
                    string relativePath = MyMods.GetSelectedMod()?.InstallInfo?.LatestInstalled?.InstalledLocation ?? "";
                    string pathToSelectedMod = Path.Combine(Sys.Settings.LibraryLocation, relativePath);
                    if (Directory.Exists(pathToSelectedMod) && File.Exists(Path.Combine(pathToSelectedMod, "mod.xml")))
                    {
                        // ensure the mod preview is reloaded so the new preview image and other info is displayed if the currently selected mod is installed via folder format
                        UpdateModPreviewInfo(MyMods.GetSelectedMod(), forceUpdate: true);
                    }
                }

                SearchText = "";
                ReloadAvailableFilters(recheckFilters: false);

                // refresh button text in case mod scan for updates started downloading the selected mod
                NotifyPropertyChanged(nameof(UpdateModButtonText));
                NotifyPropertyChanged(nameof(IsUpdateModButtonEnabled));
            }
        }

        private void CatalogViewModel_SelectedModChanged(object sender, CatalogModItemViewModel selected)
        {
            if ((TabIndex)SelectedTabIndex == TabIndex.BrowseCatalog)
            {
                UpdateModPreviewInfo(selected);
            }
        }

        private void ModsViewModel_SelectedModChanged(object sender, InstalledModViewModel selected)
        {
            if ((TabIndex)SelectedTabIndex == TabIndex.MyMods)
            {
                UpdateModPreviewInfo(selected);
            }
        }

        private void Sys_StatusChanged(object sender, ModStatusEventArgs e)
        {
            if (e.Status == ModStatus.PendingInstall)
            {
                return;
            }

            CatalogMods.UpdateModDetails(e.ModID);

            if (e.Status == ModStatus.Installed)
            {
                // remove newly installed mod from info cache incase it is stale or the install location changed
                InstalledItem mod = Sys.Library.GetItem(e.ModID);
                string mfile = mod.LatestInstalled.InstalledLocation;
                InstalledItem.RemoveFromInfoCache(mfile);
                MyMods.ReloadModListFromUIThread(MyMods.GetSelectedMod()?.InstallInfo.ModID, SearchText, CheckedCategories, CheckedTags);
            }

            if (e.Status == ModStatus.Installed && e.OldStatus != ModStatus.Installed && Sys.Settings.HasOption(GeneralOptions.AutoActiveNewMods))
            {
                if (Sys.ActiveProfile.Items.Any(i => i.ModID.Equals(e.ModID) && !i.IsModActive))
                {
                    MyMods.ToggleActivateMod(e.ModID);
                }
            }
            if (e.OldStatus == ModStatus.Installed && e.Status == ModStatus.NotInstalled && Sys.ActiveProfile.Items.Any(i => i.ModID.Equals(e.ModID) && !i.IsModActive))
            {
                MyMods.ToggleActivateMod(e.ModID);
            }


            if (e.Status == ModStatus.InfoChanged)
            {
                // update mod preview info page when a change (e.g. image downloaded or update available) has happeend for selected mod
                if ((TabIndex)SelectedTabIndex == TabIndex.MyMods)
                {
                    InstalledModViewModel currentlySelected = MyMods.GetSelectedMod();

                    if (currentlySelected?.InstallInfo.ModID == e.ModID)
                    {
                        UpdateModPreviewInfo(currentlySelected, forceUpdate: true);
                    }
                }
                else
                {
                    CatalogModItemViewModel currentlySelected = CatalogMods.GetSelectedMod();

                    if (currentlySelected?.Mod.ID == e.ModID)
                    {
                        UpdateModPreviewInfo(currentlySelected, forceUpdate: true);
                    }
                }
            }

        }

        /// <summary>
        /// Used to cleanup any resources (e.g. when shutting down app).
        /// Saves profile, Unsubscribes from Events, and nullifys certain objects.
        /// </summary>
        internal void CleanUp()
        {
            SaveActiveProfile();
            Sys.Save();

            MyMods.SelectedModChanged -= ModsViewModel_SelectedModChanged;
            MyMods.RefreshListRequested -= ModList_RefreshRequested;
            MyMods.ClearModList();
            MyMods.ModList = null;
            MyMods = null;

            CatalogMods.SelectedModChanged -= CatalogViewModel_SelectedModChanged;
            CatalogMods.RefreshListRequested -= CatalogList_RefreshRequested;
            CatalogMods.CatalogModList.Clear();
            CatalogMods = null;

            if (Sys.Settings.GameLaunchSettings.EnablePs4ControllerService)
            {
                DS4ControllerService.Instance.StopService();
            }
        }

        private void UpdateModPreviewInfo(InstalledModViewModel selected, bool forceUpdate = false)
        {
            NotifyPropertyChanged(nameof(UpdateModButtonText));
            NotifyPropertyChanged(nameof(IsUpdateModButtonEnabled));

            if (selected == null)
            {
                _previewMod = null;
                PreviewModAuthor = "";
                PreviewModName = "";
                PreviewModVersion = "";
                PreviewModReleaseDate = "";
                PreviewModReleaseNotes = "";
                PreviewModCategory = "";
                PreviewModDescription = "";
                PreviewModLink = "";
                PreviewIsAutoUpdateModsChecked = false;
                PreviewIsNotifyAboutUpdatesChecked = false;
                PreviewIgnoreModUpdatesChecked = false;
                PreviewModImageSource = null;
                return;
            }

            _previewMod = selected.InstallInfo.CachedDetails;

            PreviewModAuthor = selected.Author;
            PreviewModVersion = selected.InstallInfo.CachedDetails.LatestVersion.Version.ToString();
            PreviewModName = selected.Name;
            PreviewModReleaseDate = selected.ReleaseDate;
            PreviewModReleaseNotes = selected.InstallInfo.CachedDetails.LatestVersion.ReleaseNotes;
            PreviewModCategory = selected.Category;
            PreviewModDescription = selected.InstallInfo.CachedDetails.Description;
            PreviewModLink = selected.InstallInfo.CachedDetails.Link;

            PreviewModHasReadMe = selected.HasReadMe; // checks if mod .iro or folder has a readme file and caches the result since the disc lookup is slow on larger mods

            ModUpdateMenuVisibility = Visibility.Visible;
            PreviewIsAutoUpdateModsChecked = selected.InstallInfo?.UpdateType == UpdateType.Install;
            PreviewIsNotifyAboutUpdatesChecked = selected.InstallInfo?.UpdateType == UpdateType.Notify;
            PreviewIgnoreModUpdatesChecked = selected.InstallInfo?.UpdateType == UpdateType.Ignore;


            if (!string.IsNullOrWhiteSpace(selected.InstallInfo.CachedDetails.LatestVersion.PreviewImage))
            {
                string pathToImage = Sys.ImageCache.GetImagePath(selected.InstallInfo.CachedDetails.LatestVersion.PreviewImage, selected.InstallInfo.CachedDetails.ID);
                SetPreviewImage(pathToImage, forceUpdate);
            }
            else
            {
                // no preview image found cached for mod
                NoImageTextVisibility = Visibility.Visible;
                LoadingGifVisibility = Visibility.Hidden;
                PreviewModImageSource = null;
            }
        }

        private void UpdateModPreviewInfo(CatalogModItemViewModel selected, bool forceUpdate = false)
        {
            if (selected == null)
            {
                _previewMod = null;
                PreviewModAuthor = "";
                PreviewModName = "";
                PreviewModVersion = "";
                PreviewModReleaseDate = "";
                PreviewModReleaseNotes = "";
                PreviewModCategory = "";
                PreviewModDescription = "";
                PreviewModLink = "";
                PreviewModImageSource = null;
                return;
            }

            _previewMod = selected.Mod;

            PreviewModAuthor = selected.Author;
            PreviewModVersion = selected.Mod.LatestVersion.Version.ToString();
            PreviewModName = selected.Name;
            PreviewModReleaseDate = selected.ReleaseDate;
            PreviewModReleaseNotes = selected.Mod.LatestVersion.ReleaseNotes;
            PreviewModCategory = selected.Category;
            PreviewModDescription = selected.Mod.Description;
            PreviewModLink = selected.Mod.Link;
            PreviewModHasReadMe = false; // no READMEs for catalog (only installed mods)

            ModUpdateMenuVisibility = Visibility.Collapsed; // do not display the 'update avaialble' menu on catalog mods

            string pathToImage = Sys.ImageCache.GetImagePath(selected.Mod.LatestVersion.PreviewImage, selected.Mod.ID);

            SetPreviewImage(pathToImage, forceUpdate);
        }

        private void SetPreviewImage(string pathToImage, bool forceUpdate = false)
        {
            NoImageTextVisibility = Visibility.Hidden;
            LoadingGifVisibility = Visibility.Visible;
            Uri newImageUri = pathToImage == null ? null : new Uri(pathToImage);

            if (!forceUpdate && newImageUri?.AbsolutePath == PreviewModImageSource?.AbsolutePath)
            {
                LoadingGifVisibility = Visibility.Hidden; // image does not have to be changed so hide the loading gif
            }
            else
            {
                PreviewModImageSource = newImageUri;
            }
        }

        private void InitActiveProfile()
        {
            Sys.ActiveProfile = null;

            Directory.CreateDirectory(Path.Combine(Sys.SysFolder, "profiles"));

            if (!String.IsNullOrWhiteSpace(Sys.Settings.CurrentProfile) && File.Exists(Sys.PathToCurrentProfileFile))
            {
                try
                {
                    Sys.ActiveProfile = Util.Deserialize<Profile>(Sys.PathToCurrentProfileFile);
                    RefreshProfile();
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    Logger.Warn("Failed to load current profile xml ... Setting current profile to null");
                    Sys.Settings.CurrentProfile = null;
                }
            }

            if (Sys.ActiveProfile == null)
            {
                Sys.ActiveProfile = new Profile();
                Sys.Settings.CurrentProfile = "Default";
                Sys.SaveSettings();

                CurrentProfile = Sys.Settings.CurrentProfile;
            }
        }

        /// <summary>
        /// Sets <see cref="CurrentProfile"/> to match what is in <see cref="Sys.Settings"/> and Reloads 'My Mods' list for the profile.
        /// </summary>
        public void RefreshProfile()
        {
            if (Sys.ActiveProfile == null)
            {
                return;
            }

            CurrentProfile = Sys.Settings.CurrentProfile;


            // reload list of active mods for the profile
            MyMods.ClearModList();
            MyMods.ReloadModList(null, SearchText, CheckedCategories, CheckedTags);
        }

        /// <summary>
        /// Saves <see cref="Sys.ActiveProfile"/> to disk.
        /// </summary>
        internal static bool SaveActiveProfile()
        {
            if (Sys.ActiveProfile == null)
            {
                return false;
            }

            try
            {
                using (FileStream fs = new FileStream(Sys.PathToCurrentProfileFile, FileMode.Create))
                {
                    Util.Serialize(Sys.ActiveProfile, fs);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        private void Sys_MessageReceived(object sender, MessageEventArgs e)
        {
            string receivedMessage = e.Message.Text;

            // translate message if needed
            if (string.IsNullOrEmpty(receivedMessage) && e.Message.TextTranslationKey.HasValue)
            {
                receivedMessage = ResourceHelper.Get(e.Message.TextTranslationKey.Value);
            }
            else if (e.Message.TextTranslationKey.HasValue)
            {
                // replace key in existing string with translated text
                receivedMessage = receivedMessage.Replace($"[{e.Message.TextTranslationKey}]", ResourceHelper.Get(e.Message.TextTranslationKey.Value));
            }

            // log message to app log or status bar
            switch (e.Message.LogLevel)
            {
                case WMessageLogLevel.Error:
                    StatusMessage = receivedMessage; // this will set the status bar and log to app log
                    break;

                case WMessageLogLevel.Info:
                    StatusMessage = receivedMessage; // this will set the status bar and log to app log
                    break;

                case WMessageLogLevel.LogOnly: // only display in app log and not status bar
                    Logger.Info(receivedMessage);
                    break;

                case WMessageLogLevel.StatusOnly: // only display in status bar and not in app log
                    _statusMessage = receivedMessage;
                    NotifyPropertyChanged(nameof(StatusMessage));
                    break;

                default:
                    break;
            }

            // include exception in logs if it exists in message received
            if (e.Message.LoggedException != null)
            {
                Logger.Error(e.Message.LoggedException);
            }

            // flash status bar if important
            if (e.Message.IsImportant)
            {
                FlashStatusBar();
            }
        }

        private void FlashStatusBar(int timeToFlashInMilliseconds = 1400)
        {
            IsFlashingStatus = true;

            Task.Factory.StartNew(() =>
            {
                System.Threading.Thread.Sleep(timeToFlashInMilliseconds);
                IsFlashingStatus = false;
            });
        }

        private void LoadCatalogXmlFile()
        {
            try
            {
                _catFile = Path.Combine(Sys.SysFolder, "catalog.xml");
                Sys.SetNewCatalog(Util.Deserialize<Catalog>(_catFile));
            }
            catch (Exception e)
            {
                Logger.Warn(e);
                Logger.Warn("failed to load catalog.xml - initializing empty catalog ...");
                Sys.SetNewCatalog(new Catalog());
            }
        }

        internal static bool CheckAllowedActivate(Guid modID)
        {
            InstalledItem mod = Sys.Library.GetItem(modID);
            InstalledVersion inst = mod.LatestInstalled;
            string mfile = Path.Combine(Sys.Settings.LibraryLocation, inst.InstalledLocation);
            bool hasCode;
            var modInfo = mod.GetModInfo();

            if (mfile.EndsWith(".iro", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var arc = new _7thWrapperLib.IrosArc(mfile))
                {
                    hasCode = arc.AllFolderNames().Any(s => s.EndsWith("hext", StringComparison.InvariantCultureIgnoreCase));
                }
            }
            else if (Directory.Exists(mfile))
            {
                hasCode = Directory.GetDirectories(mfile, "*", System.IO.SearchOption.AllDirectories)
                                   .Any(s => s.EndsWith("hext", StringComparison.InvariantCultureIgnoreCase));
            }
            else
                hasCode = false;

            if (modInfo != null)
            {
                hasCode |= modInfo.LoadPlugins.Any();
                hasCode |= modInfo.LoadLibraries.Any();
                hasCode |= modInfo.LoadAssemblies.Any();
                hasCode |= modInfo.LoadPrograms.Any();
            }

            if (!hasCode) return true;


            if (Sys.Settings.HasOption(GeneralOptions.WarnAboutModCode))
            {
                // invoke the message on the Dispatcher UI Thread since this could be called from background threads
                return App.Current.Dispatcher.Invoke(() =>
                {
                    string msg = String.Format(ResourceHelper.Get(StringKey.ThisModContainsDataThatCouldHarm), mod.CachedDetails.Name);

                    AllowModToRunWindow warningWindow = new AllowModToRunWindow(msg);
                    warningWindow.ShowDialog();

                    if (warningWindow.ViewModel.IsChecked)
                    {
                        // set settings to turn off warning
                        Sys.Settings.RemoveOption(GeneralOptions.WarnAboutModCode);
                        Sys.SaveSettings();
                    }

                    if (warningWindow.ViewModel.YesRadioButtonIsChecked)
                    {
                        return true;
                    }

                    return false;
                });
            }

            return true;
        }

        internal void DoSearch()
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // user is now searching by text so clear checked filters/tags
                AvailableFilters.ForEach(a => a.IsChecked = false);
            }

            if ((TabIndex)SelectedTabIndex == TabIndex.BrowseCatalog)
            {
                CatalogMods.ClearRememberedSearchTextAndCategories();
                CatalogMods.ReloadModList(CatalogMods.GetSelectedMod()?.Mod?.ID, SearchText, CheckedCategories, CheckedTags);
            }
            else
            {
                MyMods.ClearRememberedSearchTextAndCategories();
                MyMods.ReloadModList(MyMods.GetSelectedMod()?.InstallInfo?.ModID, SearchText, CheckedCategories, CheckedTags);
            }

            ReloadAvailableFilters();
        }

        internal void OpenPreviewModLink()
        {
            if (string.IsNullOrEmpty(PreviewModLink))
            {
                Logger.Warn("link is null/empty");
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = PreviewModLink
                };

                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

        }

        /// <summary>
        /// Opens the read-me of the selected mod being previewed.
        /// First looks for .html -> .md -> .txt in that order.
        /// </summary>
        internal void OpenPreviewModReadMe()
        {
            InstalledVersion inst = Sys.Library.GetItem(_previewMod.ID)?.LatestInstalled;

            if (inst == null)
            {
                Logger.Warn("cant find installed version");
                return;
            }

            bool hasReadmeFile = false;
            string tempDirPath = Path.Combine(Sys.SysFolder, "temp");
            string tempFileName = null;

            string pathToTempFile;

            Directory.CreateDirectory(tempDirPath);

            if (inst.HasFile("readme.html"))
            {
                tempFileName = "readme.html";
                hasReadmeFile = true;
            }
            else if (inst.HasFile("readme.md"))
            {
                tempFileName = "readme.md";
                hasReadmeFile = true;
            }
            else if (inst.HasFile("readme.txt"))
            {
                tempFileName = "readme.txt";
                hasReadmeFile = true;
            }

            if (!hasReadmeFile)
            {
                Logger.Warn($"no readme file found for {_previewMod.Name} - {_previewMod.ID}");
                return;
            }

            if (tempFileName.Equals("readme.md", StringComparison.InvariantCultureIgnoreCase))
            {
                pathToTempFile = Path.Combine(tempDirPath, "readme.html"); // save .md file as .html so it can open in browser
            }
            else
            {
                pathToTempFile = Path.Combine(tempDirPath, tempFileName);
            }

            try
            {
                using (Stream s = inst.GetData(tempFileName))
                {
                    using (FileStream tempFile = new FileStream(pathToTempFile, FileMode.Create, FileAccess.Write))
                    {
                        s.CopyTo(tempFile);
                    }
                }

                if (pathToTempFile.EndsWith(".txt"))
                {
                    Process.Start("notepad.exe", pathToTempFile);
                }
                else
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = pathToTempFile
                    };
                    Process.Start(startInfo);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "failed to open readme");
                return;
            }

        }

        /// <summary>
        /// sets IsChecked to value of <paramref name="isChecked"/> for all items in <paramref name="filterItems"/> except for the 'Show All' category 
        /// </summary>
        private void ToggleIsCheckedForAll(bool isChecked, List<FilterItemViewModel> filterItems)
        {
            foreach (var item in filterItems.Where(c => c.Name != ShowAllText && c.FilterType != FilterItemType.Separator).ToList())
            {
                item.IsChecked = isChecked;
            }
        }

        /// <summary>
        /// Updates the IsChecked property of the 'Show All' item based on all other items in <paramref name="filterItems"/> being checked or not.
        /// </summary>
        private void CheckOrUncheckShowAllFilter(List<FilterItemViewModel> filterItems)
        {
            FilterItemViewModel item = filterItems.FirstOrDefault(cat => cat.Name == ShowAllText);

            if (item == null)
            {
                return;
            }

            item.SetIsChecked(filterItems.Where(cat => cat.Name != ShowAllText && cat.FilterType != FilterItemType.Separator).All(cat => cat.IsChecked));
        }

        internal List<string> GetTagsForSelectedTab()
        {
            List<string> tags = new List<string>();

            if ((TabIndex)SelectedTabIndex == TabIndex.BrowseCatalog)
            {
                foreach (List<string> modTags in Sys.Catalog.Mods.Where(c => c.Tags.Count > 0).Select(c => c.Tags))
                {
                    tags.AddRange(modTags);
                }

                // include the name of the catalogs as a tag
                List<string> catalogNames = Sys.Catalog.Mods.Where(m => !string.IsNullOrWhiteSpace(m.SourceCatalogName))
                                                            .Select(m => m.SourceCatalogName)
                                                            .Distinct().ToList();

                tags.AddRange(catalogNames);
            }
            else
            {
                foreach (List<string> modTags in Sys.Library.Items.Where(m => m.CachedDetails.Tags.Count > 0).Select(m => m.CachedDetails.Tags))
                {
                    tags.AddRange(modTags);
                }
            }

            tags = tags.Select(s => $"{s.Substring(0, 1).ToUpper()}{s.Substring(1)}") // make Tag display as proper case e.g. 'My tag' intead of 'my tag'
                       .Distinct(StringComparer.CurrentCultureIgnoreCase)
                       .OrderBy(s => s)
                       .ToList();

            return tags;
        }

        internal List<string> GetCategoriesForSelectedTab()
        {
            List<string> categories = null;

            if ((TabIndex)SelectedTabIndex == TabIndex.BrowseCatalog)
            {
                categories = Sys.Catalog.Mods.Select(c =>
                                                {
                                                    if (string.IsNullOrEmpty(c.Category))
                                                        return UnknownText;

                                                    return ResourceHelper.Get(ModLoadOrder.GetCategoryKey(c.Category));
                                                })
                                             .ToList();
            }
            else
            {
                categories = MyMods.ModList.Select(c =>
                                                {
                                                    if (string.IsNullOrEmpty(c.Category))
                                                        return UnknownText;

                                                    return c.Category;
                                                })
                                           .ToList();
            }

            categories = categories.Distinct().OrderBy(s => s).ToList();

            return categories;
        }

        internal void ApplyFiltersAndReloadList()
        {
            if (CheckedCategories.Count > 0 || CheckedTags.Count > 0)
            {
                //user is now filtering by tags/category so clear search text
                SearchText = "";
            }

            if ((TabIndex)SelectedTabIndex == TabIndex.BrowseCatalog)
            {
                CatalogMods.ClearRememberedSearchTextAndCategories();
                CatalogMods.ReloadModList(CatalogMods.GetSelectedMod()?.Mod?.ID, SearchText, CheckedCategories, CheckedTags);
            }
            else
            {
                MyMods.ClearRememberedSearchTextAndCategories();
                MyMods.ReloadModList(MyMods.GetSelectedMod()?.InstallInfo?.ModID, SearchText, CheckedCategories, CheckedTags);
            }

            ReloadAvailableFilters(true);
        }

        /// <summary>
        /// Reloads the list of dropdown category/tag filters a user can click on based on what is in the my mods / browse catalog tab
        /// </summary>
        internal void ReloadAvailableFilters(bool recheckFilters = true)
        {
            List<string> tags = GetTagsForSelectedTab();
            List<string> categories = GetCategoriesForSelectedTab();
            List<FilterItemViewModel> newList = new List<FilterItemViewModel>();

            newList.AddRange(categories.Select(c => new FilterItemViewModel(c, FilterItemType.Category)
            {
                OnChecked = new Action<bool>(isChecked => CheckOrUncheckShowAllFilter(AvailableFilters))
            }).ToList());

            if (tags.Count > 0)
            {
                newList.Add(new FilterItemViewModel("", FilterItemType.Separator));

                newList.AddRange(tags.Select(t => new FilterItemViewModel(t, FilterItemType.Tag)
                {
                    OnChecked = new Action<bool>(isChecked => CheckOrUncheckShowAllFilter(AvailableFilters))
                }).ToList());
            }


            // re-check items
            if (recheckFilters)
            {
                List<FilterItemViewModel> oldItems = AvailableFilters.ToList();

                foreach (FilterItemViewModel item in newList)
                {
                    if (oldItems.Any(c => c.Name == item.Name && c.IsChecked && c.FilterType == item.FilterType))
                    {
                        item.IsChecked = true;
                    }
                }
            }

            // setup 'Show All' filter item as first in the list
            bool allChecked = newList.Where(c => c.FilterType != FilterItemType.Separator).All(c => c.IsChecked);
            FilterItemViewModel showAllItem = new FilterItemViewModel(ShowAllText, FilterItemType.ShowAll)
            {
                IsChecked = allChecked,
                OnChecked = new Action<bool>(isChecked => ToggleIsCheckedForAll(isChecked, AvailableFilters))
            };
            newList.Insert(0, showAllItem);

            AvailableFilters = newList;
        }

        /// <summary>
        /// Opens /Resources/Help/index.html if it exists as a new process
        /// </summary>
        internal void LaunchHelpPage()
        {
            string helpFile = Path.Combine(new string[] { Sys._7HFolder, "Resources", "Help", "index.html" });

            if (!File.Exists(helpFile))
            {
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.CannotOpenHelp)));
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = helpFile
            };

            Process.Start(startInfo);
        }

        internal string GetRandomHint()
        {
            if (AppHints == null)
            {
                return "";
            }

            Random r = new Random();
            return AppHints[r.Next(0, AppHints.Count)];
        }

        internal void LaunchGame(bool variableDump = false, bool debugLogging = false, bool noMods = false, bool noValidation = false)
        {
            IsGameLaunching = true;
            GameLauncher.Instance.LaunchCompleted += GameLauncher_LaunchCompleted;
            GameLaunchWindow.Show(variableDump, debugLogging, noMods, noValidation);
        }

        private void GameLauncher_LaunchCompleted(bool wasSuccessful)
        {
            GameLauncher.Instance.LaunchCompleted -= GameLauncher_LaunchCompleted;
            IsGameLaunching = false;
        }

        internal void AddIrosUrlToSubscriptions(string irosUrl)
        {
            if (LocationUtil.TryParse(irosUrl, out LocationType type, out string httpUrl))
            {
                if (Sys.Settings.Subscriptions.Any(s => s.Url.Equals(irosUrl, StringComparison.InvariantCultureIgnoreCase)))
                {
                    Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.SubscriptionIsAlreadyAdded), irosUrl), true));
                    return;
                }

                GeneralSettingsViewModel.ResolveCatalogNameFromUrl(irosUrl, name =>
                {
                    Sys.Settings.Subscriptions.Add(new Subscription() { Url = irosUrl, Name = name });
                    Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.AddedToSubscriptions), irosUrl), true));

                    CatalogMods.ForceCheckCatalogUpdateAsync();
                });
            }
            else
            {
                Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.IrosLinkMayBeFormatedIncorrectly), irosUrl), WMessageLogLevel.LogOnly));
            }
        }

        private void ChangeUpdateModTypeForSelectedMod()
        {
            if (!PreviewIsAutoUpdateModsChecked && !PreviewIgnoreModUpdatesChecked && !PreviewIsNotifyAboutUpdatesChecked)
            {
                return; // all three options are false so skip changing the type to avoid incorrect assigning of Ignore
            }

            InstalledModViewModel selected = MyMods.GetSelectedMod();

            if (selected != null)
            {
                UpdateType updateType = UpdateType.Ignore;

                if (PreviewIsAutoUpdateModsChecked)
                {
                    updateType = UpdateType.Install;
                }
                else if (PreviewIsNotifyAboutUpdatesChecked)
                {
                    updateType = UpdateType.Notify;
                }

                InstalledItem libraryItem = Sys.Library.GetItem(selected.InstallInfo.ModID);
                if (selected.InstallInfo != null && libraryItem != null)
                {
                    selected.InstallInfo.UpdateType = updateType; // update info in viewmodel
                    libraryItem.UpdateType = updateType; // and also make sure info is updated in Sys.Library so it will be saved back to library.xml

                }

                NotifyPropertyChanged(nameof(UpdateModButtonText));
                NotifyPropertyChanged(nameof(IsUpdateModButtonEnabled));
            }

        }

        internal void UpdateSelectedMod()
        {
            InstalledModViewModel selected = MyMods.GetSelectedMod();

            if (selected != null && selected.InstallInfo.IsUpdateAvailable)
            {
                // get mod from catalog so it downloads the latest version
                Install.DownloadAndInstall(Sys.GetModFromCatalog(selected.InstallInfo.ModID), true);
                NotifyPropertyChanged(nameof(IsUpdateModButtonEnabled));
                NotifyPropertyChanged(nameof(UpdateModButtonText));
            }
        }

        internal void UpdateBackgroundImage(byte[] newImage)
        {
            try
            {
                if (newImage == null || newImage.Length == 0)
                {
                    MyMods.ThemeImage = null;
                    CatalogMods.ThemeImage = null;
                    return;
                }

                using (var stream = new MemoryStream(newImage))
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = stream;
                    bi.EndInit();

                    MyMods.ThemeImage = bi;
                    CatalogMods.ThemeImage = bi;
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e);
                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToSetBackgroundImageFromTheme), true));

                MyMods.ThemeImage = null;
                CatalogMods.ThemeImage = null;
            }

        }

        /// <summary>
        /// Raises NotifyPropertyChanged event for text that is not dynamically binded to UI
        /// </summary>
        internal void RefreshTranslations()
        {
            NotifyPropertyChanged(nameof(WindowTitle));
            NotifyPropertyChanged(nameof(UpdateModButtonText));

            foreach (var item in MyMods.ModList)
            {
                item.RaiseNotifyPropertyChangedForCategory();
            }

            foreach (var item in CatalogMods.CatalogModList)
            {
                item.RaiseNotifyPropertyChangedForCategory();
            }

            PreviewModCategory = ResourceHelper.Get(ModLoadOrder.GetCategoryKey(_previewMod?.Category));

            ReloadAvailableFilters();
        }

        #region Show Window Methods

        /// <summary>
        /// Opens the 'Profiles' window to manage and switch profiles
        /// </summary>
        internal void ShowProfilesWindow()
        {
            SaveActiveProfile(); // ensure current profile is saved to disk so it shows in list of profiles

            OpenProfileWindow profileWindow = new OpenProfileWindow();
            bool? dialogResult = profileWindow.ShowDialog();

            if (dialogResult.GetValueOrDefault(false))
            {
                RefreshProfile();
            }
        }

        internal void ShowChunkToolWindow()
        {
            ChunkToolWindow toolWindow = new ChunkToolWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            toolWindow.ShowDialog();
        }

        internal void ShowGeneralSettingsWindow()
        {
            GeneralSettingsWindow settingsWindow = new GeneralSettingsWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            bool? didSave = settingsWindow.ShowDialog();

            if (didSave.GetValueOrDefault(false))
            {
                if (settingsWindow.ViewModel.SubscriptionsChanged)
                {
                    CatalogMods.ForceCheckCatalogUpdateAsync();
                }

                if (settingsWindow.ViewModel.HasChangedInstalledModUpdateTypes && SelectedTabIndex == (int)TabIndex.MyMods)
                {
                    MyMods.ScanForModUpdates();
                    UpdateModPreviewInfo(MyMods.GetSelectedMod(), true);
                }
            }
        }

        internal void ShowIroToolsWindow()
        {
            IroCreateWindow createWindow = new IroCreateWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            createWindow.ShowDialog();
        }

        internal void ShowGameDriverConfigWindow()
        {
            string driverCfg = Path.Combine(Sys.InstallPath, "FFNx.toml");
            string appLanguage = System.Configuration.ConfigurationManager.AppSettings["DefaultAppLanguage"];
            string uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "7H_GameDriver_UI.xml"); // default to using english version

            if (string.IsNullOrWhiteSpace(appLanguage))
            {
                appLanguage = System.Threading.Thread.CurrentThread.CurrentCulture.ToString();
            }

            if (appLanguage.Length > 2 && appLanguage != "pt-BR")
            {
                appLanguage = appLanguage.Substring(0, 2);
            }

            if (appLanguage == "pt-BR")
            {
                uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.br.xml");
            }
            else
            {
                switch (appLanguage)
                {
                    case "de":
                        uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.de.xml");
                        break;

                    case "es":
                        uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.es.xml");
                        break;

                    case "fr":
                        uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.fr.xml");
                        break;

                    case "gr":
                        uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.gr.xml");
                        break;

                    //case "ja":
                    //    uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.ja.xml");
                    //    break;

                    case "it":
                        uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "Languages", "7H_GameDriver_UI.it.xml");
                        break;

                    default:
                        uiXmlPath = Path.Combine(Sys._7HFolder, "Resources", "7H_GameDriver_UI.xml"); // default to using english version
                        break;
                }
            }

            if (File.Exists(driverCfg))
            {
                ConfigureGLWindow gLWindow = new ConfigureGLWindow();
                if (gLWindow.Init(uiXmlPath, driverCfg))
                {
                    gLWindow.ShowDialog();
                }
            }
            else
            {
                MessageDialogWindow messageDialog = new MessageDialogWindow(ResourceHelper.Get(StringKey.MissingPath), $"FFNx.toml {ResourceHelper.Get(StringKey.FileNotFound)}\n\nPlease run the game at least once and try again.", MessageBoxButton.OK, MessageBoxImage.Warning);
                messageDialog.ShowDialog();
            }
        }

        internal void ShowGameLaunchSettingsWindow()
        {
            GameLaunchSettingsWindow launchSettingsWindow = new GameLaunchSettingsWindow();
            launchSettingsWindow.ShowDialog();
        }

        internal void ShowCatalogCreationTool()
        {
            CatalogCreationToolWindow window = new CatalogCreationToolWindow();
            window.Show();
        }

        #endregion

    }
}
