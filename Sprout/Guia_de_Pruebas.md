# Sprout · Guía de pruebas

Checklist para probar todo lo implementado. Para cada sistema: **qué es**, **cómo probarlo** y **qué deberías ver**. Marca cada casilla cuando funcione.

---

## 1. Movimiento y animaciones de la florista
- **Cómo:** entra en Play y muévete con WASD; corre con Shift.
- **Qué ver:** la florista anda (Walking) y corre (Running) con la animación correcta, mira hacia donde se mueve, y se queda en Idle al parar. No debe "flotar" ni estar tiesa.
- [ ] Anda y corre con su animación
- [ ] Idle al parar
- [ ] No flota / no T-pose

## 2. Interacción con NPCs (acercarse + hablar)
- **Cómo:** acércate a un NPC (Mochi, Aster, Moth, Rix) y pulsa **E**.
- **Qué ver:** se abre el panel de diálogo con el nombre del NPC y su frase inicial.
- [ ] El detector marca al NPC como interactuable
- [ ] Al pulsar E se abre el diálogo

## 3. Diálogo con IA + grafos
- **Cómo:** escribe mensajes al NPC. Prueba a darle ideas, preguntar, despedirte.
- **Qué ver:**
  - El NPC responde con la IA (coherente con su personalidad).
  - Al despedirte (nodo terminal), el diálogo **se cierra solo** tras la última frase.
  - Si tienes router de entrada, al hablar arranca por la conversación del **día actual**.
- [ ] El NPC responde con IA
- [ ] Auto-cierre al despedirse
- [ ] Arranca por el nodo del día correcto

## 4. Flores y ramos
- **Cómo:** consigue flores, abre el inventario, monta un ramo y dáselo a un NPC.
- **Qué ver:** el inventario refleja las flores; al dar un ramo, el NPC reacciona según la combinación.
- [ ] Inventario de flores funciona
- [ ] Dar un ramo provoca reacción

## 5. Evaluación de creatividad (Torrance, invisible)
- **Cómo:** habla con un NPC dándole ideas variadas; el tracker mide en segundo plano (no se ve en pantalla).
- **Qué ver:** en la **consola** deberían aparecer logs del `CreativityTracker` registrando ideas (fluidez/flexibilidad/originalidad). No hay UI; es intencionado.
- [ ] La consola registra la actividad creativa

## 6. Bucle diario (dormir → recap → día siguiente)  ⭐
- **Cómo:** añade el componente **DailyLoopDebugger** a un objeto de la escena. En Play, usa el panel arriba a la izquierda.
- **Pasos:** pulsa el botón **"Sembrar cotilleos de prueba"** → luego **"Dormir → día siguiente"**.
- **Qué ver:** fundido a negro → recap nocturno con el resumen → "Continuar" → el panel pasa de **Día 1** a **Día 2**.
- [ ] Funde y avanza a la noche
- [ ] Sale el recap nocturno
- [ ] Cambia de día (Día 1 → Día 2)

## 7. Cotilleo nocturno (gossip)
- **Cómo:** con el debugger, "Sembrar cotilleos" antes de dormir.
- **Qué ver:** el recap muestra líneas reales, p. ej. *"Hoy alguien habló bien de tu sinceridad…"* y *"El pueblo notó una pequeña bondad floreciendo…"*. Cambian relaciones por debajo.
- [ ] Aparecen cotilleos en el recap

## 8. Puertas e interiores (estilo Animal Crossing)
- **Cómo:** coloca un **DoorPortal** en una puerta del pueblo apuntando a una escena interior, y un **SpawnPoint** en cada escena. Acércate y pulsa E.
- **Qué ver:** fundido → carga la escena interior → apareces en el SpawnPoint. La puerta de salida te devuelve al pueblo.
- [ ] Entrar a un interior funciona
- [ ] Salir devuelve al pueblo

## 9. Guardado de partida
- **Cómo:** el guardado se hace automáticamente **al llegar la noche** (al dormir). Reinicia y carga.
- **Qué ver:** se conserva el día, las flags, el inventario y la memoria de los NPCs (fichero JSON).
- [ ] Guarda al dormir
- [ ] Carga el estado correcto

## 10. Finales
- **Cómo:** avanza hasta completar el último día (con el debugger, duerme 3 veces).
- **Qué ver:** al terminar el día final se resuelve y muestra un final según las flags/relaciones.
- [ ] Se dispara un final tras el día 3

## 11. Pausa (ESC)  ⚠ a revisar
- **Cómo:** pulsa **ESC** en Play.
- **Qué ver:** debería abrirse el menú de pausa.
- [ ] ESC abre la pausa  *(ahora mismo no va — pendiente de arreglar)*

---

### Notas
- Quita el **DailyLoopDebugger** (o desactívalo) antes de entregar; es solo para pruebas.
- Si algo no responde, mira siempre primero la **consola** por errores en rojo: suelen ser un componente sin asignar o un manager que falta en la escena.
