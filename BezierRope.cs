// Assets/Moonforged Christmas Decorations/Scripts/BezierRope.cs
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Draws a smooth rope between A and B using a cubic Bezier (with optional wind/jiggle for cloth-like motion).
    [RequireComponent(typeof(LineRenderer))]
    public class BezierRope : MonoBehaviour
    {
        public Transform pointA;
        public Transform pointB;

        [Header("Look")]
        public int segments = 24;
        public float width = 0.015f;
        public float sag = 0.25f;   // meters to pull the curve down in the middle
        public Material material;   // optional

        [Header("Cloth-like motion")]
        public float windStrength = 0.2f;    // pushes curve sideways (world-space)
        public float jiggleAmplitude = 0.02f; // small local oscillation
        public float jiggleSpeed = 1.0f;      // Hz-ish

        private LineRenderer _lr;
        private float _seed; // per-rope phase offset

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _lr.useWorldSpace = true;
            _lr.startWidth = width;
            _lr.endWidth = width;
            _lr.numCornerVertices = 3;
            _lr.numCapVertices = 3;
            _lr.textureMode = LineTextureMode.Stretch;
            _lr.material = material != null ? material : new Material(Shader.Find("Sprites/Default"));
            _lr.material.color = Color.black;


            // unique phase so multiple ropes don't jiggle in perfect sync
            _seed = Random.Range(0f, 1000f);
        }

        private void LateUpdate()
        {
            if (pointA == null || pointB == null) return;

            _lr.startWidth = width;
            _lr.endWidth = width;

            Vector3 a = pointA.position;
            Vector3 b = pointB.position;

            // Control points for a clean sag
            Vector3 mid = (a + b) * 0.5f;
            Vector3 c1 = Vector3.Lerp(a, mid, 0.66f) + Vector3.down * sag;
            Vector3 c2 = Vector3.Lerp(b, mid, 0.66f) + Vector3.down * sag;

            // World "wind" direction: use sled's right vector if available, else world X.
            Vector3 windDir = Vector3.right;
            if (pointA != null) windDir = pointA.right; // mild alignment with sled orientation

            float tNow = Time.time + _seed;
            float jiggle = Mathf.Sin(tNow * (2f * Mathf.PI) * Mathf.Max(0.01f, jiggleSpeed)) * jiggleAmplitude;

            // apply wind + jiggle to controls (soft clothy sway)
            c1 += windDir * windStrength * 0.5f + Vector3.up * jiggle * 0.5f;
            c2 += windDir * windStrength * 1.0f + Vector3.down * jiggle * 0.5f;

            int n = Mathf.Max(4, segments);
            _lr.positionCount = n;
            float inv = 1f / (n - 1);

            for (int i = 0; i < n; i++)
            {
                float t = i * inv;
                // Cubic Bezier
                Vector3 p =
                    Mathf.Pow(1 - t, 3) * a +
                    3f * Mathf.Pow(1 - t, 2) * t * c1 +
                    3f * (1 - t) * t * t * c2 +
                    t * t * t * b;

                // small along-curve jiggle (varies along length)
                float along = t * (1f - t); // bell curve
                Vector3 perp = Vector3.Cross((b - a).normalized, Vector3.up).normalized;
                p += perp * jiggle * along;

                _lr.SetPosition(i, p);
            }
        }
    }
}
