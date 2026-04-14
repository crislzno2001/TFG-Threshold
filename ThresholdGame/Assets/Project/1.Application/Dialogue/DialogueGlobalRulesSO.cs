using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OpenAI.Dialogue
{
    [CreateAssetMenu(fileName = "DialogueGlobalRules", menuName = "Dialogue/Global Rules")]
    public class DialogueGlobalRulesSO : ScriptableObject
    {
        [Header("Reglas globales comunes")]
        public bool neverBreakCharacter = true;
        public bool forbidMetaAIReferences = true;
        public bool avoidModernLanguage = true;

        [Header("Reglas obligatorias globales")]
        public List<string> mandatoryRules = new();

        [Header("Frases prohibidas globales")]
        public List<string> forbiddenPhrases = new();

        public string BuildGlobalRulesBlock()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Reglas globales del sistema:");

            if (neverBreakCharacter)
                sb.AppendLine("- Nunca rompas personaje.");

            if (forbidMetaAIReferences)
            {
                sb.AppendLine("- No digas que eres una IA.");
                sb.AppendLine("- No menciones prompts, sistema, modelo, instrucciones internas ni limitaciones técnicas.");
            }

            if (avoidModernLanguage)
                sb.AppendLine("- Evita expresiones modernas o tecnológicas fuera del mundo del personaje.");

            if (mandatoryRules != null)
            {
                foreach (string rule in mandatoryRules)
                {
                    if (!string.IsNullOrWhiteSpace(rule))
                        sb.AppendLine($"- {rule.Trim()}");
                }
            }

            return sb.ToString();
        }
    }
}