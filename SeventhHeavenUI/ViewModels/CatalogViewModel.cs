using _7thHeaven.Code;
using CG.Web.MegaApiClient;
using Iros._7th;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
using SeventhHeaven.ViewModels;
using SeventhHeaven.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SeventhHeavenUI.ViewModels
{
    /// <summary>
    /// ViewModel to contain interaction logic for the 'My Mods' tab user control.
    /// </summary>
    public class CatalogViewModel : ViewModelBase, IDownloader
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void OnSelectionChanged(object sender, CatalogModItemViewModel selected);
        public event OnSelectionChanged SelectedModChanged;

        public delegate void OnRefreshListRequested();
        public event OnRefreshListRequested RefreshListRequested;

        private List<CatalogModItemViewModel> _catalogModList;
        private ObservableCollection<DownloadItemViewModel> _downloadList;

        internal ReloadListOption _previousReloadOptions;

        public string PreviousSearchText
        {
            get => _previousReloadOptions.SearchText ?? "";
        }

        public bool HasPreviousCategoriesOrTags
        {
            get => _previousReloadOptions?.Categories?.Count > 0 || _previousReloadOptions?.Tags?.Count > 0;
        }

        private object _listLock = new object();
        private object _downloadLock = new object();
        private bool _isSelectedDownloadPaused;
        private DownloadItemViewModel _selectedDownload;
        private bool _pauseDownloadIsEnabled;
        private string _pauseDownloadToolTip;
        private BitmapImage _themeImage;

        /// <summary>
        /// List of installed mods (includes active mods in the currently active profile)
        /// </summary>
        public List<CatalogModItemViewModel> CatalogModList
        {
            get
            {
                // guarantee the property never returns null
                if (_catalogModList == null)
                {
                    _catalogModList = new List<CatalogModItemViewModel>();
                }

                return _catalogModList;
            }
            set
            {
                _catalogModList = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<DownloadItemViewModel> DownloadList
        {
            get
            {
                // guarantee the property never returns null
                if (_downloadList == null)
                {
                    _downloadList = new ObservableCollection<DownloadItemViewModel>();
                }

                return _downloadList;
            }
            set
            {
                _downloadList = value;
                NotifyPropertyChanged();
            }
        }

        public DownloadItemViewModel SelectedDownload
        {
            get
            {
                return _selectedDownload;
            }
            set
            {
                _selectedDownload = value;
                NotifyPropertyChanged();
                UpdatePauseDownloadButtonUI();
                NotifyPropertyChanged(nameof(RetryInstallIsEnabled));
            }
        }

        public bool IsSelectedDownloadPaused
        {
            get
            {
                return _isSelectedDownloadPaused;
            }
            set
            {
                _isSelectedDownloadPaused = value;
                NotifyPropertyChanged();
            }
        }

        public bool PauseDownloadIsEnabled
        {
            get
            {
                return _pauseDownloadIsEnabled;
            }
            set
            {
                _pauseDownloadIsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public string PauseDownloadToolTip
        {
            get
            {
                return _pauseDownloadToolTip;
            }
            set
            {
                _pauseDownloadToolTip = value;
                NotifyPropertyChanged();
            }
        }

        public BitmapImage ThemeImage
        {
            get
            {
                return _themeImage;
            }
            set
            {
                if (value != _themeImage)
                {
                    _themeImage = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool RetryInstallIsEnabled
        {
            get
            {
                if (SelectedDownload == null)
                {
                    return false;
                }

                return Sys.Library.HasPendingInstall(SelectedDownload?.Download);
            }
        }

        public CatalogViewModel()
        {
            DownloadList = new ObservableCollection<DownloadItemViewModel>();
            PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseResumeSelectedDownload);
            PauseDownloadIsEnabled = false;
            IsSelectedDownloadPaused = false;
            _previousReloadOptions = new ReloadListOption();
        }

        /// <summary>
        /// Invokes the <see cref="SelectedModChanged"/> Event if not null.
        /// </summary>
        internal void RaiseSelectedModChanged(object sender, CatalogModItemViewModel selected)
        {
            SelectedModChanged?.Invoke(this, selected);
        }

        /// <summary>
        /// Loads available mods from catalogs into <see cref="CatalogModList"/> from <see cref="Sys.Catalog.Mods"/>
        /// Ordered by Catalog Subscription, Category, Name
        /// </summary>
        /// <param name="searchText"> empty string returns all mods </param>
        internal void ReloadModList(Guid? modToSelect = null, string searchText = "", IEnumerable<FilterItemViewModel> categories = null, IEnumerable<FilterItemViewModel> tags = null)
        {
            // if there are no mods in the catalog then just clear the list and return since no extra filtering work needs to be done
            if (Sys.Catalog.Mods.Count == 0)
            {
                // make sure to set CatalogModList on the UI thread
                // ... due to uncaught exception that can be thrown when modifying on background thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    lock (_listLock)
                    {
                        CatalogModList.Clear();
                        CatalogModList = new List<CatalogModItemViewModel>();
                    }
                });

                return;
            }


            List<Mod> results;

            categories = _previousReloadOptions.SetOrGetPreviousCategories(categories);
            searchText = _previousReloadOptions.SetOrGetPreviousSearchText(searchText);
            tags = _previousReloadOptions.SetOrGetPreviousTags(tags);

            // map the order of the subscriptions so the results can honor the list order when sorting
            Dictionary<string, int> subscriptionOrder = new Dictionary<string, int>();
            for (int i = 0; i < Sys.Settings.Subscriptions.Count; i++)
            {
                subscriptionOrder[Sys.Settings.Subscriptions[i].Url] = i;
            }

            if (String.IsNullOrEmpty(searchText))
            {
                results = Sys.Catalog.Mods.Where(m =>
                {
                    if (categories.Count() > 0 && tags.Count() > 0)
                    {
                        return FilterItemViewModel.FilterByCategory(m, categories) || FilterItemViewModel.FilterByTags(m, tags);
                    }
                    else if (categories.Count() > 0)
                    {
                        return FilterItemViewModel.FilterByCategory(m, categories);
                    }
                    else
                    {
                        return FilterItemViewModel.FilterByTags(m, tags);
                    }
                }).ToList();
            }
            else
            {
                results = Sys.Catalog.Mods.Where(m =>
                {
                    bool isRelevant = m.SearchRelevance(searchText) > 0;

                    if (categories.Count() > 0 && tags.Count() > 0)
                    {
                        return FilterItemViewModel.FilterByCategory(m, categories) || FilterItemViewModel.FilterByTags(m, tags) || isRelevant;
                    }
                    else if (categories.Count() > 0)
                    {
                        return FilterItemViewModel.FilterByCategory(m, categories) || isRelevant;
                    }
                    else if (tags.Count() > 0)
                    {
                        return FilterItemViewModel.FilterByTags(m, tags) || isRelevant;
                    }
                    else
                    {
                        return isRelevant;
                    }
                }).ToList();
            }

            List<CatalogModItemViewModel> newList = new List<CatalogModItemViewModel>();

            foreach (Mod m in results.OrderBy(k =>
                                        {
                                            subscriptionOrder.TryGetValue(k.SourceCatalogUrl, out int sortOrder);
                                            return sortOrder;
                                        })
                                     .ThenBy(k => k.Category)
                                     .ThenBy(k => k.Name))
            {
                CatalogModItemViewModel item = new CatalogModItemViewModel(m);
                newList.Add(item);
            }

            if (modToSelect != null)
            {
                int index = newList.FindIndex(m => m.Mod.ID == modToSelect);

                if (index >= 0)
                {
                    newList[index].IsSelected = true;
                }
                else if (newList.Count > 0)
                {
                    newList[0].IsSelected = true;
                }
            }
            else
            {
                if (newList.Count > 0)
                {
                    newList[0].IsSelected = true;
                }
            }

            // make sure to set CatalogModList on the UI thread
            // ... due to uncaught exception that can be thrown when modifying on background thread
            App.Current.Dispatcher.Invoke(() =>
            {
                if (newList.Count == 0)
                {
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.NoResultsFound), true));

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        SetCatalogList(newList);
                    }

                    return;
                }

                SetCatalogList(newList);
            });
        }

        private void SetCatalogList(List<CatalogModItemViewModel> newList)
        {
            lock (_listLock)
            {
                CatalogModList.Clear();
                CatalogModList = newList;
            }
        }

        internal void ClearRememberedSearchTextAndCategories()
        {
            _previousReloadOptions = new ReloadListOption();
        }

        /// <summary>
        /// Clears search text and selected categories, then does a force catalog update and reloads the list
        /// </summary>
        internal void RefreshCatalogList()
        {
            ClearRememberedSearchTextAndCategories();
            ForceCheckCatalogUpdateAsync();
        }

        /// <summary>
        /// Returns selected view model in <see cref="CatalogModList"/>.
        /// </summary>
        public CatalogModItemViewModel GetSelectedMod()
        {
            CatalogModItemViewModel selected = null;
            int selectedCount = 0;

            lock (_listLock)
            {
                selected = CatalogModList.Where(m => m.IsSelected).LastOrDefault();
                selectedCount = CatalogModList.Where(m => m.IsSelected).Count();
            }

            // due to virtualization, IsSelected could be set on multiple items... 
            // ... so we will deselect the other items to avoid problems of multiple items being selected
            if (selectedCount > 1)
            {
                lock (_listLock)
                {
                    foreach (var mod in CatalogModList.Where(m => m.IsSelected && m.Mod.ID != selected.Mod.ID))
                    {
                        mod.IsSelected = false;
                    }
                }
            }

            return selected;
        }

        internal Task CheckForCatalogUpdatesAsync(bool forceCheck = false)
        {
            object countLock = new object();

            Task t = Task.Factory.StartNew(() =>
            {
                List<Guid> pingIDs = null;
                string catFile = Path.Combine(Sys.SysFolder, "catalog.xml");

                Directory.CreateDirectory(Path.Combine(Sys.SysFolder, "temp"));

                int subTotalCount = Sys.Settings.SubscribedUrls.Count; // amount of subscriptions to update
                int subUpdateCount = 0; // amount of subscriptions updated

                if (forceCheck)
                {
                    // on force check, initialize a new catalog to ignore any cached items
                    Sys.SetNewCatalog(new Catalog());
                }

                if (Sys.Settings.SubscribedUrls.Count == 0)
                {
                    if (File.Exists(catFile))
                    {
                        File.Delete(catFile);
                    }
                    ReloadModList();
                    return;
                }

                foreach (string subUrl in Sys.Settings.SubscribedUrls.ToArray())
                {
                    Subscription sub = Sys.Settings.Subscriptions.Find(s => s.Url.Equals(subUrl, StringComparison.InvariantCultureIgnoreCase));
                    if (sub == null)
                    {
                        sub = new Subscription() { Url = subUrl, FailureCount = 0, LastSuccessfulCheck = DateTime.MinValue };
                        Sys.Settings.Subscriptions.Add(sub);
                    }

                    if ((sub.LastSuccessfulCheck < DateTime.Now.AddDays(-1)) || forceCheck)
                    {
                        Logger.Info($"{ResourceHelper.Get(StringKey.CheckingSubscription)} {sub.Url}");

                        string uniqueFileName = $"cattemp{Path.GetRandomFileName()}.xml"; // save temp catalog update to unique filename so multiple catalog updates can download async
                        string path = Path.Combine(Sys.SysFolder, "temp", uniqueFileName);

                        DownloadItem download = new DownloadItem()
                        {
                            Links = new List<string>() { subUrl },
                            SaveFilePath = path,
                            Category = DownloadCategory.Catalog,
                            ItemName = $"{ResourceHelper.Get(StringKey.CheckingCatalog)} {subUrl}"
                        };

                        download.IProc = new Install.InstallProcedureCallback(e =>
                        {
                            bool success = (e.Error == null && e.Cancelled == false);
                            subUpdateCount++;

                            if (success)
                            {
                                try
                                {
                                    Catalog c = Util.Deserialize<Catalog>(path);

                                    // set the catalog name of where the mod came from for filtering later
                                    string sourceCatalogName = sub.Name;
                                    if (string.IsNullOrWhiteSpace(sourceCatalogName))
                                    {
                                        sourceCatalogName = c.Name;
                                    }

                                    c.Mods.ForEach(m =>
                                    {
                                        m.SourceCatalogName = sourceCatalogName;
                                        m.SourceCatalogUrl = subUrl;
                                    });


                                    lock (Sys.CatalogLock) // put a lock on the Catalog so multiple threads can only merge one at a time
                                    {
                                        Sys.Catalog = Catalog.Merge(Sys.Catalog, c, out pingIDs);

                                        using (FileStream fs = new FileStream(catFile, FileMode.Create))
                                        {
                                            Util.Serialize(Sys.Catalog, fs);
                                        }
                                    }

                                    Sys.Message(new WMessage() { Text = $"{ResourceHelper.Get(StringKey.UpdatedCatalogFrom)} {subUrl}" });

                                    sub.LastSuccessfulCheck = DateTime.Now;
                                    sub.FailureCount = 0;

                                    foreach (Guid id in pingIDs)
                                    {
                                        Sys.Ping(id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    sub.FailureCount++;
                                    Sys.Message(new WMessage() { Text = $"{ResourceHelper.Get(StringKey.FailedToLoadSubscription)} {subUrl}: {ex.Message}", LoggedException = ex });
                                }
                                finally
                                {
                                    // delete temp catalog
                                    if (File.Exists(path))
                                    {
                                        File.Delete(path);
                                    }
                                }
                            }
                            else
                            {
                                Logger.Warn(e.Error);
                                Logger.Warn(ResourceHelper.Get(StringKey.CatalogDownloadFailed));
                                sub.FailureCount++;
                            }

                            // reload the UI list of catalog mods and scan for any mod updates once all subs have been attempted to download
                            bool isDoneDownloading = false;

                            lock (countLock)
                            {
                                isDoneDownloading = (subUpdateCount == subTotalCount);
                            }

                            if (isDoneDownloading)
                            {
                                ReloadModList(GetSelectedMod()?.Mod.ID);
                                RefreshListRequested?.Invoke();
                            }

                        });

                        Sys.Downloads.AddToDownloadQueue(download);
                    }
                    else
                    {
                        lock (countLock)
                        {
                            subTotalCount -= 1; // This catalog does not have to be updated
                        }
                    }
                }
            });

            return t;
        }

        internal void PauseOrResumeDownload(DownloadItemViewModel downloadItem)
        {
            if (downloadItem.IsCancelling)
            {
                return; // the download has already been marked as being canceled so just return (the cancel is async so it takes a second for it to be removed from download list)
            }

            if (downloadItem?.Download?.FileDownloadTask == null)
            {
                return;
            }


            if (downloadItem.Download.FileDownloadTask?.IsPaused == true)
            {
                downloadItem.DownloadSpeed = ResourceHelper.Get(StringKey.Resuming);
                downloadItem.Download.FileDownloadTask.Start();
            }
            else
            {
                downloadItem.DownloadSpeed = ResourceHelper.Get(StringKey.Paused);
                downloadItem.RemainingTime = ResourceHelper.Get(StringKey.Unknown);
                downloadItem.Download.FileDownloadTask.Pause();
                StartNextModDownload(); // have the next mod in the download queue start automatically
            }

            UpdatePauseDownloadButtonUI();
        }

        internal void ForceCheckCatalogUpdateAsync()
        {
            Task t = CheckForCatalogUpdatesAsync(forceCheck: true);

            t.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    Logger.Warn(taskResult.Exception);
                }
            });
        }

        private void UpdatePauseDownloadButtonUI()
        {
            if (SelectedDownload == null)
            {
                PauseDownloadIsEnabled = false;
                return;
            }

            if (!SelectedDownload.Download.IsModOrPatchDownload || SelectedDownload.Download.FileDownloadTask == null)
            {
                PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseResumeSelectedDownload);
                PauseDownloadIsEnabled = false;
                return;
            }

            if (SelectedDownload.IsCancelling)
            {
                PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseSelectedDownload);
                PauseDownloadIsEnabled = false;
                return;
            }

            if (!SelectedDownload.Download.FileDownloadTask.IsStarted)
            {
                PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseSelectedDownload);
                PauseDownloadIsEnabled = false;
                return;
            }

            if (SelectedDownload.Download.Category == DownloadCategory.ModPatch && SelectedDownload.Download.ParentUniqueID != null)
            {
                // if the selected download is a ModPatch then check to see if the parent download is still downlading
                DownloadItemViewModel parentDownload = null;
                lock (_downloadLock)
                {
                    parentDownload = DownloadList.Where(m => m.Download.UniqueId == SelectedDownload.Download.ParentUniqueID).FirstOrDefault();
                }

                if (parentDownload != null && !parentDownload.Download.IsFileDownloadPaused)
                {
                    // parent download is paused in queue so don't allow patch to pause/resume
                    PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseResumeSelectedDownload);
                    PauseDownloadIsEnabled = false;
                    return;
                }
            }


            if (LocationUtil.TryParse(SelectedDownload.Download.Links[0], out LocationType downloadType, out string url))
            {
                if (downloadType != LocationType.Url && downloadType != LocationType.GDrive) // current implementation only supports Url/GDrive
                {
                    PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseResumeSelectedDownload);
                    PauseDownloadIsEnabled = false;
                    return;
                }
            }

            // if another mod is already downloading then don't allow to resume other mods
            lock (_downloadLock)
            {
                PauseDownloadIsEnabled = !DownloadList.Any(d => d.Download.UniqueId != SelectedDownload.Download.UniqueId && d.Download.IsModOrPatchDownload && d.Download.HasStarted && !d.Download.IsFileDownloadPaused);
            }

            IsSelectedDownloadPaused = SelectedDownload.Download.FileDownloadTask.IsPaused;
            if (IsSelectedDownloadPaused)
            {
                PauseDownloadToolTip = ResourceHelper.Get(StringKey.ResumeSelectedDownload); ;
            }
            else
            {
                PauseDownloadToolTip = ResourceHelper.Get(StringKey.PauseSelectedDownload); ;
            }
        }

        #region Methods Related to Downloads

        internal void CancelDownload(DownloadItemViewModel downloadItemViewModel, bool isCancellingChild = false)
        {
            bool hasChildren = false;
            lock (_downloadLock)
            {
                hasChildren = DownloadList.Any(d => d.Download.ParentUniqueID == downloadItemViewModel.Download.UniqueId);
            }

            if (hasChildren)
            {
                // cancel any child downloads when the parent download is being cancelled
                foreach (var item in DownloadList.Where(d => d.Download.ParentUniqueID == downloadItemViewModel.Download.UniqueId).ToList())
                {
                    CancelDownload(item, true);
                }
            }

            if (downloadItemViewModel.IsCancelling)
            {
                return; // the download has already been marked as being canceled so just return (the cancel is async so it takes a second for it to be removed from download list)
            }

            downloadItemViewModel.IsCancelling = true;
            downloadItemViewModel.ItemName = downloadItemViewModel.ItemName.Replace(ResourceHelper.Get(StringKey.Downloading), ResourceHelper.Get(StringKey.Cancelling));
            downloadItemViewModel.ItemName = downloadItemViewModel.ItemName.Replace(ResourceHelper.Get(StringKey.Installing), ResourceHelper.Get(StringKey.Cancelling));

            if (downloadItemViewModel?.Download?.PerformCancel != null)
            {
                downloadItemViewModel.Download.PerformCancel?.Invoke(); // PerformCancel will happen during download and internally calls OnCancel
            }

            if (!isCancellingChild)
            {
                UpdatePauseDownloadButtonUI();
            }

            Sys.Message(new WMessage(downloadItemViewModel?.ItemName));
        }

        internal void DownloadMod(CatalogModItemViewModel catalogModItemViewModel)
        {
            Mod modToDownload = catalogModItemViewModel.Mod;
            ModStatus status = Sys.GetStatus(modToDownload.ID);

            if (status == ModStatus.Downloading)
            {
                MessageDialogWindow.Show($"{modToDownload.Name} {ResourceHelper.Get(StringKey.IsAlreadyDownloading)}", ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (status == ModStatus.Updating)
            {
                MessageDialogWindow.Show($"{modToDownload.Name} {ResourceHelper.Get(StringKey.IsAlreadyUpdating)}", ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (status == ModStatus.PendingInstall)
            {
                Sys.Library.AttemptInstalls();
                return;
            }

            if (status == ModStatus.Installed)
            {
                InstalledItem installedItem = Sys.Library.GetItem(modToDownload.ID);

                bool hasPatches = modToDownload.GetPatchesFromTo(installedItem.LatestInstalled.VersionDetails.Version, modToDownload.LatestVersion.Version).Any();

                // no update is available and no patches available so warn user that latest version is already installed
                if (installedItem != null && !installedItem.IsUpdateAvailable && !hasPatches)
                {
                    MessageDialogWindow.Show($"{modToDownload.Name} {ResourceHelper.Get(StringKey.IsAlreadyDownloadedAndInstalled)}", ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    // update available for installed mod
                    Install.DownloadAndInstall(modToDownload, isUpdatingMod: true);
                }
            }
            else if (status == ModStatus.NotInstalled)
            {
                Install.DownloadAndInstall(modToDownload, isUpdatingMod: false);
            }

            List<Mod> required = new List<Mod>();
            List<string> notFound = new List<string>();
            foreach (ModRequirement req in modToDownload.Requirements)
            {
                InstalledItem inst = Sys.Library.GetItem(req.ModID);

                if (inst != null)
                {
                    continue;
                }

                Mod rMod = Sys.GetModFromCatalog(req.ModID);

                if (rMod != null)
                    required.Add(rMod);
                else
                    notFound.Add(req.Description);
            }

            if (required.Any())
            {
                if (MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.ThisModAlsoRequiresYouDownloadTheFollowing), String.Join("\n", required.Select(m => m.Name))), ResourceHelper.Get(StringKey.Requirements), MessageBoxButton.YesNo, MessageBoxImage.Question).Result == MessageBoxResult.Yes)
                {
                    foreach (Mod rMod in required)
                    {
                        Install.DownloadAndInstall(rMod);
                    }
                }
            }

            if (notFound.Any())
            {
                MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.ThisModRequiresTheFollowingButCouldNotBeFoundInCatalog), String.Join("\n", notFound)), ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void Download(string link, DownloadItem downloadInfo)
        {
            Download(new List<string>() { link }, downloadInfo);
        }

        private CancellationTokenSource _megaDownloadCancelTokenSource;

        public void Download(IEnumerable<string> links, DownloadItem downloadInfo)
        {
            downloadInfo.HasStarted = true;

            Action onError = () =>
            {
                RemoveFromDownloadList(downloadInfo);
            };

            if (links?.Count() > 1)
            {
                onError = () =>
                {
                    string[] backupLinks = links.ToArray();

                    // determine if the next url is ExternalUrl and has an empty url
                    string backupLink = backupLinks[1];
                    LocationUtil.TryParse(backupLink, out LocationType backupType, out string backupUrl);

                    if (backupType == LocationType.ExternalUrl && string.IsNullOrWhiteSpace(backupUrl))
                    {
                        backupLinks[1] = backupLinks[0].Replace("iros://Url", "iros://ExternalUrl");
                    }

                    Sys.Message(new WMessage($"{downloadInfo.ItemName} - {ResourceHelper.Get(StringKey.SwitchingToBackupUrl)} {backupLinks[1]}"));
                    Download(backupLinks.Skip(1), downloadInfo);
                };
            }

            downloadInfo.OnError = onError;

            if (links == null || links?.Count() == 0)
            {
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.NoLinksFor)} {downloadInfo.ItemName}", true));
                downloadInfo.OnError?.Invoke();
                return;
            }

            string link = links.First();
            if (!LocationUtil.TryParse(link, out LocationType type, out string location))
            {
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.FailedToParseLinkFor)} {downloadInfo.ItemName}", true));
                downloadInfo.OnError?.Invoke();
                return;
            }

            try
            {
                switch (type)
                {
                    case LocationType.ExternalUrl:
                        MessageDialogViewModel dialogViewModel = null;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            dialogViewModel = MessageDialogWindow.Show(downloadInfo.ExternalUrlDownloadMessage, ResourceHelper.Get(StringKey.ExternalDownload), MessageBoxButton.YesNo, MessageBoxImage.Information);
                        });

                        if (dialogViewModel?.Result == MessageBoxResult.Yes)
                        {
                            Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.OpeningExternalUrlInBrowserFor)} {downloadInfo.ItemName} - {location}"));
                            ProcessStartInfo startInfo = new ProcessStartInfo(location);
                            Process.Start(startInfo);
                        }

                        // ensure the download is removed from queue and status is reverted so it doesn't think it is still downloading by calling OnCancel()
                        RemoveFromDownloadList(downloadInfo);
                        downloadInfo.OnCancel?.Invoke();
                        break;

                    case LocationType.Url:
                        FileDownloadTask fileDownload = new FileDownloadTask(location, downloadInfo.SaveFilePath, downloadInfo);

                        downloadInfo.PerformCancel = () =>
                        {
                            fileDownload.CancelAsync();
                            downloadInfo.OnCancel?.Invoke();
                        };

                        fileDownload.DownloadProgressChanged += WebRequest_DownloadProgressChanged;
                        fileDownload.DownloadFileCompleted += WebRequest_DownloadFileCompleted;

                        if (downloadInfo.IsModOrPatchDownload)
                        {
                            // try resuming partial downloads for mods
                            fileDownload.SetBytesWrittenFromExistingFile();
                        }

                        downloadInfo.FileDownloadTask = fileDownload;
                        fileDownload.downloadItem = downloadInfo;
                        fileDownload.Start();

                        break;

                    case LocationType.GDrive:
                        var gd = new GDrive();
                        downloadInfo.PerformCancel = () =>
                        {
                            gd.CancelAsync();
                            downloadInfo.OnCancel?.Invoke();
                        };

                        gd.DownloadProgressChanged += _wc_DownloadProgressChanged;
                        gd.FileDownloadProgressChanged += WebRequest_DownloadProgressChanged;
                        gd.DownloadFileCompleted += WebRequest_DownloadFileCompleted;

                        gd.Download(location, downloadInfo.SaveFilePath, downloadInfo);
                        break;

                    case LocationType.MegaSharedFolder:
                        string[] parts = location.Split(',');
                        bool wasCanceled = false;

                        downloadInfo.PerformCancel = () =>
                        {
                            wasCanceled = true;

                            try
                            {
                                _megaDownloadCancelTokenSource?.Cancel();
                            }
                            catch (Exception dex)
                            {
                                Logger.Error(dex);
                            }
                            finally
                            {
                                _megaDownloadCancelTokenSource?.Dispose();
                                _megaDownloadCancelTokenSource = null;
                            }

                            downloadInfo.OnCancel?.Invoke();
                            RemoveFromDownloadList(downloadInfo);
                        };

                        var client = new MegaApiClient();
                        client.LoginAnonymousAsync().ContinueWith((loginResult) =>
                        {
                            if (wasCanceled)
                            {
                                return; // don't continue after async login since user already canceled download
                            }

                            if (loginResult.IsFaulted)
                            {
                                Sys.Message(new WMessage($"Failed to login to mega - {loginResult.Exception.GetBaseException().Message}", WMessageLogLevel.Error, loginResult.Exception));
                                downloadInfo.OnError?.Invoke();
                                return;
                            }


                            // get nodes from mega folder
                            Uri folderLink = new Uri($"https://mega.nz/{parts[0]}");
                            IEnumerable<INode> nodes = client.GetNodesFromLink(folderLink);

                            // first look for node by Id
                            INode fileNode = nodes.Where(x => x.Type == NodeType.File && x.Id == parts[1]).FirstOrDefault();

                            // if not found check by name (exact match on file name)
                            if (fileNode == null)
                            {
                                fileNode = nodes.Where(x => x.Type == NodeType.File && x.Name == parts[2]).FirstOrDefault();
                            }

                            if (wasCanceled)
                            {
                                return; // don't continue after async login since user already canceled download
                            }

                            if (fileNode != null)
                            {
                                if (File.Exists(downloadInfo.SaveFilePath))
                                {
                                    File.Delete(downloadInfo.SaveFilePath); //delete old temp file if it exists (throws exception otherwise)
                                }

                                IProgress<double> progressHandler = new Progress<double>(x =>
                                {
                                    double estimatedBytesReceived = (double)fileNode.Size * (x / 100);
                                    UpdateDownloadProgress(downloadInfo, (int)x, (long)estimatedBytesReceived, fileNode.Size);
                                });



                                _megaDownloadCancelTokenSource = new CancellationTokenSource();
                                Task downloadTask = client.DownloadFileAsync(fileNode, downloadInfo.SaveFilePath, progressHandler, _megaDownloadCancelTokenSource.Token);

                                downloadTask.ContinueWith((downloadResult) =>
                                {
                                    _megaDownloadCancelTokenSource?.Dispose();
                                    _megaDownloadCancelTokenSource = null;
                                    client.LogoutAsync();

                                    if (downloadResult.IsCanceled)
                                    {
                                        return;
                                    }

                                    if (downloadResult.IsFaulted)
                                    {
                                        Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.ErrorDownloading)} {downloadInfo.ItemName}", WMessageLogLevel.StatusOnly, downloadResult.Exception));
                                        downloadInfo.OnError?.Invoke();
                                        return;
                                    }


                                    ProcessDownloadComplete(downloadInfo, new AsyncCompletedEventArgs(null, false, downloadInfo));
                                });

                            }
                            else
                            {
                                Sys.Message(new WMessage($"Failed to find mega node for {downloadInfo.ItemName}"));
                                downloadInfo.OnError?.Invoke();
                                client.LogoutAsync();
                            }
                        });
                        break;
                }
            }
            catch (Exception e)
            {
                string msg = $"{ResourceHelper.Get(StringKey.Error)} {downloadInfo.ItemName} - {e.Message}";
                Sys.Message(new WMessage(msg, WMessageLogLevel.Error, e));
                downloadInfo.OnError?.Invoke();
            }


        }

        private void WebRequest_DownloadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DownloadItem item = e.UserState as DownloadItem;
            FileDownloadTask download = item?.FileDownloadTask;

            if (item == null || download == null)
            {
                Logger.Warn("item or download is null.");
                return;
            }

            if (item.Category == DownloadCategory.Image && download.ContentLength > 3 * 1000000)
            {
                Logger.Warn("preview image greater than 3MB, cancelling download");
                item.PerformCancel?.Invoke();
                return;
            }

            UpdateDownloadProgress(item, e.ProgressPercentage, download.BytesWritten, download.ContentLength);
            UpdatePauseDownloadButtonUI();
        }

        public void AddToDownloadQueue(DownloadItem newDownload)
        {
            int downloadCount = 0;

            // translate item name if needed
            if (newDownload.ItemNameTranslationKey.HasValue)
            {
                newDownload.ItemName = newDownload.ItemName.Replace($"[{newDownload.ItemNameTranslationKey}]", ResourceHelper.Get(newDownload.ItemNameTranslationKey.Value));
            }

            // ensure you can cancel downloads that are pending download in queue
            if (newDownload.PerformCancel == null)
            {
                newDownload.PerformCancel = () =>
                {
                    RemoveFromDownloadList(newDownload);
                    newDownload.OnCancel?.Invoke();
                };
            }

            DownloadItemViewModel downloadViewModel = new DownloadItemViewModel(newDownload);

            App.Current.Dispatcher.Invoke(() =>
            {
                lock (_downloadLock)
                {
                    if (DownloadList.All(d => d.Download.UniqueId != newDownload.UniqueId))
                    {
                        DownloadList.Add(downloadViewModel);
                    }

                    downloadCount = DownloadList.Count;
                }
            });


            if (downloadCount == 1)
            {
                // only item in queue so start downloading right away
                downloadViewModel.DownloadSpeed = ResourceHelper.Get(StringKey.Starting);
                Download(newDownload.Links, newDownload);
            }
            else if (downloadCount > 1)
            {
                // allow images and catalogs to download while mod is downloading so it does not halt queue of catalog/image refreshes
                StartNextDownloadInQueue();
            }
        }

        internal void UpdateModDetails(Guid modID)
        {
            CatalogModItemViewModel foundMod = null;

            lock (_listLock)
            {
                foundMod = CatalogModList.Where(m => m.Mod.ID == modID).FirstOrDefault();
            }

            foundMod?.UpdateDetails();
        }

        void WebRequest_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DownloadItem item = (DownloadItem)e.UserState;
            CleanUpFileDownloadTask(item);

            if (e.Cancelled)
            {
                RemoveFromDownloadList(item);
            }
            else if (e.Error != null)
            {
                string msg = $"{ResourceHelper.Get(StringKey.Error)} {item.ItemName} - {e.Error.GetBaseException().Message}";
                Sys.Message(new WMessage(msg, WMessageLogLevel.Error, e.Error.GetBaseException()));
                item.OnError?.Invoke();
            }
            else
            {
                ProcessDownloadComplete(item, e);
            }
        }

        /// <summary>
        /// Removes event handlers and nullifys the <see cref="FileDownloadTask"/> in <paramref name="item"/>
        /// </summary>
        private void CleanUpFileDownloadTask(DownloadItem item)
        {
            if (item.FileDownloadTask != null)
            {
                // remove event handlers to prevent holding object in memory
                item.FileDownloadTask.DownloadProgressChanged -= WebRequest_DownloadProgressChanged;
                item.FileDownloadTask.DownloadFileCompleted -= WebRequest_DownloadFileCompleted;
                item.FileDownloadTask = null;
            }
        }

        private void RemoveFromDownloadList(DownloadItem item)
        {
            int downloadCount = 0;


            App.Current.Dispatcher.Invoke(() =>
            {
                lock (_downloadLock)
                {
                    if (DownloadList.Any(d => d.Download.UniqueId == item.UniqueId))
                    {
                        DownloadItemViewModel viewModel = DownloadList.FirstOrDefault(d => d.Download.UniqueId == item.UniqueId);
                        DownloadList.Remove(viewModel);
                    }

                    downloadCount = DownloadList.Count;
                }
            });

            if (downloadCount > 0)
            {
                StartNextDownloadInQueue();
            }
        }

        private void StartNextDownloadInQueue()
        {
            DownloadItemViewModel nextDownload = null;
            bool isDownloadingImageOrCat = false;
            bool isDownloadingMod = false;

            lock (_downloadLock)
            {
                isDownloadingMod = DownloadList.Any(d => d.Download.IsModOrPatchDownload && d.Download.HasStarted && !d.Download.IsFileDownloadPaused);
                isDownloadingImageOrCat = DownloadList.Any(d => !d.Download.IsModOrPatchDownload && d.Download.HasStarted);

                // loop over each download in queue to see which is allowed to be started next
                foreach (DownloadItemViewModel item in DownloadList)
                {
                    if (item.Download.HasStarted)
                    {
                        continue; // skip downloads that already started
                    }

                    if (item.Download.ParentUniqueID != null)
                    {
                        // ... check if this child download is allowed to start downloading if the parent download is finished
                        var parentDownload = DownloadList.Where(d => d.Download.UniqueId == item.Download.ParentUniqueID).FirstOrDefault();
                        if (parentDownload != null)
                        {
                            // skip download where parent download is still in download queue
                            continue;
                        }
                    }

                    if (!isDownloadingMod)
                    {
                        // no mod is currently downloading so get the next download in queue
                        nextDownload = item;
                        break;
                    }
                    else if (isDownloadingMod && !item.Download.IsModOrPatchDownload)
                    {
                        // a mod is downloading so get next catalog/image to download
                        nextDownload = item;
                        break;
                    }
                }
            }

            //start the next catalog/image download if no other catalogs/images are being downloaded
            if (!isDownloadingImageOrCat && nextDownload != null)
            {
                nextDownload.DownloadSpeed = ResourceHelper.Get(StringKey.Starting);
                Download(nextDownload.Download.Links, nextDownload.Download);
            }
        }

        /// <summary>
        /// Finds the next mod in download queue to start downloading
        /// </summary>
        private void StartNextModDownload()
        {
            int downloadCount = 0;

            lock (_downloadLock)
            {
                downloadCount = DownloadList.Count;
            }

            if (downloadCount > 0)
            {
                // start next download in queue
                DownloadItemViewModel nextDownload = null;

                lock (_downloadLock)
                {
                    // loop over each download in queue to see which is allowed to be started next
                    foreach (DownloadItemViewModel item in DownloadList)
                    {
                        if (item.Download.HasStarted)
                        {
                            continue; // skip downloads that already started
                        }

                        if (item.Download.Category == DownloadCategory.ModPatch && item.Download.ParentUniqueID != null)
                        {
                            // when a mod patch is queued for download it may have a 'parent' download in the queue which is the full mod to be installed
                            // ... here we check if this mod patch is allowed to start downloading if the parent download is finished
                            var parentDownload = DownloadList.Where(d => d.Download.UniqueId == item.Download.ParentUniqueID).FirstOrDefault();
                            if (parentDownload == null)
                            {
                                nextDownload = item;
                                break;
                            }
                        }
                        else if (item.Download.IsModOrPatchDownload)
                        {
                            nextDownload = item;
                            break;
                        }
                    }
                }

                if (nextDownload != null)
                {
                    nextDownload.DownloadSpeed = ResourceHelper.Get(StringKey.Starting);
                    Download(nextDownload.Download.Links, nextDownload.Download);
                }
            }
        }

        void _wc_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            DownloadItem item = (DownloadItem)e.UserState;

            long totalBytes = e.TotalBytesToReceive;

            if (item.Category == DownloadCategory.Image && e.TotalBytesToReceive > 3 * 1000000)
            {
                Logger.Warn("preview image greater than 3MB, cancelling download");
                item.PerformCancel?.Invoke();
                return;
            }

            int prog = e.ProgressPercentage;
            if ((e.TotalBytesToReceive < 0) && (sender is GDrive))
            {
                totalBytes = (sender as GDrive).GetContentLength();
                prog = (int)(100 * e.BytesReceived / totalBytes);
            }

            UpdateDownloadProgress(item, prog, e.BytesReceived, totalBytes);
        }

        private void CompleteIProc(DownloadItem item, AsyncCompletedEventArgs e)
        {
            item.IProc.DownloadComplete(e);
            RemoveFromDownloadList(item);
            UpdatePauseDownloadButtonUI();
        }

        private void ProcessDownloadComplete(DownloadItem item, AsyncCompletedEventArgs e)
        {
            // wire-up error action to also remove the item from the download list
            Action<Exception> existingErrorAction = item.IProc.Error;
            item.IProc.Error = ex =>
            {
                existingErrorAction(ex);

                // only remove download from list if not pending to install
                if (!Sys.Library.HasPendingInstall(item))
                {
                    RemoveFromDownloadList(item);
                }
                else
                {
                    DownloadItemViewModel failedItem = null;
                    lock (_downloadLock)
                    {
                        failedItem = DownloadList.FirstOrDefault(i => i.Download.UniqueId == item.UniqueId);
                    }

                    if (failedItem != null)
                    {
                        failedItem.DownloadSpeed = ResourceHelper.Get(StringKey.Pending);
                    }

                    NotifyPropertyChanged(nameof(RetryInstallIsEnabled)); // update UI to enable 'retry install' button if download is selected (the first download in the list is usually always selected)
                    StartNextDownloadInQueue(); // don't remove from list but let the next download happen
                }
            };

            item.IProc.Complete = () =>
            {
                CompleteIProc(item, e);
            };


            // update UI viewmodel to reflect installation status
            DownloadItemViewModel itemViewModel = null;
            lock (_downloadLock)
            {
                itemViewModel = DownloadList.FirstOrDefault(i => i.Download.UniqueId == item.UniqueId);
            }

            if (itemViewModel == null || itemViewModel.IsCancelling)
            {
                return; // don't process download and finish install if download item is marked as cancelling or doesn't exist in list anymore
            }

            if (item.IsModOrPatchDownload && itemViewModel != null)
            {
                itemViewModel.ItemName = item.ItemName.Replace(ResourceHelper.Get(StringKey.Downloading), ResourceHelper.Get(StringKey.Installing));
                itemViewModel.DownloadSpeed = "N/A";
                itemViewModel.RemainingTime = ResourceHelper.Get(StringKey.Unknown);
            }

            if (itemViewModel != null)
            {
                itemViewModel.PercentComplete = 0;
                item.IProc.SetPercentComplete = i =>
                {
                    itemViewModel.PercentComplete = i;
                };
            }

            item.IProc.Schedule();
        }

        private void UpdateDownloadProgress(DownloadItem item, int percentDone, long bytesReceived, long totalBytes)
        {
            DownloadItemViewModel viewModel = null;

            lock (_downloadLock)
            {
                viewModel = DownloadList.FirstOrDefault(d => d.Download.UniqueId == item.UniqueId);
            }

            if (viewModel == null)
            {
                return;
            }

            if (item.FileDownloadTask != null && item.FileDownloadTask.IsPaused)
            {
                return; // download is paused so don't update the below information since it will overwrite the 'Paused...' text
            }

            viewModel.PercentComplete = percentDone;

            TimeSpan interval = DateTime.Now - item.LastCalc;

            if ((interval.TotalSeconds >= 3))
            {
                if (bytesReceived > 0)
                {
                    double estimatedSpeed = (((bytesReceived - item.LastBytes) / 1024.0) / interval.TotalSeconds); // estimated speed in KB/s

                    if ((estimatedSpeed / 1024.0) > 1.0)
                    {
                        // show speed in MB/s
                        viewModel.DownloadSpeed = (estimatedSpeed / 1024.0).ToString("0.0") + "MB/s";
                    }
                    else
                    {
                        // show speed in KB/s
                        viewModel.DownloadSpeed = estimatedSpeed.ToString("0.0") + "KB/s";
                    }

                    double estimatedSecondsLeft = ((totalBytes - bytesReceived) / 1024.0) / estimatedSpeed; // divide bytes by 1024 to get total KB

                    if ((estimatedSecondsLeft / 60) > 60)
                    {
                        // show in hours
                        viewModel.RemainingTime = $"{(estimatedSecondsLeft / 60) / 60: 0.0} hours";
                    }
                    else if (estimatedSecondsLeft > 60)
                    {
                        // show in minutes
                        viewModel.RemainingTime = $"{estimatedSecondsLeft / 60: 0.0} min";
                    }
                    else
                    {
                        viewModel.RemainingTime = $"{estimatedSecondsLeft: 0.0} sec";
                    }

                    item.LastBytes = bytesReceived;
                }

                item.LastCalc = DateTime.Now;
            }
        }

        #endregion
    }
}
