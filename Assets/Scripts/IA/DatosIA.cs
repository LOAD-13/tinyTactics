using UnityEngine;

namespace TinyTactics.IA
{
    /// <summary>Las tres dificultades. Los números llegan en la semana 11.</summary>
    public enum NivelDeIA { Facil, Normal, Dificil }

    /// <summary>
    /// Qué cambia de una dificultad a otra.
    /// </summary>
    /// <remarks>
    /// <b>La dificultad es economía y ritmo, no cerebro</b> (ADR-06). Es como funcionaban de
    /// verdad Warcraft, StarCraft y Age of Empires: la dificultad alta no piensa mejor, tiene
    /// más y llega antes. Implementar «una IA más lista» es un problema de investigación
    /// abierto; implementar tres multiplicadores es una tarde, y para el jugador el resultado
    /// se siente igual de difícil.
    ///
    /// Esta semana solo existe el perfil Normal, con todos los multiplicadores a uno. Los
    /// tres se calibran en la semana 11, y calibrarlos exige haber visto jugar a uno — meter
    /// tres sin eso es calibrar a ciegas.
    /// </remarks>
    [CreateAssetMenu(fileName = "DificultadIA", menuName = "Tiny Tactics/Dificultad de IA")]
    public class DatosIA : ScriptableObject
    {
        public NivelDeIA nivel = NivelDeIA.Normal;
        public string nombreVisible = "Normal";

        [Header("Economia")]
        [Tooltip("Multiplica lo que el bot recibe al depositar. En Normal vale 1: juega con " +
                 "las mismas reglas que tu.")]
        [Range(0.5f, 3f)] public float ventajaDeRecursos = 1f;

        [Header("Ritmo")]
        [Tooltip("Multiplica el tamano de cada oleada.")]
        [Range(0.3f, 2f)] public float tamanoDeOleada = 1f;

        [Tooltip("Segundos de espera antes de la primera oleada. Un bot que ataca en el " +
                 "minuto uno no da partida: gana o pierde antes de que haya juego.")]
        [Range(0f, 300f)] public float esperaInicial = 60f;

        [Header("Percepcion")]
        [Tooltip("Si el bot hace trampa y ve el mapa entero. En Normal, NO: juega con su " +
                 "propia niebla, igual que tu. La decision para las otras dos dificultades " +
                 "esta puesta en la semana 11.")]
        public bool veTodoElMapa;
    }
}
