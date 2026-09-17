using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Edificios
{
    /// <summary>
    /// Lo que un edificio sabe fabricar, con su coste, su cola y su espera.
    ///
    /// Es la pieza que <b>cierra el bucle económico</b>: sin ella, recolectar no sirve para
    /// nada y el oro solo es un número que sube. Con ella, el oro se convierte en unidades y
    /// las unidades en más oro, que es de lo que va el género entero.
    ///
    /// El cobro va al encolar y no al terminar, como en Warcraft: si se cobrara al final, se
    /// podría encolar diez unidades sin tener con qué pagarlas y el jugador descubriría que
    /// no tiene oro justo cuando ya contaba con el ejército.
    /// </summary>
    [RequireComponent(typeof(Edificio))]
    [AddComponentMenu("Tiny Tactics/Producción de edificio")]
    public class ProduccionEdificio : MonoBehaviour
    {
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
        readonly Unidad[] _destinatario = new Unidad[1];

        Edificio _edificio;
        int _producidas;

        public int EnCola => _cola.Count;
        public int Producidas => _producidas;

        /// <summary>Qué sabe fabricar este edificio, según su ficha del catálogo.</summary>
        public DatosUnidad[] Catalogo =>
            _edificio != null && _edificio.datos != null && _edificio.datos.fabrica != null
                ? _edificio.datos.fabrica
                : new DatosUnidad[0];

        /// <summary>Lo primero de la cola, para que el panel sepa qué retrato enseñar.</summary>
        public DatosUnidad EnCurso => _cola.Count > 0 ? _cola[0].Datos : null;

        /// <summary>
        /// Población ya comprometida por lo que está en cola.
        ///
        /// La cuenta el sistema de población para que encolar diez guerreros con sitio para
        /// dos sea imposible. Sin esto, el tope solo se notaría al salir la tercera unidad,
        /// con el oro de las diez ya cobrado.
        /// </summary>
        public int PoblacionEncolada
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _cola.Count; i++)
                    if (_cola[i].Datos != null) n += Mathf.Max(1, _cola[i].Datos.poblacion);

                return n;
            }
        }

        /// <summary>Cuánto lleva hecho lo que se está fabricando, de 0 a 1.</summary>
        public float Progreso
        {
            get
            {
                if (_cola.Count == 0) return 0f;

                var e = _cola[0];
                return e.Total <= 0f ? 1f : Mathf.Clamp01(1f - e.Restante / e.Total);
            }
        }

        void Awake() => _edificio = GetComponent<Edificio>();

        void Update()
        {
            // Una obra a medio levantar no fabrica nada.
            if (_cola.Count == 0 || !_edificio.Operativo) return;

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
        public bool Encolar(DatosUnidad datos, out string motivo)
        {
            motivo = null;

            if (!_edificio.Operativo)
            {
                motivo = "Todavía en obras";
                return false;
            }

            if (datos == null)
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

            // La población se comprueba antes que el oro: si no cabe, no tiene sentido
            // cobrarle nada al jugador.
            var poblacion = Poblacion.Actual;
            if (poblacion != null &&
                !poblacion.Cabe(_edificio.faccion, Mathf.Max(1, datos.poblacion)))
            {
                motivo = "Sin población. Construye una casa";
                return false;
            }

            if (!eco.Cobrar(_edificio.faccion, datos.oro, datos.madera))
            {
                motivo = "Faltan recursos";
                return false;
            }

            float tiempo = TiempoDe(datos, eco);

            _cola.Add(new Encargo
            {
                Datos = datos,
                Restante = tiempo,
                Total = tiempo,
                Oro = datos.oro,
                Madera = datos.madera
            });

            return true;
        }

        /// <summary>
        /// Cuánto tarda una unidad.
        /// </summary>
        /// <remarks>
        /// Se deriva del coste en oro tomando el pawn como referencia, en vez de guardar un
        /// tiempo por unidad. Así una unidad cara tarda más sin que nadie tenga que mantener
        /// dos tablas coherentes entre sí, que es justo el tipo de pareja de números que se
        /// acaba desincronizando en la semana de balance.
        /// </remarks>
        static float TiempoDe(DatosUnidad datos, Economia eco)
        {
            float referencia = Mathf.Max(1, 50);
            float proporcion = Mathf.Max(0.5f, datos.oro / referencia);

            return eco.datos.tiempoPawn * proporcion;
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

        /// <summary>Saca la unidad terminada junto al edificio que la fabricó.</summary>
        /// <summary>
        /// Desplazamientos, en tiles, para repartir las unidades recién salidas.
        /// </summary>
        /// <remarks>
        /// El orden importa: primero el centro, luego los lados y después la fila de atrás.
        /// Así una sola unidad sale por la puerta y no de lado, que es lo que se espera, y el
        /// abanico solo se nota cuando de verdad hay varias.
        /// </remarks>
        static readonly Vector2[] Abanico =
        {
            new Vector2(0f, 0f),
            new Vector2(1.1f, 0f),
            new Vector2(-1.1f, 0f),
            new Vector2(0.55f, -1.1f),
            new Vector2(-0.55f, -1.1f),
            new Vector2(2.2f, 0f),
            new Vector2(-2.2f, 0f),
            new Vector2(1.65f, -1.1f),
            new Vector2(-1.65f, -1.1f),
            new Vector2(0f, -2.2f),
        };

        void Sacar(DatosUnidad datos)
        {
            if (datos == null) return;

            var catalogo = CatalogoDePlantillas.Actual;
            var plantilla = catalogo != null
                ? catalogo.Obtener(datos.tipo, _edificio.faccion)
                : null;

            if (plantilla == null)
            {
                Debug.LogWarning(
                    $"[Tiny Tactics] {name}: sin plantilla de {datos.tipo} para el bando " +
                    $"{_edificio.faccion}. Regenera la escena.", this);
                return;
            }

            _producidas++;

            // El punto de salida cae dentro del terreno que el edificio bloquea. Se busca la
            // celda pisable más cercana antes de soltar la unidad: nacer sobre terreno
            // bloqueado no es un problema de dibujo, es que el pathfinding no tiene por dónde
            // empezar.
            // Cada unidad sale por un sitio DISTINTO, repartidas en abanico delante del
            // edificio. Antes todas nacían en la misma celda, y de ahí salían los dos
            // síntomas que se vieron en la exposición de la semana 06:
            //
            //  · Con la posición exactamente igual, el empuje blando no tiene ninguna
            //    dirección hacia la que separarlas y se quedan encajadas una dentro de otra.
            //  · Con la Y exactamente igual comparten orden de dibujo, y un empate deja el
            //    orden en manos del motor: los dos sprites se intercambian cada fotograma.
            //
            // Repartir la salida ataca las dos a la vez, y además es lo que hace cualquier
            // RTS: las unidades nuevas se abren en vez de amontonarse en la puerta.
            Vector3 salida = _edificio.PuntoDeSalida +
                             (Vector3)Abanico[_producidas % Abanico.Length];

            var grilla = Mundo.MundoJuego.Actual != null ? Mundo.MundoJuego.Actual.Grilla : null;
            if (grilla != null &&
                grilla.CeldaTransitableCercana(grilla.MundoACelda(salida), 10, out var libre))
            {
                salida = grilla.CeldaAMundo(libre);
            }

            var copia = Instantiate(plantilla, salida, Quaternion.identity, transform.parent);
            copia.name = $"{datos.tipo}_p{_producidas}";
            copia.SetActive(true);

            var libro = Nucleo.EstadisticasPartida.Actual;
            if (libro != null) libro.UnidadEntrenada(_edificio != null ? _edificio.faccion : 0);

            var unidad = copia.GetComponent<Unidad>();
            if (unidad != null) unidad.Configurar(datos, _edificio.faccion);

            if (!tienePuntoDeReunion || unidad == null) return;

            // Sale trabajando o caminando, pero siempre por medio de una orden y nunca
            // moviendo el transform: si mañana la IA produce unidades, tiene que pasar por
            // el mismo sitio que el jugador (ADR-01).
            _destinatario[0] = unidad;

            if (nodoDeReunion != null && !nodoDeReunion.Agotado &&
                copia.GetComponent<RecolectorPawn>() != null)
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
