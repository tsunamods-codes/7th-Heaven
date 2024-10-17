using AppCore;
using AppUI;
using System;
using System.Collections.Generic;

namespace AppUI.Classes
{
    public static class ResourceHelper
    {
        public static string Get(StringKey key)
        {
            try
            {
                return App.Current.Resources[key.ToString()].ToString().Replace("\\n", "\n");
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Maps the translated mod category to the respective english version
        /// </summary>
        public static Dictionary<string, string> ModCategoryTranslations
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { ResourceHelper.Get(StringKey.Miscellaneous), "Miscellaneous" },
                    { ResourceHelper.Get(StringKey.Animations), "Animations" },
                    { ResourceHelper.Get(StringKey.BattleModels), "Battle Models" },
                    { ResourceHelper.Get(StringKey.BattleTextures), "Battle Textures" },
                    { ResourceHelper.Get(StringKey.FieldModels), "Field Models" },
                    { ResourceHelper.Get(StringKey.FieldTextures), "Field Textures" },
                    { ResourceHelper.Get(StringKey.Gameplay), "Gameplay" },
                    { ResourceHelper.Get(StringKey.Media), "Media" },
                    { ResourceHelper.Get(StringKey.Minigames), "Minigames" },
                    { ResourceHelper.Get(StringKey.SpellTextures), "Spell Textures" },
                    { ResourceHelper.Get(StringKey.UserInterface), "User Interface" },
                    { ResourceHelper.Get(StringKey.WorldModels), "World Models" },
                    { ResourceHelper.Get(StringKey.WorldTextures), "World Textures" },
                    { ResourceHelper.Get(StringKey.Unknown), "Unknown" },
                };
            }
        }
    }
}
