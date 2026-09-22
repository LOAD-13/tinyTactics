using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// El único dueño de «esto no se dibuja porque está en niebla».
    /// </summary>
    /// <remarks>
    /// <b>Usa <c>forceRenderingOff</c> y no <c>enabled</c>, y esa es toda la clase.</b>
    /// <c>enabled</c> en los sprites de una unidad ya tiene dueños: los corchetes de
    /// selección se encienden al seleccionar y la barra de vida al recibir daño. Un segundo
    /// interruptor sobre el mismo campo acabaría —tarde o temprano, y en una demo— dejando
    /// corchetes encendidos en unidades que nadie ha seleccionado, o barras de vida flotando
    /// sobre un enemigo invisible.
    ///
    /// <c>forceRenderingOff</c> existe justo para esto: es un veto independiente, pensado
    /// para sistemas de visibilidad y culling, y nadie más en el proyecto lo toca. Los dos
    /// interruptores se pueden mover sin coordinarse porque no son el mismo interruptor.
    ///
    /// Es la misma regla del ADR-11 aplicada a otro campo: <b>un campo, un dueño</b>. La
    /// máquina de estados manda en el color; esto manda en si se dibuja.
    /// </remarks>
    [AddComponentMenu("")]
    public class OcultarEnNiebla : MonoBehaviour
    {
        Renderer[] _dibujos;
        int _hijosAlEscanear = -1;
        bool _tapado;

        /// <summary>
        /// Tapa o destapa un objeto entero, creando el componente solo si hace falta.
        /// </summary>
        /// <remarks>
        /// Si el objeto no lleva el componente y encima no hay que taparlo, no se crea nada:
        /// el caso normal —tus propias unidades, que siempre se ven— no paga ni un componente
        /// de más. Los enemigos lo ganan la primera vez que entran en niebla y ya lo
        /// conservan, que es cuando empieza a servir de algo.
        /// </remarks>
        public static void Aplicar(GameObject objeto, bool tapar)
        {
            if (objeto == null) return;

            if (!objeto.TryGetComponent(out OcultarEnNiebla oculto))
            {
                if (!tapar) return;
                oculto = objeto.AddComponent<OcultarEnNiebla>();
            }

            oculto.Tapar(tapar);
        }

        void Tapar(bool valor)
        {
            // Se relee la lista si le han salido hijos desde el último escaneo. Pasa de
            // verdad: la barra de vida de un edificio se crea la primera vez que le pegan, y
            // sin este control se quedaría fuera del veto justo en la barra que delata al
            // edificio que se está atacando en niebla.
            if (_dibujos == null || _hijosAlEscanear != transform.childCount) Escanear();

            if (_tapado == valor) return;
            _tapado = valor;

            for (int i = 0; i < _dibujos.Length; i++)
                if (_dibujos[i] != null) _dibujos[i].forceRenderingOff = valor;
        }

        void Escanear()
        {
            // Incluidos los apagados: un sprite apagado hoy puede encenderlo otro sistema
            // mañana, y el veto tiene que estar ya puesto cuando eso pase.
            _dibujos = GetComponentsInChildren<Renderer>(true);
            _hijosAlEscanear = transform.childCount;

            if (!_tapado) return;

            for (int i = 0; i < _dibujos.Length; i++)
                if (_dibujos[i] != null) _dibujos[i].forceRenderingOff = true;
        }

        void OnDestroy()
        {
            if (!_tapado || _dibujos == null) return;

            for (int i = 0; i < _dibujos.Length; i++)
                if (_dibujos[i] != null) _dibujos[i].forceRenderingOff = false;
        }
    }
}
