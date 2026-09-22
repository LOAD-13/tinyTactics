# Arquitectura técnica — Tiny Tactics

> Este documento registra **decisiones y su porqué**. Es la munición para las preguntas del docente:
> *"¿por qué eligieron esta solución y no otra?"* es una de las preguntas del balotario.

---

## 1. Visión general

```
┌─────────────────────────────────────────────────────┐
│  PRESENTACIÓN                                       │
│  Cámara · HUD · Minimapa · Menús · Animación        │
└───────────────────────┬─────────────────────────────┘
                        │ lee estado
┌───────────────────────▼─────────────────────────────┐
│  ENTRADA                                            │
│  Selección · Órdenes contextuales                   │
└───────────────────────┬─────────────────────────────┘
                        │ emite ÓRDENES (serializables)
┌───────────────────────▼─────────────────────────────┐
│  AUTORIDAD DE SIMULACIÓN                            │
│  Valida y aplica órdenes · Tick de simulación       │
└───────┬───────────────────────────────┬─────────────┘
        │                               │
┌───────▼─────────────┐   ┌─────────────▼─────────────┐
│  MUNDO              │   │  IA                       │
│  Grilla · Unidades  │   │  Estratégica (1 Hz)       │
│  Edificios · Recursos│  │  Táctica (3 Hz)           │
│  Niebla · Facciones │   │  (emite las mismas ÓRDENES)│
└─────────────────────┘   └───────────────────────────┘
```

**La idea que sostiene todo:** el jugador y la IA **no mueven unidades**. Ambos emiten
*órdenes* que la autoridad valida y aplica. Una unidad nunca lee el input directamente.

---

## 2. Decisiones

### ADR-01 · Toda acción es una orden serializable

**Decisión.** Las acciones del jugador se modelan como objetos `Orden` (mover, atacar, recolectar,
construir), no como llamadas directas a las unidades.

**Por qué.** Es lo único que mantiene abierta la puerta del PvP. Si las unidades leyeran el input
directamente, agregar red en la semana 12 significaría reescribir el core; con órdenes, significa
transportarlas por la red en vez de aplicarlas localmente.

**Beneficios colaterales:** es el **patrón Command** de manual (cubre la semana de patrones con
evidencia real), permite repetición de partidas, y hace que la IA use exactamente el mismo camino
que el jugador — si la IA puede hacer algo, el jugador también, y viceversa.

**Alternativa descartada.** Input directo a unidades. Más rápido de escribir hoy, muro infranqueable
en S12.

---

### ADR-02 · Movimiento: A* sobre grilla + interpolación + empuje blando

**Decisión.** El mapa es una grilla de tiles. El pathfinding es A* sobre esa grilla. Las unidades
**no saltan** de celda en celda: interpolan suavemente entre waypoints y se empujan blandamente
entre sí para no encimarse.

**Por qué.** Se ve como un RTS de verdad, pero cuesta como una grilla: barato, determinista y
depurable. Es exactamente lo que hacían los RTS 2D clásicos.

**Alternativas descartadas.**
- *NavMesh de Unity + RVO*: movimiento más orgánico, pero el NavMesh 2D es incómodo, la evasión
  local con 250 agentes es cara, **no es determinista** (mata el PvP) y depurar un atasco es un infierno.
- *Grilla estricta sin interpolar*: trivial, pero se ve rígido y desperdicia las animaciones de caminata del pack.

---

### ADR-03 · Prohibido el componente `Animator` en unidades

**Decisión.** Las unidades animan por **sprite-swap** con un script propio ligero.

**Por qué.** El `Animator` de Unity es el mayor costo por unidad a escala — del orden de ~0.1 ms
cada uno. Con 250 unidades es el primer cuello de botella del juego, y aparece justo en la
semana 13, cuando ya no hay tiempo para reescribir.

Las animaciones del pack son tiras de sprites simples; una máquina de estados propia con un índice
de frame y un temporizador hace exactamente lo mismo por una fracción del costo.

---

### ADR-04 · Grilla espacial para búsqueda de objetivos

**Decisión.** Las unidades se registran en una grilla espacial (spatial hash). Buscar enemigo
cercano consulta solo las celdas vecinas.

**Por qué.** La alternativa ingenua — recorrer todas las unidades por cada unidad — es O(n²).
Con 250 unidades son 62 500 comparaciones **por frame**. El juego se cae mucho antes de llegar
al alcance planeado.

**Prohibido:** `FindObjectsOfType` y `GameObject.Find` en cualquier código que corra por frame.

---

### ADR-05 · Pathfinding en cola

**Decisión.** Las peticiones de ruta entran a una cola y se resuelven **N por frame**.
Los grupos grandes comparten un *flow field* en vez de calcular una ruta por unidad.

**Por qué.** Una orden de movimiento sobre 50 unidades seleccionadas dispararía 50 A* en el mismo
frame y produciría un tirón visible. Repartirlo en varios frames es imperceptible para el jugador.

---

### ADR-06 · IA por capas, dificultad por economía

**Decisión.** Tres capas: estratégica (~1 Hz), táctica (~3 Hz), FSM por unidad (por frame).
Las dificultades se implementan como **multiplicador de recursos + timing de oleadas**.

**Por qué.** Es como funcionaban de verdad Warcraft, StarCraft y Age of Empires — que corrían
7 bots en hardware de los 90. El cerebro de la IA es barato precisamente porque **no piensa
cada frame**: piensa una vez por segundo, y eso basta para un RTS.

Y el dato incómodo pero cierto: en los RTS clásicos la dificultad alta **no es más inteligente,
hace trampa**. Recibe bonus de recursos y construye más rápido. Implementar "una IA más lista"
es un problema de investigación abierto; implementar tres multiplicadores es una tarde.

**Pendiente:** spike de investigación con fuentes antes de S11.

---

### ADR-07 · Datos de balance en `ScriptableObject`

**Decisión.** Stats de unidades, costos de edificios y parámetros de dificultad viven en
`ScriptableObject`, nunca como números en el código.

**Por qué.** La semana 16 es de balance puro. Si cada ajuste exige recompilar, se harán diez
iteraciones; si se editan desde el inspector, se harán cien. Además permite que Raúl ajuste
balance sin tocar código.

---

### ADR-08 · Sin física de Unity en las unidades

**Decisión.** Nada de `Rigidbody2D` para el movimiento de unidades. Las colisiones entre unidades
se resuelven a mano (empuje blando) y los proyectiles siguen una trayectoria calculada.

**Por qué.** `Rigidbody2D` introduce no-determinismo y comportamiento impredecible con 250 cuerpos.
Un RTS necesita que una unidad llegue exactamente donde se le ordenó.

**Nota para la sustentación:** esto **no** significa que el proyecto no aplique física. La aplica
por scripts — trayectoria de proyectiles y resolución de colisiones — que es un ejercicio más
profundo que colgar un `Rigidbody2D` y dejar que el motor decida.

---

### ADR-09 · El mapa se genera por código, no se pinta

**Decisión.** Los mapas se generan desde una `DefinicionMapa` mediante un script de editor,
en vez de pintarse a mano en el Tilemap.

**Por qué.** En un RTS conviven dos representaciones del mapa: la **visual** (tilemap) y la
**lógica** (grilla de transitabilidad para A*). Si se mantienen por separado, tarde o temprano
se desincronizan y aparecen unidades caminando sobre el agua — un bug que además es carísimo
de encontrar. Generando ambas desde la misma máscara, esa clase de fallo es estructuralmente
imposible.

**Beneficios colaterales.** Los tres mapas del proyecto son cambios de parámetros, no de
trabajo manual repetido. Y el diseño es reproducible: la misma semilla da siempre el mismo
mapa, así que un problema de balance se puede volver a mirar exactamente igual.

**Cómo se logra la simetría.** El ruido no se muestrea en la posición de la celda sino en su
posición **plegada**: se convierte a polares y el ángulo se dobla dentro del primer sector de
360/N grados, con espejo para que no haya costura en la frontera. Resultado: los N bandos
reciben terreno idéntico y ninguno arranca en desventaja.

**Alternativa descartada.** Pintar a mano. Solo lo puede hacer una persona, obliga a mantener
la grilla lógica en paralelo, y rehacer un mapa cuesta lo mismo que hacerlo la primera vez.

> Lecciones caras de esta implementación, documentadas en [`BITACORA.md`](BITACORA.md):
> multiplicar el ruido por la caída radial siempre produce un círculo (hay que interpolar),
> y un margen de agua cuadrado rompe la simetría radial.

---

### ADR-20 · La niebla es una regla de juego, no un filtro de imagen

**Decisión.** La visibilidad **condiciona la simulación y la entrada**, no solo el dibujado.
Una unidad no se engancha por su cuenta a un enemigo que su bando no está viendo, una torre no
le dispara, el cursor no lo señala y el jugador no puede ordenar un ataque sobre él.

**Por qué.** Una niebla que solo oscurece píxeles es decorado: el ejército enemigo sigue
tirando de tus tropas desde dentro de la oscuridad y tus torres siguen acertándole a lo que no
ves. Peor aún, el cursor se convertiría en un detector de enemigos — bastaría barrer el negro
con el ratón para saber qué hay detrás.

**Lo que hace que esto no mueva el balance.** Una regla nueva sobre un combate recién ajustado
es un riesgo real, y se cierra con un invariante: **el radio de visión de toda unidad es mayor
que su alcance de ataque y que su radio de vigilancia.** Con eso, ninguna unidad pierde un
objetivo que antes alcanzaba, así que la conducta observable en un combate no cambia — cambia
lo que pasa *fuera* del combate, que es justo lo que se quería añadir.

El invariante no se queda en este párrafo: `CatalogoDeUnidades` lo **comprueba** al reconstruir
las fichas y avisa por consola si alguna lo rompe. Es la respuesta directa al fallo de la
semana 08, donde dos medidas correctas por separado se rompieron porque nadie las había medido
una contra otra.

**Los edificios se recuerdan; las unidades no.** Para atacar un edificio basta haberlo
descubierto; para atacar una unidad hay que estar viéndola. La diferencia no es un capricho: un
edificio no se mueve, así que el recuerdo sigue siendo cierto, mientras que el fantasma de una
unidad en el sitio donde estaba hace diez segundos es información falsa.

**Y un edificio se recuerda por haberlo VISTO, no por estar en terreno explorado.** La memoria
vive en el edificio —un entero, un bit por bando— y no en la grilla. Atarla al terreno parece
equivalente y no lo es: en cuanto una zona está explorada, cualquier cosa que el rival levante
ahí aparecería sola en el momento de terminarla, sin que nadie hubiera ido a mirar.

**Las bases de salida se ven desde el primer fotograma, aunque estén en sombra.** Solo las bases:
el terreno sigue sin explorar y las unidades siguen sin verse. Lo único que se regala es **dónde**
empieza cada rival —un dato que en un mapa simétrico el jugador puede deducir mirando el suyo— y
no cuántos son ni qué están construyendo.

> **Se probó la versión grande de esto y se descartó jugándola.** Empezar con el mapa entero
> explorado hacía la partida más cómoda, pero la niebla dejaba de contar nada y el minimapa pasaba
> a ser una foto completa del mapa desde el segundo cero. Lo que hacía falta era mucho menos: ver
> dónde está cada rival, no qué hay entre medias.

**Alcance de esta semana.** La regla se aplica a todos los bandos por igual, pero hoy solo hay
un bando con quien la note. Si la IA de la semana 10 debe respetarla o no —y con qué
dificultad— es la decisión que ya estaba puesta en la semana 11.

---

### ADR-19 · Lo que oscurece el mapa se multiplica por encima; no se tiñe sprite a sprite

**Decisión.** La niebla de guerra y el ciclo del día oscurecen mediante **láminas por encima
del mapa con mezcla multiplicativa** (`Blend DstColor Zero`), no bajándole el color a cada
`SpriteRenderer`.

**Por qué multiplicar y no pintar encima.** Un negro semitransparente sobre un guerrero azul da
una mancha gris con forma de guerrero; multiplicar da un guerrero azul apagado. En pixel art la
diferencia salta a la vista, porque el contorno negro del pack deja de ser negro y el sprite
pierde el borde que lo separa del fondo.

**Por qué por encima y no en cada sprite.** Es el [ADR-11](#adr-11) otra vez: la máquina de
estados es la única dueña del color de su unidad, y ahí viven el destello del impacto y el
desvanecido de la muerte. Con la noche escribiendo el mismo campo, gana el último que escriba —
o la noche apaga el destello, o el destello enciende la noche. Una lámina por encima no necesita
permiso de nadie y además funciona igual sobre el terreno, los recursos y las nubes, que no
tienen máquina de estados ninguna.

**Regla derivada, y es la general:** *un campo, un dueño*. Cuando la niebla necesitó tapar
unidades enteras no tocó `enabled` —que ya tienen dueño: la selección y la barra de vida— sino
`forceRenderingOff`, que existe justo para vetos de visibilidad y que no usa nadie más.

---

### ADR-18 · La niebla es una textura de un píxel por celda: dura en el espacio, suave en el tiempo

**Decisión.** El estado de visibilidad se vuelca en una `Texture2D` de **un píxel por celda con
filtrado de punto**, estirada sobre un único sprite que cubre el mapa. El borde es un escalón de
un tile, y lo que se suaviza es **cuándo** cambia cada tile, no su forma.

**Por qué una textura.** El mapa por defecto mide 224×224: **50 176 celdas**. Un objeto con su
`SpriteRenderer` por celda son cincuenta mil objetos que el motor recorre, ordena y dibuja cada
fotograma para tapar algo que casi nunca cambia. Un `byte[]` de 50 KB se recorre entero en una
fracción de milisegundo y se sube de una vez.

**Por qué filtrado de punto, que es la parte estética.** Un difuminado suave sobre pixel art se
delata: sus bordes no caen en la misma rejilla que los tiles y su gris no sale de la paleta del
pack. Con punto, el borde de la niebla es un escalón de un tile alineado con el tileset.

**Lo que quita la dureza es el tiempo, no el espacio.** Cada celda guarda un número de 0 a 3
que persigue a su estado real, y el color sale de interpolar entre los tintes con ese número: al
descubrirse, una celda recorre los tres peldaños en medio segundo en vez de encenderse de golpe.
El dibujado corre cada fotograma; **qué se ve** se recalcula solo diez veces por segundo. Con los
dos ritmos juntos, las celdas se abrían a saltos de diez por segundo y se veía el cuadriculado
encendiéndose — no molestaba el cuadrado, molestaba el parpadeo. El desvanecido no cambia ni un
tile de lo que la unidad ve; solo cuánto tarda en enterarse el jugador.

**Lo que no se ve se oscurece, no se tapa.** Lo nunca explorado deja leer el terreno por debajo.
Una partida que empieza sin poder ver la forma del mapa agobia en vez de intrigar, y lo que la
niebla tiene que esconder son los ejércitos, no la costa.

**El manto de nubes: construido, probado y apagado.** Se implementó tapar lo inexplorado con las
ocho nubes pintadas a mano del pack —repartidas por rejilla, recicladas y balanceándose en el
sitio— con el argumento de que la frontera de la niebla la dibujara el mismo artista que dibujó
el bosque de al lado. **Jugado, tapaba demasiado:** la partida empezaba sin poder leer nada. El
código se queda y el interruptor está en el panel de partida libre, apagado por defecto: apagar
algo que funciona es reversible y borrarlo no, y la idea sigue sirviendo para un modo de
exploración de verdad. La deriva de nubes sobre el mapa descubierto sí se queda, que es ambiente
y no información.

**Tres estados y no dos.** Nunca visto · explorado sin vigilancia · a la vista. El intermedio no
es adorno: sin él, el mapa se cierra en negro detrás de cada patrulla y el jugador pierde la
costa y los bosques que ya había pagado por descubrir.

---

### ADR-17 · El editor de mapas se queda en la semana 13

**Decisión.** La épica **E13 · Editor de mapas** se mantiene donde estaba: semanas 12-13. Se
evaluó adelantarla a la 09 y **se descartó**. El calendario no se toca.

**Contexto.** En la semana 08 se construyó el primer trozo del editor —pinceles de recursos
dentro del panel de partida libre— y quedó claro que el editor completo (panel propio, pinceles
de terreno, relieve multinivel, escaleras, generadores configurables) es una semana entera de
trabajo, no un añadido.

**Se consideró adelantarlo a la 09, y el argumento a favor era bueno.** El motivo original para
ponerlo en la 12 era explícito en el backlog: *«un editor edita un formato de datos, y ese
formato todavía se está moviendo»*. Ese riesgo ya no existe — `DefinicionMapa` lleva estable
desde la semana 04, la grilla guarda la altura desde la 03 y los edificios con celdas desde la
06. La objeción que justificaba esperar se había caído sola.

**Por qué se descarta igualmente.** Adelantarlo obligaba a correr la niebla a la 10 y la IA
rival a la 11, y eso deja a la IA con **una sola semana antes de PC2** en vez de dos. PC2 es la
semana 10, vale el 20 % de la nota y pide textualmente *«una partida completa jugable contra un
bot»*. El editor no puntúa en PC2; el bot sí.

Dicho de otro modo: el argumento técnico para adelantar era válido, pero **el riesgo no era
técnico, era de calendario**. Y un riesgo de calendario no se resuelve con un buen argumento
técnico.

**Qué se hace mientras tanto.** Los pinceles de recursos de la semana 08 se quedan, y en la
semana 08 se adelanta también el **rediseño de los paneles** —botón saliente y animación de
despliegue—, que es barato, se nota mucho en pantalla y sirve igual para el HUD final de la
semana 15. Lo caro espera.

Los requisitos completos del editor, tal como los describió Joaquín, quedan recogidos en
[`SANDBOX.md`](SANDBOX.md) para no reconstruirlos de memoria dentro de cuatro semanas.

**El bloqueo técnico que habrá que resolver en la 13.** El pintado del tilemap y su paleta viven
en `ConstructorDeMapa`, dentro del ensamblado de **Editor**, que no existe en una build: usa
`AssetDatabase` para cargar el tileset y resolver el autotiling. Pintar tierra en caliente
exige, en este orden:

1. **Mover paleta y autotiling a ejecución** — un componente que guarde los sprites del tileset
   ya recortados y sepa qué pieza corresponde según los ocho vecinos. Es el grueso.
2. **Repintar por parches** — hoy se pinta el mapa entero de una vez; un pincel tiene que
   repintar la celda tocada **y sus ocho vecinas**, porque al poner tierra cambian los bordes de
   las de al lado.
3. **Relieve de varios niveles** — la grilla ya guarda la altura como número y el pathfinding ya
   permite un escalón, así que es generalizar el dibujado de la pared: se pinta donde el vecino
   de arriba sea *más alto*, no solo donde el de abajo sea cero.

El punto 1 beneficia además al generador, que hoy no puede regenerar nada en partida.

---

### ADR-16 · El panel de pruebas es una herramienta transversal, no una épica

**Decisión.** El panel de trampas (`PanelDePruebas`) no pertenece a ninguna épica. Nace en la
E06 con lo mínimo y **cada épica le añade sus propios interruptores**: la E07 quitar la niebla,
la E08 pausar la IA y ver su plan. Se destruye solo fuera del editor y de las builds de
desarrollo, y mientras está abierto pinta un sello **MODO PRUEBAS** en pantalla.

**Por qué no es una épica.** Una épica entrega juego; esto entrega herramienta. Una semana
cuya exposición fuera «miren nuestro panel de depuración» sería la peor del semestre. Y una
épica de sandbox colocada en el calendario tendría que adivinar hoy qué interruptores va a
necesitar la IA dentro de tres semanas; siempre adivinaría mal.

**Por qué en la semana 07 y no más tarde.** No hay IA rival hasta la semana 10, así que **sin
poder cambiar de bando no hay forma de enseñar un combate de dos lados**. El panel no es un
extra de la E06: es lo que hace demostrable el resto de la E06, y es lo que permite retirar el
poste de entrenamiento sin quedarse sin banco de pruebas.

**El sello es obligatorio.** Sin él, cualquier captura de una entrega podría haberse hecho con
oro regalado y unidades inmortales, y no habría forma de distinguirlo. El sello hace que una
captura tramposa se delate sola.

**Se apaga en `Awake`, no con `#if` alrededor de la clase.** Fue lo primero que se intentó:
con la clase compilada a medias, cada sitio que le pregunta algo —el cursor, el selector—
necesita su propio `#if`, y basta olvidar uno para que la build de entrega deje de compilar.
La garantía se pone en un sitio, no en cinco.

---

### ADR-15 · Lo que se puede atacar se esconde detrás de una interfaz

**Decisión.** Unidades y edificios implementan `IObjetivo`: facción, si sigue vivo, dónde está,
a qué distancia queda **de su borde** y cómo recibir daño. La máquina de estados guarda su
objetivo como `IObjetivo` y no distingue a qué le está pegando.

**Las alternativas eran peores.** Duplicar la persecución, el alcance, la cadencia y el efecto
para edificios son dos caminos que resuelven lo mismo y que se desincronizan a la primera
corrección. Hacer que el edificio heredara de `Unidad` es peor todavía: arrastraría velocidad,
hambre, rutas y empuje, todo a cero y todo estorbando.

**La distancia se mide distinto en cada uno, y es deliberado.** El edificio mide a su borde:
un castillo de cinco casillas medido al centro sería inalcanzable, porque bloquea sus propias
celdas y la unidad se quedaría empujando el muro. La unidad mide entre centros, sin restar el
radio, porque los alcances de las cinco unidades están ajustados contra esa vara desde la
semana 04 y cambiarla alargaría el cuerpo a cuerpo medio tile sin que nadie lo pidiera.

> ⚠️ **El `null` falso de Unity se pierde al guardar la referencia como interfaz.** Unity finge
> que un objeto destruido es `null`, pero ese truco vive en `UnityEngine.Object`: con una
> referencia de tipo interfaz la comparación pasa a ser la de C# y un objeto ya destruido sigue
> dando «no nulo». Es **exactamente** el fallo que en la semana 06 dejó a un pawn plantado tras
> matar una oveja, disfrazado de otra cosa. Por eso toda comprobación pasa por `Existe()`, que
> vuelve a convertir a `MonoBehaviour` para que la comparación sea otra vez la de Unity.

---

### ADR-14 · Un edificio tiene dos medidas: el dibujo y la planta

**Decisión.** Cada edificio guarda dos tamaños distintos. La **huella** es el recuadro de
píxeles opacos de su PNG, en decimales, medida por la herramienta del editor. La **planta** son
las celdas que ocupa en el suelo, en tiles enteros, escrita a mano en la ficha.

**Por qué dos y no una.** Los edificios del pack están dibujados en perspectiva: se les ve la
fachada. El monasterio mide 4,14 tiles de alto porque tiene una aguja, y su planta no llega a
tres. Con una sola medida hay que elegir cuál se sacrifica, y las dos opciones son malas: usar
el dibujo para bloquear terreno pediría cuatro tiles libres para colocar algo que cabe en tres
—imposible de construir en media base—, y usar la planta para medir distancias dejaría al pawn
entregando a través del muro.

Cada una se usa donde significa algo. La huella mide distancias al borde, estira el corchete de
selección y sitúa el retrato. La planta bloquea la grilla y dibuja la silueta verde.

**La planta va en tiles enteros** porque la silueta se ajusta a la rejilla. Un edificio que
ocupara 2,88 celdas dejaría un doceavo de celda pisable que ninguna unidad podría usar y que el
pathfinding tendría que seguir considerando.

**Consecuencia.** La posición del objeto **sale del rectángulo de celdas**, nunca al revés. Es
lo que garantiza que la silueta que el jugador vio y el terreno que acaba bloqueado sean
literalmente el mismo dato: si la silueta se colocara «donde está el ratón» y el edificio «donde
dicen las celdas», coincidirían casi siempre y discreparían medio tile justo en los bordes, que
es donde el jugador mira.

**Lo que evita.** Las huellas se **miden**, no se estiman — la regla que se ganó a pulso en la
semana 03 leyendo un tileset a ojo tres veces seguidas y en la 05 con el corchete del castillo.
La medición automática reprodujo exactamente el 4,88 × 3,25 que se había sacado a mano.

---

### ADR-13 · El progreso de una acción se cuenta en golpes, no en segundos

**Decisión.** Lo que cuesta recolectar una carga se mide en **pasadas completas de la
animación**, no en un temporizador. El animador avisa cada vez que una tira en bucle da la
vuelta, la máquina de estados lleva la cuenta, y el contador se borra en cuanto la unidad
cambia de estado.

**Por qué.** La primera versión tenía **dos relojes independientes**: un cronómetro decidía
cuándo salía el recurso y la animación del hacha corría por su cuenta como decoración. Dos
relojes que no se hablan siempre se pueden desincronizar, y una desincronización en una
mecánica de recursos es una ventaja explotable: parar y reanudar al pawn conservaba el rato ya
invertido y sacaba la carga en una fracción del tiempo.

Es además la misma regla que el [ADR-11](#adr-11) ya había fijado para el combate —*el golpe se
cobra cuando la animación termina*— y que la recolección se había saltado.

**Consecuencia.** Un golpe a medias no cuenta para nada, así que no hay forma de acumular
progreso a trocitos. Y los datos de balance hablan en la unidad que el jugador ve: «seis
hachazos», no «tres segundos».

**Detalle que casi lo rompe.** Las animaciones en bucle arrancan en un fotograma al azar, a
propósito, para que un grupo de unidades paradas no se vea sincronizado como un ejército de
clones. Con el conteo activo, eso haría que el primer hachazo valiera medio golpe: *trabajando*
queda fuera del desfase.

---

### ADR-12 · La profundidad de dibujo se recalcula en todo lo que se mueve

**Decisión.** El orden de dibujo sale de la coordenada Y, con **diez subdivisiones por tile**.
Lo quieto lo resuelve el generador una sola vez; todo lo que se mueve lleva un componente que
lo recalcula en `LateUpdate`.

**Por qué.** El orden se fijaba al crear cada objeto y no se volvía a tocar. Vale para un árbol
y no vale para algo que anda: un pawn que rodeaba el castillo por detrás conservaba el orden que
tenía al nacer y se seguía pintando delante del tejado, con aspecto de estar volando.

**Por qué subdividido.** Sin subdividir, dos unidades dentro del mismo tile empatan, y un empate
en `sortingOrder` deja el orden en manos del motor: dos unidades pegadas parpadean
intercambiándose. Con diez pasos por tile la profundidad se resuelve a nivel de decímetro.
**Todo** lo que se dibuja en el mundo usa la misma escala; mezclar dos escalas equivale a no
ordenar.

**Los edificios se ordenan por su base, no por su centro.** Un castillo mide tres tiles de alto:
usando el centro de su dibujo, todo lo que pasara por delante de la puerta se dibujaba detrás
del muro. Un edificio ocluye según **dónde se apoya**.

**Coste.** Una resta y un redondeo por objeto móvil y fotograma, y solo se escribe en el
renderizador cuando el número cambia de verdad — tocar `sortingOrder` rompe el lote de dibujado.

---

### ADR-11 · Una máquina de estados por unidad, y el estado terminal se blinda dentro

**Decisión.** Cada unidad tiene un único componente dueño de su estado —reposo, moviendo,
atacando, muriendo— que decide las transiciones y le dice al animador qué dibujar. Ningún otro
componente toca la animación.

**Por qué.** Antes la animación se decidía en `MovimientoUnidad`, que alternaba entre reposo y
caminar por su cuenta. Con dos estados el apaño funciona; con cuatro deja de escalar, porque
nadie sabe quién manda cuando una unidad muere mientras camina. Un solo dueño y una sola
función de transición hacen que la respuesta sea siempre localizable.

**La parte que costó una tarde.** *Morir* es un estado terminal, y hay que blindarlo **dentro
de la transición**, no solo en quien la llama. El daño puede aplicarse a mitad de `Update` y
matar a la unidad; si la propia función de transición no lo respeta, el mismo fotograma la
devuelve a *reposo* y queda una unidad de pie con cero de vida que no se puede seleccionar ni
desaparece nunca. Es un fallo silencioso: no lanza excepción y además esconde otros.

**La duración del golpe sale de la animación.** El animador avisa cuando una tira sin bucle
llega al final, y la máquina vuelve a reposo con ese aviso. La alternativa —un temporizador con
un número fijo— habría que mantenerla a mano cada vez que se ajuste un fps.

**Consecuencia para el combate.** Cuando llegue la épica E06, lo que cambia es *qué hace* el
estado atacando, no quién lo enciende: la orden ya entra por la autoridad (ADR-01) y la máquina
ya sabe acercarse hasta el alcance y golpear al ritmo del dibujo.

**Alternativa descartada.** El componente `Animator` de Unity, que además de resolver esto
traería su propio grafo de estados. Prohibido por el ADR-03 por coste a escala, y aquí tampoco
haría falta: cuatro estados no justifican una máquina visual.

---

### ADR-10 · El relieve se dibuja a mano; el ruido solo propone

**Decisión.** Las alturas y las rampas se guardan en un asset `RelieveMapa` que se dibuja con
un pincel propio en la vista de escena. Si ese asset existe y encaja con el tamaño del mapa, el
generador lo copia tal cual en vez de calcular ruido.

**Por qué.** Esto es una excepción deliberada al ADR-09, y conviene entender por qué no lo
contradice. La costa, los recursos y la decoración son **textura**: da igual el árbol concreto,
lo que importa es la distribución, y ahí el ruido acierta. Un acantilado no es textura: es un
**embudo**. Decide por dónde pasa un ataque, qué posición se puede defender con la mitad de
unidades y si una expansión es tomable. Eso es diseño de nivel, y el ruido no tiene criterio.

También resuelve un problema práctico: regenerar la escena es una operación cotidiana —se hace
cada vez que cambia el generador o los prefabs— y sin el asset, cada regeneración cambiaba el
escenario. No se puede balancear contra un mapa que se mueve.

**Qué queda fuera del asset.** Solo alturas y rampas. Todo lo demás sigue saliendo de la
semilla, que es determinista: mismo mapa, mismo terreno. El asset es un **parche encima**, no
una copia del mapa, y por eso ocupa 50 KB en vez de varios megas.

**Consecuencia buscada.** El pincel es el germen del editor de mapas de E13 (semanas 12-13).
Al llegar allí, el trabajo de pintar sobre la escena y persistir a un asset ya estará hecho y
probado; faltará extenderlo a recursos y puntos de aparición.

**Alternativa descartada.** Ajustar los parámetros del ruido hasta que salieran mesetas
razonables. Se probó: mueve el problema de sitio, porque un umbral que funciona en un mapa no
funciona en otro, y sigue sin poder decidir *dónde* va la subida.

---

## 3. Estructura de carpetas

```
Assets/
├── Scripts/
│   ├── Nucleo/          Autoridad de simulación, órdenes, economía, población
│   ├── Mundo/           Grilla, mapa, recursos, profundidad de dibujo
│   ├── Unidades/        FSM, stats, animación por sprite-swap, recolector, constructor
│   ├── Movimiento/      A*, cola de rutas, flow field, empuje
│   ├── Edificios/       Edificio, producción, colocación, obra, catálogo
│   ├── Combate/         Daño, targeting, proyectiles, curación
│   ├── IA/              Capa estratégica, táctica, dificultades
│   ├── Entrada/         Selección, órdenes contextuales, cámara
│   ├── Interfaz/        HUD, panel, cursor, resaltado, minimapa
│   ├── Editor/          Generadores de escena, interfaz y catálogos
│   └── Datos/           ScriptableObjects de balance
├── Prefabs/             Unidades, edificios, proyectiles, efectos
├── Scenes/              Menu, Juego, mapas
├── Datos/               Instancias de ScriptableObject
└── Tiny Swords/         Assets originales del pack — NO se modifican
```

**Regla:** `Assets/Tiny Swords/` se deja intacto. Todo lo derivado (prefabs, animaciones,
atlas) vive fuera. Así se puede actualizar el pack sin perder trabajo.

---

## 4. Presupuesto de rendimiento

| Métrica | Objetivo |
|---|---|
| Unidades simultáneas | 250 (5 bandos × 50) |
| Framerate | ≥ 60 fps con 250 unidades en combate |
| Tick de simulación | 20 Hz, desacoplado del framerate |
| Tick estratégico de IA | 1 Hz por bando |
| Rutas A* por frame | ≤ 8 |

Estos números son **criterios de aceptación**, no aspiraciones: aparecen en las HUs
correspondientes y se verifican con el profiler.
