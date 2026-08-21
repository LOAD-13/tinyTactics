using UnityEngine;

namespace TinyTactics.Datos
{
    /// <summary>Tipos de unidad, con los nombres del pack entre paréntesis en el GDD.</summary>
    public enum TipoUnidad { Pawn, Guerrero, Lancero, Arquero, Monje }

    /// <summary>
    /// Estadísticas de una unidad.
    ///
    /// Vive en un <see cref="ScriptableObject"/> y no en el código (ADR-07): la semana 16
    /// es de balance puro, y si cada ajuste exige recompilar se harán diez iteraciones
    /// en vez de cien.
    /// </summary>
    [CreateAssetMenu(fileName = "Unidad", menuName = "Tiny Tactics/Datos de unidad")]
    public class DatosUnidad : ScriptableObject
    {
        [Header("Identidad")]
        public TipoUnidad tipo = TipoUnidad.Pawn;
        public string nombreVisible = "Pawn";

        [Header("Combate")]
        [Min(1)] public int vidaMaxima = 60;
        public int dano = 5;
        public float alcance = 0.5f;

        [Header("Movimiento")]
        [Tooltip("Unidades de mundo por segundo. Un tile mide 1.")]
        public float velocidad = 3f;

        [Tooltip("Radio de separación. Dos unidades más cerca que la suma de sus radios se empujan.")]
        [Range(0.15f, 1.5f)] public float radio = 0.42f;

        [Header("Sustento")]
        [Tooltip("Carne consumida por segundo mientras la unidad esté viva.")]
        public float carnePorSegundo = 0.1f;

        [Header("Coste")]
        public int oro = 50;
        public int madera;

        [Header("Animación")]
        [Tooltip("Ruta de la tira de reposo dentro del pack, relativa a Assets/Tiny Swords/.")]
        public string rutaReposo = "Pawn and Resources/Pawn/{color} Pawn/Pawn_Idle.png";

        [Tooltip("Ruta de la tira de caminar. {color} se sustituye por la facción.")]
        public string rutaCaminar = "Pawn and Resources/Pawn/{color} Pawn/Pawn_Run.png";

        [Range(1f, 24f)] public float fpsReposo = 7f;
        [Range(1f, 24f)] public float fpsCaminar = 11f;
    }
}
