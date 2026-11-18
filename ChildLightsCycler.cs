using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class ChildLightsCycler : MonoBehaviour
    {
        public enum AnimationStyle { ColorChase, FlowingColors }

        public string[] lightRendererNames = { "Light1", "Light2", "Light3" };
        public float stepSeconds = 1f;
        public float intensity = 4.4f;
        public AnimationStyle style;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Color[] ChaseColors = { Color.red, Color.yellow, Color.blue };
        private static readonly Color[] FlowColors = { Color.blue, Color.red, Color.yellow, new Color(0.62f, 0.12f, 1f) };

        private readonly List<Renderer> _bulbs = new List<Renderer>();
        private MaterialPropertyBlock _properties;
        private Coroutine _runner;
        private static readonly WaitForSeconds RefreshDelay = new WaitForSeconds(1f / 30f);

        private void FindBulbs()
        {
            _properties = new MaterialPropertyBlock();
            foreach (string name in lightRendererNames)
            {
                Transform child = transform.Find(name);
                Renderer bulb = child == null ? null : child.GetComponent<Renderer>();
                Material material = bulb == null ? null : bulb.sharedMaterial;
                if (material == null || !material.HasProperty(EmissionColor)) continue;

                material.EnableKeyword("_EMISSION");
                _bulbs.Add(bulb);
            }
        }

        private void OnEnable()
        {
            _runner = StartCoroutine(Animate());
        }

        private void OnDisable()
        {
            if (_runner != null)
            {
                StopCoroutine(_runner);
                _runner = null;
            }
        }

        private IEnumerator Animate()
        {
            // Registration can set the animation style after AddComponent invokes OnEnable.
            float startTime = Time.time;
            yield return null;
            if (_properties == null) FindBulbs();
            if (_bulbs.Count == 0) yield break;

            while (true)
            {
                float phase = (Time.time - startTime) / Mathf.Max(0.01f, stepSeconds);
                float brightness = Mathf.LinearToGammaSpace(intensity);
                for (int i = 0; i < _bulbs.Count; i++)
                {
                    Color color = style == AnimationStyle.FlowingColors
                        ? GetFlowColor(i, phase)
                        : GetChaseColor(i, phase);
                    SetEmission(_bulbs[i], color * brightness);
                }
                yield return RefreshDelay;
            }
        }

        private static Color GetFlowColor(int index, float phase)
        {
            int step = Mathf.FloorToInt(phase);
            int current = (index + step) % FlowColors.Length;
            int next = (current + 1) % FlowColors.Length;
            float blend = Mathf.SmoothStep(0f, 1f, phase - step);
            return Color.Lerp(FlowColors[current], FlowColors[next], blend);
        }

        private void SetEmission(Renderer bulb, Color color)
        {
            if (bulb == null) return;
            bulb.GetPropertyBlock(_properties);
            _properties.SetColor(EmissionColor, color);
            bulb.SetPropertyBlock(_properties);
        }

        private static Color GetChaseColor(int index, float phase)
        {
            int step = Mathf.FloorToInt(phase) % ChaseColors.Length;
            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((phase % 1f - 0.7f) / 0.3f));
            Color current = ChaseColors[(index + step) % ChaseColors.Length];
            Color next = ChaseColors[(index + step + 1) % ChaseColors.Length];
            return Color.Lerp(current, next, fade);
        }
    }
}
