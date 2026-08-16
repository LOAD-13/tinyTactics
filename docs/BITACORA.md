# Bitácora — Tiny Tactics

> Registro de **lo que realmente pasó** cada semana, no de lo que se planeó.
> Es la memoria del proyecto: permite retomar el hilo en la semana 12 y es la materia prima
> de la documentación técnica final de la semana 17.
>
> **Regla:** si algo se prometió y no se hizo, se escribe que no se hizo. No se maquilla.
> Un registro honesto sirve; uno decorativo no sirve para nada.

Formato de entrada: entradas nuevas **arriba**.

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
