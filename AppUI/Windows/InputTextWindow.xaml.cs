using AppUI.ViewModels;
using System.Windows;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for InputTextWindow.xaml
    /// </summary>
    public partial class InputTextWindow : Window
    {
        public InputTextViewModel ViewModel { get; set; }

        public InputTextWindow()
        {
            InitializeComponent();

            ViewModel = new InputTextViewModel();
            this.DataContext = ViewModel;
            txtInput.Focus();
        }

        public InputTextWindow(string windowTitle, string prompt)
        {
            InitializeComponent();

            ViewModel = new InputTextViewModel()
            {
                WindowTitle = windowTitle,
                Message = prompt
            };

            this.DataContext = ViewModel;
            txtInput.Focus();
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

        private void txtInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}
