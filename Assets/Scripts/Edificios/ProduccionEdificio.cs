using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Edificios
{
    /// <summary>
    /// Lo que un edificio sabe fabricar, con su coste, su cola y su espera.
    ///
    /// Es la pieza que <b>cierra el bucle económico</b>: sin ella, recolectar no sirve para
    /// nada y el oro solo es un número que sube. Con ella, el oro se convierte en pawns y
    /// los pawns en más oro, que es de lo que va el género entero.
    ///
    /// El cobro va al encolar y no al terminar, como en Warcraft: si se cobrara al final,
    /// se podría encolar diez unidades sin tener con qué pagarlas y el jugador descubriría
    /// que no tiene oro justo cuando ya contaba con el ejército.
    /// </summary>
    [RequireComponent(typeof(Edificio))]
    [AddComponentMenu("Tiny Tactics/Producción de edificio")]
    public class ProduccionEdificio : MonoBehaviour
    {
        [Header("Qué fabrica")]
        [Tooltip("Datos de la unidad que produce. Esta semana, el pawn.")]
        public DatosUnidad datosUnidad;

        [Tooltip("Copia inactiva de la unidad, con sus animaciones ya resueltas a sprites.")]
        public GameObject plantilla;

        [Header("Punto de reunión")]
        [Tooltip("Si está puesto, las unidades nuevas caminan hasta aquí al salir.")]
        public bool tienePuntoDeReunion;
        public Vector3 puntoDeReunion;

        [Tooltip("Si el punto cae sobre un recurso, el pawn nuevo se pone a trabajarlo solo.")]
        public Mundo.NodoRecurso nodoDeReunion;

        /// <summary>Encargo pendiente. El coste se guarda para poder devolverlo al cancelar.</summary>
        class Encargo
        {
            public DatosUnidad Datos;
            public float Restante;
            public float Total;
            public int Oro;
            public int Madera;
        }

        readonly List<Encargo> _cola = new List<Encargo>();

        /// <summary>Lista de un solo hueco para emitir órdenes sin reservar memoria cada vez.</summary>
        readonly Unidades.Unidad[] _destinatario = new Unidades.Unidad[1];

        Edificio _edificio;
        int _producidas;

        public int EnCola => _cola.Count;
        public int Producidas => _producidas;

        /// <summary>Cuánto lleva hecho lo que se está fabricando, de 0 a 1.</summary>
        public float Progreso
        {
            get
            {
                if (_cola.Count == 0) return 0f;

                var e = _cola[0];
                if (e.Total <= 0f) return 1f;

                return Mathf.Clamp01(1f - e.Restante / e.Total);
            }
        }

        void Awake() => _edificio = GetComponent<Edificio>();

        void Update()
        {
            if (_cola.Count == 0) return;

            var encargo = _cola[0];
            encargo.Restante -= Time.deltaTime;

            if (encargo.Restante > 0f) return;

            _cola.RemoveAt(0);
            Sacar(encargo.Datos);
        }

        // -----------------------------------------------------------------

        /// <summary>
        /// Mete una unidad en la cola. Devuelve por qué no, si no ha podido.
        /// </summary>
        public bool Encolar(out string motivo)
        {
            motivo = null;

            if (datosUnidad == null || plantilla == null)
            {
                motivo = "Este edificio no sabe producir";
                return false;
            }

            var eco = Economia.Actual;
            if (eco == null || eco.datos == null)
            {
                motivo = "Sin economía";
                return false;
            }

            if (_cola.Count >= eco.datos.colaMaxima)
            {
                motivo = "Cola llena";
                return false;
            }

            if (!eco.Cobrar(_edificio.faccion, datosUnidad.oro, datosUnidad.madera))
            {
                motivo = "Faltan recursos";
                return false;
            }

            _cola.Add(new Encargo
            {
                Datos = datosUnidad,
                Restante = eco.datos.tiempoPawn,
                Total = eco.datos.tiempoPawn,
                Oro = datosUnidad.oro,
                Madera = datosUnidad.madera
            });

            return true;
        }

        /// <summary>Cancela el último encargo y devuelve lo que costó.</summary>
        public void CancelarUltimo()
        {
            if (_cola.Count == 0) return;

            var encargo = _cola[_cola.Count - 1];
            _cola.RemoveAt(_cola.Count - 1);

            var eco = Economia.Actual;
            if (eco != null) eco.Reembolsar(_edificio.faccion, encargo.Oro, encargo.Madera);
        }

        // -----------------------------------------------------------------

        /// <summary>
        /// Saca la unidad terminada.
        /// </summary>
        /// <remarks>
        /// Se clona una <b>plantilla inactiva</b> en vez de construir la unidad desde cero.
        /// El motivo es que las animaciones se resuelven a sprites en el editor, con
        /// <c>AssetDatabase</c>, que no existe al ejecutar: una unidad creada en caliente
        /// saldría muda de dibujos. La plantilla ya los lleva serializados, y al estar
        /// inactiva no se registra ni se puede seleccionar mientras espera.
        /// </remarks>
        void Sacar(DatosUnidad datos)
        {
            _producidas++;

            // El punto de salida cae dentro del disco que el castillo bloquea en la grilla.
            // Se busca la celda pisable más cercana antes de soltar la unidad: nacer sobre
            // terreno bloqueado no es un problema de dibujo, es que el pathfinding no tiene
            // por dónde empezar.
            Vector3 salida = _edificio.PuntoDeSalida;

            var grilla = Mundo.MundoJuego.Actual != null ? Mundo.MundoJuego.Actual.Grilla : null;
            if (grilla != null &&
                grilla.CeldaTransitableCercana(grilla.MundoACelda(salida), 8, out var libre))
            {
                salida = grilla.CeldaAMundo(libre);
            }

            var copia = Instantiate(plantilla, salida,
                                    Quaternion.identity, transform.parent);

            copia.name = $"{datos.tipo}_p{_producidas}";
            copia.SetActive(true);

            var unidad = copia.GetComponent<Unidades.Unidad>();
            if (unidad != null) unidad.Configurar(datos, _edificio.faccion);

            if (!tienePuntoDeReunion || unidad == null) return;

            // Sale trabajando o caminando, pero siempre por medio de una orden y nunca
            // moviendo el transform: si mañana la IA produce unidades, tiene que pasar por
            // el mismo sitio que el jugador (ADR-01).
            _destinatario[0] = unidad;

            if (nodoDeReunion != null && !nodoDeReunion.Agotado)
            {
                Autoridad.Emitir(
                    new OrdenRecolectar { Faccion = _edificio.faccion, Nodo = nodoDeReunion },
                    _destinatario);
                return;
            }

            var mundo = Mundo.MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            Vector2Int destino = mundo.Grilla.MundoACelda(puntoDeReunion);
            if (!mundo.Grilla.CeldaTransitableCercana(destino, 8, out destino)) return;

            Autoridad.Emitir(
                new OrdenMover { Faccion = _edificio.faccion, Destino = destino },
                _destinatario);
        }
    }
}
