// Assets/Moonforged Christmas Decorations/Scripts/IcicleFlow.cs
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
        public bool affectBaseColor = false;                     // usually false (only emission)

        [Header("Timing")]
        [Min(0.01f)] public float dripStepSeconds = 0.15f;       // time between bulbs lighting in a column
        [Min(0f)] public float pauseAfterColumn = 0.60f;         // pause before a column restarts
        [Min(1)] public int batchCount = 3;                      // every third column starts together by default
        [Min(0f)] public float batchSpacingSeconds = 4f;         // delay between batches

        [Header("Advanced")]
        public bool includeInactive = true;                      // include disabled children
        public bool autoDetectVerticalAxis = true;               // pick Y vs Z per setup
        public Axis verticalAxis = Axis.Auto;                    // fallback/override if needed

        [Header("Performance")]
        [Min(0.01f)] public float minUpdateInterval = 0.05f;     // throttle heavy work
        [Min(0f)] public float activeDistance = 60f;             // disable when no player near

        public enum Axis { Auto, Y, Z }

        // Internal storage
        private readonly List<Column> _columns = new List<Column>();
        private float _gammaIntensity; // cached LinearToGammaSpace(emissionIntensity)
        private int _colorId, _emissId;

        private float _accum;
        private bool _isActive;

        private class Column
        {
            public int columnIndex;                // 0..N-1 left->right in hierarchy order
            public int batch;                      // columnIndex % batchCount
            public int activeIndex = -1;
            public float nextStepTime;
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
            _accum = 0f;
            _isActive = false;
        }

        void Update()
        {
            if (_columns.Count == 0) return;

            // Skip processing when no player is nearby
            if (activeDistance > 0f && !Player.IsPlayerInRange(transform.position, activeDistance))
            {
                if (_isActive)
                {
                    // turn off all bulbs once when deactivating
                    for (int ci = 0; ci < _columns.Count; ci++)
                    {
                        SetColumnAll(_columns[ci], Color.black);
                        _columns[ci].activeIndex = -1;
                    }
                    _isActive = false;
                }
                return;
            }

            if (!_isActive)
            {
                _isActive = true;
                ResetSequence(Time.time);
            }

            // Throttle processing to the configured frequency
            _accum += Time.deltaTime;
            if (_accum < minUpdateInterval) return;
            _accum = 0f;

            float now = Time.time;

            // Advance each column by exactly one bulb per step so frame delays cannot skip bulbs
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                Column col = _columns[ci];
                if (col.bulbs.Count == 0) continue;

                if (now < col.nextStepTime)
                {
                    continue;
                }

                if (col.activeIndex < col.bulbs.Count - 1)
                {
                    col.activeIndex++;
                    SetColumnActive(col, col.activeIndex, dripColor);
                    col.nextStepTime = now + Mathf.Max(0.01f, dripStepSeconds);
                }
                else
                {
                    SetColumnAll(col, Color.black);
                    col.activeIndex = -1;
                    col.nextStepTime = now + Mathf.Max(0f, pauseAfterColumn);
                }
            }
        }

        private void ResetSequence(float now)
        {
            float spacing = Mathf.Max(0f, batchSpacingSeconds);
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                Column col = _columns[ci];
                col.activeIndex = -1;
                col.nextStepTime = now + col.batch * spacing;
                SetColumnAll(col, Color.black);
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
                Axis axis = ResolveAxis(colT, col.bulbs);
                col.bulbs.Sort((a, b) =>
                {
                    float av = GetColumnAxis(colT, a.transform, axis);
                    float bv = GetColumnAxis(colT, b.transform, axis);
                    // Descending (top first)
                    return -av.CompareTo(bv);
                });

                // Init MPBs list
                for (int k = 0; k < col.bulbs.Count; k++) col.mpb.Add(new MaterialPropertyBlock());

                _columns.Add(col);
            }
        }

        private Axis ResolveAxis(Transform columnRoot, List<Renderer> bulbs)
        {
            if (!autoDetectVerticalAxis)
                return verticalAxis == Axis.Auto ? Axis.Z : verticalAxis;

            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;

            for (int i = 0; i < bulbs.Count; i++)
            {
                Vector3 lp = columnRoot.InverseTransformPoint(bulbs[i].transform.position);
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

        private float GetColumnAxis(Transform columnRoot, Transform bulb, Axis axis)
        {
            Vector3 localPosition = columnRoot.InverseTransformPoint(bulb.position);
            switch (axis)
            {
                case Axis.Y: return localPosition.y;
                case Axis.Z: return localPosition.z;
                default: return localPosition.z;
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