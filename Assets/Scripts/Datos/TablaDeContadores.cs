using UnityEngine;

namespace TinyTactics.Datos
{
    /// <summary>
    /// Cuánto multiplica cada tipo de ataque contra cada tipo de armadura.
    /// </summary>
    /// <remarks>
    /// Es el «piedra, papel, tijera» del género, y sin él las cinco unidades solo se
    /// distinguen por sus números: la que más daño hace gana siempre y las demás sobran.
    /// Con la tabla, cada una tiene contra quién conviene mandarla.
    ///
    /// Vive en un <see cref="ScriptableObject"/> por el ADR-07: la semana 16 es de balance
    /// puro, y una tabla escrita en el código obliga a recompilar en cada ajuste.
    ///
    /// <b>Los multiplicadores son suaves a propósito.</b> Ninguno baja de 0,75, así que
    /// ninguna unidad queda inútil contra nada: lo peor que te puede pasar es un 25 % menos
    /// de daño, no la mitad. Un contador demasiado duro convierte el combate en un acertijo
    /// de composición en vez de en una decisión táctica, y encima invalidaría de golpe el
    /// balance de las cinco unidades, que lleva ajustado desde la semana 04 sin tabla.
    /// </remarks>
    [CreateAssetMenu(fileName = "Contadores", menuName = "Tiny Tactics/Tabla de contadores")]
    public class TablaDeContadores : ScriptableObject
    {
        /// <summary>
        /// Una fila: qué le hace un tipo de ataque a cada armadura.
        /// </summary>
        /// <remarks>
        /// Es una clase serializable y no una matriz de dos dimensiones porque Unity no
        /// serializa <c>float[,]</c>. Además así la tabla se lee en el inspector con los
        /// nombres puestos, que es la mitad de para qué existe un asset de balance.
        /// </remarks>
        [System.Serializable]
        public class Fila
        {
            public TipoAtaque ataque;

            [Range(0.25f, 3f)] public float contraLigera = 1f;
            [Range(0.25f, 3f)] public float contraPesada = 1f;
            [Range(0.25f, 3f)] public float contraAsta = 1f;
            [Range(0.25f, 3f)] public float contraFortificada = 1f;

            public float Contra(TipoArmadura armadura)
            {
                switch (armadura)
                {
                    case TipoArmadura.Pesada: return contraPesada;
                    case TipoArmadura.Asta: return contraAsta;
                    case TipoArmadura.Fortificada: return contraFortificada;
                    default: return contraLigera;
                }
            }
        }

        [Tooltip("Una fila por tipo de ataque. Lo que falte se resuelve como x1.")]
        public Fila[] filas = new Fila[0];

        /// <summary>
        /// Multiplicador de daño. Devuelve 1 si la combinación no está en la tabla.
        /// </summary>
        /// <remarks>
        /// Caer a 1 en vez de avisar es deliberado: una tabla incompleta tiene que dejar el
        /// juego jugable, no romperlo. Un tipo de unidad nuevo sin fila entra al combate
        /// pegando lo que dice su ficha, que es exactamente el comportamiento de antes de
        /// que existiera la tabla.
        /// </remarks>
        public float Multiplicador(TipoAtaque ataque, TipoArmadura armadura)
        {
            if (filas == null) return 1f;

            for (int i = 0; i < filas.Length; i++)
                if (filas[i] != null && filas[i].ataque == ataque)
                    return filas[i].Contra(armadura);

            return 1f;
        }

        /// <summary>
        /// La tabla en uso. La asigna el generador de la escena.
        /// </summary>
        /// <remarks>
        /// Estática y no un campo en cada unidad porque es una regla del JUEGO, no un dato
        /// de la unidad: si estuviera en la ficha habría cinco copias que mantener iguales, y
        /// bastaría olvidar una para que un guerrero y un lancero jugaran a juegos distintos.
        /// </remarks>
        public static TablaDeContadores Actual { get; private set; }

        public static void Usar(TablaDeContadores tabla) => Actual = tabla;

        /// <summary>Atajo seguro: sin tabla cargada, todo multiplica por 1.</summary>
        public static float Factor(TipoAtaque ataque, TipoArmadura armadura) =>
            Actual != null ? Actual.Multiplicador(ataque, armadura) : 1f;
    }
}
