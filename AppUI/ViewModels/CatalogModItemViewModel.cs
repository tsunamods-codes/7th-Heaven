using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using System;

namespace AppUI.ViewModels
{

    public class CatalogModItemViewModel : ViewModelBase
    {
        private string _name;
        private string _author;
        private string _version;
        private string _downloadSize;
        private int _downloadSizeInBytes;
        private string _releaseDate;
        private string _category;
        private bool _isSelected;
        private Mod _mod;

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

        public string DownloadSize
        {
            get
            {
                return _downloadSize;
            }
            set
            {
                _downloadSize = value;
                NotifyPropertyChanged();
            }
        }

        public int DownloadSizeInBytes
        {
            get
            {
                return _downloadSizeInBytes;
            }
            set
            {
                _downloadSizeInBytes = value;
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

        public string Category
        {
            get
            {
                return ResourceHelper.Get(ModLoadOrder.GetCategoryKey(_category));
            }
            set
            {
                _category = value;
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

        public bool IsInstalled
        {
            get
            {
                return Sys.GetStatus(Mod.ID) == ModStatus.Installed;
            }
        }

        public Mod Mod
        {
            get
            {
                return _mod;
            }
            set
            {
                _mod = value;
                NotifyPropertyChanged();
            }
        }


        public CatalogModItemViewModel(Mod mod)
        {
            Mod = mod;
            Name = Mod.Name;
            Author = Mod.Author;
            Category = Mod.Category;
            Version = Mod.LatestVersion.Version.ToString();
            DownloadSizeInBytes = Mod.LatestVersion.DownloadSize;
            DownloadSize = GetDLSize(DownloadSizeInBytes);
            ReleaseDate = Mod.LatestVersion.ReleaseDate.ToString(Sys.Settings.DateTimeStringFormat);

            if (string.IsNullOrWhiteSpace(Category))
            {
                Category = ModCategory.Unknown.ToString();
            }

            NotifyPropertyChanged(nameof(IsInstalled));
        }

        private string GetDLSize(int size)
        {
            if (size <= 0)
            {
                return String.Empty;
            }
            else if (size < 100 * 1024)
            {
                return String.Format("{0:0.0}MiB", size / 1024m);
            }
            else if (size < 500 * 1024)
            {
                return String.Format("{0:0}MiB", size / 1024m);
            }
            else
            {
                return String.Format("{0:0.0}GiB", size / (1024m * 1024m));
            }
        }

        /// <summary>
        /// Gets latest <see cref="Mod"/> object from <see cref="Sys.Catalog"/> and updates properties
        /// </summary>
        public void UpdateDetails()
        {
            Mod = Sys.GetModFromCatalog(Mod.ID) ?? Mod;

            Name = Mod.Name;
            Author = Mod.Author;
            Category = Mod.Category;
            Version = Mod.LatestVersion.Version.ToString();
            DownloadSize = GetDLSize(Mod.LatestVersion.DownloadSize);
            ReleaseDate = Mod.LatestVersion.ReleaseDate.ToString(Sys.Settings.DateTimeStringFormat);

            if (string.IsNullOrWhiteSpace(Category))
            {
                Category = ModCategory.Unknown.ToString();
            }

            NotifyPropertyChanged(nameof(IsInstalled));
        }

        internal void RaiseNotifyPropertyChangedForCategory()
        {
            NotifyPropertyChanged(nameof(Category));
        }
    }
}
