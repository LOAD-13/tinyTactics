using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Minimapa: el mapa entero en una esquina, con lo que el bando del jugador sabe.
    /// </summary>
    /// <remarks>
    /// <b>Una sola textura, no un objeto por punto.</b> El terreno se cuece UNA vez al
    /// arrancar —es dato de mapa y no cambia— y se guarda en un array aparte. Cada refresco
    /// copia ese fondo de un golpe con <c>Array.Copy</c>, que es un memcpy, y encima escribe
    /// la niebla, las unidades, los edificios y el rectángulo de la cámara. Volver a pintar
    /// el terreno cada vez sería recorrer cincuenta mil celdas para dibujar exactamente lo
    /// mismo que la vez anterior.
    ///
    /// <b>Refresca a su propio ritmo, no por fotograma.</b> A 15 refrescos por segundo el
    /// movimiento de los puntos ya es continuo para el ojo, y un minimapa no es la pantalla:
    /// nadie está mirando un punto de dos píxeles para ver si va a saltos.
    ///
    /// <b>Respeta la niebla, y eso es lo que lo hace útil.</b> Un minimapa que enseña los
    /// ejércitos enemigos convierte la niebla en decoración: el jugador no mira el mapa, mira
    /// la esquina. Aquí solo salen los enemigos que alguien de tu bando está viendo, y los
    /// edificios enemigos que ya descubriste — la misma regla que en el mapa grande.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Minimapa")]
    public class Minimapa : MonoBehaviour
    {
        public static Minimapa Actual { get; private set; }

        [Tooltip("De aqui salen el marco de madera y los colores de bando.")]
        public TemaInterfaz tema;

        [Header("Interruptores del panel de partida libre")]
        [Tooltip("Ensena u oculta el minimapa entero.")]
        public bool visible = true;

        [Tooltip("Si se apaga, el minimapa ensena el mapa completo y todos los ejercitos. " +
                 "Es una trampa: sirve para capturas y para ver de un vistazo que esta " +
                 "haciendo el rival mientras no hay IA que lo cuente.")]
        public bool respetarNiebla = true;

        [Header("Sitio")]
        [Tooltip("Lado del marco en pixeles de la resolucion de referencia (1920x1080).")]
        [Range(120f, 420f)] public float lado = 236f;

        [Tooltip("Separacion respecto a la esquina de la pantalla.")]
        public Vector2 margen = new Vector2(16f, 16f);

        [Tooltip("Grosor de la moldura del marco. El mapa se dibuja por dentro.")]
        [Range(0f, 48f)] public float moldura = 22f;

        [Header("Ritmo")]
        [Tooltip("Segundos entre refrescos de los puntos.")]
        [Range(0.02f, 0.5f)] public float intervalo = 0.07f;

        [Header("Colores del terreno")]
        public Color colorAgua = new Color(0.22f, 0.40f, 0.62f);
        public Color colorTierra = new Color(0.34f, 0.55f, 0.28f);
        public Color colorMeseta = new Color(0.45f, 0.66f, 0.35f);
        public Color colorEscalera = new Color(0.58f, 0.49f, 0.33f);
        public Color colorArbol = new Color(0.16f, 0.34f, 0.19f);
        public Color colorOro = new Color(0.86f, 0.71f, 0.25f);
        public Color colorRoca = new Color(0.47f, 0.47f, 0.50f);

        [Header("Colores de la niebla")]
        [Tooltip("Negro, y a proposito distinto del mapa grande. En el mapa grande lo no " +
                 "explorado se oscurece pero se deja leer, porque ahi estas jugando. El " +
                 "minimapa es un resumen de 190 pixeles: lo que necesita es que la silueta " +
                 "de lo que ya conoces se recorte contra lo que no, y para eso el negro " +
                 "plano gana a cualquier gris.")]
        public Color veloOculto = new Color(0.04f, 0.05f, 0.09f, 0.97f);

        public Color veloExplorado = new Color(0.16f, 0.19f, 0.28f, 0.22f);

        [Header("Ampliado")]
        [Tooltip("Lado del minimapa ampliado con la tecla M, en pixeles de referencia.")]
        [Range(300f, 1000f)] public float ladoAmpliado = 760f;

        [Tooltip("Lo que tarda en abrirse y cerrarse.")]
        [Range(0.02f, 1f)] public float duracionAmpliado = 0.16f;

        bool _ampliado;
        float _apertura;

        RectTransform _marco;
        RawImage _lamina;
        Texture2D _textura;
        Color32[] _fondo;
        Color32[] _pixeles;

        GrillaMapa _grilla;
        int _ancho, _alto;
        float _proximoRefresco;
        Camera _camara;

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;
        }

        void Start()
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null)
            {
                enabled = false;
                return;
            }

            _grilla = mundo.Grilla;
            _ancho = _grilla.Ancho;
            _alto = _grilla.Alto;

            Construir();
            CocerTerreno(mundo);
            Refrescar();
        }

        void Update()
        {
            if (_marco == null) return;

            if (_marco.gameObject.activeSelf != visible) _marco.gameObject.SetActive(visible);
            if (!visible) return;

            Ampliar();

            // unscaledTime: con el juego en pausa el minimapa tiene que seguir respondiendo
            // al cambio de bando y a los interruptores del panel.
            if (Time.unscaledTime < _proximoRefresco) return;
            _proximoRefresco = Time.unscaledTime + intervalo;

            Refrescar();
        }

        /// <summary>
        /// La tecla M abre el minimapa en grande, en el centro de la pantalla.
        /// </summary>
        /// <remarks>
        /// <b>M estaba cogida por la orden de mover, y se le ha quitado</b> (ahora es la V).
        /// La orden de mover ya tiene el clic derecho, que es como la usa todo el mundo, asi
        /// que su atajo de teclado era el que menos costaba mover. Y M es la tecla que la
        /// mano busca para un mapa.
        ///
        /// Se anima en vez de saltar porque el minimapa cambia de sitio Y de tamano a la vez:
        /// apareciendo de golpe en el centro, cuesta un instante entender que lo de en medio
        /// es lo que estaba en la esquina.
        /// </remarks>
        void Ampliar()
        {
            var teclado = Keyboard.current;
            if (teclado != null && teclado.mKey.wasPressedThisFrame) _ampliado = !_ampliado;

            float destino = _ampliado ? 1f : 0f;
            float paso = Time.unscaledDeltaTime / Mathf.Max(0.02f, duracionAmpliado);
            _apertura = Mathf.MoveTowards(_apertura, destino, paso);

            float t = _apertura * _apertura * (3f - 2f * _apertura);
            float tamano = Mathf.Lerp(lado, ladoAmpliado, t);

            // El ancla viaja de la esquina de abajo a la derecha al centro de la pantalla.
            // Interpolando el ancla y no la posicion, el recorrido sale recto en cualquier
            // resolucion: con posiciones en pixeles habria que rehacer la cuenta por pantalla.
            var ancla = Vector2.Lerp(new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), t);

            _marco.anchorMin = ancla;
            _marco.anchorMax = ancla;
            _marco.pivot = ancla;
            _marco.sizeDelta = new Vector2(tamano, tamano);
            _marco.anchoredPosition = Vector2.Lerp(new Vector2(-margen.x, margen.y),
                                                   Vector2.zero, t);
        }

        // -----------------------------------------------------------------
        // Montaje
        // -----------------------------------------------------------------

        void Construir()
        {
            var go = new GameObject("Minimapa", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            _marco = (RectTransform)go.transform;
            _marco.anchorMin = new Vector2(1f, 0f);
            _marco.anchorMax = new Vector2(1f, 0f);
            _marco.pivot = new Vector2(1f, 0f);
            _marco.sizeDelta = new Vector2(lado, lado);
            _marco.anchoredPosition = new Vector2(-margen.x, margen.y);

            var fondo = go.GetComponent<Image>();
            if (tema != null && tema.panelFondo != null)
            {
                fondo.sprite = tema.panelFondo;
                fondo.type = Image.Type.Sliced;
            }
            else
            {
                fondo.color = new Color(0.12f, 0.10f, 0.08f, 0.9f);
            }

            // El marco NO recibe el clic: lo recibe el mapa de dentro. Si el marco fuera el
            // que escucha, un clic en la moldura de madera movería la cámara a la esquina
            // del mapa, que es justo lo que nadie ha pedido al pulsar un borde.
            fondo.raycastTarget = false;

            _textura = new Texture2D(_ancho, _alto, TextureFormat.RGBA32, false)
            {
                name = "Minimapa",

                // Punto, igual que la niebla: un minimapa de pixel art con filtrado suave se
                // ve borroso, y borroso es exactamente lo contrario de legible en 190 px.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            _fondo = new Color32[_ancho * _alto];
            _pixeles = new Color32[_ancho * _alto];

            var mapa = new GameObject("MapaDelMinimapa", typeof(RectTransform), typeof(RawImage));
            mapa.transform.SetParent(_marco, false);

            var rt = (RectTransform)mapa.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(moldura, moldura);
            rt.offsetMax = new Vector2(-moldura, -moldura);

            _lamina = mapa.GetComponent<RawImage>();
            _lamina.texture = _textura;
            _lamina.raycastTarget = true;

            mapa.AddComponent<ZonaDelMinimapa>().minimapa = this;
        }

        /// <summary>
        /// Pinta el terreno una sola vez.
        /// </summary>
        /// <remarks>
        /// Los recursos se leen de las listas del mapa generado y no de los nodos vivos de la
        /// escena. Son la misma cosa vista de dos formas, pero las listas son dato de mapa y
        /// los nodos desaparecen al talarlos: cociendo desde los nodos, el minimapa de una
        /// partida avanzada tendría bosques donde ya no hay nada solo porque se coció tarde.
        /// </remarks>
        void CocerTerreno(MundoJuego mundo)
        {
            for (int y = 0; y < _alto; y++)
            {
                int fila = y * _ancho;
                for (int x = 0; x < _ancho; x++)
                {
                    var celda = _grilla[x, y];

                    Color color;
                    if (!celda.Suelo) color = colorAgua;
                    else if (celda.Escalera) color = colorEscalera;
                    else if (celda.Nivel > 0) color = colorMeseta;
                    else color = colorTierra;

                    _fondo[x + fila] = color;
                }
            }

            var mapa = mundo.Mapa;
            if (mapa == null) return;

            Manchar(mapa.Arboles, colorArbol, 1);
            Manchar(mapa.Arbustos, colorArbol, 0);
            Manchar(mapa.Rocas, colorRoca, 1);
            Manchar(mapa.Oro, colorOro, 1);
        }

        void Manchar(System.Collections.Generic.List<Vector2Int> celdas, Color color, int radio)
        {
            if (celdas == null) return;

            Color32 tinte = color;

            for (int i = 0; i < celdas.Count; i++)
            {
                var c = celdas[i];

                for (int dy = -radio; dy <= radio; dy++)
                {
                    int y = c.y + dy;
                    if (y < 0 || y >= _alto) continue;

                    int fila = y * _ancho;
                    for (int dx = -radio; dx <= radio; dx++)
                    {
                        int x = c.x + dx;
                        if (x < 0 || x >= _ancho) continue;

                        _fondo[x + fila] = tinte;
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // Refresco
        // -----------------------------------------------------------------

        void Refrescar()
        {
            if (_textura == null) return;

            System.Array.Copy(_fondo, _pixeles, _fondo.Length);

            var niebla = NieblaDeGuerra.Actual;
            bool conNiebla = respetarNiebla && niebla != null && niebla.enabled &&
                             !niebla.ignorarNiebla;

            if (conNiebla) TenirNiebla(niebla);

            DibujarEdificios(conNiebla);
            DibujarUnidades(conNiebla);
            DibujarEncuadre();

            _textura.SetPixels32(_pixeles);
            _textura.Apply(false);
        }

        void TenirNiebla(NieblaDeGuerra niebla)
        {
            var mapa = niebla.MapaDelJugador();

            Color32 oculto = veloOculto;
            Color32 recordado = veloExplorado;

            for (int y = 0; y < _alto; y++)
            {
                int fila = y * _ancho;
                for (int x = 0; x < _ancho; x++)
                {
                    var estado = mapa.En(x, y);
                    if (estado == EstadoVisible.Visible || estado == EstadoVisible.Penumbra)
                        continue;

                    int i = x + fila;
                    _pixeles[i] = estado == EstadoVisible.Oculto
                        ? Mezclar(_pixeles[i], oculto)
                        : Mezclar(_pixeles[i], recordado);
                }
            }
        }

        /// <summary>Mezcla alfa a mano: la textura se sube opaca y el veló ya viene aplicado.</summary>
        /// <remarks>
        /// Se hace acá en vez de dejarlo a una segunda imagen encima porque dos
        /// <c>RawImage</c> superpuestas son dos dibujados y dos texturas de 224×224 en
        /// memoria para conseguir exactamente el mismo píxel final.
        /// </remarks>
        static Color32 Mezclar(Color32 debajo, Color32 encima)
        {
            int a = encima.a;
            int resto = 255 - a;

            return new Color32(
                (byte)((encima.r * a + debajo.r * resto) / 255),
                (byte)((encima.g * a + debajo.g * resto) / 255),
                (byte)((encima.b * a + debajo.b * resto) / 255),
                255);
        }

        void DibujarEdificios(bool conNiebla)
        {
            int jugador = FaccionDelJugador;
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null) continue;

                bool propio = e.faccion == jugador;

                // Un edificio enemigo se queda en el minimapa una vez descubierto, igual que
                // en el mapa grande: no se mueve, así que el recuerdo sigue siendo verdad.
                if (!propio && conNiebla && !NieblaDeGuerra.Descubierto(jugador, e))
                    continue;

                var c = e.celdas;
                var color = ColorDeBando(e.faccion);

                if (c.width <= 0 || c.height <= 0)
                {
                    Punto(Mathf.RoundToInt(e.transform.position.x),
                          Mathf.RoundToInt(e.transform.position.y), 2, color);
                    continue;
                }

                Caja(c.x, c.y, c.width, c.height, color);
            }
        }

        void DibujarUnidades(bool conNiebla)
        {
            int jugador = FaccionDelJugador;
            var todas = RegistroDeUnidades.Todas;

            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva) continue;

                bool propio = u.faccion == jugador;
                if (!propio && conNiebla && !NieblaDeGuerra.Ve(jugador, u.transform.position))
                    continue;

                var celda = _grilla.MundoACelda(u.transform.position);

                // Las propias salen un punto más gordas. No es favoritismo: son las que el
                // jugador busca con la vista, y en un mapa de 224 píxeles un punto de dos
                // píxeles entre bosques del mismo verde no se encuentra.
                Punto(celda.x, celda.y, propio ? 2 : 1, ColorDeBando(u.faccion));
            }
        }

        /// <summary>El rectángulo de lo que se está viendo, para no perder el norte.</summary>
        void DibujarEncuadre()
        {
            if (_camara == null) _camara = Camera.main;
            if (_camara == null) return;

            float mitadAlto = _camara.orthographicSize;
            float mitadAncho = mitadAlto * _camara.aspect;
            Vector3 ojo = _camara.transform.position;

            int x0 = Mathf.Clamp(Mathf.RoundToInt(ojo.x - mitadAncho), 0, _ancho - 1);
            int x1 = Mathf.Clamp(Mathf.RoundToInt(ojo.x + mitadAncho), 0, _ancho - 1);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(ojo.y - mitadAlto), 0, _alto - 1);
            int y1 = Mathf.Clamp(Mathf.RoundToInt(ojo.y + mitadAlto), 0, _alto - 1);

            var blanco = new Color32(255, 255, 255, 255);

            for (int x = x0; x <= x1; x++)
            {
                Pintar(x, y0, blanco);
                Pintar(x, y1, blanco);
            }

            for (int y = y0; y <= y1; y++)
            {
                Pintar(x0, y, blanco);
                Pintar(x1, y, blanco);
            }
        }

        // -----------------------------------------------------------------
        // Clic
        // -----------------------------------------------------------------

        /// <summary>
        /// Lleva la cámara al punto del mapa que se ha pulsado.
        /// </summary>
        /// <remarks>
        /// Lo llama <see cref="ZonaDelMinimapa"/> tanto al pulsar como al arrastrar, y los dos
        /// hacen lo mismo a propósito: arrastrar por el minimapa es la forma natural de
        /// barrer el mapa buscando algo, y obligar a soltar y volver a pulsar para cada paso
        /// convierte un gesto en veinte clics.
        /// </remarks>
        public void LlevarCamaraA(Vector2 normalizado)
        {
            var camara = CamaraRTS.Actual;
            if (camara == null) return;

            camara.CentrarEn(new Vector2(normalizado.x * _ancho, normalizado.y * _alto));
        }

        // -----------------------------------------------------------------
        // Pinceles
        // -----------------------------------------------------------------

        void Punto(int cx, int cy, int radio, Color32 color)
        {
            for (int dy = -radio; dy <= radio; dy++)
                for (int dx = -radio; dx <= radio; dx++)
                    Pintar(cx + dx, cy + dy, color);
        }

        void Caja(int x0, int y0, int ancho, int alto, Color32 color)
        {
            for (int y = y0; y < y0 + alto; y++)
                for (int x = x0; x < x0 + ancho; x++)
                    Pintar(x, y, color);
        }

        void Pintar(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= _ancho || y >= _alto) return;
            _pixeles[x + y * _ancho] = color;
        }

        Color32 ColorDeBando(int faccion) =>
            tema != null ? tema.ColorDe(faccion) : Color.white;

        static int FaccionDelJugador
        {
            get
            {
                var selector = SelectorDeUnidades.Actual;
                return selector != null ? selector.faccionJugador : 0;
            }
        }
    }
}
