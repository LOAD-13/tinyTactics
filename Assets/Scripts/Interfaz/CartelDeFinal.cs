using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Entrada;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El cartel de VICTORIA o DERROTA con el resumen de la partida.
    /// </summary>
    /// <remarks>
    /// <b>No es un GIF ni una imagen: es interfaz animada por código.</b> Se descartó generar
    /// la pantalla como imagen por dos razones. La primera, práctica: un GIF no escala de
    /// resolución y no puede mostrar cifras que cambian, y aquí todo lo que se enseña son
    /// cifras. La segunda, de estilo: los generadores de imagen no mantienen la rejilla de
    /// píxeles, así que lo que producen parece pixel art pero tiene el contorno, la paleta y
    /// la escala mal, y puesto al lado de Tiny Swords se nota en dos segundos.
    ///
    /// Montarlo con piezas del propio pack —pergamino, cinta, espadas y los retratos que ya
    /// usa el panel— garantiza que encaje, porque <i>es</i> el pack.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Cartel de final")]
    public class CartelDeFinal : MonoBehaviour
    {
        public TemaInterfaz tema;

        [Tooltip("Lo que tarda el cartel en entrar.")]
        [Range(0.2f, 2f)] public float entrada = 0.55f;

        [Tooltip("Segundos entre una línea de estadísticas y la siguiente.")]
        [Range(0.02f, 0.4f)] public float cascada = 0.09f;

        [Tooltip("Ancho del cartel en píxeles de la resolución de referencia.")]
        public float anchoCartel = 620f;

        bool _visible;
        float _reloj;
        Desenlace _desenlace = Desenlace.EnJuego;
        int _bando;

        readonly List<(string etiqueta, string valor)> _lineas =
            new List<(string, string)>(16);

        Sprite _retratoMvp;
        string _tituloMvp = "";

        GUIStyle _titulo, _etiqueta, _valor, _pie, _boton;
        bool _estilosListos;

        Texture2D _velo;

        ArbitroDePartida _arbitro;

        /// <summary>
        /// Se engancha al árbitro en cuanto exista, no en <c>OnEnable</c>.
        /// </summary>
        /// <remarks>
        /// Unity no garantiza el orden de los <c>Awake</c> entre objetos distintos, así que
        /// suscribirse en <c>OnEnable</c> es una apuesta: si el cartel despierta antes que el
        /// árbitro, <c>Actual</c> todavía es nulo, no se suscribe nadie y la pantalla de final
        /// no aparece nunca. El síntoma sería «a veces sale y a veces no», que es la peor
        /// clase de fallo. Es la misma trampa de orden que dejó a un pawn dentro de una casa
        /// en la semana 06.
        /// </remarks>
        void Enganchar()
        {
            if (_arbitro != null) return;

            _arbitro = ArbitroDePartida.Actual;
            if (_arbitro != null) _arbitro.AlTerminar += AlTerminar;
        }

        void OnDisable()
        {
            if (_arbitro != null) _arbitro.AlTerminar -= AlTerminar;
            _arbitro = null;

            if (_velo != null) Destroy(_velo);
        }

        void AlTerminar(int ganador) => Mostrar();

        /// <summary>
        /// Levanta el cartel con el resultado del bando que el jugador esté llevando.
        /// </summary>
        /// <remarks>
        /// El resultado se calcula <b>al mostrarlo</b>, no al terminar la partida, y de ahí
        /// sale el truco de demostración: con el panel de pruebas se cambia de bando y se
        /// vuelve a levantar el cartel para ver el mismo final desde el otro lado.
        /// </remarks>
        public void Mostrar()
        {
            var selector = SelectorDeUnidades.Actual;
            _bando = selector != null ? selector.faccionJugador : 0;

            var arbitro = ArbitroDePartida.Actual;
            _desenlace = arbitro != null ? arbitro.DesenlacePara(_bando) : Desenlace.Derrota;

            Componer();

            _visible = true;
            _reloj = 0f;
        }

        public void Ocultar() => _visible = false;

        void Componer()
        {
            _lineas.Clear();

            var libro = EstadisticasPartida.Actual;
            if (libro == null) return;

            var h = libro.Hoja(_bando);

            _lineas.Add(("Duración", Reloj(libro.Duracion)));
            _lineas.Add(("Oro recolectado", $"{h.OroRecolectado}"));
            _lineas.Add(("Madera recolectada", $"{h.MaderaRecolectada}"));
            _lineas.Add(("Carne recolectada", $"{h.CarneRecolectada}"));
            _lineas.Add(("Sin gastar", $"{h.SinGastar}"));
            _lineas.Add(("Unidades entrenadas", $"{h.UnidadesEntrenadas}"));
            _lineas.Add(("Unidades perdidas", $"{h.UnidadesPerdidas}"));
            _lineas.Add(("Bajas causadas", $"{h.UnidadesEliminadas}"));
            _lineas.Add(("Edificios construidos", $"{h.EdificiosConstruidos}"));
            _lineas.Add(("Edificios destruidos", $"{h.EdificiosDestruidos}"));
            _lineas.Add(("Pico de población", $"{h.PicoDePoblacion}"));

            _lineas.Add(("Primer combate",
                         h.PrimerCombate < 0f ? "sin combate" : Reloj(h.PrimerCombate)));

            _lineas.Add(("Órdenes por minuto",
                         $"{libro.OrdenesPorMinuto(_bando):0}"));

            // El MVP solo aparece si hubo bajas. Un "MVP: Pawn, 0 bajas" es peor que no
            // enseñar nada: da la cifra y la desmiente en la misma línea.
            _retratoMvp = null;
            _tituloMvp = "";

            if (h.BajasDelMvp > 0)
            {
                _tituloMvp = $"{h.Mvp} · {h.BajasDelMvp} bajas";
                if (tema != null) _retratoMvp = tema.RetratoDe(h.Mvp, _bando);
            }
        }

        static string Reloj(float segundos)
        {
            int t = Mathf.Max(0, Mathf.RoundToInt(segundos));
            return $"{t / 60:00}:{t % 60:00}";
        }

        void Update()
        {
            Enganchar();

            if (_visible) _reloj += Time.unscaledDeltaTime;
        }

        // -----------------------------------------------------------------
        // Dibujo
        // -----------------------------------------------------------------

        void OnGUI()
        {
            if (!_visible) return;

            PrepararEstilos();

            float t = Mathf.Clamp01(_reloj / Mathf.Max(0.05f, entrada));

            // Rebote: 0 → 1,1 → 1. Es lo que separa «apareció un cartel» de «cayó un cartel».
            float escala = t < 1f
                ? Mathf.Lerp(0f, 1.1f, 1f - (1f - t) * (1f - t))
                : 1f;

            if (_reloj > entrada)
            {
                float asiento = Mathf.Clamp01((_reloj - entrada) / 0.18f);
                escala = Mathf.Lerp(1.1f, 1f, asiento);
            }

            DibujarVelo(t);

            float ancho = anchoCartel * escala;
            float alto = (anchoCartel * 0.92f) * escala;

            var caja = new Rect((Screen.width - ancho) * 0.5f,
                                (Screen.height - alto) * 0.5f - 10f, ancho, alto);

            DibujarPergamino(caja);
            DibujarLineas(caja);
            DibujarMvp(caja);
            DibujarPie(caja);

            // El titular va AL FINAL para que quede por encima del papel: cuelga del borde
            // superior y lo pisa a propósito, que es como se monta un cartel de verdad.
            DibujarTitulo(caja, t);
        }

        /// <summary>
        /// El velo oscuro del fondo. En derrota, además, apaga el color.
        /// </summary>
        /// <remarks>
        /// No se dessatura el mapa de verdad —eso necesitaría un material o un efecto de
        /// cámara— sino que se cubre con un velo gris azulado bastante opaco. A este nivel de
        /// oscurecimiento el resultado en pantalla es el mismo y no cuesta nada.
        /// </remarks>
        void DibujarVelo(float t)
        {
            if (_velo == null)
            {
                _velo = new Texture2D(1, 1);
                _velo.SetPixel(0, 0, Color.white);
                _velo.Apply();
            }

            Color fondo = _desenlace == Desenlace.Victoria
                ? new Color(0.05f, 0.06f, 0.10f, 0.62f * t)
                : new Color(0.10f, 0.10f, 0.11f, 0.80f * t);

            var color = GUI.color;
            GUI.color = fondo;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _velo);
            GUI.color = color;
        }

        /// <summary>
        /// Las espadas cruzadas, detrás de la cinta y asomando por los dos lados.
        /// </summary>
        /// <remarks>
        /// Van <b>detrás</b> y son claramente más anchas que la cinta. Puestas delante tapan
        /// justo la palabra que el cartel existe para enseñar, y puestas cortas no se leen
        /// como espadas cruzadas sino como un adorno indefinido. Detrás y largas es como se
        /// monta un escudo de armas, y es lo que hace que la cinta parezca colgada de algo en
        /// vez de flotar.
        /// </remarks>
        void DibujarEspadas(Rect cinta, float t)
        {
            if (tema == null || tema.espadas == null) return;

            // Mucho más ancho que la cinta: las hojas tienen que sobresalir por ambos lados
            // o el cruce queda escondido detrás y no se entiende qué es.
            float ancho = cinta.width * 1.55f;
            float alto = ancho * (tema.espadas.height / (float)tema.espadas.width);

            var sitio = new Rect(cinta.center.x - ancho * 0.5f,
                                 cinta.center.y - alto * 0.5f, ancho, alto);

            var color = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, t);

            // Entra girando un poco y se asienta. El giro es lo que da la sensación de que el
            // cartel cae en su sitio en vez de aparecer.
            var matriz = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Lerp(-10f, 0f, t), sitio.center);

            GUI.DrawTexture(sitio, tema.espadas, ScaleMode.StretchToFill);

            GUI.matrix = matriz;
            GUI.color = color;
        }

        void DibujarPergamino(Rect caja)
        {
            if (tema == null || tema.pergamino == null)
            {
                GUI.Box(caja, GUIContent.none);
                return;
            }

            // 26 px de borde: es lo que mide el marco oscuro del pergamino del pack.
            DibujoGUI.NueveCortes(caja, tema.pergamino, 26f);
        }


        void DibujarTitulo(Rect caja, float t)
        {
            string palabra = _desenlace == Desenlace.Victoria ? "VICTORIA" : "DERROTA";

            // La cinta se dimensiona AL TEXTO, no a una fracción del cartel. Con una fracción
            // fija, «DERROTA» —una letra menos— nadaba dentro de la misma cinta que
            // «VICTORIA», y la palabra se salía por arriba porque la caja era bastante más
            // alta que la línea de texto.
            Vector2 medida = _titulo.CalcSize(new GUIContent(palabra));

            float ancho = Mathf.Max(medida.x * 1.9f, caja.width * 0.52f);
            float alto = ancho * 0.34f;

            // A caballo del borde superior del papel: mitad fuera, mitad dentro.
            var cinta = new Rect(caja.center.x - ancho * 0.5f, caja.y - alto * 0.52f,
                                 ancho, alto);

            DibujarEspadas(cinta, t);

            if (tema != null && tema.cinta != null)
                GUI.DrawTexture(cinta, tema.cinta, ScaleMode.StretchToFill);

            var color = GUI.color;
            GUI.color = _desenlace == Desenlace.Victoria
                ? new Color(1f, 0.93f, 0.62f)
                : new Color(0.96f, 0.72f, 0.68f);

            // Un pelo por encima del centro: la cinta del pack tiene los pliegues abajo, y el
            // texto centrado geométricamente se ve caído.
            var texto = new Rect(cinta.x, cinta.y - alto * 0.06f, cinta.width, cinta.height);
            GUI.Label(texto, palabra, _titulo);

            GUI.color = color;
        }

        void DibujarLineas(Rect caja)
        {
            float alto = 22f;
            float y = caja.y + caja.height * 0.24f;
            float x = caja.x + caja.width * 0.12f;
            float ancho = caja.width * 0.76f;

            for (int i = 0; i < _lineas.Count; i++)
            {
                // La cascada. Que las cifras entren una a una y no todas de golpe es el 80 %
                // de la sensación de «partida terminada»: el ojo las lee en orden en vez de
                // encontrarse un muro de números.
                float momento = entrada + i * cascada;
                if (_reloj < momento) return;

                float aparicion = Mathf.Clamp01((_reloj - momento) / 0.16f);

                var color = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, aparicion);

                // Entra deslizándose desde la izquierda. Poco, seis píxeles: lo justo para
                // que se note el movimiento sin que parezca que la línea se ha descolocado.
                float desvio = (1f - aparicion) * 6f;

                GUI.Label(new Rect(x - desvio, y, ancho * 0.62f, alto),
                          _lineas[i].etiqueta, _etiqueta);

                GUI.Label(new Rect(x + ancho * 0.62f, y, ancho * 0.38f, alto),
                          _lineas[i].valor, _valor);

                GUI.color = color;
                y += alto;
            }
        }

        void DibujarMvp(Rect caja)
        {
            if (_retratoMvp == null || string.IsNullOrEmpty(_tituloMvp)) return;

            float momento = entrada + _lineas.Count * cascada;
            if (_reloj < momento) return;

            float lado = caja.width * 0.13f;
            var marco = new Rect(caja.x + caja.width * 0.12f,
                                 caja.yMax - caja.height * 0.27f, lado, lado);

            DibujoGUI.Sprite(marco, _retratoMvp);

            GUI.Label(new Rect(marco.xMax + 10f, marco.y + lado * 0.28f,
                               caja.width * 0.6f, 24f),
                      $"MVP · {_tituloMvp}", _etiqueta);
        }

        /// <summary>
        /// Los botones del pie: volver a jugar y cerrar.
        /// </summary>
        /// <remarks>
        /// <b>Volver a jugar recarga la escena</b>, que es lo único correcto hoy: el estado de
        /// una partida está repartido entre la grilla, la economía, el censo, el registro de
        /// unidades y los edificios, y reiniciarlo a mano sería ir componente por componente
        /// acordándose de todos. Recargar no puede olvidarse de ninguno.
        ///
        /// Cuando en la semana 12 exista el menú principal, este botón pasará por él y
        /// aparecerá al lado un «salir al menú». La forma de la pantalla ya está preparada
        /// para dos botones justamente por eso.
        /// </remarks>
        void DibujarPie(Rect caja)
        {
            float ancho = caja.width * 0.34f;
            float alto = 34f;

            var izquierda = new Rect(caja.center.x - ancho - 6f,
                                     caja.yMax - alto - caja.height * 0.045f, ancho, alto);

            var derecha = new Rect(caja.center.x + 6f, izquierda.y, ancho, alto);

            if (GUI.Button(izquierda, "Volver a jugar", _boton)) VolverAJugar();
            if (GUI.Button(derecha, "Cerrar", _boton)) Ocultar();

            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.spaceKey.wasPressedThisFrame) Ocultar();
        }

        void VolverAJugar()
        {
            Ocultar();

            // El tiempo vuelve a la normalidad antes de recargar: si la partida terminó con
            // el panel de pruebas en x4 o en pausa, la siguiente arrancaría igual y no habría
            // forma de saber por qué.
            Time.timeScale = 1f;

            var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(escena.buildIndex);
        }

        void PrepararEstilos()
        {
            if (_estilosListos) return;
            _estilosListos = true;

            var fuente = tema != null ? tema.titular : null;

            _titulo = new GUIStyle(GUI.skin.label)
            {
                font = fuente,
                fontSize = 46,
                alignment = TextAnchor.MiddleCenter,
            };

            // Las cifras NO usan MedievalSharp. Es vectorial y a tamaño pequeño se suaviza,
            // que junto al pixel art del pack se lee como texto pegado encima. El titular
            // grande sí la luce; las líneas se quedan con la fuente de sistema.
            _etiqueta = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
            };
            _etiqueta.normal.textColor = new Color(0.22f, 0.18f, 0.13f);

            _valor = new GUIStyle(_etiqueta)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
            };

            _pie = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
            };
            _pie.normal.textColor = new Color(0.35f, 0.30f, 0.24f);

            // Los botones sí llevan MedievalSharp: son texto grande sobre el pergamino, que
            // es donde la tipografía luce. Las cifras de arriba siguen con la del sistema.
            _boton = new GUIStyle(GUI.skin.button)
            {
                font = fuente,
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
            };
        }
    }
}
