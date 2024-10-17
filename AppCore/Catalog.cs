/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Iros._7th.Workshop
{
    public class Catalog
    {
        private Dictionary<Guid, Mod> _lookup;

        [System.Xml.Serialization.XmlAttribute("Name")]
        public string Name { get; set; }

        public List<Mod> Mods { get; set; }

        public Catalog()
        {
            Mods = new List<Mod>();
        }
        //https://mega.co.nz/#F!DYQWTZ7B!FWil2nif2HQF_Dr7RxvOLA

        public Mod GetMod(Guid modID)
        {
            if (_lookup == null)
            {
                _lookup = Mods.ToDictionary(m => m.ID, m => m);
            }
            Mod mod;
            _lookup.TryGetValue(modID, out mod);
            return mod;
        }

        public static Catalog Merge(Catalog c1, Catalog c2, out List<Guid> pingIDs)
        {
            Dictionary<Guid, Mod> mods = c1.Mods.ToDictionary(m => m.ID, m => m);
            pingIDs = new List<Guid>();

            foreach (var mod in c2.Mods)
            {
                Mod m;
                if (mods.TryGetValue(mod.ID, out m))
                {
                    if (mod.LatestVersion.Version > m.LatestVersion.Version || mod.MetaVersion > m.MetaVersion)
                    {
                        mods[mod.ID] = mod;
                        pingIDs.Add(mod.ID);
                    }
                }
                else
                {
                    mods[mod.ID] = mod;
                }
            }

            return new Catalog() { Mods = mods.Values.ToList() };
        }
    }

    public class ModRequirement
    {
        public string Description { get; set; }
        public Guid ModID { get; set; }
    }

    public class ModPatch
    {
        [System.Xml.Serialization.XmlAttribute("VerFrom")]
        public string VerFrom { get; set; }
        [System.Xml.Serialization.XmlAttribute("VerTo")]
        public decimal VerTo { get; set; }
        [System.Xml.Serialization.XmlText]
        public string Link { get; set; }
        [System.Xml.Serialization.XmlAttribute("DownloadSize")]
        public int DownloadSize { get; set; }

    }

    public enum LocationType
    {
        INVALID,
        Url,
        MegaSharedFolder, //Format: SharedFolderLink,FileIDString,HintFileName
        GDrive,
        ExternalUrl // iros://ExternalUrl/{url} where {url} can be left blank and it assumes the previous url in the list
    }

    public static class LocationUtil
    {
        public static bool TryParse(string link, out LocationType type, out string url)
        {
            if (link.StartsWith("iros://", StringComparison.InvariantCultureIgnoreCase)) link = link.Substring(7);
            string[] parts = link.Split(new[] { '/' }, 2);
            type = LocationType.INVALID; url = null;

            if (parts.Length < 1)
            {
                return false;
            }

            if (!Enum.TryParse<LocationType>(parts[0], out type))
            {
                return false;
            }

            if (type == LocationType.ExternalUrl && parts.Length == 1)
            {
                url = "";
                return true; // external url does not always have a url defined because it assumes the previous url
            }
            else if (parts.Length < 2)
            {
                return false;
            }

            url = parts[1];
            int dpos = url.IndexOf('$');

            if (dpos >= 0)
            {
                url = url.Substring(0, dpos) + "://" + url.Substring(dpos + 1);
            }

            return true;
        }

        public static string FormatHttpUrl(string httpUrl)
        {
            // handle empty string
            if (string.IsNullOrWhiteSpace(httpUrl))
            {
                return "";
            }

            // already formatted
            if (httpUrl.StartsWith("iros://Url/"))
            {
                return httpUrl;
            }

            return "iros://Url/" + httpUrl.Replace("://", "$");
        }
    }

    [Flags]
    public enum GameVersions
    {
        Original = 0x1,
        Rerelease = 0x2,
        Steam = 0x4,
        All = 0xff
    }
}
