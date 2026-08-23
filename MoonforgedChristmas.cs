using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public class MoonforgedChristmas : BaseUnityPlugin
    {
        public const string PluginGUID = "Moonforged.ChristmasDecorations";
        public const string PluginName = "Moonforged Christmas Decorations";
        public const string PluginVersion = "1.0.5";

        private AssetBundle christmasBundle;
        private static readonly List<GameObject> placedObjects = new List<GameObject>();

        public static ConfigEntry<string> PlayerPreferredCategory;

        private void Awake()
        {
            new Harmony("moonforged.christmas.scalingdebug").PatchAll();

            string resourcePath = "MoonforgedChristmasDecorations.christmas";

            christmasBundle = EmbeddedAssetBundleLoader.LoadBundle(resourcePath);

            if (christmasBundle == null)
            {
                Logger.LogError("Failed to load embedded AssetBundle.");
                return;
            }

            TrackAllPrefabsInBundle(christmasBundle);

            PlayerPreferredCategory = Config.Bind(
                "General",
                "CustomHammerTab",
                "Moonforged",
                "Set the hammer tab where this mod's pieces should appear (e.g., Building, Furniture, Moonforged Christmas)"
            );

            foreach (string category in RelicRegistrar.GetAllCategories())
            {
                PieceManager.Instance.AddPieceCategory(category);
            }

            PrefabManager.OnPrefabsRegistered += () =>
            {
                StartCoroutine(DelayedRegister(christmasBundle));
            };
        }

        private IEnumerator DelayedRegister(AssetBundle bundle)
        {
            while (ZNetScene.instance == null)
            {
                yield return null;
            }

            RelicRegistrar.RegisterAllRelics(bundle);
        }

        public static void TrackAllPrefabsInBundle(AssetBundle bundle)
        {
            foreach (GameObject prefab in bundle.LoadAllAssets<GameObject>())
            {
                if (prefab != null && prefab.GetComponent<PlacementWatcher>() == null)
                {
                    prefab.AddComponent<PlacementWatcher>().RegisterList = placedObjects;
                }
            }
        }
    }

    public static class EmbeddedAssetBundleLoader
    {
        public static AssetBundle LoadBundle(string resourcePath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    UnityEngine.Debug.LogError("AssetBundle resource not found: " + resourcePath);
                    return null;
                }
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return AssetBundle.LoadFromMemory(buffer);
            }
        }
    }

    // ============================================================
    // Gift opening patch
    // ============================================================
    [HarmonyPatch]
    internal static class WrappedGiftConsumePatch
    {
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        [HarmonyPrefix]
        private static bool Player_ConsumeItem_Prefix(Player __instance, Inventory inventory, ItemDrop.ItemData item)
        {
            if (__instance == null || inventory == null || item == null)
                return true;

            if (!IsGift(item))
                return true;

            // Pass the active Player instance for restart-safe gift use
            if (WrappedGiftUse.OnUse(item, __instance))
                return false;

            return true;
        }

        private static bool IsGift(ItemDrop.ItemData item)
        {
            if (item == null || item.m_dropPrefab == null)
                return false;

            string n = item.m_dropPrefab.name;
            if (string.IsNullOrEmpty(n))
                return false;

            if (n.StartsWith("M_Gift_")) return true;
            if (n.StartsWith("M_SnowFlake_")) return true;
            if (n == "M_Gree_Gold_Gift") return true;

            return false;
        }
    }

    // ============================================================
    // Wrapper hover patches
    // ============================================================
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverName))]
    internal static class WrappingBoxHoverNamePatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (__instance == null) return;

            var proc = __instance.GetComponentInParent<WrappingBoxProcessor>();
            if (proc == null) return;

            __result = "Wrapper";
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class WrappingBoxHoverTextPatch
    {
        private static void Postfix(Container __instance, ref string __result)
        {
            if (__instance == null) return;

            var proc = __instance.GetComponentInParent<WrappingBoxProcessor>();
            if (proc == null) return;

            if (proc.CanWrap())
            {
                __result =
                    "Wrapper\n" +
                    "[<color=yellow><b>E</b></color>] Open\n" +
                    "<color=orange>Close box to wrap gift</color>";
                return;
            }

            __result =
                "Wrapper\n" +
                "[<color=yellow><b>E</b></color>] Open\n" +
                "(Add 1 gift + 1 item)";
        }
    }
}
