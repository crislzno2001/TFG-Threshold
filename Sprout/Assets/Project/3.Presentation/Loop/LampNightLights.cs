using System.Collections.Generic;
using UnityEngine;
using Sprout.Application;
using Sprout.Domain.DayCycle;

namespace Sprout.Presentation
{
    /// <summary>
    /// Enciende las luces de las farolas SOLO por la noche (fases Evening y Night) y las apaga de día.
    /// Se engancha al ciclo de día del SproutGameDirector. Recoge todas las luces creadas por la
    /// herramienta "Light Up Lamp Posts" (GameObjects llamados "LampLight").
    /// </summary>
    public sealed class LampNightLights : MonoBehaviour
    {
        [SerializeField] private string lampLightName = "LampLight";

        private readonly List<Light> _lamps = new();
        private DayCycleState _day;

        private void Start()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l != null && l.gameObject.name == lampLightName) _lamps.Add(l);

            var d = SproutGameDirector.Instance;
            if (d != null && d.Day != null)
            {
                _day = d.Day;
                _day.OnPhaseChanged += OnPhase;
                Apply(_day.Phase);
            }
            else
            {
                // Sin ciclo de día en la escena: las dejamos apagadas (es de día por defecto).
                Apply(DayPhase.Morning);
            }
        }

        private void OnDestroy()
        {
            if (_day != null) _day.OnPhaseChanged -= OnPhase;
        }

        private void OnPhase(int day, DayPhase phase) => Apply(phase);

        private void Apply(DayPhase phase)
        {
            bool on = phase == DayPhase.Afternoon || phase == DayPhase.Night;
            foreach (var l in _lamps)
                if (l != null) l.enabled = on;
        }
    }
}
