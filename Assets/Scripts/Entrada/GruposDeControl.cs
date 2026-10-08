using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Edificios;
using TinyTactics.Unidades;

namespace TinyTactics.Entrada
{
    /// <summary>
    /// Grupos de control con las teclas numéricas, y la barra para volver a casa.
    /// </summary>
    /// <remarks>
    /// <b>Es comodidad, y la comodidad importa más contra un rival que contra nadie.</b> Con
    /// el mapa vacío daba igual reencontrar a los arqueros a base de arrastrar el ratón; con
    /// un bot atacando por un lado mientras tus pawns talan por el otro, tener el ejército a
    /// una tecla es la diferencia entre reaccionar y mirar.
    ///
    /// <b>Los grupos guardan unidades, no índices.</b> Guardar «las que estaban seleccionadas
    /// en tal momento» por posición en una lista se rompe en cuanto una muere. Guardando las
    /// referencias y limpiando las caídas al leer, un grupo del que han matado a la mitad
    /// sigue seleccionando a la otra mitad, que es lo que el jugador espera.
    ///
    /// <b>Ctrl y Shift hacen cosas distintas, y las dos valen.</b> En la mayoría de RTS ctrl
    /// crea el grupo y shift añade al que ya hay. Están las dos: si el grupo está vacío,
    /// shift también crea, para que ninguna de las dos teclas se sienta mal.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Grupos de control")]
    public class GruposDeControl : MonoBehaviour
    {
        [Tooltip("Segundos para que dos pulsaciones cuenten como doble y lleven la camara.")]
        [Range(0.1f, 1f)] public float ventanaDeDoble = 0.35f;

        /// <summary>Diez grupos: las teclas 1 a 9 y el 0.</summary>
        const int Cuantos = 10;

        readonly List<Unidad>[] _grupos = new List<Unidad>[Cuantos];

        int _ultimoPulsado = -1;
        float _ultimoInstante = -10f;

        void Awake()
        {
            for (int i = 0; i < Cuantos; i++) _grupos[i] = new List<Unidad>();
        }

        void Update()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return;

            // El panel de partida libre y sus botones no deben comerse las teclas, pero
            // tampoco al reves: si el panel esta abierto y el raton encima, sus atajos
            // mandan. Aqui basta con no hacer nada raro: las teclas numericas no las usa.
            if (teclado.spaceKey.wasPressedThisFrame) IrACasa();

            // Alt tambien crea, y no es un capricho: DENTRO DEL EDITOR de Unity, Ctrl+1 y
            // Ctrl+2 son «abrir la ventana Scene» y «abrir la ventana Game», y el editor se
            // queda la tecla antes de que llegue al juego. En una build compilada no pasa,
            // pero toda la semana de pruebas se juega dentro del editor. Alt no lo usa nadie.
            bool crea = teclado.leftCtrlKey.isPressed || teclado.rightCtrlKey.isPressed ||
                        teclado.leftAltKey.isPressed || teclado.rightAltKey.isPressed;

            bool anade = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;

            for (int i = 0; i < Cuantos; i++)
            {
                if (!Pulsada(teclado, i)) continue;

                if (crea) Guardar(i, false);
                else if (anade) Guardar(i, true);
                else Recuperar(i);

                return;
            }
        }

        static bool Pulsada(Keyboard teclado, int indice)
        {
            // El 0 va al final del teclado y de esta lista: la tecla 1 es el grupo 0.
            var tecla = indice switch
            {
                0 => teclado.digit1Key,
                1 => teclado.digit2Key,
                2 => teclado.digit3Key,
                3 => teclado.digit4Key,
                4 => teclado.digit5Key,
                5 => teclado.digit6Key,
                6 => teclado.digit7Key,
                7 => teclado.digit8Key,
                8 => teclado.digit9Key,
                _ => teclado.digit0Key,
            };

            return tecla.wasPressedThisFrame;
        }

        // -----------------------------------------------------------------
        // Guardar y recuperar
        // -----------------------------------------------------------------

        void Guardar(int indice, bool sumando)
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            var seleccion = selector.Seleccionadas;
            if (seleccion == null || seleccion.Count == 0) return;

            var grupo = _grupos[indice];

            // Shift sobre un grupo vacio crea, en vez de no hacer nada: quien usa shift para
            // crear no tiene por que saber que aqui eso era cosa de ctrl.
            if (!sumando) grupo.Clear();

            for (int i = 0; i < seleccion.Count; i++)
            {
                var u = seleccion[i];
                if (u == null || grupo.Contains(u)) continue;

                grupo.Add(u);
            }
        }

        void Recuperar(int indice)
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            var grupo = _grupos[indice];

            // Las caidas se limpian al leer y no al morir: no hace falta que cada unidad
            // avise a nadie de que se murio, y el coste es recorrer diez referencias.
            for (int i = grupo.Count - 1; i >= 0; i--)
                if (grupo[i] == null || !grupo[i].Viva) grupo.RemoveAt(i);

            if (grupo.Count == 0) return;

            selector.SeleccionarGrupo(grupo);

            bool doble = _ultimoPulsado == indice &&
                         Time.unscaledTime - _ultimoInstante <= ventanaDeDoble;

            _ultimoPulsado = indice;
            _ultimoInstante = Time.unscaledTime;

            if (doble) LlevarCamaraA(Centro(grupo));
        }

        static Vector2 Centro(List<Unidad> grupo)
        {
            Vector2 suma = Vector2.zero;

            for (int i = 0; i < grupo.Count; i++) suma += (Vector2)grupo[i].transform.position;

            return suma / grupo.Count;
        }

        // -----------------------------------------------------------------
        // La barra espaciadora
        // -----------------------------------------------------------------

        /// <summary>
        /// Lleva la cámara al castillo propio.
        /// </summary>
        /// <remarks>
        /// Al castillo y no a «lo último que pasó», que es lo que hace la barra en Starcraft:
        /// aquí todavía no hay avisos de nada, así que una barra que va al último aviso sería
        /// una barra que no hace nada la mayor parte de la partida. Cuando haya avisos, esta
        /// pasará a la H y la barra irá al aviso, que es la convención buena.
        /// </remarks>
        void IrACasa()
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null) return;

            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion != selector.faccionJugador || !e.centroDeEntrega)
                    continue;

                LlevarCamaraA(e.PosicionDePlanta);
                return;
            }
        }

        static void LlevarCamaraA(Vector2 punto)
        {
            var camara = CamaraRTS.Actual;
            if (camara != null) camara.CentrarEn(punto);
        }
    }
}
