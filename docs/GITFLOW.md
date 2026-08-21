# GitFlow — Tiny Tactics

## 1. Ramas

```
main                    ← solo entregas. Un tag por semana: v0.3.0-s03
└── develop             ← integración. Siempre debe abrir en Unity sin errores
    ├── feat/HU-012-seleccion-caja
    ├── feat/HU-013-orden-movimiento
    ├── fix/HU-020-pawn-atascado-arbol
    └── docs/HU-001-documentacion-base
```

| Rama | Regla |
|---|---|
| `main` | Solo recibe merges de `develop`, los domingos de entrega. **Cada merge lleva su tag.** |
| `develop` | Rama de integración. Debe abrir en Unity **sin errores de consola**, siempre. |
| Ramas de HU | Una rama por Historia de Usuario. Nacen de `develop` y vuelven a `develop`. |

### Nomenclatura

```
<tipo>/HU-<id>-<slug-corto>
```

| Tipo | Cuándo |
|---|---|
| `feat` | Funcionalidad nueva |
| `fix` | Corrección de un bug |
| `docs` | Solo documentación |
| `refactor` | Reorganización sin cambiar comportamiento |
| `chore` | Configuración, dependencias, estructura de carpetas |

Ejemplos: `feat/HU-012-seleccion-caja` · `fix/HU-031-pawn-no-deposita` · `chore/HU-002-estructura-carpetas`

---

## 2. Commits

**Conventional Commits.** El scope es el área del juego, no el archivo.

```
feat(seleccion): caja de arrastre selecciona unidades propias

Proyección de pantalla a mundo con filtro por facción.
Shift acumula sobre la selección actual en vez de reemplazarla.

HU-012
```

Reglas:
- Primera línea en **imperativo y en minúscula**, máximo ~72 caracteres.
- El cuerpo explica **el porqué**, no el qué (el diff ya dice el qué).
- Última línea: el ID de la HU. Es lo que permite rastrear commit → HU → evidencia del PPT.

Scopes habituales: `grilla`, `pathfinding`, `seleccion`, `movimiento`, `economia`, `construccion`,
`combate`, `niebla`, `ia`, `ui`, `audio`, `camara`, `datos`, `docs`.

---

## 3. Ciclo semanal

| Cuándo | Qué |
|---|---|
| **Lunes / miércoles** | Se crean las ramas de las HUs de la semana desde `develop` |
| **Durante** | Commits y push en la rama de cada HU |
| **Viernes-sábado** | PR de cada rama → `develop`, se cierra la HU y **se borra la rama** |
| **Domingo** | PR `develop` → `main` + **tag `v0.N.0-sNN`**. Ese tag es la entrega |

### Las ramas de HU se borran al mergear

Una rama de historia de usuario es **temporal**: nace de `develop`, vuelve a `develop` y
desaparece. Lo que persiste como registro histórico es el **tag**, no la rama.

Dejarlas acumularse hace que en la semana 12 haya cuarenta ramas muertas y nadie sepa cuál
sigue viva. Y no se pierde nada: los commits ya están en `develop`, y GitHub conserva el
historial del pull request aunque la rama ya no exista.

```bash
git checkout develop
git pull origin develop

git branch -d feat/HU-0NN-descripcion            # local
git push origin --delete feat/HU-0NN-descripcion  # remoto
```

⚠️ **Siempre `-d` en minúscula**, que solo borra si la rama está mergeada. Si git responde
*"not fully merged"*, es una señal real de que algo no llegó a `develop`: hay que investigar,
nunca forzar con `-D`.

Permanentes: **`main`** y **`develop`**. Todo lo demás es temporal.

### Los tags son la evidencia

En vez de entregar "acá está el repo", se entrega:

> *El avance de la semana 5 corresponde al tag `v0.5.0-s05`.*

El docente ve el estado **exacto** del proyecto en esa fecha, sin ambigüedad y sin poder confundirlo
con trabajo posterior. Es verificable, y casi ningún grupo lo hace.

Formato: `v0.<semana>.0-s<semana>` → `v0.2.0-s02`, `v0.3.0-s03`, ... La entrega final es `v1.0.0-s17`.

---

## 4. Comandos

### Crear la rama de una HU

```bash
git checkout develop
git pull origin develop
git checkout -b feat/HU-012-seleccion-caja
```

### Commitear

```bash
git add Assets/Scripts/Seleccion/
git commit -m "feat(seleccion): caja de arrastre selecciona unidades propias" \
           -m "Proyección de pantalla a mundo con filtro por facción." \
           -m "HU-012"
git push -u origin feat/HU-012-seleccion-caja
```

### Cerrar la semana

```bash
# 1) Cada HU se mergea a develop vía PR en la web de GitHub.
# 2) Ya con develop actualizado:
git checkout develop
git pull origin develop

# 3) PR de develop -> main desde la web, y una vez mergeado:
git checkout main
git pull origin main
git tag -a v0.3.0-s03 -m "Entrega semana 03 — selección y movimiento de unidades"
git push origin v0.3.0-s03
```

---

## 5. Reglas operativas

1. **`develop` siempre abre en Unity sin errores.** Si un merge lo rompe, se arregla antes de seguir.
2. **Conflictos de `.meta`: nunca a mano.** Se toma una versión y se deja que Unity regenere.
   Un `.meta` mal resuelto rompe referencias de prefabs de forma silenciosa.
3. **Nunca commitear `Library/`, `Temp/`, `Logs/`, `obj/`.** Ya están en `.gitignore`; si aparecen
   en `git status`, algo se configuró mal.
4. **Un PR por HU.** Aunque sean de una línea. El historial de PRs es parte de la evidencia
   que se le muestra al docente.

   **Excepción — HUs que comparten rama.** A veces dos HUs son técnicamente inseparables
   (por ejemplo, un generador de escena que construye también la cámara: no se puede mergear
   una sin la otra). En ese caso comparten rama, **pero se declara ANTES de empezar**, no
   después: se anota en la HU del backlog y en la bitácora. Inventar una rama vacía solo para
   aparentar trazabilidad es peor que decir la verdad.

   | Fusión | Rama | Motivo |
   |---|---|---|
   | HU-005 dentro de HU-001 | `docs/HU-001-documentacion-base` | La ficha conceptual es una sección del mismo `GDD.md` |
   | HU-003 + HU-004 | `feat/HU-003-generacion-de-mapa` | El generador construye la cámara dentro de la escena |
   | HU-006 … HU-018 | `feat/E02-nucleo-y-desniveles` | Ver nota de abajo |

   > **Sobre `feat/E02-nucleo-y-desniveles`.** Esta rama se declara *después* de empezar, y eso
   > es exactamente lo que la regla de arriba prohíbe. Se anota igualmente en vez de fingir
   > trazabilidad con ramas vacías creadas a posteriori.
   >
   > La rama nació como `feat/HU-006-grilla-logica` y acabó llevando la épica E02 entera. El
   > motivo real: las HUs del núcleo resultaron ser un solo bloque —una grilla sin A* no se
   > puede probar, un A* sin unidades tampoco, y unas unidades sin selección menos— y el
   > alcance creció en marcha con dos HUs que no estaban en el plan (HU-017 y HU-018).
   >
   > **Lección para la semana 04:** trocear la épica en ramas mergeables *antes* de escribir la
   > primera línea, y aceptar que una HU sin probar en aislamiento se mergea igual si el
   > conjunto compila. Trece HUs en una rama no son revisables.
5. **Escenas y prefabs son YAML gigante y no se mergean bien.** Hoy el riesgo es bajo porque
   solo Joaquín programa, pero la disciplina se mantiene: preferir **prefabs** sobre meter
   todo en la escena. Si el día de mañana alguien más toca código, esta regla salva el proyecto.

---

## 6. Primer arranque

Estado actual del repo: un solo commit (`Initial check-in`) en `main`, sin `develop`.
Los assets de Tiny Swords y los cambios de `ProjectSettings` están **sin commitear**.

Los comandos concretos del primer push están en la entrada de la Semana 02 de
[`BITACORA.md`](BITACORA.md).
