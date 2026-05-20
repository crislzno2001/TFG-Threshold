namespace ThresholdGame.Core.Settings
{
    /// <summary>
    /// Contrato para persistir y recuperar los ajustes del jugador.
    /// Permite cambiar la implementación (PlayerPrefs, JSON, cloud) sin
    /// afectar al SettingsService ni a la UI.
    /// </summary>
    public interface ISettingsRepository
    {
        GameSettings Load();
        void Save(GameSettings settings);
    }
}
