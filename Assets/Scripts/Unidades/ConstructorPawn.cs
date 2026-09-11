using UnityEngine;
using TinyTactics.Edificios;
using TinyTactics.Movimiento;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// El pawn levantando un edificio: ir, martillar y quedarse hasta que esté en pie.
    ///
    /// <b>Por qué no está dentro del recolector.</b> Se parecen —los dos caminan hasta algo
    /// y le dan golpes— pero se interrumpen distinto y terminan distinto: el recolector
    /// vuelve al castillo con una carga y busca relevo cuando el árbol se seca; la obra no
    /// carga nada y, al acabar, el pawn simplemente se queda quieto. Fundirlos habría
    /// significado un único componente con dos ciclos de vida entrelazados, que es la forma
    /// exacta del bug que costó la semana 05.
    ///
    /// Además así un pawn puede llegar a la obra <b>cargado</b>: la construcción no le
    /// quita el saco, solo le pone el martillo por delante.
    /// </summary>
    [RequireComponent(typeof(Unidad))]
    [RequireComponent(typeof(MovimientoUnidad))]
    [RequireComponent(typeof(MaquinaDeEstados))]
    [AddComponentMenu("Tiny Tactics/Constructor (pawn)")]
    public class ConstructorPawn : MonoBehaviour
    {
        enum Fase { Parado, Yendo, Martillando }

        [Tooltip("Cada cuánto reintentar la ruta si se queda parado sin llegar.")]
        [Range(0.2f, 3f)] public float esperaReintento = 0.8f;

        [Tooltip("Holgura sobre el borde del edificio para considerar que ya está a pie de obra.")]
        [Range(0.5f, 3f)] public float alcanceObra = 1.5f;

        Fase _fase;
        ObraEnConstruccion _obra;
        Edificio _edificio;

        bool _fichado;
        int _vueltasVistas;
        float _proximoIntento;

        Unidad _unidad;
        MovimientoUnidad _movimiento;
        MaquinaDeEstados _maquina;

        /// <summary>True si está en algún punto del ciclo de obra. Lo lee el panel.</summary>
        public bool Construyendo => _fase != Fase.Parado;

        /// <summary>Lo que está levantando, o null.</summary>
        public ObraEnConstruccion Obra => _obra;

        void Awake()
        {
            _unidad = GetComponent<Unidad>();
            _movimiento = GetComponent<MovimientoUnidad>();
            _maquina = GetComponent<MaquinaDeEstados>();
        }

        void OnDisable() => Despedirse();

        // -----------------------------------------------------------------
        // Órdenes
        // -----------------------------------------------------------------

        /// <summary>Ponerse a levantar una obra.</summary>
        public void Construir(ObraEnConstruccion obra)
        {
            if (obra == null || obra.Terminada || _maquina.Muerta) return;

            Despedirse();

            _obra = obra;
            _edificio = obra.GetComponent<Edificio>();

            // Un pawn de otro bando no construye lo ajeno. Se comprueba aquí y no en la
            // orden porque el relevo automático del final también pasa por este método.
            if (_edificio == null || _edificio.faccion != _unidad.faccion)
            {
                _obra = null;
                return;
            }

            Ir();
        }

        /// <summary>
        /// Abandona la obra sin deshacerla. La llaman mover, atacar, detener y recolectar.
        /// </summary>
        /// <remarks>
        /// Lo construido <b>no se pierde</b>, igual que no se pierde la carga de un
        /// recolector al que mandas a otro sitio: los martillazos ya dados siguen contados
        /// en la obra, que es de quien son. Lo que se pierde es el sitio en el andamio.
        /// </remarks>
        public void Cancelar()
        {
            Despedirse();

            _fase = Fase.Parado;
            _obra = null;
            _edificio = null;

            if (_maquina == null) return;

            _maquina.Trabajar(false);
            _maquina.EmpunarMartillo(false);
        }

        // -----------------------------------------------------------------
        // Ciclo
        // -----------------------------------------------------------------

        void Update()
        {
            if (_fase == Fase.Parado || _maquina.Muerta) return;

            // La obra puede haberla terminado otro pawn mientras este venía de camino.
            if (_obra == null || _obra.Terminada) { Terminar(); return; }

            if (_fase == Fase.Yendo) EnCamino();
            else Martillando();
        }

        void EnCamino()
        {
            Vector3 destino = _edificio.PuntoDeEntregaDesde(transform.position);

            // Las mismas dos condiciones que la tala: haber terminado el camino Y estar
            // cerca. Con la distancia sola, un pawn se ponía a martillar desde el otro lado
            // de una casa de dos tiles porque el radio le alcanzaba.
            bool andando = _movimiento.EnMovimiento;
            bool cerca = _edificio.DistanciaA(transform.position) <= alcanceObra;

            if (!andando && cerca) { Llegar(); return; }

            Reintentar(destino);
        }

        void Llegar()
        {
            _movimiento.Detener();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = _edificio.transform.position.x < transform.position.x;

            if (!_fichado)
            {
                _obra.Fichar();
                _fichado = true;
            }

            // Martillo antes que trabajar: la tira de obra se elige por la herramienta, y
            // sin esto el primer fotograma se vería el hacha.
            _maquina.EmpunarMartillo(true);
            _maquina.Trabajar(true);
            _maquina.ReiniciarGolpes();

            _vueltasVistas = 0;
            _fase = Fase.Martillando;
        }

        /// <summary>
        /// Cada pasada completa de la animación es un martillazo.
        /// </summary>
        /// <remarks>
        /// Se lleva la cuenta de las vueltas ya cobradas en vez de mirar solo el total: la
        /// máquina pone su contador a cero en cada cambio de estado, y sin el testigo un
        /// reinicio a mitad de obra habría hecho que el mismo golpe se cobrara dos veces.
        /// </remarks>
        void Martillando()
        {
            int vueltas = _maquina.VueltasDeTrabajo;

            // El contador se reinició por debajo (cambio de estado). Se vuelve a sincronizar
            // sin cobrar nada: un martillazo a medias no cuenta.
            if (vueltas < _vueltasVistas) { _vueltasVistas = vueltas; return; }

            while (_vueltasVistas < vueltas)
            {
                _vueltasVistas++;

                if (!_obra.Martillazo()) continue;

                Terminar();
                return;
            }
        }

        /// <summary>
        /// La obra acabó. El pawn se queda quieto a pie de edificio.
        /// </summary>
        /// <remarks>
        /// No se le busca otra obra automáticamente, al revés que al recolector cuando se le
        /// seca el árbol. Construir es una decisión de partida, no una faena repetitiva:
        /// mandar al pawn solo a la siguiente obra le quitaría al jugador el control de
        /// cuándo y dónde crece su base.
        /// </remarks>
        void Terminar()
        {
            Despedirse();

            _fase = Fase.Parado;
            _obra = null;
            _edificio = null;

            if (_maquina == null) return;

            _maquina.Trabajar(false);
            _maquina.EmpunarMartillo(false);
        }

        void Ir()
        {
            _fase = Fase.Yendo;

            _maquina.Trabajar(false);
            _maquina.EmpunarMartillo(true);

            Caminar(_edificio.PuntoDeEntregaDesde(transform.position));
        }

        void Caminar(Vector3 destino)
        {
            _proximoIntento = Time.time + esperaReintento;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            var celda = mundo.Grilla.MundoACelda(destino);
            if (mundo.Grilla.CeldaTransitableJuntoA(celda, transform.position, 12, out celda))
                _movimiento.IrA(celda);
        }

        void Reintentar(Vector3 destino)
        {
            if (_movimiento.EnMovimiento || Time.time < _proximoIntento) return;

            Caminar(destino);
        }

        void Despedirse()
        {
            if (!_fichado) return;

            _fichado = false;
            if (_obra != null) _obra.Despedirse();
        }
    }
}
