using UnityEngine;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation
{
    /// <summary>
    /// Indicador flotante sobre un NPC que brilla según su GlowState (none/soft/strong/referral/red).
    /// Crea una esferita luminosa sobre la cabeza, la billboardea a cámara y la hace flotar/pulsar.
    /// Cero arte necesario. Ponlo en cada NPC y elige su NpcId.
    /// </summary>
    public sealed class NpcGlow : MonoBehaviour
    {
        [SerializeField] private NpcId npc;
        [SerializeField] private float height = 2.4f;
        [SerializeField] private float size = 0.28f;

        [Header("Colores por estado")]
        [SerializeField] private Color soft = new Color(1f, 0.98f, 0.85f, 1f);
        [SerializeField] private Color strong = new Color(0.55f, 0.85f, 0.55f, 1f);
        [SerializeField] private Color referral = new Color(0.5f, 0.75f, 1f, 1f);
        [SerializeField] private Color red = new Color(1f, 0.45f, 0.45f, 1f);

        private NpcSpotlight _spot;
        private Transform _orb;
        private Material _mat;
        private Camera _cam;
        private float _t;

        private void Start()
        {
            BuildOrb();
            Subscribe();
            Apply(_spot != null ? _spot.GetGlow(npc) : GlowState.None);
        }

        private void OnDestroy()
        {
            if (_spot != null) _spot.OnGlowChanged -= OnChanged;
        }

        private void Subscribe()
        {
            if (_spot == null) _spot = NpcSpotlight.Instance;
            if (_spot != null) _spot.OnGlowChanged += OnChanged;
        }

        private void OnChanged(NpcId who, GlowState s) { if (who == npc) Apply(s); }

        private void Apply(GlowState s)
        {
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
            if (_spot == null) { Subscribe(); if (_spot != null) Apply(_spot.GetGlow(npc)); }
            if (_orb == null || !_orb.gameObject.activeSelf) return;

            _t += Time.deltaTime;
            _orb.localPosition = new Vector3(0, height + Mathf.Sin(_t * 2f) * 0.08f, 0);      // flotar
            _orb.localScale = Vector3.one * size * (1f + Mathf.Sin(_t * 4f) * 0.12f);          // pulso

            if (_cam == null) _cam = Camera.main;
            if (_cam != null) _orb.forward = (_orb.position - _cam.transform.position).normalized; // billboard
        }
    }
}
