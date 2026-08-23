using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class ReindeerPetInteract : MonoBehaviour, Interactable, Hoverable
    {
        private Piece piece;

        private void Awake()
        {
            piece = GetComponent<Piece>();
            InstallInteractionProxies();
        }

        private void Start()
        {
            if (piece == null)
                piece = GetComponent<Piece>();

            InstallInteractionProxies();
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user == null)
                return false;

            Player player = user as Player;
            if (player == null)
                return false;

            GameObject freezeGlandPrefab = ObjectDB.instance != null
                ? ObjectDB.instance.GetItemPrefab("FreezeGland")
                : null;

            if (freezeGlandPrefab == null && ZNetScene.instance != null)
                freezeGlandPrefab = ZNetScene.instance.GetPrefab("FreezeGland");

            if (freezeGlandPrefab == null)
            {
                player.Message(MessageHud.MessageType.Center, "The reindeer could not find a frozen booger.");
                return true;
            }

            Inventory inventory = player.GetInventory();
            bool addedFreezeGland = inventory != null
                ? inventory.AddItem(freezeGlandPrefab, 1)
                : false;

            if (!addedFreezeGland)
            {
                player.Message(MessageHud.MessageType.Center, "Your inventory is full.");
                return true;
            }

            player.Message(MessageHud.MessageType.Center, "The reindeer sneezes and gives you a frozen booger!");
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
                : "Santa's Reindeer";
        }

        public string GetHoverText()
        {
            return GetHoverName() + "\n[<color=yellow><b>E</b></color>] Pet";
        }

        private void InstallInteractionProxies()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.gameObject == gameObject)
                    continue;

                ReindeerPetInteractionProxy proxy =
                    collider.GetComponent<ReindeerPetInteractionProxy>() ??
                    collider.gameObject.AddComponent<ReindeerPetInteractionProxy>();
                proxy.Configure(this);
            }
        }
    }

    public class ReindeerPetInteractionProxy : MonoBehaviour, Interactable, Hoverable
    {
        private ReindeerPetInteract target;

        public void Configure(ReindeerPetInteract reindeer)
        {
            target = reindeer;
        }

        private void Awake()
        {
            if (target == null)
                target = GetComponentInParent<ReindeerPetInteract>();
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
            return target != null ? target.GetHoverName() : "Santa's Reindeer";
        }

        public string GetHoverText()
        {
            return target != null ? target.GetHoverText() : "";
        }
    }
}
