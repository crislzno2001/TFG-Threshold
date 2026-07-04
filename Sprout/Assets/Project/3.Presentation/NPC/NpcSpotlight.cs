using System;
using System.Collections.Generic;
using UnityEngine;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation
{
    /// <summary>Estados de brillo de un NPC (guía visual para el jugador).</summary>
    public enum GlowState { None, Soft, Strong, Referral, Red }

    /// <summary>
    /// Estado central del "spotlight" de los NPCs: qué NPC brilla y cómo, y a quién se recomienda ir a
    /// hablar. Los grafos lo controlan por FLAGS (ver SpotlightFlagBridge); los NpcGlow lo pintan.
    /// Ponlo en un objeto de la escena (por ejemplo junto a los managers).
    /// </summary>
    public sealed class NpcSpotlight : MonoBehaviour
    {
        private static NpcSpotlight _instance;

        /// <summary>
        /// Singleton AUTO-SANADOR: si no hay instancia (porque el objeto está inactivo, aún no arrancó,
        /// o se destruyó al cambiar de escena) se busca en la escena o se crea una al vuelo. Así nunca
        /// devuelve null y el glow no depende de que el objeto exista/esté activo en el momento justo.
        /// </summary>
        public static NpcSpotlight Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NpcSpotlight>();
                    if (_instance == null)
                        _instance = new GameObject("NpcSpotlight (auto)").AddComponent<NpcSpotlight>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private readonly Dictionary<NpcId, GlowState> _glow = new();

        /// <summary>NPC recomendado ahora mismo ("ve a hablar con X"), o null.</summary>
        public NpcId? CurrentRecommended { get; private set; }

        /// <summary>(npc, nuevo estado) — lo escuchan los NpcGlow y la UI.</summary>
        public event Action<NpcId, GlowState> OnGlowChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        private void OnDestroy() { if (_instance == this) _instance = null; }

        public GlowState GetGlow(NpcId npc) => _glow.TryGetValue(npc, out var s) ? s : GlowState.None;

        public void SetGlow(NpcId npc, GlowState state)
        {
            if (GetGlow(npc) == state) return;
            _glow[npc] = state;
            Debug.Log($"[Spotlight] {npc} → {state}  (oyentes: {(OnGlowChanged != null)})");
            OnGlowChanged?.Invoke(npc, state);
        }

        /// <summary>Recomienda ir a hablar con este NPC: lo pone en Referral y quita el referral anterior.</summary>
        public void Recommend(NpcId npc)
        {
            if (CurrentRecommended.HasValue && CurrentRecommended.Value != npc &&
                GetGlow(CurrentRecommended.Value) == GlowState.Referral)
                SetGlow(CurrentRecommended.Value, GlowState.None);

            CurrentRecommended = npc;
            SetGlow(npc, GlowState.Referral);
        }

        /// <summary>Quita el referral (p. ej. al ir por fin a hablar con ese NPC).</summary>
        public void ClearRecommend(NpcId npc)
        {
            if (GetGlow(npc) == GlowState.Referral) SetGlow(npc, GlowState.None);
            if (CurrentRecommended.HasValue && CurrentRecommended.Value == npc) CurrentRecommended = null;
        }
    }
}
