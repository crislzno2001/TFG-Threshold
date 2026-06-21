using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Posa el objeto sobre el suelo al arrancar: tira un rayo hacia abajo y mueve el
    /// objeto para que el punto MÁS BAJO de su malla toque el suelo (así no flota ni se
    /// hunde, sea cual sea la altura del terreno donde lo coloques).
    /// Necesita que el suelo tenga collider.
    /// </summary>
    public class GroundSnap : MonoBehaviour
    {
        [Tooltip("Capas que cuentan como suelo. Pon aquí tu suelo/terreno si quieres precisión.")]
        [SerializeField] private LayerMask ground = ~0;

        [Tooltip("Si quieres dejarlo un pelín por encima o por debajo del suelo.")]
        [SerializeField] private float extraOffset = 0f;

        private void Start() => Snap();

        // Clic derecho en la cabecera del componente (en el Inspector) → posar ahora,
        // en el editor, sin darle a Play. Queda guardado al guardar la escena.
        [ContextMenu("Posar en el suelo ahora")]
        public void Snap()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            // Caja que envuelve toda la malla del personaje (para saber su punto más bajo).
            Bounds box = renderers[0].bounds;
            foreach (var r in renderers) box.Encapsulate(r.bounds);

            // Rayo desde arriba del personaje hacia abajo.
            Vector3 origin = new Vector3(transform.position.x, box.max.y + 2f, transform.position.z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 200f, ground, QueryTriggerInteraction.Ignore);

            bool found = false;
            float groundY = 0f;
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(transform)) continue; // ignora el propio personaje
                if (!found || h.point.y > groundY) { groundY = h.point.y; found = true; }
            }
            if (!found) return;

            // Subimos/bajamos el objeto para que su punto más bajo quede en el suelo.
            float lift = (groundY + extraOffset) - box.min.y;
            transform.position += new Vector3(0f, lift, 0f);
        }
    }
}
