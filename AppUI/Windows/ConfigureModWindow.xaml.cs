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
    /// Interaction logic for ConfigureModWindow.xaml
    /// </summary>
    public partial class ConfigureModWindow : Window
    {

        public ConfigureModViewModel ViewModel { get; set; }

        public ConfigureModWindow()
        {
            InitializeComponent();

            ViewModel = new ConfigureModViewModel();
            this.DataContext = ViewModel;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PlayPreviewAudio();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ViewModel.CleanUp();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetToModDefaultValues();
        }

        private void treeOptions_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ConfigOptionViewModel option = e.NewValue as ConfigOptionViewModel;
            ViewModel.SelectedOption = option;
        }
    }
}
