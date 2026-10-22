using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// La miniatura de un mapa de archivo, para la pantalla de preparar partida.
    /// </summary>
    /// <remarks>
    /// La de Tres Coronas la genera el editor de Unity al construir la escena; un mapa que
    /// dibuja el jugador no pasa por ahí, así que se pinta en partida con los mismos colores.
    /// Un píxel por celda y filtro de punto: se amplía como pixel art, sin emborronarse.
    /// </remarks>
    public static class MiniaturaDeMapa
    {
        static readonly Color32 Agua = new Color32(56, 102, 158, 255);
        static readonly Color32 Orilla = new Color32(74, 128, 176, 255);
        static readonly Color32[] Tierras =
        {
            new Color32(87, 140, 71, 255), new Color32(104, 150, 64, 255), new Color32(146, 150, 70, 255),
            new Color32(118, 128, 88, 255), new Color32(96, 120, 100, 255),
        };
        static readonly Color32 Arbol = new Color32(41, 87, 48, 255);
        static readonly Color32 Oro = new Color32(219, 181, 64, 255);
        static readonly Color32 Roca = new Color32(120, 120, 128, 255);

        public static Sprite Crear(MapaGuardado m)
        {
            if (m == null || m.ancho <= 0 || m.alto <= 0) return null;

            var tex = new Texture2D(m.ancho, m.alto, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[m.ancho * m.alto];
            for (int y = 0; y < m.alto; y++)
                for (int x = 0; x < m.ancho; x++)
                {
                    byte t = m.TerrenoEn(x, y);
                    Color32 c;
                    if (t == 0) c = CercaDeTierra(m, x, y) ? Orilla : Agua;
                    else
                    {
                        c = Tierras[(t - 1) % Tierras.Length];
                        // Cada nivel de altura, un poco más claro: se leen las mesetas.
                        float luz = 1f + 0.12f * m.NivelEn(x, y);
                        c = new Color32((byte)Mathf.Min(255, c.r * luz), (byte)Mathf.Min(255, c.g * luz),
                                        (byte)Mathf.Min(255, c.b * luz), 255);
                    }
                    px[y * m.ancho + x] = c;
                }

            foreach (var o in m.objetos)
            {
                var c = o.tipo == SemillaMapa.Arbol ? Arbol : o.tipo == SemillaMapa.Oro ? Oro : Roca;
                if (o.tipo == SemillaMapa.Arbusto || o.tipo == SemillaMapa.Oveja) continue;
                Poner(px, m, o.x, o.y, c, o.tipo == SemillaMapa.Oro ? 1 : 0);
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, m.ancho, m.alto), new Vector2(0.5f, 0.5f), 1f);
        }

        static void Poner(Color32[] px, MapaGuardado m, int x, int y, Color32 c, int radio)
        {
            for (int dy = -radio; dy <= radio; dy++)
                for (int dx = -radio; dx <= radio; dx++)
                    if (m.Dentro(x + dx, y + dy)) px[(y + dy) * m.ancho + x + dx] = c;
        }

        static bool CercaDeTierra(MapaGuardado m, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (m.TerrenoEn(x + dx, y + dy) > 0) return true;
            return false;
        }

        /// <summary>Las bases en coordenadas de 0 a 1, para marcarlas encima de la miniatura.</summary>
        public static Vector2[] Bases(MapaGuardado m)
        {
            var r = new Vector2[m.bases.Count];
            for (int i = 0; i < r.Length; i++)
                r[i] = new Vector2((m.bases[i].x + 0.5f) / m.ancho, (m.bases[i].y + 0.5f) / m.alto);
            return r;
        }
    }
}
