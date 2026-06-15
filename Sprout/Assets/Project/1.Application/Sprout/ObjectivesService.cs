using UnityEngine;
using TMPro;
using Sprout.Domain.Narrative;

namespace Sprout.Application
{
    /// <summary>
    /// Shows the player a single, current objective (driven by flags + day) so they
    /// always know what to do next. Updates whenever the narrative state changes.
    /// </summary>
    public class ObjectivesService : MonoBehaviour
    {
        [SerializeField] private TMP_Text objectiveText;

        private SproutGameDirector D => SproutGameDirector.Instance;

        private void Start()
        {
            if (D != null) D.Flags.OnChanged += OnChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (D != null) D.Flags.OnChanged -= OnChanged;
        }

        private void OnChanged(string key) => Refresh();

        private bool F(string k) => D != null && D.Flags.GetFlag(k);

        private void Refresh()
        {
            if (objectiveText == null) return;
            objectiveText.text = "Objetivo: " + CurrentGoal();
        }

        private string CurrentGoal()
        {
            if (D == null) return "Explora el pueblo.";

            bool metAll = F(NarrativeFlagKeys.MochiMet) && F(NarrativeFlagKeys.AsterMet)
                       && F(NarrativeFlagKeys.MothKnown) && F(NarrativeFlagKeys.RixKnown);

            if (!metAll)
                return "Acércate a tus vecinos y pulsa E para presentarte.";

            if (F(NarrativeFlagKeys.AsterMet) && !F(NarrativeFlagKeys.AsterSecretKnown))
                return "Dale ideas a Aster para su invento — cuantas más y más raras, mejor.";

            if (F(NarrativeFlagKeys.MochiMet) && !F(NarrativeFlagKeys.MochiMasterpiece))
                return "Propón recetas a Mochi en su cocina (y decide si eres sincero o amable).";

            if (F(NarrativeFlagKeys.MothAskedForHelp) && !F(NarrativeFlagKeys.HelpedMothLie)
                && !F(NarrativeFlagKeys.RixTrustsPlayer))
                return "Moth te ha pedido algo sobre Rix: decide qué clase de vecino quieres ser.";

            if (F(NarrativeFlagKeys.MothKnown) && !F(NarrativeFlagKeys.MothAskedForHelp))
                return "Gánate la confianza de Moth para saber qué esconde.";

            if (D.Inventory != null && D.Inventory.Flowers.Count > 0)
                return "Pulsa C para combinar dos flores en un ramo y regálalo a un vecino.";

            return "Pulsa R para descansar: por la noche el pueblo cotillea y el día cambia.";
        }
    }
}
