using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Setup.registry
{
    public class Steam
    {
        private static string path = "";
        public static bool getFF7SteamInstalled()
        {
            if (path == "")
            {
                RegistryKey localKey;
                if (Environment.Is64BitOperatingSystem)
                {
                    localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
                }
                else
                {
                    localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
                }
                RegistryKey key = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 39140");
                string regPath = key.GetValue("InstallLocation").ToString();
                if (regPath == "INVALIDPATH" || regPath == null)
                {
                    return false;
                }
                else
                {
                    path = regPath;
                    return true;
                }
            }
            return true;
        }

        public static string getFF7SteamPath()
        {
            if (path != "")
            {
                return path;
            }
            else
            {
                string regPath = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 39140", "InstallLocation", "INVALIDPATH");
                if (regPath == "INVALIDPATH")
                {
                    return "";
                }
                else
                {
                    path = regPath;
                    return path;
                }
            }
        }
    }
}
