using AppCore;
using Iros._7th;
using Iros._7th.Workshop;
using AppUI.Classes;
using AppUI.Windows;
using AppUI;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace AppUI.ViewModels
{


    public class ImportModViewModel : ViewModelBase
    {
        enum ImportTabIndex
        {
            FromIro,
            FromFolder,
            BatchImport
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _pathToIroArchiveInput;
        private string _pathToModFolderInput;
        private string _pathToBatchFolderInput;
        private string _helpText;
        private int _selectedTabIndex;
        private bool _isImporting;
        private int _progressValue;

        public string PathToIroArchiveInput
        {
            get
            {
                return _pathToIroArchiveInput;
            }
            set
            {
                _pathToIroArchiveInput = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToModFolderInput
        {
            get
            {
                return _pathToModFolderInput;
            }
            set
            {
                _pathToModFolderInput = value;
                NotifyPropertyChanged();
            }
        }

        public string PathToBatchFolderInput
        {
            get
            {
                return _pathToBatchFolderInput;
            }
            set
            {
                _pathToBatchFolderInput = value;
                NotifyPropertyChanged();
            }
        }

        public string HelpText
        {
            get
            {
                return _helpText;
            }
            set
            {
                _helpText = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsImporting
        {
            get
            {
                return _isImporting;
            }
            set
            {
                _isImporting = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsNotImporting));
            }
        }

        public bool IsNotImporting
        {
            get
            {
                return !_isImporting;
            }
        }

        public int SelectedTabIndex
        {
            get
            {
                return _selectedTabIndex;
            }
            set
            {
                _selectedTabIndex = value;
                NotifyPropertyChanged();
                UpdateHelpText();
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
                NotifyPropertyChanged(nameof(ProgressBarVisibility));
            }
        }

        public Visibility ProgressBarVisibility
        {
            get
            {
                if (ProgressValue == 0)
                    return Visibility.Hidden;

                return Visibility.Visible;
            }
        }

        public ImportModViewModel()
        {
            SelectedTabIndex = 0;
            ProgressValue = 0;
            UpdateHelpText();
        }

        private void UpdateHelpText()
        {
            if ((ImportTabIndex)SelectedTabIndex == ImportTabIndex.FromIro)
            {
                HelpText = ResourceHelper.Get(StringKey.SelectAnIroFileToImport);
            }
            else if ((ImportTabIndex)SelectedTabIndex == ImportTabIndex.FromFolder)
            {
                HelpText = ResourceHelper.Get(StringKey.SelectAFolderThatContainsModFiles);
            }
            else
            {
                HelpText = ResourceHelper.Get(StringKey.SelectAFolderThatContainsIroModFilesAndFolders);
            }
        }


        public Task<bool> ImportModFromWindowAsync()
        {
            IsImporting = true;
            ProgressValue = 10;
            Sys.Message(new WMessage(ResourceHelper.Get(StringKey.ImportingModsPleaseWait)));

            Task<bool> t = Task.Factory.StartNew(() =>
            {
                bool didImport = false;

                switch ((ImportTabIndex)SelectedTabIndex)
                {
                    case ImportTabIndex.FromIro:
                        didImport = TryImportFromIroArchive();
                        break;
                    case ImportTabIndex.FromFolder:
                        didImport = TryImportFromFolder();
                        break;
                    case ImportTabIndex.BatchImport:
                        didImport = TryBatchImport();
                        break;
                }

                if (!didImport)
                {
                    UpdateHelpText(); // reset the help text since the window will stay open on failure
                    Sys.Message(new WMessage(ResourceHelper.Get(StringKey.FailedToImportMod)));
                }

                IsImporting = false;
                return didImport;
            });

            return t;
        }

        private bool TryImportFromIroArchive()
        {
            if (string.IsNullOrWhiteSpace(PathToIroArchiveInput))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.EnterPathToAnIroFile), ResourceHelper.Get(StringKey.ValidationError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!File.Exists(PathToIroArchiveInput))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.IroFileDoesNotExist), ResourceHelper.Get(StringKey.ValidationError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            bool isPatchFile = Path.GetExtension(PathToIroArchiveInput) == ".irop";

            ModImporter importer = null;
            try
            {
                importer = new ModImporter();
                importer.ImportProgressChanged += Importer_ImportProgressChanged;

                string fileName = Path.GetFileNameWithoutExtension(PathToIroArchiveInput);

                if (isPatchFile)
                {
                    bool didPatch = importer.ImportModPatch(PathToIroArchiveInput);

                    if (!didPatch)
                    {
                        MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FailedToImportModTheErrorHasBeenLogged), ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    Sys.Message(new WMessage($"Successfully applied patch {fileName}!"));
                }
                else
                {
                    importer.Import(PathToIroArchiveInput, ModImporter.ParseNameFromFileOrFolder(fileName), true, false);
                    Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.SuccessfullyImported)} {fileName}!"));
                }

                return true;
            }
            catch (DuplicateModException de)
            {
                Logger.Error(de);
                MessageDialogWindow.Show($"{ResourceHelper.Get(StringKey.CanNotImportMod)} {de.Message}", ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FailedToImportModTheErrorHasBeenLogged), ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                importer.ImportProgressChanged -= Importer_ImportProgressChanged;
            }

        }

        private void Importer_ImportProgressChanged(string message, double percentComplete)
        {
            HelpText = message;
            ProgressValue = (int)percentComplete;
            Logger.Info($"Mod import progress: {message} | {percentComplete:0.00}%");
            App.ForceUpdateUI();
        }

        private bool TryImportFromFolder()
        {
            if (string.IsNullOrWhiteSpace(PathToModFolderInput))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.EnterPathToFolderContainingModFiles), ResourceHelper.Get(StringKey.ValidationError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Directory.Exists(PathToModFolderInput))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.DirectoryDoesNotExist), ResourceHelper.Get(StringKey.ValidationError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            ModImporter importer = null;
            try
            {
                string folderName = new DirectoryInfo(PathToModFolderInput).Name;

                importer = new ModImporter();
                importer.ImportProgressChanged += Importer_ImportProgressChanged;
                importer.Import(PathToModFolderInput, ModImporter.ParseNameFromFileOrFolder(folderName), false, false);

                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.SuccessfullyImported)} {folderName}!", true));
                return true;
            }
            catch (DuplicateModException de)
            {
                Logger.Error(de);
                MessageDialogWindow.Show($"{ResourceHelper.Get(StringKey.CanNotImportMod)} {de.Message}", ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FailedToImportModTheErrorHasBeenLogged), ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                importer.ImportProgressChanged -= Importer_ImportProgressChanged;
            }
        }

        private bool TryBatchImport()
        {
            if (string.IsNullOrWhiteSpace(PathToBatchFolderInput))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.EnterPathToFolderContainingIroFilesOrModFolders), ResourceHelper.Get(StringKey.ValidationError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Directory.Exists(PathToBatchFolderInput))
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.DirectoryDoesNotExist), ResourceHelper.Get(StringKey.ValidationError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            ModImporter importer = null;

            try
            {
                importer = new ModImporter();
                importer.ImportProgressChanged += Importer_ImportProgressChanged;

                int modImportCount = 0;

                foreach (string iro in Directory.GetFiles(PathToBatchFolderInput, "*.iro"))
                {
                    string modName = ModImporter.ParseNameFromFileOrFolder(Path.GetFileNameWithoutExtension(iro));
                    importer.Import(iro, modName, true, false);
                    modImportCount++;
                }

                foreach (string dir in Directory.GetDirectories(PathToBatchFolderInput))
                {
                    string modName = ModImporter.ParseNameFromFileOrFolder(Path.GetFileNameWithoutExtension(dir));
                    importer.Import(dir, modName, false, false);
                    modImportCount++;
                }

                Sys.Message(new WMessage($"{ResourceHelper.Get(StringKey.SuccessfullyImported)} {modImportCount} mod(s)!", true));
                return true;
            }
            catch (DuplicateModException de)
            {
                Logger.Error(de);
                MessageDialogWindow.Show($"{ResourceHelper.Get(StringKey.CanNotImportMod)}(s). {de.Message}", ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FailedToImportModTheErrorHasBeenLogged), ResourceHelper.Get(StringKey.ImportError), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                importer.ImportProgressChanged -= Importer_ImportProgressChanged;
            }
        }
    }
}
