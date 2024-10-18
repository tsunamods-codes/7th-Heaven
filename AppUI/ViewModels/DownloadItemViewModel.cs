using AppCore;
using Iros.Workshop;
using AppUI.Classes;

namespace AppUI.ViewModels
{


    public class DownloadItemViewModel : ViewModelBase
    {
        private bool _isSelected;
        private bool _isCancelling;

        public DownloadItem Download { get; set; }

        public string ItemName
        {
            get
            {
                return Download?.ItemName;
            }
            set
            {
                if (value != Download.ItemName)
                {
                    Download.ItemName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double PercentComplete
        {
            get
            {
                return Download.PercentComplete;
            }
            set
            {
                if (value != Download.PercentComplete)
                {
                    Download.PercentComplete = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string DownloadSpeed
        {
            get
            {
                return Download.DownloadSpeed;
            }
            set
            {
                Download.DownloadSpeed = value;
                NotifyPropertyChanged();
            }
        }

        public string RemainingTime
        {
            get
            {
                return Download.RemainingTime;
            }
            set
            {
                Download.RemainingTime = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsCancelling
        {
            get
            {
                return _isCancelling;
            }
            set
            {
                _isCancelling = value;
                NotifyPropertyChanged();
            }
        }

        public DownloadItemViewModel(DownloadItem download)
        {
            // translate hard coded english words from DownloadItem constructor
            if (download.RemainingTime == "Unknown")
            {
                download.RemainingTime = ResourceHelper.Get(StringKey.Unknown);
            }

            if (download.DownloadSpeed == "Pending...")
            {
                download.DownloadSpeed = ResourceHelper.Get(StringKey.Pending);
            }

            this.Download = download;
            IsCancelling = false;

            SetExternalUrlDownloadMessage();
        }

        /// <summary>
        /// Set message shown to user when opening external link to reflect mod failing to download or only alowing external downloads for it
        /// </summary>
        private void SetExternalUrlDownloadMessage()
        {
            Download.ExternalUrlDownloadMessage = ResourceHelper.Get(StringKey.ExternalUrlDownloadMessage2);

            if (Download.IsModOrPatchDownload && Download.Links.Count == 1)
            {
                // check that the only link available is external url and set message accordingly
                if (LocationUtil.TryParse(Download.Links[0], out LocationType downloadType, out string url))
                {
                    if (downloadType == LocationType.ExternalUrl)
                    {
                        Download.ExternalUrlDownloadMessage = ResourceHelper.Get(StringKey.ExternalUrlDownloadMessage1);
                    }
                }
            }

        }

    }
}
