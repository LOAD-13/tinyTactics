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
        public float radioArbol = 1.1f;
        public float radioOro = 1.0f;
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
                        if (Mapa.Nivel[x, y] != 0 || Mapa.Nivel[x, y + 1] == 0) continue;
                        if (Mapa.Escalera != null && Mapa.Escalera[x, y]) continue;

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
