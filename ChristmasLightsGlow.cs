// ChristmasLightsGlow.cs
using System.Linq;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Put this on M_Christmas_Tree (root). It will find the child renderer
    /// and enable emission on the specified material slots.
    public class ChristmasLightsGlow : MonoBehaviour
    {
        [Header("Target renderer (child name)")]
        public string rendererChildName = "Christmas tree.007"; // renderer configured in the prefab

        [Header("Choose how to select materials")]
        public bool useIndices = true;                          // true => use slots; false => use names

        [Tooltip("Material slots that should glow (0-based)")]
        public int[] glowSlots = new int[] { 2, 7, 8 };         // emissive material slots

        [Tooltip("Match by material name (contains, case-insensitive)")]
        public string[] glowNameContains = new string[] { "vray_Christmas Tree Set3_4.001" };

        [Header("Glow settings")]
        public Color emissionColor = Color.yellow;              // base color
        public float intensity = 3f;                            // multiply for HDR
        public bool animate = false;                            // optional blink
        public float cycleSeconds = 5f;

        private Renderer _rend;
        private Material[] _mats;
        private bool[] _shouldGlow;

        void Awake()
        {
            // find the child renderer
            var child = transform.Find(rendererChildName);
            if (child != null) _rend = child.GetComponent<Renderer>();
            if (_rend == null)
            {
                // fallback: child renderer with the largest material set
                _rend = GetComponentsInChildren<Renderer>(true)
                        .OrderByDescending(r => r.sharedMaterials != null ? r.sharedMaterials.Length : 0)
                        .FirstOrDefault();
            }
            if (_rend == null) return;

            // instance materials to preserve shared materials
            _mats = _rend.materials;
            _shouldGlow = new bool[_mats.Length];

            for (int i = 0; i < _mats.Length; i++)
            {
                bool match = false;
                if (useIndices)
                {
                    for (int k = 0; k < glowSlots.Length; k++)
                        if (glowSlots[k] == i) { match = true; break; }
                }
                else
                {
                    string matName = _mats[i] != null ? _mats[i].name : "";
                    for (int k = 0; k < glowNameContains.Length; k++)
                    {
                        if (!string.IsNullOrEmpty(glowNameContains[k]) &&
                            matName.ToLower().Contains(glowNameContains[k].ToLower()))
                        { match = true; break; }
                    }
                }

                _shouldGlow[i] = match;

                if (match && _mats[i] != null)
                {
                    _mats[i].EnableKeyword("_EMISSION");
                    // set initial emission
                    Color c = emissionColor * Mathf.LinearToGammaSpace(intensity);
                    _mats[i].SetColor("_EmissionColor", c);
                    _mats[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }

            _rend.materials = _mats; // assign back
        }

        void Update()
        {
            if (!animate || _mats == null) return;

            float dur = Mathf.Max(0.01f, cycleSeconds);
            float t = (Time.time % dur) / dur;         // 0..1
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f); // 0..1

            for (int i = 0; i < _mats.Length; i++)
            {
                if (_shouldGlow[i] && _mats[i] != null)
                {
                    Color c = emissionColor * Mathf.LinearToGammaSpace(intensity * (0.5f + pulse));
                    _mats[i].SetColor("_EmissionColor", c);
                }
            }
        }
    }
}
