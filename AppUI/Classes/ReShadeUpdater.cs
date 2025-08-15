using AngleSharp.Dom;
using AppCore;
using AppUI.Windows;
using Iros.Workshop;
using Newtonsoft.Json.Linq;
using SevenZipExtractor;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AppUI.Classes
{
    public class ReShadeUpdater
    {
        private FileVersionInfo _currentReShadeVersion = null;

        private string GetUpdateInfoPath()
        {
            return Path.Combine(Sys.PathToTempFolder, "reshadeupdateinfo.json");
        }

        public string GetCurrentVersion()
        {
            try
            {
                _currentReShadeVersion = FileVersionInfo.GetVersionInfo(Sys.PathToReShade);
            }
            catch (FileNotFoundException)
            {
                _currentReShadeVersion = null;
            }

            return _currentReShadeVersion != null ? _currentReShadeVersion.FileVersion : "0.0.0.0";
        }

        private string GetUpdateChannel()
        {
            return "https://api.github.com/repos/crosire/reshade/tags";
        }

        private string GetUpdateVersion(string name)
        {
            return name.Replace("v", "");
        }

        private string GetUpdateReleaseUrl(string version)
        {
            return $"https://reshade.me/downloads/ReShade_Setup_{version}.exe";
        }

        private void SwitchToDownloadPanel()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MainWindow window = App.Current.MainWindow as MainWindow;

                window.tabCtrlMain.SelectedIndex = 1;
            });
        }

        private void SwitchToModPanel()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                MainWindow window = App.Current.MainWindow as MainWindow;

                window.tabCtrlMain.SelectedIndex = 0;
            });
        }

        public void CheckForUpdates(bool manualCheck = false)
        {
            DownloadItem download = new DownloadItem()
            {
                Links = new List<string>() { LocationUtil.FormatHttpUrl(GetUpdateChannel()) },
                SaveFilePath = GetUpdateInfoPath(),
                Category = DownloadCategory.AppUpdate,
                ItemName = $"{ResourceHelper.Get(StringKey.CheckingForUpdatesUsingChannel)} Stable"
            };

            download.IProc = new Install.InstallProcedureCallback(e =>
            {
                bool success = (e.Error == null && e.Cancelled == false);

                if (success)
                {
                    try
                    {
                        StreamReader file = File.OpenText(download.SaveFilePath);
                        JArray release = JArray.Parse(file.ReadToEnd());
                        file.Close();
                        File.Delete(download.SaveFilePath);

                        string strVersion = GetUpdateVersion(release[0]["name"].ToString());
                        Version curVersion = new Version(GetCurrentVersion());
                        Version newVersion = new Version(strVersion);

                        switch (newVersion.CompareTo(curVersion))
                        {
                            case 1: // NEWER
                                if (
                                    MessageDialogWindow.Show(
                                        string.Format(ResourceHelper.Get(StringKey.AppUpdateIsAvailableMessage), $"ReShade - {GetCurrentVersion()}", newVersion.ToString()),
                                        "See https://reshade.me/ for more information.",
                                        ResourceHelper.Get(StringKey.NewVersionAvailable),
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question
                                    ).Result == System.Windows.MessageBoxResult.Yes)
                                    DownloadAndExtract(GetUpdateReleaseUrl(strVersion), newVersion.ToString());
                                break;
                            case 0: // SAME
                                if (manualCheck)
                                    MessageDialogWindow.Show("ReShade version is up to date!", "No update found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                                break;
                            case -1: // OLDER
                                if (
                                    MessageDialogWindow.Show(
                                        $"Your ReShade version is newer than the one being offered by your channel management setting.\n\nCurrently installed: {curVersion.ToString()}\nVersion being offered: {newVersion.ToString()}\n\nContinue with the downgrade?",
                                        "Update found!",
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question
                                    ).Result == System.Windows.MessageBoxResult.Yes)
                                    DownloadAndExtract(GetUpdateReleaseUrl(strVersion), newVersion.ToString());
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        MessageDialogWindow.Show("Something went wrong while checking for ReShade updates. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        Sys.Message(new WMessage() { Text = $"Could not parse the ReShade release json at {GetUpdateChannel()}", LoggedException = e.Error });
                    }
                }
                else
                {
                    MessageDialogWindow.Show("Something went wrong while checking for ReShade updates. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    Sys.Message(new WMessage() { Text = $"Could not fetch for ReShade updates at {GetUpdateChannel()}", LoggedException = e.Error });
                }
            });

            Sys.Downloads.AddToDownloadQueue(download);
        }

        private void DownloadAndExtract(string url, string version)
        {
            if (url != String.Empty)
            {
                SwitchToDownloadPanel();

                DownloadItem download = new DownloadItem()
                {
                    Links = new List<string>() { LocationUtil.FormatHttpUrl(url) },
                    SaveFilePath = Path.Combine(Sys.PathToTempFolder, url.Substring(url.LastIndexOf("/") + 1)),
                    Category = DownloadCategory.AppUpdate,
                    ItemName = $"Downloading ReShade Update {url}..."
                };

                download.IProc = new Install.InstallProcedureCallback(e =>
                {
                    bool success = (e.Error == null && e.Cancelled == false);

                    if (success)
                    {
                        MemoryStream zipStream = new MemoryStream();

                        // Extract the ZIP from the PE file
                        using (ArchiveFile archiveFile = new ArchiveFile(download.SaveFilePath))
                        {
                            archiveFile.Entries.Where(f => f.FileName == "[0]").First().Extract(zipStream);
                        }

                        // Extract ReShade files
                        using (var archive = ZipArchive.Open(zipStream))
                        {
                            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                            {
                                entry.WriteToDirectory(Sys.PathToReShadeFolder, new ExtractionOptions()
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                            }
                        }

                        Console.WriteLine("Extraction complete!");

                        SwitchToModPanel();

                        MessageDialogWindow.Show($"Successfully updated ReShade to version {version}.\n\nEnjoy!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        Sys.Message(new WMessage() { Text = $"Successfully extracted the ReShade version {version}. Ready to launch the update." });
                    }
                    else
                    {
                        MessageDialogWindow.Show("Something went wrong while downloading the ReShade update. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        Sys.Message(new WMessage() { Text = $"Could not download the ReShade update {url}", LoggedException = e.Error });
                    }
                });

                Sys.Downloads.AddToDownloadQueue(download);
            }
            else
            {
                MessageDialogWindow.Show("Something went wrong while downloading the ReShade update. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public static void Cleanup()
        {
            // ================================================================================================
            // Always cleanup these files if present, to avoid conflict with various mods
            // ================================================================================================

            Dictionary<string, bool> pathsToDelete = new Dictionary<string, bool>(){
                { "d3d11.dll", false },
                { "d3d12.dll", false },
                { "opengl32.dll", false },
            };


            string entryPath = "";
            bool entryDeleteRecursive = false;

            foreach (var entry in pathsToDelete)
            {
                entryPath = Sys.InstallPath + "\\" + entry.Key;
                entryDeleteRecursive = entry.Value;

                // Delete recursively
                if (entryDeleteRecursive)
                {
                    if (Directory.Exists(entryPath)) Directory.Delete(entryPath, true);
                }
                else
                {
                    if (File.Exists(entryPath)) File.Delete(entryPath);
                }
            }
        }

        public static void Install()
        {
            if (File.Exists(Sys.PathToReShade))
            {
                string ffnxRenderer = Sys.FFNxConfig.Get("renderer_backend");

                switch(ffnxRenderer)
                {
                    // DirectX 11
                    case "0":
                    case "3":
                        File.Copy(Sys.PathToReShade, Path.Combine(Sys.InstallPath, "d3d11.dll"));
                        break;

                    // DirectX 12
                    case "4":
                        File.Copy(Sys.PathToReShade, Path.Combine(Sys.InstallPath, "d3d12.dll"));
                        break;

                    // OpenGL
                    case "1":
                        File.Copy(Sys.PathToReShade, Path.Combine(Sys.InstallPath, "opengl32.dll"));
                        break;

                    // Unknown, skip loading ReShade
                    default:
                        break;
                }
            }
        }
    }
}
