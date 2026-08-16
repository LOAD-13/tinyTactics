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
_(se completa al cerrar la semana)_

### Problemas y decisiones
- El repo venía con un único commit `Initial check-in` generado por Unity, sin `develop`
  y con los assets de Tiny Swords sin versionar. La semana 02 arranca resolviendo eso.

### Estado al cierre
_(se completa al cerrar la semana)_

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

git add CLAUDE.md README.md docs/ .gitignore

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
