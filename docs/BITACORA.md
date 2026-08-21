# Bitácora — Tiny Tactics

> Registro de **lo que realmente pasó** cada semana, no de lo que se planeó.
> Es la memoria del proyecto: permite retomar el hilo en la semana 12 y es la materia prima
> de la documentación técnica final de la semana 17.
>
> **Regla:** si algo se prometió y no se hizo, se escribe que no se hizo. No se maquilla.
> Un registro honesto sirve; uno decorativo no sirve para nada.

Formato de entrada: entradas nuevas **arriba**.

---

## Semana 03 — Núcleo de simulación y desniveles
**Entrega:** domingo 23/08/2026 · **Expo:** lunes 24/08/2026 · **Expone:** Kiara
**Tag:** _(pendiente)_ `v0.3.0-s03`

### Lo prometido
**Bloque A:** HU-006 grilla lógica · HU-007 A* con cola · HU-013 unidades · HU-008 movimiento
interpolado · HU-009 empuje blando · HU-010 selección por clic · HU-011 caja de arrastre ·
HU-012 orden de movimiento.
**Bloque B:** HU-014 mesetas · HU-015 acantilados · HU-016 escaleras.

### Lo entregado
Todo el bloque A y todo el bloque B. Además, **HU-017 (interfaz de selección)**, que no estaba
en el plan: el núcleo funcionaba pero no se *veía* funcionar, y el pack ya traía punteros,
corchetes, barras y retratos sin usar.

De propina, dos cosas pequeñas que no eran HU: las ovejas ahora pastan y se mueven, y apareció
un **editor de relieve con pincel** que resultó ser el germen del editor de mapas de E13.

### Lo que costó de verdad

**Tres errores seguidos con los índices del tileset.** La hoja `Tilemap_color1.png` son 9×6
celdas, pero **la quinta columna está vacía** y Unity no genera sprite para una celda
transparente: salen 44 sprites para 54 celdas. Leyendo la imagen como una rejilla de 9 salen
índices corridos, y el terreno elevado se pintó con las piezas de pared.

Pero el error de fondo no fue ese, sino **deducir en vez de medir**. Tres veces:

1. Índices corridos por contar 9 columnas en vez de 8 → mesetas a rayas de roca.
2. Las cuatro piezas de muro tomadas por variantes intercambiables cuando son un **autotile
   horizontal** (extremo izq / medio / extremo der / suelto) → una rendija de hierba entre
   bloque y bloque.
3. Las dos mitades de rampa tomadas por un bloque de 2×2 cuando cada una es **una rampa entera
   de 1×2** con sentido propio → cuestas en forma de pico.

Y una cuarta, ya con el sentido: la primera versión salió invertida porque leí el pixel art a
ojo. La corrigió Joaquín probándolo, no yo mirándolo.

**Lo que se aprende:** con un tileset, mirar la imagen no basta. Hay que **medir los sprites**
—el `.meta`, los bordes alfa de cada pieza— y montar una maqueta antes de escribir el mapeo.
Los tres primeros errores se habrían evitado con el escaneo de bordes que acabó resolviéndolos.

**El panel de unidad, a medias.** La primera versión salió cortada por abajo: `Anclar` forzaba
pivote centrado en todo, incluido el panel raíz, así que con el ancla en el borde inferior la
mitad de la caja caía fuera del encuadre — y no había forma de arreglarlo desde el Inspector
porque el pivote se reescribía en `Awake`.

También hubo dos detalles que solo se ven probando: los retratos ocupan 197 de los 256 px del
PNG y por eso la cara se veía pequeña por mucho que se agrandara la caja (se recortan al
contenido, con un recorte común a los 25 para que la rejilla no baile), y los números de las
estadísticas salían como ceros porque su rectángulo de texto empezaba antes del icono y solo
asomaba el último dígito.

**El volteo de sprite.** Pasó de `transform.localScale` a `SpriteRenderer.flipX`. Invertir la
escala arrastraba a los hijos, y la barra de vida se habría vaciado al revés cada vez que la
unidad camina hacia la izquierda.

**Un desbordamiento silencioso.** El reparto de rampas comparaba contra `int.MinValue`; la
resta se salía de rango y habría descartado siempre la primera rampa de cada meseta. Se vio
leyendo el código, no ejecutándolo.

### Decisiones tomadas

- **El relieve se dibuja, no se genera.** El ruido propone; dónde va un acantilado es una
  decisión de diseño de nivel. Queda como ADR-10.
- **Sin `.ttf` en el repo.** Las fuentes se cargan del sistema operativo con una lista de
  preferencia y respaldo a la del motor. Las de Windows tienen licencia de Microsoft y el
  repositorio es público.
- **Las cajas de facción se retiñen por código.** El pack solo trae botones azules y rojos,
  pero las facciones son cinco. Se lleva el matiz del botón al del listón de cada bando,
  tocando solo los píxeles de ese color para no ensuciar el marco crema.
- **Rampas solo al sur.** No es una limitación del código: el pack no trae más piezas. La cara
  de acantilado solo existe mirando hacia abajo, que es la única que se ve en vista cenital.

### Pendiente
- Migrar la UI a TextMeshPro en la semana 15, cuando toque el HUD completo.
- Dibujar la caja de arrastre con los corchetes del pack en vez del rectángulo de `OnGUI`.

---

## Semana 02 — Fundación
**Entrega:** domingo 16/08/2026 · **Expo:** lunes 17/08/2026 · **Expone:** Joaquín
**Tag:** _(pendiente)_ `v0.2.0-s02`

### Lo prometido
HU-001 documentación base · HU-002 estructura de carpetas · HU-003 escena con el mapa base ·
HU-004 cámara RTS · HU-005 ficha conceptual.

### Lo hecho
Las cinco, y bastante más de lo previsto en HU-003.

| HU | Estado | Notas |
|---|---|---|
| HU-001 Documentación base | ✅ | `README.md` y 6 documentos en `docs/` |
| HU-002 Estructura de carpetas | ✅ | `Assets/Scripts/` por dominio + assets Tiny Swords versionados |
| HU-005 Ficha conceptual | ✅ | Dentro de `GDD.md`, en la rama de HU-001 (declarado) |
| HU-003 Escena con el mapa | ✅ | Superó el alcance: generación procedimental completa |
| HU-004 Cámara RTS | ✅ | Paneo, zoom y confinamiento |

**2 128 líneas de C#** en 7 archivos:

- `Entrada/CamaraRTS.cs` — paneo WASD y por borde, zoom de rueda, confinamiento al mapa.
- `Mundo/DefinicionMapa.cs` — `ScriptableObject` con los 25 parámetros del mapa.
- `Mundo/GeneradorTerreno.cs` — generación determinista: ruido plegado, brazos de mar,
  autotile de 16 piezas, crecientes de recursos, arrecifes.
- `Mundo/DerivaNube.cs` — deriva de nubes con reaparición por el lado opuesto.
- `Unidades/AnimadorSprite.cs` — animación por sprite-swap (**ADR-03**, adelantado).
- `Unidades/DeambularPawn.cs` — los pawns pasean y descansan junto al castillo.
- `Editor/ConstructorDeMapa.cs` — menú que construye la escena entera.

**Resultado: mapa "Tres Coronas"**, 224×224 tiles, 3 bandos, simetría rotacional exacta.
Tres lóbulos unidos solo por la meseta central. Castillo y dos pawns por bando.

### Problemas y decisiones

**1. El repo venía sin `develop`.** Un único commit `Initial check-in` de Unity y los assets
de Tiny Swords sin versionar. Se resolvió al arrancar.

**2. El pack usa otros nombres que el GDD.** `Warrior` y `Monk`, no "caballero" y "clérigo";
`Monastery`, no "iglesia". Se renombró todo a Guerrero / Monje / Monasterio y el GDD lleva
ahora una columna con el nombre original del pack.

**3. El proyecto usa solo el Input System nuevo** (`activeInputHandler = 1`). `Input.GetAxis`
lanza excepción. La cámara se escribió con `Keyboard.current` / `Mouse.current`.

**4. El mapa salía siempre como un blob redondo.** Causa: se multiplicaba el ruido por la
caída radial, lo que aplasta la estructura del ruido y hace que el umbral recorte un círculo
sin importar la semilla. Se cambió a interpolar entre ambos (`Mathf.Lerp`). Verificado
portando el algoritmo a Python y renderizando 12 combinaciones de parámetros.

**5. Un lóbulo se recortaba y los otros no.** El margen de agua era **cuadrado** y el mapa es
radial: hacia un eje hay 111 tiles hasta el borde y hacia una diagonal 128. Se pasó a medir el
margen por radio.

**6. Recursos esparcidos en anillos.** Parecían decoración, no economía. Se rehízo a
**crecientes**: oro en el arco interior y muralla de árboles en el exterior, con huecos entre
ellas reservados para las futuras bajadas.

**7. ⚠️ Unity se quedó con ensamblados obsoletos y bloqueó el Play.** Durante horas se estuvo
probando código viejo sin saberlo: la escena decía `192x192` mientras el fuente decía `224x224`.
El log de compilación estaba congelado y se dio por buena una verificación que no lo era.
**Corrección de proceso:** el chequeo compara ahora la fecha del DLL con la del fuente y avisa
si no coinciden. No se declara "compila" sin esa comprobación en verde.

**8. Los arrecifes salían pegados a la costa e invisibles en mar abierto.** Se sembraban por
densidad con probabilidad ×6 junto a la orilla, y su orden de dibujo (−900) los dejaba por
debajo del tilemap de agua (−30). Se pasó a formaciones en aguas abiertas con separación
mínima garantizada y orden absoluto −25.

### Decisiones de alcance tomadas esta semana

- **Los tres mapas pasan a ser 1v1, 1v1v1 y 4 jugadores**, en vez de FFA de 5 en todos.
  Simetría de 2, 3 y 4 pliegues es más limpia y se parece a los mapas reales de Warcraft.
  El color se elige en la UI de partida, desacoplado del mapa. (Actualizado en `GDD.md` §8.)
- **Desniveles, acantilados y escaleras se posponen a la semana 03**, junto con la grilla
  lógica. Un acantilado no es decoración: es pathfinding. Pintarlos sin grilla dejaría a las
  unidades caminando por encima. El diseño de mapa ya reserva los huecos donde irán.
- **HU-003 y HU-004 comparten rama**, declarado antes de empezar (ver `GITFLOW.md` §5.4).

### Estado al cierre
- Escena `Assets/Scenes/Juego.unity` generada y en Build Settings.
- Mapa jugable de 224×224 con ~90 nodos de oro, ~250 árboles, ~60 ovejas, arrecifes y 34 nubes.
- Cámara funcional; ovejas, arbustos, arrecifes y pawns animados.
- Sin lógica de juego todavía: no hay selección, movimiento ni recolección. Eso es la semana 03.

---

### 📋 Comandos del primer push

Ejecutar en la raíz del repo. **Los PRs se abren desde la web de GitHub.**

**Paso 0 — abrir Unity una vez** ⚠️

Antes de commitear nada. Las carpetas nuevas de `Assets/Scripts/` se crearon desde fuera del
editor y **todavía no tienen sus archivos `.meta`**. Unity los genera al abrir el proyecto.
Si se commitea sin ese paso, la siguiente apertura ensucia el árbol con metas sin versionar.

Abrir el proyecto en Unity, esperar a que termine de importar, y cerrarlo.

**Paso 1 — crear `develop`**

```bash
git checkout main
git pull origin main
git checkout -b develop
git push -u origin develop
```

**Paso 2 — versionar la documentación** *(HU-001)*

```bash
git checkout -b docs/HU-001-documentacion-base

git add README.md docs/ .gitignore

git commit -m "docs(proyecto): documentacion base, cronograma propio y backlog" \
           -m "GDD con reglas y balance inicial, cronograma propio de 18 semanas, backlog de epicas e historias, convencion GitFlow, 8 decisiones de arquitectura y bitacora. Se anade el .gitignore del equipo." \
           -m "HU-001"

git push -u origin docs/HU-001-documentacion-base
```

→ Abrir PR `docs/HU-001-documentacion-base` → `develop` y mergear.

**Paso 3 — versionar el proyecto base, assets y estructura** *(HU-002)*

```bash
git checkout develop
git pull origin develop
git checkout -b chore/HU-002-estructura-carpetas

git add Assets ProjectSettings

git commit -m "chore(proyecto): importa Tiny Swords y crea la estructura de carpetas" \
           -m "Pack Tiny Swords (Pixel Frog, uso libre) como base visual. Esqueleto de Assets/Scripts por dominio segun ARQUITECTURA.md, mas Prefabs y Datos. Ajustes de URP y ProjectSettings para pixel art." \
           -m "HU-002"

git push -u origin chore/HU-002-estructura-carpetas
```

→ Abrir PR `chore/HU-002-estructura-carpetas` → `develop` y mergear.

**Paso 4 — verificar antes de seguir**

```bash
git checkout develop
git pull origin develop
git status          # debe salir limpio
```

⚠️ Si `git status` muestra `Library/`, `Temp/` o `Logs/`, algo quedó mal en el `.gitignore`.
No continuar hasta resolverlo: un push de `Library/` son cientos de MB.

**Paso 5 — cierre de la semana (domingo 16/08)**

```bash
# Tras mergear el PR develop -> main desde la web:
git checkout main
git pull origin main
git tag -a v0.2.0-s02 -m "Entrega semana 02 — fundacion, mapa base y camara RTS"
git push origin v0.2.0-s02
```

El link que se entrega al intranet es el del tag, no el del repo a secas:
`https://github.com/LOAD-13/tinyTactics/releases/tag/v0.2.0-s02`

---

## Semana 01 — Presentación del curso
**Expo:** —  · **Entrega:** ninguna

Presentación del curso y metodología. Formación del equipo, elección del juego, del motor y
de los assets. Sin entrega al intranet (el cronograma del docente no la contempla para esta semana).

**Decisiones tomadas:** RTS 2D estilo Warcraft · Unity 6 · pack Tiny Swords · nombre *Tiny Tactics*.
