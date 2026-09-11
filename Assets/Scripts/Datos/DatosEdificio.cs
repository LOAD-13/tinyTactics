using UnityEngine;

namespace TinyTactics.Datos
{
    /// <summary>Qué edificio es. La torre llega con el combate, en la épica E06.</summary>
    public enum TipoEdificio { Castillo, Casa, Cuartel, CampoDeTiro, Monasterio, Torre }

    /// <summary>
    /// Todo lo que define a un edificio: lo que cuesta, lo que ocupa, lo que aporta y lo que
    /// sabe fabricar.
    ///
    /// Igual que las estadísticas de unidad (ADR-07), vive en un <see cref="ScriptableObject"/>
    /// y no en el código. Aquí la ventaja se nota más todavía: <b>añadir un edificio nuevo al
    /// juego es crear un asset</b>. El panel lo muestra solo, la silueta usa su huella y el
    /// coste se cobra sin que nadie escriba un caso especial.
    /// </summary>
    [CreateAssetMenu(fileName = "Edificio", menuName = "Tiny Tactics/Datos de edificio")]
    public class DatosEdificio : ScriptableObject
    {
        [Header("Identidad")]
        public TipoEdificio tipo = TipoEdificio.Casa;
        public string nombreVisible = "Casa";

        [Tooltip("Carpeta dentro del pack. {color} se sustituye por el bando.")]
        public string carpeta = "Assets/Tiny Swords/Buildings/{color} Buildings";

        /// <summary>
        /// Un dibujo alternativo del mismo edificio.
        /// </summary>
        /// <remarks>
        /// El pack trae tres casas —<c>House1</c>, <c>House2</c> y <c>House3</c>— que hacen
        /// exactamente lo mismo. No son tres edificios: son tres fachadas. Meterlas como tres
        /// fichas se habría comido tres de las cuatro ranuras de la rejilla y habría obligado
        /// al jugador a decidir entre casas idénticas creyendo que se diferencian en algo.
        ///
        /// Cada variante lleva su propia huella <b>medida</b>. La casa más alta y la más baja
        /// se llevan un tercio de tile, y compartir una sola medida habría dejado a una de las
        /// tres flotando sobre su propia sombra.
        /// </remarks>
        [System.Serializable]
        public class Variante
        {
            [Tooltip("Nombre del PNG dentro de la carpeta del bando, sin extensión.")]
            public string archivo = "House1";

            [Tooltip("Tamaño del dibujo en tiles. Lo MIDE el generador; no se toca a mano.")]
            public Vector2 huella;

            public Vector2 huellaCentro;
        }

        [Tooltip("Fachadas del mismo edificio. La rueda del ratón pasa de una a otra al " +
                 "colocar. Una sola entrada = edificio sin variantes.")]
        public Variante[] variantes = new Variante[0];

        [Header("Coste")]
        [Min(0)] public int oro;
        [Min(0)] public int madera = 60;

        [Header("Huella")]
        [Tooltip("Celdas que ocupa en el SUELO. No es lo mismo que la huella del dibujo: el " +
                 "monasterio mide 4,14 tiles de alto y su planta no llega a tres, porque lo " +
                 "de arriba es la aguja. Pedirle cuatro tiles libres lo haría imposible de " +
                 "colocar donde cabe de sobra.")]
        public Vector2Int planta = new Vector2Int(2, 2);

        [Header("Obra")]
        [Tooltip("Martillazos que cuesta levantarlo. Se cuentan golpes y no segundos (ADR-13): " +
                 "así dos pawns tardan la mitad sin ninguna cuenta especial, y parar a medias " +
                 "no regala progreso.")]
        [Min(1)] public int golpesDeObra = 24;

        [Header("Función")]
        [Tooltip("Los pawns cargados vienen aquí a soltar. Solo el castillo, de momento.")]
        public bool centroDeEntrega;

        [Tooltip("Población que aporta al terminarse. Solo el castillo y las casas.")]
        [Min(0)] public int poblacionQueAporta;

        [Tooltip("¿Lo puede levantar un pawn? El castillo no: ya está puesto al empezar.")]
        public bool construible = true;

        [Tooltip("Qué unidades sabe entrenar. Vacío = no produce nada.")]
        public DatosUnidad[] fabrica = new DatosUnidad[0];

        /// <summary>Cuántas fachadas tiene. Nunca menos de una.</summary>
        public int Fachadas => variantes != null && variantes.Length > 0 ? variantes.Length : 1;

        /// <summary>Deja un índice de variante dentro de rango, dé lo que dé la rueda.</summary>
        public int Ajustar(int variante)
        {
            int total = Fachadas;
            return ((variante % total) + total) % total;
        }

        Variante Cual(int variante) =>
            variantes != null && variantes.Length > 0 ? variantes[Ajustar(variante)] : null;

        /// <summary>Ruta del sprite, ya resuelta al color de un bando y a la fachada.</summary>
        public string RutaDe(string color, int variante = 0)
        {
            var v = Cual(variante);
            string archivo = v != null ? v.archivo : nombreVisible;

            return $"{carpeta.Replace("{color}", color)}/{archivo}.png";
        }

        /// <summary>Tamaño del dibujo de una fachada, en tiles.</summary>
        public Vector2 HuellaDe(int variante)
        {
            var v = Cual(variante);
            return v != null ? v.huella : Vector2.one;
        }

        public Vector2 HuellaCentroDe(int variante)
        {
            var v = Cual(variante);
            return v != null ? v.huellaCentro : Vector2.zero;
        }

        /// <summary>
        /// Del origen del objeto al centro de las celdas que pisa.
        /// </summary>
        /// <remarks>
        /// El dibujo y el suelo no comparten centro, y confundirlos es lo que hace que una
        /// silueta se vea colocada un tile por encima de donde de verdad va a bloquear. La
        /// planta se apoya en el <b>borde inferior del dibujo</b>: ahí es donde el edificio
        /// toca el suelo, y todo lo que quede por encima es alzado, no terreno ocupado.
        ///
        /// Sale calculado y no guardado en otro campo a propósito. Un tercer número que
        /// mantener coherente con la huella y con la planta se desincroniza el día que
        /// alguien ajuste una de las dos.
        /// </remarks>
        public Vector2 DesplazamientoBase(int variante)
        {
            Vector2 dibujo = HuellaDe(variante);
            Vector2 centro = HuellaCentroDe(variante);

            return new Vector2(centro.x, centro.y - dibujo.y * 0.5f + planta.y * 0.5f);
        }

        /// <summary>Celdas que ocupa si su planta se centra en una celda dada.</summary>
        public RectInt CeldasDesde(Vector2Int celda)
        {
            int ancho = Mathf.Max(1, planta.x);
            int alto = Mathf.Max(1, planta.y);

            // Con ancho par no hay celda central, así que la planta se recuesta hacia la
            // izquierda y abajo. Es una convención cualquiera; lo que importa es que el
            // colocador y el bloqueo de la grilla usen la MISMA, o la silueta enseñaría un
            // sitio y se construiría en otro.
            return new RectInt(celda.x - (ancho - 1) / 2, celda.y - (alto - 1) / 2, ancho, alto);
        }

        /// <summary>True si el edificio puede sacar unidades.</summary>
        public bool Produce => fabrica != null && fabrica.Length > 0;

        /// <summary>Texto corto de coste para los avisos del panel: «60 madera».</summary>
        public string CosteVisible()
        {
            if (oro > 0 && madera > 0) return $"{oro} oro · {madera} madera";
            if (oro > 0) return $"{oro} oro";
            if (madera > 0) return $"{madera} madera";

            return "sin coste";
        }
    }
}
