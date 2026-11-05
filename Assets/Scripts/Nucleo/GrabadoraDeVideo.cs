using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Graba una toma de la partida para el vídeo de lanzamiento (semana 17).
    /// </summary>
    /// <remarks>
    /// <b>Sesenta fotogramas por segundo exactos, vaya como vaya el equipo.</b> El juego avanza
    /// 1/60 de segundo por fotograma (<c>Time.captureDeltaTime</c>) y guarda cada imagen, así que
    /// una toma con 289 unidades sale igual de fluida que una con cinco. La cámara no es la del
    /// jugador: se mueve sola de un punto a otro, con un paneo y un zoom suavizados.
    /// <code>
    /// "Tiny Tactics.exe" -autopartidas 1 -sinjugador -mapa "Cuatro Vientos" -grabar C:\tomas\mundo
    ///     -desde 2 -segundos 8 -camara base0 -hasta centro -zoom 9 -zoomfin 14 -hora dia -sinhud
    /// </code>
    /// </remarks>
    public class GrabadoraDeVideo : MonoBehaviour
    {
        public static bool Activa { get; private set; }

        string _carpeta, _desde = "centro", _hasta;
        float _segundos = 8f, _zoom = 10f, _zoomFin = -1f, _empezarEn = 60f;
        string _hora;
        bool _sinHud;
        int _fotograma, _total;
        bool _grabando;
        Camera _camara;
        RenderTexture _rt;
        Texture2D _lectura;

        public static GrabadoraDeVideo Crear(string[] args)
        {
            string Valor(string clave)
            {
                int i = Array.IndexOf(args, clave);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }

            var carpeta = Valor("-grabar");
            if (carpeta == null) return null;

            var go = new GameObject("GrabadoraDeVideo");
            DontDestroyOnLoad(go);
            var g = go.AddComponent<GrabadoraDeVideo>();
            g._carpeta = carpeta;
            if (float.TryParse(Valor("-desde"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float d)) g._empezarEn = d * 60f;
            if (float.TryParse(Valor("-segundos"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s)) g._segundos = s;
            if (float.TryParse(Valor("-zoom"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z)) g._zoom = z;
            if (float.TryParse(Valor("-zoomfin"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float zf)) g._zoomFin = zf;
            g._desde = Valor("-camara") ?? "centro";
            g._hasta = Valor("-hasta");
            g._hora = Valor("-hora");
            g._sinHud = Array.IndexOf(args, "-sinhud") >= 0;
            Directory.CreateDirectory(carpeta);
            return g;
        }

        /// <summary>¿Toca empezar? Lo pregunta <see cref="PartidasAutomaticas"/> cada fotograma.</summary>
        public bool Empezar(float duracionPartida)
        {
            if (_grabando || duracionPartida < _empezarEn) return _grabando;

            _grabando = Activa = true;
            _total = Mathf.RoundToInt(_segundos * 60f);
            Time.captureDeltaTime = 1f / 60f;

            var camaraRts = FindFirstObjectByType<Entrada.CamaraRTS>();
            if (camaraRts != null) camaraRts.enabled = false;
            _camara = Camera.main;

            var niebla = Mundo.NieblaDeGuerra.Actual;
            if (niebla != null) { niebla.ignorarNiebla = true; niebla.Refrescar(); }

            var ciclo = Mundo.CicloDelDia.Actual;
            if (ciclo != null && _hora != null)
            {
                ciclo.Ir(_hora == "noche" ? Mundo.HoraDelDia.Noche : _hora == "mediodia" ? Mundo.HoraDelDia.Mediodia : Mundo.HoraDelDia.Dia);
                ciclo.detenido = true;
            }

            if (_sinHud)
            {
                foreach (var lienzo in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) lienzo.enabled = false;
                var panel = Pruebas.PanelDePruebas.Actual;
                if (panel != null) panel.enabled = false;
                foreach (var cartel in FindObjectsByType<Interfaz.CartelDeFinal>(FindObjectsSortMode.None)) cartel.enabled = false;
            }

            StartCoroutine(Grabar());
            return true;
        }

        IEnumerator Grabar()
        {
            // Sin HUD, la cámara pinta en una textura fija de 1920x1080: la toma sale a esa
            // resolución aunque la ventana sea más pequeña (la ventana se ajusta a la pantalla).
            if (_sinHud && _camara != null)
            {
                _rt = new RenderTexture(1920, 1080, 24);
                _lectura = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                _camara.targetTexture = _rt;
            }

            var a = Punto(_desde);
            var b = _hasta != null ? Punto(_hasta) : a;
            float zFin = _zoomFin > 0f ? _zoomFin : _zoom;

            while (_fotograma < _total)
            {
                // Suavizado de entrada y salida: un paneo lineal se nota mecánico.
                float t = _total <= 1 ? 1f : (float)_fotograma / (_total - 1);
                float s = t * t * (3f - 2f * t);
                if (_camara != null)
                {
                    var p = Vector2.Lerp(a, b, s);
                    _camara.transform.position = new Vector3(p.x, p.y, _camara.transform.position.z);
                    _camara.orthographicSize = Mathf.Lerp(_zoom, zFin, s);
                }

                yield return new WaitForEndOfFrame();

                var ruta = Path.Combine(_carpeta, $"f{_fotograma:00000}.jpg");
                if (_rt != null)
                {
                    var previa = RenderTexture.active;
                    RenderTexture.active = _rt;
                    _lectura.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    RenderTexture.active = previa;
                    File.WriteAllBytes(ruta, _lectura.EncodeToJPG(92));
                }
                else
                {
                    var foto = ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(ruta, foto.EncodeToJPG(92));
                    Destroy(foto);
                }
                _fotograma++;
            }

            Debug.Log($"[Tiny Tactics] Toma grabada: {_total} fotogramas en {_carpeta}");
            Application.Quit();
        }

        /// <summary>«centro», «base0»…«base4» o «x,y» en casillas.</summary>
        static Vector2 Punto(string nombre)
        {
            var grilla = Mundo.MundoJuego.Actual != null ? Mundo.MundoJuego.Actual.Grilla : null;
            var centro = grilla != null ? new Vector2(grilla.Ancho / 2f, grilla.Alto / 2f) : Vector2.zero;
            if (nombre == "centro") return centro;

            if (nombre.StartsWith("base") && int.TryParse(nombre.Substring(4), out int b))
            {
                foreach (var e in Edificios.Edificio.Todos)
                    if (e != null && e.faccion == b && e.tipo == Datos.TipoEdificio.Castillo) return e.PosicionDePlanta;
                return centro;
            }

            var partes = nombre.Split(',');
            if (partes.Length == 2 && float.TryParse(partes[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(partes[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                return new Vector2(x, y);

            return centro;
        }
    }
}
