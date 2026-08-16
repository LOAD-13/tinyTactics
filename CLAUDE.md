# CLAUDE.md — Contexto operativo de Tiny Tactics

> Este archivo es el motor del sistema de trabajo. Se carga al inicio de cada sesión.
> Si algo acá contradice lo que creo recordar, **gana este archivo**.

---

## 1. Qué es esto

**Tiny Tactics** es un RTS 2D en tiempo real estilo Warcraft (recolectar → construir → entrenar → atacar),
desarrollado en **Unity 6000.5.8f1** con plantilla 2D URP y los assets gratuitos **Tiny Swords**.

Es el proyecto del curso **Diseño y Desarrollo de Juegos Interactivos II** (100000S11F),
ciclo 2026-2 Agosto, docente **Omar David Machuca Ñuflo**.

- Repo: <https://github.com/LOAD-13/tinyTactics>
- Diseño del juego → [`docs/GDD.md`](docs/GDD.md)
- Decisiones técnicas y su porqué → [`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)
- Plan de 18 semanas → [`docs/CRONOGRAMA.md`](docs/CRONOGRAMA.md)
- Backlog de épicas y HUs → [`docs/BACKLOG.md`](docs/BACKLOG.md)
- Ramas y commits → [`docs/GITFLOW.md`](docs/GITFLOW.md)
- **Qué pasó realmente cada semana → [`docs/BITACORA.md`](docs/BITACORA.md)**

---

## 2. Equipo

| Persona | Código | Rol |
|---|---|---|
| **Joaquín Alfonso Loa Denegri** | U22234069 | Desarrollo (escribe **todo** el código), arquitectura, integración |
| **Kiara Mishell Santti Saavedra** | U22201636 | Diseño de juego, arte, documentación |
| **Gerardo Raúl Socualaya Mandamiento** | U22229068 | QA, balance, pruebas |

> ⚠️ **Joaquín es el único que programa.** Kiara y Raúl exponen y sustentan igual, así que
> el guion semanal debe incluir **preguntas probables con su respuesta** para quien expone.
> Ese es el punto débil real del equipo frente a la rúbrica (4 pts de "dominio técnico").

---

## 3. Ritmo del curso

| Momento | Qué pasa |
|---|---|
| **Domingo** | Entrega al intranet: PPT + evidencias + link al repo (tag de la semana) |
| **Lunes** | Exposición y sustentación. **Expone un solo integrante**, rotando |
| **Miércoles** | Laboratorio: se desarrolla el incremento de la semana siguiente |

Evaluaciones: **PC1** S05 (20%) · **PC2** S10 (20%) · **PC3** S15 (30%) · **Examen Final** S18 (30%).
Entrega final consolidada en **S17**.

> Los PDFs de guía del docente dicen "martes". **Es lunes.** También dicen "solo 2D";
> el docente aclaró que puede ser 2D o 3D. Esos PDFs son orientativos, no ley.

---

## 4. Reglas de trabajo — no negociables

1. **Nunca hago commits ni PRs.** Entrego los **comandos** de commit y push;
   Joaquín los ejecuta y abre el PR desde la web de GitHub.
2. **Sin CI ni GitHub Actions.** Se evaluó y se descartó.
3. **"Automatizar" acá significa tener el contexto cargado**, no escribir scripts.
   Antes de proponer tooling: ¿resuelve un problema que Joaquín tenga hoy?
4. Los entregables semanales (PPT, guion, PDF, capturas) van a `Semana NN/` en la
   **carpeta del curso**, fuera del repo. El repo lleva código + `docs/`.

   ⚠️ **La carpeta es la semana en que se DESARROLLÓ, no en la que se expone.**
   Ejemplo: lo trabajado del 10 al 16/08 se guarda en `Semana 01/`, aunque se exponga
   el lunes 17/08, que es la semana 02 del curso. Siempre hay un desfase de uno.
5. El PPT **no siempre usa los 14 slides** de la plantilla. Se ajusta a lo que hay que mostrar.
6. **La sesión de trabajo corre desde la carpeta del curso**, no desde el repo.
   Este archivo se lee desde ahí; no hace falta abrir otra sesión.

---

## 5. Decisiones cerradas — no re-litigar

| Tema | Decisión |
|---|---|
| **Multijugador** | Core *network-ready* desde el día 1: las unidades obedecen a una **autoridad** y toda acción es una **orden serializable** (patrón Command). El entregable comprometido es single-player. PvP LAN 1v1 es *stretch* con compuerta **go/no-go en S11**. |
| **Formato** | FFA de hasta **5 bandos**, **cap 50 unidades/bando** (~250 en pantalla). Orden de prueba: 1v1 → 3 → 5. |
| **Niebla de guerra** | Dentro del alcance. Pendiente decidir si la IA la respeta o ve todo el mapa. |
| **Movimiento** | A* sobre grilla + interpolación suave + empuje blando. **Descartados** NavMesh y RVO (no deterministas, caros, matan el PvP futuro). |
| **Animación** | Sprite-swap con script propio. **Prohibido el componente `Animator`** en unidades — es el mayor costo por unidad a escala. |
| **Búsqueda de objetivos** | Grilla espacial. Prohibido `FindObjectsOfType` o recorrer todas las unidades por frame. |
| **Pathfinding** | En cola: N rutas por frame + flow field por grupo. Nunca 250 A* de golpe. |
| **IA** | Por capas: estratégica (~1 Hz), táctica (oleadas), FSM por unidad. Las 3 dificultades son **bonus de recursos + timing**, no IA más lista. |

---

## 6. El ritual semanal

### Apertura — *"arrancamos semana N"*
1. Leo `BITACORA.md` (qué pasó) y `BACKLOG.md` (qué toca).
2. Propongo el alcance de la semana y detallo las HUs con criterios de aceptación.
3. Entrego los comandos para crear las ramas desde `develop`.

### Durante
Desarrollo normal. Entrego código, revisiones, debugging y los comandos de commit
con el formato de [`docs/GITFLOW.md`](docs/GITFLOW.md).

### Cierre — *"cerramos semana N"*
1. Leo los commits reales del repo y los **comparo contra lo prometido el lunes**.
   Si algo no se hizo, se dice; no se maquilla.
2. Entrego:
   - **Contenido del PPT**, slide por slide (cantidad variable según lo que haya que mostrar)
   - **Guion del expositor** con tiempos + preguntas probables y sus respuestas
   - **Lista de capturas a tomar**, indicando en qué slide va cada una
   - **Comandos** de merge a `develop`, PR a `main` y tag `v0.N.0-sNN`
3. Actualizo `BITACORA.md`.

---

## 7. Convenciones de código

- **C#**, convenciones estándar de Unity/.NET: `PascalCase` para tipos y métodos públicos,
  `camelCase` para locales, `_camelCase` para campos privados.
- Nombres de dominio **en español** (`Unidad`, `Recurso`, `Faccion`), términos técnicos en inglés
  cuando ya son estándar (`Pathfinder`, `GridMap`). Consistencia sobre pureza.
- Un archivo, una responsabilidad. Si un script pasa de ~300 líneas, probablemente hace de más.
- Todo lo que el jugador pueda ordenar se modela como **orden serializable**, nunca como
  input leído directamente por la unidad. Esto es lo que mantiene abierta la puerta del PvP.
- `ScriptableObject` para datos de balance (stats de unidades, costos). Nunca números mágicos
  en el código — el balance se toca sin recompilar.

---

## 8. Estado actual

> **Actualizar esta sección al cerrar cada semana.**

- **Semana de trabajo:** 01 → los entregables van a `Semana 01/`
- **Se expone como:** semana 02 del curso, lunes 17/08/2026
- **Entrega al intranet:** domingo 16/08/2026
- **Expone:** Joaquín
- **Última entrega:** ninguna todavía · **Último tag:** ninguno
- **Estado del repo:** docs base escritos, sin pushear. Comandos en [`docs/BITACORA.md`](docs/BITACORA.md)
