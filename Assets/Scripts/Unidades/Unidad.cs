using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Una unidad del juego: a qué bando pertenece, si está seleccionada y sus datos.
    ///
    /// No lee el input ni decide a dónde ir. Recibe <b>órdenes</b> (ADR-01) y las ejecuta.
    /// Esa separación es lo que permite que la IA use exactamente el mismo camino que el
    /// jugador, y lo que mantiene abierta la puerta del multijugador.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Tiny Tactics/Unidad")]
    public class Unidad : MonoBehaviour
    {
        [Header("Pertenencia")]
        [Tooltip("Índice de bando. 0 es el jugador humano.")]
        public int faccion;

        public DatosUnidad datos;

        [Header("Estado")]
        [SerializeField] int _vida = 60;

        public int Vida => _vida;
        public bool Viva => _vida > 0;
        public bool Seleccionada { get; private set; }

        /// <summary>Radio de separación; cae de vuelta a un valor sano si faltan datos.</summary>
        public float Radio => datos != null ? datos.radio : 0.42f;

        public float Velocidad => datos != null ? datos.velocidad : 3f;

        Transform _anillo;

        void Awake()
        {
            if (datos != null) _vida = datos.vidaMaxima;
            _anillo = transform.Find("Seleccion");
            MostrarAnillo(false);
        }

        void OnEnable() => RegistroDeUnidades.Registrar(this);
        void OnDisable() => RegistroDeUnidades.Olvidar(this);

        public void Seleccionar(bool valor)
        {
            if (Seleccionada == valor) return;
            Seleccionada = valor;
            MostrarAnillo(valor);
        }

        void MostrarAnillo(bool visible)
        {
            if (_anillo != null) _anillo.gameObject.SetActive(visible);
        }

        public void RecibirDano(int cantidad)
        {
            _vida = Mathf.Max(0, _vida - cantidad);
            if (_vida == 0) gameObject.SetActive(false);
        }

        /// <summary>Configura la unidad desde el generador de la escena.</summary>
        public void Configurar(DatosUnidad nuevosDatos, int nuevaFaccion)
        {
            datos = nuevosDatos;
            faccion = nuevaFaccion;
            _vida = nuevosDatos != null ? nuevosDatos.vidaMaxima : 60;
        }
    }
}
