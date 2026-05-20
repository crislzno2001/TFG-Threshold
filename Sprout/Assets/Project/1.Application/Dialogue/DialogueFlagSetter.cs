using System;
using System.Collections.Generic;
using UnityEngine;
using OpenAI.Dialogue;

namespace ThresholdGame.Architecture.Events
{
    [Serializable]
    public class NarrativeFlagChange
    {
        public string flagName;
        public bool value = true;
    }

    public class DialogueFlagSetter : MonoBehaviour
    {
        [SerializeField] private NPCBrain npcBrain;
        [SerializeField] private List<NarrativeFlagChange> flagsToApply = new();

        public void ApplyFlags()
        {
            if (npcBrain == null)
            {
                Debug.LogWarning("[DialogueFlagSetter] NPCBrain reference is missing.", this);
                return;
            }

            foreach (var flag in flagsToApply)
            {
                if (flag == null || string.IsNullOrWhiteSpace(flag.flagName))
                    continue;

                npcBrain.SetFlag(flag.flagName.Trim(), flag.value);
                Debug.Log($"[DialogueFlagSetter] {flag.flagName.Trim()} = {flag.value}", this);
            }
        }
    }
}