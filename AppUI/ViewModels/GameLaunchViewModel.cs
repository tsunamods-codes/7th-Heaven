using Iros.Workshop;
using AppUI.Classes;
using AppUI;
using AppUI.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AppUI.ViewModels
{
    public class GameLaunchViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _statusLog;
        private bool variableDump;
        private bool debugLogging;

        public GameLaunchViewModel(bool variableDump, bool debugLogging)
        {
            this.variableDump = variableDump;
            this.debugLogging = debugLogging;
            StatusLog = "";
        }

        public string StatusLog
        {
            get
            {
                return _statusLog;
            }
            set
            {
                _statusLog = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsLaunchingWithNoMods { get; internal set; }

        internal Task<bool> BeginLaunchProcessAsync()
        {
            Task<bool> launchTask = Task.Factory.StartNew(() =>
            {
                bool didLaunch = false;

                try
                {
                    GameLauncher.Instance.ProgressChanged += LaunchGame_ProgressChanged;

                    didLaunch = GameLauncher.LaunchGame(variableDump, debugLogging, IsLaunchingWithNoMods).Result;
                }
                finally
                {
                    GameLauncher.Instance.ProgressChanged -= LaunchGame_ProgressChanged;
                    GameLauncher.Instance.RaiseLaunchCompleted(didLaunch);
                }

                return didLaunch;
            });

            return launchTask;
        }

        internal void LogStatus(string message)
        {
            Logger.Info(message);
            AppendToStatusLog(message);
        }

        private void LaunchGame_ProgressChanged(string message)
        {
            AppendToStatusLog(message);
        }

        internal void AppendToStatusLog(string message)
        {
            // invoke on UI thread to show message asap to user
            if (Sys.Settings.GameLaunchSettings.ShowLauncherWindow)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    StatusLog += $"{message}\n";
                }, DispatcherPriority.Background);
            }

            App.ForceUpdateUI();
        }
    }
}
