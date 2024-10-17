using AppCore;
using AppUI.Classes;
using AppUI.Windows;
using AppUI;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AppUI.ViewModels
{
    public class UnpackIroViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _pathToOutputFolder;
        private string _pathToIroFile;
        private bool _isUnpacking;
        private int _progressValue;
        private string _statusText;

        public string PathToOutputFolder
        {
            get
            {
                return _pathToOutputFolder;
            }
            set
            {
                _pathToOutputFolder = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToIroFile
        {
            get
            {
                return _pathToIroFile;
            }
            set
            {
                _pathToIroFile = value;
                NotifyPropertyChanged();
            }
        }

        public string StatusText
        {
            get
            {
                return _statusText;
            }
            set
            {
                _statusText = value;
                NotifyPropertyChanged();
            }
        }

        public int ProgressValue
        {
            get
            {
                return _progressValue;
            }
            set
            {
                _progressValue = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsUnpacking
        {
            get
            {
                return _isUnpacking;
            }
            set
            {
                _isUnpacking = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotUnpacking));
            }
        }

        public bool IsNotUnpacking
        {
            get
            {
                return !_isUnpacking;
            }
        }


        public UnpackIroViewModel()
        {
            ProgressValue = 0;
            IsUnpacking = false;
            PathToOutputFolder = "";
            PathToIroFile = "";
            StatusText = "";
        }

        internal bool Validate(bool showErrorMsg = true)
        {
            string errorMsg = "";
            bool isValid = true;

            if (string.IsNullOrEmpty(PathToOutputFolder))
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToOutputFolderRequired);
                isValid = false;
            }
            else if (string.IsNullOrEmpty(PathToIroFile))
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToSourceIroFileIsRequired);
                isValid = false;
            }

            if (!isValid && showErrorMsg)
            {
                Logger.Warn($"{ResourceHelper.Get(StringKey.InvalidUnpackIroOptions)}: {errorMsg}");
                MessageDialogWindow.Show(errorMsg, ResourceHelper.Get(StringKey.MissingRequiredInput), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return isValid;
        }

        internal Task UnpackIro()
        {
            string pathToOutput = PathToOutputFolder;
            string pathToIro = PathToIroFile;

            IsUnpacking = true;

            Task unpackTask = Task.Factory.StartNew(() =>
            {
                using (AppWrapper.IrosArc arc = new AppWrapper.IrosArc(pathToIro))
                {
                    List<string> files = arc.AllFileNames().ToList();
                    int count = 0;
                    foreach (string file in files)
                    {
                        string path = Path.Combine(pathToOutput, file);

                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.WriteAllBytes(path, arc.GetBytes(file));
                        
                        count++;
                        IroProgress(1.0 * count / files.Count, file);
                    }
                }
            });

            unpackTask.ContinueWith((result) =>
            {
                IsUnpacking = false;
                ProgressValue = 0;

                if (result.IsFaulted)
                {
                    Logger.Warn(result.Exception.GetBaseException());
                    StatusText = $"{ResourceHelper.Get(StringKey.AnErrorOccuredWhileUnpacking)}: {result.Exception.GetBaseException().Message}";
                    return;
                }

                StatusText = ResourceHelper.Get(StringKey.UnpackingComplete);
            });

            return unpackTask;
        }

        private void IroProgress(double d, string s)
        {
            StatusText = s;
            ProgressValue = (int)(100 * d);
        }
    }
}
