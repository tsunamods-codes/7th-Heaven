namespace AppUI.ViewModels
{
    public class AllowModToRunViewModel : ViewModelBase
    {
        private string _windowTitle;
        private string _message;
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
            }
        }


        public AllowModToRunViewModel()
        {
            NoRadioButtonIsChecked = true;
            YesRadioButtonIsChecked = false;
        }

    }
}
