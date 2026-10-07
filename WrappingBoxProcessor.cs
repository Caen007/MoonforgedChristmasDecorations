using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Moonforged.ChristmasDecorations
{
    public class WrappingBoxProcessor : MonoBehaviour
    {
        private Container container;
        private ZNetView nview;

        private bool wasInUseLastFrame = false;
        private bool wrapInProgress = false;

        private Vector3 restLocalPosition;
        private Quaternion restLocalRotation;
        private Coroutine idleShakeCoroutine;
        private Coroutine completionAnimationCoroutine;
        private float nextIdleShakeTime;

        private const float WRAP_DELAY = 3.8f;

        // Small attention-grabbing idle shake. Kept intentionally subtle.
        private const float IDLE_SHAKE_MIN_DELAY = 4f;
        private const float IDLE_SHAKE_MAX_DELAY = 7.5f;
        private const float IDLE_SHAKE_DURATION = 0.45f;
        private const float IDLE_SHAKE_ROTATION_DEGREES = 2.25f;
        private const float IDLE_SHAKE_SIDE_DISTANCE = 0.003f;

        // Gift-complete animation: 5 cm hop + one full turn.
        private const float COMPLETE_ANIMATION_DURATION = 0.9f;
        private const float COMPLETE_JUMP_HEIGHT = 0.05f;
        private const string COMPLETE_ANIMATION_RPC = "RPC_Moonforged_WrappingBoxComplete";

        private void Awake()
        {
            container = GetComponentInChildren<Container>(true);
            nview = GetComponent<ZNetView>();

            restLocalPosition = transform.localPosition;
            restLocalRotation = transform.localRotation;
            ScheduleNextIdleShake();

            if (nview != null)
            {
                nview.Register(COMPLETE_ANIMATION_RPC, new Action<long>(RPC_PlayCompletionAnimation));
            }
        }

        private void OnDisable()
        {
            StopAllVisualAnimation();
        }

        private void Update()
        {
            if (container == null)
                return;

            bool isInUse = container.IsInUse();

            UpdateIdleAnimation(isInUse);

            // Only the ZNetView owner changes the inventory and starts wrapping.
            if (nview != null && !nview.IsOwner())
            {
                wasInUseLastFrame = isInUse;
                return;
            }

            if (wasInUseLastFrame && !isInUse && !wrapInProgress)
            {
                if (CanWrap())
                {
                    StartCoroutine(WrapSequence());
                }
            }

            wasInUseLastFrame = isInUse;
        }

        private void UpdateIdleAnimation(bool isInUse)
        {
            if (isInUse || wrapInProgress || completionAnimationCoroutine != null)
            {
                StopIdleShakeAndReset();
                return;
            }

            if (idleShakeCoroutine == null && Time.time >= nextIdleShakeTime)
            {
                idleShakeCoroutine = StartCoroutine(IdleShakeRoutine());
            }
        }

        private void ScheduleNextIdleShake()
        {
            nextIdleShakeTime = Time.time + UnityEngine.Random.Range(IDLE_SHAKE_MIN_DELAY, IDLE_SHAKE_MAX_DELAY);
        }

        private IEnumerator IdleShakeRoutine()
        {
            float elapsed = 0f;

            while (elapsed < IDLE_SHAKE_DURATION)
            {
                if (container == null || container.IsInUse() || wrapInProgress)
                    break;

                float normalized = elapsed / IDLE_SHAKE_DURATION;
                float envelope = Mathf.Sin(normalized * Mathf.PI);
                float wave = Mathf.Sin(normalized * Mathf.PI * 6f);

                float yaw = wave * envelope * IDLE_SHAKE_ROTATION_DEGREES;
                float side = wave * envelope * IDLE_SHAKE_SIDE_DISTANCE;

                transform.localPosition = restLocalPosition + Vector3.right * side;
                transform.localRotation = restLocalRotation * Quaternion.Euler(0f, yaw, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            RestoreRestTransform();
            idleShakeCoroutine = null;
            ScheduleNextIdleShake();
        }

        private void StopIdleShakeAndReset()
        {
            if (idleShakeCoroutine != null)
            {
                StopCoroutine(idleShakeCoroutine);
                idleShakeCoroutine = null;
                ScheduleNextIdleShake();
            }

            if (completionAnimationCoroutine == null)
                RestoreRestTransform();
        }

        private void StopAllVisualAnimation()
        {
            if (idleShakeCoroutine != null)
            {
                StopCoroutine(idleShakeCoroutine);
                idleShakeCoroutine = null;
            }

            if (completionAnimationCoroutine != null)
            {
                StopCoroutine(completionAnimationCoroutine);
                completionAnimationCoroutine = null;
            }

            RestoreRestTransform();
        }

        private void RestoreRestTransform()
        {
            transform.localPosition = restLocalPosition;
            transform.localRotation = restLocalRotation;
        }

        private bool IsWrappingPaper(ItemDrop.ItemData item)
        {
            if (item == null || item.m_dropPrefab == null)
                return false;

            return item.m_dropPrefab.name.Contains("WrappingPaper");
        }

        private string GetGiftPrefabNameFromPaper(string paperPrefabName)
        {
            if (string.IsNullOrEmpty(paperPrefabName))
                return null;

            if (paperPrefabName == "M_Green_Gold_WrappingPaper")
                return "M_Green_Gold_Gift";

            if (paperPrefabName == "M_WrappingPaper_SnowFlake_Blue")
                return "M_SnowFlake_Blue";

            if (paperPrefabName == "M_WrappingPaper_SnowFlake_Red")
                return "M_SnowFlake_Red";

            if (paperPrefabName.StartsWith("M_WrappingPaper_"))
                return "M_Gift_" + paperPrefabName.Substring("M_WrappingPaper_".Length);

            return paperPrefabName.Replace("WrappingPaper", "Gift");
        }

        public bool CanWrap()
        {
            var items = container.GetInventory().GetAllItems();
            if (items.Count != 2)
                return false;

            ItemDrop.ItemData paper = null;
            ItemDrop.ItemData content = null;

            foreach (var i in items)
            {
                if (IsWrappingPaper(i))
                    paper = i;
                else
                    content = i;
            }

            return paper != null && content != null;
        }

        private IEnumerator WrapSequence()
        {
            wrapInProgress = true;
            StopIdleShakeAndReset();

            // Existing wrapping pulse while the box prepares the gift.
            yield return StartCoroutine(BlinkRoutine(WRAP_DELAY));

            // Wrap item first. The existing completion sound starts with the hop/spin.
            Wrap();
            PlayCompletionAnimationForEveryone();
            SpawnTamedFX();

            yield return new WaitForSeconds(COMPLETE_ANIMATION_DURATION);

            // Existing completion VFX lands with the box back on the table.
            SpawnBuildSmoke();

            wrapInProgress = false;
            ScheduleNextIdleShake();
        }

        private void PlayCompletionAnimationForEveryone()
        {
            StopIdleShakeAndReset();

            if (nview != null && nview.IsValid())
            {
                nview.InvokeRPC(ZNetView.Everybody, COMPLETE_ANIMATION_RPC, new object[0]);
                return;
            }

            StartCompletionAnimation();
        }

        private void RPC_PlayCompletionAnimation(long sender)
        {
            StartCompletionAnimation();
        }

        private void StartCompletionAnimation()
        {
            if (completionAnimationCoroutine != null)
            {
                StopCoroutine(completionAnimationCoroutine);
                completionAnimationCoroutine = null;
            }

            if (idleShakeCoroutine != null)
            {
                StopCoroutine(idleShakeCoroutine);
                idleShakeCoroutine = null;
            }

            RestoreRestTransform();
            completionAnimationCoroutine = StartCoroutine(CompletionAnimationRoutine());
        }

        private IEnumerator CompletionAnimationRoutine()
        {
            float elapsed = 0f;

            while (elapsed < COMPLETE_ANIMATION_DURATION)
            {
                float normalized = Mathf.Clamp01(elapsed / COMPLETE_ANIMATION_DURATION);

                // Sine arc: exactly 0 at start/end and 5 cm at the middle.
                float height = Mathf.Sin(normalized * Mathf.PI) * COMPLETE_JUMP_HEIGHT;

                // Smooth one-turn spin around the box's own Y axis.
                float spinProgress = Mathf.SmoothStep(0f, 1f, normalized);
                float spin = spinProgress * 360f;

                transform.localPosition = restLocalPosition + Vector3.up * height;
                transform.localRotation = restLocalRotation * Quaternion.Euler(0f, spin, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            RestoreRestTransform();
            completionAnimationCoroutine = null;
            ScheduleNextIdleShake();
        }

        private void Wrap()
        {
            if (ZNetScene.instance == null)
                return;

            var inv = container.GetInventory();

            ItemDrop.ItemData paper = null;
            ItemDrop.ItemData content = null;

            foreach (var i in inv.GetAllItems())
            {
                if (IsWrappingPaper(i))
                    paper = i;
                else
                    content = i;
            }

            if (paper == null || content == null)
                return;

            string giftPrefabName = GetGiftPrefabNameFromPaper(paper.m_dropPrefab.name);
            if (string.IsNullOrEmpty(giftPrefabName))
                return;

            GameObject giftPrefab = ZNetScene.instance.GetPrefab(giftPrefabName);
            if (giftPrefab == null)
                return;

            var custom = new Dictionary<string, string>
            {
                { "wrapped_prefab", content.m_dropPrefab.name },
                { "wrapped_stack", content.m_stack.ToString() },
                { "wrapped_quality", content.m_quality.ToString() },
                { "wrapped_variant", content.m_variant.ToString() },
                { "wrapped_durability", content.m_durability.ToString(CultureInfo.InvariantCulture) }
            };

            ItemDrop giftDrop = giftPrefab.GetComponent<ItemDrop>();
            if (giftDrop == null)
                return;

            ItemDrop.ItemData giftItem = giftDrop.m_itemData.Clone();
            giftItem.m_dropPrefab = giftPrefab;
            giftItem.m_stack = 1;
            giftItem.m_customData = custom;

            inv.RemoveItem(content);
            inv.RemoveItem(paper);
            inv.AddItem(giftItem);
        }

        // Blink effect
        private IEnumerator BlinkRoutine(float duration)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            float t = 0f;

            Dictionary<Material, Color> original = new Dictionary<Material, Color>();

            foreach (var r in renderers)
            {
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    if (!original.ContainsKey(r.material))
                        original[r.material] = r.material.color;
                }
            }

            while (t < duration)
            {
                float pulse = Mathf.Sin(Time.time * 10f) * 0.7f + 1.7f;

                foreach (var r in renderers)
                {
                    if (r != null && r.material != null && r.material.HasProperty("_Color"))
                        r.material.color = original[r.material] * pulse;
                }

                t += Time.deltaTime;
                yield return null;
            }

            foreach (var kv in original)
                if (kv.Key != null)
                    kv.Key.color = kv.Value;
        }

        // Completion VFX
        private void SpawnBuildSmoke()
        {
            GameObject fx = ZNetScene.instance?.GetPrefab("vfx_PlacePiece");
            if (fx != null)
                Instantiate(fx, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        // Completion sound
        private void SpawnTamedFX()
        {
            GameObject fx = ZNetScene.instance?.GetPrefab("fx_creature_tamed");
            if (fx != null)
                Instantiate(fx, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }
    }
}
