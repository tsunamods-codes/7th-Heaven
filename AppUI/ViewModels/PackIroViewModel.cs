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
    public class PackIroViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _pathToSourceFolder;
        private string _pathToOutputFile;
        private List<string> _compressionOptions;
        private bool _isPacking;
        private int _progressValue;
        private int _compressionSelectedIndex;
        private string _statusText;

        public string PathToSourceFolder
        {
            get
            {
                return _pathToSourceFolder;
            }
            set
            {
                _pathToSourceFolder = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToOutputFile
        {
            get
            {
                return _pathToOutputFile;
            }
            set
            {
                _pathToOutputFile = value;
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

        public string SelectedCompressionOptionText
        {
            get
            {
                return ((AppWrapper.CompressType)CompressionSelectedIndex).ToString();
            }
        }
        public List<string> CompressionOptions
        {
            get
            {
                if (_compressionOptions == null)
                    _compressionOptions = new List<string>();

                return _compressionOptions;
            }
            set
            {
                _compressionOptions = value;
                NotifyPropertyChanged();
            }
        }

        public int CompressionSelectedIndex
        {
            get
            {
                return _compressionSelectedIndex;
            }
            set
            {
                _compressionSelectedIndex = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(SelectedCompressionOptionText));
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

        public bool IsPacking
        {
            get
            {
                return _isPacking;
            }
            set
            {
                _isPacking = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotPacking));
            }
        }

        public bool IsNotPacking
        {
            get
            {
                return !_isPacking;
            }
        }


        public PackIroViewModel()
        {
            ProgressValue = 0;
            IsPacking = false;
            PathToSourceFolder = "";
            PathToOutputFile = "";
            StatusText = "";
            CompressionOptions = Enum.GetNames(typeof(AppWrapper.CompressType)).ToList();
            CompressionSelectedIndex = 0;
        }

        internal bool Validate(bool showErrorMsg = true)
        {
            string errorMsg = "";
            bool isValid = true;

            if (string.IsNullOrEmpty(PathToSourceFolder))
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToSourceFolderIsRequired);
                isValid = false;
            }
            else if (string.IsNullOrEmpty(PathToOutputFile))
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToOutputIroFileIsRequired);
                isValid = false;
            }
            else if (CompressionSelectedIndex < 0)
            {
                errorMsg = ResourceHelper.Get(StringKey.SelectCompressionOption);
                isValid = false;
            }

            if (!isValid && showErrorMsg)
            {
                Logger.Warn($"{ResourceHelper.Get(StringKey.InvalidPackIroOptions)}: {errorMsg}");
                MessageDialogWindow.Show(errorMsg, ResourceHelper.Get(StringKey.MissingRequiredInput), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return isValid;
        }

        internal Task PackIro()
        {
            string pathToSource = PathToSourceFolder;
            string outputFile = PathToOutputFile;
            AppWrapper.CompressType compressType = (AppWrapper.CompressType)CompressionSelectedIndex;

            IsPacking = true;

            Task packTask = Task.Factory.StartNew(() =>
            {

                var files = Directory.GetFiles(pathToSource, "*", SearchOption.AllDirectories)
                                     .Select(s => s.Substring(pathToSource.Length).Trim('\\', '/'))
                                     .ToList();

                using (var fs = new FileStream(outputFile, FileMode.Create))
                    AppWrapper.IrosArc.Create(fs, files.Select(s => AppWrapper.IrosArc.ArchiveCreateEntry.FromDisk(pathToSource, s)), AppWrapper.ArchiveFlags.None, compressType, IroProgress);
            });

            packTask.ContinueWith((result) =>
            {
                IsPacking = false;
                ProgressValue = 0;

                if (result.IsFaulted)
                {
                    Logger.Warn(result.Exception.GetBaseException());
                    StatusText = $"{ResourceHelper.Get(StringKey.AnErrorOccuredWhilePacking)}: {result.Exception.GetBaseException().Message}";
                    return;
                }

                StatusText = ResourceHelper.Get(StringKey.PackingComplete);
            });

            return packTask;
        }

        private void IroProgress(double d, string s)
        {
            StatusText = s;
            ProgressValue = (int)(100 * d);
        }
    }
}
