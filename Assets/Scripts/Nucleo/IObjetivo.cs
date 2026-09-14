using UnityEngine;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Algo a lo que se le puede pegar: una unidad o un edificio.
    ///
    /// Existe porque la máquina de estados no tiene por qué saber a qué le está pegando.
    /// Antes el objetivo era un <see cref="Unidad"/> y punto, así que atacar un edificio
    /// habría obligado a duplicar la persecución, el alcance, la cadencia y el efecto: dos
    /// caminos que resuelven lo mismo y que se desincronizan a la primera corrección.
    ///
    /// La alternativa era hacer que el edificio fuera una unidad, y eso es peor: heredaría
    /// velocidad, hambre, rutas y empuje, todo a cero y todo estorbando.
    /// </summary>
    public interface IObjetivo
    {
        /// <summary>A qué bando pertenece. Sirve para no pegarle a los propios.</summary>
        int Faccion { get; }

        /// <summary>Sigue en pie y merece la pena seguir golpeándolo.</summary>
        bool Vivo { get; }

        /// <summary>
        /// Dónde está. Se usa para orientar el sprite del atacante y para soltar la flecha.
        /// </summary>
        Vector3 Posicion { get; }

        /// <summary>
        /// Distancia desde un punto hasta el objetivo, medida al <b>borde</b>.
        ///
        /// Es lo que permite que un guerrero con 0,8 de alcance golpee un castillo de cinco
        /// casillas de ancho. Midiendo al centro tendría que meterse dentro del edificio
        /// para llegar, que es imposible porque el edificio bloquea sus propias celdas: la
        /// unidad se quedaría empujando la pared para siempre.
        /// </summary>
        float DistanciaDesde(Vector3 punto);

        /// <summary>Punto al que caminar para ponerse a tiro, según por dónde se venga.</summary>
        Vector3 PuntoDeAtaqueDesde(Vector3 origen);

        /// <summary>Aplica el daño. El agresor viaja para que el golpeado pueda responder.</summary>
        void RecibirDano(int cantidad, Unidad agresor);

        /// <summary>
        /// True si es una unidad. Lo consultan las reglas que solo valen para unidades: la
        /// curación del monje y la respuesta al ser agredido.
        /// </summary>
        bool EsUnidad { get; }
    }
}
