using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Setup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class UninstallWindow : Window
    {
        public static string[] Args;

        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(IntPtr hinst,string lpszName,uint uType,int cxDesired,int cyDesired,uint fuLoad);

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public UninstallWindow()
        {
            InitializeComponent();

            var image = LoadImage(IntPtr.Zero, "#106", 1, SystemInformation.SmallIconSize.Width, SystemInformation.SmallIconSize.Height, 0);
            var imageSource = Imaging.CreateBitmapSourceFromHIcon(image, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            // Set button content from code
            var sp = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
            };

            sp.Children.Add(new Image { Source = imageSource, Stretch = Stretch.None });
            sp.Children.Add(new TextBlock { Text = "Uninstall", Margin = new Thickness(5, 0, 0, 0) });
            InstallBtn.Content = sp;

            if(Args.Length > 1)
            {
                if (isRunningAsAdmin() && Args[1] == "begin")
                {
                    beginRemoval();
                }
            }
        }

        private bool isRunningAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void beginRemoval()
        {
            string installPath = Registry.Uninstall.GetInstallLocation();
            DirectoryInfo dirInfo = new DirectoryInfo(installPath);
            Process[] procs = Process.GetProcessesByName("7th Heaven");

            if (procs.Length > 0)
            {
                foreach(Process procc in procs)
                {
                    procc.Kill();
                    procc.WaitForExit();
                }
            }
            
            // remove all file in the install location
            foreach (FileInfo file in dirInfo.EnumerateFiles())
            {
                if (file.Name.ToLower() != "setup.exe")
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in dirInfo.EnumerateDirectories())
                {
                    dir.Delete(true);
                }
            }
            // remove the registry keys
            Registry.Uninstall.RemoveUninstallerKeys();
            Registry.Uninstall.removeShellIntegration();

            // tell windows to refresh shell caches
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

            string batchCommands = string.Empty;
            string exeFileName = installPath + "setup.exe";

            batchCommands += "@ECHO OFF\n";
            batchCommands += "del /F \"";
            batchCommands +=  exeFileName + "\"\n";
            batchCommands += "rmdir \"" + installPath + "\" \n";
            batchCommands += "PowerShell -Command \"Add-Type -AssemblyName PresentationFramework;[System.Windows.MessageBox]::Show('7th Heaven has been successfully removed from your machine', '7th Heaven Setup')\"\n";
            batchCommands += "del deleteMyProgram.bat\n pause";
                        
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = System.IO.Path.GetTempPath();
            startInfo.FileName = "deleteMyProgram.bat";

            File.WriteAllText(startInfo.WorkingDirectory+startInfo.FileName, batchCommands);
            Process proc = Process.Start(startInfo);
            System.Windows.Application.Current.Shutdown(0);
        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            InstallBtn.IsEnabled = false;
            if (isRunningAsAdmin())
            {
                beginRemoval();
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                startInfo.Arguments = "uninstall begin";
                startInfo.Verb = "runas";
                try
                {
                    Process proc = Process.Start(startInfo);
                    System.Windows.Application.Current.Shutdown(0);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return;
                }
            }
        }
    }
}
