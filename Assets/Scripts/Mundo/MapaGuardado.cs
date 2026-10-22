using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>Algo colocado en el mapa: un árbol, una mena, una piedra o un arbusto.</summary>
    [Serializable]
    public struct ObjetoDeMapa
    {
        public SemillaMapa tipo;
        public int x, y;
        public int variante;

        public ObjetoDeMapa(SemillaMapa tipo, int x, int y, int variante)
        {
            this.tipo = tipo;
            this.x = x;
            this.y = y;
            this.variante = variante;
        }
    }

    /// <summary>
    /// Un edificio de salida: una torre o unas casas con las que empieza una base. Es del
    /// punto de inicio más cercano, que es lo que un diseñador espera al ponerlo a su lado.
    /// </summary>
    [Serializable]
    public struct EdificioDeMapa
    {
        public Datos.TipoEdificio tipo;
        public int x, y;
        public int variante;

        public EdificioDeMapa(Datos.TipoEdificio tipo, int x, int y, int variante)
        {
            this.tipo = tipo;
            this.x = x;
            this.y = y;
            this.variante = variante;
        }
    }

    /// <summary>El filtro de color de un mapa: lo que le da personalidad.</summary>
    [Serializable]
    public struct AmbienteDeMapa
    {
        public Color tono;
        [Range(0f, 2f)] public float saturacion;
        [Range(0.3f, 1.6f)] public float brillo;

        public static AmbienteDeMapa Neutro => new AmbienteDeMapa { tono = Color.white, saturacion = 1f, brillo = 1f };
    }

    /// <summary>
    /// Un mapa como archivo: lo que dibuja el editor y lo que construye el juego al arrancar.
    /// </summary>
    /// <remarks>
    /// <b>Un mapa es un dato, no una escena</b> (ADR-28). Hasta la semana 12 cada mapa era una
    /// escena que fabricaba el generador del editor de Unity; un jugador no podía hacer uno.
    /// Ahora un mapa es esto: el terreno celda a celda, el relieve, las escaleras, lo que hay
    /// colocado y dónde empieza cada bando. Se guarda como JSON, y una misma escena construye
    /// cualquiera al arrancar.
    ///
    /// El terreno y la altura van comprimidos en base64 —un byte por celda— y no como listas:
    /// un mapa de 224×224 son cincuenta mil celdas, y como lista de números el archivo pesaría
    /// diez veces más sin decir nada que no diga esto.
    /// </remarks>
    [Serializable]
    public class MapaGuardado
    {
        /// <summary>Sube si el formato cambia de forma incompatible.</summary>
        public int version = 1;

        public string nombre = "Mapa sin nombre";
        public string descripcion = "";
        public int ancho = 160;
        public int alto = 160;

        /// <summary>0 es agua; 1 a 5, tierra del color de tileset correspondiente.</summary>
        public string terreno = "";

        /// <summary>Altura de cada celda. 0 es llano.</summary>
        public string nivel = "";

        public List<Rampa> rampas = new List<Rampa>();
        public List<ObjetoDeMapa> objetos = new List<ObjetoDeMapa>();
        public List<Vector2Int> criaderos = new List<Vector2Int>();
        [Min(2f)] public float segundosPorOveja = 20f;
        public List<Vector2Int> bases = new List<Vector2Int>();
        public List<EdificioDeMapa> edificios = new List<EdificioDeMapa>();
        public AmbienteDeMapa ambiente = AmbienteDeMapa.Neutro;

        // -----------------------------------------------------------------
        // Celdas
        // -----------------------------------------------------------------

        [NonSerialized] byte[] _terreno;
        [NonSerialized] byte[] _nivel;

        public byte[] Terreno => _terreno ??= Decodificar(terreno);
        public byte[] Nivel => _nivel ??= Decodificar(nivel);

        int I(int x, int y) => x + y * ancho;

        public bool Dentro(int x, int y) => x >= 0 && y >= 0 && x < ancho && y < alto;

        public byte TerrenoEn(int x, int y) => Dentro(x, y) ? Terreno[I(x, y)] : (byte)0;
        public byte NivelEn(int x, int y) => Dentro(x, y) ? Nivel[I(x, y)] : (byte)0;

        public void PonerTerreno(int x, int y, byte valor)
        {
            if (Dentro(x, y)) Terreno[I(x, y)] = valor;
        }

        public void PonerNivel(int x, int y, byte valor)
        {
            if (Dentro(x, y)) Nivel[I(x, y)] = valor;
        }

        /// <summary>El punto de inicio más cercano a una celda, o −1 si no hay ninguno.</summary>
        public int BaseMasCercana(int x, int y)
        {
            int mejor = -1;
            float d = float.MaxValue;
            for (int i = 0; i < bases.Count; i++)
            {
                float e = (bases[i] - new Vector2Int(x, y)).sqrMagnitude;
                if (e < d) { d = e; mejor = i; }
            }
            return mejor;
        }

        /// <summary>Un mapa nuevo: todo mar.</summary>
        public static MapaGuardado EnBlanco(int ancho, int alto, string nombre)
        {
            var m = new MapaGuardado
            {
                nombre = nombre,
                ancho = Mathf.Clamp(ancho, 48, 320),
                alto = Mathf.Clamp(alto, 48, 320),
            };

            m._terreno = new byte[m.ancho * m.alto];
            m._nivel = new byte[m.ancho * m.alto];
            return m;
        }

        // -----------------------------------------------------------------
        // Archivo
        // -----------------------------------------------------------------

        public string AJson()
        {
            terreno = Convert.ToBase64String(Terreno);
            nivel = Convert.ToBase64String(Nivel);
            return JsonUtility.ToJson(this, true);
        }

        public static MapaGuardado DeJson(string json)
        {
            var m = JsonUtility.FromJson<MapaGuardado>(json);
            if (m == null) return null;

            int celdas = m.ancho * m.alto;
            if (m.Terreno.Length != celdas) m._terreno = new byte[celdas];
            if (m.Nivel.Length != celdas) m._nivel = new byte[celdas];
            return m;
        }

        static byte[] Decodificar(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return Array.Empty<byte>();
            try { return Convert.FromBase64String(base64); }
            catch (FormatException) { return Array.Empty<byte>(); }
        }

        // -----------------------------------------------------------------
        // Conversión con lo que ya entiende el juego
        // -----------------------------------------------------------------

        /// <summary>
        /// Lo convierte en el <see cref="MapaGenerado"/> que ya consumen la grilla, la niebla,
        /// el minimapa y la IA. Por eso un mapa dibujado a mano se juega igual que uno generado:
        /// a partir de aquí el juego no distingue de dónde salió.
        /// </summary>
        public MapaGenerado AMapaGenerado()
        {
            var g = new MapaGenerado
            {
                Ancho = ancho,
                Alto = alto,
                Tierra = new bool[ancho, alto],
                Nivel = new byte[ancho, alto],
                Escalera = new bool[ancho, alto],
                RelieveDibujado = true,
            };

            for (int x = 0; x < ancho; x++)
                for (int y = 0; y < alto; y++)
                {
                    g.Tierra[x, y] = TerrenoEn(x, y) > 0;
                    g.Nivel[x, y] = g.Tierra[x, y] ? NivelEn(x, y) : (byte)0;
                }

            g.Rampas.AddRange(rampas);
            GeneradorTerreno.MarcarEscaleras(g);

            foreach (var o in objetos)
            {
                var c = new Vector2Int(o.x, o.y);
                switch (o.tipo)
                {
                    case SemillaMapa.Oro: g.Oro.Add(c); break;
                    case SemillaMapa.Arbol: g.Arboles.Add(c); break;
                    case SemillaMapa.Piedra: g.Rocas.Add(c); break;
                    case SemillaMapa.Arbusto: g.Arbustos.Add(c); break;
                    case SemillaMapa.Oveja: g.Ovejas.Add(c); break;
                }
            }

            g.Ovejas.AddRange(criaderos);
            g.Bases.AddRange(bases);
            return g;
        }

        /// <summary>
        /// Lo contrario: un mapa del generador pasado a archivo, para abrirlo en el editor.
        /// Es lo que permite editar Tres Coronas en vez de empezar siempre desde el mar.
        /// </summary>
        public static MapaGuardado DeMapaGenerado(MapaGenerado g, string nombre, string descripcion, byte color = 1)
        {
            var m = EnBlanco(g.Ancho, g.Alto, nombre);
            m.descripcion = descripcion;

            for (int x = 0; x < g.Ancho; x++)
                for (int y = 0; y < g.Alto; y++)
                {
                    if (!g.Tierra[x, y]) continue;
                    m.PonerTerreno(x, y, color);
                    if (g.Nivel != null) m.PonerNivel(x, y, g.Nivel[x, y]);
                }

            m.rampas.AddRange(g.Rampas);
            foreach (var c in g.Oro) m.objetos.Add(new ObjetoDeMapa(SemillaMapa.Oro, c.x, c.y, 0));
            foreach (var c in g.Arboles) m.objetos.Add(new ObjetoDeMapa(SemillaMapa.Arbol, c.x, c.y, (c.x * 7 + c.y * 3) % 4));
            foreach (var c in g.Rocas) m.objetos.Add(new ObjetoDeMapa(SemillaMapa.Piedra, c.x, c.y, (c.x + c.y) % 4));
            foreach (var c in g.Arbustos) m.objetos.Add(new ObjetoDeMapa(SemillaMapa.Arbusto, c.x, c.y, (c.x * 3 + c.y) % 4));

            // Las ovejas del generador vienen en rebaños: un criadero por rebaño, no por oveja.
            for (int i = 0; i < g.Ovejas.Count; i += 4) m.criaderos.Add(g.Ovejas[i]);

            m.bases.AddRange(g.Bases);
            return m;
        }
    }
}
