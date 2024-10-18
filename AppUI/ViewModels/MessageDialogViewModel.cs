using Iros.Workshop;
using Microsoft.Win32;
using AppUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AppUI.ViewModels
{
    public class MessageDialogViewModel : ViewModelBase
    {
        private string _windowTitle;
        private string _message;
        private bool _isChecked;
        private string _checkboxText;
        private Visibility _checkboxVisibility;
        private MessageBoxImage _imageToDisplay;
        private Visibility _detailsVisibility;
        private Visibility _messageVisibility;
        private string _details;

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

        public string Details
        {
            get
            {
                return _details;
            }
            set
            {
                _details = value;
                NotifyPropertyChanged();
            }
        }

        public string CheckboxText
        {
            get
            {
                return _checkboxText;
            }
            set
            {
                _checkboxText = value;
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

        public Visibility DetailsVisibility
        {
            get
            {
                return _detailsVisibility;
            }
            set
            {
                _detailsVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility MessageVisibility
        {
            get
            {
                return _messageVisibility;
            }
            set
            {
                _messageVisibility = value;
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

        public MessageBoxResult Result { get; set; }

        public MessageBoxImage ImageToDisplay
        {
            get
            {
                return _imageToDisplay;
            }
            set
            {
                _imageToDisplay = value;
                NotifyPropertyChanged();
            }
        }

        public MessageDialogViewModel()
        {
            Result = MessageBoxResult.Cancel;
            IsChecked = false;
            ImageToDisplay = MessageBoxImage.None;
            MessageVisibility = Visibility.Visible;
            DetailsVisibility = Visibility.Collapsed;
            CheckboxVisibility = Visibility.Collapsed;
        }

    }
}
