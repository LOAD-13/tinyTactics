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

        GUIStyle _titulo, _etiqueta, _valor, _pie;
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

            DibujarEspadas(caja, t);
            DibujarPergamino(caja);
            DibujarTitulo(caja, t);
            DibujarLineas(caja);
            DibujarMvp(caja);
            DibujarPie(caja);
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

        void DibujarEspadas(Rect caja, float t)
        {
            if (tema == null || tema.espadas == null) return;

            // Las espadas giran un poco al entrar y se quedan quietas. Van DETRÁS del
            // pergamino, asomando por arriba, que es como las coloca el propio pack en sus
            // ejemplos de interfaz.
            float lado = caja.width * 0.52f;
            var sitio = new Rect(caja.center.x - lado * 0.5f,
                                 caja.y - lado * 0.22f, lado, lado * 0.4f);

            var color = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, t);

            var pivote = sitio.center;
            var matriz = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Lerp(-8f, 0f, t), pivote);

            GUI.DrawTexture(sitio, tema.espadas, ScaleMode.ScaleToFit);

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

            GUI.DrawTexture(caja, tema.pergamino, ScaleMode.StretchToFill);
        }

        void DibujarTitulo(Rect caja, float t)
        {
            string palabra = _desenlace == Desenlace.Victoria ? "VICTORIA" : "DERROTA";

            var cinta = new Rect(caja.x + caja.width * 0.08f,
                                 caja.y + caja.height * 0.04f,
                                 caja.width * 0.84f, caja.height * 0.17f);

            if (tema != null && tema.cinta != null)
                GUI.DrawTexture(cinta, tema.cinta, ScaleMode.ScaleToFit);

            var color = GUI.color;
            GUI.color = _desenlace == Desenlace.Victoria
                ? new Color(1f, 0.93f, 0.62f)
                : new Color(0.94f, 0.62f, 0.58f);

            GUI.Label(cinta, palabra, _titulo);
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

            float lado = caja.width * 0.14f;
            var marco = new Rect(caja.x + caja.width * 0.12f,
                                 caja.yMax - caja.height * 0.20f, lado, lado);

            var r = _retratoMvp.textureRect;
            var uv = new Rect(r.x / _retratoMvp.texture.width,
                              r.y / _retratoMvp.texture.height,
                              r.width / _retratoMvp.texture.width,
                              r.height / _retratoMvp.texture.height);

            GUI.DrawTextureWithTexCoords(marco, _retratoMvp.texture, uv, true);

            GUI.Label(new Rect(marco.xMax + 10f, marco.y + lado * 0.28f,
                               caja.width * 0.6f, 24f),
                      $"MVP · {_tituloMvp}", _etiqueta);
        }

        void DibujarPie(Rect caja)
        {
            var pie = new Rect(caja.x, caja.yMax - caja.height * 0.07f, caja.width, 22f);
            GUI.Label(pie, "Pulsa ESPACIO para cerrar", _pie);

            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.spaceKey.wasPressedThisFrame) Ocultar();
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
        }
    }
}
