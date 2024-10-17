using AppUI.ViewModels;
using AppUI;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for InputTextWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            this.Title = $"About {App.GetAppName()} v{App.GetAppVersion().ToString()}";
            btnClose.Focus();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
            };
            Process.Start(startInfo);
            e.Handled = true;
        }

    }
}
