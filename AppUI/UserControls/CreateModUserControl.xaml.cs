using AppCore;
using AppUI.Classes;
using AppUI.Windows;
using AppUI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AppUI.UserControls
{
    /// <summary>
    /// Interaction logic for CreateModUserControl.xaml
    /// </summary>
    public partial class CreateModUserControl : UserControl
    {
        ModCreationViewModel ViewModel { get; set; }
        public CreateModUserControl()
        {
            InitializeComponent();

            ViewModel = new ModCreationViewModel();
            this.DataContext = ViewModel;
        }

        private void btnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            string pathToFile = FileDialogHelper.BrowseForFile("*.png,*.jpg,*.jpeg|*.png;*.jpg;*.jpeg", ResourceHelper.Get(StringKey.SelectImageToUse));


            if (!string.IsNullOrEmpty(pathToFile))
            {
                ViewModel.PreviewImageInput = System.IO.Path.GetFileName(pathToFile);
                MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.MakeSureToCopyToTheRootFolder), ViewModel.PreviewImageInput), ResourceHelper.Get(StringKey.Info), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.GenerateModOutput();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string pathToFile = FileDialogHelper.OpenSaveDialog("mod .xml|*.xml", ResourceHelper.Get(StringKey.SaveModXml));

            if (!string.IsNullOrEmpty(pathToFile))
            {
                ViewModel.SaveModXml(pathToFile);
            }
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            string pathToFile = FileDialogHelper.BrowseForFile("mod .xml|*.xml", ResourceHelper.Get(StringKey.SelectModXmlToLoad));
        
            if (!string.IsNullOrEmpty(pathToFile))
            {
                ViewModel.LoadModXml(pathToFile);
            }
        }
    }
}
