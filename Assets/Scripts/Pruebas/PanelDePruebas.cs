using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Interfaz;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Pruebas
{
    /// <summary>
    /// Panel de trampas para probar y demostrar el juego.
    ///
    /// <b>No es una épica ni una funcionalidad.</b> Es una herramienta transversal que crece
    /// pegada a las épicas: la E07 le añadirá quitar la niebla y la E08 pausar la IA y ver su
    /// plan. Una épica entrega juego; esto entrega herramienta, y una semana cuya exposición
    /// fuera «miren nuestro panel de depuración» sería la peor del semestre.
    ///
    /// Se paga sola desde el primer día, y por eso entra en la semana 07 y no más adelante:
    /// no hay IA hasta la semana 10, así que <b>sin cambiar de bando no hay forma de enseñar
    /// un combate de dos lados</b>. El panel no es un extra de esta semana, es lo que hace
    /// demostrable el resto de la semana.
    /// </summary>
    /// <remarks>
    /// <b>Se destruye solo fuera del editor y de las builds de desarrollo.</b> Una build de
    /// entrega no puede traer un botón de «+100 oro»: no es cuestión de que nadie lo pulse,
    /// es que su sola presencia invalidaría cualquier sesión de prueba de la semana 16.
    ///
    /// Se apaga en <c>Awake</c> en vez de envolver la clase entera en un <c>#if</c>, que era
    /// lo primero que se intentó: con la clase compilada a medias, cada sitio que le pregunta
    /// algo —el cursor, el selector— necesita su propio <c>#if</c>, y basta olvidar uno para
    /// que la build de entrega deje de compilar sin que nadie se entere hasta el final. La
    /// garantía se pone en un sitio y no en cinco.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Panel de pruebas")]
    public class PanelDePruebas : MonoBehaviour
    {
        public static PanelDePruebas Actual { get; private set; }

        void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(gameObject);
#endif
        }

        [Tooltip("Ancho del panel desplegado, en píxeles de pantalla.")]
        public float ancho = 250f;

        [Tooltip("Bandos que existen en la partida. Lo fija el generador de la escena.")]
        [Range(1, 5)] public int bandos = 3;

        [Tooltip("De aquí salen los iconos. Lo asigna el generador de la escena.")]
        public TemaInterfaz tema;

        /// <summary>¿Está abierto? Lo consultan el cursor y el selector para no pisarse.</summary>
        public bool Abierto { get; private set; }

        [Tooltip("Lo que tarda el panel en desplegarse o replegarse.")]
        [Range(0.05f, 1f)] public float duracionApertura = 0.22f;

        [Tooltip("Tamaño del botón que sobresale cuando el panel está plegado.")]
        public float ladoPestana = 46f;

        /// <summary>
        /// Cuánto está abierto el panel, de 0 a 1. Es lo que se anima.
        /// </summary>
        /// <remarks>
        /// La apertura es un número continuo y no un booleano porque el ancho, la opacidad y
        /// la posición de la pestaña tienen que moverse juntos. Con un booleano habría que
        /// llevar tres animaciones en paralelo y sincronizarlas a mano.
        /// </remarks>
        float _apertura;

        void Animar()
        {
            float destino = Abierto ? 1f : 0f;
            if (Mathf.Approximately(_apertura, destino)) return;

            // unscaledDeltaTime: el panel controla la velocidad del juego, así que si se
            // animara con el tiempo del juego se abriría a cámara lenta en pausa — justo
            // cuando más falta hace poder tocarlo.
            float paso = Time.unscaledDeltaTime / Mathf.Max(0.02f, duracionApertura);
            _apertura = Mathf.MoveTowards(_apertura, destino, paso);
        }

        /// <summary>Curva de entrada: rápida al principio y frenando al final.</summary>
        static float Suave(float t) => 1f - (1f - t) * (1f - t);

        readonly HashSet<int> _inmortales = new HashSet<int>();
        readonly List<Unidad> _temporal = new List<Unidad>();

        Vector2 _scroll;
        TipoUnidad _tipoAAparecer = TipoUnidad.Guerrero;
        bool _colocando;

        GUIStyle _seccion, _hueco, _texto, _textoVivo, _sello, _ayuda;
        bool _estilosListos;


        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;

            // Devolver el tiempo a la normalidad al apagar el panel no es cortesía: el
            // timeScale es global y sobrevive al componente. Salir de Play con x4 puesto
            // deja la siguiente sesión corriendo al cuádruple sin ninguna pista de por qué.
            Time.timeScale = 1f;
        }

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return;

            if (teclado.f1Key.wasPressedThisFrame) Abierto = !Abierto;

            Animar();

            if (_colocando) LeerColocacion();
            if (_pincel != null || _borrando) LeerPincel();

            // La inmortalidad se reaplica cada fotograma porque las unidades nuevas nacen
            // mortales: un bando marcado como inmortal tiene que incluir a las que entrene
            // después, o el interruptor solo valdría para el ejército que ya estaba.
            if (_inmortales.Count > 0) ReaplicarInmortalidad();
        }

        // -----------------------------------------------------------------
        // Acciones
        // -----------------------------------------------------------------

        /// <summary>
        /// Cambia el bando que controla el jugador, en caliente.
        /// </summary>
        /// <remarks>
        /// Es el interruptor que justifica el panel entero. Permite enseñar una batalla desde
        /// los dos lados sin IA: llevas a los azules contra las torres amarillas, te pasas a
        /// los amarillos, contraatacas, y te vuelves a pasar a los azules para ver la derrota
        /// desde el lado que la sufre.
        /// </remarks>
        public void CambiarBando(int bando)
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null || selector.faccionJugador == bando) return;

            // Soltar la selección ANTES de cambiar de bando. Si no, quedan seleccionadas
            // unidades que ya son enemigas y el panel de acciones ofrece ordenárselas.
            selector.SoltarTodo();
            selector.faccionJugador = bando;

            var hud = Object.FindFirstObjectByType<HudRecursos>();
            if (hud != null) hud.CambiarFaccion(bando);

            IrAlCastillo(bando);
        }

        void IrAlCastillo(int bando)
        {
            var camara = Object.FindFirstObjectByType<CamaraRTS>();
            if (camara == null) return;

            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion != bando || !e.centroDeEntrega) continue;

                camara.CentrarEn(e.PuntoDeEntrega);
                return;
            }
        }

        public void Regalar(TipoRecurso recurso, int cantidad)
        {
            var eco = Economia.Actual;
            var selector = SelectorDeUnidades.Actual;
            if (eco == null || selector == null) return;

            eco.Depositar(selector.faccionJugador, recurso, cantidad);
        }

        public void Vaciar()
        {
            var eco = Economia.Actual;
            var selector = SelectorDeUnidades.Actual;
            if (eco == null || selector == null) return;

            var almacen = eco.Almacen(selector.faccionJugador);
            if (almacen == null) return;

            eco.Depositar(selector.faccionJugador, TipoRecurso.Oro,
                          -almacen.Cantidad(TipoRecurso.Oro));
            eco.Depositar(selector.faccionJugador, TipoRecurso.Madera,
                          -almacen.Cantidad(TipoRecurso.Madera));
            eco.Depositar(selector.faccionJugador, TipoRecurso.Carne,
                          -almacen.Cantidad(TipoRecurso.Carne));
        }

        /// <summary>
        /// Enciende o apaga la inmortalidad de un bando.
        /// </summary>
        /// <remarks>
        /// Apagarla no deshace nada porque nunca se tocó ninguna ficha: la inmortalidad es
        /// una curación continua, así que basta con dejar de curar.
        /// </remarks>
        public void AlternarInmortalidad(int bando)
        {
            if (!_inmortales.Remove(bando)) _inmortales.Add(bando);
        }

        public bool EsInmortal(int bando) => _inmortales.Contains(bando);

        /// <summary>
        /// Marca como invulnerables a las unidades vivas de los bandos protegidos.
        /// </summary>
        /// <remarks>
        /// La bandera vive en <c>DatosUnidad</c>, que es un <c>ScriptableObject</c>
        /// COMPARTIDO por todas las unidades del mismo tipo — tocarlo ahí haría inmortales
        /// también a las del enemigo, y encima dejaría el asset modificado en disco después
        /// de salir de Play. Por eso se cura en vez de blindar: se les devuelve la vida
        /// perdida, que consigue lo mismo sin tocar ningún dato compartido.
        /// </remarks>
        void ReaplicarInmortalidad()
        {
            var registro = RegistroDeUnidades.Todas;

            for (int i = 0; i < registro.Count; i++)
            {
                var u = registro[i];
                if (u == null || !u.Viva || !_inmortales.Contains(u.faccion)) continue;
                if (u.datos == null) continue;

                if (u.Vida < u.datos.vidaMaxima) u.Curar(u.datos.vidaMaxima);
            }
        }

        public void MatarSeleccionadas()
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            _temporal.Clear();
            _temporal.AddRange(selector.Seleccionadas);

            for (int i = 0; i < _temporal.Count; i++)
            {
                var u = _temporal[i];
                if (u != null && u.Viva) u.RecibirDano(u.Vida);
            }
        }

        public void CurarSeleccionadas()
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            var seleccion = selector.Seleccionadas;
            for (int i = 0; i < seleccion.Count; i++)
            {
                var u = seleccion[i];
                if (u != null && u.Viva && u.datos != null) u.Curar(u.datos.vidaMaxima);
            }
        }

        // -----------------------------------------------------------------
        // Aparecer unidades
        // -----------------------------------------------------------------

        void LeerColocacion()
        {
            var raton = Mouse.current;
            if (raton == null) return;

            if (raton.rightButton.wasPressedThisFrame) { _colocando = false; return; }
            if (!raton.leftButton.wasPressedThisFrame) return;

            Vector2 pantalla = raton.position.ReadValue();
            if (pantalla.x < AnchoVisible()) return;

            var camara = Camera.main;
            if (camara == null) return;

            Vector3 punto = camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 0f));
            punto.z = 0f;

            Aparecer(_tipoAAparecer, punto);
        }

        /// <summary>
        /// Suelta una unidad del tipo pedido en un punto del mapa.
        /// </summary>
        /// <remarks>
        /// Reutiliza las plantillas que ya usan los edificios para entrenar. Es lo único que
        /// funciona: las tiras de animación se resuelven a sprites cuando se construye la
        /// escena, así que una unidad creada de cero en ejecución saldría sin dibujo.
        /// </remarks>
        public void Aparecer(TipoUnidad tipo, Vector3 punto)
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            var catalogo = CatalogoDePlantillas.Actual;
            if (catalogo == null) return;

            var plantilla = catalogo.Obtener(tipo, selector.faccionJugador);
            if (plantilla == null) return;

            var copia = Object.Instantiate(plantilla, punto, Quaternion.identity,
                                           plantilla.transform.parent);
            copia.name = $"{tipo}_pruebas";
            copia.SetActive(true);
        }

        // -----------------------------------------------------------------
        // Dibujo
        // -----------------------------------------------------------------

        /// <summary>
        /// Lo que ocupa el panel ahora mismo, en píxeles de pantalla. Lo consulta el HUD para
        /// apartar el contador de población.
        /// </summary>
        /// <remarks>
        /// Sale de la apertura animada y no del booleano, así el contador de población se
        /// aparta <b>acompañando</b> al panel en vez de saltar de golpe al final.
        /// </remarks>
        public float AnchoVisible() => Mathf.Lerp(ladoPestana, ancho, Suave(_apertura));

        /// <summary>
        /// Cuánto tiene que apartarse lo que vive pegado al borde izquierdo.
        /// </summary>
        /// <remarks>
        /// Es CERO con el panel plegado, no el ancho del botón. El contador de población va
        /// pegado a la esquina como en cualquier RTS, y solo se mueve cuando el panel llega a
        /// empujarlo de verdad. Usar <c>AnchoVisible</c> para esto lo dejaba permanentemente
        /// desplazado 46 píxeles por un botón que está a media altura y no le estorba nada.
        /// </remarks>
        public float Empuje => Mathf.Lerp(0f, ancho, Suave(_apertura));

        /// <summary>
        /// ¿El puntero está sobre el panel o sobre su botón? Lo pregunta el selector para no
        /// dar órdenes por detrás de la interfaz.
        /// </summary>
        /// <remarks>
        /// Con el panel plegado, la franja ocupada es solo la del botón y solo a media
        /// altura: el resto del borde izquierdo vuelve a ser mapa jugable. Antes se reservaba
        /// una banda de 34 px de arriba abajo, y ahí no se podía ni seleccionar ni ordenar
        /// nada aunque no hubiera nada dibujado.
        /// </remarks>
        public bool CapturaPuntero(Vector2 pantalla)
        {
            if (_apertura > 0.01f && pantalla.x <= AnchoVisible()) return true;

            // GUI mide la Y de arriba abajo y el ratón al revés: hay que dar la vuelta antes
            // de comparar con la banda del botón.
            float y = Screen.height - pantalla.y;
            float mitad = ladoPestana * 0.5f;

            return pantalla.x <= ladoPestana &&
                   Mathf.Abs(y - Screen.height * 0.5f) <= mitad;
        }

        void OnGUI()
        {
            PrepararEstilos();

            float alto = Screen.height;
            float t = Suave(_apertura);

            // El fondo es la mesa de madera del pack, dibujada en nueve cortes para que sus
            // escuadras metalicas conserven el grosor.
            //
            // El panel se DESLIZA desde fuera de la pantalla en vez de encogerse: encogiendo,
            // las escuadras del marco se juntan y el panel parece aplastarse. Deslizando, el
            // marco conserva su forma en todo el recorrido.
            float anchoPanel = ancho;
            float x = Mathf.Lerp(-anchoPanel, 0f, t);

            var marco = new Rect(x, 0f, anchoPanel, alto);

            if (t > 0.001f)
            {
                if (tema != null && tema.panelFondo != null)
                    DibujoGUI.NueveCortes(marco, tema.panelFondo, 48f);
                else
                    GUI.Box(marco, GUIContent.none);
            }

            // El botón que sobresale. Plegado es LO ÚNICO que se ve del panel, así que tiene
            // que leerse como un botón y no como el canto de algo: una raya en el borde de la
            // pantalla no invita a pulsarla, y un panel que solo se abre con una tecla que
            // nadie recuerda es un panel que no existe.
            //
            // Va pegado al borde del panel y viaja con él, así que siempre está donde el ojo
            // lo dejó.
            // Con el panel plegado, marco.xMax vale 0: centrado sobre el borde, medio botón
            // se quedaba fuera de la pantalla. Se ancla al borde y se asoma hacia dentro.
            var pestana = new Rect(Mathf.Max(2f, marco.xMax - ladoPestana * 0.35f),
                                   alto * 0.5f - ladoPestana * 0.5f,
                                   ladoPestana, ladoPestana);

            if (Boton(pestana, Abierto ? "◀" : "▶", null, false)) Abierto = !Abierto;

            DibujarSello();

            if (t < 0.999f) return;

            // El area interior se mete por dentro del marco del pack, que mide 48 px de
            // escuadra. Antes empezaba en 14 y la barra de desplazamiento quedaba montada
            // sobre la moldura, como si el panel se derramara por el borde.
            GUILayout.BeginArea(new Rect(marco.x + 26f, 52f, ancho - 52f, alto - 76f));
            // La barra de desplazamiento vuelve. Se quito para que no montara sobre la
            // moldura del marco, pero ahora el panel tiene mas contenido del que cabe y sin
            // barra no hay forma de saber que queda algo debajo: el ultimo boton se corta y
            // parece un fallo de dibujo, no una lista larga.
            _scroll = GUILayout.BeginScrollView(_scroll, false, true);

            SeccionBando();
            SeccionRecursos();
            SeccionUnidades();
            SeccionTiempo();
            SeccionPartida();
            SeccionAparecer();
            SeccionEditar();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// El rótulo del modo, sobre el listón del pack.
        /// </summary>
        /// <remarks>
        /// Dice <b>PARTIDA LIBRE</b>, que es como se llaman en español las partidas que el
        /// jugador monta a su gusto —sin campaña, sin condiciones impuestas— y que es en lo
        /// que se va a convertir este panel. Se descartó «modo pruebas» porque un rótulo que
        /// dice «pruebas» dentro de un modo publicado se lee como algo sin terminar.
        ///
        /// Sigue cumpliendo lo que tenía que cumplir —que ninguna captura tramposa pase por
        /// buena— porque lo que importa es que se vea que el modo está activo, no la palabra.
        ///
        /// Va abajo a la derecha y no arriba: arriba a la izquierda vive el contador de
        /// población y arriba a la derecha el reloj que llegará con el flujo de partida.
        /// </remarks>
        void DibujarSello()
        {
            const float ancho = 190f;
            const float alto = 46f;

            var sitio = new Rect(Screen.width - ancho - 14f,
                                 Screen.height - alto - 14f, ancho, alto);

            if (tema != null)
            {
                var liston = tema.ListonNombreDe(0);
                if (liston != null) DibujoGUI.Sprite(sitio, liston);
            }

            GUI.Label(sitio, "PARTIDA LIBRE", _sello);
        }

        void SeccionBando()
        {
            var selector = SelectorDeUnidades.Actual;
            int actual = selector != null ? selector.faccionJugador : 0;

            GUILayout.Label("BANDO", _seccion);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < bandos; i++)
            {
                bool pulsado = GUILayout.Button(GUIContent.none, _hueco, GUILayout.Height(30f));
                var caja = GUILayoutUtility.GetLastRect();

                // La caja de cada bando es la del PACK en su color: el pack ya trae las cinco
                // pintadas, así que no hay que teñir nada. Un «1», «2», «3» no diría nada —
                // el jugador piensa en «los amarillos», no en «la facción 2».
                var fondo = tema != null ? tema.CajaChicaDe(i) : null;
                if (fondo != null) DibujoGUI.NueveCortes(caja, fondo, 20f);

                if (i == actual)
                    GUI.Label(caja, "●", _textoVivo);

                if (pulsado) CambiarBando(i);
            }
            GUILayout.EndHorizontal();
        }

        void SeccionRecursos()
        {
            GUILayout.Label("RECURSOS", _seccion);

            GUILayout.BeginHorizontal();
            if (BotonConIcono("+100", Icono(TipoRecurso.Oro))) Regalar(TipoRecurso.Oro, 100);
            if (BotonConIcono("+100", Icono(TipoRecurso.Madera))) Regalar(TipoRecurso.Madera, 100);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (BotonConIcono("+50", Icono(TipoRecurso.Carne))) Regalar(TipoRecurso.Carne, 50);
            if (BotonConIcono("Vaciar", null)) Vaciar();
            GUILayout.EndHorizontal();
        }

        void SeccionUnidades()
        {
            var selector = SelectorDeUnidades.Actual;
            int actual = selector != null ? selector.faccionJugador : 0;

            GUILayout.Label("UNIDADES", _seccion);

            if (BotonConIcono(EsInmortal(actual) ? "Mortal de nuevo" : "Bando inmortal",
                              null, EsInmortal(actual)))
                AlternarInmortalidad(actual);

            GUILayout.BeginHorizontal();
            if (BotonConIcono("Matar", null)) MatarSeleccionadas();
            if (BotonConIcono("Curar", null)) CurarSeleccionadas();
            GUILayout.EndHorizontal();
        }

        void SeccionTiempo()
        {
            GUILayout.Label("TIEMPO", _seccion);

            GUILayout.BeginHorizontal();
            Velocidad("||", 0f);
            Velocidad("x1", 1f);
            Velocidad("x2", 2f);
            Velocidad("x4", 4f);
            GUILayout.EndHorizontal();
        }

        void Velocidad(string texto, float escala)
        {
            bool vivo = Mathf.Approximately(Time.timeScale, escala);
            if (BotonConIcono(texto, null, vivo)) Time.timeScale = escala;
        }

        /// <summary>
        /// Atajos para ver el cartel de final sin jugar diez minutos.
        /// </summary>
        /// <remarks>
        /// Es el interruptor que más se acaba usando. Sin él, cada retoque en la animación
        /// del cartel cuesta una partida entera, y nadie retoca nada que cueste eso.
        /// </remarks>
        void SeccionPartida()
        {
            GUILayout.Label("PARTIDA", _seccion);

            if (BotonConIcono("Ver cartel final", null)) VerCartel();

            if (BotonConIcono("Arrasar rivales", null)) ArrasarRivales();
        }

        void VerCartel()
        {
            var cartel = Object.FindFirstObjectByType<CartelDeFinal>();
            if (cartel != null) cartel.Mostrar();
        }

        /// <summary>
        /// Derriba el castillo de todos los demás bandos: victoria inmediata.
        /// </summary>
        /// <remarks>
        /// No se fuerza el resultado a mano, se provoca la CAUSA. Poniendo el ganador a dedo
        /// se probaría la pantalla pero no el árbitro, que es la mitad que puede fallar de
        /// verdad; así se recorre el mismo camino que en una partida real.
        /// </remarks>
        void ArrasarRivales()
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            var castillos = new List<Edificio>();
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion == selector.faccionJugador) continue;
                if (!e.centroDeEntrega) continue;

                castillos.Add(e);
            }

            // Se copian antes de derribar: caer saca al edificio del registro que se estaría
            // recorriendo, y eso se salta la mitad de la lista.
            for (int i = 0; i < castillos.Count; i++) castillos[i].Derribar();
        }

        static readonly TipoUnidad[] Aparecibles =
        {
            TipoUnidad.Pawn, TipoUnidad.Guerrero, TipoUnidad.Lancero,
            TipoUnidad.Arquero, TipoUnidad.Monje,
        };

        void SeccionAparecer()
        {
            GUILayout.Label("APARECER", _seccion);

            for (int i = 0; i < Aparecibles.Length; i++)
            {
                var tipo = Aparecibles[i];
                bool vivo = _colocando && _tipoAAparecer == tipo;

                var selector = SelectorDeUnidades.Actual;
                int faccion = selector != null ? selector.faccionJugador : 0;
                var cara = tema != null ? tema.RetratoDe(tipo, faccion) : null;

                if (!BotonConIcono(tipo.ToString(), cara, vivo)) continue;

                _tipoAAparecer = tipo;
                _colocando = !vivo;
            }

            if (_colocando) Ayuda("Clic en el mapa", "Dcho.: cancelar");
        }

        Sprite Icono(TipoRecurso recurso)
        {
            if (tema == null) return null;

            switch (recurso)
            {
                case TipoRecurso.Oro: return tema.iconoRecursoOro;
                case TipoRecurso.Madera: return tema.iconoRecursoMadera;
                case TipoRecurso.Carne: return tema.iconoRecursoCarne;
                default: return null;
            }
        }

        /// <summary>
        /// Un botón con el icono del pack a la izquierda del texto.
        /// </summary>
        /// <remarks>
        /// Los iconos NO se dibujan de cero. Se planteó generarlos extrayendo la paleta de
        /// los PNG del pack, y al ir a hacerlo resultó que no hacía falta: el tema ya trae
        /// los tres sacos de recurso y los veinticinco retratos de unidad, que son
        /// exactamente los iconos que este panel necesita.
        /// </remarks>
        bool BotonConIcono(string texto, Sprite icono, bool vivo = false)
        {
            // Se reserva el hueco con un botón SIN fondo y luego se pinta encima. Es la única
            // forma de usar el arte del pack en OnGUI: un GUIStyle solo acepta una textura
            // entera como fondo, y los botones del pack son recortes de un atlas.
            bool pulsado = GUILayout.Button(GUIContent.none, _hueco, GUILayout.Height(30f));
            var caja = GUILayoutUtility.GetLastRect();

            Pintar(caja, texto, icono, vivo);
            return pulsado;
        }

        /// <summary>Un botón suelto, fuera de la disposición automática.</summary>
        bool Boton(Rect caja, string texto, Sprite icono, bool vivo)
        {
            bool pulsado = GUI.Button(caja, GUIContent.none, _hueco);
            Pintar(caja, texto, icono, vivo);
            return pulsado;
        }

        /// <summary>El fondo del pack, el icono y el texto, en ese orden.</summary>
        void Pintar(Rect caja, string texto, Sprite icono, bool vivo)
        {
            var fondo = tema != null ? tema.CajaChicaDe(vivo ? 2 : 0) : null;

            if (fondo != null) DibujoGUI.NueveCortes(caja, fondo, 20f);
            else GUI.Box(caja, GUIContent.none);

            if (icono != null)
            {
                float lado = caja.height - 8f;
                DibujoGUI.Sprite(new Rect(caja.x + 5f, caja.y + 4f, lado, lado), icono);
            }

            if (string.IsNullOrEmpty(texto)) return;

            // El texto se centra en el BOTON ENTERO, no en lo que sobra a la derecha del
            // icono. Descontar el icono de un lado y la misma cifra del otro —que es lo que
            // hacia antes— desplaza la caja pero no la recentra: todas las etiquetas salian
            // corridas hacia la derecha, y las que llevaban icono el doble.
            var hueco = new Rect(caja.x, caja.y - 1f, caja.width, caja.height);
            var estilo = vivo ? _textoVivo : _texto;

            // Sombra dura debajo. Las cajas del pack son azul medio con vetas, y cualquier
            // texto plano encima se deshace: es lo que hacia que costara leerlo.
            var tinta = GUI.color;
            GUI.color = new Color(0.05f, 0.09f, 0.16f, 0.9f);
            GUI.Label(new Rect(hueco.x + 1f, hueco.y + 2f, hueco.width, hueco.height),
                      texto, estilo);

            GUI.color = tinta;
            GUI.Label(hueco, texto, estilo);
        }

        // -----------------------------------------------------------------
        // Editor de mapa en caliente
        // -----------------------------------------------------------------

        /// <summary>Qué está pintando el pincel, o nada.</summary>
        SemillaMapa? _pincel;

        /// <summary>
        /// True mientras se está pintando o borrando. Lo consulta la cámara.
        /// </summary>
        /// <remarks>
        /// Con un pincel en la mano, la rueda cambia de variante. Sin esto haría las dos
        /// cosas a la vez —cambiar el árbol y alejar el mapa— que es el mismo choque de
        /// gestos que ya hubo con el desplazamiento del panel.
        /// </remarks>
        public bool PincelActivo => _pincel != null || _borrando;

        int _varianteDePincel;
        bool _borrando;

        static readonly SemillaMapa[] Sembrables =
        {
            SemillaMapa.Oro, SemillaMapa.Arbol, SemillaMapa.Piedra,
            SemillaMapa.Arbusto, SemillaMapa.Oveja,
        };

        /// <summary>
        /// Los pinceles del editor de mapa.
        /// </summary>
        /// <remarks>
        /// Es el primer trozo de la épica del editor (E13), adelantado aquí porque el panel
        /// ya existe y porque un mapa hecho a mano vale más que uno que salió del ruido: un
        /// cuello de botella colocado con intención dice algo, uno aleatorio no.
        ///
        /// <b>De momento pinta recursos, no terreno.</b> El pintado del tilemap y su paleta
        /// viven en el ensamblado de Editor, que no existe en una partida: repintar tierra en
        /// caliente exige moverlo a ejecución, y eso es un trabajo con entidad propia.
        /// </remarks>
        void SeccionEditar()
        {
            GUILayout.Label("EDITAR MAPA", _seccion);

            for (int i = 0; i < Sembrables.Length; i++)
            {
                var semilla = Sembrables[i];
                bool vivo = _pincel == semilla && !_borrando;

                if (!BotonConIcono(Nombre(semilla), null, vivo)) continue;

                _pincel = vivo ? (SemillaMapa?)null : semilla;
                _borrando = false;
                _varianteDePincel = 0;
            }

            if (BotonConIcono("Borrar", null, _borrando))
            {
                _borrando = !_borrando;
                if (_borrando) _pincel = null;
            }

            if (_pincel != null) Ayuda("Clic para sembrar", "Rueda: variante");
            else if (_borrando) Ayuda("Clic para quitar", "Dcho.: soltar");
        }

        /// <summary>
        /// Una nota de ayuda de dos lineas, legible sobre la madera.
        /// </summary>
        /// <remarks>
        /// El estilo de seccion no vale para esto: es ambar, pequeno y esta pensado para
        /// titulos de dos palabras. Una frase entera con ese estilo se pierde contra las
        /// vetas del fondo, que es exactamente lo que pasaba.
        /// </remarks>
        void Ayuda(string primera, string segunda)
        {
            GUILayout.Space(2f);
            GUILayout.Label(primera, _ayuda);
            GUILayout.Label(segunda, _ayuda);
            GUILayout.Space(2f);
        }

        static string Nombre(SemillaMapa semilla)
        {
            switch (semilla)
            {
                case SemillaMapa.Oro: return "Mena de oro";
                case SemillaMapa.Arbol: return "Árbol";
                case SemillaMapa.Piedra: return "Piedra";
                case SemillaMapa.Arbusto: return "Arbusto";
                default: return "Oveja";
            }
        }

        /// <summary>
        /// Lee el ratón mientras hay un pincel o el borrador activos.
        /// </summary>
        /// <remarks>
        /// Se pinta con el botón MANTENIDO y no solo con la pulsación: sembrar un bosque a
        /// clic por árbol es trabajo de chinos. El límite por celda evita que arrastrar
        /// despacio siembre diez árboles en el mismo sitio.
        /// </remarks>
        void LeerPincel()
        {
            var raton = Mouse.current;
            if (raton == null) return;

            if (raton.rightButton.wasPressedThisFrame)
            {
                _pincel = null;
                _borrando = false;
                return;
            }

            // La rueda pasa de una variante a otra, igual que las fachadas de las casas.
            float rueda = raton.scroll.ReadValue().y;
            if (_pincel != null && Mathf.Abs(rueda) > 0.01f)
                _varianteDePincel += (int)Mathf.Sign(rueda);

            if (!raton.leftButton.isPressed) { _ultimaCelda = null; return; }

            Vector2 pantalla = raton.position.ReadValue();
            if (CapturaPuntero(pantalla)) return;

            var camara = Camera.main;
            var mundo = MundoJuego.Actual;
            if (camara == null || mundo == null || mundo.Grilla == null) return;

            Vector3 punto = camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 0f));
            punto.z = 0f;

            var celda = mundo.Grilla.MundoACelda(punto);
            if (_ultimaCelda == celda) return;

            _ultimaCelda = celda;

            if (_borrando) Borrar(punto);
            else Sembrar(celda);
        }

        Vector2Int? _ultimaCelda;

        void Sembrar(Vector2Int celda)
        {
            var catalogo = CatalogoDeRecursos.Actual;
            if (catalogo == null) return;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            // No se siembra sobre agua ni sobre algo que ya está ocupado: un árbol dentro de
            // un lago o encima de una casa es un mapa que no se puede jugar, y el editor no
            // debería poder producir eso ni por accidente.
            if (!mundo.Grilla.Transitable(celda.x, celda.y)) return;

            catalogo.Sembrar(_pincel.Value, _varianteDePincel, celda);
        }

        /// <summary>
        /// Quita el nodo que haya bajo el punto y devuelve su terreno.
        /// </summary>
        /// <remarks>
        /// Libera la grilla con el mismo camino que usa un pawn al talar un árbol. Borrar sin
        /// liberar dejaría celdas bloqueadas por algo que ya no está: un muro invisible en
        /// mitad del mapa, que es de los fallos más difíciles de relacionar con su causa.
        /// </remarks>
        void Borrar(Vector3 punto)
        {
            var nodo = NodoRecurso.NodoEn(punto, 1.1f);
            if (nodo == null) return;

            var mundo = MundoJuego.Actual;
            if (mundo != null) mundo.LiberarRecurso(nodo.celda, nodo.radioBloqueo);

            Destroy(nodo.gameObject);
        }

        void PrepararEstilos()
        {
            if (_estilosListos) return;
            _estilosListos = true;

            _seccion = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(2, 0, 6, 2),
            };
            _seccion.normal.textColor = new Color(1f, 0.86f, 0.55f);

            // Botón sin nada: solo reserva el hueco y detecta el clic. Todo lo que se ve se
            // pinta encima con el arte del pack.
            _hueco = new GUIStyle(GUI.skin.button);
            _hueco.normal.background = null;
            _hueco.hover.background = null;
            _hueco.active.background = null;
            _hueco.focused.background = null;
            _hueco.border = new RectOffset(0, 0, 0, 0);
            _hueco.margin = new RectOffset(2, 2, 2, 2);

            // Blanco cálido sobre la madera. El gris de Unity sobre el listón del pack tenía
            // muy poco contraste y costaba leerlo, que es lo peor que le puede pasar a un
            // panel cuya única razón de ser es que se use deprisa.
            _texto = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _texto.normal.textColor = Color.white;

            _textoVivo = new GUIStyle(_texto) { fontStyle = FontStyle.Bold };
            _textoVivo.normal.textColor = new Color(0.74f, 1f, 0.62f);

            // Blanco puro y centrado, con sitio propio. Las notas de ayuda son lo unico del
            // panel que el jugador tiene que LEER en vez de reconocer de un vistazo.
            _ayuda = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            _ayuda.normal.textColor = Color.white;

            _sello = new GUIStyle(GUI.skin.label)
            {
                font = tema != null ? tema.titular : null,
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
            };
            _sello.normal.textColor = new Color(1f, 0.93f, 0.78f);
        }
    }
}
