using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThresholdGame.Presentation.UI.Loading
{
    /// <summary>
    /// Pantalla de carga con PARALLAX: varias capas de paisaje que se deslizan a distinta
    /// velocidad (las de atrás lentas, las de delante rápidas) en bucle infinito.
    ///
    /// Cómo funciona el bucle sin cortes: cada capa usa una imagen que se REPITE en horizontal
    /// (background-repeat: repeat-x en el USS). Solo movemos su 'background-position-x' cada frame;
    /// como la imagen se repite, nunca se ve el borde. No hace falta duplicar nada.
    ///
    /// REQUISITO: la imagen de cada capa debe ser "tileable" en horizontal (que el borde izquierdo
    /// encaje con el derecho), o se verá un salto al repetir.
    ///
    /// Uso: pon este componente en el objeto con el UIDocument de la pantalla de carga y rellena la
    /// lista de capas con el 'name' de cada VisualElement del UXML y su velocidad.
    /// Usa tiempo NO escalado (funciona con Time.timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ParallaxScroller : MonoBehaviour
    {
        [Serializable]
        public struct Layer
        {
            [Tooltip("name del VisualElement de esta capa en el UXML.")]
            public string elementName;

            [Tooltip("Velocidad en px/seg. Cuanto más lejana la capa, más lenta. " +
                     "Positivo = el paisaje va a la izquierda (sensación de avanzar a la derecha).")]
            public float speed;
        }

        [Tooltip("De atrás (cielo, lento) a delante (suelo, rápido).")]
        [SerializeField] private Layer[] layers;

        private readonly List<VisualElement> _elems = new();
        private float[] _offsets;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _elems.Clear();
            _offsets = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                _elems.Add(root.Q<VisualElement>(layers[i].elementName));
        }

        private void Update()
        {
            // Topamos el delta: si un frame tarda mucho (pico de carga), el paisaje
            // avanza suave en vez de pegar un salto ("teletransporte").
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            for (int i = 0; i < _elems.Count; i++)
            {
                var e = _elems[i];
                if (e == null) continue;

                _offsets[i] -= layers[i].speed * dt; // restar = el fondo se desplaza a la izquierda
                float w = e.resolvedStyle.width;
                if (w > 0f) _offsets[i] %= w;         // mantener el valor pequeño (visualmente idéntico al repetir)

                e.style.backgroundPositionX =
                    new BackgroundPosition(BackgroundPositionKeyword.Left, _offsets[i]);
            }
        }
    }
}
