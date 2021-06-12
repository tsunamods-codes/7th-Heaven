using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SeventhHeaven.Classes.Themes
{
    public class ThemeSettings : ITheme
    {
        public string Name { get; set; } // "Dark Mode", "Light Mode", "Custom"

        public string PrimaryAppBackground { get; set; }
        public string SecondaryAppBackground { get; set; }
        public string PrimaryControlBackground { get; set; }
        public string PrimaryControlForeground { get; set; }
        public string PrimaryControlPressed { get; set; }
        public string PrimaryControlMouseOver { get; set; }
        public string PrimaryControlDisabledBackground { get; set; }
        public string PrimaryControlDisabledForeground { get; set; }
        public string BackgroundImageName { get; set; }
        public string BackgroundImageBase64 { get; set; }


        public static string ColorToHexString(Color color)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.A, color.R, color.G, color.B);
        }

        public static ITheme GetThemeFromEnum(AppTheme appTheme)
        {
            switch (appTheme)
            {
                case AppTheme.Classic7H:
                    return new Classic7HTheme();

                case AppTheme.DarkMode:
                    return new DarkTheme();

                case AppTheme.DarkModeWithBackground:
                    return new DarkThemeWithBackground();

                case AppTheme.LightMode:
                    return new LightTheme();

                case AppTheme.LightModeWithBackground:
                    return new LightThemeWithBackground();

                case AppTheme.Custom:
                    return new ThemeSettings();

                case AppTheme.Tsunamods:
                    return new TsunamodsTheme();

                default:
                    return null;
            }
        }
    }
}
