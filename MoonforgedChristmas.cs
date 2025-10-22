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
        public const string PluginVersion = "1.0.0";

        private AssetBundle christmasBundle;
        private static readonly List<GameObject> placedObjects = new List<GameObject>();

        public static ConfigEntry<string> PlayerPreferredCategory;

        private void Awake()
        {
            new Harmony("moonforged.christmas.scalingdebug").PatchAll();

            // was: "Moonforged.ChristmasDecorations.christmas"
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
                "Moonforged Christmas",
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
}
