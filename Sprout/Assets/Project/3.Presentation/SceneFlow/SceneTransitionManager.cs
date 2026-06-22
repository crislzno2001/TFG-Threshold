using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Gestiona el cambio de escena estilo Animal Crossing: fundido a negro -> cargar escena destino ->
    /// recolocar al jugador en el SpawnPoint indicado -> fundido de entrada.
    ///
    /// - Es un singleton persistente (se crea solo la primera vez que una puerta lo pide).
    /// - Crea su propio lienzo de fundido a pantalla completa (no tienes que montar nada de UI).
    /// - Busca al jugador por TAG ("Player" por defecto). Asegúrate de que tu florista tiene ese tag.
    /// - Si el jugador usa CharacterController, lo desactiva un instante para teletransportarlo (si no,
    ///   el CharacterController ignora el cambio de posición).
    /// </summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private Color fadeColor = Color.black;

        private CanvasGroup _fade;
        private bool _busy;

        public static SceneTransitionManager GetOrCreate()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SceneTransitionManager");
            var m = go.AddComponent<SceneTransitionManager>();
            return m;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeCanvas();
        }

        /// <summary>Llamado por una puerta. Carga 'sceneName' y coloca al jugador en el SpawnPoint 'spawnId'.</summary>
        public void Go(string sceneName, string spawnId)
        {
            if (_busy || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(Transition(sceneName, spawnId));
        }

        private IEnumerator Transition(string sceneName, string spawnId)
        {
            _busy = true;
            SetPlayerControl(false);

            yield return Fade(1f);                       // a negro

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneTransition] No pude cargar '{sceneName}'. " +
                               "¿Está añadida en File > Build Settings?");
                yield return Fade(0f);
                SetPlayerControl(true);
                _busy = false;
                yield break;
            }
            while (!op.isDone) yield return null;
            yield return null;                            // 1 frame para que despierten los objetos

            MovePlayerToSpawn(spawnId);

            yield return Fade(0f);                        // de negro a visible
            SetPlayerControl(true);
            _busy = false;
        }

        private void MovePlayerToSpawn(string spawnId)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) { Debug.LogWarning($"[SceneTransition] No encontré jugador con tag '{playerTag}'."); return; }

            SpawnPoint target = null;
            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                if (sp.id == spawnId) { target = sp; break; }
            if (target == null)
            {
                Debug.LogWarning($"[SceneTransition] No hay SpawnPoint con id '{spawnId}' en la escena.");
                return;
            }

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            if (cc != null) cc.enabled = true;
        }

        // Desactiva el movimiento del jugador durante el fundido (usa ILocomotionProvider si existe).
        private void SetPlayerControl(bool enabled)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
            {
                var m = mb.GetType().GetMethod("SetControlEnabled");
                if (m != null) { m.Invoke(mb, new object[] { enabled }); }
            }
        }

        private IEnumerator Fade(float target)
        {
            float start = _fade.alpha, t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _fade.alpha = Mathf.Lerp(start, target, t / fadeDuration);
                yield return null;
            }
            _fade.alpha = target;
            _fade.blocksRaycasts = target > 0.5f;
        }

        private void BuildFadeCanvas()
        {
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasGo.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var img = imgGo.AddComponent<Image>();
            img.color = fadeColor;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            _fade = imgGo.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            _fade.interactable = false;
        }
    }
}
