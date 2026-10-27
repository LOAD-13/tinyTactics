using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.Interfaz
{
    public enum TipoMensaje { Info, Logro, Aviso }

    /// <summary>
    /// Avisos cortos debajo del HUD de recursos: «Cuartel terminado», «Población llena»,
    /// «¡Te atacan!» (semana 15).
    /// </summary>
    /// <remarks>
    /// <b>Cintas del pack, centradas bajo los recursos.</b> Es donde el ojo ya va a mirar
    /// cuánto oro tiene, y no tapa ni el mapa del centro ni el panel de abajo. El color dice
    /// el tipo antes de leerlo: dorado lo bueno, rojo lo urgente, azul lo demás.
    /// <para>
    /// Como mucho tres a la vez. Un aviso que se repite no apila cintas iguales: se renueva y
    /// cuenta «×3». Entrenar cinco arqueros seguidos no puede tapar media pantalla.
    /// </para>
    /// <para>Solo habla al jugador: lo de los bots no se anuncia.</para>
    /// </remarks>
    public class MensajesEnPantalla : MonoBehaviour
    {
        public static MensajesEnPantalla Actual { get; private set; }

        const int Maximo = 3;
        const float Vida = 3.5f, Desvanecer = 0.6f, Alto = 46f, Separacion = 6f;

        class Cinta
        {
            public RectTransform Raiz;
            public CanvasGroup Grupo;
            public Text Texto;
            public string Clave;
            public int Veces = 1;
            public float Nacio;
        }

        readonly List<Cinta> _cintas = new List<Cinta>();
        TemaInterfaz _tema;
        Font _fuente;
        RectTransform _raiz;

        public static MensajesEnPantalla Crear(RectTransform padre, TemaInterfaz tema, Font fuente, Vector2 posicion)
        {
            var go = new GameObject("Mensajes en pantalla", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = posicion;
            rt.sizeDelta = new Vector2(600f, (Alto + Separacion) * Maximo);
            var m = go.AddComponent<MensajesEnPantalla>();
            m._tema = tema;
            m._fuente = fuente;
            m._raiz = rt;
            return m;
        }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>Un aviso para el jugador. Si no hay HUD (el menú, el editor), no pasa nada.</summary>
        public static void Decir(string texto, TipoMensaje tipo = TipoMensaje.Info)
        {
            if (Actual != null && !string.IsNullOrEmpty(texto)) Actual.Poner(texto, tipo);
        }

        /// <summary>¿Es del bando que lleva el jugador? Para no anunciar lo de los bots.</summary>
        public static bool EsDelJugador(int faccion)
        {
            var s = Entrada.SelectorDeUnidades.Actual;
            return s != null && !s.soloObservar && s.faccionJugador == faccion;
        }

        void Poner(string texto, TipoMensaje tipo)
        {
            // El mismo aviso otra vez: se renueva y cuenta, en vez de repetirse.
            foreach (var c in _cintas)
                if (c.Clave == texto)
                {
                    c.Veces++;
                    c.Nacio = Time.unscaledTime;
                    c.Texto.text = $"{texto}  ×{c.Veces}";
                    return;
                }

            if (_cintas.Count >= Maximo)
            {
                Destroy(_cintas[_cintas.Count - 1].Raiz.gameObject);
                _cintas.RemoveAt(_cintas.Count - 1);
            }

            _cintas.Insert(0, Montar(texto, tipo));
        }

        Cinta Montar(string texto, TipoMensaje tipo)
        {
            var go = new GameObject("Mensaje", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(_raiz, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            int color = tipo == TipoMensaje.Logro ? 2 : tipo == TipoMensaje.Aviso ? 1 : 0;
            var fondo = go.AddComponent<Image>();
            var cinta = _tema != null && _tema.listonesNombre != null && _tema.listonesNombre.Length > color
                ? _tema.listonesNombre[color] : null;
            fondo.sprite = cinta;
            fondo.type = Image.Type.Sliced;
            fondo.enabled = cinta != null;
            fondo.raycastTarget = false;

            var t = new GameObject("Texto", typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(rt, false);
            t.font = _fuente != null ? _fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(1f, 0.97f, 0.88f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = texto;
            var contorno = t.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.05f, 0.04f, 0.03f, 0.9f);
            contorno.effectDistance = new Vector2(1.6f, -1.6f);
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(0f, 4f); trt.offsetMax = Vector2.zero;   // la cinta tiene más reborde abajo

            // La cinta mide lo que el texto, con aire a los lados para las colas del dibujo.
            rt.sizeDelta = new Vector2(Mathf.Max(260f, t.preferredWidth + 110f), Alto);

            var grupo = go.AddComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            return new Cinta { Raiz = rt, Grupo = grupo, Texto = t, Clave = texto, Nacio = Time.unscaledTime };
        }

        void Update()
        {
            float ahora = Time.unscaledTime;
            for (int i = _cintas.Count - 1; i >= 0; i--)
            {
                var c = _cintas[i];
                float edad = ahora - c.Nacio;
                if (edad >= Vida)
                {
                    Destroy(c.Raiz.gameObject);
                    _cintas.RemoveAt(i);
                    continue;
                }

                // Entra cayendo un poco y se desvanece al final. La posición se acerca a su
                // sitio poco a poco: cuando llega una nueva, las demás bajan sin saltar.
                float entrada = Mathf.Clamp01(edad / 0.15f);
                c.Grupo.alpha = Mathf.Min(entrada, Mathf.Clamp01((Vida - edad) / Desvanecer));
                var destino = new Vector2(0f, -i * (Alto + Separacion) + (1f - entrada) * 12f);
                c.Raiz.anchoredPosition = Vector2.Lerp(c.Raiz.anchoredPosition, destino, Mathf.Clamp01(Time.unscaledDeltaTime * 14f));
            }
        }
    }
}
