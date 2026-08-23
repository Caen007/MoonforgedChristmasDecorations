using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class MusicBoxInteract : MonoBehaviour, Interactable, Hoverable
    {
        private const string KeyRpcName = "MoonforgedChristmas_TurnMusicBoxKey";
        private const string PauseRpcName = "MoonforgedChristmas_PauseMusicBox";
        private const string ResumeRpcName = "MoonforgedChristmas_ResumeMusicBox";
        private const string InteractionObjectName = "MusicBoxInteraction";
        private const float AnimationSeconds = 21.333f;
        private const float KeyDegreesPerSecond = 360f / AnimationSeconds;
        private const float FullTurn = 360f;
        private const float ScrewRiseSeconds = 5f;
        private const float ScrewRiseTurns = 5f;
        private const float TopSpinSeconds = AnimationSeconds - ScrewRiseSeconds;
        private const float TopSpinTurns = 5f;
        private const float LidOpenSeconds = 0.35f;
        private const float LidOpenX = -135f;
        private const float ResetDelaySeconds = 5f;

        private static GameObject soundPrefab;

        private Piece piece;
        private ZNetView networkView;
        private Transform keyPivot;
        private Transform lidPivot;
        private Transform lidObject;
        private Transform movingObject;
        private Transform bottomPivot;
        private Transform topPivot;
        private Quaternion keyRestLocalRotation;
        private Quaternion lidRestLocalRotation;
        private Quaternion movingRestLocalRotation;
        private Vector3 movingRestLocalPosition;
        private Vector3 movingBottomLocalPosition;
        private Vector3 movingTopLocalPosition;
        private GameObject activeSoundInstance;
        private AudioSource[] activeSoundSources;
        private float degreesTravelled;
        private float animationElapsed;
        private float resetDelayElapsed;
        private float lastStartTime = -10f;
        private bool rpcRegistered;
        private bool interactionReady;
        private bool isMoving;
        private bool isPaused;
        private bool isWaitingToReset;

        public static void ConfigureSound(GameObject prefab)
        {
            soundPrefab = prefab;
        }

        private void Awake()
        {
            CacheComponents();
            CacheTransforms();
            RegisterRpc();
        }

        private void Start()
        {
            CacheComponents();
            CacheTransforms();
            RegisterRpc();
            TryCreateInteractionTarget();
        }

        private void Update()
        {
            if (!interactionReady)
            {
                CacheComponents();
                RegisterRpc();
                TryCreateInteractionTarget();
            }

            if (isWaitingToReset)
            {
                resetDelayElapsed += Time.deltaTime;

                if (resetDelayElapsed >= ResetDelaySeconds)
                    ResetAnimation();

                return;
            }

            if (!isMoving || isPaused || keyPivot == null)
                return;

            float deltaTime = Time.deltaTime;
            animationElapsed += deltaTime;
            degreesTravelled = Mathf.Min(FullTurn, degreesTravelled + KeyDegreesPerSecond * deltaTime);
            keyPivot.localRotation = keyRestLocalRotation *
                                     Quaternion.AngleAxis(-degreesTravelled, Vector3.right);

            UpdateLid();
            UpdateMovingObject();

            if (degreesTravelled >= FullTurn)
                FinishAnimation();
        }

        private void OnDisable()
        {
            ResetAnimation();
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || keyPivot == null || isWaitingToReset)
                return false;

            if (!isMoving)
            {
                StartAnimation();

                if (networkView != null && networkView.IsValid())
                    networkView.InvokeRPC(ZNetView.Everybody, KeyRpcName);
            }
            else if (isPaused)
            {
                ResumeAnimation();

                if (networkView != null && networkView.IsValid())
                    networkView.InvokeRPC(ZNetView.Everybody, ResumeRpcName);
            }
            else
            {
                PauseAnimation();

                if (networkView != null && networkView.IsValid())
                    networkView.InvokeRPC(ZNetView.Everybody, PauseRpcName);
            }

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverName()
        {
            if (piece == null)
                piece = GetComponent<Piece>();

            return piece != null && !string.IsNullOrWhiteSpace(piece.m_name)
                ? piece.m_name
                : "Christmas Music Box";
        }

        public string GetHoverText()
        {
            if (isWaitingToReset)
                return GetHoverName() + "\nMusic finished";

            if (isMoving && isPaused)
                return GetHoverName() + "\n[<color=yellow><b>E</b></color>] Resume music box";

            if (isMoving)
                return GetHoverName() + "\n[<color=yellow><b>E</b></color>] Pause music box";

            return GetHoverName() + "\n[<color=yellow><b>E</b></color>] Wind key";
        }

        private void CacheComponents()
        {
            if (piece == null)
                piece = GetComponent<Piece>();

            if (networkView == null)
                networkView = GetComponent<ZNetView>();
        }

        private void CacheTransforms()
        {
            if (keyPivot == null)
            {
                keyPivot = FindChildByName(transform, "Pivot_Key");
                if (keyPivot != null)
                    keyRestLocalRotation = keyPivot.localRotation;
            }

            if (lidPivot == null)
            {
                lidPivot = FindChildByName(transform, "Pivot_Lid");
                if (lidPivot != null)
                {
                    AttachLidToPivot();
                    lidRestLocalRotation = lidPivot.localRotation;
                }
            }

            if (movingObject == null)
            {
                movingObject = FindChildByName(transform, "Object_10.002");
                if (movingObject != null)
                {
                    movingRestLocalRotation = movingObject.localRotation;
                    movingRestLocalPosition = movingObject.localPosition;
                }
            }

            if (bottomPivot == null)
                bottomPivot = FindChildByName(transform, "Bottom_Pivot");

            if (topPivot == null)
                topPivot = FindChildByName(transform, "Top_Pivot");
        }

        private void RegisterRpc()
        {
            if (rpcRegistered || networkView == null)
                return;

            networkView.Register(KeyRpcName, RpcTurnKey);
            networkView.Register(PauseRpcName, RpcPauseAnimation);
            networkView.Register(ResumeRpcName, RpcResumeAnimation);
            rpcRegistered = true;
        }

        private void RpcTurnKey(long sender)
        {
            if (!isMoving && Time.time - lastStartTime > 0.1f)
                StartAnimation();
        }

        private void RpcPauseAnimation(long sender)
        {
            if (isMoving && !isPaused)
                PauseAnimation();
        }

        private void RpcResumeAnimation(long sender)
        {
            if (isMoving && isPaused)
                ResumeAnimation();
        }

        private void StartAnimation()
        {
            CacheTransforms();

            if (keyPivot == null || isMoving)
                return;

            keyPivot.localRotation = keyRestLocalRotation;
            degreesTravelled = 0f;
            animationElapsed = 0f;
            resetDelayElapsed = 0f;
            lastStartTime = Time.time;
            isPaused = false;
            isWaitingToReset = false;

            if (lidPivot != null)
                lidPivot.localRotation = lidRestLocalRotation;

            if (movingObject != null && bottomPivot != null && topPivot != null)
            {
                Transform parent = movingObject.parent;
                movingBottomLocalPosition = parent != null
                    ? parent.InverseTransformPoint(bottomPivot.position)
                    : bottomPivot.position;
                movingTopLocalPosition = parent != null
                    ? parent.InverseTransformPoint(topPivot.position)
                    : topPivot.position;
                movingObject.localPosition = movingBottomLocalPosition;
                movingObject.localRotation = movingRestLocalRotation;
            }

            isMoving = true;
            PlaySound();
        }

        private void FinishAnimation()
        {
            isMoving = false;
            isPaused = false;
            isWaitingToReset = true;
            resetDelayElapsed = 0f;
            StopAndDestroySound();
        }

        private void PauseAnimation()
        {
            if (!isMoving || isPaused)
                return;

            isPaused = true;

            if (activeSoundSources == null)
                return;

            for (int i = 0; i < activeSoundSources.Length; i++)
            {
                AudioSource source = activeSoundSources[i];
                if (source != null)
                    source.Pause();
            }
        }

        private void ResumeAnimation()
        {
            if (!isMoving || !isPaused)
                return;

            isPaused = false;

            if (activeSoundSources == null)
                return;

            for (int i = 0; i < activeSoundSources.Length; i++)
            {
                AudioSource source = activeSoundSources[i];
                if (source != null)
                    source.UnPause();
            }
        }

        private void ResetAnimation()
        {
            if (keyPivot != null)
                keyPivot.localRotation = keyRestLocalRotation;

            if (lidPivot != null)
                lidPivot.localRotation = lidRestLocalRotation;

            if (movingObject != null)
            {
                movingObject.localPosition = movingRestLocalPosition;
                movingObject.localRotation = movingRestLocalRotation;
            }

            degreesTravelled = 0f;
            animationElapsed = 0f;
            resetDelayElapsed = 0f;
            isMoving = false;
            isPaused = false;
            isWaitingToReset = false;
            StopAndDestroySound();
        }

        private void PlaySound()
        {
            if (soundPrefab == null)
                return;

            StopAndDestroySound();
            activeSoundInstance = Instantiate(soundPrefab, transform.position, transform.rotation);
            activeSoundSources = activeSoundInstance.GetComponentsInChildren<AudioSource>(true);

            for (int i = 0; i < activeSoundSources.Length; i++)
            {
                AudioSource source = activeSoundSources[i];
                if (source == null)
                    continue;

                if (!source.isPlaying)
                    source.Play();
            }
        }

        private void StopAndDestroySound()
        {
            if (activeSoundSources != null)
            {
                for (int i = 0; i < activeSoundSources.Length; i++)
                {
                    AudioSource source = activeSoundSources[i];
                    if (source != null)
                        source.Stop();
                }
            }

            if (activeSoundInstance != null)
                Destroy(activeSoundInstance);

            activeSoundSources = null;
            activeSoundInstance = null;
        }

        private void AttachLidToPivot()
        {
            if (lidPivot == null)
                return;

            lidObject = FindChildByName(transform, "Box_Top");
            if (lidObject == null || lidObject == lidPivot || lidObject.IsChildOf(lidPivot))
                return;

            if (lidPivot.IsChildOf(lidObject))
                lidPivot.SetParent(lidObject.parent, true);

            lidObject.SetParent(lidPivot, true);
        }

        private void UpdateLid()
        {
            if (lidPivot == null)
                return;

            float progress = Mathf.Clamp01(animationElapsed / LidOpenSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            Quaternion openRotation = lidRestLocalRotation *
                                      Quaternion.AngleAxis(LidOpenX, Vector3.right);
            lidPivot.localRotation = Quaternion.Slerp(lidRestLocalRotation, openRotation, eased);
        }

        private void UpdateMovingObject()
        {
            if (movingObject == null || bottomPivot == null || topPivot == null)
                return;

            if (animationElapsed <= ScrewRiseSeconds)
            {
                float progress = Mathf.Clamp01(animationElapsed / ScrewRiseSeconds);
                movingObject.localPosition = Vector3.Lerp(
                    movingBottomLocalPosition,
                    movingTopLocalPosition,
                    progress);
                movingObject.localRotation = movingRestLocalRotation *
                                             Quaternion.AngleAxis(
                                                 -FullTurn * ScrewRiseTurns * progress,
                                                 Vector3.up);
                return;
            }

            float topProgress = Mathf.Clamp01(
                (animationElapsed - ScrewRiseSeconds) / TopSpinSeconds);
            movingObject.localPosition = movingTopLocalPosition;
            movingObject.localRotation = movingRestLocalRotation *
                                         Quaternion.AngleAxis(
                                             -FullTurn *
                                             (ScrewRiseTurns + TopSpinTurns * topProgress),
                                             Vector3.up);
        }

        private void TryCreateInteractionTarget()
        {
            if (networkView == null || !networkView.IsValid())
                return;

            Transform existing = transform.Find(InteractionObjectName);
            if (existing != null)
            {
                MusicBoxInteractionProxy existingProxy =
                    existing.GetComponent<MusicBoxInteractionProxy>() ??
                    existing.gameObject.AddComponent<MusicBoxInteractionProxy>();
                existingProxy.Configure(this);
                interactionReady = true;
                return;
            }

            GameObject interactionObject = new GameObject(InteractionObjectName);
            interactionObject.transform.SetParent(transform, false);

            int interactionLayer = LayerMask.NameToLayer("piece_nonsolid");
            if (interactionLayer < 0)
                interactionLayer = LayerMask.NameToLayer("piece");
            if (interactionLayer < 0)
                interactionLayer = gameObject.layer;
            interactionObject.layer = interactionLayer;

            BoxCollider interactionCollider = interactionObject.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = false;
            ApplyInteractionBounds(interactionCollider);

            MusicBoxInteractionProxy proxy =
                interactionObject.AddComponent<MusicBoxInteractionProxy>();
            proxy.Configure(this);
            interactionReady = true;
        }

        private void ApplyInteractionBounds(BoxCollider interactionCollider)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Vector3 minimum = Vector3.zero;
            Vector3 maximum = Vector3.zero;

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;

                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 worldCorner = center + Vector3.Scale(
                                extents,
                                new Vector3(x, y, z));
                            Vector3 localCorner = transform.InverseTransformPoint(worldCorner);

                            if (!hasBounds)
                            {
                                minimum = localCorner;
                                maximum = localCorner;
                                hasBounds = true;
                            }
                            else
                            {
                                minimum = Vector3.Min(minimum, localCorner);
                                maximum = Vector3.Max(maximum, localCorner);
                            }
                        }
                    }
                }
            }

            if (!hasBounds)
            {
                interactionCollider.center = new Vector3(0f, 0.5f, 0f);
                interactionCollider.size = new Vector3(2f, 2f, 2f);
                return;
            }

            interactionCollider.center = (minimum + maximum) * 0.5f;
            interactionCollider.size = (maximum - minimum) + new Vector3(0.2f, 0.2f, 0.2f);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == childName)
                    return children[i];
            }

            return null;
        }
    }

    public class MusicBoxInteractionProxy : MonoBehaviour, Interactable, Hoverable
    {
        private MusicBoxInteract target;

        public void Configure(MusicBoxInteract musicBox)
        {
            target = musicBox;
        }

        private void Awake()
        {
            if (target == null)
                target = GetComponentInParent<MusicBoxInteract>();
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            return target != null && target.Interact(user, hold, alt);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return target != null && target.UseItem(user, item);
        }

        public string GetHoverName()
        {
            return target != null ? target.GetHoverName() : "Christmas Music Box";
        }

        public string GetHoverText()
        {
            return target != null ? target.GetHoverText() : "";
        }
    }
}