// Assets/Moonforged Christmas Decorations/Scripts/DeerMarker.cs
using System.Collections.Generic;
using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Tag for sled deer + global registry so we don't scan the whole scene.
    public class DeerMarker : MonoBehaviour
    {
        public static readonly HashSet<DeerMarker> All = new HashSet<DeerMarker>();

        private void OnEnable()
        {
            All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }
    }
}
