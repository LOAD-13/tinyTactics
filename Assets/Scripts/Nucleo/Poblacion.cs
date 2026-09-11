using System;
using UnityEngine;
using TinyTactics.Edificios;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Cuánta población tiene ocupada cada bando y cuánta puede tener.
    ///
    /// <b>El tope no es una regla arbitraria: es el presupuesto de rendimiento.</b> Cada
    /// unidad ocupa al menos un punto, así que un tope de 50 puntos garantiza por sí solo
    /// que ningún bando pase de 50 unidades — los 250 objetos simultáneos con los que se
    /// dimensionó todo el proyecto. El límite de diseño y el límite técnico son el mismo
    /// número, y eso hace imposible que se separen por descuido.
    ///
    /// <b>Nada se guarda: se recuenta.</b> Ni la población usada ni el tope se llevan como
    /// contadores que haya que subir y bajar a mano. Se suman de las unidades vivas y de los
    /// edificios en pie cada pocas décimas. Cuesta un recorrido corto y se cura solo: una
    /// unidad que muere libera su hueco sin que nadie tenga que acordarse de descontarla, y
    /// una casa destruida baja el tope sin código extra el día que los edificios se puedan
    /// destruir.
    /// </summary>
    [RequireComponent(typeof(Economia))]
    [AddComponentMenu("Tiny Tactics/Población")]
    public class Poblacion : MonoBehaviour
    {
        [Tooltip("Tope duro por bando. Es el presupuesto de rendimiento, no un número de diseño.")]
        [Range(10, 100)] public int topeMaximo = 50;

        [Tooltip("Cada cuánto se recuenta. No hace falta cada frame.")]
        [Range(0.05f, 2f)] public float intervalo = 0.25f;

        Economia _economia;
        int[] _usada;
        int[] _tope;
        float _proxima;

        /// <summary>Instancia activa. Hay una sola por escena.</summary>
        public static Poblacion Actual { get; private set; }

        /// <summary>Avisa con el bando cuya población acaba de cambiar.</summary>
        public event Action<int> AlCambiar;

        void Awake()
        {
            Actual = this;
            _economia = GetComponent<Economia>();

            int bandos = Mathf.Max(1, _economia != null ? _economia.bandos : 1);
            _usada = new int[bandos];
            _tope = new int[bandos];
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        void Start() => Recontar();

        void Update()
        {
            if (Time.time < _proxima) return;

            _proxima = Time.time + intervalo;
            Recontar();
        }

        // -----------------------------------------------------------------
        // Consulta
        // -----------------------------------------------------------------

        bool Valido(int faccion) =>
            _usada != null && faccion >= 0 && faccion < _usada.Length;

        public int Usada(int faccion) => Valido(faccion) ? _usada[faccion] : 0;

        public int Tope(int faccion) => Valido(faccion) ? _tope[faccion] : 0;

        public int Libre(int faccion) => Mathf.Max(0, Tope(faccion) - Usada(faccion));

        /// <summary>True si el bando está al límite y no puede entrenar nada más.</summary>
        public bool Lleno(int faccion) => Libre(faccion) <= 0;

        /// <summary>
        /// ¿Cabe una unidad más de este coste?
        /// </summary>
        /// <remarks>
        /// Se pregunta <b>antes</b> de encolar, no antes de sacarla. Cobrar la población al
        /// terminar permitiría llenar la cola de diez guerreros con sitio para dos, y el
        /// jugador descubriría el problema cuando ya hubiera pagado el oro de los diez.
        /// </remarks>
        public bool Cabe(int faccion, int coste) => Usada(faccion) + coste <= Tope(faccion);

        // -----------------------------------------------------------------

        /// <summary>
        /// Recuenta lo usado y el tope a partir de lo que existe ahora mismo en el mundo.
        /// </summary>
        void Recontar()
        {
            if (_usada == null) return;

            Array.Clear(_usada, 0, _usada.Length);
            Array.Clear(_tope, 0, _tope.Length);

            // Lo que ocupan las unidades vivas.
            var unidades = RegistroDeUnidades.Todas;
            for (int i = 0; i < unidades.Count; i++)
            {
                var u = unidades[i];
                if (u == null || !u.Viva || u.datos == null) continue;
                if (u.datos.invulnerable) continue;          // el poste de pruebas no ocupa
                if (!Valido(u.faccion)) continue;

                _usada[u.faccion] += Mathf.Max(1, u.datos.poblacion);
            }

            // Lo que ocupan las unidades ya encargadas. Sin esto, encolar diez con sitio para
            // dos sería posible, y el tope dejaría de significar nada hasta que salieran.
            var edificios = Edificio.Todos;
            for (int i = 0; i < edificios.Count; i++)
            {
                var e = edificios[i];
                if (e == null || !Valido(e.faccion)) continue;

                _tope[e.faccion] += e.PoblacionQueAporta;

                var produccion = e.GetComponent<ProduccionEdificio>();
                if (produccion != null) _usada[e.faccion] += produccion.PoblacionEncolada;
            }

            for (int f = 0; f < _tope.Length; f++)
                _tope[f] = Mathf.Min(_tope[f], topeMaximo);

            Avisar();
        }

        int[] _pintado;

        /// <summary>Solo dispara el evento si de verdad cambió algo.</summary>
        void Avisar()
        {
            if (_pintado == null || _pintado.Length != _usada.Length * 2)
                _pintado = new int[_usada.Length * 2];

            for (int f = 0; f < _usada.Length; f++)
            {
                if (_pintado[f * 2] == _usada[f] && _pintado[f * 2 + 1] == _tope[f]) continue;

                _pintado[f * 2] = _usada[f];
                _pintado[f * 2 + 1] = _tope[f];

                AlCambiar?.Invoke(f);
            }
        }
    }
}
