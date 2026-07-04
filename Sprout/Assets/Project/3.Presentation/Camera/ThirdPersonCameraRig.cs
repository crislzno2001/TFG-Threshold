using UnityEngine;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    /// <summary>
    /// Cámara en TERCERA PERSONA con anti-clipping (no atraviesa paredes). Pensada para interiores como
    /// la Casa: sigue al personaje por detrás y, si una pared se interpone, acerca la cámara para no
    /// meterse en la geometría.
    ///
    /// El personaje ENTRA en runtime, así que el rig lo busca por TAG (no hace falta arrastrarlo).
    /// Además, como el player trae su propia cámara (la que se choca), este rig la APAGA mientras estás
    /// en la Casa y la vuelve a encender al salir de la escena.
    ///
    /// Uso: crea una Cámara NUEVA en la escena Casa, ponle este componente, deja targetTag = "Player" y
    /// en 'collisionMask' marca solo las paredes/entorno (NO la capa del jugador).
    /// </summary>
    public sealed class ThirdPersonCameraRig : MonoBehaviour
    {
        [Header("Objetivo (se busca por tag en runtime)")]
        [Tooltip("Tag del personaje a seguir. Se busca solo cuando entra en la escena.")]
        [SerializeField] private string targetTag = "Player";
        [Tooltip("Opcional: si ya está en la escena, arrástralo aquí y se salta la búsqueda por tag.")]
        [SerializeField] private Transform target;
        [Tooltip("Altura del punto al que mira la cámara (aprox. la cabeza).")]
        [SerializeField] private float pivotHeight = 1.6f;

        [Header("Colocación")]
        [SerializeField] private float distance = 3f;
        [SerializeField] private float height = 1.6f;
        [SerializeField] private float sideOffset = 0f;

        [Header("Suavizado")]
        [SerializeField] private float followSmooth = 10f;
        [SerializeField] private float rotationSmooth = 10f;

        [Header("Anti-paredes")]
        [Tooltip("Capas que cuentan como pared/entorno. NO incluyas la capa del jugador.")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float minDistance = 0.6f;

        [Header("Cámara del player")]
        [Tooltip("Apaga la cámara que trae el player mientras estás en esta escena (y la reactiva al salir).")]
        [SerializeField] private bool disablePlayerCamera = true;

        [Header("Órbita con ratón (opcional)")]
        [SerializeField] private bool orbitWithMouse = false;
        [SerializeField] private float orbitSpeed = 0.15f;
        [SerializeField] private float minPitch = -15f;
        [SerializeField] private float maxPitch = 60f;

        private float _yaw;
        private float _pitch = 15f;
        private Vector3 _camVel;
        private GameObject _playerCamGo;   // la cámara del player que apagamos

        private void LateUpdate()
        {
            if (target == null)
            {
                AcquireTarget();
                if (target == null) return;   // el player aún no ha entrado
            }

            if (orbitWithMouse && Mouse.current != null)
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                _yaw += d.x * orbitSpeed;
                _pitch = Mathf.Clamp(_pitch - d.y * orbitSpeed, minPitch, maxPitch);
            }
            else if (!orbitWithMouse)
            {
                _yaw = Mathf.LerpAngle(_yaw, target.eulerAngles.y, rotationSmooth * Time.deltaTime);
            }

            Vector3 pivot = target.position + Vector3.up * pivotHeight;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);

            Vector3 offset = rot * new Vector3(sideOffset, height - pivotHeight, -distance);
            Vector3 desired = pivot + offset;

            // Anti-paredes: esfera desde el pivote hacia la cámara; si choca, acércala.
            Vector3 dir = desired - pivot;
            float dist = dir.magnitude;
            if (dist > 0.001f && Physics.SphereCast(pivot, collisionRadius, dir.normalized,
                    out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                dist = Mathf.Max(minDistance, hit.distance - 0.05f);
                desired = pivot + dir.normalized * dist;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _camVel,
                                                    1f / Mathf.Max(0.01f, followSmooth));
            transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        }

        /// <summary>Busca al player por tag cuando entra en la escena y apaga su cámara.</summary>
        private void AcquireTarget()
        {
            var go = GameObject.FindWithTag(targetTag);
            if (go == null) return;
            target = go.transform;

            if (_yaw == 0f) _yaw = target.eulerAngles.y;

            if (disablePlayerCamera)
            {
                var myCam = GetComponent<Camera>();
                var playerCam = go.GetComponentInChildren<Camera>(true);
                if (playerCam != null && playerCam != myCam)
                {
                    _playerCamGo = playerCam.gameObject;
                    _playerCamGo.SetActive(false);   // evita el choque contra paredes y doble AudioListener
                }
            }
        }

        private void OnDestroy()
        {
            // Al salir de la Casa (se descarga la escena) devolvemos la cámara del player.
            if (_playerCamGo != null) _playerCamGo.SetActive(true);
        }
    }
}
