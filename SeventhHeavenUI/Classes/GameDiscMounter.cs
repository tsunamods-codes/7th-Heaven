using Iros._7th.Workshop;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace _7thHeaven.Code
{
    public class GameDiscMounter
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        MountDiscOption MountOption { get; set; }

        public GameDiscMounter(MountDiscOption mountingOption)
        {
            MountOption = mountingOption;
        }

        /// <summary>
        /// Mounts FF7DISC1.ISO in 'Resources' folder to virtual drive
        /// </summary>
        /// <remarks>
        /// This will force use WinCDEmu if OS does not support powershell mounting
        /// </remarks>
        public bool MountVirtualGameDisc()
        {
            if(MountOption == MountDiscOption.DoNotMount)
            {
                return true;
            }

            bool usePowerShell = MountOption == MountDiscOption.MountWithPowerShell;

            if (!OSHasBuiltInMountSupport())
            {
                Logger.Info($"OS does not support PowerShell Mount-DiskImage... forcing option to mount with WinCDEmu ...");
                usePowerShell = false;
            }

            try
            {
                string isoPath = Path.Combine(Sys._7HFolder, "Resources", "FF7DISC1.ISO");
                Logger.Info($"\tattempting to mount iso at {isoPath}");

                if (!File.Exists(isoPath))
                {
                    return false;
                }

                if (usePowerShell)
                {
                    return MountWithPowershell(isoPath);
                }
                else
                {
                    return MountWithWinCDEmu(isoPath);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Unmounts FF7DISC1
        /// </summary>
        /// <remarks>
        /// This assumes the iso image at Path.Combine(Sys._7HFolder, "Resources", "FF7DISC1.ISO") is the mounted iso.
        /// ... this code does not handle unmounting other iso files.
        /// </remarks>
        /// <returns></returns>
        public bool UnmountVirtualGameDisc()
        {
            if (MountOption == MountDiscOption.DoNotMount)
            {
                return true;
            }

            bool usePowerShell = MountOption == MountDiscOption.MountWithPowerShell;

            if (!OSHasBuiltInMountSupport())
            {
                usePowerShell = false;
            }

            try
            {
                string isoPath = Path.Combine(Sys._7HFolder, "Resources", "FF7DISC1.ISO");

                if (!File.Exists(isoPath))
                {
                    return false;
                }

                if (usePowerShell)
                {
                    UnmountWithPowershell(isoPath);
                }
                else
                {
                    UnmountWithWinCDEmu(isoPath);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Invokes PowerShell command 'Mount-DiskImage' with given <paramref name="isoPath"/>
        /// </summary>
        private bool MountWithPowershell(string isoPath)
        {
            try
            {
                if (!File.Exists(isoPath))
                {
                    return false;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo("PowerShell.exe")
                {
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = $"Mount-DiskImage -ImagePath '{isoPath}'"

                };
                Process.Start(startInfo);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Invokes PowerShell command 'Dismount-DiskImage' for given <paramref name="isoPath"/>
        /// </summary>
        private bool UnmountWithPowershell(string isoPath)
        {
            try
            {
                if (!File.Exists(isoPath))
                {
                    return false;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo("PowerShell.exe")
                {
                    CreateNoWindow = false,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = $"Dismount-DiskImage -ImagePath '{isoPath}'",
                };
                Process.Start(startInfo);

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Invokes PortableWinCDEmu.exe process with <paramref name="isoPath"/> as argument to mount image
        /// </summary>
        /// <returns>true if succeeded; false otherwise</returns>
        private bool MountWithWinCDEmu(string isoPath)
        {
            try
            {
                if (!File.Exists(isoPath))
                {
                    return false;
                }

                InstallWinCDEmuDriver();
                RunWinCDEmuWithArguments($"\"{isoPath}\"");

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Invokes PortableWinCDEmu.exe process with arguments '/unmount <paramref name="isoPath"/>'
        /// </summary>
        /// <returns>true if succeeded; false otherwise</returns>
        private bool UnmountWithWinCDEmu(string isoPath)
        {
            try
            {
                if (!File.Exists(isoPath))
                {
                    return false;
                }

                InstallWinCDEmuDriver();
                RunWinCDEmuWithArguments($"/unmount \"{isoPath}\"");

                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }
        }

        /// <summary>
        /// Invokes PortableWinCDEmu.exe process with argument '/install' to ensure the driver is installed
        /// </summary>
        /// <remarks>
        /// It is safe to run this command multiple times because the process will check if the driver is already installed - the output is 'The driver is already installed'
        /// If the driver installs successfully then the output of the command is 'The operation completed successfully.'
        /// </remarks>
        private void InstallWinCDEmuDriver()
        {
            RunWinCDEmuWithArguments("/install");
        }

        /// <summary>
        /// Launches PortableWinCDEmu.exe with the given <paramref name="arguments"/> and waits for the process to exit (with timeout of <paramref name="timeoutInSeconds"/>)
        /// </summary>
        private void RunWinCDEmuWithArguments(string arguments, int timeoutInSeconds = 10)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Sys.PathToWinCDEmuExe, arguments)
            {
                WorkingDirectory = Path.GetDirectoryName(Sys.PathToWinCDEmuExe),
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            using (Process winCd = Process.Start(startInfo))
            {
                winCd.WaitForExit(timeoutInSeconds * 1000);
            }
        }


        /// <summary>
        /// Returns true if OS is Win 8+ which has built in ISO mounting
        /// </summary>
        internal static bool OSHasBuiltInMountSupport()
        {
            Version osVersion = Environment.OSVersion.Version;
            if (osVersion.Major < 6)
            {
                return false;
            }
            else if (osVersion.Major == 6)
            {
                if (osVersion.Minor < 2)
                {
                    return false; // on an OS below Win 8
                }
            }

            return true;
        }

    }
}
