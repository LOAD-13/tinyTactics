# Bitácora — Tiny Tactics

> Registro de **lo que realmente pasó** cada semana, no de lo que se planeó.
> Es la memoria del proyecto: permite retomar el hilo en la semana 12 y es la materia prima
> de la documentación técnica final de la semana 17.
>
> **Regla:** si algo se prometió y no se hizo, se escribe que no se hizo. No se maquilla.
> Un registro honesto sirve; uno decorativo no sirve para nada.

Formato de entrada: entradas nuevas **arriba**.

---

## Semana 09 — La percepción (E07) · épica cerrada
**Entrega:** domingo 04/10/2026 · **Expo:** lunes 05/10/2026 · **Expone:** Kiara
**Tag:** [`v0.9.0-s09`](https://github.com/LOAD-13/tinyTactics/releases/tag/v0.9.0-s09) · **Rama:** `feat/E07-niebla-y-minimapa`

### Lo prometido
Diez HUs: HU-065 grilla de visibilidad por facción · HU-066 radios de visión en las fichas ·
HU-067 la niebla dibujada · HU-068 lo que está en niebla no se dibuja · HU-069 la niebla como
regla de juego · HU-070 minimapa con el terreno · HU-071 el minimapa respeta la niebla ·
HU-072 clic y arrastre mueven la cámara · HU-073 día, mediodía y noche · HU-074 la percepción
en el panel de partida libre.

### Lo entregado
Las diez, más dos fallos que solo salieron jugando y que no estaban en el alcance. El ciclo
del día venía **arrastrado de la semana anterior**, sin épica asignada, y entró aquí porque
comparte toda la maquinaria con la niebla: la misma lámina multiplicativa, el mismo shader, el
mismo orden de dibujado. Montarlo por separado habría sido montarlo dos veces.

### Lo que costó de verdad

**El bando con el que arrancas la partida no veía nada, y los otros dos sí.** El síntoma no
apuntaba a ninguna parte. La causa: el tamaño del mapa se fijaba en el `Start` de la niebla,
pero el minimapa también tiene `Start` y **Unity no garantiza cuál corre primero**. Cuando
corría antes el del minimapa, éste pedía el mapa de visibilidad del jugador y se creaba con el
ancho y el alto todavía a cero —un mapa de **una** celda— que se quedaba cacheado para toda la
partida. Los demás bandos iban bien porque sus mapas nacían más tarde, ya con el tamaño puesto.

Es el mismo fallo que el cartel de final de la semana 07: dar por bueno un orden de arranque
que el motor no promete. Se arregló igual que entonces —engancharse cuando hace falta en vez de
cuando toca— y además se comprueba el tamaño al entregar un mapa, por si alguien vuelve a
pedirlo demasiado pronto.

**Los pawns no le pegaban al castillo. Y es, otra vez, el fallo de la semana 08.** La distancia
a un edificio se mide contra su huella, y una unidad no puede meter su centro dentro del muro:
lo más cerca que llega es su propio radio. Un pawn tiene 0,50 de alcance y 0,42 de radio, así
que se planta a unos 0,56 del edificio y **nunca entra en su propio alcance**. El guerrero sí
llegaba, con 0,80.

Alcance, radio y dónde puede aparcar el pathfinding son tres medidas correctas por separado que
nadie había medido una contra otra. Es literalmente el mismo enunciado que la semana pasada,
con otros tres números. Contra unidades ya estaba resuelto descontando el radio del objetivo;
faltaba el caso del edificio, que es el mismo problema visto desde el otro lado.

**La niebla es dura en el espacio y suave en el tiempo, y esa frase costó dos intentos.** Las
celdas se abrían de golpe y en bloques de diez por segundo: se veía el cuadriculado
encendiéndose a saltos. El instinto era difuminar el borde, y habría sido un error —un
difuminado sobre pixel art no cae en la misma rejilla que los tiles y se delata solo. Lo que
molestaba no era el cuadrado, era el **parpadeo**. Se separaron los dos ritmos: *qué se ve* se
recalcula diez veces por segundo, *cómo se dibuja* corre cada fotograma, y cada celda se
desvanece hacia su estado. El escalón de un tile sigue ahí, a propósito.

**El manto de nubes: construido, probado y apagado.** Tapar lo inexplorado con las ocho nubes
pintadas a mano del pack tenía un argumento bueno —que la frontera de la niebla la dibujara el
mismo artista que dibujó el bosque de al lado— y se montó entero: repartidas por rejilla,
recicladas fuera del encuadre y balanceándose en el sitio en vez de ir a la deriva, porque una
nube que viaja acaba tapando terreno ya explorado. **Jugado, tapaba demasiado:** la partida
empezaba sin poder leer el mapa, y eso agobia en vez de intrigar. El código se queda con su
interruptor en el panel, apagado. Apagar algo que funciona es reversible; borrarlo no.

Las nubes grandes sí se quedaron, pero en otro sitio: como ambiente cruzando el mapa. Van a la
mitad de velocidad que las pequeñas, y esa es toda la gracia — una nube grande que cruza al
mismo ritmo se lee como un sprite grande moviéndose; a la mitad de velocidad se lee como una
nube que está más alta. Paralaje sin cámara de paralaje.

**Un campo, un responsable.** Para tapar unidades enteras no se tocó `enabled`, que ya lo
manejan la selección y la barra de vida, sino `forceRenderingOff`, que existe justo para vetos
de visibilidad y que no usa nadie más. Y la noche no tiñe sprites: multiplica una lámina por
encima. Las dos decisiones son el [ADR-11](ARQUITECTURA.md#adr-11) aplicado a otros campos.

### Decisiones
- [ADR-18](ARQUITECTURA.md#adr-18): la niebla es una textura de un píxel por celda, dura en el
  espacio y suave en el tiempo.
- [ADR-19](ARQUITECTURA.md#adr-19): lo que oscurece el mapa se multiplica por encima; no se
  tiñe sprite a sprite.
- [ADR-20](ARQUITECTURA.md#adr-20): la niebla es una regla de juego, no un filtro de imagen.

### Lo que se decidió NO hacer
- **Que la noche recorte los radios de visión.** Es una mecánica de verdad y es tentadora, pero
  movería en silencio el balance cerrado en la semana 08 y lo movería solo durante un tercio de
  cada partida. Sin antorchas ni unidades que vean de noche, la oscuridad no añade una decisión
  — solo incomodidad, y por eso la noche es además la hora más corta de las tres.
- **Empezar con el mapa entero explorado.** Se probó encendido. La partida era más cómoda, pero
  la niebla dejaba de contar nada y el minimapa pasaba a ser una foto completa del mapa desde el
  segundo cero. Se quedó **solo la mitad que hacía falta**: las bases de salida se ven desde el
  primer fotograma aunque estén en sombra. Se regala *dónde* empieza cada rival —que en un mapa
  simétrico se deduce mirando el tuyo— y no qué hay entre medias.

---

## Semana 08 — El triángulo (E06, parte B) · épica cerrada
**Entrega:** domingo 27/09/2026 · **Expo:** lunes 28/09/2026
**Tag:** [`v0.8.0-s08`](https://github.com/LOAD-13/tinyTactics/releases/tag/v0.8.0-s08) · **Rama:** `feat/E06-cierre`

### Lo prometido
Ocho HUs: HU-057 las unidades entrenadas no se apilan · HU-058 posturas en el panel
*(arrastrada de la 07)* · HU-059 el monje cura solo y se repliega · HU-060 triángulo de
contadores · HU-061 realimentación de impacto · HU-062 iconos de recurso legibles ·
HU-063 plantillas de recurso · HU-064 pinceles de recursos en el panel.

### Lo entregado
Las ocho, más el rediseño del panel lateral y **dos fallos de combate que solo aparecieron
al jugar**. La épica E06 queda cerrada: el combate ya no es una cuestión de quién pega más
fuerte, sino de a quién mandas contra quién.

### Lo que costó de verdad

**Dos guerreros no podían alcanzarse, y la decisión que lo causó se había tomado por
prudencia.** El guerrero tiene 0,80 de alcance y 0,44 de radio; el empuje entre unidades las
separa por la suma de los radios, o sea 0,88. Midiendo entre centros, dos guerreros pegados
están «a 0,88» y nunca entran en un alcance de 0,80: solo conectaban en los fotogramas
sueltos en que el empuje aún no había terminado de separarlos. Lo incómodo es de dónde venía:
en la semana 07 se eligió medir entre centros **precisamente para no tocar unos alcances ya
ajustados**. La lección no es «mide en vez de estimar», que ya estaba aprendida. Es que
**medir dos sistemas por separado no basta si nadie los mide uno contra otro.**

**Y encima la persecución se quedaba atascada.** `_persiguiendo` solo se limpiaba con un
golpe acertado, así que una unidad que llegaba un pelo corta no volvía a intentarlo nunca:
había que clicar una vez por espadazo. Los dos fallos juntos daban el síntoma que Raúl
describió —«el guerrero lo dejo a 25 % y deja de pegar»— y ninguno de los dos se ve leyendo
el código sin jugarlo.

**El triángulo salió de jugar, no de la hoja de cálculo.** La primera tabla daba ×1,0 a la
lanza contra armadura ligera, y con eso el lancero le ganaba al arquero: justo el lado del
triángulo que tenía que cumplirse al revés. Se bajó a ×0,85 y se subió la flecha contra asta
a ×1,55. Los tres duelos se probaron uno a uno.

**Ningún contador baja de ×0,75, y es una decisión de diseño.** Lo peor que puede pasar es un
25 % menos de daño, no la mitad. Un contador duro convierte el combate en un acertijo de
composición —si no traes la unidad correcta no hay nada que hacer— en vez de en una decisión
táctica, donde traerla es una ventaja y no un requisito.

**El destello aclara, no pinta de blanco.** Es multiplicativo, así que conserva la silueta y
el color del bando. Pintar blanco plano borraría de qué facción es la unidad justo en el
momento en que más importa saberlo. Y vive dentro de la máquina de estados, no en un
componente aparte, porque dos componentes escribiendo el color del mismo sprite se pelearían
con el desvanecido de la muerte y ganaría el que escribiera el último ([ADR-11](ARQUITECTURA.md#adr-11)).

**Las plantillas del editor clonan un árbol de verdad.** Configurar uno exige acertar con el
recurso, el radio de bloqueo, el tocón, los segundos de resto y la especie. Una segunda copia
de esa configuración se desincroniza a la primera corrección; copiando un nodo que ya está en
el mapa, la plantilla es correcta por construcción.

**El fallo que vio la clase, corregido.** Las unidades entrenadas se apilaban y sus sprites se
intercalaban, y eran **dos causas a la vez**: todas nacían en la misma celda —y con la
posición idéntica el empuje no tiene dirección hacia la que separarlas— y con la altura
idéntica compartían orden de dibujo, que el motor resuelve como quiere y cambiando cada
fotograma. Ahora salen en abanico y cada objeto lleva un desempate estable.

### Decisiones
- [ADR-17](ARQUITECTURA.md#adr-17): el editor de mapas **se queda en la semana 13**.

### Lo que se decidió NO hacer
- **Penalizar a las flechas contra edificios.** La propuesta era que asediar fuera trabajo de
  cuerpo a cuerpo. Se descartó: un arquero que no puede participar en la mitad de la partida
  es una unidad que nadie entrena. La columna de edificios va entera a ×1.
- **Adelantar el editor de mapas a la semana 09.** El argumento a favor era bueno —el motivo
  original para dejarlo tarde, que el formato de datos se movía, ya no aplica— pero
  adelantarlo dejaba a la IA con una semana antes de PC2 en vez de dos, y PC2 pide una partida
  jugable contra un bot. **El argumento técnico era válido; el riesgo no era técnico, era de
  calendario, y un riesgo de calendario no se resuelve con un buen argumento técnico.**

---

## Semana 07 — El conflicto (E06, parte A)
**Entrega:** domingo 20/09/2026 · **Expo:** lunes 21/09/2026
**Tag:** [`v0.7.0-s07`](https://github.com/LOAD-13/tinyTactics/releases/tag/v0.7.0-s07) · **Rama:** `feat/E06-combat`

### Lo prometido
Once HUs: HU-046 responder al ser atacado · HU-047 correa y posturas · HU-048 vida y
destrucción de edificios · HU-049 la torre con arquero guarnecido · HU-050 torres rivales ·
HU-051 panel de pruebas v1 · HU-052 iconos del panel · HU-053 retirar el poste ·
HU-054 estadísticas · HU-055 condición de derrota · HU-056 pantalla de victoria y derrota.

### Lo entregado
Las once, más tres arreglos que salieron de las pruebas y no estaban en el alcance: la
marca de clic derecho, el cursor de ataque y el derrumbe con explosiones. La partida
cambia de naturaleza: ya se puede ganar y se puede perder.

### Lo que costó de verdad

**Media épica ya estaba escrita sin que nadie lo supiera.** Al abrir la semana resultó que
`Unidad` ya tenía vida, daño y muerte como estado; que la máquina ya sabía acercarse y
golpear; que ya buscaba objetivo sola, y que `RegistroDeUnidades` ya resolvía el vecindario
con una grilla espacial por cubos. Lo que faltaba no era el combate: era que el golpeado
**respondiera**, que la persecución **terminara**, que los edificios **cayeran** y que la
partida **se pudiera perder**. Leer el código antes de planificar cambió el tamaño de la
semana y dejó sitio para adelantar la pantalla de final, que estaba puesta en la 08.

**El `null` falso de Unity se pierde al usar interfaces**
([ADR-15](ARQUITECTURA.md#adr-15)). Para que una unidad pudiera atacar a un edificio los dos
pasaron a implementar `IObjetivo`. Unity finge que un objeto destruido es `null`, pero ese
truco vive en `UnityEngine.Object` y **desaparece en cuanto la referencia se guarda como
interfaz**. Es literalmente el fallo que la semana pasada dejó al pawn plantado tras matar una
oveja, vuelto a aparecer con otra cara. Se cerró antes de que costara nada, con un `Existe()`
que vuelve a pasar por `MonoBehaviour`.

**Un monje agredido habría curado a su agresor.** El monje tiene daño negativo, así que
engancharlo automáticamente a quien le pega lo mandaba a curar al enemigo que lo estaba
matando. La respuesta al ataque necesita una guarda explícita de daño positivo, y la necesita
por una razón de diseño, no de seguridad: hay una unidad en el juego cuyo «devolver el golpe»
significa lo contrario que para todas las demás.

**El cursor marcaba como prohibido lo único que se puede atacar.** El puntero decidía con la
regla «celda intransitable = prohibido», y un edificio enemigo bloquea sus propias celdas: el
aspa caía justo encima del objetivo de la partida. Es el mismo tropiezo que ya se corrigió con
los árboles en la semana 05, repetido con los edificios. La comprobación de enemigo va ahora
antes que la de la grilla, igual que entonces se puso la de recurso antes.

**Dos fallos de orden de arranque, cazados por lectura y no por síntoma.** El cartel de final
se suscribía al árbitro en su `OnEnable`, y Unity no garantiza el orden de los `Awake` entre
objetos: si el cartel despertaba primero, no se suscribía nadie y la pantalla no salía nunca.
El síntoma habría sido «a veces sale y a veces no». Y el árbitro daba por eliminado a
cualquier bando sin castillo, incluido uno que nunca lo tuvo: con el contador de bandos por
encima de las bases del mapa, la partida terminaba en el primer fotograma.

**Ya no hace falta esperar a que Unity recupere el foco para saber si algo rompe.** Unity solo
recompila cuando el editor gana el foco, así que cada tanda de cambios se quedaba sin
verificar. Los `.csproj` que Unity genera traen la lista completa de referencias, y el propio
Unity trae Roslyn: con eso se compila el proyecto entero desde fuera, con el mismo compilador
y las mismas referencias. Un error ahí es un error en Unity.

### Decisiones
- [ADR-15](ARQUITECTURA.md#adr-15): lo que se puede atacar se esconde detrás de `IObjetivo`.
- [ADR-16](ARQUITECTURA.md#adr-16): el panel de pruebas es herramienta transversal, no épica.

### Lo que se decidió NO hacer
- **Generar los iconos del panel dibujándolos.** Se planteó extraer la paleta de los PNG del
  pack y pintar iconos de 32×32. Al ir a hacerlo resultó que no hacía falta: el tema ya trae
  los tres sacos de recurso y los veinticinco retratos de unidad, que son exactamente los
  iconos que el panel necesita. Arte nuevo para enseñar algo que ya está dibujado es arte que
  además puede desentonar.
- **La pantalla de final como imagen o GIF.** No escala de resolución, no puede mostrar cifras
  que cambian, y ningún generador de imagen mantiene la rejilla de píxeles: lo que producen
  parece pixel art con el contorno y la paleta mal. Se monta con piezas del propio pack.

---

## Semana 06 — Construcción y producción
**Entrega:** domingo 13/09/2026 · **Expo:** lunes 14/09/2026
**Tag:** [`v0.6.0-s06`](https://github.com/LOAD-13/tinyTactics/releases/tag/v0.6.0-s06) · **Rama:** `feat/E05-construccion`

### Lo prometido
Las diez HUs de la épica E05, sin bloque opcional: HU-036 resaltado del nodo (arrastrada de la
semana 05) · HU-037 catálogo de edificios · HU-038 colocación con silueta · HU-039 el pawn
construye · HU-040 población y contador · HU-041 la casa sube el límite · HU-042 los tres
edificios de producción · HU-043 el panel dinámico · HU-044 tocón por especie · HU-045 retirar
la escuadra regalada.

### Lo entregado
Las diez. La partida cambia de forma: se empieza con **dos pawns y un castillo**, y el ejército
hay que construirlo.

### Lo que costó de verdad

**El tocón mentía por una diferencia de lienzo.** El generador sembraba cada árbol eligiendo una
de las cuatro variantes al azar, y al talarlo sorteaba uno de los cuatro tocones — otra vez al
azar, sin relación con el árbol que había. El pack dibuja `Tree1` y `Tree2` en lienzos de
192×256 y `Tree3` y `Tree4` en 192×192, así que un pino talado que sacara el tocón de un roble
aparecía desplazado un tercio de tile. Se veía como un fallo de alineación y era un fallo de
emparejamiento. Ahora `Sembrar` **apunta qué variante sembró** en cada sitio y cada nodo se
queda solo con su tocón.

**El castillo bloqueaba terreno con un disco y los edificios nuevos no tenían cómo.** El mapa
marcaba las celdas de las bases con un disco de radio 2,6 desde la lista de bases del generador.
Eso no sirve para nada construido en partida: la lista es de antes de empezar. Y había un
segundo problema esperando — al talar un árbol, `LiberarRecurso` recalcula una ventana entera y
repone lo que sigue vivo, así que una casa pegada a un bosque se habría quedado con un pasillo
pisable por debajo en cuanto alguien talara al lado.

Se resolvió unificando: **cada edificio bloquea su propio rectángulo de celdas** en su `Start`,
el castillo incluido, y `LiberarRecurso` repone preguntándoles a los edificios vivos en vez de a
una lista escrita antes de la partida. Una sola regla para el que viene con el mapa y para el
que levanta el jugador.

**Dos medidas, no una** ([ADR-14](ARQUITECTURA.md#adr-14)). Los edificios están dibujados en
perspectiva y el recuadro del dibujo no es el suelo que ocupan. Intentar que una sola cifra
sirviera para las dos cosas daba a elegir entre un monasterio imposible de colocar o un pawn
entregando a través del muro.

**El martillo no cabía en `TipoRecurso`.** La tabla de animación del pawn se indexa por estado y
por recurso, y la tentación era añadir un cuarto valor al enum. Habría colado el martillo en las
tablas de extracción y de carga de la economía, que se indexan por ese mismo enum. Es una
bandera aparte, y además tenía que serlo: un pawn puede llegar a la obra **con el saco todavía
encima**, así que el martillo tiene que ganarle al dibujo de la carga sin borrarlo.

**La medición automática se validó contra la manual.** La herramienta nueva mide el recuadro de
píxeles opacos abriendo el PNG. Para el castillo devolvió 4,88 × 3,25 con el centro 0,27 por
debajo: exactamente las cifras que se habían sacado a mano en la semana 05. La fórmula del
corchete de selección, generalizada a `huella.x / 1.33`, da 3,67 donde había un 3,7 puesto a
ojo.

### Lo que salió en la prueba de Joaquín

Seis cosas, y dos de ellas eran fallos de verdad.

**El pawn se quedaba plantado tras matar una oveja.** El más interesante de la semana, porque el
síntoma señalaba al sitio equivocado. Al buscar relevo, el tipo de recurso se deducía del nodo
que acababa de trabajar; pero la oveja **se destruye** al sacrificarla, y un objeto destruido de
Unity finge ser `null`, así que el tipo caía al de la carga… que se acababa de vaciar al
depositar. Resultado: tipo «ninguno» y a casa.

Con los árboles no pasaba, porque el tronco talado sigue existiendo como tocón. Por eso parecía
un problema de las ovejas y era un problema de suponer que un nodo sigue ahí después de
explotarlo. Ahora el tipo se guarda en un testigo al empezar la faena.

**Un pawn encerrado entre dos construcciones.** Seleccionable, aceptando órdenes y sin dar un
paso: las ocho celdas de alrededor habían quedado bloqueadas. El A* no falla, simplemente no
encuentra ruta, así que no hay ni un mensaje que lo delate. Se añade una comprobación al
confirmar la colocación —un barrido acotado que busca bolsas sin salida— y, aparte, el edificio
aparta a quien se le quede debajo. Lo primero evita la ratonera; lo segundo evita tener que
mover las unidades a mano antes de cada casa.

**Y cuatro añadidos.** Las piedras y los arbustos se pueden despejar. Los tocones se van solos al
medio minuto. Las tres casas del pack se eligen con la rueda. Y el cartel del resaltado pasa de
un rectángulo negro a la caja de madera del propio pack: se leía bien, pero un solo elemento con
estética de menú de depuración basta para que toda la interfaz parezca provisional.

**En la segunda pasada salieron dos más, y los dos eran del mismo día.** El pawn se ponía
literalmente encima de la piedra a picarla: los estorbos se dejaron sin bloquear la grilla —son
decoración, razoné— y por eso su casilla era la más cercana y estaba libre. Con el terreno
tapado, como el árbol, se arrima por fuera sin tocar una línea del recolector.

El otro fue peor porque lo había dado por arreglado. Una casa colocada justo encima de un pawn
lo dejaba dentro, sin poder moverse, pese a que el código ya buscaba apartarlo. El motivo es de
manual y lo pasé por alto: al encender un objeto en caliente, Unity ejecuta su `Awake` en el
acto pero **aplaza el `Start` hasta antes del siguiente Update**, y era `Start` quien bloqueaba
el terreno. Así que se le buscaba sitio libre al pawn preguntándole a una grilla que todavía no
sabía que la casa estaba ahí, y la respuesta era «donde estás ya vale». Ahora el edificio reclama
su terreno en el acto y el apartado va después.

**El contador de población vuelve a la esquina.** Lo había puesto en la fila de recursos
razonando que se gasta como el oro. Joaquín lo quería aparte y tiene razón por un motivo mejor
que el mío: la población no es un recurso, es un límite. Junto al oro y la madera se lee como
«cuánto tengo» cuando lo que dice es «cuánto me cabe».

### Decisiones tomadas
- **La obra se cuenta en martillazos, no en segundos** ([ADR-13](ARQUITECTURA.md#adr-13)). Que
  dos pawns tarden la mitad sale gratis y parar a medias no regala progreso.
- **Una sola orden planta una sola obra**, por muchos pawns que la reciban. Es la diferencia
  entre mandar cinco pawns a construir una casa y acabar con cinco casas apiladas.
- **La población se recuenta, no se guarda.** Una unidad que muere libera su hueco sin que nadie
  la descuente, y el día que los edificios se puedan destruir, una casa caída bajará el tope sin
  una línea más.
- **La rejilla del panel se volvió dinámica.** El cuartel entrena dos unidades, y con un botón
  fijo por acción el lancero no habría tenido dónde salir.
- **El contador de población va aparte**, arriba a la izquierda. No es un recurso: es un límite.
- **Lo que no se puede pagar se ve apagado, no escondido.** Un cuartel que desaparece cuando
  falta madera nunca le enseña al jugador hacia dónde ahorrar.
- **Una ficha de casa con tres fachadas**, no tres fichas. Cuestan y dan lo mismo, así que son
  un edificio con tres dibujos; la rueda pasa de uno a otro al colocar.
- **Los estorbos tapan su casilla, como un árbol.** Se probó sin bloquear —total, son
  decoración— y el pawn se plantaba encima de la piedra a picarla, porque la celda estaba libre
  y era la más cercana. Con el terreno tapado se arrima por fuera, que es lo que ya hacía bien
  con los árboles, y de paso «despejar la zona» significa algo también para el paso.

### Andamio que hay que retirar
- El **poste de entrenamiento** sigue. Es la única forma de ver daño y muerte hasta que haya
  enemigos de verdad, en E06.
- La **escuadra de diez unidades ya se retiró**: era de cuando no se podía entrenar nada.

### Pendiente
- Torre, vida y destrucción de edificios: declarados fuera de alcance antes de empezar, van a E06.
- El coste de obra de cada edificio está puesto a ojo; se calibra en la semana 16.

---

## Semana 05 — Economía · HITO 1 · PC1
**Entrega:** domingo 06/09/2026 · **Expo:** lunes 07/09/2026 · **Expone:** Joaquín
**Tag:** [`v0.5.0-s05`](https://github.com/LOAD-13/tinyTactics/releases/tag/v0.5.0-s05)

### Lo prometido
HU-026 nodos que se agotan · HU-027 almacén por facción · HU-028 el castillo como entidad
seleccionable · HU-029 ciclo de recolección · HU-030 animaciones de trabajo y de carga ·
HU-031 orden contextual de recolectar · HU-032 HUD · HU-033 entrenar pawns · HU-034 sustento.

### Lo entregado
Las nueve del bloque A, más HU-035 (punto de reunión) que era bloque B. Queda fuera HU-036, el
resaltado del nodo bajo el cursor. Además, tres cosas que no estaban en la lista y salieron de
jugar: el criadero de ovejas, la orden de entregar con clic derecho sobre el castillo, y el
orden de dibujo dinámico.

**El alcance creció a mitad de semana y se decidió a conciencia.** El plan original dejaba el
castillo como simple punto de entrega. Se amplió a castillo seleccionable que entrena pawns,
adelantando trabajo de E05, porque sin producción la economía no cierra el bucle: recolectar
sin nada en qué gastar es un número que sube. Con producción, el oro se convierte en pawns y
los pawns en más oro, que es de lo que va el género.

### Lo que costó de verdad

**Los pawns recién entrenados no se movían.** Se seleccionaban, aceptaban la orden y no daban un
paso, sin un solo error por consola. La causa estaba en `PuedePasar`, que exigía que la celda de
**partida** fuera transitable: cualquier unidad que acabara sobre terreno bloqueado quedaba
encerrada para siempre, y los pawns nacían dentro del disco de 2,6 tiles del castillo. Se quitó
la comprobación de origen —entrar en una celda bloqueada lo sigue impidiendo la de destino— y
además la salida se ajusta a la celda pisable más cercana.

**El dibujo mentía sobre lo que llevaba el pawn, y eso hizo parecer rotos dos arreglos buenos.**
Al empezar a picar se le dice a la máquina «llevas madera», porque de esa tabla sale la
herramienta. Si se interrumpía a mitad, el pawn quedaba dibujado con el tronco al hombro sin
llevar nada. Se reportó como «puede coger otro recurso con uno en la mano» y como «el exploit
sigue»: los dos eran el mismo sprite mintiendo. Se sincroniza al cancelar.

**El exploit de los golpes.** Parar y reanudar a un pawn conservaba el progreso, así que picar a
tirones sacaba la carga en una fracción del tiempo. Razonado en el [ADR-13](ARQUITECTURA.md).
No se llegó a identificar con certeza cuál de las rutas de interrupción se dejaba el contador a
medias; se cerró la clase entera de fallo en vez de parchear una ruta: el contador exige que el
trabajo esté encendido, cancelar sale del estado en el acto, y el recolector pone el contador a
cero al empezar cada tanda.

**Los recursos bloqueaban cinco tiles en vez de uno.** Con radio 1,0 cada veta tapaba su celda y
las cuatro vecinas; como el oro va en bolsones apretados, seis vetas formaban un bloque macizo y
el pawn picaba desde el borde de la mancha, a dos tiles del oro. Ahora cada nodo tapa solo su
celda. El bosque denso sigue siendo un muro, que es lo que pide el GDD, pero deja de inflarse.

**Todos los pawns se ponían en el mismo lado del árbol.** `CeldaTransitableCercana` devuelve la
primera celda de su barrido, y el barrido recorre los anillos siempre en el mismo orden. Se
añadió una variante que recorre el anillo entero y elige la más cercana a quien viene.

**El pawn iba a una puerta que no existe.** Entregaba en un punto fijo bajo el castillo, así que
si volvía por el norte rodeaba el edificio entero. Ahora la distancia se mide al **borde** de la
huella —medida sobre el PNG: 4,88 × 3,25 unidades, centro 0,27 por debajo del centro del
lienzo— y el destino es el lado por el que llega.

**El empuje entre unidades era simétrico**, así que el que llegaba a un punto desplazaba al que
ya estaba plantado allí y el grupo se iba reptando. Ahora el que anda es el que se aparta.

### Decisiones tomadas

- **La carne se renueva y el oro no.** Una veta agotada empuja a salir a disputar la siguiente,
  que es el conflicto que el agotamiento existe para provocar. Con la carne el efecto sería el
  contrario: es un gasto continuo, y una despensa seca del todo no crea una decisión, crea una
  partida perdida sin nada que hacer. La carne es **renta**, no reserva.
- **La oveja cae de un golpe** y da 25 de carne. No se ordeña por viajes.
- **Sin piedra.** El pack no trae animación de pawn cargando piedra —solo oro, madera y carne—
  así que un cuarto recurso obligaría a un peón que vuelve de la cantera con las manos vacías.
  Además diluiría la carne, que es lo que distingue nuestra economía. Las rocas se quedan de
  decoración, canalizando el movimiento.
- **Sin población esta semana.** El GDD la fija en 5 iniciales y +5 por casa; con la escuadra de
  pruebas de diez unidades, el tope bloquearía la producción antes del primer pawn. Entra en la
  semana 06 junto con las casas, que es cuando existe algo con lo que subirla.
- **La decisión de recolectar vive fuera de la máquina de estados.** La máquina dice qué se
  dibuja; el recolector decide qué se hace. Mantiene el ADR-11 sin engordar el archivo.
- **`Economia.asset` no se sobrescribe al regenerar la escena**, al revés que el catálogo de
  unidades. Son los números que Raúl va a mover en la semana 16 y regenerar un mapa no puede
  borrarle una tarde de ajustes.
- **Una orden ya no le quita la carga al pawn.** Perdía lo recolectado, o sea que mover a un
  peón borraba en silencio varios segundos de trabajo.

### Andamio que hay que retirar
- El **poste de entrenamiento** y la **escuadra de diez unidades** por base. Siguen ahí a
  propósito: le dan al consumo de carne algo con qué morder. Se retiran en E05/E06.
- **`DeambularPawn` ya se borró**: era código muerto desde que llegó la máquina de estados.

### Pendiente
- HU-036, resaltado del nodo bajo el cursor.
- Población, construcción y el botón Construir (E05).
- El balance de la carne está sin calibrar: todo el ritmo cuelga de `ritmoSustento`.

---

## Semana 04 — Unidades y animación
**Entrega:** domingo 30/08/2026 · **Expo:** lunes 31/08/2026 · **Expone:** Raúl
**Tag:** [`v0.4.0-s04`](https://github.com/LOAD-13/tinyTactics/releases/tag/v0.4.0-s04)

### Lo prometido
HU-019 animador de estados · HU-020 máquina de estados · HU-021 las cinco unidades en los
cinco colores · HU-022 muerte y desaparición · HU-023 ataque visible · HU-024 lancero
direccional · HU-025 panel de acciones.

### Lo entregado
Las siete. Además, tres cosas que no estaban en la lista y salieron de probar el juego:
efectos de flecha y de curación, cooldown del monje, y avisos de comando al pasar el ratón.

### Lo que costó de verdad

**El fallo de la semana: la unidad moría y resucitaba en el mismo fotograma.** Al golpear al
poste de entrenamiento, el daño devuelto la mataba a mitad de `Update`; tres líneas más abajo,
el mismo `Update` llamaba a `Aplicar(Decidir())`, que devolvía *Reposo* y la revivía. Quedaban
de pie con cero de vida, sin poder seleccionarse y sin desvanecerse nunca.

Lo revelador es que **también escondió los otros fallos**: como la muerte quedaba a medias, ni
la nube de polvo ni la flecha ni el destello de curación se llegaban a ver, y se reportaron
como tres errores distintos cuando eran síntomas del mismo.

**Lección:** una máquina de estados necesita blindar el estado terminal *dentro* de la
transición, no solo en quien la llama. `Aplicar` ahora se niega a sacar a nadie de *Muriendo*.

**Los atajos de teclado pisaban la cámara.** La `S` de «panear abajo» era la misma que la de
Detener, así que bajar la vista paraba en seco a todo lo seleccionado; la `A` de «izquierda»
disparaba Atacar. Se resolvió quitando WASD del paneo y dejando solo las flechas, que es el
reparto de cualquier RTS.

**El ataque automático no atacaba, por dos motivos a la vez.** El primero, un orden de
operaciones: se encendía la vigilancia y acto seguido la orden de movimiento llamaba a
`Cancelar()`, que la apagaba. El segundo, más de fondo: el índice espacial responde con un
bloque de 3×3 cubos de dos unidades, o sea que **solo garantiza radio 2**, y se le estaba
pidiendo radio 5,5. Se añadió una consulta que agranda el bloque hasta cubrir el radio pedido.

**Un `CanvasGroup` que habría dejado los botones muertos.** El panel tenía `blocksRaycasts` e
`interactable` en `false` de cuando era decorativo. Un `CanvasGroup` así anula el raycast de
todos sus hijos: los botones se habrían dibujado perfectos y ninguno habría respondido, sin un
solo error en consola. Se detectó leyendo el código, no ejecutándolo.

**Los retratos estaban cruzados** entre lancero, arquero y monje. Otra vez por deducir en vez
de comprobar: supuse el orden mirando los cascos del pack. El orden real es el de sus carpetas
— Warrior, Lancer, Archer, Monk — con el Pawn al final.

### Decisiones tomadas

- **El pack no trae animación de muerte.** Comprobado buscando «death», «die» y «dead» en todo
  el paquete. Se resuelve sin arte nuevo: la unidad se apaga a gris, se desvanece y suelta una
  nube de polvo de `Particle FX`.
- **El golpe se cobra al terminar la animación**, no al emitir la orden. Es lo que hace que el
  ritmo lo marque el dibujo y no los clics del jugador.
- **Una orden de ataque persiste**: la unidad sigue golpeando sola hasta que el objetivo cae o
  llega otra orden. Repetir el clic sobre el mismo objetivo no reinicia nada.
- **Sin fuego amigo, nunca.** Es una regla del juego, no una comprobación defensiva.
- **El lancero direccional sale de cinco tiras más espejo.** El pack dibuja el lado derecho en
  cinco ángulos; las otras tres orientaciones se obtienen volteando el sprite.
- **El marco de comandos es independiente del de la unidad.** Los comandos son del jugador, no
  de la unidad seleccionada; meterlos en la misma caja los hacía parecer parte de la ficha.
- **Una rama por épica** a partir de ahora. Razonado en `GITFLOW.md` §5.4.

### Andamio que hay que retirar
- El **poste de entrenamiento** junto a cada base y su asset `MunecoDePruebas`. Existe solo
  para poder ver morir a una unidad propia sin enemigos reales. Se borra con la épica E06.
- La **escuadra de diez unidades por base**. En el juego real se empieza con dos pawns y todo
  lo demás se entrena: se cambia en la semana 06.

### Pendiente
- Combate de verdad: alcance en la persecución, cadencia, respuesta del atacado (E06).
- Que el botón Construir haga algo (E05/E06).

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
