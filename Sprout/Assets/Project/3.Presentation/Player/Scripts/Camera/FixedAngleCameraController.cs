using UnityEngine;

namespace ThresholdGame.Presentation.Player.Camera
{
    /// <summary>
    /// Controla un anchor de cámara fijo estilo Animal Crossing.
    /// Calcula posición y rotación del anchor cada LateUpdate basándose
    /// en el target (player), un offset configurable y ángulos fijos.
    /// 
    /// La cámara virtual de Cinemachine debe seguir este anchor con
    /// "Hard Lock to Target" (sin Third Person Follow), de forma que
    /// la composición del encuadre depende únicamente de este script.
    /// </summary>
    public sealed class FixedAngleCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform al que la cámara sigue (normalmente el Player).")]
        [SerializeField] private Transform followTarget;

        [Tooltip("Transform que la Cinemachine usará como Tracking Target. " +
                 "Este script controla su posición y rotación.")]
        [SerializeField] private Transform cameraAnchor;

        [Header("Fixed Orientation")]
        [Range(0f, 89f)]
        [SerializeField] private float pitch = 50f;

        [Range(-180f, 180f)]
        [SerializeField] private float yaw = 0f;

        [Header("Framing")]
        [Tooltip("Distancia horizontal de la cámara al target.")]
        [SerializeField] private float distance = 10f;

        [Tooltip("Altura de la cámara sobre el target.")]
        [SerializeField] private float height = 7f;

        [Tooltip("Offset del punto al que mira la cámara (relativo al target). " +
                 "Sube Y para mirar a la cabeza, baja para mirar al suelo.")]
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

      
        /// <summary>Yaw actual de la cámara, para que el sistema de movimiento calcule direcciones relativas.</summary>
        public float CurrentYaw => yaw;
        private void LateUpdate()
        {
            if (followTarget == null || cameraAnchor == null) return;

            Vector3 lookAtPoint = followTarget.position + lookAtOffset;

            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 backDirection = yawRotation * Vector3.back;

            Vector3 desiredPosition = lookAtPoint
                                    + Vector3.up * height
                                    + backDirection * distance;

            // Follow directo, sin suavizado (estilo Animal Crossing).
            cameraAnchor.position = desiredPosition;
            cameraAnchor.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (followTarget == null) return;
            Vector3 lookAt = followTarget.position + lookAtOffset;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lookAt, 0.15f);
            if (cameraAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(cameraAnchor.position, lookAt);
            }
        }
#endif
    }
}