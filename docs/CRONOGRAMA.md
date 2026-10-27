# Cronograma Tiny Tactics — 18 semanas

**Principio rector:** cada semana cierra con **algo que corre y se puede mostrar**.
Cero semanas de trabajo invisible.

Este es un cronograma **propio**, construido sobre el desarrollo real del juego.
El sílabo y el cronograma del docente son **insumo, no índice**: cuando un tema no aplica
a un RTS cenital, se reemplaza y se justifica (ver §3). El docente autorizó esto por escrito:

> *"El cronograma no significa que todos los temas indicados deban implementarse obligatoriamente
> como funcionalidades del videojuego. Algunos, como Networking o adaptación a móviles, se aplicarán
> de acuerdo con el alcance y características de cada proyecto."*

---

## 1. Calendario

Entrega **domingo**, exposición **lunes**. Expone un solo integrante, rotando. Desde la semana 14
exponen solo Kiara y Joaquín, intercalando.

| Sem | Entrega | Expo | Qué se demuestra | Expone |
|---|---|---|---|---|
| **02** | 16/08 | 17/08 | **Concepto + mapa navegable.** Ficha conceptual. Repo con GitFlow y `docs/`. Escena con tilemap pintado y **cámara RTS** (paneo por borde/WASD, zoom). | Joaquín |
| **03** | 23/08 | 24/08 | **Selección y movimiento.** Grilla lógica, A*, interpolación, empuje blando. Clic, caja de arrastre, shift. Orden con clic derecho. | Kiara |
| **04** | 30/08 | 31/08 | **Animación y estados.** FSM por unidad (idle/mover/atacar/morir), animador por sprite-swap, las 4 unidades en los 5 colores. | Raúl |
| **05** | 06/09 | 07/09 | **🏁 HITO 1 · PC1 — Economía.** Pawns recolectan oro y madera, vuelven al castillo, HUD con contadores. | Joaquín |
| **06** | 13/09 | 14/09 | **Construcción y población.** Colocar casa/cuartel con costo, límite de población, entrenar pawns. Patrones documentados. | Kiara |
| **07** | 20/09 | 21/09 | **Combate cuerpo a cuerpo.** Guerrero y lancero: HP, daño, barras de vida, muerte. Targeting con grilla espacial. | Raúl |
| **08** | 27/09 | 28/09 | **Combate a distancia.** Arquero con proyectiles, monje que cura, triángulo de contadores. Cuartel/campo de tiro/monasterio. | Joaquín |
| **09** | 04/10 | 05/10 | **Niebla de guerra + minimapa + hora del día.** Visibilidad por facción, minimapa con unidades, ciclo de día y noche. | Kiara |
| **10** | 11/10 | 12/10 | **🏁 HITO 2 · PC2 — IA rival v1.** Capa estratégica + táctica. El bot recolecta, construye, entrena, ataca **y defiende su base**. Primera partida completa vs bot. Grupos de control. | Joaquín |
| **11** | 18/10 | 19/10 | **IA v2 + ⚖️ COMPUERTA PvP + primer audio.** 3 dificultades, la IA respeta la niebla en las tres, decisión go/no-go del PvP documentada (**NO-GO**). Efectos de sonido de combate, economía, construcción e interfaz. | Raúl |
| **12** | 25/10 | 26/10 | **Partida completa + empieza la UI.** Pantalla de inicio, menú principal y pantalla de partida libre (mapa, nº de bandos, dificultad de cada rival). El panel de pruebas deja de verse en partida normal: solo vive en el modo sandbox o tras un atajo oculto. | Kiara |
| **13** | 01/11 | 02/11 | **Editor de mapas y el mapa como dato.** Editor completo dentro del juego (terreno en cinco colores, relieve de varios niveles, escaleras, naturaleza, criaderos, puntos de inicio, ambiente), guardar y cargar, y segundo mapa (4 jugadores) dibujado con él. | Joaquín |
| **14** | 08/11 | 09/11 | **Audio, feedback y FFA.** Música con transición a combate, sonido ambiente de día y de noche, mezclador con volumen por canal, partículas, aviso de ataque. **Más el FFA que pasa de la 13:** cada bot elige rival y prueba de carga con más de 250 unidades. Cierra E09. | Kiara |
| **15** | 15/11 | 16/11 | **🏁 HITO 3 · PC3 — UI/UX.** Fichas emergentes, mensajes en pantalla, pawns ociosos, tutorial guiado y cartel final que escala con la pantalla. Tercer mapa (Lago Helado). Se demuestra el FFA. Cierra E10. | Joaquín |
| **16** | 22/11 | 23/11 | **Balance y QA.** Sesiones de prueba reales, ajuste de números (incluida la carne), plan de pruebas, corrección de bugs. | Kiara |
| **17** | 29/11 | 30/11 | **📦 Entrega final.** Build jugable + documentación técnica consolidada desde la bitácora. | Joaquín |
| **18** | — | 07/12 | **Examen final.** Preparación individual: los tres dominan el proyecto completo. | — |

---

### Nota · semana 14: dos expositores, y el reparto entre la 14 y la 15

> **Decidido el 22/10/2026.** Raúl deja de exponer; desde la 14 exponen Kiara y Joaquín,
> intercalando (14 Kiara, 15 Joaquín, 16 Kiara, 17 Joaquín). Y se ordena lo que quedaba de dos
> épicas que se repartían entre semanas: **E09 se cierra en la 14** con el FFA; de **E10**, el
> audio y los efectos se cierran en la 14 y la UI final en la 15. La 15 ya no tiene trabajo de
> FFA: solo lo demuestra. Los efectos visuales salen de la 16, que queda solo para balance y QA.

### Nota · semana 13: el editor de mapas primero, el FFA a la 14

> **Decidido el 22/10/2026.** Los mapas 2 y 3 se dibujan con el editor de mapas en vez de
> generarse, así que el editor tenía que existir antes que ellos. Es la tarea más grande del
> proyecto —convertir el mapa en un dato que el juego construye al arrancar— y no cabía junto
> al FFA y el rendimiento. El FFA pasa a la semana 14, que tiene hueco porque la mitad de su
> audio se adelantó a la 11. **Ninguna semana se queda sin algo que enseñar.**

### Nota · semana 11: el audio se parte en dos y la UI empieza en la 12

> **Decidido el 15/10/2026.** Los efectos de sonido puntuales pasan de la semana 14 a la 11:
> son lo que hace falta oír dentro del juego para decidir qué sonidos de la primera tanda se
> cambian, y esperar a la 14 no dejaría tiempo para buscar sustitutos. La música, el
> ambiente, el mezclador y las partículas se quedan en la 14, que conserva tema propio y lo
> más vistoso para exponer. **Criterio: adelantar sí, pero sin dejar ninguna semana sin
> algo que demostrar.**
>
> La UI deja de ser una sola semana. Empieza en la **12** con el menú, la pantalla de inicio
> y la partida libre como modo de verdad —es lo que la 12 ya pedía como «flujo de
> partida»—, sigue en la 13 con el sandbox ordenado junto al editor de mapas, y la **15**
> la termina: HUD final, tooltips, pausa y opciones. La UI final se hace con Canvas y los
> marcos del pack, no con la GUI inmediata del panel actual, para que la 15 pula y no rehaga.
>
> **Compuerta del PvP: NO-GO** — ver [ADR-24](ARQUITECTURA.md#adr-24).

### Nota · el editor de mapas se evaluó adelantar y se dejó donde estaba

> **Corrección de la columna «Expone» (22/09/2026).** La rotación real es la que se cumplió:
> semana 07 Joaquín, 08 Raúl, 09 Kiara. La columna llevaba otra secuencia desde el principio y
> nadie la había cruzado con quién expuso de verdad. Corregida de la 10 en adelante.
>
> **Cambio de alcance (22/09/2026).** *La IA que defiende su base* pasa de la semana 11 a la
> 10. Una vez que existe la capa táctica, defender es decidir a dónde va la oleada, no un
> sistema nuevo: hacerlo en la 11 significaría volver a abrir el mismo archivo. La semana 11 se
> queda con las tres dificultades, la niebla de la IA y la compuerta del PvP.

En la semana 08 se planteó traer la épica E13 (editor de mapas) a la semana 09. **Se descartó**
y el calendario queda intacto: ver [ADR-17](ARQUITECTURA.md#adr-17).

El motivo no fue técnico —el formato de datos ya está estable y el editor se podría construir
hoy— sino de calendario: adelantarlo dejaba a la IA rival con una sola semana antes de PC2 en
vez de dos, y PC2 pide una partida jugable contra un bot.

Lo que sí se adelantó a la semana 08: los pinceles de recursos y el rediseño de los paneles.

---

## 2. Hitos y evaluaciones

| Hito | Semana | Peso | Qué debe estar corriendo |
|---|---|---|---|
| **Hito 1 · PC1** | 05 | 20 % | Unidades seleccionables que se mueven y recolectan, con HUD |
| **Hito 2 · PC2** | 10 | 20 % | Partida completa jugable contra un bot funcional |
| **Hito 3 · PC3** | 15 | 30 % | Juego con UI final, 3 mapas y FFA de 5 bandos |
| **Examen Final** | 18 | 30 % | Proyecto consolidado, los tres lo sustentan |

### ⚖️ Compuerta PvP — Semana 11

Se decide con criterio explícito, no por entusiasmo:

**GO** solo si en S11 se cumple **todo**:
- El combate y la IA están cerrados y sin bugs bloqueantes.
- El juego corre estable con 3 bandos.
- Existe holgura real de tiempo (nada del cronograma base atrasado).

**NO-GO** → se documenta como análisis técnico de viabilidad, que cubre el tema del sílabo igual.
No es un fracaso; es una decisión de alcance justificada.

---

## 3. Reemplazos frente al cronograma del docente

Argumentario para la sustentación:

| Tema del sílabo | Qué hacemos | Justificación |
|---|---|---|
| **Parallax** (S09) | Niebla de guerra | El parallax es propio del scroll lateral. En vista cenital no existe. La niebla es el equivalente en escenario dinámico. |
| **Carrera de objetos** (S07) | Combate | No aplica al género. Las matemáticas del sílabo (vectores, coordenadas, distancias) se cubren de sobra con A*, el alcance de ataque y las trayectorias. |
| **Física con Rigidbody** (S09-10) | Física por scripts | Un RTS de grilla evita Rigidbody **a propósito**: rompería el determinismo que sostiene el PvP. Sí hay física real implementada a mano: trayectoria de proyectiles y resolución de empuje entre unidades. |
| **Plataformas móviles** (S14) | Audio y feedback | Selección por arrastre y 250 unidades no constituyen un juego móvil. Se cubre con un análisis técnico corto en el documento final. |
| **Networking** (S12) | Compuerta S11 | Autorizado explícitamente por el docente según alcance del proyecto. |

---

## 4. Riesgos del cronograma

| Riesgo | Semana | Mitigación |
|---|---|---|
| **S07-S08 es el tramo más pesado** — combate completo en dos semanas | 07-08 | El combate melee de S07 se puede recortar a solo guerrero si hace falta; el lancero pasa a S08 |
| **Escalar a 5 bandos revela problemas de rendimiento** | 13 | Está puesta después de PC2 a propósito, para tener margen. Si no rinde, se queda en 3 bandos y sigue siendo un juego completo |
| **Solo una persona programa** | Todas | El cronograma ya está dimensionado para eso. El riesgo real no es el código: es que Kiara y Raúl sustenten código que no escribieron → el guion semanal incluye preguntas y respuestas |
| **Balance sin datos de partidas reales** | 16 | Sesiones de prueba de los tres integrantes desde S12, no solo en S16 |
