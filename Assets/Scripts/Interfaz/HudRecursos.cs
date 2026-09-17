using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Los tres contadores de recursos, arriba y centrados.
    ///
    /// Se redibuja <b>por aviso y no por fotograma</b>: la economía dispara un evento
    /// cuando algo cambia y el HUD reacciona. Preguntar en <c>Update</c> habría sido más
    /// corto de escribir, pero serían sesenta comprobaciones por segundo para reescribir
    /// tres cifras que cambian cada varios segundos, generando basura en el montón cada vez
    /// que se compone un <c>string</c>.
    ///
    /// Se construye por código, como el resto de la interfaz: la escena se regenera entera
    /// desde el menú del editor y nada de esto se toca a mano.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/HUD de recursos")]
    public class HudRecursos : MonoBehaviour
    {
        [Header("Tema")]
        public TemaInterfaz tema;

        [Header("Bando")]
        [Tooltip("De quién son los recursos que se muestran. 0 es el jugador humano.")]
        public int faccion;

        [Header("Aspecto")]
        public Vector2 tamanoCaja = new Vector2(168f, 58f);
        public float separacion = 8f;
        public Vector2 margen = new Vector2(24f, 18f);
        public float ladoIcono = 40f;

        [Tooltip("Color normal del número. Blanco con contorno: la madera del pack es " +
                 "oscura y el negro del panel inferior se perdía sobre ella.")]
        public Color colorTexto = new Color(1f, 0.98f, 0.92f);

        [Tooltip("Color del contador de carne cuando la despensa se está vaciando.")]
        public Color colorAviso = new Color(1f, 0.55f, 0.42f);

        [Tooltip("Contorno del texto, para que se despegue de la madera sea cual sea el bando.")]
        public Color colorContorno = new Color(0.05f, 0.04f, 0.03f, 0.95f);

        public string[] fuentes = { "Cambria", "Constantia", "Georgia" };

        [Tooltip("Color del contador de población cuando ya no cabe nadie más.")]
        public Color colorLleno = new Color(1f, 0.72f, 0.30f);

        class Contador
        {
            public TipoRecurso Recurso;
            public Text Numero;
            public int Pintado = -1;
        }

        readonly Contador[] _contadores = new Contador[3];

        Text _poblacion;
        int _usadaPintada = -1;
        int _topePintado = -1;

        Font _fuente;
        Economia _economia;
        Poblacion _censo;

        void Awake()
        {
            _fuente = PanelDeUnidad.PrimeraInstalada(fuentes);
            Construir();
        }

        void OnEnable()
        {
            // La economía puede no existir todavía si el orden de Awake juega en contra.
            // Start lo resuelve: para entonces todos los Awake han corrido.
            Enganchar();
        }

        void Start()
        {
            Enganchar();

            Refrescar();
            RefrescarPoblacion();
        }

        void Enganchar()
        {
            if (_economia == null)
            {
                _economia = Economia.Actual;
                if (_economia != null) _economia.AlCambiar += AlCambiar;
            }

            if (_censo != null) return;

            _censo = Poblacion.Actual;
            if (_censo != null) _censo.AlCambiar += AlCambiarPoblacion;
        }

        void OnDisable()
        {
            if (_economia != null) _economia.AlCambiar -= AlCambiar;
            _economia = null;

            if (_censo != null) _censo.AlCambiar -= AlCambiarPoblacion;
            _censo = null;
        }

        void AlCambiar(int bando)
        {
            if (bando == faccion) Refrescar();
        }

        void AlCambiarPoblacion(int bando)
        {
            if (bando == faccion) RefrescarPoblacion();
        }

        /// <summary>
        /// «3 / 10»: lo que ocupan las unidades y lo que permiten los edificios.
        /// </summary>
        /// <remarks>
        /// Se pinta la pareja entera y no solo lo usado porque el tope se mueve: sube cinco
        /// con cada casa. Enseñar solo «3» dejaría al jugador sin saber cuándo tiene que
        /// construir la siguiente, que es justo la decisión que este contador existe para
        /// provocar.
        /// </remarks>
        /// <summary>
        /// Cambia el bando que muestra el HUD. Solo la usa el panel de pruebas.
        /// </summary>
        /// <remarks>
        /// El HUD se refresca por eventos de economia filtrados por bando, asi que cambiar
        /// el campo a secas dejaria las cifras del bando anterior en pantalla hasta que el
        /// nuevo gastara o recolectara algo. Hay que forzar el repintado.
        /// </remarks>
        public void CambiarFaccion(int nueva)
        {
            faccion = nueva;
            Refrescar();
            RefrescarPoblacion();
        }

        void RefrescarPoblacion()
        {
            if (_poblacion == null || _censo == null) return;

            int usada = _censo.Usada(faccion);
            int tope = _censo.Tope(faccion);

            if (usada == _usadaPintada && tope == _topePintado) return;

            _usadaPintada = usada;
            _topePintado = tope;

            _poblacion.text = $"{usada} / {tope}";
            _poblacion.color = usada >= tope ? colorLleno : colorTexto;
        }

        // -----------------------------------------------------------------

        void Refrescar()
        {
            if (_economia == null) return;

            for (int i = 0; i < _contadores.Length; i++)
            {
                var c = _contadores[i];
                if (c == null || c.Numero == null) continue;

                int valor = _economia.Cantidad(faccion, c.Recurso);
                if (valor == c.Pintado) continue;

                c.Pintado = valor;
                c.Numero.text = valor.ToString();

                // La carne avisa antes de agotarse. Enterarse de que el ejército tiene
                // hambre cuando ya pega la mitad es enterarse tarde.
                if (c.Recurso == TipoRecurso.Carne)
                    c.Numero.color = Sustento.EnAviso(faccion) ? colorAviso : colorTexto;
            }
        }

        // -----------------------------------------------------------------
        // Construcción
        // -----------------------------------------------------------------

        void Construir()
        {
            if (GetComponent<Canvas>() == null) return;

            // Arriba y al centro. Pegado a la derecha quedaba descolgado del resto de la
            // interfaz, que esta centrada abajo: el ojo del jugador tiene que hacer un
            // recorrido vertical, no diagonal.
            var raiz = Nodo("HudRecursos", (RectTransform)transform);
            raiz.anchorMin = new Vector2(0.5f, 1f);
            raiz.anchorMax = new Vector2(0.5f, 1f);
            raiz.pivot = new Vector2(0.5f, 1f);
            raiz.anchoredPosition = new Vector2(0f, -margen.y);
            raiz.sizeDelta = new Vector2(
                tamanoCaja.x * 3f + separacion * 2f, tamanoCaja.y);

            var recursos = new[] { TipoRecurso.Oro, TipoRecurso.Madera, TipoRecurso.Carne };

            float x0 = -raiz.sizeDelta.x * 0.5f;

            for (int i = 0; i < recursos.Length; i++)
            {
                float x = x0 + tamanoCaja.x * (i + 0.5f) + separacion * i;
                _contadores[i] = ConstruirContador(raiz, recursos[i],
                                                   new Vector2(x, -tamanoCaja.y * 0.5f));
            }

            ConstruirPoblacion();
        }

        /// <summary>
        /// El contador de población, arriba a la izquierda y en su propia caja.
        /// </summary>
        /// <remarks>
        /// Aparte de los recursos, y no como una cuarta caja de la misma fila, porque no es
        /// un recurso: no se recolecta, no se gasta en construir y no sube al depositar. Es
        /// un límite. Mezclarlo con el oro y la madera invita a leerlo como «cuánto tengo»
        /// cuando lo que dice es «cuánto me cabe».
        /// </remarks>
        RectTransform _cajaPoblacion;

        /// <summary>
        /// Aparta el contador de población si el panel lateral está abierto.
        /// </summary>
        /// <remarks>
        /// Las dos cosas viven arriba a la izquierda, así que el panel tapaba justo el único
        /// dato del HUD que no se puede deducir mirando el mapa.
        ///
        /// La conversión de unidades no es un detalle: el panel se dibuja con <c>OnGUI</c>, en
        /// píxeles reales de pantalla, y el HUD vive en un lienzo escalado a una resolución de
        /// referencia de 1920. Sumar el ancho del panel tal cual dejaría el contador bien en
        /// un monitor y mal en todos los demás.
        /// </remarks>
        void LateUpdate()
        {
            if (_cajaPoblacion == null) return;

            float desplazamiento = 0f;

            // Se aparta siguiendo la ANIMACION del panel, no su estado. Asi la caja acompana
            // al panel mientras se despliega en vez de saltar de golpe, y con el panel
            // plegado vuelve a su esquina: el boton esta a media altura y no le estorba.
            var panel = Pruebas.PanelDePruebas.Actual;
            if (panel != null && Screen.width > 0)
            {
                var lienzo = GetComponentInParent<Canvas>();
                float escala = lienzo != null ? lienzo.scaleFactor : 1f;

                if (escala > 0.0001f) desplazamiento = panel.Empuje / escala;
            }

            var sitio = new Vector2(margen.x + desplazamiento, -margen.y);
            if (_cajaPoblacion.anchoredPosition != sitio)
                _cajaPoblacion.anchoredPosition = sitio;
        }

        void ConstruirPoblacion()
        {
            var raiz = Nodo("HudPoblacion", (RectTransform)transform);
            _cajaPoblacion = raiz;
            raiz.anchorMin = new Vector2(0f, 1f);
            raiz.anchorMax = new Vector2(0f, 1f);
            raiz.pivot = new Vector2(0f, 1f);
            raiz.anchoredPosition = new Vector2(margen.x, -margen.y);
            raiz.sizeDelta = tamanoCaja;

            // La caja se ancla al centro superior de su raíz, así que media caja hacia abajo
            // la deja centrada dentro. Es la misma cuenta que usan los tres contadores.
            var censo = ConstruirContador(raiz, TipoRecurso.Ninguno,
                                          new Vector2(0f, -tamanoCaja.y * 0.5f),
                                          "Poblacion");

            _poblacion = censo.Numero;

            // Un punto menos que los recursos: «12 / 50» son siete caracteres donde los
            // demás tienen tres, y a 28 puntos se le comían el icono.
            _poblacion.fontSize = 24;
            _poblacion.text = "0 / 0";
        }

        Contador ConstruirContador(RectTransform padre, TipoRecurso recurso, Vector2 posicion,
                                   string nombre = null)
        {
            var caja = Nodo(nombre ?? recurso.ToString(), padre);
            caja.anchorMin = caja.anchorMax = new Vector2(0.5f, 1f);
            caja.pivot = new Vector2(0.5f, 0.5f);
            caja.anchoredPosition = posicion;
            caja.sizeDelta = tamanoCaja;

            var fondo = caja.gameObject.AddComponent<Image>();
            fondo.sprite = tema != null ? tema.CajaChicaDe(faccion) : null;
            fondo.type = Image.Type.Sliced;
            fondo.enabled = fondo.sprite != null;

            // El icono y el número van centrados COMO PAREJA dentro de la caja, y de eso se
            // encarga un layout y no unas coordenadas a mano. Colocándolos a mano habría que
            // saber de antemano lo que ocupa el número, y ese ancho cambia entre «87» y
            // «1240»: la pareja se descentraría sola en cuanto la economía creciera.
            var fila = caja.gameObject.AddComponent<HorizontalLayoutGroup>();
            fila.childAlignment = TextAnchor.MiddleCenter;
            fila.spacing = 10f;
            // Más relleno abajo que arriba: la caja de madera del pack tiene el reborde
            // inferior más grueso, así que un centrado geométrico se ve caído.
            fila.padding = new RectOffset(10, 10, 0, 12);
            fila.childControlWidth = true;
            fila.childControlHeight = true;
            fila.childForceExpandWidth = false;
            fila.childForceExpandHeight = false;

            // El icono a la izquierda y el número justo donde acaba: si el texto empezara
            // antes, el icono le comería los dígitos y solo se vería el último. Ya pasó con
            // las estadísticas del panel en la semana 04.
            var icono = Nodo("Icono", caja);

            var imagen = icono.gameObject.AddComponent<Image>();
            imagen.sprite = IconoDe(recurso);
            imagen.preserveAspect = true;
            imagen.enabled = imagen.sprite != null;

            // Un Image no declara tamaño preferido por su cuenta, así que sin esto el layout
            // lo dejaría en cero de ancho y el icono desaparecería.
            var medida = icono.gameObject.AddComponent<LayoutElement>();
            medida.preferredWidth = ladoIcono;
            medida.preferredHeight = ladoIcono;

            var texto = Nodo("Numero", caja);

            var etiqueta = texto.gameObject.AddComponent<Text>();
            etiqueta.font = _fuente;
            etiqueta.fontSize = 28;
            etiqueta.fontStyle = FontStyle.Bold;
            etiqueta.alignment = TextAnchor.MiddleCenter;
            etiqueta.color = colorTexto;
            etiqueta.horizontalOverflow = HorizontalWrapMode.Overflow;
            etiqueta.verticalOverflow = VerticalWrapMode.Overflow;
            etiqueta.raycastTarget = false;
            etiqueta.text = "0";

            var contorno = texto.gameObject.AddComponent<Outline>();
            contorno.effectColor = colorContorno;
            contorno.effectDistance = new Vector2(1.8f, -1.8f);

            return new Contador { Recurso = recurso, Numero = etiqueta };
        }

        Sprite IconoDe(TipoRecurso recurso)
        {
            if (tema == null) return null;

            switch (recurso)
            {
                case TipoRecurso.Oro: return tema.iconoRecursoOro;
                case TipoRecurso.Madera: return tema.iconoRecursoMadera;
                case TipoRecurso.Carne: return tema.iconoRecursoCarne;

                // La población se dibuja con la cara del pawn. Es el retrato que el jugador
                // ya asocia con «una unidad», así que el contador se entiende sin leyenda.
                default: return tema.RetratoDe(TipoUnidad.Pawn, faccion);
            }
        }

        static RectTransform Nodo(string nombre, RectTransform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);

            return rt;
        }
    }
}
