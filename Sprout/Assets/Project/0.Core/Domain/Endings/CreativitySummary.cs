using System.Collections.Generic;
using Sprout.Domain.Creativity;
using Sprout.Domain.Narrative;

namespace Sprout.Domain.Endings
{
    /// <summary>
    /// Genera un resumen POÉTICO del final según CÓMO jugó la florista (dimensiones de creatividad +
    /// si manipuló). No dice "creatividad alta/baja": lo describe con imágenes. Pure C#.
    /// </summary>
    public static class CreativitySummary
    {
        public static List<string> Build(CreativityScores c, NarrativeFlagStore flags, float high = 0.55f)
        {
            var lines = new List<string>();
            bool F(string k) => flags != null && flags.GetFlag(k);

            // "Manipulación" no es una dimensión de creatividad: se deduce de los actos.
            int manipulation = 0;
            if (F(NarrativeFlagKeys.HelpedMothLie)) manipulation++;
            if (F(NarrativeFlagKeys.GossipToMochiAboutAster)) manipulation++;
            if (F(NarrativeFlagKeys.PlayerGossiped)) manipulation++;

            if (c.Originality >= high)
                lines.Add("El pueblo recordará tus ideas porque no caminaban recto. Algunas tropezaron. " +
                          "Algunas volaron. Pero ninguna parecía muerta.");

            if (c.Elaboration >= high)
                lines.Add("No diste respuestas rápidas. Les pusiste textura, sombra y bordes. Eso hizo que " +
                          "algunos personajes se sintieran vistos.");

            // Ramificación ética: pensar en las consecuencias (empatía/coherencia altas y SIN manipular).
            if (manipulation == 0 && (c.Empathy >= high || c.Coherence >= high))
                lines.Add("Pensaste en lo que pasaría después de tus palabras. En un pueblo pequeño, eso vale " +
                          "más que tener razón.");

            if (c.Empathy >= high)
                lines.Add("No arreglaste a nadie. Eso fue lo importante. Solo te quedaste lo bastante cerca " +
                          "para que algunos dejaran de fingir.");

            if (manipulation >= 1)
                lines.Add("Fuiste creativa, sí. Pero a veces usaste la imaginación como una cuerda alrededor de otros.");

            if (lines.Count == 0)
                lines.Add("Fuiste amable con el pueblo, pero te guardaste un poco. Algunas puertas se quedaron " +
                          "entornadas, esperando.");

            return lines;
        }
    }
}
