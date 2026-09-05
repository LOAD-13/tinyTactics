using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Movimiento;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// El ciclo de recolección de un pawn: ir, picar, cargar, volver, soltar y repetir.
    ///
    /// <b>Por qué es un componente aparte y no más estados dentro de la máquina.</b> La
    /// máquina de estados dice <i>qué se dibuja</i>; esto decide <i>qué se hace</i>, que es
    /// otra pregunta: a qué nodo ir, cuándo está llena la carga, a qué castillo volver y
    /// qué hacer si el árbol se acaba a mitad. Meterlo dentro habría dejado un archivo que
    /// hace de animador, de árbitro de combate y de capataz a la vez — y el ADR-11 se ganó
    /// precisamente por repartir mal esa responsabilidad.
    ///
    /// La máquina sigue siendo la única que toca al animador. Este componente le pide
    /// estados (<c>Trabajar</c>) y le dice qué lleva encima (<c>Cargar</c>); nunca dibuja.
    /// </summary>
    [RequireComponent(typeof(Unidad))]
    [RequireComponent(typeof(MovimientoUnidad))]
    [RequireComponent(typeof(MaquinaDeEstados))]
    [AddComponentMenu("Tiny Tactics/Recolector (pawn)")]
    public class RecolectorPawn : MonoBehaviour
    {
        enum Fase { Parado, Yendo, Trabajando, Volviendo }

        [Tooltip("Cada cuánto reintentar la ruta si la unidad se queda parada sin llegar.")]
        [Range(0.2f, 3f)] public float esperaReintento = 0.8f;

        Fase _fase;
        NodoRecurso _nodo;
        Edificio _entrega;

        TipoRecurso _carga = TipoRecurso.Ninguno;
        int _cantidad;

        int _golpes = 1;
        float _proximoIntento;
        Vector3 _destinoPedido;
        bool _reservado;

        Unidad _unidad;
        MovimientoUnidad _movimiento;
        MaquinaDeEstados _maquina;

        /// <summary>True si está en algún punto del ciclo. Lo lee el panel.</summary>
        public bool Recolectando => _fase != Fase.Parado;

        /// <summary>Lo que lleva encima ahora mismo.</summary>
        public TipoRecurso Carga => _carga;

        void Awake()
        {
            _unidad = GetComponent<Unidad>();
            _movimiento = GetComponent<MovimientoUnidad>();
            _maquina = GetComponent<MaquinaDeEstados>();
        }

        void OnDisable() => SoltarReserva();

        // -----------------------------------------------------------------
        // Órdenes
        // -----------------------------------------------------------------

        /// <summary>Empieza —o cambia— el nodo que trabaja.</summary>
        public void Recolectar(NodoRecurso nodo)
        {
            if (nodo == null || nodo.Agotado || _maquina.Muerta) return;

            SoltarReserva();

            _nodo = nodo;

            // Cargado no se empieza nada: primero se entrega y DESPUÉS se va al nodo nuevo,
            // sea del recurso que sea. Un peón que tira al suelo diez de oro porque le has
            // señalado un árbol pierde trabajo ya hecho sin avisar, y el jugador no tiene
            // forma de anticiparlo. Como _nodo ya apunta al nuevo, al depositar arranca solo
            // el ciclo que se acaba de pedir.
            if (_carga != TipoRecurso.Ninguno)
            {
                IrAEntregar();
                return;
            }

            _cantidad = 0;
            _maquina.Cargar(TipoRecurso.Ninguno);

            IrAlNodo();
        }

        /// <summary>Llevar lo que tenga al centro de entrega más cercano y soltarlo.</summary>
        public void Entregar()
        {
            if (_maquina.Muerta || _carga == TipoRecurso.Ninguno) return;

            SoltarReserva();
            _nodo = null;
            IrAEntregar();
        }

        /// <summary>
        /// Abandona el ciclo. La llaman las órdenes de mover, atacar y detener.
        /// </summary>
        /// <remarks>
        /// <b>La carga NO se pierde.</b> Se perdía, y estaba mal: mover a un pawn que volvía
        /// con el saco lo dejaba con las manos vacías y libre para ir a por otra cosa, o sea
        /// que una orden de movimiento borraba silenciosamente varios segundos de trabajo.
        ///
        /// Ahora una orden corta el ciclo pero no le quita nada de encima: se le sigue viendo
        /// el saco, y en cuanto se le vuelva a mandar a un recurso pasará antes por el
        /// castillo. Lo único que se suelta es la reserva del nodo, que sí deja de trabajar.
        /// </remarks>
        public void Cancelar()
        {
            SoltarReserva();

            _fase = Fase.Parado;
            _nodo = null;
            _entrega = null;

            if (_maquina == null) return;

            _maquina.Trabajar(false);

            // Y el dibujo vuelve a decir la verdad. Al empezar a picar se le pone al pawn
            // el recurso del nodo —de ahí sale la herramienta— así que al interrumpirlo se
            // quedaba dibujado con el tronco al hombro sin llevar nada. El jugador veía un
            // peón cargado que aceptaba irse a otro recurso, y parecía que el bloqueo de
            // «primero entrega» no funcionaba, cuando lo que fallaba era el dibujo.
            _maquina.Cargar(_carga);
        }

        // -----------------------------------------------------------------
        // Ciclo
        // -----------------------------------------------------------------

        void Update()
        {
            if (_fase == Fase.Parado || _maquina.Muerta) return;

            switch (_fase)
            {
                case Fase.Yendo: EnCamino(); break;
                case Fase.Trabajando: Picando(); break;
                case Fase.Volviendo: DeVuelta(); break;
            }
        }

        void EnCamino()
        {
            if (_nodo == null || _nodo.Agotado) { Relevo(); return; }

            float alcance = AlcanceBase() + _nodo.radioBloqueo;
            Vector3 destino = _nodo.PuntoDeTrabajo;

            // Se exigen las DOS condiciones: haber terminado el camino y estar cerca.
            //
            // Con la distancia sola, el pawn se plantaba a picar en cuanto entraba en el
            // radio, que con un árbol son dos tiles y medio: se le veía dar hachazos al aire
            // a media pantalla del tronco. Esperar a que la ruta acabe hace que ande hasta
            // la última celda libre que el mapa le permite, que es donde debe estar. La
            // distancia se queda como red de seguridad para el caso de una ruta que muere
            // lejos porque otra unidad ocupaba el destino.
            bool andando = _movimiento.EnMovimiento;
            bool cerca = (destino - transform.position).sqrMagnitude <= alcance * alcance;

            if (!andando && cerca)
            {
                Llegar();
                return;
            }

            // La oveja se mueve mientras el pawn va hacia ella. Se repite la ruta solo si
            // se ha ido lejos de donde se pidió: recalcular cada frame saturaría la cola
            // del buscador para corregir medio tile.
            if ((destino - _destinoPedido).sqrMagnitude > 2.25f) { IrAlNodo(); return; }

            Reintentar(destino);
        }

        void Llegar()
        {
            _movimiento.Detener();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = _nodo.PuntoDeTrabajo.x < transform.position.x;

            if (!_reservado)
            {
                _nodo.Reservar();
                _reservado = true;
            }

            var eco = Economia.Actual;
            _golpes = eco != null && eco.datos != null ? eco.datos.GolpesDe(_nodo.recurso) : 4;

            // Cargar antes de Trabajar: la herramienta con la que pica sale de la misma
            // tabla que el saco que carga, indexada por recurso. Sin esto picaría a mano.
            _maquina.Cargar(_nodo.recurso);
            _maquina.Trabajar(true);

            // Cada tanda empieza de cero. Sin esto, la cuenta dependería de que ninguna de
            // las rutas de interrupción se dejara el contador a medias.
            _maquina.ReiniciarGolpes();

            _fase = Fase.Trabajando;
        }

        void Picando()
        {
            if (_nodo == null || _nodo.Agotado) { SoltarReserva(); Relevo(); return; }

            // El recurso sale al completar los golpes, y el contador de golpes vive en la
            // máquina de estados, que lo borra en cuanto la unidad deja de trabajar. Es lo
            // mismo que ya se decidió para el combate en la semana 04: el ritmo lo marca el
            // dibujo, no un cronómetro por detrás.
            if (_maquina.VueltasDeTrabajo < _golpes) return;

            if (!_nodo.Extraer(out int cantidad)) { SoltarReserva(); Relevo(); return; }

            _carga = _nodo.recurso;
            _cantidad = cantidad;

            SoltarReserva();
            _maquina.Trabajar(false);
            _maquina.Cargar(_carga);

            IrAEntregar();
        }

        void DeVuelta()
        {
            if (_entrega == null)
            {
                _entrega = Edificio.EntregaMasCercana(transform.position, _unidad.faccion);
                if (_entrega == null) { Detenerse(); return; }
            }

            // La distancia se mide al BORDE del castillo, no a un punto fijo, y el destino
            // es el lado por el que viene el pawn. Antes se caminaba siempre a un punto
            // concreto por debajo del edificio: si el peón llegaba por el norte, se le veía
            // rodear el castillo entero para ir a plantarse a una puerta que no existe.
            float alcance = AlcanceBase() + 1.6f;
            Vector3 destino = _entrega.PuntoDeEntregaDesde(transform.position);

            bool cerca = _entrega.DistanciaA(transform.position) <= alcance;

            if (_movimiento.EnMovimiento || !cerca)
            {
                if (!cerca) Reintentar(destino);
                return;
            }

            Depositar();
        }

        void Depositar()
        {
            var eco = Economia.Actual;
            if (eco != null) eco.Depositar(_unidad.faccion, _carga, _cantidad);

            _carga = TipoRecurso.Ninguno;
            _cantidad = 0;
            _maquina.Cargar(TipoRecurso.Ninguno);

            // Y vuelta a empezar. Que el ciclo se reanude solo es lo que convierte una
            // orden en una decisión de partida: mandas dos pawns al oro y te olvidas.
            if (_nodo != null && !_nodo.Agotado) IrAlNodo();
            else Relevo();
        }

        // -----------------------------------------------------------------
        // Apoyo
        // -----------------------------------------------------------------

        /// <summary>
        /// Busca otro nodo del mismo tipo cuando el actual se seca.
        ///
        /// Sin esto, agotar un árbol dejaría al pawn plantado sin decir nada y el jugador
        /// descubriría media partida después que la mitad de su economía estaba parada.
        /// </summary>
        void Relevo()
        {
            TipoRecurso tipo = _nodo != null ? _nodo.recurso : _carga;
            _nodo = null;

            if (_carga != TipoRecurso.Ninguno) { IrAEntregar(); return; }
            if (tipo == TipoRecurso.Ninguno) { Detenerse(); return; }

            var eco = Economia.Actual;
            float radio = eco != null && eco.datos != null ? eco.datos.radioRelevo : 14f;

            var siguiente = NodoRecurso.MasCercano(transform.position, tipo, radio);
            if (siguiente == null) { Detenerse(); return; }

            _nodo = siguiente;
            IrAlNodo();
        }

        void IrAlNodo()
        {
            _fase = Fase.Yendo;
            _maquina.Trabajar(false);

            if (_nodo != null) Caminar(_nodo.PuntoDeTrabajo);
        }

        void IrAEntregar()
        {
            _fase = Fase.Volviendo;
            _maquina.Trabajar(false);

            _entrega = Edificio.EntregaMasCercana(transform.position, _unidad.faccion);
            if (_entrega == null) { Detenerse(); return; }

            Caminar(_entrega.PuntoDeEntregaDesde(transform.position));
        }

        /// <summary>
        /// Se queda quieto sin soltar lo que lleve. Es lo que pasa cuando no hay nada más
        /// que recolectar cerca o cuando no existe ningún sitio donde entregar.
        /// </summary>
        void Detenerse()
        {
            SoltarReserva();

            _fase = Fase.Parado;
            _nodo = null;
            _entrega = null;

            if (_maquina == null) return;

            _maquina.Trabajar(false);
            _maquina.Cargar(_carga);
        }

        /// <summary>Pide ruta a la celda transitable más cercana a un punto del mundo.</summary>
        void Caminar(Vector3 destino)
        {
            _destinoPedido = destino;
            _proximoIntento = Time.time + esperaReintento;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            // Se pide la celda libre más cercana AL PAWN, no la primera del barrido: es lo
            // que hace que se arrime al árbol por el lado por el que llega en vez de rodearlo
            // para ir siempre al mismo sitio.
            var celda = mundo.Grilla.MundoACelda(destino);
            if (mundo.Grilla.CeldaTransitableJuntoA(celda, transform.position, 10, out celda))
                _movimiento.IrA(celda);
        }

        /// <summary>
        /// Si se ha quedado quieto sin llegar, vuelve a pedir la ruta. Pasa cuando otra
        /// unidad le ocupa la celda de destino mientras venía de camino.
        /// </summary>
        void Reintentar(Vector3 destino)
        {
            if (_movimiento.EnMovimiento || Time.time < _proximoIntento) return;

            Caminar(destino);
        }

        void SoltarReserva()
        {
            if (!_reservado) return;

            _reservado = false;
            if (_nodo != null) _nodo.Soltar();
        }

        static float AlcanceBase()
        {
            var eco = Economia.Actual;
            return eco != null && eco.datos != null ? eco.datos.alcanceTrabajo : 1.4f;
        }
    }
}
