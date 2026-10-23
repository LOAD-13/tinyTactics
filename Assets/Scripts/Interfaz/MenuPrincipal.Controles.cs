using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El cartel de controles: un teclado y un ratón dibujados, con fichas que señalan cada
    /// tecla que usa el juego (semana 14).
    /// </summary>
    /// <remarks>
    /// <b>El dibujo es pixel art hecho por código</b> (<c>tools/dibujar_controles.py</c>), no
    /// una imagen bajada ni generada: así las teclas en dorado son exactamente las que el
    /// juego lee, y el script escribe dónde cae cada una para tirar las líneas hasta ella.
    /// <para>
    /// <b>Las líneas van en ángulo recto y no se cruzan.</b> Cada ficha baja (o sube) hasta su
    /// propio carril, cruza en horizontal y entra en la tecla en vertical. Con las fichas en
    /// el mismo orden que sus teclas y los carriles escalonados, ninguna línea corta a otra.
    /// </para>
    /// </remarks>
    public partial class MenuPrincipal
    {
        [Tooltip("Dibujo del teclado. Lo genera tools/dibujar_controles.py.")]
        public Sprite teclado;
        public Sprite raton;

        /// <summary>Centros de las teclas usadas, en fracción del dibujo (lo imprime el script).</summary>
        static readonly System.Collections.Generic.Dictionary<string, Vector2> Teclas = new System.Collections.Generic.Dictionary<string, Vector2>
        {
            ["esc"] = new Vector2(0.0427f, 0.8942f), ["d1"] = new Vector2(0.0939f, 0.7468f), ["d9"] = new Vector2(0.5037f, 0.7468f),
            ["d5"] = new Vector2(0.2988f, 0.7468f), ["t"] = new Vector2(0.3232f, 0.6122f), ["p"] = new Vector2(0.5817f, 0.6122f),
            ["a"] = new Vector2(0.1329f, 0.4776f), ["s"] = new Vector2(0.1841f, 0.4776f), ["shift"] = new Vector2(0.0744f, 0.3429f),
            ["x"] = new Vector2(0.2085f, 0.3429f), ["v"] = new Vector2(0.311f, 0.3429f), ["b"] = new Vector2(0.3622f, 0.3429f),
            ["m"] = new Vector2(0.4646f, 0.3429f), ["ctrl"] = new Vector2(0.0488f, 0.2083f), ["espacio"] = new Vector2(0.43f, 0.2083f),
            ["abajo"] = new Vector2(0.889f, 0.2083f),
        };

        static readonly System.Collections.Generic.Dictionary<string, Vector2> BotonesRaton = new System.Collections.Generic.Dictionary<string, Vector2>
        {
            ["izquierdo"] = new Vector2(0.25f, 0.8163f), ["derecho"] = new Vector2(0.75f, 0.8163f), ["rueda"] = new Vector2(0.5f, 0.8265f),
        };

        // Dónde y a qué tamaño van los dibujos dentro de la mesa (centro de la mesa = 0,0).
        static readonly Vector2 CentroTeclado = new Vector2(-170f, 10f), CentroRaton = new Vector2(560f, 30f);
        const float EscalaDibujo = 2.4f;

        /// <summary>De centro a borde de una tecla, ya escalada.</summary>
        const float BordeDeTecla = 22f;

        static readonly Color ColorLinea = new Color(0.93f, 0.72f, 0.25f);

        GameObject ConstruirControles(RectTransform padre)
        {
            var velo = KitInterfaz.Velo(padre, 0.6f);
            var mesa = Mesa((RectTransform)velo.transform, new Vector2(1720f, 960f), "Controles", Negro, out _, out _);

            var tamTeclado = teclado != null ? teclado.rect.size * EscalaDibujo : new Vector2(984f, 374f);
            var tamRaton = raton != null ? raton.rect.size * EscalaDibujo : new Vector2(149f, 235f);
            Dibujo(mesa, "Teclado", teclado, CentroTeclado, tamTeclado);
            Dibujo(mesa, "Raton", raton, CentroRaton, tamRaton);

            Vector2 Tecla(string t) => CentroTeclado - tamTeclado / 2f + Vector2.Scale(Teclas[t], tamTeclado);
            Vector2 Boton(string b) => CentroRaton - tamRaton / 2f + Vector2.Scale(BotonesRaton[b], tamRaton);

            // Arriba: lo que está en las filas altas, y los botones del ratón.
            float arriba = 320f, bordeArriba = CentroTeclado.y + tamTeclado.y / 2f;
            var fichasArriba = new (string tecla, string accion, Vector2[] destinos)[]
            {
                ("ESC", "Cancelar · pausa", new[] { Tecla("esc") }),
                ("1 … 9", "Recuperar un grupo", new[] { Tecla("d1"), Tecla("d5") }),
                ("T", "Atacar lo que encuentre", new[] { Tecla("t") }),
                ("P", "Entrenar pawn (castillo)", new[] { Tecla("p") }),
                ("CLIC IZQUIERDO", "Seleccionar · arrastrar", new[] { Boton("izquierdo") }),
                ("RUEDA", "Zoom", new[] { Boton("rueda") }),
                ("CLIC DERECHO", "Mover · atacar · recolectar", new[] { Boton("derecho") }),
            };
            float[] xArriba = { -720f, -480f, -240f, 0f, 290f, 510f, 720f };
            for (int i = 0; i < fichasArriba.Length; i++)
            {
                var (tecla, accion, destinos) = fichasArriba[i];
                var ficha = new Vector2(xArriba[i], arriba);
                FichaDeControl(mesa, tecla, accion, ficha);
                float carril = bordeArriba + 14f + i * 6f;
                Linea(mesa, ficha + new Vector2(0f, -40f), carril, destinos, +1f);
            }

            // Abajo: el resto del teclado, en el mismo orden que sus teclas.
            float abajo = -315f, bordeAbajo = CentroTeclado.y - tamTeclado.y / 2f;
            var fichasAbajo = new (string tecla, string accion, Vector2[] destinos)[]
            {
                ("CTRL + 1 … 9", "Guardar un grupo", new[] { Tecla("ctrl") }),
                ("SHIFT", "Añadir a la selección", new[] { Tecla("shift") }),
                ("A · S", "Atacar · detener", new[] { Tecla("a"), Tecla("s") }),
                ("X", "Cambiar postura", new[] { Tecla("x") }),
                ("V · B", "Mover · construir", new[] { Tecla("v"), Tecla("b") }),
                ("ESPACIO", "Volver al castillo", new[] { Tecla("espacio") }),
                ("M", "Minimapa grande", new[] { Tecla("m") }),
                ("FLECHAS", "Mover la cámara", new[] { Tecla("abajo") }),
            };
            for (int i = 0; i < fichasAbajo.Length; i++)
            {
                var (tecla, accion, destinos) = fichasAbajo[i];
                var ficha = new Vector2(-720f + i * 205.7f, abajo);
                FichaDeControl(mesa, tecla, accion, ficha);
                // Las fichas de la derecha cruzan por encima: así ninguna vertical corta un carril.
                float carril = bordeAbajo - 74f + i * 9f;
                Linea(mesa, ficha + new Vector2(0f, 40f), carril, destinos, -1f);
            }

            var nota = KitInterfaz.Texto(mesa, "Nota", "El ratón en el borde de la pantalla también mueve la cámara",
                                         _fuente, 20, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(nota.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(900f, 30f), new Vector2(0f, -405f));

            BotonDeAccion(mesa, "Volver", Rojo, () => velo.gameObject.SetActive(false), new Vector2(0.5f, 0f),
                          new Vector2(260f, 74f), new Vector2(0f, 10f), 30);

            velo.gameObject.SetActive(false);
            return velo.gameObject;
        }

        void Dibujo(RectTransform mesa, string nombre, Sprite sprite, Vector2 centro, Vector2 tamano)
        {
            var img = KitInterfaz.Imagen(mesa, nombre, sprite, Color.white);
            KitInterfaz.Colocar(img.rectTransform, new Vector2(0.5f, 0.5f), tamano, centro);
        }

        /// <summary>Una ficha de pergamino: la tecla arriba, en grande, y lo que hace debajo.</summary>
        void FichaDeControl(RectTransform mesa, string tecla, string accion, Vector2 centro)
        {
            var papel = KitInterfaz.Imagen(mesa, "Ficha " + tecla, KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(papel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(200f, 130f), centro);

            var t = KitInterfaz.TextoQueCabe(papel.transform, "Tecla", _fuente, 24, 12, new Color(0.55f, 0.32f, 0.08f), TextAnchor.MiddleCenter);
            t.GetComponent<Outline>().enabled = false;
            t.text = tecla;
            // El papel del pack trae margen transparente y un marco ancho: el texto va en el
            // centro, que es lo único que se ve como papel.
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150f, 28f), new Vector2(0f, 14f));

            var a = KitInterfaz.TextoQueCabe(papel.transform, "Accion", _fuente, 17, 10, KitInterfaz.Tinta, TextAnchor.MiddleCenter);
            a.GetComponent<Outline>().enabled = false;
            a.text = accion;
            KitInterfaz.Colocar(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(150f, 26f), new Vector2(0f, -14f));
        }

        /// <summary>
        /// De la ficha a su carril, por el carril hasta encima de la tecla, y a la tecla. Si la
        /// ficha señala dos teclas, el carril llega a las dos.
        /// </summary>
        void Linea(RectTransform mesa, Vector2 desde, float carril, Vector2[] teclas, float lado)
        {
            Segmento(mesa, desde, new Vector2(desde.x, carril));
            float min = desde.x, max = desde.x;
            foreach (var t in teclas) { min = Mathf.Min(min, t.x); max = Mathf.Max(max, t.x); }
            Segmento(mesa, new Vector2(min, carril), new Vector2(max, carril));
            foreach (var t in teclas)
            {
                // Se para en el borde de la tecla, no en su centro: el punto no tapa la letra.
                var fin = t + new Vector2(0f, lado * BordeDeTecla);
                Segmento(mesa, new Vector2(t.x, carril), fin);
                var punto = KitInterfaz.Imagen(mesa, "Punto", null, ColorLinea);
                KitInterfaz.Colocar(punto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(10f, 10f), fin);
            }
        }

        void Segmento(RectTransform mesa, Vector2 a, Vector2 b)
        {
            const float grosor = 3f;
            var s = KitInterfaz.Imagen(mesa, "Linea", null, ColorLinea);
            s.raycastTarget = false;
            var centro = (a + b) / 2f;
            var tam = new Vector2(Mathf.Abs(b.x - a.x) + grosor, Mathf.Abs(b.y - a.y) + grosor);
            KitInterfaz.Colocar(s.rectTransform, new Vector2(0.5f, 0.5f), tam, centro);
        }
    }
}
