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
    public class Unidad : MonoBehaviour, IObjetivo
    {
        // --- IObjetivo -------------------------------------------------------
        //
        // La unidad ya sabía hacer todo esto; lo único que añade la interfaz es un nombre
        // común con el edificio para que quien pega no tenga que distinguirlos.

        int IObjetivo.Faccion => faccion;
        bool IObjetivo.Vivo => Viva;
        bool IObjetivo.EsUnidad => true;

        TipoArmadura IObjetivo.Armadura =>
            datos != null ? datos.armadura : TipoArmadura.Ligera;
        Vector3 IObjetivo.Posicion => transform.position;

        /// <summary>
        /// Distancia al CUERPO del objetivo: entre centros menos su radio.
        /// </summary>
        /// <remarks>
        /// En la semana 07 se midió entre centros a propósito, para no mover unos alcances
        /// que llevaban ajustados desde la semana 04. Fue un error, y este es el caso que lo
        /// demuestra: <b>dos guerreros no podían alcanzarse</b>.
        ///
        /// Las cifras. El guerrero tiene 0,80 de alcance y 0,44 de radio; el empuje blando
        /// separa a dos unidades por la suma de sus radios, o sea 0,88. Midiendo entre
        /// centros, dos guerreros pegados están «a 0,88» y nunca entran en un alcance de
        /// 0,80: se creen fuera, intentan acercarse, el empuje los devuelve, y solo conectan
        /// en los fotogramas sueltos en que el empuje aún no ha terminado de separarlos.
        ///
        /// Restando el radio del objetivo la medida pasa a ser «cuánto falta para tocarlo»,
        /// que es lo que un alcance significa. Y no rompe nada de lo ajustado: el arquero
        /// pasa de 5,00 a 5,44 efectivos, un 9 %, mientras que el cuerpo a cuerpo pasa de
        /// imposible a funcionar.
        /// </remarks>
        float IObjetivo.DistanciaDesde(Vector3 punto) =>
            Mathf.Max(0f, Vector2.Distance(punto, transform.position) - Radio);

        Vector3 IObjetivo.PuntoDeAtaqueDesde(Vector3 origen) => transform.position;

        void IObjetivo.RecibirDano(int cantidad, Unidad agresor) => RecibirDano(cantidad, agresor);

        // ---------------------------------------------------------------------

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
        /// Tiles que esta unidad destapa de la niebla.
        /// </summary>
        /// <remarks>
        /// Cae de vuelta a 8 y no a 0 si faltan los datos: una unidad sin ficha con radio 0
        /// sería una unidad ciega, y una unidad ciega no se nota como un dato mal puesto, se
        /// nota como una niebla que no se abre. Un valor sano falla de forma visible.
        /// </remarks>
        public float RadioDeVision => datos != null && datos.radioVision > 0f
            ? datos.radioVision
            : 8f;

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
        MaquinaDeEstados _maquina;

        void Awake()
        {
            if (datos != null) _vida = datos.vidaMaxima;
            _anillo = transform.Find("Seleccion");
            _maquina = GetComponent<MaquinaDeEstados>();
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

        /// <summary>
        /// Quita vida y, si sobrevive, deja que la unidad reaccione a quien la hirió.
        /// </summary>
        /// <param name="agresor">
        /// Quién pegó. Es opcional porque no todo el daño viene de alguien —el hambre o un
        /// derrumbe no tienen a quién devolverle el golpe— y porque así no hay que tocar
        /// las llamadas que ya existían.
        /// </param>
        public void RecibirDano(int cantidad, Unidad agresor = null)
        {
            if (_vida == 0 || cantidad <= 0) return;

            // El muñeco de pruebas aguanta lo que le echen: está para que le peguen, no
            // para morirse. Quien tiene que caer es el atacante.
            if (datos != null && datos.invulnerable) return;

            _vida = Mathf.Max(0, _vida - cantidad);

            if (_maquina != null) _maquina.Destellar();

            if (_vida > 0)
            {
                // La unidad decide ella sola si responde, huye o sigue a lo suyo: eso
                // depende de su postura y de si el jugador le había mandado algo, y ninguna
                // de las dos cosas es asunto de quien reparte el daño.
                if (agresor != null)
                {
                    var apuntes = EstadisticasPartida.Actual;
                    if (apuntes != null)
                    {
                        apuntes.Combate(faccion);
                        apuntes.Combate(agresor.faccion);
                    }

                    if (_maquina != null) _maquina.Agredida(agresor);
                }

                return;
            }

            var libro = EstadisticasPartida.Actual;
            if (libro != null) libro.UnidadCaida(this, agresor);

            // Morir es un estado, no un interruptor. Antes esto apagaba el objeto de golpe
            // y la unidad desaparecía en un frame; ahora la máquina de estados se encarga
            // del desvanecido y de retirarla cuando termina.
            if (_maquina != null) _maquina.Morir();
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
