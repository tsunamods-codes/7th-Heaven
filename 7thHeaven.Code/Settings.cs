/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using _7thHeaven.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Iros._7th.Workshop {


    public enum GeneralOptions {
        None = 0,
        KeepOldVersions,
        AutoActiveNewMods,
        AutoImportMods,
        CheckForUpdates,
        BypassCompatibility,
        OpenIrosLinksWith7H,
        OpenModFilesWith7H,
        Show7HInFileExplorerContextMenu,
        WarnAboutModCode,
        AutoUpdateMods
    }

    public enum AppUpdateChannelOptions
    {
        Stable = 0,
        Canary
    }

    public enum FFNxUpdateChannelOptions
    {
        Stable = 0,
        Canary
    }

    [Flags]
    public enum InterfaceOptions {
        None = 0,
        ProfileCollapse = 0x1,
    }

    public class SavedWindow {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public WindowState State { get; set; }
    }

    public class Subscription {
        public DateTime LastSuccessfulCheck { get; set; }
        public int FailureCount { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }

    }

    public class ProgramLaunchInfo
    {
        public string PathToProgram { get; set; }
        public string ProgramArgs { get; set; }
    }

    public class Settings {

        public IEnumerable<string> VerifySettings() {
            bool validexe = System.IO.File.Exists(FF7Exe);
            if (!validexe) yield return "FF7 Exe: " + FF7Exe + " not found";
            foreach (var al in ProgramsToLaunchPrior.Where(s => !String.IsNullOrWhiteSpace(s.PathToProgram)))
                if (!System.IO.File.Exists(al.PathToProgram)) yield return "AlsoLaunch: " + al.PathToProgram + " not found";
            if (!System.IO.Directory.Exists(MovieFolder)) yield return "Movie Folder: " + MovieFolder + " not found";
            if (!System.IO.Directory.Exists(AaliFolder)) yield return "Texture Path: " + AaliFolder + " not found";
            if (validexe) {
                string ff7folder = System.IO.Path.GetDirectoryName(FF7Exe);
                foreach (string extra in ExtraFolders.Where(s => !String.IsNullOrWhiteSpace(s))) {
                    string path = System.IO.Path.Combine(ff7folder, extra);
                    if (!System.IO.Directory.Exists(path)) yield return "Extra Folder: " + path + " not found";
                }
            }
        }


        public List<string> SubscribedUrls
        {
            get
            {
                return Subscriptions?.Select(s => s.Url).ToList();
            }
        }

        public List<string> ExtraFolders { get; set; }
        public List<Subscription> Subscriptions { get; set; }

        public string LibraryLocation { get; set; }
        
        public string FF7Exe { get; set; }
        [System.Xml.Serialization.XmlElement("AlsoLaunch")]
        public List<ProgramLaunchInfo> ProgramsToLaunchPrior { get; set; }
        public string AaliFolder { get; set; }
        public string MovieFolder { get; set; }
        public FFNxUpdateChannelOptions FFNxUpdateChannel { get; set; }

        public DateTime LastUpdateCheck { get; set; }
        public List<GeneralOptions> Options { get; set; }
        public InterfaceOptions IntOptions { get; set; }
        public string CurrentProfile { get; set; }
        public SavedWindow MainWindow { get; set; }
        public string AutoUpdateSource { get; set; }
        public decimal AutoUpdateOffered { get; set; }

        public string DateTimeStringFormat { get; set; }

        /// <summary>
        /// Flag to determine if the app is being launched for the first time.
        /// </summary>
        public bool IsFirstStart { get; set; }

        public LaunchSettings GameLaunchSettings { get; set; }

        public ColumnSettings UserColumnSettings { get; set; }

        public Settings() {
            ExtraFolders = new List<string>();
            ProgramsToLaunchPrior = new List<ProgramLaunchInfo>();
            Subscriptions = new List<Subscription>();
            Options = new List<GeneralOptions>();
            AutoUpdateSource = "#F!yBlHTYiJ!SFpmT2xII7iXcgXAmNYLJg";
            DateTimeStringFormat = "MM/dd/yyyy";
            IsFirstStart = false;
            GameLaunchSettings = LaunchSettings.DefaultSettings();
            UserColumnSettings = new ColumnSettings();
        }

        public bool HasOption(GeneralOptions option)
        {
            return Options != null && Options.Any(o => o == option);
        }

        public void RemoveOption(GeneralOptions option)
        {
            if (Options.Contains(option))
            {
                Options.Remove(option);
            }
        }

        public static Settings UseDefaultSettings()
        {
            Settings defaultSettings = new Settings();

            defaultSettings.Options.Add(GeneralOptions.AutoImportMods);
            defaultSettings.Options.Add(GeneralOptions.AutoActiveNewMods);
            defaultSettings.Options.Add(GeneralOptions.WarnAboutModCode);
            defaultSettings.Options.Add(GeneralOptions.OpenIrosLinksWith7H);
            defaultSettings.Options.Add(GeneralOptions.OpenModFilesWith7H);
            defaultSettings.Options.Add(GeneralOptions.CheckForUpdates);

            defaultSettings.Subscriptions.Add(new Subscription() { Url = "iros://Url/http$qhimm.7thheaven.rocks/catalog", Name = "Qhimm Catalog" });
            defaultSettings.Subscriptions.Add(new Subscription() { Url = "iros://Url/http$pastebin.com/raw.php?i=vXc4PpBS", Name = "Sega Catalog" });

            defaultSettings.ExtraFolders.Add("direct");
            defaultSettings.ExtraFolders.Add("music");
            defaultSettings.ExtraFolders.Add("sfx");
            defaultSettings.ExtraFolders.Add("voice");
            defaultSettings.ExtraFolders.Add("ambient");

            defaultSettings.FFNxUpdateChannel = FFNxUpdateChannelOptions.Stable;

            defaultSettings.UserColumnSettings = ColumnSettings.GetDefaultSettings();

            return defaultSettings;
        }

        /// <summary>
        /// Sets the default paths for an FF7 Install i.e 'mods/Textures', 'mods/7th Heaven', 'data/movies'
        /// and creates the folders if they do not exist. Default extra folders like 'direct' and 'music' are also created
        /// </summary>
        /// <param name="pathToFf7Install"></param>
        public void SetPathsFromInstallationPath(string pathToFf7Install)
        {
            FF7Exe = Path.Combine(pathToFf7Install, "FF7.exe");
            AaliFolder = Path.Combine(pathToFf7Install, "mods", @"Textures");
            LibraryLocation = Path.Combine(pathToFf7Install, "mods", @"7th Heaven");
            MovieFolder = Path.Combine(pathToFf7Install, "data", @"movies");

            LogAndCreateFolderIfNotExists(LibraryLocation);
            LogAndCreateFolderIfNotExists(MovieFolder);
            LogAndCreateFolderIfNotExists(AaliFolder);

            foreach (string folder in Settings.UseDefaultSettings().ExtraFolders)
            {
                LogAndCreateFolderIfNotExists(Path.Combine(pathToFf7Install, folder));
            }
        }

        private static void LogAndCreateFolderIfNotExists(string pathToFolder)
        {
            if (!Directory.Exists(pathToFolder))
            {
                Sys.Message(new WMessage($"directory missing. creating {pathToFolder}", WMessageLogLevel.LogOnly));
                Directory.CreateDirectory(pathToFolder);
            }
        }
    }
}
