using UnityEngine;
using UnityEngine.UIElements;

namespace ThresholdGame.Presentation.UI.Loading
{
    /// <summary>
    /// Animación de pantalla de carga: un personaje cute (o unas patitas) camina hacia la
    /// derecha con rebotito y, al salir por el borde, vuelve a entrar por la izquierda (bucle).
    ///
    /// UI Toolkit no tiene animaciones infinitas en USS, así que el movimiento se hace desde C#:
    ///   - avanza en X con 'style.translate'
    ///   - rebota arriba/abajo (andar cute) con un seno
    ///   - se balancea un poco con 'style.rotate' (opcional)
    ///   - cambia de frame para animar las patitas (opcional, si le pasas varias imágenes)
    ///
    /// Uso: pon este componente en el objeto con el UIDocument de la pantalla de carga y en el
    /// UXML crea un VisualElement llamado "walker" con la imagen del personaje.
    /// Usa tiempo NO escalado (funciona aunque Time.timeScale = 0).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoadingWalker : MonoBehaviour
    {
        [Tooltip("Nombre (name) del VisualElement que se mueve en el UXML.")]
        [SerializeField] private string walkerName = "walker";

        [Tooltip("Velocidad horizontal en píxeles por segundo.")]
        [SerializeField] private float speed = 220f;

        [Header("Rebote (andar cute)")]
        [Tooltip("Cuánto sube al andar (px).")]
        [SerializeField] private float bobHeight = 16f;
        [Tooltip("Ritmo del rebote.")]
        [SerializeField] private float bobSpeed = 6f;
        [Tooltip("Balanceo lateral en grados (0 = sin balanceo).")]
        [SerializeField] private float tiltDegrees = 5f;

        [Header("Patitas (opcional): varios frames para el ciclo de andar")]
        [SerializeField] private Texture2D[] frames;
        [Tooltip("Segundos entre frame y frame.")]
        [SerializeField] private float frameInterval = 0.15f;

        private VisualElement _root, _walker;
        private float _x, _t, _frameTimer;
        private int _frame;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _walker = _root.Q<VisualElement>(walkerName);
            _x = -200f; // empieza fuera, por la izquierda
        }

        private void Update()
        {
            if (_walker == null) return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); // tope anti-saltos en picos de carga
            _t += dt;

            // Avanza a la derecha; al salir por el borde, vuelve a entrar por la izquierda.
            _x += speed * dt;
            float screenW = _root.resolvedStyle.width;
            float w = _walker.resolvedStyle.width;
            if (screenW > 0f && _x > screenW) _x = -w;

            // Rebote hacia arriba (valor absoluto del seno = "brinquitos").
            float bob = Mathf.Abs(Mathf.Sin(_t * bobSpeed)) * bobHeight;
            _walker.style.translate = new Translate(_x, -bob);

            // Balanceo suave de lado a lado.
            if (tiltDegrees != 0f)
                _walker.style.rotate = new Rotate(new Angle(Mathf.Sin(_t * bobSpeed) * tiltDegrees, AngleUnit.Degree));

            // Ciclo de patitas (si hay varios frames).
            if (frames != null && frames.Length > 1)
            {
                _frameTimer += dt;
                if (_frameTimer >= frameInterval)
                {
                    _frameTimer = 0f;
                    _frame = (_frame + 1) % frames.Length;
                    _walker.style.backgroundImage = new StyleBackground(frames[_frame]);
                }
            }
        }
    }
}
