using System.Windows;
using System.Windows.Media;

namespace SeventhHeaven.Classes.Themes
{
    public enum AppTheme
    {
        Custom,
        DarkMode,
        DarkModeWithBackground,
        LightMode,
        LightModeWithBackground,
        Classic7H,
        Tsunamods,
        SeventhHeavenTheme
    }

    public interface ITheme
    {
        string Name { get; }
        string PrimaryAppBackground { get; }
        string SecondaryAppBackground { get; }
        string PrimaryControlBackground { get; }
        string PrimaryControlForeground { get; }
        string PrimaryControlSecondary { get; }
        string PrimaryControlPressed { get; }
        string PrimaryControlMouseOver { get; }
        string PrimaryControlDisabledBackground { get; }
        string PrimaryControlDisabledForeground { get; }

        string BackgroundImageName { get; }
        string BackgroundImageBase64 { get; }
        HorizontalAlignment BackgroundHorizontalAlignment { get; }
        VerticalAlignment BackgroundVerticalAlignment { get; }
        Stretch BackgroundStretch { get; }

    }
}
