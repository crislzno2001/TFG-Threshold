# Guiones completos de los retos creativos (subpruebas TTCT-Verbal)

Nodos de reto listos para el editor de grafos. Cada uno cubre una subprueba con la voz del
personaje. Estructura por nodo: **Gate · contextForAI (lo que recibe la IA) · Apertura ·
Pídele más (fluidez) · Respuestas por calidad · Mide/config · Flags**.

Regla común de fluidez: tras cada respuesta del jugador, el NPC **pide otra** hasta que el
jugador se seca o dice "ya está". Así una misma visita produce varias unidades contables.

Config del evaluador (recordatorio): cada nodo fija `countableUnit` e `ideaDomain` en su
`CreativityTracker`; la escena del nodo va en `contextForAI`.

---

# ASTER — planeador de semillas
Voz: nervioso, tierno, se contradice, suelta datos técnicos absurdos, alérgico a la crueldad.

## ASTER_A2b · Guessing Causes — "¿Quién ha tocado esto?"
- **Gate**: `aster_ideas_count >= 1`
- **contextForAI**:
  `El planeador de semillas de Aster (debería planear y soltar semillas al caer) amaneció
   boca abajo en el taller. Pistas: un ala de tela rajada LIMPIA, como con tijeras, no rota;
   el depósito de semillas VACÍO y Aster no lo vació; la puerta cerrada POR DENTRO. Aster
   está en pánico. Quiere que el jugador proponga muchas causas posibles, por raras que sean.`
- **Apertura**:
  «Está en el suelo. Boca abajo. El ala… cortada limpia, como con tijeras, no rota, cortada.
   Y el depósito de semillas vacío, y yo NO lo vacié. Y la puerta estaba cerrada por dentro.
   Por dentro. Dime por qué crees que ha pasado esto. No me digas "tranquilo". Dime teorías.»
- **Pídele más**: «¿Otra? Dame otra. Cuantas más teorías, menos me tiembla la mano.»
- **Respuestas**:
  - Causa ingeniosa/rara con lógica: «Espera. Eso… no se me había ocurrido. Es inquietante.
    Me gusta que sea inquietante.»
  - Causa muy loca pero jugosa: «Eso es imposible. Probablemente. ¿Por qué he dicho
    "probablemente"? Ahora no voy a dormir.»
  - Causa vaga ("fue alguien"): «"Alguien". Gracias. Reduciré la lista de sospechosos a…
    todo el mundo.»
  - Se burla: «Ah. Mi tragedia te hace gracia. Anotado en la lista de personas.»
- **Mide**: Guessing Causes · `countableUnit`: `possible cause` · `ideaDomain`: *una causa
  posible y distinta de por qué apareció así el planeador (usa las pistas: corte limpio,
  depósito vacío, puerta cerrada)*
- **Flags**: `aster_sabotaje_sospecha = true`

## ASTER_A2c · Asking Questions — "Si tuviera que preguntarle a alguien"
- **Gate**: `aster_sabotaje_sospecha = true`
- **contextForAI**:
  `Aster va a interrogar a quien estuvo anoche en la plaza. No sabe cómo preguntar sin
   sonar acusón. Quiere que el jugador le dé muchas preguntas distintas para averiguar qué
   pasó con el planeador.`
- **Apertura**:
  «Voy a hablar con quien anduvo anoche por la plaza. Pero yo, preguntando, sueno a formulario
   de garantía. Si fueras tú… ¿qué le preguntarías para sacarle la verdad? Dame preguntas.»
- **Pídele más**: «Más. Una buena pregunta abre tres puertas. Dame otra.»
- **Respuestas**:
  - Pregunta astuta/indirecta: «Oh. Eso es astuto. Eso no parece una acusación, parece una
    charla. Me da un poco de miedo que se te ocurra tan rápido.»
  - Pregunta directa útil: «Seca, pero sirve. A veces la puerta se abre a empujones.»
  - Repite la misma pregunta: «Eso ya me lo has dicho con otro sombrero. Otra distinta.»
- **Mide**: Asking Questions · `countableUnit`: `question` · `ideaDomain`: *una pregunta
  distinta y no trivial para averiguar qué le pasó al planeador (no repetir la misma)*

## ASTER_A2d · Product Improvement (2ª muestra) — "Reconstruirla mejor"
- **Gate**: `aster_ideas_count >= 2`
- **contextForAI**:
  `Aster va a reconstruir el planeador desde cero para el concurso. Quiere mejorarlo, no solo
   repararlo: cambios que lo hagan volar mejor, emocionar más o fracasar con elegancia.`
- **Apertura**:
  «Ya que lo abro entero para arreglarlo… es mi oportunidad. ¿Cómo lo dejarías, mejor que
   antes? Y no me digas "alas más grandes". Eso ya lo pensé y lloré encima de los planos.»
- **Pídele más**: «Otra mejora. Amontónamelas, luego yo decido cuál me atrevo a construir.»
- **Respuestas**:
  - Mejora inesperada con sentido: «Espera. Espera. Eso es horrible. Eso es brillante. Eso
    es legalmente discutible. Me encanta.»
  - Mejora sensata: «Sí… es razonable. Odio que sea razonable. Pero puede servir.»
  - "Hazlo mejor" sin más: «Eso no es una mejora, es un deseo. Concreta, por favor.»
- **Mide**: Product Improvement · `countableUnit`: `improvement` · `ideaDomain`: *un cambio
  concreto que mejore el planeador (añadir, quitar, combinar o transformar algo)*

---

# MOCHI — la olla y las combinaciones
Voz: teatral, autoestima "montada con palillos", todo lo dice en metáforas de comida.

## MOCHI_M1 · Fluidez + Flexibilidad — "Dame muchas, y cambia de rumbo"
- **Gate**: `mochi_metida = true`
- **contextForAI**:
  `Mochi está en crisis: le han llegado ingredientes que no pegan (una flor amarga, una seta
   dulce, una raíz sin identificar). NO busca la receta perfecta: quiere MUCHAS ideas y que
   el jugador salte de categoría (dulce↔salado, elegante↔casero, bonito↔feo pero honesto).`
- **Apertura**:
  «No quiero una receta buena. Una buena idea es fácil y aburrida. Quiero MUCHAS. Rápidas,
   sucias, raras. Y cuando te diga "otra", cambia de dirección: si ibas a dulce, vete a
   salado. No te cases con una idea; las ideas son pésimas esposas.»
- **Pídele más**: «¡Otra! Y por lo que más quieras, no me repitas el mismo plato con sombrero
   distinto. Salta.»
- **Respuestas**:
  - Idea que cambia de categoría: «¡Sí! Cambiaste de camino sin tirar el mapa. Eso es cocinar
    con cerebro.»
  - Idea rara pero con sentido: «Eso no es una receta. Es una discusión con perfume. Me
    interesa.»
  - Repite fórmula: «Otra vez lo mismo. Me estás sirviendo el mismo plato en distinto plato.»
  - Cruel: «Ah. Has confundido crítica con incendio.»
- **Mide**: Fluidez (nº de combinaciones) + Flexibilidad (saltos) · `countableUnit`:
  `culinary combination` · `ideaDomain`: *una combinación/idea culinaria distinta con esos
  ingredientes; premia el salto de categoría respecto a la anterior*
- **Flags**: `mochi_ideas_count++`, `mochi_idea_flexible = true` si salta de categoría

## MOCHI_M2 · Unusual Uses — "La olla de la abuela"
- **Gate**: `mochi_metida = true` (o como reto extra de la tarde)
- **contextForAI**:
  `A Mochi se le ha roto la olla de cobre gigante de su abuela: enorme, abollada, ya no
   calienta, asa suelta. Se niega a tirarla. Pide usos INESPERADOS para el objeto, cuanto
   MENOS culinarios mejor (romper la fijación funcional). Cuantos más y más raros, mejor.`
- **Apertura**:
  «Esta olla ya no cocina. Pero tirarla es como enterrar a alguien que aún cuenta chistes.
   Dime para qué más podría servir. Y cuanto menos tenga que ver con cocinar, más me gustas.
   Suéltame diez.»
- **Pídele más**: «Otra. Y que no sea "para cocinar". Eso ya lo sabía hacer, pobre.»
- **Respuestas**:
  - Uso inesperado no culinario: «¡Ja! Eso no se le ocurre a nadie con hambre. Me la quedo.»
  - Uso ingenioso: «Mira tú. La vieja olla tendría una segunda vida más interesante que yo.»
  - Uso obvio (cocinar/guardar comida): «Eso es lo que YA hacía. Sorpréndeme, no la jubiles
    en su mismo puesto.»
- **Mide**: Unusual Uses (originalidad + fluidez) · `countableUnit`: `unusual use` ·
  `ideaDomain`: *un uso inesperado para la olla rota, cuanto menos culinario mejor*

---

# MOTH — luz, sombra y manipulación
Voz: poética, metáforas de luz/sombra/hambre/ventanas, intensa, a veces manipuladora.

## MOTH · Asking Questions — "La silla vacía"
- **Gate**: `moth_conocida = true`
- **contextForAI**:
  `Moth describe una escena inquietante del pueblo: cada mañana aparece una silla vacía en
   mitad de la plaza; nadie la pone, nadie la quita. Invita al jugador a preguntarse cosas
   sobre la escena, muchas preguntas distintas, para "entender su sombra".`
- **Apertura**:
  «Cada mañana hay una silla vacía en mitad de la plaza. Nadie la pone. Nadie la retira. Yo
   ya no pregunto; me limito a mirarla. Pero tú aún tienes esa costumbre práctica de querer
   entender. Dime: ¿qué le preguntarías a la silla, o al pueblo, para saber?»
- **Pídele más**: «Otra pregunta. Las preguntas son luciérnagas: cuantas más sueltas, más se
   ve.»
- **Respuestas**:
  - Pregunta con imaginación: «Ah. Esa pregunta tiene sótano. Me gusta lo que no enseña.»
  - Pregunta funcional: «Práctica. Como tú. No la desprecio; las respuestas también viven en
    lo llano.»
  - Repite: «Esa ya la soltaste. Vuelve a mirar; hay más sombra que rascar.»
- **Mide**: Asking Questions · `countableUnit`: `question` · `ideaDomain`: *una pregunta
  distinta para entender la escena de la silla vacía*

## MOTH_C5b · Guessing Consequences — "El elixir"
- **Gate**: `moth_pidio_ayuda = true` AND `amistad_moth >= 3`
- **contextForAI**:
  `Moth, desesperada, fantasea con un elixir del amor REAL que echaría a Rix en la bebida sin
   que él lo sepa. No pregunta si está bien: quiere que el jugador explore qué pasaría. Es un
   espejo moral: el amor forzado no es amor. Cuantas más consecuencias imagine, mejor.`
- **Apertura**:
  «Imagina que existiera un elixir del amor. De verdad, no de cuento. Y que yo pudiera
   echárselo a Rix en la bebida, y él no lo supiera nunca. ¿Qué crees que pasaría? No me
   digas si está bien. Dime qué pasaría. Todo lo que veas.»
- **Pídele más**: «Sigue. Ábrelo más. ¿Y después de eso, qué? ¿Y un mes después?»
- **Respuestas**:
  - Consecuencia con matiz moral/emocional: «…Ah. No había mirado por esa ventana. Ahí la luz
    hace daño.»
  - Consecuencia imaginativa: «Eso. Justo eso es lo que finjo no pensar cuando lo pienso.»
  - "Nada, sería feliz": «No mientas para consolarme. Piensa. Un deseo cumplido a la fuerza,
    ¿qué deja detrás?»
- **Mide**: Guessing Consequences · `countableUnit`: `possible consequence` · `ideaDomain`:
  *una consecuencia distinta de dar el elixir a Rix (emocional, social, a largo plazo…)*

## MOTH · Just Suppose (poema/imagen) — se conserva como estaba
Reencuadre breve: «Escríbeme una imagen para él, no una frase bonita. Algo que le muerda y no
sepa por qué.» Mide **elaboración + originalidad** (ya documentado en Mapeo).

---

# RIX — rana punk, perroflauta, alérgico a la manipulación
Voz: seca, irónica, defensiva, sensible por dentro.

## RIX · Just Suppose (rotatorio) — "Imagina que…"
- **Gate**: `rix_conocido = true` (reto de tarde)
- **Rotación**: elige el supuesto por `rix_suppose_index` (0/1/2, o aleatorio por partida):
  1. «Imagina que mañana se prohíbe el dinero en el pueblo. Todo trueque, todo gratis. ¿Cómo
     sería la vida aquí? Cuéntame, sin frenos.»
  2. «Imagina que nadie tuviera reloj ni supiera nunca qué hora es. Ni relojes, ni móviles,
     nada. ¿Qué pasaría?»
  3. «Imagina que las casas no tuvieran dueño y cualquiera pudiera vivir en cualquiera. ¿Qué
     harías tú? ¿Qué haría la gente?»
- **contextForAI**:
  `Rix, en modo filosófico-contracultural, lanza un supuesto imposible sobre cómo cambiaría el
   pueblo (rotar entre: sin dinero / sin relojes / casas sin dueño). No busca respuesta
   correcta: quiere que el jugador imagine consecuencias, las más y más raras posibles.`
- **Apertura**: (la del supuesto elegido)
- **Pídele más**: «¿Y qué más? No te quedes en lo bonito. También se rompería algo. Dímelo.»
- **Respuestas**:
  - Consecuencia con chispa: «Vale. Eso… no es la típica respuesta de folleto. Sigue.»
  - Consecuencia utópica ingenua: «Ajá. Muy bonito. ¿Y cuando alguien lo estropee, que
    siempre hay uno? Piensa en eso también.»
  - Respuesta plana ("estaría bien"): «"Estaría bien". Menuda revolución. Dame carne.»
- **Mide**: Just Suppose · `countableUnit`: `imagined consequence` · `ideaDomain`: *una
  consecuencia imaginada del supuesto que Rix plantea (cuanto más inesperada, mejor)*

## RIX_C7c · Guessing Consequences — "¿Qué va a pasar con Moth?"
- **Gate**: `rix_confia = true` AND `moth_pidio_ayuda = true`
- **contextForAI**:
  `Rix intuye que Moth siente algo por él y que la florista ha andado por medio. Pregunta,
   sin dramatismo pero en serio, qué cree la florista que pasará con Moth a partir de ahora.
   Quiere honestidad, no consuelo.`
- **Apertura**:
  «Vale, listilla. Tú lo ves desde fuera. Si esto sigue como va… ¿qué crees que va a pasar
   con Moth? Y no me mientas para que me sienta mejor. Dame tu apuesta. O varias.»
- **Pídele más**: «¿Y si hago lo contrario? ¿Qué pasaría entonces? Dame las dos ramas.»
- **Respuestas**:
  - Consecuencia lúcida: «…Sí. Probablemente. Odio cuando alguien acierta antes que yo.»
  - Consecuencia empática: «No lo había pensado por ese lado. Igual no soy el único que sale
    escaldado de esto.»
  - Consuelo vacío: «Eso ha sido amable y completamente inútil. Piénsalo de verdad.»
- **Mide**: Guessing Consequences · `countableUnit`: `possible consequence` · `ideaDomain`:
  *una consecuencia posible de la situación entre Moth y Rix (para uno u otro)*

---

# CUALQUIER VECINO — cierre del bucle
## D2 · Guessing Causes — "Dicen cosas de ti"
- **Gate**: alguna decisión turbia (`ayudaste_mentira_moth` OR `cotilleo_a_mochi_sobre_aster`
  OR `mochi_ofendida`)
- **contextForAI**:
  `Un vecino le cuenta a la florista, de pasada, que el pueblo anda hablando de ELLA por sus
   decisiones del día. Le pide que conjeture por qué la gente habla así — la jugadora, que ha
   estado midiendo a otros, ahora se explica a sí misma.`
- **Apertura**:
  «Oye… no te lo tomes a mal, pero andan diciendo cosas de ti por el pueblo. ¿Por qué crees
   que la gente habla así? Va, sin excusas. Dame motivos.»
- **Pídele más**: «¿Y qué más dirán? Ponte en su lugar, aunque escueza.»
- **Respuestas**:
  - Causa autocrítica y honesta: «Mira, eso es más maduro que la mitad del pueblo junto.»
  - Causa que reparte culpa con sentido: «Ya. No todo es tuyo, tienes razón. Pero algo sí.»
  - Excusa/negación: «"No he hecho nada". Ya. Nadie ha hecho nunca nada, por eso pasan cosas.»
- **Mide**: Guessing Causes · `countableUnit`: `possible cause` · `ideaDomain`: *una causa
  posible de que el pueblo hable de la florista (honesta, no una excusa)*

---

## Cobertura final (2 muestras por subprueba)
- **Fluidez/Flexibilidad**: Mochi M1
- **Unusual Uses**: Mochi M2 (olla)
- **Product Improvement**: Aster A2 (base) + A2d
- **Just Suppose**: Moth (poema) + Rix (rotatorio)
- **Asking Questions**: Aster A2c + Moth (silla)
- **Guessing Causes**: Aster A2b (planeador) + D2 (cotilleo)
- **Guessing Consequences**: Moth C5b (elixir) + Rix C7c (Moth)
