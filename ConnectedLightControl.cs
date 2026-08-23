// Assets/Moonforged Christmas Decorations/Scripts/ConnectedLightControl.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Interaction, speed control, and same-prefab synchronization for animated Christmas lights.
    public class ConnectedLightControl : MonoBehaviour, Interactable, Hoverable
    {
        public string prefabIdentity = "";

        [Header("Connected lights")]
        [Min(0.05f)] public float connectionDistance = 0.35f;
        [Min(0.05f)] public float connectionRefreshSeconds = 0.25f;

        [Header("Rapid blink")]
        [Min(0.01f)] public float rapidBlinkSeconds = 0.03f;

        public enum LightMode
        {
            Normal,
            Fast5x,
            Fast10x,
            Vibrate,
            Off
        }

        private const string ModeZdoKey = "mf_connected_light_mode";
        private const string ModeVersionZdoKey = "mf_connected_light_version";

        private static readonly List<ConnectedLightControl> Instances = new List<ConnectedLightControl>();

        private readonly List<Transform> _snapPoints = new List<Transform>();
        private readonly List<Renderer> _bulbRenderers = new List<Renderer>();
        private readonly List<MaterialPropertyBlock> _savedBulbBlocks = new List<MaterialPropertyBlock>();
        private readonly List<MaterialPropertyBlock> _workingBulbBlocks = new List<MaterialPropertyBlock>();

        private ZNetView _nview;
        private ChristmasLightChaser _chaser;
        private ChildLightsCycler _cycler;
        private IcicleFlow _icicleFlow;
        private ChristmasLightsGlow _glow;

        private float _baseChaserStep;
        private float _baseCyclerStep;
        private float _baseFlowDripStep;
        private float _baseFlowPause;
        private float _baseFlowBatchSpacing;
        private float _baseFlowUpdateInterval;
        private float _baseGlowCycle;
        private bool _baseChaserEnabled;
        private bool _baseCyclerEnabled;
        private bool _baseFlowEnabled;
        private bool _baseGlowEnabled;
        private bool _baseTimingCaptured;

        private LightMode _mode = LightMode.Normal;
        private int _modeVersion;
        private LightMode _appliedMode = LightMode.Normal;
        private int _appliedVersion;
        private bool _hasAppliedMode;
        private int _groupSignature;
        private int _linkedLightCount = 1;
        private int _blinkPhase = -1;
        private float _nextConnectionRefresh;
        private int _emissionColorId;

        void Awake()
        {
            _emissionColorId = Shader.PropertyToID("_EmissionColor");
            EnsureNetworkView();
            CacheSnapPoints();
            CacheAnimationComponents();
        }

        void Start()
        {
            EnsureNetworkView();
            CacheAnimationComponents();
            CaptureBaseTiming();
            CacheBulbRenderers();
            ReadNetworkMode(true);
            ApplyMode(_mode, _modeVersion, false, true);
        }

        void OnEnable()
        {
            if (!Instances.Contains(this))
                Instances.Add(this);

            _nextConnectionRefresh = 0f;
            _blinkPhase = -1;
        }

        void OnDisable()
        {
            Instances.Remove(this);
        }

        void Update()
        {
            EnsureNetworkView();
            if (ReadNetworkMode(false))
                ApplyMode(_mode, _modeVersion, false, true);

            float now = Time.time;
            if (now >= _nextConnectionRefresh)
            {
                _nextConnectionRefresh = now + Mathf.Max(0.05f, connectionRefreshSeconds);
                RefreshLinkedGroup();
            }

            if (_mode == LightMode.Vibrate)
                UpdateRapidBlink(now);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;

            List<ConnectedLightControl> group = GatherConnectedControls(this);
            group.Sort(CompareStable);
            LightMode currentMode = ResolveGroupMode(group, out int highestVersion);
            LightMode nextMode = GetNextMode(currentMode);
            int nextVersion = highestVersion + 1;

            for (int i = 0; i < group.Count; i++)
                group[i].ApplyMode(nextMode, nextVersion, true, true);

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverName()
        {
            return string.IsNullOrEmpty(prefabIdentity) ? "Christmas Lights" : prefabIdentity;
        }

        public string GetHoverText()
        {
            return
                "[<color=yellow><b>E</b></color>] Change light speed\n" +
                "Mode: <color=orange>" + GetModeName(_mode) + "</color>\n" +
                "Linked lights: " + Mathf.Max(1, _linkedLightCount);
        }

        private static LightMode GetNextMode(LightMode current)
        {
            if (current >= LightMode.Off)
                return LightMode.Normal;

            return (LightMode)((int)current + 1);
        }

        private static string GetModeName(LightMode mode)
        {
            switch (mode)
            {
                case LightMode.Fast5x:
                    return "5x Speed";
                case LightMode.Fast10x:
                    return "10x Speed";
                case LightMode.Vibrate:
                    return "Rapid Blink";
                case LightMode.Off:
                    return "Off";
                default:
                    return "Normal";
            }
        }

        private void ApplyMode(LightMode mode, int version, bool persist, bool forceRestart)
        {
            bool changed = !_hasAppliedMode || _appliedMode != mode || _appliedVersion != version;
            LightMode previousMode = _hasAppliedMode ? _appliedMode : LightMode.Normal;
            _mode = mode;
            _modeVersion = version;
            _appliedMode = mode;
            _appliedVersion = version;
            _hasAppliedMode = true;

            if (persist)
                WriteNetworkMode();

            if (!changed && !forceRestart)
                return;

            CacheAnimationComponents();
            CaptureBaseTiming();
            CacheBulbRenderers();

            bool enteringOverride = mode == LightMode.Vibrate || mode == LightMode.Off;
            bool leavingOverride = previousMode == LightMode.Vibrate || previousMode == LightMode.Off;

            if (enteringOverride && !leavingOverride)
                CaptureCurrentBulbState();

            if (enteringOverride)
            {
                DisableAnimationComponents();
                _blinkPhase = -1;

                if (mode == LightMode.Off)
                    SetBulbsOff();
                else
                    UpdateRapidBlink(Time.time);

                return;
            }

            RestoreBulbState();

            float speedMultiplier = mode == LightMode.Fast5x
                ? 5f
                : mode == LightMode.Fast10x ? 10f : 1f;

            RestartAnimationComponents(speedMultiplier);
            _blinkPhase = -1;
        }

        private void UpdateRapidBlink(float now)
        {
            int phase = Mathf.FloorToInt(now / Mathf.Max(0.01f, rapidBlinkSeconds)) & 1;
            if (phase == _blinkPhase)
                return;

            _blinkPhase = phase;
            if (phase == 0)
                SetBulbsOff();
            else
                RestoreBulbState();
        }

        private void CacheAnimationComponents()
        {
            if (_chaser == null) _chaser = GetComponent<ChristmasLightChaser>();
            if (_cycler == null) _cycler = GetComponent<ChildLightsCycler>();
            if (_icicleFlow == null) _icicleFlow = GetComponent<IcicleFlow>();
            if (_glow == null) _glow = GetComponent<ChristmasLightsGlow>();
        }

        private void CaptureBaseTiming()
        {
            if (_baseTimingCaptured)
                return;

            if (_chaser != null)
            {
                _baseChaserStep = Mathf.Max(0.01f, _chaser.stepSeconds);
                _baseChaserEnabled = _chaser.enabled;
            }

            if (_cycler != null)
            {
                _baseCyclerStep = Mathf.Max(0.01f, _cycler.stepSeconds);
                _baseCyclerEnabled = _cycler.enabled;
            }

            if (_icicleFlow != null)
            {
                _baseFlowDripStep = Mathf.Max(0.01f, _icicleFlow.dripStepSeconds);
                _baseFlowPause = Mathf.Max(0f, _icicleFlow.pauseAfterColumn);
                _baseFlowBatchSpacing = Mathf.Max(0f, _icicleFlow.batchSpacingSeconds);
                _baseFlowUpdateInterval = Mathf.Max(0.01f, _icicleFlow.minUpdateInterval);
                _baseFlowEnabled = _icicleFlow.enabled;
            }

            if (_glow != null)
            {
                _baseGlowCycle = Mathf.Max(0.01f, _glow.cycleSeconds);
                _baseGlowEnabled = _glow.enabled;
            }

            _baseTimingCaptured = true;
        }

        private void RestartAnimationComponents(float speedMultiplier)
        {
            float speed = Mathf.Max(1f, speedMultiplier);

            if (_chaser != null)
            {
                _chaser.enabled = false;
                _chaser.stepSeconds = Mathf.Max(0.01f, _baseChaserStep / speed);
                _chaser.enabled = _baseChaserEnabled;
            }

            if (_cycler != null)
            {
                _cycler.enabled = false;
                _cycler.stepSeconds = Mathf.Max(0.01f, _baseCyclerStep / speed);
                _cycler.enabled = _baseCyclerEnabled;
            }

            if (_icicleFlow != null)
            {
                _icicleFlow.enabled = false;
                _icicleFlow.dripStepSeconds = Mathf.Max(0.01f, _baseFlowDripStep / speed);
                _icicleFlow.pauseAfterColumn = _baseFlowPause / speed;
                _icicleFlow.batchSpacingSeconds = _baseFlowBatchSpacing / speed;
                _icicleFlow.minUpdateInterval = Mathf.Max(0.01f, _baseFlowUpdateInterval / speed);
                _icicleFlow.enabled = _baseFlowEnabled;
            }

            if (_glow != null)
            {
                _glow.enabled = false;
                _glow.cycleSeconds = Mathf.Max(0.01f, _baseGlowCycle / speed);
                _glow.enabled = _baseGlowEnabled;
            }
        }

        private void DisableAnimationComponents()
        {
            if (_chaser != null) _chaser.enabled = false;
            if (_cycler != null) _cycler.enabled = false;
            if (_icicleFlow != null) _icicleFlow.enabled = false;
            if (_glow != null) _glow.enabled = false;
        }

        private void CacheBulbRenderers()
        {
            if (_bulbRenderers.Count > 0)
                return;

            HashSet<Renderer> unique = new HashSet<Renderer>();

            if (_chaser != null)
            {
                for (int i = 0; i < _chaser.targets.Count; i++)
                    AddBulbRenderer(_chaser.targets[i], unique);
            }

            if (_cycler != null)
            {
                for (int i = 0; i < _cycler.lightRendererNames.Length; i++)
                {
                    Transform target = FindChildByName(transform, _cycler.lightRendererNames[i]);
                    if (target != null)
                        AddBulbRenderer(target.GetComponent<Renderer>(), unique);
                }
            }

            if (_icicleFlow != null)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null && renderer.gameObject.name.StartsWith(_icicleFlow.bulbNamePrefix))
                        AddBulbRenderer(renderer, unique);
                }
            }

            if (_bulbRenderers.Count == 0)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null && renderer.gameObject.name.ToLowerInvariant().StartsWith("light"))
                        AddBulbRenderer(renderer, unique);
                }
            }

            CaptureCurrentBulbState();
        }

        private void AddBulbRenderer(Renderer renderer, HashSet<Renderer> unique)
        {
            if (renderer == null || !unique.Add(renderer))
                return;

            Material material = renderer.sharedMaterial;
            if (material != null)
                material.EnableKeyword("_EMISSION");

            _bulbRenderers.Add(renderer);
            _savedBulbBlocks.Add(new MaterialPropertyBlock());
            _workingBulbBlocks.Add(new MaterialPropertyBlock());
        }

        private void CaptureCurrentBulbState()
        {
            for (int i = 0; i < _bulbRenderers.Count; i++)
            {
                Renderer renderer = _bulbRenderers[i];
                if (renderer != null)
                    renderer.GetPropertyBlock(_savedBulbBlocks[i]);
            }
        }

        private void RestoreBulbState()
        {
            for (int i = 0; i < _bulbRenderers.Count; i++)
            {
                Renderer renderer = _bulbRenderers[i];
                if (renderer != null)
                    renderer.SetPropertyBlock(_savedBulbBlocks[i]);
            }
        }

        private void SetBulbsOff()
        {
            for (int i = 0; i < _bulbRenderers.Count; i++)
            {
                Renderer renderer = _bulbRenderers[i];
                if (renderer == null) continue;

                MaterialPropertyBlock block = _workingBulbBlocks[i];
                renderer.GetPropertyBlock(block);
                block.SetColor(_emissionColorId, Color.black);
                renderer.SetPropertyBlock(block);
            }
        }

        private void RefreshLinkedGroup()
        {
            List<ConnectedLightControl> group = GatherConnectedControls(this);
            group.Sort(CompareStable);
            _linkedLightCount = Mathf.Max(1, group.Count);

            if (group.Count == 0 || group[0] != this)
                return;

            LightMode groupMode = ResolveGroupMode(group, out int highestVersion);
            int signature = CalculateGroupSignature(group);
            bool groupChanged = false;

            for (int i = 0; i < group.Count; i++)
            {
                ConnectedLightControl member = group[i];
                member._linkedLightCount = group.Count;
                if (member._groupSignature != signature)
                    groupChanged = true;
            }

            for (int i = 0; i < group.Count; i++)
            {
                ConnectedLightControl member = group[i];
                member._groupSignature = signature;
                bool stateChanged = member._mode != groupMode || member._modeVersion != highestVersion;
                bool appliedStateChanged = !member._hasAppliedMode ||
                                           member._appliedMode != groupMode ||
                                           member._appliedVersion != highestVersion;
                member.ApplyMode(
                    groupMode,
                    highestVersion,
                    stateChanged,
                    groupChanged || stateChanged || appliedStateChanged);
            }
        }

        private static LightMode ResolveGroupMode(List<ConnectedLightControl> group, out int highestVersion)
        {
            highestVersion = 0;
            LightMode mode = LightMode.Normal;

            for (int i = 0; i < group.Count; i++)
            {
                ConnectedLightControl control = group[i];
                control.ReadNetworkMode(false);
                if (i == 0 || control._modeVersion > highestVersion)
                {
                    highestVersion = control._modeVersion;
                    mode = control._mode;
                }
            }

            return mode;
        }

        private static List<ConnectedLightControl> GatherConnectedControls(ConnectedLightControl origin)
        {
            List<ConnectedLightControl> result = new List<ConnectedLightControl>();
            if (origin == null)
                return result;

            result.Add(origin);
            if (!origin.IsLiveInstance())
                return result;

            Queue<ConnectedLightControl> queue = new Queue<ConnectedLightControl>();
            HashSet<ConnectedLightControl> visited = new HashSet<ConnectedLightControl>();
            queue.Enqueue(origin);
            visited.Add(origin);

            while (queue.Count > 0)
            {
                ConnectedLightControl current = queue.Dequeue();
                for (int i = 0; i < Instances.Count; i++)
                {
                    ConnectedLightControl candidate = Instances[i];
                    if (candidate == null || candidate == current || visited.Contains(candidate))
                        continue;
                    if (!candidate.isActiveAndEnabled || !candidate.IsLiveInstance())
                        continue;
                    if (!SamePrefab(current, candidate) || !AreConnected(current, candidate))
                        continue;

                    visited.Add(candidate);
                    result.Add(candidate);
                    queue.Enqueue(candidate);
                }
            }

            return result;
        }

        private static bool SamePrefab(ConnectedLightControl a, ConnectedLightControl b)
        {
            return !string.IsNullOrEmpty(a.prefabIdentity) &&
                   string.Equals(a.prefabIdentity, b.prefabIdentity, System.StringComparison.Ordinal);
        }

        private static bool AreConnected(ConnectedLightControl a, ConnectedLightControl b)
        {
            a.CacheSnapPoints();
            b.CacheSnapPoints();
            if (a._snapPoints.Count == 0 || b._snapPoints.Count == 0)
                return false;

            float maximumDistance = Mathf.Max(a.connectionDistance, b.connectionDistance);
            float maximumDistanceSquared = maximumDistance * maximumDistance;

            for (int aIndex = 0; aIndex < a._snapPoints.Count; aIndex++)
            {
                for (int bi = 0; bi < b._snapPoints.Count; bi++)
                {
                    if ((a._snapPoints[aIndex].position - b._snapPoints[bi].position).sqrMagnitude <= maximumDistanceSquared)
                        return true;
                }
            }

            return false;
        }

        private void CacheSnapPoints()
        {
            if (_snapPoints.Count > 0)
                return;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child != transform && child.name.ToLowerInvariant().Contains("snap"))
                    _snapPoints.Add(child);
            }
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i];
            }

            return null;
        }

        private static int CalculateGroupSignature(List<ConnectedLightControl> group)
        {
            unchecked
            {
                int signature = 17;
                for (int i = 0; i < group.Count; i++)
                    signature = signature * 31 + group[i].GetInstanceID();
                return signature;
            }
        }

        private static int CompareStable(ConnectedLightControl a, ConnectedLightControl b)
        {
            Vector3 ap = a.transform.position;
            Vector3 bp = b.transform.position;

            int result = ap.x.CompareTo(bp.x);
            if (result != 0) return result;
            result = ap.z.CompareTo(bp.z);
            if (result != 0) return result;
            result = ap.y.CompareTo(bp.y);
            if (result != 0) return result;
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        }

        private bool IsLiveInstance()
        {
            EnsureNetworkView();
            return _nview != null && _nview.IsValid() && _nview.GetZDO() != null;
        }

        private void EnsureNetworkView()
        {
            if (_nview == null)
                _nview = GetComponent<ZNetView>();
        }

        private bool ReadNetworkMode(bool force)
        {
            EnsureNetworkView();
            if (_nview == null || !_nview.IsValid())
                return false;

            ZDO zdo = _nview.GetZDO();
            if (zdo == null)
                return false;

            int version = zdo.GetInt(ModeVersionZdoKey, 0);
            if (!force && version <= _modeVersion)
                return false;

            int storedMode = zdo.GetInt(ModeZdoKey, 0);
            if (storedMode < 0 || storedMode > (int)LightMode.Off)
                storedMode = 0;

            LightMode mode = (LightMode)storedMode;
            bool changed = _mode != mode || _modeVersion != version;
            _mode = mode;
            _modeVersion = version;
            return changed;
        }

        private void WriteNetworkMode()
        {
            EnsureNetworkView();
            if (_nview == null || !_nview.IsValid())
                return;

            if (!_nview.IsOwner())
            {
                MethodInfo claimOwnership = typeof(ZNetView).GetMethod(
                    "ClaimOwnership",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (claimOwnership != null)
                {
                    try
                    {
                        claimOwnership.Invoke(_nview, null);
                    }
                    catch
                    {
                    }
                }
            }

            ZDO zdo = _nview.GetZDO();
            if (zdo == null)
                return;

            zdo.Set(ModeZdoKey, (int)_mode);
            zdo.Set(ModeVersionZdoKey, _modeVersion);
        }
    }
}