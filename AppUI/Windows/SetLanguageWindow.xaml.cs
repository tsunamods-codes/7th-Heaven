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
using System.Windows.Shapes;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for SetLanguageWindow.xaml
    /// </summary>
    public partial class SetLanguageWindow : Window
    {
        internal SetLanguageViewModel ViewModel { get; set; }

        public SetLanguageWindow()
        {
            InitializeComponent();

            ViewModel = new SetLanguageViewModel();
            this.DataContext = ViewModel;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SaveSelectedLanguageAsDefault())
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
