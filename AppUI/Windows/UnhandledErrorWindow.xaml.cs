using AppUI;
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
using System.Windows.Shapes;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for UnhandledErrorWindow.xaml
    /// </summary>
    public partial class UnhandledErrorWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public static void Show(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                UnhandledErrorWindow window = new UnhandledErrorWindow(message)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                window.ShowDialog();
            });
        }

        public static void Show(Exception exception)
        {
            Logger.Error(exception);

            string message = $"{exception.Message}\n\n{exception.StackTrace}";
            Show(message);
        }

        public UnhandledErrorWindow()
        {
            InitializeComponent();
        }

        public UnhandledErrorWindow(string exceptionMessage)
        {
            InitializeComponent();
            txtMessage.Text = exceptionMessage;
        }

        private void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
