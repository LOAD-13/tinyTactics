using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Edificios;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>Cómo terminó la partida para el bando que mira.</summary>
    public enum Desenlace { EnJuego, Victoria, Derrota }

    /// <summary>
    /// Decide quién sigue en pie y cuándo se acabó la partida.
    ///
    /// La regla es una sola y vale igual para todos: <b>el bando que pierde su castillo queda
    /// eliminado</b>, y con él caen sus unidades y el resto de sus edificios. Cuando solo
    /// queda un bando, se acabó.
    /// </summary>
    /// <remarks>
    /// No hay caso especial para el jugador, y es deliberado. Un árbitro con una rama «si es
    /// el humano...» es un árbitro que hay que volver a tocar cuando llegue el FFA de cinco
    /// en la semana 13. Aquí el jugador es el bando 0 y nada más; que la pantalla le enseñe
    /// victoria o derrota depende solo de en qué bando esté mirando, que es justo lo que el
    /// panel de pruebas puede cambiar en caliente.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Árbitro de partida")]
    public class ArbitroDePartida : MonoBehaviour
    {
        public static ArbitroDePartida Actual { get; private set; }

        [Tooltip("Cada cuánto se revisa quién sigue vivo. No hace falta cada frame.")]
        [Range(0.1f, 2f)] public float intervalo = 0.4f;

        [Tooltip("Bandos que empiezan la partida. Lo fija el generador de la escena.")]
        [Range(1, 5)] public int bandos = 3;

        readonly HashSet<int> _eliminados = new HashSet<int>();
        readonly List<Unidad> _condenadas = new List<Unidad>();
        readonly List<Edificio> _ruinas = new List<Edificio>();

        float _proximaRevision;

        /// <summary>Se dispara una vez, cuando la partida termina. Lo escucha la pantalla.</summary>
        public event System.Action<int> AlTerminar;

        /// <summary>Bando que ganó, o −1 si la partida sigue.</summary>
        public int Ganador { get; private set; } = -1;

        public bool Terminada { get; private set; }

        public bool Eliminado(int bando) => _eliminados.Contains(bando);

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>Cómo va la partida desde el punto de vista de un bando.</summary>
        public Desenlace DesenlacePara(int bando)
        {
            if (_eliminados.Contains(bando)) return Desenlace.Derrota;
            if (!Terminada) return Desenlace.EnJuego;

            return Ganador == bando ? Desenlace.Victoria : Desenlace.Derrota;
        }

        readonly HashSet<int> _enJuego = new HashSet<int>();
        readonly List<int> _porEliminar = new List<int>();

        /// <summary>
        /// Apunta qué bandos empiezan la partida de verdad.
        /// </summary>
        /// <remarks>
        /// Hace falta porque <c>bandos</c> es cuántos bandos <i>caben</i>, no cuántos hay: si
        /// el mapa genera dos bases y el contador dice tres, el tercero no tiene castillo
        /// desde el primer fotograma. Sin esta foto inicial se le daría por eliminado en la
        /// primera revisión y la partida terminaría antes de empezar, con una pantalla de
        /// victoria en cuanto le das a Play.
        ///
        /// Va en <c>Start</c> y no en <c>Awake</c>: los edificios se registran en su
        /// <c>OnEnable</c>, y solo a partir de <c>Start</c> está garantizado que estén todos.
        /// </remarks>
        void Start()
        {
            for (int bando = 0; bando < bandos; bando++)
                if (TieneCastillo(bando)) _enJuego.Add(bando);
        }

        void Update()
        {
            if (Terminada || Time.time < _proximaRevision) return;
            _proximaRevision = Time.time + intervalo;

            Revisar();
        }

        void Revisar()
        {
            int enPie = 0;
            int ultimo = -1;

            foreach (int bando in _enJuego)
            {
                if (_eliminados.Contains(bando)) continue;

                if (!TieneCastillo(bando))
                {
                    // No se elimina aquí dentro: eliminar toca _eliminados, y modificar un
                    // conjunto mientras se recorre otro que comparte estado es pedir una
                    // excepción a mitad de la revisión. Se apunta y se resuelve al salir.
                    _porEliminar.Add(bando);
                    continue;
                }

                enPie++;
                ultimo = bando;
            }

            for (int i = 0; i < _porEliminar.Count; i++) Eliminar(_porEliminar[i]);
            _porEliminar.Clear();

            // Con un solo bando en pie se acabó. También con NINGUNO: si dos castillos caen
            // en la misma revisión no se puede dejar la partida corriendo para siempre
            // esperando a un ganador que ya no existe.
            if (enPie > 1) return;

            Terminada = true;
            Ganador = ultimo;

            AlTerminar?.Invoke(Ganador);
        }

        static bool TieneCastillo(int bando)
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion != bando) continue;

                // El castillo se reconoce por ser centro de entrega, no por su tipo. Así, el
                // día que haya expansiones con su propio centro, perder el castillo original
                // dejará de ser el fin de la partida sin tocar esta línea — que es justo lo
                // que hace Warcraft y lo que la semana 13 va a necesitar.
                if (e.centroDeEntrega && e.Operativo) return true;
            }

            return false;
        }

        /// <summary>
        /// Borra del mapa todo lo que le quedaba a un bando.
        /// </summary>
        /// <remarks>
        /// Sin esto, un bando sin castillo seguiría teniendo tropas sueltas peleando por un
        /// sitio en el que ya no puede reponer nada. Se ve como un juego roto, no como una
        /// última resistencia heroica.
        /// </remarks>
        void Eliminar(int bando)
        {
            if (!_eliminados.Add(bando)) return;

            _condenadas.Clear();
            var registro = RegistroDeUnidades.Todas;

            for (int i = 0; i < registro.Count; i++)
            {
                var u = registro[i];
                if (u != null && u.Viva && u.faccion == bando) _condenadas.Add(u);
            }

            // Se copian antes de matar: morir saca a la unidad del registro, y modificar la
            // lista que se está recorriendo es la forma clásica de saltarse la mitad.
            for (int i = 0; i < _condenadas.Count; i++)
                _condenadas[i].RecibirDano(_condenadas[i].Vida);

            _ruinas.Clear();
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e != null && e.faccion == bando) _ruinas.Add(e);
            }

            for (int i = 0; i < _ruinas.Count; i++)
                if (_ruinas[i] != null) _ruinas[i].Derribar();
        }
    }
}
