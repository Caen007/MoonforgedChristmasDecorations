// Assets/Moonforged Christmas Decorations/Scripts/IcicleLampFlow.cs
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Named-grid animation, interaction, and connected-string controller for M_Icicle_Lamp.
    public class IcicleLampFlow : MonoBehaviour, Interactable, Hoverable
    {
        [Header("Target naming")]
        public string columnNamePrefix = "Icicle_Lamp_";
        public string bulbNamePrefix = "Light_";
        public string leftSnapName = "Snappoint_Left";
        public string rightSnapName = "Snappoint_Right";

        [Header("Look")]
        public Color dripColor = new Color(0.60f, 0.85f, 1.00f);
        [Min(0f)] public float emissionIntensity = 4.5f;
        public bool affectBaseColor = false;

        [Header("Timing")]
        [Min(0.01f)] public float raindropStepSeconds = 0.05f;
        [Min(0.01f)] public float diagonalLineStepSeconds = 0.10f;
        [Min(0.01f)] public float freezeFillStepSeconds = 0.12f;
        [Min(0.01f)] public float fallingStarStepSeconds = 0.06f;
        [Min(0.01f)] public float sparkleStepSeconds = 0.08f;
        [Min(0.01f)] public float drippingStepSeconds = 0.08f;
        [Min(0.01f)] public float cascadeStepSeconds = 0.08f;
        [Min(0.01f)] public float twinkleStepSeconds = 0.08f;
        [Min(0.01f)] public float multiModeStepSeconds = 0.08f;
        [Min(0f)] public float pauseBetweenAnimations = 0.60f;

        [Header("Random animations")]
        [Range(1, 16)] public int randomDropCount = 8;
        [Range(1, 3)] public int fallingStarCount = 3;
        [Range(0.01f, 1f)] public float sparkleChance = 0.12f;
        [Min(1)] public int sparkleSteps = 24;
        [Range(1, 5)] public int diagonalLineCount = 3;
        [Min(8)] public int twinkleBlastSteps = 48;
        [Min(8)] public int multiModeStepsPerPattern = 24;

        [Header("Connected lamps")]
        [Min(0.05f)] public float connectionDistance = 0.75f;
        [Min(0.05f)] public float connectionRefreshSeconds = 0.25f;

        [Header("Advanced")]
        public bool includeInactive = true;

        [Header("Performance")]
        [Min(0.01f)] public float minUpdateInterval = 0.01f;
        [Min(0f)] public float activeDistance = 60f;

        public enum AnimationSelection
        {
            AllAnimations,
            DiagonalLeftToRight,
            DiagonalRightToLeft,
            FreezeFill,
            RandomDrops,
            FallingStars,
            Sparkles,
            DrippingSnow,
            CascadeChase,
            TwinkleBlast,
            MultiModeFlashing
        }

        private enum AnimationPhase
        {
            DiagonalLineLeftToRight,
            DiagonalLineRightToLeft,
            FreezeFill,
            RandomDrops,
            FallingStars,
            SparkleFinale,
            DrippingSnow,
            CascadeChase,
            TwinkleBlast,
            MultiModeFlashing
        }

        private const int StarWidth = 5;
        private const int StarHeight = 5;
        private const string ModeZdoKey = "mf_icicle_animation_mode";
        private const string ModeVersionZdoKey = "mf_icicle_animation_version";

        private static readonly int[,] StarMask =
        {
            { 0, 0, 1, 0, 0 },
            { 1, 0, 1, 0, 1 },
            { 0, 1, 1, 1, 0 },
            { 1, 1, 1, 1, 1 },
            { 0, 1, 0, 1, 0 }
        };

        private static readonly List<IcicleLampFlow> Instances = new List<IcicleLampFlow>();

        private class Column
        {
            public List<Renderer> bulbs = new List<Renderer>();
            public List<MaterialPropertyBlock> mpb = new List<MaterialPropertyBlock>();
        }

        private class RandomDrop
        {
            public int columnIndex;
            public int startStep;
        }

        private class FallingStar
        {
            public int centerColumn;
            public int startDelay;
        }

        private class LinkedMember
        {
            public IcicleLampFlow flow;
            public bool reverseColumns;
        }

        private readonly List<Column> _columns = new List<Column>();
        private readonly List<Column> _animationColumns = new List<Column>();
        private readonly List<LinkedMember> _linkedMembers = new List<LinkedMember>();
        private readonly List<RandomDrop> _randomDrops = new List<RandomDrop>();
        private readonly List<FallingStar> _fallingStars = new List<FallingStar>();

        private ZNetView _nview;
        private Transform _leftSnap;
        private Transform _rightSnap;
        private float _gammaIntensity;
        private int _colorId, _emissId;
        private float _accum;
        private bool _isActive;
        private bool _isGroupLeader = true;
        private AnimationSelection _selectedMode = AnimationSelection.AllAnimations;
        private int _modeVersion;
        private AnimationPhase _phase;
        private int _step = -1;
        private int _maxBulbCount;
        private int _diagonalLineStepCount;
        private int _cascadeStepCount;
        private int _randomDropStepCount;
        private int _fallingStarStepCount;
        private int _groupSignature;
        private int _linkedLampCount = 1;
        private float _nextStepTime;
        private float _nextConnectionRefresh;

        void Awake()
        {
            _colorId = Shader.PropertyToID("_Color");
            _emissId = Shader.PropertyToID("_EmissionColor");
            _gammaIntensity = Mathf.LinearToGammaSpace(Mathf.Max(0f, emissionIntensity));
            CacheSnapPoints();
            BuildColumns();
            UseLocalAnimationGrid();
            InstallInteractionProxies();
            EnsureNetworkView();
        }

        void Start()
        {
            EnsureNetworkView();
            ReadNetworkSelection(true);
        }

        void OnEnable()
        {
            if (!Instances.Contains(this))
                Instances.Add(this);

            CacheSnapPoints();
            InstallInteractionProxies();
            PrepareMPBs();
            SetLocalAllOff();
            _accum = 0f;
            _isActive = false;
            _nextConnectionRefresh = 0f;
        }

        void OnDisable()
        {
            Instances.Remove(this);
            SetLocalAllOff();
            _isActive = false;
        }

        void Update()
        {
            EnsureNetworkView();
            ReadNetworkSelection(false);

            float now = Time.time;
            if (now >= _nextConnectionRefresh)
            {
                _nextConnectionRefresh = now + Mathf.Max(0.05f, connectionRefreshSeconds);
                RefreshLinkedGroup(now);
            }

            if (!_isGroupLeader || _animationColumns.Count == 0)
                return;

            if (!IsPlayerNearLinkedGroup())
            {
                if (_isActive)
                {
                    SetAllOff();
                    _isActive = false;
                }
                return;
            }

            if (!_isActive)
            {
                _isActive = true;
                ResetSequence(now);
            }

            _accum += Time.deltaTime;
            if (_accum < minUpdateInterval) return;
            _accum = 0f;

            if (now < _nextStepTime) return;

            switch (_phase)
            {
                case AnimationPhase.DiagonalLineLeftToRight:
                    AdvanceDiagonalLine(now, false);
                    break;
                case AnimationPhase.DiagonalLineRightToLeft:
                    AdvanceDiagonalLine(now, true);
                    break;
                case AnimationPhase.FreezeFill:
                    AdvanceFreezeFill(now);
                    break;
                case AnimationPhase.RandomDrops:
                    AdvanceRandomDrops(now);
                    break;
                case AnimationPhase.FallingStars:
                    AdvanceFallingStars(now);
                    break;
                case AnimationPhase.SparkleFinale:
                    AdvanceSparkleFinale(now);
                    break;
                case AnimationPhase.DrippingSnow:
                    AdvanceDrippingSnow(now);
                    break;
                case AnimationPhase.CascadeChase:
                    AdvanceCascadeChase(now);
                    break;
                case AnimationPhase.TwinkleBlast:
                    AdvanceTwinkleBlast(now);
                    break;
                case AnimationPhase.MultiModeFlashing:
                    AdvanceMultiModeFlashing(now);
                    break;
            }
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;

            List<LinkedMember> group = BuildOrderedGroup(this);
            AnimationSelection currentSelection = ResolveGroupSelection(group, out int highestVersion);
            AnimationSelection nextSelection = GetNextSelection(currentSelection);
            int nextVersion = highestVersion + 1;

            for (int i = 0; i < group.Count; i++)
            {
                group[i].flow.ApplySelection(nextSelection, nextVersion, true);
            }

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverName()
        {
            return "Icicle Christmas Lights";
        }

        public string GetHoverText()
        {
            return
                "[<color=yellow><b>E</b></color>] Change animation\n" +
                "Mode: <color=orange>" + GetSelectionName(_selectedMode) + "</color>\n" +
                "Linked lamps: " + Mathf.Max(1, _linkedLampCount);
        }

        private void AdvanceDiagonalLine(float now, bool reverse)
        {
            _step++;

            if (_step < _diagonalLineStepCount)
            {
                SetDiagonalLines(_step, reverse);
                _nextStepTime = now + Mathf.Max(0.01f, diagonalLineStepSeconds);
                return;
            }

            CompletePhase(reverse ? AnimationPhase.FreezeFill : AnimationPhase.DiagonalLineRightToLeft, now);
        }

        private void AdvanceFreezeFill(float now)
        {
            _step++;

            if (_step < _maxBulbCount)
            {
                for (int ci = 0; ci < _animationColumns.Count; ci++)
                {
                    SetColumnThrough(_animationColumns[ci], _step, dripColor);
                }

                _nextStepTime = now + Mathf.Max(0.01f, freezeFillStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.RandomDrops, now);
        }

        private void AdvanceRandomDrops(float now)
        {
            _step++;

            if (_step == 0)
                PrepareRandomDrops();

            if (_step < _randomDropStepCount)
            {
                SetRandomDrops(_step);
                _nextStepTime = now + Mathf.Max(0.01f, raindropStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.FallingStars, now);
        }

        private void AdvanceFallingStars(float now)
        {
            _step++;

            if (_step == 0)
                PrepareFallingStars();

            if (_step < _fallingStarStepCount)
            {
                SetFallingStars(_step);
                _nextStepTime = now + Mathf.Max(0.01f, fallingStarStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.SparkleFinale, now);
        }

        private void AdvanceSparkleFinale(float now)
        {
            _step++;

            if (_step < Mathf.Max(1, sparkleSteps))
            {
                SetRandomSparkles();
                _nextStepTime = now + Mathf.Max(0.01f, sparkleStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.DrippingSnow, now);
        }

        private void AdvanceDrippingSnow(float now)
        {
            _step++;
            int stepCount = Mathf.Max(12, _maxBulbCount * 4 + 8);

            if (_step < stepCount)
            {
                SetDrippingSnow(_step);
                _nextStepTime = now + Mathf.Max(0.01f, drippingStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.CascadeChase, now);
        }

        private void AdvanceCascadeChase(float now)
        {
            _step++;

            if (_step < _cascadeStepCount)
            {
                SetCascadeChase(_step);
                _nextStepTime = now + Mathf.Max(0.01f, cascadeStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.TwinkleBlast, now);
        }

        private void AdvanceTwinkleBlast(float now)
        {
            _step++;

            if (_step < Mathf.Max(8, twinkleBlastSteps))
            {
                SetTwinkleBlast(_step);
                _nextStepTime = now + Mathf.Max(0.01f, twinkleStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.MultiModeFlashing, now);
        }

        private void AdvanceMultiModeFlashing(float now)
        {
            _step++;
            int patternSteps = Mathf.Max(8, multiModeStepsPerPattern);
            int stepCount = patternSteps * 8;

            if (_step < stepCount)
            {
                SetMultiModePattern(_step, patternSteps);
                _nextStepTime = now + Mathf.Max(0.01f, multiModeStepSeconds);
                return;
            }

            CompletePhase(AnimationPhase.DiagonalLineLeftToRight, now);
        }

        private void CompletePhase(AnimationPhase allAnimationsNextPhase, float now)
        {
            SetAllOff();
            _phase = _selectedMode == AnimationSelection.AllAnimations
                ? allAnimationsNextPhase
                : GetSelectedPhase(_selectedMode);
            _step = -1;
            _nextStepTime = now + Mathf.Max(0f, pauseBetweenAnimations);
        }

        private void ResetSequence(float now)
        {
            SetAllOff();
            _randomDrops.Clear();
            _fallingStars.Clear();
            _phase = GetSelectedPhase(_selectedMode);
            _step = -1;
            _nextStepTime = now;
        }

        private static AnimationSelection GetNextSelection(AnimationSelection current)
        {
            if (current >= AnimationSelection.MultiModeFlashing)
                return AnimationSelection.AllAnimations;

            return (AnimationSelection)((int)current + 1);
        }

        private static AnimationPhase GetSelectedPhase(AnimationSelection selection)
        {
            switch (selection)
            {
                case AnimationSelection.DiagonalRightToLeft:
                    return AnimationPhase.DiagonalLineRightToLeft;
                case AnimationSelection.FreezeFill:
                    return AnimationPhase.FreezeFill;
                case AnimationSelection.RandomDrops:
                    return AnimationPhase.RandomDrops;
                case AnimationSelection.FallingStars:
                    return AnimationPhase.FallingStars;
                case AnimationSelection.Sparkles:
                    return AnimationPhase.SparkleFinale;
                case AnimationSelection.DrippingSnow:
                    return AnimationPhase.DrippingSnow;
                case AnimationSelection.CascadeChase:
                    return AnimationPhase.CascadeChase;
                case AnimationSelection.TwinkleBlast:
                    return AnimationPhase.TwinkleBlast;
                case AnimationSelection.MultiModeFlashing:
                    return AnimationPhase.MultiModeFlashing;
                default:
                    return AnimationPhase.DiagonalLineLeftToRight;
            }
        }

        private static string GetSelectionName(AnimationSelection selection)
        {
            switch (selection)
            {
                case AnimationSelection.DiagonalLeftToRight:
                    return "Diagonal Line Left to Right";
                case AnimationSelection.DiagonalRightToLeft:
                    return "Diagonal Line Right to Left";
                case AnimationSelection.FreezeFill:
                    return "Freeze Fill";
                case AnimationSelection.RandomDrops:
                    return "Random Raindrops";
                case AnimationSelection.FallingStars:
                    return "Falling Stars";
                case AnimationSelection.Sparkles:
                    return "Random Sparkles";
                case AnimationSelection.DrippingSnow:
                    return "Dripping / Falling Snow";
                case AnimationSelection.CascadeChase:
                    return "Cascading / Chasing";
                case AnimationSelection.TwinkleBlast:
                    return "Twinkling / Blasting";
                case AnimationSelection.MultiModeFlashing:
                    return "8-Pattern Flashing";
                default:
                    return "All Animations";
            }
        }

        private void ApplySelection(AnimationSelection selection, int version, bool persist)
        {
            bool changed = _selectedMode != selection || _modeVersion != version;
            _selectedMode = selection;
            _modeVersion = version;

            if (persist)
                WriteNetworkSelection();

            if (changed)
                _isActive = false;
        }

        private void EnsureNetworkView()
        {
            if (_nview == null)
                _nview = GetComponent<ZNetView>();
        }

        private bool ReadNetworkSelection(bool force)
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
            if (storedMode < 0 || storedMode > (int)AnimationSelection.MultiModeFlashing)
                storedMode = 0;

            AnimationSelection selection = (AnimationSelection)storedMode;
            bool changed = _selectedMode != selection || _modeVersion != version;
            _selectedMode = selection;
            _modeVersion = version;

            if (changed)
                _isActive = false;

            return changed;
        }

        private void WriteNetworkSelection()
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

            zdo.Set(ModeZdoKey, (int)_selectedMode);
            zdo.Set(ModeVersionZdoKey, _modeVersion);
        }

        private void RefreshLinkedGroup(float now)
        {
            List<LinkedMember> orderedGroup = BuildOrderedGroup(this);
            _linkedLampCount = Mathf.Max(1, orderedGroup.Count);
            bool wasGroupLeader = _isGroupLeader;
            _isGroupLeader = orderedGroup.Count == 0 || orderedGroup[0].flow == this;

            if (!_isGroupLeader)
            {
                if (wasGroupLeader)
                    _isActive = false;
                return;
            }

            if (!wasGroupLeader)
                _isActive = false;

            AnimationSelection groupSelection = ResolveGroupSelection(orderedGroup, out int highestVersion);
            for (int i = 0; i < orderedGroup.Count; i++)
            {
                IcicleLampFlow member = orderedGroup[i].flow;
                if (member._selectedMode != groupSelection || member._modeVersion != highestVersion)
                    member.ApplySelection(groupSelection, highestVersion, true);
            }

            int signature = CalculateGroupSignature(orderedGroup);
            if (signature != _groupSignature)
            {
                SetAllOff();
                _linkedMembers.Clear();
                _linkedMembers.AddRange(orderedGroup);
                RebuildAnimationGrid();
                _groupSignature = signature;
                _isActive = false;
                _nextStepTime = now;
            }
        }

        private static AnimationSelection ResolveGroupSelection(List<LinkedMember> group, out int highestVersion)
        {
            highestVersion = 0;
            AnimationSelection selection = AnimationSelection.AllAnimations;

            for (int i = 0; i < group.Count; i++)
            {
                IcicleLampFlow flow = group[i].flow;
                flow.ReadNetworkSelection(false);

                if (i == 0 || flow._modeVersion > highestVersion)
                {
                    highestVersion = flow._modeVersion;
                    selection = flow._selectedMode;
                }
            }

            return selection;
        }

        private static int CalculateGroupSignature(List<LinkedMember> group)
        {
            unchecked
            {
                int signature = 17;
                for (int i = 0; i < group.Count; i++)
                {
                    signature = signature * 31 + group[i].flow.GetInstanceID();
                    signature = signature * 31 + (group[i].reverseColumns ? 1 : 0);
                }
                return signature;
            }
        }

        private void RebuildAnimationGrid()
        {
            _animationColumns.Clear();

            for (int mi = 0; mi < _linkedMembers.Count; mi++)
            {
                LinkedMember member = _linkedMembers[mi];
                if (member.reverseColumns)
                {
                    for (int ci = member.flow._columns.Count - 1; ci >= 0; ci--)
                        _animationColumns.Add(member.flow._columns[ci]);
                }
                else
                {
                    for (int ci = 0; ci < member.flow._columns.Count; ci++)
                        _animationColumns.Add(member.flow._columns[ci]);
                }
            }

            RecalculateAnimationGrid();
        }

        private void UseLocalAnimationGrid()
        {
            _animationColumns.Clear();
            for (int ci = 0; ci < _columns.Count; ci++)
                _animationColumns.Add(_columns[ci]);
            RecalculateAnimationGrid();
        }

        private void RecalculateAnimationGrid()
        {
            _maxBulbCount = 0;
            _cascadeStepCount = 0;

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                int bulbCount = _animationColumns[ci].bulbs.Count;
                if (bulbCount > _maxBulbCount)
                    _maxBulbCount = bulbCount;
                _cascadeStepCount += bulbCount;
            }

            int launchSpacing = Mathf.Max(1, Mathf.CeilToInt(_animationColumns.Count * 0.5f));
            int lineCount = Mathf.Max(1, diagonalLineCount);
            _diagonalLineStepCount = _animationColumns.Count + _maxBulbCount +
                                     launchSpacing * (lineCount - 1);
            _cascadeStepCount += _animationColumns.Count + 2;
        }

        private bool IsPlayerNearLinkedGroup()
        {
            if (activeDistance <= 0f)
                return true;

            for (int i = 0; i < _linkedMembers.Count; i++)
            {
                IcicleLampFlow member = _linkedMembers[i].flow;
                if (member != null && Player.IsPlayerInRange(member.transform.position, activeDistance))
                    return true;
            }

            return Player.IsPlayerInRange(transform.position, activeDistance);
        }

        private static List<LinkedMember> BuildOrderedGroup(IcicleLampFlow origin)
        {
            List<IcicleLampFlow> connected = GatherConnectedFlows(origin);
            connected.Sort(CompareStable);

            List<LinkedMember> ordered = new List<LinkedMember>();
            if (connected.Count == 0)
                return ordered;

            IcicleLampFlow start = connected[0];
            for (int i = 0; i < connected.Count; i++)
            {
                if (GetConnectionDegree(connected[i], connected) <= 1)
                {
                    start = connected[i];
                    break;
                }
            }

            bool hasLeft = GetNeighborAtEndpoint(start, false, connected, null, out bool ignoredLeftSide) != null;
            bool hasRight = GetNeighborAtEndpoint(start, true, connected, null, out bool ignoredRightSide) != null;
            bool exitRight = hasRight || !hasLeft;

            HashSet<IcicleLampFlow> visited = new HashSet<IcicleLampFlow>();
            IcicleLampFlow current = start;
            IcicleLampFlow previous = null;

            while (current != null && !visited.Contains(current))
            {
                visited.Add(current);
                LinkedMember member = new LinkedMember();
                member.flow = current;
                member.reverseColumns = !exitRight;
                ordered.Add(member);

                IcicleLampFlow next = GetNeighborAtEndpoint(
                    current,
                    exitRight,
                    connected,
                    previous,
                    out bool nextEntryIsRight);

                if (next == null || visited.Contains(next))
                    break;

                previous = current;
                current = next;
                exitRight = !nextEntryIsRight;
            }

            for (int i = 0; i < connected.Count; i++)
            {
                if (visited.Contains(connected[i]))
                    continue;

                LinkedMember member = new LinkedMember();
                member.flow = connected[i];
                member.reverseColumns = false;
                ordered.Add(member);
            }

            return ordered;
        }

        private static List<IcicleLampFlow> GatherConnectedFlows(IcicleLampFlow origin)
        {
            List<IcicleLampFlow> result = new List<IcicleLampFlow>();
            if (origin == null)
                return result;

            result.Add(origin);
            if (!origin.IsSceneInstance())
                return result;

            Queue<IcicleLampFlow> queue = new Queue<IcicleLampFlow>();
            HashSet<IcicleLampFlow> visited = new HashSet<IcicleLampFlow>();
            queue.Enqueue(origin);
            visited.Add(origin);

            while (queue.Count > 0)
            {
                IcicleLampFlow current = queue.Dequeue();
                for (int i = 0; i < Instances.Count; i++)
                {
                    IcicleLampFlow candidate = Instances[i];
                    if (candidate == null || candidate == current || visited.Contains(candidate))
                        continue;
                    if (!candidate.isActiveAndEnabled || !candidate.IsSceneInstance())
                        continue;
                    if (!AreConnected(current, candidate))
                        continue;

                    visited.Add(candidate);
                    result.Add(candidate);
                    queue.Enqueue(candidate);
                }
            }

            return result;
        }

        private static bool AreConnected(IcicleLampFlow a, IcicleLampFlow b)
        {
            a.CacheSnapPoints();
            b.CacheSnapPoints();
            float distance = Mathf.Max(a.connectionDistance, b.connectionDistance);
            float distanceSquared = distance * distance;
            Transform[] aEndpoints = { a._leftSnap, a._rightSnap };
            Transform[] bEndpoints = { b._leftSnap, b._rightSnap };

            for (int aIndex = 0; aIndex < aEndpoints.Length; aIndex++)
            {
                if (aEndpoints[aIndex] == null) continue;

                for (int bi = 0; bi < bEndpoints.Length; bi++)
                {
                    if (bEndpoints[bi] == null) continue;
                    if ((aEndpoints[aIndex].position - bEndpoints[bi].position).sqrMagnitude <= distanceSquared)
                        return true;
                }
            }

            return false;
        }

        private static int GetConnectionDegree(IcicleLampFlow flow, List<IcicleLampFlow> group)
        {
            HashSet<IcicleLampFlow> neighbors = new HashSet<IcicleLampFlow>();

            IcicleLampFlow left = GetNeighborAtEndpoint(flow, false, group, null, out bool ignoredLeftSide);
            if (left != null) neighbors.Add(left);

            IcicleLampFlow right = GetNeighborAtEndpoint(flow, true, group, null, out bool ignoredRightSide);
            if (right != null) neighbors.Add(right);

            return neighbors.Count;
        }

        private static IcicleLampFlow GetNeighborAtEndpoint(
            IcicleLampFlow flow,
            bool useRightEndpoint,
            List<IcicleLampFlow> group,
            IcicleLampFlow excluded,
            out bool neighborEndpointIsRight)
        {
            neighborEndpointIsRight = false;
            flow.CacheSnapPoints();
            Transform source = useRightEndpoint ? flow._rightSnap : flow._leftSnap;
            if (source == null)
                return null;

            IcicleLampFlow nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < group.Count; i++)
            {
                IcicleLampFlow candidate = group[i];
                if (candidate == null || candidate == flow || candidate == excluded)
                    continue;

                candidate.CacheSnapPoints();
                Transform[] endpoints = { candidate._leftSnap, candidate._rightSnap };
                for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
                {
                    Transform endpoint = endpoints[endpointIndex];
                    if (endpoint == null) continue;

                    float maximumDistance = Mathf.Max(flow.connectionDistance, candidate.connectionDistance);
                    float distanceSquared = (source.position - endpoint.position).sqrMagnitude;
                    if (distanceSquared > maximumDistance * maximumDistance ||
                        distanceSquared >= nearestDistanceSquared)
                        continue;

                    nearest = candidate;
                    nearestDistanceSquared = distanceSquared;
                    neighborEndpointIsRight = endpointIndex == 1;
                }
            }

            return nearest;
        }

        private static int CompareStable(IcicleLampFlow a, IcicleLampFlow b)
        {
            Vector3 ap = a.transform.position;
            Vector3 bp = b.transform.position;

            int result = ap.x.CompareTo(bp.x);
            if (result != 0) return result;
            result = ap.z.CompareTo(bp.z);
            if (result != 0) return result;
            result = ap.y.CompareTo(bp.y);
            if (result != 0) return result;
            result = string.CompareOrdinal(a.gameObject.name, b.gameObject.name);
            if (result != 0) return result;
            return a.GetInstanceID().CompareTo(b.GetInstanceID());
        }

        private bool IsSceneInstance()
        {
            return gameObject != null && gameObject.scene.IsValid();
        }

        private void CacheSnapPoints()
        {
            if (_leftSnap == null)
                _leftSnap = FindChildByName(transform, leftSnapName);
            if (_rightSnap == null)
                _rightSnap = FindChildByName(transform, rightSnapName);

            if (_leftSnap == null)
                _leftSnap = FindSnapPointBySide(transform, "left");
            if (_rightSnap == null)
                _rightSnap = FindSnapPointBySide(transform, "right");
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null &&
                    string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        private static Transform FindSnapPointBySide(Transform root, string side)
        {
            if (root == null || string.IsNullOrEmpty(side))
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == null) continue;
                string lowerName = child.name.ToLowerInvariant();
                if (lowerName.Contains("snap") && lowerName.Contains(side))
                    return child;
            }

            return null;
        }

        private void InstallInteractionProxies()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.gameObject == gameObject)
                    continue;

                IcicleLampInteractionProxy proxy =
                    collider.GetComponent<IcicleLampInteractionProxy>() ??
                    collider.gameObject.AddComponent<IcicleLampInteractionProxy>();
                proxy.Configure(this);
            }
        }

        private void PrepareRandomDrops()
        {
            _randomDrops.Clear();
            _randomDropStepCount = 0;

            List<int> availableColumns = new List<int>();
            for (int ci = 0; ci < _animationColumns.Count; ci++)
                availableColumns.Add(ci);

            int count = Mathf.Min(Mathf.Max(1, randomDropCount), availableColumns.Count);
            for (int i = 0; i < count; i++)
            {
                int availableIndex = Random.Range(0, availableColumns.Count);
                int columnIndex = availableColumns[availableIndex];
                availableColumns.RemoveAt(availableIndex);

                RandomDrop drop = new RandomDrop();
                drop.columnIndex = columnIndex;
                drop.startStep = Random.Range(0, 6);
                _randomDrops.Add(drop);

                int dropEnd = drop.startStep + _animationColumns[columnIndex].bulbs.Count;
                if (dropEnd > _randomDropStepCount)
                    _randomDropStepCount = dropEnd;
            }
        }

        private void PrepareFallingStars()
        {
            _fallingStars.Clear();
            _fallingStarStepCount = 0;

            List<int> availableCenters = new List<int>();
            int halfWidth = StarWidth / 2;
            for (int ci = halfWidth; ci < _animationColumns.Count - halfWidth; ci++)
                availableCenters.Add(ci);

            int maximumDelay = 0;
            int targetCount = Mathf.Min(Mathf.Max(1, fallingStarCount), availableCenters.Count);

            while (_fallingStars.Count < targetCount && availableCenters.Count > 0)
            {
                int availableIndex = Random.Range(0, availableCenters.Count);
                FallingStar star = new FallingStar();
                star.centerColumn = availableCenters[availableIndex];
                star.startDelay = _fallingStars.Count * 3 + Random.Range(0, 3);
                _fallingStars.Add(star);

                for (int ci = availableCenters.Count - 1; ci >= 0; ci--)
                {
                    if (Mathf.Abs(availableCenters[ci] - star.centerColumn) < StarWidth)
                        availableCenters.RemoveAt(ci);
                }

                if (star.startDelay > maximumDelay)
                    maximumDelay = star.startDelay;
            }

            _fallingStarStepCount = _maxBulbCount + StarHeight - 1 + maximumDelay;
        }

        private void SetRandomDrops(int step)
        {
            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                int activeIndex = -1;

                for (int di = 0; di < _randomDrops.Count; di++)
                {
                    RandomDrop drop = _randomDrops[di];
                    if (drop.columnIndex == ci)
                    {
                        activeIndex = step - drop.startStep;
                        break;
                    }
                }

                if (activeIndex >= 0 && activeIndex < col.bulbs.Count)
                    SetColumnActive(col, activeIndex, dripColor);
                else
                    SetColumnAll(col, Color.black);
            }
        }

        private void SetFallingStars(int step)
        {
            int halfWidth = StarWidth / 2;

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    bool isLit = false;

                    for (int si = 0; si < _fallingStars.Count && !isLit; si++)
                    {
                        FallingStar star = _fallingStars[si];
                        int maskColumn = ci - (star.centerColumn - halfWidth);
                        int starTop = step - (StarHeight - 1) - star.startDelay;
                        int maskRow = bi - starTop;

                        if (maskColumn >= 0 && maskColumn < StarWidth &&
                            maskRow >= 0 && maskRow < StarHeight &&
                            StarMask[maskRow, maskColumn] != 0)
                        {
                            isLit = true;
                        }
                    }

                    SetBulb(col, bi, isLit ? dripColor : Color.black);
                }
            }
        }

        private void SetRandomSparkles()
        {
            float chance = Mathf.Clamp01(sparkleChance);
            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    SetBulb(col, bi, Random.value < chance ? dripColor : Color.black);
                }
            }
        }

        private void SetDiagonalLines(int step, bool reverse)
        {
            int width = _animationColumns.Count;
            int launchSpacing = Mathf.Max(1, Mathf.CeilToInt(width * 0.5f));
            int lineCount = Mathf.Max(1, diagonalLineCount);

            for (int ci = 0; ci < width; ci++)
            {
                Column col = _animationColumns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    bool isLit = false;

                    for (int li = 0; li < lineCount && !isLit; li++)
                    {
                        int lineHead = step - li * launchSpacing;
                        int targetColumn = lineHead - bi;
                        if (reverse)
                            targetColumn = width - 1 - targetColumn;

                        isLit = ci == targetColumn;
                    }

                    SetBulb(col, bi, isLit ? dripColor : Color.black);
                }
            }
        }

        private void SetDrippingSnow(int step)
        {
            int cycleLength = Mathf.Max(6, _maxBulbCount + 5);

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                int head = (step + ci * 3) % cycleLength - 2;

                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    int trailDistance = head - bi;
                    float strength = trailDistance == 0
                        ? 1f
                        : trailDistance == 1 ? 0.55f : trailDistance == 2 ? 0.20f : 0f;
                    SetBulb(col, bi, dripColor * strength);
                }
            }
        }

        private void SetCascadeChase(int step)
        {
            int horizontalSteps = _animationColumns.Count + 2;
            if (step < horizontalSteps)
            {
                for (int ci = 0; ci < _animationColumns.Count; ci++)
                {
                    Column col = _animationColumns[ci];
                    int trailDistance = step - ci;
                    float strength = trailDistance == 0 ? 1f : trailDistance == 1 ? 0.45f : 0f;

                    for (int bi = 0; bi < col.bulbs.Count; bi++)
                        SetBulb(col, bi, bi == 0 ? dripColor * strength : Color.black);
                }
                return;
            }

            int remainingStep = step - horizontalSteps;
            int activeColumn = -1;
            int activeBulb = -1;

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                int bulbCount = _animationColumns[ci].bulbs.Count;
                if (remainingStep < bulbCount)
                {
                    activeColumn = ci;
                    activeBulb = remainingStep;
                    break;
                }
                remainingStep -= bulbCount;
            }

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    float strength = ci == activeColumn && bi == activeBulb
                        ? 1f
                        : ci == activeColumn && bi == activeBulb - 1 ? 0.45f : 0f;
                    SetBulb(col, bi, dripColor * strength);
                }
            }
        }

        private void SetTwinkleBlast(int step)
        {
            bool blasting = (step / 12) % 2 == 1;

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    float strength;
                    if (blasting)
                    {
                        int blastPattern = Mathf.Abs(step * 11 + ci * 7 + bi * 13) % 23;
                        strength = blastPattern == 0 ? 1.35f : 0f;
                    }
                    else
                    {
                        float phase = step * 0.16f + ci * 1.37f + bi * 0.73f;
                        float wave = 0.5f + 0.5f * Mathf.Sin(phase);
                        strength = 0.05f + 0.95f * wave * wave * wave;
                    }

                    SetBulb(col, bi, dripColor * strength);
                }
            }
        }

        private void SetMultiModePattern(int step, int patternSteps)
        {
            int pattern = Mathf.Clamp(step / patternSteps, 0, 7);
            int localStep = step % patternSteps;

            switch (pattern)
            {
                case 0:
                    SetEveryBulb(dripColor);
                    break;
                case 1:
                    {
                        float glowPhase = patternSteps <= 1 ? 0f : localStep / (float)(patternSteps - 1);
                        float glowStrength = 0.10f + 0.90f * (0.5f - 0.5f * Mathf.Cos(glowPhase * Mathf.PI * 2f));
                        SetEveryBulb(dripColor * glowStrength);
                        break;
                    }
                case 2:
                    SetSequentialWave(localStep);
                    break;
                case 3:
                    SetHorizontalMainLineChase(localStep);
                    break;
                case 4:
                    SetAlternatingStrands(localStep, patternSteps);
                    break;
                case 5:
                    SetEveryBulb(localStep % 2 == 0 ? dripColor : Color.black);
                    break;
                case 6:
                    SetTwinkleBlast(localStep);
                    break;
                default:
                    SetDrippingSnow(localStep);
                    break;
            }
        }

        private void SetSequentialWave(int step)
        {
            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(step * 0.55f - ci * 0.85f);
                SetColumnAll(_animationColumns[ci], dripColor * (0.10f + 0.90f * wave));
            }
        }

        private void SetHorizontalMainLineChase(int step)
        {
            int cycleLength = Mathf.Max(1, _animationColumns.Count + 2);
            int head = step % cycleLength;

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                Column col = _animationColumns[ci];
                int trailDistance = head - ci;
                float strength = trailDistance == 0
                    ? 1f
                    : trailDistance == 1 ? 0.55f : trailDistance == 2 ? 0.20f : 0f;

                for (int bi = 0; bi < col.bulbs.Count; bi++)
                    SetBulb(col, bi, bi == 0 ? dripColor * strength : Color.black);
            }
        }

        private void SetAlternatingStrands(int step, int patternSteps)
        {
            int switchSteps = Mathf.Max(1, patternSteps / 6);
            bool evenColumnsOn = (step / switchSteps) % 2 == 0;

            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                bool isOn = ci % 2 == 0 ? evenColumnsOn : !evenColumnsOn;
                SetColumnAll(_animationColumns[ci], isOn ? dripColor : Color.black);
            }
        }

        private void SetEveryBulb(Color color)
        {
            for (int ci = 0; ci < _animationColumns.Count; ci++)
                SetColumnAll(_animationColumns[ci], color);
        }

        private void BuildColumns()
        {
            _columns.Clear();

            List<Transform> columnParents = new List<Transform>();
            foreach (Transform child in GetComponentsInChildren<Transform>(includeInactive))
            {
                if (!child) continue;
                if (!string.IsNullOrEmpty(columnNamePrefix) && !child.name.StartsWith(columnNamePrefix)) continue;
                columnParents.Add(child);
            }

            columnParents.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < columnParents.Count; i++)
            {
                Transform columnTransform = columnParents[i];
                Column col = new Column();

                foreach (Renderer renderer in columnTransform.GetComponentsInChildren<Renderer>(includeInactive))
                {
                    if (!renderer) continue;
                    if (!string.IsNullOrEmpty(bulbNamePrefix) &&
                        !renderer.gameObject.name.StartsWith(bulbNamePrefix))
                        continue;
                    col.bulbs.Add(renderer);
                }

                if (col.bulbs.Count == 0)
                    continue;

                col.bulbs.Sort((a, b) => string.CompareOrdinal(a.gameObject.name, b.gameObject.name));

                for (int bi = 0; bi < col.bulbs.Count; bi++)
                    col.mpb.Add(new MaterialPropertyBlock());

                _columns.Add(col);
            }
        }

        private void PrepareMPBs()
        {
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                Column col = _columns[ci];
                for (int bi = 0; bi < col.bulbs.Count; bi++)
                {
                    Renderer renderer = col.bulbs[bi];
                    if (!renderer) continue;
                    Material material = renderer.sharedMaterial;
                    if (material != null) material.EnableKeyword("_EMISSION");
                    renderer.GetPropertyBlock(col.mpb[bi]);
                }
            }
        }

        private void SetColumnActive(Column col, int activeIndex, Color onColor)
        {
            for (int i = 0; i < col.bulbs.Count; i++)
            {
                SetBulb(col, i, i == activeIndex ? onColor : Color.black);
            }
        }

        private void SetColumnThrough(Column col, int activeIndex, Color onColor)
        {
            for (int i = 0; i < col.bulbs.Count; i++)
            {
                SetBulb(col, i, i <= activeIndex ? onColor : Color.black);
            }
        }

        private void SetColumnAll(Column col, Color color)
        {
            for (int i = 0; i < col.bulbs.Count; i++)
            {
                SetBulb(col, i, color);
            }
        }

        private void SetBulb(Column col, int bulbIndex, Color color)
        {
            Renderer renderer = col.bulbs[bulbIndex];
            if (!renderer) return;
            MaterialPropertyBlock mpb = col.mpb[bulbIndex];
            if (affectBaseColor) mpb.SetColor(_colorId, color);
            mpb.SetColor(_emissId, color * _gammaIntensity);
            renderer.SetPropertyBlock(mpb);
        }

        private void SetAllOff()
        {
            for (int ci = 0; ci < _animationColumns.Count; ci++)
            {
                SetColumnAll(_animationColumns[ci], Color.black);
            }
        }

        private void SetLocalAllOff()
        {
            for (int ci = 0; ci < _columns.Count; ci++)
            {
                SetColumnAll(_columns[ci], Color.black);
            }
        }
    }

    public class IcicleLampInteractionProxy : MonoBehaviour, Interactable, Hoverable
    {
        private IcicleLampFlow _owner;

        public void Configure(IcicleLampFlow owner)
        {
            _owner = owner;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            IcicleLampFlow owner = GetOwner();
            return owner != null && owner.Interact(user, hold, alt);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            IcicleLampFlow owner = GetOwner();
            return owner != null && owner.UseItem(user, item);
        }

        public string GetHoverName()
        {
            IcicleLampFlow owner = GetOwner();
            return owner != null ? owner.GetHoverName() : "Icicle Christmas Lights";
        }

        public string GetHoverText()
        {
            IcicleLampFlow owner = GetOwner();
            return owner != null ? owner.GetHoverText() : "";
        }

        private IcicleLampFlow GetOwner()
        {
            if (_owner == null)
                _owner = GetComponentInParent<IcicleLampFlow>();
            return _owner;
        }
    }
}
