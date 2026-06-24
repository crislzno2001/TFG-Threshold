# Estilo visual: shader de mundo curvado y post-procesado *cozy*

Este capítulo describe dos de las decisiones técnico-artísticas que definen la identidad visual de *Sprout*: el **shader de mundo curvado**, que deforma la geometría del escenario para evocar un mundo pequeño y acogedor al estilo de *Animal Crossing*, y el **post-procesado *cozy***, una capa de corrección de imagen que unifica el aspecto del juego y refuerza su tono cálido. Ambos sistemas son, además, un buen ejemplo de cómo una decisión estética se traduce en una implementación concreta dentro de Unity y la Universal Render Pipeline (URP).

## 1. El shader de mundo curvado (*Curved World*)

### 1.1. Motivación

Uno de los objetivos de dirección artística de *Sprout* era transmitir la sensación de un mundo **pequeño, contenido y acogedor**, coherente con su naturaleza *cozy* y con una protagonista *chibi*. Los juegos que persiguen ese efecto —*Animal Crossing* es el referente más claro— recurren con frecuencia a curvar el suelo de modo que el horizonte se "doble" hacia abajo y desaparezca de la vista. El resultado es doble: por un lado se oculta el horizonte lejano, que rompería la ilusión de un espacio íntimo; por otro, se genera la lectura subconsciente de estar sobre un pequeño planeta o una maqueta, lo que aporta calidez y carácter de juguete.

En lugar de modelar un terreno físicamente curvo —algo costoso y poco flexible— se optó por un **shader que deforma la geometría en tiempo de render**. La malla permanece plana en disco; es el propio material el que la curva visualmente. Esto permite aplicar y ajustar el efecto sobre todo el escenario sin tocar la geometría original ni la lógica de juego (colisiones, navegación, posiciones), que siguen operando sobre el mundo "real" sin curvar.

### 1.2. Fundamento técnico

La curvatura se implementa íntegramente en la **etapa de vértices** del shader. Antes de que cada vértice se proyecte a pantalla, el shader desplaza su posición hacia abajo en función de lo lejos que se encuentre de un punto de referencia (el jugador). Cuanto más lejos está un vértice de ese punto, más se hunde, y ese hundimiento crece de forma **no lineal**: los objetos cercanos apenas se ven afectados, mientras que los lejanos caen de manera pronunciada, dibujando una curva parabólica que imita la convexidad de un horizonte cercano.

El uso exclusivo de la etapa de vértices es importante por dos motivos. Primero, es muy eficiente: la deformación se calcula una vez por vértice, no por píxel. Segundo, no altera el color ni la iluminación del material, de modo que la curvatura puede combinarse con cualquier modelo de sombreado físicamente realista (PBR) sin interferencias.

### 1.3. Algoritmo

El shader se construyó con **Shader Graph** sobre la Universal Render Pipeline, empleando el subtarget *Lit* (iluminación PBR completa). La deformación se obtiene encadenando los siguientes nodos, que reproducen paso a paso la idea anterior:

1. Se obtiene la **posición del vértice** (nodo *Position*) y se le resta el punto de referencia `_CurveOrigin` mediante un nodo *Subtract*. El resultado es el vector de desplazamiento del vértice respecto al jugador.
2. Ese vector se descompone con un nodo *Split* para operar sobre sus componentes y, apoyándose en el nodo *Camera*/*Transform*, se trabaja con la distancia relevante respecto al punto focal.
3. La distancia se eleva con un nodo *Power*, lo que produce la **caída parabólica** característica (los objetos cercanos casi no se mueven; los lejanos se hunden mucho).
4. El resultado se escala con la propiedad `_CurveStrength` mediante un nodo *Multiply*, que controla globalmente la intensidad de la curvatura.
5. Un nodo *Negate* invierte el signo para que el desplazamiento sea **hacia abajo**, y un nodo *Add* lo suma a la posición original.
6. La posición resultante se reinyecta en el bloque *Vertex Position*, de modo que la malla se dibuja ya curvada.

El shader expone cuatro propiedades de control: `_CurveOrigin` (el centro de la curvatura), `_CurveStrength` (su intensidad), y `_Tiling`/`_Offset` (que gobiernan el mapeo de las texturas mediante un nodo *Tiling And Offset*).

Conviene matizar un detalle del algoritmo coherente con el referente: el efecto se aplica fundamentalmente sobre el eje de profundidad (avance y retroceso del jugador), no sobre el desplazamiento lateral. Esto reproduce el comportamiento real de *Animal Crossing*, cuyo mundo no es una esfera perfecta sino un cilindro que se dobla hacia delante y hacia atrás, manteniendo siempre al jugador y a la cámara en el vértice de la curva.

**Atribución y aportaciones propias.** La técnica base de curvatura por desplazamiento de vértices no es original de este trabajo: parte de un enfoque ampliamente documentado en la comunidad de Unity. En concreto, se tomó como punto de partida un tutorial divulgativo[^curved] que, a su vez, se apoya en shaders previos de la comunidad. Sobre esa base, *Sprout* incorpora varias aportaciones propias que extienden el shader original —que se limitaba al color base— hasta convertirlo en un sistema completo: (i) un **modelo de materiales PBR** con mapa de normales, mapa metallic/smoothness empaquetado y tinte de color; (ii) **tres variantes especializadas** —recorte por alfa a doble cara para la vegetación, adaptación para el *Terrain* de Unity y una variante transparente para el agua— que cubren todos los tipos de superficie del escenario; y (iii) un **sistema de gestión centralizado y autogestionado**, basado en propiedades globales y recolección automática de materiales, que elimina el mantenimiento manual. La idea de la curvatura es, por tanto, heredada; su integración como sistema completo, robusto y adaptado a las necesidades del juego constituye la aportación propia.

[^curved]: Tutorial de referencia: «Let's learn how to make a curved Shader graph like Animal Crossing in Unity», disponible en <https://www.youtube.com/watch?v=GF0_1-8NWBs>.

### 1.4. Integración en tiempo de ejecución

Para que el efecto sea coherente, el centro de la curvatura debe seguir continuamente a la protagonista: el mundo ha de curvarse **siempre alrededor del jugador**, de manera que este permanezca en la "cima" de la pequeña esfera y el horizonte se doble de forma simétrica a su alrededor.

De ello se encarga el componente `CurvedWorldOriginSetter`. En cada `LateUpdate` —después de que el jugador se haya movido— actualiza la propiedad `_CurveOrigin` con la posición actual del jugador y la `_CurveStrength` global, de modo que un único componente gobierna la curvatura de todo el escenario y basta modificar un valor para ajustar la intensidad del mundo entero a la vez.

El diseño de este componente evolucionó de forma significativa durante el desarrollo, y esa evolución ilustra un principio de ingeniería relevante: **eliminar el mantenimiento manual propenso a errores**. En una primera versión, el componente mantenía una lista de materiales asignada a mano en el inspector; cada vez que un objeto pasaba a usar el shader curvado había que recordar añadirlo a esa lista, lo que resultaba tedioso y fácil de olvidar. La versión final automatiza por completo este proceso combinando dos mecanismos complementarios:

1. **Propiedades globales.** `_CurveOrigin` y `_CurveStrength` se declaran con ámbito *Global* en el shader y se actualizan una sola vez por fotograma mediante `Shader.SetGlobalVector` / `Shader.SetGlobalFloat`. Una propiedad global alcanza automáticamente a **cualquier** material que la use, sin necesidad de mantener referencia alguna; como contrapartida estética, deja de mostrarse en el inspector de cada material, centralizando su control en el manager.
2. **Recolección automática.** Como salvaguarda, el componente recolecta de la escena, al cargarla, todos los materiales cuyo shader pertenezca a la familia *CurvedWorld* (incluyendo el material del *Terrain*) y les aplica los valores directamente. Como ese conjunto no cambia durante la partida, en ejecución el escaneo se realiza **una sola vez**; el re-escaneo periódico se limita al editor, como comodidad para recoger los materiales que se vayan convirtiendo al shader mientras se compone la escena. Así, cualquier material que se convierta al shader curvado queda incluido **solo**, sin intervención manual.

El componente se marca además con `[ExecuteAlways]`, de modo que la curvatura se actualiza también en el editor —no solo en ejecución—, lo que permite previsualizar el efecto mientras se compone la escena.

```csharp
[ExecuteAlways]
public class CurvedWorldOriginSetter : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float curveStrength = 0.01f;

    private static readonly int CurveOriginID   = Shader.PropertyToID("_CurveOrigin");
    private static readonly int CurveStrengthID = Shader.PropertyToID("_CurveStrength");
    private readonly List<Material> _materials = new();

    private void LateUpdate()
    {
        if (player == null) return;
        Vector3 origin = player.position;

        // Global: alcanza a todo material con _CurveOrigin/_CurveStrength en ámbito Global.
        Shader.SetGlobalVector(CurveOriginID, origin);
        Shader.SetGlobalFloat(CurveStrengthID, curveStrength);

        // Recolección automática (los materiales curvados se incluyen solos).
        foreach (var m in _materials)
            if (m != null)
            {
                m.SetVector(CurveOriginID, origin);
                m.SetFloat(CurveStrengthID, curveStrength);
            }
    }
}
```

Este enfoque elimina por completo la lista manual: añadir un objeto curvado al mundo se reduce a asignarle el shader, y el sistema lo integra automáticamente.

### 1.5. Variantes del shader

Un único shader no es suficiente para todo el escenario, porque distintos tipos de superficie tienen requisitos de render incompatibles entre sí. Por ello no se desarrolló un solo shader, sino toda una **familia de cinco shaders** que comparten exactamente la misma lógica de curvatura en la etapa de vértices, pero difieren en el modo de superficie, en los canales de textura que exponen y en el tratamiento de la transparencia:

- **`CurvedWorld`** — variante completa y opaca, con sombreado PBR íntegro: color base (`_Albedo`), mapa de normales (`_Normal`), mapa metallic/smoothness empaquetado (`_MetallicSmoothness`) y tinte de color (`_BaseColor`). Es la versión de referencia para superficies sólidas con detalle, como edificios y *props*.
- **`CurvedWorld_Albedo`** — variante opaca simplificada que solo expone el color base, pensada para materiales planos sin texturas de detalle (por ejemplo, la floristería de la protagonista, modelada con colores planos). Reduce el coste y el número de propiedades cuando el detalle PBR no es necesario.
- **`CurvedWorld_Cutout`** — añade **recorte por alfa** (*alpha clipping*) y renderizado a **doble cara**. Es imprescindible para la vegetación: las hojas y ramas se representan como planos texturizados cuyo canal alfa define la silueta real de la hoja; sin recorte se verían como rectángulos sólidos, y la doble cara permite verlas por ambos lados.
- **`CurvedWorld_Terrain`** — adaptación para el sistema de *Terrain* de Unity, que utiliza un esquema de materiales propio basado en *splatmaps* (mezcla de capas de textura). Permite que el suelo pintado se curve junto con el resto del mundo conservando el pintado.
- **`CurvedWorld_Water`** — variante **transparente** con mezcla por alfa, empleada para el agua. Combina la curvatura con un color translúcido, un mapa de normales de oleaje y una alta suavidad para simular una superficie acuática.

La existencia de esta familia de variantes es una consecuencia directa de las limitaciones de la pipeline: la curvatura es la misma en todas, pero el modo de superficie (opaco, simplificado, recortado, terreno o transparente) debe declararse de forma distinta en cada caso. Mantener una lógica de curvatura común entre todas ellas garantiza, además, que todo el mundo se doble de manera coherente con un único valor de intensidad.

### 1.6. Modelo de materiales

Las variantes opaca y de recorte siguen un modelo PBR estándar, con cuatro entradas de textura y una de color:

- **`_Albedo`** — color base de la superficie.
- **`_Normal`** — mapa de normales para el detalle de relieve. Cuando un material carece de él, la propiedad se configura en modo *Normal Map* para devolver una normal plana neutra en lugar de un valor erróneo.
- **`_MetallicSmoothness`** — mapa empaquetado donde el **canal rojo codifica el grado metálico** y el **canal alfa la suavidad** (*smoothness*). Empaquetar ambos valores en una sola textura ahorra memoria y accesos de muestreo.
- **`_BaseColor`** — color de tinte que multiplica al albedo. Resulta esencial para la vegetación del pack utilizado, cuyas texturas de hojas son **acromáticas a propósito**: el color verde no está en la textura, sino que se aplica mediante este tinte, lo que permite reutilizar una misma textura para distintos tonos de follaje.

### 1.7. Retos de integración y decisiones de diseño

La adopción del shader sobre un escenario importado de un *asset pack* diseñado para la *Built-in Render Pipeline* obligó a resolver varios problemas que ilustran el funcionamiento interno de Shader Graph:

- **Propiedades de textura vacías.** En Shader Graph, una textura no asignada devuelve por defecto un valor blanco. Sobre el canal metálico esto se traducía en un grado metálico máximo y, sin reflejos en la escena, en superficies completamente **negras**; sobre el canal de normales, en un sombreado erróneo. La solución pasó por asignar texturas neutras a los huecos vacíos y por configurar el modo por defecto de cada propiedad (normal plana, metálico nulo).
- **Mapeo de texturas (*tiling*).** Un valor de *tiling* igual a cero colapsa las coordenadas UV y muestrea un único téxel, produciendo superficies de **color plano**. Fijar el *tiling* a la unidad restauró el detalle.
- **Recorte de la vegetación.** El aspecto correcto de las hojas exige conectar específicamente el **canal alfa** del albedo al bloque *Alpha* y ajustar el umbral de recorte; de lo contrario se renderiza el plano rectangular completo.

Estas incidencias, lejos de ser anecdóticas, evidencian que portar contenido entre pipelines de render no es una operación automática, sino que requiere comprender cómo cada motor interpreta los canales de los materiales.

## 2. Post-procesado *cozy*

### 2.1. Objetivo

*Sprout* combina un escenario de estilo pintado y semidetallado con personajes *chibi* de sombreado más liso. Sin un tratamiento común, esa diferencia de estilos podía leerse como una mezcla incoherente. El **post-procesado** se introdujo para **unificar ambos registros bajo una misma atmósfera** y, al mismo tiempo, reforzar el tono cálido y acogedor que persigue el juego. El principio de diseño es que la cohesión visual de una escena depende menos del estilo concreto de cada elemento que de que **compartan la misma iluminación y el mismo tratamiento de color**.

### 2.2. Implementación

El efecto se construyó con el sistema de **Volúmenes** de URP. Un *Volume* global aplica a toda la escena un perfil de post-procesado compuesto por los siguientes efectos, activados sobre la cámara del jugador:

- **Tonemapping (Neutral).** Mapea el rango de color de forma equilibrada, evitando que las zonas claras se "quemen" o que los colores se laven.
- **Color Adjustments.** Aplica un ligero filtro de color cálido, junto con un leve aumento de saturación y contraste, para dar vida y temperatura a la imagen.
- **White Balance.** Desplaza el balance de blancos hacia tonos templados, lo que aporta la sensación de luz cálida característica de un ambiente acogedor.
- **Bloom.** Un resplandor suave en las zonas más luminosas que produce un acabado amable y de ensueño, coherente con la estética *cozy*.
- **Vignette.** Un sutil oscurecimiento de los bordes que centra la atención en el personaje y la acción.

### 2.3. Justificación

Cada efecto responde a una intención concreta: el *tonemapping* y el balance de blancos fijan la **temperatura emocional** de la imagen; el ajuste de color y el *bloom* aportan la calidez y el carácter onírico; y la viñeta dirige la mirada. En conjunto, "abrazan" a personajes y escenario bajo una misma capa de color e iluminación, de modo que el contraste de estilos deja de percibirse como un error y pasa a leerse como una elección estética. Al tratarse de un *Volume* global con un perfil editable, todos estos parámetros pueden afinarse de forma centralizada y no destructiva, lo que facilitó la iteración artística.
