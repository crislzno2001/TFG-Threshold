using System;
using UnityEngine;
using OpenAI.Dialogue;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation
{
    /// <summary>
    /// Indicador flotante sobre un NPC que brilla según su GlowState (none/soft/strong/referral/red).
    /// Crea una esferita luminosa sobre la cabeza, la billboardea a cámara y la hace flotar/pulsar.
    /// Cero arte necesario. Ponlo en cada NPC.
    ///
    /// - Detecta su propio NpcId desde el NPCBrain de su mismo objeto (no hace falta configurarlo).
    /// - Hace de PUENTE flags→spotlight: escucha al brain de SU objeto y traduce los flags de los grafos
    ///   (glow_&lt;npc&gt;_strong/soft/red/none, recommend_&lt;npc&gt;) al NpcSpotlight.
    /// - LEE el estado del spotlight cada frame (polling): así el brillo siempre refleja la verdad,
    ///   sin depender del timing de los eventos.
    /// </summary>
    public sealed class NpcGlow : MonoBehaviour
    {
        [SerializeField] private NpcId npc;
        [SerializeField] private float height = 2.4f;
        [SerializeField] private float size = 0.28f;
        [Tooltip("Compensa la curva del mundo para que la bola siga sobre la cabeza aunque el NPC esté lejos.")]
        [SerializeField] private float curveCompensation = 1f;
        [Tooltip("Radio cerca de la florista donde NO compensa (para que de cerca no sobre). Prueba 3-5.")]
        [SerializeField] private float curveFlatRadius = 0f;

        [Header("Colores por estado")]
        [SerializeField] private Color soft = new Color(1f, 0.98f, 0.85f, 1f);
        [SerializeField] private Color strong = new Color(0.55f, 0.85f, 0.55f, 1f);
        [SerializeField] private Color referral = new Color(0.5f, 0.75f, 1f, 1f);
        [SerializeField] private Color red = new Color(1f, 0.45f, 0.45f, 1f);

        private NpcSpotlight _spot;
        private NPCBrain _brain;
        private Transform _orb;
        private Material _mat;
        private Camera _cam;
        private float _t;
        private GlowState _applied = GlowState.None;

        private void Start()
        {
            _brain = GetComponent<NPCBrain>();
            if (_brain == null) _brain = GetComponentInParent<NPCBrain>();

            // Detecta su NpcId desde el brain (evita que el campo quede mal puesto en el inspector).
            if (_brain != null && Enum.TryParse<NpcId>(_brain.npcName, true, out var parsed))
                npc = parsed;

            BuildOrb();
            _spot = NpcSpotlight.Instance;
            Apply(_spot != null ? _spot.GetGlow(npc) : GlowState.None);

            // Puente flags→spotlight: escuchamos al brain de ESTE mismo NPC (mismo GameObject).
            if (_brain != null) _brain.OnFlagSet += OnBrainFlag;
        }

        private void OnDestroy()
        {
            if (_brain != null) _brain.OnFlagSet -= OnBrainFlag;
        }

        // ── Puente FLAGS → SPOTLIGHT (los pone flagsOnEnter en los grafos) ──
        private void OnBrainFlag(string flag, bool value)
        {
            if (!value || string.IsNullOrEmpty(flag)) return;
            var spot = NpcSpotlight.Instance;
            if (spot == null) return;
            flag = flag.Trim().ToLowerInvariant();

            if (flag.StartsWith("recommend_"))
            {
                if (TryNpc(flag.Substring("recommend_".Length), out var who)) spot.Recommend(who);
                return;
            }
            if (flag.StartsWith("glow_"))
            {
                string rest = flag.Substring("glow_".Length);   // "<npc>_<estado>"
                int us = rest.LastIndexOf('_');
                if (us <= 0) return;
                if (!TryNpc(rest.Substring(0, us), out var who)) return;
                switch (rest.Substring(us + 1))
                {
                    case "strong":   spot.SetGlow(who, GlowState.Strong); break;
                    case "soft":     spot.SetGlow(who, GlowState.Soft); break;
                    case "red":      spot.SetGlow(who, GlowState.Red); break;
                    case "referral": spot.Recommend(who); break;
                    case "none":     spot.SetGlow(who, GlowState.None); break;
                }
            }
        }

        private static bool TryNpc(string name, out NpcId id) => Enum.TryParse(name, true, out id);

        // ── Botones de PRUEBA (clic derecho en el componente, en Play) ──
        [ContextMenu("TEST · Strong (verde)")]   private void TestStrong()   => Test(GlowState.Strong);
        [ContextMenu("TEST · Referral (azul)")]  private void TestReferral() => Test(GlowState.Referral);
        [ContextMenu("TEST · Red (rojo)")]        private void TestRed()      => Test(GlowState.Red);
        [ContextMenu("TEST · Soft (suave)")]      private void TestSoft()     => Test(GlowState.Soft);
        [ContextMenu("TEST · None (apagar)")]     private void TestNone()     => Test(GlowState.None);

        private void Test(GlowState s)
        {
            var sp = NpcSpotlight.Instance;
            if (sp != null) sp.SetGlow(npc, s); else Apply(s);
        }

        private void Apply(GlowState s)
        {
            _applied = s;
            if (_orb == null) BuildOrb();
            if (_orb != null) _orb.gameObject.SetActive(s != GlowState.None);
            if (_mat != null)
            {
                Color c = ColorFor(s);
                _mat.color = c;
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
            }
        }

        private Color ColorFor(GlowState s) => s switch
        {
            GlowState.Soft => soft,
            GlowState.Strong => strong,
            GlowState.Referral => referral,
            GlowState.Red => red,
            _ => Color.white
        };

        private void BuildOrb()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "GlowOrb";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, height, 0);
            go.transform.localScale = Vector3.one * size;

            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            _mat = new Material(sh);
            go.GetComponent<Renderer>().sharedMaterial = _mat;

            _orb = go.transform;
            go.SetActive(false);
        }

        private void LateUpdate()
        {
            // Fuente única de verdad: el spotlight. Leemos su estado cada frame (polling), así no
            // dependemos de que el evento OnGlowChanged se suscribiera a tiempo.
            if (_spot == null) _spot = NpcSpotlight.Instance;
            if (_spot != null)
            {
                GlowState target = _spot.GetGlow(npc);
                if (target != _applied) Apply(target);
            }

            if (_orb == null || !_orb.gameObject.activeSelf) return;

            _t += Time.deltaTime;
            float curveY = CurvedWorldCompensation.OffsetY(transform.position, curveCompensation, curveFlatRadius); // sigue la curva del mundo
            _orb.localPosition = new Vector3(0, height + Mathf.Sin(_t * 2f) * 0.08f + curveY, 0); // flotar + compensar curva
            _orb.localScale = Vector3.one * size * (1f + Mathf.Sin(_t * 4f) * 0.12f);          // pulso

            if (_cam == null) _cam = Camera.main;
            if (_cam != null) _orb.forward = (_orb.position - _cam.transform.position).normalized; // billboard
        }
    }
}
