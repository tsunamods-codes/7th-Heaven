using _7thHeaven.Code;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
using SeventhHeaven.ViewModels;
using SeventhHeavenUI.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SeventhHeaven.Windows
{
    /// <summary>
    /// Interaction logic for GeneralSettingsWindow.xaml
    /// </summary>
    public partial class GeneralSettingsWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public GeneralSettingsViewModel ViewModel { get; set; }
        
        private UpdateChecker CoreUpdater = new UpdateChecker();

        private FFNxDriverUpdater FFNxUpdater = new FFNxDriverUpdater();

        public GeneralSettingsWindow()
        {
            InitializeComponent();

            ViewModel = new GeneralSettingsViewModel();
            ViewModel.ListDataChanged += ViewModel_ListDataChanged;

            this.DataContext = ViewModel;

            ViewModel.LoadSettings(Sys.Settings);
        }

        private void ViewModel_ListDataChanged()
        {
            RecalculateColumnWidths();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            bool settingsSaved = ViewModel.SaveSettings();

            if (settingsSaved)
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

        private void btnFf7Exe_Click(object sender, RoutedEventArgs e)
        {
            string initialDir = "";

            if (File.Exists(ViewModel.FF7ExePathInput))
            {
                initialDir = Path.GetDirectoryName(ViewModel.FF7ExePathInput);
            }

            string exePath = FileDialogHelper.BrowseForFile("*.exe|*.exe", ResourceHelper.Get(StringKey.SelectFf7Exe), initialDir);

            if (!string.IsNullOrEmpty(exePath))
            {
                FileInfo fileSelected = new FileInfo(exePath);
                if (fileSelected.Name.Equals("ff7_en.exe", System.StringComparison.InvariantCultureIgnoreCase) || fileSelected.Name.Equals("FF7_Launcher.exe", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    // User selected the exe's for Steam release so we try to auto copy the 1.02 patched exe and select it for them
                    string targetPathToFf7Exe = Path.Combine(fileSelected.DirectoryName, "ff7.exe");
                    string copyOrSelectMessage = ResourceHelper.Get(StringKey.Selected);

                    if (!File.Exists(targetPathToFf7Exe))
                    {
                        // use game converter to copy files over
                        var gc = new GameConverter(fileSelected.DirectoryName);
                        if (!gc.CopyFF7ExeToGame())
                        {
                            MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ThisExeIsUsedForSteamReleaseFailedToCopyExe),
                                                     ResourceHelper.Get(StringKey.ErrorIncorrectExe),
                                                     MessageBoxButton.OK,
                                                     MessageBoxImage.Error);
                            return;
                        }

                        copyOrSelectMessage = ResourceHelper.Get(StringKey.CopiedAndSelected);
                    }

                    ViewModel.FF7ExePathInput = targetPathToFf7Exe;

                    MessageDialogWindow.Show(string.Format(ResourceHelper.Get(StringKey.ThisExeIsUsedForSteamReleaseCopiedSelectedForYou), copyOrSelectMessage),
                                             ResourceHelper.Get(StringKey.ErrorIncorrectExe),
                                             MessageBoxButton.OK,
                                             MessageBoxImage.Warning);

                    return;
                }

                if (fileSelected.Name.Equals("FF7Config.exe", System.StringComparison.InvariantCultureIgnoreCase))
                {
                    MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ThisExeIsUsedForConfiguringFf7Settings),
                                             ResourceHelper.Get(StringKey.ErrorIncorrectExe),
                                             MessageBoxButton.OK,
                                             MessageBoxImage.Error);
                    return;
                }

                ViewModel.FF7ExePathInput = exePath;
            }
        }

        private void btnMovies_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FileDialogHelper.BrowseForFolder(ResourceHelper.Get(StringKey.SelectMoviesFolder), ViewModel.MoviesPathInput);

            if (!string.IsNullOrEmpty(folderPath))
            {
                ViewModel.MoviesPathInput = folderPath;
            }
        }

        private void btnTextures_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FileDialogHelper.BrowseForFolder(ResourceHelper.Get(StringKey.SelectTexturesFolder), ViewModel.TexturesPathInput);

            if (!string.IsNullOrEmpty(folderPath))
            {
                ViewModel.TexturesPathInput = folderPath;
            }
        }

        private void btnLibrary_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = FileDialogHelper.BrowseForFolder(ResourceHelper.Get(StringKey.Select7thHeavenLibraryFolder), ViewModel.LibraryPathInput);

            if (!string.IsNullOrEmpty(folderPath))
            {
                ViewModel.LibraryPathInput = folderPath;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollTextboxesToEnd();
            RecalculateColumnWidths();

            // resize window to fit grid contents (can happen when set to another language other than english)
            double padding = 100; // add to account for padding/margin between two group boxes
            double newWidth = grpOptions.ActualWidth + grpFolders.ActualWidth + padding;

            // re-position window based on difference between old and new width
            double deltaX = newWidth - this.Width;
            this.Left -= deltaX / 2;

            if (newWidth > this.Width)
            {
                this.Width = newWidth;
            }
        }

        private void ScrollTextboxesToEnd()
        {
            try
            {
                // reference: https://stackoverflow.com/questions/11232438/single-line-wpf-textbox-horizontal-scroll-to-end
                if (!string.IsNullOrWhiteSpace(ViewModel.MoviesPathInput))
                {
                    txtMovies.CaretIndex = ViewModel.MoviesPathInput.Length;
                    var rect = txtMovies.GetRectFromCharacterIndex(txtMovies.CaretIndex);
                    txtMovies.ScrollToHorizontalOffset(rect.Right);
                }

                if (!string.IsNullOrWhiteSpace(ViewModel.TexturesPathInput))
                {
                    txtTextures.CaretIndex = ViewModel.TexturesPathInput.Length;
                    var rect = txtTextures.GetRectFromCharacterIndex(txtTextures.CaretIndex);
                    txtTextures.ScrollToHorizontalOffset(rect.Right);

                }

                if (!string.IsNullOrWhiteSpace(ViewModel.LibraryPathInput))
                {
                    txtLibrary.CaretIndex = ViewModel.LibraryPathInput.Length;
                    var rect = txtLibrary.GetRectFromCharacterIndex(txtLibrary.CaretIndex);
                    txtLibrary.ScrollToHorizontalOffset(rect.Right);

                }

                if (!string.IsNullOrWhiteSpace(ViewModel.FF7ExePathInput))
                {
                    txtFf7Exe.CaretIndex = ViewModel.FF7ExePathInput.Length;
                    var rect = txtFf7Exe.GetRectFromCharacterIndex(txtFf7Exe.CaretIndex);
                    txtFf7Exe.ScrollToHorizontalOffset(rect.Right);
                }
            }
            catch (System.Exception ex)
            {
                Logger.Warn($"Failed to scroll textbox to end: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens 'Edit Subscription' popup
        /// </summary>
        private void btnEditUrl_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            if (lstSubscriptions.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectSubscriptionToEdit);
                return;
            }

            ViewModel.EditSelectedSubscription((lstSubscriptions.SelectedItem as SubscriptionSettingViewModel));
        }

        private void btnRemoveUrl_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup is opened
            }

            if (lstSubscriptions.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectSubscriptionToRemove);
                return;
            }

            ViewModel.RemoveSelectedSubscription((lstSubscriptions.SelectedItem as SubscriptionSettingViewModel));
        }

        /// <summary>
        /// Opens 'Add Subscription' popup
        /// </summary>
        private void btnAddUrl_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            ViewModel.AddNewSubscription();
        }

        private void btnMoveUrlDown_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup is opened
            }

            if (lstSubscriptions.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectSubscriptionToMove);
                return;
            }

            ViewModel.MoveSelectedSubscription((lstSubscriptions.SelectedItem as SubscriptionSettingViewModel), +1);
        }

        private void btnMoveUrlUp_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup is opened
            }

            if (lstSubscriptions.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectSubscriptionToMove);
                return;
            }

            ViewModel.MoveSelectedSubscription((lstSubscriptions.SelectedItem as SubscriptionSettingViewModel), -1);
        }

        /// <summary>
        /// Closes 'New/Edit' subscription popup and clears out any entered text
        /// </summary>
        private void btnCancelSubscription_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseSubscriptionPopup();
        }

        private void btnSaveSubscription_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveSubscription();
        }

        private void Window_LocationChanged(object sender, System.EventArgs e)
        {
            ViewModel.IsSubscriptionPopupOpen = false;
        }

        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            ViewModel.IsSubscriptionPopupOpen = false;
        }

        internal void RecalculateColumnWidths()
        {
            // trigger columns to auto re-size. https://stackoverflow.com/questions/42676198/gridviewcolumn-autosize-only-work-once
            colName.Width = colName.ActualWidth;
            colName.Width = double.NaN;

            colUrl.Width = colUrl.ActualWidth;
            colUrl.Width = double.NaN;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ListDataChanged -= ViewModel_ListDataChanged;
        }

        private void btnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddExtraFolder();
        }

        private void btnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (lstExtraFolders.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectFolderToRemove);
                return;
            }

            ViewModel.ExtraFolderList.Remove((lstExtraFolders.SelectedItem as string));
        }

        private void btnMoveUrlUp_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup is opened
            }

            if (lstSubscriptions.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectSubscriptionToMove);
                return;
            }

            ViewModel.MoveSelectedSubscription((lstSubscriptions.SelectedItem as SubscriptionSettingViewModel), 0 - lstSubscriptions.SelectedIndex);

        }

        private void btnMoveUrlDown_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.IsSubscriptionPopupOpen)
            {
                return; // dont do anything if popup is opened
            }

            if (lstSubscriptions.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectSubscriptionToMove);
                return;
            }

            ViewModel.MoveSelectedSubscription((lstSubscriptions.SelectedItem as SubscriptionSettingViewModel), (ViewModel.SubscriptionList.Count - 1) - lstSubscriptions.SelectedIndex);

        }

        private void btnMoveFolderUp_Click(object sender, RoutedEventArgs e)
        {
            if (lstExtraFolders.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectFolderToMove);
                return;
            }

            ViewModel.MoveSelectedFolder((lstExtraFolders.SelectedItem as string), -1);
        }

        private void btnMoveFolderDown_Click(object sender, RoutedEventArgs e)
        {
            if (lstExtraFolders.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectFolderToMove);
                return;
            }

            ViewModel.MoveSelectedFolder((lstExtraFolders.SelectedItem as string), +1);
        }

        private void btnMoveFolderUp_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstExtraFolders.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectFolderToMove);
                return;
            }

            ViewModel.MoveSelectedFolder((lstExtraFolders.SelectedItem as string), 0 - lstExtraFolders.SelectedIndex);
        }

        private void btnMoveFolderDown_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstExtraFolders.SelectedItem == null)
            {
                ViewModel.StatusMessage = ResourceHelper.Get(StringKey.SelectFolderToMove);
                return;
            }

            ViewModel.MoveSelectedFolder((lstExtraFolders.SelectedItem as string), (ViewModel.ExtraFolderList.Count - 1) - lstExtraFolders.SelectedIndex);
        }

        private void btnDefaults_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetToDefaults();
        }

        private void cmbFFNxChannel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.FFNxUpdateChannel = (FFNxUpdateChannelOptions)cmbFFNxChannel.SelectedIndex;

                // Bypass the default save button in order to make the Check for Updates button work instantly
                Sys.Settings.FFNxUpdateChannel = ViewModel.FFNxUpdateChannel;
            }
        }

        private void cmb7thChannel_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.AppUpdateChannel = (AppUpdateChannelOptions)cmb7thChannel.SelectedIndex;

                // Bypass the default save button in order to make the Check for Updates button work instantly
                Sys.Settings.AppUpdateChannel = ViewModel.AppUpdateChannel;
            }
        }

        private void btnFFNxCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            FFNxUpdater.CheckForUpdates(Sys.Settings.FFNxUpdateChannel);
        }

        private void btn7thCheckForUpdate_Click(object sender, RoutedEventArgs e)
        {
            CoreUpdater.CheckForUpdates(Sys.Settings.AppUpdateChannel, true);
        }
    }
}
