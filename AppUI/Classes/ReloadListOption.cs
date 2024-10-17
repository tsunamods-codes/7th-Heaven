using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppUI.Classes
{
    public class ReloadListOption
    {
        public string SearchText { get; set; }

        public List<FilterItemViewModel> Categories { get; set; }

        public List<FilterItemViewModel> Tags { get; set; }

        public ReloadListOption()
        {
            SearchText = null;
            Categories = null;
            Tags = null;
        }

        /// <summary>
        /// Returns <see cref="Tags"/> if <paramref name="tags"/> is null; otherwise returns <paramref name="tags"/>.
        /// if <paramref name="tags"/> is not null then <see cref="Tags"/> will be set.
        /// </summary>
        public IEnumerable<FilterItemViewModel> SetOrGetPreviousTags(IEnumerable<FilterItemViewModel> tags)
        {
            if (tags == null && Tags != null)
            {
                tags = Tags;
            }
            else if (tags == null && Tags == null)
            {
                tags = new List<FilterItemViewModel>();
                Tags = tags.ToList();
            }
            else if (tags != null)
            {
                Tags = tags.ToList();
            }

            return tags;
        }

        /// <summary>
        /// Returns <see cref="SearchText"/> if <paramref name="searchText"/> is null/empty; otherwise returns <paramref name="searchText"/>.
        /// if <paramref name="searchText"/> is not null then <see cref="SearchText"/> will be set.
        /// </summary>
        public string SetOrGetPreviousSearchText(string searchText)
        {
            if (string.IsNullOrEmpty(searchText) && !string.IsNullOrEmpty(SearchText))
            {
                searchText = SearchText;
            }
            else if (string.IsNullOrEmpty(searchText) && string.IsNullOrEmpty(SearchText))
            {
                searchText = "";
                SearchText = "";
            }
            else if (!string.IsNullOrEmpty(searchText))
            {
                SearchText = searchText;
            }

            return searchText;
        }

        /// <summary>
        /// Returns <see cref="Categories"/> if <paramref name="categories"/> is null; otherwise returns <paramref name="categories"/>.
        /// if <paramref name="categories"/> is not null then <see cref="Categories"/> will be set.
        /// </summary>
        public IEnumerable<FilterItemViewModel> SetOrGetPreviousCategories(IEnumerable<FilterItemViewModel> categories)
        {
            if (categories == null && Categories != null)
            {
                categories = Categories;
            }
            else if (categories == null && Categories == null)
            {
                categories = new List<FilterItemViewModel>();
                Categories = categories.ToList();
            }
            else if (categories != null)
            {
                Categories = categories.ToList();
            }

            return categories;
        }
    }
}
