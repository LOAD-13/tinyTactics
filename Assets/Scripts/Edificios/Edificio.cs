using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Edificios
{
    /// <summary>Qué edificio es. Hoy solo hay castillo; el resto llega con la épica E05.</summary>
    public enum TipoEdificio { Castillo, Casa, Cuartel, CampoDeTiro, Monasterio, Torre }

    /// <summary>
    /// Un edificio del mapa: a qué bando pertenece y qué se puede hacer con él.
    ///
    /// Hasta esta semana el castillo era un sprite decorativo. Se convierte en entidad
    /// porque la economía lo necesita para dos cosas distintas: es <b>donde se deposita</b>
    /// lo recolectado y es <b>donde se entrena</b>. Ese doble papel es lo que hace que la
    /// posición del castillo importe y lo que cierra el bucle de la partida.
    ///
    /// No tiene vida ni se puede destruir todavía: eso es la épica E06. Meterlo ahora sería
    /// escribir un sistema de daño a edificios sin nada que los ataque.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Edificio")]
    public class Edificio : MonoBehaviour
    {
        [Header("Identidad")]
        public TipoEdificio tipo = TipoEdificio.Castillo;
        public string nombreVisible = "Castillo";

        [Tooltip("Índice de bando. 0 es el jugador humano.")]
        public int faccion;

        [Header("Interfaz")]
        [Tooltip("Retrato para el panel. Se recorta del propio sprite del edificio.")]
        public Sprite retrato;

        [Header("Economía")]
        [Tooltip("Los pawns cargados vienen aquí a soltar lo que traen.")]
        public bool centroDeEntrega = true;

        [Tooltip("Tamaño real del dibujo, en tiles. Medido sobre el PNG, no estimado.")]
        public Vector2 huella = new Vector2(4.88f, 3.25f);

        [Tooltip("Desplazamiento del centro del dibujo respecto al centro del lienzo.")]
        public Vector2 huellaCentro = new Vector2(0f, -0.27f);

        [Tooltip("Dónde aparecen las unidades recién entrenadas, relativo al edificio.")]
        public Vector2 puntoSalida = new Vector2(0f, -2.6f);

        [Header("Selección")]
        [Tooltip("Holgura alrededor de la huella en la que un clic ya cuenta como suyo.")]
        [Range(0f, 2f)] public float holguraSeleccion = 0.25f;

        public bool Seleccionado { get; private set; }

        /// <summary>Centro real del edificio, no el del lienzo del sprite.</summary>
        public Vector3 PuntoDeEntrega =>
            transform.position + new Vector3(huellaCentro.x, huellaCentro.y, 0f);

        /// <summary>
        /// A qué distancia queda un punto del <b>borde</b> del edificio. Cero si está encima.
        /// </summary>
        /// <remarks>
        /// Un castillo mide casi cinco tiles de ancho, así que medir contra su centro
        /// significaba que un pawn pegado a la esquina estaba «a dos tiles y medio» y uno
        /// pegado al frente estaba «a uno y medio»: el mismo sitio en la práctica y dos
        /// respuestas distintas. Medir contra la huella hace que arrimarse valga por
        /// cualquier lado.
        /// </remarks>
        public float DistanciaA(Vector3 punto)
        {
            Vector3 centro = PuntoDeEntrega;

            float dx = Mathf.Max(0f, Mathf.Abs(punto.x - centro.x) - huella.x * 0.5f);
            float dy = Mathf.Max(0f, Mathf.Abs(punto.y - centro.y) - huella.y * 0.5f);

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Punto del edificio más cercano a quien viene: el pawn se arrima por el lado por
        /// el que llega, en vez de rodear el castillo entero para ir a una puerta imaginaria.
        /// </summary>
        public Vector3 PuntoDeEntregaDesde(Vector3 origen)
        {
            Vector3 centro = PuntoDeEntrega;
            Vector2 mitad = huella * 0.5f;

            return new Vector3(
                Mathf.Clamp(origen.x, centro.x - mitad.x, centro.x + mitad.x),
                Mathf.Clamp(origen.y, centro.y - mitad.y, centro.y + mitad.y),
                0f);
        }

        public Vector3 PuntoDeSalida =>
            transform.position + new Vector3(puntoSalida.x, puntoSalida.y, 0f);

        Transform _anillo;

        // -----------------------------------------------------------------
        // Registro
        // -----------------------------------------------------------------

        static readonly List<Edificio> _todos = new List<Edificio>();

        public static IReadOnlyList<Edificio> Todos => _todos;

        void Awake()
        {
            _anillo = transform.Find("Seleccion");
            MostrarAnillo(false);
        }

        void OnEnable() => _todos.Add(this);
        void OnDisable() => _todos.Remove(this);

        public void Seleccionar(bool valor)
        {
            if (Seleccionado == valor) return;

            Seleccionado = valor;
            MostrarAnillo(valor);
        }

        void MostrarAnillo(bool visible)
        {
            if (_anillo != null) _anillo.gameObject.SetActive(visible);
        }

        // -----------------------------------------------------------------
        // Búsqueda
        // -----------------------------------------------------------------

        /// <summary>
        /// Centro de entrega propio más cercano a un punto.
        ///
        /// Es lo que permitirá que un castillo adicional junto a una expansión lejana
        /// acorte el viaje de vuelta sin que el pawn tenga que saber nada de mapas: pregunta
        /// por el más cercano y ya está.
        /// </summary>
        public static Edificio EntregaMasCercana(Vector3 punto, int faccion)
        {
            Edificio mejor = null;
            float mejorDistancia = float.MaxValue;

            for (int i = 0; i < _todos.Count; i++)
            {
                var e = _todos[i];
                if (e == null || !e.centroDeEntrega || e.faccion != faccion) continue;

                float d = e.DistanciaA(punto);
                if (d >= mejorDistancia) continue;

                mejorDistancia = d;
                mejor = e;
            }

            return mejor;
        }

        /// <summary>Edificio propio bajo un punto del mundo, para el clic de selección.</summary>
        public static Edificio EdificioEn(Vector3 punto, int faccion)
        {
            Edificio mejor = null;
            float mejorDistancia = float.MaxValue;

            for (int i = 0; i < _todos.Count; i++)
            {
                var e = _todos[i];
                if (e == null || e.faccion != faccion) continue;

                // Vale clicar cualquier parte del edificio, no solo su centro. Con un radio
                // se quedaban fuera las dos torres de los extremos, que es justo donde el
                // ojo apunta cuando quieres seleccionar un castillo.
                float d = e.DistanciaA(punto);
                if (d > e.holguraSeleccion || d >= mejorDistancia) continue;

                mejorDistancia = d;
                mejor = e;
            }

            return mejor;
        }
    }
}
