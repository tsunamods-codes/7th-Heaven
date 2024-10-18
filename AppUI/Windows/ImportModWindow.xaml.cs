using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using AppUI.ViewModels;
using AppUI;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for ImportModWindow.xaml
    /// </summary>
    public partial class ImportModWindow : Window
    {
        ImportModViewModel ViewModel { get; set; }

        public ImportModWindow()
        {
            InitializeComponent();

            ViewModel = new ImportModViewModel();
            this.DataContext = ViewModel;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Task<bool> t = ViewModel.ImportModFromWindowAsync();
            this.Cursor = System.Windows.Input.Cursors.Wait;
            App.ForceUpdateUI();

            t.ContinueWith((taskResult) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                }); 
                
                if (taskResult.IsFaulted)
                {
                    Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.FailedToImportModTheErrorHasBeenLogged)} {taskResult.Exception.GetBaseException()?.Message}", true) { LoggedException = taskResult.Exception.GetBaseException() });
                    return;
                }

                // close window on succesful import
                if (taskResult.Result)
                {
                    CloseWindow();
                }

                ViewModel.ProgressValue = 0;
            });
        }

        private void CloseWindow()
        {
            App.Current.Dispatcher.Invoke(() => 
            { 
                this.Close();
            });
        }

        private void btnBrowseIro_Click(object sender, RoutedEventArgs e)
        {
            string selectedFile = FileDialogHelper.BrowseForFile("*.iro *.irop|*.iro;*.irop", ResourceHelper.Get(StringKey.SelectIroFile));

            if (!string.IsNullOrEmpty(selectedFile))
            {
                ViewModel.PathToIroArchiveInput = selectedFile;
            }
        }

        private void btnBrowseModFolder_Click(object sender, RoutedEventArgs e)
        {
            string selectedFolder = FileDialogHelper.BrowseForFolder();

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                ViewModel.PathToModFolderInput = selectedFolder;
            }
        }

        private void btnBrowseBatchFolder_Click(object sender, RoutedEventArgs e)
        {
            string selectedFolder = FileDialogHelper.BrowseForFolder();

            if (!string.IsNullOrEmpty(selectedFolder))
            {
                ViewModel.PathToBatchFolderInput = selectedFolder;
            }
        }

    }
}
