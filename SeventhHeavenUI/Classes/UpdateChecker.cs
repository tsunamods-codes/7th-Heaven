using _7thHeaven.Code;
using Iros._7th.Workshop;
using Newtonsoft.Json.Linq;
using SeventhHeaven.Windows;
using SeventhHeavenUI;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
<<<<<<< HEAD
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
=======
>>>>>>> d966b12ca916a5904bebd931a0980cb18943446f

namespace SeventhHeaven.Classes
{
    public class UpdateChecker
    {
        private FileVersionInfo _currentAppVersion = null;

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private string GetUpdateInfoPath()
        {
            return Path.Combine(Sys.PathToTempFolder, "7thheavenupdateinfo.json");
        }

        private string GetCurrentAppVersion()
        {
            return _currentAppVersion != null ? _currentAppVersion.FileVersion : "0.0.0.0";
        }

<<<<<<< HEAD
        public void CheckForUpdates(Updater.GitHub.Releases.Channel channel)
=======
        private string GetUpdateChannel(AppUpdateChannelOptions channel)
        {
            switch (channel)
            {
                case AppUpdateChannelOptions.Stable:
                    return "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/latest";
                case AppUpdateChannelOptions.Canary:
                    return "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/canary";
                default:
                    return "";
            }
        }

        private string GetUpdateVersion(string name)
        {
            return name.Replace("7thHeaven-v", "");
        }

        private string GetUpdateReleaseUrl(dynamic assets)
        {
            foreach (dynamic asset in assets)
            {
                string url = asset.browser_download_url.Value;

                if (url.Contains("7thHeaven-v"))
                    return url;
            }

            return String.Empty;
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

        public void CheckForUpdates(AppUpdateChannelOptions channel, bool manualCheck = false)
>>>>>>> d966b12ca916a5904bebd931a0980cb18943446f
        {
            try
            {
                _currentAppVersion = FileVersionInfo.GetVersionInfo(
                    Path.Combine(Sys._7HFolder, $"{App.GetAppName()}.exe")
                );
            }
            catch (FileNotFoundException e)
            {
                _currentAppVersion = null;
                Sys.Message(new WMessage() { Text = $"Could not get application version ", LoggedException = e });
            }
            
            if(_currentAppVersion != null)
            {
                Updater.GitHub.NewReleaseVersionInfo releaseVersioninfo = Updater.GitHub.Releases.isNewerVersion(new Version(GetCurrentAppVersion()), channel);
                if (releaseVersioninfo.isNewer)
                {
                    if(MessageDialogWindow.Show(
                        string.Format(ResourceHelper.Get(StringKey.AppUpdateIsAvailableMessage), $"{App.GetAppName()} - {App.GetAppVersion()}", releaseVersioninfo.version.ToString(), releaseVersioninfo.changeLog),
                        ResourceHelper.Get(StringKey.NewVersionAvailable),
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question
                    ).Result == System.Windows.MessageBoxResult.Yes)
                    {
                        Sys.Message(new WMessage() { Text = "Sarting updater application" });
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.UseShellExecute = false;
                        startInfo.WorkingDirectory = Environment.CurrentDirectory;
                        startInfo.FileName = "updater.exe";
                        startInfo.Arguments = "\"" + System.AppDomain.CurrentDomain.BaseDirectory + "\\\" v"+ 
                            String.Format("{0}.{1}.{2}", releaseVersioninfo.version.Major, releaseVersioninfo.version.Minor, releaseVersioninfo.version.Build);
                        Process proc = Process.Start(startInfo);
                        IntPtr hWnd = proc.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
<<<<<<< HEAD
                            SetForegroundWindow(hWnd);
                            ShowWindow(hWnd, int.Parse("9"));
=======
                            case 1: // NEWER
                                if (
                                    MessageDialogWindow.Show(
                                        string.Format(ResourceHelper.Get(StringKey.AppUpdateIsAvailableMessage), $"{App.GetAppName()} - {App.GetAppVersion()}", newVersion.ToString(), release.body.Value),
                                        ResourceHelper.Get(StringKey.NewVersionAvailable),
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question
                                    ).Result == System.Windows.MessageBoxResult.Yes)
                                    DownloadAndExtract(GetUpdateReleaseUrl(release.assets), newVersion.ToString());
                                break;
                            case 0: // SAME
                                if (manualCheck)
                                    MessageDialogWindow.Show("You seem to be up to date!", "No update found", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                                break;
                            case -1: // OLDER
                                if (
                                    MessageDialogWindow.Show(
                                        $"Your current version seems newer to the one currently available.\n\nCurrent Version: {curVersion.ToString()}\nNew Version: {newVersion.ToString()}\n\nWould you like to install it anyway?",
                                        "Update found!",
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question
                                    ).Result == System.Windows.MessageBoxResult.Yes)
                                    DownloadAndExtract(GetUpdateReleaseUrl(release.assets), newVersion.ToString());
                                break;
>>>>>>> d966b12ca916a5904bebd931a0980cb18943446f
                        }
                        App.ShutdownApp();
                    }
                }
            }
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
                    ItemName = $"Downloading 7thHeaven Update {url}..."
                };

                download.IProc = new Install.InstallProcedureCallback(e =>
                {
                    bool success = (e.Error == null && e.Cancelled == false);

                    if (success)
                    {
                        string ExtractPath = Path.Combine(Sys.PathToTempFolder, $"7thHeaven-v{version}");

                        using (var archive = ZipArchive.Open(download.SaveFilePath))
                        {
                            foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                            {
                                entry.WriteToDirectory(ExtractPath, new ExtractionOptions()
                                {
                                    ExtractFullPath = true,
                                    Overwrite = true
                                });
                            }
                        }

                        SwitchToModPanel();

                        MessageDialogWindow.Show($"Successfully downloaded version {version}.\n\nWe will now start the update process. 7th Heaven will restart automatically when the update will be completed.\n\nEnjoy!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        Sys.Message(new WMessage() { Text = $"Successfully extracted the 7thHeaven version {version}. Ready to launch the update." });

                        File.Delete(download.SaveFilePath);
                        StartUpdate(ExtractPath);
                    }
                    else
                    {
                        MessageDialogWindow.Show("Something went wrong while downloading the 7thHeaven update. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        Sys.Message(new WMessage() { Text = $"Could not download the 7thHeaven update {url}", LoggedException = e.Error });
                    }
                });

                Sys.Downloads.AddToDownloadQueue(download);
            }
            else
            {
                MessageDialogWindow.Show("Something went wrong while downloading the 7thHeaven update. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void StartUpdate(string sourcePath)
        {
            string fileName = Path.Combine(Sys.PathToTempFolder, "update.bat");

            System.IO.File.WriteAllText(
                fileName,
                $@"@echo off
@echo Waiting for 7th Heaven to be closed, please wait...
@taskkill /IM ""7th Heaven.exe"" /F >NUL 2>NUL
@timeout /t 5 /nobreak
@robocopy ""{sourcePath}"" ""{Sys._7HFolder}"" /S /MOV >NUL 2>NUL
@echo Waiting for the update to take place, please wait...
@timeout /t 5 /nobreak
@rmdir /s /q ""{sourcePath}""
@start """" /d ""{Sys._7HFolder}"" ""{Path.Combine(Sys._7HFolder, "7th Heaven.exe")}""
@del ""{fileName}""
"
            );

            // Execute temp batch script with admin privileges
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = fileName;
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = false;
            try
            {
                // Launch process, wait and then save exit code
                using (Process temp = Process.Start(startInfo))
                {
                    temp.WaitForExit();
                }
            }
            catch (Exception e) {
                MessageDialogWindow.Show("Something went wrong while trying to update 7th Heaven. See the error log for more details.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Sys.Message(new WMessage() { Text = $"Error while trying to update 7thHeaven", LoggedException = e });
            }
        }
    }
}
