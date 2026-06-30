using System.Collections;
using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Música de fondo + ambiente (pájaros/naturaleza) en bucle, que persiste entre escenas. Es lo que
    /// más "vida" le da a un juego cozy. Ponlo en un objeto de la escena inicial y asigna los clips.
    /// Suena en 2D (siempre audible) y entra con un fundido suave.
    /// </summary>
    public sealed class BackgroundMusic : MonoBehaviour
    {
        public static BackgroundMusic Instance { get; private set; }

        [Header("Clips (asigna al menos la música)")]
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambient;

        [Header("Volumen")]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.5f;
        [SerializeField] private float fadeInDuration = 2f;

        private AudioSource _music, _ambient;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            _music = CreateSource(music, 0f);
            _ambient = CreateSource(ambient, 0f);

            if (music != null) StartCoroutine(FadeIn(_music, musicVolume));
            if (ambient != null) StartCoroutine(FadeIn(_ambient, ambientVolume));
        }

        private AudioSource CreateSource(AudioClip clip, float vol)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D, siempre audible
            src.volume = vol;
            if (clip != null) src.Play();
            return src;
        }

        private IEnumerator FadeIn(AudioSource src, float target)
        {
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(0f, target, t / fadeInDuration);
                yield return null;
            }
            src.volume = target;
        }
    }
}
