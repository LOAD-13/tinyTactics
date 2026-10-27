using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Dónde viven los mapas en disco, y cómo se validan antes de jugarlos.
    /// </summary>
    /// <remarks>
    /// Dos carpetas. Los mapas <b>oficiales</b> viajan con el juego en <c>StreamingAssets</c>:
    /// cuando el equipo dibuja un mapa que vale, se copia ahí y pasa a estar en la lista de
    /// todos. Los <b>tuyos</b> se guardan en la carpeta de datos del usuario, que sobrevive a
    /// reinstalar el juego y no necesita permisos de escritura en la carpeta del programa.
    /// </remarks>
    public static class ArchivoDeMapas
    {
        public static string CarpetaOficial => Path.Combine(Application.streamingAssetsPath, "Mapas");
        public static string CarpetaPropia => Path.Combine(Application.persistentDataPath, "Mapas");

        public struct Entrada
        {
            public string ruta;
            public bool oficial;
            public MapaGuardado mapa;
        }

        public static List<Entrada> Listar()
        {
            var lista = new List<Entrada>();
            Leer(CarpetaOficial, true, lista);
            Leer(CarpetaPropia, false, lista);
            return lista;
        }

        static void Leer(string carpeta, bool oficial, List<Entrada> lista)
        {
            if (!Directory.Exists(carpeta)) return;

            foreach (var ruta in Directory.GetFiles(carpeta, "*.json"))
            {
                try
                {
                    var m = MapaGuardado.DeJson(File.ReadAllText(ruta));
                    if (m != null) lista.Add(new Entrada { ruta = ruta, oficial = oficial, mapa = m });
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Tiny Tactics] No pude leer el mapa {ruta}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Borra un mapa tuyo. Solo de la carpeta propia: un oficial no se puede borrar
        /// desde el juego, aunque alguien pase su ruta.
        /// </summary>
        public static bool Borrar(string ruta)
        {
            try
            {
                if (string.IsNullOrEmpty(ruta)) return false;
                var carpeta = Path.GetFullPath(CarpetaPropia);
                var completa = Path.GetFullPath(ruta);
                if (!completa.StartsWith(carpeta, System.StringComparison.OrdinalIgnoreCase) || !File.Exists(completa)) return false;
                File.Delete(completa);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tiny Tactics] No pude borrar el mapa {ruta}: {e.Message}");
                return false;
            }
        }

        /// <summary>El prado del tutorial, o null si falta el archivo.</summary>
        public static MapaGuardado LeerTutorial()
        {
            try
            {
                var ruta = Path.Combine(Application.streamingAssetsPath, "Tutorial", "Tutorial.json");
                return File.Exists(ruta) ? MapaGuardado.DeJson(File.ReadAllText(ruta)) : null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tiny Tactics] No pude leer el tutorial: {e.Message}");
                return null;
            }
        }

        /// <summary>Guarda en la carpeta propia. Devuelve la ruta, o null si falló.</summary>
        public static string Guardar(MapaGuardado m)
        {
            try
            {
                Directory.CreateDirectory(CarpetaPropia);
                string ruta = Path.Combine(CarpetaPropia, NombreDeArchivo(m.nombre) + ".json");
                File.WriteAllText(ruta, m.AJson());
                return ruta;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tiny Tactics] No pude guardar el mapa: {e.Message}");
                return null;
            }
        }

        public static string NombreDeArchivo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) nombre = "mapa";
            foreach (char c in Path.GetInvalidFileNameChars()) nombre = nombre.Replace(c, '_');
            return nombre.Trim().Replace(' ', '_');
        }
    }

    /// <summary>
    /// Las reglas que un mapa tiene que cumplir para poder jugarse. Funciones puras.
    /// </summary>
    public static class ReglasDeMapa
    {
        public const int BasesMinimas = 2, BasesMaximas = 5;

        /// <summary>
        /// ¿Se puede jugar? Entre dos y cinco bases, todas en tierra, y todas conectadas.
        /// </summary>
        /// <remarks>
        /// La conexión se comprueba andando: desde la primera base se recorre la tierra,
        /// cambiando de altura solo por escaleras y de una en una —la misma regla que la
        /// grilla del juego—. Una base a la que no se puede llegar es una partida que no
        /// termina nunca, y eso se dice al guardar, no a los veinte minutos de jugarla.
        /// </remarks>
        public static bool EsJugable(MapaGuardado m, out string motivo)
        {
            if (m.bases.Count < BasesMinimas) { motivo = $"Faltan puntos de inicio: hay {m.bases.Count}, el mínimo es {BasesMinimas}."; return false; }
            if (m.bases.Count > BasesMaximas) { motivo = $"Sobran puntos de inicio: el máximo es {BasesMaximas}."; return false; }

            foreach (var b in m.bases)
                if (m.TerrenoEn(b.x, b.y) == 0) { motivo = "Hay un punto de inicio en el agua."; return false; }

            var alcanzadas = Alcanzables(m, m.bases[0]);
            for (int i = 1; i < m.bases.Count; i++)
                if (!alcanzadas.Contains(m.bases[i]))
                {
                    motivo = $"La base {i + 1} no está conectada por tierra con la base 1.";
                    return false;
                }

            motivo = "";
            return true;
        }

        public static HashSet<Vector2Int> Alcanzables(MapaGuardado m, Vector2Int desde)
        {
            var vistas = new HashSet<Vector2Int>();
            if (m.TerrenoEn(desde.x, desde.y) == 0) return vistas;

            var escaleras = new HashSet<Vector2Int>();
            foreach (var r in m.rampas) { escaleras.Add(new Vector2Int(r.x, r.y)); escaleras.Add(new Vector2Int(r.x, r.y + 1)); }

            var cola = new Queue<Vector2Int>();
            cola.Enqueue(desde);
            vistas.Add(desde);

            var pasos = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (cola.Count > 0)
            {
                var c = cola.Dequeue();
                foreach (var p in pasos)
                {
                    var n = c + p;
                    if (vistas.Contains(n) || m.TerrenoEn(n.x, n.y) == 0) continue;

                    int dn = m.NivelEn(n.x, n.y) - m.NivelEn(c.x, c.y);
                    if (dn != 0 && (System.Math.Abs(dn) > 1 || (!escaleras.Contains(c) && !escaleras.Contains(n)))) continue;

                    vistas.Add(n);
                    cola.Enqueue(n);
                }
            }

            return vistas;
        }

        /// <summary>
        /// Zonas altas a las que nadie puede subir: mesetas sin escalera desde la base 1.
        /// </summary>
        /// <remarks>
        /// No impiden jugar —el mapa se juega igual—, pero casi nunca son a propósito: la isla
        /// central de Cuatro Vientos se publicó así en la semana 13. Por eso se avisa al
        /// guardar en vez de bloquear. Se ignoran las de menos de <paramref name="tamanoMinimo"/>
        /// celdas, que son restos de pincel.
        /// </remarks>
        public static int ZonasAltasSinAcceso(MapaGuardado m, int tamanoMinimo = 6)
        {
            if (m.bases.Count == 0) return 0;
            var alcanzadas = Alcanzables(m, m.bases[0]);
            var vistas = new HashSet<Vector2Int>();
            var cola = new Queue<Vector2Int>();
            var pasos = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            int zonas = 0;

            for (int x = 0; x < m.ancho; x++)
            for (int y = 0; y < m.alto; y++)
            {
                var c = new Vector2Int(x, y);
                if (m.TerrenoEn(x, y) == 0 || m.NivelEn(x, y) == 0 || alcanzadas.Contains(c) || !vistas.Add(c)) continue;

                // Una zona: celdas altas, sin alcanzar, del mismo nivel y pegadas.
                int nivel = m.NivelEn(x, y), tamano = 0;
                cola.Enqueue(c);
                while (cola.Count > 0)
                {
                    var a = cola.Dequeue();
                    tamano++;
                    foreach (var p in pasos)
                    {
                        var n = a + p;
                        if (vistas.Contains(n) || m.TerrenoEn(n.x, n.y) == 0 || m.NivelEn(n.x, n.y) != nivel || alcanzadas.Contains(n)) continue;
                        vistas.Add(n);
                        cola.Enqueue(n);
                    }
                }
                if (tamano >= tamanoMinimo) zonas++;
            }

            return zonas;
        }

        /// <summary>¿Se puede poner una escalera con el pie en esta celda?</summary>
        public static bool CabeEscalera(MapaGuardado m, int x, int y) =>
            m.TerrenoEn(x, y) > 0 && m.TerrenoEn(x, y + 1) > 0 &&
            m.NivelEn(x, y + 1) == m.NivelEn(x, y) + 1;
    }
}
