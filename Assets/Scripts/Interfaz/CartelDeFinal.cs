using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
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

        readonly List<(Sprite icono, int valor, float fraccion)> _barras =
            new List<(Sprite, int, float)>(3);

        Sprite _retratoMvp;
        string _tituloMvp = "";

        /// <summary>
        /// Copia de la curva de poblacion en el momento de levantar el cartel.
        /// </summary>
        /// <remarks>
        /// Es una COPIA y no la lista del registro a proposito: el muestreo sigue corriendo
        /// mientras el cartel esta en pantalla, asi que apuntando a la lista viva la grafica
        /// seguiria creciendo por detras y la animacion de dibujado nunca terminaria de
        /// cuadrar con lo que se esta viendo.
        /// </remarks>
        readonly List<int> _historia = new List<int>(256);

        float _pasoHistoria = 2f;

        GUIStyle _titulo, _etiqueta, _valor, _boton, _hueco;
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

            // Los tres recursos salen como BARRAS y no como tres líneas más de texto. Son
            // la única terna comparable entre sí de toda la hoja, y una barra contesta de un
            // vistazo la pregunta que el jugador se hace —«¿de qué fui corto?»— que trece
            // números alineados no contestan nunca.
            _barras.Clear();

            int techo = Mathf.Max(1, Mathf.Max(h.OroRecolectado,
                                  Mathf.Max(h.MaderaRecolectada, h.CarneRecolectada)));

            _barras.Add((tema != null ? tema.iconoRecursoOro : null,
                         h.OroRecolectado, h.OroRecolectado / (float)techo));
            _barras.Add((tema != null ? tema.iconoRecursoMadera : null,
                         h.MaderaRecolectada, h.MaderaRecolectada / (float)techo));
            _barras.Add((tema != null ? tema.iconoRecursoCarne : null,
                         h.CarneRecolectada, h.CarneRecolectada / (float)techo));

            _historia.Clear();
            _historia.AddRange(h.Historia);
            _pasoHistoria = libro.PasoDeHistoria;

            _lineas.Add(("Duración", Reloj(libro.Duracion)));
            _lineas.Add(("Sin gastar", $"{h.SinGastar}"));
            _lineas.Add(("Unidades entrenadas", $"{h.UnidadesEntrenadas}"));
            _lineas.Add(("Unidades perdidas", $"{h.UnidadesPerdidas}"));
            _lineas.Add(("Bajas causadas", $"{h.UnidadesEliminadas}"));
            _lineas.Add(("Edificios construidos", $"{h.EdificiosConstruidos}"));
            _lineas.Add(("Edificios destruidos", $"{h.EdificiosDestruidos}"));

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

            // El tamaño sale de la PANTALLA, no de una constante. Con 620 px fijos, en la
            // ventana del editor el cartel era más alto que la vista y el titular se quedaba
            // fuera por arriba — que es justo lo que pasaba.
            //
            // Se reserva además una cabecera para la cinta, que cuelga por encima del papel:
            // sin ese hueco, centrar el papel deja el titular medio cortado.
            // El tamaño sale de la PANTALLA y deja sitio a los retratos de los lados. El
            // papel nunca pasa del 66 % del ancho: el resto es el hueco donde asoman las
            // unidades, que es lo que le da profundidad al cartel.
            float cabecera = Mathf.Min(Screen.height * 0.13f, 120f);

            float altoPapel = Mathf.Min(anchoCartel * 0.98f,
                                        Screen.height * 0.84f - cabecera);

            float anchoPapel = Mathf.Min(anchoCartel, Screen.width * 0.66f,
                                         altoPapel * 1.12f);

            altoPapel *= escala;
            anchoPapel *= escala;

            var caja = new Rect((Screen.width - anchoPapel) * 0.5f,
                                (Screen.height - altoPapel) * 0.5f + cabecera * 0.45f,
                                anchoPapel, altoPapel);

            DibujarPergamino(caja);

            // El pie —MVP y botones— tiene su trozo reservado, y las líneas se reparten lo
            // que quede. Antes cada bloque se colocaba con su propia fracción del alto y con
            // el papel pequeño acababan pisándose unos a otros.
            float suelo = caja.yMax - caja.height * 0.30f;

            float y = DibujarBarras(caja);
            y = DibujarGrafica(caja, y, suelo);
            DibujarLineas(caja, y, suelo);

            DibujarMvp(caja, suelo);
            DibujarPie(caja);

            // Los retratos van DELANTE del papel, pero casi enteros FUERA de él: solo el
            // borde interior queda montado sobre el pergamino. Detrás quedaban tapados, y
            // metidos dentro tapan las cifras. Fuera y delante es lo único que da profundidad
            // sin robarle sitio a los datos, que son el motivo de la pantalla.
            DibujarEscolta(caja, t);

            // El titular va AL FINAL para que quede por encima del papel: cuelga del borde
            // superior y lo pisa a propósito, que es como se monta un cartel de verdad.
            DibujarTitulo(caja, t, cabecera);
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
            Color fondo = _desenlace == Desenlace.Victoria
                ? new Color(0.05f, 0.06f, 0.10f, 0.62f * t)
                : new Color(0.10f, 0.10f, 0.11f, 0.80f * t);

            var color = GUI.color;
            GUI.color = fondo;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Velo());
            GUI.color = color;
        }

        /// <summary>Un píxel blanco reutilizable, para velos y canales de barra.</summary>
        Texture2D Velo()
        {
            if (_velo != null) return _velo;

            _velo = new Texture2D(1, 1);
            _velo.SetPixel(0, 0, Color.white);
            _velo.Apply();
            return _velo;
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

            // El tamaño se ata al ALTO de la cinta, no a su ancho. Atado al ancho, la espada
            // crecía en proporción 2:1 hasta medir el triple de alto que la cinta y se metía
            // encima de las estadísticas — que es justo lo que se veía.
            float alto = cinta.height * 1.35f;
            float ancho = alto * (tema.espadas.width / (float)tema.espadas.height);

            // Un poco por encima del centro de la cinta: así asoma la hoja por arriba y el
            // estandarte parece colgado de ella.
            var sitio = new Rect(cinta.center.x - ancho * 0.5f,
                                 cinta.center.y - alto * 0.62f, ancho, alto);

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


        void DibujarTitulo(Rect caja, float t, float cabecera)
        {
            string palabra = _desenlace == Desenlace.Victoria ? "VICTORIA" : "DERROTA";

            // La cinta se estira a lo ancho del cartel y ALGO MÁS. Antes se dimensionaba al
            // texto, y con siete u ocho letras salía una cinta corta y rechoncha que parecía
            // una pegatina: un estandarte se lee como estandarte cuando es claramente más
            // largo que alto y más ancho que lo que cuelga de él.
            float ancho = Mathf.Min(caja.width * 1.24f, Screen.width * 0.9f);

            // El alto lo manda la cabecera reservada, no la proporción del dibujo: así la
            // cinta nunca puede crecer hasta comerse las estadísticas, que es lo que pasaba.
            float alto = Mathf.Min(ancho * 0.19f, cabecera * 1.05f);

            // A caballo del borde superior del papel: mitad fuera, mitad dentro.
            var cinta = new Rect(caja.center.x - ancho * 0.5f, caja.y - alto * 0.62f,
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
            var texto = new Rect(cinta.x, cinta.y - alto * 0.08f, cinta.width, cinta.height);

            // Sombra dura de un píxel debajo. El listón del pack es azul claro con vetas, y
            // sin sombra la palabra se deshace justo encima de los pliegues.
            var tinta = GUI.color;
            GUI.color = new Color(0.06f, 0.08f, 0.16f, 0.85f);
            GUI.Label(new Rect(texto.x + 2f, texto.y + 2f, texto.width, texto.height),
                      palabra, _titulo);

            GUI.color = tinta;
            GUI.Label(texto, palabra, _titulo);

            GUI.color = color;
        }

        /// <summary>
        /// Las tres barras de recursos. Devuelve la Y donde puede seguir el resto.
        /// </summary>
        /// <remarks>
        /// Las barras se dibujan con el marco y el relleno del propio pack, los mismos que
        /// usan la vida y el progreso de fabricación. Es lo que hace que se lean como parte
        /// del juego y no como un gráfico pegado encima.
        /// </remarks>
        float DibujarBarras(Rect caja)
        {
            float x = caja.x + caja.width * 0.12f;
            float ancho = caja.width * 0.76f;
            float y = caja.y + caja.height * 0.13f;

            float alto = Mathf.Max(20f, caja.height * 0.052f);
            float lado = alto * 1.15f;

            for (int i = 0; i < _barras.Count; i++)
            {
                float momento = entrada + i * cascada;
                if (_reloj < momento) return y;

                float aparicion = Mathf.Clamp01((_reloj - momento) / 0.22f);

                var color = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, aparicion);

                DibujoGUI.Sprite(new Rect(x, y, lado, lado), _barras[i].icono);

                float x0 = x + lado + 6f;
                float anchoCanal = ancho - lado - 6f - 52f;

                var canal = new Rect(x0, y + alto * 0.22f, anchoCanal, alto * 0.56f);

                // El canal vacío es un rectángulo oscuro traslúcido: sin él, una barra corta
                // no se distingue de una barra que no se ha dibujado.
                GUI.color = new Color(0.22f, 0.18f, 0.13f, 0.28f * aparicion);
                GUI.DrawTexture(canal, Velo());

                GUI.color = new Color(1f, 1f, 1f, aparicion);

                // La barra crece con la aparición: no aparece llena, se llena.
                var lleno = new Rect(canal.x, canal.y,
                                     canal.width * _barras[i].fraccion * aparicion,
                                     canal.height);

                if (tema != null && tema.barraGrandeRelleno != null)
                    DibujoGUI.Sprite(lleno, tema.barraGrandeRelleno);

                GUI.Label(new Rect(x + ancho - 52f, y, 52f, alto),
                          $"{_barras[i].valor}", _valor);

                GUI.color = color;
                y += alto + 4f;
            }

            return y + 6f;
        }

        void DibujarLineas(Rect caja, float desde, float suelo)
        {
            // El alto de línea sale de lo que queda, no de una fracción del cartel. Es la
            // diferencia entre una hoja que siempre cabe y una que se sale por abajo en
            // cuanto la ventana es un poco más baja de lo previsto.
            int cuantas = Mathf.Max(1, _lineas.Count);
            float alto = Mathf.Clamp((suelo - desde) / cuantas, 12f, 24f);

            float y = desde;
            float x = caja.x + caja.width * 0.12f;
            float ancho = caja.width * 0.76f;

            for (int i = 0; i < _lineas.Count; i++)
            {
                // La cascada. Que las cifras entren una a una y no todas de golpe es el 80 %
                // de la sensación de «partida terminada»: el ojo las lee en orden en vez de
                // encontrarse un muro de números.
                float momento = entrada + (_barras.Count + i) * cascada;
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

        /// <summary>
        /// La curva de población a lo largo de la partida. Devuelve la Y de continuación.
        /// </summary>
        /// <remarks>
        /// Es la única cifra de la hoja que cuenta <i>cuándo</i> pasaron las cosas. «Pico de
        /// población: 30» dice el número más alto pero no si se alcanzó al minuto tres o al
        /// quince, ni cuántas veces te rehiciste. La curva dice las dos cosas de un vistazo:
        /// una subida sostenida es una economía que funcionó, un diente de sierra son
        /// batallas perdidas y rehechas, y una caída en vertical al final es el momento en
        /// que se acabó la partida.
        ///
        /// Se dibuja como columnas y no como línea a propósito: <c>OnGUI</c> no sabe trazar
        /// líneas, y una línea a base de rectángulos rotados a esta escala se ve peor que
        /// unas columnas limpias. Además la lectura es la misma.
        /// </remarks>
        float DibujarGrafica(Rect caja, float desde, float suelo)
        {
            if (_historia.Count < 2) return desde;

            float momento = entrada + _barras.Count * cascada;
            if (_reloj < momento) return desde;

            float aparicion = Mathf.Clamp01((_reloj - momento) / 0.55f);

            float x = caja.x + caja.width * 0.12f;
            float ancho = caja.width * 0.76f;
            float alto = Mathf.Clamp((suelo - desde) * 0.34f, 40f, 84f);

            // Se reserva sitio abajo para el eje de tiempos y arriba para el titulillo.
            var marco = new Rect(x + 26f, desde + 16f, ancho - 26f, alto);

            var color = GUI.color;

            // --- Techo REDONDEADO hacia arriba, nunca el pico exacto.
            //
            // Con techo igual al pico, la columna mas alta toca siempre el borde y una
            // partida de poblacion 2 salia como un bloque macizo del 100 %: identica a una
            // de poblacion 40. El techo tiene que ser una cifra redonda por encima del pico
            // para que la altura signifique algo.
            int pico = 1;
            for (int i = 0; i < _historia.Count; i++)
                if (_historia[i] > pico) pico = _historia[i];

            int techo = Techo(pico);

            DibujarRejilla(marco, techo, aparicion);

            // --- Columnas
            int columnas = Mathf.Min(48, _historia.Count);
            float paso = marco.width / columnas;

            var relleno = _desenlace == Desenlace.Victoria
                ? new Color(0.45f, 0.62f, 0.38f, 0.55f)
                : new Color(0.60f, 0.40f, 0.36f, 0.55f);

            var cresta = _desenlace == Desenlace.Victoria
                ? new Color(0.30f, 0.48f, 0.24f)
                : new Color(0.52f, 0.28f, 0.24f);

            for (int c = 0; c < columnas; c++)
            {
                int desdeIdx = Mathf.FloorToInt(c * _historia.Count / (float)columnas);
                int hastaIdx = Mathf.FloorToInt((c + 1) * _historia.Count / (float)columnas);

                // El MAXIMO de cada tramo, no la media: de un pico lo que importa es que
                // hubo pico, y promediando desaparece justo lo que se quiere ver.
                int valor = 0;
                for (int i = desdeIdx; i < hastaIdx && i < _historia.Count; i++)
                    if (_historia[i] > valor) valor = _historia[i];

                float avance = Mathf.Clamp01(aparicion * columnas - c);
                if (avance <= 0f) break;

                float altoColumna = marco.height * (valor / (float)techo) * avance;
                float px = marco.x + c * paso;
                float anchoColumna = Mathf.Max(1f, paso - 1f);

                GUI.color = relleno;
                GUI.DrawTexture(new Rect(px, marco.yMax - altoColumna,
                                         anchoColumna, altoColumna), Velo());

                // La cresta: dos pixeles opacos en el techo de cada columna. Es lo que hace
                // que el conjunto se lea como una CURVA y no como un muro de barras, sin
                // tener que trazar lineas —que OnGUI no sabe hacer.
                GUI.color = cresta;
                GUI.DrawTexture(new Rect(px, marco.yMax - altoColumna - 1f,
                                         anchoColumna, 2f), Velo());
            }

            // --- Rotulos
            GUI.color = new Color(1f, 1f, 1f, aparicion);

            GUI.Label(new Rect(marco.x, desde, marco.width, 15f),
                      "Población a lo largo de la partida", _etiqueta);

            GUI.Label(new Rect(marco.x, marco.yMax + 1f, marco.width, 15f),
                      "0:00", _etiqueta);

            GUI.Label(new Rect(marco.x, marco.yMax + 1f, marco.width, 15f),
                      Reloj(_historia.Count * _pasoHistoria), _valor);

            GUI.color = color;
            return marco.yMax + 20f;
        }

        /// <summary>
        /// La cifra redonda inmediatamente por encima del pico: 5, 10, 20, 50, 100...
        /// </summary>
        /// <remarks>
        /// Sin esto la escala no dice nada. Usando el pico como techo, la columna mas alta
        /// toca el borde SIEMPRE, y una partida que no paso de dos unidades se dibuja
        /// exactamente igual que una que llego a cuarenta.
        /// </remarks>
        static int Techo(int pico)
        {
            int[] escalones = { 5, 10, 20, 30, 50, 75, 100, 150, 200 };

            for (int i = 0; i < escalones.Length; i++)
                if (pico <= escalones[i]) return escalones[i];

            return Mathf.CeilToInt(pico / 50f) * 50;
        }

        /// <summary>Rejilla horizontal con su escala a la izquierda.</summary>
        void DibujarRejilla(Rect marco, int techo, float aparicion)
        {
            const int Lineas = 4;

            for (int i = 0; i <= Lineas; i++)
            {
                float f = i / (float)Lineas;
                float y = marco.yMax - marco.height * f;

                // La base va mas marcada que las intermedias: es el cero, y un cero que se
                // confunde con una linea de rejilla hace dudar de donde empieza la escala.
                GUI.color = new Color(0.30f, 0.25f, 0.19f,
                                      (i == 0 ? 0.45f : 0.16f) * aparicion);

                GUI.DrawTexture(new Rect(marco.x, y, marco.width, 1f), Velo());

                GUI.color = new Color(1f, 1f, 1f, aparicion);
                GUI.Label(new Rect(marco.x - 26f, y - 8f, 22f, 15f),
                          $"{Mathf.RoundToInt(techo * f)}", _valor);
            }
        }

        /// <summary>
        /// Los retratos que asoman por los costados del pergamino.
        /// </summary>
        /// <remarks>
        /// Van detrás del papel en orden de dibujo, de ahí la sensación de profundidad: no
        /// son ilustraciones puestas al lado, son unidades que están <i>detrás</i> del cartel.
        ///
        /// Salen con el color del bando que mira, así que en victoria y en derrota son caras
        /// distintas — las tuyas. Y se atenúan en derrota, que es la misma idea del velo
        /// dessaturado del fondo aplicada a los retratos.
        /// </remarks>
        void DibujarEscolta(Rect caja, float t)
        {
            if (tema == null) return;

            // El retrato se dimensiona por lo que CABE al lado, no por el ancho del papel:
            // así en una ventana estrecha se encoge en vez de meterse sobre las cifras.
            float hueco = (Screen.width - caja.width) * 0.5f;
            float lado = Mathf.Clamp(hueco * 0.86f, 48f, caja.width * 0.26f);

            float aparicion = Mathf.Clamp01((_reloj - entrada * 0.4f) / 0.5f);
            if (aparicion <= 0f) return;

            var color = GUI.color;

            float alfa = (_desenlace == Desenlace.Victoria ? 1f : 0.80f) * aparicion;
            GUI.color = new Color(1f, 1f, 1f, alfa);

            // Solo un 14 % del retrato pisa el pergamino: lo justo para que se lea «delante
            // de» y no «al lado de». Escalonados en altura para que no parezcan un friso.
            float dentro = lado * 0.14f;

            Cara(TipoUnidad.Guerrero, caja.x - lado + dentro, caja.y + caja.height * 0.14f, lado);
            Cara(TipoUnidad.Pawn, caja.x - lado + dentro, caja.y + caja.height * 0.52f, lado * 0.84f);

            Cara(TipoUnidad.Arquero, caja.xMax - dentro, caja.y + caja.height * 0.18f, lado);
            Cara(TipoUnidad.Lancero, caja.xMax - dentro, caja.y + caja.height * 0.56f, lado * 0.84f);

            GUI.color = color;
        }

        void Cara(TipoUnidad tipo, float x, float y, float lado)
        {
            var retrato = tema.RetratoDe(tipo, _bando);
            if (retrato == null) return;

            DibujoGUI.Sprite(new Rect(x, y, lado, lado), retrato);
        }

        /// <summary>
        /// El MVP, en su propia banda entre las cifras y los botones.
        /// </summary>
        /// <remarks>
        /// Tiene banda propia y no una esquina porque si no se solapa: las líneas se estiran
        /// hasta donde haga falta y un retrato metido en un hueco fijo acaba debajo de ellas
        /// en cuanto la lista crece o el papel se encoge.
        /// </remarks>
        void DibujarMvp(Rect caja, float suelo)
        {
            if (_retratoMvp == null || string.IsNullOrEmpty(_tituloMvp)) return;

            float momento = entrada + (_barras.Count + _lineas.Count) * cascada;
            if (_reloj < momento) return;

            float lado = Mathf.Min(caja.width * 0.12f, caja.height * 0.13f);

            var texto = new GUIContent($"MVP · {_tituloMvp}");
            float anchoTexto = _etiqueta.CalcSize(texto).x;

            // El conjunto retrato + texto se centra como un bloque, no cada uno por su lado.
            float total = lado + 8f + anchoTexto;
            float x = caja.center.x - total * 0.5f;

            var marco = new Rect(x, suelo + 6f, lado, lado);
            DibujoGUI.Sprite(marco, _retratoMvp);

            GUI.Label(new Rect(marco.xMax + 8f, marco.y, anchoTexto + 4f, lado),
                      texto, _etiqueta);
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
            float alto = 38f;

            var izquierda = new Rect(caja.center.x - ancho - 6f,
                                     caja.yMax - alto - caja.height * 0.045f, ancho, alto);

            var derecha = new Rect(caja.center.x + 6f, izquierda.y, ancho, alto);

            if (Boton(izquierda, "Volver a jugar")) VolverAJugar();
            if (Boton(derecha, "Cerrar")) Ocultar();

            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.spaceKey.wasPressedThisFrame) Ocultar();
        }

        /// <summary>
        /// Un botón con el arte del pack en vez del gris de Unity.
        /// </summary>
        /// <remarks>
        /// Se reserva el hueco con un botón sin fondo y se pinta encima. Es la única forma:
        /// un <c>GUIStyle</c> solo acepta una textura entera como fondo, y las cajas del pack
        /// son recortes de un atlas. Mismo truco que en el panel lateral.
        /// </remarks>
        bool Boton(Rect caja, string texto)
        {
            bool pulsado = GUI.Button(caja, GUIContent.none, _hueco);

            var fondo = tema != null ? tema.CajaChicaDe(0) : null;
            if (fondo != null) DibujoGUI.NueveCortes(caja, fondo, 20f);
            else GUI.Box(caja, GUIContent.none);

            // La etiqueta sube tres píxeles. MedievalSharp tiene ascendentes largas y su
            // altura de línea es mayor que la caja del glifo, así que un MiddleCenter
            // geométrico deja la palabra visiblemente caída dentro del botón.
            GUI.Label(new Rect(caja.x, caja.y - 3f, caja.width, caja.height), texto, _boton);
            return pulsado;
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


            // Los botones sí llevan MedievalSharp: son texto grande sobre madera, que es
            // donde la tipografía luce. Las cifras de arriba siguen con la del sistema.
            _boton = new GUIStyle(GUI.skin.label)
            {
                font = fuente,
                fontSize = 19,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            _boton.normal.textColor = new Color(1f, 0.97f, 0.90f);

            // Botón sin nada: solo reserva el hueco y detecta el clic.
            _hueco = new GUIStyle(GUI.skin.button);
            _hueco.normal.background = null;
            _hueco.hover.background = null;
            _hueco.active.background = null;
            _hueco.focused.background = null;
            _hueco.border = new RectOffset(0, 0, 0, 0);
        }
    }
}
