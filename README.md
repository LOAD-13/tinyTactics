<div align="center">

<img src="docs/img/logo.png" alt="Tiny Tactics" width="560">

### Recolecta. Construye. Conquista.

**Un RTS de estrategia en tiempo real en pixel art, para hasta 5 bandos.**

<br>

![Unity](https://img.shields.io/badge/Unity-6000.5.8f1-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-.NET-512BD4?style=for-the-badge&logo=csharp&logoColor=white)
![URP](https://img.shields.io/badge/Render-2D%20URP-1a7f5a?style=for-the-badge)
![Windows](https://img.shields.io/badge/Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![Linux](https://img.shields.io/badge/Linux-FCC624?style=for-the-badge&logo=linux&logoColor=black)

<br>

**[⬇️ Descargar la última versión](https://github.com/LOAD-13/tinyTactics/releases/latest)** · **[🎬 Trailer TinyTactics 1.0](https://utpedupe-my.sharepoint.com/:v:/g/personal/u22201636_utp_edu_pe/IQAvCohMUD_nTI856ytjaqsUAcxYS8IpSLYU2vG1s8ENVII?e=tQ7FfW)**

<br>

<a href="https://utpedupe-my.sharepoint.com/:v:/g/personal/u22201636_utp_edu_pe/IQAvCohMUD_nTI856ytjaqsUAcxYS8IpSLYU2vG1s8ENVII?e=tQ7FfW"><img src="docs/img/capturas/trailer.jpg" alt="Fotogramas del tráiler de Tiny Tactics 1.0" width="860"></a>

<sub>Fotogramas del tráiler. Haz clic para verlo.</sub>

<br><br>

<img src="docs/img/faccion_blue.png" height="72">
<img src="docs/img/faccion_red.png" height="72">
<img src="docs/img/faccion_yellow.png" height="72">
<img src="docs/img/faccion_purple.png" height="72">
<img src="docs/img/faccion_black.png" height="72">

</div>

---

## Índice

- [El juego](#el-juego)
- [Descargar y jugar](#descargar-y-jugar)
- [Características](#características)
- [Cómo se juega](#cómo-se-juega)
- [La IA rival](#la-ia-rival)
- [Mapas y editor](#mapas-y-editor)
- [Historial de partidas](#historial-de-partidas)
- [Controles](#controles)
- [Arquitectura técnica](#arquitectura-técnica)
- [Pruebas](#pruebas)
- [Abrir el proyecto en Unity](#abrir-el-proyecto-en-unity)
- [Desarrollo](#desarrollo)
- [Documentación](#documentación)
- [Equipo](#equipo)
- [Créditos](#créditos)

---

## El juego

Empiezas con un castillo y unos pocos pawns en tu rincón del mapa. En algún lugar, oculto
tras la niebla, hay hasta cuatro rivales haciendo exactamente lo mismo.

**Tiny Tactics** es un RTS clásico de vista cenital: mandas a tus pawns a talar, picar oro y
cazar ovejas; conviertes esos recursos en casas que amplían tu población, en cuarteles que
producen soldados y en torres que protegen tu territorio. Y cuando crees que tienes ventaja,
atacas, porque el que espera demasiado pierde.

> **Cada pawn que mandas a recolectar es un soldado que no entrenaste.**

Invertir en economía te da un ejército más grande *después*; invertir en tropas te da presión
*ahora*. La niebla impide saber cuál de las dos eligió tu rival, y para cuando lo descubres,
normalmente ya es tarde.

<div align="center">
<img src="docs/img/capturas/aldea.jpg" alt="Una base en marcha" width="49%">
<img src="docs/img/capturas/noche.jpg" alt="La noche en Lago Helado" width="49%">
</div>

---

## Descargar y jugar

En **[Releases](https://github.com/LOAD-13/tinyTactics/releases/latest)** están los ejecutables
listos para jugar, sin instalar nada:

| Sistema | Archivo | Cómo se abre |
|---|---|---|
| **Windows 10/11** (64 bits) | `TinyTactics-v1.0-Windows.zip` | Descomprimir y abrir `Tiny Tactics.exe` |
| **Linux** (x86_64) | `TinyTactics-v1.0-Linux.zip` | Descomprimir, `chmod +x TinyTactics.x86_64` y `./TinyTactics.x86_64` |

> Windows puede avisar de que el ejecutable no está firmado: **Más información → Ejecutar de
> todas formas.** Las partidas y los mapas propios se guardan en la carpeta de datos del
> usuario, no junto al ejecutable.

**Para empezar:** la espada **Tutorial** del menú. El capataz te enseña lo básico en seis pasos.
Después, **Jugar** contra uno a cuatro bots de la dificultad que quieras.

---

## Características

| | |
|---|---|
| 🏰 **Economía de tres recursos** | Oro para tropas, madera para edificios, y **carne como sustento**: tu ejército come |
| ⚔️ **Cinco unidades con contadores reales** | Un triángulo donde ninguna composición pura gana |
| 🏗️ **Seis edificios y población** | Cada casa amplía tu límite de tropas hasta un tope de 50 |
| 🌫️ **Niebla de guerra y ciclo de día** | Visibilidad por bando, terreno recordado y noche que acorta la vista |
| 🤖 **IA rival en tres dificultades** | Reparte su economía, responde a lo que ve, defiende agrupada, se retira y hostiga |
| 👥 **Todos contra todos** | De 2 a 5 bandos, sin alianzas |
| 🗺️ **Tres mapas y un editor** | Crea tus propios mapas y juégalos en el sandbox |
| 📜 **Historial de partidas** | Marcador, evolución y mapa de cada partida que juegas |
| 🎓 **Tutorial guiado** | Un pawn capataz que habla y enseña a jugar |
| 🔊 **Música y sonido** | Música que pasa a combate cuando hay pelea, ambiente de día y de noche |

---

## Cómo se juega

```
   Recolectar  ──▶  Construir  ──▶  Subir población
        ▲                │                  │
        │                ▼                  ▼
        └────────── Defender  ◀──  Entrenar tropas ──▶ Atacar
```

**Ganas** cuando eres el último bando con castillo en pie. **Pierdes** cuando cae el tuyo.

### Recursos

<table>
<tr>
<td align="center" width="140"><img src="docs/img/gold.png" height="64"><br><b>Oro</b></td>
<td>Se pica de las minas. Es la moneda del ejército: <b>todas</b> las unidades cuestan oro.</td>
</tr>
<tr>
<td align="center"><img src="docs/img/wood.png" height="64"><br><b>Madera</b></td>
<td>Se tala de los árboles. Es lo que construye: casas, cuarteles, torres.</td>
</tr>
<tr>
<td align="center"><img src="docs/img/meat.png" height="64"><br><b>Carne</b></td>
<td>Se obtiene cazando ovejas. <b>Es sustento, no moneda:</b> cada unidad viva la consume. Si llega a cero, tus tropas pierden daño y velocidad hasta que la repongas.</td>
</tr>
</table>

### Unidades

<table>
<tr>
<th width="110">&nbsp;</th><th>Unidad</th><th>Rol</th><th>Vida</th><th>Daño</th><th>Alcance</th><th>Vel.</th><th>Coste</th>
</tr>
<tr>
<td align="center"><img src="docs/img/pawn.png" height="72"></td>
<td><b>Pawn</b></td><td>Recolecta y construye</td><td>60</td><td>5</td><td>0.5</td><td>3.0</td><td>50 oro</td>
</tr>
<tr>
<td align="center"><img src="docs/img/warrior.png" height="72"></td>
<td><b>Guerrero</b></td><td>Melee pesado, aguanta el frente</td><td>140</td><td>18</td><td>0.8</td><td>2.6</td><td>90 oro · 10 madera</td>
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

Los valores viven en `ScriptableObject`, no en el código: el balance se ajusta sin recompilar.

### Triángulo de contadores

| Enfrentamiento | Por qué |
|---|---|
| **Lancero > Guerrero** | Su alcance le deja pegar primero |
| **Guerrero > Arquero** | Resiste el camino y lo aplasta de cerca |
| **Arquero > Lancero** | Lo castiga desde fuera de su alcance |

El **monje** queda fuera del triángulo: no gana ningún duelo, pero multiplica el valor de
cualquier composición. Cada unidad tiene además tres **posturas**: agresiva, defensiva y quieta.

### Edificios

<table>
<tr>
<td align="center" width="130"><img src="docs/img/castle.png" height="86"><br><b>Castillo</b></td>
<td><b>250 madera · 300 oro</b> · Recibe los recursos y entrena pawns. +10 de población. <b>Si cae el último, pierdes.</b></td>
</tr>
<tr>
<td align="center"><img src="docs/img/house.png" height="86"><br><b>Casa</b></td>
<td><b>60 madera</b> · +5 de población</td>
</tr>
<tr>
<td align="center"><img src="docs/img/barracks.png" height="86"><br><b>Cuartel</b></td>
<td><b>100 madera · 40 oro</b> · Guerreros y lanceros</td>
</tr>
<tr>
<td align="center"><img src="docs/img/archery.png" height="86"><br><b>Campo de tiro</b></td>
<td><b>90 madera · 40 oro</b> · Arqueros</td>
</tr>
<tr>
<td align="center"><img src="docs/img/monastery.png" height="86"><br><b>Monasterio</b></td>
<td><b>80 madera · 80 oro</b> · Monjes</td>
</tr>
<tr>
<td align="center"><img src="docs/img/tower.png" height="86"><br><b>Torre</b></td>
<td><b>70 madera · 30 oro</b> · Defensa con ataque a distancia</td>
</tr>
</table>

**El tope de 50 unidades por bando** no es arbitrario: es el presupuesto de rendimiento del
motor (5 bandos × 50 = 250 unidades a la vez).

### Niebla de guerra

| Estado | Qué ves |
|---|---|
| ⬛ **Sin explorar** | Negro |
| 🌫️ **Explorado, sin visión** | El terreno y los edificios que recuerdas. Las unidades enemigas, no |
| 👁️ **Visible** | Todo, en tiempo real |

La IA respeta la niebla en las tres dificultades: no ve lo que tú no verías.

---

## La IA rival

Tres capas que piensan a ritmos distintos:

| Capa | Qué decide |
|---|---|
| **Económica** | Qué construir, cuántos pawns a cada recurso según lo que le falta, qué tropa entrenar |
| **Militar** | Cuándo atacar, a quién, cómo defender, cuándo retirarse |
| **Por unidad** | Máquina de estados: quieta · mover · atacar · recolectar · construir · huir |

**La IA no hace trampas:** emite las mismas órdenes que el jugador, por el mismo camino.

**Lo que sabe hacer:**
- Repartir los pawns según sus reservas, no con una proporción fija.
- **Responder a lo que ve:** si te ve con lanceros, entrena arqueros.
- Elegir objetivo con cabeza: los cuarteles y campos de tiro antes que las casas.
- Defender agrupada, dejar guarnición, mandar refuerzos en grupo y retirarse si pierde.
- Hostigar tus minas con incursiones pequeñas.

### Dificultades

| | Recursos | Tamaño de oleada | Primer ataque | Incursiones |
|---|---|---|---|---|
| 🟢 **Fácil** | ×0.75 | ×0.7 | a los 240 s | cada 240 s |
| 🟡 **Normal** | ×1.0 | ×1.0 | a los 60 s | cada 150 s |
| 🔴 **Difícil** | ×1.35 | ×1.3 | a los 30 s | cada 90 s |

> La dificultad **no es inteligencia: es economía y ritmo.** Las tres usan el mismo cerebro.
> Calibrado con partidas automáticas: un bot Normal gana el 90 % a Fácil, el 40 % a Normal y
> el 10 % a Difícil.

---

## Mapas y editor

| Mapa | Bandos | Carácter |
|---|---|---|
| 👑 **Tres Coronas** | 3 | Tres lóbulos separados por brazos de mar, unidos por el centro |
| 🌬️ **Cuatro Vientos** | 4 | Bases en molinete alrededor de una isla con foso. El centro manda |
| ❄️ **Lago Helado** | 2 | Duelo con un lago helado y tres islas de bosque en el centro |

Con el **editor de mapas** se pinta terreno, relieve, bosques, minas, ovejas y bases (de 2 a 5),
y se prueba al momento. Los mapas propios aparecen en el **Sandbox**, donde también se pueden
cambiar las reglas: recursos infinitos, niebla, velocidad de la partida.

<div align="center">
<img src="docs/img/capturas/editor.jpg" alt="Editor de mapas" width="49%">
<img src="docs/img/capturas/sandbox.jpg" alt="Sandbox" width="49%">
</div>

---

## Historial de partidas

Cada partida que juegas queda guardada. En la espada **Historial** del menú ves tus últimas 20
partidas, tu porcentaje de victorias, tu racha y cómo te va contra cada dificultad. Al abrir una:
el marcador de todos los bandos, una gráfica de población o de recursos minuto a minuto y el mapa.

<div align="center">
<img src="docs/img/capturas/historial.jpg" alt="Historial de partidas" width="49%">
<img src="docs/img/capturas/evolucion.jpg" alt="Evolución de una partida" width="49%">
</div>

---

## Controles

| Acción | Control |
|---|---|
| Seleccionar · seleccionar en caja | `Clic izquierdo` · arrastrar |
| Añadir a la selección | `Shift` |
| Mover · atacar · recolectar | `Clic derecho` *(orden contextual)* |
| Atacar · detener | `A` · `S` |
| Atacar lo que encuentre por el camino | `T` |
| Mover · construir | `V` · `B` |
| Cambiar postura | `X` |
| Entrenar pawn (castillo) | `P` |
| Guardar · recuperar un grupo | `Ctrl` + `1`…`9` · `1`…`9` |
| Volver al castillo | `Espacio` |
| Minimapa grande | `M` |
| Mover la cámara | Flechas o ratón en el borde |
| Zoom | Rueda del ratón |
| Cancelar · pausa | `Esc` |

**Orden contextual:** el clic derecho hace lo correcto según lo que haya debajo: suelo = mover,
enemigo = atacar, árbol, mina u oveja = recolectar, obra propia = ayudar a construir.

<div align="center">
<img src="docs/img/capturas/controles.jpg" alt="Pantalla de controles" width="70%">
</div>

---

## Arquitectura técnica

```
┌──────────────────────────────────────────────────────┐
│  PRESENTACIÓN   Cámara · HUD · Minimapa · Menús      │
└────────────────────────┬─────────────────────────────┘
                         │ lee estado
┌────────────────────────▼─────────────────────────────┐
│  ENTRADA        Selección · órdenes contextuales     │
└────────────────────────┬─────────────────────────────┘
                         │ emite ÓRDENES (serializables)
┌────────────────────────▼─────────────────────────────┐
│  SIMULACIÓN     Valida y aplica las órdenes          │
└────────┬─────────────────────────────┬───────────────┘
┌────────▼──────────────┐  ┌───────────▼───────────────┐
│  MUNDO                │  │  IA                       │
│  Grilla · Unidades    │  │  Económica · Militar      │
│  Edificios · Niebla   │  │  ↑ emite las mismas ÓRDENES│
└───────────────────────┘  └───────────────────────────┘
```

Ni el jugador ni la IA mueven unidades directamente: los dos emiten **órdenes** (patrón
Command). La IA y el jugador comparten un único camino de código y el núcleo queda listo para
transportar órdenes por red algún día.

| Decisión | Motivo |
|---|---|
| **A\* sobre grilla** + interpolación y empuje blando | Se ve como un RTS moderno y cuesta como una grilla. Descartados NavMesh y RVO |
| **Sin `Animator`**: animación por cambio de sprite propio | El `Animator` es el mayor coste por unidad cuando hay 250 |
| **Grilla espacial** para buscar objetivos | La búsqueda ingenua es O(n²) |
| **Cola de rutas** | Ordenar mover a 50 unidades no lanza 50 A\* en el mismo fotograma |
| **El mapa es un JSON** | Los oficiales y los del editor se cargan igual |
| **Balance en `ScriptableObject`** | Cambiar un número no cuesta una recompilación |

**Rendimiento medido en la build:** 289 unidades peleando a **170 FPS de media** (1 % más lento: 94).

📄 Cada decisión, con sus alternativas descartadas, en **[`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)**.

### Estructura

```
Assets/
├── Scripts/
│   ├── Nucleo/       Órdenes, población, economía, árbitro, informe e historial
│   ├── Mundo/        Grilla, mapa, niebla, ciclo del día, ovejas
│   ├── Unidades/     Máquina de estados, animación, recolección
│   ├── Movimiento/   A*, cola de rutas
│   ├── Edificios/    Colocación, obras, producción, torres
│   ├── Combate/      Efectos, fuego, avisos de ataque
│   ├── IA/           Cerebro, mente económica y militar, plan y dificultades
│   ├── Entrada/      Cámara, selección, grupos
│   ├── Interfaz/     Menú, HUD, minimapa, editor de mapas, tutorial, historial
│   ├── Audio/        Música, sonidos y mezclador
│   ├── Pruebas/      Panel de pruebas y medidor de rendimiento
│   └── Editor/       Herramientas de editor y construcción del ejecutable
├── Datos/            Instancias de ScriptableObject (balance, IA, sonidos)
├── StreamingAssets/  Mapas oficiales y prado del tutorial
└── Tiny Swords/      Pack original, sin modificar
tools/                Pruebas y compilación sin abrir Unity
```

---

## Pruebas

**Sin abrir Unity**, con Python 3 y el Unity instalado:

```bash
python tools/compilar.py    # compila el juego con el mismo compilador que Unity
python tools/probar.py      # 96 bloques y más de 48 000 comprobaciones de lógica pura
```

**El juego se prueba a sí mismo.** El ejecutable juega partidas solo, sin ventana y unas once
veces más rápido que en tiempo real, y guarda el informe de cada una:

```bash
"Tiny Tactics.exe" -batchmode -nographics -autopartidas 20 -mapa "Lago Helado" \
    -rivales Normal -yo Normal -sinjugador -alternar -historial resultados
python tools/comparar_reparto.py resultados
```

Así se calibró la IA y se encontró una ventaja injusta de una base en Lago Helado (71 % de
victorias), ya corregida. El plan completo está en **[`docs/PLAN_DE_PRUEBAS.md`](docs/PLAN_DE_PRUEBAS.md)**.

---

## Abrir el proyecto en Unity

**Requisito:** Unity **6000.5.8f1** (con *Linux Build Support* si se quiere construir para Linux).

```bash
git clone https://github.com/LOAD-13/tinyTactics.git
```

1. Unity Hub → **Add** → la carpeta del repositorio.
2. Esperar la primera importación (varios minutos: se reconstruye `Library/`).
3. Abrir `Assets/Scenes/Inicio.unity` y pulsar **Play**.

**Construir el ejecutable sin abrir el editor:**

```bash
Unity -batchmode -quit -projectPath . \
  -executeMethod TinyTactics.EditorHerramientas.ConstruirEjecutable.Windows \
  [-linux] -salida "build/Tiny Tactics.exe"
```

Cada entrega semanal tiene su tag, así que el proyecto de cualquier semana es reproducible:

```bash
git checkout v0.10.0-s10     # el proyecto tal como estaba en la semana 10
```

---

## Desarrollo

```
main        ← solo entregas, una por semana, cada una con su tag
└── develop ← integración; siempre abre en Unity sin errores
    └── feat/E11-entrega-final
```

Ramas por épica o historia, commits con [Conventional Commits](https://www.conventionalcommits.org/)
y un tag `v0.<semana>.0-s<semana>` por entrega. Detalle en **[`docs/GITFLOW.md`](docs/GITFLOW.md)**.

### Versiones

| Tag | Entrega |
|---|---|
| `v0.2.0-s02` | Fundación, mapa base y cámara RTS |
| `v0.3.0-s03` | Núcleo de simulación y desniveles |
| `v0.4.0-s04` | Unidades y animación |
| `v0.5.0-s05` | 🏁 **Hito 1**: economía y recolección |
| `v0.6.0-s06` | Construcción y producción |
| `v0.7.0-s07` | Combate: el conflicto |
| `v0.8.0-s08` | Combate: el triángulo de contadores |
| `v0.9.0-s09` | Niebla de guerra, minimapa y ciclo del día |
| `v0.10.0-s10` | 🏁 **Hito 2**: la IA rival |
| `v0.11.0-s11` | IA v2, dificultades y primer audio |
| `v0.12.0-s12` | Menú, flujo de partida y sandbox |
| `v0.13.0-s13` | Editor de mapas |
| `v0.14.0-s14` | Audio, efectos y todos contra todos |
| `v0.15.0-s15` | 🏁 **Hito 3**: interfaz final, tutorial y tercer mapa |
| `v0.16.0-s16` | Balance, pruebas automáticas e historial de partidas |
| `v0.17.0-s17` | Entrega final: IA que responde, ejecutables para Windows y Linux |
| `v1.0.0` | 📦 **Versión 1.0**, publicada en Releases |

---

## Documentación

| Documento | Contenido |
|---|---|
| **[`docs/GDD.md`](docs/GDD.md)** | Diseño del juego: reglas, economía, unidades, edificios, mapas |
| **[`docs/ARQUITECTURA.md`](docs/ARQUITECTURA.md)** | Decisiones técnicas (ADR), alternativas descartadas y por qué |
| **[`docs/CRONOGRAMA.md`](docs/CRONOGRAMA.md)** | Plan de desarrollo de 18 semanas |
| **[`docs/BACKLOG.md`](docs/BACKLOG.md)** | Épicas, historias de usuario y criterios de aceptación |
| **[`docs/BITACORA.md`](docs/BITACORA.md)** | Lo que realmente pasó cada semana |
| **[`docs/PLAN_DE_PRUEBAS.md`](docs/PLAN_DE_PRUEBAS.md)** | Pruebas automáticas, casos manuales, IA y rendimiento |
| **[`docs/SANDBOX.md`](docs/SANDBOX.md)** | Requisitos del sandbox y del editor de mapas |
| **[`docs/GITFLOW.md`](docs/GITFLOW.md)** | Ramas, commits y entregas |

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
**Universidad:** Universidad Tecnológica del Perú (UTP)

---

## Créditos

- **Arte:** [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords), de [Pixel Frog](https://pixelfrog-assets.itch.io/), gratuito y con uso comercial permitido.
- **Tipografía:** MedievalSharp (SIL Open Font License).
- **Música y sonidos:** recursos CC0 (dominio público).
- El logotipo de *Tiny Tactics* es una composición propia sobre elementos del mismo pack.

<div align="center">
<br>
<sub>Hecho con ⚔️ y demasiadas horas de Unity.</sub>
</div>
