# Modo partida libre y editor de mapas — requisitos

> **Qué es esto.** Los requisitos del modo de partida libre y su editor, recogidos tal como
> se pidieron para no reconstruirlos de memoria cuando llegue la épica E13 en las semanas
> 12-13. Ver [ADR-16](ARQUITECTURA.md#adr-16) y [ADR-17](ARQUITECTURA.md#adr-17).
>
> No es una lista de tareas: es la fuente de la que saldrán las HUs.

---

## 1. Qué es el modo partida libre

Un modo en el que el jugador **monta su propia partida**: elige mapa o lo dibuja, coloca lo
que quiera, decide de qué bando juega y lo cambia en caliente si le apetece.

El panel de trampas que existe desde la semana 07 es su primera mitad. La segunda es el
editor. Los dos acaban siendo la misma cosa desde el punto de vista del jugador: **una
partida en la que manda él**, no el generador.

Por eso el rótulo dice **PARTIDA LIBRE** y no «modo pruebas»: dentro de un modo publicado,
un cartel que diga «pruebas» se lee como algo sin terminar.

---

## 2. Ya construido

**Semana 07 — panel de partida libre v1**
- Cambiar de bando en caliente, con la cámara y el HUD siguiéndote.
- Regalar oro, madera y carne; vaciar el almacén.
- Inmortalidad por bando.
- Velocidad: pausa, x1, x2, x4.
- Aparecer unidades de cualquier tipo en el cursor.
- Ver el cartel de final y arrasar a los bandos rivales.
- Rótulo visible mientras el modo está activo.

**Semana 08 — primeros pinceles**
- Sembrar mena de oro, árboles, piedras, arbustos y ovejas.
- Borrador que quita el nodo y devuelve el terreno.
- Rueda para cambiar de variante.

---

## 3. Lo que falta — requisitos

### 3.1 Panel propio, abajo, acoplable

El editor **no va en el mismo panel** que la configuración de la partida. Es otro panel,
parecido en estilo, **en la parte inferior de la pantalla**.

- Se acopla al panel lateral de configuración, esté desplegado o retraído.
- **No interfiere con el panel de detalle de unidad.** Si el jugador está editando y
  selecciona una unidad, el editor se retrae y deja el panel de siempre.
- Todo con animación de despliegue y repliegue.

### 3.2 Categorías con iconos

Navegación visual, no una lista de botones de texto:

- Una categoría se representa con un icono —un árbol para recursos, por ejemplo— y al
  abrirla salen dentro los iconos de lo que contiene: oro, ovejas, piedras…
- Vale para todas las categorías: terreno, relieve, recursos, unidades, edificios.

### 3.3 Silueta de colocación

Al elegir algo, **se ve dónde se va a colocar antes de soltarlo**, igual que la silueta
verde/roja de la construcción de edificios. Hoy el pincel siembra a ciegas.

### 3.4 Terreno

- Pintar tierra y agua.
- **Tierras de varios colores.** El pack trae cuatro variantes de tileset.

### 3.5 Relieve

- Pintar desniveles.
- **Relieve sobre relieve**: más de un nivel de altura, no solo llano y meseta.
- Colocar escaleras entre niveles.

### 3.6 Generadores en vez de objetos sueltos

Las ovejas **no se colocan una a una**. Se coloca un **bloque generador** que las produce, y
el jugador configura **cada cuánto** aparecen.

Es el patrón correcto para cualquier cosa que se repueble, no solo para ovejas.

### 3.7 Unidades, edificios y puntos de inicio

- Colocar unidades de cualquier bando.
- Colocar edificios ya terminados, sin coste ni obra.
- **Definir el punto de comienzo** de cada bando.

### 3.8 Guardar, cargar y elegir

- Los mapas creados **se guardan**.
- Se pueden **seleccionar** al empezar una partida.

> Esto depende del menú principal, que es la semana 12. Guardar mapas sin ningún sitio donde
> elegirlos no le sirve a nadie, así que las dos cosas van juntas.

---

## 4. Rediseño de los paneles

Vale para el panel lateral y para el del editor, y **se adelanta a la semana 08** porque es
barato, se nota mucho y sirve igual para el HUD final de la semana 15.

- Retraído, un panel **no es una raya**: es un **botón que sobresale**.
- El lateral, en el centro del borde izquierdo. El del editor, en el centro del borde
  inferior.
- Al pulsarlo, el panel se despliega **con animación**. Hoy aparece de golpe.

---

## 5. Lo que hay que resolver antes del terreno

El pintado del tilemap y su paleta viven en `ConstructorDeMapa`, dentro del ensamblado de
**Editor**, que no existe en una build: usa `AssetDatabase` para cargar el tileset y resolver
el autotiling. Pintar tierra en caliente exige, en este orden:

1. **Mover paleta y autotiling a ejecución.** Un componente que guarde los sprites del
   tileset ya recortados y sepa qué pieza corresponde según los ocho vecinos. Es el grueso
   del trabajo.
2. **Repintar por parches.** Hoy el mapa se pinta entero de una vez. Un pincel tiene que
   repintar la celda tocada **y sus ocho vecinas**, porque al poner tierra cambian los bordes
   de las de al lado.
3. **Generalizar el relieve a varios niveles.** La grilla ya guarda la altura como número y
   el pathfinding ya permite un escalón. Falta que la pared se dibuje donde el vecino de
   arriba sea *más alto*, no solo donde el de abajo sea cero.

El punto 1 beneficia además al generador, que hoy no puede regenerar nada en partida.

---

## 6. Por qué merece la pena

El generador produce mapas **plausibles**; un editor produce mapas **diseñados**. Un cuello
de botella colocado con intención vale más que uno que salió del ruido.

Y abre trabajo técnico para Kiara, que hoy aporta sobre todo documentación y diseño — que es
la debilidad real del equipo frente al criterio de dominio técnico de la rúbrica.
