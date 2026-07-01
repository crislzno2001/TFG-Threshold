using UnityEngine;
using ThresholdGame.Application.Settings;

namespace Sprout.Presentation
{
    /// <summary>
    /// Multiplicador global de tamaño de texto, leído de la configuración (SettingsService.Current.TextScale).
    /// Los paneles de texto (carta, tutorial, diálogo guionizado) multiplican su fontSize por esto, así el
    /// ajuste "tamaño de letra" del menú de config afecta a todo. Si no hay Settings, devuelve 1.
    /// </summary>
    public static class SproutTextScale
    {
        public static float Get()
        {
            var s = SettingsService.Instance;
            return (s != null && s.Current != null) ? Mathf.Clamp(s.Current.TextScale, 0.5f, 3f) : 1f;
        }
    }
}
