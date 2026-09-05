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

        /// <summary>
        /// True mientras la unidad recorre una ruta. La mantiene al día
        /// <c>MovimientoUnidad</c>.
        ///
        /// Vive aquí y no se consulta con <c>GetComponent</c> porque la lee el empuje entre
        /// unidades, que la pregunta por cada vecina y en cada fotograma.
        /// </summary>
        public bool EnMarcha { get; set; }

        /// <summary>Radio de separación; cae de vuelta a un valor sano si faltan datos.</summary>
        public float Radio => datos != null ? datos.radio : 0.42f;

        /// <summary>
        /// Velocidad efectiva. Con la despensa vacía el ejército se arrastra.
        ///
        /// El castigo se aplica aquí y no en cada sitio que lee la velocidad para que sea
        /// imposible olvidarse de uno: quien pregunte por la velocidad de una unidad
        /// obtiene la de verdad, con hambre incluida.
        /// </summary>
        public float Velocidad =>
            (datos != null ? datos.velocidad : 3f) * Sustento.FactorVelocidad(faccion);

        /// <summary>Daño efectivo, con el castigo por hambre ya aplicado. Cura si es negativo.</summary>
        public int Dano
        {
            get
            {
                if (datos == null || datos.dano == 0) return 0;

                float factor = Sustento.FactorDano(faccion);

                // Se redondea hacia arriba en valor absoluto para que un golpe con hambre
                // siga haciendo algo: un daño de 5 por 0,5 no puede quedarse en cero, o el
                // hambre dejaría de ser un castigo para pasar a ser una inmunidad.
                int valor = Mathf.RoundToInt(Mathf.Abs(datos.dano) * factor);
                valor = Mathf.Max(1, valor);

                return datos.dano < 0 ? -valor : valor;
            }
        }

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

        /// <summary>Devuelve vida sin pasarse del máximo.</summary>
        public void Curar(int cantidad)
        {
            if (_vida == 0 || cantidad <= 0) return;

            int maximo = datos != null ? datos.vidaMaxima : 60;
            _vida = Mathf.Min(maximo, _vida + cantidad);
        }

        public void RecibirDano(int cantidad)
        {
            if (_vida == 0 || cantidad <= 0) return;

            // El muñeco de pruebas aguanta lo que le echen: está para que le peguen, no
            // para morirse. Quien tiene que caer es el atacante.
            if (datos != null && datos.invulnerable) return;

            _vida = Mathf.Max(0, _vida - cantidad);
            if (_vida > 0) return;

            // Morir es un estado, no un interruptor. Antes esto apagaba el objeto de golpe
            // y la unidad desaparecía en un frame; ahora la máquina de estados se encarga
            // del desvanecido y de retirarla cuando termina.
            var maquina = GetComponent<MaquinaDeEstados>();

            if (maquina != null) maquina.Morir();
            else gameObject.SetActive(false);
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
