using UnityEngine;
using UnityEngine.InputSystem;
using Sprout.Application;
using Sprout.Domain.Flowers;

namespace Sprout.Presentation
{
    /// <summary>
    /// Flor recogible en el mundo. Ponlo en un modelo de flor por el suelo. Al acercarte puedes recogerla
    /// (pulsando E, o automáticamente si marcas Auto Pickup) y se añade al inventario. Así repartes flores
    /// por el pueblo para que la jugadora las coja.
    /// </summary>
    public sealed class PickableFlower : MonoBehaviour
    {
        [SerializeField] private FlowerKind kind = FlowerKind.Sol;
        [SerializeField] private int amount = 1;
        [SerializeField] private float radius = 1.8f;
        [Tooltip("Si está marcado, se recoge sola al tocarla; si no, hay que pulsar E.")]
        [SerializeField] private bool autoPickup = false;
        [SerializeField] private Key pickupKey = Key.E;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private AudioClip pickupSound;
        [Tooltip("Pequeño giro/flotación para que llame la atención.")]
        [SerializeField] private bool bob = true;

        private Transform _player;
        private bool _near;
        private Vector3 _basePos;
        private SproutGameDirector D => SproutGameDirector.Instance;

        private void Start() => _basePos = transform.position;

        private void Update()
        {
            if (bob)
            {
                transform.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
                transform.position = _basePos + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.08f + 0.08f);
            }

            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag(playerTag);
                if (p == null) return;
                _player = p.transform;
            }

            _near = (_player.position - transform.position).sqrMagnitude <= radius * radius;
            if (!_near) return;

            var kb = Keyboard.current;
            if (autoPickup || (kb != null && kb[pickupKey].wasPressedThisFrame))
                Pick();
        }

        private void Pick()
        {
            if (D != null && D.Inventory != null) D.Inventory.AddFlower(kind, amount);
            if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (!_near || autoPickup) return;
            var style = new GUIStyle(GUI.skin.label)
            { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(0.98f, 0.96f, 0.90f);
            GUI.Label(new Rect(Screen.width / 2f - 160f, Screen.height - 150f, 320f, 24f), $"E · recoger {kind}", style);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.5f, 0.7f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
