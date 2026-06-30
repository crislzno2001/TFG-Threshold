using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Sprout.Application;
using Sprout.Domain.DayCycle;
using Sprout.Domain.Narrative;
using Sprout.Domain.Flowers;

namespace Sprout.Presentation
{
    /// <summary>
    /// Panel de DEPURACIÓN del bucle diario. Ponlo en cualquier objeto de la escena de juego.
    /// - Muestra en pantalla el día y la fase actuales (y el último resumen nocturno).
    /// - Con la tecla (F9 por defecto) dispara el ciclo completo: fundido -> avanzar a la noche
    ///   (corre el gossip) -> recap -> mañana siguiente. Así pruebas el bucle sin andar hasta la cama.
    ///
    /// Quítalo (o desactívalo) antes de entregar; es solo para probar.
    /// </summary>
    public sealed class DailyLoopDebugger : MonoBehaviour
    {
        [Tooltip("Teclas opcionales. Por defecto desactivadas (None): usa los botones en pantalla.")]
        [SerializeField] private Key sleepKey = Key.None;
        [SerializeField] private Key seedGossipKey = Key.None;
        [SerializeField] private DayCycleService dayCycle;
        [SerializeField] private NightGossipService gossip;

        private bool _busy;
        private bool _seeded;
        private List<string> _lastSummary;
        private GUIStyle _title, _line;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Atajos FIJOS: funcionan aunque el cursor esté bloqueado y no puedas clicar los botones.
            if (kb.digit3Key.wasPressedThisFrame) GiveTestFlowers();
            if (kb.digit1Key.wasPressedThisFrame) SeedTestGossip();
            if (kb.digit2Key.wasPressedThisFrame && !_busy) StartCoroutine(SleepRoutine());

            // Teclas opcionales configurables en el inspector.
            if (seedGossipKey != Key.None && kb[seedGossipKey].wasPressedThisFrame) SeedTestGossip();
            if (sleepKey != Key.None && !_busy && kb[sleepKey].wasPressedThisFrame) StartCoroutine(SleepRoutine());
        }

        /// <summary>Activa flags reales que disparan cotilleos en el GossipRuleEngine (para probar el recap).</summary>
        private void SeedTestGossip()
        {
            var f = SproutGameDirector.Instance != null ? SproutGameDirector.Instance.Flags : null;
            if (f == null) { Debug.LogWarning("[DailyLoopDebugger] No hay Flags (SproutGameDirector)."); return; }

            // Dos cotilleos positivos que no encadenan enfados:
            f.SetFlag(NarrativeFlagKeys.PlayerWasHonest, true);   // -> "alguien habló bien de tu sinceridad"
            f.SetFlag("gave_comforting_bouquet", true);           // -> "una pequeña bondad floreciendo"
            _seeded = true;
            Debug.Log("[DailyLoopDebugger] Cotilleos de prueba sembrados. Pulsa Dormir para verlos.");
        }

        /// <summary>Mete 3 de CADA flor en el inventario para poder probar todos los ramos.</summary>
        private void GiveTestFlowers()
        {
            var D = SproutGameDirector.Instance;
            if (D == null || D.Inventory == null) { Debug.LogWarning("[DailyLoopDebugger] No hay Inventory (SproutGameDirector)."); return; }
            foreach (FlowerKind k in System.Enum.GetValues(typeof(FlowerKind)))
                if (k != FlowerKind.None) D.Inventory.AddFlower(k, 3);
            Debug.Log("[DailyLoopDebugger] +3 de cada flor. Pulsa C para crear ramos.");
        }

        private IEnumerator SleepRoutine()
        {
            _busy = true;
            if (dayCycle == null) dayCycle = FindAnyObjectByType<DayCycleService>();
            if (gossip == null) gossip = FindAnyObjectByType<NightGossipService>();
            var D = SproutGameDirector.Instance;

            if (D == null || D.Day == null) { Debug.LogWarning("[DailyLoopDebugger] No hay SproutGameDirector/Day en la escena."); _busy = false; yield break; }

            var recap = NightRecapUI.GetOrCreate();
            List<string> lines = null;
            UnityAction<List<string>> cap = ls => { lines = ls; _lastSummary = ls; };
            if (gossip != null) gossip.onNightSummary.AddListener(cap);

            yield return recap.FadeIn();

            int guard = 0;
            while (D.Day.Phase != DayPhase.Night && !D.Day.IsFinished && guard++ < 12)
                dayCycle?.AdvancePhase();

            if (gossip != null) gossip.onNightSummary.RemoveListener(cap);

            int day = D.Day.Day;
            bool cont = false;
            recap.ShowContent(day, lines, () => cont = true);
            while (!cont) yield return null;

            dayCycle?.AdvancePhase(); // -> mañana siguiente
            yield return recap.FadeOut();
            _busy = false;
        }

        private void OnGUI()
        {
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                _line = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white } };
            }

            var D = SproutGameDirector.Instance;
            int h = 164 + (_lastSummary != null ? _lastSummary.Count * 18 + 22 : 0);
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(new Rect(10, 10, 340, h), GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(22, 16, 320, 22), "BUCLE DIARIO · debug", _title);
            string estado = (D != null && D.Day != null)
                ? (D.Day.IsFinished ? "Juego terminado" : $"Día {D.Day.Day}  ·  {D.Day.Phase}")
                : "⚠ SproutGameDirector NO encontrado";
            GUI.Label(new Rect(22, 38, 320, 22), estado, _line);

            if (GUI.Button(new Rect(22, 62, 300, 26), "Sembrar cotilleos (tecla 1)" + (_seeded ? "   ✓" : "")))
                SeedTestGossip();
            if (GUI.Button(new Rect(22, 94, 300, 26), "Dar 3 de cada flor (tecla 3)"))
                GiveTestFlowers();
            if (GUI.Button(new Rect(22, 126, 300, 26), _busy ? "..." : "Dormir → día siguiente (tecla 2)") && !_busy)
                StartCoroutine(SleepRoutine());

            if (_lastSummary != null && _lastSummary.Count > 0)
            {
                GUI.Label(new Rect(22, 160, 320, 20), "Último resumen nocturno:", _line);
                for (int i = 0; i < _lastSummary.Count; i++)
                    GUI.Label(new Rect(28, 180 + i * 18, 312, 18), "· " + _lastSummary[i], _line);
            }
        }
    }
}
