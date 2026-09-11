using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Edificios
{

    /// <summary>
    /// Un edificio del mapa: a qué bando pertenece y qué se puede hacer con él.
    ///
    /// Hasta esta semana el castillo era un sprite decorativo. Se convierte en entidad
    /// porque la economía lo necesita para dos cosas distintas: es <b>donde se deposita</b>
    /// lo recolectado y es <b>donde se entrena</b>. Ese doble papel es lo que hace que la
    /// posición del castillo importe y lo que cierra el bucle de la partida.
    ///
    /// No tiene vida ni se puede destruir todavía: eso es la épica E06. Meterlo ahora sería
    /// escribir un sistema de daño a edificios sin nada que los ataque.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Edificio")]
    public class Edificio : MonoBehaviour
    {
        [Header("Identidad")]
        [Tooltip("Ficha del catálogo: coste, huella, población y qué fabrica.")]
        public DatosEdificio datos;

        public TipoEdificio tipo = TipoEdificio.Castillo;
        public string nombreVisible = "Castillo";

        [Tooltip("Índice de bando. 0 es el jugador humano.")]
        public int faccion;

        [Header("Interfaz")]
        [Tooltip("Retrato para el panel. Se recorta del propio sprite del edificio.")]
        public Sprite retrato;

        [Header("Economía")]
        [Tooltip("Los pawns cargados vienen aquí a soltar lo que traen.")]
        public bool centroDeEntrega = true;

        [Tooltip("Tamaño real del dibujo, en tiles. Medido sobre el PNG, no estimado.")]
        public Vector2 huella = new Vector2(4.88f, 3.25f);

        [Tooltip("Desplazamiento del centro del dibujo respecto al centro del lienzo.")]
        public Vector2 huellaCentro = new Vector2(0f, -0.27f);

        [Tooltip("Dónde aparecen las unidades recién entrenadas, relativo al edificio.")]
        public Vector2 puntoSalida = new Vector2(0f, -2.6f);

        [Header("Terreno")]
        [Tooltip("Esquina inferior izquierda de las celdas que ocupa.")]
        public Vector2Int celdaOrigen;

        [Tooltip("Cuántas celdas ocupa. Cero = todavía no reclama terreno.")]
        public Vector2Int celdaTamano;

        /// <summary>
        /// Celdas que ocupa en la grilla. Las fija quien lo coloca; no se deducen de la
        /// posición, para que la silueta que el jugador vio y el terreno que acaba
        /// bloqueado sean literalmente el mismo dato.
        /// </summary>
        /// <remarks>
        /// Se guarda como dos <c>Vector2Int</c> y no como un <c>RectInt</c> porque el
        /// serializador de Unity no trata a los dos igual de bien, y un campo que no se
        /// guarda falla de la peor manera: en el editor va, y al reabrir la escena el
        /// castillo deja de ocupar terreno sin decir nada.
        /// </remarks>
        public RectInt celdas
        {
            get => new RectInt(celdaOrigen.x, celdaOrigen.y, celdaTamano.x, celdaTamano.y);
            set
            {
                celdaOrigen = new Vector2Int(value.x, value.y);
                celdaTamano = new Vector2Int(value.width, value.height);
            }
        }

        [Header("Selección")]
        [Tooltip("Holgura alrededor de la huella en la que un clic ya cuenta como suyo.")]
        [Range(0f, 2f)] public float holguraSeleccion = 0.25f;

        public bool Seleccionado { get; private set; }

        /// <summary>
        /// False mientras es una obra a medio levantar.
        ///
        /// Un edificio en obras ya existe en el mundo —ocupa terreno y se puede seleccionar—
        /// pero todavía no hace nada: no recibe recursos, no fabrica y no suma población. Es
        /// lo que hace que construir cueste tiempo de verdad y no sea un cobro instantáneo.
        /// </summary>
        public bool Operativo { get; private set; } = true;

        /// <summary>La llama la obra al terminarse.</summary>
        public void Inaugurar() => Operativo = true;

        /// <summary>La llama el colocador al plantar la obra.</summary>
        public void MarcarEnObras() => Operativo = false;

        /// <summary>
        /// Deja el edificio cuadrado con su ficha y con el terreno que va a ocupar.
        ///
        /// La posición sale del rectángulo y no al revés. Así la silueta que el jugador vio
        /// y las celdas que acaban bloqueadas son literalmente el mismo dato, y ningún
        /// redondeo puede separarlas.
        /// </summary>
        public void Colocar(DatosEdificio ficha, int bando, RectInt donde, int fachada = 0)
        {
            datos = ficha;
            faccion = bando;
            celdas = donde;
            variante = ficha != null ? ficha.Ajustar(fachada) : 0;

            if (ficha != null)
            {
                tipo = ficha.tipo;
                nombreVisible = ficha.nombreVisible;
                centroDeEntrega = ficha.centroDeEntrega;
                huella = ficha.HuellaDe(variante);
                huellaCentro = ficha.HuellaCentroDe(variante);
            }

            transform.position = PosicionPara(ficha, donde, variante);
        }

        [Tooltip("Cuál de las fachadas del catálogo lleva puesta. Solo las casas tienen más de una.")]
        public int variante;

        /// <summary>Dónde va el objeto para que su planta caiga justo sobre esas celdas.</summary>
        public static Vector3 PosicionPara(DatosEdificio ficha, RectInt donde, int fachada = 0)
        {
            Vector2 centro = new Vector2(
                donde.x + donde.width * 0.5f,
                donde.y + donde.height * 0.5f);

            if (ficha != null) centro -= ficha.DesplazamientoBase(fachada);

            return new Vector3(centro.x, centro.y, 0f);
        }

        // -----------------------------------------------------------------
        // Terreno
        // -----------------------------------------------------------------

        bool _ocupando;

        /// <summary>
        /// Bloquea en la grilla las celdas que pisa.
        /// </summary>
        /// <remarks>
        /// Va en <c>Start</c> y no en <c>Awake</c> porque la grilla se construye en el
        /// <c>Awake</c> de <see cref="Mundo.MundoJuego"/> y el orden entre los Awake de dos
        /// objetos distintos no está garantizado. Unity sí garantiza que todos los Awake
        /// corren antes que cualquier Start, y eso basta: nadie pide una ruta antes del
        /// primer Update.
        /// </remarks>
        void Start() => ReclamarTerreno();

        /// <summary>
        /// Bloquea ya las celdas que pisa, sin esperar al <c>Start</c>.
        /// </summary>
        /// <remarks>
        /// La llama el colocador nada más plantar una obra, y hace falta por una cuestión de
        /// orden: al encender un objeto en caliente, Unity ejecuta su <c>Awake</c> en el acto
        /// pero <b>aplaza el <c>Start</c> hasta justo antes del siguiente Update</b>. En ese
        /// hueco, el edificio ya existe y el terreno todavía está libre.
        ///
        /// Es exactamente lo que dejaba a un pawn metido dentro de una casa recién puesta:
        /// se le buscaba sitio fuera preguntándole a una grilla que aún no sabía que la casa
        /// estaba ahí, así que la respuesta era «donde estás ya vale».
        ///
        /// Es idempotente: el testigo impide marcar dos veces.
        /// </remarks>
        public void ReclamarTerreno() => Ocupar(true);

        /// <summary>Al derribarse devuelve el terreno. Al cerrar la partida no hay a quién.</summary>
        void OnDestroy() => Ocupar(false);

        void Ocupar(bool valor)
        {
            if (_ocupando == valor) return;
            if (celdas.width <= 0 || celdas.height <= 0) return;

            var mundo = Mundo.MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            _ocupando = valor;
            mundo.Grilla.MarcarCaja(celdas, valor);
        }

        /// <summary>Vuelve a marcar su terreno. La usa el mundo tras recalcular una zona.</summary>
        public void RemarcarTerreno()
        {
            if (!_ocupando || celdas.width <= 0 || celdas.height <= 0) return;

            var mundo = Mundo.MundoJuego.Actual;
            if (mundo != null && mundo.Grilla != null) mundo.Grilla.MarcarCaja(celdas, true);
        }

        /// <summary>Población que aporta, solo si ya está en pie.</summary>
        public int PoblacionQueAporta =>
            Operativo && datos != null ? datos.poblacionQueAporta : 0;

        /// <summary>Centro real del edificio, no el del lienzo del sprite.</summary>
        public Vector3 PuntoDeEntrega =>
            transform.position + new Vector3(huellaCentro.x, huellaCentro.y, 0f);

        /// <summary>
        /// A qué distancia queda un punto del <b>borde</b> del edificio. Cero si está encima.
        /// </summary>
        /// <remarks>
        /// Un castillo mide casi cinco tiles de ancho, así que medir contra su centro
        /// significaba que un pawn pegado a la esquina estaba «a dos tiles y medio» y uno
        /// pegado al frente estaba «a uno y medio»: el mismo sitio en la práctica y dos
        /// respuestas distintas. Medir contra la huella hace que arrimarse valga por
        /// cualquier lado.
        /// </remarks>
        public float DistanciaA(Vector3 punto)
        {
            Vector3 centro = PuntoDeEntrega;

            float dx = Mathf.Max(0f, Mathf.Abs(punto.x - centro.x) - huella.x * 0.5f);
            float dy = Mathf.Max(0f, Mathf.Abs(punto.y - centro.y) - huella.y * 0.5f);

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Punto del edificio más cercano a quien viene: el pawn se arrima por el lado por
        /// el que llega, en vez de rodear el castillo entero para ir a una puerta imaginaria.
        /// </summary>
        public Vector3 PuntoDeEntregaDesde(Vector3 origen)
        {
            Vector3 centro = PuntoDeEntrega;
            Vector2 mitad = huella * 0.5f;

            return new Vector3(
                Mathf.Clamp(origen.x, centro.x - mitad.x, centro.x + mitad.x),
                Mathf.Clamp(origen.y, centro.y - mitad.y, centro.y + mitad.y),
                0f);
        }

        public Vector3 PuntoDeSalida =>
            transform.position + new Vector3(puntoSalida.x, puntoSalida.y, 0f);

        Transform _anillo;

        // -----------------------------------------------------------------
        // Registro
        // -----------------------------------------------------------------

        static readonly List<Edificio> _todos = new List<Edificio>();

        public static IReadOnlyList<Edificio> Todos => _todos;

        void Awake()
        {
            _anillo = transform.Find("Seleccion");
            MostrarAnillo(false);
        }

        void OnEnable() => _todos.Add(this);
        void OnDisable() => _todos.Remove(this);

        public void Seleccionar(bool valor)
        {
            if (Seleccionado == valor) return;

            Seleccionado = valor;
            MostrarAnillo(valor);
        }

        void MostrarAnillo(bool visible)
        {
            if (_anillo != null) _anillo.gameObject.SetActive(visible);
        }

        // -----------------------------------------------------------------
        // Búsqueda
        // -----------------------------------------------------------------

        /// <summary>
        /// Centro de entrega propio más cercano a un punto.
        ///
        /// Es lo que permitirá que un castillo adicional junto a una expansión lejana
        /// acorte el viaje de vuelta sin que el pawn tenga que saber nada de mapas: pregunta
        /// por el más cercano y ya está.
        /// </summary>
        public static Edificio EntregaMasCercana(Vector3 punto, int faccion)
        {
            Edificio mejor = null;
            float mejorDistancia = float.MaxValue;

            for (int i = 0; i < _todos.Count; i++)
            {
                var e = _todos[i];
                if (e == null || !e.centroDeEntrega || !e.Operativo || e.faccion != faccion)
                    continue;

                float d = e.DistanciaA(punto);
                if (d >= mejorDistancia) continue;

                mejorDistancia = d;
                mejor = e;
            }

            return mejor;
        }

        /// <summary>Edificio propio bajo un punto del mundo, para el clic de selección.</summary>
        public static Edificio EdificioEn(Vector3 punto, int faccion)
        {
            Edificio mejor = null;
            float mejorDistancia = float.MaxValue;

            for (int i = 0; i < _todos.Count; i++)
            {
                var e = _todos[i];
                if (e == null || e.faccion != faccion) continue;

                // Vale clicar cualquier parte del edificio, no solo su centro. Con un radio
                // se quedaban fuera las dos torres de los extremos, que es justo donde el
                // ojo apunta cuando quieres seleccionar un castillo.
                float d = e.DistanciaA(punto);
                if (d > e.holguraSeleccion || d >= mejorDistancia) continue;

                mejorDistancia = d;
                mejor = e;
            }

            return mejor;
        }
    }
}
