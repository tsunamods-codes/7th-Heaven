using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Updater.GitHub
{
    public class ReleaseInfo
    {
        public string version;
        public string url;
        public string channel;
    }

    public class NewReleaseVersionInfo
    {
        public bool isNewer { get; private set; } = false;
        public string changeLog { get; private set; } = null;
        public Version version { get; private set; } = null;

        public NewReleaseVersionInfo(bool n, string cl, Version ver)
        {
            isNewer = n;
            changeLog = cl;
            version = ver;
        }
    }


    public class Releases
    {
        public enum Channel
        {
            Stable = 0,
            Canary = 1,
            Custom = 3
        }

        public static NewReleaseVersionInfo isNewerVersion(Version ver, Channel channel)
        {
            if(ver.ToString() == "0.0.0.0")
            {
                // something is wrong we can't identify the version don't ask them about updates
                return new NewReleaseVersionInfo(false,null,null); 
            }
            string url;

            switch (channel)
            {
                case Channel.Canary:
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/canary";
                    break;
                default:
                case Channel.Stable:
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/latest";
                    break;
            }

            WebClient wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "7th Heaven Updater - Tsunamods Community");
            string jsontext = wc.DownloadString(url);
            dynamic release = JValue.Parse(jsontext);
            string name = release.name.Value;
            // NameFormat: 7thHeaven-v2.3.1.0 (split on -v and use right half of split)
            Version remoteVersion = new Version(name.Split(new string[] { "-v" }, StringSplitOptions.RemoveEmptyEntries)[1]);
            return new NewReleaseVersionInfo(remoteVersion > ver, remoteVersion > ver? release.body.Value:null, remoteVersion);
        }

        private bool signed = false;

        private string GetUpdateInfoPath()
        {
            return Path.Combine(Path.GetTempPath(), "7thheavenupdateinfo.json");
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

        public ReleaseInfo GetReleaseJSON(Channel channel)
        {
            ReleaseInfo ri = new ReleaseInfo();
            string filePath = GetUpdateInfoPath();
            string url;
            signed = false;
            string version = MainWindow.Args[1];
            if (version.StartsWith("v"))
            {
                version = version.Substring(1);
            }
            switch (channel)
            {
                case Channel.Canary:
                    ri.channel = "canary";
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/canary";
                    break;
                default:
                case Channel.Stable:
                    ri.channel = "stable";
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/latest";
                    signed = true;
                    break;
                case Channel.Custom:
                    ri.channel = "locked";
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/"+version;
                    signed = true;
                    break;
            }
            WebClient wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "7th Heaven Updater - Tsunamods Community");
            string jsontext = wc.DownloadString(url);
            dynamic release = JValue.Parse(jsontext);
            ri.version = release.name.ToString().Split(new char[] { '-' })[1];
            foreach(dynamic asset in release.assets)
            {
                if (signed)
                {
                    string name = asset.name.ToString();
                    if (name.ToLower().IndexOf("signed") > -1)
                    {
                        ri.url = asset.browser_download_url.ToString();
                        return ri;
                    }
                }
                else
                {
                    string name = asset.name.ToString();
                    if (name.ToLower().IndexOf("signed") == -1 && name.ToLower().IndexOf(".zip") > -1)
                    {
                        ri.url = asset.browser_download_url.ToString();
                        return ri;
                    }
                }
            }
            return ri;
        }
    }
}