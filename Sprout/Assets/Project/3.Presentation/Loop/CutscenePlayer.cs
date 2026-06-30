using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sprout.Presentation
{
    /// <summary>
    /// Cinemáticas sencillas moviendo la cámara por una serie de "planos". Cada plano es un objeto vacío
    /// colocado donde quieres la cámara; la cámara viaja suavemente de uno a otro. En cada plano puedes
    /// disparar un evento (animación del personaje, sonido, mostrar la carta, etc.).
    ///
    /// Ejemplos:
    ///  - Carta de la abuela: Plano1 = vista general + evento "personaje coge la carta" (animación PickUp);
    ///    Plano2 = cerca de la cara (lookAt = cabeza) para el zoom.
    ///  - Mochi probando la comida: planos acercándose + eventos que lanzan las animaciones de Mochi y su madre.
    ///
    /// Desactiva el control del jugador y la cámara normal durante la escena, y los restaura al acabar.
    /// </summary>
    public sealed class CutscenePlayer : MonoBehaviour
    {
        [Serializable]
        public class Shot
        {
            [Tooltip("Objeto vacío colocado donde quieres la cámara (posición + rotación).")]
            public Transform cameraPose;
            [Tooltip("Opcional: si lo asignas, la cámara MIRA a este objetivo (p. ej. la cabeza) en vez de usar la rotación del pose.")]
            public Transform lookAt;
            public float moveDuration = 1.5f;
            public float holdDuration = 1f;
            [Tooltip("Se dispara al empezar este plano: animación del personaje, sonido, activar la carta, etc.")]
            public UnityEvent onShotStart;
        }

        [SerializeField] private Camera cam;
        [SerializeField] private List<Shot> shots = new();
        [SerializeField] private bool playOnStart = false;
        [Tooltip("La cámara normal (FixedAngleCameraController) a desactivar durante la cinemática.")]
        [SerializeField] private MonoBehaviour cameraControllerToDisable;
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Se dispara al terminar la cinemática (devolver control, cargar escena, etc.).")]
        public UnityEvent onFinished;

        private bool _playing;

        private void Start() { if (playOnStart) Play(); }

        /// <summary>Lanza la cinemática (puedes llamarlo desde un trigger, un botón, un evento…).</summary>
        public void Play()
        {
            if (_playing) return;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _playing = true;
            if (cam == null) cam = Camera.main;
            if (cam == null) { _playing = false; yield break; }

            if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = false;
            SetPlayerControl(false);

            var camT = cam.transform;
            foreach (var shot in shots)
            {
                if (shot == null || shot.cameraPose == null) continue;
                shot.onShotStart?.Invoke();

                Vector3 fromPos = camT.position;
                Quaternion fromRot = camT.rotation;
                float t = 0f;
                while (t < shot.moveDuration)
                {
                    t += Time.deltaTime;
                    float k = shot.moveDuration > 0f ? Mathf.SmoothStep(0f, 1f, t / shot.moveDuration) : 1f;
                    camT.position = Vector3.Lerp(fromPos, shot.cameraPose.position, k);
                    Quaternion targetRot = shot.lookAt != null
                        ? Quaternion.LookRotation(shot.lookAt.position - camT.position)
                        : shot.cameraPose.rotation;
                    camT.rotation = Quaternion.Slerp(fromRot, targetRot, k);
                    yield return null;
                }
                camT.position = shot.cameraPose.position;
                camT.rotation = shot.lookAt != null
                    ? Quaternion.LookRotation(shot.lookAt.position - camT.position)
                    : shot.cameraPose.rotation;

                if (shot.holdDuration > 0f) yield return new WaitForSeconds(shot.holdDuration);
            }

            if (cameraControllerToDisable != null) cameraControllerToDisable.enabled = true;
            SetPlayerControl(true);
            onFinished?.Invoke();
            _playing = false;
        }

        private void SetPlayerControl(bool enabled)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p == null) return;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
            {
                var m = mb.GetType().GetMethod("SetControlEnabled");
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }
    }
}
