namespace ThresholdGame.Core.Settings
{
    /// <summary>
    /// Modelo de datos puros para los ajustes del juego.
    /// Sin dependencias de Unity ni infraestructura.
    /// </summary>
    public sealed class GameSettings
    {
        // Volúmenes en rango lineal [0..1].
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume  { get; set; } = 1f;
        public float SfxVolume    { get; set; } = 1f;
        public float AiVoiceVolume { get; set; } = 1f;

        // Multiplicador de tamaño de texto (1 = base, 1.5 = 150%).
        public float TextScale { get; set; } = 1f;

        public static GameSettings Default => new();
    }
}
