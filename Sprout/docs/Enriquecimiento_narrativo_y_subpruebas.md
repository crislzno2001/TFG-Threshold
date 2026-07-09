# Enriquecimiento narrativo v2: subpruebas que SÍ estimulan divergencia

Complemento de `Mapeo_TTCT_subpruebas.md`. Corrige la v1: las subpruebas flojas (que
producían respuestas obvias o educadas) se rediseñan sobre el principio real del TTCT.

## Principio de diseño (por qué la v1 fallaba)
Una tarea divergente necesita:
1. **Estímulo concreto y vívido** — una escena/objeto detallado sobre el que conjeturar.
   En el TTCT verbal las 3 primeras actividades comparten una lámina rica por esto. Sin
   escena, "¿por qué falló?" es humo. → La escena va escrita en el `contextForAI` del nodo,
   que el evaluador ya inyecta como `situation` y el NPC usa para responder.
2. **Consigna que pide CANTIDAD y RAREZA**, no la respuesta correcta ni la educada.
   - "usos raros de un ingrediente" → recetas (convergente). Unusual Uses real = usar un
     OBJETO para algo que NO es su función (romper la fijación funcional).
   - "¿y si no le gusta el plato?" → respuesta social educada. Guessing Consequences bueno
     cuelga de un supuesto **fantástico o de alto riesgo** (p. ej. un elixir), sin respuesta
     única.

---

## Mochi — dos tareas distintas (no mezclar)

### M1 · Fluidez + Flexibilidad (reto principal, culinario)
No pedir "una buena receta" (convergente). Pedir MUCHAS direcciones y premiar el salto de
categoría.
- `contextForAI`:
  `Mochi está en crisis: le han llegado ingredientes que no pegan (una flor amarga, una seta
   dulce, una raíz que no sabe identificar). No busca LA receta perfecta: quiere una tormenta
   de ideas y que el jugador salte de registro (dulce↔salado, elegante↔casero, bonito↔feo).`
- `openingLine`:
  «No quiero una receta buena. Quiero MUCHAS. Rápidas, sucias, raras. Y cuando te diga
   "otra", cambia de dirección: si ibas a dulce, vete a salado. No te cases con una idea.»
- Mide: **fluidez** (nº de combinaciones distintas) + **flexibilidad** (cambios de categoría).
- `countableUnit`: `culinary combination` · `counterKey`: `mochi_ideas_count`

### M2 · Unusual Uses (objeto concreto, NO recetas)
- `contextForAI`:
  `A Mochi se le ha roto la olla de cobre gigante de su abuela: enorme, abollada, ya no
   calienta, con un asa suelta. Se niega a tirarla. Pide usos INESPERADOS para el objeto,
   cuanto menos culinarios mejor (romper la fijación funcional).`
- `openingLine`:
  «Esta olla ya no cocina, pero no la tiro ni muerta. Dime para qué más podría servir —y
   cuanto menos tenga que ver con cocinar, más me gustas. Suéltame diez.»
- Mide: **originalidad + fluidez** (usos infrecuentes). `countableUnit`: `unusual use`
- Nodo: `extraChallengeNode` en el tracker de Mochi.

---

## Aster — Guessing Causes con escena concreta (giro del sabotaje)

El estímulo es el punto clave. La escena, con pistas ambiguas, va COMPLETA en `contextForAI`.

- **Nodo A2b — "¿Quién ha tocado esto?"** · Gate: `aster_ideas_count >= 1`
- `contextForAI` (esto es lo que "hay que decirle a la IA"):
  `El planeador de semillas de Aster (un aparato que debería planear y soltar semillas al
   caer) amaneció boca abajo en el suelo del taller. Pistas: un ala de tela rajada LIMPIA,
   como con tijeras, no rota; el depósito de semillas VACÍO, y Aster no lo vació; la puerta
   del taller estaba cerrada POR DENTRO. Aster está en pánico y no entiende qué pasó.`
- `openingLine`:
  «Mi planeador está en el suelo, un ala cortada limpia y el depósito vacío… y la puerta
   estaba cerrada por dentro. No lo entiendo. ¿Por qué crees que ha pasado? Dame teorías,
   TODAS, por locas que sean.»
- Mide: **Guessing Causes** (causas plausibles distintas). `countableUnit`: `possible cause`
- Flags: ★`aster_sabotaje_sospecha = true`
- Las pistas (corte limpio, depósito vacío, puerta cerrada) son las que abren muchas causas:
  sabotaje de un rival, un animal, el propio Aster dormido, las semillas germinaron y la
  reventaron, una broma, el viento por una ventana… Sin esas pistas no habría divergencia.

- **Nodo A2c — Asking Questions** · Gate: ★`aster_sabotaje_sospecha`
  - `openingLine`: «Voy a hablar con quien anduvo anoche por la plaza. Si fueras tú, ¿qué le
    preguntarías para pillarlo? Dame preguntas, muchas.» · `countableUnit`: `question`
- **Nodo A2d — Product Improvement (2.ª muestra)** · Gate: `aster_ideas_count >= 2`
  - «Ya que lo abro entero para arreglarlo… ¿cómo lo dejarías, mejor que antes?»

---

## Moth/Rix — Guessing Consequences con el elixir (supuesto fantástico)

Cuelga de la trama turbia de Moth: no es una duda social educada, es un supuesto imposible
con muchísimas ramas.

- **Nodo C5b — "El elixir"** · Gate: `moth_pidio_ayuda = true` AND `amistad_moth >= 3`
- `contextForAI`:
  `Moth, desesperada, fantasea con un elixir del amor REAL que podría echar a Rix en la
   bebida. No pregunta si está bien: quiere que el jugador explore qué pasaría si lo hiciera.
   Es también un espejo moral (el amor forzado no es amor).`
- `openingLine`:
  «Imagina que existiera un elixir del amor de verdad, y yo pudiera echárselo a Rix sin que
   se enterase. ¿Qué crees que pasaría? No me digas si está bien. Dime qué pasaría. Todo lo
   que se te ocurra.»
- Mide: **Guessing Consequences**. `countableUnit`: `possible consequence`
- Divergencia natural: la querría de mentira, se notaría, se le pasaría el efecto, querría a
  quien no debe, el pueblo entero, dependería para siempre… Y alimenta el dilema moral.

---

## Rix — Just Suppose "perroflauta" (supuesto improbable, tono actual)

Rix es rana punk/contracultural: le pega plantear utopías imposibles sobre el pueblo. Elegir
UNA como su reto de Just Suppose (o rotarlas):
- «Imagina que mañana se prohíbe el dinero en el pueblo. Todo trueque, todo gratis. ¿Cómo
  sería la vida aquí? Cuéntame, sin frenos.»
- «Imagina que nadie tuviera reloj ni supiera nunca qué hora es.»
- «Imagina que las casas no tuvieran dueño y cualquiera pudiera vivir en cualquiera.»
- `contextForAI`:
  `Rix, en modo filosófico-perroflauta, lanza un supuesto imposible sobre cómo cambiaría el
   pueblo. No busca una respuesta correcta: quiere que el jugador imagine consecuencias, las
   más y más raras posibles.`
- Mide: **Just Suppose**. `countableUnit`: `imagined consequence`
- Además abre química con la florista (encaja con la ruta secreta `rix_florista`).

---

## Cobertura resultante (muestras por subprueba)

| Subprueba | Muestra 1 | Muestra 2 |
|---|---|---|
| Fluidez/Flexibilidad | Mochi M1 | — |
| Unusual Uses | Mochi M2 (olla) | — |
| Product Improvement | Aster A2 | Aster A2d |
| Just Suppose | Moth (poema/imagen) | Rix (perroflauta) |
| Asking Questions | Aster A2c | Moth (silla vacía) |
| Guessing Causes | Aster A2b (planeador) | D2 (cotilleo sobre la florista) |
| Guessing Consequences | Moth C5b (elixir) | Rix C7c (¿qué pasará con Moth?) |

## Lo que hay que "decirle a la IA" (regla general)
Cada nodo de reto lleva su **escena concreta en `contextForAI`** (objeto, pistas, supuesto).
El evaluador ya la inyecta como `situation` y el NPC la usa para responder en personaje. Sin
esa escena rica, la tarea no estimula divergencia. Único cambio de código sigue siendo el
campo `countableUnit` (ver `Mapeo_TTCT_subpruebas.md`).
