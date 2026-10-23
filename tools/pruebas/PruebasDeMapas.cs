// Pruebas del mapa como dato y del editor de mapas (semana 13).
//
// Lo que se comprueba es lo que decide si un mapa dibujado a mano se juega bien: que se
// convierte sin perder nada en el mapa que ya entiende el juego, y que las reglas de
// «se puede jugar» detectan lo que dejaria una partida rota.
using System.Text.RegularExpressions;
using UnityEngine;
using TinyTactics.Mundo;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeMapas
    {
        static MapaGuardado Isla(int lado, int x0, int y0, int x1, int y1)
        {
            var m = MapaGuardado.EnBlanco(lado, lado, "Prueba");
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    m.PonerTerreno(x, y, 1);
            return m;
        }

        public static void Registrar()
        {
            Arranque.Registrar("mapa: uno nuevo es todo mar y de su tamaño", v =>
            {
                var m = MapaGuardado.EnBlanco(96, 128, "Nuevo");
                v.Igual(m.Terreno.Length, 96 * 128, "una celda por casilla");
                bool todoMar = true;
                foreach (var t in m.Terreno) if (t != 0) todoMar = false;
                v.Cierto(todoMar, "todo agua");
                v.Igual(MapaGuardado.EnBlanco(10, 9999, "x").alto, 320, "el tamaño se limita");
            });

            Arranque.Registrar("mapa: se convierte en el mapa del juego sin perder nada", v =>
            {
                var m = Isla(64, 10, 10, 40, 40);
                m.PonerNivel(20, 20, 1);
                m.PonerNivel(5, 5, 2);           // en el agua: no puede tener altura
                m.rampas.Add(new Rampa(20, 19, true));
                m.objetos.Add(new ObjetoDeMapa(SemillaMapa.Arbol, 12, 12, 1));
                m.objetos.Add(new ObjetoDeMapa(SemillaMapa.Oro, 30, 30, 0));
                m.criaderos.Add(new Vector2Int(15, 30));
                m.bases.Add(new Vector2Int(12, 35));

                var g = m.AMapaGenerado();
                v.Cierto(g.Tierra[10, 10] && g.Tierra[40, 40] && !g.Tierra[9, 10], "la tierra donde se pinto");
                v.Igual(g.Nivel[20, 20], 1, "la altura se conserva");
                v.Igual(g.Nivel[5, 5], 0, "el agua nunca tiene altura");
                v.Cierto(g.Escalera[20, 19] && g.Escalera[20, 20], "la escalera ocupa sus dos celdas");
                v.Igual(g.Arboles.Count, 1, "el arbol");
                v.Igual(g.Oro.Count, 1, "la mena");
                v.Cierto(g.Ovejas.Contains(new Vector2Int(15, 30)), "el criadero");
                v.Igual(g.Bases.Count, 1, "la base");
            });

            Arranque.Registrar("mapa: ida y vuelta con el mapa del generador", v =>
            {
                var m = Isla(48, 5, 5, 30, 30);
                m.bases.Add(new Vector2Int(8, 8));
                m.bases.Add(new Vector2Int(28, 28));
                var vuelta = MapaGuardado.DeMapaGenerado(m.AMapaGenerado(), "Vuelta", "");
                int iguales = 0;
                for (int x = 0; x < 48; x++)
                    for (int y = 0; y < 48; y++)
                        if ((vuelta.TerrenoEn(x, y) > 0) == (m.TerrenoEn(x, y) > 0)) iguales++;
                v.Igual(iguales, 48 * 48, "la misma tierra celda a celda");
                v.Igual(vuelta.bases.Count, 2, "las mismas bases");
            });

            Arranque.Registrar("mapa: hace falta entre dos y cinco bases, en tierra", v =>
            {
                var m = Isla(64, 5, 5, 58, 58);
                v.Falso(ReglasDeMapa.EsJugable(m, out _), "sin bases no");
                m.bases.Add(new Vector2Int(10, 10));
                v.Falso(ReglasDeMapa.EsJugable(m, out _), "con una no");
                m.bases.Add(new Vector2Int(50, 50));
                v.Cierto(ReglasDeMapa.EsJugable(m, out _), "con dos conectadas si");
                for (int i = 0; i < 4; i++) m.bases.Add(new Vector2Int(20 + i * 5, 20));
                v.Falso(ReglasDeMapa.EsJugable(m, out var motivo), "con seis no");
                v.Cierto(motivo.Length > 0, "y dice por que");

                var agua = Isla(64, 5, 5, 30, 30);
                agua.bases.Add(new Vector2Int(10, 10));
                agua.bases.Add(new Vector2Int(50, 50));
                v.Falso(ReglasDeMapa.EsJugable(agua, out _), "una base en el agua no");
            });

            Arranque.Registrar("mapa: dos islas separadas no se pueden jugar", v =>
            {
                var m = Isla(64, 2, 2, 20, 20);
                for (int x = 40; x <= 60; x++) for (int y = 40; y <= 60; y++) m.PonerTerreno(x, y, 3);
                m.bases.Add(new Vector2Int(10, 10));
                m.bases.Add(new Vector2Int(50, 50));
                v.Falso(ReglasDeMapa.EsJugable(m, out _), "separadas por el mar");

                for (int x = 20; x <= 40; x++) m.PonerTerreno(x, 30, 2);
                for (int y = 20; y <= 40; y++) { m.PonerTerreno(20, y, 2); m.PonerTerreno(40, y, 2); }
                v.Cierto(ReglasDeMapa.EsJugable(m, out _), "con un puente de tierra, si");
            });

            Arranque.Registrar("mapa: a una meseta solo se sube por escalera", v =>
            {
                var m = Isla(64, 5, 5, 58, 58);
                for (int x = 30; x <= 50; x++) for (int y = 30; y <= 50; y++) m.PonerNivel(x, y, 1);
                m.bases.Add(new Vector2Int(10, 10));
                m.bases.Add(new Vector2Int(40, 40));
                v.Falso(ReglasDeMapa.EsJugable(m, out _), "sin escalera la base de arriba queda aislada");

                v.Cierto(ReglasDeMapa.CabeEscalera(m, 35, 29), "la escalera cabe al pie del escalon");
                v.Falso(ReglasDeMapa.CabeEscalera(m, 35, 20), "en el llano no");
                m.rampas.Add(new Rampa(35, 29, true));
                v.Cierto(ReglasDeMapa.EsJugable(m, out _), "con escalera, si");
            });

            Arranque.Registrar("mapa: el nombre de archivo nunca rompe la ruta", v =>
            {
                v.Igual(ArchivoDeMapas.NombreDeArchivo("Cuatro Vientos"), "Cuatro_Vientos", "espacios");
                v.Igual(ArchivoDeMapas.NombreDeArchivo("  "), "mapa", "vacio");
                v.Cierto(!ArchivoDeMapas.NombreDeArchivo("a/b:c").Contains("/"), "sin barras");
            });

            // Semana 14: la isla central de Cuatro Vientos se publico sin escalera.
            Arranque.Registrar("mapa: avisa de las zonas altas sin escalera", v =>
            {
                var m = Isla(64, 5, 5, 58, 58);
                for (int x = 30; x <= 50; x++) for (int y = 30; y <= 50; y++) m.PonerNivel(x, y, 1);
                m.bases.Add(new Vector2Int(10, 10));
                m.bases.Add(new Vector2Int(12, 50));
                v.Cierto(ReglasDeMapa.EsJugable(m, out _), "se puede jugar igual");
                v.Igual(ReglasDeMapa.ZonasAltasSinAcceso(m), 1, "pero la meseta no tiene subida");

                m.PonerNivel(8, 40, 1);
                v.Igual(ReglasDeMapa.ZonasAltasSinAcceso(m), 1, "un resto de pincel no cuenta");

                m.rampas.Add(new Rampa(35, 29, true));
                v.Igual(ReglasDeMapa.ZonasAltasSinAcceso(m), 0, "con escalera, ninguna");
            });

            Arranque.Registrar("mapa: en los mapas oficiales se sube a todas partes", v =>
            {
                var carpeta = CarpetaOficial();
                v.Cierto(carpeta != null, "se encuentra la carpeta de mapas oficiales");
                if (carpeta == null) return;
                foreach (var ruta in System.IO.Directory.GetFiles(carpeta, "*.json"))
                {
                    var m = LeerSinUnity(System.IO.File.ReadAllText(ruta));
                    var nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
                    v.Cierto(ReglasDeMapa.EsJugable(m, out var motivo), nombre + " se puede jugar " + motivo);
                    v.Igual(ReglasDeMapa.ZonasAltasSinAcceso(m), 0, nombre + " sin zonas altas aisladas");
                }
            });
        }

        /// <summary>
        /// Lo justo del JSON para las reglas de alturas. <c>JsonUtility</c> es codigo nativo de
        /// Unity y fuera del motor no existe.
        /// </summary>
        static MapaGuardado LeerSinUnity(string json)
        {
            string Texto(string clave) =>
                Regex.Match(json, "\"" + clave + @"""\s*:\s*""([^""]*)""").Groups[1].Value;
            int Numero(string clave) =>
                int.Parse(Regex.Match(json, "\"" + clave + @"""\s*:\s*(\d+)").Groups[1].Value);
            string Lista(string clave) =>
                Regex.Match(json, "\"" + clave + @"""\s*:\s*\[(.*?)\]", RegexOptions.Singleline).Groups[1].Value;

            var m = new MapaGuardado { ancho = Numero("ancho"), alto = Numero("alto"), terreno = Texto("terreno"), nivel = Texto("nivel") };
            foreach (Match r in Regex.Matches(Lista("rampas"), @"""x""\s*:\s*(\d+)\s*,\s*""y""\s*:\s*(\d+)\s*,\s*""derecha""\s*:\s*(true|false)"))
                m.rampas.Add(new Rampa(int.Parse(r.Groups[1].Value), int.Parse(r.Groups[2].Value), r.Groups[3].Value == "true"));
            foreach (Match b in Regex.Matches(Lista("bases"), @"""x""\s*:\s*(\d+)\s*,\s*""y""\s*:\s*(\d+)"))
                m.bases.Add(new Vector2Int(int.Parse(b.Groups[1].Value), int.Parse(b.Groups[2].Value)));
            return m;
        }

        static string CarpetaOficial()
        {
            var d = new System.IO.DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
            for (; d != null; d = d.Parent)
            {
                var c = System.IO.Path.Combine(d.FullName, "Assets", "StreamingAssets", "Mapas");
                if (System.IO.Directory.Exists(c)) return c;
            }
            return null;
        }
    }
}
