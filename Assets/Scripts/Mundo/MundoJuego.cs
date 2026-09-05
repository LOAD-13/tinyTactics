using UnityEngine;
using TinyTactics.Movimiento;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Punto de entrada del mundo: reconstruye la grilla lógica y ofrece el buscador
    /// de rutas al resto del juego.
    ///
    /// La grilla <b>no se lee del tilemap</b>: se regenera desde la misma
    /// <see cref="DefinicionMapa"/> que pintó la escena. Como la generación es
    /// determinista, la misma semilla produce exactamente el mismo terreno, así que
    /// lo lógico y lo visual no pueden discrepar (ADR-09).
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Mundo del juego")]
    public class MundoJuego : MonoBehaviour
    {
        [Header("Definición")]
        [Tooltip("El mismo asset con el que se generó la escena.")]
        public DefinicionMapa definicion;

        [Header("Radios de bloqueo (en tiles)")]
        [Tooltip("Cada árbol y cada veta tapan SOLO su propia celda. Con un radio mayor, un " +
                 "bolsón denso se convertía en un bloque macizo de cinco tiles de ancho y el " +
                 "pawn se quedaba picando desde el borde, a dos tiles del oro.")]
        public float radioArbol = 0.7f;

        public float radioOro = 0.6f;
        public float radioCastillo = 2.6f;

        [Header("Depuración")]
        [Tooltip("Dibuja las celdas intransitables en la vista de escena.")]
        public bool mostrarGrilla;
        [Range(8, 80)] public int radioGizmo = 30;

        public GrillaMapa Grilla { get; private set; }
        public BuscadorDeRutas Rutas { get; private set; }
        public MapaGenerado Mapa { get; private set; }

        /// <summary>Instancia activa. Hay un único mundo por escena.</summary>
        public static MundoJuego Actual { get; private set; }

        void Awake()
        {
            Actual = this;

            if (definicion == null)
            {
                Debug.LogError("[Tiny Tactics] MundoJuego no tiene DefinicionMapa asignada.", this);
                enabled = false;
                return;
            }

            Construir();
        }

        void Update()
        {
            Rutas?.Procesar();
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        // -----------------------------------------------------------------

        public void Construir()
        {
            if (definicion == null)
            {
                Debug.LogError("[Tiny Tactics] MundoJuego no tiene DefinicionMapa asignada.", this);
                return;
            }

            float inicio = Time.realtimeSinceStartup;

            Mapa = GeneradorTerreno.Generar(definicion);
            Grilla = new GrillaMapa(Mapa.Ancho, Mapa.Alto);

            for (int x = 0; x < Mapa.Ancho; x++)
            {
                for (int y = 0; y < Mapa.Alto; y++)
                {
                    Grilla[x, y] = new Celda
                    {
                        Suelo = Mapa.Tierra[x, y],
                        Nivel = Mapa.Nivel != null ? Mapa.Nivel[x, y] : (byte)0,
                        Escalera = Mapa.Escalera != null && Mapa.Escalera[x, y],
                        Obstaculo = false
                    };
                }
            }

            // La fila que queda justo al sur de una meseta lleva pintada la pared del
            // acantilado, así que no se puede pisar aunque el terreno de debajo sea llano.
            // Las rampas son la excepción: ahí la pared se sustituye por la cuesta.
            if (Mapa.Nivel != null)
            {
                for (int x = 0; x < Mapa.Ancho; x++)
                {
                    for (int y = 0; y < Mapa.Alto - 1; y++)
                    {
                        if (!EsParedDeAcantilado(x, y)) continue;

                        var celda = Grilla[x, y];
                        celda.Obstaculo = true;
                        Grilla[x, y] = celda;
                    }
                }
            }

            // Lo que estorba el paso. Las ovejas no cuentan: se mueven solas y
            // bloquear celdas con algo que cambia de sitio ensuciaría el pathfinding.
            foreach (var c in Mapa.Arboles) Grilla.MarcarObstaculo(c, radioArbol);
            foreach (var c in Mapa.Oro) Grilla.MarcarObstaculo(c, radioOro);
            foreach (var c in Mapa.Bases) Grilla.MarcarObstaculo(c, radioCastillo);

            Rutas = new BuscadorDeRutas(Grilla);

            float ms = (Time.realtimeSinceStartup - inicio) * 1000f;
            Debug.Log(
                $"[Tiny Tactics] Grilla lista: {Mapa.Ancho}x{Mapa.Alto}, " +
                $"{Grilla.ContarTransitables()} celdas transitables de {Mapa.Ancho * Mapa.Alto} " +
                $"({ms:F0} ms).");
        }

        /// <summary>
        /// La fila justo al sur de una meseta lleva pintada la pared del acantilado, así
        /// que no se puede pisar aunque el terreno de debajo sea llano. Las rampas son la
        /// excepción: ahí la pared se sustituye por la cuesta.
        /// </summary>
        bool EsParedDeAcantilado(int x, int y)
        {
            if (Mapa.Nivel == null || y + 1 >= Mapa.Alto) return false;
            if (Mapa.Nivel[x, y] != 0 || Mapa.Nivel[x, y + 1] == 0) return false;

            return Mapa.Escalera == null || !Mapa.Escalera[x, y];
        }

        /// <summary>
        /// Devuelve al mapa el terreno de un recurso agotado.
        /// </summary>
        /// <remarks>
        /// No basta con apagar el disco que ocupaba: los bolsones de recursos van apretados
        /// y dos árboles vecinos comparten celdas, así que borrar sin más abriría un
        /// pasillo por debajo del árbol de al lado. Y borrar a ciegas también tumbaría las
        /// paredes de acantilado, que viven en el mismo campo.
        ///
        /// Por eso se <b>recalcula</b> una ventana alrededor: se limpia, se vuelven a poner
        /// los acantilados, y se remarcan los nodos que siguen vivos y las bases. Cuesta un
        /// puñado de celdas y es correcto por construcción, que en pathfinding vale más que
        /// ser rápido: una celda mal abierta manda unidades a atravesar un bosque.
        /// </remarks>
        public void LiberarRecurso(Vector2Int centro, float radio)
        {
            if (Grilla == null || Mapa == null) return;

            float mayor = Mathf.Max(radioCastillo, Mathf.Max(radioArbol, radioOro));
            int margen = Mathf.CeilToInt(radio + mayor) + 1;

            int minX = Mathf.Max(0, centro.x - margen);
            int maxX = Mathf.Min(Mapa.Ancho - 1, centro.x + margen);
            int minY = Mathf.Max(0, centro.y - margen);
            int maxY = Mathf.Min(Mapa.Alto - 1, centro.y + margen);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var celda = Grilla[x, y];
                    celda.Obstaculo = EsParedDeAcantilado(x, y);
                    Grilla[x, y] = celda;
                }
            }

            // Los nodos vivos son la fuente de verdad de lo que estorba: la lista original
            // del generador incluye los que ya se han talado.
            var nodos = NodoRecurso.Todos;

            for (int i = 0; i < nodos.Count; i++)
            {
                var nodo = nodos[i];
                if (nodo == null || nodo.Agotado || nodo.radioBloqueo <= 0f) continue;

                if (nodo.celda.x < minX - margen || nodo.celda.x > maxX + margen ||
                    nodo.celda.y < minY - margen || nodo.celda.y > maxY + margen) continue;

                Grilla.MarcarObstaculo(nodo.celda, nodo.radioBloqueo);
            }

            foreach (var c in Mapa.Bases) Grilla.MarcarObstaculo(c, radioCastillo);
        }

        /// <summary>Celda transitable más cercana a un punto del mundo.</summary>
        public bool CeldaCercana(Vector3 mundo, out Vector2Int celda)
        {
            if (Grilla == null)
            {
                celda = default;
                return false;
            }
            return Grilla.CeldaTransitableCercana(Grilla.MundoACelda(mundo), 12, out celda);
        }

        void OnDrawGizmosSelected()
        {
            if (!mostrarGrilla || Grilla == null) return;

            // Solo alrededor de la cámara: pintar 50 000 cubos congela el editor.
            var camara = Camera.main;
            Vector2Int centro = camara != null
                ? Grilla.MundoACelda(camara.transform.position)
                : new Vector2Int(Grilla.Ancho / 2, Grilla.Alto / 2);

            for (int x = centro.x - radioGizmo; x <= centro.x + radioGizmo; x++)
            {
                for (int y = centro.y - radioGizmo; y <= centro.y + radioGizmo; y++)
                {
                    if (!Grilla.EnRango(x, y)) continue;

                    var celda = Grilla[x, y];
                    if (celda.Transitable && !celda.Escalera && celda.Nivel == 0) continue;

                    // Mientras el terreno elevado no esté pintado (HU-015), este gizmo es
                    // la única forma de ver dónde hay meseta y por dónde se sube.
                    Gizmos.color = celda.Escalera
                        ? new Color(0.2f, 0.8f, 1f, 0.55f)
                        : celda.Nivel > 0 && celda.Transitable
                            ? new Color(0.55f, 0.4f, 0.25f, 0.30f) // meseta
                            : celda.Suelo
                                ? new Color(1f, 0.45f, 0.1f, 0.45f)   // obstáculo
                                : new Color(0.1f, 0.3f, 0.9f, 0.25f); // agua

                    Gizmos.DrawCube(Grilla.CeldaAMundo(x, y), new Vector3(0.9f, 0.9f, 0.01f));
                }
            }
        }
    }
}
