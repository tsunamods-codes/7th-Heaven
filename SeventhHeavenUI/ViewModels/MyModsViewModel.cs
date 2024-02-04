using _7thHeaven.Code;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
using SeventhHeaven.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SeventhHeavenUI.ViewModels
{
    /// <summary>
    /// ViewModel to contain interaction logic for the 'My Mods' tab user control.
    /// </summary>
    public class MyModsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void OnSelectionChanged(object sender, InstalledModViewModel selected);
        public event OnSelectionChanged SelectedModChanged;

        public delegate void OnRefreshListRequested(bool beforeRefresh);
        public event OnRefreshListRequested RefreshListRequested;

        private object listLock = new object();
        
        /// <summary>
        /// Keep track of load order (row index) of mods
        /// </summary>
        Dictionary<Guid, int> modLoadOrders = new Dictionary<Guid, int>();

        private ObservableCollection<InstalledModViewModel> _modList;

        /// <summary>
        /// List of installed mods (includes active mods in the currently active profile)
        /// </summary>
        public ObservableCollection<InstalledModViewModel> ModList
        {
            get
            {
                // guarantee the property never returns null
                if (_modList == null)
                {
                    _modList = new ObservableCollection<InstalledModViewModel>();
                }

                return _modList;
            }
            set
            {
                _modList = value;
                NotifyPropertyChanged();
            }
        }

        internal ReloadListOption _previousReloadOptions;
        private BitmapImage _themeImage;

        public string PreviousSearchText
        {
            get => _previousReloadOptions.SearchText ?? "";
        }

        public bool HasPreviousCategoriesOrTags
        {
            get => _previousReloadOptions?.Categories?.Count > 0 || _previousReloadOptions?.Tags?.Count > 0;
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

        public MyModsViewModel()
        {
            _previousReloadOptions = new ReloadListOption();
        }

        /// <summary>
        /// Invokes the <see cref="SelectedModChanged"/> Event if not null.
        /// </summary>
        internal void RaiseSelectedModChanged(object sender, InstalledModViewModel selected)
        {
            SelectedModChanged?.Invoke(this, selected);
        }

        internal void ClearRememberedSearchTextAndCategories()
        {
            _previousReloadOptions = new ReloadListOption();
        }

        internal Task RefreshModListAsync()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                RefreshModList();
            });

            return t;
        }

        internal void RefreshModList()
        {
            RefreshListRequested?.Invoke(true);

            Sys.TryAutoImportMods();

            UpdateModCachedDetails();
            ScanForModUpdates();

            ClearRememberedSearchTextAndCategories();
            ReloadModListFromUIThread(GetSelectedMod()?.InstallInfo.ModID);

            RefreshListRequested?.Invoke(false);
        }

        /// <summary>
        /// Loop over installed mods and update Cached Details of mod by parsing mod.xml (if mod is installed in folder format, not .iro file)
        /// </summary>
        private void UpdateModCachedDetails()
        {
            foreach (InstalledItem item in Sys.Library.Items)
            {
                string absolutePath = Path.Combine(Sys.Settings.LibraryLocation, item.LatestInstalled?.InstalledLocation);
                if (Directory.Exists(absolutePath))
                {
                    string pathToXml = Path.Combine(absolutePath, "mod.xml");
                    
                    if (File.Exists(pathToXml))
                    {
                        Mod parsedModXml = new ModImporter().ParseModXmlFromSource(absolutePath, item.CachedDetails);
                        item.CachedDetails = parsedModXml;
                    }
                }
            }
        }

        /// <summary>
        /// Loads installed and active mods into <see cref="ModList"/> from <see cref="Sys.Library"/> and <see cref="Sys.ActiveProfile"/>
        /// </summary>
        internal void ReloadModList(Guid? modToSelect = null, string searchText = "", IEnumerable<FilterItemViewModel> categories = null, IEnumerable<FilterItemViewModel> tags = null)
        {
            // make sure to remove any deleted items and auto import mods first
            Sys.ActiveProfile.RemoveDeletedItems();

            // if there are no mods installed then just clear the list and return since no extra filtering work needs to be done
            if (Sys.Library.Items.Count == 0)
            {
                ClearModList();
                lock (listLock)
                {
                    ModList = new ObservableCollection<InstalledModViewModel>();
                }

                RaiseSelectedModChanged(this, null); // raise selected change event so mod preview info panel is cleared
                return;
            }

            categories = _previousReloadOptions.SetOrGetPreviousCategories(categories);
            searchText = _previousReloadOptions.SetOrGetPreviousSearchText(searchText);
            tags = _previousReloadOptions.SetOrGetPreviousTags(tags);

            List<InstalledModViewModel> allMods = new List<InstalledModViewModel>();

            foreach (ProfileItem item in Sys.ActiveProfile.Items)
            {
                InstalledItem mod = Sys.Library.GetItem(item.ModID);

                if (mod != null)
                {
                    mod.CachedDetails.Category = item.Category ?? mod.CachedDetails.Category; // ensure cached details match the active profile
                    bool includeMod = DoesModMatchSearchCriteria(searchText, categories, tags, mod.CachedDetails);

                    if (includeMod)
                    {
                        InstalledModViewModel activeMod = new InstalledModViewModel(mod, item);
                        activeMod.ActivationChanged += InstalledMod_ActivationChanged;
                        activeMod.CategoryChanged += InstalledMod_CategoryChanged;
                        allMods.Add(activeMod);
                    }
                }
            }

            foreach (InstalledItem item in Sys.Library.Items.ToList())
            {
                bool isAdded = allMods.Any(m => m.InstallInfo.ModID == item.ModID && m.InstallInfo.LatestInstalled.InstalledLocation == item.LatestInstalled.InstalledLocation);

                // ensure installed mod is included under the active profile if it is missing so you can toggle activation later
                ProfileItem profileItem = Sys.ActiveProfile.GetItem(item.ModID);

                if (profileItem == null)
                {
                    profileItem = new ProfileItem() { ModID = item.ModID, Name = item.CachedDetails.Name, Settings = new List<ProfileSetting>(), IsModActive = false };
                    Sys.ActiveProfile.AddItem(profileItem);
                }

                item.CachedDetails.Category = profileItem.Category ?? item.CachedDetails.Category;
                bool includeMod = DoesModMatchSearchCriteria(searchText, categories, tags, item.CachedDetails);


                if (!isAdded && includeMod)
                {
                    InstalledModViewModel installedMod = new InstalledModViewModel(item, profileItem);
                    installedMod.ActivationChanged += InstalledMod_ActivationChanged;
                    installedMod.CategoryChanged += InstalledMod_CategoryChanged;
                    allMods.Add(installedMod);
                }
            }

            if (modToSelect != null)
            {
                int index = allMods.FindIndex(m => m.InstallInfo.ModID == modToSelect);

                if (index >= 0)
                {
                    allMods[index].IsSelected = true;
                }
                else if (allMods.Count > 0)
                {
                    allMods[0].IsSelected = true;
                }
            }
            else
            {
                if (allMods.Count > 0)
                {
                    allMods[0].IsSelected = true;
                }
            }


            int installedModCount = Sys.Library.Items.Count;
            bool isFilteredBySearch = (allMods.Count != installedModCount);

            if (allMods.Count == 0)
            {
                if (isFilteredBySearch && !string.IsNullOrWhiteSpace(searchText))
                {
                    // when searching by text clear the list to visually show to the user no results were found
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.NoResultsFound), true));
                    ClearModList();
                }

                return;
            }


            if (!isFilteredBySearch)
            {
                bool storeLoadOrder = (allMods.Count == ModList.Count);
                SetPreviousLoadOrder(allMods, storeLoadOrder);
            }

            ClearModList();

            lock (listLock)
            {
                if (allMods.Any(m => m.SortOrder != InstalledModViewModel.defaultSortOrder))
                {
                    ModList = new ObservableCollection<InstalledModViewModel>(allMods.OrderBy(m => m.SortOrder));
                }
                else
                {
                    ModList = new ObservableCollection<InstalledModViewModel>(allMods);
                }
            }
        }

        /// <summary>
        /// Gets sort order for <see cref="ModList"/> and applies the same sort order to <paramref name="allMods"/> based on the Mod ID.
        /// </summary>
        private void SetPreviousLoadOrder(List<InstalledModViewModel> allMods, bool storeLoadOrder)
        {

            if (storeLoadOrder)
            {
                SetModLoadOrders();
            }

            if (modLoadOrders.Count > 0)
            {
                foreach (var mod in allMods)
                {
                    if (modLoadOrders.TryGetValue(mod.InstallInfo.ModID, out int expectedRowIdx))
                    {
                        mod.SortOrder = expectedRowIdx;
                    }
                }
            }
        }

        private void SetModLoadOrders()
        {
            modLoadOrders = new Dictionary<Guid, int>();
            int rowIdx = 0;
            lock (listLock)
            {
                foreach (var mod in ModList.ToList())
                {
                    if (!modLoadOrders.ContainsKey(mod.InstallInfo.ModID))
                    {
                        modLoadOrders.Add(mod.InstallInfo.ModID, rowIdx);
                    }

                    rowIdx++;
                }
            }
        }

        /// <summary>
        /// Invokes ReloadModList on the UI Thread (useful when having to modify collection from background thread)
        /// </summary>
        /// <param name="modToSelect"></param>
        /// <param name="searchText"></param>
        /// <param name="categories"></param>
        /// <param name="tags"></param>
        internal void ReloadModListFromUIThread(Guid? modToSelect = null, string searchText = "", IEnumerable<FilterItemViewModel> categories = null, IEnumerable<FilterItemViewModel> tags = null)
        {
            App.Current.Dispatcher.Invoke(() =>
            {   
                // Auto sort mods on launch if user has the option active
                if (Sys.Settings.HasOption(GeneralOptions.AutoSortMods))
                {
                    ReloadModList(modToSelect, "", categories, tags);
                    AutoSortBasedOnCategory();
                }
                else
                {
                    ReloadModList(modToSelect, searchText, categories, tags);
                }
            });
        }

        private static bool DoesModMatchSearchCriteria(string searchText, IEnumerable<FilterItemViewModel> categories, IEnumerable<FilterItemViewModel> tags, Mod mod)
        {
            bool isSearchTextRelevant = !string.IsNullOrWhiteSpace(searchText) && mod.SearchRelevance(searchText) > 0;

            if (categories.Count() > 0 && tags.Count() > 0)
            {
                return FilterItemViewModel.FilterByCategory(mod, categories) || FilterItemViewModel.FilterByTags(mod, tags) || isSearchTextRelevant;
            }
            else if (categories.Count() > 0)
            {
                return FilterItemViewModel.FilterByCategory(mod, categories) || isSearchTextRelevant;
            }
            else if (tags.Count() > 0)
            {
                return FilterItemViewModel.FilterByTags(mod, tags) || isSearchTextRelevant;
            }
            else
            {
                return isSearchTextRelevant || string.IsNullOrWhiteSpace(searchText); // returns all if search text is empty and no category/tags are checked
            }

        }

        internal void ClearModList()
        {
            lock (listLock)
            {
                foreach (var item in ModList)
                {
                    item.ActivationChanged -= InstalledMod_ActivationChanged;
                    item.CategoryChanged -= InstalledMod_CategoryChanged;
                }

                ModList.Clear();
            }

            modLoadOrders.Clear();
        }

        private void InstalledMod_ActivationChanged(InstalledModViewModel selected)
        {
            ToggleActivateMod(selected.InstallInfo.ModID, reloadList: false);
        }

        private void InstalledMod_CategoryChanged(InstalledModViewModel selected)
        {
            RefreshListRequested?.Invoke(beforeRefresh: false);
        }

        internal void ShowImportModWindow()
        {
            ImportModWindow modWindow = new ImportModWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            modWindow.ShowDialog();
            ReloadModList(GetSelectedMod()?.InstallInfo?.ModID);
        }

        /// <summary>
        /// Returns selected view model in <see cref="ModList"/>.
        /// </summary>
        public InstalledModViewModel GetSelectedMod()
        {
            InstalledModViewModel selected = null;

            lock (listLock)
            {
                selected = ModList.Where(m => m.IsSelected).LastOrDefault();

                // due to virtualization, IsSelected could be set on multiple items... 
                // ... so we will deselect the other items to avoid problems of multiple items being selected
                if (ModList.Where(m => m.IsSelected).Count() > 1)
                {
                    foreach (var mod in ModList.Where(m => m.IsSelected && m.InstallInfo.ModID != selected.InstallInfo.ModID))
                    {
                        mod.IsSelected = false;
                    }
                }
            }

            return selected;
        }

        internal void ToggleActivateMod(Guid modID, bool reloadList = true)
        {
            if (String.IsNullOrWhiteSpace(Sys.Settings.CurrentProfile)) return;

            ProfileItem item = Sys.ActiveProfile.Items.Find(i => i.ModID.Equals(modID));

            // item == null - activate mod for current profile
            // item != null - deactivate mod for current profile

            if (item == null || item?.IsModActive == false)
            {
                HashSet<Guid> examined = new HashSet<Guid>();
                List<InstalledItem> pulledIn = new List<InstalledItem>();
                List<ProfileItem> remove = new List<ProfileItem>();
                List<string> missing = new List<string>();
                List<InstalledItem> badVersion = new List<InstalledItem>();
                Stack<InstalledItem> toExamine = new Stack<InstalledItem>();

                // verify the mod in the profile still exists in the library.
                InstalledItem installedMod = Sys.Library.GetItem(modID);
                if (installedMod == null)
                {
                    return;
                }

                toExamine.Push(installedMod);

                while (toExamine.Any())
                {
                    examined.Add(toExamine.Peek().ModID);
                    var info = toExamine.Pop().GetModInfo();

                    if (info == null)
                    {
                        continue;
                    }


                    foreach (var req in info.Compatibility.Requires)
                    {
                        if (!examined.Contains(req.ModID))
                        {
                            var inst = Sys.Library.GetItem(req.ModID);
                            if (inst == null)
                                missing.Add(req.Description);
                            else if (req.Versions.Any() && !req.Versions.Contains(inst.Versions.Last().VersionDetails.Version))
                                badVersion.Add(inst);
                            else
                            {
                                if (Sys.ActiveProfile.ActiveItems.Find(pi => pi.ModID.Equals(req.ModID)) == null)
                                    pulledIn.Add(inst);
                                toExamine.Push(inst);
                            }
                        }
                    }

                    foreach (var forbid in info.Compatibility.Forbids)
                    {
                        if (!examined.Contains(forbid.ModID))
                        {
                            examined.Add(forbid.ModID);
                            var pItem = Sys.ActiveProfile.ActiveItems.Find(i => i.ModID.Equals(forbid.ModID));
                            if (pItem != null)
                            {
                                if (!forbid.Versions.Any() || forbid.Versions.Contains(Sys.Library.GetItem(pItem.ModID).LatestInstalled.VersionDetails.Version))
                                {
                                    remove.Add(pItem);
                                }
                            }
                        }
                    }
                }

                foreach (var active in Sys.ActiveProfile.ActiveItems.Except(remove))
                {
                    var info = Sys.Library.GetItem(active.ModID)?.GetModInfo();
                    if (info != null)
                    {
                        foreach (Guid mID in pulledIn.Select(ii => ii.ModID).Concat(new[] { modID }))
                        {
                            var forbid = info.Compatibility.Forbids.Find(f => f.ModID.Equals(mID));

                            if (forbid == null)
                            {
                                continue;
                            }

                            if (!forbid.Versions.Any() || forbid.Versions.Contains(Sys.Library.GetItem(mID).LatestInstalled.VersionDetails.Version))
                            {
                                if (mID.Equals(modID))
                                    MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.CannotActivateModBecauseItIsIncompatibleWithOtherMod), Sys.Library.GetItem(active.ModID).CachedDetails.Name), ResourceHelper.Get(StringKey.Warning));
                                else
                                    MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.CannotActivateModBecauseDependentIsIncompatible), Sys.Library.GetItem(mID).CachedDetails.Name, Sys.Library.GetItem(active.ModID).CachedDetails.Name), ResourceHelper.Get(StringKey.Warning));
                                return;
                            }
                        }
                    }
                }

                if (missing.Any())
                {
                    MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.ModRequiresModsIsMissingWarning), String.Join("\n", missing)), ResourceHelper.Get(StringKey.MissingRequirements));
                }
                if (badVersion.Any())
                {
                    MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.UnsupportedModVersionsWarning), String.Join("\n", badVersion.Select(ii => ii.CachedDetails.Name))), ResourceHelper.Get(StringKey.UnsupportedVersion));
                }
                if (pulledIn.Any())
                {
                    MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.ThisModRequiresYouActivateFollowingMods), String.Join("\n", pulledIn.Select(ii => ii.CachedDetails.Name))), ResourceHelper.Get(StringKey.MissingRequiredActiveMods));
                }
                if (remove.Any())
                {
                    MessageDialogWindow.Show(String.Format(ResourceHelper.Get(StringKey.ModRequiresYouDeactivateTheFollowingMods), String.Join("\n", remove.Select(pi => Sys.Library.GetItem(pi.ModID).CachedDetails.Name))), ResourceHelper.Get(StringKey.DeactivateModsWarning));
                }

                DoActivate(modID, reloadList);

                foreach (InstalledItem req in pulledIn)
                {
                    DoActivate(req.ModID, reloadList);
                    if (reloadList) 
                        Sys.Ping(req.ModID);
                }

                foreach (ProfileItem pi in remove)
                {
                    DoDeactivate(pi, reloadList);
                    if (reloadList) 
                        Sys.Ping(pi.ModID);
                }

                GameLauncher.SanityCheckSettings();
            }
            else
            {
                DoDeactivate(item, reloadList);
            }

            if (reloadList)
            {
                Sys.Ping(modID);
            }
        }

        private void DoDeactivate(ProfileItem item, bool reloadList = true)
        {
            item.IsModActive = false;
            ModList.FirstOrDefault(m => m.InstallInfo.ModID == item.ModID)?.RaiseIsActivePropertyChanged();

            if (reloadList)
            {
                ReloadModListFromUIThread(item.ModID);
            }
        }

        private void DoActivate(Guid modID, bool reloadList = true)
        {
            if (!MainWindowViewModel.CheckAllowedActivate(modID)) return;

            var mod = Sys.ActiveProfile.Items.FirstOrDefault(m => m.ModID == modID);

            if (mod != null)
            {
                // already exists in list so just mark as active
                mod.IsModActive = true;
            }
            else
            {
                var installedItem = Sys.Library.GetItem(modID);

                var item = new ProfileItem() { ModID = modID, Name = installedItem?.CachedDetails.Name, Settings = new List<ProfileSetting>(), IsModActive = true };
                Sys.ActiveProfile.AddItem(item);
            }

            ModList.FirstOrDefault(m => m.InstallInfo.ModID == modID)?.RaiseIsActivePropertyChanged();

            if (reloadList)
            {
                ReloadModListFromUIThread(modID);
            }
        }

        internal void DeactivateAllActiveMods()
        {
            foreach (InstalledModViewModel installedMod in ModList.Where(m => m.IsActive).ToList())
            {
                ToggleActivateMod(installedMod.ActiveModInfo.ModID, reloadList: false); // reload list at the end
            }

            ReloadModList();
        }

        internal void ActivateAllMods()
        {
            foreach (InstalledModViewModel installedMod in ModList.Where(m => !m.IsActive).ToList())
            {
                ToggleActivateMod(installedMod.InstallInfo.ModID, reloadList: false); // reload list at the end
            }

            ReloadModList();
        }

        internal void UninstallMod(InstalledModViewModel installedModViewModel)
        {
            InstalledItem installed = Sys.Library.GetItem(installedModViewModel.InstallInfo.ModID);

            if (installed == null)
            {
                Logger.Warn("could not get InstalledItem for mod.");
                return;
            }

            Sys.Library.DeleteAndRemoveInstall(installed);
            Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.Uninstalled)} {installed.CachedDetails.Name}"));
            ReloadModList();
        }

        /// <summary>
        /// Change the sort order of an active mod
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="change"></param>
        public void ReorderProfileItem(InstalledModViewModel mod, int change)
        {
            // validate mod has not been removed from filesystem
            if (!mod.InstallInfo.ModExistsOnFileSystem())
            {
                Sys.ValidateAndRemoveDeletedMods();
                Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.CanNotReOrderModItHasBeenRemoved), mod.Name), true));
                ReloadModListFromUIThread();
                return;
            }

            int index = ModList.IndexOf(mod);

            int newindex = index + change;

            if (newindex < 0 || newindex >= ModList.Count)
                return;

            App.Current.Dispatcher.Invoke(() =>
            {
                // ensure to modify observable collection on UI thread 
                lock (listLock)
                {
                    ModList.RemoveAt(index);
                    ModList.Insert(newindex, mod);
                }

                Sys.ActiveProfile.Items = ModList.Select(m => m.ActiveModInfo).ToList();

                ReloadModList(mod.InstallInfo.CachedDetails.ID);
            });
        }

        public void SendModToBottom(InstalledModViewModel mod)
        {
            // validate mod has not been removed from filesystem
            if (!mod.InstallInfo.ModExistsOnFileSystem())
            {
                Sys.ValidateAndRemoveDeletedMods();
                Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.CanNotReOrderModItHasBeenRemoved), mod.Name), true));
                ReloadModListFromUIThread();
                return;
            }

            int index = ModList.IndexOf(mod);

            App.Current.Dispatcher.Invoke(() =>
            {
                // ensure to modify observable collection on UI thread 
                lock (listLock)
                {
                    ModList.RemoveAt(index);
                    ModList.Add(mod);
                }

                Sys.ActiveProfile.Items = ModList.Select(m => m.ActiveModInfo).ToList();

                ReloadModList(mod.InstallInfo.CachedDetails.ID);
            });
        }

        public void SendModToTop(InstalledModViewModel mod)
        {
            // validate mod has not been removed from filesystem
            if (!mod.InstallInfo.ModExistsOnFileSystem())
            {
                Sys.ValidateAndRemoveDeletedMods();
                Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.CanNotReOrderModItHasBeenRemoved), mod.Name), true));
                ReloadModListFromUIThread();
                return;
            }

            int index = ModList.IndexOf(mod);

            App.Current.Dispatcher.Invoke(() =>
            {
                // ensure to modify observable collection on UI thread 
                lock (listLock)
                {
                    ModList.RemoveAt(index);
                    ModList.Insert(0, mod);
                }

                Sys.ActiveProfile.Items = ModList.Select(m => m.ActiveModInfo).ToList();

                ReloadModList(mod.InstallInfo.CachedDetails.ID);
            });
        }

        /// <summary>
        /// Opens Configure Mod Window to make changes to mod settings.
        /// </summary>
        /// <param name="modToConfigure"></param>
        internal void ShowConfigureModWindow(InstalledModViewModel modToConfigure)
        {
            // validate mod has not been removed from filesystem
            if (!modToConfigure.InstallInfo.ModExistsOnFileSystem())
            {
                Sys.ValidateAndRemoveDeletedMods();
                Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.CanNotConfigureModItHasBeenRemoved), modToConfigure.Name), true));
                ReloadModListFromUIThread();
                return;
            }

            _7thWrapperLib.ModInfo info = modToConfigure.InstallInfo.GetModInfo();
            if (info == null)
            {
                Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.CanNotConfigureModFailedToReadModXml), modToConfigure.Name), true));
                return;
            }

            InstalledVersion installed = Sys.Library.GetItem(modToConfigure.InstallInfo.ModID)?.LatestInstalled;
            Func<string, string> imageReader;
            Func<string, Stream> audioReader;

            string config_temp_folder = Path.Combine(Sys.PathToTempFolder, "configmod", modToConfigure.InstallInfo.CachedDetails.ID.ToString());
            string pathToModXml = Path.Combine(Sys.Settings.LibraryLocation, installed.InstalledLocation);

            // wire up the imageReader and audioReader to extract/read files from the .iro archive
            IDisposable arcToDispose = null;
            if (pathToModXml.EndsWith(".iro", StringComparison.InvariantCultureIgnoreCase))
            {
                var arc = new _7thWrapperLib.IrosArc(pathToModXml);

                imageReader = s =>
                {
                    if (arc.HasFile(s))
                    {
                        string tempImgPath = Path.Combine(config_temp_folder, s);

                        Directory.CreateDirectory(config_temp_folder);

                        string fullImgDirectoryPath = Path.GetDirectoryName(tempImgPath);

                        if (!Directory.Exists(fullImgDirectoryPath))
                        {
                            Directory.CreateDirectory(fullImgDirectoryPath);
                        }

                        if (File.Exists(tempImgPath))
                        {
                            return tempImgPath;
                        }

                        // extract img from IRO arc to temp img location
                        using (Stream imgStream = arc.GetData(s))
                        {
                            try
                            {
                                using (FileStream fileStream = new FileStream(tempImgPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    fileStream.Seek(0, SeekOrigin.Begin);
                                    imgStream.CopyTo(fileStream);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Warn(e);
                                Logger.Warn("Failed to write image to file");
                                return null;
                            }
                        }

                        return tempImgPath;
                    }

                    return null;
                };

                audioReader = s =>
                {
                    if (arc.HasFile(s))
                        arc.GetData(s);
                    return null;
                };

                arcToDispose = arc;
            }
            else
            {
                // if mod is in a folder than wire up imageReader and audioReader to return path to file and audio file stream

                imageReader = s =>
                {
                    string ifile = Path.Combine(pathToModXml, s);
                    if (File.Exists(ifile))
                    {
                        return ifile;
                    }

                    return null;
                };

                audioReader = s =>
                {
                    string ifile = Path.Combine(pathToModXml, s);
                    if (File.Exists(ifile))
                        return new FileStream(ifile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return null;
                };
            }

            using (arcToDispose)
            {
                info = info ?? new _7thWrapperLib.ModInfo();
                if (info.Options.Count == 0)
                {
                    MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ThereAreNoOptionsToConfigureForThisMod), ResourceHelper.Get(StringKey.NoOptions), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }


                List<Constraint> modConstraints = GameLauncher.GetConstraints().Where(c => c.ModID.Equals(modToConfigure.InstallInfo.ModID)).ToList();

                ConfigureModWindow modWindow = new ConfigureModWindow();
                modWindow.ViewModel.Init(info, imageReader, audioReader, modToConfigure.ActiveModInfo, modConstraints, pathToModXml);

                // Open dialog for configuring settings - if true is returned then the settings are saved
                bool? dialogResult = modWindow.ShowDialog();
                if (dialogResult.GetValueOrDefault(false) == true)
                {
                    modToConfigure.ActiveModInfo.Settings = modWindow.ViewModel.GetSettings();

                    GameLauncher.SanityCheckSettings();
                }

                modWindow.ViewModel.CleanUp();
                modWindow.ViewModel = null;
            }
        }

        /// <summary>
        /// This will sort <see cref="ModList"/> by categories based on <see cref="ModLoadOrder"/>.
        /// Mod order constraints in mod.xml is taken into account and takes precedence over the cateogry load sort
        /// </summary>
        public void AutoSortBasedOnCategory()
        {
            List<InstalledModViewModel> sortedList = ModList.OrderBy(s => ModLoadOrder.Get(ResourceHelper.ModCategoryTranslations[s.Category]))
                                                            .ThenBy(m => m.Name)
                                                            .ToList();

            // loop over and check mod info for ordering of before/after other mods
            foreach (var installedMod in sortedList.ToList())
            {
                _7thWrapperLib.ModInfo info = installedMod.InstallInfo.GetModInfo();

                if (info == null)
                {
                    continue;
                }

                foreach (Guid after in info.OrderAfter)
                {
                    // always find current mod IDs so we don't move wrong mod due to movement
                    int modAfter = sortedList.FindIndex(m => m.InstallInfo.ModID.Equals(after));

                    if (modAfter == -1)
                    {
                        // mod could not be found in list
                        continue;
                    }

                    // always find current mod IDs so we don't move wrong mod due to movement
                    int modtoMove = sortedList.FindIndex(m => m.InstallInfo.ModID.Equals(info.ID));

                    if (modAfter > modtoMove)
                    {
                        // move mod to come after this guid
                        sortedList.RemoveAt(modtoMove);
                        sortedList.Insert(modAfter, installedMod);
                    }
                }

                foreach (Guid before in info.OrderBefore)
                {
                    // always find current mod IDs so we don't move wrong mod due to movement
                    int modBefore = sortedList.FindIndex(m => m.InstallInfo.ModID.Equals(before));               

                    if (modBefore == -1)
                    {
                        // mod to go before could not be found in list
                        continue;
                    }

                    // always find current mod IDs so we don't move wrong mod due to movement
                    int modtoMove = sortedList.FindIndex(m => m.InstallInfo.ModID.Equals(info.ID));

                    if (modBefore < modtoMove)
                    {
                        // move mod to come before this guid
                        sortedList.RemoveAt(modtoMove);
                        sortedList.Insert(modBefore, installedMod);
                    }
                }
            }

            ClearModList();

            lock (listLock)
            {
                ModList = new ObservableCollection<InstalledModViewModel>(sortedList);
            }

            // update active profile with new sort order
            Sys.ActiveProfile.Items = ModList.Select(m => m.ActiveModInfo).ToList();

            ReloadModList(GetSelectedMod()?.InstallInfo?.ModID);
        }

        internal void ScanForModUpdates()
        {
            foreach (InstalledItem inst in Sys.Library.Items.ToList())
            {
                if (inst.IsUpdateAvailable)
                {
                    Mod cat = Sys.GetModFromCatalog(inst.ModID);

                    switch (inst.UpdateType)
                    {
                        case UpdateType.Notify:
                            Sys.Message(new WMessage(string.Format(ResourceHelper.Get(StringKey.NewVersionOfModAvailable), cat.Name)));
                            Sys.PingInfoChange(inst.ModID);
                            break;

                        case UpdateType.Install:
                            if (Sys.GetStatus(cat.ID) != ModStatus.Downloading && Sys.GetStatus(cat.ID) != ModStatus.Updating)
                            {
                                Install.DownloadAndInstall(cat, true);
                            }
                            break;
                    }
                }
            }
        }
    }
}
