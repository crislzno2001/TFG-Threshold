using UnityEngine;
using Sprout.Application;

namespace Sprout.Presentation
{
    /// <summary>
    /// Muestra la tarjeta de receta de Mochi cuando se activa una flag concreta (p. ej.
    /// "mochi_recipe_day1"). Ponlo en cualquier objeto de la escena y rellena flag + texto.
    /// Se apoya en NarrativeFlagStore.OnChanged del SproutGameDirector.
    /// </summary>
    public sealed class ShowIngredientCardOnFlag : MonoBehaviour
    {
        [Tooltip("Flag que, al ponerse a true, muestra la tarjeta.")]
        public string flag = "mochi_recipe_day1";
        public string title = "Receta de hoy";
        [TextArea] public string ingredient = "Champiñón lunar";

        private bool _shown;

        private void Start()
        {
            var d = SproutGameDirector.Instance;
            if (d != null && d.Flags != null) d.Flags.OnChanged += OnFlagChanged;
        }

        private void OnDestroy()
        {
            var d = SproutGameDirector.Instance;
            if (d != null && d.Flags != null) d.Flags.OnChanged -= OnFlagChanged;
        }

        private void OnFlagChanged(string key)
        {
            if (_shown) return;
            var d = SproutGameDirector.Instance;
            if (d == null || d.Flags == null) return;
            if (d.Flags.GetFlag(flag))
            {
                _shown = true;
                IngredientCardUI.GetOrCreate().Show(title, ingredient);
            }
        }
    }
}
