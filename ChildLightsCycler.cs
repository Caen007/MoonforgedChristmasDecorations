using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Cycles Red/Yellow/Blue on specific child renderers only (Light1/Light2/Light3).
    public class ChildLightsCycler : MonoBehaviour
    {
        public string[] lightRendererNames = new[] { "Light1", "Light2", "Light3" };
        public float stepSeconds = 1f;
        public float intensity = 4.4f;

        private readonly List<Material> _mats = new List<Material>();
        private static readonly Color[] Palette = { Color.red, Color.yellow, Color.blue };

        void Start()
        {
            _mats.Clear();

            for (int i = 0; i < lightRendererNames.Length; i++)
            {
                Transform t = transform.Find(lightRendererNames[i]);
                if (t == null) continue;

                Renderer r = t.GetComponent<Renderer>();
                if (r == null) continue;

                // instanced material so we don't affect shared assets
                Material m = r.material;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Palette[i % Palette.Length] * Mathf.LinearToGammaSpace(intensity));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                _mats.Add(m);
            }
        }

        void Update()
        {
            if (_mats.Count == 0) return;

            int step = Mathf.FloorToInt(Time.time / Mathf.Max(0.01f, stepSeconds)) % Palette.Length;

            for (int i = 0; i < _mats.Count; i++)
            {
                Material m = _mats[i];
                if (m == null) continue;

                Color c = Palette[(i + step) % Palette.Length];
                m.SetColor("_EmissionColor", c * Mathf.LinearToGammaSpace(intensity));
            }
        }
    }
}
