using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Lo que hizo un bando durante la partida.
    /// </summary>
    public class HojaDeBando
    {
        public int OroRecolectado, MaderaRecolectada, CarneRecolectada;
        public int OroGastado, MaderaGastada;

        public int UnidadesEntrenadas, UnidadesPerdidas, UnidadesEliminadas;
        public int EdificiosConstruidos, EdificiosPerdidos, EdificiosDestruidos;

        public int PicoDePoblacion;
        public int Ordenes;

        /// <summary>Segundo de partida en que este bando entró en combate. −1 si nunca.</summary>
        public float PrimerCombate = -1f;

        /// <summary>Bajas por tipo de unidad. De aquí sale el MVP.</summary>
        public readonly Dictionary<TipoUnidad, int> BajasPorTipo = new Dictionary<TipoUnidad, int>();

        /// <summary>
        /// Población del bando muestreada a lo largo de la partida.
        /// </summary>
        /// <remarks>
        /// Es lo que convierte la hoja de resultados en una historia: un pico dice cuándo se
        /// montó el ejército y una caída en vertical dice cuándo lo perdiste. «Pico de
        /// población: 30» da el número más alto pero no dice si se alcanzó al minuto tres o
        /// al minuto quince, ni cuántas veces te rehiciste.
        /// </remarks>
        public readonly List<int> Historia = new List<int>(256);

        public int RecolectadoTotal => OroRecolectado + MaderaRecolectada + CarneRecolectada;

        /// <summary>
        /// Lo recolectado y no gastado. Delata al que acumula por miedo, que es la
        /// estadística que más discusión genera entre jugadores.
        /// </summary>
        public int SinGastar =>
            Mathf.Max(0, (OroRecolectado - OroGastado) + (MaderaRecolectada - MaderaGastada));

        public TipoUnidad Mvp
        {
            get
            {
                TipoUnidad mejor = TipoUnidad.Pawn;
                int mas = 0;

                foreach (var par in BajasPorTipo)
                {
                    if (par.Value <= mas) continue;
                    mas = par.Value;
                    mejor = par.Key;
                }

                return mejor;
            }
        }

        public int BajasDelMvp
        {
            get
            {
                int mas = 0;
                foreach (var par in BajasPorTipo)
                    if (par.Value > mas) mas = par.Value;

                return mas;
            }
        }
    }

    /// <summary>
    /// Lleva la cuenta de todo lo que pasa en la partida, por bando.
    /// </summary>
    /// <remarks>
    /// <b>Se acumula en vivo, no se calcula al final.</b> Al terminar la partida ya no quedan
    /// cadáveres que contar ni edificios caídos que sumar: si no se apunta cuando ocurre, no
    /// hay de dónde sacarlo después.
    ///
    /// Y es <b>por bando desde el primer día</b>, aunque hoy solo se enseñe el del jugador.
    /// El FFA de cinco llega en la semana 13, y convertir un contador global en cinco más
    /// tarde es rehacerlo entero.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Estadísticas de partida")]
    public class EstadisticasPartida : MonoBehaviour
    {
        public static EstadisticasPartida Actual { get; private set; }

        readonly Dictionary<int, HojaDeBando> _hojas = new Dictionary<int, HojaDeBando>();

        float _inicio;

        /// <summary>Cuánto lleva corriendo la partida, en segundos.</summary>
        public float Duracion => Time.time - _inicio;

        void Awake()
        {
            Actual = this;
            _inicio = Time.time;
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        public HojaDeBando Hoja(int faccion)
        {
            if (!_hojas.TryGetValue(faccion, out var hoja))
            {
                hoja = new HojaDeBando();
                _hojas[faccion] = hoja;
            }

            return hoja;
        }

        // -----------------------------------------------------------------
        // Apuntes
        // -----------------------------------------------------------------

        public void Recolectado(int faccion, TipoRecurso recurso, int cantidad)
        {
            if (cantidad <= 0) return;

            var h = Hoja(faccion);
            switch (recurso)
            {
                case TipoRecurso.Oro: h.OroRecolectado += cantidad; break;
                case TipoRecurso.Madera: h.MaderaRecolectada += cantidad; break;
                case TipoRecurso.Carne: h.CarneRecolectada += cantidad; break;
            }
        }

        public void Gastado(int faccion, int oro, int madera)
        {
            var h = Hoja(faccion);
            h.OroGastado += Mathf.Max(0, oro);
            h.MaderaGastada += Mathf.Max(0, madera);
        }

        public void UnidadEntrenada(int faccion) => Hoja(faccion).UnidadesEntrenadas++;

        public void EdificioConstruido(int faccion) => Hoja(faccion).EdificiosConstruidos++;

        /// <summary>
        /// Alguien cayó. Se apunta en las dos hojas: pérdida para uno, baja para el otro.
        /// </summary>
        public void UnidadCaida(Unidades.Unidad victima, Unidades.Unidad matador)
        {
            if (victima == null) return;

            Hoja(victima.faccion).UnidadesPerdidas++;

            if (matador == null || matador.faccion == victima.faccion) return;

            var h = Hoja(matador.faccion);
            h.UnidadesEliminadas++;

            if (matador.datos == null) return;

            var tipo = matador.datos.tipo;
            h.BajasPorTipo.TryGetValue(tipo, out int bajas);
            h.BajasPorTipo[tipo] = bajas + 1;
        }

        public void EdificioCaido(int duenoFaccion, int agresorFaccion)
        {
            Hoja(duenoFaccion).EdificiosPerdidos++;

            if (agresorFaccion >= 0 && agresorFaccion != duenoFaccion)
                Hoja(agresorFaccion).EdificiosDestruidos++;
        }

        /// <summary>Una orden del jugador o de la IA. De aquí salen las órdenes por minuto.</summary>
        public void Orden(int faccion) => Hoja(faccion).Ordenes++;

        public void Combate(int faccion)
        {
            var h = Hoja(faccion);
            if (h.PrimerCombate < 0f) h.PrimerCombate = Duracion;
        }

        /// <summary>
        /// Órdenes por minuto de un bando.
        /// </summary>
        /// <remarks>
        /// Esta métrica cuesta una línea porque desde la semana 02 <b>toda</b> acción pasa por
        /// una <c>Orden</c> (ADR-01). No hubo que instrumentar el ratón, ni la interfaz, ni la
        /// futura IA: hay un solo sitio por el que pasa todo, y contar ahí lo cuenta todo.
        /// </remarks>
        public float OrdenesPorMinuto(int faccion)
        {
            float minutos = Duracion / 60f;
            return minutos < 0.02f ? 0f : Hoja(faccion).Ordenes / minutos;
        }

        [Tooltip("Cada cuántos segundos se apunta un punto de la historia.")]
        [Range(0.5f, 10f)] public float pasoDeHistoria = 2f;

        float _proximaMuestra;

        /// <summary>
        /// Cuántos segundos representa cada punto de la historia. Lo usa la gráfica para
        /// poner las marcas de tiempo.
        /// </summary>
        public float PasoDeHistoria => pasoDeHistoria;

        void Update()
        {
            // El pico de población se muestrea, no se engancha a un evento, porque la
            // población ya se recuenta sola cuatro veces por segundo (HU-040): preguntar es
            // más barato y más fiable que mantener un segundo camino en paralelo.
            var censo = Poblacion.Actual;
            if (censo == null) return;

            bool apuntar = Time.time >= _proximaMuestra;
            if (apuntar) _proximaMuestra = Time.time + pasoDeHistoria;

            foreach (var par in _hojas)
            {
                int usada = censo.Usada(par.Key);
                if (usada > par.Value.PicoDePoblacion) par.Value.PicoDePoblacion = usada;

                if (apuntar) par.Value.Historia.Add(usada);
            }

            // Un bando que todavía no ha hecho nada no tiene hoja, y sin hoja no tiene
            // historia: su gráfica saldría vacía aunque lleve diez minutos con dos pawns.
            // Se le abre en la primera muestra, que es cuando empieza a importar.
            if (apuntar && _hojas.Count == 0) Hoja(0);
        }
    }
}
