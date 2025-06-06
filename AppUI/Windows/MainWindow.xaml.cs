﻿using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using AppUI.ViewModels;
using AppUI.Windows;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;

namespace AppUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal MainWindowViewModel ViewModel { get; set; }

        private int _currentTabIndex = 0;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel();
            this.DataContext = ViewModel;

            ctrlMyMods.SetDataContext(ViewModel.MyMods);
            ctrlCatalog.SetDataContext(ViewModel.CatalogMods);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.InitViewModel();

            _currentTabIndex = tabCtrlMain.SelectedIndex;

            SetWindowSizeAndLocation();

            App.ProcessCommandLineArgs(Environment.GetCommandLineArgs(), true);

            InitColumnSettings();
        }

        private void InitColumnSettings()
        {
            // ensure no column settings are null before loading them
            if (Sys.Settings.UserColumnSettings == null)
            {
                Sys.Settings.UserColumnSettings = ColumnSettings.GetDefaultSettings();
            }
            else if (Sys.Settings.UserColumnSettings.MyModsColumns == null)
            {
                Sys.Settings.UserColumnSettings.MyModsColumns = ColumnSettings.GetDefaultSettings().MyModsColumns;
            }
            else if (Sys.Settings.UserColumnSettings.BrowseCatalogColumns == null)
            {
                Sys.Settings.UserColumnSettings.BrowseCatalogColumns = ColumnSettings.GetDefaultSettings().BrowseCatalogColumns;
            }

            ctrlMyMods.ApplyColumnSettings(Sys.Settings.UserColumnSettings.MyModsColumns);
            ctrlCatalog.ApplyColumnSettings(Sys.Settings.UserColumnSettings.BrowseCatalogColumns);
            ctrlCatalog.SetSortColumn(Sys.Settings.UserColumnSettings.SortColumn, (ListSortDirection)Sys.Settings.UserColumnSettings.SortDirection);
        }

        /// <summary>
        /// Sets the window size and location based on what was saved in <see cref="Sys.Settings"/>.
        /// Sets startup location to <see cref="WindowStartupLocation.CenterScreen"/> if no settings saved
        /// </summary>
        private void SetWindowSizeAndLocation()
        {
            if (Sys.Settings.MainWindow != null)
            {
                var loc = new System.Drawing.Point((int)Sys.Settings.MainWindow.X, (int)Sys.Settings.MainWindow.Y);

                if (Screen.AllScreens.Any(s => s.Bounds.Contains(loc)))
                {
                    WindowStartupLocation = WindowStartupLocation.Manual;
                    System.Windows.Application.Current.MainWindow.Left = Sys.Settings.MainWindow.X;
                    System.Windows.Application.Current.MainWindow.Top = Sys.Settings.MainWindow.Y;
                }

                Width = Sys.Settings.MainWindow.W;
                Height = Sys.Settings.MainWindow.H;
                WindowState = Sys.Settings.MainWindow.State;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (GameLauncher.IsFF7Running())
            {
                e.Cancel = true;
                return;
            }

            if (Sys.IsImporting)
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.CannotExitWhileImporting), ResourceHelper.Get(StringKey.Warning), MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            if (ViewModel.CatalogMods.DownloadList.Count > 0)
            {
                var result = MessageDialogWindow.Show(ResourceHelper.Get(StringKey.AreYouSureYouWantToExitPendingDownloads), ResourceHelper.Get(StringKey.ConfirmExit), MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result.Result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            Sys.Settings.MainWindow = new SavedWindow()
            {
                X = (int)System.Windows.Application.Current.MainWindow.Left,
                Y = (int)System.Windows.Application.Current.MainWindow.Top,
                W = (int)ActualWidth,
                H = (int)ActualHeight,
                State = WindowState
            };

            Sys.Settings.UserColumnSettings.MyModsColumns = ctrlMyMods.GetColumnSettings();
            ctrlCatalog.SaveUsersColumnSettings();

            ViewModel.CleanUp();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LaunchGame(variableDump: false, debugLogging: false);
        }

        private void txtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.DoSearch();
            }
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!menuSettings.IsOpen)
            {
                menuSettings.PlacementTarget = btnSettings;
                menuSettings.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menuSettings.IsOpen = true;
                btnSettings.IsEnabled = false;
            }
        }

        private void btnTools_Click(object sender, RoutedEventArgs e)
        {
            if (!menuTools.IsOpen)
            {
                menuTools.PlacementTarget = btnTools;
                menuTools.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menuTools.IsOpen = true;
                btnTools.IsEnabled = false;
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            if (!menuHelp.IsOpen)
            {
                menuHelp.PlacementTarget = btnHelp;
                menuHelp.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menuHelp.IsOpen = true;
                btnTools.IsEnabled = false;
            }
        }

        private void menuTools_Closed(object sender, RoutedEventArgs e)
        {
            btnTools.IsEnabled = true;
        }
        private void menuHelp_Closed(object sender, RoutedEventArgs e)
        {
            btnTools.IsEnabled = true;
        }

        private void menuSettings_Closed(object sender, RoutedEventArgs e)
        {
            btnSettings.IsEnabled = true;
        }

        private void btnPlayOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!menuPlayOptions.IsOpen)
            {
                menuPlayOptions.PlacementTarget = btnPlayOptions;
                menuPlayOptions.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menuPlayOptions.IsOpen = true;
                ViewModel.IsPlayToggleButtonEnabled = false;
            }
        }

        private void menuPlayVariableDump_Click(object sender, RoutedEventArgs e)
        {
            if (MessageDialogWindow.Show(ResourceHelper.Get(StringKey.AreYouSureYouWantToPlayDebugWarning), ResourceHelper.Get(StringKey.Warning), MessageBoxButton.YesNo, MessageBoxImage.Warning).Result == MessageBoxResult.Yes)
            {
                ViewModel.LaunchGame(variableDump: true, debugLogging: false);
            }
        }

        private void menuPlayDebugLog_Click(object sender, RoutedEventArgs e)
        {
            if (MessageDialogWindow.Show(ResourceHelper.Get(StringKey.AreYouSureYouWantToPlayDebugWarning), ResourceHelper.Get(StringKey.Warning), MessageBoxButton.YesNo, MessageBoxImage.Warning).Result == MessageBoxResult.Yes)
            {
                ViewModel.LaunchGame(variableDump: false, debugLogging: true);
            }
        }

        private void menuPlayOptions_Closed(object sender, RoutedEventArgs e)
        {
            ViewModel.IsPlayToggleButtonEnabled = true;
        }

        private void btnHelpOpen_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LaunchHelpPage();
        }

        private void btnOpenModLink_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenPreviewModLink();
        }

        private void btnOpenDonationModLink_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenDonationModLink();
        }

        private void btnOpenModReadme_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenPreviewModReadMe();
        }

        private void menuItemGeneralSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowGeneralSettingsWindow();
        }

        private void menuItemOpenProfile_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowProfilesWindow();
        }

        private void menuItemChunkTool_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowChunkToolWindow();
        }

        private void btnFilters_Click(object sender, RoutedEventArgs e)
        {
            if (!popupFilters.IsOpen)
            {
                btnFilters.IsEnabled = false; // disable button while it is opened
                popupFilters.IsOpen = true;
            }
        }

        private void popupFilters_Closed(object sender, EventArgs e)
        {
            btnFilters.IsEnabled = true;
            ViewModel.ApplyFiltersAndReloadList();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ctrlMyMods.RecalculateColumnWidths();
            ctrlCatalog.RecalculateColumnWidths();
        }

        private void menuItemIroCreation_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowIroToolsWindow();
        }

        private void menuItemThemeSettings_Click(object sender, RoutedEventArgs e)
        {
            ThemeSettingsWindow window = new ThemeSettingsWindow();
            window.ViewModel.BackgroundImageChanged += ThemeSettings_BackgroundImageChanged;
            window.ViewModel.BackgroundPropsChanged += ThemeSettings_BackgroundPropsChanged;
            window.ShowDialog();
            window.ViewModel.BackgroundImageChanged -= ThemeSettings_BackgroundImageChanged;
            window.ViewModel.BackgroundPropsChanged -= ThemeSettings_BackgroundPropsChanged;
        }

        private void ThemeSettings_BackgroundImageChanged(byte[] newImage)
        {
            ViewModel.UpdateBackgroundImage(newImage);
        }

        private void ThemeSettings_BackgroundPropsChanged(System.Windows.HorizontalAlignment horizontalAlignment, System.Windows.VerticalAlignment verticalAlignment, System.Windows.Media.Stretch stretch)
        {
            ViewModel.UpdateBackgroundProps(horizontalAlignment, verticalAlignment, stretch);
        }

        private void tabCtrlMain_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int tabSelectedIndex = tabCtrlMain.SelectedIndex;

            if (tabCtrlMain.SelectedIndex == 0 && _currentTabIndex != tabSelectedIndex)
            {
                _currentTabIndex = tabSelectedIndex;
                ctrlMyMods.RecalculateColumnWidths(ctrlCatalog.lstCatalogMods.ActualWidth);
            }
            else if (_currentTabIndex != tabSelectedIndex)
            {
                _currentTabIndex = tabSelectedIndex;
                ctrlCatalog.RecalculateColumnWidths(ctrlMyMods.lstMods.ActualWidth);
                ctrlCatalog.RecalculateDownloadColumnWidths(ctrlMyMods.lstMods.ActualWidth);
            }
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.LoadingGifVisibility = Visibility.Hidden;
        }

        private void menuItemAaliSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowGameDriverConfigWindow();
        }

        private void menuPlayWithMods_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LaunchGame(false, false);
        }

        private void menuPlayWithoutMods_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LaunchGame(false, false, noMods: true);
        }

        private void btnOpenAppLog_Click(object sender, RoutedEventArgs e)
        {
            Sys.OpenAppLog();
        }

        private void btnOpenAppLog_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Sys.PathToCrashReports)
            {
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }

        private void MyModsTabItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // additional check the source of the click since this event can be raised when the content of the TabItem is clicked
            if (e.Source is TabItem || e.Source is System.Windows.Controls.TextBlock)
            {
                Sys.OpenLibraryFolderInExplorer();
                e.Handled = true;
            }
        }

        private void BrowseCatalogTabItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is TabItem || e.Source is System.Windows.Controls.TextBlock)
            {
                ViewModel.ShowGeneralSettingsWindow();
                e.Handled = true;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModel.DoSearch();
        }

        private void menuItemLaunchSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowGameLaunchSettingsWindow();
        }

        private void menuItemCatalogCreation_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowCatalogCreationTool();
        }

        private void btnUpdateMod_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateSelectedMod();
        }

        private void btnUpdateModType_Click(object sender, RoutedEventArgs e)
        {
            if (!popupModUpdateType.IsOpen)
            {
                popupModUpdateType.IsOpen = true;
                btnUpdateModType.IsEnabled = false;
            }
        }

        private void popupModUpdateType_Closed(object sender, EventArgs e)
        {
            btnUpdateModType.IsEnabled = true;
        }

        private void menuItemSetLanguage_Click(object sender, RoutedEventArgs e)
        {
            SetLanguageWindow languageWindow = new SetLanguageWindow();
            bool? isChanged = languageWindow.ShowDialog();

            if (isChanged.GetValueOrDefault(false) == true)
            {
                ViewModel.RefreshTranslations();
            }
        }

        private void menuItemControls_Click(object sender, RoutedEventArgs e)
        {
            new ControlMappingWindow().ShowDialog();
        }

        private void menuItemMovieImport_Click(object sender, RoutedEventArgs e)
        {
            new MovieImportWindow().ShowDialog();
        }

        private void menuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        private void TsunamodsSupport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("https://download.tsunamods.com/?id=15")
            {
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void menuItemOpenSaveDir_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(Sys.InstallPath, "save");

            if (Sys.Settings.FF7InstalledVersion == FF7Version.Steam)
            {
                path = Directory.EnumerateDirectories(GameConverter.GetSteamFF7UserPath(), "user_*").First();
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
            };
            Process.Start(startInfo);
        }
    }
}
