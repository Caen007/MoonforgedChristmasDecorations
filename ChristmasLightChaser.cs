using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class ChristmasLightChaser : MonoBehaviour
    {
        public enum AnimationMode { Chase, BlinkAll }

        public bool includeInactive = true;

        // default palette = same as tree (R/Y/B)
        public Color[] defaultPalette = new Color[] { Color.red, Color.yellow, Color.blue };

        // optional per-prefab override (set by installer)
        public Color[] paletteOverride;

        public AnimationMode mode = AnimationMode.Chase;
        [Min(0.01f)] public float stepSeconds = 1f;
        [Min(0f)] public float emissionIntensity = 4.4f;
        public bool affectBaseColor = false;
        public string[] ignoreNameContains = new string[] { "cable", "snap" };

        public List<Renderer> targets = new List<Renderer>();

        private readonly List<MaterialPropertyBlock> _mpbs = new List<MaterialPropertyBlock>();
        private int _colorId, _emissId, _step;
        private Coroutine _runner;
        private bool _liveStarted;
        private Color[] _palette; // resolved palette

        void Awake()
        {
            _palette = (paletteOverride != null && paletteOverride.Length > 0) ? paletteOverride : defaultPalette;

            _colorId = Shader.PropertyToID("_Color");
            _emissId = Shader.PropertyToID("_EmissionColor");

            if (targets.Count == 0)
            {
                foreach (var r in GetComponentsInChildren<Renderer>(includeInactive))
                {
                    if (!r) continue;
                    var n = r.name.ToLowerInvariant();
                    bool skip = false;
                    for (int k = 0; k < ignoreNameContains.Length; k++)
                        if (n.Contains(ignoreNameContains[k])) { skip = true; break; }
                    if (!skip) targets.Add(r);
                }
            }

            _mpbs.Clear();
            foreach (var r in targets)
            {
                if (!r) continue;
                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                if (r.material) r.material.EnableKeyword("_EMISSION");
                _mpbs.Add(mpb);
            }
        }

        void OnEnable()
        {
            if (_runner == null) _runner = StartCoroutine(Run());
        }

        void OnDisable()
        {
            if (_runner != null) StopCoroutine(_runner);
            _runner = null;
            _liveStarted = false;
        }

        System.Collections.IEnumerator Run()
        {
            var poll = new WaitForSeconds(0.2f);
            var stepWait = new WaitForSeconds(stepSeconds);

            while (true)
            {
                // pause while placement ghost (no ZDO yet)
                bool hasZdo = false;
                var znv = GetComponentInParent<ZNetView>();
                if (znv != null) hasZdo = znv.GetZDO() != null;

                if (!hasZdo)
                {
                    _liveStarted = false;
                    yield return poll;
                    continue;
                }

                if (!_liveStarted)
                {
                    _liveStarted = true;
                    _step = 0;
                }

                int n = Mathf.Max(1, _palette.Length);

                if (mode == AnimationMode.Chase)
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var r = targets[i]; if (!r) continue;
                        var mpb = _mpbs[i];
                        Color c = _palette[(i + _step) % n];
                        if (affectBaseColor) mpb.SetColor(_colorId, c);
                        mpb.SetColor(_emissId, c * Mathf.LinearToGammaSpace(emissionIntensity));
                        r.SetPropertyBlock(mpb);
                    }
                }
                else // BlinkAll
                {
                    Color c = _palette[_step % n]; // typically 2 colors: white/yellow
                    for (int i = 0; i < targets.Count; i++)
                    {
                        var r = targets[i]; if (!r) continue;
                        var mpb = _mpbs[i];
                        if (affectBaseColor) mpb.SetColor(_colorId, c);
                        mpb.SetColor(_emissId, c * Mathf.LinearToGammaSpace(emissionIntensity));
                        r.SetPropertyBlock(mpb);
                    }
                }

                _step++;
                yield return stepWait;
            }
        }
    }
}
