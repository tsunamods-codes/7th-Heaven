using _7thHeaven.Code;
using Iros._7th.Workshop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValveKeyValue;

namespace SeventhHeaven.Classes
{
    /// <summary>
    /// An instance of <see cref="GameConverter"/> is used to perform actions necessary to make FF7 compatible with mods.
    /// </summary>
    public class GameConverter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public delegate void OnMessageSent(string message, NLog.LogLevel logLevel);
        public event OnMessageSent MessageSent;

        public const string BackupFolderName = "7H2.0-BACKUP";

        public string InstallPath { get; set; }

        public GameConverter(string installPath)
        {
            InstallPath = installPath;
        }

        public static string GetInstallLocation(FF7Version installedVersion)
        {
            string installPath = "";

            switch (installedVersion)
            {
                case FF7Version.Steam:

                    // Detect if Steam is installed
                    string steamPath = GetSteamPath();

                    if (steamPath != null)
                    {
                        var stream = File.OpenRead(Path.Combine(steamPath, "steamapps\\libraryfolders.vdf"));
                        var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                        KVObject data = kv.Deserialize(stream);

                        // Look through multiple libraries
                        foreach (var section in data)
                        {
                            var libraryPath = section["path"].ToString().Replace("\\\\", "\\");

                            if (File.Exists(Path.Combine(libraryPath, "steamapps\\appmanifest_39140.acf")))
                            {
                                installPath = Path.Combine(libraryPath, "steamapps\\common\\FINAL FANTASY VII");
                            }
                        }
                    }

                    break;

                case FF7Version.ReRelease:
                    // check 32-bit then 64-bit registry if not exists
                    installPath = RegistryHelper.GetValue(RegistryHelper.RereleaseKeyPath32Bit, "InstallLocation", "") as string;

                    if (string.IsNullOrWhiteSpace(installPath))
                    {
                        installPath = RegistryHelper.GetValue(RegistryHelper.RereleaseKeyPath64Bit, "InstallLocation", "") as string;
                    }

                    break;

                case FF7Version.Original98:
                    installPath = RegistryHelper.GetValue(FF7RegKey.FF7AppKeyPath, "Path", "") as string;
                    break;
            }

            return installPath;
        }

        public static string GetSteamPath()
        {
            string ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath32Bit, "SteamPath", "") as string;

            if (ret == null) ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath64Bit, "SteamPath", "") as string;

            return ret.Replace("/","\\");
        }

        public static string GetSteamExePath()
        {
            string ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath32Bit, "SteamExe", "") as string;
            
            if (ret == null) ret = RegistryHelper.GetValue(RegistryHelper.SteamKeyPath64Bit, "SteamExe", "") as string;

            return ret.Replace("/","\\");
        }

        public static string GetSteamFF7UserPath()
        {
            string ret = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Square Enix\\FINAL FANTASY VII Steam";

            return ret;
        }

        public bool IsGamePirated()
        {
            string[] foldersToExclude = new string[] { "The_Reunion", "mods", "direct" }; // folders to skip in check

            // check all folders at root of InstallPath (excluding some)
            foreach (string subfolder in Directory.GetDirectories(InstallPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(subfolder);

                if (foldersToExclude.Any(f => dirInfo.Name.Equals(f, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue; // don't check these folders for signs of pirated files
                }

                bool isPirated = DirectoryHasPirates(subfolder);
                if (isPirated)
                {
                    return true;
                }
            }

            // check files at root of InstallPath
            foreach (string file in Directory.GetFiles(InstallPath))
            {
                bool isPirated = IsFileOrFolderAPirate(file);
                if (isPirated)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks all files, folders, and sub-folders for signs of pirated files 
        /// </summary>
        /// <param name="folderPath"> folder to loop over and check </param>
        private bool DirectoryHasPirates(string folderPath)
        {

            string[] filesToAllow = new string[] { "00422 [F - Crackling fire, looped].ogg" };

            foreach (string fileEntry in Directory.GetFileSystemEntries(folderPath, "*", SearchOption.AllDirectories))
            {
                FileInfo info = new FileInfo(fileEntry);

                if (fileEntry.IndexOf("torrent", StringComparison.InvariantCultureIgnoreCase) >= 0 && fileEntry.IndexOf("Reunion", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    continue; // allow Reunion torrent files
                }

                if (filesToAllow.Any(f => info.Name.Equals(f, StringComparison.InvariantCultureIgnoreCase)))
                {
                    continue; // this file has been marked as allowed by us so we skip the file
                }

                bool isPirated = IsFileOrFolderAPirate(fileEntry);

                if (isPirated)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if the given folder or file is pirated by checking against specific keywords.
        /// </summary>
        private bool IsFileOrFolderAPirate(string pathToFileOrFolder)
        {
            string[] pirateKeyWords = new string[] { "crack", "warez", "torrent", "skidrow", "goodies" }; // folders and keywords usually found in files when the game is pirated
            string[] pirateExtensions = new string[] { ".nfo" };                                          // file extensions that indicate the game could be pirated
            string[] pirateExactKeywords = new string[] { "ali213.ini", "rld.dll", "gameservices.dll" };  // files that indicate the game is pirated

            string name;
            string ext;

            if (Directory.Exists(pathToFileOrFolder))
            {
                name = new DirectoryInfo(pathToFileOrFolder).Name;
                ext = new DirectoryInfo(pathToFileOrFolder).Extension;
            }
            else
            {
                name = new FileInfo(pathToFileOrFolder).Name;
                ext = new FileInfo(pathToFileOrFolder).Extension;
            }


            if (pirateExactKeywords.Any(s => s.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }

            if (pirateExtensions.Any(s => s == ext))
            {
                return true;
            }

            if (pirateKeyWords.Any(s => pathToFileOrFolder.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0))
            {
                return true;
            }

            return false;
        }

        public bool IsGameLocatedInSystemFolders()
        {
            return FileUtils.IsLocatedInSystemFolders(InstallPath);
        }

        public bool CopyGame(string targetPath = @"C:\Games\Final Fantasy VII")
        {
            if (!Directory.Exists(InstallPath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(targetPath);
                FileUtils.CopyDirectoryRecursively(InstallPath, targetPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }

            return true;
        }

        #region Backup And Cleanup Related Methods

        internal bool BackupExe(string backupFolderPath)
        {
            Directory.CreateDirectory(backupFolderPath);

            string ff7ExePath = Path.Combine(InstallPath, "ff7.exe");

            try
            {
                if (File.Exists(ff7ExePath))
                {
                    File.Copy(ff7ExePath, Path.Combine(backupFolderPath, "ff7.exe"), true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        internal bool BackupFF7ConfigExe(string backupFolderPath)
        {
            string ff7ConfigPath = Path.Combine(InstallPath, "FF7Config.exe");

            try
            {
                if (File.Exists(ff7ConfigPath))
                {
                    Directory.CreateDirectory(backupFolderPath);
                    File.Copy(ff7ConfigPath, Path.Combine(backupFolderPath, "FF7Config.exe"), true);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Backup registry keys to .reg files in 'BackupGC2020' folder
        /// </summary>
        /// <param name="installPath"></param>
        public void BackupRegistry(string pathToBackup)
        {
            Directory.CreateDirectory(pathToBackup);

            if (Environment.Is64BitOperatingSystem)
            {
                // check which registry key exists for the steam release (could be in 32-bit or 64-bit area
                if (RegistryHelper.GetValue(RegistryHelper.SteamKeyPath64Bit, "InstallLocation") != null)
                {
                    RegistryHelper.ExportKey(RegistryHelper.SteamKeyPath64Bit, Path.Combine(pathToBackup, "FF7-01.reg"));
                }
                else
                {
                    RegistryHelper.ExportKey(RegistryHelper.SteamKeyPath32Bit, Path.Combine(pathToBackup, "FF7-01.reg"));
                }
            }

            RegistryHelper.ExportKey(RegistryHelper.GetKeyPath(FF7RegKey.RereleaseKeyPath), Path.Combine(pathToBackup, "FF7-02.reg"));
            RegistryHelper.ExportKey(RegistryHelper.GetKeyPath(FF7RegKey.FF7AppKeyPath), Path.Combine(pathToBackup, "FF7-03.reg"));

            string oldGameConverterKeyPath = $"{RegistryHelper.GetKeyPath(FF7RegKey.SquareSoftKeyPath)}\\Final Fantasy VII\\GameConverterkeys";
            RegistryHelper.ExportKey(oldGameConverterKeyPath, Path.Combine(pathToBackup, "FF7-OldGC.reg"));
        }

        public void MoveOriginalConverterFilesToBackup(string pathToBackup)
        {
            Directory.CreateDirectory(pathToBackup);

            List<string> foldersToMove = new List<string>() { "DLL_in", "Hext_in", "LOADR", "Multi_DLL", "FF7anyCDv2", "BackupGC" };

            foreach (string folder in foldersToMove)
            {
                string fullPath = Path.Combine(InstallPath, folder);
                if (Directory.Exists(fullPath))
                {
                    FileUtils.MoveDirectoryRecursively(fullPath, Path.Combine(pathToBackup, folder));
                    Directory.Delete(fullPath, true);
                }
            }
        }

        public void MoveOriginalAppFilesToBackup(string pathToBackup)
        {
            Directory.CreateDirectory(pathToBackup);

            List<string> filesToMove = new List<string>() { "app.log", "ff7.exe", "ff7config.exe", "RunFFVIIConfig.bat", "RunFFVIIConfig.exe", "ff7_mo.exe", "ff7_nt.exe", "ff7_ss.exe", "ff7_ss_safer.exe", "ff7_bc.exe", "ff7input.cfg", "Multi_Readme.txt", "cfg.log", "Hext.log", "FF7_GC.log", "eax.dll", "Hext.dll", "multi.dll", "ff7_opengl.cfg", "ff7_opengl.pdb", "ff7_opengl.reg", "crash.dmp", "ff7_opengl.fgd", @"\plugins\ff7music.fgp", @"\plugins\ffmpeg_movies.fgp", @"\plugins\vgmstream_music.fgp" };

            foreach (string file in filesToMove)
            {
                string fullPath = Path.Combine(InstallPath, file);
                if (File.Exists(fullPath))
                {
                    string destPath = Path.Combine(pathToBackup, file);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                    File.Move(fullPath, destPath);
                }
            }
        }

        /// <summary>
        /// Delete all cache files (S*D.P and T*D.P files) in <see cref="InstallPath"/>
        /// </summary>
        public bool DeleteCacheFiles()
        {
            if (!Directory.Exists(InstallPath))
            {
                return true;
            }

            try
            {
                foreach (string file in Directory.GetFiles(InstallPath, "S*D.P"))
                {
                    File.Delete(file);
                }

                foreach (string file in Directory.GetFiles(InstallPath, "T*D.P"))
                {
                    File.Delete(file);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }


        }

        #endregion

        public bool CopyFF7ExeToGame()
        {
            if (!Directory.Exists(InstallPath))
            {
                return false;
            }

            string ff7ExePath = Path.Combine(Sys.PathToPatchedExeFolder, "ff7.exe");

            try
            {
                File.Copy(ff7ExePath, Path.Combine(InstallPath, "ff7.exe"), true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public bool CopyFF7ConfigExeToGame()
        {
            if (!Directory.Exists(InstallPath))
            {
                return false;
            }

            string ff7ConfigPath = Path.Combine(Sys.PathToPatchedExeFolder, "FF7Config.exe");

            try
            {
                File.Copy(ff7ConfigPath, Path.Combine(InstallPath, "FF7Config.exe"), true);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public bool CopyGLDriversToGame()
        {
            string pathToPlugins = Path.Combine(InstallPath, "plugins");
            string pathToShaders = Path.Combine(InstallPath, "shaders");
            string sourceOpenGlFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenGL");

            if (!Directory.Exists(sourceOpenGlFolder) || !Directory.Exists(InstallPath))
            {
                return false;
            }

            try
            {
                // Create "plugins" folder in root install folder and copy all files under OpenGL driver\plugins folder to install folder.
                Directory.CreateDirectory(pathToPlugins);
                Directory.CreateDirectory(pathToShaders);

                FileUtils.CopyDirectoryRecursively(Path.Combine(sourceOpenGlFolder, "plugins"), pathToPlugins);
                FileUtils.CopyDirectoryRecursively(Path.Combine(sourceOpenGlFolder, "shaders"), pathToShaders);


                // Copy all ff7_opengl.* files to root install folder.
                foreach (string file in Directory.GetFiles(sourceOpenGlFolder, "ff7_opengl.*"))
                {
                    FileInfo info = new FileInfo(file);
                    File.Copy(file, Path.Combine(InstallPath, info.Name));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Verifies a FF7 install is a Full/Max install by checking if specific files are in the game dir.
        /// They will automatically be copied from discs if not found. 
        /// Returns false if failed to find/copy all files
        /// </summary>
        /// <returns> Returns true if all files found and/or copied; false otherwise </returns>
        public bool VerifyFullInstallation()
        {
            bool foundAllFiles = true;

            string[] expectedFiles = new string[]
            {
                @"wm\world_us.lgp",
                @"field\char.lgp",
                @"field\flevel.lgp",
                @"minigame\chocobo.lgp",
                @"minigame\coaster.lgp",
                @"minigame\condor.lgp",
                @"minigame\high-us.lgp",
                @"minigame\snowboard-us.lgp",
                @"minigame\sub.lgp"
            };

            string[] volumeLabels = new string[]
            {
                "ff7install",
                "ff7disc1",
                "ff7disc2",
                "ff7disc3"
            };

            foreach (string file in expectedFiles)
            {
                string fullTargetPath = Path.Combine(InstallPath, "data", file);

                if (File.Exists(fullTargetPath))
                {
                    // file already exists at install path so continue
                    continue;
                }

                SendMessage($"...\t {file} {ResourceHelper.Get(StringKey.NotFoundScanningDiscsForFiles)}", NLog.LogLevel.Warn);

                // search all drives for the file
                bool foundFileOnDrive = false;
                foreach (string label in volumeLabels)
                {
                    string driveLetter = GameLauncher.GetDriveLetter(label);

                    if (!string.IsNullOrWhiteSpace(driveLetter))
                    {
                        string fullSourcePath = Path.Combine(driveLetter, "ff7", file);
                        if (label == "ff7install")
                        {
                            fullSourcePath = Path.Combine(driveLetter, "data", file); // ff7install disc has a different path then disc1-3
                        }


                        if (File.Exists(fullSourcePath))
                        {
                            foundFileOnDrive = true;
                            SendMessage($"... \t {string.Format(ResourceHelper.Get(StringKey.FoundFileOnAtCopyingFile), label, driveLetter)}");
                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(fullTargetPath)); // ensure all subfolders are created
                                File.Copy(fullSourcePath, fullTargetPath, true);
                                SendMessage($"... \t\t {ResourceHelper.Get(StringKey.Copied)}!");
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e);
                                SendMessage($"... \t\t {ResourceHelper.Get(StringKey.FailedToCopyErrorHasBeenLogged)}", NLog.LogLevel.Warn);
                            }
                        }
                    }

                    if (foundFileOnDrive)
                    {
                        break; // done searching drives as file found/copied
                    }
                }

                // at this point if file not found/copied on any drive then failed verification
                if (!foundFileOnDrive)
                {
                    SendMessage($"... \t {string.Format(ResourceHelper.Get(StringKey.FailedToFindOnAnyDisc), file)}", NLog.LogLevel.Warn);
                    foundAllFiles = false;
                }
            }

            return foundAllFiles;
        }

        /// <summary>
        /// Verifies specific files exist in /data/[subfolder] where [subfolder] is battle, kernel, and movies.
        /// If files not found then they are copied from /data/lang-en/[subfolder] (language folder could be different based on languaged installed)
        /// </summary>
        /// <returns></returns>
        internal bool VerifyAdditionalFilesExist()
        {
            string[] expectedFiles = new string[]
            {
                @"battle\camdat0.bin",
                @"battle\camdat1.bin",
                @"battle\camdat2.bin",
                @"battle\co.bin",
                @"battle\scene.bin",
                @"kernel\KERNEL.BIN",
                @"kernel\kernel2.bin",
                @"kernel\WINDOW.BIN"
            };

            string installedLang = GetInstalledLanguage();
            if (installedLang == "")
            {
                installedLang = "en";
            }

            foreach (string file in expectedFiles)
            {
                string fullTargetPath = Path.Combine(InstallPath, "data", file);

                if (File.Exists(fullTargetPath))
                {
                    continue; // file exists as expected
                }

                SendMessage($"... \t{file} {ResourceHelper.Get(StringKey.FileNotFound)}", NLog.LogLevel.Warn);

                string fullSourcePath = Path.Combine(InstallPath, "data", $"lang-{installedLang}", file);
                if (!File.Exists(fullSourcePath))
                {
                    SendMessage($"... \t{ResourceHelper.Get(StringKey.CannotCopySourceFileBecauseItIsMissingAt)} {fullSourcePath}", NLog.LogLevel.Warn);
                    return false;
                }


                try
                {
                    SendMessage($"... \t{ResourceHelper.Get(StringKey.CopyingFileFrom)} {fullSourcePath}");
                    File.Copy(fullSourcePath, fullTargetPath, true);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    SendMessage($"... \t{ResourceHelper.Get(StringKey.FailedToCopyErrorHasBeenLogged)}: {e.Message}", NLog.LogLevel.Warn);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if all movies exist in the given <paramref name="rootFolder"/>
        /// </summary>
        internal static bool AllMovieFilesExist(string rootFolder)
        {
            foreach (string file in FF7FileLister.GetMovieFiles().Keys)
            {
                string fullPath = Path.Combine(rootFolder, file);

                if (!File.Exists(fullPath))
                {
                    return false;
                }
            }

            return true;
        }

        internal static Dictionary<string, string[]> GetMissingMovieFiles(string rootFolder)
        {
            Dictionary<string, string[]> missingMovies = new Dictionary<string, string[]>();

            foreach (var movie in FF7FileLister.GetMovieFiles())
            {
                string fullPath = Path.Combine(rootFolder, movie.Key);

                if (!File.Exists(fullPath))
                {
                    missingMovies.Add(movie.Key, movie.Value);
                }
            }

            return missingMovies;
        }

        /// <summary>
        /// Copies ending2.avi and jenova_e.avi to movies folder if they dont exist
        /// </summary>
        internal bool CopyMovieFilesToFolder(string movieFolder)
        {
            string[] movieFiles = new string[] { "ending2.avi", "jenova_e.avi" };
            bool copiedAllFiles = true;

            foreach (string file in movieFiles)
            {
                string fullPath = Path.Combine(movieFolder, file);

                if (File.Exists(fullPath))
                {
                    continue; // no need to copy file as it exists as expected
                }

                // check if they exist at data/lang-en/movies location and copy them 
                // ... note we get the installed language and copy from there e.g. "lang-en" "lang-es" "lang-fr" "land-de"
                string language = GetInstalledLanguage();
                if (language == "")
                {
                    language = "en";
                }

                string otherPath = Path.Combine(InstallPath, "data", $"lang-{language}", "movies", file);
                if (File.Exists(otherPath))
                {
                    SendMessage($"\t{ResourceHelper.Get(StringKey.Copying).ToLower()} {otherPath} -> {fullPath}");
                    File.Copy(otherPath, fullPath, true);
                    continue;
                }
                else
                {
                    copiedAllFiles = false;
                }
            }

            return copiedAllFiles;
        }

        public void SendMessage(string message, NLog.LogLevel logLevel = null)
        {
            if (logLevel == null)
            {
                logLevel = NLog.LogLevel.Info;
            }

            MessageSent?.Invoke(message, logLevel);
        }

        public bool AllMusicFilesExist()
        {
            bool allFilesExist = true;

            Directory.CreateDirectory(Path.Combine(InstallPath, "music", "vgmstream")); // ensure music and music/vgmstream folders exist

            foreach (string file in FF7FileLister.GetMusicFiles())
            {
                string fullPath = Path.Combine(InstallPath, "music", "vgmstream", file);

                if (!File.Exists(fullPath))
                {
                    allFilesExist = false;
                }
            }

            return allFilesExist;
        }

        public void CopyMusicFiles()
        {
            Directory.CreateDirectory(Path.Combine(InstallPath, "music", "vgmstream")); // ensure music and music/vgmstream folders exist

            foreach (string file in FF7FileLister.GetMusicFiles())
            {
                string fullTargetPath = Path.Combine(InstallPath, "music", "vgmstream", file);

                if (File.Exists(fullTargetPath))
                {
                    continue;
                }

                string sourcePath = Path.Combine(InstallPath, "data", "music_ogg", file);

                if (!File.Exists(sourcePath))
                {
                    continue; // source music file so skip over copying
                }

                try
                {
                    SendMessage($"\t{ResourceHelper.Get(StringKey.Copying).ToLower()} {sourcePath} -> {fullTargetPath}");
                    File.Copy(sourcePath, fullTargetPath, true);
                }
                catch (Exception e)
                {
                    Logger.Warn(e); // log error but don't halt copying of files
                }
            }
        }

        /// <summary>
        /// Creates 'direct' and subfolders, 'mods/Textures' folders if missing
        /// </summary>
        public void CreateMissingDirectories()
        {
            string[] requiredFolders = new string[]
            {
                Path.Combine(InstallPath, "direct"),
                Path.Combine(InstallPath, "direct", "battle"),
                Path.Combine(InstallPath, "direct", "char"),
                Path.Combine(InstallPath, "direct", "chocobo"),
                Path.Combine(InstallPath, "direct", "coaster"),
                Path.Combine(InstallPath, "direct", "condor"),
                Path.Combine(InstallPath, "direct", "cr"),
                Path.Combine(InstallPath, "direct", "disc"),
                Path.Combine(InstallPath, "direct", "flevel"),
                Path.Combine(InstallPath, "direct", "high"),
                Path.Combine(InstallPath, "direct", "magic"),
                Path.Combine(InstallPath, "direct", "menu"),
                Path.Combine(InstallPath, "direct", "midi"),
                Path.Combine(InstallPath, "direct", "moviecam"),
                Path.Combine(InstallPath, "direct", "snowboard"),
                Path.Combine(InstallPath, "direct", "sub"),
                Path.Combine(InstallPath, "direct", "world"),
                Path.Combine(InstallPath, "mods", "Textures"),
            };

            foreach (string dir in requiredFolders)
            {
                if (!Directory.Exists(dir))
                {
                    Logger.Info($"\t{ResourceHelper.Get(StringKey.DirectoryMissingCreating)} {dir}");
                    Directory.CreateDirectory(dir);
                }
            }
        }

        /// <summary>
        /// Checks registry for old game converter registry values. If found then backup of original game converter files and registry is taken.
        /// The old game converter registry key is also deleted.
        /// </summary>
        /// <param name="pathToBackup"></param>
        internal void CheckAndCopyOldGameConverterFiles(string pathToBackup)
        {
            string converterKeyPath = $"{RegistryHelper.GetKeyPath(FF7RegKey.SquareSoftKeyPath)}\\Final Fantasy VII\\GameConverterkeys";


            string regValue = RegistryHelper.GetValue(converterKeyPath, "Manipulated_by_GameConverter", "") as string;

            if (!string.IsNullOrWhiteSpace(regValue))
            {
                SendMessage(ResourceHelper.Get(StringKey.OldGameConverterRegistryKeysFoundBackingUp));
                MoveOriginalAppFilesToBackup(pathToBackup);
                MoveOriginalConverterFilesToBackup(pathToBackup);
                BackupRegistry(pathToBackup);
                DeleteCacheFiles();

                RegistryHelper.DeleteKey(converterKeyPath);
            }
        }

        internal bool IsExeDifferent()
        {
            string ff7ExePath = Path.Combine(Sys.PathToPatchedExeFolder, "ff7.exe");
            return !FileUtils.AreFilesEqual(ff7ExePath, Path.Combine(InstallPath, "ff7.exe"));
        }

        internal bool IsConfigExeDifferent()
        {
            string ff7ExePath = Path.Combine(Sys.PathToPatchedExeFolder, "FF7Config.exe");
            return !FileUtils.AreFilesEqual(ff7ExePath, Path.Combine(InstallPath, "FF7Config.exe"));
        }

        /// <summary>
        /// Returns true if the flevel.lgp file is found in '<see cref="InstallPath"/>/data/field' folder
        /// </summary>
        public bool IsEnglishGameInstalled()
        {
            string fullPath = Path.Combine(InstallPath, "data", "field", "flevel.lgp");
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Copies files in 'data' for a different language and patches them using ulgp tool to work with the ff7.exe english patch
        /// </summary>
        /// <remarks>
        /// Mods only work with the english patched .exe so different language installs need to be further converted to support the english .exe
        /// The language specific files are copied to the correct english file names and then ulgp tool is used to encode the files correctly
        /// </remarks>
        public void ConvertToEnglishInstall()
        {
            string languageInstalled = GetInstalledLanguage();

            string filePrefix;
            string fileSuffix;
            string fileSuffix2; // why does German files have two different suffixes???? 'ge' and 'gm'

            switch (languageInstalled)
            {
                case "fr":
                    filePrefix = "f";
                    fileSuffix = "fr";
                    fileSuffix2 = "fr";
                    break;

                case "de":
                    filePrefix = "g";
                    fileSuffix = "gm";
                    fileSuffix2 = "ge";
                    break;

                case "es":
                    filePrefix = "s";
                    fileSuffix = "sp";
                    fileSuffix2 = "sp";
                    break;

                default:
                    return; // no other language files found so assume files are already copied/converted
            }

            //
            // copy language Specific files and rename them to English specific file names
            //

            Dictionary<string, string> sourceFilesToCopy = new Dictionary<string, string>()
            {
                {Path.Combine(InstallPath, "data", "cd", $"cr_{fileSuffix}.lgp"), "cr_us.lgp"},
                {Path.Combine(InstallPath, "data", "menu", $"menu_{fileSuffix}.lgp"), "menu_us.lgp"},
                {Path.Combine(InstallPath, "data", "wm", $"world_{fileSuffix}.lgp"), "world_us.lgp"},
                {Path.Combine(InstallPath, "data", "field", $"{filePrefix}flevel.lgp"), "flevel.lgp"},
                {Path.Combine(InstallPath, "data", "minigame", $"{filePrefix}chocobo.lgp"), "chocobo.lgp"},
                {Path.Combine(InstallPath, "data", "minigame", $"high-{fileSuffix2}.lgp"), "high-us.lgp"},
            };

            string destFilePath;

            foreach (var sourceFile in sourceFilesToCopy)
            {
                if (File.Exists(sourceFile.Key))
                {
                    FileInfo info = new FileInfo(sourceFile.Key);
                    destFilePath = Path.Combine(info.DirectoryName, sourceFile.Value);
                    File.Copy(sourceFile.Key, destFilePath, true);
                }
                else
                {
                    SendMessage($"{string.Format(ResourceHelper.Get(StringKey.CouldNotFindFileToCopy), sourceFile.Key)}", NLog.LogLevel.Warn);
                }
            }

            SendMessage($"\t{ResourceHelper.Get(StringKey.ExtractingLGPFiles)}");

            string pathToDiscUs = Path.Combine(Sys.PathToTempFolder, "disc_us");
            string pathToCondor = Path.Combine(Sys.PathToTempFolder, "condor");
            string pathToSnowboard = Path.Combine(Sys.PathToTempFolder, "snowboard-us");
            string pathToSub = Path.Combine(Sys.PathToTempFolder, "sub");

            RunUlgp(Path.Combine(InstallPath, "data", "cd", $"disc_{fileSuffix}.lgp"), pathToDiscUs);
            RunUlgp(Path.Combine(InstallPath, "data", "minigame", $"{filePrefix}condor.lgp"), pathToCondor);
            RunUlgp(Path.Combine(InstallPath, "data", "minigame", $"snowboard-{fileSuffix2}.lgp"), pathToSnowboard);
            RunUlgp(Path.Combine(InstallPath, "data", "minigame", $"{filePrefix}sub.lgp"), pathToSub);

            // copy _pal specific Condor .tga files
            string condorTranFolder = Path.Combine(Sys._7HFolder, "Resources", "Languages", "Condor_Tran");
            FileUtils.CopyDirectoryRecursively(condorTranFolder, Path.Combine(Sys.PathToTempFolder, "condor"));

            //
            // Rename extracted files to match english file names
            //
            Dictionary<string, string> filesToRename = new Dictionary<string, string>()
            {
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk1_a.tex"), "disk1_a.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk1_b.tex"), "disk1_b.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk1_x.tex"), "disk1_x.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk2_a.tex"), "disk2_a.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk2_b.tex"), "disk2_b.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk2_x.tex"), "disk2_x.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk3_a.tex"), "disk3_a.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk3_b.tex"), "disk3_b.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}disk3_x.tex"), "disk3_x.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_a.tex"), "e_over_a.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_b.tex"), "e_over_b.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_c.tex"), "e_over_c.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_d.tex"), "e_over_d.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_e.tex"), "e_over_e.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_f.tex"), "e_over_f.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_la.tex"), "e_over_la.tex"},
                {Path.Combine(pathToDiscUs, $"{filePrefix}_over_lb.tex"), "e_over_lb.tex"},

                {Path.Combine(pathToCondor, $"{filePrefix}help.tim"), "ehelp.tim" },
                {Path.Combine(pathToCondor, $"{filePrefix}help_1.tim"), "ehelp_1.tim" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes00.tex"), "emes00.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes00a.tex"), "emes00a.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes00b.tex"), "emes00b.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes00c.tex"), "emes00c.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes00d.tex"), "emes00d.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes01.tex"), "emes01.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes01a.tex"), "emes01a.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes01b.tex"), "emes01b.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}mes08.tex"), "emes08.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit00.tex"), "eunit00.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit00a.tex"), "eunit00a.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit00b.tex"), "eunit00b.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit00c.tex"), "eunit00c.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit00d.tex"), "eunit00d.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit01.tex"), "eunit01.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit01a.tex"), "eunit01a.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit01b.tex"), "eunit01b.tex" },
                {Path.Combine(pathToCondor, $"{filePrefix}unit01c.tex"), "eunit01c.tex" },

                {Path.Combine(pathToSnowboard, $"{filePrefix}a.tex"), "ea_k.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}b.tex"), "eb_k.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}c.tex"), "ec_k.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}g.tex"), "eg_k.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}eita_k.tex"), "eita_k.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}stamp0.tex"), "estamp0.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}stamp1.tex"), "estamp1.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}time1.tex"), "time1.tex" },
                {Path.Combine(pathToSnowboard, $"{filePrefix}time2.tex"), "time2.tex" },

                {Path.Combine(pathToSub, $"{filePrefix}hud.tex"), "hud.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}huda.tex"), "huda.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}hudb.tex"), "hudb.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}hudc.tex"), "hudc.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}hudd.tex"), "hudd.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}text.tex"), "text.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}texta.tex"), "texta.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}textb.tex"), "textb.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}textc.tex"), "textc.tex"},
                {Path.Combine(pathToSub, $"{filePrefix}textd.tex"), "textd.tex"},
            };

            SendMessage($"\t{ResourceHelper.Get(StringKey.RenamingLanguageSpecificFiles)}");

            foreach (var file in filesToRename)
            {
                if (File.Exists(file.Key))
                {
                    FileInfo info = new FileInfo(file.Key);
                    destFilePath = Path.Combine(info.DirectoryName, file.Value);
                    File.Move(file.Key, destFilePath);
                }
                else
                {
                    SendMessage($"{ResourceHelper.Get(StringKey.CouldNotFindFileToRename)}: {file.Key}", NLog.LogLevel.Warn);

                }
            }

            // Run Ulgp to encode altered .lgp file
            SendMessage($"\t{ResourceHelper.Get(StringKey.EncodingNewLGPFiles)}");

            RunUlgp(pathToDiscUs, Path.Combine(InstallPath, "data", "cd", "disc_us.lgp"));
            RunUlgp(pathToCondor, Path.Combine(InstallPath, "data", "minigame", "condor.lgp"));
            RunUlgp(pathToSnowboard, Path.Combine(InstallPath, "data", "minigame", "snowboard-us.lgp"));
            RunUlgp(pathToSub, Path.Combine(InstallPath, "data", "minigame", "sub.lgp"));


            SendMessage($"\t{ResourceHelper.Get(StringKey.DeletingTemporaryFiles)}");

            string[] foldersToDelete = { pathToDiscUs, pathToCondor, pathToSnowboard, pathToSub };
            foreach (string folder in foldersToDelete)
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        /// <summary>
        /// Runs ulgp v1.3.2 with given arguments.
        /// if <paramref name="sourcePath"/> is *.lgp then it will be dumped to <paramref name="destPath"/>,
        /// otherwise <paramref name="sourcePath"/> is assumed to be a directory containing files to add to a new or existing .lgp
        /// </summary>
        private void RunUlgp(string sourcePath, string destPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = Sys.PathToUlgpExe,
                    WorkingDirectory = Path.GetDirectoryName(Sys.PathToUlgpExe),
                    Arguments = $"\"{sourcePath}\" \"{destPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas", // ensures the process is started as Admin
                };

                using (Process proc = Process.Start(startInfo))
                {
                    proc.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                SendMessage($"{ResourceHelper.Get(StringKey.AnErrorOccurredStartingULGP)}", NLog.LogLevel.Error);
            }
        }

        /// <summary>
        /// Returns 'fr' for french, 'de' for german, and 'es' for spanish installations.
        /// Returns empty string if none of these languages found.
        /// </summary>
        public string GetInstalledLanguage()
        {
            string languageInstalled = "";

            if (File.Exists(Path.Combine(InstallPath, "data", "field", "fflevel.lgp")))
            {
                languageInstalled = "fr";
            }
            else if (File.Exists(Path.Combine(InstallPath, "data", "field", "gflevel.lgp")))
            {
                languageInstalled = "de";
            }
            else if (File.Exists(Path.Combine(InstallPath, "data", "field", "sflevel.lgp")))
            {
                languageInstalled = "es";
            }

            return languageInstalled;
        }
    }
}
