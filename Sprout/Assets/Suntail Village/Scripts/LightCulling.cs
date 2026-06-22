

using UnityEngine;

/*Script to disable lighting and shadows 
when moving away at a set distance*/
namespace Suntail
{
    public class LightCulling : MonoBehaviour
    {
        [SerializeField] private GameObject playerCamera;
        [SerializeField] private float shadowCullingDistance = 15f;
        [SerializeField] private float lightCullingDistance = 30f;
        private Light _light;
        public bool enableShadows = false;

        private void Awake()
        {
            _light = GetComponent<Light>();

            // Si no se asignó la cámara en el inspector, usar la cámara principal (tag MainCamera).
            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main.gameObject;
        }

        private void Update()
        {
            // Sin cámara no hay nada que calcular: evita el NullReferenceException.
            if (playerCamera == null) return;

            //Calculate the distance between a given object and the light source
            float cameraDistance = Vector3.Distance(playerCamera.transform.position, gameObject.transform.position);

            if (cameraDistance <= shadowCullingDistance && enableShadows)
            {
                _light.shadows = LightShadows.Soft;
            }
            else
            {
                _light.shadows = LightShadows.None;
            }

            if (cameraDistance <= lightCullingDistance)
            {
                _light.enabled = true;
            }
            else
            {
                _light.enabled = false;
            }
        }
    }
}
