using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Glows and cycles color when the object is placed on an ItemStand (for M_Star).
    public class RainbowGlow : MonoBehaviour
    {
        public float cycleSeconds = 10f;
        public float emissionIntensity = 1.5f;
        public float lightRange = 3f;
        public float lightIntensity = 1.8f;

        private bool _active;
        private Material _matInstance;
        private Light _light;

        void Start()
        {
            // Enable only when spawned as a stand visual
            _active = GetComponentInParent<ItemStand>() != null;
            if (!_active) return;

            var rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                _matInstance = rend.material;            // instance, not shared
                _matInstance.EnableKeyword("_EMISSION");
                _matInstance.SetColor("_EmissionColor", Color.white * emissionIntensity);
                _matInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            _light = GetComponent<Light>();
            if (_light == null)
            {
                _light = gameObject.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.range = lightRange;
                _light.intensity = lightIntensity;
                _light.shadows = LightShadows.None;
            }
        }

        void Update()
        {
            if (!_active || _matInstance == null || _light == null) return;

            float dur = Mathf.Max(0.01f, cycleSeconds);
            float phase = (Time.time % dur) / dur;
            Color c = Color.HSVToRGB(phase, 1f, 1f);

            _matInstance.SetColor("_EmissionColor", c * emissionIntensity);
            _light.color = c;
        }
    }
}
