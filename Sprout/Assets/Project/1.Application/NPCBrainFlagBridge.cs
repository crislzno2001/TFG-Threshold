using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenAI.Dialogue;

namespace Sprout.Application
{
    /// <summary>
    /// Mantiene los flags locales de cada NPCBrain en sync con el NarrativeFlagStore central, para que
    /// el prompt de la IA, el cotilleo y los finales reflejen el estado de la historia (incluidos los
    /// resultados de cotilleo entre NPCs). Ponlo en el objeto del director.
    ///
    /// Se auto-suscribe a TODOS los NPCBrain de la escena (y re-escanea al cargar escena), así que no
    /// depende de asignarlos a mano ni del orden de arranque.
    ///
    /// NOTA: los flags de UI (glow_*, recommend_*) NO se propagan al store: son señales visuales que
    /// gestiona NpcGlow directamente sobre cada NPC; no tienen sentido para la IA ni el cotilleo.
    /// </summary>
    public class NPCBrainFlagBridge : MonoBehaviour
    {
        [Tooltip("Opcional. Si lo dejas vacío se auto-buscan todos los NPCBrain de la escena.")]
        [SerializeField] private List<NPCBrain> npcBrains = new();

        private SproutGameDirector _director;
        private readonly HashSet<NPCBrain> _subscribed = new();

        private void Start()
        {
            _director = SproutGameDirector.Instance;
            if (_director == null)
            {
                Debug.LogError("[NPCBrainFlagBridge] No SproutGameDirector in scene.");
                enabled = false;
                return;
            }

            _director.Flags.OnChanged += OnFlagChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Rescan();
            PushAll();
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode m) { Rescan(); PushAll(); }

        /// <summary>Suscribe a los brains asignados + los que haya en la escena (sin duplicar).</summary>
        private void Rescan()
        {
            foreach (var brain in npcBrains) TrySubscribe(brain);
            foreach (var brain in FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                TrySubscribe(brain);
        }

        private void TrySubscribe(NPCBrain brain)
        {
            if (brain == null || _subscribed.Contains(brain)) return;
            brain.OnFlagSet += OnBrainFlag;
            _subscribed.Add(brain);
        }

        private void OnDestroy()
        {
            if (_director != null) _director.Flags.OnChanged -= OnFlagChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            foreach (var brain in _subscribed)
                if (brain != null) brain.OnFlagSet -= OnBrainFlag;
        }

        private void OnFlagChanged(string key) => PushAll();

        private void OnBrainFlag(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return;
            // Las señales de UI las gestiona NpcGlow; no ensucian el store de historia.
            if (key.StartsWith("glow_") || key.StartsWith("recommend_")) return;
            _director?.Flags.SetFlag(key, value);
        }

        /// <summary>Empuja cada flag + contador del store a cada brain (para la IA / cotilleo).</summary>
        public void PushAll()
        {
            if (_director == null) return;
            foreach (var brain in _subscribed)
            {
                if (brain == null) continue;
                foreach (var kv in _director.Flags.AllFlags)
                {
                    if (kv.Key.StartsWith("glow_") || kv.Key.StartsWith("recommend_")) continue;
                    brain.SetFlag(kv.Key, kv.Value);
                }
                foreach (var kv in _director.Flags.AllCounters)
                    brain.SetFlag($"{kv.Key}_value_{kv.Value}", true);
            }
        }
    }
}
