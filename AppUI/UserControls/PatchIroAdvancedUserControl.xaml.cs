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
    public partial class PatchIroAdvancedUserControl : UserControl
    {
        PatchIroViewModel ViewModel { get; set; }

        public PatchIroAdvancedUserControl()
        {
            InitializeComponent();

            ViewModel = new PatchIroViewModel(isAdvancedPatching: true);
            this.DataContext = ViewModel;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Validate())
            {
                ViewModel.PatchIroAdvanced();
            }
        }

        private void btnBrowseSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            string sourceFolder = FileDialogHelper.BrowseForFolder();

            if (!string.IsNullOrEmpty(sourceFolder))
            {
                ViewModel.PathToSourceFolder = sourceFolder;
            }
        }

        private void btnBrowseIrop_Click(object sender, RoutedEventArgs e)
        {
            string saveFile = FileDialogHelper.OpenSaveDialog(".iro patch (.irop)|*.irop", ResourceHelper.Get(StringKey.SaveAsIropTitle));

            if (!string.IsNullOrEmpty(saveFile))
            {
                ViewModel.PathToIropFile = saveFile;
            }
        }
    }
}
