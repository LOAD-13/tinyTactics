using UnityEngine;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Qué color lleva cada bando en esta partida.
    /// </summary>
    /// <remarks>
    /// <b>Bando y color dejaron de ser lo mismo en la semana 12.</b> Hasta entonces el bando 0
    /// era siempre azul, el 1 rojo y así; todo lo que dibuja algo de un bando —plantillas de
    /// unidad y de edificio, retratos, listones, el color del minimapa— buscaba por número de
    /// bando, y funcionaba porque el número coincidía con el color.
    ///
    /// Ahora esas búsquedas preguntan aquí primero. El generador deja preparadas plantillas
    /// en los cinco colores del pack, y cada partida decide cuál lleva cada base. Es el único
    /// sitio que hubo que cambiar: el resto del juego sigue hablando de bandos.
    /// </remarks>
    public static class Bandos
    {
        public const int Colores = 5;

        public static readonly string[] NombresDeColor = { "Azul", "Rojo", "Amarillo", "Morado", "Negro" };

        /// <summary>El color de un bando. Sin configuración, el de siempre: su mismo número.</summary>
        public static int ColorDe(int faccion)
        {
            var c = ArranqueDePartida.Configuracion;
            if (c != null && c.colores != null && faccion >= 0 && faccion < c.colores.Length)
                return Mathf.Clamp(c.colores[faccion], 0, Colores - 1);

            return Mathf.Clamp(faccion, 0, Colores - 1);
        }

        public static string NombreDeColor(int color) =>
            NombresDeColor[Mathf.Clamp(color, 0, Colores - 1)];
    }
}
