using UnityEngine;
using Sprout.Application;

namespace Sprout.Presentation
{
    /// <summary>
    /// Cozy day/night atmosphere: smoothly shifts the directional light, ambient,
    /// soft shadows, warm distance fog AND the skybox (exposure + tint) between
    /// Morning, Afternoon, Evening and Night. The skybox is dimmed/tinted at night
    /// (a runtime copy, so your skybox asset isn't modified), so the sky stops
    /// looking like permanent daytime. Pair with lamp lights for cozy pools of light.
    /// </summary>
    public class DayNightLighting : MonoBehaviour
    {
        [SerializeField] private Light sun;
        [SerializeField] private DayCycleService dayCycle;
        [SerializeField] private float blendSpeed = 1.2f;
        [SerializeField] private bool useFog = true;
        [SerializeField] private bool controlSkybox = true;

        private Color _color = Color.white;
        private float _intensity = 1.0f;
        private Quaternion _rot = Quaternion.Euler(52, -20, 0);
        private Color _ambient = new Color(0.50f, 0.50f, 0.46f);
        private Color _fog = new Color(0.80f, 0.84f, 0.78f);
        private float _fogDensity = 0.006f;

        private Material _sky;
        private float _skyExposure = 1.1f;
        private Color _skyTint = new Color(0.5f, 0.5f, 0.5f);

        private void Start()
        {
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
            }
            if (sun != null)
            {
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.55f; // soft but with presence
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            if (useFog) { RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; }

            // Runtime copy of the skybox so we can dim/tint it without touching the asset.
            if (controlSkybox && RenderSettings.skybox != null)
            {
                _sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = _sky;
            }

            if (dayCycle != null) dayCycle.onPhaseChanged.AddListener(OnPhase);

            OnPhase(1, "Morning");
            ApplyImmediate();
        }

        private void OnPhase(int day, string phase)
        {
            switch (phase)
            {
                case "Morning":   Set(new Color(1f, 0.95f, 0.84f), 1.00f, new Vector3(52, -20, 0), new Color(0.50f, 0.50f, 0.46f), new Color(0.80f, 0.84f, 0.78f), 0.006f); SetSky(1.05f, new Color(0.50f, 0.51f, 0.52f)); break;
                case "Afternoon": Set(new Color(1f, 0.97f, 0.89f), 1.10f, new Vector3(70, 10, 0),  new Color(0.56f, 0.56f, 0.53f), new Color(0.84f, 0.87f, 0.82f), 0.005f); SetSky(1.20f, new Color(0.52f, 0.53f, 0.55f)); break;
                case "Evening":   Set(new Color(1f, 0.70f, 0.44f), 0.80f, new Vector3(40, 40, 0),  new Color(0.40f, 0.35f, 0.33f), new Color(0.78f, 0.58f, 0.42f), 0.009f); SetSky(0.70f, new Color(0.55f, 0.40f, 0.32f)); break;
                case "Night":     Set(new Color(0.40f, 0.48f, 0.82f), 0.28f, new Vector3(45, -30, 0), new Color(0.14f, 0.16f, 0.26f), new Color(0.09f, 0.12f, 0.22f), 0.014f); SetSky(0.22f, new Color(0.22f, 0.27f, 0.45f)); break;
            }
        }

        private void Set(Color c, float i, Vector3 rot, Color amb, Color fog, float fogDensity)
        {
            _color = c; _intensity = i; _rot = Quaternion.Euler(rot);
            _ambient = amb; _fog = fog; _fogDensity = fogDensity;
        }

        private void SetSky(float exposure, Color tint) { _skyExposure = exposure; _skyTint = tint; }

        private void ApplyImmediate()
        {
            if (sun != null) { sun.color = _color; sun.intensity = _intensity; sun.transform.rotation = _rot; }
            RenderSettings.ambientLight = _ambient;
            RenderSettings.fogColor = _fog;
            RenderSettings.fogDensity = _fogDensity;
            ApplySky(1f);
        }

        private void ApplySky(float t)
        {
            if (_sky == null) return;
            if (_sky.HasProperty("_Exposure"))
                _sky.SetFloat("_Exposure", Mathf.Lerp(_sky.GetFloat("_Exposure"), _skyExposure, t));
            if (_sky.HasProperty("_Tint"))
                _sky.SetColor("_Tint", Color.Lerp(_sky.GetColor("_Tint"), _skyTint, t));
        }

        private void Update()
        {
            float t = blendSpeed * Time.deltaTime;
            if (sun != null)
            {
                sun.color = Color.Lerp(sun.color, _color, t);
                sun.intensity = Mathf.Lerp(sun.intensity, _intensity, t);
                sun.transform.rotation = Quaternion.Slerp(sun.transform.rotation, _rot, t);
            }
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, _ambient, t);
            if (useFog)
            {
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, _fog, t);
                RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, _fogDensity, t);
            }
            ApplySky(t);
        }
    }
}
