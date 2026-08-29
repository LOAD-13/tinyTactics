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

        public void Configurar(string texto, System.Action<string> mostrar)
        {
            _texto = texto;
            _mostrar = mostrar;
        }

        public void OnPointerEnter(PointerEventData evento) => _mostrar?.Invoke(_texto);

        public void OnPointerExit(PointerEventData evento) => _mostrar?.Invoke(null);

        // Si el botón se apaga con el ratón encima, el aviso se quedaría colgado en
        // pantalla señalando algo que ya no está.
        void OnDisable() => _mostrar?.Invoke(null);
    }
}
