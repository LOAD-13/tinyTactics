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
| `E03` | Unidades y animación | 04 | 🟢 Cerrada |
| `E04` | Economía | 05 | 🟢 Cerrada |
| `E05` | Construcción y producción | 06 | 🟢 Cerrada |
| `E06` | Combate | 07-08 | 🟢 Cerrada |
| `E07` | Percepción — niebla, minimapa y hora | 09 | 🟢 Cerrada |
| `E08` | IA rival | 10-11 | ⚪ Pendiente |
| `E09` | Flujo de partida y FFA | 12-13 | ⚪ Pendiente |
| `E10` | Presentación — audio, VFX, UI | 14-15 | ⚪ Pendiente |
| `E11` | Calidad — balance, QA, build | 16-17 | ⚪ Pendiente |
| `E12` | PvP LAN *(condicional — compuerta S11)* | 12-14 | ⚫ Condicional |
| `E13` | Editor de mapas | 12-13 | 🔵 Empezada *(primer trozo en la 08; requisitos en [SANDBOX.md](SANDBOX.md))* |

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

# Semana 04 — Unidades y animación (E03)

> ✅ **Épica cerrada.** Las siete HUs entregadas en el tag `v0.4.0-s04`, incluidas HU-024 y
> HU-025 que estaban marcadas como bloque B. El detalle honesto de lo que costó está en
> [`BITACORA.md`](BITACORA.md), y la decisión de fondo en el [ADR-11](ARQUITECTURA.md).

**Meta de la semana:** que las cinco unidades existan de verdad — con sus estadísticas, sus
animaciones y sus cuatro estados — y que se pueda ver a una morir.

> **Una sola rama para la épica:** `feat/E03-unidades-y-animacion`. La política cambió en la
> semana 04 y está razonada en [`GITFLOW.md`](GITFLOW.md) §5.4.

| HU | Título | Bloque |
|---|---|---|
| HU-019 | Animador de varios estados con tiras de una sola pasada | A |
| HU-020 | Máquina de estados de unidad | A |
| HU-021 | Las cinco unidades en los cinco colores | A |
| HU-022 | Muerte y desaparición | A |
| HU-023 | Ataque visible con daño de prueba | A |
| HU-024 | Lancero direccional | B |
| HU-025 | Panel de acciones | B |

### HU-019 · Animador de varios estados
**Épica:** E03 · **Semana:** 04

- [ ] La tabla de animaciones vive en el `ScriptableObject`, una entrada por estado.
- [ ] Una tira puede declararse sin bucle: se reproduce una vez y se queda en el último frame.
- [ ] El animador avisa cuando una tira sin bucle termina.

**Nota técnica.** El aviso de fin es lo que permite que un golpe dure lo que dura su dibujo.
Un temporizador con un número fijo habría que mantenerlo a mano cada vez que se cambie el fps.

---

### HU-020 · Máquina de estados de unidad
**Épica:** E03 · **Semana:** 04

- [ ] Cuatro estados: reposo, moviendo, atacando, muriendo.
- [ ] Un solo componente decide el estado; `MovimientoUnidad` deja de tocar la animación.
- [ ] Un golpe no se interrumpe a mitad aunque la unidad reciba otra orden.
- [ ] Morir gana a todo y es terminal.

---

### HU-021 · Las cinco unidades en los cinco colores
**Épica:** E03 · **Semana:** 04

- [ ] Un asset de datos por tipo, con las estadísticas de la tabla §4 del GDD.
- [ ] Las rutas de animación llevan `{color}`: una entrada sirve para las cinco facciones.
- [ ] El panel de unidad muestra el retrato correcto de cada tipo.
- [ ] Si falta una tira, se avisa por consola y no se crea la unidad a medias.

---

### HU-022 · Muerte y desaparición
**Épica:** E03 · **Semana:** 04

- [ ] Al llegar a cero de vida la unidad se apaga a gris, se desvanece y se retira.
- [ ] Suelta una nube de polvo al caer.
- [ ] El marcador de selección y la barra de vida se apagan al morir.
- [ ] Sale de la selección y del índice espacial sin dejar referencias colgando.

**Nota técnica.** El pack **no trae animación de muerte** para ninguna unidad; se comprobó
buscando «death», «die» y «dead» en todo el paquete. Se resuelve sin arte nuevo.

---

### HU-023 · Ataque visible con daño de prueba
**Épica:** E03 · **Semana:** 04

- [ ] Clic derecho sobre un objetivo enemigo emite una orden de atacar.
- [ ] La unidad se gira hacia la víctima antes de golpear.
- [ ] El golpe resta vida y se ve bajar la barra y el panel.
- [ ] Junto a cada base hay un muñeco de pruebas contra el que practicar.

> ⚠️ **Esto no es combate.** No hay búsqueda automática de objetivo, ni persecución, ni
> cadencia, ni comprobación de alcance, ni respuesta del atacado. Todo eso es la épica E06,
> semanas 07 y 08. Lo de esta semana existe para que la animación y la muerte se puedan ver.
> El muñeco de pruebas es andamio y se retira cuando haya enemigos de verdad.

---

### HU-024 · Lancero direccional *(bloque B)*
**Épica:** E03 · **Semana:** 04

- [ ] El lancero ataca en ocho orientaciones, a partir de las cinco tiras del pack más espejo.

---

### HU-025 · Panel de acciones *(bloque B)*
**Épica:** E03 · **Semana:** 04

- [ ] Rejilla de botones a la derecha del panel con las acciones de lo seleccionado.
- [ ] Esta semana: Atacar (A), Mover (M), Detener (S).
- [ ] Los atajos de teclado hacen lo mismo que el botón.
- [ ] Pulsar Atacar deja el puntero en modo objetivo hasta el siguiente clic.

**De dónde sale.** Del menú de comandos de Warcraft III: panel inferior derecho con los
comandos de lo que esté seleccionado. La rejilla se diseña ya con hueco para las acciones
que llegan después — Construir en la semana 06 y las de producción de edificios.

---

# Semana 05 — Economía (E04) · 🏁 HITO 1 · PC1

> ✅ **Épica cerrada.** Las nueve HUs del bloque A entregadas en el tag `v0.5.0-s05`, más
> HU-035 que era bloque B. Queda fuera HU-036. El detalle honesto de lo que costó está en
> [`BITACORA.md`](BITACORA.md), y las dos decisiones de fondo en el ADR-12 y el ADR-13.

**Meta de la semana:** cerrar el bucle económico. No basta con que un pawn pique piedra: hay
que poder **recolectar oro, gastarlo en más pawns, y que esos pawns recolecten más**. Ese
bucle es lo que separa un RTS de una maqueta con unidades que caminan.

> **Una sola rama para la épica:** `feat/E04-economia`, según la política de `GITFLOW.md` §5.4.

| HU | Título | Bloque |
|---|---|---|
| HU-026 | Nodo de recurso con agotamiento | A |
| HU-027 | Almacén de recursos por facción | A |
| HU-028 | El castillo como entidad seleccionable | A |
| HU-029 | Ciclo de recolección del pawn | A |
| HU-030 | Animaciones de trabajo y de carga | A |
| HU-031 | Orden contextual de recolectar | A |
| HU-032 | HUD de recursos | A |
| HU-033 | Entrenar pawns en el castillo | A |
| HU-034 | Sustento por carne | A |
| HU-035 | Punto de reunión del castillo | B |
| HU-036 | Resaltado del nodo bajo el cursor | B |

> ⚠️ **La población queda fuera a propósito.** El GDD la fija en 5 iniciales y +5 por casa.
> Meterla esta semana bloquearía la producción antes del primer pawn, porque la escuadra
> inicial de pruebas ya son 10 unidades. Entra en la semana 06 junto con las casas, que es
> cuando existe algo con lo que subir el tope. Esta semana el pawn solo cuesta oro.

> **Orden de recorte si el domingo aprieta.** Primero HU-034 pierde la penalización y se queda
> en el contador bajando; después HU-033 pierde la cola y entrena de uno en uno. HU-029 no se
> recorta nunca: es el hito.

---

### HU-026 · Nodo de recurso con agotamiento
**Épica:** E04 · **Semana:** 05

**Como** jugador **quiero** que los árboles, las vetas y las ovejas se agoten **para** que la
partida me obligue a salir de mi esquina a buscar más.

**Criterios de aceptación**
- [ ] Cada nodo declara su tipo, sus extracciones restantes y cuánto entrega por viaje.
- [ ] Al llegar a cero: la veta desaparece, el árbol se convierte en tocón, la oveja desaparece.
- [ ] El árbol agotado **deja de bloquear la grilla**: por el tocón se puede pasar.
- [ ] Un nodo agotado deja de aceptar recolectores y los que lo trabajaban buscan otro cercano.
- [ ] Los tres tipos se siembran desde el generador de escena, sin colocar nada a mano.

**Nota técnica.** La liberación de celda en caliente no existe todavía: `GrillaMapa` solo sabe
`MarcarObstaculo`. Sin lo contrario, un bosque talado se queda como muro invisible — el tipo de
fallo que no da error por consola y solo se ve jugando.

---

### HU-027 · Almacén de recursos por facción
**Épica:** E04 · **Semana:** 05

**Como** sistema **quiero** una única fuente de verdad de lo que tiene cada bando **para** que
HUD, producción y IA lean todos del mismo sitio.

**Criterios de aceptación**
- [ ] Oro, madera y carne por facción, con cantidades iniciales en un `ScriptableObject` (ADR-07).
- [ ] Avisa por evento al cambiar: nadie consulta el almacén cada frame.
- [ ] `PuedePagar` y `Cobrar` son la única vía de gasto; cobrar de más es imposible.
- [ ] Ninguna cantidad puede quedar negativa.

---

### HU-028 · El castillo como entidad seleccionable
**Épica:** E04 · **Semana:** 05

**Como** jugador **quiero** poder clicar mi castillo **para** ver qué es y qué puede hacer.

**Criterios de aceptación**
- [ ] El castillo deja de ser un sprite suelto: conoce su facción y es un punto de entrega.
- [ ] Clic izquierdo lo selecciona y muestra su ficha en el panel, con el castillo de retrato.
- [ ] La caja de arrastre selecciona unidades, **nunca** edificios — como en cualquier RTS.
- [ ] Un edificio seleccionado no obedece órdenes de mover ni de atacar.
- [ ] Existe un registro por el que un pawn cargado encuentra el centro de entrega más cercano.

**Fuera de alcance.** Vida, destrucción y el resto de edificios son E05 y E06.

---

### HU-029 · Ciclo de recolección del pawn
**Épica:** E04 · **Semana:** 05

**Como** jugador **quiero** mandar un pawn a un recurso y olvidarme **para** dedicar mi atención
a otra cosa, que es de lo que va el género.

**Criterios de aceptación**
- [ ] El ciclo completo se repite solo: ir → trabajar → cargar el tope → volver → depositar → ir.
- [ ] Al depositar, el almacén de su facción sube y el HUD lo refleja.
- [ ] Si el nodo se agota con el pawn cargado, entrega primero y luego busca otro nodo del mismo tipo.
- [ ] Una orden de mover o de atacar cancela la recolección limpiamente, sin dejarlo cargado a medias.
- [ ] Varios pawns pueden trabajar el mismo nodo sin amontonarse ni bloquearse.

**Nota técnica.** La decisión —qué nodo, cuándo está lleno, a qué castillo volver— vive en un
componente propio, no en `MaquinaDeEstados`. La máquina sigue siendo la **única** que toca al
animador (ADR-11); el recolector le pide estados, no dibuja.

---

### HU-030 · Animaciones de trabajo y de carga
**Épica:** E04 · **Semana:** 05

**Como** jugador **quiero** ver qué está haciendo cada pawn de un vistazo **para** leer mi
economía sin abrir ningún panel.

**Criterios de aceptación**
- [ ] Pica con el pico en el oro, con el hacha en el árbol y con el cuchillo en la oveja.
- [ ] Al volver cargado se le ve el saco: oro, madera o carne, según lo que lleve.
- [ ] La carga cambia el dibujo **en reposo y andando**, no solo en uno de los dos.
- [ ] Al depositar, el saco desaparece y vuelve al aspecto normal.

**Nota técnica.** La tabla de clips pasa a indexarse por **estado × carga**. Es una extensión de
`ClipDe(estado)`, no un rediseño: el pack ya trae las doce combinaciones por color.

---

### HU-031 · Orden contextual de recolectar
**Épica:** E04 · **Semana:** 05

**Criterios de aceptación**
- [ ] Clic derecho sobre árbol, veta u oveja con un pawn seleccionado emite orden de recolectar.
- [ ] Sobre el suelo sigue siendo mover y sobre un enemigo, atacar: un botón, tres órdenes.
- [ ] Las unidades militares del grupo ignoran la orden en vez de irse encima del árbol.
- [ ] Es una `Orden` serializable como todas las demás (ADR-01).

---

### HU-032 · HUD de recursos
**Épica:** E04 · **Semana:** 05

**Criterios de aceptación**
- [ ] Tres contadores en pantalla —oro, madera y carne— con los iconos reales del pack.
- [ ] Se actualizan por evento al depositar o al gastar, no cada frame.
- [ ] Mantienen la estética de madera del panel de unidad; no es UI por defecto de Unity.
- [ ] Se ven a cualquier resolución sin taparse con el panel de unidad.

---

### HU-033 · Entrenar pawns en el castillo
**Épica:** E04 · **Semana:** 05

**Como** jugador **quiero** gastar oro en más pawns **para** que mi economía crezca sola.

**Criterios de aceptación**
- [ ] Con el castillo seleccionado, la rejilla de comandos muestra «Entrenar pawn» con su coste.
- [ ] Sin oro suficiente el botón no obedece y lo dice; con oro, cobra al encolar.
- [ ] Una barra de progreso muestra cuánto falta.
- [ ] El pawn aparece junto al castillo, vivo, seleccionable y listo para recibir órdenes.
- [ ] Se pueden encolar varios y salen de uno en uno, en orden.

**De dónde sale.** La rejilla de HU-025 se diseñó con hueco para esto. Adelanta trabajo de E05
a propósito: sin producción, la economía de esta semana no cierra el bucle y el Hito 1 se queda
en «un pawn pica piedra».

---

### HU-034 · Sustento por carne
**Épica:** E04 · **Semana:** 05

**Como** diseñador **quiero** que el ejército coma **para** que nadie pueda gastar todo en un
único ataque y desentenderse de la economía.

**Criterios de aceptación**
- [ ] Cada unidad viva consume carne de forma continua, según su tipo.
- [ ] A cero, las tropas pierden daño y velocidad hasta que se reponga la reserva.
- [ ] El jugador se entera: el contador avisa antes de que llegue a cero.
- [ ] El ritmo es un parámetro editable, no un número dentro del código.

**Nota de balance.** Los valores del GDD (0,10–0,20 por segundo y unidad) vacían una reserva de
100 en **un minuto** con la escuadra inicial. Van con un multiplicador global de sustento para
poder calibrarlos en la semana 16 sin recompilar, tal como el propio GDD anticipa.

---

### HU-035 · Punto de reunión del castillo *(bloque B)*
**Épica:** E04 · **Semana:** 05

- [ ] Con el castillo seleccionado, clic derecho fija dónde salen las unidades nuevas.
- [ ] Si el punto es un nodo de recurso, el pawn nuevo empieza a recolectarlo directamente.

---

### HU-036 · Resaltado del nodo bajo el cursor *(bloque B)*
**Épica:** E04 · **Semana:** 05

> ⛔ **No entregada. Se arrastra a la semana 06** con los criterios corregidos, más abajo.
>
> El criterio original decía «usando los sprites `_Highlight` del pack», y al mirar el
> paquete resulta que **solo el oro los trae**: las seis vetas sí, los árboles y las ovejas
> no. Escrito así era imposible de cumplir de forma uniforme, y un juego donde el oro se
> ilumina y los árboles no se lee como un fallo, no como una función. Es el mismo error de
> siempre —dar por hecho lo que hay en el pack en vez de mirarlo— solo que esta vez salió
> barato porque la HU era opcional.

---

### Semana 06 — Construcción y producción (E05)

> 📌 **Viene arrastrada de la semana 05:** HU-036, con los criterios rehechos.

### HU-036 · Resaltado del nodo bajo el cursor *(arrastrada de la semana 05)*
**Épica:** E05 · **Semana:** 06

**Como** jugador **quiero** saber de un vistazo qué recurso estoy señalando y cuánto le queda
**para** no tener que contar viajes para saber si una veta está a punto de secarse.

**Criterios de aceptación**
- [ ] El resaltado se hace **por código**, aclarando el color del sprite, y no con arte del
      pack: solo el oro trae variante `_Highlight`.
- [ ] Funciona igual en árbol, veta y oveja. Ninguno se queda sin él.
- [ ] Al señalar un nodo se ve cuántas cargas le quedan.
- [ ] El resaltado se apaga al salir el cursor, aunque el nodo se agote mientras tanto.

**Meta de la semana:** que el jugador construya. Al retirar la escuadra regalada, la partida
pasa a ser una de verdad: dos pawns, recolectar, levantar una casa, levantar un cuartel y sacar
el primer guerrero. Sin construcción no hay ejército.

> **Una sola rama para la épica:** `feat/E05-construccion`.
>
> **Sin bloque B esta semana.** Las diez HUs son obligatorias.

| HU | Título | Riesgo |
|---|---|---|
| HU-036 | Resaltado del nodo bajo el cursor *(arrastrada de la semana 05)* | bajo |
| HU-037 | Catálogo de edificios | bajo |
| HU-038 | Modo de colocación con silueta y validación de terreno | alto |
| HU-039 | El pawn construye | alto |
| HU-040 | Sistema de población y contador en el HUD | medio |
| HU-041 | La casa sube el límite | bajo |
| HU-042 | Cuartel, campo de tiro y monasterio entrenan su unidad | bajo |
| HU-043 | El panel muestra qué fabrica cada edificio y qué construye el pawn | medio |
| HU-044 | Tocón acorde al tipo de árbol | bajo |
| HU-045 | Retirar el andamio de la escuadra inicial | medio |

> ⛔ **Fuera de alcance, declarado antes de empezar.** La **torre** es defensa estática: necesita
> combate y va a E06. La **vida y destrucción de edificios**, lo mismo. Declararlo ahora evita
> que acaben siendo dos HUs a medias.

---

### HU-037 · Catálogo de edificios
**Épica:** E05 · **Semana:** 06

**Como** diseñador **quiero** que cada edificio declare su coste, su huella y lo que produce
**para** poder añadir uno nuevo sin tocar código.

**Criterios de aceptación**
- [ ] Un asset por edificio con coste en oro y madera, huella medida sobre el PNG, población
      que aporta, coste de obra y qué unidades fabrica.
- [ ] Las huellas se **miden** sobre el archivo de imagen, no se estiman.
- [ ] El catálogo se reconstruye desde el menú del editor, como el de unidades.
- [ ] Añadir un edificio al catálogo lo hace aparecer en el panel sin más cambios.

**Huella y planta son dos medidas distintas.** La **huella** es el recuadro del dibujo, en
decimales, y sirve para saber a qué distancia está un pawn del borde y para estirar el corchete
de selección. La **planta** son las celdas que el edificio ocupa en el suelo, en tiles enteros,
y es lo que bloquea la grilla. No coinciden y no deben: el monasterio dibuja 4,14 tiles de alto
pero su planta son 3, porque lo de arriba es la aguja. Usar la huella para bloquear habría hecho
imposible construirlo en sitios donde cabe de sobra.

**La obra se mide en martillazos, no en segundos** (ADR-13). Así dos pawns tardan la mitad sin
ninguna regla especial, y parar la obra a medias no regala progreso. Es el mismo razonamiento
que cerró el exploit de la tala en la semana 05.

---

### HU-038 · Modo de colocación con silueta y validación de terreno
**Épica:** E05 · **Semana:** 06

**Como** jugador **quiero** ver dónde va a quedar el edificio antes de confirmar **para** no
gastar recursos en una colocación que no quería.

**Criterios de aceptación**
- [ ] Al elegir un edificio, una silueta sigue al cursor con la huella real del edificio.
- [ ] La silueta se tiñe de verde si el sitio vale y de rojo si no.
- [ ] No se puede colocar sobre agua, sobre otro edificio, sobre un recurso ni a distinto nivel.
- [ ] Clic derecho o Escape cancelan sin gastar nada.
- [ ] Sin recursos suficientes, el comando avisa y no entra en modo de colocación.

---

### HU-039 · El pawn construye
**Épica:** E05 · **Semana:** 06

**Como** jugador **quiero** que un pawn levante lo que he colocado **para** que construir cueste
tiempo y ocupe a un trabajador, como en cualquier RTS.

**Criterios de aceptación**
- [ ] Al confirmar se cobra el coste y aparece una **obra** en el sitio.
- [ ] El pawn camina hasta la obra y la martillea con la animación del pack.
- [ ] La obra se va **opacando** conforme avanza y muestra una barra de progreso.
- [ ] Al terminar se convierte en edificio funcional y el pawn queda libre.
- [ ] Una orden de mover o recolectar abandona la obra, que se queda a medias esperando.
- [ ] Varios pawns sobre la misma obra la levantan más rápido.

**Nota técnica.** El pack no trae andamios, así que la obra es el sprite del propio edificio con
transparencia creciente. Cero arte nuevo, y se lee sin explicación.

---

### HU-040 · Sistema de población y contador en el HUD
**Épica:** E05 · **Semana:** 06

**Como** jugador **quiero** ver cuánta población tengo y cuánta me queda **para** saber cuándo
necesito otra casa.

**Criterios de aceptación**
- [ ] Cada unidad cuesta población: pawn 1, guerrero, lancero y arquero 2, monje 3.
- [ ] El castillo aporta 10 y el tope duro es 50.
- [ ] Contador con el icono del pawn, en formato «usada / tope».
- [ ] Sin población libre, entrenar avisa y no encola.
- [ ] El contador avisa visualmente cuando el tope está lleno.

**Dónde va el contador.** Arriba a la izquierda, en su propia caja, separado de los tres
recursos. Se probó como cuarta caja de la fila y se descartó: la población no es un recurso
—no se recolecta, no se gasta en construir y no sube al depositar—, es un límite. Junto al oro
y la madera se lee como «cuánto tengo» cuando lo que dice es «cuánto me cabe».

**Nota de diseño.** El tope de 50 **puntos** garantiza por sí solo el presupuesto de rendimiento:
como ninguna unidad cuesta menos de 1 punto, un bando nunca puede pasar de 50 unidades.

---

### HU-041 · La casa sube el límite
**Épica:** E05 · **Semana:** 06

**Criterios de aceptación**
- [ ] Solo las casas aportan población. Cuartel, campo de tiro y monasterio no aportan nada.
- [ ] Cada casa terminada suma 5, y solo al **terminar** la obra, no al colocarla.
- [ ] El tope nunca pasa de 50 por muchas casas que se construyan.

**Una ficha, tres fachadas.** El pack trae `House1`, `House2` y `House3`. No son tres edificios
—cuestan lo mismo, ocupan lo mismo y dan los mismos cinco de población— así que son **una sola
ficha con tres dibujos**, y la rueda del ratón pasa de uno a otro mientras la silueta está en la
mano. Tres fichas se habrían comido tres de las cuatro ranuras de la rejilla y habrían obligado
al jugador a elegir entre casas idénticas creyendo que se diferencian en algo.

Cada fachada lleva su propia huella medida: la casa más alta y la más baja se llevan un tercio
de tile, y con una medida compartida una de las tres quedaría flotando sobre su sombra.

---

### HU-042 · Cuartel, campo de tiro y monasterio entrenan su unidad
**Épica:** E05 · **Semana:** 06

**Criterios de aceptación**
- [ ] El cuartel entrena guerreros y lanceros; el campo de tiro, arqueros; el monasterio, monjes.
- [ ] Cada uno cobra el coste de la unidad y respeta la población libre.
- [ ] Las unidades salen del edificio que las fabricó, no del castillo.
- [ ] Cada edificio tiene su propia cola y su propia barra.

---

### HU-043 · El panel muestra qué fabrica cada edificio y qué construye el pawn
**Épica:** E05 · **Semana:** 06

**Criterios de aceptación**
- [ ] Con un pawn seleccionado, el botón Construir abre la rejilla de edificios disponibles.
- [ ] Con un edificio seleccionado, la rejilla muestra lo que sabe fabricar con su coste.
- [ ] Al pasar el ratón por un botón se lee el nombre y el coste.
- [ ] Lo que no se puede pagar se ve apagado, no oculto: el jugador tiene que saber que existe.

---

### HU-044 · Tocón acorde al tipo de árbol
**Épica:** E05 · **Semana:** 06

**Criterios de aceptación**
- [ ] Al talar un `TreeN` queda el `Stump N` que le corresponde, nunca otro.
- [ ] El tocón queda alineado con el suelo, sin flotar ni hundirse.

**Nota técnica.** `Tree1` y `Tree2` tienen fotogramas de 192×256 y `Tree3` y `Tree4` de 192×192,
y los tocones siguen el mismo reparto. Elegir el tocón al azar desalineaba media casilla los
árboles bajos. El generador tiene que **recordar qué variante sembró** en cada sitio.

---

### HU-045 · Retirar el andamio de la escuadra inicial
**Épica:** E05 · **Semana:** 06

**Criterios de aceptación**
- [ ] Cada bando empieza con **dos pawns** y su castillo, nada más.
- [ ] Con los recursos iniciales se puede levantar la primera casa sin quedarse bloqueado.
- [ ] El poste de entrenamiento **se mantiene** hasta la épica E06: sigue siendo la única forma
      de probar daño y muerte hasta que haya enemigos de verdad.

**Nota de alcance.** Es lo que convierte el avance en una partida: sin ejército regalado, el
jugador tiene que construir para tener tropas.

> **Interfaz de producción.** Al seleccionar un edificio, el panel de acciones (HU-025) muestra
> qué puede fabricar con su coste, y al pulsar encola la unidad con su tiempo de espera y una
> barra de progreso. Al seleccionar un pawn, muestra qué puede construir. Es el mismo panel
> que se estrena en la semana 04, y el castillo ya lo estrena en la 05 con HU-033: aquí solo se
> extiende al resto de edificios.

> **La escuadra inicial de 10 unidades por base es andamio de pruebas.** En el juego real se
> empieza con **dos pawns**, como en Warcraft, y todo lo demás se entrena. Se mantiene durante
> la semana 05 a propósito —le da al consumo de carne algo con qué morder y a la muerte algo
> que enseñar— y se retira aquí, cuando el cuartel y el campo de tiro puedan producir lo que
> hoy viene regalado.

### Semana 07 — El conflicto (E06, parte A)

> 📌 **Lo que ya estaba hecho sin anunciarse.** Buena parte del combate se construyó de
> refilón en épicas anteriores y conviene tenerlo presente para no rehacerlo: `Unidad` ya
> tiene vida, `RecibirDano`, `Curar` y muerte como estado (E03); `MaquinaDeEstados` ya sabe
> acercarse y golpear, y ya busca objetivo sola con `Buscar`; `RegistroDeUnidades` ya
> resuelve el vecindario con una **grilla espacial por cubos**, no con un bucle sobre todas
> las unidades. `OrdenAtacar` y `OrdenCurar` existen desde la semana 04.
>
> Lo que falta no es el combate: es que el golpeado **responda**, que la persecución
> **termine**, que los edificios **caigan** y que la partida **se pueda perder**.

### HU-046 · Responder al ser atacado
**Épica:** E06 · **Semana:** 07

**Como** jugador **quiero** que mis unidades devuelvan el golpe **para** no perderlas por
haber estado mirando otra esquina del mapa.

**Criterios de aceptación**
- [ ] Una unidad ociosa a la que hieren pasa a atacar a quien la hirió.
- [ ] Una unidad que ya tiene orden del jugador **no la abandona** por recibir un golpe.
- [ ] Un pawn recolectando responde huyendo, no peleando: vuelve al centro de entrega.
- [ ] Si el agresor está fuera de alcance y la postura lo permite, se le persigue.

**Nota técnica.** El disparador va en `Unidad.RecibirDano`, que hoy solo resta vida, y tiene
que avisar a la máquina de estados **sin pisar una orden del jugador**. Esa es la única regla
delicada: un RTS donde el clic derecho se cancela solo porque pasó una flecha es injugable.

---

### HU-047 · Correa y posturas
**Épica:** E06 · **Semana:** 07

**Como** jugador **quiero** decidir si una unidad persigue, aguanta o ignora **para** que mis
recolectores no se vayan solos a morir a la base enemiga.

**Criterios de aceptación**
- [ ] Tres posturas: **agresiva** (persigue), **defensiva** (responde sin moverse del sitio),
      **quieta** (aguanta sin responder).
- [ ] La postura se cambia desde el panel de la unidad seleccionada.
- [ ] Los pawns nacen en **quieta**; las unidades militares, en **agresiva**.
- [ ] Una unidad agresiva que persigue más de N casillas desde donde empezó **abandona y
      vuelve** a su posición.

**Nota técnica.** La correa no es un lujo: sin ella, un solo arquero enemigo arrastra media
base al otro lado del mapa porque cada unidad que entra en su radio encadena la siguiente. Es
el fallo clásico del combate sin límite de persecución, y se ve en cuanto hay dos bandos.

---

### HU-048 · Vida, barra y destrucción de edificios
**Épica:** E06 · **Semana:** 07

**Como** jugador **quiero** poder derribar los edificios enemigos **para** que atacar una base
sirva de algo.

**Criterios de aceptación**
- [ ] Cada edificio tiene vida propia en su ficha, proporcional a lo que cuesta.
- [ ] La barra de vida aparece al pasar el cursor o al estar dañado, no siempre.
- [ ] Al caer: deja escombros un instante, **libera sus celdas** en la grilla y desaparece.
- [ ] Una obra en construcción también se puede derribar, y no devuelve recursos.
- [ ] Al caer una casa, el límite de población **baja solo**, sin código nuevo.
- [ ] Un edificio destruido deja de ser centro de entrega y de fabricar.

**Nota técnica.** El último criterio es gratis por una decisión de la semana 06: la población
**se recuenta, no se guarda** (HU-040). Una casa derribada baja el tope sin que nadie la
descuente. Es la primera vez que esa decisión cobra, y conviene enseñarlo en la expo.

Liberar las celdas reutiliza `Ocupar(false)`, que ya existe de `ReclamarTerreno`.

---

### HU-049 · La torre con arquero guarnecido
**Épica:** E06 · **Semana:** 07

**Como** jugador **quiero** levantar torres **para** defender la base sin tener tropas paradas.

**Criterios de aceptación**
- [ ] La torre se construye como cualquier otro edificio, con su coste y su planta.
- [ ] Al inaugurarse aparece un arquero en la almena que **dispara solo** a lo que entre en
      alcance.
- [ ] El arquero **se orienta** hacia su objetivo usando las cinco direcciones de ataque que
      ya existen.
- [ ] La flecha sale de la almena, no de la base del edificio.
- [ ] Si la torre cae, el arquero cae con ella.
- [ ] La torre no se puede mover ni consume carne.

**Balance.** Más alcance (×1,3) y más daño por flecha (×1,4) que un arquero, pero **cadencia
más lenta**. La torre es un disuasorio, no una prohibición: una torre que gana el intercambio
contra unidades hace imposible atacar una base, y en la semana 10 la IA tiene que poder
atacarnos o la partida contra el bot no existe.

**Nota técnica.** Es un edificio **con guarnición**, no una unidad rara. Reutiliza el arte, el
animador y la máquina de estados del arquero, que ya están hechos; lo único nuevo es el punto
de disparo en la ficha, igual que el `puntoSalida` que ya usan castillo y cuartel.

---

### HU-050 · Torres rivales en el mapa generado
**Épica:** E06 · **Semana:** 07

**Criterios de aceptación**
- [ ] El generador puede sembrar torres en las bases que no son la del jugador.
- [ ] Es una **opción del generador**, no una constante escrita en el código.
- [ ] Se puede apagar desde el panel de pruebas.

**Nota de alcance.** Sirve para tener contra qué pelear antes de que exista la IA (E08). Que
sea opción y no constante importa: en la semana 10 la IA decidirá ella misma si construye
torres, y entonces esto se apaga sin tocar código.

---

### HU-051 · Panel de pruebas — versión 1
**Épica:** E06 · **Semana:** 07

**Como** equipo **queremos** un panel de trampas **para** poder demostrar en clase situaciones
que de otro modo tardarían diez minutos en darse.

**Criterios de aceptación**
- [ ] Panel vertical plegable a la izquierda, con riel de iconos y secciones desplegables.
- [ ] **Bando:** cambiar de bando en caliente. La cámara salta a su castillo y la selección y
      el HUD se reinician.
- [ ] **Recursos:** +100 oro · +100 madera · +50 carne · vaciar.
- [ ] **Unidades:** bando inmortal · matar seleccionadas · curar seleccionadas.
- [ ] **Tiempo:** x1 · x2 · x4 · pausa.
- [ ] **Aparecer:** soltar una unidad del tipo elegido en el cursor, para el bando actual.
- [ ] Solo existe en el editor y en builds de desarrollo
      (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
- [ ] Mientras está abierto se ve un sello **MODO PRUEBAS** en pantalla.

**Por qué ahora y no más adelante.** No hay IA hasta la semana 10. **Sin cambiar de bando no
hay forma de enseñar un combate de dos lados**, así que el panel no es un extra de esta
semana: es lo que hace demostrable el resto de la semana. Y es lo que permite retirar el poste
de entrenamiento (HU-053) sin quedarnos sin banco de pruebas.

**Por qué no es una épica.** Una épica entrega juego; esto entrega herramienta, y cada épica
necesita interruptores distintos que hoy no se pueden adivinar. Crece pegado a las épicas: la
E07 le añadirá quitar la niebla y la E08 pausar la IA y ver su plan. Ver el sello en pantalla
es obligatorio para que ninguna captura de una entrega parezca hecha con trampas.

---

### HU-052 · Iconos del panel con la paleta del pack
**Épica:** E06 · **Semana:** 07

**Criterios de aceptación**
- [ ] Los iconos que el pack no cubre se **generan**, con la paleta extraída de los propios
      PNG de Tiny Swords.
- [ ] Mismo tamaño y mismo grosor de contorno que los iconos del pack.
- [ ] El oro y la madera reutilizan el sprite que ya existe en `Pawn and Resources`.

**Nota técnica.** Misma disciplina que las huellas de la semana 06: **medido, no adivinado**.
La paleta sale de contar los colores reales de los archivos del pack, así que los iconos
encajan por construcción y no por buen ojo. Descartada la IA generativa: los generadores de
imagen no mantienen la rejilla de píxeles y el resultado desentona junto a pixel art de verdad.

---

### HU-053 · Retirar el poste de entrenamiento
**Épica:** E06 · **Semana:** 07

**Criterios de aceptación**
- [ ] El poste desaparece del mapa y del generador.
- [ ] Nada en el código lo da por supuesto.

**Nota de alcance.** Era el andamio que permitía probar daño y muerte sin enemigos. Con dos
bandos que se pegan de verdad y un panel que permite cambiarse de bando, ya sobra. Se retira
**después** de que el panel funcione, no antes.

---

### HU-054 · Registro de estadísticas de partida
**Épica:** E06 · **Semana:** 07

**Criterios de aceptación**
- [ ] Se acumulan **durante** la partida, no se calculan al final.
- [ ] Son **por bando**, no globales.
- [ ] Se registran: oro, madera y carne recolectados · recursos gastados · unidades
      entrenadas, perdidas y eliminadas · edificios construidos, perdidos y destruidos ·
      pico de población · duración · tiempo hasta el primer combate · órdenes por minuto ·
      la unidad con más bajas.

**Nota técnica.** Al final de la partida ya no quedan cadáveres que contar: si no se acumula
en vivo, no hay de dónde sacarlo. Por bando desde el principio porque el FFA de cinco llega en
la semana 13, y convertir un contador global en cinco después es rehacerlo entero.

Las **órdenes por minuto** salen de una línea porque desde la semana 02 toda acción pasa por
una `Orden` ([ADR-01](ARQUITECTURA.md#adr-01)). Conviene decirlo en la expo: la métrica es
gratis por una decisión de arquitectura de hace cinco semanas.

---

### HU-055 · Condición de derrota y eliminación de bando
**Épica:** E06 · **Semana:** 07

**Criterios de aceptación**
- [ ] Un bando que pierde su castillo queda **eliminado**.
- [ ] Al eliminarse, todo lo suyo cae: unidades y edificios restantes.
- [ ] Cuando queda un solo bando en pie, la partida termina.
- [ ] La regla es la misma para el jugador y para cualquier bando: no hay caso especial.

**Nota de alcance.** La regla vive aquí; el **menú principal, la selección de mapa y el número
de bandos** siguen en la semana 12 (E09). Esto adelanta de E09 solo la condición y la pantalla
de resultado, que es lo que cierra el bucle de esta épica.

---

### HU-056 · Pantalla de victoria y derrota
**Épica:** E06 · **Semana:** 07

**Como** jugador **quiero** ver cómo terminó la partida **para** saber si gané y qué tal lo
hice.

**Criterios de aceptación**
- [ ] Cartel de **VICTORIA** o **DERROTA** montado con piezas del pack: banner, cintas,
      pergamino y espadas cruzadas.
- [ ] El titular usa **MedievalSharp** (SIL OFL 1.1, incluida en el repo con su licencia).
- [ ] Entra con rebote (escala 0 → 1,1 → 1) sobre un fondo oscurecido; en derrota, además,
      dessaturado.
- [ ] Las estadísticas aparecen **en cascada**, una a una, no todas de golpe.
- [ ] En victoria caen partículas usando `Particle FX` del pack.
- [ ] Muestra el **MVP** de la partida con su retrato de `Human Avatars`.
- [ ] Cambiando de bando con el panel se puede ver el resultado desde el lado que pierde.

**Nota técnica.** No es un GIF ni una imagen: es **interfaz animada por código**. Un GIF no
escala de resolución y no puede mostrar cifras que cambian. Además, montarla con piezas del
propio pack garantiza que el estilo encaje, cosa que ninguna imagen generada consigue con
pixel art.

---

**Meta de la semana:** que dos bandos que hasta ahora compartían mapa y se ignoraban entren en
conflicto, y que ese conflicto **tenga desenlace**. Al terminar la semana la partida se puede
ganar y se puede perder.

> **Una sola rama para la épica:** `feat/E06-combat`.

| HU | Título | Riesgo |
|---|---|---|
| HU-046 | Responder al ser atacado | medio |
| HU-047 | Correa y posturas | medio |
| HU-048 | Vida, barra y destrucción de edificios | medio |
| HU-049 | La torre con arquero guarnecido | alto |
| HU-050 | Torres rivales en el mapa generado | bajo |
| HU-051 | Panel de pruebas — versión 1 | alto |
| HU-052 | Iconos del panel con la paleta del pack | medio |
| HU-053 | Retirar el poste de entrenamiento | bajo |
| HU-054 | Registro de estadísticas de partida | bajo |
| HU-055 | Condición de derrota y eliminación de bando | medio |
| HU-056 | Pantalla de victoria y derrota | medio |

> ⛔ **Fuera de alcance, declarado antes de empezar.** El **proyectil con impacto real** sigue
> siendo cosmético esta semana: el daño se resuelve al terminar la animación y la flecha solo
> lo explica en pantalla. El **monje que cura solo** y el **triángulo de contadores** son de la
> semana 08. Dejarlos fuera ahora es lo que permite que la derrota y la pantalla de resultado
> entren completas.

### Semana 08 — El desenlace (E06, parte B)

> 📌 **Arrastrada de la semana 07:** HU-047 quedó con un criterio sin cumplir —«la postura se
> cambia desde el panel de la unidad seleccionada»—. Entra aquí como HU-058.

> ⛔ **Descartado tras discutirlo.** El proyectil con impacto real (arco, vuelo y fallo si el
> objetivo se mueve) estaba planificado y **se retira del alcance**. Un arquero que falla
> contra objetivos móviles y que no puede tocar edificios es un arquero que nadie entrena, y
> la complejidad de usarlo dejaría de compensar. La flecha se queda como está: el daño se
> resuelve al terminar la animación y la flecha lo explica en pantalla.

### HU-057 · Las unidades entrenadas no se apilan
**Épica:** E06 · **Semana:** 08

**Criterios de aceptación**
- [ ] Varias unidades entrenadas seguidas salen **repartidas en abanico**, no una encima de otra.
- [ ] Dos unidades quietas en el mismo sitio no intercambian su orden de dibujo.

**Nota técnica.** Fallo detectado en la exposición de la semana 06, y son **dos causas a la
vez**. Todas las unidades nacían en la misma celda: con la posición idéntica, el empuje blando
no tiene ninguna dirección hacia la que separarlas; y con la Y idéntica comparten
`sortingOrder`, y un empate deja el orden en manos del motor, que puede cambiarlo cada
fotograma. Se arregla repartiendo la salida **y** añadiendo un desempate estable por objeto.

---

### HU-058 · Posturas en el panel *(arrastrada de la semana 07)*
**Épica:** E06 · **Semana:** 08

**Criterios de aceptación**
- [ ] El panel de la unidad seleccionada permite cambiar entre agresiva, defensiva y quieta.
- [ ] Un solo botón que cicla, con atajo de teclado.
- [ ] Aplicado a un grupo, todas acaban en la misma postura.

**Nota de diseño.** Se cicla con un botón en vez de ofrecer tres: son tres estados de una misma
cosa —cuánta iniciativa se le deja a la unidad— y tres botones invitan a leerlos como tres
acciones distintas. La postura del grupo se decide por la primera unidad y se aplica a todas;
ciclando cada una por su cuenta, un grupo mixto no convergería nunca.

---

### HU-059 · El monje cura solo y se repliega
**Épica:** E06 · **Semana:** 08

**Criterios de aceptación**
- [ ] Busca aliados heridos en su radio y los cura sin que se lo pidan.
- [ ] Elige al que peor está **por fracción de vida**, no por vida absoluta.
- [ ] No cura por encima del 95 %.
- [ ] Al recibir daño se repliega al centro de entrega más cercano.

**Nota de diseño.** La fracción y no el valor absoluto: un guerrero de 140 al que le quedan 60
está mejor que un arquero de 70 al que le quedan 30, aunque en puntos tenga el doble. Curar al
que peor está es curar al que se va a morir, que es para lo que sirve un monje.

---

### HU-060 · Triángulo de contadores
**Épica:** E06 · **Semana:** 08

**Criterios de aceptación**
- [ ] Tipos de ataque (cortante, perforante, flecha) y de armadura (ligera, pesada, asta,
      fortificada) en la ficha de cada unidad.
- [ ] Multiplicadores en un `ScriptableObject`, editables sin recompilar.
- [ ] El triángulo se cumple: **lancero > guerrero > arquero > lancero**.
- [ ] Ningún multiplicador baja de 0,75.
- [ ] **Los edificios reciben daño completo de todos los tipos.**
- [ ] Sin tabla cargada, todo multiplica por 1 y el juego sigue.

**Nota de diseño.** Los multiplicadores son suaves a propósito. Un contador duro convierte el
combate en un acertijo de composición en vez de en una decisión táctica, y además invalidaría
de golpe el balance de las cinco unidades, que lleva ajustado desde la semana 04 sin tabla.

La columna de fortificada va entera a 1: penalizar a las flechas contra edificios haría del
arquero una unidad que no puede participar en la mitad de la partida, y una unidad que solo
sirve la mitad del tiempo es una unidad que nadie entrena.

---

### HU-061 · Realimentación de impacto
**Épica:** E06 · **Semana:** 08

**Criterios de aceptación**
- [ ] Unidades y edificios destellan al recibir un golpe.
- [ ] El destello **aclara** el sprite, no lo pinta de blanco: el color del bando se sigue viendo.
- [ ] Una obra a medio construir no destella.

**Nota técnica.** Un golpe se ve hoy como una barra que baja, y con veinte unidades peleando no
se entiende quién le está pegando a quién. Que el destello sea multiplicativo importa: pintar
de blanco plano borraría de qué facción es la unidad justo en el momento en que más importa
saberlo.

El destello vive dentro de la máquina de estados por el ADR-11 —es la única que toca el dibujo
de la unidad—: un segundo componente escribiendo el color pelearía con el desvanecido de la
muerte, y ganaría el que escribiera el último.

---

### HU-062 · Iconos de recurso legibles
**Épica:** E06 · **Semana:** 08

**Criterios de aceptación**
- [ ] Los tres iconos del HUD se recortan a su dibujo.
- [ ] El oro pasa de la moneda a la **mena**.

**Nota técnica.** Medido sobre los archivos: la moneda de `Gold_Resource` ocupa 24x26 px de un
lienzo de 128x128 — el **4 %**. El HUD escala el lienzo entero, así que salía minúscula en una
caja casi vacía. Es el mismo fallo del pergamino del cartel de final, otra vez. La mena ocupa
el 21 % del suyo y además es lo que el jugador ve en el mapa: el contador y su fuente pasan a
enseñar la misma cosa.

---

### HU-063 · Plantillas de recurso para sembrar en caliente
**Épica:** E13 *(adelantada)* · **Semana:** 08

**Criterios de aceptación**
- [ ] El generador deja una plantilla apagada de cada cosa sembrable, con hasta cuatro variantes.
- [ ] Un nodo sembrado en partida bloquea la grilla igual que uno del generador.

**Nota técnica.** Las plantillas **clonan nodos que ya están en el mapa** en vez de construirse
desde cero. Configurar un árbol requiere acertar con el recurso, el radio de bloqueo, los
tocones, los segundos de resto y la especie; una segunda copia de ese código se desincroniza
del original a la primera corrección y el editor produciría árboles que no se comportan como
los del generador. Copiando uno de verdad, la plantilla es correcta por construcción.

---

### HU-064 · Pinceles de recursos en el panel de partida libre
**Épica:** E13 *(adelantada)* · **Semana:** 08

**Criterios de aceptación**
- [ ] Pinceles de mena de oro, árbol, piedra, arbusto y oveja.
- [ ] Se pinta con el botón **mantenido**, una vez por celda.
- [ ] La rueda cambia de variante; el clic derecho suelta el pincel.
- [ ] No se siembra sobre agua ni sobre terreno ocupado.
- [ ] Borrador que quita el nodo bajo el cursor **y libera su terreno**.
- [ ] Con el pincel en la mano, la rueda no hace zoom a la cámara.

**Nota de alcance.** Es el primer trozo de la épica E13. Se adelanta desde la semana 12 porque
el argumento que la ponía allí —«el editor edita un formato de datos, y ese formato todavía se
está moviendo»— ya no aplica: `DefinicionMapa` lleva estable desde la semana 04 y los edificios
con celdas desde la 06.

> ⛔ **Fuera de alcance, y por un motivo técnico concreto.** El pincel **no pinta terreno ni
> relieve**. El pintado del tilemap y su paleta viven en el ensamblado de Editor, que no existe
> en una partida: repintar tierra en caliente exige mover ese generador a ejecución, y eso es
> un trabajo con entidad propia. Queda anotado como el primer paso de la E13 completa.

> ⛔ **El guardado de mapas tampoco entra.** Guardar mapas sin ningún sitio donde elegirlos no
> le sirve a nadie: el menú principal y la selección de mapa son la semana 12, y es ahí donde
> guardar empieza a significar algo.

---

**Meta de la semana:** cerrar la épica del combate y dejarla balanceada. La IA de la semana 10
se construye encima de esto, así que el combate tiene que estar quieto antes de PC2.

> **Una sola rama para la parte B:** `feat/E06-cierre`.

| HU | Título | Riesgo |
|---|---|---|
| HU-057 | Las unidades entrenadas no se apilan | bajo |
| HU-058 | Posturas en el panel *(arrastrada)* | bajo |
| HU-059 | El monje cura solo y se repliega | medio |
| HU-060 | Triángulo de contadores | alto |
| HU-061 | Realimentación de impacto | bajo |
| HU-062 | Iconos de recurso legibles | bajo |
| HU-063 | Plantillas de recurso | medio |
| HU-064 | Pinceles de recursos en el panel | medio |

---

# Semana 09 — Percepción (E07)

**Meta de la semana:** que el mapa deje de estar todo a la vista. Hasta ahora la partida se
jugaba con información perfecta: sabías dónde estaba el rival sin haber mandado a nadie a
mirar. Desde esta semana hay que ir a ver, y lo que ves se queda en el minimapa.

> **Una sola rama:** `feat/E07-niebla-y-minimapa`.

| HU | Título | Riesgo |
|---|---|---|
| HU-065 | Grilla de visibilidad por facción | medio |
| HU-066 | Radios de visión en las fichas | bajo |
| HU-067 | La niebla se dibuja dura en el espacio y suave en el tiempo | **alto** |
| HU-068 | Lo que está en niebla no se dibuja | medio |
| HU-069 | La niebla es una regla de juego, no un filtro | **alto** |
| HU-070 | Minimapa con el terreno del mapa | medio |
| HU-071 | El minimapa enseña bandos y respeta la niebla | medio |
| HU-072 | Clic y arrastre en el minimapa mueven la cámara | bajo |
| HU-073 | Día, mediodía y noche | medio |
| HU-074 | La percepción y la hora, en el panel de partida libre | bajo |

---

### HU-065 · Grilla de visibilidad por facción
**Épica:** E07 · **Semana:** 09

**Como** jugador **quiero** que cada bando sepa solo lo que ha visto **para** que explorar
sirva de algo.

**Criterios de aceptación**
- [ ] Cada facción tiene su propia grilla; no hay una niebla global.
- [ ] Las bases de salida —castillo y torres— se ven desde el primer fotograma aunque estén en
      sombra; el terreno y las unidades, no.
- [ ] Tres estados por celda: nunca vista, explorada sin vigilancia, a la vista.
- [ ] Lo explorado **nunca** vuelve a nunca visto.
- [ ] Cambiar de bando en el panel cambia la niebla que se dibuja, sin reiniciar la partida.
- [ ] El recálculo es periódico, no por fotograma, y sigue corriendo con el juego en pausa.

**Nota técnica.** Un byte por celda ([ADR-18](ARQUITECTURA.md#adr-18)). El mapa por defecto
tiene 50 176 celdas: cualquier cosa con un objeto de escena por celda se descarta antes de
escribirla.

---

### HU-066 · Radios de visión en las fichas
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] `DatosUnidad` y `DatosEdificio` llevan `radioVision`, y se toca sin recompilar (ADR-07).
- [ ] **El radio de visión de toda unidad es mayor que su alcance y que su radio de
      vigilancia.** Se comprueba al reconstruir el catálogo y avisa por consola si se rompe.
- [ ] El arquero ve más lejos de lo que dispara; el pawn ve menos que un militar.
- [ ] Un edificio mira desde el centro de su **planta**, no de su dibujo.

**Nota de balance.** Este invariante es lo que permite que la HU-069 no mueva nada de lo que
se ajustó en la semana 08. Es la lección de aquella semana convertida en una comprobación
automática: dos medidas correctas por separado no bastan si nadie las mide una contra otra.

---

### HU-067 · La niebla se dibuja dura en el espacio y suave en el tiempo
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] La niebla es **una** textura de un píxel por celda con filtrado de punto: el borde es un
      escalón de un tile, alineado con el tileset.
- [ ] Una celda que se descubre **se desvanece**, no se enciende de golpe.
- [ ] El dibujado corre cada fotograma aunque la visibilidad se recalcule diez veces por segundo.
- [ ] Quieto no cuesta nada: sin celdas moviéndose, la textura no se vuelve a subir.
- [ ] Lo nunca explorado se oscurece mucho pero **deja leer el terreno**.
- [ ] Lo explorado sin vigilancia lleva apenas un velo: se lee como terreno normal.

**Nota de alcance.** El manto de nubes del pack se construyó y se probó para tapar lo
inexplorado. Jugado, tapaba tanto que la partida empezaba sin poder leer el mapa. Queda en el
código con su interruptor en el panel, apagado por defecto ([ADR-18](ARQUITECTURA.md#adr-18)).

---

### HU-068 · Lo que está en niebla no se dibuja
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] Una unidad enemiga fuera de la vista no se dibuja, ni ella ni su barra de vida.
- [ ] Un edificio enemigo ya descubierto **sigue dibujándose** aunque nadie lo esté mirando.
- [ ] «Descubierto» significa **haberlo visto**, no que su terreno esté explorado.
- [ ] Las flechas no se dibujan donde el jugador no ve.
- [ ] Las unidades propias se ven siempre.
- [ ] Tapar y destapar no pelea con la selección ni con la barra de vida.

**Nota técnica.** Se veta el dibujado con `forceRenderingOff`, no con `enabled`: ese campo ya
tiene dueños ([ADR-19](ARQUITECTURA.md#adr-19)).

---

### HU-069 · La niebla es una regla de juego, no un filtro
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] Una unidad no se engancha sola a un enemigo que su bando no ve.
- [ ] Una torre no dispara a lo que su bando no ve.
- [ ] El cursor no señala como atacable lo que no se ve: el puntero no es un detector.
- [ ] No se puede ordenar un ataque sobre una unidad invisible.
- [ ] Para atacar un edificio basta haberlo descubierto.
- [ ] **Demostrable en vivo:** los tres duelos de la semana 08 dan el mismo resultado que
      antes de la niebla.

---

### HU-070 · Minimapa con el terreno del mapa
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] Abajo a la derecha, con el marco de madera del pack y escalando con la resolución.
- [ ] El terreno se cuece **una vez**: agua, tierra, mesetas, escaleras, bosque, oro y piedra.
- [ ] Se dibuja con filtrado de punto; nada borroso.

---

### HU-071 · El minimapa enseña bandos y respeta la niebla
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] Unidades y edificios salen con el color de su bando; las propias, más gruesas.
- [ ] Lo no explorado sale tapado y lo recordado, oscurecido.
- [ ] Un enemigo que nadie ve **no sale**.
- [ ] Un rectángulo marca lo que la cámara está encuadrando.

---

### HU-072 · Clic y arrastre en el minimapa mueven la cámara
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] Pulsar lleva la cámara a ese punto, respetando los límites del mapa.
- [ ] Arrastrar la sigue llevando sin soltar.
- [ ] Un clic sobre la moldura de madera no mueve la cámara.
- [ ] Un clic en el minimapa no emite ninguna orden a las unidades seleccionadas.
- [ ] **M** lo abre en grande en el centro de la pantalla, y sigue respondiendo al clic.

---

### HU-073 · Día, mediodía y noche
**Épica:** E07 · **Semana:** 09

**Como** jugador **quiero** que pase el tiempo en el mapa **para** que una partida larga no
sea siempre la misma postal.

**Criterios de aceptación**
- [ ] Tres horas encadenadas y en bucle, cada una con **su propia duración**.
- [ ] Cada hora es una meseta y luego un cambio, no un viaje continuo: sin meseta, acortar la
      noche solo acorta el trayecto, no el rato que está oscuro.
- [ ] La noche dura menos que el día. Sin antorchas ni unidades que vean de noche, la oscuridad
      no añade decisión — solo incomodidad.
- [ ] La hora oscurece **por encima**, sin tocar el color de un solo sprite.
- [ ] De noche se sigue distinguiendo el color de cada bando.
- [ ] El mediodía no cuesta ningún dibujado: lámina apagada.
- [ ] La hora avanza con el tiempo del juego: en pausa no corre, a x4 corre a x4.
- [ ] **La hora NO cambia los radios de visión.** Queda anotado para la semana de balance.

**Nota de alcance.** Venía pedido de la semana anterior y no estaba en ninguna épica. Entra
aquí porque comparte toda la maquinaria con la niebla —la misma lámina multiplicativa, el
mismo shader, el mismo orden de dibujado— y montarlo por separado sería montarlo dos veces.

---

### HU-074 · La percepción y la hora, en el panel de partida libre
**Épica:** E07 · **Semana:** 09

**Criterios de aceptación**
- [ ] **Ignorar niebla**: se ve el mapa y los ejércitos.
- [ ] **Revelar mapa**: se ve el terreno pero **no** lo que se mueve. Las dos trampas son
      distintas y se pueden enseñar por separado.
- [ ] Mostrar u ocultar el minimapa, y que el minimapa respete la niebla o no.
- [ ] Saltar a día, mediodía o noche, y congelar el reloj.
- [ ] El rótulo **PARTIDA LIBRE** sigue viéndose: ninguna captura tramposa pasa por buena.

**Nota.** El panel crece con cada épica y esto es lo que le tocaba a la E07
([ADR-16](ARQUITECTURA.md#adr-16)). El sello se muda a la esquina de arriba a la derecha,
porque abajo a la derecha entra el minimapa y de las cuatro esquinas esa era la única libre.

---

### Semana 09 — Niebla, minimapa y hora del día (E07)
Detallada arriba, en su propia sección.

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

> **Mesetas de varios niveles.** El pincel de relieve solo distingue llano y meseta. La grilla
> ya guarda la altura como número y el pathfinding ya permite un escalón, así que subir a dos
> alturas es cuestión de generalizar el pintado: la pared se dibuja donde el vecino de arriba
> sea más alto, no solo donde el de abajo sea cero. Queda anotado aquí y no en la semana 04
> porque el relieve de un nivel ya está entregado y funcionando.

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
