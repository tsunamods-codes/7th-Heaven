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
    public partial class MainWindow : Window
    {
        public static string[] Args;
        public static bool repair;

        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(IntPtr hinst,string lpszName,uint uType,int cxDesired,int cyDesired,uint fuLoad);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public MainWindow()
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
            sp.Children.Add(new TextBlock { Text = repair?"Repair":"Install", Margin = new Thickness(5, 0, 0, 0) });
            InstallBtn.Content = sp;

            if (repair)
            {
                PathInp.Text = Registry.Uninstall.GetInstallLocation();
                BrowserBtn.Visibility = Visibility.Hidden;
                Thickness instBtnPos = InstallBtn.Margin;
                instBtnPos.Top = 253;
                InstallBtn.Margin = instBtnPos;
            }

            if(Args.Length > 1)
            {
                PathInp.Text = Args[1];
            }

            if(PathInp.Text == "")
            {
                PathInp.Text = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\7th Heaven\\";
            }

            if(Args.Length > 2)
            {
                if (isRunningAsAdmin() && Args[2] == "begin")
                {
                    PathInp.Text = Args[1];
                    beginSetup();
                }
            }
        }

        private bool isRunningAsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void beginSetup()
        {
            if (!Directory.Exists(PathInp.Text))
            {
                Directory.CreateDirectory(PathInp.Text);
            }
            Registry.Uninstall.CreateUninstallerKeys(PathInp.Text);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = "updater.exe";
            startInfo.Arguments = "\"" + PathInp.Text + "\\\" stable";
            try
            {
                Process proc = Process.Start(startInfo);
                IntPtr hWnd = proc.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                {
                    SetForegroundWindow(hWnd);
                    ShowWindow(hWnd, int.Parse("9"));
                }
                proc.WaitForExit(); 
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return;
            }
            System.Windows.Application.Current.Shutdown(0);

        }

        private void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            InstallBtn.IsEnabled = false;
            if (isRunningAsAdmin())
            {
                beginSetup();
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                if (repair)
                {
                    startInfo.Arguments = "repair \"" + PathInp.Text + "\\\" begin";
                }
                else
                {
                    startInfo.Arguments = "install \"" + PathInp.Text + "\\\" begin";
                }
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathInp.Text = folderBrowserDialog1.SelectedPath + "\\7th Heaven\\";
                InstallBtn.IsEnabled = true;
            }
        }
    }
}
