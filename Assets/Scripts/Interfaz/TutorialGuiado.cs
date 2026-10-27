using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El tutorial: el capataz, un pawn, te enseña a jugar en seis pasos (semana 15).
    /// </summary>
    /// <remarks>
    /// <b>Un personaje que habla, no un manual.</b> La cara del capataz va en su marco a la
    /// izquierda del pergamino y se mueve mientras habla; el texto sale letra a letra con un
    /// murmullo, como en los juegos de rol de siempre. La idea es de Joaquín.
    /// <para>
    /// <b>Cada paso avanza solo cuando lo haces</b>, no con un botón de «siguiente»: así se
    /// aprende haciendo y no leyendo. Solo los mensajes que no piden nada (el saludo y la
    /// despedida) tienen botón. Se puede saltar en cualquier momento, y la partida sigue.
    /// </para>
    /// <para>
    /// Las condiciones miran el estado del juego —¿hay un pawn recolectando?, ¿hay una casa
    /// terminada más?— y no los clics: da igual si el jugador usó el atajo, el botón o el
    /// clic derecho, lo que cuenta es que lo consiguió.
    /// </para>
    /// </remarks>
    public class TutorialGuiado : MonoBehaviour
    {
        public const int Pasos = 6;

        class Paso
        {
            public string texto;
            public System.Func<bool> hecho;
            public string boton;     // si no es nulo, el paso avanza con un botón
        }

        TemaInterfaz _tema;
        Font _fuente;
        RectTransform _raiz, _cara;
        Text _texto, _numero, _textoBoton;
        Button _boton;
        CanvasGroup _grupo;
        readonly List<Paso> _pasos = new List<Paso>();
        int _actual = -1;
        string _completo = "";
        float _letras, _proximoMurmullo, _hablando;

        // Lo que había al empezar cada paso, para medir «una más».
        int _casasAlEmpezar, _guerrerosAlEmpezar;

        const int Jugador = 0;
        const float LetrasPorSegundo = 38f;

        public static TutorialGuiado Crear(Transform lienzo, TemaInterfaz tema, Font fuente)
        {
            var go = new GameObject("Tutorial", typeof(RectTransform));
            go.transform.SetParent(lienzo, false);
            var t = go.AddComponent<TutorialGuiado>();
            t._tema = tema;
            t._fuente = fuente;
            t.Montar();
            t.PrepararPasos();
            t.Ir(0);
            return t;
        }

        // -----------------------------------------------------------------
        // Los pasos
        // -----------------------------------------------------------------

        void PrepararPasos()
        {
            _pasos.Add(new Paso
            {
                texto = "¡Hola, jefe! Soy el capataz de tus pawns. En un momento te enseño " +
                        "lo básico para ganar una partida. ¿Empezamos?",
                boton = "Empezar",
            });
            _pasos.Add(new Paso
            {
                texto = "Primero, elige a un pawn: haz clic izquierdo sobre uno. También puedes " +
                        "arrastrar una caja para elegir varios a la vez.",
                hecho = () => HayPawnElegido(),
            });
            _pasos.Add(new Paso
            {
                texto = "¡Bien! Ahora haz clic derecho sobre la mina de oro o sobre un árbol. " +
                        "El pawn irá a trabajar solo y llevará lo que saque al castillo.",
                hecho = () => HayPawnRecolectando(),
            });
            _pasos.Add(new Paso
            {
                texto = "Cada casa da sitio para más unidades. Elige un pawn, pulsa B, escoge la " +
                        "Casa y colócala cerca del castillo.",
                hecho = () => Contar(TipoEdificio.Casa) > _casasAlEmpezar,
            });
            _pasos.Add(new Paso
            {
                texto = "Ahora el ejército. Construye un Cuartel igual que la casa y, cuando esté " +
                        "listo, elígelo y entrena un Guerrero.",
                hecho = () => ContarUnidades(TipoUnidad.Guerrero) > _guerrerosAlEmpezar,
            });
            _pasos.Add(new Paso
            {
                texto = "¡Un guerrero! Elige a tus soldados y haz clic derecho sobre el castillo " +
                        "enemigo, a la derecha del prado.",
                hecho = () => CastilloEnemigoHerido(),
            });
            _pasos.Add(new Paso
            {
                texto = "¡Eso es! Sigue así: derriba el castillo y la partida es tuya.",
                hecho = () => Ganado(),
            });
            _pasos.Add(new Paso
            {
                texto = "¡Lo lograste, jefe! Ya sabes lo básico. Cuando quieras, juega una partida " +
                        "de verdad desde el menú. ¡Suerte ahí fuera!",
                boton = "Volver al menú",
            });
        }

        void Ir(int paso)
        {
            _actual = paso;
            if (_actual >= _pasos.Count) { Navegacion.IrAlMenu(); return; }

            _casasAlEmpezar = Contar(TipoEdificio.Casa);
            _guerrerosAlEmpezar = ContarUnidades(TipoUnidad.Guerrero);

            var p = _pasos[_actual];
            _completo = p.texto;
            _letras = 0f;
            _texto.text = "";

            // Los pasos con tarea van numerados del 1 al 6; el saludo y la despedida, no.
            bool numerado = p.boton == null;
            _numero.transform.parent.gameObject.SetActive(numerado);
            if (numerado) _numero.text = $"Paso {_actual} de {Pasos}";

            _boton.gameObject.SetActive(p.boton != null);
            if (p.boton != null) _textoBoton.text = p.boton;
        }

        void Pulsar()
        {
            Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.BotonPanel);
            // Un clic con el texto a medias lo completa; otro, avanza.
            if (_letras < _completo.Length) { _letras = _completo.Length; return; }
            if (_actual == _pasos.Count - 1) { Navegacion.IrAlMenu(); return; }
            Ir(_actual + 1);
        }

        void Saltar()
        {
            Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.BotonPanel);
            gameObject.SetActive(false);
        }

        // -----------------------------------------------------------------
        // Las condiciones: el estado del juego, no los clics
        // -----------------------------------------------------------------

        static bool HayPawnElegido()
        {
            var s = SelectorDeUnidades.Actual;
            if (s == null) return false;
            foreach (var u in s.Seleccionadas)
                if (u != null && u.faccion == Jugador && u.datos != null && u.datos.tipo == TipoUnidad.Pawn) return true;
            return false;
        }

        static bool HayPawnRecolectando()
        {
            foreach (var u in RegistroDeUnidades.Todas)
            {
                if (u == null || u.faccion != Jugador || !u.Viva) continue;
                var r = u.GetComponent<RecolectorPawn>();
                if (r != null && r.Recolectando) return true;
            }
            return false;
        }

        /// <summary>Edificios terminados de un tipo; una obra a medias no cuenta.</summary>
        static int Contar(TipoEdificio tipo)
        {
            int n = 0;
            foreach (var e in Edificio.Todos)
                if (e != null && e.faccion == Jugador && e.tipo == tipo && e.GetComponent<ObraEnConstruccion>() == null) n++;
            return n;
        }

        static int ContarUnidades(TipoUnidad tipo)
        {
            int n = 0;
            foreach (var u in RegistroDeUnidades.Todas)
                if (u != null && u.Viva && u.faccion == Jugador && u.datos != null && u.datos.tipo == tipo) n++;
            return n;
        }

        static bool CastilloEnemigoHerido()
        {
            foreach (var e in Edificio.Todos)
                if (e != null && e.faccion != Jugador && e.centroDeEntrega && e.FraccionDeVida < 0.999f) return true;
            return Ganado();
        }

        static bool Ganado()
        {
            var a = ArbitroDePartida.Actual;
            return a != null && a.Terminada && a.DesenlacePara(Jugador) == Desenlace.Victoria;
        }

        // -----------------------------------------------------------------
        // Dibujo
        // -----------------------------------------------------------------

        void Montar()
        {
            // Arriba a la derecha (pedido por Joaquín): abajo lo taparía el panel de la
            // selección, y a la izquierda tapaba la base. La cara a la derecha, mirando al texto.
            _raiz = (RectTransform)transform;
            _raiz.anchorMin = _raiz.anchorMax = new Vector2(1f, 1f);
            _raiz.pivot = new Vector2(1f, 1f);
            _raiz.anchoredPosition = new Vector2(-24f, -20f);
            _raiz.sizeDelta = new Vector2(640f, 230f);
            _grupo = gameObject.AddComponent<CanvasGroup>();

            // El marco de la cara, del color del jugador.
            var marco = Imagen(_raiz, "Marco", _tema != null ? _tema.CajaChicaDe(Jugador) : null, true);
            Poner(marco.rectTransform, new Vector2(1f, 0.5f), new Vector2(180f, 180f), new Vector2(-90f, 14f));
            var cara = Imagen(marco.rectTransform, "Cara", _tema != null ? _tema.RetratoDe(TipoUnidad.Pawn, Jugador) : null, false);
            cara.preserveAspect = true;
            _cara = cara.rectTransform;
            Poner(_cara, new Vector2(0.5f, 0.5f), new Vector2(150f, 150f), new Vector2(0f, 6f));

            // El nombre, en una cinta bajo la cara.
            var cinta = Imagen(_raiz, "Nombre", _tema != null && _tema.listonesNombre != null && _tema.listonesNombre.Length > 0 ? _tema.listonesNombre[0] : null, true);
            Poner(cinta.rectTransform, new Vector2(1f, 0.5f), new Vector2(180f, 44f), new Vector2(-90f, -88f));
            var nombre = Texto(cinta.rectTransform, "Texto", "Capataz", 24, new Color(1f, 0.97f, 0.88f), TextAnchor.MiddleCenter, true);
            Llenar(nombre.rectTransform, new Vector2(0f, 4f));

            // El pergamino del diálogo.
            var papel = Imagen(_raiz, "Dialogo", _tema != null && _tema.papel != null ? KitInterfaz.ConBorde(_tema.papel, new Vector4(28, 28, 28, 28)) : null, true);
            Poner(papel.rectTransform, new Vector2(1f, 0.5f), new Vector2(450f, 220f), new Vector2(-190f - 225f, 0f));
            _texto = Texto(papel.rectTransform, "Texto", "", 21, new Color(0.22f, 0.15f, 0.09f), TextAnchor.UpperLeft, false);
            _texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            var trt = _texto.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(34f, 58f); trt.offsetMax = new Vector2(-30f, -30f);

            // «Paso 2 de 6», en una cinta pequeña colgando del borde de arriba.
            var etiqueta = Imagen(papel.rectTransform, "Paso", _tema != null && _tema.listonesNombre != null && _tema.listonesNombre.Length > 2 ? _tema.listonesNombre[2] : null, true);
            Poner(etiqueta.rectTransform, new Vector2(1f, 1f), new Vector2(170f, 40f), new Vector2(-100f, 4f));
            _numero = Texto(etiqueta.rectTransform, "Texto", "", 18, new Color(1f, 0.97f, 0.88f), TextAnchor.MiddleCenter, true);
            Llenar(_numero.rectTransform, new Vector2(0f, 4f));

            // Abajo: el botón del paso (si lo tiene) y «Saltar tutorial».
            _boton = Boton(papel.rectTransform, _tema != null ? _tema.BotonDeColor(0) : null, out _textoBoton, Pulsar);
            Poner((RectTransform)_boton.transform, new Vector2(1f, 0f), new Vector2(190f, 52f), new Vector2(-120f, 40f));
            var saltar = Boton(papel.rectTransform, _tema != null ? _tema.BotonDeColor(1) : null, out var textoSaltar, Saltar);
            textoSaltar.text = "Saltar tutorial";
            textoSaltar.fontSize = 17;
            Poner((RectTransform)saltar.transform, new Vector2(0f, 0f), new Vector2(170f, 44f), new Vector2(110f, 38f));
        }

        void Update()
        {
            if (_actual < 0 || _actual >= _pasos.Count) return;

            // El texto sale letra a letra, con un murmullo cada pocas letras.
            int antes = (int)_letras;
            _letras = Mathf.Min(_completo.Length, _letras + Time.unscaledDeltaTime * LetrasPorSegundo);
            int ahora = (int)_letras;
            if (ahora != antes)
            {
                _texto.text = _completo.Substring(0, ahora);
                char c = _completo[ahora - 1];
                if (char.IsLetter(c) && Time.unscaledTime >= _proximoMurmullo)
                {
                    Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.Murmullo);
                    _proximoMurmullo = Time.unscaledTime + 0.085f;
                }
                _hablando = 0.15f;
            }
            _hablando -= Time.unscaledDeltaTime;

            // El capataz se mueve: brinca y se estira al hablar, respira cuando calla. Va
            // volteado (x negativa) para mirar al texto, que tiene a su izquierda.
            float t = Time.unscaledTime;
            bool habla = _hablando > 0f;
            float salto = habla ? Mathf.Abs(Mathf.Sin(t * 16f)) * 7f : Mathf.Sin(t * 2f) * 2f;
            float estira = habla ? 1f + 0.05f * Mathf.Sin(t * 16f) : 1f + 0.015f * Mathf.Sin(t * 2f);
            _cara.anchoredPosition = new Vector2(0f, 6f + salto);
            _cara.localScale = new Vector3(-(2f - estira), estira, 1f);
            _cara.localRotation = Quaternion.Euler(0f, 0f, habla ? 3f * Mathf.Sin(t * 8f) : 0f);

            // El paso se cumple: un respiro y al siguiente, para que se lea el «¡bien!».
            var p = _pasos[_actual];
            if (p.hecho != null && _letras >= _completo.Length && p.hecho())
            {
                Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.EdificioTerminado);
                Ir(_actual + 1);
            }
        }

        // -----------------------------------------------------------------

        Image Imagen(RectTransform padre, string nombre, Sprite sprite, bool cortes)
        {
            var img = new GameObject(nombre, typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(padre, false);
            img.sprite = sprite;
            img.type = cortes ? Image.Type.Sliced : Image.Type.Simple;
            img.enabled = sprite != null;
            img.raycastTarget = cortes;
            return img;
        }

        Text Texto(RectTransform padre, string nombre, string texto, int tam, Color color, TextAnchor ancla, bool contorno)
        {
            var t = new GameObject(nombre, typeof(RectTransform)).AddComponent<Text>();
            t.transform.SetParent(padre, false);
            t.font = _fuente != null ? _fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = tam;
            t.color = color;
            t.alignment = ancla;
            t.raycastTarget = false;
            t.text = texto;
            if (contorno)
            {
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.05f, 0.04f, 0.03f, 0.9f);
                o.effectDistance = new Vector2(1.5f, -1.5f);
            }
            return t;
        }

        Button Boton(RectTransform padre, Sprite fondo, out Text texto, System.Action accion)
        {
            var img = Imagen(padre, "Boton", fondo, true);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => accion());
            texto = Texto(img.rectTransform, "Texto", "", 20, new Color(1f, 0.97f, 0.88f), TextAnchor.MiddleCenter, true);
            Llenar(texto.rectTransform, new Vector2(0f, 4f));
            return b;
        }

        static void Poner(RectTransform rt, Vector2 ancla, Vector2 tam, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = ancla;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tam;
            rt.anchoredPosition = pos;
        }

        static void Llenar(RectTransform rt, Vector2 abajo)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = abajo; rt.offsetMax = Vector2.zero;
        }
    }
}
