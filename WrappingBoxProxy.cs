using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class WrappingBoxProxy : MonoBehaviour, Interactable, Hoverable
    {
        private WrappingBoxProcessor processor;

        private void Awake()
        {
            processor = GetComponentInChildren<WrappingBoxProcessor>(true);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverName()
        {
            return "Wrapper";
        }

        public string GetHoverText()
        {
            if (processor != null && processor.CanWrap())
            {
                return
                    "Wrapper\n" +
                    "[<color=yellow><b>E</b></color>] Open\n" +
                    "<color=orange>Close box to wrap gift</color>";
            }

            return
                "Wrapper\n" +
                "[<color=yellow><b>E</b></color>] Open\n" +
                "(Add 1 gift + 1 item)";
        }
    }
}
