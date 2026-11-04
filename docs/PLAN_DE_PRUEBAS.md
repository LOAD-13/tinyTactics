# Plan de pruebas — Tiny Tactics

> Semana 16 (E11, HU-141). Qué se prueba, cómo y con qué criterio se da por bueno, antes de
> la versión 1.0 de la semana 17. Los resultados se apuntan en las columnas vacías y pasan a
> la sección «Pruebas y QA» del informe final.

## 1. Tres niveles de prueba

| Nivel | Qué cubre | Cómo se ejecuta | Quién |
|---|---|---|---|
| **Automáticas** | La lógica pura: visibilidad, plan y reparto de la IA, rivales del FFA, mapas oficiales, mezclador, historial | `python tools/probar.py` (ADR-22) | Joaquín, en cada cambio |
| **Manuales** | Cada pantalla y cada flujo del juego, en la build de entrega | Los casos de la sección 3 | Los tres |
| **Sesiones de juego** | El balance: partidas reales contra las tres dificultades | La sección 4, con el informe de partida | Los tres |

Las automáticas no tocan nada que dependa del motor (objetos, corrutinas, interfaz): eso solo
se comprueba jugando, y por eso existen los otros dos niveles.

## 2. Pruebas automáticas

90 bloques y 48 144 comprobaciones al empezar la semana 16. Las nuevas de esta semana:

| Bloques | Qué comprueban |
|---|---|
| `reparto: …` (6) | Que el bot mueve pawns hacia el recurso que le falta, de forma gradual, y que con un pawn de margen no los cambia de sitio |
| `historial: …` (6) | Los puestos del marcador, la racha, el porcentaje de victorias, las claves de una partida y la medida de economía torcida |
| `rendimiento: …` (1) | La media, el 1 % lento y el peor fotograma del medidor |

## 3. Casos manuales

Se ejecutan sobre la **build de Windows**, no en el editor. ✅ pasa · ❌ falla (apuntar el error en la sección 7).

| ID | Área | Pasos | Resultado esperado | Resultado | Quién |
|---|---|---|---|---|---|
| CP-01 | Arranque | Abrir el ejecutable | Ventana maximizada, título, «Pulsa cualquier tecla», música del menú | | |
| CP-02 | Menú | Pasar el ratón y pulsar cada una de las siete espadas | Cada una abre su pantalla; ESC vuelve | | |
| CP-03 | Opciones | Cambiar volumen general y por canal, pantalla completa, FPS | Se aplica al momento y se conserva al reiniciar | | |
| CP-04 | Controles | Abrir los controles desde Opciones | Teclado y ratón con sus fichas, ninguna línea cruzada | | |
| CP-05 | Preparar partida | Cambiar mapa, ocupantes y colores; intentar dos bandos del mismo color | No deja empezar y dice por qué | | |
| CP-06 | Tutorial | Hacer los seis pasos | Cada paso avanza solo al cumplirse; la despedida al ganar | | |
| CP-07 | Tutorial | Pulsar «Saltar tutorial» | Vuelve al menú sin errores | | |
| CP-08 | Economía | Recolectar oro, madera y carne con pawns | Los contadores suben al depositar | | |
| CP-09 | Construcción | Construir casa, cuartel, arquería, torre | Fichas con coste; en rojo y con motivo si falta algo | | |
| CP-10 | Producción | Entrenar las cinco unidades | Cola, tiempo y mensaje «X listo» | | |
| CP-11 | Población | Llenar la población | Aviso en pantalla y el botón dice «construye una casa» | | |
| CP-12 | Combate | Atacar con cada tipo de unidad | Daño visible, triángulo lancero > guerrero > arquero > lancero | | |
| CP-13 | Aviso | Dejar que el bot ataque la base | «¡Te atacan!» y marca en el minimapa | | |
| CP-14 | Pawns ociosos | Dejar un pawn parado y pulsar O | El botón aparece y la cámara va al pawn | | |
| CP-15 | Niebla | Explorar el mapa | Lo no visto, oculto; lo visto, en gris | | |
| CP-16 | Grupos | Ctrl+1, seleccionar otra cosa, 1 | Recupera el grupo | | |
| CP-17 | Pausa | ESC en partida | El juego se para; Revancha, Menú y Salir funcionan | | |
| CP-18 | Victoria | Ganar una partida | Cartel de victoria escalado a la pantalla | | |
| CP-19 | Derrota | Perder una partida | Cartel de derrota | | |
| CP-20 | FFA | Partida en Cuatro Vientos con 3 bots | Los bots también se atacan entre ellos | | |
| CP-21 | Historial | Terminar una partida y abrir Historial | La partida aparece la primera, con su resultado | | |
| CP-22 | Historial | Abrir el detalle de una partida | Marcador por puesto; Evolución con una línea por bando | | |
| CP-23 | Historial | Salir al menú a mitad de partida (más de 1 min) | Queda como «Abandonada» y no cuenta en el porcentaje | | |
| CP-24 | Editor | Crear, guardar, jugar y borrar un mapa | Aparece en la lista; al borrarlo desaparece | | |
| CP-25 | Sandbox | Recursos infinitos, x4, bots que no atacan | Cada regla hace lo que dice | | |
| CP-26 | Audio | Jugar hasta que haya combate | La música pasa a la de combate y vuelve tras la calma | | |

## 4. Sesiones de juego (balance)

Cada integrante juega **tres partidas**, una por dificultad, de modo que entre los tres se
cubra cada mapa contra cada dificultad:

| Quién | Fácil | Normal | Difícil |
|---|---|---|---|
| Joaquín | Tres Coronas | Cuatro Vientos | Lago Helado |
| Kiara | Lago Helado | Tres Coronas | Cuatro Vientos |
| Raúl | Cuatro Vientos | Lago Helado | Tres Coronas |

**Qué se entrega:** la carpeta del historial (`%USERPROFILE%\AppData\LocalLow\Grupo 01 UTP\Tiny Tactics\Historial`),
que el juego rellena solo, y una línea por partida en la tabla de abajo con lo que se sintió.
Los números salen de los archivos; la sensación solo la puede contar quien jugó.

| Quién | Mapa | Dificultad | Resultado | Duración | ¿Demasiado fácil o difícil? ¿Qué frustró? |
|---|---|---|---|---|---|
| | | | | | |

**Criterio de balance:** el Fácil se gana casi siempre jugando con calma; el Normal pide
atacar con un ejército mixto; el Difícil gana a quien juega de forma pasiva. Si una dificultad
no cumple su frase, se ajustan sus números —bonus de recursos y tiempo de la primera oleada—, no la
inteligencia del bot.

## 5. Prueba de la IA: reparto de pawns (HU-136/137)

Compara la regla vieja (proporción fija) con la nueva (según el almacén).

Se juega sola, desde la línea de comandos, sin nadie delante (`PartidasAutomaticas`):

```
"Tiny Tactics.exe" -batchmode -nographics -autopartidas 6 -mapa "Lago Helado" -rivales Dificil -yo Dificil -reparto fijo
"Tiny Tactics.exe" -batchmode -nographics -autopartidas 6 -mapa "Lago Helado" -rivales Dificil -yo Dificil -reparto reservas
python tools/comparar_reparto.py
```

A mano también vale: panel de pruebas (Ctrl+Mayús+F12) → «Bot contra bot» → x4, y el
interruptor «Reparto» para cambiar de regla.

**Criterio:** la regla nueva pasa menos segundos con la economía torcida (sin un recurso y
sobrado del otro) y no recolecta menos por minuto.

**Resultado (04/11/2026):** con 6 partidas por regla, la nueva pasa 58 s con la economía
torcida frente a 127 s, recolecta un 5 % menos al minuto 5 y gasta un 10 % más por minuto.

### 5.1 Patrones de ataque y defensa (HU-145)

Táctica nueva contra la anterior, Normal contra Normal en Lago Helado, cada una en las dos
bases (`-tacticayo vieja` / `-tacticarivales vieja`, 15 + 15 partidas por variante):

| Variante de la táctica nueva | Gana |
|---|---|
| Completa | 19 de 30 (63 %) |
| Sin defensa agrupada | 17 de 30 (57 %) |
| Sin retirada | 16 de 30 (53 %) |
| Sin incursiones | 16 de 30 (53 %) |

La primera versión perdió sus diez primeras partidas: repetía la orden de agruparse y las
tropas morían andando, y la incursión dejaba la base a medias. Se corrigió antes de medir.

### 5.2 Calibración de dificultades

Un bot Normal contra cada dificultad, cambiando de base en cada partida (`-alternar`), 10
partidas: gana el 90 % al Fácil, el 40 % al Normal y el 10 % al Difícil.

### 5.3 Hallazgo: ventaja de la base sur en Lago Helado

En 150 partidas de bots, la base sur ganó 107 (71 %). No es el asiento del jugador (se repite
sin jugador) ni la distancia a las minas (es idéntica). Queda abierto para la semana 17.

## 6. Rendimiento

1. Build de entrega, **Cuatro Vientos**, los cuatro bandos con bot (bot contra bot), x4.
2. Hacia el minuto 15 de partida, con ejércitos grandes, volver a x1.
3. Durante una batalla, panel → «Medir rendimiento (60 s)». El resultado queda en el panel y
   en `rendimiento.txt`, junto a la carpeta del historial.

**Automático:** `-recursos infinitos -carga 60 -medir 2` hace aparecer 60 tropas por bando en
el centro y mide 60 s a tiempo real, con ventana.

**Resultado (04/11/2026, build):** 289 unidades peleando, 170 FPS de media, 94 en el 1 % lento
y peor fotograma de 31 ms. Cumple el criterio con margen: no se optimiza.

**Criterio:** 1 % lento de 45 FPS o más. Referencia de la semana 14 (editor, 263 unidades
peleando): 66 de media, 30 en el 1 % lento y un peor fotograma de 54 ms. Si la build no llega,
se reutilizan los efectos y las flechas en vez de crearlos en cada golpe; si llega, no se toca.

## 7. Registro de errores

| ID | Caso | Qué pasó | Gravedad (alta / media / baja) | Estado | Arreglo |
|---|---|---|---|---|---|
| | | | | | |
