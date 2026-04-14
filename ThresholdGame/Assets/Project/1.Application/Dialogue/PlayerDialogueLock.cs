using UnityEngine;
using ThresholdGame.Presentation.Player;

namespace OpenAI.Dialogue
{
    [RequireComponent(typeof(PlayerStateMachine))]
    public class PlayerDialogueLock : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine stateMachine;

        public bool IsLocked { get; private set; }

        private void Awake()
        {
            if (stateMachine == null)
                stateMachine = GetComponent<PlayerStateMachine>();
        }

        public void Lock()
        {
            if (IsLocked) return;

            IsLocked = true;
            stateMachine?.EnterDialogue();
        }

        public void Unlock()
        {
            if (!IsLocked) return;

            IsLocked = false;
            stateMachine?.EnterFreeRoam();
        }
    }
}