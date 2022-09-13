/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

namespace _7thWrapperLib {
    public class LGPWrapper {
        public static OverrideFile MapFile(string file, RuntimeProfile profile)
        {
            foreach (var item in profile.Mods)
            {
                var entry = item.GetOverride(file);
                if (entry != null)
                {
                    DebugLogger.WriteLine($"File {file} overridden by {entry.Archive}{entry.File}");
                    return entry;
                }
            }
            return null;
        }
    }
}
