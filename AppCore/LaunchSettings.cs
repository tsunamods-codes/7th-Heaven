using System;
using System.Collections.Generic;
using System.Linq;

namespace AppCore
{

    [Serializable]
    public class LaunchSettings
    {
        public bool AutoUpdateDiscPath { get; set; }

        public bool DisableReunionOnLaunch { get; set; }

        public bool ReverseSpeakers { get; set; }
        public bool LogarithmicVolumeControl { get; set; }
        public Guid SelectedSoundDevice { get; set; }
        public string SelectedMidiData { get; set; }

        public bool ShowLauncherWindow { get; set; }

        public bool HasDisplayedOggMusicWarning { get; set; }
        public bool HasDisplayedMovieWarning { get; set; }

        public bool EnablePs4ControllerService { get; set; }

        public string SelectedGameLanguage { get; set; }

        /// <summary>
        /// True means that the launcher will poll for input from a gamepad to intercept trigger/dpad presses
        /// </summary>
        public bool EnableGamepadPolling { get; set; }



        /// <summary>
        /// File name of the ff7input.cfg file to copy to ff7 game dir 
        /// e.g. "stock game.cfg" or "custom.cfg"
        /// </summary>
        public string InGameConfigOption { get; set; }

        public static LaunchSettings DefaultSettings()
        {
            return new LaunchSettings()
            {
                AutoUpdateDiscPath = true,
                DisableReunionOnLaunch = true,
                SelectedSoundDevice = Guid.Empty,
                ReverseSpeakers = false,
                LogarithmicVolumeControl = true,
                SelectedMidiData = "GENERAL_MIDI",
                ShowLauncherWindow = true,
                InGameConfigOption = "[Default] Steam KB+PlayStation (Stock).cfg",
                HasDisplayedOggMusicWarning = false,
                HasDisplayedMovieWarning = false,
                EnablePs4ControllerService = false,
                SelectedGameLanguage = GameLanguage.English,
                EnableGamepadPolling = false,
            };
        }
    }

    public static class GameLanguage
    {
        public const string English = "en";
        public const string French = "fr";
        public const string German = "de";
        public const string Spanish = "es";
        public const string Japanese = "ja";
        public const string Any = "ANY";

        public static bool BypassLanguageCompatibility { get; set; }

        public static string Normalize(string language)
        {
            switch (language?.Trim().ToLowerInvariant())
            {
                case "fr": return French;
                case "de": return German;
                case "es": return Spanish;
                case "ja":
                case "ff7_ja": return Japanese;
                case "en":
                case "ff7_en":
                default: return English;
            }
        }

        public static int GetFFNxLanguageId(string language)
        {
            switch (Normalize(language))
            {
                case French: return 2;
                case German: return 3;
                case Spanish: return 4;
                case Japanese: return 6;
                default: return 1;
            }
        }

        public static string ToModXmlCode(string language)
        {
            return Normalize(language).ToUpperInvariant();
        }

        public static List<string> ParseSupportedLanguages(IEnumerable<string> languageNodes)
        {
            return AppWrapper.GameLanguageParser.ParseSupportedLanguages(languageNodes);
        }

        public static bool IsSupportedBy(IEnumerable<string> supportedLanguages, string language)
        {
            if (BypassLanguageCompatibility)
            {
                return true;
            }

            string selectedLanguage = ToModXmlCode(language);
            if (supportedLanguages == null || !supportedLanguages.Any())
            {
                return selectedLanguage == "EN";
            }

            return supportedLanguages.Any(supportedLanguage => MatchesLanguage(supportedLanguage, selectedLanguage));
        }

        public static bool MatchesLanguage(string language, string selectedLanguage)
        {
            string normalizedSelectedLanguage = ToModXmlCode(selectedLanguage);
            string normalizedLanguage = language?.Trim();
            return string.Equals(normalizedLanguage, Any, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(normalizedLanguage, "*", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(normalizedLanguage, normalizedSelectedLanguage, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
