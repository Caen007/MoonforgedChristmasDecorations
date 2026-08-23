using System;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    [DisallowMultipleComponent]
    public class TrainPivotRotator : MonoBehaviour, Hoverable, Interactable
    {
        private const string StartRpcName = "MoonforgedChristmas_StartTrain";
        private const string StopRpcName = "MoonforgedChristmas_StopTrain";
        private const float RunDurationSeconds = 31.378f;

        private static GameObject trainSoundPrefab;

        public float degreesPerSecond = 10f;

        private Piece piece;
        private ZNetView networkView;
        private Transform trainPivot;
        private GameObject activeSoundInstance;
        private AudioSource activeAudioSource;
        private float runElapsed;
        private bool rpcReady;
        private bool isMoving;

        public static void ConfigureTrainSound(GameObject soundPrefab)
        {
            trainSoundPrefab = soundPrefab;
        }

        public void ConfigurePivot(Transform pivot)
        {
            trainPivot = pivot;
        }

        private void Awake()
        {
            piece = GetComponent<Piece>();
            networkView = GetComponent<ZNetView>();
            ResolvePivot();
            TryRegisterRpcs();
        }

        private void Update()
        {
            if (piece == null)
            {
                piece = GetComponent<Piece>();
            }

            if (networkView == null)
            {
                networkView = GetComponent<ZNetView>();
            }

            TryRegisterRpcs();

            if (trainPivot == null)
            {
                ResolvePivot();
            }

            if (!isMoving || trainPivot == null)
            {
                return;
            }

            float remainingSeconds = Mathf.Max(0f, RunDurationSeconds - runElapsed);
            float movementSeconds = Mathf.Min(Time.deltaTime, remainingSeconds);
            trainPivot.Rotate(Vector3.up, degreesPerSecond * movementSeconds, Space.World);
            runElapsed += Time.deltaTime;

            if (runElapsed >= RunDurationSeconds)
            {
                StopTrain();
            }
        }

        private void TryRegisterRpcs()
        {
            if (rpcReady || networkView == null)
            {
                return;
            }

            networkView.Register(StartRpcName, RpcStartTrain);
            networkView.Register(StopRpcName, RpcStopTrain);
            rpcReady = true;
        }

        private void OnDisable()
        {
            StopTrain();
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || trainPivot == null)
            {
                return false;
            }

            if (isMoving)
            {
                StopTrain();

                if (networkView != null && networkView.IsValid())
                {
                    networkView.InvokeRPC(ZNetView.Everybody, StopRpcName);
                }
            }
            else
            {
                StartTrain();

                if (networkView != null && networkView.IsValid())
                {
                    networkView.InvokeRPC(ZNetView.Everybody, StartRpcName);
                }
            }

            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            return GetHoverName() + (isMoving
                ? "\n[<color=yellow><b>E</b></color>] Stop train"
                : "\n[<color=yellow><b>E</b></color>] Start train");
        }

        public string GetHoverName()
        {
            return piece != null && !string.IsNullOrWhiteSpace(piece.m_name) ? piece.m_name : "Christmas Train";
        }

        private void RpcStartTrain(long sender)
        {
            if (!isMoving)
            {
                StartTrain();
            }
        }

        private void RpcStopTrain(long sender)
        {
            if (isMoving)
            {
                StopTrain();
            }
        }

        private void StartTrain()
        {
            if (trainPivot == null || isMoving)
            {
                return;
            }

            runElapsed = 0f;
            isMoving = true;
            PlayTrainSound();
        }

        private void PlayTrainSound()
        {
            if (trainSoundPrefab == null || trainPivot == null)
            {
                return;
            }

            activeSoundInstance = Instantiate(trainSoundPrefab, trainPivot.position, Quaternion.identity);
            activeAudioSource = activeSoundInstance.GetComponent<AudioSource>();

            if (activeAudioSource == null)
            {
                activeAudioSource = activeSoundInstance.GetComponentInChildren<AudioSource>(true);
            }

            if (activeAudioSource != null)
            {
                activeAudioSource.Stop();
                activeAudioSource.playOnAwake = false;
                activeAudioSource.loop = false;
                activeAudioSource.Play();
            }

            Destroy(activeSoundInstance, RunDurationSeconds + 0.1f);
        }

        private void StopTrain()
        {
            if (activeAudioSource != null)
            {
                activeAudioSource.Stop();
            }

            if (activeSoundInstance != null)
            {
                Destroy(activeSoundInstance);
            }

            activeAudioSource = null;
            activeSoundInstance = null;
            runElapsed = 0f;
            isMoving = false;
        }

        private void ResolvePivot()
        {
            Transform center = FindDeepChild(transform, "TrackCenter");
            if (center == null)
            {
                return;
            }

            trainPivot = center.Find("pivot") ?? center.Find("OrbitPivot");
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform result = FindDeepChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}