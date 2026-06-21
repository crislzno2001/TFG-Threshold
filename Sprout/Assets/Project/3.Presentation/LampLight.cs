using UnityEngine;
using Sprout.Application;
using Sprout.Domain.DayCycle;

namespace Sprout.Presentation
{
    /// <summary>
    /// A warm lamppost light that turns ON at Evening/Night and OFF during the day,
    /// with a gentle flicker — cozy pools of light at night. Reads the current phase
    /// from the game director (no event-timing issues). Put it on a child Point Light
    /// of the lamppost (the "Add Lamp Lights" tool does this for you).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LampLight : MonoBehaviour
    {
        [SerializeField] private Light lamp;
        [SerializeField] private float nightIntensity = 3.2f;
        [SerializeField] private Color color = new Color(1f, 0.78f, 0.45f);
        [SerializeField] private float flicker = 0.12f;

        private float _seed;

        private void Awake()
        {
            if (lamp == null) lamp = GetComponent<Light>();
            if (lamp != null) { lamp.color = color; lamp.intensity = 0f; }
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            if (lamp == null) return;

            var d = SproutGameDirector.Instance;
            bool on = d != null && (d.Day.Phase == DayPhase.Evening || d.Day.Phase == DayPhase.Night);

            float wobble = 1f + (Mathf.PerlinNoise(Time.time * 2.2f, _seed) - 0.5f) * 2f * flicker;
            float target = on ? nightIntensity * wobble : 0f;

            lamp.intensity = Mathf.Lerp(lamp.intensity, target, Time.deltaTime * 4f);
            lamp.enabled = lamp.intensity > 0.03f;
        }
    }
}
