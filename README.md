<div align="center">

<img src="docs/img/logo.png" alt="Tiny Tactics" width="560">

### Recolecta. Construye. Conquista.

**Un RTS de estrategia en tiempo real en pixel art, para hasta 5 bandos.**

<br>

![Unity](https://img.shields.io/badge/Unity-6000.5.8f1-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-.NET-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![URP](https://img.shields.io/badge/Render-2D%20URP-1a7f5a?style=for-the-badge)
![Plataforma](https://img.shields.io/badge/Plataforma-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)

<br>

<img src="docs/img/faccion_blue.png" height="80">
<img src="docs/img/faccion_red.png" height="80">
<img src="docs/img/faccion_yellow.png" height="80">
<img src="docs/img/faccion_purple.png" height="80">
<img src="docs/img/faccion_black.png" height="80">

</div>

---

## Índice

- [El juego](#el-juego)
- [Características](#características)
- [Cómo se juega](#cómo-se-juega)
  - [Recursos](#recursos)
  - [Unidades](#unidades)
  - [Triángulo de contadores](#triángulo-de-contadores)
  - [Edificios](#edificios)
  - [Niebla de guerra](#niebla-de-guerra)
- [La IA rival](#la-ia-rival)
- [Mapas](#mapas)
- [Controles](#controles)
- [Arquitectura técnica](#arquitectura-técnica)
- [Cómo ejecutar el proyecto](#cómo-ejecutar-el-proyecto)
- [Desarrollo](#desarrollo)
- [Documentación](#documentación)
- [Equipo](#equipo)
- [Créditos](#créditos)

---

## El juego

Empiezas con un castillo, una casa y dos pawns en una esquina del mapa. En algún lugar,
oculto tras la niebla, hay hasta cuatro rivales haciendo exactamente lo mismo.

**Tiny Tactics** es un RTS clásico de vista cenital: mandas a tus pawns a talar, picar oro y
cazar ovejas; conviertes esos recursos en casas que amplían tu población, en cuarteles que
producen soldados y en torres que protegen tu territorio. Y cuando crees que tienes ventaja,
atacas — porque el que espera demasiado pierde.

La tensión central es la del género desde hace treinta años:

> **Cada pawn que mandas a recolectar es un soldado que no entrenaste.**

No hay forma de ir seguro. Invertir en economía te da un ejército más grande *después*;
invertir en tropas te da presión *ahora*. La niebla de guerra impide saber cuál de las dos
eligió tu rival, y para cuando lo descubres, normalmente ya es tarde.

---

## Características

| | |
|---|---|
| 🏰 **Economía de tres recursos** | Oro para tropas, madera para edificios, y **carne como sustento**: tu ejército come, así que no puedes desatender la economía mientras peleas |
| ⚔️ **Cinco unidades con contadores reales** | Un triángulo donde ninguna composición pura gana. Un ejército de puros guerreros pierde contra uno mixto del mismo costo |
| 🏗️ **Construcción y población** | Seis edificios. Cada casa amplía tu límite de tropas hasta un tope de 50 |
| 🌫️ **Niebla de guerra** | Visibilidad por facción, con terreno recordado y unidades ocultas. Lo que no ves, no existe |
| 🤖 **IA rival en tres dificultades** | Arquitectura por capas: estratégica, táctica y por unidad |
| 👥 **Hasta 5 bandos** | Todos contra todos, sin alianzas |
| 🗺️ **Tres mapas** | Cada uno favorece un estilo de juego distinto |

---

## Cómo se juega

### El bucle

```
        ┌──────────────────────────────────────────────┐
        │                                              │
        ▼                                              │
   Recolectar  ──▶  Construir  ──▶  Subir población    │
                         │                  │          │
                         ▼                  ▼          │
                    Defender  ◀──  Entrenar tropas ──▶ Atacar
```

**Ganas** cuando eres el último bando con castillo en pie.
**Pierdes** cuando destruyen el tuyo.

### Recursos

<table>
<tr>
<td align="center" width="140"><img src="docs/img/gold.png" height="64"><br><b>Oro</b></td>
<td>Se pica de las vetas del mapa. Es la moneda del ejército: <b>todas</b> las unidades cuestan oro, y las avanzadas cuestan más.</td>
</tr>
<tr>
<td align="center"><img src="docs/img/wood.png" height="64"><br><b>Madera</b></td>
<td>Se tala de los árboles. Es lo que construye: casas, cuarteles, torres. Sin madera no creces.</td>
</tr>
<tr>
<td align="center"><img src="docs/img/meat.png" height="64"><br><b>Carne</b></td>
<td>Se obtiene cazando las ovejas del mapa. <b>Es sustento, no una moneda:</b> cada unidad viva la consume lentamente. Si tu reserva llega a cero, tus tropas pierden daño y velocidad hasta que la repongas.</td>
</tr>
</table>

> **Por qué la carne funciona así.** Un tercer recurso que solo se gastara en comprar cosas sería
> decoración. Como sustento obliga a mantener economía **durante** la guerra, y castiga la
> estrategia de vaciar el banco en un único ataque desesperado.

### Unidades

<table>
<tr>
<th width="110">&nbsp;</th><th>Unidad</th><th>Rol</th><th>HP</th><th>Daño</th><th>Alcance</th><th>Vel.</th><th>Costo</th>
</tr>
<tr>
<td align="center"><img src="docs/img/pawn.png" height="72"></td>
<td><b>Pawn</b></td><td>Recolecta y construye</td><td>60</td><td>5</td><td>0.5</td><td>3.0</td><td>50 oro</td>
</tr>
<tr>
<td align="center"><img src="docs/img/warrior.png" height="72"></td>
<td><b>Guerrero</b></td><td>Melee equilibrado, aguanta el frente</td><td>140</td><td>18</td><td>0.8</td><td>2.6</td><td>90 oro · 10 madera</td>
</tr>
<tr>
<td align="center"><img src="docs/img/lancer.png" height="72"></td>
<td><b>Lancero</b></td><td>Melee con alcance, alto daño</td><td>100</td><td>22</td><td>1.6</td><td>2.8</td><td>80 oro</td>
</tr>
<tr>
<td align="center"><img src="docs/img/archer.png" height="72"></td>
<td><b>Arquero</b></td><td>Daño a distancia, frágil</td><td>70</td><td>14</td><td>5.0</td><td>2.9</td><td>85 oro · 20 madera</td>
</tr>
<tr>
<td align="center"><img src="docs/img/monk.png" height="72"></td>
<td><b>Monje</b></td><td>Soporte, cura aliados</td><td>65</td><td>−20 (cura)</td><td>3.5</td><td>2.7</td><td>120 oro</td>
</tr>
</table>

Todas existen en los cinco colores de facción. Los valores viven en `ScriptableObject`,
no en el código: el balance se ajusta sin recompilar.

### Triángulo de contadores

```
            ┌───────── vence a ─────────▶ Guerrero
            │                                 │
         Lancero                          vence a
            ▲                                 │
            │                                 ▼
            └───────── vence a ────────── Arquero
```

| Enfrentamiento | Por qué |
|---|---|
| **Lancero > Guerrero** | Su alcance mayor le deja pegar primero |
| **Guerrero > Arquero** | Resiste el camino y lo aplasta de cerca |
| **Arquero > Lancero** | Lo castiga desde fuera de su alcance |

El **monje** queda fuera del triángulo: no gana ningún duelo, pero multiplica el valor de
cualquier composición. Y muere solo si lo dejas sin escolta.

### Edificios

<table>
<tr>
<td align="center" width="130"><img src="docs/img/castle.png" height="86"><br><b>Castillo</b></td>
<td>Centro de entrega de recursos y productor de pawns. Aporta 5 de población.<br><b>Si cae, pierdes.</b></td>
</tr>
<tr>
<td align="center"><img src="docs/img/house.png" height="86"><br><b>Casa</b></td>
<td><b>60 madera</b> · +5 al límite de población</td>
</tr>
<tr>
<td align="center"><img src="docs/img/barracks.png" height="86"><br><b>Cuartel</b></td>
<td><b>120 madera · 50 oro</b> · Entrena guerreros y lanceros</td>
</tr>
<tr>
<td align="center"><img src="docs/img/archery.png" height="86"><br><b>Campo de tiro</b></td>
<td><b>100 madera · 70 oro</b> · Entrena arqueros</td>
</tr>
<tr>
<td align="center"><img src="docs/img/monastery.png" height="86"><br><b>Monasterio</b></td>
<td><b>150 madera · 100 oro</b> · Entrena monjes</td>
</tr>
<tr>
<td align="center"><img src="docs/img/tower.png" height="86"><br><b>Torre</b></td>
<td><b>80 madera · 40 oro</b> · Defensa estática con ataque a distancia</td>
</tr>
</table>

**Población:** empiezas con 5 y cada casa suma 5, hasta un **tope duro de 50 unidades**.
Ese tope no es una decisión de diseño arbitraria: es el presupuesto de rendimiento del motor
(5 bandos × 50 = 250 unidades simultáneas en pantalla).

### Niebla de guerra

El mapa tiene tres estados por cada bando:

| Estado | Qué ves |
|---|---|
| ⬛ **Sin explorar** | Negro total |
| 🌫️ **Explorado, sin visión** | El terreno y los edificios que recuerdas. Las unidades enemigas, no |
| 👁️ **Visible** | Todo, en tiempo real |

Cada unidad y edificio aporta su propio radio de visión. Explorar cuesta unidades, y las
unidades exploradoras son unidades que no están recolectando ni peleando.

---

## La IA rival

Tres capas que piensan a ritmos distintos — el mismo enfoque que usaban Warcraft y
Age of Empires para correr siete bots en hardware de los 90:

| Capa | Qué decide | Frecuencia |
|---|---|---|
| **Estratégica** | Orden de construcción, cuándo expandir, cuándo atacar | ~1 Hz |
| **Táctica** | Formar oleadas, elegir objetivo, cuándo retirarse | ~3 Hz |
| **Por unidad** | Máquina de estados: idle · mover · atacar · recolectar · huir | Por frame |

La IA **no tiene atajos**: emite exactamente las mismas órdenes que el jugador, a través del
mismo sistema. Si la IA puede hacer algo, tú también, y viceversa.

### Dificultades

| | Bonus de recursos | Tamaño de oleada | Intervalo de ataque |
|---|---|---|---|
| 🟢 **Fácil** | ×1.0 | 4 unidades | 150 s |
| 🟡 **Normal** | ×1.25 | 8 unidades | 100 s |
| 🔴 **Difícil** | ×1.6 | 14 unidades | 70 s |

> La dificultad **no es inteligencia: es economía y tempo.** Es como lo resolvían los RTS
> clásicos, y sigue siendo la forma honesta de hacerlo: una IA que "piensa mejor" es un
> problema de investigación abierto; tres multiplicadores bien calibrados producen rivales
> que se sienten genuinamente distintos.

---

## Mapas

| Mapa | Bandos | Carácter |
|---|---|---|
| 🏝️ **Isla central** | 5 | Los mejores recursos están al centro y son de todos. Conflicto temprano garantizado |
| ⛰️ **Cuencas separadas** | 5 | Cada bando con su valle y pasos estrechos. Partidas largas y defensivas |
| 🌾 **Llanura abierta** | 3 | Poca cobertura, expansión rápida. Agresivo y corto |

---

## Controles

| Acción | Control |
|---|---|
| Seleccionar unidad | `Clic izquierdo` |
| Selección múltiple | `Arrastrar` con clic izquierdo |
| Sumar a la selección | `Shift` + clic / arrastre |
| Mover · atacar · recolectar | `Clic derecho` *(orden contextual)* |
| Mover cámara | `W` `A` `S` `D` o empujar el borde de la pantalla |
| Zoom | `Rueda del mouse` |
| Asignar grupo de control | `Ctrl` + `1`…`9` |
| Llamar grupo de control | `1`…`9` |
| Pausa | `Esc` |

**Orden contextual:** un solo botón hace lo correcto según lo que haya debajo del cursor —
suelo vacío = mover · enemigo = atacar · árbol, oro u oveja = recolectar · obra propia = ayudar a construir.

---

## Arquitectura técnica

```
┌──────────────────────────────────────────────────────┐
│  PRESENTACIÓN                                        │
│  Cámara · HUD · Minimapa · Menús · Animación         │
└────────────────────────┬─────────────────────────────┘
                         │ lee estado
┌────────────────────────▼─────────────────────────────┐
│  ENTRADA                                             │
│  Selección · Órdenes contextuales                    │
└────────────────────────┬─────────────────────────────┘
                         │ emite ÓRDENES (serializables)
┌────────────────────────▼─────────────────────────────┐
│  AUTORIDAD DE SIMULACIÓN                             │
│  Valida y aplica órdenes · Tick a 20 Hz              │
└────────┬─────────────────────────────┬───────────────┘
         │                             │
┌────────▼──────────────┐  ┌───────────▼───────────────┐
│  MUNDO                │  │  IA                       │
│  Grilla · Unidades    │  │  Estratégica (1 Hz)       │
│  Edificios · Recursos │  │  Táctica (3 Hz)           │
│  Niebla · Facciones   │  │  ↑ emite las mismas ÓRDENES│
└───────────────────────┘  └───────────────────────────┘
```

**La idea que sostiene todo:** ni el jugador ni la IA mueven unidades directamente. Ambos
emiten **órdenes** que la autoridad valida y aplica. Una unidad nunca lee el input.

Eso es el patrón **Command** de manual, y trae tres cosas gratis: la IA y el jugador comparten
un único camino de código, las partidas son reproducibles, y el día que se quiera añadir
multijugador solo hay que transportar órdenes por la red en vez de reescribir el núcleo.

### Decisiones que definen el motor

| Decisión | Motivo |
|---|---|
| **A\* sobre grilla** + interpolación suave + empuje blando | Se ve como un RTS moderno, cuesta como una grilla. Determinista y depurable. Descartados NavMesh y RVO |
| **Sin `Animator`** — animación por sprite-swap propio | El `Animator` de Unity es ~0.1 ms por instancia: con 250 unidades es el primer cuello de botella del juego |
| **Grilla espacial** para buscar objetivos | La búsqueda ingenua es O(n²): 62 500 comparaciones por frame con 250 unidades |
| **Cola de pathfinding** — máx. 8 rutas por frame | Ordenar mover a 50 unidades dispararía 50 A\* en el mismo frame y produciría un tirón visible |
| **Sin `Rigidbody2D`** en unidades | Introduce no-determinismo. La física se resuelve por script: trayectorias de proyectiles y empuje entre unidades |
| **Balance en `ScriptableObject`** | Ajustar un número no debe costar una recompilación |

📄 Cada decisión está documentada con sus alternativas descartadas en
**[`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)**.

### Presupuesto de rendimiento

| Métrica | Objetivo |
|---|---|
| Unidades simultáneas | **250** (5 bandos × 50) |
| Framerate en combate pleno | **≥ 60 fps** |
| Tick de simulación | 20 Hz, desacoplado del framerate |
| Rutas A\* por frame | ≤ 8 |

Son **criterios de aceptación** verificados con el profiler, no aspiraciones.

### Estructura del proyecto

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
└── Tiny Swords/         Pack original — no se modifica
```

---

## Cómo ejecutar el proyecto

**Requisito:** Unity **6000.5.8f1** exactamente. Instalar desde Unity Hub.

```bash
git clone https://github.com/LOAD-13/tinyTactics.git
```

1. Abrir Unity Hub → **Add** → seleccionar la carpeta `tinyTactics`.
2. Abrir el proyecto y esperar la primera importación.
3. Cargar `Assets/Scenes/Juego.unity` y pulsar **Play**.

> ⏳ La primera apertura tarda varios minutos: Unity reconstruye la carpeta `Library/`,
> que no está versionada a propósito. Es normal y solo pasa una vez.

### Para revisar una entrega concreta

Cada entrega semanal tiene su **tag**, así que el estado exacto del proyecto en cualquier
fecha es reproducible:

```bash
git checkout v0.5.0-s05     # el proyecto tal como estaba en la semana 5
```

---

## Desarrollo

### Flujo de trabajo

```
main                    ← solo entregas, una por semana, cada una con su tag
└── develop             ← integración; siempre abre en Unity sin errores
    ├── feat/HU-012-seleccion-caja
    ├── fix/HU-020-pawn-atascado-arbol
    └── docs/HU-001-documentacion-base
```

- **Ramas:** `<tipo>/HU-<id>-<slug>` — una por historia de usuario
- **Commits:** [Conventional Commits](https://www.conventionalcommits.org/), con el ID de la HU al pie
- **Entregas:** tag `v0.<semana>.0-s<semana>` sobre `main`

📄 Detalle completo en **[`docs/GITFLOW.md`](docs/GITFLOW.md)**.

### Hoja de ruta de versiones

| Tag | Entrega |
|---|---|
| `v0.2.0-s02` | Fundación, mapa base y cámara RTS |
| `v0.3.0-s03` | Selección y movimiento de unidades |
| `v0.4.0-s04` | Animación y máquina de estados |
| `v0.5.0-s05` | 🏁 **Hito 1** — Economía y recolección |
| `v0.6.0-s06` | Construcción y población |
| `v0.7.0-s07` | Combate cuerpo a cuerpo |
| `v0.8.0-s08` | Combate a distancia y curación |
| `v0.9.0-s09` | Niebla de guerra y minimapa |
| `v0.10.0-s10` | 🏁 **Hito 2** — IA rival |
| `v0.11.0-s11` | Dificultades de IA |
| `v0.12.0-s12` | Flujo de partida completo |
| `v0.13.0-s13` | Todos contra todos y rendimiento |
| `v0.14.0-s14` | Audio y efectos |
| `v0.15.0-s15` | 🏁 **Hito 3** — Interfaz final |
| `v0.16.0-s16` | Balance y QA |
| `v1.0.0-s17` | 📦 **Versión final** |

---

## Documentación

| Documento | Contenido |
|---|---|
| **[`docs/GDD.md`](docs/GDD.md)** | Diseño del juego: reglas, economía, unidades, edificios, mapas |
| **[`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)** | Decisiones técnicas, alternativas descartadas y por qué |
| **[`docs/CRONOGRAMA.md`](docs/CRONOGRAMA.md)** | Plan de desarrollo de 18 semanas |
| **[`docs/BACKLOG.md`](docs/BACKLOG.md)** | Épicas, historias de usuario y criterios de aceptación |
| **[`docs/GITFLOW.md`](docs/GITFLOW.md)** | Convención de ramas, commits y entregas |
| **[`docs/BITACORA.md`](docs/BITACORA.md)** | Registro semanal de lo que realmente se hizo |

---

## Equipo

<table>
<tr>
<td align="center" width="200"><b>Joaquín Alfonso<br>Loa Denegri</b><br><sub>U22234069</sub><br><br>Desarrollo<br>y arquitectura</td>
<td align="center" width="200"><b>Kiara Mishell<br>Santti Saavedra</b><br><sub>U22201636</sub><br><br>Diseño de juego,<br>arte y documentación</td>
<td align="center" width="200"><b>Gerardo Raúl<br>Socualaya Mandamiento</b><br><sub>U22229068</sub><br><br>QA, balance<br>y pruebas</td>
</tr>
</table>

**Curso:** Diseño y Desarrollo de Juegos Interactivos II · 100000S11F · Ciclo 2026-2 Agosto
**Docente:** Omar David Machuca Ñuflo
**Universidad:** UTP

---

## Créditos

Todo el arte del proyecto proviene de **[Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords)**,
de **[Pixel Frog](https://pixelfrog-assets.itch.io/)** — publicado gratuitamente y con uso
comercial permitido. No se emplean recursos de terceros sin declarar.

El logotipo de *Tiny Tactics* es una composición propia sobre elementos del mismo pack.

<div align="center">
<br>
<sub>Hecho con ⚔️ y demasiadas horas de Unity.</sub>
</div>
