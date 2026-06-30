using UnityEngine;
using Unity.Cinemachine;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Durante una conversación, una cámara dedicada encuadra a las DOS enfrentadas: el vecino se gira a
    /// mirar a la florista, y la cámara se coloca detrás del hombro de la florista (más lejos), mirando
    /// hacia el vecino. Así se ve la cabeza de la florista en primer plano y la cara del vecino al fondo.
    /// Necesita un CinemachineBrain en la Main Camera (tu cámara de jugador ya lo tiene).
    /// </summary>
    public class DialogueCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera dialogueCam;
        [SerializeField] private string playerTag = "Player";

        [Header("Encuadre (las dos enfrentadas)")]
        [Tooltip("Cuánto se aleja la cámara por detrás de la florista. Súbelo para verlas más lejos.")]
        [SerializeField] private float backDistance = 5f;
        [Tooltip("Altura de la cámara.")]
        [SerializeField] private float height = 2f;
        [Tooltip("Desplazamiento lateral (para que la florista no tape al vecino).")]
        [SerializeField] private float sideOffset = 1.6f;
        [Tooltip("Altura a la que mira la cámara (apunta a la cara).")]
        [SerializeField] private float lookHeight = 1.3f;
        [Tooltip("Velocidad a la que se giran a mirarse.")]
        [SerializeField] private float turnSpeed = 8f;
        [Tooltip("Que la florista también se gire a mirar al vecino (para no quedar mirando raro).")]
        [SerializeField] private bool turnPlayerToo = true;

        [Header("Evitar que algo tape la vista")]
        [Tooltip("Capas que pueden tapar la cámara (paredes, árboles, casas...). Si una se interpone entre " +
                 "la cámara y los personajes, la cámara se acerca para no perderlos. Déjalo en Nothing para desactivar.")]
        [SerializeField] private LayerMask occluders = 0;
        [SerializeField] private float occlusionBuffer = 0.3f;

        private Transform _player;

        private void Awake()
        {
            if (dialogueCam == null) dialogueCam = GetComponent<CinemachineCamera>();
            if (dialogueCam == null) dialogueCam = gameObject.AddComponent<CinemachineCamera>();
            dialogueCam.Priority = -100;
        }

        private void LateUpdate()
        {
            if (dialogueCam == null) return;

            var active = DialogueUI.Active;
            var npc = active != null ? active.CurrentNpc : null;
            if (npc == null) { dialogueCam.Priority = -100; return; }

            Transform npcT = npc.transform;

            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p != null) _player = p.transform;
            }

            // Sin player localizado: encuadre simple desde delante del vecino.
            if (_player == null)
            {
                dialogueCam.transform.position = npcT.position + new Vector3(0f, height, -backDistance);
                dialogueCam.transform.LookAt(npcT.position + Vector3.up * lookHeight);
                dialogueCam.Priority = 100;
                return;
            }

            // 1) El vecino se gira a mirar a la florista (enfrentadas).
            Vector3 toPlayer = _player.position - npcT.position; toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
                npcT.rotation = Quaternion.Slerp(npcT.rotation, Quaternion.LookRotation(toPlayer), Time.deltaTime * turnSpeed);

            // 1b) La florista también se gira a mirar al vecino.
            if (turnPlayerToo)
            {
                Vector3 toNpc = npcT.position - _player.position; toNpc.y = 0f;
                if (toNpc.sqrMagnitude > 0.01f)
                    _player.rotation = Quaternion.Slerp(_player.rotation, Quaternion.LookRotation(toNpc), Time.deltaTime * turnSpeed);
            }

            // 2) Cámara detrás del hombro de la florista, mirando hacia el vecino.
            Vector3 dir = npcT.position - _player.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = _player.forward; else dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir);

            // Mira al punto MEDIO entre las dos (a la altura de la cara) para que entren AMBAS en cuadro.
            Vector3 mid = (_player.position + npcT.position) * 0.5f + Vector3.up * lookHeight;
            Vector3 camPos = _player.position - dir * backDistance + right * sideOffset + Vector3.up * height;

            // Si algo (pared, árbol, casa) se mete entre la cámara y los personajes, acercamos la cámara.
            if (occluders.value != 0)
            {
                Vector3 toCam = camPos - mid;
                float d = toCam.magnitude;
                if (d > 0.01f && Physics.Raycast(mid, toCam / d, out var hit, d, occluders, QueryTriggerInteraction.Ignore))
                    camPos = hit.point - (toCam / d) * occlusionBuffer;
            }

            dialogueCam.transform.position = camPos;
            dialogueCam.transform.LookAt(mid);
            dialogueCam.Priority = 100;
        }
    }
}
