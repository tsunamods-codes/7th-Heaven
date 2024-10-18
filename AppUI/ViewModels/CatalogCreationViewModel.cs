using AppCore;
using AppWrapper;
using Iros;
using Iros.Workshop;
using AppUI.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace AppUI.ViewModels
{

    public class CatalogCreationViewModel : ViewModelBase
    {
        private string _nameInput;
        private string _idInput;
        private string _authorInput;
        private string _descriptionInput;
        private string _categoryInput;
        private List<string> _categoryList;
        private string _versionInput;
        private string _metaVersionInput;
        private string _previewImageInput;
        private string _infoLinkInput;
        private string _donationLinkInput;
        private string _releaseNotesInput;
        private string _modOutput;
        private string _releaseDateInput;
        private ObservableCollection<CatalogModItemViewModel> _catalogModList;
        private CatalogModItemViewModel _selectedMod;

        private Mod _modToEdit;
        private ObservableCollection<DownloadLinkViewModel> _downloadLinkList;
        private string _tagsInput;
        private string _catalogNameInput;

        public string IDInput
        {
            get
            {
                return _idInput;
            }
            set
            {
                _idInput = value;
                NotifyPropertyChanged();
            }
        }

        public string NameInput
        {
            get
            {
                return _nameInput;
            }
            set
            {
                _nameInput = value;
                NotifyPropertyChanged();
            }
        }

        public string AuthorInput
        {
            get
            {
                return _authorInput;
            }
            set
            {
                _authorInput = value;
                NotifyPropertyChanged();
            }
        }

        public string DescriptionInput
        {
            get
            {
                return _descriptionInput;
            }
            set
            {
                _descriptionInput = value;
                NotifyPropertyChanged();
            }
        }

        public string CategoryInput
        {
            get
            {
                return _categoryInput;
            }
            set
            {
                _categoryInput = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> CategoryList
        {
            get
            {
                if (_categoryList == null)
                {
                    _categoryList = ModLoadOrder.Orders.Keys.Select(s => ResourceHelper.Get(ModLoadOrder.GetCategoryKey(s)))
                                                            .OrderBy(s => s)
                                                            .ToList();
                }

                return _categoryList;
            }
        }

        public string VersionInput
        {
            get
            {
                return _versionInput;
            }
            set
            {
                _versionInput = value;
                NotifyPropertyChanged();
            }
        }

        public string MetaVersionInput
        {
            get
            {
                return _metaVersionInput;
            }
            set
            {
                _metaVersionInput = value;
                NotifyPropertyChanged();
            }
        }

        public string PreviewImageInput
        {
            get
            {
                return _previewImageInput;
            }
            set
            {
                _previewImageInput = value;
                NotifyPropertyChanged();
            }
        }

        public string InfoLinkInput
        {
            get
            {
                return _infoLinkInput;
            }
            set
            {
                _infoLinkInput = value;
                NotifyPropertyChanged();
            }
        }

        public string DonationLinkInput
        {
            get
            {
                return _donationLinkInput;
            }
            set
            {
                _donationLinkInput = value;
                NotifyPropertyChanged();
            }
        }

        public string ReleaseNotesInput
        {
            get
            {
                return _releaseNotesInput;
            }
            set
            {
                _releaseNotesInput = value;
                NotifyPropertyChanged();
            }
        }

        public string ReleaseDateInput
        {
            get
            {
                return _releaseDateInput;
            }
            set
            {
                _releaseDateInput = value;
                NotifyPropertyChanged();
            }
        }

        public string TagsInput
        {
            get
            {
                return _tagsInput;
            }
            set
            {
                _tagsInput = value;
                NotifyPropertyChanged();
            }
        }

        public string CatalogNameInput
        {
            get
            {
                return _catalogNameInput;
            }
            set
            {
                _catalogNameInput = value;
                NotifyPropertyChanged();
            }
        }


        public ObservableCollection<DownloadLinkViewModel> DownloadLinkList
        {
            get
            {
                if (_downloadLinkList == null)
                {
                    _downloadLinkList = new ObservableCollection<DownloadLinkViewModel>();
                }

                return _downloadLinkList;
            }
            set
            {
                _downloadLinkList = value;
                NotifyPropertyChanged();
            }
        }

        public string CatalogOutput
        {
            get
            {
                return _modOutput;
            }
            set
            {
                _modOutput = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<CatalogModItemViewModel> CatalogModList
        {
            get
            {
                return _catalogModList;
            }
            set
            {
                _catalogModList = value;
                NotifyPropertyChanged();
            }
        }

        public CatalogModItemViewModel SelectedMod
        {
            get
            {
                return _selectedMod;
            }
            set
            {
                if (_modToEdit != null && _selectedMod != null)
                {
                    // save back to selected mod before switching
                    _selectedMod.Mod.Name = NameInput;
                    _selectedMod.Name = NameInput;

                    _selectedMod.Mod.Author = AuthorInput;
                    _selectedMod.Author = AuthorInput;

                    _selectedMod.Mod.Category = ResourceHelper.ModCategoryTranslations[CategoryInput];
                    _selectedMod.Category = ResourceHelper.ModCategoryTranslations[CategoryInput];

                    _selectedMod.Mod.Tags = TagsInput.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    _selectedMod.Mod.Description = DescriptionInput;
                    _selectedMod.Mod.Link = InfoLinkInput;
                    _selectedMod.Mod.DonationLink = DonationLinkInput;

                    if (Guid.TryParse(IDInput, out Guid parsedID))
                    {
                        _selectedMod.Mod.ID = parsedID;
                    }

                    DateTime.TryParse(ReleaseDateInput, out DateTime parsedDate);
                    decimal.TryParse(VersionInput, System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.CultureInfo(""), out decimal parsedVersion);

                    _selectedMod.Mod.LatestVersion = new ModVersion()
                    {
                        Links = DownloadLinkList.Where(d => !string.IsNullOrEmpty(d.SourceLinkInput)).Select(s => s.GetFormattedLink()).ToList(),
                        ReleaseDate = parsedDate,
                        Version = parsedVersion,
                        PreviewImage = PreviewImageInput,
                        ReleaseNotes = ReleaseNotesInput,
                        CompatibleGameVersions = GameVersions.All
                    };

                    decimal.TryParse(MetaVersionInput, System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.CultureInfo(""), out decimal parsedMetaVersion);
                    _selectedMod.Mod.MetaVersion = parsedMetaVersion;

                    _selectedMod.ReleaseDate = parsedDate.ToString(Sys.Settings.DateTimeStringFormat);
                }

                _selectedMod = value;

                if (_selectedMod != null)
                {
                    _modToEdit = _selectedMod.Mod;

                    NameInput = _modToEdit.Name;
                    AuthorInput = _modToEdit.Author;
                    IDInput = _modToEdit.ID.ToString();
                    CategoryInput = ResourceHelper.Get(ModLoadOrder.GetCategoryKey(_modToEdit.Category));
                    TagsInput = string.Join("\n", _modToEdit.Tags);
                    DescriptionInput = _modToEdit.Description;
                    InfoLinkInput = _modToEdit.Link;
                    DonationLinkInput = _modToEdit.DonationLink;
                    ReleaseNotesInput = _modToEdit.LatestVersion.ReleaseNotes;
                    PreviewImageInput = _modToEdit.LatestVersion.PreviewImage;
                    VersionInput = _modToEdit.LatestVersion.Version.ToString();
                    MetaVersionInput = _modToEdit.MetaVersion.ToString();
                    ReleaseDateInput = _modToEdit.LatestVersion.ReleaseDate.ToString(Sys.Settings.DateTimeStringFormat);

                    DownloadLinkList.Clear();
                    foreach (string link in _modToEdit.LatestVersion.Links)
                    {
                        if (LocationUtil.TryParse(link, out LocationType linkKind, out string url))
                        {
                            DownloadLinkList.Add(new DownloadLinkViewModel(linkKind.ToString(), url));
                        }
                    }

                    if (DownloadLinkList.Count == 0)
                    {
                        AddEmptyDownloadLink();
                    }

                }

                NotifyPropertyChanged();
            }
        }

        public CatalogCreationViewModel()
        {
            CatalogModList = new ObservableCollection<CatalogModItemViewModel>();
            VersionInput = "1.00";
            MetaVersionInput = "1.00";
            TagsInput = "";
            AddEmptyDownloadLink();
        }

        public string GenerateCatalogOutput()
        {
            Catalog generated = new Catalog()
            {
                Mods = CatalogModList.Select(m => m.Mod).ToList(),
                Name = CatalogNameInput
            };

            return Util.Serialize(generated).Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }

        internal void LoadModXml(string pathToModXml)
        {
            try
            {
                ModInfo mod = new ModInfo(pathToModXml, Sys._context);

                IDInput = mod.ID.ToString();
                NameInput = mod.Name;
                CategoryInput = ResourceHelper.Get(ModLoadOrder.GetCategoryKey(mod.Category));
                AuthorInput = mod.Author;
                DescriptionInput = mod.Description;
                VersionInput = mod.Version.ToString();
                PreviewImageInput = "";
                TagsInput = "";
                InfoLinkInput = mod.Link;
                DonationLinkInput = mod.DonationLink;
                ReleaseNotesInput = mod.ReleaseNotes;
                ReleaseDateInput = mod.ReleaseDate.ToString(Sys.Settings.DateTimeStringFormat);

                if (string.IsNullOrWhiteSpace(CategoryInput))
                {
                    CategoryInput = ResourceHelper.Get(StringKey.Unknown);
                }
                MetaVersionInput = "1.00";

                DownloadLinkList.Clear();
                AddEmptyDownloadLink();

                _modToEdit = null;
            }
            catch (Exception e)
            {
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.CouldNotLoadModXml)}: {e.Message}", true)
                {
                    LoggedException = e
                });
            }
        }

        internal void LoadModXmlFromIro(string pathToIroFile)
        {
            try
            {
                Mod parsedMod = new ModImporter().ParseModXmlFromSource(pathToIroFile);

                if (parsedMod == null)
                {
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.CouldNotLoadModXmlFromIroFile), true));
                    return;
                }

                IDInput = parsedMod.ID.ToString();
                NameInput = parsedMod.Name;
                CategoryInput = ResourceHelper.Get(ModLoadOrder.GetCategoryKey(parsedMod.Category));
                AuthorInput = parsedMod.Author;
                DescriptionInput = parsedMod.Description;
                VersionInput = parsedMod.LatestVersion.Version.ToString();
                MetaVersionInput = "1.00";
                PreviewImageInput = "";
                TagsInput = "";
                InfoLinkInput = parsedMod.Link;
                DonationLinkInput = parsedMod.DonationLink;
                ReleaseNotesInput = parsedMod.LatestVersion.ReleaseNotes;
                ReleaseDateInput = parsedMod.LatestVersion.ReleaseDate.ToString(Sys.Settings.DateTimeStringFormat);

                if (string.IsNullOrWhiteSpace(CategoryInput))
                {
                    CategoryInput = ResourceHelper.Get(StringKey.Unknown);
                }

                DownloadLinkList.Clear();
                AddEmptyDownloadLink();
                _modToEdit = null;
            }
            catch (Exception e)
            {
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.CouldNotLoadModXmlFromIroFile)}: {e.Message}", true)
                {
                    LoggedException = e
                });
            }
        }

        internal void SaveCatalogXml(string pathToFile)
        {
            try
            {
                File.WriteAllText(pathToFile, GenerateCatalogOutput());
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.SuccessfullySavedTo)} {pathToFile}", true));
            }
            catch (Exception e)
            {
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.FailedToSaveCatalogXml)}: {e.Message}", true)
                {
                    LoggedException = e
                });
            }
        }

        internal void LoadCatalogXml(string pathToFile)
        {
            try
            {
                Catalog catalog = Util.Deserialize<Catalog>(pathToFile);
                CatalogNameInput = catalog.Name;
                CatalogModList = new ObservableCollection<CatalogModItemViewModel>(catalog.Mods.Select(m => new CatalogModItemViewModel(m)));
                SelectedMod = null;
                ClearInputFields();
                CatalogOutput = "";
            }
            catch (Exception e)
            {
                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.CouldNotLoadCatalogXml)}: {e.Message}", true)
                {
                    LoggedException = e
                });
            }
        }

        private void ClearInputFields()
        {
            IDInput = Guid.NewGuid().ToString();
            NameInput = "";
            AuthorInput = "";
            DescriptionInput = "";
            CategoryInput = ResourceHelper.Get(StringKey.Unknown);
            TagsInput = "";
            VersionInput = "1.00";
            MetaVersionInput = "1.00";
            PreviewImageInput = "";
            InfoLinkInput = "";
            DonationLinkInput = "";
            ReleaseNotesInput = "";
            ReleaseDateInput = DateTime.Now.ToString(Sys.Settings.DateTimeStringFormat);
            DownloadLinkList.Clear();
            AddEmptyDownloadLink();
        }

        internal void AddModToList()
        {
            if (string.IsNullOrWhiteSpace(IDInput))
            {
                IDInput = Guid.NewGuid().ToString();
            }

            if (!Guid.TryParse(IDInput, out Guid parsedID))
            {
                return;
            }

            if (CatalogModList.Any(c => c.Mod.ID == parsedID))
            {
                return;
            }

            if (string.IsNullOrEmpty(VersionInput))
            {
                VersionInput = "1.00";
            }

            if (string.IsNullOrEmpty(MetaVersionInput))
            {
                MetaVersionInput = "1.00";
            }

            DateTime.TryParse(ReleaseDateInput, out DateTime parsedDate);
            decimal.TryParse(VersionInput, System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.CultureInfo(""), out decimal parsedVersion);
            decimal.TryParse(MetaVersionInput, System.Globalization.NumberStyles.AllowDecimalPoint, new System.Globalization.CultureInfo(""), out decimal parsedMetaVersion);

            if (string.IsNullOrEmpty(ReleaseDateInput) || parsedDate == DateTime.MinValue)
            {
                parsedDate = DateTime.Now;
            }

            if (parsedVersion <= 0)
            {
                parsedVersion = 1.0m;
            }

            if (parsedMetaVersion <= 0)
            {
                parsedMetaVersion = 1.0m;
            }

            Mod newMod = new Mod()
            {
                ID = parsedID,
                Name = NameInput,
                Author = AuthorInput,
                Category = ResourceHelper.ModCategoryTranslations[string.IsNullOrEmpty(CategoryInput) ? "Unknown" : CategoryInput],
                Tags = TagsInput.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                Description = DescriptionInput,
                Link = InfoLinkInput,
                DonationLink = DonationLinkInput,
                MetaVersion = parsedMetaVersion,
                LatestVersion = new ModVersion()
                {
                    Links = DownloadLinkList.Where(d => !string.IsNullOrEmpty(d.SourceLinkInput)).Select(d => d.GetFormattedLink()).ToList(),
                    ReleaseDate = parsedDate,
                    Version = parsedVersion,
                    CompatibleGameVersions = GameVersions.All,
                    PreviewImage = PreviewImageInput
                }
            };

            CatalogModList.Add(new CatalogModItemViewModel(newMod));

            _modToEdit = null;
            SelectedMod = CatalogModList.LastOrDefault();
        }

        internal void AddEmptyDownloadLink()
        {
            if (DownloadLinkList.Any(d => string.IsNullOrEmpty(d.SourceLinkInput)))
            {
                return; // don't add a new link since there is an empty one in the list that can be filled out
            }

            DownloadLinkList.Add(new DownloadLinkViewModel(LocationType.GDrive.ToString(), ""));
        }

        internal void RemoveSelectedMod()
        {
            if (SelectedMod == null)
            {
                return;
            }

            int listIndex = CatalogModList.IndexOf(SelectedMod);

            _modToEdit = null;
            CatalogModList.Remove(SelectedMod);
            SelectedMod = null;

            // ensure selection in list does not change after removing
            if (listIndex < CatalogModList.Count)
            {
                SelectedMod = CatalogModList[listIndex];
            }
        }
    }
}
