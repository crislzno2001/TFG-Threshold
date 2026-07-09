# Mapeo de las 6 subpruebas verbales del TTCT a Sprout

**Principio**: reutilizar los nodos de reto y de situación que YA existen en los grafos
(`*_Biblia.asset`), reencuadrando su texto y configurando cada `CreativityTracker`.
No se copian los ítems reales del TTCT (copyright STS + validez): se usan **análogos
originales** inspirados en el marco, coherente con la sección 2.2.7 de la memoria.

Estímulo gráfico: **no se usan fotos**. En el TTCT verbal las tres primeras actividades
comparten una lámina; aquí el "estímulo" es una **escena narrada por el propio vecino**,
que cumple la misma función disparadora sin romper la estética 3D ni infringir copyright.

---

## 1. Reparto (coincide con lo que ya afirma la memoria)

| Subprueba | Personaje | Tipo | Nodo existente que se reutiliza |
|---|---|---|---|
| Product Improvement | Aster | Plena | `Aster_challenge` (+ `Aster_refine`) |
| Unusual Uses | Mochi | Plena | `Mochi_challenge` (+ `Mochi_night` = adaptación) |
| Just Suppose | Moth | Plena | reencuadre de `Moth_poem` |
| Asking Questions | Moth | Ligera | una situación de Moth (`Moth_meet` u otra) |
| Guessing Causes | Rix | Ligera | `Rix_open` |
| Guessing Consequences | Rix | Ligera | `Rix_aboutMoth` (o beat "¿qué pasará?") |

Plenas = nodo de reto dedicado, con contador de fluidez. Ligeras = se enganchan como
`extraChallengeNodes` sobre un nodo de situación que ya presenta una escena.

---

## 2. Único cambio de código (para las 3 ligeras)

En `CreativityTracker.cs`, generalizar la unidad que se cuenta. Añadir el campo:

```csharp
[Header("Unidad que se cuenta en este reto")]
[Tooltip("idea/propuesta (por defecto), pregunta, causa, consecuencia, uso, mejora…")]
[SerializeField] private string countableUnit = "concrete idea/proposal";
```

Y en el prompt del evaluador (método `Evaluate`), sustituir estas dos líneas:

```
- sb.AppendLine($"What counts as a concrete idea for this character: {ideaDomain}");
+ sb.AppendLine($"What counts as ONE countable {countableUnit} here: {ideaDomain}");

- sb.AppendLine("IDEA=yes only if it's a concrete idea/proposal (no for greetings/questions/filler).");
+ sb.AppendLine($"IDEA=yes only if the message contains a genuine {countableUnit} (no for greetings/off-topic/filler).");
```

Con `countableUnit = "concrete idea/proposal"` por defecto, las 3 plenas se comportan
EXACTAMENTE igual que ahora. Solo las ligeras cambian su valor.

> Limitación honesta: la fluidez cuenta *mensajes de reto con ≥1 unidad válida*, no
> unidades por mensaje. Es la simplificación que la memoria ya describe ("fluidez como
> conteo") y basta para el prototipo; contar unidades por mensaje queda como mejora.

---

## 3. Config por `CreativityTracker` (Inspector)

### Aster — Product Improvement (plena)
- `ideasNode`: `Aster_challenge`
- `extraChallengeNodes`: `Aster_refine` (revisión/mejora)
- `counterKey`: `aster_ideas_count`
- `countableUnit`: `improvement`
- `ideaDomain`:
  `una mejora concreta para el invento de Aster: un cambio que lo haga más interesante,
   más bonito, más emotivo o más útil (añadir, quitar, combinar o transformar algo).`

### Mochi — Unusual Uses (plena)
- `ideasNode`: `Mochi_challenge`
- `extraChallengeNodes`: `Mochi_night` (adaptación)
- `counterKey`: `mochi_ideas_count`
- `countableUnit`: `unusual use`
- `ideaDomain`:
  `un uso o preparación inesperada para un ingrediente (usarlo de una forma que no es la
   habitual, combinarlo de forma rara pero con sentido, o darle un papel nuevo en el plato).`

### Moth — Just Suppose (plena)
- `ideasNode`: `Moth_poem` (reencuadrado, ver guiones)
- `counterKey`: `moth_ideas_count`
- `countableUnit`: `imagined consequence`
- `ideaDomain`:
  `una consecuencia imaginativa y concreta de un supuesto imposible: qué cambiaría en el
   pueblo o en la gente si ese supuesto fuera cierto.`

### Moth — Asking Questions (ligera)
- `extraChallengeNodes`: (nodo de situación de Moth, p. ej. `Moth_meet`)
- `countableUnit`: `question`
- `ideaDomain`:
  `una pregunta distinta y no trivial para entender mejor la escena que Moth describe
   (quién, por qué, desde cuándo, qué falta…). No vale repetir la misma pregunta.`

### Rix — Guessing Causes (ligera)
- `extraChallengeNodes`: `Rix_open`
- `countableUnit`: `possible cause`
- `ideaDomain`:
  `una causa posible y distinta de lo que Rix cuenta que ha pasado (por qué pudo ocurrir).`

### Rix — Guessing Consequences (ligera)
- `extraChallengeNodes`: `Rix_aboutMoth` (o beat equivalente)
- `countableUnit`: `possible consequence`
- `ideaDomain`:
  `una consecuencia posible y distinta de lo que Rix cuenta (qué podría pasar a partir de ahora).`

---

## 4. Guiones — reencuadre mínimo (solo `openingLine` y `exitCondition`)

Se conserva la voz de cada personaje. Solo se ajusta el disparador para que pida el tipo
de pensamiento divergente correcto.

### Aster — Product Improvement (`Aster_challenge`)
- `openingLine` (ajuste sugerido):
  «He construido una máquina que proyecta el cielo de una noche concreta… pero le falta
   algo. ¿Cómo la mejorarías para que emocione de verdad? Dime cambios: añade, quita,
   combina… lo que sea.»
- `exitCondition`: «El jugador propone al menos una mejora concreta del invento.»
- (Ya casi lo era: Product Improvement es, literalmente, mejorar un objeto.)

### Mochi — Unusual Uses (`Mochi_challenge`)
- `openingLine`:
  «Me han traído estos ingredientes imposibles y no sé qué hacer con ellos. Dime: ¿para
   qué más podrían servir, aparte de lo obvio? Sorpréndeme.»
- `exitCondition`: «El jugador propone al menos un uso o preparación inesperada.»
- `Mochi_night` se mantiene como está: mide **adaptación** (mejorar la idea tras la noche).

### Moth — Just Suppose (`Moth_poem` reencuadrado)
- `openingLine`:
  «Imagina que cada farola del pueblo dejara caer un hilo de luz hasta el suelo, y se
   pudieran tocar. ¿Qué haría la gente? ¿Qué cambiaría aquí?»
- `exitCondition`: «El jugador explora al menos una consecuencia del supuesto.»
- (Supuesto original, no el de las nubes del TTCT: mismo tipo de tarea, ítem propio.)

### Moth — Asking Questions (nodo de situación)
- `openingLine`:
  «Cada mañana aparece una silla vacía en mitad de la plaza. Nadie la pone. Nadie la
   quita. Si quisieras entenderlo… ¿qué preguntarías?»
- `exitCondition`: «El jugador formula al menos una pregunta sobre la escena.»

### Rix — Guessing Causes (`Rix_open`)
- Añadir un cierre que invite a conjeturar causas, tras su confidencia:
  «…¿Y por qué crees tú que me pasa esto? Suéltalo, aunque sea una locura.»
- `exitCondition`: «El jugador propone al menos una causa posible.»

### Rix — Guessing Consequences (`Rix_aboutMoth`)
- Cierre que invite a anticipar:
  «Vale, listilla: si esto sigue así, ¿qué crees que va a pasar? Dame tu apuesta.»
- `exitCondition`: «El jugador propone al menos una consecuencia posible.»

---

## 5. Prompt del evaluador adaptado (resultado, ya generalizado)

Es el mismo prompt actual con las dos líneas cambiadas (sección 2). Estructura final que
se envía por cada mensaje en un nodo de reto:

```
You invisibly score a player's message in a cozy narrative game, for a
Torrance-style creativity profile. Be strict: use the FULL 0-10 range.
Character/situation right now: {contextForAI del nodo}
What counts as ONE countable {countableUnit} here: {ideaDomain}
World elements the player could weave in: flowers, bouquets, cooking/food, the
village, the neighbours, the day/night cycle, rumours, and emotions.
Previous player idea (only for ADAPTATION): "{mensaje anterior}"

Score the CURRENT message 0-10 on each dimension:
ORIGINALITY = rare/unexpected. DETAIL = concrete/specific. COHERENCE = fits the
problem and world. EMPATHY = considers the character's feelings. WORLDUSE = uses
world elements. RISK = dares something odd but sensible. ADAPTATION = improves or
revises the previous idea after pushback (0 if unrelated or no previous idea).
IDEA=yes only if the message contains a genuine {countableUnit} (no for greetings/off-topic/filler).
CATEGORY = one short word for the kind of {countableUnit}.
Reply in EXACTLY this pipe format, nothing else:
IDEA=yes|no ; ORIGINALITY=0-10 ; DETAIL=0-10 ; COHERENCE=0-10 ; EMPATHY=0-10 ; WORLDUSE=0-10 ; RISK=0-10 ; ADAPTATION=0-10 ; CATEGORY=word

Player message: "{mensaje}"
```

---

## 6. Coherencia con la memoria

Con esto, la afirmación "las seis subpruebas verbales están mapeadas (tres plenamente,
tres de forma ligera)" pasa a ser **verdad demostrable**:
- Plenas: Product Improvement (Aster), Unusual Uses (Mochi), Just Suppose (Moth), cada una
  con nodo de reto dedicado y contador de fluidez.
- Ligeras: Asking Questions (Moth), Guessing Causes y Consequences (Rix), enganchadas a
  nodos de situación existentes.

Y se puede defender que **no se replican los ítems del TTCT** sino que se implementan
análogos conversacionales originales, con el mismo tipo de demanda cognitiva.
