using System;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Piezas de interfaz con el arte del pack, montadas por código.
    /// </summary>
    /// <remarks>
    /// <b>Por código y no como prefabs</b>, igual que el HUD desde la semana 05: la escena se
    /// regenera entera desde un menú del editor, y un prefab de UI es una cosa más que se
    /// desincroniza del código que lo usa. Aquí cada pantalla dice en C# qué tiene y dónde,
    /// y se puede leer de arriba abajo.
    ///
    /// Todo es uGUI con <c>Canvas</c>, no la GUI inmediata del panel de pruebas: es lo que la
    /// semana 15 va a pulir, no a rehacer.
    /// </remarks>
    public static class KitInterfaz
    {
        public static readonly Color Tinta = new Color(0.20f, 0.14f, 0.09f);
        public static readonly Color Crema = new Color(0.98f, 0.94f, 0.82f);
        public static readonly Color Sombra = new Color(0f, 0f, 0f, 0.55f);

        public static Canvas Lienzo(Transform padre, string nombre, int orden)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);

            var lienzo = go.AddComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = orden;

            var escala = go.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return lienzo;
        }

        public static RectTransform Hijo(Transform padre, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Ocupa todo el padre.</summary>
        public static RectTransform Llenar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Anclado a un punto del padre (0..1), con tamaño fijo y desplazamiento.</summary>
        public static RectTransform Colocar(RectTransform rt, Vector2 ancla, Vector2 tamano,
                                            Vector2 desplazamiento = default)
        {
            rt.anchorMin = rt.anchorMax = ancla;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamano;
            rt.anchoredPosition = desplazamiento;
            return rt;
        }

        public static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color)
        {
            var img = Hijo(padre, nombre).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;

            // Una pieza con borde se estira por el centro, no entera: es lo que deja usar la
            // misma caja de 64 px para un botón y para un panel de media pantalla.
            if (sprite != null && sprite.border != Vector4.zero) img.type = Image.Type.Sliced;
            else img.preserveAspect = sprite != null;

            return img;
        }

        public static Text Texto(Transform padre, string nombre, string texto, Font fuente,
                                 int tamano, Color color, TextAnchor alineado, bool contorno = true)
        {
            var t = Hijo(padre, nombre).gameObject.AddComponent<Text>();
            t.text = texto;
            t.font = fuente;
            t.fontSize = tamano;
            t.color = color;
            t.alignment = alineado;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            if (contorno)
            {
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = Sombra;
                o.effectDistance = new Vector2(2f, -2f);
            }

            return t;
        }

        /// <summary>
        /// Un botón del pack con su texto. Al pulsarlo suena y llama a la acción.
        /// </summary>
        /// <remarks>
        /// El pack trae solo el estado normal de cada botón. Los otros tres —encima, pulsado,
        /// apagado— salen del tinte de color que uGUI ya sabe hacer: más claro, más oscuro y
        /// gris. No hace falta dibujar tres versiones de cada botón.
        /// </remarks>
        public static Button Boton(Transform padre, string nombre, Sprite fondo, string texto,
                                   Font fuente, int tamano, Action alPulsar)
        {
            var img = Imagen(padre, nombre, fondo, Color.white);
            img.raycastTarget = true;

            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;

            var colores = b.colors;
            colores.normalColor = Color.white;
            colores.highlightedColor = new Color(1.12f, 1.12f, 1.05f);
            colores.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colores.selectedColor = Color.white;
            colores.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            colores.colorMultiplier = 1.1f;
            colores.fadeDuration = 0.08f;
            b.colors = colores;

            var etiqueta = Texto(img.transform, "Texto", texto, fuente, tamano, Crema, TextAnchor.MiddleCenter);
            Llenar(etiqueta.rectTransform);

            b.onClick.AddListener(() =>
            {
                Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.BotonPanel);
                alPulsar?.Invoke();
            });

            return b;
        }

        public static void CambiarTexto(Button b, string texto)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = texto;
        }

        /// <summary>Una barra del pack que se arrastra: el volumen.</summary>
        public static Slider Deslizador(Transform padre, string nombre, Sprite marco, Sprite relleno,
                                        Sprite tirador, float valor, Action<float> alCambiar)
        {
            var raiz = Hijo(padre, nombre);
            var s = raiz.gameObject.AddComponent<Slider>();

            var fondo = Imagen(raiz, "Fondo", marco, Color.white);
            Llenar(fondo.rectTransform);

            var area = Hijo(raiz, "Relleno");
            Llenar(area).offsetMin = new Vector2(16f, 13f);
            area.offsetMax = new Vector2(-16f, -13f);

            var barra = Imagen(area, "Barra", relleno, Color.white);
            barra.type = Image.Type.Sliced;
            Llenar(barra.rectTransform);

            var zona = Hijo(raiz, "Zona");
            Llenar(zona).offsetMin = new Vector2(10f, 0f);
            zona.offsetMax = new Vector2(-10f, 0f);

            var mango = Imagen(zona, "Tirador", tirador, Color.white);
            mango.rectTransform.sizeDelta = new Vector2(46f, 16f);
            mango.raycastTarget = true;

            s.fillRect = barra.rectTransform;
            s.handleRect = mango.rectTransform;
            s.targetGraphic = mango;
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = valor;
            s.onValueChanged.AddListener(v => alCambiar?.Invoke(v));

            return s;
        }

        /// <summary>Una columna que apila a sus hijos con separación.</summary>
        public static RectTransform Columna(Transform padre, string nombre, float separacion)
        {
            var rt = Hijo(padre, nombre);
            var col = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            col.spacing = separacion;
            col.childAlignment = TextAnchor.UpperCenter;
            col.childControlWidth = col.childControlHeight = true;
            col.childForceExpandWidth = true;
            col.childForceExpandHeight = false;
            return rt;
        }

        public static T Alto<T>(T componente, float alto) where T : Component
        {
            var le = componente.gameObject.GetComponent<LayoutElement>() ??
                     componente.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = alto;
            le.minHeight = alto;
            return componente;
        }

        /// <summary>Un velo oscuro a pantalla completa que se traga los clics.</summary>
        public static Image Velo(Transform padre, float opacidad)
        {
            var v = Imagen(padre, "Velo", null, new Color(0.05f, 0.04f, 0.06f, opacidad));
            v.raycastTarget = true;
            Llenar(v.rectTransform);
            return v;
        }

        // -----------------------------------------------------------------
        // Piezas de la semana 12, segunda vuelta: la mesa de guerra
        // -----------------------------------------------------------------

        static readonly System.Collections.Generic.Dictionary<(Sprite, Vector4), Sprite> _conBorde =
            new System.Collections.Generic.Dictionary<(Sprite, Vector4), Sprite>();

        /// <summary>
        /// La misma pieza, con un borde para estirarse por el centro.
        /// </summary>
        /// <remarks>
        /// Varias piezas del pack llegan sin borde —la barra grande, el pergamino— y estiradas
        /// enteras se deforman: el marco de la barra salía como un ladrillo (semana 12). En vez
        /// de tocar su importación, se crea en caliente un sprite gemelo con borde, una vez.
        /// </remarks>
        public static Sprite ConBorde(Sprite s, Vector4 borde)
        {
            if (s == null) return null;
            if (_conBorde.TryGetValue((s, borde), out var hecho) && hecho != null) return hecho;

            var nuevo = Sprite.Create(s.texture, s.rect, new Vector2(0.5f, 0.5f), s.pixelsPerUnit,
                                      0, SpriteMeshType.FullRect, borde);
            nuevo.name = s.name + "_borde";
            _conBorde[(s, borde)] = nuevo;
            return nuevo;
        }

        /// <summary>Una cinta del pack con un título encima.</summary>
        public static Text Cinta(Transform padre, string nombre, Sprite cinta, string texto, Font fuente,
                                 int tamano, Vector2 ancla, Vector2 medida, Vector2 desplazamiento)
        {
            var img = Imagen(padre, nombre, cinta, Color.white);
            Colocar(img.rectTransform, ancla, medida, desplazamiento);

            var t = Texto(img.transform, "Texto", texto, fuente, tamano, Crema, TextAnchor.MiddleCenter);
            Llenar(t.rectTransform).offsetMin = new Vector2(medida.x * 0.18f, medida.y * 0.22f);
            t.rectTransform.offsetMax = new Vector2(-medida.x * 0.18f, -medida.y * 0.05f);
            return t;
        }

        /// <summary>
        /// Un control «‹ valor ›»: dos flechas y el valor en medio.
        /// </summary>
        /// <remarks>
        /// Sirve para todo lo que tiene pocas opciones —quién ocupa una base, la dificultad,
        /// la niebla del sandbox, el límite de FPS—. Con dos a cinco valores un desplegable
        /// esconde lo que hay; las flechas lo enseñan y se recorren sin abrir nada. Pulsar en
        /// el valor también avanza, que es lo primero que intenta casi todo el mundo.
        /// </remarks>
        public static Text Ciclador(Transform padre, string nombre, Font fuente, Sprite caja, Sprite flecha,
                                    System.Action<int> cambiar, Vector2 ancla, Vector2 medida,
                                    Vector2 desplazamiento, int tamano = 28)
        {
            var raiz = Hijo(padre, nombre);
            Colocar(raiz, ancla, medida, desplazamiento);

            float lado = medida.y;

            var menos = Boton(raiz, "Menos", flecha, "‹", fuente, tamano + 8, () => cambiar(-1));
            Colocar((RectTransform)menos.transform, new Vector2(0f, 0.5f), new Vector2(lado, lado),
                    new Vector2(lado * 0.5f, 0f));

            var mas = Boton(raiz, "Mas", flecha, "›", fuente, tamano + 8, () => cambiar(+1));
            Colocar((RectTransform)mas.transform, new Vector2(1f, 0.5f), new Vector2(lado, lado),
                    new Vector2(-lado * 0.5f, 0f));

            var centro = Boton(raiz, "Valor", caja, "", fuente, tamano, () => cambiar(+1));
            var rc = (RectTransform)centro.transform;
            rc.anchorMin = new Vector2(0f, 0f);
            rc.anchorMax = new Vector2(1f, 1f);
            rc.offsetMin = new Vector2(lado + 6f, 0f);
            rc.offsetMax = new Vector2(-lado - 6f, 0f);

            var texto = centro.GetComponentInChildren<Text>();
            texto.resizeTextForBestFit = true;
            texto.resizeTextMinSize = 14;
            texto.resizeTextMaxSize = tamano;
            return texto;
        }

        /// <summary>Etiqueta a la izquierda de una fila de opciones.</summary>
        public static Text Etiqueta(Transform padre, string texto, Font fuente, int tamano,
                                    Vector2 ancla, Vector2 medida, Vector2 desplazamiento)
        {
            var t = Texto(padre, "Etiqueta_" + texto, texto, fuente, tamano, Crema, TextAnchor.MiddleLeft);
            Colocar(t.rectTransform, ancla, medida, desplazamiento);
            return t;
        }

        /// <summary>Un texto que nunca se sale de su caja: encoge la letra si hace falta.</summary>
        public static Text TextoQueCabe(Transform padre, string nombre, Font fuente, int maximo, int minimo,
                                        Color color, TextAnchor alineado)
        {
            var t = Texto(padre, nombre, "", fuente, maximo, color, alineado);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = maximo;
            t.resizeTextMinSize = minimo;
            return t;
        }
    }
}
