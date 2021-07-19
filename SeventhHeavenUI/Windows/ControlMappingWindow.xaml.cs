using SeventhHeaven.Classes;
using SeventhHeaven.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace SeventhHeaven.Windows
{
    /// <summary>
    /// Interaction logic for ControlMappingWindow.xaml
    /// </summary>
    public partial class ControlMappingWindow : Window
    {
        public ControlMappingViewModel ViewModel { get; set; }

        public ControlMappingWindow()
        {
            InitializeComponent();

            ViewModel = new ControlMappingViewModel();
            this.DataContext = ViewModel;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool didCapture = false;

            if (e.Key == Key.System)
            {
                didCapture = ViewModel.SetControlIfCapturing(e.SystemKey);

            }
            else if (IsNumpadEnterKey(e))
            {
                didCapture = ViewModel.SetNumpadEnterControlIfCapturing();
            }
            else if (e.Key == Key.Enter)
            {
                // keyboard enter pressed which is also recognized as 'Return'
                didCapture = ViewModel.SetControlIfCapturing(Key.Return);
            }
            else
            {
                didCapture = ViewModel.SetControlIfCapturing(e.Key);
            }

            if (didCapture)
            {
                e.Handled = true; // prevent event from bubbling up and peforming other ations
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            // the print screen key is detected on key up events
            if (e.Key == Key.PrintScreen)
            {
                Window_PreviewKeyDown(sender, e);
            }
        }

        private void btnDeleteControls_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteSelectedCustomControl();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsCustomConfigOptionSelected)
            {
                ViewModel.SaveNewCustomControl();
            }
            else
            {
                ViewModel.SaveChangesToFile();
                MessageDialogWindow.Show(
                    "In-Game Controls have been successfully updated",
                    "Controls Saved"
                );
            }
        }

        private static bool IsNumpadEnterKey(KeyEventArgs e)
        {
            // reference: https://stackoverflow.com/questions/8059177/distinguish-between-normal-enter-and-the-number-pad-enter-keypress

            if (e.Key != Key.Enter)
                return false;

            // To understand the following UGLY implementation please check this MSDN link. Suggested workaround to differentiate between the Return key and Enter key.
            // https://social.msdn.microsoft.com/Forums/vstudio/en-US/b59e38f1-38a1-4da9-97ab-c9a648e60af5/whats-the-difference-between-keyenter-and-keyreturn?forum=wpf
            try
            {
                return (bool)typeof(KeyEventArgs).InvokeMember("IsExtendedKey", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, e, null);
            }
            catch (Exception)
            {
            }

            return false;
        }

        #region Button Events for keyboard bindings


        private void btnOkKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Ok);
        }


        private void btnCancelKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Cancel);
        }

        private void btnMenuKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Menu);
        }

        private void btnSwitchKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Switch);
        }

        private void btnPageUpKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Pageup);
        }

        private void btnPageDownKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Pagedown);
        }

        private void btnCameraKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Camera);
        }

        private void btnTargetKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Target);
        }

        private void btnAssistKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Assist);
        }

        private void btnStartKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Start);
        }

        private void btnUpKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Up);
        }

        private void btnDownKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Down);
        }

        private void btnLeftKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Left);
        }

        private void btnRightKeyboard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingKeyboardInput(GameControl.Right);
        }

        #endregion


        #region Button Events for controller bindings


        private void btnOkController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Ok);
        }

        private void btnCancelController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Cancel);
        }

        private void btnMenuController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Menu);
        }

        private void btnSwitchController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Switch);
        }

        private void btnPageUpController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Pageup);
        }

        private void btnPageDownController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Pagedown);
        }

        private void btnCameraController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Camera);
        }

        private void btnTargetController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Target);
        }

        private void btnAssistController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Assist);
        }

        private void btnStartController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Start);
        }

        private void btnUpController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Up);
        }

        private void btnDownController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Down);
        }

        private void btnLeftController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Left);
        }

        private void btnRightController_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartCapturingControllerInput(GameControl.Right);
        }

        #endregion

        private void btnOpenGameControllersWindow_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.LaunchControlPanelGameControllersWindow();
        }

        private void windowControls_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ViewModel.ConnectedController?.ReleaseDevice();
        }

        private void windowControls_Loaded(object sender, RoutedEventArgs e)
        {
            ResizeWindowToFitContent();
        }

        private void ResizeWindowToFitContent()
        {
            double padding = 5;
            double newWidth = gridMain.ActualWidth + padding;

            if (newWidth > this.Width)
            {
                this.Width = newWidth;
            }
        }
    }
}
