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
    public class PatchIroViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _pathToSourceFolder;
        private string _pathToOriginalIroFile;
        private string _pathToNewIroFile;
        private string _pathToIropFile;
        private string _statusText;
        private List<string> _compressionOptions;
        private int _compressionSelectedIndex;
        private int _progressValue;
        private bool _isPatching;
        private string _filesToDeleteText;

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

        public string PathToOriginalIroFile
        {
            get
            {
                return _pathToOriginalIroFile;
            }
            set
            {
                _pathToOriginalIroFile = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToNewIroFile
        {
            get
            {
                return _pathToNewIroFile;
            }
            set
            {
                _pathToNewIroFile = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToIropFile
        {
            get
            {
                return _pathToIropFile;
            }
            set
            {
                _pathToIropFile = value;
                NotifyPropertyChanged();
            }
        }

        public string FilesToDeleteText
        {
            get
            {
                return _filesToDeleteText;
            }
            set
            {
                _filesToDeleteText = value;
                NotifyPropertyChanged();
            }
        }

        public List<string> FilesToDelete
        {
            get
            {
                return FilesToDeleteText.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
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

        public bool IsPatching
        {
            get
            {
                return _isPatching;
            }
            set
            {
                _isPatching = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotPatching));
            }
        }


        public bool IsNotPatching
        {
            get
            {
                return !_isPatching;
            }
        }

        public bool IsAdvancedMode { get; private set; }

        public PatchIroViewModel(bool isAdvancedPatching)
        {
            ProgressValue = 0;
            IsPatching = false;
            IsAdvancedMode = isAdvancedPatching;
            PathToSourceFolder = "";
            PathToOriginalIroFile = "";
            PathToNewIroFile = "";
            StatusText = "";
            CompressionOptions = Enum.GetNames(typeof(AppWrapper.CompressType)).ToList();
            CompressionSelectedIndex = 0;
        }

        internal bool Validate(bool showErrorMsg = true)
        {
            string errorMsg = "";
            bool isValid = true;

            if (string.IsNullOrEmpty(PathToOriginalIroFile) && !IsAdvancedMode)
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToOriginalIroIsRequired);
                isValid = false;
            }
            else if (string.IsNullOrEmpty(PathToNewIroFile) && !IsAdvancedMode)
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToNewIroFileIsRequired);
                isValid = false;
            }
            else if (string.IsNullOrEmpty(PathToSourceFolder) && IsAdvancedMode)
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToSourceFolderIsRequired);
                isValid = false;
            }
            else if (string.IsNullOrEmpty(PathToIropFile))
            {
                errorMsg = ResourceHelper.Get(StringKey.PathToIropFileIsRequired);
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

        internal void PatchIro()
        {
            string pathToOriginalIro = PathToOriginalIroFile;
            string pathToNewIro = PathToNewIroFile;
            string iropFile = PathToIropFile;
            AppWrapper.CompressType compressType = (AppWrapper.CompressType)CompressionSelectedIndex;

            IsPatching = true;

            Task patchTask = Task.Factory.StartNew(() =>
            {
                using (AppWrapper.IrosArc orig = new AppWrapper.IrosArc(pathToOriginalIro, true))
                {
                    using (AppWrapper.IrosArc newiro = new AppWrapper.IrosArc(pathToNewIro))
                    {
                        using (var fs = new FileStream(iropFile, FileMode.Create))
                            AppWrapper.IrosPatcher.Create(orig, newiro, fs, compressType, IroProgress);
                    }
                }
            });

            patchTask.ContinueWith((result) =>
            {
                IsPatching = false;
                ProgressValue = 0;

                if (result.IsFaulted)
                {
                    Logger.Warn(result.Exception.GetBaseException());
                    StatusText = $"{ResourceHelper.Get(StringKey.AnErrorOccuredWhilePatching)}: {result.Exception.GetBaseException().Message}";
                    return;
                }

                StatusText = ResourceHelper.Get(StringKey.PatchingComplete);
            });
        }

        internal void PatchIroAdvanced()
        {
            string pathToSource = PathToSourceFolder;
            string iropFile = PathToIropFile;
            List<string> filesToDelete = FilesToDelete;
            AppWrapper.CompressType compressType = (AppWrapper.CompressType)CompressionSelectedIndex;

            IsPatching = true;

            Task patchTask = Task.Factory.StartNew(() =>
            {
                var files = Directory.GetFiles(pathToSource, "*", SearchOption.AllDirectories)
                                     .Select(s => s.Substring(pathToSource.Length).Trim('\\', '/'))
                                     .Select(s => AppWrapper.IrosArc.ArchiveCreateEntry.FromDisk(pathToSource, s))
                                     .ToList();

                if (filesToDelete.Any())
                {
                    byte[] deldata = Encoding.Unicode.GetBytes(String.Join("\n", filesToDelete));
                    files.Add(new AppWrapper.IrosArc.ArchiveCreateEntry()
                    {
                        Filename = "%IrosPatch:Deleted",
                        GetData = () => deldata
                    });
                }

                using (var fs = new FileStream(iropFile, FileMode.Create))
                    AppWrapper.IrosArc.Create(fs, files, AppWrapper.ArchiveFlags.Patch, compressType, IroProgress);
            });

            patchTask.ContinueWith((result) =>
            {
                IsPatching = false;
                ProgressValue = 0;

                if (result.IsFaulted)
                {
                    Logger.Warn(result.Exception.GetBaseException());
                    StatusText = $"{ResourceHelper.Get(StringKey.AnErrorOccuredWhilePatching)}: {result.Exception.GetBaseException().Message}";
                    return;
                }

                StatusText = ResourceHelper.Get(StringKey.PatchingComplete);
            });
        }


        private void IroProgress(double d, string s)
        {
            StatusText = s;
            ProgressValue = (int)(100 * d);
        }
    }
}
