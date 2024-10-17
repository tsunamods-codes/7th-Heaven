using AppCore;
using AppUI.Classes;
using AppUI.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace AppUI.UserControls
{
    /// <summary>
    /// Interaction logic for CreateCatalogUserControl.xaml
    /// </summary>
    public partial class CreateCatalogUserControl : UserControl
    {
        CatalogCreationViewModel ViewModel { get; set; }

        public CreateCatalogUserControl()
        {
            InitializeComponent();

            ViewModel = new CatalogCreationViewModel();
            this.DataContext = ViewModel;
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CatalogOutput = ViewModel.GenerateCatalogOutput();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string pathToFile = FileDialogHelper.OpenSaveDialog("catalog .xml|*.xml", ResourceHelper.Get(StringKey.SaveCatalogXml));

            if (!string.IsNullOrEmpty(pathToFile))
            {
                ViewModel.SaveCatalogXml(pathToFile);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            string pathToFile = FileDialogHelper.BrowseForFile("catalog .xml|*.xml", ResourceHelper.Get(StringKey.SelectCatalogXmlToLoad));

            if (!string.IsNullOrEmpty(pathToFile))
            {
                ViewModel.LoadCatalogXml(pathToFile);
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddModToList();

            if (ViewModel.SelectedMod != null)
            { 
                lstMods.ScrollIntoView(ViewModel.SelectedMod);
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            string pathToFile = FileDialogHelper.BrowseForFile("mod .xml|*.xml|iro file (*.iro)|*.iro", ResourceHelper.Get(StringKey.SelectModXmlOrIroFileToLoad));

            if (!string.IsNullOrEmpty(pathToFile))
            {
                FileInfo info = new FileInfo(pathToFile);

                if (info.Extension == ".xml")
                {
                    ViewModel.LoadModXml(pathToFile);
                }
                else
                {
                    ViewModel.LoadModXmlFromIro(pathToFile);
                }
            }
        }

        private void btnAddLink_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddEmptyDownloadLink();
            scrollInputs.ScrollToBottom();
        }

        private void menuDeleteMod_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveSelectedMod();
        }
    }
}
