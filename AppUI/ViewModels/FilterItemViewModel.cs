using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AppUI.ViewModels
{
    public enum FilterItemType
    {
        Category,
        Tag,
        ShowAll,
        Separator
    }

    public class FilterItemViewModel : ViewModelBase
    {
        private string _name;
        private bool _isChecked;
        private FilterItemType _filterType;

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

        public FilterItemType FilterType
        {
            get
            {
                return _filterType;
            }
            set
            {
                _filterType = value;
                NotifyPropertyChanged();
            }
        }

        public Action<bool> OnChecked { get; set; }

        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                _isChecked = value;
                NotifyPropertyChanged();
                OnChecked?.Invoke(_isChecked);
            }
        }

        public FilterItemViewModel(string name, FilterItemType filterType)
        {
            Name = name;
            IsChecked = false;
            FilterType = filterType;
        }

        /// <summary>
        /// Sets <see cref="_isChecked"/> without invoking <see cref="OnChecked"/>
        /// </summary>
        public void SetIsChecked(bool isChecked)
        {
            _isChecked = isChecked;
            NotifyPropertyChanged(nameof(IsChecked));
        }


        /// <summary>
        /// Returns true if <paramref name="mod"/> has a tag matching the given <paramref name="tags"/>.
        /// Returns true if <paramref name="tags"/> is empty.
        /// </summary>
        public static bool FilterByTags(Mod mod, IEnumerable<FilterItemViewModel> tags)
        {
            if (tags == null)
                tags = new List<FilterItemViewModel>();

            if (tags.Count() == 0)
                return true;

            // Check that the tag exists in the mods list of Tags or it matches the source catalog name (the catalog name is treated as a Tag)
            return tags.Any(t => t.Name.Equals(mod.SourceCatalogName, StringComparison.InvariantCultureIgnoreCase) ||  mod.Tags.Contains(t.Name, StringComparer.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Returns true if <paramref name="mod"/>.Category is found in <paramref name="categories"/>.
        /// Returns true if <paramref name="categories"/> is empty.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="categories"></param>
        /// <returns></returns>
        public static bool FilterByCategory(Mod mod, IEnumerable<FilterItemViewModel> categories)
        {
            if (categories == null)
                categories = new List<FilterItemViewModel>();

            string modCategory = mod?.Category;

            // treat missing mod category as 'Unknown'
            if (string.IsNullOrWhiteSpace(modCategory))
                modCategory = ResourceHelper.Get(StringKey.Unknown);

            return categories.Count() == 0 ||
                   categories.Any(c => ResourceHelper.ModCategoryTranslations[c.Name].Equals(modCategory, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
