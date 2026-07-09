using UnityEngine;
using UnityEngine.InputSystem;
using Sprout.Application;
using Sprout.Domain.Endings;
using Sprout.Domain.Narrative;

namespace Sprout.Presentation
{
    /// <summary>
    /// DEPURACIÓN: fuerza cada uno de los cinco finales para sacarles foto, sin jugarlos.
    /// TECLAS (funcionan aunque el cursor esté bloqueado): 4 = Pueblo en flor · 5 = Raíces enredadas
    /// · 6 = Aceptación serena · 7 = Bonito pero hueco · 8 = Final secreto.
    /// Requiere que en la escena estén el EndingService (con sus EndingDefinitionSO en la lista
    /// 'endings') y un EndingScreenUI cuyo campo 'endingService' apunte a ese EndingService.
    /// Quítalo o desactívalo antes de entregar.
    /// </summary>
    public sealed class EndingDebugger : MonoBehaviour
    {
        [SerializeField] private EndingService endingService;
        private GUIStyle _title;

        private void Awake()
        {
            if (endingService == null) endingService = FindAnyObjectByType<EndingService>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.digit4Key.wasPressedThisFrame) Force(EndingKind.BloomingVillage);
            if (kb.digit5Key.wasPressedThisFrame) Force(EndingKind.TangledRoots);
            if (kb.digit6Key.wasPressedThisFrame) Force(EndingKind.QuietAcceptance);
            if (kb.digit7Key.wasPressedThisFrame) Force(EndingKind.PrettyButHollow);
            if (kb.digit8Key.wasPressedThisFrame) Force(EndingKind.SecretEnding);
        }

        private void Force(EndingKind k)
        {
            if (endingService == null) endingService = FindAnyObjectByType<EndingService>();
            SeedFlagsFor(k);
            if (endingService == null) { Debug.LogWarning("[EndingDebugger] No hay EndingService en la escena."); return; }
            endingService.ForceEnding(k);
        }

        private void SeedFlagsFor(EndingKind k)
        {
            var f = SproutGameDirector.Instance != null ? SproutGameDirector.Instance.Flags : null;
            if (f == null) return;
            switch (k)
            {
                case EndingKind.SecretEnding:
                    f.SetFlag(NarrativeFlagKeys.RixTrustsPlayer, true);
                    f.SetFlag(NarrativeFlagKeys.RixCuriosity, true);
                    break;
                case EndingKind.TangledRoots:
                    f.SetFlag(NarrativeFlagKeys.HelpedMothLie, true);
                    f.SetFlag(NarrativeFlagKeys.MochiOffended, true);
                    break;
                case EndingKind.QuietAcceptance:
                    f.SetFlag(NarrativeFlagKeys.PlayerWasHonest, true);
                    f.SetFlag(NarrativeFlagKeys.UnresolvedArgument, true);
                    break;
                case EndingKind.BloomingVillage:
                    f.SetFlag(NarrativeFlagKeys.PlayerWasHonest, true);
                    break;
                case EndingKind.PrettyButHollow:
                    break;
            }
        }

        private void OnGUI()
        {
            if (_title == null)
                _title = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };

            float x = Screen.width - 250, y = 12;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(new Rect(x - 10, y - 6, 246, 214), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, 236, 22), "FINALES · debug (teclas 4-8)", _title); y += 28;
            if (GUI.Button(new Rect(x, y, 226, 28), "4 · Pueblo en flor"))    Force(EndingKind.BloomingVillage); y += 32;
            if (GUI.Button(new Rect(x, y, 226, 28), "5 · Raíces enredadas"))  Force(EndingKind.TangledRoots);    y += 32;
            if (GUI.Button(new Rect(x, y, 226, 28), "6 · Aceptación serena")) Force(EndingKind.QuietAcceptance); y += 32;
            if (GUI.Button(new Rect(x, y, 226, 28), "7 · Bonito pero hueco")) Force(EndingKind.PrettyButHollow); y += 32;
            if (GUI.Button(new Rect(x, y, 226, 28), "8 · Final secreto"))     Force(EndingKind.SecretEnding);
        }
    }
}
