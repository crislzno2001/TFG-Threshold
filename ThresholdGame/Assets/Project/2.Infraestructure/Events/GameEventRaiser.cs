using UnityEngine;

namespace ThresholdGame.Architecture.Events
{
    public class GameEventRaiser : MonoBehaviour
    {
        [SerializeField] private GameEventSO eventToRaise;

        public void Raise()
        {
            if (eventToRaise != null)
                eventToRaise.Raise();
        }
    }
}