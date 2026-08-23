// Assets/Moonforged Christmas Decorations/Scripts/SledReinsConnector.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Attaches Bezier ropes from sled hub (RopeAttach) to each deer ReinAnchor.
    /// No physics, no colliders. Supports up to 9 deer (8 + Rudolph lead).
    public class SledReinsConnector : MonoBehaviour
    {
        [Header("Hierarchy names")]
        public string sledAnchorRootName = "RopeAttach"; // under sled
        public string deerAnchorName = "ReinAnchor";     // under each deer

        [Header("Deer selection")]
        public int maxDeer = 9;          // 8 + Rudolph
        public float searchRadius = 20f; // simple distance filter
        public float refreshSeconds = 1.0f;

        [Header("Anchor layout (local to RopeAttach)")]
        public bool useSingleStart = true;     // if true, all ropes start at exact RopeAttach position
        public Vector3 baseLocal = new Vector3(0f, 0.0f, 0f);
        public float verticalLift = 0.00f;
        public float lateralSpacing = 0.45f;   // L/R (ignored when useSingleStart)
        public float forwardSpacing = 0.85f;   // rows (ignored when useSingleStart)
        public float leadExtraForward = 0.60f; // lead (ignored when useSingleStart)

        [Header("Rope look")]
        public float ropeWidth = 0.018f;
        public float ropeSag = 0.35f;
        public int ropeSegments = 28;

        [Header("Cloth-like sway")]
        public float windStrength = 0.25f;   // base wind push
        public float jiggleAmplitude = 0.03f; // small local flutter
        public float jiggleSpeed = 1.1f;      // flutter speed

        public Material ropeMaterial; // optional

        private Transform _sledRoot;                 // RopeAttach
        private readonly List<Transform> _sledAnchors = new List<Transform>(); // up to 9
        private readonly List<RopeEntry> _ropes = new List<RopeEntry>();
        private float _nextRefresh;

        private void Awake()
        {
            _sledRoot = FindChildRecursive(transform, sledAnchorRootName);
            if (_sledRoot == null)
            {
                GameObject go = new GameObject(sledAnchorRootName);
                _sledRoot = go.transform;
                _sledRoot.SetParent(transform, false);
                _sledRoot.localPosition = Vector3.zero;
                _sledRoot.localRotation = Quaternion.identity;
            }

            for (int i = 0; i < 9; i++)
            {
                Transform a = new GameObject("SledAnchor_" + i).transform;
                a.SetParent(_sledRoot, false);
                a.localPosition = ComputeAnchorLocal(i, 9);
                _sledAnchors.Add(a);
            }
        }

        private void OnEnable() { _nextRefresh = 0f; }
        private void OnDisable() { ClearRopes(); }

        private void Update()
        {
            if (Time.time >= _nextRefresh)
            {
                BuildOrUpdateLinks();
                _nextRefresh = Time.time + Mathf.Max(0.25f, refreshSeconds);
            }
        }

        private void BuildOrUpdateLinks()
        {
            List<Transform> deerRoots = FindDeerRoots();

            // In front, near->far, center bias
            Vector3 pos = transform.position;
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;

            deerRoots = deerRoots
                .Where(t => t && Vector3.Dot(fwd, (t.position - pos).normalized) > 0.05f)
                .OrderBy(t => Vector3.Dot(fwd, (t.position - pos)))
                .ThenBy(t => Mathf.Abs(Vector3.Dot(right, (t.position - pos))))
                .ToList();

            // Lead = Rudy if present
            Transform rudolph = deerRoots.FirstOrDefault(t =>
            {
                string n = t.name.ToLowerInvariant();
                return n.Contains("rudy") || n.Contains("rudolph");
            });
            if (rudolph != null)
            {
                deerRoots.Remove(rudolph);
                deerRoots.Add(rudolph);
            }

            int want = Mathf.Clamp(maxDeer, 0, 9);
            if (deerRoots.Count > want) deerRoots.RemoveRange(want, deerRoots.Count - want);

            bool same = _ropes.Count == deerRoots.Count;
            if (same)
            {
                for (int i = 0; i < _ropes.Count; i++)
                {
                    RopeEntry r = _ropes[i];
                    Transform wantDeer = deerRoots[i];
                    if (r == null || r.deerRoot == null || wantDeer == null ||
                        r.deerRoot.GetInstanceID() != wantDeer.GetInstanceID())
                    { same = false; break; }
                }
            }

            if (!same)
            {
                ClearRopes();
                for (int i = 0; i < deerRoots.Count; i++)
                {
                    _sledAnchors[i].localPosition = ComputeAnchorLocal(i, deerRoots.Count);
                    Transform deerAnchor = ResolveDeerAnchor(deerRoots[i]);

                    GameObject ropeGO = new GameObject("Rope_" + i);
                    ropeGO.transform.SetParent(transform, false);

                    BezierRope rope = ropeGO.AddComponent<BezierRope>();
                    rope.pointA = _sledAnchors[i];
                    rope.pointB = deerAnchor;
                    rope.width = ropeWidth;
                    rope.sag = ropeSag;
                    rope.segments = ropeSegments;
                    rope.material = ropeMaterial;

                    // cloth-like motion inputs
                    rope.windStrength = windStrength;
                    rope.jiggleAmplitude = jiggleAmplitude;
                    rope.jiggleSpeed = jiggleSpeed;

                    RopeEntry entry = new RopeEntry();
                    entry.deerRoot = deerRoots[i];
                    entry.deerAnchor = deerAnchor;
                    entry.sledAnchor = _sledAnchors[i];
                    entry.rope = rope;

                    _ropes.Add(entry);
                }
            }
            else
            {
                // Update local anchors and retarget changed deer anchors
                for (int i = 0; i < _ropes.Count; i++)
                {
                    _sledAnchors[i].localPosition = ComputeAnchorLocal(i, _ropes.Count);
                    Transform newAnchor = ResolveDeerAnchor(_ropes[i].deerRoot);
                    if (newAnchor != _ropes[i].deerAnchor)
                    {
                        _ropes[i].deerAnchor = newAnchor;
                        _ropes[i].rope.pointB = newAnchor;
                    }
                }
            }
        }

        private List<Transform> FindDeerRoots()
        {
            var list = new List<Transform>();
            Vector3 myPos = transform.position;

            // Use the DeerMarker registry for nearby deer
            foreach (var marker in DeerMarker.All)
            {
                if (!marker) continue;
                Transform t = marker.transform;
                if (Vector3.Distance(t.position, myPos) <= searchRadius)
                    list.Add(t);
            }

            return list;
        }

        private Transform ResolveDeerAnchor(Transform deerRoot)
        {
            if (deerRoot == null) return null;
            Transform a = FindChildRecursive(deerRoot, deerAnchorName);
            if (a != null) return a;

            Transform h = FindChildContains(deerRoot, "Head");
            if (h == null) h = FindChildContains(deerRoot, "Neck");
            if (h != null) return h;

            return deerRoot;
        }

        private Transform FindChildRecursive(Transform root, string exact)
        {
            if (root == null) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name == exact) return c;
                Transform f = FindChildRecursive(c, exact);
                if (f != null) return f;
            }
            return null;
        }

        private Transform FindChildContains(Transform root, string contains)
        {
            if (root == null) return null;
            string low = contains.ToLowerInvariant();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name.ToLowerInvariant().Contains(low)) return c;
                Transform f = FindChildContains(c, contains);
                if (f != null) return f;
            }
            return null;
        }

        private Vector3 ComputeAnchorLocal(int index, int total)
        {
            if (useSingleStart)
            {
                // all ropes start at the exact RopeAttach position
                return Vector3.zero;
            }

            Vector3 p = baseLocal + Vector3.up * verticalLift;

            bool hasLead = total >= 9;
            bool isLead = hasLead && (index == total - 1);
            if (isLead)
            {
                int pairRows = 4;
                p += Vector3.forward * (pairRows * forwardSpacing + leadExtraForward);
                return p; // centered lead
            }

            int row = index / 2;               // 0..3
            bool rightSide = (index % 2) == 1; // R if odd
            float side = rightSide ? +1f : -1f;

            p += Vector3.forward * (row * forwardSpacing);
            p += Vector3.right * (side * lateralSpacing);
            return p;
        }

        private void ClearRopes()
        {
            for (int i = 0; i < _ropes.Count; i++)
            {
                RopeEntry r = _ropes[i];
                if (r != null && r.rope != null) Destroy(r.rope.gameObject);
            }
            _ropes.Clear();
        }

        private class RopeEntry
        {
            public Transform deerRoot;
            public Transform deerAnchor;
            public Transform sledAnchor;
            public BezierRope rope;
        }
    }
}
