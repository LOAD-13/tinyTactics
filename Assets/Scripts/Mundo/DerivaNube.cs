using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Desplaza una nube por el mapa y la reaparece por el lado contrario.
    ///
    /// Las nubes del pack son sprites únicos, sin tira de frames: no se animan
    /// intercambiando sprites como el resto, se animan <b>moviéndose</b>.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Tiny Tactics/Deriva de nube")]
    public class DerivaNube : MonoBehaviour
    {
        [Tooltip("Unidades por segundo. Muy lento a propósito: es ambiente, no debe distraer.")]
        public float velocidad = 0.6f;

        [Tooltip("Dirección del viento. Igual para todas las nubes del mapa.")]
        public Vector2 direccion = Vector2.right;

        [Tooltip("Límites del mapa. Al salir por un lado, reaparece por el opuesto.")]
        public Vector2 limiteMinimo = Vector2.zero;
        public Vector2 limiteMaximo = new Vector2(192f, 192f);

        [Tooltip("Holgura fuera del mapa antes de reaparecer, para que no aparezca de golpe.")]
        public float margen = 12f;

        void Update()
        {
            transform.position += (Vector3)(direccion.normalized * velocidad * Time.deltaTime);

            Vector3 p = transform.position;

            if (p.x > limiteMaximo.x + margen) p.x = limiteMinimo.x - margen;
            else if (p.x < limiteMinimo.x - margen) p.x = limiteMaximo.x + margen;

            if (p.y > limiteMaximo.y + margen) p.y = limiteMinimo.y - margen;
            else if (p.y < limiteMinimo.y - margen) p.y = limiteMaximo.y + margen;

            transform.position = p;
        }
    }
}
