using Iros._7th.Workshop;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AppUI.Classes.KeyboardInputSender;

namespace AppUI.Classes
{
    /// <summary>
    /// Class contains logic to handle intercepting XInput devices for controls that are not supported natively by FF7.
    /// These controls are D-pad and the left/right trigger (on standard ds4 or xbox controllers; but named 'button 11' and 'button 12')
    /// </summary>
    internal class ControllerInterceptor
    {
        public bool PollingInput { get; set; }

        private GameController ConnectedController { get; set; }

        /// <summary>
        /// Polls for game pad input and maps the non-supported buttons to the keyboard binding.
        /// Non-supported buttons include D-pad, Left-stick click, Right-stick click
        /// </summary>
        internal Task PollForGamepadInput()
        {
            if (PollingInput)
            {
                return null; // already polling so just return
            }

            PollingInput = true;

            bool wasUpPressed = false;
            bool wasDownPressed = false;
            bool wasLeftPressed = false;
            bool wasRightPressed = false;


            return Task.Factory.StartNew(() =>
            {
                ControlConfiguration loadedConfig = ControlMapper.LoadConfigurationFromFile(Path.Combine(Sys.PathToControlsFolder, Sys.Settings.GameLaunchSettings.InGameConfigOption));

                bool hasDpadBinded = loadedConfig.GamepadInputs.Values.Any(i => i?.GamepadInput.Value == GamePadButton.DPadUp ||
                                                                                i?.GamepadInput.Value == GamePadButton.DPadDown ||
                                                                                i?.GamepadInput.Value == GamePadButton.DPadLeft ||
                                                                                i?.GamepadInput.Value == GamePadButton.DPadRight);


                ScanCodeShort? upKey = null;
                bool upIsExtended = false;
                ScanCodeShort? downKey = null;
                bool downIsExtended = false;
                ScanCodeShort? leftKey = null;
                bool leftIsExtended = false;
                ScanCodeShort? rightKey = null;
                bool rightIsExtended = false;

                GameControl bindedControl;

                if (hasDpadBinded)
                {
                    if (loadedConfig.GamepadInputs.Any(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadUp))
                    {
                        bindedControl = loadedConfig.GamepadInputs.Where(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadUp).Select(kv => kv.Key).FirstOrDefault();
                        upKey = loadedConfig.KeyboardInputs[bindedControl].KeyScanCode;
                        upIsExtended = loadedConfig.KeyboardInputs[bindedControl].KeyIsExtended;
                    }

                    if (loadedConfig.GamepadInputs.Any(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadDown))
                    {
                        bindedControl = loadedConfig.GamepadInputs.Where(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadDown).Select(kv => kv.Key).FirstOrDefault();
                        downKey = loadedConfig.KeyboardInputs[bindedControl].KeyScanCode;
                        downIsExtended = loadedConfig.KeyboardInputs[bindedControl].KeyIsExtended;
                    }

                    if (loadedConfig.GamepadInputs.Any(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadLeft))
                    {
                        bindedControl = loadedConfig.GamepadInputs.Where(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadLeft).Select(kv => kv.Key).FirstOrDefault();
                        leftKey = loadedConfig.KeyboardInputs[bindedControl].KeyScanCode;
                        leftIsExtended = loadedConfig.KeyboardInputs[bindedControl].KeyIsExtended;
                    }

                    if (loadedConfig.GamepadInputs.Any(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadRight))
                    {
                        bindedControl = loadedConfig.GamepadInputs.Where(kv => kv.Value?.GamepadInput.Value == GamePadButton.DPadRight).Select(kv => kv.Key).FirstOrDefault();
                        rightKey = loadedConfig.KeyboardInputs[bindedControl].KeyScanCode;
                        rightIsExtended = loadedConfig.KeyboardInputs[bindedControl].KeyIsExtended;
                    }
                }
                else
                {
                    // user has not binded their DPAD buttons to any controls so just assume it is going to be used for movement
                    upKey = loadedConfig.KeyboardInputs[GameControl.Up].KeyScanCode;
                    upIsExtended = loadedConfig.KeyboardInputs[GameControl.Up].KeyIsExtended;

                    downKey = loadedConfig.KeyboardInputs[GameControl.Down].KeyScanCode;
                    downIsExtended = loadedConfig.KeyboardInputs[GameControl.Down].KeyIsExtended;

                    leftKey = loadedConfig.KeyboardInputs[GameControl.Left].KeyScanCode;
                    leftIsExtended = loadedConfig.KeyboardInputs[GameControl.Left].KeyIsExtended;

                    rightKey = loadedConfig.KeyboardInputs[GameControl.Right].KeyScanCode;
                    rightIsExtended = loadedConfig.KeyboardInputs[GameControl.Right].KeyIsExtended;
                }

                if (ConnectedController == null)
                {
                    ConnectedController = new GameController();
                }

                while (PollingInput)
                {
                    if (ConnectedController.IsConnected == false)
                    {
                        // null means no connected controller found so just sleep for a little and check back later
                        Thread.Sleep(1000);
                        DS4ControllerService.Instance?.RootHub?.HotPlug();
                        ConnectedController.CreateDevice();
                        continue;
                    }

                    if (ConnectedController.ReadState() == null)
                    {
                        ConnectedController?.ReleaseDevice();
                        continue;
                    }



                    if (upKey != null && ConnectedController.IsButtonPressed(GamePadButton.DPadUp) && !wasUpPressed)
                    {
                        wasUpPressed = true;
                        SendKey(upKey.Value, upIsExtended);
                    }
                    else if (upKey != null && !ConnectedController.IsButtonPressed(GamePadButton.DPadUp) && wasUpPressed)
                    {
                        ReleaseKey(upKey.Value, upIsExtended);
                        wasUpPressed = false;
                    }

                    if (downKey != null && ConnectedController.IsButtonPressed(GamePadButton.DPadDown) && !wasDownPressed)
                    {
                        wasDownPressed = true;
                        SendKey(downKey.Value, downIsExtended);
                    }
                    else if (downKey != null && !ConnectedController.IsButtonPressed(GamePadButton.DPadDown) && wasDownPressed)
                    {
                        ReleaseKey(downKey.Value, downIsExtended);
                        wasDownPressed = false;
                    }

                    if (leftKey != null && ConnectedController.IsButtonPressed(GamePadButton.DPadLeft) && !wasLeftPressed)
                    {
                        wasLeftPressed = true;
                        SendKey(leftKey.Value, leftIsExtended);
                    }
                    else if (leftKey != null && !ConnectedController.IsButtonPressed(GamePadButton.DPadLeft) && wasLeftPressed)
                    {
                        ReleaseKey(leftKey.Value, leftIsExtended);
                        wasLeftPressed = false;
                    }

                    if (rightKey != null && ConnectedController.IsButtonPressed(GamePadButton.DPadRight) && !wasRightPressed)
                    {
                        wasRightPressed = true;
                        SendKey(rightKey.Value, rightIsExtended);
                    }
                    else if (rightKey != null && !ConnectedController.IsButtonPressed(GamePadButton.DPadRight) && wasRightPressed)
                    {
                        ReleaseKey(rightKey.Value, rightIsExtended);
                        wasRightPressed = false;
                    }
                }

                ConnectedController.ReleaseDevice();

            });

        }
    }
}
