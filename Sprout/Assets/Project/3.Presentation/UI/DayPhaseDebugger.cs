using UnityEngine;
using UnityEngine.InputSystem;
using Sprout.Application;

namespace Sprout.Presentation
{
    /// <summary>
    /// DEBUG: pulsa una tecla para AVANZAR de fase (mañana → mediodía → tarde → noche → nuevo día) y así
    /// probar avisos como "deberías ir a dormir". Muestra arriba a la izquierda el día y la fase actual.
    /// Ponlo en cualquier objeto de la escena. Quítalo (o desactívalo) para la versión final.
    /// </summary>
    public sealed class DayPhaseDebugger : MonoBehaviour
    {
        [Tooltip("Tecla para avanzar de fase.")]
        [SerializeField] private Key advanceKey = Key.N;

        private DayCycleService _svc;
        private GUIStyle _style;

        private void Update()
        {
            if (_svc == null) _svc = FindFirstObjectByType<DayCycleService>(FindObjectsInactive.Include);
            var kb = Keyboard.current;
            if (kb == null || !kb[advanceKey].wasPressedThisFrame) return;

            if (_svc == null)
            {
                Debug.LogWarning("[DayDebug] no encuentro ningún DayCycleService en la escena.");
                return;
            }
            var before = SproutGameDirector.Instance?.Day?.Phase;
            _svc.AdvancePhase();
            var after = SproutGameDirector.Instance?.Day?.Phase;
            Debug.Log($"[DayDebug] {advanceKey} pulsada · fase {before} → {after}");
        }

        private void OnGUI()
        {
            var d = SproutGameDirector.Instance;
            string info = (d != null && d.Day != null)
                ? $"DEBUG · Día {d.Day.Day} · {d.Day.Phase}    [{advanceKey}] = avanzar fase"
                : $"DEBUG · (sin director aún)    [{advanceKey}] = avanzar fase";

            if (_style == null)
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            _style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(12f, 44f, 600f, 26f), info, _style);
        }
    }
}
