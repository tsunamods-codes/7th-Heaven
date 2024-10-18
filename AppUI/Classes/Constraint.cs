using AppCore;
using Iros.Workshop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppUI.Classes
{
    internal class Constraint
    {
        public Guid ModID { get; set; }
        public string Setting { get; set; }
        public List<int> Require { get; set; }
        public HashSet<int> Forbid { get; set; }

        /// <summary>
        /// dictionary of mods participating in constraint with Key being Mod ID and Value being Mod Name
        /// </summary>
        public Dictionary<Guid, string> ParticipatingMods { get; set; }
        public AppWrapper.ConfigOption Option { get; set; }

        public Constraint()
        {
            Forbid = new HashSet<int>();
            Require = new List<int>();
            ParticipatingMods = new Dictionary<Guid, string>();
        }

        public bool Verify(out string message)
        {
            message = null;
            if (Option == null) return true; //setting no longer exists, constraints are irrelevant?
            var pItem = Sys.ActiveProfile.ActiveItems.Find(pi => pi.ModID.Equals(ModID));
            var inst = Sys.Library.GetItem(pItem.ModID);
            var setting = pItem.Settings.Find(s => s.ID.Equals(Setting, StringComparison.InvariantCultureIgnoreCase));
            if (setting == null)
            {
                setting = new ProfileSetting() { ID = Setting, Value = Option.Default };
                pItem.Settings.Add(setting);
            }

            string modsList = String.Join("\n", ParticipatingMods.Select(p => $"{p.Value} | ({p.Key})")); // list of mods participating in constraint

            if (Require.Any() && (Require.Min() != Require.Max()))
            {
                message = String.Format(ResourceHelper.Get(StringKey.ModSettingNoCompatibleOptionFoundTheFollowingModsRestrictIt), inst.CachedDetails.Name, Option.Name, modsList);
                return false;
            }

            if (Require.Any() && Forbid.Contains(Require[0]))
            {
                message = String.Format(ResourceHelper.Get(StringKey.ModSettingNoCompatibleOptionFoundTheFollowingModsRestrictIt), inst.CachedDetails.Name, Option.Name, modsList);
                return false;
            }
            if (Option.Values.All(o => Forbid.Contains(o.Value)))
            {
                message = String.Format(ResourceHelper.Get(StringKey.ModSettingNoCompatibleOptionFoundTheFollowingModsRestrictIt), inst.CachedDetails.Name, Option.Name, modsList);
                return false;
            }
            if (Require.Any() && (setting.Value != Require[0]))
            {
                var opt = Option.Values.Find(v => v.Value == Require[0]);
                if (opt == null)
                {
                    message = String.Format(ResourceHelper.Get(StringKey.ModSettingNoCompatibleOptionFoundTheFollowingModsRestrictIt), inst.CachedDetails.Name, Option.Name, modsList);
                    return false;
                }
                setting.Value = Require[0];
                message = String.Format(ResourceHelper.Get(StringKey.ModChangedSettingTo), inst.CachedDetails.Name, Option.Name, opt.Name);
            }
            else if (Forbid.Contains(setting.Value))
            {
                setting.Value = Option.Values.First(v => !Forbid.Contains(v.Value)).Value;
                var opt = Option.Values.Find(v => v.Value == setting.Value);
                message = String.Format(ResourceHelper.Get(StringKey.ModChangedSettingTo), inst.CachedDetails.Name, Option.Name, opt.Name);
            }
            return true;
        }
    }
}
