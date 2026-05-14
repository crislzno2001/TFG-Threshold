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
    /// Clase pura de C# que analiza el texto del jugador con expresiones regulares
    /// y devuelve la orden detectada.
    /// Si en el futuro quieres usar OpenAI function calling en vez de regex,
    /// solo cambias esta clase.
    /// </summary>
    public sealed class NPCOrderInterpreter
    {
        private static readonly Regex FollowPattern = new(
            @"\b(ven|sígueme|sigueme|acompáñame|acompañame|ven\s+aquí|ven\s+aqui)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex StopPattern = new(
            @"\b(para|detente|quédate|quedate|quieto|espera|no\s+te\s+muevas)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ReturnPattern = new(
            @"\b(vuelve|regresa|ve\s+a\s+tu\s+sitio|vuelve\s+a\s+tu\s+sitio)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Captura el alias del destino: "ve a la foto", "muévete hacia la puerta"
        private static readonly Regex MoveToPattern = new(
            @"\b(?:ve|muévete|muevete|dirígete|dirigete|acércate|acercate)\s+(?:a|al|hacia|hasta)\s+(?:la\s+|el\s+|los\s+|las\s+)?(\w+(?:\s+\w+)?)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public NPCOrder Interpret(string playerMessage)
        {
            if (string.IsNullOrWhiteSpace(playerMessage)) return NPCOrder.None;

            if (FollowPattern.IsMatch(playerMessage))
                return new NPCOrder(NPCOrderType.Follow);

            if (StopPattern.IsMatch(playerMessage))
                return new NPCOrder(NPCOrderType.Stop);

            if (ReturnPattern.IsMatch(playerMessage))
                return new NPCOrder(NPCOrderType.ReturnHome);

            var match = MoveToPattern.Match(playerMessage);
            if (match.Success)
            {
                string id = match.Groups[1].Value.Trim().ToLowerInvariant();
                return new NPCOrder(NPCOrderType.MoveToDestination, id);
            }

            return NPCOrder.None;
        }
    }
}