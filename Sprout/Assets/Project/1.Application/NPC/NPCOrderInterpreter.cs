using System.Text.RegularExpressions;

namespace ThresholdGame.Application.NPC
{
    public enum NPCOrderType
    {
        None,
        Follow,
        Stop,
        MoveToDestination,
        ReturnHome
    }

    public sealed class NPCOrder
    {
        public static readonly NPCOrder None = new(NPCOrderType.None, null);
        public NPCOrderType Type { get; }
        public string DestinationId { get; }
        public bool IsOrder => Type != NPCOrderType.None;

        public NPCOrder(NPCOrderType type, string destinationId = null)
        {
            Type = type;
            DestinationId = destinationId;
        }
    }

    /// <summary>
    /// Interpreta el mensaje del jugador y detecta si es una orden para el NPC.
    ///
    /// Solo detecta órdenes cuando son CLARAS:
    /// - El mensaje debe ser corto (≤ 8 palabras) — las frases largas son conversación, no órdenes.
    /// - Los patrones requieren contexto (no solo palabras sueltas).
    /// - Para movimiento, también acepta "me traes X" o "tráeme X" si X está registrado.
    /// </summary>
    public sealed class NPCOrderInterpreter
    {
        // ── Órdenes de seguimiento (frases cortas e imperativas) ─────────────
        private static readonly Regex FollowPattern = new(
            @"^(?:venga,?\s+)?(sígueme|sigueme|acompáñame|acompañame|ven\s+(?:aquí|aqui|conmigo))\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ── Órdenes de parar (imperativas, no "para X") ──────────────────────
        // OJO: "para" solo se detecta cuando va sola o seguida de signos.
        private static readonly Regex StopPattern = new(
            @"^(detente|quédate\s+(?:aquí|aqui|quieto|quieta)|quedate\s+(?:aquí|aqui|quieto|quieta)|quieto|quieta|no\s+te\s+muevas|espérame\s+aquí|esperame\s+aqui)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ── Órdenes de volver ────────────────────────────────────────────────
        private static readonly Regex ReturnPattern = new(
            @"^(vuelve\s+(?:a\s+tu\s+sitio|a\s+casa|atrás|atras)|regresa\s+(?:a\s+tu\s+sitio|a\s+casa))\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ── Mover a un destino: "ve a X", "muévete hacia X", "dirígete a X" ──
        private static readonly Regex MoveToPattern = new(
            @"\b(?:ve|muévete|muevete|dirígete|dirigete|acércate|acercate)\s+(?:a|al|hacia|hasta)\s+(?:la\s+|el\s+|los\s+|las\s+)?(\w+(?:\s+\w+)?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ── "Tráeme X" / "Me traes X" / "Puedes traerme X" ───────────────────
        private static readonly Regex BringPattern = new(
            @"\b(?:tráeme|traeme|me\s+traes|puedes\s+traerme|tráete|me\s+puedes\s+traer)\s+(?:la\s+|el\s+|los\s+|las\s+|un\s+|una\s+)?(\w+(?:\s+\w+)?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public NPCOrder Interpret(string playerMessage)
        {
            if (string.IsNullOrWhiteSpace(playerMessage)) return NPCOrder.None;

            string trimmed = playerMessage.Trim();

            // Si el mensaje es muy largo, casi seguro no es una orden directa
            // sino una propuesta o conversación.
            int wordCount = trimmed.Split(' ').Length;
            if (wordCount > 8) return NPCOrder.None;

            if (FollowPattern.IsMatch(trimmed))
                return new NPCOrder(NPCOrderType.Follow);

            if (StopPattern.IsMatch(trimmed))
                return new NPCOrder(NPCOrderType.Stop);

            if (ReturnPattern.IsMatch(trimmed))
                return new NPCOrder(NPCOrderType.ReturnHome);

            // "ve a X" — orden directa de movimiento
            var moveMatch = MoveToPattern.Match(trimmed);
            if (moveMatch.Success)
            {
                string id = moveMatch.Groups[1].Value.Trim().ToLowerInvariant();
                return new NPCOrder(NPCOrderType.MoveToDestination, id);
            }

            // "tráeme la sal" — petición de movimiento hacia el objeto
            var bringMatch = BringPattern.Match(trimmed);
            if (bringMatch.Success)
            {
                string id = bringMatch.Groups[1].Value.Trim().ToLowerInvariant();
                return new NPCOrder(NPCOrderType.MoveToDestination, id);
            }

            return NPCOrder.None;
        }
    }
}