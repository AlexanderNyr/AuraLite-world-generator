using UnityEngine;

namespace AuraLiteWorldGenerator.Runtime
{
    /// <summary>
    /// Makes a transform (typically a billboard quad) face the main camera.
    /// Supports yaw-only mode for world elements that should stay upright.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    [AddComponentMenu("AuraLite/World Generator/Camera Facing Billboard")]
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        [Tooltip("If true, only rotate around the Y axis. Useful for trees and vertical billboards.")]
        public bool yOnly = true;

        [Tooltip("Additional yaw offset applied after rotation.")]
        public float yawOffset = 180f;

        private Camera _targetCamera;

        private void OnEnable()
        {
            _targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
                if (_targetCamera == null)
                    return;
            }

            if (_targetCamera.transform == null)
                return;

            Vector3 direction = _targetCamera.transform.position - transform.position;
            if (yOnly)
                direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion lookRotation = Quaternion.LookRotation(-direction.normalized);
            transform.rotation = Quaternion.Euler(0f, lookRotation.eulerAngles.y + yawOffset, 0f);
        }

        private void OnValidate()
        {
            yawOffset = Mathf.Repeat(yawOffset, 360f);
        }
    }
}
