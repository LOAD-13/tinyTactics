using UnityEngine;
using UnityEngine.Tilemaps;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Los tiles del terreno en los cinco colores del pack, listos para pintar en partida.
    /// </summary>
    /// <remarks>
    /// Hasta la semana 12 el terreno solo se pintaba en el editor de Unity, que carga los
    /// tiles con <c>AssetDatabase</c>. En partida esa herramienta no existe, así que el
    /// generador deja aquí los tiles ya creados, y el editor de mapas y el constructor de
    /// partida los usan sin saber de dónde salieron.
    ///
    /// Las piezas se guardan por su índice en la hoja del pack —el mismo que calculan las
    /// funciones de autotiling de <see cref="GeneradorTerreno"/>— para no tener una segunda
    /// tabla de equivalencias que mantener.
    /// </remarks>
    [CreateAssetMenu(fileName = "PaletaDeTerreno", menuName = "Tiny Tactics/Paleta de terreno")]
    public class PaletaDeTerreno : ScriptableObject
    {
        [System.Serializable]
        public class JuegoDeColor
        {
            public string nombre;
            [Tooltip("Una muestra para el icono del pincel.")]
            public Sprite muestra;
            public TileBase[] piezas = new TileBase[48];
        }

        public TileBase agua;
        public TileBase espuma;

        [Tooltip("Los cinco tilesets del pack, en orden: Tilemap_color1 … 5.")]
        public JuegoDeColor[] colores = new JuegoDeColor[5];

        public int Colores => colores != null ? colores.Length : 0;

        /// <summary>La pieza de índice <paramref name="indice"/> en el color dado (1..5).</summary>
        public TileBase Pieza(int color, int indice)
        {
            if (colores == null || colores.Length == 0) return null;

            var juego = colores[Mathf.Clamp(color - 1, 0, colores.Length - 1)];
            if (juego == null || juego.piezas == null || indice < 0 || indice >= juego.piezas.Length) return null;
            return juego.piezas[indice];
        }
    }
}
