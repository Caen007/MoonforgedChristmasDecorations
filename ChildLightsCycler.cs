// Assets/Moonforged Christmas Decorations/Scripts/ChildLightsCycler.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Cycles Red/Yellow/Blue on specific child renderers only (Light1/Light2/Light3).
    /// Now uses sharedMaterial + timed coroutine instead of per-frame Update.
    public class ChildLightsCycler : MonoBehaviour
    {
        public string[] lightRendererNames = new[] { "Light1", "Light2", "Light3" };
        public float stepSeconds = 1f;
        public float intensity = 4.4f;

        private readonly List<Material> _mats = new List<Material>();
        private static readonly Color[] Palette = { Color.red, Color.yellow, Color.blue };
        private int _step;
        private Coroutine _runner;

        private void Awake()
        {
            _mats.Clear();

            for (int i = 0; i < lightRendererNames.Length; i++)
            {
                Transform t = transform.Find(lightRendererNames[i]);
                if (t == null) continue;

                Renderer r = t.GetComponent<Renderer>();
                if (r == null) continue;

                // Use sharedMaterial to avoid many unique material instances
                Material m = r.sharedMaterial;
                if (m == null) continue;

                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Palette[i % Palette.Length] * Mathf.LinearToGammaSpace(intensity));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                _mats.Add(m);
            }
        }

        private void OnEnable()
        {
            if (_runner == null && _mats.Count > 0)
            {
                _runner = StartCoroutine(Run());
            }
        }

        private void OnDisable()
        {
            if (_runner != null)
            {
                StopCoroutine(_runner);
                _runner = null;
            }
        }

        private IEnumerator Run()
        {
            float delay = Mathf.Max(0.05f, stepSeconds);

            while (true)
            {
                if (_mats.Count > 0)
                {
                    ApplyStep();
                }
                yield return new WaitForSeconds(delay);
            }
        }

        private void ApplyStep()
        {
            for (int i = 0; i < _mats.Count; i++)
            {
                Material m = _mats[i];
                if (m == null) continue;

                Color c = Palette[(i + _step) % Palette.Length];
                m.SetColor("_EmissionColor", c * Mathf.LinearToGammaSpace(intensity));
            }

            _step = (_step + 1) % Palette.Length;
        }
    }
}
