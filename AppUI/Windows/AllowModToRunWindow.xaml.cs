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
                CheckboxVisibility = Visibility.Visible,
            };
            this.DataContext = ViewModel;
            btnOkay.Focus();
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Result = MessageBoxResult.OK;
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
            ViewModel.CheckboxVisibility = Visibility.Collapsed; // hide checkbox after Window is loaded so the ActualHeight is calculated
            ResizeWindowToFitContent();
        }

        private void ResizeWindowToFitContent()
        {
            double padding = 90; // this is to account for margins between controls

            double newHeight = txtMessage.ActualHeight + btnOkay.ActualHeight + chkOption.ActualHeight + stackRadioOptions.ActualHeight + padding;

            this.Height = newHeight;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ResizeWindowToFitContent();
        }
    }
}
