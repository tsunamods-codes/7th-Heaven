using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Tsunamods
    }

    public interface ITheme
    {
        string Name { get; }
        string PrimaryAppBackground { get; }
        string SecondaryAppBackground { get; }
        string PrimaryControlBackground { get; }
        string PrimaryControlForeground { get; }
        string PrimaryControlPressed { get; }
        string PrimaryControlMouseOver { get; }
        string PrimaryControlDisabledBackground { get; }
        string PrimaryControlDisabledForeground { get; }

        string BackgroundImageName { get; }

        string BackgroundImageBase64 { get; }

    }
}
