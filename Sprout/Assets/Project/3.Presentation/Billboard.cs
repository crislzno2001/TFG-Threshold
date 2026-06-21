using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Hace que un objeto (p. ej. la etiqueta de nombre de un NPC) mire siempre a la
    /// cámara, para que el texto se lea de frente y nunca del revés. Se pone en el
    /// propio objeto del texto.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _cam;

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // Alineamos el "delante" del texto con el "delante" de la cámara: así el
            // plano del texto queda paralelo a la pantalla y siempre legible.
            transform.forward = _cam.transform.forward;
        }
    }
}
