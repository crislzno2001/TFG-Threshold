using UnityEngine;
using Unity.Cinemachine;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// When a conversation is open, a dedicated Cinemachine camera frames the NPC
    /// (a close, fixed-angle shot) and takes priority, so the brain blends in
    /// smoothly; when it closes, priority drops and the player camera returns.
    /// Needs a CinemachineBrain on the Main Camera (your player camera already has it).
    /// </summary>
    public class DialogueCameraRig : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera dialogueCam;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, -2.6f);
        [SerializeField] private float lookHeight = 1.1f;

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

            if (npc != null)
            {
                Vector3 target = npc.transform.position;
                dialogueCam.transform.position = target + offset;
                dialogueCam.transform.LookAt(target + Vector3.up * lookHeight);
                dialogueCam.Priority = 100;
            }
            else
            {
                dialogueCam.Priority = -100;
            }
        }
    }
}
