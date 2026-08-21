using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Todos los sprites de interfaz del pack, reunidos en un solo asset.
    ///
    /// Es el mismo criterio del ADR-07 que ya se usa para el balance: los datos viven en un
    /// <see cref="ScriptableObject"/>, no en el código. Aquí resuelve además un problema
    /// concreto — la escena se regenera entera desde el editor, y sin un asset central cada
    /// script tendría que buscar sus texturas por ruta en tiempo de ejecución.
    ///
    /// Lo rellena <c>ConstructorDeInterfaz</c>. No hace falta tocarlo a mano.
    /// </summary>
    [CreateAssetMenu(fileName = "TemaInterfaz", menuName = "Tiny Tactics/Tema de interfaz")]
    public class TemaInterfaz : ScriptableObject
    {
        [Header("Punteros")]
        public Texture2D cursorNormal;
        public Texture2D cursorMano;
        public Texture2D cursorProhibido;

        [Tooltip("Punto activo de cada puntero, en píxeles desde la esquina superior izquierda.")]
        public Vector2 puntoNormal = new Vector2(23f, 18f);
        public Vector2 puntoMano = new Vector2(27f, 18f);
        public Vector2 puntoProhibido = new Vector2(32f, 31f);

        [Header("Mundo")]
        [Tooltip("Corchetes de selección que rodean a la unidad elegida.")]
        public Sprite marcadorSeleccion;

        public Sprite barraChicaMarco;
        public Sprite barraChicaRelleno;

        [Header("Panel")]
        public Sprite panelFondo;

        [Tooltip("Caja grande del pack (BigBlueButton), reteñida al color de cada facción.")]
        public Sprite[] cajas = new Sprite[5];

        [Tooltip("Caja pequeña. Envuelve la barra de vida, los stats y la rejilla de grupo.")]
        public Sprite[] cajasChicas = new Sprite[5];

        [Tooltip("Listón ancho que asoma por detrás del panel. Uno por facción.")]
        public Sprite[] listonesPanel = new Sprite[5];

        [Tooltip("Listón estrecho para el nombre de la unidad. Uno por facción.")]
        public Sprite[] listonesNombre = new Sprite[5];

        public Sprite barraGrandeMarco;
        public Sprite barraGrandeRelleno;
        public Sprite iconoAtaque;
        public Sprite iconoOro;
        public Sprite iconoVelocidad;

        [Header("Retratos")]
        [Tooltip("Las 25 caras del pack: 5 tipos de unidad × 5 colores de facción.")]
        public Sprite[] retratos = new Sprite[25];

        /// <summary>Color de cada bando, para teñir marcadores y barras.</summary>
        public Color[] coloresFaccion =
        {
            new Color(0.36f, 0.63f, 0.92f), // Blue
            new Color(0.90f, 0.33f, 0.33f), // Red
            new Color(0.94f, 0.82f, 0.31f), // Yellow
            new Color(0.68f, 0.45f, 0.88f), // Purple
            new Color(0.42f, 0.45f, 0.52f), // Black
        };

        /// <summary>
        /// Columna de la hoja de retratos que le corresponde a cada tipo.
        ///
        /// El pack no los ordena como nuestro GDD: la primera columna es el yelmo con
        /// penacho y escudo, la última la cara sin casco. Este mapeo es el único sitio
        /// donde esa diferencia existe.
        /// </summary>
        static int ColumnaDe(TipoUnidad tipo)
        {
            switch (tipo)
            {
                case TipoUnidad.Guerrero: return 0;
                case TipoUnidad.Monje: return 1;
                case TipoUnidad.Lancero: return 2;
                case TipoUnidad.Arquero: return 3;
                default: return 4; // Pawn
            }
        }

        public Sprite RetratoDe(TipoUnidad tipo, int faccion)
        {
            if (retratos == null || retratos.Length < 25) return null;

            int fila = Mathf.Clamp(faccion, 0, 4);
            return retratos[fila * 5 + ColumnaDe(tipo)];
        }

        /// <summary>
        /// Listón del bando. El pack ya los dibuja en los cinco colores y en el mismo orden
        /// que las facciones, así que no hay que teñir nada: teñir un sprite verdeazulado
        /// para sacar un rojo da un marrón sucio, nunca el rojo del pack.
        /// </summary>
        public Sprite ListonPanelDe(int faccion) => Elegir(listonesPanel, faccion);

        public Sprite ListonNombreDe(int faccion) => Elegir(listonesNombre, faccion);

        public Sprite CajaDe(int faccion) => Elegir(cajas, faccion);

        public Sprite CajaChicaDe(int faccion) => Elegir(cajasChicas, faccion);

        static Sprite Elegir(Sprite[] lista, int indice)
        {
            if (lista == null || lista.Length == 0) return null;
            return lista[Mathf.Clamp(indice, 0, lista.Length - 1)];
        }

        public Color ColorDe(int faccion)
        {
            if (coloresFaccion == null || coloresFaccion.Length == 0) return Color.white;
            return coloresFaccion[Mathf.Clamp(faccion, 0, coloresFaccion.Length - 1)];
        }
    }
}
