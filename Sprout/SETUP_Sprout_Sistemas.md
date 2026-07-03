# SPROUT — Checklist de configuración (al abrir Unity)

Todo el código y los grafos ya están hechos. Esto es solo **arrastrar/asignar en el Inspector**.

---

## 1. En la ESCENA (managers) — una vez

Añade estos componentes a un objeto de la escena (p. ej. tu objeto de managers):

- [ ] **Npc Spotlight** (uno en la escena) → controla los brillos.
- [ ] **Spotlight Flag Bridge** (uno) → traduce los flags de los grafos a brillos.
- [ ] **Night Sleep Notice** (uno) → aviso "ve a dormir" al entrar la noche.
- [ ] **DayCycleService** → `Count Talk As Phase Progress`:
  - **ON** = la fase avanza al hablar con los 4 NPCs (fácil, recomendado para empezar).
  - **OFF** = solo avanza cuando cada NPC llega a su nodo de cierre (necesita el Phase Done Reporter, ver punto 4).
- [ ] **NPCBrainFlagBridge** → comprueba que los 4 NPC brains están asignados en su lista.
- [ ] **EndingService** → evento **`On Ending Summary`** → conéctalo a un TMP_Text de tu pantalla final (muestra las frases poéticas del resumen).

## 2. En CADA NPC (Mochi, Aster, Moth, Rix)

- [ ] **Npc Glow** → pon el **NpcId** correcto (Mochi / Aster / Moth / Rix).
- [ ] **Creativity Tracker** → `Ideas Node` y `Counter Key`:

| NPC   | Ideas Node        | Counter Key        | Extra Challenge Nodes |
|-------|-------------------|--------------------|-----------------------|
| Mochi | `Mochi_ideas`     | `mochi_ideas_count`| `Mochi_night_reto`    |
| Aster | `Aster_ideas`     | `aster_ideas_count`| —                     |
| Moth  | `Moth_poem`       | `moth_friendship`  | —                     |
| Rix   | (sin reto; déjalo vacío) | —           | —                     |

- [ ] **Npc Animator Driver** → ya está (de la parte de animación).

## 3. Confrontaciones — `Dialogue Entry Router` → lista `Entries By Flag`

Esto hace que, si pasó algo en el cotilleo, al hablar con el NPC entre por la escena correcta:

- [ ] **Aster** → Flag = `aster_angry`, Node = `Aster_gossip`
- [ ] **Rix** → Flag = `helped_moth_lie`, Node = `Rix_alerta`

## 4. (OPCIONAL) Pacing por fase día/noche

Solo si quieres el flujo mañana/mediodía/tarde/noche fino:

- [ ] En cada `Dialogue Entry Router` → `Entries By Phase`: Morning→intro, Midday→reto, Afternoon→consecuencia, (Mochi) Night→`Mochi_night_reto`.
- [ ] Si pusiste `Count Talk As Phase Progress = OFF`: añade **Npc Phase Done Reporter** a cada NPC y arrastra su nodo de despedida (`Mochi_bye`, `Aster_bye`, `Moth_bye`, `Rix_bye`) a `Phase Done Nodes`.

---

## Cómo fluye todo (para que lo veas mientras pruebas)

- **Brillos**: al conocer a un NPC brilla **verde** (tiene contenido). Al despedirse te **manda a otro** (referido, brilla **azul**). El cotilleo nocturno enciende **rojo** si hay confrontación.
- **Cadena de referidos**: Aster → Moth → Rix → Aster (y Mochi → Rix). Te van guiando solos.
- **Cotilleo nocturno**: propaga flags entre NPCs (confrontaciones, reputación) y verás un resumen vago al dormir.
- **Final**: frases poéticas según originalidad / empatía / manipulación.

---

## Aparte (de antes, no de este bloque)

- [ ] NPCs a rig **Humanoid** + Controller `Player_Anim` (o `StarterAssetsThirdPerson`) + `NpcAnimatorDriver`.
- [ ] **NavMesh** horneado (para que paseen).
- [ ] Shader **CurvedWorld** en los materiales NPC (para que no floten) — `Tools/Sprout/NPCs use Curved World shader`.
- [ ] `SproutSetupFlowerLoop` y demás herramientas de `Tools/Sprout/...` si aún no las corriste.
