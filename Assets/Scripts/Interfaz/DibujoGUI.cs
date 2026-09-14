using UnityEngine;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Utilidades para dibujar arte del pack con <c>OnGUI</c>.
    /// </summary>
    /// <remarks>
    /// <c>OnGUI</c> es un sistema aparte del de interfaz de Unity y no trae ni 9-slice ni
    /// soporte de <c>Sprite</c>: solo sabe de texturas enteras. El pack, en cambio, es todo
    /// marcos con borde y sprites recortados de un atlas. Estas dos funciones son el puente,
    /// y viven aquí para que el cartel de final y el panel de pruebas usen exactamente el
    /// mismo dibujo en vez de cada uno el suyo.
    /// </remarks>
    public static class DibujoGUI
    {
        /// <summary>Un sprite del pack, respetando su recorte dentro del atlas.</summary>
        public static void Sprite(Rect destino, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;

            GUI.DrawTextureWithTexCoords(destino, sprite.texture, Uv(sprite), true);
        }

        static Rect Uv(Sprite sprite)
        {
            var r = sprite.textureRect;

            return new Rect(r.x / sprite.texture.width,
                            r.y / sprite.texture.height,
                            r.width / sprite.texture.width,
                            r.height / sprite.texture.height);
        }

        /// <summary>
        /// Dibuja en nueve trozos: esquinas a tamaño fijo, bordes y centro estirados.
        /// </summary>
        /// <remarks>
        /// Estirando la textura entera, el marco pintado se aplasta hasta desaparecer. El
        /// borde tiene que conservar su grosor mientras el interior crece, y eso son nueve
        /// trozos y no uno. Es lo que hace el 9-slice de uGUI, a mano.
        /// </remarks>
        public static void NueveCortes(Rect destino, Texture2D tex, float borde)
        {
            if (tex == null) return;

            NueveCortes(destino, tex, new Rect(0f, 0f, 1f, 1f),
                        borde / tex.width, borde / tex.height, borde);
        }

        /// <summary>La misma idea, pero sobre un sprite recortado de un atlas.</summary>
        public static void NueveCortes(Rect destino, Sprite sprite, float borde)
        {
            if (sprite == null || sprite.texture == null) return;

            var uv = Uv(sprite);
            var r = sprite.textureRect;

            NueveCortes(destino, sprite.texture, uv,
                        borde / r.width * uv.width,
                        borde / r.height * uv.height, borde);
        }

        static void NueveCortes(Rect destino, Texture2D tex, Rect uv,
                                float bu, float bv, float borde)
        {
            // Con un destino pequeno los dos bordes no caben: se encogen antes de solaparse
            // y empezar a dibujarse del reves.
            float bx = Mathf.Min(borde, destino.width * 0.5f);
            float by = Mathf.Min(borde, destino.height * 0.5f);

            float[] xs = { destino.x, destino.x + bx, destino.xMax - bx };
            float[] ws = { bx, destino.width - bx * 2f, bx };

            float[] ys = { destino.y, destino.y + by, destino.yMax - by };
            float[] hs = { by, destino.height - by * 2f, by };

            float[] us = { uv.x, uv.x + bu, uv.xMax - bu };
            float[] uws = { bu, uv.width - bu * 2f, bu };

            // GUI mide la Y de arriba abajo y la textura de abajo arriba: las filas de UV van
            // en orden inverso a las de pantalla.
            float[] vs = { uv.yMax - bv, uv.y + bv, uv.y };
            float[] vhs = { bv, uv.height - bv * 2f, bv };

            for (int fila = 0; fila < 3; fila++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (ws[col] <= 0f || hs[fila] <= 0f) continue;

                    GUI.DrawTextureWithTexCoords(
                        new Rect(xs[col], ys[fila], ws[col], hs[fila]), tex,
                        new Rect(us[col], vs[fila], uws[col], vhs[fila]), true);
                }
            }
        }
    }
}
