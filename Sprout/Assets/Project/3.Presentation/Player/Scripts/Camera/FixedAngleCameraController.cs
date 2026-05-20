using UnityEngine;

namespace ThresholdGame.Presentation.Player.Camera
{
    /// <summary>
    /// Controla la orientación fija del target de la Cinemachine Camera
    /// para conseguir un encuadre tipo Animal Crossing.
    /// 
    /// La cámara virtual (Cinemachine) sigue a un Transform "anchor"
    /// que este script orienta cada frame con un pitch (X) y un yaw (Y)
    /// fijos definidos en el Inspector.
    /// 
    /// El jugador NO puede rotar esta cámara con input — es intencionalmente
    /// estática para preservar la composición del encuadre, igual que
    /// Animal Crossing.
    /// </summary>
    public sealed class FixedAngleCameraController : MonoBehaviour
    {
        [Header("Camera Anchor")]
        [Tooltip("Transform al que Cinemachine sigue como Tracking Target. " +
                 "Este script controlará su rotación.")]
        [SerializeField] private Transform cameraAnchor;

        [Header("Fixed Orientation")]
        [Tooltip("Ángulo picado de la cámara (X). 45-55 da feeling Animal Crossing.")]
        [Range(0f, 89f)]
        [SerializeField] private float pitch = 50f;

        [Tooltip("Ángulo lateral (Y). 0 = frontal, 45 = vista diagonal estilo isométrico.")]
        [Range(-180f, 180f)]
        [SerializeField] private float yaw = 0f;

        /// <summary>
        /// Yaw actual de la cámara. Lo expone para que el sistema de
        /// movimiento pueda calcular direcciones relativas a la cámara.
        /// </summary>
        public float CurrentYaw => yaw;

        private void Reset()
        {
            // Por defecto, intentamos auto-asignar el target si está como hijo.
            if (cameraAnchor == null)
                cameraAnchor = transform;
        }

        private void LateUpdate()
        {
            if (cameraAnchor == null) return;

            // LateUpdate para garantizar que se aplica después de cualquier
            // movimiento del personaje en Update.
            cameraAnchor.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}