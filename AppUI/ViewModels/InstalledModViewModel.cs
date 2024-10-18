using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace AppUI.ViewModels
{
    /// <summary>
    /// Represents a list item in the <see cref="MyModsViewModel"/> used to display
    /// installed and active mods in the 'My Mods' tab.
    /// </summary>
    public class InstalledModViewModel : ViewModelBase
    {
        public const int defaultSortOrder = 9999;

        private string _name;
        private string _author;
        private string _version;
        private string _category;
        private bool _isSelected;
        private ProfileItem _activeModInfo;
        private InstalledItem _installInfo;
        private string _releaseDate;
        private int _sortOrder;
        private List<string> _categoryList;
        private Thickness _borderThickness;

        public delegate void OnActivationChanged(InstalledModViewModel selected);
        public event OnActivationChanged ActivationChanged;

        public delegate void OnCategoryChanged(InstalledModViewModel selected);
        public event OnCategoryChanged CategoryChanged;


        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        public string Author
        {
            get
            {
                return _author;
            }
            set
            {
                _author = value;
                NotifyPropertyChanged();
            }
        }

        public string Version
        {
            get
            {
                return _version;
            }
            set
            {
                _version = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Returns the mod category, translated. 
        /// Setting Category will translate and store the category in english.
        /// </summary>
        public string Category
        {
            get
            {
                return ResourceHelper.Get(ModLoadOrder.GetCategoryKey(_category));
            }
            set
            {
                _category = ResourceHelper.ModCategoryTranslations[value];
                ActiveModInfo.Category = _category;
                NotifyPropertyChanged();
                CategoryChanged?.Invoke(this);
            }
        }

        public List<string> CategoryList
        {
            get
            {
                if (_categoryList == null)
                    _categoryList = ModLoadOrder.Orders.Keys.Select(s => ResourceHelper.Get(ModLoadOrder.GetCategoryKey(s))).OrderBy(s => s).ToList();

                return _categoryList;
            }
            set
            {
                _categoryList = value;
                NotifyPropertyChanged();
            }
        }


        public string ReleaseDate
        {
            get
            {
                return _releaseDate;
            }
            set
            {
                _releaseDate = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Returns info about the active mod and its setting 
        /// when the mod is active for the current profile.
        /// Will return null if installed mod is not active
        /// </summary>
        public ProfileItem ActiveModInfo
        {
            get
            {
                return _activeModInfo;
            }
            set
            {
                _activeModInfo = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsActive));
            }
        }

        public InstalledItem InstallInfo
        {
            get
            {
                return _installInfo;
            }
            set
            {
                _installInfo = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsActive
        {
            get
            {
                return ActiveModInfo != null && ActiveModInfo.IsModActive;
            }
            set
            {
                ActivationChanged?.Invoke(this);
            }
        }

        public int SortOrder
        {
            get
            {
                return _sortOrder;
            }
            set
            {
                _sortOrder = value;
                NotifyPropertyChanged();
            }
        }

        public Thickness BorderThickness
        {
            get
            {
                return _borderThickness;
            }
            set
            {
                _borderThickness = value;
                NotifyPropertyChanged();
            }
        }

        public bool HasReadMe
        {
            get
            {
                return InstallInfo.HasReadmeFile;
            }
        }

        public bool HasOptions
        {
            get
            {
                var info = InstallInfo.GetModInfo() ?? new AppWrapper.ModInfo();

                if (info.Options.Count == 0)
                    return false;

                return true;
            }
        }

        public bool IsUpdateAvailable
        {
            get
            {
                return InstallInfo.IsUpdateAvailable;
            }
        }

        public InstalledModViewModel(InstalledItem installedItem, ProfileItem profileItem)
        {
            InstallInfo = installedItem;
            ActiveModInfo = profileItem;

            Name = InstallInfo.CachedDetails.Name;
            Author = InstallInfo.CachedDetails.Author;
            Version = InstallInfo.CachedDetails.LatestVersion.Version.ToString();
            ReleaseDate = InstallInfo.CachedDetails.LatestVersion.ReleaseDate.ToString(Sys.Settings.DateTimeStringFormat);

            SortOrder = defaultSortOrder;

            // check active profile for custom category then default to library installed item
            _category = profileItem.Category;
            if (string.IsNullOrWhiteSpace(_category))
            {
                _category = InstallInfo.CachedDetails.Category;
            }

            if (string.IsNullOrWhiteSpace(_category))
            {
                Category = ResourceHelper.Get(StringKey.Unknown);
            }


            BorderThickness = new Thickness(0);
        }

        public override string ToString()
        {
            return $"{SortOrder}; {InstallInfo?.ModID} = {Name}";
        }

        public void RaiseIsActivePropertyChanged()
        {
            NotifyPropertyChanged(nameof(IsActive));
        }

        internal void RaiseNotifyPropertyChangedForCategory()
        {
            NotifyPropertyChanged(nameof(Category));
            CategoryList = null; // set to null will refresh translations since the list will be rebuilt
        }
    }
}
