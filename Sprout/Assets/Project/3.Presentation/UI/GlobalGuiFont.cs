using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Aplica UNA fuente (Fredoka) a TODO el texto IMGUI (OnGUI) del juego de una sola vez: carteles
    /// ("¡Una nota!"), la carta, el recuadro de tu cara, el aviso de dormir, los HUD por código, etc.
    ///
    /// Cómo funciona: se ejecuta ANTES que cualquier otro OnGUI (execution order muy negativo) y fija
    /// GUI.skin.font. Como casi todos los estilos IMGUI del proyecto no fijan fuente propia, heredan esta.
    ///
    /// Uso: ponlo en un objeto que esté SIEMPRE en escena (p. ej. el mismo persistente que el NpcSpotlight)
    /// y arrástrale tu Fredoka-Medium.ttf en el campo 'Font'.
    /// </summary>
    [DefaultExecutionOrder(-30000)]
    public sealed class GlobalGuiFont : MonoBehaviour
    {
        [Tooltip("Arrastra Fredoka-Medium.ttf (Assets/Project/UI/Fonts/Fredoka/static/).")]
        [SerializeField] private Font font;

        private void OnGUI()
        {
            if (font != null) GUI.skin.font = font;
        }
    }
}
