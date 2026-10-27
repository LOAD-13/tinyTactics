using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Avisa cuando el ratón entra y sale de un botón de la rejilla de comandos.
    ///
    /// Se usa esto en vez de un <c>EventTrigger</c> porque el trigger guarda la reacción
    /// como una lista serializada de llamadas y aquí todo se monta por código: quedaría
    /// una configuración invisible en la escena que nadie sabría dónde tocar.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Aviso de botón")]
    public class AvisoDeBoton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        System.Action<string> _mostrar;
        string _texto;
        System.Func<FichaEmergente.Contenido> _ficha;

        public void Configurar(string texto, System.Action<string> mostrar)
        {
            _texto = texto;
            _mostrar = mostrar;
        }

        /// <summary>
        /// Semana 15: en vez de una línea de texto, una ficha. El contenido se pide al pasar
        /// el ratón, para que «te falta oro» sea verdad en ese momento.
        /// </summary>
        public void Configurar(System.Func<FichaEmergente.Contenido> ficha) => _ficha = ficha;

        public void OnPointerEnter(PointerEventData evento)
        {
            if (_ficha != null && FichaEmergente.Actual != null)
                FichaEmergente.Actual.Mostrar(_ficha(), (RectTransform)transform);
            else
                _mostrar?.Invoke(_texto);
        }

        public void OnPointerExit(PointerEventData evento) => Apagar();

        // Si el botón se apaga con el ratón encima, el aviso se quedaría colgado en
        // pantalla señalando algo que ya no está.
        void OnDisable() => Apagar();

        void Apagar()
        {
            if (_ficha != null && FichaEmergente.Actual != null) FichaEmergente.Actual.Ocultar();
            _mostrar?.Invoke(null);
        }
    }
}
