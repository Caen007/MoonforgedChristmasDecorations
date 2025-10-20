using Jotunn.Managers;
using System.Collections.Generic;

namespace Moonforged.ChristmasDecorations
{
    public static class LocalizationCache
    {
        private static Dictionary<string, string> translations = new Dictionary<string, string>();

        public static void Add(string key, string value)
        {
            translations[key] = value;
        }

        public static string Get(string key, string fallback = null)
        {
            if (translations.TryGetValue(key, out string value))
                return value;
            return fallback ?? key;
        }

        public static void RegisterToJotunnLocalization()
        {
            var jotunnLoc = LocalizationManager.Instance.GetLocalization();
            foreach (var kvp in translations)
            {
                jotunnLoc.AddTranslation("English", kvp.Key, kvp.Value);
            }
        }
    }
}
