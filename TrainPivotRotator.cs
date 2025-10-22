using UnityEngine;

namespace Moonforged.ChristmasDecorations
{
    /// Rotates the pivot around +Y at a constant rate.
    /// Attach at runtime to TrackCenter/pivot from RelicRegistrar.
    public class TrainPivotRotator : MonoBehaviour
    {
        // + = CCW, - = CW (top-down view)
        public float degreesPerSecond = 10f;

        void Update()
        {
            transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
