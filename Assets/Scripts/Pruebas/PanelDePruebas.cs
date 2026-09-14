using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Interfaz;
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
        public float ancho = 214f;

        [Tooltip("Bandos que existen en la partida. Lo fija el generador de la escena.")]
        [Range(1, 5)] public int bandos = 3;

        [Tooltip("De aquí salen los iconos. Lo asigna el generador de la escena.")]
        public TemaInterfaz tema;

        /// <summary>¿Está abierto? Lo consultan el cursor y el selector para no pisarse.</summary>
        public bool Abierto { get; private set; }

        readonly HashSet<int> _inmortales = new HashSet<int>();
        readonly List<Unidad> _temporal = new List<Unidad>();

        Vector2 _scroll;
        TipoUnidad _tipoAAparecer = TipoUnidad.Guerrero;
        bool _colocando;

        GUIStyle _seccion, _boton, _botonVivo, _sello;
        bool _estilosListos;

        Texture2D _plano;

        /// <summary>Un pixel blanco para pintar cuadros de color. Se crea una sola vez.</summary>
        Texture2D Textura()
        {
            if (_plano != null) return _plano;

            _plano = new Texture2D(1, 1);
            _plano.SetPixel(0, 0, Color.white);
            _plano.Apply();
            return _plano;
        }

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;

            // Devolver el tiempo a la normalidad al apagar el panel no es cortesía: el
            // timeScale es global y sobrevive al componente. Salir de Play con x4 puesto
            // deja la siguiente sesión corriendo al cuádruple sin ninguna pista de por qué.
            Time.timeScale = 1f;

            if (_plano != null) Destroy(_plano);
        }

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return;

            if (teclado.f1Key.wasPressedThisFrame) Abierto = !Abierto;

            if (_colocando) LeerColocacion();

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
        public float AnchoVisible() => Abierto ? ancho : 34f;

        /// <summary>¿El puntero está sobre el panel? Lo pregunta el selector para no ordenar detrás.</summary>
        public bool CapturaPuntero(Vector2 pantalla) => pantalla.x <= AnchoVisible();

        void OnGUI()
        {
            PrepararEstilos();

            float alto = Screen.height;

            // El fondo es la mesa de madera del pack, dibujada en nueve cortes para que sus
            // escuadras metalicas conserven el grosor. Antes era el GUI.Box gris de Unity, y
            // desentonaba con todo lo demas: esto no es una ventana de depuracion que se tira
            // a la basura, es el futuro modo practica y tiene que parecer parte del juego.
            var marco = new Rect(0f, 0f, AnchoVisible(), alto);

            if (tema != null && tema.panelFondo != null)
                DibujoGUI.NueveCortes(marco, tema.panelFondo, 48f);
            else
                GUI.Box(marco, GUIContent.none);

            // La pestaña siempre visible. Un panel que solo se abre con una tecla que nadie
            // recuerda es un panel que no existe.
            if (GUI.Button(new Rect(4f, 6f, 26f, 26f), Abierto ? "<" : ">"))
                Abierto = !Abierto;

            DibujarSello();

            if (!Abierto) return;

            GUILayout.BeginArea(new Rect(14f, 46f, ancho - 28f, alto - 62f));
            _scroll = GUILayout.BeginScrollView(_scroll);

            SeccionBando();
            SeccionRecursos();
            SeccionUnidades();
            SeccionTiempo();
            SeccionPartida();
            SeccionAparecer();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// El rótulo del modo, sobre el listón del pack.
        /// </summary>
        /// <remarks>
        /// Dice <b>MODO LIBRE</b> y no «modo pruebas» porque esto no es un andamio que se tire
        /// a la basura: las mismas funciones van a ser el modo libre que el jugador podrá
        /// elegir para montar sus partidas. Un rótulo que diga «pruebas» dentro de un modo
        /// publicado se lee como algo sin terminar.
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

            GUI.Label(sitio, "MODO LIBRE", _sello);
        }

        void SeccionBando()
        {
            var selector = SelectorDeUnidades.Actual;
            int actual = selector != null ? selector.faccionJugador : 0;

            GUILayout.Label("BANDO", _seccion);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < bandos; i++)
            {
                bool pulsado = GUILayout.Button(i == actual ? "●" : " ",
                                                i == actual ? _botonVivo : _boton,
                                                GUILayout.Height(26f));

                // El botón se tiñe del color real del bando. Un «1», «2», «3» no dice nada:
                // el jugador piensa en «los amarillos», no en «la facción 2».
                if (tema != null)
                {
                    var caja = GUILayoutUtility.GetLastRect();
                    var color = GUI.color;
                    GUI.color = new Color(tema.ColorDe(i).r, tema.ColorDe(i).g,
                                          tema.ColorDe(i).b, 0.55f);
                    GUI.DrawTexture(new Rect(caja.x + 3f, caja.y + 3f,
                                             caja.width - 6f, caja.height - 6f), Textura());
                    GUI.color = color;
                }

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
            if (GUILayout.Button("Vaciar", _boton)) Vaciar();
            GUILayout.EndHorizontal();
        }

        void SeccionUnidades()
        {
            var selector = SelectorDeUnidades.Actual;
            int actual = selector != null ? selector.faccionJugador : 0;

            GUILayout.Label("UNIDADES", _seccion);

            if (GUILayout.Button(EsInmortal(actual) ? "Mortal de nuevo" : "Bando inmortal",
                                 EsInmortal(actual) ? _botonVivo : _boton))
                AlternarInmortalidad(actual);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Matar sel.", _boton)) MatarSeleccionadas();
            if (GUILayout.Button("Curar sel.", _boton)) CurarSeleccionadas();
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
            if (GUILayout.Button(texto, vivo ? _botonVivo : _boton)) Time.timeScale = escala;
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

            if (GUILayout.Button("Ver cartel de final", _boton)) VerCartel();

            if (GUILayout.Button("Arrasar bandos rivales", _boton)) ArrasarRivales();
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

            if (_colocando)
                GUILayout.Label("Clic en el mapa · derecho cancela", _seccion);
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
        /// exactamente los iconos que este panel necesita. Arte nuevo para enseñar algo que
        /// ya está dibujado es arte que además puede desentonar.
        ///
        /// Hay que pintar el icono encima del botón en vez de pasárselo como contenido
        /// porque un <c>Sprite</c> del pack es un recorte de un atlas: <c>GUIContent</c> solo
        /// acepta texturas enteras y dibujaría la hoja completa.
        /// </remarks>
        bool BotonConIcono(string texto, Sprite icono, bool vivo = false)
        {
            bool pulsado = GUILayout.Button("     " + texto, vivo ? _botonVivo : _boton,
                                            GUILayout.Height(26f));

            if (icono == null) return pulsado;

            var caja = GUILayoutUtility.GetLastRect();
            var sitio = new Rect(caja.x + 3f, caja.y + 3f, 20f, 20f);

            DibujarSprite(sitio, icono);
            return pulsado;
        }

        static void DibujarSprite(Rect donde, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;

            var r = sprite.textureRect;
            var uv = new Rect(r.x / sprite.texture.width,
                              r.y / sprite.texture.height,
                              r.width / sprite.texture.width,
                              r.height / sprite.texture.height);

            GUI.DrawTextureWithTexCoords(donde, sprite.texture, uv, true);
        }

        void PrepararEstilos()
        {
            if (_estilosListos) return;
            _estilosListos = true;

            _seccion = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            _seccion.normal.textColor = new Color(1f, 0.90f, 0.72f);
            _boton = new GUIStyle(GUI.skin.button) { fontSize = 11 };

            _botonVivo = new GUIStyle(_boton) { fontStyle = FontStyle.Bold };
            _botonVivo.normal.textColor = new Color(0.45f, 0.92f, 0.42f);

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
