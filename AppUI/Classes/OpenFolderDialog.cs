using System;
using System.Reflection;
using System.Windows.Forms;

/// <summary>
/// Dialog to Select a Folder which resembles the UI of the <see cref="OpenFileDialog"/>
/// </summary>
/// <remarks>
/// code found from: https://stackoverflow.com/questions/11767/browse-for-a-directory-in-c-sharp
/// </remarks>
class OpenFolderDialog
{
    //public bool AddExtension { get; set; }
    public bool AutoUpgradeEnabled { get; set; }
    //public bool CheckFileExists { get; set; }
    public bool CheckPathExists { get; set; }
    //public bool DereferenceLinks { get; set; }
    public string Title { get; set; }
    //public string DefaultExt { get; set; }
    public string InitialDirectory { get; set; }
    //public bool ValidateNames { get; set; }
    //public bool SupportMultiDottedExtensions { get; set; }
    //public bool ShowHelp { get; set; }
    public bool Multiselect { get; set; }
    public bool RestoreDirectory { get; set; }
    //public string Filter { get; set; }
    public string SelectedPath { get; private set; }
    public string[] SelectedPaths { get; private set; }

    private FolderBrowserDialog FolderBrowser
    {
        get
        {
            return new FolderBrowserDialog()
            {
                ShowNewFolderButton = true,
                Description = Title,
                SelectedPath = InitialDirectory
            };
        }
    }

    private OpenFileDialog Dialog
    {
        get
        {
            return new OpenFileDialog()
            {
                Title = Title,
                AddExtension = false,
                AutoUpgradeEnabled = AutoUpgradeEnabled,
                CheckFileExists = true,
                CheckPathExists = CheckPathExists,
                DefaultExt = string.Empty,
                DereferenceLinks = false,
                InitialDirectory = InitialDirectory,
                ValidateNames = false,
                SupportMultiDottedExtensions = false,
                ShowHelp = false,
                Multiselect = Multiselect,
                RestoreDirectory = RestoreDirectory,
                Filter = string.Empty,
                FileName = SelectedPath
            };
        }
    }

    /// <param name="hWndOwner">Handle of the control or window to be the parent of the file dialog</param>
    /// <returns>true if the user clicks OK</returns>
    public DialogResult ShowDialog(IntPtr hWndOwner)
    {
        FolderBrowserDialog XPDialog = FolderBrowser;
        DialogResult result = XPDialog.ShowDialog();
        SelectedPath = XPDialog.SelectedPath;
        return result;
    }
}