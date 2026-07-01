using System.Collections.Generic;
using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Marca la MESA DE CRAFTEO. Ponlo en la mesa de la floristería. Mientras haya alguna CraftingStation
    /// en la escena, el panel de ramos (tecla C) SOLO se abre si estás cerca de una (lo comprueba
    /// SproutHotkeys). Si te alejas, se cierra solo. Así el crafteo solo se hace en la mesa.
    /// </summary>
    public sealed class CraftingStation : MonoBehaviour
    {
        private static readonly List<CraftingStation> _all = new();

        [Tooltip("Distancia a la que puedes craftear.")]
        [SerializeField] private float radius = 3f;

        private void OnEnable() { if (!_all.Contains(this)) _all.Add(this); }
        private void OnDisable() { _all.Remove(this); }

        /// <summary>True si existe al menos una mesa en la escena (entonces el crafteo se restringe).</summary>
        public static bool Any => _all.Count > 0;

        public static bool AnyNear(Transform player)
        {
            if (player == null) return false;
            foreach (var s in _all)
            {
                if (s == null) continue;
                if ((s.transform.position - player.position).sqrMagnitude <= s.radius * s.radius) return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.6f, 0.7f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
