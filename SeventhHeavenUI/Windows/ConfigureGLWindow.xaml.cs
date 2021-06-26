using _7thHeaven.Code;
using Iros._7th;
using Iros._7th.Workshop;
using SeventhHeaven.Classes;
using SeventhHeaven.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SeventhHeaven.Windows
{
    /// <summary>
    /// Interaction logic for ConfigureGLWindow.xaml
    /// </summary>
    public partial class ConfigureGLWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Iros._7th.Workshop.ConfigSettings.Settings _settings;
        private Iros._7th.Workshop.ConfigSettings.ConfigSpec _spec;
        private string _file;

        private List<GLSettingViewModel> ViewModels { get; set; }

        private Button btnClearTextureCache;

        public ConfigureGLWindow()
        {
            InitializeComponent();

            ViewModels = new List<GLSettingViewModel>();
        }

        public void SetStatusMessage(string message)
        {
            txtStatusMessage.Text = message;
        }


        public bool Init(string cfgSpec, string cfgFile)
        {
            _file = cfgFile;

            try
            {
                _spec = Util.Deserialize<Iros._7th.Workshop.ConfigSettings.ConfigSpec>(cfgSpec);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.FailedToReadRequiredSpecXmlFile), ResourceHelper.Get(StringKey.Error), MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // build new DDOption for resolutions
            List<Iros._7th.Workshop.ConfigSettings.DDOption> DigitalResolutions = new List<Iros._7th.Workshop.ConfigSettings.DDOption>();
            DigitalResolutions.Add(new Iros._7th.Workshop.ConfigSettings.DDOption() { Settings = "window_size_x = 0,window_size_y = 0", Text = "Auto" });

            List<SupportedResolution> supportedResolutions = PrimaryScreen.GetSupportedResolutions();
            foreach(SupportedResolution sr in supportedResolutions)
            {
                DigitalResolutions.Add(new Iros._7th.Workshop.ConfigSettings.DDOption() { Settings = $"window_size_x = {sr.H},window_size_y = {sr.V}", Text = $"{sr.H}x{sr.V}"});
            }

            // find the resolutions option
            Iros._7th.Workshop.ConfigSettings.DropDown ResolutionDD = (Iros._7th.Workshop.ConfigSettings.DropDown)_spec.Settings.Find(item => { return item.Name == "Resolution"; });
            // over ride it with OS reported suported resolutions
            ResolutionDD.Options = DigitalResolutions;

            _settings = new Iros._7th.Workshop.ConfigSettings.Settings(_file);
            _settings.SetMissingDefaults(_spec.Settings);

            Dictionary<string, int> tabOrders = new Dictionary<string, int>()
            {
                {ResourceHelper.Get(StringKey.Graphics), 0},
                {ResourceHelper.Get(StringKey.Cheats), 1},
                {ResourceHelper.Get(StringKey.Advanced), 2}
            };

            foreach (var items in _spec.Settings.GroupBy(s => s.Group)
                                                .Select(g => new { settingGroup = g, SortOrder = tabOrders[g.Key] })
                                                .OrderBy(g => g.SortOrder)
                                                .Select(g => g.settingGroup))
            {
                TabItem tab = new TabItem()
                {
                    Header = items.Key,
                };

                StackPanel stackPanel = new StackPanel()
                {
                    Margin = new Thickness(0, 5, 0, 0)
                };

                ScrollViewer scrollViewer = new ScrollViewer()
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                foreach (Iros._7th.Workshop.ConfigSettings.Setting setting in items)
                {
                    GLSettingViewModel settingViewModel = new GLSettingViewModel(setting, _settings);

                    ContentControl settingControl = new ContentControl();
                    settingControl.DataContext = settingViewModel;
                    settingControl.MouseEnter += SettingControl_MouseEnter;

                    ViewModels.Add(settingViewModel);
                    stackPanel.Children.Add(settingControl);
                }

                if (items.Key == ResourceHelper.Get(StringKey.Advanced))
                {
                    // add clear texture cache button
                    btnClearTextureCache = new Button()
                    {
                        Content = ResourceHelper.Get(StringKey.ClearTextureCache),
                        ToolTip = $"{ResourceHelper.Get(StringKey.WillDeleteEverythingUnder)} {Path.Combine(Sys.Settings.AaliFolder, "cache")}",
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    btnClearTextureCache.Click += BtnClearTextureCache_Click;

                    stackPanel.Children.Add(btnClearTextureCache);
                }


                scrollViewer.Content = stackPanel;
                tab.Content = scrollViewer;
                tabCtrlMain.Items.Add(tab);
            }

            return true;
        }

        private void BtnClearTextureCache_Click(object sender, RoutedEventArgs e)
        {
            ClearTextureCache();
        }

        private void ClearTextureCache()
        {
            string pathToCache = Path.Combine(Sys.Settings.AaliFolder, "cache");

            if (!Directory.Exists(pathToCache))
            {
                SetStatusMessage(ResourceHelper.Get(StringKey.PathToCacheFolderDoesNotExist));
                return;
            }

            try
            {
                foreach (string file in Directory.GetFiles(pathToCache, "*.ctx", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        Logger.Warn(ex); // add warning to log if fail to delete file but continue deleting other files
                    }
                }

                // delete empty folders recursively
                FileUtils.DeleteEmptyFolders(pathToCache);

                SetStatusMessage(ResourceHelper.Get(StringKey.TextureCacheCleared));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatusMessage($"{ResourceHelper.Get(StringKey.FailedToClearTextureCache)}: {ex.Message}");
            }
        }

        private void SettingControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            GLSettingViewModel viewModel = ((ContentControl)sender)?.DataContext as GLSettingViewModel;

            if (viewModel == null)
            {
                return;
            }

            SetStatusMessage(viewModel.Description);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (GLSettingViewModel item in ViewModels)
                {
                    item.Save(_settings);
                }

                _settings.Save(_file);

                Sys.Message(new WMessage(ResourceHelper.Get(StringKey.GameDriverSettingsSaved)));
                this.Close();
            }
            catch (UnauthorizedAccessException)
            {
                MessageDialogWindow.Show(ResourceHelper.Get(StringKey.CouldNotWriteTo7HGameDriverCfg), ResourceHelper.Get(StringKey.SaveError), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetStatusMessage(ResourceHelper.Get(StringKey.UnknownErrorWhileSaving));
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ViewModels)
            {
                item.ResetToDefault(_settings);
            }

            SetStatusMessage(ResourceHelper.Get(StringKey.GameDriverSettingsResetToDefaults));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (btnClearTextureCache != null)
            {
                btnClearTextureCache.Click -= BtnClearTextureCache_Click;
            }
        }
    }
}
