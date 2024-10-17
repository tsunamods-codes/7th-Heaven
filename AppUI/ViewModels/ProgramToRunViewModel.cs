namespace AppUI.ViewModels
{

    public class ProgramToRunViewModel : ViewModelBase
    {
        private string _programPath;
        private string _programArguments;

        public string ProgramPath
        {
            get
            {
                return _programPath;
            }
            set
            {
                _programPath = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DisplayName));
            }
        }

        public string ProgramArguments
        {
            get
            {
                return _programArguments;
            }
            set
            {
                _programArguments = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName
        {
            get
            {
                return $"{ProgramPath} {ProgramArguments}";
            }
        }


        public ProgramToRunViewModel(string programPath, string args)
        {
            ProgramPath = programPath;
            ProgramArguments = args;
        }
    }
}
