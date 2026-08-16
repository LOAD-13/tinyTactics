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

Entrega **domingo**, exposición **lunes**. Expone un solo integrante, rotando.

| Sem | Entrega | Expo | Qué se demuestra | Expone |
|---|---|---|---|---|
| **02** | 16/08 | 17/08 | **Concepto + mapa navegable.** Ficha conceptual. Repo con GitFlow y `docs/`. Escena con tilemap pintado y **cámara RTS** (paneo por borde/WASD, zoom). | Joaquín |
| **03** | 23/08 | 24/08 | **Selección y movimiento.** Grilla lógica, A*, interpolación, empuje blando. Clic, caja de arrastre, shift. Orden con clic derecho. | Kiara |
| **04** | 30/08 | 31/08 | **Animación y estados.** FSM por unidad (idle/mover/atacar/morir), animador por sprite-swap, las 4 unidades en los 5 colores. | Raúl |
| **05** | 06/09 | 07/09 | **🏁 HITO 1 · PC1 — Economía.** Pawns recolectan oro y madera, vuelven al castillo, HUD con contadores. | Joaquín |
| **06** | 13/09 | 14/09 | **Construcción y población.** Colocar casa/cuartel con costo, límite de población, entrenar pawns. Patrones documentados. | Kiara |
| **07** | 20/09 | 21/09 | **Combate cuerpo a cuerpo.** Guerrero y lancero: HP, daño, barras de vida, muerte. Targeting con grilla espacial. | Raúl |
| **08** | 27/09 | 28/09 | **Combate a distancia.** Arquero con proyectiles, monje que cura, triángulo de contadores. Cuartel/campo de tiro/monasterio. | Joaquín |
| **09** | 04/10 | 05/10 | **Niebla de guerra + minimapa.** Visibilidad por facción, minimapa con unidades. | Kiara |
| **10** | 11/10 | 12/10 | **🏁 HITO 2 · PC2 — IA rival v1.** Capa estratégica + táctica. El bot recolecta, construye, entrena y ataca. Primera partida completa vs bot. | Raúl |
| **11** | 18/10 | 19/10 | **IA v2 + ⚖️ COMPUERTA PvP.** 3 dificultades, IA que defiende. Decisión go/no-go documentada. | Joaquín |
| **12** | 25/10 | 26/10 | **Partida completa.** Victoria/derrota por castillo, menú principal, selección de mapa y nº de bandos. | Kiara |
| **13** | 01/11 | 02/11 | **Escalado a FFA.** Spawns múltiples, IA multi-bando, tuning de rendimiento a ~250 unidades. Segundo mapa. | Raúl |
| **14** | 08/11 | 09/11 | **Audio y feedback.** Música, SFX, AudioMixer, partículas de impacto y construcción. | Joaquín |
| **15** | 15/11 | 16/11 | **🏁 HITO 3 · PC3 — UI/UX.** HUD final, panel de selección, tooltips, menús, pausa. Tercer mapa, FFA de 5 corriendo. | Kiara |
| **16** | 22/11 | 23/11 | **Balance y QA.** Sesiones de prueba reales, ajuste de números (incluida la carne), plan de pruebas, corrección de bugs. VFX. | Raúl |
| **17** | 29/11 | 30/11 | **📦 Entrega final.** Build jugable + documentación técnica consolidada desde la bitácora. | Joaquín |
| **18** | — | 07/12 | **Examen final.** Preparación individual: los tres dominan el proyecto completo. | — |

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
