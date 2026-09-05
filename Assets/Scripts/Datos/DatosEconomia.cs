using UnityEngine;

namespace TinyTactics.Datos
{
    /// <summary>
    /// Todos los números de la economía en un solo sitio.
    ///
    /// Vive en un <see cref="ScriptableObject"/> por el ADR-07, igual que las estadísticas
    /// de unidad. La semana 16 es de balance puro y la lleva Raúl, que no programa: si para
    /// bajar el ritmo de la carne hay que abrir un <c>.cs</c> y recompilar, el balance lo
    /// acaba haciendo el programador con prisa en vez del que ha jugado las partidas.
    /// </summary>
    [CreateAssetMenu(fileName = "Economia", menuName = "Tiny Tactics/Datos de economía")]
    public class DatosEconomia : ScriptableObject
    {
        [Header("Reserva inicial")]
        [Min(0)] public int oroInicial = 200;
        [Min(0)] public int maderaInicial = 150;
        [Min(0)] public int carneInicial = 100;

        [Header("Veta de oro")]
        [Tooltip("Viajes que aguanta la veta antes de agotarse.")]
        [Min(1)] public int extraccionesOro = 50;

        [Tooltip("Oro que se lleva el pawn en cada viaje.")]
        [Min(1)] public int cargaOro = 10;

        [Tooltip("Golpes de pico antes de llenar la carga. Se cuentan pasadas enteras de " +
                 "la animación, no segundos: así un golpe a medias no cuenta para nada.")]
        [Range(1, 20)] public int golpesOro = 5;

        [Header("Árbol")]
        [Min(1)] public int extraccionesMadera = 12;
        [Min(1)] public int cargaMadera = 10;
        [Range(1, 20)] public int golpesMadera = 6;

        [Header("Oveja")]
        [Tooltip("Una sola. Una oveja no se ordeña por viajes: se sacrifica y se acabó.")]
        [Min(1)] public int extraccionesCarne = 1;

        [Min(1)] public int cargaCarne = 25;
        [Range(1, 20)] public int golpesCarne = 4;

        [Header("Recolección")]
        [Tooltip("Holgura sobre el final del camino para dar por bueno que ha llegado. " +
                 "No es la distancia de trabajo: el pawn anda hasta la ultima celda libre.")]
        [Range(0.5f, 4f)] public float alcanceTrabajo = 1.2f;

        [Tooltip("Radio en el que buscar otro nodo del mismo tipo cuando el actual se agota.")]
        [Range(2f, 40f)] public float radioRelevo = 14f;

        [Header("Sustento")]
        [Tooltip("Multiplica el consumo de carne de todas las unidades. Es la perilla global.")]
        [Range(0.01f, 2f)] public float ritmoSustento = 0.1f;

        [Tooltip("Por debajo de esta reserva el contador avisa.")]
        [Min(0)] public int avisoCarne = 25;

        [Tooltip("Daño que conserva el ejército con la despensa vacía.")]
        [Range(0.1f, 1f)] public float danoConHambre = 0.5f;

        [Tooltip("Velocidad que conserva el ejército con la despensa vacía.")]
        [Range(0.1f, 1f)] public float velocidadConHambre = 0.6f;

        [Header("Producción")]
        [Tooltip("Segundos que tarda el castillo en sacar un pawn.")]
        [Min(0.5f)] public float tiempoPawn = 12f;

        [Tooltip("Cuántas unidades caben en la cola de un edificio.")]
        [Range(1, 12)] public int colaMaxima = 5;

        // -----------------------------------------------------------------

        /// <summary>Viajes que aguanta un nodo de este tipo.</summary>
        public int ExtraccionesDe(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Oro: return extraccionesOro;
                case TipoRecurso.Madera: return extraccionesMadera;
                case TipoRecurso.Carne: return extraccionesCarne;
                default: return 0;
            }
        }

        /// <summary>Cuánto se lleva el pawn en un viaje.</summary>
        public int CargaDe(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Oro: return cargaOro;
                case TipoRecurso.Madera: return cargaMadera;
                case TipoRecurso.Carne: return cargaCarne;
                default: return 0;
            }
        }

        /// <summary>Golpes completos que cuesta una carga.</summary>
        public int GolpesDe(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Oro: return golpesOro;
                case TipoRecurso.Madera: return golpesMadera;
                case TipoRecurso.Carne: return golpesCarne;
                default: return 1;
            }
        }
    }
}
