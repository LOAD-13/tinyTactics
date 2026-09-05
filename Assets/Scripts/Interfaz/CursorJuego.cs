using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Cambia el puntero según lo que haya debajo: flecha por defecto, mano sobre una
    /// unidad propia y prohibido cuando hay selección y el destino es intransitable.
    ///
    /// Es puramente cosmético — no decide nada. La orden la sigue emitiendo
    /// <see cref="SelectorDeUnidades"/>, y este componente solo lee el mismo estado para
    /// anticiparle al jugador si el clic va a servir de algo.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Cursor del juego")]
    public class CursorJuego : MonoBehaviour
    {
        enum Forma { Normal, Mano, Prohibido, Accion }

        [Header("Tema")]
        public TemaInterfaz tema;

        [Header("Detección")]
        [Tooltip("Radio en unidades de mundo para considerar que el puntero está sobre una unidad.")]
        public float radioUnidad = 0.55f;

        [Tooltip("Radio para considerar que el puntero está sobre un recurso.")]
        public float radioNodo = 1.1f;

        [Tooltip("Windows admite punteros de 64 px por hardware. Si se ven recortados, " +
                 "poner ForceSoftware: los dibuja el motor, a costa de un frame de retraso.")]
        public CursorMode modo = CursorMode.Auto;

        Camera _camara;
        Forma _forma = (Forma)(-1);

        void Awake()
        {
            _camara = Camera.main;
        }

        void OnEnable() => Aplicar(Forma.Normal, true);

        void OnDisable() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        void Update()
        {
            if (tema == null || _camara == null) return;

            var raton = Mouse.current;
            if (raton == null) return;

            Aplicar(Decidir(raton.position.ReadValue()), false);
        }

        Forma Decidir(Vector2 pantalla)
        {
            // Sobre el panel no hay terreno que evaluar: puntero normal y punto.
            if (PanelDeUnidad.Actual != null && PanelDeUnidad.Actual.CapturaPuntero(pantalla))
                return Forma.Normal;

            Vector3 punto = _camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 0f));
            punto.z = 0f;

            var selector = SelectorDeUnidades.Actual;
            int faccion = selector != null ? selector.faccionJugador : 0;

            // El índice espacial ya está reconstruido por el selector en este mismo frame.
            if (HayUnidadPropia(punto, faccion)) return Forma.Mano;

            // Un edificio propio también responde al clic.
            if (Edificios.Edificio.EdificioEn(punto, faccion) != null) return Forma.Mano;

            // Un recurso vivo NUNCA sale como prohibido, y esta comprobación va antes que la
            // de la grilla precisamente por eso: un árbol y una veta bloquean su celda, así
            // que la regla de «celda intransitable = prohibido» los marcaba a todos con el
            // aspa. El jugador leía «aquí no puedes hacer nada» sobre lo único con lo que
            // sí puede interactuar.
            var nodo = NodoRecurso.NodoEn(punto, radioNodo);
            if (nodo != null)
                return HayRecolector(selector) ? Forma.Accion : Forma.Normal;

            // Sin nada seleccionado no hay orden que dar, así que tampoco hay nada que prohibir.
            if (selector == null || selector.Seleccionadas.Count == 0) return Forma.Normal;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return Forma.Normal;

            var celda = mundo.Grilla.MundoACelda(punto);
            return mundo.Grilla.Transitable(celda.x, celda.y) ? Forma.Normal : Forma.Prohibido;
        }

        static bool HayRecolector(SelectorDeUnidades selector)
        {
            if (selector == null) return false;

            var seleccion = selector.Seleccionadas;
            for (int i = 0; i < seleccion.Count; i++)
            {
                var u = seleccion[i];
                if (u != null && u.GetComponent<RecolectorPawn>() != null) return true;
            }

            return false;
        }

        static readonly List<Unidad> _cerca = new List<Unidad>(32);

        bool HayUnidadPropia(Vector3 punto, int faccion)
        {
            RegistroDeUnidades.Vecinas(punto, _cerca);

            float radio2 = radioUnidad * radioUnidad;

            for (int i = 0; i < _cerca.Count; i++)
            {
                var u = _cerca[i];
                if (u == null || !u.Viva || u.faccion != faccion) continue;
                if (((Vector2)(u.transform.position - punto)).sqrMagnitude <= radio2) return true;
            }

            return false;
        }

        void Aplicar(Forma forma, bool forzar)
        {
            if (!forzar && forma == _forma) return;
            _forma = forma;

            switch (forma)
            {
                case Forma.Mano:
                    Cursor.SetCursor(tema != null ? tema.cursorMano : null,
                                     tema != null ? tema.puntoMano : Vector2.zero, modo);
                    break;

                case Forma.Accion:
                    Cursor.SetCursor(tema != null ? tema.cursorAccion : null,
                                     tema != null ? tema.puntoAccion : Vector2.zero, modo);
                    break;

                case Forma.Prohibido:
                    Cursor.SetCursor(tema != null ? tema.cursorProhibido : null,
                                     tema != null ? tema.puntoProhibido : Vector2.zero, modo);
                    break;

                default:
                    Cursor.SetCursor(tema != null ? tema.cursorNormal : null,
                                     tema != null ? tema.puntoNormal : Vector2.zero, modo);
                    break;
            }
        }
    }
}
