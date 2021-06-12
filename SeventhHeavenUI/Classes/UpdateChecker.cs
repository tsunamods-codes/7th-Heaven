using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using Newtonsoft.Json.Linq;
using SeventhHeaven.Windows;
using SeventhHeavenUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SeventhHeaven.Classes
{
    public class UpdateChecker
    {
        private FileVersionInfo _currentAppVersion = null;

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public void CheckForUpdates(Updater.GitHub.Releases.Channel channel)
        {
            try
            {
                _currentAppVersion = FileVersionInfo.GetVersionInfo(
                    Path.Combine(Sys._7HFolder, $"{App.GetAppName()}.exe")
                );
            }
			
            catch (FileNotFoundException e)
            {
                _currentAppVersion = null;
                Sys.Message(new WMessage() { Text = $"Could not get application version ", LoggedException = e });
            }
            
            if(_currentAppVersion != null)
            {
                Updater.GitHub.NewReleaseVersionInfo releaseVersioninfo = Updater.GitHub.Releases.isNewerVersion(new Version(GetCurrentAppVersion()), channel);
                if (releaseVersioninfo.isNewer)
                {
                    if(MessageDialogWindow.Show(
                        string.Format(ResourceHelper.Get(StringKey.AppUpdateIsAvailableMessage), $"{App.GetAppName()} - {App.GetAppVersion()}", releaseVersioninfo.version.ToString(), releaseVersioninfo.changeLog),
                        ResourceHelper.Get(StringKey.NewVersionAvailable),
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question
                    ).Result == System.Windows.MessageBoxResult.Yes)
                    {
                        Sys.Message(new WMessage() { Text = "Sarting updater application" });
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.UseShellExecute = false;
                        startInfo.WorkingDirectory = Environment.CurrentDirectory;
                        startInfo.FileName = "updater.exe";
                        startInfo.Arguments = "\"" + System.AppDomain.CurrentDomain.BaseDirectory + "\\\" v"+ 
                            String.Format("{0}.{1}.{2}", releaseVersioninfo.version.Major, releaseVersioninfo.version.Minor, releaseVersioninfo.version.Build);
                        Process proc = Process.Start(startInfo);
                        IntPtr hWnd = proc.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            SetForegroundWindow(hWnd);
                            ShowWindow(hWnd, int.Parse("9"));
                        }
                        App.ShutdownApp();
                    }
                }
            }
        }
    }

}
