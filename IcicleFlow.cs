using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Meteor/icicle "drip" animation with staggered start batches.
    /// - Finds children named "Icicle_Lamp_*"
    /// - Within each, finds renderers whose GameObject names start with "Light"
    /// - Sorts bulbs top→bottom (auto-detects local vertical axis Y vs Z)
    /// - Runs a drip down each column, with columns assigned to batches by index%batchCount
    public class IcicleFlow : MonoBehaviour
    {
        [Header("Target naming")]
        public string columnNamePrefix = "Icicle_Lamp_";
        public string bulbNamePrefix = "Light";

        [Header("Look")]
        public Color dripColor = new Color(0.60f, 0.85f, 1.00f); // icy blue
        [Min(0f)] public float emissionIntensity = 4.5f;         // HDR-ish
        public bool affectBaseColor = false;                      // usually false (only emission)

        [Header("Timing")]
        [Min(0.01f)] public float dripStepSeconds = 0.15f;        // time between bulbs lighting in a column
        [Min(0f)] public float pauseAfterColumn = 0.60f;      // pause before a column restarts
        [Min(1)] public int batchCount = 3;                  // e.g. 3 => every 3rd column starts together
        [Min(0f)] public float batchSpacingSeconds = 4f;      // delay between batches (your 4s)

        [Header("Advanced")]
        public bool includeInactive = true;                       // include disabled children
        public bool autoDetectVerticalAxis = true;                // pick Y vs Z per setup
        public Axis verticalAxis = Axis.Auto;                     // fallback/override if needed

        public enum Axis { Auto, Y, Z }

        // Internal storage
        private readonly List<Column> _columns = new List<Column>();
        private float _gammaIntensity; // cached LinearToGammaSpace(emissionIntensity)
        private int _colorId, _emissId;

        private class Column
        {
            public int columnIndex;                // 0..N-1 left->right in hierarchy order
            public int batch;                      // columnIndex % batchCount
            public float cycleLength;              // bulbs*step + pause
            public List<Renderer> bulbs = new List<Renderer>();
            public List<MaterialPropertyBlock> mpb = new List<MaterialPropertyBlock>();
        }

        void Awake()
        {
            _colorId = Shader.PropertyToID("_Color");
            _emissId = Shader.PropertyToID("_EmissionColor");
            _gammaIntensity = Mathf.LinearToGammaSpace(Mathf.Max(0f, emissionIntensity));
            BuildColumns();
        }

        void OnEnable()
        {
            // ensure emission keywords/material blocks are ready
            PrepareMPBs();
        }

        void Update()
        {
            if (_columns.Count == 0) return;

            float now = Time.time;

            // For each column, compute its local time with batch offset, then light exactly one bulb
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                Column col = _columns[ci];
                if (col.bulbs.Count == 0) continue;

                // Batch offset: columns in batch 0 start immediately; batch 1 starts after +batchSpacing, etc.
                float startOffset = col.batch * Mathf.Min(batchSpacingSeconds, col.cycleLength * 0.9f);
                float t = now - startOffset;

                if (t < 0f)
                {
                    // not started yet -> ensure all bulbs off
                    SetColumnAll(col, Color.black);
                    continue;
                }

                float cycle = col.cycleLength;
                if (cycle <= 0.0001f) cycle = 0.0001f;

                float u = t % cycle; // time within cycle
                int steps = col.bulbs.Count;
                float litWindow = steps * Mathf.Max(0.01f, dripStepSeconds);

                if (u >= 0f && u < litWindow)
                {
                    int activeIndex = Mathf.Clamp(Mathf.FloorToInt(u / Mathf.Max(0.01f, dripStepSeconds)), 0, steps - 1);
                    SetColumnActive(col, activeIndex, dripColor);
                }
                else
                {
                    // pause segment: everything off
                    SetColumnAll(col, Color.black);
                }
            }
        }

        // --------- Build scene data ----------
        private void BuildColumns()
        {
            _columns.Clear();

            // Find all potential column parents under this prefab
            List<Transform> colParents = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(includeInactive))
            {
                if (!t) continue;
                if (!string.IsNullOrEmpty(columnNamePrefix) && !t.name.StartsWith(columnNamePrefix)) continue;
                colParents.Add(t);
            }

            // Sort columns by local X (approx left->right) for stable batching
            colParents.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));

            for (int i = 0; i < colParents.Count; i++)
            {
                Transform colT = colParents[i];
                Column col = new Column();
                col.columnIndex = i;
                col.batch = (batchCount > 0) ? (i % batchCount) : 0;

                // Collect bulb renderers under the column
                foreach (var r in colT.GetComponentsInChildren<Renderer>(includeInactive))
                {
                    if (!r) continue;
                    string n = r.gameObject.name.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(bulbNamePrefix) && !n.StartsWith(bulbNamePrefix.ToLowerInvariant()))
                        continue;
                    col.bulbs.Add(r);
                }

                if (col.bulbs.Count == 0)
                    continue;

                // Sort bulbs top->bottom. Auto-detect vertical axis by which has larger range.
                Axis axis = ResolveAxis(col.bulbs);
                col.bulbs.Sort((a, b) =>
                {
                    float av = GetLocalAxis(a.transform, axis);
                    float bv = GetLocalAxis(b.transform, axis);
                    // Descending (top first)
                    return -av.CompareTo(bv);
                });

                // Init MPBs list
                for (int k = 0; k < col.bulbs.Count; k++) col.mpb.Add(new MaterialPropertyBlock());

                // Cycle length = (#bulbs * step) + pause
                col.cycleLength = col.bulbs.Count * Mathf.Max(0.01f, dripStepSeconds) + Mathf.Max(0f, pauseAfterColumn);

                _columns.Add(col);
            }
        }

        private Axis ResolveAxis(List<Renderer> bulbs)
        {
            if (!autoDetectVerticalAxis)
                return verticalAxis == Axis.Auto ? Axis.Z : verticalAxis;

            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

            for (int i = 0; i < bulbs.Count; i++)
            {
                Transform t = bulbs[i].transform;
                Vector3 lp = t.localPosition;
                if (lp.y < minY) minY = lp.y;
                if (lp.y > maxY) maxY = lp.y;
                if (lp.z < minZ) minZ = lp.z;
                if (lp.z > maxZ) maxZ = lp.z;
            }

            float rangeY = maxY - minY;
            float rangeZ = maxZ - minZ;

            if (rangeY >= rangeZ) return Axis.Y;
            return Axis.Z;
        }

        private float GetLocalAxis(Transform t, Axis axis)
        {
            switch (axis)
            {
                case Axis.Y: return t.localPosition.y;
                case Axis.Z: return t.localPosition.z;
                default: return t.localPosition.z;
            }
        }

        private void PrepareMPBs()
        {
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                Column col = _columns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    var r = col.bulbs[bi];
                    if (!r) continue;
                    var mat = r.sharedMaterial;
                    if (mat != null) mat.EnableKeyword("_EMISSION");
                    r.GetPropertyBlock(col.mpb[bi]);
                }
            }
        }

        private void SetColumnActive(Column col, int activeIndex, Color onColor)
        {
            for (int i = 0; i < col.bulbs.Count; i++)
            {
                var r = col.bulbs[i]; if (!r) continue;
                var mpb = col.mpb[i];
                if (affectBaseColor) mpb.SetColor(_colorId, (i == activeIndex) ? onColor : Color.black);
                mpb.SetColor(_emissId, (i == activeIndex) ? onColor * _gammaIntensity : Color.black);
                r.SetPropertyBlock(mpb);
            }
        }

        private void SetColumnAll(Column col, Color c)
        {
            for (int i = 0; i < col.bulbs.Count; i++)
            {
                var r = col.bulbs[i]; if (!r) continue;
                var mpb = col.mpb[i];
                if (affectBaseColor) mpb.SetColor(_colorId, c);
                mpb.SetColor(_emissId, c * _gammaIntensity);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
