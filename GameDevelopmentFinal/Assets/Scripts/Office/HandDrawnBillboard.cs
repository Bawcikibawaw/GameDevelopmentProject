using UnityEngine;

namespace Tylerland.Office
{
    /// <summary>
    /// Keeps 2D hand-drawn sprites (animatronics, props, window threats) facing the player's 3D camera.
    /// Enables the 2.5D aesthetic: hand-drawn 2D artwork living inside a 3D acoustic room.
    /// </summary>
    [ExecuteAlways]
    public class HandDrawnBillboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [Tooltip("Lock X and Z axes so the 2D sprite only turns horizontally towards the camera")]
        [SerializeField] private bool _lockYOnly = true;

        [Tooltip("Flip horizontally if the raw sprite was drawn facing the opposite direction")]
        [SerializeField] private bool _flipHorizontal = false;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;

            if (_lockYOnly)
            {
                Vector3 targetPosition = cam.transform.position;
                targetPosition.y = transform.position.y;
                Vector3 direction = transform.position - targetPosition;

                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            else
            {
                transform.rotation = cam.transform.rotation;
            }

            if (_flipHorizontal)
            {
                transform.Rotate(0f, 180f, 0f);
            }
        }
    }
}
