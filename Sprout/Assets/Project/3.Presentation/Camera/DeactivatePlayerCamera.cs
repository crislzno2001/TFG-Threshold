using UnityEngine;
using UnityEngine.InputSystem;

namespace Sprout.Presentation
{
    public sealed class DeactivatePlayerCamera : MonoBehaviour
    {
        [Tooltip("Tag del personaje a seguir. Se busca solo cuando entra en la escena.")]
        [SerializeField] private string targetTag = "Player";
      
        [SerializeField] private Transform target;


        [Header("Cámara del player")]
        [Tooltip("Apaga la cámara que trae el player mientras estás en esta escena (y la reactiva al salir).")]
        [SerializeField] private bool disablePlayerCamera = true;


        private GameObject _playerCamGo;   // la cámara del player que apagamos

        private void LateUpdate()
        {
            if (target == null)
            {
                AcquireTarget();
                if (target == null) return;   // el player aún no ha entrado
            } }

        
        private void AcquireTarget()
        {
            var go = GameObject.FindWithTag(targetTag);
            if (go == null) return;
            target = go.transform;


            if (disablePlayerCamera)
            {
                var myCam = GetComponent<Camera>();
                var playerCam = go.GetComponentInChildren<Camera>(true);
                if (playerCam != null && playerCam != myCam)
                {
                    _playerCamGo = playerCam.gameObject;
                    _playerCamGo.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            // Al salir de la Casa (se descarga la escena) devolvemos la cámara del player.
            if (_playerCamGo != null) _playerCamGo.SetActive(true);
        }
    }
}
