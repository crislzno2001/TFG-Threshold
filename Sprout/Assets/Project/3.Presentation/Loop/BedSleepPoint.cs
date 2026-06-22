using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sprout.Application;
using Sprout.Domain.DayCycle;

namespace Sprout.Presentation
{
    /// <summary>
    /// Punto para DORMIR (cama, saco, cartel "descansar"). Ponlo en un objeto con Collider (Is Trigger).
    /// Cuando el jugador está dentro y pulsa E:
    ///   1) funde a negro,
    ///   2) avanza el ciclo hasta la Noche (el gossip nocturno corre solo y genera el resumen del día),
    ///   3) muestra el RECAP con ese resumen,
    ///   4) al pulsar "Continuar", pasa a la mañana siguiente y funde de vuelta.
    ///
    /// Así se cierra el bucle: hablas de día -> duermes -> ves lo que pasó -> al día siguiente reaccionan.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BedSleepPoint : MonoBehaviour
    {
        [Header("Activación")]
        public string playerTag = "Player";
        public KeyCode interactKey = KeyCode.E;

        [Header("Referencias (se autocompletan si las dejas vacías)")]
        [SerializeField] private DayCycleService dayCycle;
        [SerializeField] private NightGossipService gossip;

        private bool _inside, _busy;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) { if (other.CompareTag(playerTag)) _inside = true; }
        private void OnTriggerExit(Collider other) { if (other.CompareTag(playerTag)) _inside = false; }

        private void Update()
        {
            if (_inside && !_busy && Input.GetKeyDown(interactKey))
                StartCoroutine(SleepRoutine());
        }

        private IEnumerator SleepRoutine()
        {
            _busy = true;
            if (dayCycle == null) dayCycle = FindFirstObjectByType<DayCycleService>();
            if (gossip == null) gossip = FindFirstObjectByType<NightGossipService>();

            var D = SproutGameDirector.Instance;
            var recap = NightRecapUI.GetOrCreate();

            // Capturamos el resumen que emite el gossip cuando entramos en la Noche.
            List<string> lines = null;
            UnityAction<List<string>> capture = ls => lines = ls;
            if (gossip != null) gossip.onNightSummary.AddListener(capture);

            SetPlayerControl(false);
            yield return recap.FadeIn();

            // Avanzar fases hasta la Noche (el DayCycleService dispara el gossip al llegar a Night).
            int guard = 0;
            while (D != null && D.Day != null && D.Day.Phase != DayPhase.Night && !D.Day.IsFinished && guard++ < 12)
                dayCycle?.AdvancePhase();

            if (gossip != null) gossip.onNightSummary.RemoveListener(capture);

            int day = (D != null && D.Day != null) ? D.Day.Day : 1;
            bool cont = false;
            recap.ShowContent(day, lines, () => cont = true);
            while (!cont) yield return null;

            // Pasar a la mañana del día siguiente (Noche -> roll over).
            dayCycle?.AdvancePhase();

            yield return recap.FadeOut();
            SetPlayerControl(true);
            _busy = false;
        }

        // Activa/desactiva el control del jugador usando su SetControlEnabled (si existe), como el resto del juego.
        private void SetPlayerControl(bool enabled)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
            {
                var m = mb.GetType().GetMethod("SetControlEnabled");
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }
    }
}
