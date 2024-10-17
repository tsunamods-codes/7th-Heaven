using System.Collections.Generic;
using System.Linq;

namespace AppCore
{
    public class ColumnSettings
    {
        public string SortColumn { get; set; }
        public int SortDirection { get; set; }
        public List<ColumnInfo> MyModsColumns { get; set; }
        public List<ColumnInfo> BrowseCatalogColumns { get; set; }

        public ColumnSettings()
        {
            MyModsColumns = new List<ColumnInfo>();
            BrowseCatalogColumns = new List<ColumnInfo>();
        }

        public static ColumnSettings GetDefaultSettings()
        {
            ColumnSettings defaults = new ColumnSettings();

            defaults.MyModsColumns.Add(new ColumnInfo("Name", 0, true));
            defaults.MyModsColumns.Add(new ColumnInfo("Author", 0, true));
            defaults.MyModsColumns.Add(new ColumnInfo("Category", 140));
            defaults.MyModsColumns.Add(new ColumnInfo("Active", 80));
            defaults.MyModsColumns.Add(new ColumnInfo("Miscellaneous", 300));

            defaults.BrowseCatalogColumns.Add(new ColumnInfo("Name", 0, true));
            defaults.BrowseCatalogColumns.Add(new ColumnInfo("Author", 0, true));
            defaults.BrowseCatalogColumns.Add(new ColumnInfo("Released", 90));
            defaults.BrowseCatalogColumns.Add(new ColumnInfo("Category", 100));
            defaults.BrowseCatalogColumns.Add(new ColumnInfo("Size", 60));
            defaults.BrowseCatalogColumns.Add(new ColumnInfo("Inst.", 40));

            return defaults;
        }
    }

    public class ColumnInfo
    {
        public string Name { get; set; }
        public double Width { get; set; }
        /// <summary>
        /// When set to true, the Width property is ignored and the column width will be calculated at run-time to auto size into remaining space
        /// </summary>
        public bool AutoResize { get; set; }

        public ColumnInfo()
        {
            Name = "";
            Width = 0;
            AutoResize = false;
        }

        public ColumnInfo(string name, double width, bool resize = false)
        {
            Name = name;
            Width = width;
            AutoResize = resize;
        }
    }
}
