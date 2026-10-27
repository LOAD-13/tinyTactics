using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Entrada;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El botón de pawns ociosos, junto a la población: la cara del pawn y cuántos están
    /// sin hacer nada. Un clic (o la tecla O) selecciona el siguiente y lleva la cámara a él
    /// (semana 15).
    /// </summary>
    /// <remarks>
    /// Un pawn parado es oro que no entra, y en un RTS es el error más caro y el más fácil de
    /// no ver: el pawn está quieto en una esquina que no estás mirando. Por eso el botón solo
    /// aparece cuando hay alguno, y late un poco para que se note.
    /// <para>
    /// Ocioso es lo mismo que usa el bot (semana 10): ni recolecta ni construye. Y además
    /// está quieto: uno que camina a donde lo mandaste no está ocioso, va de camino.
    /// </para>
    /// </remarks>
    public class PawnsOciosos : MonoBehaviour
    {
        const float Refresco = 0.5f;

        readonly List<Unidad> _ociosos = new List<Unidad>();
        Text _cuenta;
        CanvasGroup _grupo;
        RectTransform _raiz;
        float _proximo;
        int _siguiente;

        public static PawnsOciosos Crear(RectTransform padre, TemaInterfaz tema, Font fuente, Vector2 tamano, Vector2 posicion)
        {
            var go = new GameObject("Pawns ociosos", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = posicion;
            rt.sizeDelta = tamano;

            var fondo = go.AddComponent<Image>();
            fondo.sprite = tema != null ? tema.CajaChicaDeColor(2) : null;
            fondo.type = Image.Type.Sliced;

            var cara = new GameObject("Cara", typeof(RectTransform)).AddComponent<Image>();
            cara.transform.SetParent(rt, false);
            cara.sprite = tema != null ? tema.RetratoDeColor(TipoUnidad.Pawn, 2) : null;
            cara.preserveAspect = true;
            cara.raycastTarget = false;
            var crt = cara.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.32f, 0.56f);
            crt.sizeDelta = new Vector2(tamano.y * 0.72f, tamano.y * 0.72f);

            var cuenta = new GameObject("Cuenta", typeof(RectTransform)).AddComponent<Text>();
            cuenta.transform.SetParent(rt, false);
            cuenta.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cuenta.fontSize = 26;
            cuenta.fontStyle = FontStyle.Bold;
            cuenta.color = new Color(1f, 0.98f, 0.92f);
            cuenta.alignment = TextAnchor.MiddleCenter;
            cuenta.raycastTarget = false;
            var contorno = cuenta.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.05f, 0.04f, 0.03f, 0.95f);
            contorno.effectDistance = new Vector2(1.8f, -1.8f);
            var trt = cuenta.rectTransform;
            trt.anchorMin = new Vector2(0.55f, 0f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(0f, 10f); trt.offsetMax = new Vector2(-8f, 0f);

            var boton = go.AddComponent<Button>();
            boton.targetGraphic = fondo;

            var p = go.AddComponent<PawnsOciosos>();
            p._cuenta = cuenta;
            p._raiz = rt;
            p._grupo = go.AddComponent<CanvasGroup>();
            boton.onClick.AddListener(p.IrAlSiguiente);

            go.AddComponent<AvisoDeBoton>().Configurar(() => new FichaEmergente.Contenido
            {
                titulo = "Pawns ociosos",
                atajo = "O",
                descripcion = "Pawns que ni recolectan ni construyen. Clic para ir al siguiente.",
            });
            return p;
        }

        /// <summary>Los pawns de <paramref name="faccion"/> que están sin hacer nada.</summary>
        public static void Buscar(int faccion, List<Unidad> salida)
        {
            salida.Clear();
            var todas = RegistroDeUnidades.Todas;
            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva || u.faccion != faccion) continue;
                if (u.datos == null || u.datos.tipo != TipoUnidad.Pawn) continue;

                var recolector = u.GetComponent<RecolectorPawn>();
                var constructor = u.GetComponent<ConstructorPawn>();
                if ((recolector != null && recolector.Recolectando) || (constructor != null && constructor.Construyendo)) continue;

                var maquina = u.GetComponent<MaquinaDeEstados>();
                if (maquina != null && maquina.Estado != EstadoUnidad.Reposo) continue;

                salida.Add(u);
            }
        }

        void Update()
        {
            var selector = SelectorDeUnidades.Actual;
            bool jugando = selector != null && !selector.soloObservar;

            if (Time.unscaledTime >= _proximo)
            {
                _proximo = Time.unscaledTime + Refresco;
                if (jugando) Buscar(selector.faccionJugador, _ociosos); else _ociosos.Clear();
                _cuenta.text = _ociosos.Count.ToString();
            }

            bool hay = _ociosos.Count > 0;
            _grupo.alpha = Mathf.MoveTowards(_grupo.alpha, hay ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            _grupo.blocksRaycasts = hay;
            _grupo.interactable = hay;

            // Un latido suave mientras haya alguno parado.
            float latido = hay ? 1f + 0.04f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
            _raiz.localScale = new Vector3(latido, latido, 1f);

            var teclado = Keyboard.current;
            if (hay && jugando && teclado != null && teclado.oKey.wasPressedThisFrame) IrAlSiguiente();
        }

        /// <summary>Selecciona el siguiente pawn ocioso y lleva la cámara hasta él.</summary>
        public void IrAlSiguiente()
        {
            var selector = SelectorDeUnidades.Actual;
            if (selector == null || selector.soloObservar) return;
            Buscar(selector.faccionJugador, _ociosos);
            if (_ociosos.Count == 0) return;

            var pawn = _ociosos[_siguiente++ % _ociosos.Count];
            selector.SeleccionarGrupo(new[] { pawn });
            var camara = CamaraRTS.Actual;
            if (camara != null) camara.CentrarEn(pawn.transform.position);
            Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.SeleccionarUnidad);
        }
    }
}
