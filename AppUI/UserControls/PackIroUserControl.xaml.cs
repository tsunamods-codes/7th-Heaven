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
    public partial class PackIroUserControl : UserControl
    {
        PackIroViewModel ViewModel { get; set; }

        public PackIroUserControl()
        {
            InitializeComponent();

            ViewModel = new PackIroViewModel();
            this.DataContext = ViewModel;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Validate())
            {
                ViewModel.PackIro();
            }
        }

        private void btnBrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            string saveFile = FileDialogHelper.OpenSaveDialog("*.iro|*.iro", ResourceHelper.Get(StringKey.SaveAsIroTitle));

            if (!string.IsNullOrEmpty(saveFile))
            {
                ViewModel.PathToOutputFile = saveFile;
            }
        }

        private void btnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            string sourceFolder = FileDialogHelper.BrowseForFolder(ResourceHelper.Get(StringKey.SelectTheFolderThatContainsAllTheModFiles));

            if (!string.IsNullOrEmpty(sourceFolder))
            {
                ViewModel.PathToSourceFolder = sourceFolder;
            }
        }
    }
}
