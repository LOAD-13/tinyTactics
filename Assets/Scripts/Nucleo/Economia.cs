using System;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// La despensa de un bando: cuánto oro, madera y carne tiene ahora mismo.
    ///
    /// La carne se guarda en decimales porque el sustento la gasta de forma continua y a
    /// ritmos por debajo de la unidad. Redondear cada frame haría que un consumo de 0,15
    /// por segundo no gastara nunca nada.
    /// </summary>
    [Serializable]
    public class AlmacenRecursos
    {
        public int oro;
        public int madera;
        public float carne;

        public int Cantidad(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Oro: return oro;
                case TipoRecurso.Madera: return madera;
                case TipoRecurso.Carne: return Mathf.FloorToInt(carne);
                default: return 0;
            }
        }
    }

    /// <summary>
    /// Única fuente de verdad de lo que tiene cada bando.
    ///
    /// Todo el que quiera saber o cambiar recursos pasa por aquí: el HUD, la producción del
    /// castillo, el pawn que deposita y, cuando llegue, la IA. Es el mismo principio del
    /// ADR-01 aplicado a la economía — un solo sitio por donde entra y sale todo, de modo
    /// que añadir red o repetir una partida no obliga a buscar quién más tocaba el oro.
    ///
    /// <b>Avisa por evento, no se consulta cada frame.</b> Con cinco bandos y un HUD que
    /// preguntara en <c>Update</c>, se estarían leyendo quince cifras por fotograma para
    /// redibujar tres que casi nunca cambian.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Economía")]
    public class Economia : MonoBehaviour
    {
        [Header("Balance")]
        public DatosEconomia datos;

        [Header("Bandos")]
        [Tooltip("Cuántos almacenes crear. Uno por bando en juego.")]
        [Range(1, 5)] public int bandos = 3;

        [SerializeField] AlmacenRecursos[] _almacenes;

        /// <summary>Instancia activa. Hay una sola economía por escena.</summary>
        public static Economia Actual { get; private set; }

        /// <summary>Avisa con el índice del bando cuyos recursos acaban de cambiar.</summary>
        public event Action<int> AlCambiar;

        void Awake()
        {
            Actual = this;

            if (datos == null)
            {
                Debug.LogError("[Tiny Tactics] Economía sin DatosEconomia asignados.", this);
                enabled = false;
                return;
            }

            _almacenes = new AlmacenRecursos[bandos];

            for (int i = 0; i < bandos; i++)
                _almacenes[i] = new AlmacenRecursos
                {
                    oro = datos.oroInicial,
                    madera = datos.maderaInicial,
                    carne = datos.carneInicial
                };
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        // -----------------------------------------------------------------
        // Consulta
        // -----------------------------------------------------------------

        bool BandoValido(int faccion) =>
            _almacenes != null && faccion >= 0 && faccion < _almacenes.Length;

        /// <summary>Almacén de un bando, o null si el índice no existe.</summary>
        public AlmacenRecursos Almacen(int faccion) =>
            BandoValido(faccion) ? _almacenes[faccion] : null;

        public int Cantidad(int faccion, TipoRecurso recurso) =>
            BandoValido(faccion) ? _almacenes[faccion].Cantidad(recurso) : 0;

        public bool PuedePagar(int faccion, int oro, int madera)
        {
            if (!BandoValido(faccion)) return false;

            var a = _almacenes[faccion];
            return a.oro >= oro && a.madera >= madera;
        }

        // -----------------------------------------------------------------
        // Cambio
        // -----------------------------------------------------------------

        /// <summary>Ingresa lo que trae un pawn.</summary>
        public void Depositar(int faccion, TipoRecurso recurso, int cantidad)
        {
            if (!BandoValido(faccion) || cantidad <= 0) return;

            var a = _almacenes[faccion];

            switch (recurso)
            {
                case TipoRecurso.Oro: a.oro += cantidad; break;
                case TipoRecurso.Madera: a.madera += cantidad; break;
                case TipoRecurso.Carne: a.carne += cantidad; break;
                default: return;
            }

            AlCambiar?.Invoke(faccion);
        }

        /// <summary>
        /// Cobra un coste. Devuelve false y no toca nada si no alcanza.
        ///
        /// Comprobar y cobrar van juntos a propósito: si fueran dos llamadas, entre una y
        /// otra podría colarse otro gasto y dejar la reserva en negativo. Es el mismo
        /// motivo por el que nadie suma o resta estos campos desde fuera.
        /// </summary>
        public bool Cobrar(int faccion, int oro, int madera)
        {
            if (!PuedePagar(faccion, oro, madera)) return false;

            var a = _almacenes[faccion];
            a.oro -= oro;
            a.madera -= madera;

            AlCambiar?.Invoke(faccion);
            return true;
        }

        /// <summary>Devuelve un coste cobrado, al cancelar una unidad en cola.</summary>
        public void Reembolsar(int faccion, int oro, int madera)
        {
            if (!BandoValido(faccion)) return;

            var a = _almacenes[faccion];
            a.oro += Mathf.Max(0, oro);
            a.madera += Mathf.Max(0, madera);

            AlCambiar?.Invoke(faccion);
        }

        /// <summary>
        /// Gasto continuo de carne. Lo llama el sustento cada frame.
        ///
        /// Solo avisa cuando cruza un número entero: el HUD muestra enteros, así que
        /// disparar el evento sesenta veces por segundo redibujaría el mismo texto.
        /// </summary>
        public void ConsumirCarne(int faccion, float cantidad)
        {
            if (!BandoValido(faccion) || cantidad <= 0f) return;

            var a = _almacenes[faccion];
            if (a.carne <= 0f) return;

            int antes = Mathf.FloorToInt(a.carne);
            a.carne = Mathf.Max(0f, a.carne - cantidad);

            if (Mathf.FloorToInt(a.carne) != antes) AlCambiar?.Invoke(faccion);
        }

        /// <summary>True si a ese bando se le acabó la despensa.</summary>
        public bool ConHambre(int faccion) =>
            BandoValido(faccion) && _almacenes[faccion].carne <= 0f;
    }
}
