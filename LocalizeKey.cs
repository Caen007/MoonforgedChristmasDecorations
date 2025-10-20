using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class LocalizeKey : MonoBehaviour
    {
        public string Key;

        public void Awake()
        {
            if (!string.IsNullOrEmpty(Key))
            {
                string localized = LocalizationCache.Get(Key);
                // Example: attach to a text component if needed
                // var textComponent = GetComponent<UnityEngine.UI.Text>();
                // if (textComponent != null) textComponent.text = localized;
            }
        }
    }
}
