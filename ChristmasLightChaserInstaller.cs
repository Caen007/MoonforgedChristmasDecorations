// Assets/Moonforged Christmas Decorations/Scripts/ChristmasLightChaserInstaller.cs
// Filters bulb renderers by name while excluding cables and snap points.

using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public static class ChristmasLightChaserInstaller
    {
        // Back-compat overload
        public static void InstallOn(GameObject prefabRoot,
                                     float stepSeconds,
                                     float emissionIntensity)
        {
            InstallOn(prefabRoot, stepSeconds, emissionIntensity, null, ChristmasLightChaser.AnimationMode.Chase);
        }

        // Main installer with palette + mode
        public static void InstallOn(GameObject prefabRoot,
                                     float stepSeconds,
                                     float emissionIntensity,
                                     Color[] paletteOverride,
                                     ChristmasLightChaser.AnimationMode mode)
        {
            if (!prefabRoot) return;

            var comp = prefabRoot.GetComponent<ChristmasLightChaser>();
            if (!comp) comp = prefabRoot.AddComponent<ChristmasLightChaser>();

            comp.includeInactive = true;
            comp.stepSeconds = stepSeconds;
            comp.emissionIntensity = emissionIntensity;
            comp.mode = mode;
            // Keep affectBaseColor disabled to preserve albedo

            if (paletteOverride != null && paletteOverride.Length > 0)
                comp.paletteOverride = paletteOverride;

            // Bulb-only renderer selection
            comp.targets = CollectBulbRenderers(prefabRoot,
                includePrefixes: new[] { "light" },                // names starting with Light
                ignoreContains: new[] { "cable", "wire", "string", "snap", "garland", "rope" }
            );
        }

        private static List<Renderer> CollectBulbRenderers(GameObject root, string[] includePrefixes, string[] ignoreContains)
        {
            var list = new List<Renderer>();
            if (!root) return list;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!r) continue;
                var n = r.gameObject.name.ToLowerInvariant();

                // Skip known non-bulb parts
                bool skip = false;
                if (ignoreContains != null)
                {
                    for (int k = 0; k < ignoreContains.Length; k++)
                    {
                        var s = ignoreContains[k];
                        if (!string.IsNullOrEmpty(s) && n.Contains(s))
                        {
                            skip = true; break;
                        }
                    }
                }
                if (skip) continue;

                // Require name to start with one of the include prefixes (e.g., "light")
                bool ok = (includePrefixes == null || includePrefixes.Length == 0);
                if (!ok)
                {
                    for (int k = 0; k < includePrefixes.Length; k++)
                    {
                        var p = includePrefixes[k];
                        if (!string.IsNullOrEmpty(p) && n.StartsWith(p))
                        {
                            ok = true; break;
                        }
                    }
                }
                if (!ok) continue;

                list.Add(r);
            }
            return list;
        }
    }
}
