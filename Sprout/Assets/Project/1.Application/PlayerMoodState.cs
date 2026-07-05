namespace Sprout.Application
{
    /// <summary>
    /// Ánimo/expresión ACTUAL de la florista, elegido por la jugadora al tocar su foto en el diálogo.
    /// Es un puente sencillo entre la Presentación (que lo fija) y la IA (que lo lee al responder),
    /// sin acoplar capas. Vacío = neutral (no se le dice nada especial a la IA).
    ///
    /// Ejemplos de valor: "enfadada", "triste", "coqueta", "ilusionada"… (lo define la etiqueta de cada
    /// emoción en DialogueFaceCam). Persiste hasta que la jugadora elige otra expresión.
    /// </summary>
    public static class PlayerMoodState
    {
        /// <summary>Etiqueta del ánimo actual (o "" si neutral).</summary>
        public static string Current { get; set; } = "";

        public static bool HasMood => !string.IsNullOrWhiteSpace(Current);

        public static void Clear() => Current = "";
    }
}
