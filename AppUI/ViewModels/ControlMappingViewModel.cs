using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using AppUI.Windows;
using AppUI;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
namespace AppUI.ViewModels
{

    public class ControlMappingViewModel : ViewModelBase
    {
        private enum CaptureState
        {
            NotCapturing,
            CaptureKeyboard,
            CaptureController
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private List<string> _defaultControlNames = new List<string>()
        {
            "[Default] Steam KB+PlayStation (Stock)",
            "[Default-Alt] Steam KB+PlayStation (Swap AB-XO)",
            "1998 KB+PlayStation (Stock)",
            "1998 KB+PlayStation (Swap AB-XO)",
            "unab0mb's Choice+PlayStation (Stock)",
            "unab0mb's Choice+PlayStation (Swap AB-XO)"
        };

        private bool _isCapturing;
        private GameControl _controlToCapture;
        private CaptureState _captureState;
        private bool _hasUnsavedChanges;
        private string _selectedGameConfigOption;

        private bool _isDpadSupportChecked;
        private bool _isPs4SupportChecked;
        private bool _isInstallingDriver;

        private string _okKeyboardText;
        private string _cancelKeyboardText;
        private string _menuKeyboardText;
        private string _switchKeyboardText;
        private string _pageUpKeyboardText;
        private string _pageDownKeyboardText;
        private string _cameraKeyboardText;
        private string _targetKeyboardText;
        private string _assistKeyboardText;
        private string _startKeyboardText;
        private string _upKeyboardText;
        private string _downKeyboardText;
        private string _leftKeyboardText;
        private string _rightKeyboardText;

        private string _okControllerText;
        private string _cancelControllerText;
        private string _menuControllerText;
        private string _switchControllerText;
        private string _pageUpControllerText;
        private string _pageDownControllerText;
        private string _cameraControllerText;
        private string _targetControllerText;
        private string _startControllerText;
        private string _upControllerText;
        private string _assistControllerText;
        private string _downControllerText;
        private string _leftControllerText;
        private string _rightControllerText;

        private string _okIcon;
        private string _cancelIcon;
        private string _menuIcon;
        private string _switchIcon;
        private string _pageUpIcon;
        private string _pageDownIcon;
        private string _cameraIcon;
        private string _targetIcon;
        private string _assistIcon;
        private string _startIcon;
        private string _upIcon;
        private string _downIcon;
        private string _leftIcon;
        private string _rightIcon;

        private ControlConfiguration LoadedConfiguration { get; set; }

        public string OkIcon
        {
            get
            {
                return _okIcon;
            }
            set
            {
                _okIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string CancelIcon
        {
            get
            {
                return _cancelIcon;
            }
            set
            {
                _cancelIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string MenuIcon
        {
            get
            {
                return _menuIcon;
            }
            set
            {
                _menuIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string SwitchIcon
        {
            get
            {
                return _switchIcon;
            }
            set
            {
                _switchIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string PageUpIcon
        {
            get
            {
                return _pageUpIcon;
            }
            set
            {
                _pageUpIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string PageDownIcon
        {
            get
            {
                return _pageDownIcon;
            }
            set
            {
                _pageDownIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string CameraIcon
        {
            get
            {
                return _cameraIcon;
            }
            set
            {
                _cameraIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string TargetIcon
        {
            get
            {
                return _targetIcon;
            }
            set
            {
                _targetIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string AssistIcon
        {
            get
            {
                return _assistIcon;
            }
            set
            {
                _assistIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string StartIcon
        {
            get
            {
                return _startIcon;
            }
            set
            {
                _startIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string UpIcon
        {
            get
            {
                return _upIcon;
            }
            set
            {
                _upIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string DownIcon
        {
            get
            {
                return _downIcon;
            }
            set
            {
                _downIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string LeftIcon
        {
            get
            {
                return _leftIcon;
            }
            set
            {
                _leftIcon = value;
                NotifyPropertyChanged();
            }
        }

        public string RightIcon
        {
            get
            {
                return _rightIcon;
            }
            set
            {
                _rightIcon = value;
                NotifyPropertyChanged();
            }
        }



        public string OkKeyboardText
        {
            get
            {
                return _okKeyboardText;
            }
            set
            {
                _okKeyboardText = value;
                NotifyPropertyChanged();
            }
        }


        public string CancelKeyboardText
        {
            get
            {
                return _cancelKeyboardText;
            }
            set
            {
                _cancelKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string MenuKeyboardText
        {
            get
            {
                return _menuKeyboardText;
            }
            set
            {
                _menuKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string SwitchKeyboardText
        {
            get
            {
                return _switchKeyboardText;
            }
            set
            {
                _switchKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string PageUpKeyboardText
        {
            get
            {
                return _pageUpKeyboardText;
            }
            set
            {
                _pageUpKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string PageDownKeyboardText
        {
            get
            {
                return _pageDownKeyboardText;
            }
            set
            {
                _pageDownKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string CameraKeyboardText
        {
            get
            {
                return _cameraKeyboardText;
            }
            set
            {
                _cameraKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string TargetKeyboardText
        {
            get
            {
                return _targetKeyboardText;
            }
            set
            {
                _targetKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string AssistKeyboardText
        {
            get
            {
                return _assistKeyboardText;
            }
            set
            {
                _assistKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string StartKeyboardText
        {
            get
            {
                return _startKeyboardText;
            }
            set
            {
                _startKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string UpKeyboardText
        {
            get
            {
                return _upKeyboardText;
            }
            set
            {
                _upKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string DownKeyboardText
        {
            get
            {
                return _downKeyboardText;
            }
            set
            {
                _downKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string LeftKeyboardText
        {
            get
            {
                return _leftKeyboardText;
            }
            set
            {
                _leftKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string RightKeyboardText
        {
            get
            {
                return _rightKeyboardText;
            }
            set
            {
                _rightKeyboardText = value;
                NotifyPropertyChanged();
            }
        }

        public string OkControllerText
        {
            get
            {
                return _okControllerText;
            }
            set
            {
                _okControllerText = value;
                NotifyPropertyChanged();
            }
        }


        public string CancelControllerText
        {
            get
            {
                return _cancelControllerText;
            }
            set
            {
                _cancelControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string MenuControllerText
        {
            get
            {
                return _menuControllerText;
            }
            set
            {
                _menuControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string SwitchControllerText
        {
            get
            {
                return _switchControllerText;
            }
            set
            {
                _switchControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string PageUpControllerText
        {
            get
            {
                return _pageUpControllerText;
            }
            set
            {
                _pageUpControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string PageDownControllerText
        {
            get
            {
                return _pageDownControllerText;
            }
            set
            {
                _pageDownControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string CameraControllerText
        {
            get
            {
                return _cameraControllerText;
            }
            set
            {
                _cameraControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string TargetControllerText
        {
            get
            {
                return _targetControllerText;
            }
            set
            {
                _targetControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string AssistControllerText
        {
            get
            {
                return _assistControllerText;
            }
            set
            {
                _assistControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string StartControllerText
        {
            get
            {
                return _startControllerText;
            }
            set
            {
                _startControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string UpControllerText
        {
            get
            {
                return _upControllerText;
            }
            set
            {
                _upControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string DownControllerText
        {
            get
            {
                return _downControllerText;
            }
            set
            {
                _downControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string LeftControllerText
        {
            get
            {
                return _leftControllerText;
            }
            set
            {
                _leftControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public string RightControllerText
        {
            get
            {
                return _rightControllerText;
            }
            set
            {
                _rightControllerText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsCapturing
        {
            get
            {
                return _isCapturing;
            }
            set
            {
                _isCapturing = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotCapturing));
            }
        }

        public bool IsNotCapturing
        {
            get
            {
                return !_isCapturing;
            }
        }

        public bool HasUnsavedChanges
        {
            get
            {
                return _hasUnsavedChanges;
            }
            set
            {
                _hasUnsavedChanges = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(ApplyButtonIsEnabled));
            }
        }

        public bool ApplyButtonIsEnabled
        {
            get
            {
                return HasUnsavedChanges && IsCustomConfigOptionSelected;
            }
        }


        public List<string> InGameConfigOptions
        {
            get
            {
                return InGameConfigurationMap.Keys.ToList();
            }
        }

        public Dictionary<string, string> InGameConfigurationMap { get; set; }

        public string SelectedGameConfigOption
        {
            get
            {
                return _selectedGameConfigOption;
            }
            set
            {
                _selectedGameConfigOption = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsCustomConfigOptionSelected));

                if (_selectedGameConfigOption != null)
                {
                    SetSelectedConfigOptionInLaunchSettings();
                    LoadSelectedConfiguration();
                    IsCapturing = false;
                }
            }
        }

        public bool IsCustomConfigOptionSelected
        {
            get
            {
                return !_defaultControlNames.Any(s => s.Equals(SelectedGameConfigOption, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public bool IsPs4SupportChecked
        {
            get
            {
                return _isPs4SupportChecked;
            }
            set
            {
                if (value == _isPs4SupportChecked)
                {
                    return; // value did not change
                }

                _isPs4SupportChecked = value;

                if (_isPs4SupportChecked)
                {
                    IsDpadSupportChecked = false; // do not allow both ps4 and dpad options on at same time
                    ConnectedController?.ReleaseDevice();
                    TurnOnPs4Service();
                }
                else
                {
                    ConnectedController?.ReleaseDevice();
                    TurnOffPs4Service();
                }

                SetPs4SupportInLaunchSettings();
                NotifyPropertyChanged();
            }
        }

        public bool IsInstallingDriver
        {
            get
            {
                return _isInstallingDriver;
            }
            set
            {
                _isInstallingDriver = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotInstallingDriver));
            }
        }

        public bool IsNotInstallingDriver
        {
            get
            {
                return !_isInstallingDriver;
            }
        }

        public bool IsDpadSupportChecked
        {
            get
            {
                return _isDpadSupportChecked;
            }
            set
            {
                if (value != _isDpadSupportChecked)
                {
                    _isDpadSupportChecked = value;
                    SetGamepadPollingInLaunchSettings();

                    NotifyPropertyChanged();
                    SetButtonIcon(nameof(UpIcon), LoadedConfiguration.GamepadInputs[GameControl.Up].GamepadInput.Value);
                    SetButtonIcon(nameof(DownIcon), LoadedConfiguration.GamepadInputs[GameControl.Down].GamepadInput.Value);
                    SetButtonIcon(nameof(LeftIcon), LoadedConfiguration.GamepadInputs[GameControl.Left].GamepadInput.Value);
                    SetButtonIcon(nameof(RightIcon), LoadedConfiguration.GamepadInputs[GameControl.Right].GamepadInput.Value);
                }

                // do not allow both ps4 and dpad options on at same time
                if (IsDpadSupportChecked) 
                {
                    IsPs4SupportChecked = false;
                }
            }
        }

        public ControlMappingViewModel()
        {
            _captureState = CaptureState.NotCapturing;

            InitInGameConfigOptions();

            _selectedGameConfigOption = InGameConfigurationMap.Where(s => s.Value == Sys.Settings.GameLaunchSettings.InGameConfigOption)
                                                             .Select(c => c.Key)
                                                             .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(SelectedGameConfigOption))
            {
                // default to first option if their previous option is missing
                _selectedGameConfigOption = InGameConfigOptions[0];
            }
            
            // setting private variables here so code in property setters are not executed (e.g update sys.settings and ui)
            _isPs4SupportChecked = Sys.Settings.GameLaunchSettings.EnablePs4ControllerService;
            _isDpadSupportChecked = Sys.Settings.GameLaunchSettings.EnableGamepadPolling;

            LoadSelectedConfiguration();
        }

        private void LoadSelectedConfiguration()
        {
            LoadedConfiguration = ControlMapper.LoadConfigurationFromFile(Path.Combine(Sys.PathToControlsFolder, InGameConfigurationMap[SelectedGameConfigOption]));
            UpdateAllButtonText();
            UpdateAllButtonIcons();
            HasUnsavedChanges = false;
        }

        private void InitInGameConfigOptions()
        {
            Dictionary<string, string> configOptions = new Dictionary<string, string>();

            if (!Directory.Exists(Sys.PathToControlsFolder))
            {
                Logger.Warn($"Controls folder missing. creating {Sys.PathToControlsFolder}");
                Directory.CreateDirectory(Sys.PathToControlsFolder);
            }

            foreach (string filePath in Directory.GetFiles(Sys.PathToControlsFolder, "*.cfg").OrderBy(s => s))
            {
                FileInfo info = new FileInfo(filePath);
                configOptions.Add(Path.GetFileNameWithoutExtension(filePath), info.Name);
            }

            InGameConfigurationMap = configOptions;
            NotifyPropertyChanged(nameof(InGameConfigOptions));
        }

        /// <summary>
        /// Saves which input configuration to use in-game (saves back to <see cref="Sys.Settings.GameLaunchSettings"/>)
        /// </summary>
        internal void SetSelectedConfigOptionInLaunchSettings()
        {
            try
            {
                Sys.Settings.GameLaunchSettings.InGameConfigOption = InGameConfigurationMap[SelectedGameConfigOption];
                Sys.SaveSettings();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Sets the ps4 controller support bool in game launch settings and  saves back to <see cref="Sys.Settings.GameLaunchSettings"/>
        /// </summary>
        internal void SetPs4SupportInLaunchSettings()
        {
            try
            {
                Sys.Settings.GameLaunchSettings.EnablePs4ControllerService = IsPs4SupportChecked;
                Sys.SaveSettings();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Sets the gamepad polling bool in game launch settings and  saves back to <see cref="Sys.Settings.GameLaunchSettings"/>
        /// </summary>
        internal void SetGamepadPollingInLaunchSettings()
        {
            try
            {
                Sys.Settings.GameLaunchSettings.EnableGamepadPolling = IsDpadSupportChecked;
                Sys.SaveSettings();
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        internal bool SetControlIfCapturing(System.Windows.Input.Key key)
        {
            if (!IsCapturing || _captureState != CaptureState.CaptureKeyboard)
            {
                return false;
            }

            LoadedConfiguration.SetKeyboardInput(_controlToCapture, key);

            UpdateAllButtonText();

            IsCapturing = false;
            HasUnsavedChanges = true;
            _captureState = CaptureState.NotCapturing;
            return true;
        }

        internal bool SetControlIfCapturing(GamePadButton pressedButton)
        {
            if (!IsCapturing || _captureState != CaptureState.CaptureController)
            {
                return false;
            }

            LoadedConfiguration.SetControllerInput(_controlToCapture, pressedButton);

            UpdateAllButtonText();
            UpdateAllButtonIcons();

            IsCapturing = false;
            _captureState = CaptureState.NotCapturing;
            HasUnsavedChanges = true;
            return true;
        }

        internal bool SetNumpadEnterControlIfCapturing()
        {
            if (!IsCapturing)
            {
                return false;
            }

            ControlInputSetting numpadEnterSetting = ControlMapper.ControlInputs.Where(c => c.DisplayText == "NUMPADENTER").FirstOrDefault();
            LoadedConfiguration.SetKeyboardInput(_controlToCapture, numpadEnterSetting);

            UpdateAllButtonText();

            IsCapturing = false;
            HasUnsavedChanges = true;
            _captureState = CaptureState.NotCapturing;
            return true;
        }

        internal void UpdateAllButtonIcons()
        {
            Dictionary<GameControl, string> propertiesToUpdate = new Dictionary<GameControl, string>()
            {
                { GameControl.Ok, nameof(OkIcon) },
                { GameControl.Cancel, nameof(CancelIcon) },
                { GameControl.Menu, nameof(MenuIcon) },
                { GameControl.Switch, nameof(SwitchIcon) },
                { GameControl.Pageup, nameof(PageUpIcon) },
                { GameControl.Pagedown, nameof(PageDownIcon) },
                { GameControl.Camera, nameof(CameraIcon) },
                { GameControl.Target, nameof(TargetIcon) },
                { GameControl.Assist, nameof(AssistIcon) },
                { GameControl.Start, nameof(StartIcon) },
                { GameControl.Up, nameof(UpIcon) },
                { GameControl.Down, nameof(DownIcon) },
                { GameControl.Left, nameof(LeftIcon) },
                { GameControl.Right, nameof(RightIcon) },
            };

            foreach (var item in propertiesToUpdate)
            {
                LoadedConfiguration.GamepadInputs.TryGetValue(item.Key, out ControlInputSetting setting);
                if (setting != null)
                {
                    SetButtonIcon(item.Value, LoadedConfiguration.GamepadInputs[item.Key].GamepadInput.Value);
                }
                else
                {
                    SetButtonIcon(item.Value, null);
                }
            }
        }

        internal void UpdateAllButtonText()
        {
            foreach (GameControl item in Enum.GetValues(typeof(GameControl)).Cast<GameControl>())
            {
                UpdateButtonText(item);
            }
        }

        private void UpdateButtonText(GameControl controlToUpdate)
        {
            Dictionary<GameControl, string> propertiesToUpdate = new Dictionary<GameControl, string>()
            {
                { GameControl.Ok, nameof(OkKeyboardText) },
                { GameControl.Cancel, nameof(CancelKeyboardText) },
                { GameControl.Menu, nameof(MenuKeyboardText) },
                { GameControl.Switch, nameof(SwitchKeyboardText) },
                { GameControl.Pageup, nameof(PageUpKeyboardText) },
                { GameControl.Pagedown, nameof(PageDownKeyboardText) },
                { GameControl.Camera, nameof(CameraKeyboardText) },
                { GameControl.Target, nameof(TargetKeyboardText) },
                { GameControl.Assist, nameof(AssistKeyboardText) },
                { GameControl.Start, nameof(StartKeyboardText) },
                { GameControl.Up, nameof(UpKeyboardText) },
                { GameControl.Down, nameof(DownKeyboardText) },
                { GameControl.Left, nameof(LeftKeyboardText) },
                { GameControl.Right, nameof(RightKeyboardText) },
            };

            Dictionary<GameControl, string> gamepadpropertiesToUpdate = new Dictionary<GameControl, string>()
            {
                { GameControl.Ok, nameof(OkControllerText) },
                { GameControl.Cancel, nameof(CancelControllerText) },
                { GameControl.Menu, nameof(MenuControllerText) },
                { GameControl.Switch, nameof(SwitchControllerText) },
                { GameControl.Pageup, nameof(PageUpControllerText) },
                { GameControl.Pagedown, nameof(PageDownControllerText) },
                { GameControl.Camera, nameof(CameraControllerText) },
                { GameControl.Target, nameof(TargetControllerText) },
                { GameControl.Assist, nameof(AssistControllerText) },
                { GameControl.Start, nameof(StartControllerText) },
                { GameControl.Up, nameof(UpControllerText) },
                { GameControl.Down, nameof(DownControllerText) },
                { GameControl.Left, nameof(LeftControllerText) },
                { GameControl.Right, nameof(RightControllerText) },
            };

            // use reflection to set the correct property based on the control that was updated

            PropertyInfo prop = this.GetType().GetProperty(propertiesToUpdate[controlToUpdate], BindingFlags.Public | BindingFlags.Instance);
            if (prop?.CanWrite == true && LoadedConfiguration.KeyboardInputs.ContainsKey(controlToUpdate))
            {
                prop.SetValue(this, LoadedConfiguration.KeyboardInputs[controlToUpdate]?.DisplayText, null);
            }

            prop = this.GetType().GetProperty(gamepadpropertiesToUpdate[controlToUpdate], BindingFlags.Public | BindingFlags.Instance);
            if (prop?.CanWrite == true && LoadedConfiguration.GamepadInputs.ContainsKey(controlToUpdate))
            {
                prop.SetValue(this, LoadedConfiguration.GamepadInputs[controlToUpdate]?.DisplayText, null);
            }

        }

        internal void StartCapturingKeyboardInput(GameControl controlToCapture)
        {
            IsCapturing = true;
            _controlToCapture = controlToCapture;
            _captureState = CaptureState.CaptureKeyboard;
        }

        internal void StartCapturingControllerInput(GameControl controlToCapture)
        {
            IsCapturing = true;
            _controlToCapture = controlToCapture;
            _captureState = CaptureState.CaptureController;
            PollForGamePadInput();
        }

        internal GameController ConnectedController { get; set; }

        private Task PollForGamePadInput()
        {
            return Task.Factory.StartNew(() =>
            {

                if (ConnectedController == null)
                {
                    ConnectedController = new GameController();
                    ConnectedController.CreateDevice();
                }

                while (IsCapturing && _captureState == CaptureState.CaptureController)
                {
                    if (ConnectedController?.IsConnected == false)
                    {
                        Thread.Sleep(1000);
                        ConnectedController?.CreateDevice();
                        continue;
                    }

                    if (ConnectedController.ReadState() == null)
                    {
                        ConnectedController?.ReleaseDevice();
                        continue;
                    }


                    GamePadButton? pressedButton = ConnectedController.GetPressedButton();
                    if (pressedButton.HasValue)
                    {
                        if (ConnectedController.IsXInputDevice)
                        {
                            // convert game button press to the DInput equivalent
                            pressedButton = GameController.GetXInputToDInputButton(pressedButton.Value);
                        }

                        if (pressedButton == GamePadButton.Button11 || pressedButton == GamePadButton.Button12)
                        {
                            // ignore stick clicks for now (button 11/12 for DInput devices)
                            continue;
                        }

                        // if the user disabled dpad overrides then don't capture those buttons
                        if (!IsDpadSupportChecked && IsDpadButton(pressedButton.Value))
                        {
                            continue;
                        }

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            SetControlIfCapturing(pressedButton.Value);
                        });

                        break;
                    }
                }

            });
        }

        private bool IsDpadButton(GamePadButton button)
        {
            return button == GamePadButton.DPadUp ||
                   button == GamePadButton.DPadDown ||
                   button == GamePadButton.DPadLeft ||
                   button == GamePadButton.DPadRight;
        }

        internal void SaveNewCustomControl()
        {
            bool isValid = true;
            string title = ResourceHelper.Get(StringKey.SaveControlConfiguration);
            string prompt = ResourceHelper.Get(StringKey.SaveControlsPreset);
            string controlName;
            string pathToFile;

            do
            {
                isValid = true;
                InputTextWindow inputBox = new InputTextWindow(title, prompt);
                inputBox.ViewModel.MaxCharLength = 24;

                bool? dialogResult = inputBox.ShowDialog();
                if (!dialogResult.GetValueOrDefault(false))
                {
                    return;
                }

                controlName = inputBox.ViewModel.TextInput;

                if (string.IsNullOrEmpty(controlName))
                {
                    isValid = false;
                    MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ControlNameIsEmpty), ResourceHelper.Get(StringKey.SaveError), MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                if (Path.GetInvalidFileNameChars().Any(c => controlName.Contains(c)))
                {
                    isValid = false;
                    MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ControlNameContainsInvalidChars), ResourceHelper.Get(StringKey.SaveError), MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                // construct path and check if already exists if name is valid
                if (isValid)
                {
                    if (controlName.EndsWith(".cfg"))
                    {
                        controlName = controlName.Substring(0, controlName.Length - 4);
                    }

                    pathToFile = Path.Combine(Sys.PathToControlsFolder, $"{controlName}.cfg");

                    if (File.Exists(pathToFile))
                    {
                        isValid = false;
                        MessageDialogWindow.Show(ResourceHelper.Get(StringKey.ControlsWithThatNameAlreadyExist), ResourceHelper.Get(StringKey.SaveError), MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            } while (!isValid);

            try
            {
                string pathToDefaultFile = Path.Combine(Sys.PathToControlsFolder, InGameConfigurationMap[_defaultControlNames[0]]);

                ControlMapper.CopyConfigurationFileAndSaveAsNew(pathToDefaultFile, $"{controlName}.cfg", LoadedConfiguration);

                InitInGameConfigOptions();
                SelectedGameConfigOption = controlName;
                //StatusMessage = ResourceHelper.Get(StringKey.SuccessfullyCreatedCustomControls);

            }
            catch (Exception e)
            {
                Logger.Error(e);
                //StatusMessage = $"{ResourceHelper.Get(StringKey.FailedToCreateCustomControls)}: {e.Message}";
            }
        }

        internal void DeleteSelectedCustomControl()
        {
            if (!IsCustomConfigOptionSelected)
            {
                return;
            }

            string fileName = $"{SelectedGameConfigOption}.cfg";
            string pathToFile = Path.Combine(Sys.PathToControlsFolder, fileName);

            if (!File.Exists(pathToFile))
            {
                Logger.Warn($"Can not delete custom contols: no file found at {pathToFile}");
                return;
            }

            try
            {
                File.Delete(pathToFile);
                InitInGameConfigOptions();
                SelectedGameConfigOption = InGameConfigOptions[0];
                //StatusMessage = $"{ResourceHelper.Get(StringKey.SuccessfullyDeletedCustomControls)} {fileName}.";
            }
            catch (Exception e)
            {
                Logger.Error(e);
                //StatusMessage = $"{ResourceHelper.Get(StringKey.FailedToDeleteCustomControls)}: {e.Message}";
            }
        }

        /// <summary>
        /// Copies ff7input.cfg from FF7 game folder to ./Resources/Controls/ folder with the given <paramref name="customFileName"/>
        /// </summary>
        /// <param name="forceCopy"> copies ff7input.cfg if it already exists; overwriting the current custom.cfg </param>
        public bool CopyInputCfgToCustomCfg(bool forceCopy, string customFileName)
        {
            string pathToCustomCfg = Path.Combine(Sys.PathToControlsFolder, customFileName);
            string pathToInputCfg = Path.Combine(
                Sys.Settings.FF7InstalledVersion == FF7Version.Steam ? GameConverter.GetSteamFF7UserPath() : Path.GetDirectoryName(Sys.Settings.FF7Exe),
                "ff7input.cfg"
            );

            Directory.CreateDirectory(Sys.PathToControlsFolder);

            if (!File.Exists(pathToCustomCfg) || forceCopy)
            {
                if (File.Exists(pathToInputCfg))
                {
                    try
                    {
                        File.Copy(pathToInputCfg, pathToCustomCfg, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        //StatusMessage = $"{ResourceHelper.Get(StringKey.ErrorCopyingFf7InputCfg)} {ex.Message}";
                    }
                }
                else
                {
                    //StatusMessage = $"{ResourceHelper.Get(StringKey.NoFf7InputCfgFoundAt)} {pathToInputCfg}";
                    //Logger.Warn(StatusMessage);
                }
            }
            else
            {
                //StatusMessage = $"{customFileName} {ResourceHelper.Get(StringKey.AlreadyExistsAt)} {Sys.PathToControlsFolder}";
                //Logger.Warn(StatusMessage);
            }

            return false;
        }

        internal void SaveChangesToFile()
        {
            try
            {
                ControlMapper.SaveConfigurationToFile(Path.Combine(Sys.PathToControlsFolder, InGameConfigurationMap[SelectedGameConfigOption]), LoadedConfiguration);
                HasUnsavedChanges = false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                //StatusMessage = $"{ResourceHelper.Get(StringKey.FailedToDeleteCustomControls)}: {e.Message}";
            }
        }

        private void SetButtonIcon(string propertyToUpdate, GamePadButton? newButton)
        {
            Dictionary<GamePadButton, string> icons = new Dictionary<GamePadButton, string>()
            {
                { GamePadButton.Button1, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Square_X.png" },
                { GamePadButton.Button2, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Cross_A.png" },
                { GamePadButton.Button3, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Circle_B.png" },
                { GamePadButton.Button4, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Triangle_Y.png" },
                { GamePadButton.Button5, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/L1_LB.png" },
                { GamePadButton.Button6, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/R1_RB.png" },
                { GamePadButton.Button7, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/L2_LT.png" },
                { GamePadButton.Button8, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/R2_RT.png" },
                { GamePadButton.Button9, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Share_Back.png" },
                { GamePadButton.Button10, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Options_Start.png" },
                { GamePadButton.Button11, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XB_LS_Click.png" },
                { GamePadButton.Button12, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XB_RS_Click.png" },
                { GamePadButton.Button13, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/Home_Guide.png" },
                { GamePadButton.Up, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XB_LS_Up.png" },
                { GamePadButton.Down, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XB_LS_Down.png" },
                { GamePadButton.Left, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XB_LS_Left.png" },
                { GamePadButton.Right, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XB_LS_Right.png" },
                { GamePadButton.DPadUp, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XBOne_DPad_Up.png" },
                { GamePadButton.DPadDown, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XBOne_DPad_Down.png" },
                { GamePadButton.DPadLeft, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XBOne_DPad_Left.png" },
                { GamePadButton.DPadRight, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/XBOne_DPad_Right.png" },
            };

            Dictionary<GamePadButton, string> directionalIcons = new Dictionary<GamePadButton, string>()
            {
                { GamePadButton.Up, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/LS_DPad_Up.png" },
                { GamePadButton.Down, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/LS_DPad_Down.png" },
                { GamePadButton.Left, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/LS_DPad_Left.png" },
                { GamePadButton.Right, "/7th Heaven;component/Resources/Icons/PS_Xbox_Icons/LS_DPad_Right.png" },
            };


            PropertyInfo prop = this.GetType().GetProperty(propertyToUpdate, BindingFlags.Public | BindingFlags.Instance);
            if (newButton == null)
            {
                prop.SetValue(this, null, null);
                return;
            }


            GamePadButton button = newButton.Value;

            if (IsDpadSupportChecked && (button == GamePadButton.Up || button == GamePadButton.Down || button == GamePadButton.Left || button == GamePadButton.Right))
            {
                // check if dpad is binded to other controls; if not then display the image of the dpad/leftstick together
                if (LoadedConfiguration.IsButtonBinded(GamePadButton.DPadUp) || LoadedConfiguration.IsButtonBinded(GamePadButton.DPadDown) || LoadedConfiguration.IsButtonBinded(GamePadButton.DPadLeft) || LoadedConfiguration.IsButtonBinded(GamePadButton.DPadRight))
                {
                    prop.SetValue(this, icons[button], null);
                }
                else
                {
                    prop.SetValue(this, directionalIcons[button], null);
                }
            }
            else
            {
                prop.SetValue(this, icons[button], null);
            }
        }

        internal void LaunchControlPanelGameControllersWindow()
        {
            Process.Start(new ProcessStartInfo("control.exe")
            {
                Arguments = "joy.cpl",
                UseShellExecute = true,
            });
        }

        internal void TurnOffPs4Service()
        {
            DS4ControllerService.Instance.StopService();
        }

        internal void TurnOnPs4Service()
        {
            if (IsPs4SupportChecked)
            {
                if (!DS4ControllerService.IsScpDriverInstalled())
                {
                    // user must install the driver first before using ds4. prompt user driver will silently install then turn on
                    var messageResult = MessageDialogWindow.Show("The SCP Virtual Bus Driver is not installed. It is required to enable PS4 controller support.\n\nDo you want to install the driver?", "Missing Required Driver", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (messageResult.Result == MessageBoxResult.No)
                    {
                        IsPs4SupportChecked = false; // uncheck this since user does not have driver installed so it can't be supported
                        return;
                    }

                    InstallDriverAsync().ContinueWith((result) => 
                    {
                        if (IsPs4SupportChecked && DS4ControllerService.IsScpDriverInstalled())
                        {
                            DS4ControllerService.Instance.StartService();
                        }
                    });
                    return;
                }

                // just turn on service if driver installed
                DS4ControllerService.Instance.StartService();
            }
        }

        /// <summary>
        /// Run ScpDriver.exe on background task to install driver. Shows message and turns off ps4 controller support if fails.
        /// </summary>
        /// <returns></returns>
        private Task InstallDriverAsync()
        {
            IsInstallingDriver = true;
            return Task.Factory.StartNew(() =>
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(Sys.PathToScpDriverExe, "si")
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Sys.PathToVBusDriver // ensure the working directory is where the .exe is located
                };
                Process.Start(startInfo);

                // wait for the .exe driver install to finish
                while (Process.GetProcessesByName("ScpDriver").Length > 0)
                {
                    Thread.Sleep(100);
                }

                // check for installation by looking at log file
                bool installSuccess = false;

                if (File.Exists(Path.Combine(Sys.PathToVBusDriver, "ScpDriver.log")))
                {
                    string logContents = File.ReadAllText(Path.Combine(Sys.PathToVBusDriver, "ScpDriver.log"));
                    installSuccess = logContents.Contains("Install Succeeded");
                }

                // show message to user if issues with install or detecting
                if (installSuccess && !DS4ControllerService.IsScpDriverInstalled())
                {
                    MessageDialogWindow.Show("The SCP driver finished installing but could not be detected yet. Wait 5-10 seconds and try again (you may need to reboot for Windows to notice the driver.)", "Driver Installed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsPs4SupportChecked = false; // uncheck so user can toggle back on in a few seconds
                }
                else if (!installSuccess)
                {
                    MessageDialogWindow.Show("The SCP driver failed to install.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsPs4SupportChecked = false; // uncheck so user can toggle back on in a few seconds to try again
                }

                IsInstallingDriver = false;
            });
        }
    }
}
