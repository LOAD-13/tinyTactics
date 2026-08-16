# Arquitectura técnica — Tiny Tactics

> Este documento registra **decisiones y su porqué**. Es la munición para las preguntas del docente:
> *"¿por qué eligieron esta solución y no otra?"* es una de las preguntas del balotario.

---

## 1. Visión general

```
┌─────────────────────────────────────────────────────┐
│  PRESENTACIÓN                                       │
│  Cámara · HUD · Minimapa · Menús · Animación        │
└───────────────────────┬─────────────────────────────┘
                        │ lee estado
┌───────────────────────▼─────────────────────────────┐
│  ENTRADA                                            │
│  Selección · Órdenes contextuales                   │
└───────────────────────┬─────────────────────────────┘
                        │ emite ÓRDENES (serializables)
┌───────────────────────▼─────────────────────────────┐
│  AUTORIDAD DE SIMULACIÓN                            │
│  Valida y aplica órdenes · Tick de simulación       │
└───────┬───────────────────────────────┬─────────────┘
        │                               │
┌───────▼─────────────┐   ┌─────────────▼─────────────┐
│  MUNDO              │   │  IA                       │
│  Grilla · Unidades  │   │  Estratégica (1 Hz)       │
│  Edificios · Recursos│  │  Táctica (3 Hz)           │
│  Niebla · Facciones │   │  (emite las mismas ÓRDENES)│
└─────────────────────┘   └───────────────────────────┘
```

**La idea que sostiene todo:** el jugador y la IA **no mueven unidades**. Ambos emiten
*órdenes* que la autoridad valida y aplica. Una unidad nunca lee el input directamente.

---

## 2. Decisiones

### ADR-01 · Toda acción es una orden serializable

**Decisión.** Las acciones del jugador se modelan como objetos `Orden` (mover, atacar, recolectar,
construir), no como llamadas directas a las unidades.

**Por qué.** Es lo único que mantiene abierta la puerta del PvP. Si las unidades leyeran el input
directamente, agregar red en la semana 12 significaría reescribir el core; con órdenes, significa
transportarlas por la red en vez de aplicarlas localmente.

**Beneficios colaterales:** es el **patrón Command** de manual (cubre la semana de patrones con
evidencia real), permite repetición de partidas, y hace que la IA use exactamente el mismo camino
que el jugador — si la IA puede hacer algo, el jugador también, y viceversa.

**Alternativa descartada.** Input directo a unidades. Más rápido de escribir hoy, muro infranqueable
en S12.

---

### ADR-02 · Movimiento: A* sobre grilla + interpolación + empuje blando

**Decisión.** El mapa es una grilla de tiles. El pathfinding es A* sobre esa grilla. Las unidades
**no saltan** de celda en celda: interpolan suavemente entre waypoints y se empujan blandamente
entre sí para no encimarse.

**Por qué.** Se ve como un RTS de verdad, pero cuesta como una grilla: barato, determinista y
depurable. Es exactamente lo que hacían los RTS 2D clásicos.

**Alternativas descartadas.**
- *NavMesh de Unity + RVO*: movimiento más orgánico, pero el NavMesh 2D es incómodo, la evasión
  local con 250 agentes es cara, **no es determinista** (mata el PvP) y depurar un atasco es un infierno.
- *Grilla estricta sin interpolar*: trivial, pero se ve rígido y desperdicia las animaciones de caminata del pack.

---

### ADR-03 · Prohibido el componente `Animator` en unidades

**Decisión.** Las unidades animan por **sprite-swap** con un script propio ligero.

**Por qué.** El `Animator` de Unity es el mayor costo por unidad a escala — del orden de ~0.1 ms
cada uno. Con 250 unidades es el primer cuello de botella del juego, y aparece justo en la
semana 13, cuando ya no hay tiempo para reescribir.

Las animaciones del pack son tiras de sprites simples; una máquina de estados propia con un índice
de frame y un temporizador hace exactamente lo mismo por una fracción del costo.

---

### ADR-04 · Grilla espacial para búsqueda de objetivos

**Decisión.** Las unidades se registran en una grilla espacial (spatial hash). Buscar enemigo
cercano consulta solo las celdas vecinas.

**Por qué.** La alternativa ingenua — recorrer todas las unidades por cada unidad — es O(n²).
Con 250 unidades son 62 500 comparaciones **por frame**. El juego se cae mucho antes de llegar
al alcance planeado.

**Prohibido:** `FindObjectsOfType` y `GameObject.Find` en cualquier código que corra por frame.

---

### ADR-05 · Pathfinding en cola

**Decisión.** Las peticiones de ruta entran a una cola y se resuelven **N por frame**.
Los grupos grandes comparten un *flow field* en vez de calcular una ruta por unidad.

**Por qué.** Una orden de movimiento sobre 50 unidades seleccionadas dispararía 50 A* en el mismo
frame y produciría un tirón visible. Repartirlo en varios frames es imperceptible para el jugador.

---

### ADR-06 · IA por capas, dificultad por economía

**Decisión.** Tres capas: estratégica (~1 Hz), táctica (~3 Hz), FSM por unidad (por frame).
Las dificultades se implementan como **multiplicador de recursos + timing de oleadas**.

**Por qué.** Es como funcionaban de verdad Warcraft, StarCraft y Age of Empires — que corrían
7 bots en hardware de los 90. El cerebro de la IA es barato precisamente porque **no piensa
cada frame**: piensa una vez por segundo, y eso basta para un RTS.

Y el dato incómodo pero cierto: en los RTS clásicos la dificultad alta **no es más inteligente,
hace trampa**. Recibe bonus de recursos y construye más rápido. Implementar "una IA más lista"
es un problema de investigación abierto; implementar tres multiplicadores es una tarde.

**Pendiente:** spike de investigación con fuentes antes de S11.

---

### ADR-07 · Datos de balance en `ScriptableObject`

**Decisión.** Stats de unidades, costos de edificios y parámetros de dificultad viven en
`ScriptableObject`, nunca como números en el código.

**Por qué.** La semana 16 es de balance puro. Si cada ajuste exige recompilar, se harán diez
iteraciones; si se editan desde el inspector, se harán cien. Además permite que Raúl ajuste
balance sin tocar código.

---

### ADR-08 · Sin física de Unity en las unidades

**Decisión.** Nada de `Rigidbody2D` para el movimiento de unidades. Las colisiones entre unidades
se resuelven a mano (empuje blando) y los proyectiles siguen una trayectoria calculada.

**Por qué.** `Rigidbody2D` introduce no-determinismo y comportamiento impredecible con 250 cuerpos.
Un RTS necesita que una unidad llegue exactamente donde se le ordenó.

**Nota para la sustentación:** esto **no** significa que el proyecto no aplique física. La aplica
por scripts — trayectoria de proyectiles y resolución de colisiones — que es un ejercicio más
profundo que colgar un `Rigidbody2D` y dejar que el motor decida.

---

## 3. Estructura de carpetas

```
Assets/
├── Scripts/
│   ├── Nucleo/          Autoridad de simulación, órdenes, tick
│   ├── Mundo/           Grilla, mapa, facciones, niebla
│   ├── Unidades/        FSM, stats, animación por sprite-swap
│   ├── Movimiento/      A*, cola de rutas, flow field, empuje
│   ├── Economia/        Recursos, recolección, almacenamiento
│   ├── Construccion/    Edificios, colocación, población
│   ├── Combate/         Daño, targeting, proyectiles, curación
│   ├── IA/              Capa estratégica, táctica, dificultades
│   ├── Entrada/         Selección, órdenes contextuales, cámara
│   ├── UI/              HUD, minimapa, menús
│   └── Datos/           ScriptableObjects de balance
├── Prefabs/             Unidades, edificios, proyectiles, efectos
├── Scenes/              Menu, Juego, mapas
├── Datos/               Instancias de ScriptableObject
└── Tiny Swords/         Assets originales del pack — NO se modifican
```

**Regla:** `Assets/Tiny Swords/` se deja intacto. Todo lo derivado (prefabs, animaciones,
atlas) vive fuera. Así se puede actualizar el pack sin perder trabajo.

---

## 4. Presupuesto de rendimiento

| Métrica | Objetivo |
|---|---|
| Unidades simultáneas | 250 (5 bandos × 50) |
| Framerate | ≥ 60 fps con 250 unidades en combate |
| Tick de simulación | 20 Hz, desacoplado del framerate |
| Tick estratégico de IA | 1 Hz por bando |
| Rutas A* por frame | ≤ 8 |

Estos números son **criterios de aceptación**, no aspiraciones: aparecen en las HUs
correspondientes y se verifican con el profiler.
