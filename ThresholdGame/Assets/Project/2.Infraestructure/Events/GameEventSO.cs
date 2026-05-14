using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThresholdGame.Architecture.Events
{
    [CreateAssetMenu(fileName = "EV_NewGameEvent", menuName = "Threshold/Events/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [SerializeField] private bool logWhenRaised = false;

        private readonly List<Action> listeners = new();

        public void Raise()
        {
            if (logWhenRaised)
                Debug.Log($"[GameEvent] Raised: {name}", this);

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                listeners[i]?.Invoke();
            }
        }

        public void Register(Action listener)
        {
            if (listener != null && !listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void Unregister(Action listener)
        {
            if (listener != null)
                listeners.Remove(listener);
        }
    }
}