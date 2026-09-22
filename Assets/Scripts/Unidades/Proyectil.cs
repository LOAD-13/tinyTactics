using UnityEngine;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Una flecha en vuelo.
    ///
    /// Es puramente cosmética: el daño ya se resolvió cuando el arquero terminó su
    /// animación, y esto solo lo explica en pantalla. Por eso no comprueba colisiones ni
    /// avisa a nadie al llegar — si el objetivo muere antes, la flecha sigue su camino y
    /// se desvanece, que es exactamente lo que se ve en cualquier RTS.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Proyectil")]
    public class Proyectil : MonoBehaviour
    {
        Vector3 _destino;
        float _velocidad = 14f;

        public void Configurar(Vector3 destino, float velocidad)
        {
            _destino = destino;
            _velocidad = velocidad;

            // Red de seguridad: si por lo que sea nunca llega, no se queda en la escena
            // para siempre acumulando objetos.
            Destroy(gameObject, 3f);
        }

        void Update()
        {
            Vector3 hacia = _destino - transform.position;
            float paso = _velocidad * Time.deltaTime;

            if (hacia.sqrMagnitude <= paso * paso)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += hacia.normalized * paso;

            Esconder();
        }

        /// <summary>
        /// Una flecha tampoco se dibuja donde el jugador no ve.
        /// </summary>
        /// <remarks>
        /// No necesita saber de qué bando es: lo que decide es si el jugador ve el TROZO DE
        /// MAPA por el que pasa la flecha, y eso no depende de quién la disparó. Sin esto,
        /// las flechas de una escaramuza en niebla salen de la nada y se pierden en la nada,
        /// que es exactamente el dato que la niebla debería estar ocultando.
        /// </remarks>
        void Esconder()
        {
            var niebla = Mundo.NieblaDeGuerra.Actual;
            if (niebla == null) return;

            bool tapar = niebla.EstadoParaJugador(transform.position) != Mundo.EstadoVisible.Visible;
            Mundo.OcultarEnNiebla.Aplicar(gameObject, tapar);
        }
    }
}
