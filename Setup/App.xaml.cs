using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Setup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0)
            {
                switch (e.Args[0])
                {
                    case "uninstall":
                        Setup.UninstallWindow.Args = e.Args;
                        this.StartupUri = new Uri("UninstallWindow.xaml", UriKind.Relative);
                        break;
                    case "install":
                        Setup.MainWindow.Args = e.Args;
                        this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
                        break;
                }
            }
            else
            {
                Setup.MainWindow.Args =  (new string[] { "install" }).Union(e.Args).ToArray();
                Setup.MainWindow.repair = true;
                this.StartupUri = new Uri("MainWindow.xaml", UriKind.Relative);
            }
        }
    }
}
