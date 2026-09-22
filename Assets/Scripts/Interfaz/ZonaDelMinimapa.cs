using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// La parte del minimapa que recibe el ratón.
    /// </summary>
    /// <remarks>
    /// Va en un componente aparte y no en <see cref="Minimapa"/> porque las interfaces de
    /// puntero de uGUI solo llegan al objeto que tiene el <c>Graphic</c> debajo del cursor, y
    /// el minimapa vive en la raíz del lienzo junto al HUD y al panel de unidad. Colgar las
    /// interfaces de la raíz haría que un clic en cualquier parte de la interfaz —un botón de
    /// entrenar, una postura— acabara moviendo la cámara.
    ///
    /// Solo traduce coordenadas. La decisión de qué hacer con el punto es del minimapa.
    /// </remarks>
    [AddComponentMenu("")]
    public class ZonaDelMinimapa : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Minimapa minimapa;

        RectTransform _sitio;

        void Awake() => _sitio = (RectTransform)transform;

        public void OnPointerDown(PointerEventData datos) => Llevar(datos);

        public void OnDrag(PointerEventData datos) => Llevar(datos);

        void Llevar(PointerEventData datos)
        {
            if (minimapa == null || _sitio == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _sitio, datos.position, datos.pressEventCamera, out var local))
                return;

            // El punto llega relativo al PIVOTE del rectángulo, que no tiene por qué estar en
            // una esquina. Pasar por el rect propio en vez de dividir por el tamaño es lo que
            // hace que esto siga funcionando si alguien mueve el minimapa de sitio o cambia
            // su anclaje.
            var caja = _sitio.rect;
            float x = Mathf.InverseLerp(caja.xMin, caja.xMax, local.x);
            float y = Mathf.InverseLerp(caja.yMin, caja.yMax, local.y);

            minimapa.LlevarCamaraA(new Vector2(x, y));
        }
    }
}
