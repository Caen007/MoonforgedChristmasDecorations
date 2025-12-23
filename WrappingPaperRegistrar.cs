using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public static class WrappingPaperRegistrar
    {
        private static bool registered;

        private struct PaperDef
        {
            public string PrefabName;
            public string DisplayName;

            public PaperDef(string prefabName, string displayName)
            {
                PrefabName = prefabName;
                DisplayName = displayName;
            }
        }

        public static void Register(AssetBundle bundle)
        {
            if (registered || bundle == null)
                return;

            var papers = new[]
            {
                new PaperDef("M_WrappingPaper_BlackOrange_Valheim", "Wrapping Paper (Black & Orange)"),
                new PaperDef("M_WrappingPaper_Red_Blue", "Wrapping Paper (Red & Blue)"),
                new PaperDef("M_WrappingPaper_Silver_Black", "Wrapping Paper (Silver & Black)"),
                new PaperDef("M_WrappingPaper_Yellow_Deco", "Wrapping Paper (Yellow Deco)"),
                new PaperDef("M_WrappingPaper_SnowFlake_Blue", "Wrapping Paper (SnowFlake Blue)"),
                new PaperDef("M_WrappingPaper_SnowFlake_Red", "Wrapping Paper (SnowFlake Red)"),
                new PaperDef("M_Gree_Gold_WrappingPaper", "Wrapping Paper (Green & Gold)"),
            };

            foreach (var def in papers)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>(def.PrefabName);
                if (prefab == null)
                    continue;

                prefab.name = def.PrefabName;

                ZNetView znv = prefab.GetComponent<ZNetView>() ?? prefab.AddComponent<ZNetView>();
                znv.m_persistent = true;

                prefab.layer = LayerMask.NameToLayer("item");

                ItemDrop item = prefab.GetComponent<ItemDrop>();
                if (item == null || item.m_itemData?.m_shared == null)
                    continue;

                var shared = item.m_itemData.m_shared;
                shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
                shared.m_maxStackSize = 100;
                shared.m_weight = 0.1f;
                shared.m_value = 0;
                shared.m_name = def.DisplayName;
                shared.m_description = "Festive paper used to wrap gifts.";

                Sprite icon = bundle.LoadAsset<Sprite>(def.PrefabName);
                if (icon != null)
                    shared.m_icons = new[] { icon };

                item.m_itemData.m_dropPrefab = prefab;

                ItemManager.Instance.AddItem(new CustomItem(prefab, true));

                ItemManager.Instance.AddRecipe(new CustomRecipe(new RecipeConfig
                {
                    Item = def.PrefabName,
                    Amount = 1,
                    CraftingStation = "M_Wrapping_Table",
                    Requirements = new[]
                    {
                        new RequirementConfig("Wood", 1),
                        new RequirementConfig("Dandelion", 1)
                    }
                }));
            }

            registered = true;
        }
    }
}
