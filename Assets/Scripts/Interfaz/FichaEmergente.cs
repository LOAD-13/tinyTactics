using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// La ficha que sale al pasar el ratón por un botón de comandos: qué es, qué cuesta,
    /// cuánto tarda, con qué tecla se pulsa y, si no se puede, por qué (semana 15).
    /// </summary>
    /// <remarks>
    /// <b>Pergamino del pack, no un recuadro negro.</b> Es la misma familia que los
    /// créditos y los controles: el juego entero habla desde la mesa de guerra. La tecla va
    /// en dorado, igual que en la pantalla de controles, para que el jugador relacione las
    /// dos cosas sin que nadie se lo explique.
    /// <para>
    /// <b>El contenido se pide al pasar el ratón, no al montar el botón.</b> «Te falta oro»
    /// cambia cada segundo; un texto fijado al crear el botón mentiría en cuanto el jugador
    /// recoge algo.
    /// </para>
    /// <para>
    /// Va <b>encima</b> del botón y nunca lo tapa: el jugador está mirando justo ahí.
    /// Se ajusta a la pantalla si no cabe.
    /// </para>
    /// </remarks>
    public class FichaEmergente : MonoBehaviour
    {
        public class Contenido
        {
            public string titulo;
            public string descripcion;
            public string atajo;
            public int oro, madera, poblacion;
            public float segundos;
            public int golpes;

            /// <summary>Por qué no se puede ahora. Vacío si se puede.</summary>
            public string bloqueo;
        }

        public static FichaEmergente Actual { get; private set; }

        const float Ancho = 360f;
        static readonly Color Tinta = new Color(0.22f, 0.15f, 0.09f);
        static readonly Color TintaRoja = new Color(0.62f, 0.16f, 0.12f);
        static readonly Color Oro = new Color(0.55f, 0.32f, 0.08f);

        TemaInterfaz _tema;
        RectTransform _raiz, _filaCoste;
        Text _titulo, _descripcion, _bloqueo, _atajo, _oro, _madera, _extra;
        Image _iconoOro, _iconoMadera, _tecla;
        CanvasGroup _grupo;
        float _visible, _objetivo;

        /// <summary>Monta la ficha dentro del lienzo dado. Una por escena.</summary>
        public static FichaEmergente Crear(Transform lienzo, TemaInterfaz tema, Font titular, Font texto)
        {
            var go = new GameObject("Ficha emergente", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            var f = go.AddComponent<FichaEmergente>();
            f.Montar(tema, titular, texto);
            return f;
        }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        void Montar(TemaInterfaz tema, Font titular, Font texto)
        {
            _tema = tema;
            _raiz = (RectTransform)transform;
            _raiz.anchorMin = _raiz.anchorMax = Vector2.zero;
            _raiz.pivot = new Vector2(0.5f, 0f);
            _grupo = gameObject.AddComponent<CanvasGroup>();
            _grupo.blocksRaycasts = false;
            _grupo.interactable = false;
            _grupo.alpha = 0f;

            var papel = gameObject.AddComponent<Image>();
            papel.sprite = tema != null && tema.papel != null ? KitInterfaz.ConBorde(tema.papel, new Vector4(28, 28, 28, 28)) : null;
            papel.type = Image.Type.Sliced;
            papel.raycastTarget = false;

            // Columna que crece con el texto: la ficha mide lo que tenga que decir.
            var columna = gameObject.AddComponent<VerticalLayoutGroup>();
            columna.padding = new RectOffset(30, 30, 26, 28);
            columna.spacing = 4f;
            columna.childControlHeight = true;
            columna.childControlWidth = true;
            columna.childForceExpandHeight = false;
            var ajuste = gameObject.AddComponent<ContentSizeFitter>();
            ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _raiz.sizeDelta = new Vector2(Ancho, 120f);

            // Cabecera: el nombre a la izquierda, la tecla dorada a la derecha.
            var cabecera = Fila("Cabecera", 34f);
            _titulo = Texto(cabecera, "Titulo", titular, 26, Tinta, TextAnchor.MiddleLeft);
            var t = _titulo.rectTransform;
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one; t.offsetMin = Vector2.zero; t.offsetMax = new Vector2(-44f, 0f);

            _tecla = new GameObject("Tecla", typeof(RectTransform)).AddComponent<Image>();
            _tecla.transform.SetParent(cabecera, false);
            _tecla.sprite = tema != null ? tema.CajaChicaDeColor(2) : null;
            _tecla.type = Image.Type.Sliced;
            _tecla.raycastTarget = false;
            var rt = _tecla.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(40f, 36f);
            _atajo = Texto(_tecla.transform, "Letra", titular, 22, Tinta, TextAnchor.MiddleCenter);
            KitInterfaz.Llenar(_atajo.rectTransform);

            _descripcion = Texto(transform, "Descripcion", texto, 18, Tinta, TextAnchor.UpperLeft);
            _descripcion.horizontalOverflow = HorizontalWrapMode.Wrap;
            _descripcion.verticalOverflow = VerticalWrapMode.Overflow;

            // Coste: los mismos sacos del HUD de recursos, para que se lean igual.
            _filaCoste = Fila("Coste", 30f);
            var linea = _filaCoste.gameObject.AddComponent<HorizontalLayoutGroup>();
            linea.spacing = 6f;
            linea.childControlWidth = false;
            linea.childControlHeight = false;
            linea.childAlignment = TextAnchor.MiddleLeft;
            _iconoOro = Icono(_filaCoste, tema != null ? tema.iconoRecursoOro : null);
            _oro = Cifra(_filaCoste, texto);
            _iconoMadera = Icono(_filaCoste, tema != null ? tema.iconoRecursoMadera : null);
            _madera = Cifra(_filaCoste, texto);
            _extra = Cifra(_filaCoste, texto);
            _extra.rectTransform.sizeDelta = new Vector2(150f, 28f);

            _bloqueo = Texto(transform, "Bloqueo", texto, 18, TintaRoja, TextAnchor.UpperLeft);
            _bloqueo.fontStyle = FontStyle.Bold;
            _bloqueo.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        RectTransform Fila(string nombre, float alto)
        {
            var rt = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = alto;
            le.minHeight = alto;
            return rt;
        }

        static Text Texto(Transform padre, string nombre, Font fuente, int tam, Color color, TextAnchor ancla)
        {
            var t = new GameObject(nombre, typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(padre, false);
            t.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = tam;
            t.color = color;
            t.alignment = ancla;
            t.raycastTarget = false;
            return t;
        }

        static Image Icono(Transform padre, Sprite s)
        {
            var img = new GameObject("Icono", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(padre, false);
            img.sprite = s;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(28f, 28f);
            return img;
        }

        static Text Cifra(Transform padre, Font fuente)
        {
            var t = Texto(padre, "Cifra", fuente, 20, Oro, TextAnchor.MiddleLeft);
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.sizeDelta = new Vector2(52f, 28f);
            return t;
        }

        // -----------------------------------------------------------------

        /// <summary>Enseña la ficha encima de <paramref name="boton"/>.</summary>
        public void Mostrar(Contenido c, RectTransform boton)
        {
            if (c == null || boton == null) { Ocultar(); return; }

            _titulo.text = c.titulo;
            _descripcion.text = c.descripcion ?? "";
            _descripcion.gameObject.SetActive(!string.IsNullOrEmpty(c.descripcion));

            bool conTecla = !string.IsNullOrEmpty(c.atajo);
            _tecla.gameObject.SetActive(conTecla);
            if (conTecla) _atajo.text = c.atajo;

            bool cuesta = c.oro > 0 || c.madera > 0 || c.poblacion > 0 || c.segundos > 0f || c.golpes > 0;
            _filaCoste.gameObject.SetActive(cuesta);
            _iconoOro.gameObject.SetActive(c.oro > 0); _oro.gameObject.SetActive(c.oro > 0);
            _iconoMadera.gameObject.SetActive(c.madera > 0); _madera.gameObject.SetActive(c.madera > 0);
            _oro.text = c.oro.ToString();
            _madera.text = c.madera.ToString();
            var extra = new System.Collections.Generic.List<string>();
            if (c.poblacion > 0) extra.Add($"{c.poblacion} de pobl.");
            if (c.segundos > 0f) extra.Add($"{Mathf.CeilToInt(c.segundos)} s");
            if (c.golpes > 0) extra.Add($"{c.golpes} martillazos");
            _extra.text = string.Join("  ·  ", extra);

            _bloqueo.text = c.bloqueo ?? "";
            _bloqueo.gameObject.SetActive(!string.IsNullOrEmpty(c.bloqueo));

            Colocar(boton);
            _objetivo = 1f;
        }

        public void Ocultar() => _objetivo = 0f;

        /// <summary>Encima del botón, centrada, y dentro de la pantalla.</summary>
        void Colocar(RectTransform boton)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_raiz);
            var lienzo = _raiz.parent as RectTransform;
            var esquinas = new Vector3[4];
            boton.GetWorldCorners(esquinas);
            Vector2 arriba = (esquinas[1] + esquinas[2]) * 0.5f;   // centro del borde de arriba
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lienzo, arriba, null, out var local);
            local += lienzo.rect.size * lienzo.pivot;               // a coordenadas desde la esquina
            float medio = _raiz.rect.width * 0.5f;
            local.x = Mathf.Clamp(local.x, medio + 8f, lienzo.rect.width - medio - 8f);
            local.y = Mathf.Min(local.y + 10f, lienzo.rect.height - _raiz.rect.height - 8f);
            _raiz.anchoredPosition = local;
        }

        void Update()
        {
            // Aparece en una décima y sube un poco: se nota sin distraer.
            _visible = Mathf.MoveTowards(_visible, _objetivo, Time.unscaledDeltaTime / 0.1f);
            _grupo.alpha = _visible;
            _raiz.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, _visible);
        }
    }
}
