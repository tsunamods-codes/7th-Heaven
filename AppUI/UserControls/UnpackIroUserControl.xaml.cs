using AppCore;
using AppUI.Classes;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppUI.UserControls
{
    /// <summary>
    /// Interaction logic for PackIroUserControl.xaml
    /// </summary>
    public partial class UnpackIroUserControl : UserControl
    {
        UnpackIroViewModel ViewModel { get; set; }

        public UnpackIroUserControl()
        {
            InitializeComponent();

            ViewModel = new UnpackIroViewModel();
            this.DataContext = ViewModel;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Validate())
            {
                ViewModel.UnpackIro();
            }
        }

        private void btnBrowseIro_Click(object sender, RoutedEventArgs e)
        {
            string iroFile = FileDialogHelper.BrowseForFile("*.iro|*.iro", ResourceHelper.Get(StringKey.SelectIroFile));

            if (!string.IsNullOrEmpty(iroFile))
            {
                ViewModel.PathToIroFile = iroFile;
            }
        }

        private void btnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            string outputFolder = FileDialogHelper.BrowseForFolder(ResourceHelper.Get(StringKey.SelectFolderToOutputUnpackedFiles));

            if (!string.IsNullOrEmpty(outputFolder))
            {
                ViewModel.PathToOutputFolder = outputFolder;
            }
        }
    }
}
