using System.Windows;
using AppUI.ViewModels;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for MovieImportWindow.xaml
    /// </summary>
    public partial class MovieImportWindow : Window
    {
        public MovieImportViewModel ViewModel { get; set; }

        public MovieImportWindow()
        {
            InitializeComponent();

            ViewModel = new MovieImportViewModel();
            this.DataContext = ViewModel;
        }

        private void btnImportMovies_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ImportMissingMovies();
        }
    }
}
