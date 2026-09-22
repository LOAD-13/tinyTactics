using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// La niebla de guerra: quién sabe qué del mapa, y cómo se dibuja.
    /// </summary>
    /// <remarks>
    /// <b>Una textura, no cincuenta mil objetos</b> (ADR-18). El estado de las celdas se
    /// vuelca en una <see cref="Texture2D"/> de un píxel por celda con filtrado de punto, y
    /// esa textura se estira sobre un único sprite que cubre el mapa entero. El filtrado de
    /// punto no es un ahorro: es la decisión estética. Un difuminado suave sobre pixel art
    /// rompe la rejilla —los bordes dejan de caer en los mismos píxeles que el terreno— y se
    /// nota enseguida. Con punto, el borde de la niebla es un escalón de un tile, alineado
    /// con el tileset del pack.
    ///
    /// Lo que suaviza ese escalón no es un filtro, son <b>las nubes del propio pack</b>
    /// (<see cref="MantoDeNubes"/>). El pack trae ocho nubes dibujadas a mano; puestas sobre
    /// lo no explorado, la frontera la dibuja el mismo artista que dibujó el resto del juego.
    ///
    /// <b>Cada bando tiene su mapa.</b> No hay una niebla global: hay una por facción, y la
    /// que se dibuja es la del bando que lleva el jugador. Cambiar de bando en el panel de
    /// partida libre cambia la que se ve, que es lo que permite enseñar la niebla desde los
    /// dos lados sin tener dos partidas.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Niebla de guerra")]
    public class NieblaDeGuerra : MonoBehaviour
    {
        public static NieblaDeGuerra Actual { get; private set; }

        [Header("Trampas del panel de partida libre")]
        [Tooltip("Apaga la niebla del todo: se ve el mapa y tambien los ejercitos. Es una " +
                 "trampa, no una opcion de partida.")]
        public bool ignorarNiebla;

        [Tooltip("Marca el mapa entero como explorado, pero sin regalar vision: se ve el " +
                 "terreno y no lo que se mueve.")]
        public bool mapaRevelado;

        [Header("Reglas de partida")]
        [Tooltip("Las BASES de salida se ven desde el primer fotograma, aunque esten en " +
                 "sombra: castillo y torres de cada bando. " +
                 "Solo las bases. El terreno sigue sin explorar y las unidades siguen sin " +
                 "verse, asi que explorar sigue haciendo falta; lo unico que se regala es " +
                 "DONDE empieza cada uno, que en un mapa simetrico de tres coronas el " +
                 "jugador podria deducir mirando el suyo. Lo que no se regala es cuantos " +
                 "son ni que estan construyendo. " +
                 "Y solo lo que existe al empezar: lo que se levante despues hay que ir a " +
                 "verlo.")]
        public bool basesConocidasAlEmpezar = true;

        [Header("Ritmo")]
        [Tooltip("Segundos entre recalculos. No hace falta uno por fotograma: una unidad a " +
                 "3 tiles por segundo tarda un tercio de segundo en cruzar un tile.")]
        [Range(0.03f, 0.5f)] public float intervalo = 0.1f;

        [Header("Tintes")]
        [Tooltip("Color por el que se MULTIPLICA una celda nunca vista.\n\n" +
                 "Oscurece MUCHO pero no tapa: el terreno se sigue leyendo debajo. Una " +
                 "partida que empieza sin poder ver la forma del mapa agobia en vez de " +
                 "intrigar, y lo que la niebla tiene que esconder son los ejercitos, no la " +
                 "costa. Lo no explorado se distingue de lo recordado por lo oscuro que " +
                 "esta, no por estar tapado.")]
        public Color tinteOculto = new Color(0.34f, 0.37f, 0.46f, 1f);

        [Tooltip("Celda recordada y sin vigilancia. Apenas un velo: lo que ya viste tiene " +
                 "que seguir leyendose como terreno, no como penumbra. Lo que dice que ahi " +
                 "ya no hay nadie mirando no es la sombra, es que no se ven unidades.")]
        public Color tinteExplorado = new Color(0.80f, 0.83f, 0.90f, 1f);

        [Tooltip("Anillo exterior del radio de vision. El peldano intermedio del borde.")]
        public Color tintePenumbra = new Color(0.92f, 0.94f, 0.97f, 1f);

        [Header("Dibujo")]
        [Tooltip("Material con el shader de tinte multiplicativo. Lo asigna el generador.")]
        public Material material;

        [Tooltip("Orden de dibujo. Por encima de las unidades y de las nubes de ambiente.")]
        public int ordenDeDibujo = 6000;

        readonly Dictionary<int, MapaDeVisibilidad> _mapas = new Dictionary<int, MapaDeVisibilidad>();
        readonly List<int> _bandos = new List<int>();

        GrillaMapa _grilla;
        int _ancho, _alto;

        Texture2D _textura;
        Color32[] _pixeles;
        SpriteRenderer _lienzo;

        float _proximaRonda;
        int _faccionDibujada = -1;

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;

            // Al apagar la niebla hay que devolver a la vista todo lo que estaba tapado. Sin
            // esta pasada, quitar el componente en el editor dejaria media escena invisible
            // sin nada que lo explique.
            DevolverTodoALaVista();
        }

        void Start()
        {
            if (!Preparar())
            {
                Debug.LogWarning("[Tiny Tactics] La niebla no encuentra el mundo. Se apaga.", this);
                enabled = false;
                return;
            }

            // Las bases de salida se dan por vistas por todos.
            //
            // Se probo la version grande de esto —empezar con el mapa entero explorado— y se
            // descarto jugandola: con todo el terreno conocido, la niebla dejaba de contar
            // nada y el minimapa pasaba a ser una foto completa del mapa. Lo que hacia falta
            // era mucho menos: ver DONDE esta cada rival, no que hay entre medias.
            //
            // Solo lo que existe al arrancar: lo que se construya despues hay que ir a verlo.
            if (basesConocidasAlEmpezar) DarPorVistasLasBases();

            Ronda();
        }

        void DarPorVistasLasBases()
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
                if (casas[i] != null)
                    for (int bando = 0; bando < 5; bando++)
                        casas[i].MarcarVisto(bando);
        }

        /// <summary>
        /// Engancha la grilla y monta el lienzo. Se puede llamar mil veces.
        /// </summary>
        /// <remarks>
        /// <b>Esto no estaba y costo el fallo del bando azul.</b> El tamano del mapa se fijaba
        /// solo en <c>Start</c>, pero el minimapa tambien tiene <c>Start</c> y Unity no
        /// garantiza cual corre primero. Cuando corria antes el del minimapa, este pedia el
        /// mapa de visibilidad del jugador, se creaba con el ancho y el alto todavia a cero
        /// —o sea un mapa de UNA celda— y se quedaba cacheado para siempre. Resultado: el
        /// bando con el que arranca la partida no veia nada nunca, y los demas iban bien
        /// porque sus mapas se creaban mas tarde, ya con el tamano puesto.
        ///
        /// Es el mismo fallo que el cartel de final de la semana 07: dar por bueno un orden
        /// de arranque que el motor no promete. La solucion es la misma que entonces —
        /// engancharse cuando hace falta en vez de cuando toca— y ademas se comprueba el
        /// tamano al entregar un mapa, por si alguien lo pidio demasiado pronto igualmente.
        /// </remarks>
        bool Preparar()
        {
            if (_grilla != null) return true;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return false;

            _grilla = mundo.Grilla;
            _ancho = _grilla.Ancho;
            _alto = _grilla.Alto;

            PrepararLienzo();
            return true;
        }

        // -----------------------------------------------------------------
        // Consulta — lo que pregunta el resto del juego
        // -----------------------------------------------------------------

        /// <summary>
        /// ¿Ve ese bando ese punto ahora mismo?
        /// </summary>
        /// <remarks>
        /// Es estático y tolera que no haya niebla en la escena: devuelve <c>true</c>. Así
        /// una escena antigua, o el mapa de pruebas de rutas, siguen funcionando sin tener
        /// que enchufar el componente, y ningún sitio que pregunte necesita una guarda.
        /// </remarks>
        public static bool Ve(int faccion, Vector3 punto)
        {
            var niebla = Actual;
            if (niebla == null || niebla.ignorarNiebla || !niebla.enabled) return true;

            return niebla.Consultar(faccion, punto) == EstadoVisible.Visible;
        }

        /// <summary>
        /// ¿Ha descubierto ese bando ese edificio?
        /// </summary>
        /// <remarks>
        /// Un edificio se recuerda <b>por haberlo visto</b>, no por estar en terreno
        /// explorado. Con el mapa conocido desde el arranque las dos cosas dejarian de ser lo
        /// mismo: todo el terreno esta explorado, asi que la segunda regla delataria cada
        /// edificio nuevo del rival en el momento de terminarlo.
        /// </remarks>
        public static bool Descubierto(int faccion, Edificio edificio)
        {
            if (edificio == null) return false;

            var niebla = Actual;
            if (niebla == null || niebla.ignorarNiebla || !niebla.enabled) return true;

            return edificio.VistoPor(faccion) ||
                   niebla.Consultar(faccion, edificio.PosicionDePlanta) == EstadoVisible.Visible;
        }

        /// <summary>¿Conoce ese bando esa celda, aunque ahora no la esté mirando?</summary>
        public static bool Conoce(int faccion, Vector3 punto)
        {
            var niebla = Actual;
            if (niebla == null || niebla.ignorarNiebla || !niebla.enabled) return true;

            return niebla.Consultar(faccion, punto) != EstadoVisible.Oculto;
        }

        EstadoVisible Consultar(int faccion, Vector3 punto)
        {
            // Si todavia no hay mundo, todo se ve. Es lo unico que puede contestar sin
            // mentir: decir «oculto» antes de que exista el mapa dejaria al juego entero
            // creyendo que nadie ve nada durante los primeros fotogramas.
            if (!Preparar()) return EstadoVisible.Visible;

            var mapa = MapaDe(faccion);
            var celda = _grilla.MundoACelda(punto);
            return mapa.En(celda.x, celda.y);
        }

        /// <summary>
        /// Estado de un punto para el bando que lleva el jugador.
        /// </summary>
        /// <remarks>
        /// Lo piden los dos que dibujan —el manto de nubes y el minimapa—, y por eso devuelve
        /// el estado entero y no un booleano: a ellos no les basta «se ve o no se ve», tienen
        /// que distinguir lo nunca visto de lo recordado, que es la diferencia entre poner
        /// una nube y poner una sombra.
        /// </remarks>
        public EstadoVisible EstadoParaJugador(Vector3 punto) =>
            ignorarNiebla ? EstadoVisible.Visible : Consultar(FaccionDelJugador, punto);

        /// <summary>Mapa de un bando; se crea el primero que pregunte por él.</summary>
        public MapaDeVisibilidad MapaDe(int faccion)
        {
            Preparar();

            if (_mapas.TryGetValue(faccion, out var mapa))
            {
                // Cinturon y tirantes: si el mapa se creo antes de saber el tamano, se tira y
                // se rehace. Sin esto, un mapa naciendo del tamano equivocado se queda
                // cacheado para toda la partida y el sintoma —un bando que no ve nada— no
                // apunta a ninguna parte.
                if (mapa.Ancho == _ancho && mapa.Alto == _alto) return mapa;

                _mapas.Remove(faccion);
                _bandos.Remove(faccion);
            }

            mapa = new MapaDeVisibilidad(Mathf.Max(1, _ancho), Mathf.Max(1, _alto));
            if (mapaRevelado) mapa.RevelarTerreno();

            _mapas[faccion] = mapa;
            _bandos.Add(faccion);
            return mapa;
        }

        /// <summary>El mapa del bando que lleva el jugador. Lo usa el minimapa.</summary>
        public MapaDeVisibilidad MapaDelJugador() => MapaDe(FaccionDelJugador);

        static int FaccionDelJugador
        {
            get
            {
                var selector = SelectorDeUnidades.Actual;
                return selector != null ? selector.faccionJugador : 0;
            }
        }

        // -----------------------------------------------------------------
        // Ronda
        // -----------------------------------------------------------------

        [Header("Revelado")]
        [Tooltip("Niveles de niebla por segundo. Son tres peldanos de oculto a visible, asi " +
                 "que con 6 una celda tarda medio segundo en abrirse del todo.")]
        [Range(0.5f, 30f)] public float velocidadDeRevelado = 6f;

        void Update()
        {
            if (!Preparar()) return;

            // Dos ritmos distintos, y la diferencia importa.
            //
            // QUE SE VE se recalcula diez veces por segundo: es la verdad del juego y no
            // hace falta mas, porque una unidad a 3 tiles por segundo tarda un tercio de
            // segundo en cruzar un tile.
            //
            // COMO SE DIBUJA se mueve cada fotograma, porque es lo unico que el ojo mira.
            // Con las dos cosas al mismo ritmo, las celdas se abrian de golpe y en bloques
            // de diez por segundo: se veia el cuadriculado encendiendose a saltos. El
            // desvanecido no cambia lo que la unidad ve ni un tile, solo cuanto tarda en
            // enterarse el jugador.
            //
            // unscaledTime: el panel de partida libre pone el juego en pausa, y en pausa la
            // niebla tiene que seguir respondiendo al cambio de bando y a los interruptores.
            if (Time.unscaledTime >= _proximaRonda)
            {
                _proximaRonda = Time.unscaledTime + intervalo;
                Ronda();
            }

            Pintar();
        }

        void Ronda()
        {
            for (int i = 0; i < _bandos.Count; i++)
                _mapas[_bandos[i]].EmpezarRonda();

            IluminarUnidades();
            IluminarEdificios();
            ApuntarLoQueSeVe();

            if (mapaRevelado)
                for (int i = 0; i < _bandos.Count; i++)
                    _mapas[_bandos[i]].RevelarTerreno();

            AplicarOcultamiento();
        }

        void IluminarUnidades()
        {
            var todas = RegistroDeUnidades.Todas;

            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva) continue;

                float radio = u.RadioDeVision;
                if (radio <= 0f) continue;

                MapaDe(u.faccion).Iluminar(_grilla.MundoACelda(u.transform.position), radio);
            }
        }

        void IluminarEdificios()
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null) continue;

                float radio = e.RadioDeVision;
                if (radio <= 0f) continue;

                // Un edificio mira desde su PLANTA, no desde su centro de dibujo: el
                // castillo ocupa cinco por tres celdas y su sprite tiene el centro muy por
                // encima del suelo. Iluminando desde el dibujo, la mitad del disco caeria
                // sobre el tejado y la base quedaria en penumbra.
                var celdas = e.celdas;
                var centro = new Vector2Int(celdas.x + celdas.width / 2,
                                            celdas.y + celdas.height / 2);

                MapaDe(e.faccion).Iluminar(centro, radio);
            }
        }

        /// <summary>
        /// Apunta en cada edificio qué bandos lo están viendo ahora mismo.
        /// </summary>
        /// <remarks>
        /// Se recorren todos los bandos y no solo el del jugador porque la memoria es del
        /// edificio y la van a leer otros: una unidad que termina de derribar un cuartel
        /// busca el siguiente entre los que su bando conoce, y eso vale para la IA de la
        /// semana 10 igual que para ti.
        /// </remarks>
        void ApuntarLoQueSeVe()
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null) continue;

                var donde = e.PosicionDePlanta;

                for (int b = 0; b < _bandos.Count; b++)
                {
                    int bando = _bandos[b];
                    if (e.VistoPor(bando)) continue;

                    if (Consultar(bando, donde) == EstadoVisible.Visible) e.MarcarVisto(bando);
                }
            }
        }

        // -----------------------------------------------------------------
        // Ocultar lo que está en niebla
        // -----------------------------------------------------------------

        /// <summary>
        /// Tapa o destapa lo que el bando del jugador no debería estar viendo.
        /// </summary>
        /// <remarks>
        /// <b>Las unidades desaparecen; los edificios se quedan donde los viste.</b> No es
        /// una inconsistencia, es la diferencia entre los dos: un edificio no se mueve, así
        /// que el recuerdo sigue siendo verdad y borrarlo castigaría al jugador por haber
        /// explorado. Una unidad sí se mueve, y dejar su fantasma en el sitio donde la viste
        /// hace diez segundos es decirle al jugador algo que ya es mentira.
        ///
        /// Quien tapa no es este método: es <see cref="OcultarEnNiebla"/>, que usa
        /// <c>forceRenderingOff</c> y no <c>enabled</c>. Son dos interruptores distintos a
        /// propósito — <c>enabled</c> ya lo manejan la selección y la barra de vida, y un
        /// segundo dueño del mismo campo acabaría encendiendo corchetes de selección en
        /// unidades que nadie ha seleccionado.
        /// </remarks>
        void AplicarOcultamiento()
        {
            int jugador = FaccionDelJugador;
            bool sinNiebla = ignorarNiebla;

            var todas = RegistroDeUnidades.Todas;
            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null) continue;

                bool tapar = !sinNiebla && u.faccion != jugador &&
                             Consultar(jugador, u.transform.position) != EstadoVisible.Visible;

                OcultarEnNiebla.Aplicar(u.gameObject, tapar);
            }

            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null) continue;

                // Un edificio se tapa hasta que lo ves; una vez visto se queda dibujado
                // donde estaba. No se mueve, asi que el recuerdo sigue siendo verdad.
                bool tapar = !sinNiebla && e.faccion != jugador && !e.VistoPor(jugador);

                OcultarEnNiebla.Aplicar(e.gameObject, tapar);
            }
        }

        void DevolverTodoALaVista()
        {
            var todas = RegistroDeUnidades.Todas;
            for (int i = 0; i < todas.Count; i++)
                if (todas[i] != null) OcultarEnNiebla.Aplicar(todas[i].gameObject, false);

            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
                if (casas[i] != null) OcultarEnNiebla.Aplicar(casas[i].gameObject, false);
        }

        // -----------------------------------------------------------------
        // Dibujo
        // -----------------------------------------------------------------

        void PrepararLienzo()
        {
            _textura = new Texture2D(_ancho, _alto, TextureFormat.RGBA32, false)
            {
                name = "NieblaDeGuerra",

                // Punto y no bilineal: ver el remate de la clase. Es la decision estetica.
                filterMode = FilterMode.Point,

                // Sin repeticion: el borde del mapa no debe reflejar la columna opuesta.
                wrapMode = TextureWrapMode.Clamp,
            };

            _pixeles = new Color32[_ancho * _alto];

            // Se rellena a mano antes de ensenarla: una Texture2D recien creada trae
            // basura, y el primer fotograma se veria ruido de colores sobre el mapa.
            Color32 arranque = tinteOculto;
            for (int i = 0; i < _pixeles.Length; i++) _pixeles[i] = arranque;

            var go = new GameObject("LienzoDeNiebla");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(_ancho * 0.5f, _alto * 0.5f, 0f);

            _lienzo = go.AddComponent<SpriteRenderer>();

            // Un pixel por tile: con pixelsPerUnit = 1, la textura mide exactamente lo que
            // mide el mapa en unidades de mundo y cada pixel cae sobre su celda.
            _lienzo.sprite = Sprite.Create(_textura, new Rect(0f, 0f, _ancho, _alto),
                                           new Vector2(0.5f, 0.5f), 1f, 0,
                                           SpriteMeshType.FullRect);
            _lienzo.sortingOrder = ordenDeDibujo;

            _textura.SetPixels32(_pixeles);
            _textura.Apply(false);

            if (material != null) _lienzo.sharedMaterial = material;
            else Debug.LogWarning("[Tiny Tactics] La niebla no tiene material: se dibujara " +
                                  "tapando en vez de oscureciendo.", this);

            _faccionDibujada = -1;
        }

        /// <summary>
        /// Dibuja la niebla, desvaneciendo cada celda hacia su estado en vez de saltar.
        /// </summary>
        /// <remarks>
        /// Cada celda guarda un numero de 0 a 3 —oculto, explorado, penumbra, visible— que
        /// persigue a su estado real. El color sale de interpolar entre los tintes con ese
        /// numero, asi que una celda que se descubre recorre los tres peldanos en medio
        /// segundo en vez de encenderse de golpe.
        ///
        /// <b>Es suave en el TIEMPO, no en el espacio.</b> El borde sigue siendo un escalon
        /// de un tile, porque difuminarlo en el espacio romperia la rejilla del pixel art
        /// (ADR-18). Lo que se suaviza es el momento en que cada tile cambia, que es de donde
        /// venia la sensacion de dureza: no molestaba el cuadrado, molestaba el parpadeo.
        ///
        /// <b>Al cambiar de bando se salta el fundido.</b> Mezclar dos mapas de visibilidad
        /// completamente distintos durante medio segundo no lee como una transicion, lee como
        /// un error de dibujado.
        /// </remarks>
        void Pintar()
        {
            if (_textura == null) return;

            int jugador = FaccionDelJugador;
            if (jugador != _faccionDibujada)
            {
                _faccionDibujada = jugador;
                _saltarFundido = true;
            }

            // Sin niebla no se dibuja nada: se apaga el lienzo y se ahorra el volcado. Y se
            // pide salto, para que al volver a encenderla no se vea el mapa fundiendose
            // desde lo que hubiera quedado en la ultima foto.
            if (ignorarNiebla)
            {
                if (_lienzo != null) _lienzo.enabled = false;
                _saltarFundido = true;
                return;
            }

            if (_lienzo != null) _lienzo.enabled = true;

            var mapa = MapaDe(jugador);

            if (_luz == null || _luz.Length != _pixeles.Length)
            {
                _luz = new float[_pixeles.Length];
                _saltarFundido = true;
            }

            float paso = velocidadDeRevelado * Time.unscaledDeltaTime;
            bool cambio = _saltarFundido;

            for (int y = 0; y < _alto; y++)
            {
                int fila = y * _ancho;
                for (int x = 0; x < _ancho; x++)
                {
                    int i = x + fila;
                    float objetivo = (int)mapa.En(x, y);
                    float actual = _luz[i];

                    if (_saltarFundido)
                    {
                        actual = objetivo;
                    }
                    else if (actual != objetivo)
                    {
                        actual = Mathf.MoveTowards(actual, objetivo, paso);
                        cambio = true;
                    }

                    _luz[i] = actual;
                    _pixeles[i] = Tinte(actual);
                }
            }

            _saltarFundido = false;

            // Quieto no cuesta nada: si ninguna celda se ha movido, la textura que esta en
            // la tarjeta ya es la correcta y subirla otra vez seria pagar 200 KB por
            // fotograma para no cambiar un pixel.
            if (!cambio) return;

            _textura.SetPixels32(_pixeles);
            _textura.Apply(false);
        }

        float[] _luz;
        bool _saltarFundido;

        /// <summary>El color de una celda segun lo abierta que este, de 0 a 3.</summary>
        Color32 Tinte(float nivel)
        {
            if (nivel <= 1f) return Color.Lerp(tinteOculto, tinteExplorado, nivel);
            if (nivel <= 2f) return Color.Lerp(tinteExplorado, tintePenumbra, nivel - 1f);

            return Color.Lerp(tintePenumbra, Color.white, nivel - 2f);
        }

        /// <summary>Lo llama el panel al tocar un interruptor: no espera a la próxima ronda.</summary>
        public void Refrescar()
        {
            if (!Preparar()) return;
            Ronda();
        }
    }
}
