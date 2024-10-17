using AppUI.ViewModels;
using System.Windows;

namespace AppUI.ViewModels
{
    public class AllowModToRunViewModel : ViewModelBase
    {
        private string _windowTitle;
        private string _message;
        private bool _isChecked;
        private Visibility _checkboxVisibility;
        private bool _noRadioButtonIsChecked;
        private bool _yesRadioButtonIsChecked;

        public string WindowTitle
        {
            get
            {
                return _windowTitle;
            }
            set
            {
                _windowTitle = value;
                NotifyPropertyChanged();
            }
        }

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility CheckboxVisibility
        {
            get
            {
                return _checkboxVisibility;
            }
            set
            {
                _checkboxVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                _isChecked = value;
                NotifyPropertyChanged();
            }
        }

        public bool NoRadioButtonIsChecked
        {
            get
            {
                return _noRadioButtonIsChecked;
            }
            set
            {
                _noRadioButtonIsChecked = value;
                NotifyPropertyChanged();

                if (_noRadioButtonIsChecked)
                {
                    IsChecked = false;
                }
            }
        }

        public bool YesRadioButtonIsChecked
        {
            get
            {
                return _yesRadioButtonIsChecked;
            }
            set
            {
                _yesRadioButtonIsChecked = value;
                NotifyPropertyChanged();


                if (_yesRadioButtonIsChecked)
                {
                    CheckboxVisibility = Visibility.Visible;
                }
                else
                {
                    CheckboxVisibility = Visibility.Collapsed;
                }
            }
        }


        public MessageBoxResult Result { get; set; }

        public AllowModToRunViewModel()
        {
            Result = MessageBoxResult.Cancel;
            IsChecked = false;
            CheckboxVisibility = Visibility.Collapsed;
            NoRadioButtonIsChecked = true;
            YesRadioButtonIsChecked = false;
        }

    }
}
