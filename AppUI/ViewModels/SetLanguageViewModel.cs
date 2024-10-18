using AppCore;
using Iros.Workshop;
using AppUI.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace AppUI.ViewModels
{

    public class SetLanguageViewModel : ViewModelBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _selectedLanguage;
        private Dictionary<string, string> _languagesMap;

        public List<string> Languages
        {
            get
            {
                return _languagesMap?.Keys.ToList();
            }
        }

        public Dictionary<string, string> LanguagesMap
        {
            get
            {
                return _languagesMap;
            }
            set
            {
                _languagesMap = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Languages));
            }
        }

        public string SelectedLanguage
        {
            get
            {
                return _selectedLanguage;
            }
            set
            {
                _selectedLanguage = value;
                NotifyPropertyChanged();
            }
        }


        public SetLanguageViewModel()
        {
            LanguagesMap = new Dictionary<string, string>();
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.BrazilianPortuguese)} (Português Brasileiro)", "pt-BR");
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.English)} (English)", "en");
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.French)} (Français)", "fr");
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.German)} (Deutsche)", "de");
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.Greek)} (Ελληνικά)", "gr");
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.Italian)} (Italiano)", "it");
            LanguagesMap.Add($"{ResourceHelper.Get(StringKey.Spanish)} (Español)", "es");
            //LanguagesMap.Add($"{ResourceHelper.Get(StringKey.Japanese)} (\u65e5\u672c\u8a9e)", "ja");


            string defaultLang = Sys.Settings.AppLanguage;

            if (string.IsNullOrWhiteSpace(defaultLang))
            {
                SelectedLanguage = LanguagesMap.Where(kv => kv.Value == "en").Select(k => k.Key).FirstOrDefault();
            }
            else if (defaultLang.Length >= 2)
            {
                SelectedLanguage = LanguagesMap.Where(kv => kv.Value == defaultLang.Substring(0, 2)).Select(k => k.Key).FirstOrDefault();
            }
        }

        internal bool SaveSelectedLanguageAsDefault()
        {
            try
            {
                Sys.Settings.AppLanguage = LanguagesMap[SelectedLanguage];

                (App.Current as AppUI.App).SetLanguageDictionary(Sys.Settings.AppLanguage);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }


            return true;
        }
    }
}
