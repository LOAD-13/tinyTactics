using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// El ejército come. Cada unidad viva gasta carne, y con la despensa vacía las tropas
    /// pierden fuelle hasta que se reponga.
    ///
    /// <b>Para qué sirve esta regla.</b> Sin ella, un RTS premia gastar todo el banco en un
    /// único ataque: si sale, ganas; si no, has perdido igual, así que no hay decisión. Con
    /// sustento hay que mantener la economía <i>durante</i> la guerra, y las ovejas del pack
    /// dejan de ser decoración para pasar a ser un sitio que merece la pena defender.
    ///
    /// Es la mecánica más sensible al balance de todo el proyecto, y por eso el ritmo entero
    /// cuelga de una sola perilla en los datos de economía.
    /// </summary>
    [RequireComponent(typeof(Economia))]
    [AddComponentMenu("Tiny Tactics/Sustento")]
    public class Sustento : MonoBehaviour
    {
        [Tooltip("Cada cuánto se pasa la cuenta. No hace falta cada frame.")]
        [Range(0.05f, 2f)] public float intervalo = 0.25f;

        Economia _economia;
        float _proxima;
        float[] _gasto;

        void Awake()
        {
            _economia = GetComponent<Economia>();

            // El acumulador se reserva una vez. Crearlo en cada pasada serían cuatro
            // asignaciones por segundo que el recolector de basura acaba pagando.
            _gasto = new float[Mathf.Max(1, _economia.bandos)];
        }

        void Update()
        {
            if (_economia == null || _economia.datos == null) return;
            if (Time.time < _proxima) return;

            _proxima = Time.time + intervalo;
            Cobrar(intervalo);
        }

        /// <summary>
        /// Suma lo que come cada bando y se lo descuenta.
        ///
        /// Un solo recorrido de todas las unidades por pasada, no una consulta por bando:
        /// con cinco bandos y cincuenta unidades cada uno, preguntar por bando sería
        /// recorrer la misma lista cinco veces para repartir los mismos números.
        /// </summary>
        void Cobrar(float segundos)
        {
            var unidades = RegistroDeUnidades.Todas;
            if (unidades.Count == 0 || _gasto == null) return;

            System.Array.Clear(_gasto, 0, _gasto.Length);

            for (int i = 0; i < unidades.Count; i++)
            {
                var u = unidades[i];
                if (u == null || !u.Viva || u.datos == null) continue;
                if (u.datos.invulnerable) continue;   // el poste de pruebas no come
                if (u.faccion < 0 || u.faccion >= _gasto.Length) continue;

                _gasto[u.faccion] += u.datos.carnePorSegundo;
            }

            float ritmo = _economia.datos.ritmoSustento * segundos;

            for (int f = 0; f < _gasto.Length; f++)
                if (_gasto[f] > 0f) _economia.ConsumirCarne(f, _gasto[f] * ritmo);

            PasarHambre(unidades, segundos);
        }

        float[] _deuda;

        /// <summary>
        /// Un bando sin carne pierde vida, poco a poco y en todas sus unidades.
        /// </summary>
        /// <remarks>
        /// <b>Hasta la semana 10 el hambre solo bajaba dano y velocidad, y no se notaba.</b>
        /// Una partida entera a cero de carne terminaba sin que nadie lo arreglara, porque
        /// el castigo era invisible: con menos dano y menos velocidad sigues jugando igual
        /// de mal que el rival, que tambien esta sin carne. Quitando vida, el hambre se
        /// convierte en un reloj — y la carne, en una decision.
        ///
        /// Se lleva una DEUDA por bando en vez de redondear cada pasada. A un cuarto de
        /// segundo por pasada, el dano por unidad es una fraccion: redondeando se quedaria
        /// en cero siempre y el hambre no haria nada. Acumulando, la deuda llega a uno y se
        /// cobra entera.
        ///
        /// Y se cobra SIN destello: el destello cuenta un golpe, y veinte unidades
        /// parpadeando a la vez cada dos segundos haria ilegible cualquier pelea.
        /// </remarks>
        void PasarHambre(IReadOnlyList<Unidades.Unidad> unidades, float segundos)
        {
            var datos = _economia.datos;
            if (datos == null || datos.vidaPorHambre <= 0f) return;

            if (_deuda == null || _deuda.Length != _gasto.Length)
                _deuda = new float[_gasto.Length];

            bool alguien = false;

            for (int f = 0; f < _deuda.Length; f++)
            {
                if (!_economia.ConHambre(f)) { _deuda[f] = 0f; continue; }

                _deuda[f] += datos.vidaPorHambre * segundos;
                if (_deuda[f] >= 1f) alguien = true;
            }

            if (!alguien) return;

            for (int i = 0; i < unidades.Count; i++)
            {
                var u = unidades[i];
                if (u == null || !u.Viva || u.datos == null || u.datos.invulnerable) continue;
                if (u.faccion < 0 || u.faccion >= _deuda.Length) continue;

                int golpe = Mathf.FloorToInt(_deuda[u.faccion]);
                if (golpe <= 0) continue;

                u.RecibirDano(golpe, null, false);
            }

            for (int f = 0; f < _deuda.Length; f++)
                if (_deuda[f] >= 1f) _deuda[f] -= Mathf.Floor(_deuda[f]);
        }

        // -----------------------------------------------------------------
        // Consulta
        // -----------------------------------------------------------------

        /// <summary>Cuánto daño conserva un bando. 1 mientras tenga carne.</summary>
        public static float FactorDano(int faccion) => Factor(faccion, true);

        /// <summary>Cuánta velocidad conserva un bando. 1 mientras tenga carne.</summary>
        public static float FactorVelocidad(int faccion) => Factor(faccion, false);

        static float Factor(int faccion, bool dano)
        {
            var eco = Economia.Actual;
            if (eco == null || eco.datos == null || !eco.ConHambre(faccion)) return 1f;

            return dano ? eco.datos.danoConHambre : eco.datos.velocidadConHambre;
        }

        /// <summary>True si al bando le queda poca carne. Lo usa el HUD para avisar.</summary>
        public static bool EnAviso(int faccion)
        {
            var eco = Economia.Actual;
            if (eco == null || eco.datos == null) return false;

            return eco.Cantidad(faccion, Datos.TipoRecurso.Carne) <= eco.datos.avisoCarne;
        }
    }
}
