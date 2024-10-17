using AppCore;
using AppUI.Classes;
using AppUI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AppUI.UserControls
{
    /// <summary>
    /// Interaction logic for PackIroUserControl.xaml
    /// </summary>
    public partial class PatchIroUserControl : UserControl
    {
        PatchIroViewModel ViewModel { get; set; }

        public PatchIroUserControl()
        {
            InitializeComponent();

            ViewModel = new PatchIroViewModel(isAdvancedPatching: false);
            this.DataContext = ViewModel;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Validate())
            {
                ViewModel.PatchIro();
            }
        }

        private void btnBrowseNewIro_Click(object sender, RoutedEventArgs e)
        {
            string saveFile = FileDialogHelper.BrowseForFile("*.iro|*.iro", ResourceHelper.Get(StringKey.SelectIroFile));

            if (!string.IsNullOrEmpty(saveFile))
            {
                ViewModel.PathToNewIroFile = saveFile;
            }
        }

        private void btnBrowseOriginalIro_Click(object sender, RoutedEventArgs e)
        {
            string sourceFolder = FileDialogHelper.BrowseForFile("*.iro|*.iro", ResourceHelper.Get(StringKey.SelectIroFile));

            if (!string.IsNullOrEmpty(sourceFolder))
            {
                ViewModel.PathToOriginalIroFile = sourceFolder;
            }
        }

        private void btnBrowseIrop_Click(object sender, RoutedEventArgs e)
        {
            string saveFile = FileDialogHelper.OpenSaveDialog(".iro patch (*.irop)|*.irop", ResourceHelper.Get(StringKey.SaveAsIropTitle));

            if (!string.IsNullOrEmpty(saveFile))
            {
                ViewModel.PathToIropFile = saveFile;
            }
        }
    }
}
