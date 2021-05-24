using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
using SeventhHeaven.Classes.Themes;
using SeventhHeavenUI;
using SeventhHeavenUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SeventhHeaven.ViewModels
{
    public class ThemeSettingsViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void OnImageChanged(byte[] newImage);
        public event OnImageChanged BackgroundImageChanged;

        private string _statusText;
        private string _selectedThemeText;
        private string _appBackgroundText;
        private string _secondaryBackgroundText;
        private string _controlDisabledBgText;
        private string _controlDisabledFgText;
        private string _controlBackgroundText;
        private string _controlForegroundText;
        private string _controlMouseOverText;
        private string _controlPressedText;
        private string _backgroundImageText;
        private byte[] currentImageTheme;

        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsCustomThemeEnabled
        {
            get
            {
                return SelectedThemeText == "Custom";
            }
        }

        public string SelectedThemeText
        {
            get
            {
                return _selectedThemeText;
            }
            set
            {
                _selectedThemeText = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsCustomThemeEnabled));
                ChangeTheme();
            }
        }

        public List<string> ThemeDropdownItems
        {
            get
            {
                return DropDownOptionEnums.Keys.ToList();
            }
        }

        public string AppBackgroundText
        {
            get
            {
                return _appBackgroundText;
            }
            set
            {
                _appBackgroundText = value;
                NotifyPropertyChanged();
            }
        }

        public string SecondaryBackgroundText
        {
            get
            {
                return _secondaryBackgroundText;
            }
            set
            {
                _secondaryBackgroundText = value;
                NotifyPropertyChanged();
            }
        }

        public string ControlDisabledBgText
        {
            get
            {
                return _controlDisabledBgText;
            }
            set
            {
                _controlDisabledBgText = value;
                NotifyPropertyChanged();
            }
        }

        public string ControlDisabledFgText
        {
            get
            {
                return _controlDisabledFgText;
            }
            set
            {
                _controlDisabledFgText = value;
                NotifyPropertyChanged();
            }
        }

        public string ControlBackgroundText
        {
            get
            {
                return _controlBackgroundText;
            }
            set
            {
                _controlBackgroundText = value;
                NotifyPropertyChanged();
            }
        }

        public string ControlForegroundText
        {
            get
            {
                return _controlForegroundText;
            }
            set
            {
                _controlForegroundText = value;
                NotifyPropertyChanged();
            }
        }

        public string ControlMouseOverText
        {
            get
            {
                return _controlMouseOverText;
            }
            set
            {
                _controlMouseOverText = value;
                NotifyPropertyChanged();
            }
        }

        public string ControlPressedText
        {
            get
            {
                return _controlPressedText;
            }
            set
            {
                _controlPressedText = value;
                NotifyPropertyChanged();
            }
        }

        public string BackgroundImageText
        {
            get
            {
                return _backgroundImageText;
            }
            set
            {
                _backgroundImageText = value;
                NotifyPropertyChanged();
            }
        }

        public byte[] CurrentImageTheme 
        { 
            get => currentImageTheme;
            set
            {
                currentImageTheme = value;
                BackgroundImageChanged?.Invoke(currentImageTheme);
            }
        }

        public Dictionary<string, AppTheme> DropDownOptionEnums
        {
            get
            {
                return new Dictionary<string, AppTheme>
                {
                    { "Tsunamods", AppTheme.Tsunamods },
                    { "Dark Mode", AppTheme.DarkMode },
                    { "Dark Mode w/ Background", AppTheme.DarkModeWithBackground },
                    { "Light Mode", AppTheme.LightMode },
                    { "Light Mode w/ Background", AppTheme.LightModeWithBackground},
                    { "Classic 7H", AppTheme.Classic7H },
                    { "Custom", AppTheme.Custom },
                };
            }
        }

        public ThemeSettingsViewModel()
        {
            StatusText = "";
            SelectedThemeText = GetSavedThemeName();
        }

        public ThemeSettingsViewModel(bool loadThemeXml)
        {
            StatusText = "";

            if (loadThemeXml)
            {
                SelectedThemeText = GetSavedThemeName(); // this will trigger ChangeTheme()
            }
        }

        /// <summary>
        /// imports the theme.xml file and sets the app color brushes.
        /// </summary>
        internal static void LoadThemeFromFile()
        {
            string pathToThemeFile = Path.Combine(Sys.SysFolder, "theme.xml");

            // dark theme w/ background will be applied as the default when theme.xml file does not exist
            if (!File.Exists(pathToThemeFile))
            {
                new ThemeSettingsViewModel(loadThemeXml: false).ApplyBuiltInTheme(AppTheme.Tsunamods);
                return;
            }

            ImportTheme(pathToThemeFile);
        }

        /// <summary>
        /// Returns ITheme loaded from theme.xml file. Dark Theme w/ Background will be returned if theme.xml file does not exist or fail to deserialize.
        /// </summary>
        internal static ITheme GetThemeSettingsFromFile()
        {
            string pathToThemeFile = Path.Combine(Sys.SysFolder, "theme.xml");

            if (!File.Exists(pathToThemeFile))
            {
                return ThemeSettings.GetThemeFromEnum(AppTheme.Tsunamods);
            }

            try
            {
                ThemeSettings theme = Util.Deserialize<ThemeSettings>(pathToThemeFile);
                return theme;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return ThemeSettings.GetThemeFromEnum(AppTheme.Tsunamods);
            }
        }

        /// <summary>
        /// Reads theme .xml and sets App Brush resources
        /// </summary>
        /// <param name="themeFile"></param>
        internal static void ImportTheme(string themeFile)
        {
            try
            {
                ThemeSettings theme = Util.Deserialize<ThemeSettings>(themeFile);
                ThemeSettingsViewModel settingsViewModel = new ThemeSettingsViewModel(loadThemeXml: false);

                settingsViewModel.DropDownOptionEnums.TryGetValue(theme.Name, out AppTheme appTheme);

                if (appTheme == AppTheme.Custom)
                {
                    settingsViewModel.ApplyThemeFromFile(themeFile);
                    return;
                }
                else
                {
                    settingsViewModel.ApplyBuiltInTheme(appTheme);
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        /// <summary>
        /// Reads theme.xml file and returns Name of theme ("Dark Mode", "Light Mode", or "Custom")
        /// </summary>
        /// <returns></returns>
        internal static string GetSavedThemeName()
        {
            try
            {
                string pathToThemeFile = Path.Combine(Sys.SysFolder, "theme.xml");

                if (!File.Exists(pathToThemeFile))
                {
                    Logger.Warn("theme.xml does not exist");
                    return "Dark Mode w/ Background";
                }

                ThemeSettings savedTheme = Util.Deserialize<ThemeSettings>(pathToThemeFile);
                return savedTheme.Name;
            }
            catch (Exception e)
            {
                Logger.Warn(e);
            }

            return "Dark Mode w/ Background";
        }

        /// <summary>
        /// saves current colors to theme.xml
        /// </summary>
        internal void SaveTheme()
        {
            string pathToThemeFile = Path.Combine(Sys.SysFolder, "theme.xml");
            SaveTheme(pathToThemeFile);
            StatusText = ResourceHelper.Get(StringKey.ThemeSaved);
        }

        internal void SaveTheme(string pathToTheme)
        {
            ThemeSettings settings = new ThemeSettings()
            {
                Name = SelectedThemeText,
                BackgroundImageName = BackgroundImageText
            };

            if (settings.Name == "Custom")
            {
                settings.PrimaryAppBackground = AppBackgroundText;
                settings.SecondaryAppBackground = SecondaryBackgroundText;
                settings.PrimaryControlDisabledBackground = ControlDisabledBgText;
                settings.PrimaryControlDisabledForeground = ControlDisabledFgText;
                settings.PrimaryControlBackground = ControlBackgroundText;
                settings.PrimaryControlForeground = ControlForegroundText;
                settings.PrimaryControlMouseOver = ControlMouseOverText;
                settings.PrimaryControlPressed = ControlPressedText;
            }




            try
            {
                if (CurrentImageTheme?.Length > 0)
                {
                    settings.BackgroundImageBase64 = Convert.ToBase64String(CurrentImageTheme);
                }

                using (FileStream file = new FileStream(pathToTheme, FileMode.Create, FileAccess.ReadWrite))
                {
                    Util.Serialize(settings, file);
                }

                StatusText = $"{ResourceHelper.Get(StringKey.ThemeSavedAs)} {Path.GetFileName(pathToTheme)}";
            }
            catch (Exception e)
            {
                Logger.Error(e);
                StatusText = ResourceHelper.Get(StringKey.FailedToSaveTheme);
            }

        }

        internal void ChangeTheme()
        {
            DropDownOptionEnums.TryGetValue(SelectedThemeText, out AppTheme selectedTheme);

            if (selectedTheme == AppTheme.Custom)
            {
                InitColorTextInput();
                UpdateAppBrushesAndColors();
            }
            else
            {
                ApplyBuiltInTheme(selectedTheme);
            }
        }

        internal void ApplyBuiltInTheme(AppTheme selectedTheme)
        {
            ITheme theme = ThemeSettings.GetThemeFromEnum(selectedTheme);

            AppBackgroundText = theme.PrimaryAppBackground;
            SecondaryBackgroundText = theme.SecondaryAppBackground;
            ControlBackgroundText = theme.PrimaryControlBackground;
            ControlForegroundText = theme.PrimaryControlForeground;
            ControlPressedText = theme.PrimaryControlPressed;
            ControlMouseOverText = theme.PrimaryControlMouseOver;
            ControlDisabledBgText = theme.PrimaryControlDisabledBackground;
            ControlDisabledFgText = theme.PrimaryControlDisabledForeground;
            BackgroundImageText = theme.BackgroundImageName;

            if (!string.IsNullOrEmpty(theme.BackgroundImageBase64))
            {
                try
                {
                    CurrentImageTheme = Convert.FromBase64String(theme.BackgroundImageBase64);
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    CurrentImageTheme = null;
                }
            }
            else
            {
                CurrentImageTheme = null;
            }

            UpdateAppBrushesAndColors();
        }

        /// <summary>
        /// Updates App brush resources based on properties in view model (e.g. <see cref="AppBackgroundText"/>, <see cref="ControlBackgroundText"/>, etc...)
        /// </summary>
        internal void UpdateAppBrushesAndColors()
        {
            try
            {
                App.Current.Resources["PrimaryAppBackground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(AppBackgroundText));
                App.Current.Resources["SecondaryAppBackground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(SecondaryBackgroundText));

                App.Current.Resources["PrimaryControlBackground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlBackgroundText));
                App.Current.Resources["PrimaryControlForeground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlForegroundText));
                App.Current.Resources["PrimaryControlPressed"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlPressedText));
                App.Current.Resources["PrimaryControlMouseOver"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlMouseOverText));

                App.Current.Resources["PrimaryControlDisabledBackground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlDisabledBgText));
                App.Current.Resources["PrimaryControlDisabledForeground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlDisabledFgText));

                App.Current.Resources["iconColorBrush"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(ControlForegroundText));

                App.Current.Resources["PrimaryControlPressedColor"] = ((SolidColorBrush)(new BrushConverter().ConvertFrom(ControlPressedText))).Color;
                App.Current.Resources["PrimaryControlMouseOverColor"] = ((SolidColorBrush)(new BrushConverter().ConvertFrom(ControlMouseOverText))).Color;
            }
            catch (Exception e)
            {
                Logger.Warn(e);
                StatusText = $"Failed to apply theme: {e.Message}";
            }
        }

        /// <summary>
        /// Updates app brush resources from valid theme .xml file
        /// </summary>
        /// <param name="themeFile"></param>
        internal void ApplyThemeFromFile(string themeFile)
        {
            try
            {
                ThemeSettings theme = Util.Deserialize<ThemeSettings>(themeFile);

                AppBackgroundText = theme.PrimaryAppBackground;
                SecondaryBackgroundText = theme.SecondaryAppBackground;
                ControlBackgroundText = theme.PrimaryControlBackground;
                ControlForegroundText = theme.PrimaryControlForeground;
                ControlPressedText = theme.PrimaryControlPressed;
                ControlMouseOverText = theme.PrimaryControlMouseOver;
                ControlDisabledBgText = theme.PrimaryControlDisabledBackground;
                ControlDisabledFgText = theme.PrimaryControlDisabledForeground;
                BackgroundImageText = theme.BackgroundImageName;

                if (!string.IsNullOrEmpty(theme.BackgroundImageBase64))
                {
                    try
                    {
                        CurrentImageTheme = Convert.FromBase64String(theme.BackgroundImageBase64);
                    }
                    catch (Exception e)
                    {
                        Logger.Warn(e);
                        CurrentImageTheme = null;
                    }
                }
                else
                {
                    CurrentImageTheme = null;
                }

                UpdateAppBrushesAndColors();
                SetThemeToCustom();

                StatusText = ResourceHelper.Get(StringKey.ThemeLoadedClickSavveToSaveThis);
            }
            catch (Exception e)
            {
                Logger.Warn(e);
                StatusText = $"{ResourceHelper.Get(StringKey.FailedToLoadTheme)}: {e.Message}";
            }
        }

        /// <summary>
        /// Reads theme.xml file and sets properties in view model
        /// </summary>
        private void InitColorTextInput()
        {
            ThemeSettings savedTheme = null;

            try
            {
                string pathToThemeFile = Path.Combine(Sys.SysFolder, "theme.xml");

                if (!File.Exists(pathToThemeFile))
                {
                    savedTheme = ThemeSettings.GetThemeFromEnum(AppTheme.Tsunamods) as ThemeSettings;
                }
                else
                {
                    savedTheme = Util.Deserialize<ThemeSettings>(pathToThemeFile);
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e);
                return;
            }

            if (savedTheme == null || savedTheme?.Name != "Custom")
            {
                return;
            }

            AppBackgroundText = savedTheme.PrimaryAppBackground;
            SecondaryBackgroundText = savedTheme.SecondaryAppBackground;
            ControlBackgroundText = savedTheme.PrimaryControlBackground;
            ControlForegroundText = savedTheme.PrimaryControlForeground;
            ControlPressedText = savedTheme.PrimaryControlPressed;
            ControlMouseOverText = savedTheme.PrimaryControlMouseOver;
            ControlDisabledBgText = savedTheme.PrimaryControlDisabledBackground;
            ControlDisabledFgText = savedTheme.PrimaryControlDisabledForeground;
            BackgroundImageText = savedTheme.BackgroundImageName;

            if (!string.IsNullOrEmpty(savedTheme.BackgroundImageBase64))
            {
                try
                {
                    CurrentImageTheme = Convert.FromBase64String(savedTheme.BackgroundImageBase64);
                }
                catch (Exception e)
                {
                    Logger.Warn(e);
                    CurrentImageTheme = null;
                }
            }
            else
            {
                CurrentImageTheme = null;
            }
        }

        /// <summary>
        /// Use Reflection to set the property to a new color
        /// </summary>
        /// <param name="propertyName"> property to update e.g. 'ControlForegroundText' </param>
        /// <param name="newValue"> new color to use </param>
        internal void ColorChanged(string propertyName, Color? newValue)
        {
            if (newValue == null)
            {
                return;
            }

            string hexValue = ThemeSettings.ColorToHexString(newValue.Value);

            PropertyInfo propInfo = typeof(ThemeSettingsViewModel).GetProperty(propertyName);
            propInfo.SetValue(this, hexValue);

            UpdateAppBrushesAndColors();
        }

        internal void SelectBackgroundImage()
        {
            string pathToFile = FileDialogHelper.BrowseForFile("*.png,*.jpg,*.jpeg|*.png;*.jpg;*.jpeg", ResourceHelper.Get(StringKey.SelectBackgroundImageFile));

            if (!string.IsNullOrEmpty(pathToFile))
            {
                CurrentImageTheme = File.ReadAllBytes(pathToFile);
                BackgroundImageText = Path.GetFileName(pathToFile);
                SetThemeToCustom();
            }
        }

        /// <summary>
        /// Sets dropdown theme option to 'Custom' without firing <see cref="ChangeTheme()"/>
        /// </summary>
        internal void SetThemeToCustom()
        {
            _selectedThemeText = "Custom";
            NotifyPropertyChanged(nameof(SelectedThemeText));
            NotifyPropertyChanged(nameof(IsCustomThemeEnabled));
        }
    }
}
