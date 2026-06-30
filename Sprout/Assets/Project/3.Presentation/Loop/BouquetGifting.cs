using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using OpenAI.Dialogue;
using Sprout.Application;
using Sprout.Domain.Flowers;
using Sprout.Domain.Narrative;
using ThresholdGame.Core.Interaction;
using ThresholdGame.Presentation.Interaction;

namespace Sprout.Presentation
{
    /// <summary>
    /// REGALAR un ramo a un vecino — cierra el bucle flores → ramo → regalo.
    /// Ponlo en el Player. Acércate a un NPC (que tenga NPCBrain) y, si tienes algún ramo hecho (con C),
    /// pulsa G para dárselo: se aplica su reacción (sube o baja la relación según el ramo y el personaje)
    /// y sale un aviso de cómo le ha sentado.
    /// </summary>
    public sealed class BouquetGifting : MonoBehaviour
    {
        [SerializeField] private FlowerService flowerService;
        [SerializeField] private string npcTag = "NPC";
        [Tooltip("Distancia a la que puedes regalar.")]
        [SerializeField] private float giftRadius = 3f;
        [SerializeField] private Key giveKey = Key.G;

        [Header("Animación de regalo (opcional)")]
        [Tooltip("De dónde sale el ramo (la mano de la florista). Si lo dejas vacío, sale de delante del Player.")]
        [SerializeField] private Transform handPoint;
        [SerializeField] private float giveDuration = 0.9f;
        [Tooltip("Altura del arco que describe el ramo al volar.")]
        [SerializeField] private float arcHeight = 0.8f;
        [SerializeField] private AudioClip giftSound;
        [Tooltip("Opcional: Animator del Player con un trigger de 'entregar'.")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string giveTrigger = "";

        private SproutGameDirector D => SproutGameDirector.Instance;
        private Transform[] _npcs;
        private float _scan;
        private NPCBrain _near;
        private string _toast;
        private float _toastT;
        private GUIStyle _hintStyle, _toastStyle;

        private void Awake()
        {
            if (flowerService == null) flowerService = FindFirstObjectByType<FlowerService>();
        }

        private void Update()
        {
            if (_toastT > 0f) _toastT -= Time.deltaTime;

            // Si hay un diálogo abierto, el regalo se hace con el botón "Dar ramo" del panel (y G escribiría 'g').
            var dlg = DialogueUI.Active;
            if (dlg != null && dlg.IsOpen) { _near = null; return; }

            _scan -= Time.deltaTime;
            if (_scan <= 0f) { _scan = 0.2f; _near = FindNearNpc(); }

            var kb = Keyboard.current;
            if (kb != null && _near != null && kb[giveKey].wasPressedThisFrame)
                TryGive(_near);
        }

        private NPCBrain FindNearNpc()
        {
            if (_npcs == null || _npcs.Length == 0)
            {
                GameObject[] gos;
                try { gos = GameObject.FindGameObjectsWithTag(npcTag); } catch { return null; }
                _npcs = new Transform[gos.Length];
                for (int i = 0; i < gos.Length; i++) _npcs[i] = gos[i].transform;
            }

            NPCBrain best = null;
            float bestD = giftRadius * giftRadius;
            foreach (var t in _npcs)
            {
                if (t == null) continue;
                float d = (t.position - transform.position).sqrMagnitude;
                if (d > bestD) continue;
                var b = t.GetComponentInChildren<NPCBrain>();
                if (b == null) continue;
                bestD = d; best = b;
            }
            return best;
        }

        private void TryGive(NPCBrain brain)
        {
            if (D == null || flowerService == null) { Toast("Falta el sistema de flores en la escena."); return; }
            if (!TryGetNpcId(brain, out var npc)) { Toast("No reconozco a este vecino (Mochi/Aster/Moth/Rix)."); return; }
            if (FirstBouquet() == BouquetKind.None) { Toast("No tienes ningún ramo. Haz uno con C."); return; }

            // Preferido: abrir la conversación (la cámara se acerca) y que el vecino reaccione en el CHAT.
            var trigger = brain.GetComponent<NPCInteractionTrigger>();
            var player = GetComponentInParent<IPlayerController>();
            if (trigger != null && player != null)
            {
                StartCoroutine(OpenAndGift(trigger, player));
                return;
            }

            // Fallback (sin sistema de interacción): regalo en el mundo, el ramo vuela + frase de agradecimiento.
            var bouquet = FirstBouquet();
            int before = D.Relationships.Get(npc);
            flowerService.GiveBouquetTo(bouquet, npc);
            int delta = D.Relationships.Get(npc) - before;
            StartCoroutine(GiftAnim(brain, bouquet, brain.npcName, GratitudeLine(npc, delta)));
        }

        private BouquetKind FirstBouquet()
        {
            if (D == null) return BouquetKind.None;
            foreach (var kv in D.Inventory.Bouquets) if (kv.Value > 0) return kv.Key;
            return BouquetKind.None;
        }

        /// <summary>Abre el diálogo con el vecino (la cámara se acerca) y, tras su saludo, le entrega el ramo
        /// para que reaccione en el chat normal con la IA.</summary>
        private IEnumerator OpenAndGift(NPCInteractionTrigger trigger, IPlayerController player)
        {
            trigger.Interact(player);                       // abre el chat + la cámara se acerca
            yield return new WaitForSeconds(0.15f);

            var dlg = DialogueUI.Active;
            float guard = 0f;
            while (dlg != null && dlg.IsBusy && guard < 8f) { guard += Time.deltaTime; yield return null; } // espera a que acabe el saludo
            if (dlg != null) dlg.GiveActiveBouquet();        // el NPC reacciona al ramo en el chat
        }

        /// <summary>El ramo 3D vuela de la mano de la florista al vecino; este se gira, da un botecito y suelta
        /// su frase de agradecimiento — todo sin tener que abrir el diálogo.</summary>
        private IEnumerator GiftAnim(NPCBrain brain, BouquetKind bouquet, string who, string line)
        {
            if (playerAnimator != null && !string.IsNullOrEmpty(giveTrigger))
                playerAnimator.SetTrigger(giveTrigger);
            if (giftSound != null)
                AudioSource.PlayClipAtPoint(giftSound, transform.position);

            var def = flowerService.DefOf(bouquet);
            GameObject model = def != null ? def.model : null;
            var npcT = brain.transform;

            Vector3 start = handPoint != null
                ? handPoint.position
                : transform.position + Vector3.up * 1.2f + transform.forward * 0.4f;

            GameObject inst;
            if (model != null)
            {
                inst = Instantiate(model, start, Quaternion.identity);
            }
            else
            {
                // Sin modelo (o FlowerService sin la lista asignada): ramo de repuesto para que SIEMPRE se vea volar.
                inst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = inst.GetComponent<Collider>(); if (col != null) Destroy(col);
                inst.transform.localScale = Vector3.one * 0.5f;
                var rr = inst.GetComponent<Renderer>();
                if (rr != null) rr.material.color = new Color(0.92f, 0.55f, 0.7f);
                inst.transform.position = start;
            }

            float t = 0f;
            while (t < giveDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / giveDuration);

                Vector3 end = npcT.position + Vector3.up * 1.0f; // sigue al vecino por si se mueve
                Vector3 p = Vector3.Lerp(start, end, k);
                p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;       // arquito
                inst.transform.position = p;
                inst.transform.Rotate(Vector3.up, 220f * Time.deltaTime, Space.World);

                // El vecino se gira a mirarte mientras le llega el ramo.
                Vector3 look = transform.position - npcT.position; look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    npcT.rotation = Quaternion.Slerp(npcT.rotation, Quaternion.LookRotation(look), Time.deltaTime * 8f);

                yield return null;
            }
            Destroy(inst);

            yield return StartCoroutine(Bounce(npcT));   // botecito de reacción
            Toast($"{who}: “{line}”");           // su frase de agradecimiento
        }

        /// <summary>Un botecito (squash & stretch) que no pelea con el movimiento del NPC.</summary>
        private static IEnumerator Bounce(Transform t)
        {
            if (t == null) yield break;
            Vector3 baseScale = t.localScale;
            float dur = 0.35f, e = 0f;
            while (e < dur)
            {
                e += Time.deltaTime;
                float k = e / dur;
                float s = 1f + 0.15f * Mathf.Sin(k * Mathf.PI);
                t.localScale = new Vector3(baseScale.x, baseScale.y * s, baseScale.z);
                yield return null;
            }
            t.localScale = baseScale;
        }

        /// <summary>Frase de agradecimiento en personaje, según le haya gustado o no el ramo.</summary>
        private static string GratitudeLine(NpcId npc, int delta)
        {
            bool good = delta > 0, bad = delta < 0;
            switch (npc)
            {
                case NpcId.Mochi:
                    return good ? "¡MAMMA MIA! É bellissimo! Grazie, grazie mille!!"
                         : bad  ? "...¿esto qué es? No, no me convence. Para nada."
                         :        "Ah... grazie. Lo pondré por ahí.";
                case NpcId.Aster:
                    return good ? "Oh... ¿para mí? No tenías que... g-gracias, de verdad."
                         : bad  ? "Ah. Vale. No pasa nada. (no pasa nada, no pasa nada)"
                         :        "Gracias... creo. Lo guardo.";
                case NpcId.Moth:
                    return good ? "Esto significa más de lo que crees. Lo guardaré para siempre."
                         : bad  ? "...no era esta la luz que esperaba sentir."
                         :        "Curioso. Lo aceptaré, por ahora.";
                case NpcId.Rix:
                    return good ? "...gracias. No esperaba esto. No se lo digas a nadie, ¿vale?"
                         : bad  ? "¿Y esto qué? Pff. No lo quiero."
                         :        "Ya. Vale. Gracias, supongo.";
                default:
                    return good ? "¡Gracias! Me encanta." : bad ? "...no, gracias." : "Gracias.";
            }
        }

        private static bool TryGetNpcId(NPCBrain brain, out NpcId id)
        {
            id = default;
            string n = brain != null ? brain.npcName : null;
            return !string.IsNullOrEmpty(n) && System.Enum.TryParse(n.Trim(), true, out id);
        }

        private void Toast(string s) { _toast = s; _toastT = 3.5f; }

        private static string PrettyBouquet(BouquetKind k) => k switch
        {
            BouquetKind.Peace        => "el Ramo de Paz",
            BouquetKind.HiddenDesire => "el Ramo de Deseo Oculto",
            BouquetKind.Comfort      => "el Ramo de Consuelo",
            BouquetKind.Obsession    => "el Ramo de Obsesión",
            BouquetKind.Promise      => "el Ramo de Promesa",
            BouquetKind.Confession   => "el Ramo de Confesión",
            BouquetKind.Farewell     => "el Ramo de Despedida",
            BouquetKind.Suspicion    => "el Ramo de Sospecha",
            _ => "un ramo"
        };

        private void OnGUI()
        {
            EnsureStyles();
            if (_toastT > 0f && !string.IsNullOrEmpty(_toast))
                GUI.Label(new Rect(Screen.width / 2f - 230f, Screen.height - 120f, 460f, 30f), _toast, _toastStyle);
            else if (_near != null)
                GUI.Label(new Rect(Screen.width / 2f - 180f, Screen.height - 90f, 360f, 24f),
                    $"Pulsa G para regalar un ramo a {_near.npcName}", _hintStyle);
        }

        private void EnsureStyles()
        {
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
                _hintStyle.normal.textColor = new Color(0.98f, 0.96f, 0.90f);
            }
            if (_toastStyle == null)
            {
                _toastStyle = new GUIStyle(GUI.skin.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 17, fontStyle = FontStyle.Bold };
                _toastStyle.normal.textColor = new Color(1f, 0.93f, 0.80f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.5f, 0.7f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, giftRadius);
        }
    }
}
