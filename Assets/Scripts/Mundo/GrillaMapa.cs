using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>Estado de una celda del mapa para la simulación.</summary>
    public struct Celda
    {
        /// <summary>Hay suelo firme. El agua no lo es.</summary>
        public bool Suelo;

        /// <summary>Altura del terreno. 0 = llano, 1 en adelante = mesetas.</summary>
        public byte Nivel;

        /// <summary>Rampa que permite cambiar de nivel.</summary>
        public bool Escalera;

        /// <summary>Ocupada por algo que estorba: árbol, veta de oro, edificio.</summary>
        public bool Obstaculo;

        /// <summary>Se puede caminar por aquí.</summary>
        public bool Transitable => Suelo && !Obstaculo;
    }

    /// <summary>
    /// Representación lógica del mapa para pathfinding y ocupación.
    ///
    /// Se construye a partir de <b>la misma máscara</b> que pinta el tilemap, nunca
    /// leyendo los tiles ni por separado. Mantener dos representaciones del terreno es
    /// el error clásico del género: acaban desincronizándose y aparecen unidades
    /// caminando sobre el agua (ver ADR-09).
    ///
    /// Coordenadas: la celda (x, y) ocupa el mundo desde (x, y) hasta (x+1, y+1),
    /// y su centro está en (x+0.5, y+0.5). Coincide con un Grid de Unity de celda 1.
    /// </summary>
    public class GrillaMapa
    {
        public readonly int Ancho;
        public readonly int Alto;

        readonly Celda[] _celdas;

        public GrillaMapa(int ancho, int alto)
        {
            Ancho = ancho;
            Alto = alto;
            _celdas = new Celda[ancho * alto];
        }

        public bool EnRango(int x, int y) => x >= 0 && y >= 0 && x < Ancho && y < Alto;

        public Celda this[int x, int y]
        {
            get => EnRango(x, y) ? _celdas[x + y * Ancho] : default;
            set { if (EnRango(x, y)) _celdas[x + y * Ancho] = value; }
        }

        public bool Transitable(int x, int y) => EnRango(x, y) && _celdas[x + y * Ancho].Transitable;

        public byte NivelDe(int x, int y) => EnRango(x, y) ? _celdas[x + y * Ancho].Nivel : (byte)0;

        // -----------------------------------------------------------------
        // Conversión mundo ↔ celda
        // -----------------------------------------------------------------

        public Vector2Int MundoACelda(Vector3 mundo) =>
            new Vector2Int(Mathf.FloorToInt(mundo.x), Mathf.FloorToInt(mundo.y));

        public Vector3 CeldaAMundo(Vector2Int celda) =>
            new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f);

        public Vector3 CeldaAMundo(int x, int y) => new Vector3(x + 0.5f, y + 0.5f, 0f);

        // -----------------------------------------------------------------
        // Reglas de tránsito
        // -----------------------------------------------------------------

        /// <summary>
        /// ¿Se puede pasar de una celda a otra <b>adyacente</b>?
        ///
        /// Dos reglas más allá de la transitabilidad:
        ///
        /// <b>Niveles.</b> Solo se cambia de nivel si alguna de las dos celdas es escalera.
        /// Es lo que convierte un acantilado en un muro y una rampa en un cuello de botella:
        /// sin esto, los desniveles serían decoración y las unidades treparían paredes.
        ///
        /// <b>Esquinas.</b> En diagonal, ambas celdas ortogonales intermedias deben ser
        /// transitables. Si no, las unidades se cuelan por la esquina entre dos árboles.
        /// </summary>
        public bool PuedePasar(int desdeX, int desdeY, int haciaX, int haciaY)
        {
            if (!Transitable(haciaX, haciaY)) return false;
            if (!Transitable(desdeX, desdeY)) return false;

            int dx = haciaX - desdeX;
            int dy = haciaY - desdeY;

            if (dx == 0 && dy == 0) return true;
            if (Mathf.Abs(dx) > 1 || Mathf.Abs(dy) > 1) return false;

            var origen = this[desdeX, desdeY];
            var destino = this[haciaX, haciaY];

            if (origen.Nivel != destino.Nivel)
            {
                // Un salto de más de un nivel no se sube ni por escalera.
                if (Mathf.Abs(origen.Nivel - destino.Nivel) > 1) return false;
                if (!origen.Escalera && !destino.Escalera) return false;

                // Cambiar de nivel en diagonal se ve mal y complica el render de rampas.
                if (dx != 0 && dy != 0) return false;
            }

            if (dx != 0 && dy != 0)
            {
                if (!Transitable(desdeX + dx, desdeY)) return false;
                if (!Transitable(desdeX, desdeY + dy)) return false;
            }

            return true;
        }

        /// <summary>
        /// Celda transitable más cercana a la dada, en anillos crecientes.
        /// Sirve cuando el jugador ordena moverse encima de un árbol o del agua:
        /// en vez de rechazar la orden, la unidad va a lo más cerca posible.
        /// </summary>
        public bool CeldaTransitableCercana(Vector2Int origen, int radioMaximo, out Vector2Int salida)
        {
            if (Transitable(origen.x, origen.y))
            {
                salida = origen;
                return true;
            }

            for (int r = 1; r <= radioMaximo; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        // Solo el perímetro del anillo: el interior ya se revisó.
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;

                        int x = origen.x + dx, y = origen.y + dy;
                        if (Transitable(x, y))
                        {
                            salida = new Vector2Int(x, y);
                            return true;
                        }
                    }
                }
            }

            salida = origen;
            return false;
        }

        // -----------------------------------------------------------------
        // Construcción
        // -----------------------------------------------------------------

        /// <summary>Marca un disco de celdas como obstáculo. Lo usan árboles, oro y edificios.</summary>
        public void MarcarObstaculo(Vector2Int centro, float radio)
        {
            int r = Mathf.CeilToInt(radio);
            float r2 = radio * radio;

            for (int x = centro.x - r; x <= centro.x + r; x++)
            {
                for (int y = centro.y - r; y <= centro.y + r; y++)
                {
                    if (!EnRango(x, y)) continue;

                    float dx = x - centro.x, dy = y - centro.y;
                    if (dx * dx + dy * dy > r2) continue;

                    var celda = this[x, y];
                    celda.Obstaculo = true;
                    this[x, y] = celda;
                }
            }
        }

        public int ContarTransitables()
        {
            int n = 0;
            for (int i = 0; i < _celdas.Length; i++)
                if (_celdas[i].Transitable) n++;
            return n;
        }
    }
}
