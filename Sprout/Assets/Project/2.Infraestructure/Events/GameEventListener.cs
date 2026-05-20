using UnityEngine;
using UnityEngine.Events;

namespace ThresholdGame.Architecture.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEventSO gameEvent;
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            if (gameEvent != null)
                gameEvent.Register(OnEventRaised);
        }

        private void OnDisable()
        {
            if (gameEvent != null)
                gameEvent.Unregister(OnEventRaised);
        }

        public void OnEventRaised()
        {
            response?.Invoke();
        }
    }
}