using System.Collections.Generic;
using UnityEngine;

namespace ThresholdGame.Infraestructure.NPC
{
    [System.Serializable]
    public class DestinationEntry
    {
        [Tooltip("Alias con el que el jugador nombra este punto. Ej: 'foto', 'puerta', 'mesa'")]
        public string id;
        public Transform destination;
    }

    /// <summary>
    /// Registro de puntos de destino de la escena.
    /// Traduce el alias de texto que dice el jugador al Transform real de la escena.
    /// Un objeto global por escena, todos los NPCs lo comparten.
    /// Añade destinos desde el Inspector sin tocar código.
    /// </summary>
    public class SceneDestinationRegistry : MonoBehaviour
    {
        [SerializeField] private List<DestinationEntry> destinations = new();

        private Dictionary<string, Transform> _map;

        private void Awake() => BuildMap();

        private void BuildMap()
        {
            _map = new();
            foreach (var entry in destinations)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.destination == null)
                    continue;

                string key = entry.id.Trim().ToLowerInvariant();
                if (!_map.TryAdd(key, entry.destination))
                    Debug.LogWarning($"[DestinationRegistry] ID duplicado: '{key}'", this);
            }
        }

        public Transform GetDestination(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            _map.TryGetValue(id.Trim().ToLowerInvariant(), out var t);
            return t;
        }
    }
}