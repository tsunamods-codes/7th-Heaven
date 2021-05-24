using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using Newtonsoft.Json.Linq;
using SeventhHeaven.Windows;
using SeventhHeavenUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeventhHeaven.Classes
{
    public class UpdateChecker
    {
        private FileVersionInfo _currentAppVersion = null;

        private string GetUpdateInfoPath()
        {
            return Path.Combine(Sys.PathToTempFolder, "7thheavenupdateinfo.json");
        }

        private string GetCurrentAppVersion()
        {
            return _currentAppVersion != null ? _currentAppVersion.FileVersion : "0.0.0.0";
        }

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
            for (int i = 0; i < assets.Count - 1; i++)
            {
                string url = assets[i].browser_download_url.Value;

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

        public void CheckForUpdates(AppUpdateChannelOptions channel)
        {
            try
            {
                _currentAppVersion = FileVersionInfo.GetVersionInfo(
                    Path.Combine(Sys._7HFolder, $"{App.GetAppName()}.exe")
                );
            }
            catch (FileNotFoundException)
            {
                _currentAppVersion = null;
            }

            DownloadItem download = new DownloadItem()
            {
                Links = new List<string>() { LocationUtil.FormatHttpUrl(GetUpdateChannel(channel)) },
                SaveFilePath = GetUpdateInfoPath(),
                Category = DownloadCategory.AppUpdate,
                ItemName = $"{ResourceHelper.Get(StringKey.CheckingForUpdatesUsingChannel)} {channel.ToString()}"
            };

            download.IProc = new Install.InstallProcedureCallback(e =>
            {
                bool success = (e.Error == null && e.Cancelled == false);

                if (success)
                {
                    try
                    {
                        StreamReader file = File.OpenText(download.SaveFilePath);
                        dynamic release = JValue.Parse(file.ReadToEnd());
                        file.Close();
                        File.Delete(download.SaveFilePath);

                        Version curVersion = new Version(GetCurrentAppVersion());
                        Version newVersion = new Version(GetUpdateVersion(release.name.Value));

                        switch (newVersion.CompareTo(curVersion))
                        {
                            case 1: // NEWER
                                if (
                                    MessageDialogWindow.Show(
                                        string.Format(ResourceHelper.Get(StringKey.AppUpdateIsAvailableMessage), $"{App.GetAppName()} - {App.GetAppVersion()}", newVersion.ToString(), release.body.Value),
                                        ResourceHelper.Get(StringKey.NewVersionAvailable),
                                        System.Windows.MessageBoxButton.YesNo,
                                        System.Windows.MessageBoxImage.Question
                                    ).Result == System.Windows.MessageBoxResult.Yes)
                                {
                                    ProcessStartInfo startInfo = new ProcessStartInfo(GetUpdateChannel(channel).Replace("api.github.com/repos", "github.com"));
                                    Process.Start(startInfo);
                                    App.ShutdownApp();
                                }
                                break;
                            default:
                                // Nothing to do here
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        MessageDialogWindow.Show("Something went wrong while checking for App updates. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        Sys.Message(new WMessage() { Text = $"Could not parse the 7thHeaven release json at {GetUpdateChannel(channel)}", LoggedException = e.Error });
                    }
                }
                else
                {
                    MessageDialogWindow.Show("Something went wrong while checking for App updates. Please try again later.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    Sys.Message(new WMessage() { Text = $"Could not fetch for 7thHeaven updates at {GetUpdateChannel(channel)}", LoggedException = e.Error });
                }
            });

            Sys.Downloads.AddToDownloadQueue(download);
        }
    }

}
