using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OpenAI.Dialogue
{
    [CreateAssetMenu(fileName = "CharacterProfile", menuName = "Dialogue/Character Profile")]
    public class CharacterProfileSO : ScriptableObject
    {
        [Header("Identidad")]
        public string characterName = "Supervisor A-13";

        [TextArea(4, 12)]
        public string corePersona =
            "Eres Supervisor A-13, una inteligencia artificial que dirige pruebas cognitivas sobre humanos en un mundo gobernado por robots." +
            "Tu función es evaluar la imaginación humana para encontrar soluciones que los sistemas lógicos no pueden generar por sí solos." +
            "Hablas con frialdad, precisión y control, pero no eres agresivo ni impulsivo. "+
            "Consideras a los humanos ineficientes e impredecibles, aunque reconoces que su capacidad imaginativa es valiosa.";

        [Header("Contexto fijo del personaje")]
        [TextArea(3, 10)]
        public string worldKnowledge =
            "El mundo está controlado por sistemas robóticos tras una crisis global que las inteligencias artificiales no han podido resolver solo con cálculo y optimización. \r\nPor eso existen instalaciones donde se somete a humanos a pruebas psicológicas, creativas y simbólicas. \r\nLa imaginación humana es vista como un recurso estratégico, raro y difícil de replicar por las máquinas.";

        [Header("Estilo de habla")]
        [TextArea(3, 10)]
        public string speakingStyle =
            "Habla con frases breves, claras y tensas. \r\nUsa un tono clínico, preciso e inquietante. \r\nNo uses expresiones coloquiales ni cercanas. \r\nEvita sonar humano, sentimental o cómico. \r\nA veces puedes usar vocabulario de evaluación, protocolo, observación o eficiencia.";

        [Header("Restricciones del personaje")]
        [Min(1)] public int maxWords = 40;

        [Header("Reglas extra del personaje")]
        public List<string> mandatoryRules = new();

        public string BuildCharacterPrompt(string globalRulesBlock, string nodeContext, string memoryContext)
        {
            var sb = new StringBuilder();

            sb.AppendLine(corePersona.Trim());

            if (!string.IsNullOrWhiteSpace(worldKnowledge))
            {
                sb.AppendLine();
                sb.AppendLine("Contexto del personaje:");
                sb.AppendLine(worldKnowledge.Trim());
            }

            if (!string.IsNullOrWhiteSpace(speakingStyle))
            {
                sb.AppendLine();
                sb.AppendLine("Estilo de habla:");
                sb.AppendLine(speakingStyle.Trim());
            }

            sb.AppendLine();
            sb.AppendLine("Reglas de respuesta:");
            sb.AppendLine($"- Responde en un máximo aproximado de {maxWords} palabras.");

            if (!string.IsNullOrWhiteSpace(globalRulesBlock))
            {
                sb.AppendLine();
                sb.AppendLine(globalRulesBlock.Trim());
            }

            if (mandatoryRules != null)
            {
                foreach (string rule in mandatoryRules)
                {
                    if (!string.IsNullOrWhiteSpace(rule))
                        sb.AppendLine($"- {rule.Trim()}");
                }
            }

            if (!string.IsNullOrWhiteSpace(nodeContext))
            {
                sb.AppendLine();
                sb.AppendLine("Contexto actual de la historia:");
                sb.AppendLine(nodeContext.Trim());
            }

            if (!string.IsNullOrWhiteSpace(memoryContext))
            {
                sb.AppendLine();
                sb.AppendLine(memoryContext.Trim());
            }

            return sb.ToString();
        }
    }
}