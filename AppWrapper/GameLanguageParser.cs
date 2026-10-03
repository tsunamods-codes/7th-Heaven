using System;
using System.Collections.Generic;
using System.Linq;

namespace AppWrapper
{
    public static class GameLanguageParser
    {
        public static List<string> ParseSupportedLanguages(IEnumerable<string> languageNodes)
        {
            List<string> languages = (languageNodes ?? Enumerable.Empty<string>())
                .Where(node => node != null)
                .SelectMany(node => node.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(language => language.Trim().ToUpperInvariant())
                .Where(language => language.Length > 0)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            if (languages.Count == 0)
            {
                languages.Add("EN");
            }

            return languages;
        }
    }
}