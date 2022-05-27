using SeventhHeavenUI.ViewModels;
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace SeventhHeaven.Classes
{
    /// <summary>
    /// Class to compare DateTime for sorting <see cref="CatalogModItemViewModel.ReleaseDate"/>
    /// </summary>
    /// <remarks>
    /// reference: https://stackoverflow.com/questions/4734055/c-sharp-icomparer-if-datetime-is-null-then-should-be-sorted-to-the-bottom-no
    /// </remarks>
    public class DateTimeComparer : IComparer
    {
        public ListSortDirection SortDirection = ListSortDirection.Ascending;

        public int Compare(DateTime? x, DateTime? y)
        {
            DateTime nx = x ?? DateTime.MinValue;
            DateTime ny = y ?? DateTime.MinValue;

            return nx.CompareTo(ny);
        }

        public int Compare(object x, object y)
        {
            CatalogModItemViewModel ax = (x as CatalogModItemViewModel);
            CatalogModItemViewModel ay = (y as CatalogModItemViewModel);

            DateTime.TryParseExact(ax?.ReleaseDate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dx);
            DateTime.TryParseExact(ay?.ReleaseDate, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dy);

            if (SortDirection == ListSortDirection.Ascending)
            {
                return Compare(dx, dy);
            }
            else
            {
                return Compare(dy, dx);
            }
        }
    }
}
