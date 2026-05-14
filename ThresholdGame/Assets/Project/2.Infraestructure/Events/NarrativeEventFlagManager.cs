using System;
using System.Collections.Generic;
using UnityEngine;
using OpenAI.Dialogue;
using ThresholdGame.Architecture.Events;

namespace ThresholdGame.Presentation.Narrative
{
    [Serializable]
    public class NarrativeEventFlagBinding
    {
        [Header("Evento que se escucha")]
        public GameEventSO gameEvent;

        [Header("NPC afectado")]
        public NPCBrain npcBrain;

        [Header("Flags que se aplican cuando ocurre el evento")]
        public List<NarrativeFlagChange> flagsToApply = new();
    }

    [Serializable]
    public class NarrativeFlagChange
    {
        public string flagName;
        public bool value = true;
    }

    public class NarrativeEventFlagManager : MonoBehaviour
    {
        [SerializeField] private List<NarrativeEventFlagBinding> bindings = new();

        private readonly List<RuntimeSubscription> runtimeSubscriptions = new();

        private void OnEnable()
        {
            runtimeSubscriptions.Clear();

            foreach (var binding in bindings)
            {
                if (binding == null || binding.gameEvent == null)
                    continue;

                Action callback = () => ApplyBinding(binding);

                binding.gameEvent.Register(callback);

                runtimeSubscriptions.Add(new RuntimeSubscription
                {
                    gameEvent = binding.gameEvent,
                    callback = callback
                });
            }
        }

        private void OnDisable()
        {
            foreach (var subscription in runtimeSubscriptions)
            {
                if (subscription.gameEvent != null && subscription.callback != null)
                    subscription.gameEvent.Unregister(subscription.callback);
            }

            runtimeSubscriptions.Clear();
        }

        private void ApplyBinding(NarrativeEventFlagBinding binding)
        {
            if (binding.npcBrain == null)
            {
                Debug.LogWarning("[NarrativeEventFlagManager] Falta NPCBrain en un binding.", this);
                return;
            }

            foreach (var flag in binding.flagsToApply)
            {
                if (flag == null || string.IsNullOrWhiteSpace(flag.flagName))
                    continue;

                string cleanFlagName = flag.flagName.Trim();

                binding.npcBrain.SetFlag(cleanFlagName, flag.value);

                Debug.Log(
                    $"[NarrativeEventFlagManager] {binding.gameEvent.name} → {cleanFlagName} = {flag.value}",
                    this
                );
            }
        }

        private class RuntimeSubscription
        {
            public GameEventSO gameEvent;
            public Action callback;
        }
    }
}