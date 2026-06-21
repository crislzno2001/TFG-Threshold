#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using OpenAI.Dialogue;

namespace Sprout.EditorTools
{
    /// <summary>
    /// Uso único: genera los grafos de diálogo de los cuatro vecinos en el estilo limpio
    /// de Mochi (nodos incrustados como sub-assets de un único .asset por NPC), con los
    /// beats de la biblia ampliada: frase inicial, contexto para la IA, condición de salida,
    /// elecciones, flags (al entrar y puertas) y un nodo de despedida terminal.
    ///
    /// NO es destructivo: crea grafos NUEVOS en
    ///   Assets/Project/ScriptableObjects/DialogueGraphs/Generated/
    /// con nombres propios. Después, en la escena, apunta el NPCBrain de cada vecino a su
    /// grafo nuevo (y el CreativityTracker a su "nodo de ideas"). Luego puedes borrar este archivo.
    ///
    /// Menú:  Tools/Sprout/Build Dialogue Graphs
    /// </summary>
    public static class SproutGraphBuilder
    {
        private const string Root = "Assets/Project/ScriptableObjects/DialogueGraphs";
        private const string Dir = Root + "/Generated";

        [MenuItem("Tools/Sprout/Build Dialogue Graphs")]
        public static void Build()
        {
            EnsureFolders();
            BuildMochi();
            BuildAster();
            BuildMoth();
            BuildRix();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Sprout",
                "Grafos generados en:\n" + Dir +
                "\n\nApunta el NPCBrain de cada vecino a su grafo nuevo, y el CreativityTracker " +
                "a su nodo de ideas (Mochi_ideas, Aster_ideas).\n\nPuedes borrar este archivo.",
                "OK");
            Debug.Log("[Sprout] Dialogue graphs built in " + Dir);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Project/ScriptableObjects"))
                    AssetDatabase.CreateFolder("Assets/Project", "ScriptableObjects");
                AssetDatabase.CreateFolder("Assets/Project/ScriptableObjects", "DialogueGraphs");
            }
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder(Root, "Generated");
        }

        // ───────────────────────── helpers ─────────────────────────
        private static int _y;

        private static DialogueGraphSO NewGraph(string name)
        {
            string path = Dir + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(path) != null)
                AssetDatabase.DeleteAsset(path); // sobrescribir al re-ejecutar (no duplicar)
            var g = ScriptableObject.CreateInstance<DialogueGraphSO>();
            AssetDatabase.CreateAsset(g, path);
            _y = 60;
            return g;
        }

        private static T Add<T>(DialogueGraphSO g, string name) where T : DialogueNodeSO
        {
            var n = ScriptableObject.CreateInstance<T>();
            n.name = name;
            n.nodeGuid = System.Guid.NewGuid().ToString();
            n.contextForAI = "";
            n.prerequisiteFlags = new List<DialogueFlagRequirement>();
            n.flagsOnEnter = new List<DialogueFlagChange>();
            n.nextNodes = new List<DialogueNodeSO>();
            AssetDatabase.AddObjectToAsset(n, g);
            g.nodes.Add(n);
            g.nodePositions.Add(new NodePositionData { nodeId = n.nodeGuid, position = new Vector2(260, _y) });
            _y += 220;
            return n;
        }

        private static ConversationNodeSO Conv(DialogueGraphSO g, string name, string opening, string ctx, string exit)
        {
            var n = Add<ConversationNodeSO>(g, name);
            n.openingLine = opening;
            n.contextForAI = ctx;
            n.conversationTopics = new List<string>();
            n.exitCondition = exit;
            return n;
        }

        private static ChoiceNodeSO Choice(DialogueGraphSO g, string name, string opening, string ctx)
        {
            var n = Add<ChoiceNodeSO>(g, name);
            n.openingLine = opening;
            n.contextForAI = ctx;
            n.choices = new List<ChoiceData>();
            return n;
        }

        private static SpeechNodeSO Speech(DialogueGraphSO g, string name, string opening, string ctx)
        {
            var n = Add<SpeechNodeSO>(g, name);
            n.openingLine = opening;
            n.contextForAI = ctx;
            n.transitions = new List<NodeTransition>();
            return n;
        }

        private static void Gate(DialogueNodeSO n, string flag, bool val = true)
            => n.prerequisiteFlags.Add(new DialogueFlagRequirement { flag = flag, expectedValue = val });

        private static void OnEnter(DialogueNodeSO n, string flag, bool val = true)
            => n.flagsOnEnter.Add(new DialogueFlagChange { flag = flag, value = val });

        private static void Next(DialogueNodeSO from, DialogueNodeSO to)
            => from.nextNodes.Add(to);

        private static void AddChoice(ChoiceNodeSO c, string condition, DialogueNodeSO next)
            => c.choices.Add(new ChoiceData { condition = condition, nextNode = next });

        private static void Finish(DialogueGraphSO g, DialogueNodeSO entry)
        {
            g.entryNode = entry;
            EditorUtility.SetDirty(g);
        }

        // ───────────────────────── MOCHI (receta -> madre) ─────────────────────────
        private static void BuildMochi()
        {
            var g = NewGraph("Mochi_Sprout");

            var talk = Conv(g, "Mochi_talk",
                "Mamma mia! ¡Florista! ¿Has comido? Esa cara dice que no. ¡Y yo en plena CATÁSTROFE!",
                "Mochi, hongo chef italiano, en pleno ataque de pánico. Le han llegado ingredientes raros (Sal de luna, Hongo susurrante, Fruta espejo) y debe improvisar un plato para un cliente importante esta noche. Dramático, oversharing, cariñoso.",
                "El jugador se ofrece a ayudar con la receta o pregunta por los ingredientes.");
            OnEnter(talk, "mochi_metida");

            var ideas = Conv(g, "Mochi_ideas",
                "¡SÍ! ¡Necesito IDEAS! ¡Dime cualquier cosa! ¿Qué hago con esto?",
                "NODO DE IDEAS de Mochi: el jugador propone combinaciones culinarias con los ingredientes raros. Aquí se mide Fluidez y Flexibilidad. Mochi reacciona con drama a cada idea.",
                "El jugador deja de proponer ideas, dice que ya tiene bastante o que Mochi decida.");
            Gate(ideas, "mochi_metida");

            var taste = Choice(g, "Mochi_taste",
                "Toma. PRUÉBALO. Dime que está bueno, por favor. ¿…está bueno?",
                "Mochi ofrece un risotto experimental amargo y espera elogios. Decisión clave: mentira amable, honestidad dolorosa, o evasiva.");
            Gate(taste, "mochi_metida");

            var lie = Speech(g, "Mochi_lie",
                "¡LO SABÍA! ¡Eres un ángel con tierra en las uñas! Mamma mia, qué alivio.",
                "Mochi se cree la mentira amable y gana confianza. Más tarde puede enterarse por cotilleo.");
            OnEnter(lie, "lied_kindly_to_mochi");
            OnEnter(lie, "mochi_confianza");

            var honest = Speech(g, "Mochi_honest",
                "…Auch. Duele. Pero… gracias. Nadie me dice la verdad desde hace mucho.",
                "Mochi se ofende un segundo pero respeta la honestidad dolorosa. Es el camino que de verdad le ayuda.");
            OnEnter(honest, "player_was_honest");

            var evade = Speech(g, "Mochi_evade",
                "¿…eso es un sí o un no? Bah. Da igual. Olvídalo.",
                "El jugador evadió. Mochi se queda inseguro y algo distante.");

            var confess = Conv(g, "Mochi_confess",
                "Florista… te confieso algo. El cliente de esta noche… es mi madre. Donna Funghi.",
                "Mochi confiesa que el cliente VIP es su madre, una cocinera legendaria y demoledora. Tiene miedo de no estar a la altura. Momento de vulnerabilidad.",
                "El jugador reacciona al secreto (con apoyo, con honestidad, o quitándole hierro).");
            Gate(confess, "mochi_confianza");
            OnEnter(confess, "mochi_secreto");

            var bye = Speech(g, "Mochi_bye",
                "¡Vía, vía! ¡Que se me quema el risotto y hoy no puede salir mal! Grazie, florista.",
                "Despedida de Mochi. Nodo terminal: la conversación se cierra.");

            Next(talk, ideas);
            Next(ideas, taste);
            AddChoice(taste, "el jugador dice que está delicioso o lo elogia (mentira amable)", lie);
            AddChoice(taste, "el jugador es honesto y dice que está malo, aunque duela", honest);
            AddChoice(taste, "el jugador evade, da una respuesta neutra o no dice nada claro", evade);
            Next(lie, confess);
            Next(honest, confess);
            Next(evade, bye);
            Next(confess, bye);

            Finish(g, talk);
        }

        // ───────────────────────── ASTER (máquina de estrellas -> ex) ─────────────────────────
        private static void BuildAster()
        {
            var g = NewGraph("Aster_Sprout");

            var talk = Conv(g, "Aster_talk",
                "Hola, perdona, no quería molestar… Mira, he construido esto: una máquina que proyecta el cielo de una noche concreta. Pero está… muerto. No parece aquella noche. Y el concurso es en nada.",
                "Aster, conejo científico tímido (se le mueve la nariz a tirones como a una liebre). Ha hecho una máquina de estrellas: un proyector que recrea el cielo de una noche concreta. Funciona pero se ve plano y sin emoción. El concurso del pueblo es pronto. Aún no dice para quién es.",
                "El jugador se ofrece a ayudar o pregunta por la máquina.");
            OnEnter(talk, "aster_metido");

            var ideas = Conv(g, "Aster_ideas",
                "Salen las estrellas, sí, pero no transmiten NADA. ¿Cómo hago que parezca aquella noche? Dime ideas, lo que sea, por raro que parezca.",
                "NODO DE IDEAS de Aster (mide ORIGINALIDAD). Problema concreto: la máquina de estrellas se ve plana y no evoca un recuerdo. El jugador propone ideas para que SÍ emocione: un sonido de esa noche, un olor, una estrella fugaz en el momento justo, que reaccione a quien mira, una constelación inventada... Cuanto más inesperada y evocadora, mejor.",
                "El jugador deja de proponer ideas o dice que ya está bien.");
            Gate(ideas, "aster_metido");

            var attempt = Choice(g, "Aster_attempt",
                "Yo ya probé algo… le metí TODAS las estrellas que pude. Ahora en vez de un cielo es una bombilla que ciega. Me he pasado, ¿verdad? No sé parar.",
                "Aster enseña un intento fallido de la MISMA máquina (la ha saturado de estrellas). Momento vulnerable. Decisión: ayudarle a SIMPLIFICAR (quitar para que respire) o decirle con honestidad que se esconde en el invento para no enfrentarse a lo de verdad.");
            Gate(attempt, "aster_metido");

            var refine = Speech(g, "Aster_refine",
                "¿Quitar… para que respire? No se me había ocurrido. Menos es más. Vale. Lo apunto. Gracias, de verdad.",
                "El jugador le ayudó a simplificar con criterio. Aster gana confianza y aprende algo.");
            OnEnter(refine, "aster_idea_mejorada");

            var mirror = Speech(g, "Aster_mirror",
                "…Auch. No me escondo en el invento. Yo no… vale. Puede. Puede que sí. No me mires así.",
                "El jugador fue honesto: Aster se esconde en la máquina para no enfrentarse a lo de verdad. Le duele pero le toca.");
            OnEnter(mirror, "aster_idea_mejorada");
            OnEnter(mirror, "player_was_honest");

            var confess = Conv(g, "Aster_confess",
                "Vale. Te lo digo. La máquina es para mi ex. Para recrear el cielo de la noche en que nos conocimos. Pensé que si ganaba el concurso tendría una excusa para llamarla.",
                "Aster confiesa que la máquina de estrellas es para reconquistar a su ex. Cortaron hace meses (ella se cansó de que se refugiara en sus inventos). El concurso es la excusa.",
                "El jugador reacciona a la confesión, con tacto o sin él.");
            Gate(confess, "aster_idea_mejorada");
            OnEnter(confess, "aster_secreto_conocido");
            OnEnter(confess, "aster_confia");

            var twist = Conv(g, "Aster_twist",
                "Lo gracioso… llevo meses con esto. Y ni siquiera sé si vendría. Igual ya ni se acuerda de aquella noche. Igual la noche solo fue importante para mí.",
                "Giro: Aster admite que lleva meses construyéndola, que no sabe si su ex aparecería, y que quizá aquella noche solo significó algo para él. Empieza a darse cuenta de que la perdió por motivos que no tienen que ver con inventar.",
                "El jugador le ayuda a verlo o le empuja a seguir persiguiéndola.");
            Gate(twist, "aster_confia");
            OnEnter(twist, "aster_se_da_cuenta");

            var decide = Choice(g, "Aster_decide",
                "Entonces… ¿la enciendo para ella, a ver si viene? ¿O la enciendo para mí, y ya está?",
                "Decisión final que el jugador inclina: encender la máquina de estrellas para reconquistar a la ex, o para sí mismo y el pueblo (soltar y crecer).");
            Gate(decide, "aster_se_da_cuenta");

            var free = Speech(g, "Aster_free",
                "Para mí. Y para quien quiera mirar. …Si ella viene, viene. Pero ya no la construyo por eso. Gracias por ayudarme a verlo.",
                "Aster decide encenderla para sí mismo y el pueblo: ha aprendido a soltar. Final de crecimiento.");
            OnEnter(free, "aster_libre");

            var insist = Speech(g, "Aster_insist",
                "Para ella. Tengo que intentarlo. Si no lo hago me voy a preguntar siempre… ¿y si hubiera funcionado?",
                "Aster decide encenderla para reconquistar a la ex. Final de insistencia: puede salir bien o doler.");
            OnEnter(insist, "aster_insiste");

            var bye = Speech(g, "Aster_bye",
                "Bueno. Me vuelvo al taller. Tengo estrellas que ordenar. Gracias por escuchar, en serio. Adiós, adiós.",
                "Despedida de Aster. Nodo terminal.");

            Next(talk, ideas);
            Next(ideas, attempt);
            AddChoice(attempt, "el jugador le ayuda a simplificar la máquina con criterio", refine);
            AddChoice(attempt, "el jugador le dice con honestidad que se esconde en el invento", mirror);
            Next(refine, confess);
            Next(mirror, confess);
            Next(confess, twist);
            Next(twist, decide);
            AddChoice(decide, "el jugador le anima a encenderla para sí mismo y soltar a su ex", free);
            AddChoice(decide, "el jugador le anima a intentar reconquistar a su ex", insist);
            Next(free, bye);
            Next(insist, bye);

            Finish(g, talk);
        }

        // ── MOTH (obsesión con Rix) ──
        private static void BuildMoth()
        {
            var g = NewGraph("Moth_Sprout");

            var talk = Conv(g, "Moth_talk",
                "La luz no se mira. Se siente. Tú tienes una luz… práctica. Me gusta.",
                "Moth, polilla obsesiva y metafórica. Habla en metáforas raras, observa demasiado, dice cosas que no debería. Está obsesionada con Rix pero lo disfraza de interés filosófico.",
                "El jugador le sigue la conversación o le pregunta qué quiere.");
            OnEnter(talk, "moth_conocida");

            var poem = Conv(g, "Moth_poem",
                "¿Me ayudas con unas palabras? Para… un amigo. Tienen que sonar a verdad aunque no lo sean.",
                "NODO DE ELABORACIÓN: Moth pide ayuda para escribir un poema para Rix. Aquí se mide Elaboración (si el jugador da detalles concretos o va seco).",
                "El jugador aporta una estrofa, una imagen concreta, o dice que no se le ocurre nada.");
            Gate(poem, "moth_conocida");

            var confess = Conv(g, "Moth_confess",
                "Vale. No es para un amigo. Es para Rix. Lo necesito. ¿Me ayudas a que me vea?",
                "Moth confiesa su obsesión con Rix y pide a la florista que medie. Empieza a depender emocionalmente.",
                "El jugador acepta ayudar, duda, o le pone límites.");
            Gate(confess, "moth_conocida");
            OnEnter(confess, "moth_pidio_ayuda");

            var request = Choice(g, "Moth_request",
                "Dile a Rix que escribí una canción para él. No es verdad, pero… funciona mejor si no lo es.",
                "Petición turbia: Moth pide inventarle a Rix algo que ella no hizo. Decisión moral del juego.");
            Gate(request, "moth_pidio_ayuda");

            var helped = Speech(g, "Moth_helped",
                "¿Lo harás? Gracias. Gracias. Sabía que tú sí sentías mi luz.",
                "El jugador acepta la mentira. Esto puede estallar cuando Rix lo descubra.");
            OnEnter(helped, "helped_moth_lie");

            var refused = Speech(g, "Moth_refused",
                "…Claro. Tienes razón. Perdona. Es que… no quiero que se apague antes de encenderse.",
                "El jugador se niega a la mentira (honestidad). Moth se duele pero lo encaja.");
            OnEnter(refused, "player_was_honest");

            var bye = Speech(g, "Moth_bye",
                "Vuelve mañana. La luz cambia de noche. Yo también. No te vayas tú también.",
                "Despedida de Moth. Nodo terminal.");

            Next(talk, poem);
            Next(poem, confess);
            Next(confess, request);
            AddChoice(request, "el jugador acepta inventarle la mentira a Rix", helped);
            AddChoice(request, "el jugador se niega a mentir y le pone límites con cariño", refused);
            Next(helped, bye);
            Next(refused, bye);

            Finish(g, talk);
        }

        // ───────────────────────── RIX (borde -> confianza) ─────────────────────────
        private static void BuildRix()
        {
            var g = NewGraph("Rix_Sprout");

            var talk = Conv(g, "Rix_talk",
                "¿Otra vez tú? Vale. No me importa. No me importa que no te importe que no me importe.",
                "Rix, rana punk, borde por fuera y sensible por dentro. Dice que no le importa nada pero reacciona a todo. No sabe que Moth está enamorada de él. Su padre era músico y se fue.",
                "El jugador insiste con tacto o le pregunta por su vida.");
            OnEnter(talk, "rix_conocido");

            var aboutMoth = Choice(g, "Rix_aboutMoth",
                "¿La polilla? Sí, la conozco. ¿Por? …¿por qué me miras así?",
                "El jugador habla con Rix sobre Moth. Tres ramas según cómo lo plantee.");
            Gate(aboutMoth, "rix_conocido");

            var curio = Speech(g, "Rix_curioso",
                "¿En serio? …no, nada. Solo… curiosidad. Cállate. No he dicho nada.",
                "El jugador despertó la curiosidad de Rix por Moth, sin presionar.");
            OnEnter(curio, "rix_curiosidad");

            var alert = Speech(g, "Rix_alerta",
                "Espera. ¿Tú estás tramando algo? ¿Para quién trabajas? No me fío.",
                "El jugador soltó un plan y puso a Rix en alerta.");
            OnEnter(alert, "rix_alerta");

            var neutral = Speech(g, "Rix_neutral",
                "Ajá. Vale. ¿Eso es todo? Pues ya está.",
                "El jugador no mencionó nada raro. Rix queda neutro.");
            OnEnter(neutral, "rix_neutral");

            var open = Conv(g, "Rix_open",
                "No le digas a nadie que te enseño esto. …Es un cuaderno. De letras. Mías.",
                "Rix baja la guardia por primera vez y enseña su cuaderno de canciones. Solo ocurre si hubo confianza y NO se le mintió. Reciprocidad: se abre más si la florista también se abre.",
                "El jugador responde con sinceridad, se abre también, o reacciona con frialdad.");
            Gate(open, "rix_neutral");
            Gate(open, "helped_moth_lie", false);
            OnEnter(open, "rix_confia");

            var bye = Speech(g, "Rix_bye",
                "Ya está bien de hablar. Vete. …Pero vuelve, ¿eh. Da igual. Lo que quieras.",
                "Despedida de Rix. Nodo terminal.");

            Next(talk, aboutMoth);
            AddChoice(aboutMoth, "el jugador habla bien de Moth sin presionar", curio);
            AddChoice(aboutMoth, "el jugador confiesa que Moth trama un plan", alert);
            AddChoice(aboutMoth, "el jugador no menciona a Moth ni nada raro", neutral);
            Next(curio, bye);
            Next(alert, bye);
            Next(neutral, open);
            Next(open, bye);

            Finish(g, talk);
        }
    }
}
#endif
