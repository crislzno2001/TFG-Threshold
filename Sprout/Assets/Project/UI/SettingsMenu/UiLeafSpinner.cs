using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThresholdGame.Presentation.UI.Settings
{
    /// <summary>
    /// Gira lentamente elementos de UI Toolkit (p. ej. hojas decorativas de un menú).
    ///
    /// IMPORTANTE: UI Toolkit NO tiene animaciones infinitas en USS (no hay @keyframes).
    /// Por eso el giro continuo se hace desde C#: cada frame actualizamos 'style.rotate'.
    ///
    /// Uso:
    ///   1) En el UXML, marca las hojas que quieras girar con la clase indicada (por defecto "spin-leaf").
    ///   2) Pon este componente en el mismo objeto que el UIDocument del menú.
    /// Usa tiempo NO escalado, así gira aunque el juego esté en pausa (Time.timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiLeafSpinner : MonoBehaviour
    {
        [Tooltip("Clase USS de los elementos que deben girar.")]
        [SerializeField] private string leafClass = "spin-leaf";

        [Tooltip("Grados por segundo (negativo = sentido contrario).")]
        [SerializeField] private float degreesPerSecond = 18f;

        [Tooltip("Si está activo, cada hoja gira a una velocidad ligeramente distinta (más natural).")]
        [SerializeField] private bool variedSpeeds = true;

        private readonly List<VisualElement> _leaves = new();
        private readonly List<float> _speeds = new();
        private float _t;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _leaves.Clear();
            _speeds.Clear();

            root.Query(className: leafClass).ForEach(e =>
            {
                _leaves.Add(e);
                float mul = variedSpeeds ? Random.Range(0.6f, 1.4f) : 1f;
                _speeds.Add(degreesPerSecond * mul);
            });
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            for (int i = 0; i < _leaves.Count; i++)
            {
                float angle = _t * _speeds[i];
                _leaves[i].style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
            }
        }
    }
}
