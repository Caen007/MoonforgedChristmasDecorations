using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public static class WrappedGiftData
    {
        public const string KEY_PREFAB = "mf_wrap_prefab";
        public const string KEY_STACK = "mf_wrap_stack";
        public const string KEY_QUALITY = "mf_wrap_quality";
        public const string KEY_VARIANT = "mf_wrap_variant";
        public const string KEY_DURABILITY = "mf_wrap_durability";

        public static void Write(ZNetView znv, ItemDrop.ItemData item)
        {
            if (znv == null || !znv.IsValid() || item == null)
                return;

            ZDO zdo = znv.GetZDO();
            if (zdo == null)
                return;

            zdo.Set(KEY_PREFAB, item.m_dropPrefab.name);
            zdo.Set(KEY_STACK, item.m_stack);
            zdo.Set(KEY_QUALITY, item.m_quality);
            zdo.Set(KEY_VARIANT, item.m_variant);
            zdo.Set(KEY_DURABILITY, item.m_durability);
        }

        public static bool HasData(ZNetView znv)
        {
            if (znv == null || !znv.IsValid())
                return false;

            ZDO zdo = znv.GetZDO();
            if (zdo == null)
                return false;

            return !string.IsNullOrEmpty(zdo.GetString(KEY_PREFAB, ""));
        }

        public static ItemDrop.ItemData Read(ZNetView znv)
        {
            if (!HasData(znv))
                return null;

            ZDO zdo = znv.GetZDO();

            string prefab = zdo.GetString(KEY_PREFAB, "");
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefab);
            if (itemPrefab == null)
                return null;

            ItemDrop drop = itemPrefab.GetComponent<ItemDrop>();
            if (drop == null)
                return null;

            ItemDrop.ItemData data = drop.m_itemData.Clone();
            data.m_stack = zdo.GetInt(KEY_STACK, 1);
            data.m_quality = zdo.GetInt(KEY_QUALITY, 1);
            data.m_variant = zdo.GetInt(KEY_VARIANT, 0);
            data.m_durability = zdo.GetFloat(KEY_DURABILITY, data.GetMaxDurability());

            return data;
        }
    }
}
