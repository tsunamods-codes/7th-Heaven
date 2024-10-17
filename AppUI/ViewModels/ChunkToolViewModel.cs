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
    public class ChunkToolViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _pathToFlevelFile;
        private string _pathToOutputFolder;
        private bool _isExtracting;
        private int _progressValue;
        private bool _sectionOneIsChecked;
        private bool _sectionTwoIsChecked;
        private bool _sectionThreeIsChecked;
        private bool _sectionFourIsChecked;
        private bool _sectionFiveIsChecked;
        private bool _sectionSixIsChecked;
        private bool _sectionSevenIsChecked;
        private bool _sectionEightIsChecked;
        private bool _sectionNineIsChecked;

        public string PathToFlevelFile
        {
            get
            {
                return _pathToFlevelFile;
            }
            set
            {
                _pathToFlevelFile = value;
                NotifyPropertyChanged();
            }
        }

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

        public bool IsExtracting
        {
            get
            {
                return _isExtracting;
            }
            set
            {
                _isExtracting = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotExtracting));
            }
        }

        public bool IsNotExtracting
        {
            get
            {
                return !_isExtracting;
            }
        }

        public bool SectionOneIsChecked
        {
            get
            {
                return _sectionOneIsChecked;
            }
            set
            {
                _sectionOneIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionTwoIsChecked
        {
            get
            {
                return _sectionTwoIsChecked;
            }
            set
            {
                _sectionTwoIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionThreeIsChecked
        {
            get
            {
                return _sectionThreeIsChecked;
            }
            set
            {
                _sectionThreeIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionFourIsChecked
        {
            get
            {
                return _sectionFourIsChecked;
            }
            set
            {
                _sectionFourIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionFiveIsChecked
        {
            get
            {
                return _sectionFiveIsChecked;
            }
            set
            {
                _sectionFiveIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionSixIsChecked
        {
            get
            {
                return _sectionSixIsChecked;
            }
            set
            {
                _sectionSixIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionSevenIsChecked
        {
            get
            {
                return _sectionSevenIsChecked;
            }
            set
            {
                _sectionSevenIsChecked = value;
                NotifyPropertyChanged();
            }
        }


        public bool SectionEightIsChecked
        {
            get
            {
                return _sectionEightIsChecked;
            }
            set
            {
                _sectionEightIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool SectionNineIsChecked
        {
            get
            {
                return _sectionNineIsChecked;
            }
            set
            {
                _sectionNineIsChecked = value;
                NotifyPropertyChanged();
            }
        }

        public ChunkToolViewModel()
        {
            SectionOneIsChecked = false;
            SectionTwoIsChecked = false;
            SectionThreeIsChecked = false;
            SectionFourIsChecked = false;
            SectionFiveIsChecked = false;
            SectionSixIsChecked = false;
            SectionSevenIsChecked = false;
            SectionEightIsChecked = false;
            SectionNineIsChecked = false;
            ProgressValue = 0;
            IsExtracting = false;
            PathToFlevelFile = "";
            PathToOutputFolder = "";
        }

        internal void BeginExtract()
        {
            if (string.IsNullOrWhiteSpace(PathToFlevelFile))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.PathToFlevelRequired), ResourceHelper.Get(StringKey.MissingPath), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(PathToOutputFolder))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.PathToOutputFolderRequired), ResourceHelper.Get(StringKey.MissingPath), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (GetSelectedChunks()?.Count == 0)
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.SelectTheSectionsToExtract), ResourceHelper.Get(StringKey.MissingPath), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(PathToFlevelFile))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FlevelFileDoesNotExistAtGivenPath), ResourceHelper.Get(StringKey.MissingPath), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(PathToOutputFolder))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.OutputFolderDoesNotExist), ResourceHelper.Get(StringKey.MissingPath), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            Task t = ExtractFlevelAsync();

            t.ContinueWith((taskResult) =>
            {
                string message = ResourceHelper.Get(StringKey.Complete);

                if (taskResult.IsFaulted)
                {
                    Logger.Error(taskResult.Exception?.GetBaseException());
                    message = ResourceHelper.Get(StringKey.FailedToExtractTheErrorHasBeenLogged);
                }

                MessageDialogWindow.Show(message, ResourceHelper.Get(StringKey.ExtractComplete));
                ProgressValue = 0;
            });
        }

        private Task ExtractFlevelAsync()
        {
            IsExtracting = true;
            List<int> selectedChunks = GetSelectedChunks();

            Task t = Task.Factory.StartNew(() =>
            {

                using (FileStream fLevelStream = new FileStream(PathToFlevelFile, FileMode.Open))
                {
                    ProcMonParser.DataFile df = ProcMonParser.FF7Files.LoadLGP(fLevelStream, PathToFlevelFile);
                    int file = 0;

                    foreach (ProcMonParser.DataItem item in df.Items)
                    {
                        file++;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            ProgressValue = (100 * file) / df.Items.Count;
                        });

                        if (Path.GetExtension(item.Name).Length == 0)
                        {
                            byte[] fLevelChunkedData = new byte[item.Length - 24];

                            fLevelStream.Position = item.Start + 24;

                            fLevelStream.Read(fLevelChunkedData, 0, fLevelChunkedData.Length);

                            List<byte[]> chunks = AppWrapper.FieldFile.Unchunk(fLevelChunkedData);
                            if (chunks.Count > 0)
                            {
                                foreach (int i in selectedChunks)
                                {
                                    string outputFile = Path.Combine(PathToOutputFolder, Path.GetFileNameWithoutExtension(item.Name) + ".chunk." + i);
                                    File.WriteAllBytes(outputFile, chunks[i - 1]);
                                }
                            }
                        }
                    }
                }

                IsExtracting = false;
            });

            return t;
        }

        private List<int> GetSelectedChunks()
        {
            List<int> selected = new List<int>();

            if (SectionOneIsChecked)
            {
                selected.Add(1);
            }

            if (SectionTwoIsChecked)
            {
                selected.Add(2);
            }

            if (SectionThreeIsChecked)
            {
                selected.Add(3);
            }

            if (SectionFourIsChecked)
            {
                selected.Add(4);
            }

            if (SectionFiveIsChecked)
            {
                selected.Add(5);
            }

            if (SectionSixIsChecked)
            {
                selected.Add(6);
            }

            if (SectionSevenIsChecked)
            {
                selected.Add(7);
            }

            if (SectionEightIsChecked)
            {
                selected.Add(8);
            }

            if (SectionNineIsChecked)
            {
                selected.Add(9);
            }

            return selected;
        }

    }
}
