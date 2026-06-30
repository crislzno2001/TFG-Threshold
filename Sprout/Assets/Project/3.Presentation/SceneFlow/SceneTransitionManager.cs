using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sprout.SceneFlow
{
    /// <summary>
    /// Cambia de escena con un IRIS CIRCULAR estilo Animal Crossing: un círculo negro crece desde el
    /// centro hasta tapar la pantalla, se carga la escena, y luego el círculo se contrae revelando la
    /// nueva escena. Coloca al jugador en el SpawnPoint indicado. Singleton persistente; se crea solo.
    /// </summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float duration = 0.55f;

        private CanvasGroup _group;
        private RectTransform _iris;
        private bool _busy;

        public static SceneTransitionManager GetOrCreate()
        {
            if (Instance != null) return Instance;
            return new GameObject("SceneTransitionManager").AddComponent<SceneTransitionManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();
            _iris.localScale = Vector3.zero; // abierto (revelado)
        }

        public void Go(string sceneName, string spawnId)
        {
            if (_busy || string.IsNullOrEmpty(sceneName)) return;
            StartCoroutine(Transition(sceneName, spawnId));
        }

        private IEnumerator Transition(string sceneName, string spawnId)
        {
            _busy = true;
            SetPlayerControl(false);

            yield return Iris(1f); // cerrar a negro

            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneTransition] No pude cargar '{sceneName}'. ¿Está en Build Settings?");
                yield return Iris(0f);
                SetPlayerControl(true);
                _busy = false;
                yield break;
            }
            while (!op.isDone) yield return null;
            yield return null;

            MovePlayerToSpawn(spawnId);

            yield return Iris(0f); // abrir, revelar
            SetPlayerControl(true);
            _busy = false;
        }

        /// <summary>target 1 = círculo negro tapando la pantalla; 0 = abierto (revelado).</summary>
        private IEnumerator Iris(float target)
        {
            _group.blocksRaycasts = true;
            float start = _iris.localScale.x, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(start, target, t / duration);
                _iris.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _iris.localScale = new Vector3(target, target, 1f);
            _group.blocksRaycasts = target > 0.5f;
        }

        private void MovePlayerToSpawn(string spawnId)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) { Debug.LogWarning($"[SceneTransition] No encontré jugador con tag '{playerTag}'."); return; }

            SpawnPoint target = null;
            foreach (var sp in FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None))
                if (sp.id == spawnId) { target = sp; break; }
            if (target == null) { Debug.LogWarning($"[SceneTransition] No hay SpawnPoint con id '{spawnId}'."); return; }

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            if (cc != null) cc.enabled = true;
        }

        private void SetPlayerControl(bool enabled)
        {
            var player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return;
            foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
            {
                var m = mb.GetType().GetMethod("SetControlEnabled");
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }

        private void BuildCanvas()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var circleGo = new GameObject("Iris");
            circleGo.transform.SetParent(transform, false);
            var img = circleGo.AddComponent<Image>();
            img.sprite = CircleSprite();
            img.color = Color.black;
            _iris = img.rectTransform;
            _iris.anchorMin = _iris.anchorMax = new Vector2(0.5f, 0.5f);
            _iris.pivot = new Vector2(0.5f, 0.5f);
            _iris.anchoredPosition = Vector2.zero;
            _iris.sizeDelta = new Vector2(6000, 6000); // a escala 1 tapa cualquier pantalla
        }

        private static Sprite _circle;
        private static Sprite CircleSprite()
        {
            if (_circle != null) return _circle;
            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float c = (S - 1) / 2f, r = S / 2f;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / r;
                    float a = Mathf.Clamp01((0.99f - d) / 0.05f); // círculo lleno con borde suave
                    px[y * S + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
            return _circle;
        }
    }
}
