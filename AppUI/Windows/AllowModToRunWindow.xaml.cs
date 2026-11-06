using AppCore;
using AppUI.Classes;
using AppUI.ViewModels;
using System.Windows;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for InputTextWindow.xaml
    /// </summary>
    public partial class AllowModToRunWindow : Window
    {
        public AllowModToRunViewModel ViewModel { get; set; }

        public AllowModToRunWindow(string message)
        {
            InitializeComponent();

            ViewModel = new AllowModToRunViewModel()
            {
                Message = message,
                WindowTitle = ResourceHelper.Get(StringKey.AllowModToRun),
            };
            this.DataContext = ViewModel;
            btnOkay.Focus();
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void gridMain_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResizeWindowToFitContent();
        }

        private void ResizeWindowToFitContent()
        {
            double padding = 90; // this is to account for margins between controls

            double newHeight = txtMessage.ActualHeight + btnOkay.ActualHeight + stackRadioOptions.ActualHeight + padding;

            this.Height = newHeight;
        }

    }
}
