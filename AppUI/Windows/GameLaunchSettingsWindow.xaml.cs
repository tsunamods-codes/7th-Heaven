using AppCore;
using AppUI.Classes;
using AppUI.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for GameLaunchSettingsWindow.xaml
    /// </summary>
    public partial class GameLaunchSettingsWindow : Window
    {
        GameLaunchSettingsViewModel ViewModel { get; set; }

        public GameLaunchSettingsWindow()
        {
            InitializeComponent();

            ViewModel = new GameLaunchSettingsViewModel();
            this.DataContext = ViewModel;
        }

        private void btnTestAudio_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TestAudio(AudioChannel.Center);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SaveSettings())
            {
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuItemTestLeft_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TestAudio(AudioChannel.Left);
        }

        private void menuItemTestRight_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TestAudio(AudioChannel.Right);
        }

        private void menuAudioTest_Closed(object sender, RoutedEventArgs e)
        {
            btnAudioOptions.IsEnabled = true; // ensure button is re-enabled after context menu closes
        }

        private void btnAudioOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!menuAudioTest.IsOpen)
            {
                menuAudioTest.PlacementTarget = btnAudioOptions;
                menuAudioTest.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                menuAudioTest.IsOpen = true;
                btnAudioOptions.IsEnabled = false; // disable the button while the menu is open until context menu is closed
            }
        }

        private void btnAddProgram_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsProgramPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            ViewModel.AddNewProgram();
        }

        private void btnRemoveProgram_Click(object sender, RoutedEventArgs e)
        {
            if (lstPrograms.SelectedItem == null)
            {
                return;
            }

            ViewModel.RemoveSelectedProgram((lstPrograms.SelectedItem as ProgramToRunViewModel));
        }

        private void btnEditProgram_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsProgramPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            if (lstPrograms.SelectedItem == null)
            {
                return;
            }

            ViewModel.EditSelectedProgram((lstPrograms.SelectedItem as ProgramToRunViewModel));
        }

        private void btnCancelProgramAction_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseProgramPopup();
        }

        private void btnSaveProgram_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveProgramToRun();
        }

        private void btnBrowseProgram_Click(object sender, RoutedEventArgs e)
        {
            string pathToProgram = FileDialogHelper.BrowseForFile("All files|*.*", ResourceHelper.Get(StringKey.SelectProgramToRunSuchAs), ViewModel.NewProgramPathText);

            ViewModel.IsProgramPopupOpen = true; // opening file dialog closes popup so re-open it

            if (!string.IsNullOrWhiteSpace(pathToProgram))
            {
                ViewModel.NewProgramPathText = pathToProgram;
            }
        }

        private void Window_LocationChanged(object sender, System.EventArgs e)
        {
            ViewModel.IsProgramPopupOpen = false;
        }

        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            ViewModel.IsProgramPopupOpen = false;
        }

        private void btnMoveProgramDown_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsProgramPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            if (lstPrograms.SelectedItem == null)
            {
                return;
            }

            ViewModel.ChangeProgramOrder((lstPrograms.SelectedItem as ProgramToRunViewModel), +1);
        }

        private void btnMoveProgramUp_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsProgramPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            if (lstPrograms.SelectedItem == null)
            {
                return;
            }

            ViewModel.ChangeProgramOrder((lstPrograms.SelectedItem as ProgramToRunViewModel), -1);
        }

        private void sliderSfxVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowOrHideToolTip(sliderSfxVolume.ToolTip as ToolTip, true);
        }

        private void sliderSfxVolume_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowOrHideToolTip(sliderSfxVolume.ToolTip as ToolTip, false);
        }

        private void ShowOrHideToolTip(ToolTip controlToolTip, bool isOpen)
        {
            if (controlToolTip != null && controlToolTip.Content != null)
            {
                controlToolTip.IsOpen = isOpen;
            }
        }

        private void sliderMusicVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowOrHideToolTip(sliderMusicVolume.ToolTip as ToolTip, true);
        }

        private void sliderMusicVolume_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowOrHideToolTip(sliderMusicVolume.ToolTip as ToolTip, false);
        }

        private void sliderVoiceVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowOrHideToolTip(sliderVoiceVolume.ToolTip as ToolTip, true);
        }

        private void sliderVoiceVolume_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowOrHideToolTip(sliderVoiceVolume.ToolTip as ToolTip, false);
        }

        private void sliderAmbientVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowOrHideToolTip(sliderAmbientVolume.ToolTip as ToolTip, true);
        }

        private void sliderAmbientVolume_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowOrHideToolTip(sliderAmbientVolume.ToolTip as ToolTip, false);
        }

        private void sliderMovieVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ShowOrHideToolTip(sliderMovieVolume.ToolTip as ToolTip, true);
        }

        private void sliderMovieVolume_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowOrHideToolTip(sliderMovieVolume.ToolTip as ToolTip, false);
        }

        private void btnMoveProgramUp_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.IsProgramPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            if (lstPrograms.SelectedItem == null)
            {
                return;
            }

            ViewModel.ChangeProgramOrder((lstPrograms.SelectedItem as ProgramToRunViewModel), 0 - lstPrograms.SelectedIndex);
        }

        private void btnMoveProgramDown_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ViewModel.IsProgramPopupOpen)
            {
                return; // dont do anything if popup opened already
            }

            if (lstPrograms.SelectedItem == null)
            {
                return;
            }

            ViewModel.ChangeProgramOrder((lstPrograms.SelectedItem as ProgramToRunViewModel), (lstPrograms.Items.Count - 1) - lstPrograms.SelectedIndex);
        }

        private void btnDefaults_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetToDefaults();
        }

        private void windowSettings_Loaded(object sender, RoutedEventArgs e)
        {
            ResizeWindowToFitContent();
        }

        /// <summary>
        /// Resize window to fit grid contents (can happen when set to another language other than english)
        /// </summary>
        private void ResizeWindowToFitContent()
        {
            double padding = 60; // add to account for padding/margin on group boxes
            double newWidth = Math.Max(gridOptions.ActualWidth, gridAudio.ActualWidth) + padding;

            // re-position window based on difference between old and new width
            double deltaX = newWidth - this.Width;

            if (newWidth > this.Width)
            {
                this.Left -= deltaX / 2;
                this.Width = newWidth;
            }

        }
    }
}
