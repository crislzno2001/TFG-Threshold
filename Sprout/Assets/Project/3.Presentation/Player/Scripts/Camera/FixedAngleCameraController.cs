using UnityEngine;

namespace ThresholdGame.Presentation.Player.Camera
{
    /// <summary>
    /// Controla un anchor de cámara fijo estilo Animal Crossing.
    /// La cámara mantiene un ángulo fijo y sigue al personaje.
    /// Si un objeto de la layer "Occluder" tapa al personaje, sube suavemente
    /// a una vista más cenital para evitar que el jugador quede oculto.
    /// </summary>
    public sealed class FixedAngleCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform al que la cámara sigue, normalmente el Player.")]
        [SerializeField] private Transform followTarget;

        [Tooltip("Transform que la Cinemachine usa como Tracking Target. Este script lo coloca.")]
        [SerializeField] private Transform cameraAnchor;

        [Header("Fixed Orientation")]
        [Range(0f, 89f)]
        [SerializeField] private float pitch = 35f;

        [Range(-180f, 180f)]
        [SerializeField] private float yaw = 0f;

        [Header("Framing")]
        [Tooltip("Distancia normal de la cámara al personaje.")]
        [SerializeField] private float distance = 15f;

        [Tooltip("Punto al que mira la cámara relativo al personaje.")]
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

        [Header("Evitar oclusión")]
        [Tooltip("Si algo de la layer Occluder tapa al personaje, la cámara sube.")]
        [SerializeField] private bool avoidOcclusion = true;

        [Tooltip("Pitch usado cuando el personaje queda tapado.")]
        [Range(0f, 89f)]
        [SerializeField] private float occludedPitch = 75f;

        [Tooltip("Distancia usada cuando el personaje queda tapado.")]
        [SerializeField] private float occludedDistance = 18f;

        [Tooltip("Velocidad con la que la cámara sube y baja.")]
        [SerializeField] private float adjustSpeed = 3f;

        [Header("Occluder Layer")]
        [Tooltip("Nombre exacto de la layer que tapa al jugador.")]
        [SerializeField] private string occluderLayerName = "Occluder";

        private int _occluderMask;

        // 0 = cámara normal, 1 = cámara elevada.
        private float _occlusion;

        /// <summary>
        /// Yaw actual de la cámara, útil para mover al jugador relativo a cámara.
        /// </summary>
        public float CurrentYaw => yaw;

        private void Awake()
        {
            RefreshOccluderMask();
        }

        private void OnValidate()
        {
            RefreshOccluderMask();
        }

        private void RefreshOccluderMask()
        {
            _occluderMask = LayerMask.GetMask(occluderLayerName);

            if (_occluderMask == 0)
            {
                Debug.LogWarning(
                    $"No existe ninguna layer llamada '{occluderLayerName}'. " +
                    $"Crea una layer con ese nombre y asígnala a edificios, árboles grandes o paredes.",
                    this
                );
            }
        }

        private void LateUpdate()
        {
            if (followTarget == null || cameraAnchor == null) return;

            Vector3 lookAtPoint = followTarget.position + lookAtOffset;

            /*
             * 1. Calculamos SIEMPRE la posición normal de cámara.
             * Esta posición no depende de _occlusion.
             * Así evitamos el bucle:
             * normal -> tapado -> sube -> ya no tapado -> baja -> tapado...
             */
            Quaternion normalRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 normalViewDirection = normalRotation * Vector3.forward;
            Vector3 normalCameraPosition = lookAtPoint - normalViewDirection * distance;

            /*
             * 2. Comprobamos si desde la cámara NORMAL el personaje está tapado.
             * Aunque la cámara esté actualmente arriba, seguimos preguntando:
             * "¿la cámara normal estaría tapada?"
             */
            bool isOccluded = false;

            if (avoidOcclusion && _occluderMask != 0)
            {
                isOccluded =
                    Physics.Linecast(
                        normalCameraPosition,
                        lookAtPoint,
                        out RaycastHit lineHit,
                        _occluderMask,
                        QueryTriggerInteraction.Ignore
                    )
                    && !lineHit.transform.IsChildOf(followTarget);
            }

            /*
             * 3. Suavizamos la transición.
             * Si está tapado, _occlusion va hacia 1.
             * Si ya no está tapado desde la cámara normal, vuelve hacia 0.
             */
            _occlusion = Mathf.MoveTowards(
                _occlusion,
                isOccluded ? 1f : 0f,
                adjustSpeed * Time.deltaTime
            );

            /*
             * 4. Calculamos la cámara final mezclando entre:
             * - cámara normal
             * - cámara elevada/cenital
             */
            float currentPitch = Mathf.Lerp(pitch, occludedPitch, _occlusion);
            float currentDistance = Mathf.Lerp(distance, occludedDistance, _occlusion);

            Quaternion finalRotation = Quaternion.Euler(currentPitch, yaw, 0f);
            Vector3 finalViewDirection = finalRotation * Vector3.forward;
            Vector3 desiredPosition = lookAtPoint - finalViewDirection * currentDistance;

            Vector3 finalPosition = desiredPosition;

            /*
             * 5. Evitamos que la cámara atraviese edificios.
             * Solo choca contra objetos de la layer Occluder.
             */
            if (avoidOcclusion && _occluderMask != 0)
            {
                Vector3 toCamera = desiredPosition - lookAtPoint;
                float maxDistance = toCamera.magnitude;

                if (maxDistance > 0.01f &&
                    Physics.SphereCast(
                        lookAtPoint,
                        0.25f,
                        toCamera.normalized,
                        out RaycastHit sphereHit,
                        maxDistance,
                        _occluderMask,
                        QueryTriggerInteraction.Ignore
                    )
                    && !sphereHit.transform.IsChildOf(followTarget))
                {
                    finalPosition = sphereHit.point + sphereHit.normal * 0.25f;
                }
            }

            cameraAnchor.position = finalPosition;
            cameraAnchor.rotation = finalRotation;
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