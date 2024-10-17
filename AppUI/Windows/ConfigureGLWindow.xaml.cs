using AppCore;
using Iros._7th;
using Iros._7th.Workshop;
using AppUI.Classes;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppUI.Windows
{
    /// <summary>
    /// Interaction logic for ConfigureGLWindow.xaml
    /// </summary>
    public partial class ConfigureGLWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Iros._7th.Workshop.ConfigSettings.Settings _settings;
        private Iros._7th.Workshop.ConfigSettings.ConfigSpec _spec;

        private List<GLSettingViewModel> ViewModels { get; set; }

        public ConfigureGLWindow()
        {
            InitializeComponent();

            ViewModels = new List<GLSettingViewModel>();
        }

        public void SetStatusMessage(string message)
        {
            txtStatusMessage.Text = message;
        }


        public bool Init(string cfgSpec)
        {
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
            Iros._7th.Workshop.ConfigSettings.DropDown ResolutionDD = (Iros._7th.Workshop.ConfigSettings.DropDown)_spec.Settings.Find(item => {
                return item.DefaultValue == "window_size_x = 1280,window_size_y = 720";
            });
            // override it with OS reported suported resolutions
            ResolutionDD.Options = DigitalResolutions;

            _settings = new Iros._7th.Workshop.ConfigSettings.Settings();
            _settings.SetMissingDefaults(_spec.Settings);

            Dictionary<string, int> tabOrders = new Dictionary<string, int>()
            {
                {ResourceHelper.Get(StringKey.Graphics), 0},
                {ResourceHelper.Get(StringKey.Controls), 1},
                {ResourceHelper.Get(StringKey.Cheats), 2},
                {ResourceHelper.Get(StringKey.Advanced), 3}
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

                // Update driver version label
                FFNxDriverUpdater ffnxUpdater = new FFNxDriverUpdater();
                txtDriverVersion.Text = $"FFNx v{ffnxUpdater.GetCurrentDriverVersion()}";

                scrollViewer.Content = stackPanel;
                tab.Content = scrollViewer;
                tabCtrlMain.Items.Add(tab);
            }

            return true;
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

                _settings.Save();

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
            Sys.FFNxConfig.RestoreBackup();
            Sys.FFNxConfig.ResetTo7thHeavenDefaults();
            Sys.FFNxConfig.Save();

            foreach (var item in ViewModels)
            {
                item.ResetToDefault(_settings);
            }

            SetStatusMessage(ResourceHelper.Get(StringKey.GameDriverSettingsResetToDefaults));
        }
    }
}
