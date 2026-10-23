using UnityEngine;

namespace TinyTactics.Audio
{
    /// <summary>
    /// La música y el sonido ambiente, leídos de sus carpetas por prefijo.
    /// </summary>
    /// <remarks>
    /// Igual que los efectos (semana 11): cambiar una pista es sustituir el archivo y
    /// regenerar, sin tocar código. Las pistas se releen siempre; los volúmenes se ajustan a
    /// oído en el inspector y regenerar no los pisa.
    /// </remarks>
    [CreateAssetMenu(fileName = "CatalogoDeMusica", menuName = "Tiny Tactics/Catalogo de musica")]
    public class CatalogoDeMusica : ScriptableObject
    {
        public const string Menu = "musica_menu_", Partida = "musica_partida_", Combate = "musica_combate_",
                            Victoria = "musica_victoria_", Derrota = "musica_derrota_",
                            Dia = "ambiente_dia_", Noche = "ambiente_noche_", Viento = "ambiente_viento_";

        [Header("Música")]
        public AudioClip[] menu = new AudioClip[0];

        [Tooltip("Varias se alternan: al acabar una empieza la siguiente.")]
        public AudioClip[] partida = new AudioClip[0];

        public AudioClip[] combate = new AudioClip[0];

        [Tooltip("Si no hay, suena solo el jingle de efectos del cartel final.")]
        public AudioClip[] victoria = new AudioClip[0];
        public AudioClip[] derrota = new AudioClip[0];

        [Tooltip("La del menú suena a volumen normal: es la única que se escucha de frente.")]
        [Range(0f, 1f)] public float volumenMenu = 0.7f;

        [Tooltip("En partida la música es de fondo, muy baja: acompaña sin tapar los efectos " +
                 "(decidido con Joaquín en la semana 14).")]
        [Range(0f, 1f)] public float volumenPartida = 0.3f;

        [Range(0f, 1f)] public float volumenCombate = 0.3f;

        [Range(0f, 1f)] public float volumenFinal = 0.5f;

        [Tooltip("Segundos del fundido entre pistas.")]
        [Range(0.2f, 8f)] public float fundido = 4f;

        [Header("Ambiente")]
        public AudioClip[] dia = new AudioClip[0];
        public AudioClip[] noche = new AudioClip[0];
        public AudioClip[] viento = new AudioClip[0];

        [Tooltip("El ambiente es ambiente: apenas se oye.")]
        [Range(0f, 1f)] public float volumenDia = 0.25f;
        [Range(0f, 1f)] public float volumenNoche = 0.25f;
        [Range(0f, 1f)] public float volumenViento = 0.15f;

        public static AudioClip Primera(AudioClip[] lista) =>
            lista != null && lista.Length > 0 ? lista[0] : null;
    }
}
