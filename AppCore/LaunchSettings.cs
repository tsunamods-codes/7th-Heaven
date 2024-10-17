using System;

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
                EnableGamepadPolling = false,
            };
        }
    }
}
