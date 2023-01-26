/*
  This source is subject to the Microsoft Public License. See LICENSE.TXT for details.
  The original developer is Iros <irosff@outlook.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Iros._7th.Workshop {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
                bool result;
                var mutex = new System.Threading.Mutex(true, "UniqueAppId", out result);
                if (!result)
                {
                  MessageBox.Show("Sorry, only one instance of 7th Heaven can be run at a time!  Please first close all instances of 7th Heaven if you are trying to import an IRO.");
                    return;
                }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.Run(new fLibrary());
            GC.KeepAlive(mutex);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            fErr.Display((Exception)e.ExceptionObject);
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) {
            fErr.Display(e.Exception);
        }
    }
}
