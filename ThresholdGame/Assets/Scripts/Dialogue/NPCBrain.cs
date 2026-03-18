using System.Collections.Generic;
using UnityEngine;
using OpenAI;

namespace OpenAI.Dialogue
{
    /// <summary>
    /// La "mente" del NPC. Gestiona su personalidad, historial de conversación
    /// y las llamadas a OpenAI. Añádelo al mismo GameObject que NPCInteractionTrigger.
    /// </summary>
    public class NPCBrain : MonoBehaviour
    {
        [Header("Identidad")]
        public string npcName = "Gareth";

        [TextArea(4, 10)]
        public string personality = "Eres un tabernero medieval llamado Gareth. Eres gruñón pero en el fondo tienes buen corazón. Hablas de forma informal. Sabes todo sobre la taberna y el pueblo. Responde siempre en menos de 3 frases cortas.";

        [Header("OpenAI")]
        [SerializeField] private string model = "gpt-4o-mini";
        [Range(0f, 2f)]
        [SerializeField] private float temperature = 0.8f;
        [SerializeField] private int maxTokens = 150;

        [Header("Memoria")]
        [Tooltip("Cuántos mensajes anteriores recuerda el NPC (para no gastar demasiados tokens)")]
        [SerializeField] private int maxHistoryMessages = 2;

        private OpenAIApi openai;
        private List<ChatMessage> history = new List<ChatMessage>();

        private void Awake()
        {
            openai = new OpenAIApi();
            ResetHistory();
        }

        public void ResetHistory()
        {
            history.Clear();
            history.Add(new ChatMessage
            {
                role = "system",
                content = personality
            });
        }

        /// <summary>
        /// Envía un mensaje y devuelve la respuesta del NPC.
        /// </summary>
        public async System.Threading.Tasks.Task<string> SendMessage(string userMessage)
        {
            history.Add(new ChatMessage { role = "user", content = userMessage });

            // Limitar historial para no gastar tokens innecesarios
            TrimHistory();

            var req = new CreateChatCompletionRequest
            {
                model = model,
                messages = history,
                temperature = temperature,
                max_tokens = maxTokens
            };

            var response = await openai.CreateChatCompletion(req);

            if (response?.choices == null || response.choices.Count == 0)
                return "...";

            string reply = response.choices[0].message.content;
            history.Add(new ChatMessage { role = "assistant", content = reply });

            return reply;
        }

        private void TrimHistory()
        {
            // Mantener siempre el system prompt (índice 0) + últimos N mensajes
            while (history.Count > maxHistoryMessages + 1)
                history.RemoveAt(1);
        }
    }
}