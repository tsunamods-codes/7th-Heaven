using Iros._7th.Workshop;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace _7thHeaven.Code
{
    public enum FF7RegKey
    {
        SteamKeyPath,
        RereleaseKeyPath,
        SquareSoftKeyPath,
        FF7AppKeyPath,
        VirtualStoreKeyPath
    }

    public static class RegistryHelper
    {

        private static bool isAtomicTransaction = false;

        private static List<string> transaction = new();

        public const string SteamKeyPath64Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 39140";

        public const string SteamKeyPath32Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 39140";

        public const string RereleaseKeyPath64Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{141B8BA9-BFFD-4635-AF64-078E31010EC3}_is1";

        public const string RereleaseKeyPath32Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{141B8BA9-BFFD-4635-AF64-078E31010EC3}_is1";

        public const string SquareSoftKeyPath64Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Square Soft, Inc.";

        public const string SquareSoftKeyPath32Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Square Soft, Inc.";

        public const string FF7AppKeyPath64Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\App Paths\FF7app.exe";

        public const string FF7AppKeyPath32Bit = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\FF7app.exe";

        public const string VirtualStoreKeyPath64Bit = @"HKEY_CURRENT_USER\SOFTWARE\Classes\VirtualStore\MACHINE\SOFTWARE\Wow6432Node\Square Soft, Inc.";

        public const string VirtualStoreKeyPath32Bit = @"HKEY_CURRENT_USER\SOFTWARE\Classes\VirtualStore\MACHINE\SOFTWARE\Square Soft, Inc.";

        /// <summary>
        /// Returns the path to the Registry Key associated with the given <paramref name="regKey"/> for the correct OS Bitness.
        /// </summary>
        public static string GetKeyPath(FF7RegKey regKey)
        {
            return GetKeyPath(regKey, use32BitKey: !Environment.Is64BitOperatingSystem);
        }

        /// <summary>
        /// Returns the path to the Registry Key associated with the given <paramref name="regKey"/>.
        /// The returned path can be for 32-bit or 64-bit OS depending on <paramref name="use32BitKey"/>
        /// </summary>
        public static string GetKeyPath(FF7RegKey regKey, bool use32BitKey)
        {
            if (regKey == FF7RegKey.SteamKeyPath)
            {
                if (use32BitKey)
                {
                    return SteamKeyPath32Bit;
                }
                else
                {
                    return SteamKeyPath64Bit;
                }
            }

            if (regKey == FF7RegKey.RereleaseKeyPath)
            {
                if (use32BitKey)
                {
                    return RereleaseKeyPath32Bit;
                }
                else
                {
                    return RereleaseKeyPath64Bit;
                }
            }

            if (regKey == FF7RegKey.FF7AppKeyPath)
            {
                if (use32BitKey)
                {
                    return FF7AppKeyPath32Bit;
                }
                else
                {
                    return FF7AppKeyPath64Bit;
                }
            }

            if (regKey == FF7RegKey.SquareSoftKeyPath)
            {
                if (use32BitKey)
                {
                    return SquareSoftKeyPath32Bit;
                }
                else
                {
                    return SquareSoftKeyPath64Bit;
                }
            }

            if (regKey == FF7RegKey.VirtualStoreKeyPath)
            {
                if (use32BitKey)
                {
                    return VirtualStoreKeyPath32Bit;
                }
                else
                {
                    return VirtualStoreKeyPath64Bit;
                }
            }

            return "";
        }

        /// <summary>
        /// Returns the current user or local machine RegistryKey based on what the key path starts with.
        /// (Opens the key with the correct RegistryView based on the OS Bitness)
        /// </summary>
        private static RegistryKey GetBaseKey(string fullRegPath, bool force32View = false)
        {
            RegistryView view = RegistryView.Registry32;
            if (Environment.Is64BitOperatingSystem && !force32View)
            {
                view = RegistryView.Registry64;
            }

            if (fullRegPath.StartsWith("HKEY_LOCAL_MACHINE"))
            {
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
            }
            else if (fullRegPath.StartsWith("HKEY_CURRENT_USER"))
            {
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
            }
            else if (fullRegPath.StartsWith("HKEY_CLASSES_ROOT"))
            {
                return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view);
            }

            return null;
        }

        /// <summary>
        /// Removes 'HKEY_LOCAL_MACHINE\' or 'HKEY_CURRENT_USER\' from <paramref name="fullRegKeyPath"/>
        /// </summary>
        /// <returns> modified registry key path </returns>
        private static string RemoveBaseKeyFromPath(string fullRegKeyPath)
        {
            return fullRegKeyPath.Replace(@"HKEY_LOCAL_MACHINE\", "").Replace(@"HKEY_CURRENT_USER\", "").Replace(@"HKEY_CLASSES_ROOT\", ""); ;
        }

        private static string ReplaceBaseKeyFromPath(string fullRegKeyPath)
        {
            if (fullRegKeyPath.StartsWith("HKEY_LOCAL_MACHINE"))
            {
                return fullRegKeyPath.Replace("HKEY_LOCAL_MACHINE", "HKLM");
            }
            else if (fullRegKeyPath.StartsWith("HKEY_CURRENT_USER"))
            {
                return fullRegKeyPath.Replace("HKEY_CURRENT_USER", "HKCU");
            }
            else if (fullRegKeyPath.StartsWith("HKEY_CLASSES_ROOT"))
            {
                return fullRegKeyPath.Replace("HKEY_CLASSES_ROOT", "HKCR");
            }

            return String.Empty;
        }

        public static object GetValue(FF7RegKey key, string valueName, object defaultVal = null)
        {
            return GetValue(GetKeyPath(key), valueName, defaultVal);
        }

        public static object GetValue(string key, string valueName, object defaultVal = null)
        {
            RegistryKey rootKey = GetBaseKey(key);

            if (rootKey == null)
            {
                return defaultVal;
            }

            // remove from key path if exists since not needed to open a Subkey
            string fullRegPath = RemoveBaseKeyFromPath(key);

            RegistryKey regKey = rootKey.OpenSubKey(fullRegPath, false);

            if (regKey == null)
            {
                return defaultVal;
            }

            return regKey.GetValue(valueName, defaultVal);
        }

        /// <summary>
        /// Sets the name/value pair in the specified Registry Key. Creates the Registry Key if it does not exist already.
        /// </summary>
        public static bool SetValue(string key, string valueName, object value, RegistryValueKind regKind = RegistryValueKind.String)
        {
            string regType = String.Empty;

            switch (regKind)
            {
                case RegistryValueKind.Binary:
                    regType = "REG_BINARY";
                    if (value is Guid) value = $"\"{BitConverter.ToString(((Guid)value).ToByteArray()).ToLower().Replace("-","")}\"";
                    break;
                case RegistryValueKind.DWord:
                    regType = "REG_DWORD";
                    value = $"\"{value}\"";
                    break;
                case RegistryValueKind.ExpandString:
                    regType = "REG_EXPAND_SZ";
                    break;
                case RegistryValueKind.MultiString:
                    regType = "REG_MULTI_SZ";
                    break;
                case RegistryValueKind.None:
                    regType = "REG_NONE";
                    break;
                case RegistryValueKind.QWord:
                    regType = "REG_QWORD";
                    break;
                case RegistryValueKind.String:
                case RegistryValueKind.Unknown:
                    regType = "REG_SZ";
                    bool lastCharIsEscape = value.ToString()[value.ToString().Length - 1].Equals('\\');
                    value = $"\"{value.ToString().Replace("\"", "\"\"").Replace("%", "%%")}{(lastCharIsEscape ? "\\" : "")}\"";
                    break;
            }

            RegistryKey rootKey = GetBaseKey(key);

            if (rootKey == null)
            {
                return false;
            }

            // remove from key path if exists since not needed to open a Subkey
            string fullRegPath = RemoveBaseKeyFromPath(key);

            if (valueName == String.Empty)
                valueName = "/ve";
            else
                valueName = $"/v \"{valueName}\"";

            return ExecReg($"add \"{key}\" /f {valueName} /d {value} /t {regType}"); ;
        }

        /// <summary>
        /// Uses reg.exe to export a registry key to specified .reg file
        /// </summary>
        /// <param name="regKey"> registry key to export </param>
        /// <param name="savePath"> full path to .reg file to save to </param>
        /// <returns></returns>
        public static bool ExportKey(string regKey, string savePath)
        {
            return ExecReg($"export \"{regKey}\" \"{savePath}\" /y");
        }

        /// <summary>
        /// Deletes a given Registry Key and any child subkeys
        /// </summary>
        public static bool DeleteKey(string keyPath)
        {
            RegistryKey rootKey = GetBaseKey(keyPath);

            if (rootKey == null)
            {
                return false;
            }


            try
            {
                return ExecReg($"delete \"{ReplaceBaseKeyFromPath(keyPath)}\" /f");
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns list of Value Names of a Registry Key
        /// </summary>
        public static List<string> GetValueNamesFromKey(string keyPath)
        {
            RegistryKey rootKey = GetBaseKey(keyPath);

            if (rootKey == null)
            {
                return new List<string>();
            }

            try
            {
                string fullRegPath = RemoveBaseKeyFromPath(keyPath);
                RegistryKey regKey = rootKey.OpenSubKey(fullRegPath, false);

                return regKey.GetValueNames().ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Deletes a specified Value from a Registry Key
        /// </summary>
        public static bool DeleteValueFromKey(string keyPath, string valueName)
        {
            RegistryKey rootKey = GetBaseKey(keyPath);

            if (rootKey == null)
            {
                return false;
            }

            try
            {
                string fullRegPath = RemoveBaseKeyFromPath(keyPath);
                RegistryKey regKey = rootKey.OpenSubKey(fullRegPath, true);

                if (regKey == null)
                {
                    // check if subkey can be opened in 32-bit view
                    rootKey = GetBaseKey(keyPath, force32View: true);
                    regKey = rootKey?.OpenSubKey(fullRegPath, true);

                    if (regKey == null)
                    {
                        return false;
                    }
                }

                return ExecReg($"delete \"{keyPath}\" /v {valueName} /f");
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Update Registry with new value if it has changed from the current value in the Registry.
        /// Returns true if changed.
        /// </summary>
        public static bool SetValueIfChanged(string regKeyPath, string regValueName, object newValue, RegistryValueKind valueKind = RegistryValueKind.String)
        {
            object currentValue = GetValue(regKeyPath, regValueName, null);
            bool isValuesEqual;

            if (newValue is Guid)
            {
                bool isCurrentValueValid = currentValue != null && (currentValue as byte[]).Length == 16;
                isValuesEqual = isCurrentValueValid && new Guid(currentValue as byte[]).Equals(newValue);
            }
            else
            {
                isValuesEqual = currentValue != null && currentValue.Equals(newValue);
            }

            if (!isValuesEqual)
            {
                SetValue(regKeyPath, regValueName, newValue, valueKind);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the Reg binary with admin permissions when required to do changes on Registry
        /// Returns true if succeded.
        /// </summary>
        private static bool ExecReg(string args)
        {
            string bitness = "32";
            if (Environment.Is64BitOperatingSystem)
            {
                bitness = "64";
            }

            if (isAtomicTransaction)
            {
                transaction.Add($"@reg.exe {args} /reg:{bitness}");
                return true;
            }
            else
            {
                Process proc = new Process();

                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo("reg.exe")
                    {
                        Arguments = $"{args} /reg:{bitness}",
                        Verb = "runas",
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };
                    proc = Process.Start(startInfo);

                    if (proc != null) proc.WaitForExit();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    if (proc != null) proc.Dispose();
                }
            }
        }

        public static void BeginTransaction()
        {
            isAtomicTransaction = true;
        }

        public static void CommitTransaction()
        {
            string fileName = Path.Combine(Sys.PathToTempFolder, "registry_transaction.bat");

            System.IO.File.WriteAllText(
                fileName,
                $"@echo off\n" + String.Join("\n", transaction)
            );

            if (transaction.Count > 0)
            {
                try
                {
                    // Execute temp batch script with admin privileges
                    ProcessStartInfo startInfo = new ProcessStartInfo(fileName)
                    {
                        Verb = "runas",
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };

                    // Launch process, wait and then save exit code
                    using (Process temp = Process.Start(startInfo))
                    {
                        temp.WaitForExit();
                    }

                    transaction.Clear();
                }
                catch (Exception e)
                {
                    Sys.Message(new WMessage() { Text = $"Error while trying to update 7thHeaven settings", LoggedException = e });
                }
            }

            isAtomicTransaction = false;
        }
    }
}
