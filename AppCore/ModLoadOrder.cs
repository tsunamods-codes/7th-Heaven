using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCore
{
    public enum ModCategory
    {
        Miscellaneous,
        Animations,
        BattleModels,
        BattleTextures,
        FieldModels,
        FieldTextures,
        Gameplay,
        Media,
        Minigames,
        SpellTextures,
        UserInterface,
        WorldModels,
        WorldTextures,
        Unknown
    }

    public static class ModLoadOrder
    {
        /// <summary>
        /// Maps the category to the respective load order (also accessible from <see cref="ModCategory"/> enum)
        /// </summary>
        public static Dictionary<string, int> Orders = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Miscellaneous", 0},
            { "Animations", 1},
            { "Battle Models", 2},
            { "Battle Textures", 3},
            { "Field Models", 4},
            { "Field Textures", 5},
            { "Gameplay", 6},
            { "Media", 7},
            { "Minigames", 8},
            { "Spell Textures", 9},
            { "User Interface", 10},
            { "World Models", 11},
            { "World Textures", 12},
            { "Unknown", 13}
        };

        /// <summary>
        /// Maps the english category to the respective <see cref="StringKey"/> that is used to translate the category.
        /// </summary>
        public static Dictionary<string, StringKey> ModCategoryTranslationKeys = new Dictionary<string, StringKey>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Miscellaneous", StringKey.Miscellaneous},
            { "Animations", StringKey.Animations},
            { "Battle Models", StringKey.BattleModels},
            { "Battle Textures", StringKey.BattleTextures},
            { "Field Models", StringKey.FieldModels},
            { "Field Textures", StringKey.FieldTextures},
            { "Gameplay", StringKey.Gameplay},
            { "Media", StringKey.Media},
            { "Minigames", StringKey.Minigames},
            { "Spell Textures", StringKey.SpellTextures},
            { "User Interface", StringKey.UserInterface},
            { "World Models", StringKey.WorldModels},
            { "World Textures", StringKey.WorldTextures},
            { "Unknown", StringKey.Unknown}
        };


        /// <summary>
        /// Returns the <see cref="StringKey"/> used for translating category. Null/empty string returns 'Unknown'
        /// </summary>
        /// <param name="englishCategory"></param>
        /// <returns></returns>
        public static StringKey GetCategoryKey(string englishCategory)
        {
            if (string.IsNullOrEmpty(englishCategory) || !ModCategoryTranslationKeys.ContainsKey(englishCategory))
            {
                return StringKey.Unknown;
            }

            return ModCategoryTranslationKeys[englishCategory];
        }

        /// <summary>
        /// Returns <paramref name="category"/> as an int
        /// </summary>
        public static int Get(ModCategory category)
        {
            return (int)category;
        }

        /// <summary>
        /// Returns the load order based on the <paramref name="category"/>
        /// </summary>
        /// <returns> 
        /// returns an int >= 0 if valid; 
        /// -1 if not valid category; 
        /// returns load order of Unknown if <paramref name="category"/> is null or empty string 
        /// </returns>
        public static int Get(string category)
        {            
            if (string.IsNullOrWhiteSpace(category))
            {
                return (int)ModCategory.Unknown;
            }

            if (Orders.TryGetValue(category, out int loadOrder))
            {
                return loadOrder;
            }

            if (Enum.TryParse<ModCategory>(category, true, out ModCategory result))
            {
                return (int)result;
            }

            return -1;
        }
    }
}
