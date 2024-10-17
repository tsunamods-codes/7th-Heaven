using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppUI.Classes
{
    public class SupportedResolution
    {
        public UInt32 H { get; private set; }
        public UInt32 V { get; private set; }

        private SupportedResolution(UInt32 h, UInt32 v)
        {
            H = h;
            V = v;
        }

        private static Dictionary<string, SupportedResolution> cache = new Dictionary<string, SupportedResolution>();

        public static SupportedResolution CreateIfNotExist(UInt32 h, UInt32 v)
        {
            string key = $"{h}x{v}";
            if (!cache.ContainsKey(key))
            {
                cache[key] = new SupportedResolution(h, v);
            }
            return cache[key];
        }

        public static List<SupportedResolution> GetAllCachedResolutions()
        {
            return cache.Values.ToList();
        }
    }
}
