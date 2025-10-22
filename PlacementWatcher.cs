using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    public class PlacementWatcher : MonoBehaviour
    {
        public List<GameObject> RegisterList;

        void Start()
        {
            if (RegisterList != null && !RegisterList.Contains(gameObject))
                RegisterList.Add(gameObject);
        }

        void OnDestroy()
        {
            if (RegisterList != null)
                RegisterList.Remove(gameObject);
        }
    }
}
