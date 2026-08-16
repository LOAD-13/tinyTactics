# Tiny Tactics — Documento de Diseño del Juego

Curso: Diseño y Desarrollo de Juegos Interactivos II (100000S11F) · Ciclo 2026-2 Agosto
Equipo: Joaquín Loa Denegri · Kiara Santti Saavedra · Gerardo Raúl Socualaya Mandamiento

---

## 1. Ficha conceptual

| Campo | Valor |
|---|---|
| **Nombre** | Tiny Tactics |
| **Género** | RTS — Estrategia en tiempo real |
| **Perspectiva** | Cenital (top-down) sobre grilla de tiles |
| **Estilo visual** | Pixel art — pack *Tiny Swords* (Pixel Frog) |
| **Motor** | Unity 6000.5.8f1 · 2D URP |
| **Plataforma** | PC (Windows). Build final jugable |
| **Modo** | Un jugador contra IA. FFA de hasta 5 bandos |
| **Público objetivo** | Jugadores de 14+ familiarizados con estrategia clásica (Warcraft, Age of Empires) |
| **Duración de partida** | 10-25 min según número de bandos |

### La experiencia que proponemos

El jugador empieza con un castillo, una casa y dos pawns en una esquina del mapa.
Debe **expandir su economía** antes de que los rivales expandan la suya, **traducir esa economía
en ejército** y **destruir el castillo enemigo** — todo bajo la presión de no saber qué está
haciendo el rival, porque la niebla de guerra le oculta el mapa.

La tensión central es la clásica del género: **cada pawn que mandas a recolectar es un soldado
que no entrenaste**, y cada minuto que dedicas a economía es un minuto en que el rival pudo
haber construido su ataque.

---

## 2. Reglas del juego

### Condición de victoria y derrota

- **Derrota:** tu castillo es destruido.
- **Victoria:** eres el último bando con castillo en pie.
- En FFA no hay alianzas: todos contra todos.

### Bucle de juego

```
Recolectar recursos  →  Construir edificios  →  Subir población
        ↑                                              ↓
        └──────  Atacar y defender  ←  Entrenar tropas ┘
```

---

## 3. Recursos

| Recurso | Se obtiene de | Se gasta en |
|---|---|---|
| 🪙 **Oro** | Vetas de oro del mapa | Entrenar unidades, mejoras |
| 🪵 **Madera** | Árboles | Construir y mejorar edificios |
| 🍖 **Carne** | Ovejas del mapa | **Sustento**: consumo pasivo del ejército |

**Mecánica de la carne.** Cada unidad viva consume carne lentamente. Si la reserva llega a cero,
las tropas empiezan a perder efectividad (daño y velocidad reducidos) hasta que se reponga.

Esto obliga a mantener economía **durante** la guerra en vez de gastar todo en un ataque único,
y le da a las ovejas del pack un propósito real en vez de decoración.

> ⚠️ La tasa de consumo es un **parámetro de balance**, se ajusta durante el desarrollo
> (ver `docs/CRONOGRAMA.md`, semana 16). Los valores iniciales son punto de partida, no verdad.

### Ciclo de recolección

El pawn camina al recurso → recolecta durante N segundos → carga un tope → **vuelve al castillo**
a depositar → repite. El viaje de ida y vuelta es lo que hace que la posición del castillo importe.

---

## 4. Unidades

Todas existen en los 5 colores de facción del pack.

| Unidad | Sprite del pack | Rol | HP | Daño | Alcance | Vel. | Costo | Carne/s |
|---|---|---|---|---|---|---|---|---|
| **Pawn** | `Pawn` | Recolecta y construye | 60 | 5 | 0.5 | 3.0 | 50 🪙 | 0.10 |
| **Guerrero** | `Warrior` | Melee equilibrado, tanque | 140 | 18 | 0.8 | 2.6 | 90 🪙 + 10 🪵 | 0.20 |
| **Lancero** | `Lancer` | Melee con alcance, alto daño | 100 | 22 | 1.6 | 2.8 | 80 🪙 | 0.20 |
| **Arquero** | `Archer` | Daño a distancia, frágil | 70 | 14 | 5.0 | 2.9 | 85 🪙 + 20 🪵 | 0.15 |
| **Monje** | `Monk` | Soporte, cura aliados | 65 | −20 (cura) | 3.5 | 2.7 | 120 🪙 | 0.15 |

> **Valores iniciales, sujetos a balance.** Viven en `ScriptableObject`, no en código.
>
> **Nomenclatura:** los nombres de dominio en español son los de este documento; los nombres
> en inglés son los de las carpetas del pack Tiny Swords. Se mantienen ambos a propósito:
> el código usa el nombre en español, las rutas de assets conservan el original del pack.

### Triángulo de contadores

```
            ┌───────── vence a ─────────▶ Guerrero
            │                                 │
         Lancero                          vence a
            ▲                                 │
            │                                 ▼
            └───────── vence a ────────── Arquero
```

- **Lancero > Guerrero** — su alcance mayor le permite pegar primero.
- **Guerrero > Arquero** — resiste el camino y lo aplasta en cuerpo a cuerpo.
- **Arquero > Lancero** — lo castiga desde fuera de su alcance.
- **Monje** — fuera del triángulo. Multiplica el valor de cualquier composición, pero muere solo.

El objetivo de diseño es que **ninguna composición pura gane**: un ejército de solo guerreros
debe perder contra uno mixto de costo equivalente.

---

## 5. Edificios

| Edificio | Sprite del pack | Costo | Función |
|---|---|---|---|
| **Castillo** | `Castle` | — (inicial) | Centro de entrega de recursos. Entrena pawns. Su destrucción = derrota. Aporta 5 de población |
| **Casa** | `House1-3` | 60 🪵 | +5 al límite de población |
| **Cuartel** | `Barracks` | 120 🪵 + 50 🪙 | Entrena guerreros y lanceros |
| **Campo de tiro** | `Archery` | 100 🪵 + 70 🪙 | Entrena arqueros |
| **Monasterio** | `Monastery` | 150 🪵 + 100 🪙 | Entrena monjes |
| **Torre** | `Tower` | 80 🪵 + 40 🪙 | Defensa estática, ataque a distancia |

**Población:** empieza en 5 (castillo), +5 por casa, **tope duro de 50 unidades por bando**.
Ese tope no es diseño arbitrario: es el presupuesto de rendimiento (5 bandos × 50 = 250 unidades).

**Construcción:** el jugador elige el edificio, aparece una silueta que sigue al cursor con
validación de terreno, y al confirmar un pawn camina hasta el sitio y lo levanta.

---

## 6. Controles

| Acción | Control |
|---|---|
| Seleccionar unidad | Clic izquierdo |
| Selección múltiple | Arrastrar caja con clic izquierdo |
| Sumar a la selección | Shift + clic / Shift + arrastre |
| Mover / atacar / recolectar | Clic derecho sobre el destino (orden contextual) |
| Mover cámara | WASD o empujar el borde de la pantalla |
| Zoom | Rueda del mouse |
| Grupos de control | Ctrl + número para asignar, número para llamar |

**Orden contextual:** un solo botón hace lo correcto según el objetivo — suelo = mover,
enemigo = atacar, árbol/oro/oveja = recolectar, construcción propia = ayudar a construir.

---

## 7. Niebla de guerra

- Cada bando tiene su propia grilla de visibilidad.
- **No explorado:** negro total.
- **Explorado sin visión actual:** se ve el terreno y los edificios conocidos, pero no las unidades.
- **Visible:** todo en tiempo real.
- Cada unidad y edificio aporta un radio de visión.

> **Decisión pendiente:** si la IA respeta la niebla o ve todo el mapa. Los RTS clásicos hacían
> trampa y veían todo — es más barato y, en la práctica, más divertido. Se decide en S10.

---

## 8. Mapas

Mínimo **3 mapas**, cada uno con posiciones iniciales balanceadas para hasta 5 bandos:

1. **Isla central** — recursos abundantes al centro, disputados. Fomenta el conflicto temprano.
2. **Cuencas separadas** — cada bando con su valle de recursos y pasos estrechos. Juego más lento y defensivo.
3. **Llanura abierta** — poca cobertura, expansión rápida. Partidas cortas y agresivas.

Cada mapa declara cuántos bandos soporta (3 o 5).

---

## 9. Inteligencia artificial

Arquitectura por capas (detalle técnico en [`ARQUITECTURA.md`](ARQUITECTURA.md)):

| Capa | Decide | Frecuencia |
|---|---|---|
| **Estratégica** | Build order, cuándo expandir, cuándo atacar | ~1 Hz |
| **Táctica** | Formar oleadas, elegir objetivo, retirarse | ~3 Hz |
| **Unidad** | FSM: idle / mover / atacar / recolectar / huir | Por frame |

### Dificultades

Igual que en los RTS clásicos, **la dificultad no es inteligencia: es economía y timing.**

| Dificultad | Bonus de recursos | Tamaño de oleada | Intervalo entre ataques |
|---|---|---|---|
| Fácil | ×1.0 | 4 unidades | 150 s |
| Normal | ×1.25 | 8 unidades | 100 s |
| Difícil | ×1.6 | 14 unidades | 70 s |

> Valores iniciales, a calibrar en S16.

---

## 10. Alcance

### Dentro
Economía de 3 recursos · construcción y población · 4 unidades militares + pawn · combate con
contadores · niebla de guerra y minimapa · IA con 3 dificultades · FFA hasta 5 bandos ·
3 mapas · audio y VFX · menús y HUD · build jugable.

### Fuera
Campaña con narrativa · mejoras tecnológicas por edades · unidades navales o montadas ·
editor de mapas · guardar y cargar partida · versión móvil.

### Condicional
**PvP LAN 1v1** — depende de la compuerta go/no-go de la semana 11.

---

## 11. Recursos externos

| Recurso | Autor | Licencia | Uso |
|---|---|---|---|
| **Tiny Swords** | Pixel Frog | Gratuito, uso comercial permitido | Todos los sprites: unidades, edificios, terreno, UI, efectos |

> El docente exige identificar y explicar todo recurso externo. Todo el arte del proyecto
> proviene de este pack; no hay assets propios ni de terceros sin declarar.
