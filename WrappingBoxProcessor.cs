using UnityEngine;
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

        private const float WRAP_DELAY = 3.8f;

        private void Awake()
        {
            container = GetComponentInChildren<Container>(true);
            nview = GetComponent<ZNetView>();
        }

        private void Update()
        {
            if (container == null)
                return;

            if (nview != null && !nview.IsOwner())
                return;

            bool isInUse = container.IsInUse();

            if (wasInUseLastFrame && !isInUse && !wrapInProgress)
            {
                if (CanWrap())
                {
                    StartCoroutine(WrapSequence());
                }
            }

            wasInUseLastFrame = isInUse;
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

            if (paperPrefabName == "M_Gree_Gold_WrappingPaper")
                return "M_Gree_Gold_Gift";

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

            // Blink effect
            yield return StartCoroutine(BlinkRoutine(WRAP_DELAY));

            // Wrap item
            Wrap();

            // Completion effects
            SpawnBuildSmoke();
            SpawnTamedFX();

            wrapInProgress = false;
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

            inv.RemoveItem(content);
            inv.RemoveItem(paper);

            ItemDrop giftDrop = giftPrefab.GetComponent<ItemDrop>();
            if (giftDrop == null)
                return;

            ItemDrop.ItemData giftItem = giftDrop.m_itemData.Clone();
            giftItem.m_dropPrefab = giftPrefab;
            giftItem.m_stack = 1;
            giftItem.m_customData = custom;

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
