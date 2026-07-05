using System.Collections;
using UnityEngine;

namespace Sprout.Presentation
{
    /// <summary>
    /// Intro del coche: el coche RECORRE el pueblo (de un punto a otro) con la florista sentada dentro,
    /// mientras habláis (ScriptedDialogue). Es como el tour del pueblo, pero en un coche que se mueve.
    ///
    /// Montaje:
    ///  - 'car' = el objeto del coche. Pon el/la conductor(a) como HIJO del coche, ya sentado (puedes
    ///    usar otro CharacterSitController en 'sit' al arrancar, o dejarlo posado).
    ///  - 'playerSeat' = un objeto vacío HIJO del coche donde se sienta la florista.
    ///  - 'waypoints' = 2+ puntos por los que pasa el coche, en orden.
    ///  - 'dialogue' = el ScriptedDialogue con las frases del conductor (y las opciones de sabor).
    /// Al acabar la ruta y el diálogo, baja a la florista y lanza onFinished (p. ej. el tour del pueblo).
    /// </summary>
    public sealed class CarIntroRide : MonoBehaviour
    {
        [Header("Coche y ruta")]
        [SerializeField] private Transform car;
        [Tooltip("Puntos por los que pasa el coche, en orden (mínimo 2).")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float rideSeconds = 12f;
        [Tooltip("Giro para alinear el FRENTE del coche con el movimiento. Si tu coche mira por el eje X " +
                 "(flecha roja), prueba -90 o 90 hasta que vaya de frente.")]
        [SerializeField] private float carYawOffset = -90f;

        [Header("Player sentado")]
        [SerializeField] private string playerTag = "Player";
        [Tooltip("Asiento del player: un objeto vacío HIJO del coche. La florista se sienta aquí y viaja con el coche.")]
        [SerializeField] private Transform playerSeat;
        [Tooltip("Dónde aparece la florista al BAJAR del coche (un objeto vacío FUERA del coche, en el suelo). " +
                 "Si lo dejas vacío se queda dentro y el collider del coche la atrapa.")]
        [SerializeField] private Transform exitPoint;

        [Header("Cámara")]
        [Tooltip("Offset de la cámara respecto al coche (en su espacio local). Lateral para verlo de perfil.")]
        [SerializeField] private Vector3 camOffset = new Vector3(6f, 3f, 0f);
        [SerializeField] private float camLookHeight = 1.2f;

        [Header("Diálogo (opcional)")]
        [SerializeField] private ScriptedDialogue dialogue;

        [Header("Ocultar durante el viaje")]
        [Tooltip("Objetos de HUD que se ESCONDEN mientras dura la intro (relaciones, objetivo, minimapa…) y vuelven al bajar.")]
        [SerializeField] private GameObject[] hideDuringRide;

        [Header("Reproducir")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool playOnce = true;
        [SerializeField] private string saveKey = "intro_coche";

        public UnityEngine.Events.UnityEvent onFinished;

        private Camera _cam;
        private bool _running;

        private void Start() { if (playOnStart) StartCoroutine(AutoPlay()); }

        private IEnumerator AutoPlay()
        {
            float t = 0f;
            while (Sprout.Application.SproutGameDirector.Instance == null && t < 3f)
            { t += Time.unscaledDeltaTime; yield return null; }
            yield return null;
            Play();
        }

        [ContextMenu("Reset 'ya visto' (volver a probar)")]
        private void ResetSeen()
        {
            var d = Sprout.Application.SproutGameDirector.Instance;
            if (d != null && d.Flags != null) { d.Flags.SetFlag(saveKey, false); Debug.Log("[CarIntro] reseteado."); }
            else Debug.Log("[CarIntro] el reset solo funciona en Play. Para probar, desmarca 'Play Once'.");
        }

        public void Play()
        {
            if (_running) return;
            var d = Sprout.Application.SproutGameDirector.Instance;
            if (playOnce && d != null && d.Flags != null && d.Flags.GetFlag(saveKey))
            {
                Debug.Log("[CarIntro] SALTADO: ya se vio en esta partida.");
                return;
            }
            if (car == null || waypoints == null || waypoints.Length < 2)
            {
                Debug.LogWarning("[CarIntro] falta 'car' o hay menos de 2 waypoints.");
                return;
            }
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            _running = true;

            // Bloquea el movimiento del player (WASD) y libera el cursor durante TODO el viaje.
            SetPlayerControl(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Esconde el HUD durante la intro.
            if (hideDuringRide != null)
                foreach (var go in hideDuringRide) if (go != null) go.SetActive(false);

            // Sentar a la florista dentro del coche (viaja con él al hacerla hija del coche).
            var pgo = GameObject.FindGameObjectWithTag(playerTag);
            var sitter = pgo != null ? pgo.GetComponentInChildren<CharacterSitController>() : null;
            Transform pt = pgo != null ? pgo.transform : null;
            if (pt != null && playerSeat != null)
            {
                pt.SetParent(car, true);
                if (sitter != null) sitter.SitAt(playerSeat, false, true);   // bloqueado: no se levanta a mitad del viaje
                else pt.SetPositionAndRotation(playerSeat.position, playerSeat.rotation);
            }

            // Cámara de la intro (por encima de la principal).
            _cam = new GameObject("CarIntroCamera").AddComponent<Camera>();
            _cam.depth = 100f;

            car.position = waypoints[0].position;
            UpdateCam();

            if (dialogue != null) dialogue.Play();

            // Recorre los waypoints repartiendo el tiempo total.
            int segs = waypoints.Length - 1;
            float perSeg = Mathf.Max(0.1f, rideSeconds / segs);
            for (int i = 0; i < segs; i++)
            {
                Vector3 a = waypoints[i].position, b = waypoints[i + 1].position;
                Vector3 dir = (b - a).sqrMagnitude > 0.0001f ? (b - a).normalized : car.forward;
                Quaternion faceB = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, carYawOffset, 0f);
                float t = 0f;
                while (t < perSeg)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / perSeg));
                    car.position = Vector3.Lerp(a, b, k);
                    car.rotation = Quaternion.Slerp(car.rotation, faceB, Time.deltaTime * 3f);
                    UpdateCam();
                    yield return null;
                }
            }

            // Si el diálogo sigue, espera a que acabe (el coche ya está parado).
            while (dialogue != null && dialogue.IsPlaying) { UpdateCam(); yield return null; }

            // Baja a la florista: la saca del coche (al exitPoint), la levanta y devuelve el control.
            if (pt != null)
            {
                pt.SetParent(null, true);
                if (sitter != null) sitter.StandUp();   // reactiva su CharacterController

                if (exitPoint != null)
                {
                    var cc = pt.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;                 // para teletransportar sin colisión
                    pt.SetPositionAndRotation(exitPoint.position, exitPoint.rotation);
                    if (cc != null) cc.enabled = true;
                }
            }
            if (_cam != null) Destroy(_cam.gameObject);
            SetPlayerControl(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Vuelve a mostrar el HUD.
            if (hideDuringRide != null)
                foreach (var go in hideDuringRide) if (go != null) go.SetActive(true);

            var dir2 = Sprout.Application.SproutGameDirector.Instance;
            if (playOnce && dir2 != null && dir2.Flags != null) dir2.Flags.SetFlag(saveKey, true);

            _running = false;
            onFinished?.Invoke();
        }

        private void SetPlayerControl(bool enabled)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p == null) return;
            foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb == null) continue;
                var m = mb.GetType().GetMethod("SetControlEnabled", new[] { typeof(bool) });
                if (m != null) m.Invoke(mb, new object[] { enabled });
            }
        }

        private void UpdateCam()
        {
            if (_cam == null || car == null) return;
            _cam.transform.position = car.position + car.rotation * camOffset;
            Vector3 look = car.position + Vector3.up * camLookHeight;
            _cam.transform.rotation = Quaternion.LookRotation(look - _cam.transform.position, Vector3.up);
        }
    }
}
