using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Setup.Registry
{
    public class Uninstall
    {
        public static void CreateUninstallerKeys(string installPath)
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
            RegistryKey key;
            key = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\7thHeaven");
            if (key == null) {
                key = localKey.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\7thHeaven");
            }

            try
            {
                key.SetValue("DisplayIcon", installPath + "7th Heaven.exe");
                key.SetValue("DisplayName", "7th Heaven");
                key.SetValue("InstallLocation", installPath);
                key.SetValue("NoModify", 1);
                key.SetValue("NoRepair", 0);
                key.SetValue("URLInfoAbout", "https://7thheaven.rocks/");
                key.SetValue("UninstallString", "\"" + installPath + "setup.exe\" uninstall");
                key.SetValue("RepairPath", "\"" + installPath + "7th Heaven.exe\"");
            }
            catch (Exception) { }
        }

        public static void RemoveUninstallerKeys()
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
            localKey.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\7thHeaven");

        }

        public static string GetInstallLocation()
        {
            RegistryKey localKey, key;
            if (Environment.Is64BitOperatingSystem)
            {
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            }
            else
            {
                localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry32);
            }
            key = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\7thHeaven");
            if(key != null)
            {
                return (string)key.GetValue("InstallLocation");
            }
            return null;
        }

        public static void removeShellIntegration()
        {
            RegistryKey key = Microsoft.Win32.Registry.ClassesRoot;
            try
            {
                List<string> subkeys = key.GetSubKeyNames().Where(k => k == "Directory" || k == "7thHeaven").ToList();

                if (subkeys.Contains("Directory"))
                {
                    var dirKey = key.OpenSubKey("Directory", true);

                    if (dirKey.GetSubKeyNames().Any(k => k == "shell"))
                    {
                        var shell = dirKey.OpenSubKey("shell", true);
                        if (shell.GetSubKeyNames().Any(k => k == "Pack into IRO"))
                        {
                            shell.DeleteSubKeyTree("Pack into IRO");
                        }
                    }
                }

                if (subkeys.Contains("7thHeaven"))
                {
                    var dirKey = key.OpenSubKey("7thHeaven", true);

                    if (dirKey.GetSubKeyNames().Any(k => k == "shell"))
                    {
                        var shell = dirKey.OpenSubKey("shell", true);
                        if (shell.GetSubKeyNames().Any(k => k == "Unpack IRO"))
                        {
                            shell.DeleteSubKeyTree("Unpack IRO");
                        }
                    }
                }
            }
            catch (Exception) { }
            try
            {
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                List<string> subkeys = key.GetSubKeyNames().Where(k => k == "iros").ToList();

                if (subkeys.Contains("iros"))
                {
                    key.DeleteSubKeyTree("iros");

                }
            }
            catch (Exception){}
            try
            {
                key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software").OpenSubKey("Classes");
                List<string> subkeys = key.GetSubKeyNames().Where(k => k == "7thHeaven" || k == ".iro" || k == ".irop").ToList();

                if (subkeys.Contains("7thHeaven"))
                {
                    var progKey = key.OpenSubKey("7thHeaven", true);
                    string[] subKeys = progKey.GetSubKeyNames();

                    if (subKeys.Any(k => k == "shell"))
                    {
                        var shell = progKey.OpenSubKey("shell", true);
                        if (shell.GetSubKeyNames().Any(k => k == "open"))
                        {
                            shell.DeleteSubKeyTree("open");
                        }
                    }

                    if (subKeys.Any(k => k == ".iro"))
                    {
                        progKey.DeleteSubKeyTree(".iro");
                    }

                    if (subKeys.Any(k => k == "DefaultIcon"))
                    {
                        progKey.DeleteSubKeyTree("DefaultIcon");
                    }
                }

                if (subkeys.Contains(".iro"))
                {
                    key.DeleteSubKeyTree(".iro");
                }


                if (subkeys.Contains(".irop"))
                {
                    key.DeleteSubKeyTree(".irop");
                }
            }
            catch (Exception) { }
        }
    }
}
