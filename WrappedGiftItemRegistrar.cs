using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public static class WrappedGiftItemRegistrar
    {
        private static bool registered;

        private static readonly string[] GiftPrefabs =
        {
            "M_Gift_BlackOrange_Valheim",
            "M_Gift_Yellow_Deco",
            "M_Gift_Red_Blue",
            "M_Gree_Gold_Gift",
            "M_SnowFlake_Blue",
            "M_SnowFlake_Red",
            "M_Gift_Silver_Black"
        };

        public static void Register(AssetBundle bundle)
        {
            if (registered || bundle == null)
                return;

            foreach (string name in GiftPrefabs)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(name);
                if (prefab == null)
                    continue;

                prefab.name = name;

                ZNetView znv = prefab.GetComponent<ZNetView>() ?? prefab.AddComponent<ZNetView>();
                znv.m_persistent = true;

                prefab.layer = LayerMask.NameToLayer("item");

                ItemDrop item = prefab.GetComponent<ItemDrop>();
                if (item == null || item.m_itemData?.m_shared == null)
                    continue;

                var shared = item.m_itemData.m_shared;
                shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
                shared.m_maxStackSize = 1;
                shared.m_weight = 1f;
                shared.m_value = 0;
                shared.m_description = "A wrapped gift. Right-click to open.";

                Sprite icon = bundle.LoadAsset<Sprite>(name);
                if (icon != null)
                    shared.m_icons = new[] { icon };

                item.m_itemData.m_dropPrefab = prefab;

                ItemManager.Instance.AddItem(new CustomItem(prefab, true));
            }

            registered = true;
        }
    }
}
