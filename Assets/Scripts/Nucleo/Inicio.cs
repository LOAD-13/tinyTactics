using UnityEngine;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// La primera escena de la build: no enseña nada, solo abre el menú.
    /// </summary>
    /// <remarks>
    /// Existe para que la build arranque siempre en el menú y, a la vez, abrir la escena de
    /// juego desde Unity siga lanzando una partida directa, como hasta la semana 11. Sin
    /// ella habría que elegir entre las dos cosas.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Inicio")]
    public class Inicio : MonoBehaviour
    {
        [Tooltip("Escena de juego del primer mapa: el fondo del menú.")]
        public string escenaDelMenu = "Juego";

        void Start()
        {
            Navegacion.EscenaDelMenu = escenaDelMenu;
            Navegacion.IrAlMenu();
        }
    }
}
