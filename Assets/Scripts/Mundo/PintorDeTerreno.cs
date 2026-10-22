using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Pinta un <see cref="MapaGuardado"/> en los tilemaps de la escena, entero o por zonas.
    /// </summary>
    /// <remarks>
    /// Es el pintado del generador de la semana 03 llevado a la partida, con dos cosas que
    /// aquel no necesitaba:
    ///
    /// <b>Por zonas.</b> Un pincel no puede repintar 50 000 celdas cada vez que toca una. Se
    /// repinta la zona tocada <i>más un borde</i>: al poner tierra cambian las piezas de las
    /// ocho vecinas (su borde de costa), y al subir una meseta cambia la pared de la fila de
    /// debajo.
    ///
    /// <b>Varios niveles.</b> Una capa de meseta por altura, cada una encima de la anterior.
    /// En cada capa, una celda está «arriba» si su altura llega a ese nivel; la pared se pinta
    /// en la celda de debajo cuando esta queda por debajo. Así una meseta sobre otra se dibuja
    /// con las mismas piezas que una sobre el llano.
    ///
    /// La tierra de cada celda usa el tileset de su color: el pack trae cinco, y mezclarlos es
    /// lo que da variedad a un mapa hecho a mano.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Pintor de terreno")]
    public class PintorDeTerreno : MonoBehaviour
    {
        /// <summary>Alturas por encima del llano: dos, o sea tres en total contando el llano.</summary>
        public const int NivelesMaximos = 2;

        public PaletaDeTerreno paleta;

        Tilemap _agua, _espuma, _suelo;
        readonly Tilemap[] _mesetas = new Tilemap[NivelesMaximos + 1];
        readonly HashSet<Vector2Int> _rampas = new HashSet<Vector2Int>();
        readonly Dictionary<Vector2Int, Rampa> _rampaEnCelda = new Dictionary<Vector2Int, Rampa>();

        public static PintorDeTerreno Actual { get; private set; }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        void Preparar()
        {
            if (_suelo != null) return;

            if (GetComponent<Grid>() == null) gameObject.AddComponent<Grid>().cellSize = new Vector3(1f, 1f, 0f);

            _agua = Capa("Agua", -30);
            _espuma = Capa("Espuma", -20);
            _suelo = Capa("Suelo", -10);
            _mesetas[1] = Capa("Meseta", -5);
            for (int n = 2; n <= NivelesMaximos; n++) _mesetas[n] = Capa($"Meseta{n}", -5 + n);
        }

        Tilemap Capa(string nombre, int orden)
        {
            var hijo = transform.Find(nombre);
            if (hijo != null && hijo.TryGetComponent<Tilemap>(out var existente)) return existente;

            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var t = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = orden;
            return t;
        }

        /// <summary>
        /// Mar alrededor del mapa, solo de dibujo: el editor deja alejar la cámara más allá
        /// del borde, y ahí tiene que verse agua y no el vacío.
        /// </summary>
        public void PintarMarco(MapaGuardado m, int margen)
        {
            Preparar();
            if (paleta == null || margen <= 0) return;

            for (int x = -margen; x < m.ancho + margen; x++)
                for (int y = -margen; y < m.alto + margen; y++)
                    if (!m.Dentro(x, y)) _agua.SetTile(new Vector3Int(x, y, 0), paleta.agua);
        }

        /// <summary>Borra todas las capas.</summary>
        public void Limpiar()
        {
            Preparar();
            _agua.ClearAllTiles();
            _espuma.ClearAllTiles();
            _suelo.ClearAllTiles();
            for (int n = 1; n <= NivelesMaximos; n++) _mesetas[n].ClearAllTiles();
        }

        public void Pintar(MapaGuardado m)
        {
            Limpiar();
            Repintar(m, new RectInt(0, 0, m.ancho, m.alto));
        }

        /// <summary>Repinta una zona y su borde (las piezas vecinas dependen de ella).</summary>
        public void Repintar(MapaGuardado m, RectInt zona)
        {
            Preparar();
            if (paleta == null) return;

            IndexarRampas(m);

            int x0 = Mathf.Max(0, zona.xMin - 1), y0 = Mathf.Max(0, zona.yMin - 2);
            int x1 = Mathf.Min(m.ancho - 1, zona.xMax), y1 = Mathf.Min(m.alto - 1, zona.yMax);

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    PintarCelda(m, x, y);
        }

        void IndexarRampas(MapaGuardado m)
        {
            _rampas.Clear();
            _rampaEnCelda.Clear();

            foreach (var r in m.rampas)
            {
                _rampas.Add(new Vector2Int(r.x, r.y));
                _rampaEnCelda[new Vector2Int(r.x, r.y)] = r;
                _rampaEnCelda[new Vector2Int(r.x, r.y + 1)] = r;
            }
        }

        void PintarCelda(MapaGuardado m, int x, int y)
        {
            var p = new Vector3Int(x, y, 0);
            byte color = m.TerrenoEn(x, y);
            bool tierra = color > 0;

            _agua.SetTile(p, paleta.agua);

            if (!tierra)
            {
                _suelo.SetTile(p, null);
                _espuma.SetTile(p, null);
            }
            else
            {
                int indice = GeneradorTerreno.IndiceAutotile(
                    m.TerrenoEn(x, y + 1) > 0, m.TerrenoEn(x, y - 1) > 0,
                    m.TerrenoEn(x + 1, y) > 0, m.TerrenoEn(x - 1, y) > 0);

                _suelo.SetTile(p, paleta.Pieza(color, indice));
                _espuma.SetTile(p, VecinosDeTierra(m, x, y) < 8 ? paleta.espuma : null);
            }

            byte propio = tierra ? m.NivelEn(x, y) : (byte)0;

            for (int n = 1; n <= NivelesMaximos; n++)
                _mesetas[n].SetTile(p, PiezaDeNivel(m, x, y, n, propio, tierra));
        }

        TileBase PiezaDeNivel(MapaGuardado m, int x, int y, int n, byte propio, bool tierra)
        {
            var celda = new Vector2Int(x, y);

            // La rampa sustituye a la pared y al borde de meseta en sus dos celdas.
            if (_rampaEnCelda.TryGetValue(celda, out var r) && NivelDeRampa(m, r) == n)
            {
                bool arriba = y == r.y + 1;
                int color = Mathf.Max(1, m.TerrenoEn(r.x, r.y + 1));
                int indice = arriba
                    ? (r.derecha ? GeneradorTerreno.RampaAltaDer : GeneradorTerreno.RampaAltaIzq)
                    : (r.derecha ? GeneradorTerreno.RampaBajaDer : GeneradorTerreno.RampaBajaIzq);
                return paleta.Pieza(color, indice);
            }

            if (propio >= n)
            {
                int indice = GeneradorTerreno.IndiceAutotile(
                    Alto(m, x, y + 1, n), Alto(m, x, y - 1, n), Alto(m, x + 1, y, n), Alto(m, x - 1, y, n))
                    + GeneradorTerreno.DesplazamientoElevado;
                return paleta.Pieza(m.TerrenoEn(x, y), indice);
            }

            // La pared de este nivel va en la celda de debajo de su borde sur.
            if (!EsMuro(m, x, y, n)) return null;

            var indices = tierra ? GeneradorTerreno.IndicesCaraEnTierra : GeneradorTerreno.IndicesCaraEnAgua;
            int rol = GeneradorTerreno.RolDeMuro(EsMuro(m, x - 1, y, n), EsMuro(m, x + 1, y, n));
            return paleta.Pieza(Mathf.Max(1, m.TerrenoEn(x, y + 1)), indices[rol]);
        }

        static bool Alto(MapaGuardado m, int x, int y, int n) => m.TerrenoEn(x, y) > 0 && m.NivelEn(x, y) >= n;

        bool EsMuro(MapaGuardado m, int x, int y, int n)
        {
            if (!m.Dentro(x, y) || !m.Dentro(x, y + 1)) return false;
            if (Alto(m, x, y, n) || !Alto(m, x, y + 1, n)) return false;
            return !_rampas.Contains(new Vector2Int(x, y));
        }

        /// <summary>El nivel al que sube una rampa: el de su celda alta.</summary>
        static int NivelDeRampa(MapaGuardado m, Rampa r) => Mathf.Max(1, (int)m.NivelEn(r.x, r.y + 1));

        static int VecinosDeTierra(MapaGuardado m, int x, int y)
        {
            int n = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && m.TerrenoEn(x + dx, y + dy) > 0) n++;
            return n;
        }
    }
}
