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
    public class Releases
    {
        public enum Channel
        {
            Stable = 0,
            Beta = 1,
            Alpha = 2,
            Canary = 3
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

        public string GetReleaseJSON(Channel channel)
        {
            string filePath = GetUpdateInfoPath();
            string url;
            signed = false;
            switch (channel)
            {
                case Channel.Beta:
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/beta";
                    break;
                case Channel.Alpha:
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/alpha";
                    break;
                case Channel.Canary:
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/tags/canary";
                    break;
                default:
                case Channel.Stable:
                    url = "https://api.github.com/repos/tsunamods-codes/7th-Heaven/releases/latest";
                    signed = true;
                    break;
            }

            WebClient wc = new WebClient();
            wc.Headers.Add(HttpRequestHeader.UserAgent, "7th Heaven Updater - Tsunamods Community");
            string jsontext = wc.DownloadString(url);
            dynamic release = JValue.Parse(jsontext);
            foreach(dynamic asset in release.assets)
            {
                if (signed)
                {
                    string name = asset.name.ToString();
                    if (name.ToLower().IndexOf("signed") > -1)
                    {
                        return asset.browser_download_url.ToString();
                    }
                }
            }
            return null;
        }
    }
}