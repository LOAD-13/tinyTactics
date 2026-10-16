using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.IA
{
    /// <summary>
    /// El jugador artificial de un bando. Uno por bando rival.
    /// </summary>
    /// <remarks>
    /// <b>No mueve unidades: emite órdenes.</b> Es la regla que sostiene toda la épica y se
    /// puede comprobar con un <c>grep</c>: ningún script de este espacio de nombres toca
    /// <c>MovimientoUnidad</c> ni <c>MaquinaDeEstados</c>. Todo pasa por
    /// <see cref="Autoridad"/>, exactamente igual que un clic del ratón.
    ///
    /// Eso no es purismo. Desde la semana 02 toda acción del jugador es una orden
    /// serializable (ADR-01) precisamente para que el día que haya red las dos partes hablen
    /// el mismo idioma. Una IA que moviera unidades a mano sería la primera cosa del proyecto
    /// que no se puede mandar por un cable.
    ///
    /// <b>Piensa una vez por segundo, no sesenta.</b> Tres capas con tres relojes: la
    /// estratégica decide qué construir y entrenar (~1 Hz), la táctica decide a dónde va el
    /// ejército (~3 Hz), y la máquina de estados de cada unidad sigue corriendo por fotograma
    /// como siempre. Es como corrían siete bots en el hardware de los noventa (ADR-06).
    ///
    /// <b>Y los bots piensan desfasados entre sí.</b> Cada cerebro arranca su reloj con un
    /// desfase distinto según su bando. Con cinco bandos en la semana 13, cinco cerebros
    /// pensando en el mismo fotograma es un tirón visible una vez por segundo; repartidos, no
    /// se nota ninguno.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Cerebro de IA")]
    public class CerebroIA : MonoBehaviour
    {
        [Header("Quien es")]
        [Tooltip("Bando que lleva este cerebro.")]
        [Range(0, 4)] public int faccion = 1;

        [Tooltip("Su plan de juego. Es un dato editable, no codigo (ADR-21).")]
        public PlanDeIA plan;

        [Tooltip("Su dificultad actual.")]
        public DatosIA dificultad;

        [Tooltip("Los tres perfiles, en orden Facil, Normal, Dificil. Los asigna el " +
                 "generador; el panel los recorre para cambiar la dificultad en caliente.")]
        public DatosIA[] perfiles = new DatosIA[0];

        [Tooltip("Fichas de edificio que puede levantar. Las asigna el generador de escena.")]
        public DatosEdificio[] fichas = new DatosEdificio[0];

        [Header("Ritmo")]
        [Tooltip("Segundos entre decisiones de la capa estrategica.")]
        [Range(0.2f, 5f)] public float pasoEstrategico = 1f;

        [Tooltip("Segundos entre decisiones de la capa tactica.")]
        [Range(0.1f, 2f)] public float pasoTactico = 0.35f;

        [Header("Interruptores del panel")]
        [Tooltip("Apagado, el bot deja de decidir. Sus unidades siguen defendiendose solas.")]
        public bool pensando = true;

        [Tooltip("Trampa de pruebas: el bot ve el mapa entero. Ninguna dificultad la usa " +
                 "(ADR-23).")]
        public bool trampaVerTodo;

        /// <summary>Todos los cerebros vivos. Lo usa el panel de partida libre.</summary>
        public static readonly List<CerebroIA> Todos = new List<CerebroIA>();

        MenteEconomica _economia;
        MenteMilitar _militar;

        float _proximaEstrategica;
        float _proximaTactica;

        int[] _inventario;

        /// <summary>Qué está haciendo, en una línea. Lo enseña el panel.</summary>
        public string Estado
        {
            get => _estado;
            private set
            {
                if (value != _estado)
                    Nucleo.DiarioDePartida.Anotar($"Bando {faccion + 1} [{NombreDificultad}] " +
                                                  $"economia: {value}");
                _estado = value;
            }
        }

        string _estado = "arrancando";

        /// <summary>El paso del plan en el que va.</summary>
        public string PasoVisible { get; private set; } = "—";

        /// <summary>Cuántas oleadas ha lanzado.</summary>
        public int Oleadas => _militar != null ? _militar.Lanzadas : 0;

        /// <summary>Qué está haciendo su ejército. Lo enseña el panel.</summary>
        public string EstadoMilitar => _militar != null ? _militar.Estado : "—";

        /// <summary>Segundos de partida cuando podrá atacar por primera vez.</summary>
        public float Espera => dificultad != null ? dificultad.esperaInicial : 60f;

        /// <summary>Si este bot se salta la niebla. Solo con la trampa del panel.</summary>
        public bool VeTodo => trampaVerTodo || (dificultad != null && dificultad.veTodoElMapa);

        /// <summary>El nombre de su dificultad, para el panel.</summary>
        public string NombreDificultad => dificultad != null ? dificultad.nombreVisible : "—";

        /// <summary>El cerebro que lleva un bando, o null si lo juega una persona.</summary>
        public static CerebroIA De(int faccion)
        {
            for (int i = 0; i < Todos.Count; i++)
                if (Todos[i] != null && Todos[i].faccion == faccion) return Todos[i];

            return null;
        }

        float _restoVentaja;

        /// <summary>
        /// Lo que de verdad entra al almacén de este bot por una carga de
        /// <paramref name="cantidad"/>. Lo llama el pawn al depositar.
        /// </summary>
        public int ConVentaja(int cantidad)
        {
            float ventaja = dificultad != null ? dificultad.ventajaDeRecursos : 1f;
            return ReglasDeDificultad.ConVentaja(cantidad, ventaja, ref _restoVentaja);
        }

        /// <summary>Pasa al siguiente perfil. Lo usa el panel.</summary>
        public void CambiarDificultad()
        {
            if (perfiles == null || perfiles.Length == 0) return;

            var actual = dificultad != null ? dificultad.nivel : NivelDeIA.Normal;
            var siguiente = ReglasDeDificultad.Siguiente(actual);

            for (int i = 0; i < perfiles.Length; i++)
                if (perfiles[i] != null && perfiles[i].nivel == siguiente)
                {
                    dificultad = perfiles[i];
                    _restoVentaja = 0f;
                    return;
                }
        }

        void OnEnable() => Todos.Add(this);

        void OnDisable() => Todos.Remove(this);

        void Start()
        {
            _inventario = new int[System.Enum.GetValues(typeof(Objetivo)).Length];

            _economia = new MenteEconomica(this);
            _militar = new MenteMilitar(this);

            // El desfase por bando: dos cerebros nunca piensan en el mismo fotograma.
            float desfase = faccion * 0.23f;

            _proximaEstrategica = Time.time + desfase;
            _proximaTactica = Time.time + desfase * 0.5f;

            if (plan == null)
                Debug.LogWarning($"[Tiny Tactics] El cerebro del bando {faccion} no tiene " +
                                 "plan. No va a hacer nada.", this);
        }

        void Update()
        {
            if (!pensando)
            {
                Estado = "en pausa";
                return;
            }

            if (plan == null) return;

            if (Time.time >= _proximaEstrategica)
            {
                float ritmo = dificultad != null ? dificultad.ritmo : 1f;
                _proximaEstrategica = Time.time +
                                      ReglasDeDificultad.PasoEstrategico(pasoEstrategico, ritmo);
                Estrategia();
            }

            if (Time.time >= _proximaTactica)
            {
                _proximaTactica = Time.time + Mathf.Max(0.1f, pasoTactico);
                _militar.Pensar();
            }
        }

        // -----------------------------------------------------------------
        // Capa estratégica
        // -----------------------------------------------------------------

        /// <summary>
        /// Qué le falta al bot para estar como quiere estar.
        /// </summary>
        /// <remarks>
        /// Dos cosas, y en este orden. Primero las reglas que no esperan —si se va a quedar
        /// sin población, casa ya, sin mirar el plan— y después el plan. Un plan que se
        /// recorre sin reaccionar a nada es un bot que se queda a tope de población con
        /// ochocientos de oro y una cola de entrenamiento que no avanza, y ese no es un bot
        /// difícil: es un bot roto.
        /// </remarks>
        void Estrategia()
        {
            Inventariar();

            _economia.MantenerPawns();

            if (_economia.HaceFaltaCastillo())
            {
                Estado = "sin castillo: levantando otro";
                _economia.Construir(TipoEdificio.Castillo);
                return;
            }

            if (_economia.HaceFaltaCasa())
            {
                Estado = "levantando una casa";
                _economia.Construir(TipoEdificio.Casa);
                return;
            }

            int paso = PlanDeIA.PasoActual(plan.pasos, _inventario);

            if (paso < 0)
            {
                PasoVisible = "plan cumplido";
                Estado = "entrenando ejercito";
                _economia.SeguirElBucle(plan.bucleMilitar, _inventario);
                return;
            }

            var renglon = plan.pasos[paso];
            PasoVisible = $"{renglon.que} x{renglon.cantidad} " +
                          $"({_inventario[(int)renglon.que]} hechos)";

            Estado = _economia.Perseguir(renglon.que);
        }

        /// <summary>Cuántas cosas tiene de cada tipo. Es la entrada del plan.</summary>
        void Inventariar()
        {
            System.Array.Clear(_inventario, 0, _inventario.Length);

            var unidades = RegistroDeUnidades.Todas;
            for (int i = 0; i < unidades.Count; i++)
            {
                var u = unidades[i];
                if (u == null || !u.Viva || u.faccion != faccion || u.datos == null) continue;

                switch (u.datos.tipo)
                {
                    case TipoUnidad.Pawn: _inventario[(int)Objetivo.Pawns]++; break;
                    case TipoUnidad.Guerrero: _inventario[(int)Objetivo.Guerreros]++; break;
                    case TipoUnidad.Lancero: _inventario[(int)Objetivo.Lanceros]++; break;
                    case TipoUnidad.Arquero: _inventario[(int)Objetivo.Arqueros]++; break;
                    case TipoUnidad.Monje: _inventario[(int)Objetivo.Monjes]++; break;
                }
            }

            // Las unidades en cola cuentan como hechas. Sin esto, el bot pide otro guerrero
            // cada segundo mientras el primero se entrena y acaba con la cola llena de
            // guerreros que ya no quiere.
            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion != faccion) continue;

                switch (e.tipo)
                {
                    case TipoEdificio.Casa: _inventario[(int)Objetivo.Casas]++; break;
                    case TipoEdificio.Cuartel: _inventario[(int)Objetivo.Cuarteles]++; break;
                    case TipoEdificio.CampoDeTiro: _inventario[(int)Objetivo.CamposDeTiro]++; break;
                    case TipoEdificio.Monasterio: _inventario[(int)Objetivo.Monasterios]++; break;
                    case TipoEdificio.Torre: _inventario[(int)Objetivo.Torres]++; break;
                }

                // Lo que se esta entrenando ahora mismo cuenta como hecho. Sin esto, el
                // bot pide otro guerrero cada segundo mientras el primero se entrena y acaba
                // con la cola llena de guerreros que ya no queria.
                if (e.TryGetComponent(out ProduccionEdificio produccion))
                    ContarEncolada(produccion.EnCurso);
            }
        }

        void ContarEncolada(DatosUnidad datos)
        {
            if (datos == null) return;

            switch (datos.tipo)
            {
                case TipoUnidad.Pawn: _inventario[(int)Objetivo.Pawns]++; break;
                case TipoUnidad.Guerrero: _inventario[(int)Objetivo.Guerreros]++; break;
                case TipoUnidad.Lancero: _inventario[(int)Objetivo.Lanceros]++; break;
                case TipoUnidad.Arquero: _inventario[(int)Objetivo.Arqueros]++; break;
                case TipoUnidad.Monje: _inventario[(int)Objetivo.Monjes]++; break;
            }
        }

        /// <summary>La ficha de un tipo de edificio, de las que tiene asignadas.</summary>
        public DatosEdificio FichaDe(TipoEdificio tipo)
        {
            for (int i = 0; i < fichas.Length; i++)
                if (fichas[i] != null && fichas[i].tipo == tipo) return fichas[i];

            return null;
        }

        /// <summary>Su castillo, o null si ya no tiene. Es el centro de su mundo.</summary>
        /// <summary>
        /// La casa del bot: el castillo, y si no lo hay, cualquier cosa suya en pie.
        /// </summary>
        public Edificio Base()
        {
            var castillo = Castillo();
            if (castillo != null) return castillo;

            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
                if (casas[i] != null && casas[i].faccion == faccion) return casas[i];

            return null;
        }

        public Edificio Castillo()
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e != null && e.faccion == faccion && e.centroDeEntrega) return e;
            }

            return null;
        }
    }
}
