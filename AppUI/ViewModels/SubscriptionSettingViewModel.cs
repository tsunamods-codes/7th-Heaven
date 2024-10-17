namespace AppUI.ViewModels
{

    public class SubscriptionSettingViewModel : ViewModelBase
    {
        private string _name;
        private string _url;



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
            }
        }

        public string Url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
                NotifyPropertyChanged();
            }
        }


        public SubscriptionSettingViewModel(string url, string name)
        {
            Url = url;
            Name = name;
        }
    }
}
