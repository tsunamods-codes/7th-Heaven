using Iros.Workshop.ConfigSettings;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AppUI.ViewModels
{
    enum GLSettingType
    {
        TextEntry,
        Checkbox,
        Dropdown
    }

    class GLSettingViewModel : ViewModelBase
    {
        private string _name;
        private string _description;
        private string _group;
        private List<GLSettingDropdownOptionViewModel> _dropdownOptions;
        private GLSettingType _settingType;
        private string _textEntryOptionName;
        private string _textEntryOptionValue;
        private int _selectedDropdownIndex;
        private bool _isOptionChecked;

        public Setting Setting { get; set; }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName
        {
            get
            {
                return $"{Name}:";
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = String.Format(value, "\r\n");
                NotifyPropertyChanged();
            }
        }

        public string Group
        {
            get
            {
                return _group;
            }
            set
            {
                _group = value;
                NotifyPropertyChanged();
            }
        }

        public GLSettingType SettingType
        {
            get
            {
                return _settingType;
            }
            set
            {
                _settingType = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CheckBoxVisibility));
                NotifyPropertyChanged(nameof(TextBoxVisibility));
                NotifyPropertyChanged(nameof(ComboBoxVisibility));
            }
        }

        public bool IsOptionChecked
        {
            get
            {
                return _isOptionChecked;
            }
            set
            {
                _isOptionChecked = value;
                NotifyPropertyChanged();
            }
        }
        public List<GLSettingDropdownOptionViewModel> DropdownOptions
        {
            get
            {
                return _dropdownOptions;
            }
            set
            {
                _dropdownOptions = value;
                NotifyPropertyChanged();
            }
        }

        public int SelectedDropdownIndex
        {
            get
            {
                return _selectedDropdownIndex;
            }
            set
            {
                _selectedDropdownIndex = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(SelectedDropdownOptionText));
            }
        }
        
        public string SelectedDropdownOptionText
        {
            get
            {
                if (DropdownOptions == null || SelectedDropdownIndex < 0 || SelectedDropdownIndex >= DropdownOptions.Count)
                    return "";

                return DropdownOptions[SelectedDropdownIndex].DisplayText;
            }
        }


        public string TextEntryOptionName
        {
            get
            {
                return _textEntryOptionName;
            }
            set
            {
                _textEntryOptionName = value;
                NotifyPropertyChanged();
            }
        }

        public string TextEntryOptionValue
        {
            get
            {
                return _textEntryOptionValue;
            }
            set
            {
                _textEntryOptionValue = value;
                NotifyPropertyChanged();
            }
        }

        
        public Visibility CheckBoxVisibility
        {
            get
            {
                if (SettingType == GLSettingType.Checkbox)
                    return Visibility.Visible;

                return Visibility.Hidden;
            }
        }

        public Visibility TextBoxVisibility
        {
            get
            {
                if (SettingType == GLSettingType.TextEntry)
                    return Visibility.Visible;

                return Visibility.Hidden;
            }
        }

        public Visibility ComboBoxVisibility
        {
            get
            {
                if (SettingType == GLSettingType.Dropdown)
                    return Visibility.Visible;

                return Visibility.Hidden;
            }
        }


        public GLSettingViewModel(Setting setting, Settings settings)
        {
            Setting = setting;
            Name = setting.Name;
            Description = setting.Description;
            Group = setting.Group;

            Init(settings);
        }

        private void Init(Settings settings)
        {
            if (Setting is Iros.Workshop.ConfigSettings.Checkbox)
            {
                SettingType = GLSettingType.Checkbox;
                IsOptionChecked = settings.IsMatched((Setting as Checkbox).TrueSetting);
            }
            else if (Setting is Iros.Workshop.ConfigSettings.TextEntry)
            {
                SettingType = GLSettingType.TextEntry;
                TextEntryOptionName = (Setting as TextEntry).Option;
                TextEntryOptionValue = settings.Get(TextEntryOptionName);
            }
            else if (Setting is Iros.Workshop.ConfigSettings.DropDown)
            {
                SettingType = GLSettingType.Dropdown;
                
                if (DropdownOptions == null)
                {
                    DropdownOptions = (Setting as DropDown).Options.Select(item => new GLSettingDropdownOptionViewModel()
                                                                                    {
                                                                                        DisplayText = item.Text,
                                                                                        SettingsValue = item.Settings
                                                                                    }).ToList();
                }

                int i = 0;
                foreach (var item in DropdownOptions)
                {
                    if (settings.IsMatched(item.SettingsValue))
                    {
                        SelectedDropdownIndex = i;
                    }

                    i++;
                }

            }
        }

        public void Save(Settings settings)
        {
            if (SettingType == GLSettingType.TextEntry)
            {
                settings.Apply($"{TextEntryOptionName}={TextEntryOptionValue}");
            }
            else if (SettingType == GLSettingType.Dropdown && SelectedDropdownIndex >= 0)
            {
                GLSettingDropdownOptionViewModel selected = DropdownOptions[SelectedDropdownIndex];
                settings.Apply(selected.SettingsValue);
            }
            else if (SettingType == GLSettingType.Checkbox)
            {
                string settingValue = IsOptionChecked ? (Setting as Checkbox).TrueSetting : (Setting as Checkbox).FalseSetting;
                settings.Apply(settingValue);
            }
        }

        public void ResetToDefault(Settings settings)
        {
            settings.Apply(Setting.DefaultValue);
            Init(settings);
        }
    }

    class GLSettingDropdownOptionViewModel : ViewModelBase
    {
        private string _displayText;
        private string _settingsValue;

        public string DisplayText
        {
            get
            {
                return _displayText;
            }
            set
            {
                _displayText = value;
                NotifyPropertyChanged();
            }
        }

        public string SettingsValue
        {
            get
            {
                return _settingsValue;
            }
            set
            {
                _settingsValue = value;
                NotifyPropertyChanged();
            }
        }
    }
}
