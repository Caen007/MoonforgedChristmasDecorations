using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class StockingGiftStorage : MonoBehaviour
    {
        private const string VisualZdoKey = "mf_stocking_visual_full";
        private const string EmptyVisualName = "Visual_Empty";
        private const string FullVisualName = "Visual_Full";

        private Container container;
        private ZNetView nview;
        private GameObject emptyVisual;
        private GameObject fullVisual;
        private float nextCheckTime;
        private float nextInvalidItemMessageTime;

        public static bool IsStockingPrefabName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
                return false;

            return prefabName.IndexOf("Stocking_", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   string.Equals(prefabName, "M_Christmas_Stocking", System.StringComparison.OrdinalIgnoreCase);
        }

        public static void Install(GameObject prefab)
        {
            if (prefab == null)
                return;

            Container stockingContainer = prefab.GetComponent<Container>() ?? prefab.AddComponent<Container>();
            SetContainerField(stockingContainer, "m_name", "Christmas Stocking");
            SetContainerField(stockingContainer, "m_width", 2);
            SetContainerField(stockingContainer, "m_height", 1);
            SetContainerField(stockingContainer, "m_autoDestroyEmpty", false);
            SetContainerField(stockingContainer, "m_usePrefabName", false);

            if (prefab.GetComponent<StockingGiftStorage>() == null)
                prefab.AddComponent<StockingGiftStorage>();
        }

        private static void SetContainerField(Container target, string fieldName, object value)
        {
            if (target == null)
                return;

            FieldInfo field = typeof(Container).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
                field.SetValue(target, value);
        }

        private void Awake()
        {
            container = GetComponent<Container>();
            nview = GetComponent<ZNetView>();
            CacheVisuals();
        }

        private void Start()
        {
            RefreshStorageState();
        }

        private void Update()
        {
            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + 0.10f;
            RefreshStorageState();
        }

        public string GetDisplayName()
        {
            Piece piece = GetComponent<Piece>();
            return piece != null && !string.IsNullOrWhiteSpace(piece.m_name)
                ? piece.m_name
                : "Christmas Stocking";
        }

        public string GetStorageHoverText()
        {
            return
                GetDisplayName() + "\n" +
                "[<color=yellow><b>E</b></color>] Open\n" +
                GetStoredGiftCount() + "/2 wrapped gifts";
        }

        private void RefreshStorageState()
        {
            if (container == null)
                container = GetComponent<Container>();

            if (container == null)
                return;

            if (nview == null)
                nview = GetComponent<ZNetView>();

            CacheVisuals();

            Inventory inventory = container.GetInventory();
            if (inventory == null)
                return;

            if (nview != null && nview.IsValid() && nview.IsOwner())
                ReturnInvalidItems(inventory);

            int giftCount = CountWrappingTableGifts(inventory.GetAllItems());
            UpdateVisualState(giftCount > 0);
        }

        private int GetStoredGiftCount()
        {
            if (container == null)
                container = GetComponent<Container>();

            Inventory inventory = container != null ? container.GetInventory() : null;
            return inventory != null
                ? CountWrappingTableGifts(inventory.GetAllItems())
                : 0;
        }

        private void CacheVisuals()
        {
            if (emptyVisual == null)
            {
                Transform empty = FindChildByName(transform, EmptyVisualName);
                if (empty != null)
                    emptyVisual = empty.gameObject;
            }

            if (fullVisual == null)
            {
                Transform full = FindChildByName(transform, FullVisualName);
                if (full != null)
                    fullVisual = full.gameObject;
            }
        }

        private void UpdateVisualState(bool hasGift)
        {
            bool showFull = hasGift;

            if (nview != null && nview.IsValid())
            {
                ZDO zdo = nview.GetZDO();
                if (zdo != null)
                {
                    int desiredState = hasGift ? 1 : 0;

                    if (nview.IsOwner())
                    {
                        if (zdo.GetInt(VisualZdoKey, -1) != desiredState)
                            zdo.Set(VisualZdoKey, desiredState);
                    }
                    else
                    {
                        showFull = zdo.GetInt(VisualZdoKey, desiredState) == 1;
                    }
                }
            }

            if (emptyVisual != null && emptyVisual.activeSelf == showFull)
                emptyVisual.SetActive(!showFull);

            if (fullVisual != null && fullVisual.activeSelf != showFull)
                fullVisual.SetActive(showFull);
        }

        private void ReturnInvalidItems(Inventory stockingInventory)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            Inventory playerInventory = player.GetInventory();
            if (playerInventory == null)
                return;

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>(stockingInventory.GetAllItems());
            bool returnedAnyItem = false;

            for (int i = 0; i < items.Count; i++)
            {
                ItemDrop.ItemData item = items[i];
                if (item == null || item.m_dropPrefab == null || IsWrappingTableGift(item))
                    continue;

                ItemDrop.ItemData returnedItem = item.Clone();
                if (!playerInventory.CanAddItem(returnedItem))
                    continue;

                if (playerInventory.AddItem(returnedItem))
                {
                    stockingInventory.RemoveItem(item);
                    returnedAnyItem = true;
                }
            }

            if (returnedAnyItem && Time.time >= nextInvalidItemMessageTime)
            {
                nextInvalidItemMessageTime = Time.time + 1f;
                player.Message(MessageHud.MessageType.Center, "Only gifts made at the wrapping table can be placed in a Christmas stocking.");
            }
        }

        private static int CountWrappingTableGifts(List<ItemDrop.ItemData> items)
        {
            int count = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (IsWrappingTableGift(items[i]))
                    count += Mathf.Max(1, items[i].m_stack);
            }

            return count;
        }

        private static bool IsWrappingTableGift(ItemDrop.ItemData item)
        {
            if (!IsGiftPrefab(item) || item.m_customData == null)
                return false;

            Dictionary<string, string> data = item.m_customData;
            string wrappedPrefab;

            return data.TryGetValue(WrappedGiftUse.KEY_PREFAB, out wrappedPrefab) &&
                   !string.IsNullOrEmpty(wrappedPrefab) &&
                   data.ContainsKey(WrappedGiftUse.KEY_STACK) &&
                   data.ContainsKey(WrappedGiftUse.KEY_QUALITY) &&
                   data.ContainsKey(WrappedGiftUse.KEY_VARIANT) &&
                   data.ContainsKey(WrappedGiftUse.KEY_DURABILITY);
        }

        private static bool IsGiftPrefab(ItemDrop.ItemData item)
        {
            if (item == null || item.m_dropPrefab == null)
                return false;

            string prefabName = item.m_dropPrefab.name;
            if (string.IsNullOrEmpty(prefabName))
                return false;

            return prefabName.StartsWith("M_Gift_", System.StringComparison.Ordinal) ||
                   prefabName.StartsWith("M_SnowFlake_", System.StringComparison.Ordinal) ||
                   prefabName == "M_Gree_Gold_Gift";
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null &&
                    string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverName))]
    internal static class StockingGiftStorageHoverNamePatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (__instance == null)
                return;

            StockingGiftStorage storage = __instance.GetComponent<StockingGiftStorage>();
            if (storage != null)
                __result = storage.GetDisplayName();
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class StockingGiftStorageHoverTextPatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (__instance == null)
                return;

            StockingGiftStorage storage = __instance.GetComponent<StockingGiftStorage>();
            if (storage != null)
                __result = storage.GetStorageHoverText();
        }
    }
}