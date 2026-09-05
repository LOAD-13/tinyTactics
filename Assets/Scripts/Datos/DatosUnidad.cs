using UnityEngine;

namespace TinyTactics.Datos
{
    /// <summary>Tipos de unidad, con los nombres del pack entre paréntesis en el GDD.</summary>
    public enum TipoUnidad { Pawn, Guerrero, Lancero, Arquero, Monje }

    /// <summary>
    /// En qué está una unidad. La prioridad NO sale de este orden: la resuelve
    /// <c>MaquinaDeEstados.Decidir()</c> con una cadena explícita, y morir gana a todo.
    ///
    /// <b>Trabajando va al final a propósito.</b> Los valores del enum se serializan en la
    /// escena dentro de las tiras de animación; meterlo en medio habría corrido
    /// <c>Atacando</c> y <c>Muriendo</c> un puesto y las unidades ya guardadas atacarían
    /// con la animación de morir. Añadir al final es gratis, insertar no.
    /// </summary>
    public enum EstadoUnidad { Reposo, Moviendo, Atacando, Muriendo, Trabajando }

    /// <summary>
    /// Los tres recursos del juego, más el caso normal de no llevar nada.
    ///
    /// Sirve para dos cosas a la vez: qué da un nodo del mapa y qué lleva un pawn encima.
    /// Es el mismo dato mirado desde los dos lados, y unificarlo evita tener que traducir
    /// entre dos enums cada vez que un pawn deposita.
    /// </summary>
    public enum TipoRecurso { Ninguno, Oro, Madera, Carne }

    /// <summary>
    /// Hacia dónde apunta un ataque.
    ///
    /// Solo cinco, no ocho: el pack dibuja el lado derecho y las otras tres salen de
    /// voltear el sprite. <c>Ninguna</c> es lo normal — casi todas las unidades atacan
    /// igual mires a donde mires.
    /// </summary>
    public enum DireccionAtaque { Ninguna, Arriba, ArribaDerecha, Derecha, AbajoDerecha, Abajo }

    /// <summary>
    /// Una animación de la unidad: qué tira usa, a qué ritmo y si se repite.
    ///
    /// La ruta lleva <c>{color}</c>, que se sustituye por el nombre del bando en el pack
    /// (Blue, Red, Yellow, Purple, Black). Una sola entrada sirve para las cinco facciones.
    /// </summary>
    [System.Serializable]
    public class ClipUnidad
    {
        public EstadoUnidad estado = EstadoUnidad.Reposo;

        [Tooltip("Ruta dentro de Assets/Tiny Swords/. {color} se sustituye por la facción.")]
        public string ruta = "";

        [Range(1f, 30f)] public float fps = 8f;

        [Tooltip("Solo para ataques direccionales. El lancero es la única unidad que los tiene.")]
        public DireccionAtaque direccion = DireccionAtaque.Ninguna;

        [Tooltip("Recurso con el que trabaja o que lleva encima. Solo lo usa el pawn.")]
        public TipoRecurso recurso = TipoRecurso.Ninguno;

        [Tooltip("Reposo y caminar se repiten; atacar y morir se reproducen una sola vez.")]
        public bool enBucle = true;
    }

    /// <summary>
    /// Estadísticas y animaciones de una unidad.
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

        [Tooltip("Positivo hiere; negativo cura. El monje es el único que cura.")]
        public int dano = 5;

        [Tooltip("Distancia máxima a la que puede golpear o curar, en tiles.")]
        public float alcance = 0.5f;

        [Tooltip("No recibe daño. Solo lo usa el muñeco de pruebas.")]
        public bool invulnerable;

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
        [Tooltip("Una entrada por estado. Los estados sin entrada caen a la de reposo.")]
        public ClipUnidad[] clips = new ClipUnidad[0];

        /// <summary>
        /// Clip de un estado, o null si no está declarado.
        ///
        /// Que falte no es un error: no todas las unidades atacan igual, y ninguna del
        /// pack tiene animación de muerte. Quien pregunte decide qué hacer con el hueco.
        /// </summary>
        public ClipUnidad ClipDe(EstadoUnidad estado) => ClipDe(estado, TipoRecurso.Ninguno);

        /// <summary>
        /// Clip de un estado teniendo en cuenta lo que la unidad lleva o trabaja.
        ///
        /// La tabla se indexa por <b>estado × recurso</b> desde que existe la economía: un
        /// pawn cargado de madera no se dibuja igual andando que uno con las manos vacías,
        /// y el pack trae las doce combinaciones por color. Si no hay entrada para el
        /// recurso pedido se cae a la versión sin carga, que es la de siempre.
        /// </summary>
        public ClipUnidad ClipDe(EstadoUnidad estado, TipoRecurso recurso)
        {
            if (clips == null) return null;

            for (int i = 0; i < clips.Length; i++)
                if (clips[i] != null && clips[i].estado == estado && clips[i].recurso == recurso)
                    return clips[i];

            if (recurso == TipoRecurso.Ninguno) return null;

            for (int i = 0; i < clips.Length; i++)
                if (clips[i] != null && clips[i].estado == estado &&
                    clips[i].recurso == TipoRecurso.Ninguno)
                    return clips[i];

            return null;
        }
    }
}
