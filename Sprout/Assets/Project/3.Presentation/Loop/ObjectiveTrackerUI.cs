using System.Collections.Generic;
using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.Presentation
{
    /// <summary>
    /// Da PROPÓSITO a la demo: muestra un objetivo en pantalla ("Conoce a los vecinos") que se va
    /// marcando solo cada vez que el jugador habla con un NPC nuevo. Convierte el mundo en un juego con
    /// meta. Ponlo en cualquier objeto de la escena de juego.
    /// </summary>
    public sealed class ObjectiveTrackerUI : MonoBehaviour
    {
        [SerializeField] private string objective = "Conoce a los vecinos del pueblo";
        [SerializeField] private string doneText = "¡Has conocido a todo el pueblo!";

        private readonly HashSet<string> _met = new();
        private int _total = 4;
        private GUIStyle _title, _line;

        private void Start()
        {
            int found = Object.FindObjectsByType<NPCBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            if (found > 0) _total = found;
        }

        private void Update()
        {
            var ui = DialogueUI.Active;
            if (ui != null && ui.CurrentNpc != null && !string.IsNullOrEmpty(ui.CurrentNpc.npcName))
                _met.Add(ui.CurrentNpc.npcName);
        }

        private void OnGUI()
        {
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.9f, 0.95f, 0.8f) } };
                _line = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = Color.white } };
            }

            bool done = _met.Count >= _total;
            string text = done ? doneText : $"{objective}   ({_met.Count}/{_total})";

            const float w = 360f;
            float x = Screen.width - w - 14f;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(new Rect(x, 14, w, 58), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 14, 20, w - 24, 18), "OBJETIVO", _title);
            GUI.Label(new Rect(x + 14, 40, w - 24, 24), text, _line);
        }
    }
}
