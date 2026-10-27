using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Pantalla de título, menú principal, preparar partida, opciones y créditos.
    /// </summary>
    /// <remarks>
    /// <b>La mesa de guerra.</b> Todo el menú se dibuja como la mesa de un comandante: marcos
    /// de madera, contenedores de color dentro, el mapa en un pergamino y las entradas del
    /// menú principal como espadas que se desenvainan. Son piezas del propio pack, así que
    /// encaja con el juego sin dibujar nada nuevo.
    ///
    /// La composición del menú principal es asimétrica a propósito —el logo a la izquierda,
    /// las espadas a la derecha—, y deja el centro libre para que se vea la partida de bots
    /// que corre detrás: el fondo es lo más vistoso del menú y taparlo sería desperdiciarlo.
    ///
    /// Vive dentro de la escena de juego y solo se enciende en modo menú (ADR-26).
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Menú principal")]
    public partial class MenuPrincipal : MonoBehaviour
    {
        [System.Serializable]
        public struct FichaDeMapa
        {
            public string nombre;
            [TextArea] public string descripcion;
            public string escena;
            [Range(2, 5)] public int bandos;
            public Sprite miniatura;
            [Tooltip("Posición de cada base en el mapa, de 0 a 1. Para marcarlas en la miniatura.")]
            public Vector2[] bases;

            /// <summary>Si el mapa viene de un archivo (semana 13). Null es la escena: Tres Coronas.</summary>
            [System.NonSerialized] public MapaGuardado archivo;
            [System.NonSerialized] public bool oficial;
            [System.NonSerialized] public string ruta;
        }

        /// <summary>La lista que se enseña: los mapas de la escena y los de archivo.</summary>
        readonly List<FichaDeMapa> _lista = new List<FichaDeMapa>();
        GameObject _botonesEditor, _dialogoNuevo;
        Text _valorTamanoNuevo;
        int _tamanoNuevo = 160;

        public TemaInterfaz tema;
        public Sprite logo;

        [Header("Piezas del pack")]
        [Tooltip("Las cinco espadas del pack, en el orden de los colores.")]
        public Sprite[] espadas = new Sprite[5];
        public Sprite pergamino;
        public Sprite perilla;

        [Tooltip("Los mapas que se pueden elegir. El generador pone el de la escena.")]
        public FichaDeMapa[] mapas = new FichaDeMapa[0];

        public string version = "v0.12 · Grupo 01 · UTP";

        enum Pantalla { Titulo, Principal, Partida, Opciones, Creditos }

        const int Azul = 0, Rojo = 1, Amarillo = 2, Morado = 3, Negro = 4;

        readonly Dictionary<Pantalla, GameObject> _pantallas = new Dictionary<Pantalla, GameObject>();
        Pantalla _actual;
        Font _fuente;

        RectTransform _logo;
        Vector2 _logoAncla, _logoTam;

        // Preparar partida
        ModoDePartida _modoAPreparar;
        int _mapa;
        ConfiguracionDePartida _preparada;
        Text _titulo, _nombreMapa, _descripcion, _motivo, _cabeceraReglas;
        Image _miniatura, _cintaPartida;
        Button _empezar;
        RectTransform _columnaBases, _bloqueReglas;
        readonly List<FilaDeBase> _filas = new List<FilaDeBase>();
        readonly List<(Image caja, Text numero)> _marcas = new List<(Image, Text)>();
        readonly List<Text> _valoresReglas = new List<Text>();

        // Opciones
        Text _valorVolumen, _valorSonido, _valorCompleta, _valorVsync, _valorFps, _valorCamara, _valorBorde;

        class FilaDeBase
        {
            public RectTransform raiz;
            public Image caja, retrato;
            public Text nombre, color, ocupante;
        }

        Sprite Boton(int color) => tema != null ? tema.BotonDeColor(color) : null;
        Sprite Caja(int color) => tema != null ? tema.CajaDeColor(color) : null;
        Sprite CajaChica(int color) => tema != null ? tema.CajaChicaDeColor(color) : null;

        // -----------------------------------------------------------------

        void Awake()
        {
            _fuente = tema != null && tema.titular != null
                ? tema.titular
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var lienzo = KitInterfaz.Lienzo(transform, "LienzoMenu", 50);
            Construir((RectTransform)lienzo.transform);

            Ir(Navegacion.TituloVisto ? Pantalla.Principal : Pantalla.Titulo, instantaneo: true);
        }

        void Update()
        {
            AnimarLogo();

            var teclado = Keyboard.current;
            var raton = Mouse.current;

            if (_actual == Pantalla.Titulo)
            {
                bool tecla = teclado != null && teclado.anyKey.wasPressedThisFrame;
                bool clic = raton != null && raton.leftButton.wasPressedThisFrame;

                if (tecla || clic)
                {
                    Navegacion.TituloVisto = true;
                    Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.BotonPanel);
                    Ir(Pantalla.Principal);
                }

                return;
            }

            if (teclado != null && teclado.escapeKey.wasPressedThisFrame && _actual != Pantalla.Principal)
                Ir(Pantalla.Principal);
        }

        void Ir(Pantalla p, bool instantaneo = false)
        {
            _actual = p;
            foreach (var par in _pantallas) par.Value.SetActive(par.Key == p);

            // El logo empieza grande y centrado en el título y, al entrar al menú, se desliza a
            // la izquierda. Ese desplazamiento es la transición entre las dos pantallas.
            if (_logo != null)
            {
                bool visible = p == Pantalla.Titulo || p == Pantalla.Principal;
                _logo.gameObject.SetActive(visible);

                // Por anclas y no por píxeles: así cae en el mismo sitio en 16:9 y en 16:10.
                _logoAncla = p == Pantalla.Titulo ? new Vector2(0.5f, 0.56f) : new Vector2(0.29f, 0.53f);
                _logoTam = p == Pantalla.Titulo ? new Vector2(940f, 640f) : new Vector2(820f, 558f);

                if (instantaneo)
                {
                    _logo.anchorMin = _logo.anchorMax = _logoAncla;
                    _logo.sizeDelta = _logoTam;
                }
            }

            if (p == Pantalla.Partida) PintarPartida();
            if (p == Pantalla.Opciones) PintarOpciones();
        }

        void AnimarLogo()
        {
            if (_logo == null || !_logo.gameObject.activeSelf) return;

            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f);

            var ancla = Vector2.Lerp(_logo.anchorMin, _logoAncla, k);
            _logo.anchorMin = _logo.anchorMax = ancla;
            _logo.sizeDelta = Vector2.Lerp(_logo.sizeDelta, _logoTam, k);
        }

        // -----------------------------------------------------------------
        // Montaje general
        // -----------------------------------------------------------------

        void Construir(RectTransform raiz)
        {
            var velo = KitInterfaz.Velo(raiz, 0.22f);
            velo.raycastTarget = false;

            _pantallas[Pantalla.Titulo] = ConstruirTitulo(raiz);
            _pantallas[Pantalla.Principal] = ConstruirPrincipal(raiz);
            _pantallas[Pantalla.Partida] = ConstruirPartida(raiz);
            _pantallas[Pantalla.Opciones] = ConstruirOpciones(raiz);
            _pantallas[Pantalla.Creditos] = ConstruirCreditos(raiz);

            ConstruirLogo(raiz);

            var v = KitInterfaz.Texto(raiz, "Version", version, _fuente, 22, KitInterfaz.Crema, TextAnchor.LowerRight);
            KitInterfaz.Colocar(v.rectTransform, new Vector2(1f, 0f), new Vector2(600f, 40f), new Vector2(-320f, 26f));
        }

        void ConstruirLogo(RectTransform raiz)
        {
            if (logo == null) return;

            var img = KitInterfaz.Imagen(raiz, "Logo", logo, Color.white);
            img.preserveAspect = true;
            _logo = img.rectTransform;
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 0.56f);
            _logo.pivot = new Vector2(0.5f, 0.5f);

            var mascara = img.gameObject.AddComponent<Mask>();
            mascara.showMaskGraphic = true;

            var franja = KitInterfaz.Imagen(img.transform, "Brillo", null, new Color(1f, 0.97f, 0.85f, 0.38f));
            franja.rectTransform.sizeDelta = new Vector2(110f, 1400f);
            franja.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -22f);

            var brillo = img.gameObject.AddComponent<BrilloDeLogo>();
            brillo.franja = franja.rectTransform;
        }

        /// <summary>El marco de madera con su cinta de título, el «tablero» de cada pantalla.</summary>
        RectTransform Mesa(RectTransform raiz, Vector2 tamano, string titulo, int colorCinta, out Text cabecera,
                           out Image cinta)
        {
            var madera = tema != null ? tema.panelFondo : null;
            var mesa = KitInterfaz.Imagen(raiz, "Mesa", madera, Color.white);
            mesa.raycastTarget = true;
            KitInterfaz.Colocar(mesa.rectTransform, new Vector2(0.5f, 0.5f), tamano, new Vector2(0f, -20f));

            cinta = KitInterfaz.Imagen(mesa.transform, "Cinta",
                                       tema != null ? tema.ListonPanelDeColor(colorCinta) : null, Color.white);
            KitInterfaz.Colocar(cinta.rectTransform, new Vector2(0.5f, 1f), new Vector2(680f, 150f), new Vector2(0f, 6f));

            cabecera = KitInterfaz.Texto(cinta.transform, "Titulo", titulo, _fuente, 50, KitInterfaz.Crema,
                                         TextAnchor.MiddleCenter);
            KitInterfaz.Llenar(cabecera.rectTransform).offsetMin = new Vector2(120f, 34f);
            cabecera.rectTransform.offsetMax = new Vector2(-120f, -6f);

            return mesa.rectTransform;
        }

        /// <summary>Un contenedor de color dentro de la mesa.</summary>
        RectTransform Contenedor(RectTransform padre, string nombre, int color, Vector2 ancla, Vector2 tamano,
                                 Vector2 desplazamiento, string titulo = null)
        {
            var c = KitInterfaz.Imagen(padre, nombre, Caja(color), Color.white);
            KitInterfaz.Colocar(c.rectTransform, ancla, tamano, desplazamiento);

            if (!string.IsNullOrEmpty(titulo))
            {
                var t = KitInterfaz.Texto(c.transform, "Titulo", titulo, _fuente, 34, KitInterfaz.Crema,
                                          TextAnchor.MiddleCenter);
                KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(tamano.x - 40f, 54f),
                                    new Vector2(0f, -40f));
            }

            return c.rectTransform;
        }

        Button BotonDeAccion(RectTransform padre, string texto, int color, System.Action accion,
                             Vector2 ancla, Vector2 tamano, Vector2 desplazamiento, int letra = 36)
        {
            var b = KitInterfaz.Boton(padre, texto, Boton(color), texto, _fuente, letra, accion);
            KitInterfaz.Colocar((RectTransform)b.transform, ancla, tamano, desplazamiento);
            return b;
        }

        // -----------------------------------------------------------------
        // Título y menú principal
        // -----------------------------------------------------------------

        GameObject ConstruirTitulo(RectTransform raiz)
        {
            var p = KitInterfaz.Llenar(KitInterfaz.Hijo(raiz, "Titulo"));

            var pulsa = KitInterfaz.Texto(p, "Pulsa", "Pulsa cualquier tecla", _fuente, 44,
                                          KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(pulsa.rectTransform, new Vector2(0.5f, 0.15f), new Vector2(900f, 70f));
            pulsa.gameObject.AddComponent<TextoQueLate>();

            return p.gameObject;
        }

        GameObject ConstruirPrincipal(RectTransform raiz)
        {
            var p = KitInterfaz.Llenar(KitInterfaz.Hijo(raiz, "Principal"));

            // Las espadas, en columna a la derecha. Cada una con el color de un bando: el menú
            // ya enseña los cinco colores del juego antes de empezar.
            var entradas = new (string texto, int color, System.Action accion)[]
            {
                ("Jugar", Azul, () => Preparar(ModoDePartida.Partida)),
                ("Tutorial", Rojo, Navegacion.Tutorial),
                ("Sandbox", Amarillo, () => Preparar(ModoDePartida.Sandbox)),
                ("Opciones", Morado, () => Ir(Pantalla.Opciones)),
                ("Créditos", Negro, () => Ir(Pantalla.Creditos)),
                ("Salir", Rojo, Navegacion.Salir),
            };

            for (int i = 0; i < entradas.Length; i++)
            {
                var (texto, color, accion) = entradas[i];
                // Seis espadas desde la semana 15 (entró el tutorial): un poco más juntas para
                // que la columna siga cabiendo entera en 1080 de alto.
                var b = Espada(p, texto, color, accion,
                               new Vector2(-470f, 300f - i * 120f));
                b.gameObject.AddComponent<EfectoDeEspada>().orden = i;
            }

            return p.gameObject;
        }

        Button Espada(RectTransform padre, string texto, int color, System.Action accion, Vector2 desplazamiento)
        {
            var sprite = espadas != null && color < espadas.Length && espadas[color] != null
                ? espadas[color]
                : tema != null ? tema.BotonDeColor(color) : null;

            var img = KitInterfaz.Imagen(padre, texto, sprite, Color.white);
            img.preserveAspect = true;
            img.raycastTarget = true;
            // Solo cuenta como clic la hoja, no el aire transparente de arriba y abajo: las
            // espadas están muy juntas y sus rectángulos se tocan.
            img.raycastPadding = new Vector4(0f, 70f, 0f, 70f);
            KitInterfaz.Colocar(img.rectTransform, new Vector2(1f, 0.5f), new Vector2(600f, 240f), desplazamiento);

            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colores = b.colors;
            colores.highlightedColor = new Color(1.1f, 1.08f, 1f);
            colores.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colores.colorMultiplier = 1.1f;
            b.colors = colores;
            b.onClick.AddListener(() =>
            {
                Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.BotonPanel);
                accion?.Invoke();
            });

            // El texto va sobre la hoja, en tinta: la empuñadura queda a la izquierda.
            var t = KitInterfaz.Texto(img.transform, "Texto", texto, _fuente, 46, KitInterfaz.Tinta,
                                      TextAnchor.MiddleCenter, contorno: false);
            KitInterfaz.Llenar(t.rectTransform).offsetMin = new Vector2(205f, 0f);
            t.rectTransform.offsetMax = new Vector2(-70f, 4f);
            return b;
        }

        // -----------------------------------------------------------------
        // Preparar partida
        // -----------------------------------------------------------------

        GameObject ConstruirPartida(RectTransform raiz)
        {
            var p = KitInterfaz.Llenar(KitInterfaz.Hijo(raiz, "Partida"));
            var mesa = Mesa(p, new Vector2(1660f, 920f), "Nueva partida", Azul, out _titulo, out _cintaPartida);

            // Izquierda: el mapa.
            var izquierda = Contenedor(mesa, "Mapa", Azul, new Vector2(0f, 0.5f), new Vector2(660f, 700f),
                                       new Vector2(400f, 0f));

            var papel = KitInterfaz.Imagen(izquierda, "Pergamino",
                                           KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(papel.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 420f), new Vector2(0f, -240f));

            _miniatura = KitInterfaz.Imagen(papel.transform, "Miniatura", null, Color.white);
            _miniatura.preserveAspect = true;
            KitInterfaz.Colocar(_miniatura.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(360f, 360f));

            for (int i = 0; i < Bandos.Colores; i++)
            {
                var marca = KitInterfaz.Imagen(_miniatura.transform, $"Base{i}", CajaChica(i), Color.white);
                marca.rectTransform.sizeDelta = new Vector2(46f, 46f);
                var numero = KitInterfaz.Texto(marca.transform, "Numero", (i + 1).ToString(), _fuente, 26,
                                               KitInterfaz.Crema, TextAnchor.MiddleCenter);
                KitInterfaz.Llenar(numero.rectTransform);
                _marcas.Add((marca, numero));
            }

            var nombreMapa = KitInterfaz.Ciclador(izquierda, "Mapa", _fuente, Caja(Azul), Boton(Azul), CambiarMapa,
                                                  new Vector2(0.5f, 1f), new Vector2(560f, 66f), new Vector2(0f, -490f), 34);
            _nombreMapa = nombreMapa;
            // «Cuatro Vientos (copia) (tuyo)» no cabe a 34: la letra se encoge antes que salirse.
            _nombreMapa.resizeTextForBestFit = true;
            _nombreMapa.resizeTextMinSize = 16;
            _nombreMapa.resizeTextMaxSize = 34;
            _nombreMapa.horizontalOverflow = HorizontalWrapMode.Wrap;
            _nombreMapa.verticalOverflow = VerticalWrapMode.Truncate;

            _descripcion = KitInterfaz.TextoQueCabe(izquierda, "Descripcion", _fuente, 24, 14, KitInterfaz.Crema,
                                                    TextAnchor.UpperCenter);
            KitInterfaz.Colocar(_descripcion.rectTransform, new Vector2(0.5f, 0f), new Vector2(580f, 74f),
                                new Vector2(0f, 128f));

            // Solo en sandbox: llevar el mapa elegido al editor, o empezar uno desde el mar.
            var editar = KitInterfaz.Hijo(izquierda, "BotonesEditor");
            KitInterfaz.Colocar(editar, new Vector2(0.5f, 0f), new Vector2(600f, 60f), new Vector2(0f, 48f));
            // Tres botones del mismo ancho; «Borrar» solo funciona con los mapas tuyos.
            BotonDeAccion(editar, "Editar", Morado, EditarElegido, new Vector2(0.5f, 0.5f),
                          new Vector2(190f, 60f), new Vector2(-200f, 0f), 26);
            BotonDeAccion(editar, "Mapa nuevo", Morado, () => _dialogoNuevo.SetActive(true), new Vector2(0.5f, 0.5f),
                          new Vector2(190f, 60f), new Vector2(0f, 0f), 26);
            _botonBorrar = BotonDeAccion(editar, "Borrar", Rojo, PreguntarBorrar, new Vector2(0.5f, 0.5f),
                                         new Vector2(190f, 60f), new Vector2(200f, 0f), 26);
            _botonesEditor = editar.gameObject;

            ConstruirDialogoNuevo(p);
            ConstruirDialogoBorrar(p);

            // Derecha: los bandos y, en sandbox, sus reglas.
            var derecha = Contenedor(mesa, "Derecha", Azul, new Vector2(1f, 0.5f), new Vector2(860f, 700f),
                                     new Vector2(-490f, 0f));

            _columnaBases = KitInterfaz.Hijo(derecha, "Bases");
            KitInterfaz.Llenar(_columnaBases);

            var cabecera = KitInterfaz.Texto(_columnaBases, "Titulo", "Bandos", _fuente, 34, KitInterfaz.Crema,
                                             TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(cabecera.rectTransform, new Vector2(0.5f, 1f), new Vector2(800f, 50f), new Vector2(0f, -40f));

            for (int i = 0; i < Bandos.Colores; i++) _filas.Add(ConstruirFila(_columnaBases, i));

            _bloqueReglas = ConstruirReglas(derecha);

            // Pie: volver, el motivo si no se puede empezar, y empezar.
            BotonDeAccion(mesa, "Volver", Rojo, () => Ir(Pantalla.Principal), new Vector2(0f, 0f),
                          new Vector2(300f, 84f), new Vector2(230f, 20f));

            _motivo = KitInterfaz.Texto(mesa, "Motivo", "", _fuente, 28, new Color(1f, 0.82f, 0.62f),
                                        TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_motivo.rectTransform, new Vector2(0.5f, 0f), new Vector2(700f, 50f), new Vector2(0f, 26f));

            _empezar = BotonDeAccion(mesa, "Empezar", Azul, Empezar, new Vector2(1f, 0f),
                                     new Vector2(380f, 96f), new Vector2(-260f, 22f), 44);

            return p.gameObject;
        }

        FilaDeBase ConstruirFila(RectTransform padre, int indice)
        {
            int i = indice;
            var f = new FilaDeBase { raiz = KitInterfaz.Hijo(padre, $"Base{i}") };
            f.raiz.anchorMin = new Vector2(0f, 1f);
            f.raiz.anchorMax = new Vector2(1f, 1f);
            f.raiz.pivot = new Vector2(0.5f, 1f);

            // El retrato del pawn en el color elegido. Pulsarlo cambia de color.
            var marco = KitInterfaz.Boton(f.raiz, "Retrato", CajaChica(0), "", _fuente, 10, () => CambiarColor(i, +1));
            f.caja = (Image)marco.targetGraphic;
            KitInterfaz.Colocar((RectTransform)marco.transform, new Vector2(0f, 0.5f), new Vector2(92f, 92f),
                                new Vector2(80f, 0f));

            f.retrato = KitInterfaz.Imagen(marco.transform, "Pawn", null, Color.white);
            f.retrato.preserveAspect = true;
            KitInterfaz.Llenar(f.retrato.rectTransform).offsetMin = new Vector2(10f, 10f);
            f.retrato.rectTransform.offsetMax = new Vector2(-10f, -10f);

            f.nombre = KitInterfaz.Texto(f.raiz, "Nombre", $"Base {i + 1}", _fuente, 32, KitInterfaz.Crema,
                                         TextAnchor.MiddleLeft);
            KitInterfaz.Colocar(f.nombre.rectTransform, new Vector2(0f, 0.5f), new Vector2(220f, 40f),
                                new Vector2(250f, 22f));

            f.color = KitInterfaz.Ciclador(f.raiz, "Color", _fuente, CajaChica(0), Boton(Azul), paso => CambiarColor(i, paso),
                                           new Vector2(0f, 0.5f), new Vector2(230f, 44f), new Vector2(255f, -22f), 24);

            f.ocupante = KitInterfaz.Ciclador(f.raiz, "Ocupante", _fuente, Caja(Azul), Boton(Azul), paso => CambiarOcupante(i, paso),
                                              new Vector2(1f, 0.5f), new Vector2(370f, 64f), new Vector2(-215f, 0f), 30);
            return f;
        }

        RectTransform ConstruirReglas(RectTransform padre)
        {
            var bloque = KitInterfaz.Hijo(padre, "Reglas");
            bloque.anchorMin = new Vector2(0f, 0f);
            bloque.anchorMax = new Vector2(1f, 0f);
            bloque.pivot = new Vector2(0.5f, 0f);
            bloque.sizeDelta = new Vector2(0f, 330f);
            bloque.anchoredPosition = new Vector2(0f, 20f);

            var fondo = KitInterfaz.Imagen(bloque, "Fondo", Caja(Amarillo), Color.white);
            KitInterfaz.Llenar(fondo.rectTransform).offsetMin = new Vector2(24f, 0f);
            fondo.rectTransform.offsetMax = new Vector2(-24f, 0f);

            _cabeceraReglas = KitInterfaz.Texto(bloque, "Titulo", "Reglas del sandbox", _fuente, 30,
                                                KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_cabeceraReglas.rectTransform, new Vector2(0.5f, 1f), new Vector2(700f, 44f),
                                new Vector2(0f, -32f));

            var reglas = new (string etiqueta, System.Action<int> cambiar)[]
            {
                ("Niebla de guerra", _ => _preparada.sandbox.niebla = !_preparada.sandbox.niebla),
                ("Recursos", paso => _preparada.sandbox.recursos =
                    (RecursosIniciales)(((int)_preparada.sandbox.recursos + paso + 3) % 3)),
                ("Velocidad", paso => _preparada.sandbox.velocidad = (_preparada.sandbox.velocidad - 1 + paso + 4) % 4 + 1),
                ("Los bots atacan", _ => _preparada.sandbox.botsAtacan = !_preparada.sandbox.botsAtacan),
                ("Ejército de salida", _ => _preparada.sandbox.ejercitoInicial = !_preparada.sandbox.ejercitoInicial),
            };

            // En dos columnas: cinco reglas en una sola columna no caben junto a cinco bases.
            for (int k = 0; k < reglas.Length; k++)
            {
                var (etiqueta, cambiar) = reglas[k];
                int columna = k < 3 ? 0 : 1;
                int fila = k < 3 ? k : k - 3;

                float x = columna == 0 ? 60f : 440f;
                float y = -90f - fila * 76f;

                KitInterfaz.Etiqueta(bloque, etiqueta, _fuente, 24, new Vector2(0f, 1f), new Vector2(170f, 60f),
                                     new Vector2(x + 85f, y));

                var valor = KitInterfaz.Ciclador(bloque, etiqueta, _fuente, CajaChica(Amarillo), Boton(Amarillo),
                                                 paso => { cambiar(paso); PintarPartida(); },
                                                 new Vector2(0f, 1f), new Vector2(200f, 50f), new Vector2(x + 280f, y), 22);
                _valoresReglas.Add(valor);
            }

            return bloque;
        }

        void Preparar(ModoDePartida modo)
        {
            _modoAPreparar = modo;
            ListarMapas();
            _mapa = Mathf.Clamp(_mapa, 0, Mathf.Max(0, _lista.Count - 1));
            _preparada = NuevaConfiguracion();
            Ir(Pantalla.Partida);
        }

        ConfiguracionDePartida NuevaConfiguracion()
        {
            var ficha = _lista.Count > 0
                ? _lista[_mapa]
                : new FichaDeMapa { escena = Navegacion.EscenaDelMenu, bandos = Navegacion.BandosDelMenu };

            var c = ConfiguracionDePartida.PorDefecto(Mathf.Max(1, ficha.bandos), ficha.escena);
            c.modo = _modoAPreparar;

            // Una copia: la partida no debe poder cambiar el mapa que se queda en la lista.
            if (ficha.archivo != null) c.mapa = MapaGuardado.DeJson(ficha.archivo.AJson());
            return c;
        }

        void CambiarMapa(int paso)
        {
            if (_lista.Count <= 1) return;

            _mapa = (_mapa + paso + _lista.Count) % _lista.Count;
            _preparada = NuevaConfiguracion();
            PintarPartida();
        }

        void CambiarOcupante(int bando, int paso)
        {
            // Hacia delante es el ciclo de siempre; hacia atrás, cuatro pasos adelante.
            int veces = paso > 0 ? 1 : 4;
            for (int k = 0; k < veces; k++) ReglasDeConfiguracion.Pulsar(_preparada.bases, bando);

            // Abrir una base cerrada puede dejarla con un color que ya lleva otra.
            if (ReglasDeConfiguracion.ColoresRepetidos(_preparada.colores, _preparada.bases))
                _preparada.colores[bando] = ReglasDeConfiguracion.SiguienteColor(_preparada.colores, _preparada.bases, bando);

            PintarPartida();
        }

        void CambiarColor(int bando, int paso)
        {
            if (_preparada.bases[bando] == Ocupante.Cerrada) return;
            _preparada.colores[bando] = ReglasDeConfiguracion.SiguienteColor(_preparada.colores, _preparada.bases, bando, paso);
            PintarPartida();
        }

        void PintarPartida()
        {
            if (_preparada == null) _preparada = NuevaConfiguracion();

            bool sandbox = _modoAPreparar == ModoDePartida.Sandbox;
            _titulo.text = sandbox ? "Sandbox" : "Nueva partida";
            if (tema != null) _cintaPartida.sprite = tema.ListonPanelDeColor(sandbox ? Amarillo : Azul);

            var ficha = _lista.Count > 0 ? _lista[_mapa] : default;
            if (_botonesEditor != null) _botonesEditor.SetActive(sandbox);
            if (_botonBorrar != null)
            {
                bool tuyo = !ficha.oficial && !string.IsNullOrEmpty(ficha.ruta);
                _botonBorrar.interactable = tuyo;
                // Apagado se ve apagado: con el mismo rojo parecería que no responde.
                ((Image)_botonBorrar.targetGraphic).color = tuyo ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
            _nombreMapa.text = string.IsNullOrEmpty(ficha.nombre) ? "Mapa" : ficha.nombre;
            _descripcion.text = $"{_preparada.bases.Length} bases. {ficha.descripcion}";
            _miniatura.sprite = ficha.miniatura;
            _miniatura.enabled = ficha.miniatura != null;

            int n = _preparada.bases.Length;

            // Las filas se reparten el alto que queda: con reglas de sandbox debajo, menos.
            float disponible = 700f - 80f - (sandbox ? 350f : 20f);
            float alto = Mathf.Clamp(disponible / Mathf.Max(1, n), 64f, 120f);

            for (int i = 0; i < _filas.Count; i++)
            {
                var f = _filas[i];
                bool existe = i < n;
                f.raiz.gameObject.SetActive(existe);
                if (!existe) { _marcas[i].caja.gameObject.SetActive(false); continue; }

                f.raiz.sizeDelta = new Vector2(0f, alto);
                f.raiz.anchoredPosition = new Vector2(0f, -78f - i * alto);
                // Con cinco bases y las reglas del sandbox debajo, la fila encoge entera.
                f.raiz.localScale = Vector3.one * Mathf.Min(1f, alto / 108f);

                var ocupante = _preparada.bases[i];
                bool cerrada = ocupante == Ocupante.Cerrada;
                int color = _preparada.colores[i];

                f.caja.sprite = CajaChica(color);
                f.retrato.sprite = tema != null ? tema.RetratoDeColor(TipoUnidad.Pawn, color) : null;
                f.retrato.color = cerrada ? new Color(1f, 1f, 1f, 0.25f) : Color.white;
                f.color.text = cerrada ? "—" : Bandos.NombreDeColor(color);
                f.ocupante.text = ReglasDeConfiguracion.Nombre(ocupante);
                f.nombre.text = ocupante == Ocupante.Jugador ? $"Base {i + 1} · tú" : $"Base {i + 1}";

                // La marca de la base en el mapa, en su color, y apagada si está cerrada.
                var (marca, numero) = _marcas[i];
                marca.gameObject.SetActive(ficha.bases != null && i < ficha.bases.Length);
                if (marca.gameObject.activeSelf)
                {
                    var pos = ficha.bases[i];
                    marca.rectTransform.anchorMin = marca.rectTransform.anchorMax = pos;
                    marca.rectTransform.anchoredPosition = Vector2.zero;
                    marca.sprite = CajaChica(color);
                    marca.color = cerrada ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                    numero.text = ocupante == Ocupante.Jugador ? "Tú" : (i + 1).ToString();
                    numero.fontSize = ocupante == Ocupante.Jugador ? 20 : 26;
                }
            }

            _bloqueReglas.gameObject.SetActive(sandbox);
            if (sandbox)
            {
                var r = _preparada.sandbox;
                _valoresReglas[0].text = r.niebla ? "Sí" : "No";
                _valoresReglas[1].text = r.recursos == RecursosIniciales.Normales ? "Normales"
                                       : r.recursos == RecursosIniciales.Abundantes ? "Abundantes" : "Infinitos";
                _valoresReglas[2].text = $"x{r.velocidad}";
                _valoresReglas[3].text = r.botsAtacan ? "Sí" : "Solo defienden";
                _valoresReglas[4].text = r.ejercitoInicial ? "Sí" : "No";
            }

            bool valida = ReglasDeConfiguracion.EsValida(_preparada.bases, out var motivo);
            if (valida && ficha.archivo != null && !ReglasDeMapa.EsJugable(ficha.archivo, out var queFalta))
            {
                valida = false;
                motivo = $"Este mapa aún no se puede jugar: {queFalta}";
            }
            _motivo.text = motivo;
            _empezar.interactable = valida;
        }

        /// <summary>
        /// Los mapas de la escena (Tres Coronas) y, detrás, los de archivo: primero los
        /// oficiales y luego los tuyos. En partida normal solo los que se pueden jugar; en
        /// sandbox todos, porque desde ahí se abren en el editor para terminarlos.
        /// </summary>
        void ListarMapas()
        {
            _lista.Clear();
            _lista.AddRange(mapas);

            foreach (var e in ArchivoDeMapas.Listar())
            {
                if (_modoAPreparar != ModoDePartida.Sandbox && !ReglasDeMapa.EsJugable(e.mapa, out _)) continue;

                _lista.Add(new FichaDeMapa
                {
                    nombre = e.oficial ? e.mapa.nombre : $"{e.mapa.nombre} (tuyo)",
                    descripcion = e.mapa.descripcion,
                    escena = Navegacion.EscenaDelMenu,
                    bandos = Mathf.Max(1, e.mapa.bases.Count),
                    miniatura = MiniaturaDeMapa.Crear(e.mapa),
                    bases = MiniaturaDeMapa.Bases(e.mapa),
                    archivo = e.mapa,
                    oficial = e.oficial,
                    ruta = e.ruta,
                });
            }
        }

        Button _botonBorrar;
        GameObject _dialogoBorrar;
        Text _textoBorrar;

        void PreguntarBorrar()
        {
            var ficha = _lista.Count > 0 ? _lista[_mapa] : default;
            if (ficha.oficial || string.IsNullOrEmpty(ficha.ruta)) return;
            _textoBorrar.text = $"«{ficha.archivo.nombre}» se borrará de tu carpeta de mapas.\nNo se puede deshacer.";
            _dialogoBorrar.SetActive(true);
            _dialogoBorrar.transform.SetAsLastSibling();
        }

        void BorrarElegido()
        {
            _dialogoBorrar.SetActive(false);
            var ficha = _lista.Count > 0 ? _lista[_mapa] : default;
            if (!ArchivoDeMapas.Borrar(ficha.ruta)) return;

            ListarMapas();
            _mapa = Mathf.Clamp(_mapa - 1, 0, Mathf.Max(0, _lista.Count - 1));
            _preparada = NuevaConfiguracion();
            PintarPartida();
        }

        /// <summary>La misma caja que «Mapa nuevo», con la pregunta de sí o no.</summary>
        void ConstruirDialogoBorrar(RectTransform raiz)
        {
            var velo = KitInterfaz.Velo(raiz, 0.5f);
            _dialogoBorrar = velo.gameObject;

            var caja = KitInterfaz.Imagen(velo.transform, "Caja", tema != null ? tema.panelFondo : null, Color.white);
            caja.raycastTarget = true;
            KitInterfaz.Colocar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(660f, 360f));

            var t = KitInterfaz.Texto(caja.transform, "Titulo", "¿Borrar el mapa?", _fuente, 44, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(600f, 70f), new Vector2(0f, -60f));

            _textoBorrar = KitInterfaz.TextoQueCabe(caja.transform, "Texto", _fuente, 26, 16, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_textoBorrar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(580f, 110f), new Vector2(0f, 10f));

            BotonDeAccion((RectTransform)caja.transform, "No", Azul, () => _dialogoBorrar.SetActive(false),
                          new Vector2(0.5f, 0f), new Vector2(220f, 70f), new Vector2(-125f, 60f), 30);
            BotonDeAccion((RectTransform)caja.transform, "Borrar", Rojo, BorrarElegido,
                          new Vector2(0.5f, 0f), new Vector2(220f, 70f), new Vector2(125f, 60f), 30);

            _dialogoBorrar.SetActive(false);
        }

        void EditarElegido()
        {
            var ficha = _lista.Count > 0 ? _lista[_mapa] : default;

            MapaGuardado mapa;
            if (ficha.archivo != null)
            {
                mapa = MapaGuardado.DeJson(ficha.archivo.AJson());
                // Un mapa oficial se edita como copia tuya: el oficial no se toca.
                if (ficha.oficial) mapa.nombre += " (copia)";
            }
            else
            {
                // Tres Coronas no es un archivo: se pasa a archivo desde el mapa que ya corre
                // detrás del menú, que es exactamente ese.
                var mundo = MundoJuego.Actual;
                if (mundo == null || mundo.Mapa == null) return;
                mapa = MapaGuardado.DeMapaGenerado(mundo.Mapa, $"{ficha.nombre} (copia)", ficha.descripcion);
            }

            Navegacion.Editar(mapa);
        }

        void ConstruirDialogoNuevo(RectTransform raiz)
        {
            var velo = KitInterfaz.Velo(raiz, 0.5f);
            _dialogoNuevo = velo.gameObject;

            var caja = KitInterfaz.Imagen(velo.transform, "Caja", tema != null ? tema.panelFondo : null, Color.white);
            caja.raycastTarget = true;
            KitInterfaz.Colocar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(620f, 380f));

            var t = KitInterfaz.Texto(caja.transform, "Titulo", "Mapa nuevo", _fuente, 44, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 70f), new Vector2(0f, -60f));

            var d = KitInterfaz.Texto(caja.transform, "Texto", "Empieza como mar abierto. Elige su tamaño:", _fuente, 24,
                                      KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(d.rectTransform, new Vector2(0.5f, 1f), new Vector2(560f, 40f), new Vector2(0f, -120f));

            int[] tamanos = { 96, 128, 160, 192, 224 };
            _valorTamanoNuevo = KitInterfaz.Ciclador(caja.transform, "Tamano", _fuente, CajaChica(Morado), Boton(Morado), paso =>
            {
                int i = System.Array.IndexOf(tamanos, _tamanoNuevo);
                _tamanoNuevo = tamanos[(i + paso + tamanos.Length) % tamanos.Length];
                _valorTamanoNuevo.text = $"{_tamanoNuevo} × {_tamanoNuevo}";
            }, new Vector2(0.5f, 0.5f), new Vector2(360f, 60f), new Vector2(0f, -10f), 28);
            _valorTamanoNuevo.text = $"{_tamanoNuevo} × {_tamanoNuevo}";

            BotonDeAccion((RectTransform)caja.transform, "Cancelar", Rojo, () => _dialogoNuevo.SetActive(false),
                          new Vector2(0.5f, 0f), new Vector2(220f, 70f), new Vector2(-125f, 60f), 30);
            BotonDeAccion((RectTransform)caja.transform, "Crear", Azul,
                          () => Navegacion.Editar(MapaGuardado.EnBlanco(_tamanoNuevo, _tamanoNuevo, "Mapa nuevo")),
                          new Vector2(0.5f, 0f), new Vector2(220f, 70f), new Vector2(125f, 60f), 30);

            _dialogoNuevo.SetActive(false);
        }

        void Empezar()
        {
            if (!ReglasDeConfiguracion.EsValida(_preparada.bases, out _)) return;
            Navegacion.Empezar(_preparada.Copia());
        }

        // -----------------------------------------------------------------
        // Opciones
        // -----------------------------------------------------------------

        GameObject ConstruirOpciones(RectTransform raiz)
        {
            var p = KitInterfaz.Llenar(KitInterfaz.Hijo(raiz, "Opciones"));
            var mesa = Mesa(p, new Vector2(1500f, 880f), "Opciones", Morado, out _, out _);

            // Izquierda: el sonido entero, un deslizador por canal (semana 14). Derecha:
            // pantalla y cámara. Los controles pasan a su propio cartel, con un botón.
            var sonido = Contenedor(mesa, "Sonido", Azul, new Vector2(0f, 1f), new Vector2(660f, 670f),
                                    new Vector2(390f, -435f), "Sonido");
            var pantalla = Contenedor(mesa, "Pantalla", Azul, new Vector2(1f, 1f), new Vector2(660f, 330f),
                                      new Vector2(-390f, -265f), "Pantalla");
            var camara = Contenedor(mesa, "Camara", Morado, new Vector2(1f, 1f), new Vector2(660f, 310f),
                                    new Vector2(-390f, -615f), "Cámara");

            // Sonido
            _valorVolumen = DeslizadorDeVolumen(sonido, "General", 0, () => Opciones.Volumen, v => Opciones.Volumen = v);
            var canales = new (Audio.Canal, string)[]
            {
                (Audio.Canal.Musica, "Música"), (Audio.Canal.Efectos, "Efectos"),
                (Audio.Canal.Ambiente, "Ambiente"), (Audio.Canal.Interfaz, "Interfaz"),
            };
            _valoresCanal = new Text[canales.Length];
            for (int i = 0; i < canales.Length; i++)
            {
                var canal = canales[i].Item1;
                _valoresCanal[i] = DeslizadorDeVolumen(sonido, canales[i].Item2, i + 1,
                    () => Opciones.VolumenDe(canal), v => Opciones.PonerVolumen(canal, v));
            }

            Fila(sonido, "Sonido", 5);
            _valorSonido = Valor(sonido, 5, _ => Opciones.Silencio = !Opciones.Silencio, Azul);

            // Pantalla
            Fila(pantalla, "Pantalla completa", 0);
            _valorCompleta = Valor(pantalla, 0, _ => Opciones.PantallaCompleta = !Opciones.PantallaCompleta, Azul);
            Fila(pantalla, "Sincronía vertical", 1);
            _valorVsync = Valor(pantalla, 1, _ => Opciones.SincronizacionVertical = !Opciones.SincronizacionVertical, Azul);
            Fila(pantalla, "Límite de FPS", 2);
            _valorFps = Valor(pantalla, 2, paso => Opciones.LimiteDeFps =
                (Opciones.LimiteDeFps + paso + Opciones.LimitesDeFps.Length) % Opciones.LimitesDeFps.Length, Azul);

            // Cámara
            Fila(camara, "Velocidad", 0);
            _valorCamara = Valor(camara, 0, paso => Opciones.VelocidadDeCamara =
                Mathf.Clamp(Mathf.Round((Opciones.VelocidadDeCamara + paso * 0.25f) * 4f) / 4f, 0.5f, 2f), Morado);
            Fila(camara, "Mover con el borde", 1);
            _valorBorde = Valor(camara, 1, _ => Opciones.DesplazarConElBorde = !Opciones.DesplazarConElBorde, Morado);

            // Controles: su propio cartel, con el teclado y el ratón dibujados.
            _cartelControles = ConstruirControles(p);

            BotonDeAccion(mesa, "Controles", Negro, () => { _cartelControles.SetActive(true); _cartelControles.transform.SetAsLastSibling(); },
                          new Vector2(0.5f, 0f), new Vector2(300f, 84f), new Vector2(-170f, 20f));
            BotonDeAccion(mesa, "Volver", Rojo, () => { _cartelControles.SetActive(false); Ir(Pantalla.Principal); },
                          new Vector2(0.5f, 0f), new Vector2(300f, 84f), new Vector2(170f, 20f));

            return p.gameObject;
        }

        Text[] _valoresCanal;
        GameObject _cartelControles;

        /// <summary>Una fila de volumen: etiqueta, deslizador y el porcentaje a la derecha.</summary>
        Text DeslizadorDeVolumen(RectTransform padre, string etiqueta, int fila,
                                 System.Func<float> leer, System.Action<float> poner)
        {
            Fila(padre, etiqueta, fila);
            var d = KitInterfaz.Deslizador(padre, etiqueta,
                KitInterfaz.ConBorde(tema != null ? tema.barraGrandeMarco : null, new Vector4(16, 0, 16, 0)),
                KitInterfaz.ConBorde(tema != null ? tema.barraGrandeRellenoAzul : null, new Vector4(6, 0, 6, 0)),
                perilla, leer(), v => { poner(v); PintarOpciones(); });
            float y = -115f - fila * 72f;
            KitInterfaz.Colocar((RectTransform)d.transform, new Vector2(0f, 1f), new Vector2(280f, 50f), new Vector2(380f, y));
            var valor = KitInterfaz.Texto(padre, "Valor" + etiqueta, "", _fuente, 26, KitInterfaz.Crema, TextAnchor.MiddleRight);
            KitInterfaz.Colocar(valor.rectTransform, new Vector2(1f, 1f), new Vector2(90f, 50f), new Vector2(-60f, y));
            return valor;
        }

        void Fila(RectTransform padre, string etiqueta, int fila) =>
            KitInterfaz.Etiqueta(padre, etiqueta, _fuente, 28, new Vector2(0f, 1f), new Vector2(260f, 50f),
                                 new Vector2(170f, -115f - fila * 72f));

        Text Valor(RectTransform padre, int fila, System.Action<int> cambiar, int color) =>
            KitInterfaz.Ciclador(padre, $"Valor{fila}", _fuente, CajaChica(color), Boton(color),
                                 paso => { cambiar(paso); PintarOpciones(); },
                                 new Vector2(1f, 1f), new Vector2(300f, 54f), new Vector2(-200f, -115f - fila * 72f), 24);

        void PintarOpciones()
        {
            if (_valorVolumen == null) return;

            _valorVolumen.text = $"{Mathf.RoundToInt(Opciones.Volumen * 100f)} %";
            var canales = new[] { Audio.Canal.Musica, Audio.Canal.Efectos, Audio.Canal.Ambiente, Audio.Canal.Interfaz };
            for (int i = 0; i < _valoresCanal.Length; i++)
                _valoresCanal[i].text = $"{Mathf.RoundToInt(Opciones.VolumenDe(canales[i]) * 100f)} %";
            _valorSonido.text = Opciones.Silencio ? "Apagado" : "Encendido";
            _valorCompleta.text = Opciones.PantallaCompleta ? "Sí" : "No";
            _valorVsync.text = Opciones.SincronizacionVertical ? "Sí" : "No";
            _valorFps.text = Opciones.TextoFps(Opciones.LimiteDeFps);
            _valorCamara.text = $"x{Opciones.VelocidadDeCamara:0.##}";
            _valorBorde.text = Opciones.DesplazarConElBorde ? "Sí" : "No";
        }

        // -----------------------------------------------------------------
        // Créditos
        // -----------------------------------------------------------------

        GameObject ConstruirCreditos(RectTransform raiz)
        {
            var p = KitInterfaz.Llenar(KitInterfaz.Hijo(raiz, "Creditos"));
            var mesa = Mesa(p, new Vector2(1500f, 900f), "Créditos", Negro, out _, out _);

            // El equipo, en fichas como las del informe semanal: la cara de una unidad del
            // pack por persona (semana 14). Solo la cara, sin el cuerpo.
            var equipo = new (string nombre, string codigo, string rol, Datos.TipoUnidad cara, int color)[]
            {
                ("Joaquín Alfonso Loa Denegri", "U22234069", "Desarrollo y arquitectura", Datos.TipoUnidad.Guerrero, Azul),
                ("Kiara Mishell Santti Saavedra", "U22201636", "Diseño de juego, arte y documentación", Datos.TipoUnidad.Pawn, Amarillo),
                ("Gerardo Raúl Socualaya Mandamiento", "U22229068", "QA, balance y pruebas", Datos.TipoUnidad.Monje, Morado),
            };
            for (int i = 0; i < equipo.Length; i++)
            {
                var (nombre, codigo, rol, cara, color) = equipo[i];
                var ficha = KitInterfaz.Imagen(mesa, "Ficha" + i, KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
                KitInterfaz.Colocar(ficha.rectTransform, new Vector2(0.5f, 1f), new Vector2(420f, 420f),
                                    new Vector2((i - 1) * 450f, -330f));

                var marco = KitInterfaz.Imagen(ficha.transform, "Marco", Caja(color), Color.white);
                KitInterfaz.Colocar(marco.rectTransform, new Vector2(0.5f, 1f), new Vector2(340f, 190f), new Vector2(0f, -120f));
                var retrato = KitInterfaz.Imagen(marco.transform, "Cara", tema != null ? tema.RetratoDeColor(cara, color) : null, Color.white);
                retrato.preserveAspect = true;
                KitInterfaz.Colocar(retrato.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150f, 150f));

                var n = KitInterfaz.TextoQueCabe(ficha.transform, "Nombre", _fuente, 28, 16, KitInterfaz.Tinta, TextAnchor.MiddleCenter);
                n.GetComponent<Outline>().enabled = false;
                n.text = nombre;
                KitInterfaz.Colocar(n.rectTransform, new Vector2(0.5f, 1f), new Vector2(370f, 44f), new Vector2(0f, -258f));

                var c = KitInterfaz.Texto(ficha.transform, "Codigo", "Cód. " + codigo, _fuente, 22, new Color(0.62f, 0.16f, 0.12f), TextAnchor.MiddleCenter);
                c.GetComponent<Outline>().enabled = false;
                KitInterfaz.Colocar(c.rectTransform, new Vector2(0.5f, 1f), new Vector2(370f, 32f), new Vector2(0f, -298f));

                var r = KitInterfaz.TextoQueCabe(ficha.transform, "Rol", _fuente, 22, 14, KitInterfaz.Tinta, TextAnchor.MiddleCenter);
                r.GetComponent<Outline>().enabled = false;
                r.text = rol;
                KitInterfaz.Colocar(r.rectTransform, new Vector2(0.5f, 1f), new Vector2(370f, 50f), new Vector2(0f, -345f));
            }

            var papel = KitInterfaz.Imagen(mesa, "Pergamino",
                                           KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(papel.rectTransform, new Vector2(0.5f, 0f), new Vector2(1320f, 230f), new Vector2(0f, 235f));

            var texto = KitInterfaz.TextoQueCabe(papel.transform, "Texto", _fuente, 24, 14, KitInterfaz.Tinta,
                                                 TextAnchor.MiddleCenter);
            texto.GetComponent<Outline>().enabled = false;
            texto.text =
                "TINY TACTICS · Diseño y Desarrollo de Juegos Interactivos II · UTP · Grupo 01\n" +
                "Docente: Omar David Machuca Ñuflo\n" +
                "Gráficos: Tiny Swords, de Pixel Frog · Fuente: MedievalSharp (SIL Open Font License)\n" +
                "Música y sonidos CC0: RandomMind, Spring Spring, Wolfgang_, Kenney, Juhani Junkala,\n" +
                "artisticdude, OpenGameArt y BigSoundBank";
            KitInterfaz.Llenar(texto.rectTransform).offsetMin = new Vector2(50f, 30f);
            texto.rectTransform.offsetMax = new Vector2(-50f, -30f);

            BotonDeAccion(mesa, "Volver", Rojo, () => Ir(Pantalla.Principal), new Vector2(0.5f, 0f),
                          new Vector2(300f, 84f), new Vector2(0f, 20f));

            return p.gameObject;
        }
    }
}
