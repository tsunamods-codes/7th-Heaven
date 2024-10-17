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
using System.Windows.Shapes;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for OpenProfileWindow.xaml
    /// </summary>
    public partial class OpenProfileWindow : Window
    {
        public OpenProfileViewModel ViewModel { get; set; }

        public OpenProfileWindow()
        {
            InitializeComponent();

            ViewModel = new OpenProfileViewModel();
            this.DataContext = ViewModel;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!IsProfileSelected())
            {
                return;
            }

            ViewModel.SwitchToProfile((string)lstProfiles.SelectedItem);
            CloseWindow(true);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedProfile = null;
            CloseWindow(false);
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsProfileSelected())
            {
                return;
            }

            string selected = (string)lstProfiles.SelectedItem;

            if (MessageDialogWindow.Show($"{ResourceHelper.Get(StringKey.AreYouSureYouWantToDeleteSelectedProfile)} ({selected})?", ResourceHelper.Get(StringKey.Warning), MessageBoxButton.YesNo, MessageBoxImage.Warning).Result == MessageBoxResult.Yes)
            {
                ViewModel.DeleteProfile(selected);
            }
        }

        private void CopyProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!IsProfileSelected())
            {
                return;
            }

            ViewModel.CopyProfile((string)lstProfiles.SelectedItem);
        }

        private void SaveActiveProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveActiveProfileAsNew();
        }

        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (!IsProfileSelected())
            {
                return;
            }

            ViewModel.ViewProfileDetails((string)lstProfiles.SelectedItem);
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstProfiles.SelectedItem == null)
            {
                return;
            }

            if (ViewModel.SwitchToProfile((string)lstProfiles.SelectedItem))
            {
                CloseWindow(true);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.AttemptDeleteTempFiles();
        }


        private bool IsProfileSelected()
        {
            if (lstProfiles.SelectedItem == null)
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.SelectProfileToContinue), ResourceHelper.Get(StringKey.NoProfileSelected), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private void CloseWindow(bool dialogResult)
        {
            this.DialogResult = dialogResult;
            this.Close();
        }
    }
}
