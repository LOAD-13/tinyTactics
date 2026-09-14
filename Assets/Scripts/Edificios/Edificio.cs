using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

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
    /// Desde la épica E06 también se puede derribar, y por eso implementa
    /// <see cref="IObjetivo"/>: quien pega no distingue entre una unidad y un edificio.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Edificio")]
    public class Edificio : MonoBehaviour, IObjetivo
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
        public void Inaugurar()
        {
            Operativo = true;

            // La obra acaba de soltar la barra: ahora cuenta vida en vez de martillazos.
            EnchufarBarra(true);
        }

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

        // -----------------------------------------------------------------
        // Vida y derrumbe
        // -----------------------------------------------------------------

        [Header("Combate")]
        [Tooltip("Segundos que tarda en desmoronarse una vez derribado.")]
        [Range(0.2f, 3f)] public float duracionDerrumbe = 0.8f;

        [Tooltip("Fotogramas de la explosión. Los asigna el generador desde Particle FX.")]
        [SerializeField] Sprite[] _explosion;

        [SerializeField] int _vida = -1;

        bool _cayendo;

        /// <summary>La llama el generador de la escena. Los sprites sí se serializan.</summary>
        public void ConfigurarEfectos(Sprite[] explosion) => _explosion = explosion;

        public int Vida => _vida;
        public int VidaMaxima => datos != null ? datos.vidaMaxima : 400;

        /// <summary>Fracción de vida, de 0 a 1. La lee la barra.</summary>
        public float FraccionDeVida
        {
            get
            {
                int maximo = Mathf.Max(1, VidaMaxima);
                return Mathf.Clamp01(VidaActual / (float)maximo);
            }
        }

        /// <summary>
        /// Vida efectiva, resolviendo el «todavía sin inicializar».
        /// </summary>
        /// <remarks>
        /// El campo arranca en −1 y no en el máximo a propósito. Un edificio puede recibir
        /// su ficha <i>después</i> de despertar —los que levanta el jugador la reciben en
        /// <c>Colocar</c>— así que fijar la vida en el <c>Awake</c> la dejaría clavada en el
        /// valor por defecto. Con el centinela, el primer acceso ya tiene la ficha puesta.
        /// </remarks>
        public int VidaActual
        {
            get
            {
                if (_vida < 0) _vida = VidaMaxima;
                return _vida;
            }
        }

        // --- IObjetivo -------------------------------------------------------

        int IObjetivo.Faccion => faccion;
        bool IObjetivo.EsUnidad => false;
        bool IObjetivo.Vivo => !_cayendo && VidaActual > 0;
        Vector3 IObjetivo.Posicion => PuntoDeEntrega;

        float IObjetivo.DistanciaDesde(Vector3 punto) => DistanciaA(punto);
        Vector3 IObjetivo.PuntoDeAtaqueDesde(Vector3 origen) => PuntoDeEntregaDesde(origen);

        void IObjetivo.RecibirDano(int cantidad, Unidad agresor) => RecibirDano(cantidad, agresor);

        // ---------------------------------------------------------------------

        /// <summary>
        /// Quita vida al edificio y lo derriba si se queda a cero.
        /// </summary>
        /// <remarks>
        /// El edificio no responde ni avisa a nadie: no tiene con qué. Quien defiende una
        /// base son las unidades que haya cerca, y esas ya reaccionan por su cuenta cuando
        /// el atacante entra en su radio.
        /// </remarks>
        public void RecibirDano(int cantidad, Unidad agresor = null)
        {
            if (_cayendo || cantidad <= 0) return;

            _vida = Mathf.Max(0, VidaActual - cantidad);

            // Se recuerda quién dio el último golpe para poder apuntarle el edificio en su
            // hoja. Derribar() también la llama el árbitro al eliminar un bando, y entonces
            // no hay agresor: −1 significa «se cayó solo», que es exactamente lo que pasa.
            if (agresor != null) _ultimoAgresor = agresor.faccion;

            if (_vida > 0) return;

            Derribar();
        }

        int _ultimoAgresor = -1;

        /// <summary>
        /// Lo tira abajo: libera el terreno, deja de contar para todo y se desmorona.
        /// </summary>
        public void Derribar()
        {
            if (_cayendo) return;
            _cayendo = true;

            var libro = EstadisticasPartida.Actual;
            if (libro != null) libro.EdificioCaido(faccion, _ultimoAgresor);

            // Deja de estar operativo ANTES que nada. Con esta sola línea se cae solo todo
            // lo que colgaba de él: deja de ser centro de entrega, deja de fabricar y deja
            // de aportar población — y el límite baja sin que nadie lo descuente, porque la
            // población se recuenta en vez de guardarse (HU-040).
            Operativo = false;

            // El terreno se libera ya, no al terminar la animación. Unos escombros que
            // siguen bloqueando el paso mientras se desvanecen son un muro invisible, y el
            // jugador no tiene forma de saber que está ahí.
            Ocupar(false);

            Seleccionar(false);
            MostrarAnillo(false);

            var obra = GetComponent<ObraEnConstruccion>();
            if (obra != null) Destroy(obra);

            if (gameObject.activeInHierarchy) StartCoroutine(Desmoronarse());
            else Destroy(gameObject);
        }

        /// <summary>
        /// El desmoronamiento: estalla por varios sitios, se hunde y se apaga.
        /// </summary>
        /// <remarks>
        /// El pack no trae ruina ni escombros para ningún edificio, pero sí trae
        /// <c>Particle FX/Explosion_01</c>, ocho fotogramas ya recortados. Las explosiones
        /// van <b>escalonadas y repartidas por la planta</b>, no una sola en el centro: un
        /// único estallido se lee como «alguien apagó el sprite», y tres o cuatro corriéndose
        /// por el edificio se leen como algo que se viene abajo. Es la diferencia entre una
        /// transición y una demolición, y cuesta lo mismo.
        /// </remarks>
        IEnumerator Desmoronarse()
        {
            Reventar();

            var pintores = GetComponentsInChildren<SpriteRenderer>(true);
            var iniciales = new Color[pintores.Length];
            for (int i = 0; i < pintores.Length; i++) iniciales[i] = pintores[i].color;

            Vector3 origen = transform.position;
            float duracion = Mathf.Max(0.05f, duracionDerrumbe);
            float reloj = 0f;

            while (reloj < duracion)
            {
                reloj += Time.deltaTime;
                float t = Mathf.Clamp01(reloj / duracion);

                for (int i = 0; i < pintores.Length; i++)
                {
                    if (pintores[i] == null) continue;

                    Color c = iniciales[i];
                    float gris = Mathf.Lerp(1f, 0.35f, t);
                    pintores[i].color = new Color(c.r * gris, c.g * gris, c.b * gris,
                                                  c.a * (1f - t));
                }

                // Se hunde un cuarto de tile mientras cae. Poco: lo justo para que el ojo
                // lea «se viene abajo» y no «alguien apagó el sprite».
                transform.position = origen + new Vector3(0f, -0.25f * t, 0f);

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Reparte las explosiones por la planta del edificio, escalonadas en el tiempo.
        /// </summary>
        void Reventar()
        {
            if (_explosion == null || _explosion.Length == 0) return;

            // Una explosión por cada dos casillas de planta, entre dos y seis. Así una casa
            // de 2x2 revienta dos veces y un castillo de 5x3 revienta seis: el estruendo va
            // con el tamaño sin tener que escribir una tabla.
            //
            // Las celdas se leen del rectángulo ya colocado y no de la ficha: un edificio
            // siempre tiene su rectángulo puesto, pero la ficha puede faltar en los que
            // vienen del generador antiguo, y ahí el castillo reventaría como una choza.
            int ancho = Mathf.Max(1, celdas.width);
            int alto = Mathf.Max(1, celdas.height);

            int cuantas = Mathf.Clamp((ancho * alto) / 2, 2, 6);

            Vector3 centro = PuntoDeEntrega;
            Vector2 mitad = new Vector2(huella.x * 0.45f, huella.y * 0.45f);

            int orden = 0;
            var pintor = GetComponent<SpriteRenderer>();
            if (pintor != null) orden = pintor.sortingOrder;

            for (int i = 0; i < cuantas; i++)
            {
                Vector3 donde = centro + new Vector3(
                    Random.Range(-mitad.x, mitad.x),
                    Random.Range(-mitad.y, mitad.y), 0f);

                // La primera va sin retraso: el jugador tiene que ver el estallido en el
                // mismo instante en que cae el último golpe, no medio segundo después.
                float retraso = i == 0 ? 0f : Random.Range(0.05f, duracionDerrumbe * 0.7f);

                StartCoroutine(Estallido(donde, retraso, orden + 60 + i));
            }
        }

        IEnumerator Estallido(Vector3 donde, float retraso, int orden)
        {
            if (retraso > 0f) yield return new WaitForSeconds(retraso);

            var go = new GameObject("Explosion");
            go.transform.position = donde;

            // Se desata del edificio a propósito: el edificio se está hundiendo y apagándose,
            // y una explosión que heredara esa animación se hundiría y se apagaría con él.
            go.transform.SetParent(null, true);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _explosion[0];
            sr.sortingOrder = orden;

            const float fps = 14f;

            var anim = go.AddComponent<AnimadorSprite>();
            anim.dispersion = 0f;
            anim.Configurar(_explosion, fps, 0, false);

            Destroy(go, _explosion.Length / fps + 0.15f);
        }

        /// <summary>Edificio de cualquier bando bajo un punto. Lo usa el clic de ataque.</summary>
        public static Edificio Bajo(Vector3 punto)
        {
            Edificio mejor = null;
            float mejorDistancia = float.MaxValue;

            for (int i = 0; i < _todos.Count; i++)
            {
                var e = _todos[i];
                if (e == null || e._cayendo) continue;

                float d = e.DistanciaA(punto);
                if (d > e.holguraSeleccion || d >= mejorDistancia) continue;

                mejorDistancia = d;
                mejor = e;
            }

            return mejor;
        }

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
            EnchufarBarra();
        }

        /// <summary>
        /// Conecta la barra de vida a la vida real del edificio.
        /// </summary>
        /// <remarks>
        /// Se hace en ejecución y no en el generador de la escena porque lo que se conecta
        /// son <b>delegados</b>, y Unity no serializa delegados: si el generador los
        /// asignara, la escena se guardaría con la barra apuntando a nada y al darle a Play
        /// todos los edificios saldrían con la vida a cero. El generador construye el
        /// armazón; quien lo enchufa es el edificio, cada vez que despierta.
        /// </remarks>
        void EnchufarBarra(bool forzar = false)
        {
            var barra = GetComponentInChildren<Interfaz.BarraDeVida>(true);
            if (barra == null) return;

            // La obra en construcción se queda con la barra para su propio progreso, y esa
            // manda mientras el edificio esté a medias. Al inaugurarse la devuelve y
            // entonces sí se enchufa, con «forzar» — no vale mirar si el componente sigue
            // ahí, porque Unity lo destruye al final del fotograma y en ese hueco la
            // comprobación todavía diría que hay obra.
            if (!forzar && GetComponent<ObraEnConstruccion>() != null) return;

            barra.fuente = () => FraccionDeVida;

            // Ni siempre ni nunca: una base entera con la barra encendida es ruido, y una
            // base sin barras no deja ver qué está aguantando y qué se está cayendo.
            barra.visible = () => Seleccionado || FraccionDeVida < 0.999f;
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
