using AppUI.ViewModels;

namespace AppUI.ViewModels
{
    public class InputTextViewModel : ViewModelBase
    {
        private string _windowTitle;
        private string _message;
        private string _textInput;
        private int _maxCharLength;

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

        public string TextInput
        {
            get
            {
                return _textInput;
            }
            set
            {
                _textInput = value;
                NotifyPropertyChanged();
            }
        }

        public int MaxCharLength
        {
            get
            {
                return _maxCharLength;
            }
            set
            {
                _maxCharLength = value;
                NotifyPropertyChanged();
            }

        }


        public InputTextViewModel()
        {
            MaxCharLength = 255;
        }

    }
}
