using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Updater.GitHub;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string[] args;

        public MainWindow()
        {
            this.Initialized += MainWindow_Initialized;
            InitializeComponent();
        }

        private void MainWindow_Initialized(object sender, EventArgs e)
        {
            Releases releases = new Releases();
            string downloadUrl = releases.GetReleaseJSON(Releases.Channel.Stable);
            if (downloadUrl != null)
            {
                Progress_Text.Content = String.Format("Fetching: {0}", downloadUrl.Split(new char[] { '/' }).Last());
            }
            else
            {
                // something went wrong close the updater.exe
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
            // something went wrong close the updater.exr
        }

        private void Download_downloadFinished(string path)
        {
            Progress_Text.Content = String.Format("Downloading Finished: {0}", path.Split(new char[] { '\\' }).Last());
            ProgressBar.Value = 100;
            Console.WriteLine(path);
        }

        private void Download_downloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }
    }
}
