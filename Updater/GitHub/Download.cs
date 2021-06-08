using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Updater.GitHub
{
    public class Download
    {
        public delegate void DownloadFinished(string path);
        public delegate void DownloadFailed();

        public event DownloadProgressChangedEventHandler downloadProgressChanged;
        public event DownloadFinished downloadFinished;
        public event DownloadFailed downloadFailed;

        private WebClient webClient;

        private string GetUpdateUpdatePath()
        {
            return Path.Combine(Path.GetTempPath(), "update.zip");
        }

        public void downloadToTemp(string url)
        {
            webClient = new WebClient();
            if (downloadProgressChanged != null)
            {
                webClient.DownloadProgressChanged += this.downloadProgressChanged;
            }
            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;

            webClient.Headers.Add(HttpRequestHeader.UserAgent, "7th Heaven Updater - Tsunamods Community");
            webClient.DownloadFileAsync(new Uri(url), GetUpdateUpdatePath());
        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if(e.Error != null || e.Cancelled)
            {
                downloadFailed();
            }
            else
            {
                downloadFinished(GetUpdateUpdatePath());
            }
        }
    }
}
