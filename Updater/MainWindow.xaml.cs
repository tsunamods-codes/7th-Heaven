using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Updater.GitHub;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string[] Args;
        private string extractPath;
        private Releases.Channel releaseChannel;

        private ReleaseInfo ri;

        private bool finished = false;

        public MainWindow()
        {
            if (!Directory.Exists(Args[0]))
            {
                System.Windows.Application.Current.Shutdown(1);
            }
            extractPath = Args[0];
            switch (Args[1].ToLower())
            {
                case "canary":
                    releaseChannel = Releases.Channel.Canary;
                    break;
                case "stable":
                    releaseChannel = Releases.Channel.Stable;
                    break;
                default:
                    releaseChannel = Releases.Channel.Locked;
                    break;
            }
            Initialized += MainWindow_Initialized;
        }

        private void MainWindow_Initialized(object sender, EventArgs e)
        {
            AppReady();
        }

        private void AppReady()
        {
            Releases releases = new Releases();
            try
            {
                ri = releases.GetReleaseJSON(releaseChannel);
            }
			
            catch (Exception e)
            {
                MessageBox.Show("Version requested could not be found no changes have occured.", "Version Not Found");
                Console.WriteLine(String.Format("Tried to download for {0} but failed to", releaseChannel.ToString()));
                Application.Current.Shutdown(0);
            }
            string downloadUrl = ri.url;
            if (downloadUrl != null)
            {
                Progress_Text.Content = String.Format("Fetching: {0}", downloadUrl.Split(new char[] { '/' }).Last());
            }
            else
            {
                Application.Current.Shutdown(2);
            }

            Download download = new Download();
            download.downloadProgressChanged += Download_downloadProgressChanged;
            download.downloadFinished += Download_downloadFinished;
            download.downloadFailed += Download_downloadFailed;
            download.downloadToTemp(downloadUrl);
        }

        private void Download_downloadFailed()
        {
            Progress_Text.Content = String.Format("Downloading Failed");
            Application.Current.Shutdown(3);
        }

        private void Download_downloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void Download_downloadFinished(string path)
        {
            Progress_Text.Content = String.Format("Downloading Finished: {0}", path.Split(new char[] { '\\' }).Last());
            ProgressBar.Value = 100;
            Core.Zip zip = new Core.Zip();
            zip.ZipExtractProgress += Zip_ZipExtractProgress;
            zip.ZipExtractComplete += Zip_ZipExtractComplete;
            zip.ExtractZipFileToDirectory(path, extractPath, true);
        }

        private void Zip_ZipExtractProgress(int percent, int curr, int total, string fileCompleted)
        {
            Progress_Text.Content = String.Format("Processed {0} of {1} files.", curr, total);
            // if more details want to be showed could add info using fileCompleted;
            ProgressBar.Value = percent;
        }

        private void Zip_ZipExtractComplete()
        {
            Progress_Text.Content = String.Format("Files Successfully installed.");
            string jsonString = JsonConvert.SerializeObject(ri);
            File.WriteAllText(extractPath+"\\updater.json", jsonString);
			
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Args[0];
            startInfo.FileName = "powershell";
            startInfo.Arguments = "-windowstyle hidden start-sleep 1; start \\\"7th Heaven.exe\\\"";
            try
            {
                Process proc = Process.Start(startInfo);
                IntPtr hWnd = proc.MainWindowHandle;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return;
            }
            System.Windows.Application.Current.Shutdown(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!finished)
            {
                e.Cancel = true;
            }
        }
    }
}
