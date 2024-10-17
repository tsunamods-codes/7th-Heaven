using AppCore;
using Iros._7th.Workshop;
using AppUI.Classes;
using AppUI.ViewModels;
using AppUI.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AppUI.ViewModels
{

    public class MovieImportViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private string _importStatusMessage;
        private bool _importButtonIsEnabled;
        private Visibility _importProgressVisibility;
        private double _importProgressValue;

        public string ImportStatusMessage
        {
            get
            {
                return _importStatusMessage;
            }
            set
            {
                _importStatusMessage = value;
                NotifyPropertyChanged();
            }
        }

        public bool ImportButtonIsEnabled
        {
            get
            {
                return _importButtonIsEnabled;
            }
            set
            {
                _importButtonIsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility ImportProgressVisibility
        {
            get
            {
                return _importProgressVisibility;
            }
            set
            {
                _importProgressVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public double ImportProgressValue
        {
            get
            {
                return _importProgressValue;
            }
            set
            {
                _importProgressValue = value;
                NotifyPropertyChanged();
            }
        }

        public MovieImportViewModel()
        {
            InitImportMovieOption();
        }

        private void InitImportMovieOption()
        {
            ImportProgressVisibility = Visibility.Hidden;
            ImportProgressValue = 0;
            ImportStatusMessage = "";
            ImportButtonIsEnabled = false;

            if (!string.IsNullOrEmpty(Sys.Settings.FF7Exe) & File.Exists(Sys.Settings.FF7Exe))
            {
                ImportButtonIsEnabled = !GameConverter.AllMovieFilesExist(Sys.PathToFF7Movies);

                Dictionary<string, string[]> missingMovies = GameConverter.GetMissingMovieFiles(Sys.PathToFF7Movies);

                if (missingMovies.Count > 0)
                {
                    List<string> discsToInsert = GetDiscsToInsertForMissingMovies(missingMovies);

                    ImportStatusMessage = string.Format(ResourceHelper.Get(StringKey.InsertAndClickImport), discsToInsert[0]);
                }
                else
                {
                    ImportStatusMessage = ResourceHelper.Get(StringKey.AllMovieFilesAlreadyImported);
                }
            }
            else
            {
                ImportStatusMessage = ResourceHelper.Get(StringKey.Ff7ExeNotFoundYouMayNeedToConfigure);
            }

            
        }

        private List<string> GetDiscsToInsertForMissingMovies(Dictionary<string, string[]> missingMovies)
        {
            List<string> discsToInsert = new List<string>();
            missingMovies.Values.GroupBy(s => s)
                                .Select(g => g.Key)
                                .ToList()
                                .ForEach(s => discsToInsert.AddRange(s)); // add all string[] to one List<string> 

            discsToInsert = discsToInsert.Distinct().OrderBy(s => s).ToList();

            return discsToInsert;
        }

        internal void ImportMissingMovies()
        {
            string warningMessage = string.Format(ResourceHelper.Get(StringKey.ImportMissingMoviesWarningMessage), Sys.PathToFF7Movies);
            MessageDialogViewModel result = MessageDialogWindow.Show(warningMessage, ResourceHelper.Get(StringKey.Warning), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result.Result == MessageBoxResult.No)
            {
                return;
            }

            ImportButtonIsEnabled = false;
            ImportProgressVisibility = Visibility.Visible;

            bool cancelProcess = false;
            int totalFiles = 0;
            int filesCopied = 0;

            Task importTask = Task.Factory.StartNew(() =>
            {
                Dictionary<string, string[]> missingMovies = GameConverter.GetMissingMovieFiles(Sys.PathToFF7Movies);
                List<string> discsToInsert = GetDiscsToInsertForMissingMovies(missingMovies);

                totalFiles = missingMovies.Count;

                foreach (string disc in discsToInsert)
                {
                    List<string> driveLetters;

                    do
                    {
                        SetImportStatus($"{ResourceHelper.Get(StringKey.LookingFor)} {disc} ...");
                        driveLetters = GameLauncher.GetDriveLetters(disc);

                        if (driveLetters.Count == 0)
                        {
                            SetImportStatus(string.Format(ResourceHelper.Get(StringKey.InsertToContinue), disc));

                            App.Current.Dispatcher.Invoke(() =>
                            {
                                string discNotFoundMessage = string.Format(ResourceHelper.Get(StringKey.PleaseInsertToContinueCopying), disc);
                                MessageDialogViewModel insertDiscResult = MessageDialogWindow.Show(discNotFoundMessage, ResourceHelper.Get(StringKey.InsertDisc), MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                                cancelProcess = (insertDiscResult.Result == MessageBoxResult.Cancel);
                            });
                        }

                        if (cancelProcess)
                        {
                            return;
                        }

                    } while (driveLetters.Count == 0);

                    SetImportStatus($"{string.Format(ResourceHelper.Get(StringKey.FoundDiscAt), disc)} {string.Join("  ", driveLetters)} ...");

                    // loop over missing files on the found disc and copy to data/movies destination
                    foreach (string movieFile in missingMovies.Where(kv => kv.Value.Any(s => s.Equals(disc, StringComparison.InvariantCultureIgnoreCase)))
                                                              .Select(kv => kv.Key))
                    {

                        foreach (string drive in driveLetters)
                        {
                            string fullTargetPath = Path.Combine(Sys.PathToFF7Movies, movieFile);
                            string sourceFilePath = Path.Combine(drive, "ff7", "movies", movieFile);

                            if (File.Exists(sourceFilePath))
                            {
                                if (File.Exists(fullTargetPath))
                                {
                                    SetImportStatus($"{ResourceHelper.Get(StringKey.Overwriting)} {movieFile} ...");
                                }
                                else
                                {
                                    SetImportStatus($"{ResourceHelper.Get(StringKey.Copying)} {movieFile} ...");
                                }

                                File.Copy(sourceFilePath, fullTargetPath, true);
                                filesCopied++;
                                UpdateImportProgress(filesCopied, totalFiles);
                                break;
                            }
                            else
                            {
                                SetImportStatus(string.Format(ResourceHelper.Get(StringKey.FailedToFindAt), movieFile, sourceFilePath));
                            }
                        }
                    }
                }
            });

            importTask.ContinueWith((taskResult) =>
            {
                if (taskResult.IsFaulted)
                {
                    Logger.Error(taskResult.Exception);
                    SetImportStatus($"{ResourceHelper.Get(StringKey.AnErrorOccurredCopyingMovies)}: {taskResult.Exception.GetBaseException().Message}");
                }
                else if (cancelProcess)
                {
                    InitImportMovieOption();
                }
                else
                {
                    if (filesCopied == totalFiles)
                    {
                        SetImportStatus(ResourceHelper.Get(StringKey.SuccessfullyCopiedMovies));
                    }
                    else
                    {
                        SetImportStatus(ResourceHelper.Get(StringKey.FinishedCopyingMoviesSomeFailed));
                    }

                    ImportButtonIsEnabled = !GameConverter.AllMovieFilesExist(Sys.PathToFF7Movies);
                    ImportProgressValue = 0;
                    ImportProgressVisibility = Visibility.Hidden;
                }
            });

        }

        private void SetImportStatus(string message)
        {
            ImportStatusMessage = message;
            Logger.Info(ImportStatusMessage);
            App.ForceUpdateUI();
        }

        private void UpdateImportProgress(int copiedCount, int totalCount)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (totalCount == 0)
                {
                    ImportProgressValue = 0;
                }
                else
                {
                    ImportProgressValue = ((double)copiedCount / (double)totalCount) * 100;
                }
                App.ForceUpdateUI();
            });
        }

    }
}
