# Backlog — Tiny Tactics

> **Backlog vivo.** Solo la semana en curso y la siguiente tienen HUs detalladas con criterios.
> El resto son títulos. Escribir hoy las HUs de la semana 15 es trabajo que se va a tirar.

Numeración global correlativa. La épica es un campo, no un prefijo.

---

## Épicas

| ID | Épica | Semanas | Estado |
|---|---|---|---|
| `E01` | Fundación del proyecto | 02 | 🟢 Cerrada |
| `E02` | Núcleo de simulación — grilla, A*, movimiento, selección, órdenes | 03 | 🟢 Cerrada |
| `E03` | Unidades y animación | 04 | ⚪ Pendiente |
| `E04` | Economía | 05 | ⚪ Pendiente |
| `E05` | Construcción y producción | 06 | ⚪ Pendiente |
| `E06` | Combate | 07-08 | ⚪ Pendiente |
| `E07` | Percepción — niebla y minimapa | 09 | ⚪ Pendiente |
| `E08` | IA rival | 10-11 | ⚪ Pendiente |
| `E09` | Flujo de partida y FFA | 12-13 | ⚪ Pendiente |
| `E10` | Presentación — audio, VFX, UI | 14-15 | ⚪ Pendiente |
| `E11` | Calidad — balance, QA, build | 16-17 | ⚪ Pendiente |
| `E12` | PvP LAN *(condicional — compuerta S11)* | 12-14 | ⚫ Condicional |
| `E13` | Editor de mapas | 12-13 | ⚪ Pendiente |

Leyenda: 🔵 en curso · 🟢 cerrada · ⚪ pendiente · ⚫ condicional

---

## Formato de HU

```markdown
### HU-NNN · Título
**Épica:** EXX · **Semana:** NN · **Responsable:** Nombre · **Rama:** `tipo/HU-NNN-slug`

**Como** <rol> **quiero** <capacidad> **para** <beneficio>.

**Criterios de aceptación**
- [ ] Verificable, no opinable.

**Evidencia PPT:** qué captura o demo sale de acá, y en qué slide va.
```

Reglas de los criterios:
- **Verificables, nunca opinables.** "Se ve bien" ❌ · "el fps no baja de 60" ✅
- Entre 3 y 7 por HU.
- Al menos uno **demostrable en vivo** — el docente puede pedir que se ejecute.

---

# Semana 02 — Fundación (E01)

**Meta de la semana:** el proyecto existe formalmente, está versionado con GitFlow, y hay un
mapa que se puede recorrer con la cámara. La expo es conceptual por naturaleza, pero cierra
con algo corriendo.

> ✅ **Épica cerrada.** Las cinco HUs entregadas en el tag `v0.2.0-s02`. HU-003 superó su
> alcance: en vez de un mapa pintado a mano salió un generador procedimental completo.
> El detalle de lo que costó está en [`BITACORA.md`](BITACORA.md).

### HU-001 · Documentación base del proyecto
**Épica:** E01 · **Semana:** 02 · **Responsable:** Joaquín · **Rama:** `docs/HU-001-documentacion-base`

**Como** equipo **queremos** tener la documentación del proyecto versionada en el repo
**para** que las decisiones estén escritas y el docente pueda verificarlas.

**Criterios de aceptación**
- [ ] Existen `README.md` y `docs/` con GDD, CRONOGRAMA, BACKLOG, GITFLOW, ARQUITECTURA y BITACORA.
- [ ] El `README.md` explica qué es el juego, cómo abrirlo y quién lo hizo.
- [ ] `.gitignore` excluye `Library/`, `Temp/`, `Logs/` y las notas locales de trabajo.
- [ ] `git status` en limpio no muestra ningún archivo generado por Unity.

**Evidencia PPT:** captura del árbol de `docs/` en GitHub → slide de arquitectura/proceso.

---

### HU-002 · Estructura de carpetas del proyecto Unity
**Épica:** E01 · **Semana:** 02 · **Responsable:** Joaquín · **Rama:** `chore/HU-002-estructura-carpetas`

**Como** desarrollador **quiero** una estructura de carpetas definida desde el inicio
**para** no tener que reorganizar 200 archivos en la semana 10.

**Criterios de aceptación**
- [ ] Existen las carpetas de `Assets/Scripts/` según [`ARQUITECTURA.md`](ARQUITECTURA.md) §3.
- [ ] Existen `Assets/Prefabs/`, `Assets/Scenes/` y `Assets/Datos/`.
- [ ] `Assets/Tiny Swords/` queda **intacto**, sin archivos propios dentro.
- [ ] El proyecto abre en Unity sin errores de consola.

**Evidencia PPT:** captura del Project window de Unity → slide de arquitectura.

---

### HU-003 · Escena de juego con el mapa base
**Épica:** E01 · **Semana:** 02 · **Responsable:** Joaquín · **Rama:** `feat/HU-003-generacion-de-mapa`

**Como** jugador **quiero** ver un mapa con terreno, agua y vegetación
**para** tener un escenario reconocible donde ocurrirá la partida.

**Criterios de aceptación**
- [ ] Existe la escena `Assets/Scenes/Juego.unity`.
- [ ] El mapa usa Tilemap con el tileset de Tiny Swords: al menos tierra, agua y algo de vegetación.
- [ ] El mapa mide como mínimo 40×40 tiles. *(Entregado: 224×224.)*
- [ ] Los sprites se ven nítidos: filtro **Point (no filter)**, compresión **None**, píxeles por unidad consistente.
- [ ] El orden de capas es correcto — la vegetación no queda tapada por el terreno.

**Evidencia PPT:** captura del mapa completo en el editor → slide de concepto/mapa.

---

### HU-004 · Cámara RTS con paneo y zoom
**Épica:** E01 · **Semana:** 02 · **Responsable:** Joaquín · **Rama:** `feat/HU-003-generacion-de-mapa`

> ℹ️ **Comparte rama con HU-003**, declarado antes de empezar. El generador construye la cámara
> dentro de la escena, así que no se puede mergear una sin la otra. Ver [`GITFLOW.md`](GITFLOW.md) §5.4.

**Como** jugador **quiero** mover y acercar la cámara libremente
**para** poder observar cualquier zona del mapa durante la partida.

**Criterios de aceptación**
- [ ] WASD y las flechas mueven la cámara.
- [ ] Acercar el cursor a menos de 20 px del borde de la pantalla mueve la cámara en esa dirección.
- [ ] La rueda del mouse hace zoom entre un mínimo y un máximo configurables.
- [ ] La cámara **no puede salirse** de los límites del mapa en ningún nivel de zoom.
- [ ] El movimiento es suave (interpolado), no a saltos.
- [ ] La velocidad de paneo se ajusta al zoom: alejado se mueve más rápido.

**Evidencia PPT:** vídeo corto o GIF recorriendo el mapa → slide de demo.

---

### HU-005 · Ficha conceptual del videojuego
**Épica:** E01 · **Semana:** 02 · **Responsable:** Kiara · **Rama:** — *(ver nota)*

> ⚠️ **Entregada dentro de `docs/HU-001-documentacion-base`.** La ficha conceptual vive en
> `GDD.md`, un único archivo, y separarla en su propia rama habría sido burocracia sin valor.
> Se deja registrado aquí en vez de crear una rama vacía para simular trazabilidad.

**Como** equipo **queremos** la ficha conceptual completa
**para** cubrir lo que el docente pide en la semana 2 y fijar el diseño.

**Criterios de aceptación**
- [ ] `GDD.md` define género, público objetivo, perspectiva, motor y duración de partida.
- [ ] Están escritas las reglas, la condición de victoria y la de derrota.
- [ ] Están las 5 unidades con sus stats iniciales y el triángulo de contadores.
- [ ] Están los 6 edificios con sus costos.
- [ ] Los 3 recursos tienen un propósito definido (incluida la carne).
- [ ] Los recursos externos están identificados con autor y licencia.

**Evidencia PPT:** tabla de unidades y triángulo de contadores → slides de concepto y pilares.

---

# Semana 03 — Núcleo de simulación (E02)

> ✅ **Épica cerrada.** Los dos bloques entregados en el tag `v0.3.0-s03`, más dos HUs que no
> estaban en el plan: **HU-017** (interfaz de selección) y **HU-018** (relieve editable).
> El detalle honesto de lo que costó está en [`BITACORA.md`](BITACORA.md).

**Meta de la semana:** seleccionar un grupo de unidades y mandarlas a un punto del mapa,
esquivando obstáculos y sin encimarse. Es el corazón del RTS.

> **Alcance ampliado con los desniveles.** Se decidió meter también acantilados y escaleras
> esta semana. Van en un segundo bloque y con prioridad menor: si algo se complica, se recorta
> el terreno, nunca el núcleo. La grilla se diseña con nivel por celda desde el principio, así
> que el bloque de desniveles no obliga a rehacer nada.
>
> **HU-013 es nueva.** No existía en el plan original y hace falta: hoy no hay unidades en el
> juego, solo sprites decorativos junto a los castillos. Sin unidades reales no hay nada que
> seleccionar ni mover.

## Bloque A — Núcleo (prioridad 1)

| HU | Título | Rama |
|---|---|---|
| HU-006 | Grilla lógica con nivel por celda | `feat/HU-006-grilla-logica` |
| HU-007 | Pathfinding A* con cola | `feat/HU-007-pathfinding-astar` |
| HU-013 | Unidad seleccionable: datos, prefab y spawn | `feat/HU-013-unidades` |
| HU-008 | Movimiento interpolado | `feat/HU-008-movimiento` |
| HU-009 | Empuje blando entre unidades | `feat/HU-009-empuje-blando` |
| HU-010 | Selección por clic e indicador | `feat/HU-010-seleccion` |
| HU-011 | Caja de arrastre y shift | `feat/HU-011-seleccion-caja` |
| HU-012 | Orden de movimiento contextual | `feat/HU-012-orden-movimiento` |
| HU-017 | Interfaz de selección con los assets del pack | `feat/HU-017-interfaz-seleccion` |
| HU-018 | Relieve editable a mano | `feat/HU-018-relieve-editable` |

> **HU-017 es nueva.** Tampoco estaba en el plan. Salió de una observación en el laboratorio:
> el núcleo funcionaba pero no se *veía* funcionar — un anillo dibujado por código y ningún
> dato en pantalla. El pack ya trae punteros, corchetes, barras y retratos; usarlos convierte
> la demo en algo presentable y adelanta trabajo de la semana 15.

## Bloque B — Desniveles (prioridad 2)

| HU | Título | Rama |
|---|---|---|
| HU-014 | Generación de mesetas sobre el mapa | `feat/HU-014-mesetas` |
| HU-015 | Autotile de terreno elevado y acantilados | `feat/HU-015-acantilados` |
| HU-016 | Escaleras y tránsito entre niveles | `feat/HU-016-escaleras` |

> Varias HUs compartirán rama cuando sean técnicamente inseparables — se declara antes,
> según [`GITFLOW.md`](GITFLOW.md) §5.4.

### HU-006 · Grilla lógica del mapa
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-006-grilla-logica`

**Como** sistema **necesito** una representación lógica del mapa en celdas
**para** poder calcular rutas y saber qué terreno es transitable.

**Criterios de aceptación**
- [ ] La grilla se construye leyendo el Tilemap al iniciar la escena.
- [ ] Cada celda sabe si es transitable (el agua y los obstáculos no lo son).
- [ ] Existen conversiones mundo↔celda y celda↔mundo, verificadas en ambos sentidos.
- [ ] Un gizmo de depuración dibuja las celdas no transitables en el editor.

**Evidencia PPT:** captura del gizmo sobre el mapa → slide técnico.

---

### HU-007 · Pathfinding A* sobre la grilla
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-007-pathfinding-astar`

**Como** unidad **necesito** calcular una ruta hasta un destino
**para** llegar rodeando los obstáculos en vez de atravesarlos.

**Criterios de aceptación**
- [ ] A* devuelve la ruta más corta entre dos celdas transitables.
- [ ] Si el destino es intransitable, devuelve la celda transitable más cercana.
- [ ] Si no existe ruta, devuelve vacío sin lanzar excepción ni colgar el juego.
- [ ] Las peticiones entran a una **cola** y se resuelven como máximo 8 por frame (ADR-05).
- [ ] Una ruta de extremo a extremo en un mapa de 40×40 se resuelve en menos de 5 ms.
- [ ] Un gizmo dibuja la ruta calculada de la unidad seleccionada.

**Evidencia PPT:** captura del gizmo de ruta rodeando un obstáculo → slide técnico.

---

### HU-008 · Movimiento de unidad con interpolación
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-008-movimiento-interpolado`

**Como** jugador **quiero** que las unidades se desplacen con fluidez
**para** que el juego se sienta un RTS en tiempo real y no un táctico por turnos.

**Criterios de aceptación**
- [ ] La unidad recorre los waypoints de la ruta interpolando, sin saltar de celda en celda.
- [ ] La unidad se orienta según su dirección de avance.
- [ ] Al llegar al destino se detiene limpiamente, sin vibrar ni pasarse.
- [ ] La velocidad se lee del `ScriptableObject` de la unidad, no del código.

**Evidencia PPT:** GIF de una unidad recorriendo el mapa → slide de demo.

---

### HU-009 · Empuje blando entre unidades
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-009-empuje-blando`

**Como** jugador **quiero** que las unidades no se encimen
**para** poder distinguirlas y que el grupo se vea como un ejército.

**Criterios de aceptación**
- [ ] Dos unidades a menos de su radio de separación se empujan suavemente.
- [ ] El empuje **no** las saca del terreno transitable.
- [ ] 20 unidades enviadas al mismo punto se acomodan alrededor sin quedar apiladas.
- [ ] No hay oscilación: las unidades detenidas no vibran empujándose mutuamente.
- [ ] La detección de vecinos usa la grilla espacial, no un recorrido completo (ADR-04).

**Evidencia PPT:** captura de 20 unidades acomodadas alrededor de un punto → slide de demo.

---

### HU-010 · Selección de unidad con clic
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-010-seleccion-clic`

**Como** jugador **quiero** seleccionar una unidad con un clic
**para** poder darle órdenes.

**Criterios de aceptación**
- [ ] Clic izquierdo sobre una unidad propia la selecciona y deselecciona la anterior.
- [ ] Clic izquierdo en terreno vacío deselecciona todo.
- [ ] Solo se pueden seleccionar unidades de la facción propia.
- [ ] La unidad seleccionada muestra un indicador visible bajo sus pies.

**Evidencia PPT:** captura con el indicador de selección → slide de demo.

---

### HU-011 · Selección múltiple con caja de arrastre
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-011-seleccion-caja`

**Como** jugador **quiero** seleccionar varias unidades arrastrando un recuadro
**para** darles órdenes en grupo sin hacer clic una por una.

**Criterios de aceptación**
- [ ] Mantener el botón izquierdo y arrastrar dibuja un recuadro visible en pantalla.
- [ ] Al soltar quedan seleccionadas las unidades **propias** dentro del recuadro.
- [ ] Las unidades enemigas o neutrales dentro del recuadro **no** se seleccionan.
- [ ] Shift + arrastre **suma** a la selección actual en vez de reemplazarla.
- [ ] Con 50 unidades seleccionadas el framerate no baja de 60 fps.

**Evidencia PPT:** captura del recuadro activo sobre un grupo → slide de demo.

---

### HU-012 · Orden de movimiento contextual
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-012-orden-movimiento`

**Como** jugador **quiero** mover a las unidades seleccionadas con clic derecho
**para** dirigir el grupo con un solo botón.

**Criterios de aceptación**
- [ ] Clic derecho sobre terreno transitable emite una **`Orden` de movimiento** (ADR-01),
      no una llamada directa a la unidad.
- [ ] Todas las unidades seleccionadas reciben la orden.
- [ ] El grupo se reparte alrededor del destino en vez de apuntar todas a la misma celda.
- [ ] Una marca visual aparece brevemente en el punto ordenado.
- [ ] Una orden nueva **cancela** la anterior.

**Evidencia PPT:** GIF ordenando un grupo de 10 unidades → slide de demo. **Es la captura principal de la semana.**

---

# Semanas 04-17 — Títulos

> Se detallan al abrir cada semana.

### HU-017 · Interfaz de selección con los assets del pack
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-017-interfaz-seleccion`

**Como** jugador
**quiero** ver a quién tengo seleccionado y en qué estado está
**para** decidir sin adivinar.

**Criterios de aceptación**

- [ ] El puntero cambia solo: flecha por defecto, mano sobre una unidad propia y
      prohibido cuando hay selección y el punto de destino es intransitable.
- [ ] La unidad seleccionada se marca con los corchetes del pack, teñidos del color de
      su bando. El anillo generado por código queda retirado.
- [ ] Cada unidad lleva una barra de vida encima, con el marco y el relleno del pack.
      La barra no se voltea cuando la unidad camina hacia la izquierda.
- [ ] Con una unidad seleccionada, el panel inferior muestra retrato, nombre, vida
      numérica y estadísticas.
- [ ] Con varias, el panel muestra una rejilla de retratos con su barra de vida, y
      "+N" si no caben todas.
- [ ] Un clic sobre el panel no llega al terreno: ni selecciona ni da órdenes.
- [ ] La interfaz escala con la resolución de la ventana.

**Notas técnicas**

Los sprites de UI del pack vienen centrados en lienzos mucho mayores que su contenido
(una barra de 94 px dentro de una imagen de 192×64). Los márgenes transparentes se estiran
junto al resto y descolocan lo que sí se ve, así que se recortan una vez a su contenido útil
en `Assets/Datos/UI`. Los originales del pack no se modifican.

El panel se construye por código, sin prefab: la escena se regenera entera desde el editor
y un prefab sería una segunda fuente de verdad que mantener a mano.

---

### HU-018 · Relieve editable a mano
**Épica:** E02 · **Semana:** 03 · **Responsable:** Joaquín · **Rama:** `feat/HU-018-relieve-editable`

**Como** diseñador del nivel
**quiero** colocar yo las mesetas y las rampas
**para** que los acantilados canalicen el combate donde yo decida, y que no cambien cada vez
que se regenera la escena.

**Criterios de aceptación**

- [ ] Hay un pincel en la vista de escena para subir, bajar y colocar rampas.
- [ ] La rampa se coloca de una en una y se puede elegir hacia dónde cae, o dejarlo en
      automático.
- [ ] Mayúsculas + clic borra, y acepta el clic en cualquiera de las dos celdas de la cuesta.
- [ ] El pincel no deja elevar agua.
- [ ] Guardar escribe un asset y lo enlaza en la definición del mapa.
- [ ] Con ese asset enlazado, «Generar escena de juego» reproduce el relieve exactamente.
- [ ] El log dice si el relieve vino del asset o del ruido.

**Notas técnicas**

Razonada en el [ADR-10](ARQUITECTURA.md). Funciona con el editor parado: la ventana regenera
el mapa desde la definición, que al ser determinista da el mismo terreno que en Play.

---

### Semana 04 — Unidades y animación (E03)
Animador propio por sprite-swap · FSM de unidad (idle/mover/atacar/morir) · las 4 unidades militares ·
las 5 facciones por color · ScriptableObject de stats.

### Semana 05 — Economía · HITO 1 · PC1 (E04)
Nodos de recurso en el mapa · pawn recolecta oro · pawn recolecta madera · ciclo de retorno al castillo ·
almacén de recursos por facción · HUD con contadores.

### Semana 06 — Construcción y producción (E05)
Modo de colocación con silueta · validación de terreno · pawn construye · sistema de población ·
casa sube el límite · castillo entrena pawns · cola de producción.

### Semana 07 — Combate cuerpo a cuerpo (E06)
Componente de salud y muerte · barras de vida · targeting con grilla espacial · ataque del guerrero ·
ataque del lancero · orden de atacar · respuesta automática al ser atacado.

### Semana 08 — Combate a distancia (E06)
Proyectiles del arquero con trayectoria · curación del monje · triángulo de contadores ·
cuartel, campo de tiro e monasterio entrenan su unidad · torres defensivas.

### Semana 09 — Niebla de guerra y minimapa (E07)
Grilla de visibilidad por facción · radios de visión · tres estados de niebla · render de la niebla ·
minimapa con terreno y unidades · clic en el minimapa mueve la cámara.

### Semana 10 — IA rival v1 · HITO 2 · PC2 (E08)
Capa estratégica con build order · gestor de economía de la IA · gestor militar y oleadas ·
la IA emite las mismas órdenes que el jugador · primera partida completa vs bot.

### Semana 11 — IA v2 y compuerta PvP (E08)
Tres dificultades por multiplicador · IA que defiende su base · IA que decide si la niebla la afecta ·
**decisión go/no-go de PvP documentada**.

### Semana 12 — Flujo de partida (E09)
Condición de victoria y derrota · pantalla de resultado · menú principal · selección de mapa ·
selección de número de bandos y dificultad.

### Semana 13 — FFA y rendimiento (E09)
Spawns múltiples · IA multi-bando · perfilado con 250 unidades · optimización de los cuellos detectados ·
segundo mapa.

### Semana 14 — Audio y feedback (E10)
AudioMixer con buses · música de fondo · SFX de combate, construcción y recolección ·
partículas de impacto · feedback visual de daño.

### Semana 15 — UI/UX · HITO 3 · PC3 (E10)
HUD final con los assets del pack · panel de unidad seleccionada · tooltips · menú de pausa ·
opciones · tercer mapa · FFA de 5 bandos corriendo.

### Semana 16 — Balance y QA (E11)
Plan de pruebas · sesiones de juego de los 3 integrantes · ajuste de stats y de la tasa de carne ·
calibración de dificultades · corrección de bugs · VFX del pack.

### E13 — Editor de mapas *(semanas 12-13)*

Ventana de editor para **pintar mapas a mano**: terreno, desniveles, recursos y posiciones
iniciales, con guardado y carga.

**Por qué merece la pena.** El generador produce mapas *plausibles*; un editor produce mapas
*diseñados*. Un cuello de botella colocado con intención vale más que uno que salió del ruido.
Además abre trabajo técnico para Kiara, que hoy solo aporta documentación — y esa es la
debilidad real del equipo frente al criterio de dominio técnico de la rúbrica.

**Por qué en la semana 12 y no antes.** Un editor edita un *formato de datos*, y ese formato
todavía se está moviendo: en la semana 3 la celda pasa de un simple booleano a tener nivel,
escalera y ocupación. Construir el editor sobre un formato inestable obliga a rehacerlo cada
vez que cambia. En la semana 12 llevará semanas asentado.

**No compite con el generador, lo complementa.** Ambos producen la misma `DefinicionMapa`:

```
Generador ─┐
           ├──▶  DefinicionMapa  ──▶  Escena
Editor  ───┘
```

El flujo natural será **generar un mapa y retocarlo a mano**, que es como se hacen los mapas
de verdad.

**HUs previstas:** ventana de editor con pinceles · pintar terreno y niveles · colocar recursos
y spawns · guardar y cargar · validación (¿todas las bases conectadas? ¿simetría razonable?).

---

### Semana 17 — Entrega final (E11)
Build jugable · documentación técnica final consolidada desde la bitácora · manual breve ·
vídeo de demostración.
