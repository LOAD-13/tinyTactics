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

        class Contador
        {
            public TipoRecurso Recurso;
            public Text Numero;
            public int Pintado = -1;
        }

        readonly Contador[] _contadores = new Contador[3];

        Font _fuente;
        Economia _economia;

        void Awake()
        {
            _fuente = PanelDeUnidad.PrimeraInstalada(fuentes);
            Construir();
        }

        void OnEnable()
        {
            _economia = Economia.Actual;

            // La economía puede no existir todavía si el orden de Awake juega en contra.
            // Start lo resuelve: para entonces todos los Awake han corrido.
            if (_economia != null) _economia.AlCambiar += AlCambiar;
        }

        void Start()
        {
            if (_economia == null)
            {
                _economia = Economia.Actual;
                if (_economia != null) _economia.AlCambiar += AlCambiar;
            }

            Refrescar();
        }

        void OnDisable()
        {
            if (_economia != null) _economia.AlCambiar -= AlCambiar;
            _economia = null;
        }

        void AlCambiar(int bando)
        {
            if (bando == faccion) Refrescar();
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
        }

        Contador ConstruirContador(RectTransform padre, TipoRecurso recurso, Vector2 posicion)
        {
            var caja = Nodo(recurso.ToString(), padre);
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
                default: return null;
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
