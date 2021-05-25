using Setup.registry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(IntPtr hinst,string lpszName,uint uType,int cxDesired,int cyDesired,uint fuLoad);

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
            sp.Children.Add(new TextBlock { Text = "INSTALL", Margin = new Thickness(5, 0, 0, 0) });
            InstallBtn.Content = sp;

            if(Args.Length > 0)
            {
                PathInp.Text = Args[0];
                InstallBtn.IsEnabled = true;
            }

            if(PathInp.Text == "")
            {
                if (Steam.getFF7SteamInstalled())
                {
                    PathInp.Text = Steam.getFF7SteamPath();
                }
            }

            if(Args.Length > 1)
            {
                if (isRunningAsAdmin() && Args[1] == "begin")
                {
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
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = "updater.exe";
            startInfo.Arguments = "\"" + PathInp.Text + "\" stable";
            try
            {
                Process proc = Process.Start(startInfo);
                proc.WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception ex)
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
                startInfo.Arguments = "\""+ PathInp.Text + "\" begin";
                startInfo.Verb = "runas";
                try
                {
                    Process proc = Process.Start(startInfo);
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    return;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "FF7 Game (ff7.exe)|ff7.exe";
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathInp.Text = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
                InstallBtn.IsEnabled = true;
            }
        }
    }
}
