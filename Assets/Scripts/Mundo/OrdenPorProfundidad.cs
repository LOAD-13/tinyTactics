using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Reordena el dibujo según la altura del objeto en el mapa: lo que está más abajo se
    /// pinta delante.
    ///
    /// <b>Por qué hacía falta.</b> El orden se fijaba al crear cada objeto y no se volvía a
    /// tocar, lo cual vale para un árbol pero no para algo que anda: un pawn que rodeaba el
    /// castillo por detrás seguía dibujándose delante, y parecía que volaba por encima del
    /// tejado. Todo lo que se mueve necesita recalcularlo.
    ///
    /// Corre en <c>LateUpdate</c> a propósito, después de que el movimiento haya colocado ya
    /// la posición de este fotograma, y solo escribe en el renderizador cuando el número
    /// cambia de verdad: tocar <c>sortingOrder</c> ensucia el lote de dibujado.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Tiny Tactics/Orden por profundidad")]
    public class OrdenPorProfundidad : MonoBehaviour
    {
        /// <summary>
        /// Subdivisiones de orden por tile.
        /// </summary>
        /// <remarks>
        /// Sin escala, dos objetos dentro del mismo tile empatan, y un empate en
        /// <c>sortingOrder</c> deja el orden en manos del motor: dos unidades pegadas
        /// parpadearían intercambiándose. Con diez pasos por tile, la profundidad se
        /// resuelve a nivel de decímetro. Todo lo que se dibuja en el mundo usa esta misma
        /// escala; mezclarla con otra sería tanto como no ordenar.
        /// </remarks>
        public const int PasosPorTile = 10;

        [Tooltip("Alto del mapa en tiles. Invierte el eje para que el sur quede delante.")]
        public int alto = 128;

        [Tooltip("Desempate fino. Las unidades van ligeramente por delante de la decoración.")]
        public int extra;

        SpriteRenderer _sprite;
        int _ultimo = int.MinValue;

        void Awake() => _sprite = GetComponent<SpriteRenderer>();

        void LateUpdate()
        {
            if (_sprite == null) return;

            int orden = Calcular(alto, transform.position.y) + extra;
            if (orden == _ultimo) return;

            _ultimo = orden;
            _sprite.sortingOrder = orden;
        }

        /// <summary>
        /// La fórmula, en un solo sitio. La usan también las cosas quietas, que la resuelven
        /// una vez en el editor en vez de pagarla cada fotograma.
        /// </summary>
        public static int Calcular(int alto, float y) =>
            Mathf.RoundToInt((alto - y) * PasosPorTile);
    }
}
