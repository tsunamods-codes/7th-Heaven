/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using AppCore;
using AppWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Iros._7th.Workshop {
    public class Profile {
        public List<ProfileItem> Items { get; set; }
        public string OpenGLConfig { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public List<ProfileItem> ActiveItems
        {
            get
            {
                return Items?.Where(m => m.IsModActive).ToList();
            }
        }

        public Profile() {
            Items = new List<ProfileItem>();
        }

        public IEnumerable<string> GetDetails() 
        {
            List<string> profileDetails = new List<string>();

            profileDetails.Add($"App Version: {Sys.AppVersion}\n");

            foreach (ProfileItem item in Items) 
            {
                InstalledItem mod = Sys.Library.GetItem(item.ModID);

                if (mod != null) 
                {
                    Mod details = mod.CachedDetails;

                    if (details != null) 
                    {
                        profileDetails.Add(String.Format("# {0}", details.Name));
                        profileDetails.Add(String.Format("\tID: {0}", details.ID));
                        profileDetails.Add(String.Format("\tVersion: {0}", mod.LatestInstalled.VersionDetails.Version));
                    } 
                    else
                    {
                        profileDetails.Add(String.Format("\tModID {0}", mod.ModID));
                    }

                    profileDetails.Add(String.Format("\tIs Active: {0}", item.IsModActive));




                    ModInfo info = mod.GetModInfo();
                    string detailFormat = "";

                    if (info != null)
                    {
                        detailFormat = item.GetFormatString(info);
                    }

                    foreach (ProfileSetting config in item.Settings.ToList()) 
                    {
                        if (info != null)
                        {
                            // extract configuration variable name and name of the selected value from Mod Info
                            ConfigOption configOption = info.Options.FirstOrDefault(o => o.ID == config.ID);
                            string optionValue = "";

                            if (configOption == null)
                            {
                                // the option ID may have changed from last time so skip it (that means the active profile item is out of date but opening the configure mod window would refresh it)
                                item.Settings.Remove(config);
                                continue;
                            }

                            if (configOption.Type == OptionType.Bool)
                            {
                                optionValue = (config.Value == 1).ToString();
                            }
                            else
                            {
                                optionValue = $"\"{configOption.Values.Where(o => o.Value == config.Value).Select(o => o.Name).FirstOrDefault()}\"";
                            }


                            string name = $"\"{configOption.Name}\"";
                            string id = $"({config.ID})";
                            string value = $"({config.Value})";

                            profileDetails.Add(string.Format(detailFormat, name, id, "=", optionValue, value)); // "=" is passed into string.Format() so all equal signs are aligned using the {2, 2} syntax
                        }
                        else
                        {
                            profileDetails.Add(String.Format("\t{0} = {1}", config.ID, config.Value));
                        }
                    }
                }
            }

            return profileDetails;
        }

        public void AddItem(ProfileItem toAdd)
        {
            if (!Items.Any(p => p.ModID == toAdd.ModID))
            {
                Items.Add(toAdd);
            }
        }

        public ProfileItem GetItem(Guid modID)
        {
            return Items.FirstOrDefault(m => m.ModID == modID);
        }

        public bool HasItem(Guid modID)
        {
            return Items?.Any(m => m.ModID == modID) == true;
        }

        public void RemoveDeletedItems(bool doWarn = false)
        {
            List<string> removedMods = new List<string>();

            foreach (ProfileItem item in Items.ToList())
            {
                if (Sys.Library.GetItem(item.ModID) == null)
                {
                    removedMods.Add(item.Name);
                    Items.Remove(item);
                }
            }

            if (removedMods.Count > 0 && doWarn)
            {
                Sys.Message(new WMessage($"[{StringKey.TheFollowingModsWereNotFoundInstalledAndHaveBeenRemoved}]: {string.Join(", ", removedMods)}", true) { TextTranslationKey = StringKey.TheFollowingModsWereNotFoundInstalledAndHaveBeenRemoved });
            }
        }
    }

    public class ProfileItem {
        public Guid ModID { get; set; }
        public List<ProfileSetting> Settings { get; set; }

        /// <summary>
        /// Name is only saved for reference in case a mod is not installed (so the name is at least known)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Custom category saved to a profile
        /// </summary>
        public string Category { get; set; }

        public bool IsModActive { get; set; }

        private static string[] _comparison = new[] { "=", "!=", "<", ">", "<=", ">=" };
        private static List<string> _comparisonL = _comparison.ToList();

        private bool IsConfigActive(string spec) {
            if (String.IsNullOrWhiteSpace(spec)) return true;

            string[] parts = spec.Split(_comparison, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;
            parts = new[] { parts[0].Trim(), spec.Substring(parts[0].Length, spec.Length - parts[1].Length - parts[0].Length), parts[1].Trim() };

            var conf = new ProfileSetting();

            if (parts[0].StartsWith("ffnx_", StringComparison.InvariantCultureIgnoreCase))
            {
                var key = parts[0].Substring(5);
                if (Sys.FFNxConfig.HasKey(key)) conf = new ProfileSetting() { ID = key, Value = int.Parse(Sys.FFNxConfig.Get(key)) };
            }
            else
            {
                conf = Settings.Find(c => c.ID.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));
                if (conf == null) return false;
            }

            int val;
            if (!int.TryParse(parts[2], out val)) return false;

            switch (_comparisonL.IndexOf(parts[1])) {
                case 0:
                    return val == conf.Value;
                case 1:
                    return val != conf.Value;
                case 2:
                    return val < conf.Value;
                case 3:
                    return val > conf.Value;
                case 4:
                    return val <= conf.Value;
                case 5:
                    return val >= conf.Value;
                default:
                    return false;
            }
        }

        private bool IsActive(AppWrapper.ActiveWhen aw) {
            if (aw == null) return true;
            return aw.IsActive(IsConfigActive);
        }

        public AppWrapper.RuntimeMod GetRuntime(AppWrapper.LoaderContext context) {
            var mod = Sys.Library.GetItem(ModID);
            if (mod == null) return null;
            string location = System.IO.Path.Combine(Sys.Settings.LibraryLocation, mod.LatestInstalled.InstalledLocation);
            AppWrapper.ModInfo modinfo = null;
            if (mod.LatestInstalled.InstalledLocation.EndsWith(".iro")) {
                using (var arc = new AppWrapper.IrosArc(location)) {
                    if (arc.HasFile("mod.xml")) {
                        var doc = new System.Xml.XmlDocument();
                        doc.Load(arc.GetData("mod.xml"));
                        modinfo = new AppWrapper.ModInfo(doc, context);
                    }
                }
            } else {
                string mfile = System.IO.Path.Combine(location, "mod.xml");
                if (System.IO.File.Exists(mfile))
                    modinfo = new AppWrapper.ModInfo(mfile, context);
            }
            modinfo = modinfo ?? new AppWrapper.ModInfo();

            foreach (var opt in modinfo.Options) {
                if (!Settings.Any(s => s.ID.Equals(opt.ID, StringComparison.InvariantCultureIgnoreCase)))
                    Settings.Add(new ProfileSetting() { ID = opt.ID, Value = opt.Default });
            }

            return new AppWrapper.RuntimeMod(
                location,
                modinfo.Conditionals.Where(f => IsActive(f.ActiveWhen)),
                modinfo.ModFolders.Where(f => IsActive(f.ActiveWhen)).Select(f => f.Folder),
                modinfo
                );
        }

        /// <summary>
        /// returns a string that can be used with String.Format() for displaying ConfigOption values and names in tabbed columns.
        /// Calculates what indendation to use for each column by looking at the max length of the variable names/values
        /// </summary>
        /// <returns>a string in the format "\t{0, -XX} {1, -XX} {2, 2} {3, -XX} {4, -10}" where XX is calculated</returns>
        internal string GetFormatString(ModInfo info)
        {
            int longestNameLength = 0;
            int longestVarLength = Settings.Count > 0 ? Settings.Select(s => s.ID.Length).Max() + 3 : 0; // use LINQ to get max length of a setting variable ID
            int longestValueNameLength = 4; // length of 'True' 

            // loop over each setting and get the max length of the variable name and variables value name
            foreach (var config in Settings)
            {
                ConfigOption configOption = info.Options.FirstOrDefault(o => o.ID == config.ID);
                if (configOption?.Name.Length > longestNameLength)
                {
                    longestNameLength = configOption.Name.Length;
                }

                if (configOption?.Type == OptionType.List)
                {
                    string value = configOption.Values.Where(o => o.Value == config.Value).Select(o => o.Name).FirstOrDefault();
                    if (value?.Length > longestValueNameLength)
                    {
                        longestValueNameLength = value.Length;
                    }
                }
            }

            longestNameLength += 3; // add 3 to account for the quotes wrapping name and space e.g. "Aerith" 
            longestValueNameLength += 3;

            string format = "\t{0, -" + longestNameLength.ToString() + "} {1, -" + longestVarLength + "} {2, 2} {3, -" + longestValueNameLength + "} {4, -10}";
            return format;
        }
    }

    public class ProfileSetting {
        public string ID { get; set; }
        public int Value { get; set; }
    }
}
