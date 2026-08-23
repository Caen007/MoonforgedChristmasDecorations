using UnityEngine;
using System.Globalization;
using System.Collections.Generic;

namespace Moonforged.ChristmasDecorations
{
    public static class WrappedGiftUse
    {
        public const string KEY_PREFAB = "wrapped_prefab";
        public const string KEY_STACK = "wrapped_stack";
        public const string KEY_QUALITY = "wrapped_quality";
        public const string KEY_VARIANT = "wrapped_variant";
        public const string KEY_DURABILITY = "wrapped_durability";

        public static bool OnUse(ItemDrop.ItemData gift, Player player)
        {
            if (gift == null || player == null)
                return false;

            Inventory inv = player.GetInventory();
            if (inv == null)
                return false;

            Dictionary<string, string> data = gift.m_customData;
            if (data == null || !data.TryGetValue(KEY_PREFAB, out string prefabName))
                return false;

            GameObject prefab = ZNetScene.instance?.GetPrefab(prefabName);
            if (prefab == null)
                return false;

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop == null)
                return false;

            int stack = 1;
            int quality = 1;
            int variant = 0;
            float durability = 0f;

            if (data.TryGetValue(KEY_STACK, out string s)) int.TryParse(s, out stack);
            if (data.TryGetValue(KEY_QUALITY, out s)) int.TryParse(s, out quality);
            if (data.TryGetValue(KEY_VARIANT, out s)) int.TryParse(s, out variant);
            if (data.TryGetValue(KEY_DURABILITY, out s))
                float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out durability);

            ItemDrop.ItemData restored = drop.m_itemData.Clone();
            restored.m_dropPrefab = prefab; // preserve the restored item identity
            restored.m_stack = Mathf.Max(1, stack);
            restored.m_quality = Mathf.Max(1, quality);
            restored.m_variant = variant;
            if (durability > 0f) restored.m_durability = durability;

            if (!inv.CanAddItem(restored))
                return false;

            inv.AddItem(restored);
            inv.RemoveItem(gift, 1);

            return true;
        }

        public static bool OnUse(ItemDrop.ItemData gift)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return false;

            return OnUse(gift, player);
        }
    }
}
