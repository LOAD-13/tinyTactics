using System;
using UnityEngine;
using TinyTactics.IA;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Juega partidas sin nadie delante, desde la línea de comandos (semana 16).
    /// </summary>
    /// <remarks>
    /// Sirve para probar la IA sin pasar una tarde mirando: se lanza el ejecutable con
    /// <c>-autopartidas N</c> y juega N partidas seguidas con un bot también en la base 1,
    /// guarda el informe de cada una en el historial y se cierra. Con <c>-batchmode
    /// -nographics</c> ni siquiera abre ventana.
    /// <para>
    /// <b>El tiempo es fijo, no real.</b> Cada fotograma avanza la partida exactamente
    /// <c>velocidad / 60</c> segundos y no se espera al reloj: es lo mismo que jugar a x4 en
    /// el panel, pero tan rápido como dé la CPU.
    /// </para>
    /// <code>
    /// "Tiny Tactics.exe" -batchmode -nographics -autopartidas 6 -mapa "Lago Helado"
    ///     -rivales Dificil -yo Dificil -reparto fijo -limite 30
    /// </code>
    /// <c>-comojugador</c> guarda las partidas como del jugador y marcadas de demostración: es
    /// lo que llena el historial de ejemplo de la entrega.
    /// </remarks>
    public class PartidasAutomaticas : MonoBehaviour
    {
        public static bool Activas { get; private set; }

        /// <summary>Las partidas cuentan como del jugador, marcadas de demostración.</summary>
        public static bool ComoJugador { get; private set; }

        /// <summary>Todas las bases para bots, sin asiento de jugador: para buscar sesgos del asiento.</summary>
        static bool _sinJugador;

        /// <summary>El bot probado cambia de base en cada partida: así la base no decide el resultado.</summary>
        static bool _alternar;

        /// <summary>-recursos infinitos: reglas de sandbox para la prueba de carga.</summary>
        static bool _infinitos;

        /// <summary>-carga N: al empezar a medir aparecen N tropas por bando en el centro, como la prueba de la semana 14.</summary>
        int _carga;
        float _capturaEn = -1f;
        GrabadoraDeVideo _grabadora;
        bool _cargado;
        static int _partidaActual;

        int _restantes;
        string[] _mapas = { "Lago Helado" };
        NivelDeIA[] _rivales = { NivelDeIA.Normal };
        NivelDeIA _yo = NivelDeIA.Normal;
        float _paso = 4f / 60f;
        float _limite = 30f * 60f;
        int _jugadas;
        bool _enPartida, _cerebroPuesto;
        int _frameDeCarga;
        bool _tacticaYo = true, _tacticaRivales = true;
        string[] _sinRivales = new string[0];

        // -medir: minuto de partida en que se pasa a tiempo real y se mide el rendimiento.
        float _medirEn = -1f;
        bool _midiendo;
        float _finVisto = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            var args = Environment.GetCommandLineArgs();
            string Valor(string clave)
            {
                int i = Array.IndexOf(args, clave);
                return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
            }

            if (!int.TryParse(Valor("-autopartidas"), out int n) || n <= 0) return;

            // Sin nadie mirando, la ventana puede perder el foco: el juego no se pausa por eso.
            Application.runInBackground = true;

            var go = new GameObject("PartidasAutomaticas");
            DontDestroyOnLoad(go);
            var auto = go.AddComponent<PartidasAutomaticas>();
            auto._restantes = n;

            if (Valor("-mapa") is string mapas) auto._mapas = mapas.Split(',');
            if (Valor("-rivales") is string rivales)
                auto._rivales = Array.ConvertAll(rivales.Split(','), r => (NivelDeIA)Enum.Parse(typeof(NivelDeIA), r.Trim()));
            if (Valor("-yo") is string yo) auto._yo = (NivelDeIA)Enum.Parse(typeof(NivelDeIA), yo);
            if (float.TryParse(Valor("-velocidad"), out float v) && v > 0f) auto._paso = v / 60f;
            if (float.TryParse(Valor("-limite"), out float m) && m > 0f) auto._limite = m * 60f;
            PlanDeIA.RepartoPorReservas = Valor("-reparto") != "fijo";
            ComoJugador = Array.IndexOf(args, "-comojugador") >= 0;
            _sinJugador = Array.IndexOf(args, "-sinjugador") >= 0;
            _alternar = Array.IndexOf(args, "-alternar") >= 0;
            _infinitos = Valor("-recursos") == "infinitos";
            if (int.TryParse(Valor("-carga"), out int carga)) auto._carga = carga;
            auto._grabadora = GrabadoraDeVideo.Crear(args);
            if (Valor("-historial") is string carpeta) HistorialDePartidas.CarpetaForzada = carpeta;
            // Táctica vieja o nueva por lado: para enfrentarlas en la misma partida.
            if (Valor("-sinpatron") is string sin)
                foreach (var p in sin.Split(',')) PlanDeIA.PatronesApagados.Add(p.Trim());
            if (float.TryParse(Valor("-medir"), out float medir) && medir > 0f) auto._medirEn = medir * 60f;
            auto._tacticaYo = Valor("-tacticayo") != "vieja";
            if (Valor("-sinpatronrivales") is string sinr) auto._sinRivales = sinr.Split(',');
            auto._tacticaRivales = Valor("-tacticarivales") != "vieja";
            Activas = true;

            Debug.Log($"[Tiny Tactics] Partidas automáticas: {n}, mapas {string.Join("/", auto._mapas)}, " +
                      $"reparto {(PlanDeIA.RepartoPorReservas ? "por reservas" : "fijo")}");
        }

        void Update()
        {
            // Las opciones del jugador ponen un tope de FPS al cargar cada escena: aquí no hay
            // nadie mirando, así que se quita en cada fotograma.
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;

            // Grabando, el paso de tiempo lo pone la grabadora: 1/60 exacto.
            if (GrabadoraDeVideo.Activa) return;

            if (_midiendo)
            {
                // Se mide a tiempo real y sin tope de FPS: lo que da la máquina de verdad.
                Time.captureDeltaTime = 0f;
                // La captura, unos segundos después: con las tropas ya peleando.
                if (_capturaEn > 0f && Time.unscaledTime >= _capturaEn && HistorialDePartidas.CarpetaForzada != null)
                {
                    _capturaEn = -1f;
                    ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(HistorialDePartidas.CarpetaForzada, "medida.png"));
                }
                if (Pruebas.MedidorDeRendimiento.Actual == null)
                {
                    Debug.Log($"[Tiny Tactics] Rendimiento: {Pruebas.MedidorDeRendimiento.Resultado}");
                    Application.Quit();
                }
                return;
            }

            Time.captureDeltaTime = _paso;

            var c = ArranqueDePartida.Configuracion;
            if (c == null) return;

            if (c.modo == ModoDePartida.Menu)
            {
                if (!_enPartida) Siguiente();
                return;
            }

            // Después del Start del arranque, que quita el cerebro de la base del jugador.
            if (!_cerebroPuesto && c.Jugador < 0) _cerebroPuesto = true;
            if (!_cerebroPuesto && Time.frameCount > _frameDeCarga + 3)
            {
                // La base del jugador también la lleva un bot, como «Bot contra bot» del panel.
                var cerebro = CerebroIA.De(c.Jugador) ?? CerebroIA.Crear(c.Jugador);
                if (cerebro != null)
                {
                    cerebro.PonerDificultad(_yo);
                    cerebro.pensando = true;
                }
                foreach (var otro in CerebroIA.Todos)
                    if (otro != null)
                    {
                        otro.tacticaAgrupada = otro.faccion == c.Jugador ? _tacticaYo : _tacticaRivales;
                        if (otro.faccion != c.Jugador) foreach (var p in _sinRivales) otro.patronesApagados.Add(p.Trim());
                    }
                _cerebroPuesto = true;
            }

            var arbitro = ArbitroDePartida.Actual;
            var libro = EstadisticasPartida.Actual;

            if (_grabadora != null && libro != null && _grabadora.Empezar(libro.Duracion))
            {
                if (_carga > 0 && !_cargado) { _cargado = true; Cargar(c.bases.Length); }
                return;
            }

            if (_medirEn > 0f && libro != null && libro.Duracion >= _medirEn && !(arbitro != null && arbitro.Terminada))
            {
                _midiendo = true;
                Time.captureDeltaTime = 0f;
                if (_carga > 0) Cargar(c.bases.Length);
                Pruebas.MedidorDeRendimiento.Empezar();
                _capturaEn = Time.unscaledTime + 5f;
                return;
            }
            bool acabada = (arbitro != null && arbitro.Terminada) ||
                           (libro != null && libro.Duracion > _limite);
            if (!acabada) return;

            // Un segundo de margen para que el cronista escriba el informe antes de cambiar de escena.
            if (_finVisto < 0f) _finVisto = Time.unscaledTime;
            if (Time.unscaledTime - _finVisto < 1f) return;

            Debug.Log($"[Tiny Tactics] Partida {_jugadas} terminada: ganó el bando {(arbitro != null ? arbitro.Ganador + 1 : 0)}");
            Siguiente();
        }

        void Siguiente()
        {
            if (_restantes <= 0)
            {
                Debug.Log("[Tiny Tactics] Partidas automáticas terminadas.");
                Application.Quit();
                return;
            }

            _restantes--;
            string nombre = _mapas[_jugadas % _mapas.Length].Trim();
            var rival = _rivales[_jugadas % _rivales.Length];
            _jugadas++;

            var config = Configurar(nombre, rival);
            if (config == null)
            {
                Debug.LogError($"[Tiny Tactics] No encuentro el mapa «{nombre}».");
                Application.Quit();
                return;
            }

            _enPartida = true;
            _cerebroPuesto = false;
            _finVisto = -1f;
            _frameDeCarga = Time.frameCount;
            Navegacion.Empezar(config);
        }

        /// <summary>
        /// La prueba de carga: un ejército por bando alrededor del centro del mapa, lo bastante
        /// cerca para que se encuentren enseguida. Usa «Aparecer» del panel de pruebas.
        /// </summary>
        void Cargar(int bandos)
        {
            // También con el panel apagado: la grabadora sin HUD lo apaga justo antes.
            var panel = Pruebas.PanelDePruebas.Actual ?? UnityEngine.Object.FindFirstObjectByType<Pruebas.PanelDePruebas>();
            var grilla = Mundo.MundoJuego.Actual != null ? Mundo.MundoJuego.Actual.Grilla : null;
            if (panel == null || grilla == null) return;

            var tipos = new[] { Datos.TipoUnidad.Guerrero, Datos.TipoUnidad.Lancero, Datos.TipoUnidad.Arquero };
            var centro = new Vector2(grilla.Ancho / 2f, grilla.Alto / 2f);

            for (int b = 0; b < bandos; b++)
            {
                panel.CambiarBando(b);
                float angulo = b * Mathf.PI * 2f / bandos;
                var foco = centro + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 8f;

                for (int i = 0; i < _carga; i++)
                {
                    var celda = Vector2Int.RoundToInt(foco + new Vector2(i % 8 - 4, i / 8 - 4));
                    if (!grilla.CeldaTransitableCercana(celda, 6, out celda)) continue;
                    panel.Aparecer(tipos[i % tipos.Length], grilla.CeldaAMundo(celda));
                }
            }

            // Al final: cambiar de bando lleva la cámara a su castillo, y la batalla está en el centro.
            var camara = UnityEngine.Object.FindFirstObjectByType<Entrada.CamaraRTS>();
            if (camara != null) camara.CentrarEn(new Vector2(grilla.Ancho / 2f, grilla.Alto / 2f));
        }

        static ConfiguracionDePartida Configurar(string nombre, NivelDeIA rival)
        {
            Mundo.MapaGuardado mapa = null;
            int bases = 3;

            if (nombre != "Tres Coronas")
            {
                foreach (var e in Mundo.ArchivoDeMapas.Listar())
                    if (e.mapa.nombre == nombre) { mapa = e.mapa; break; }
                if (mapa == null) return null;
                bases = mapa.bases.Count;
            }

            var c = new ConfiguracionDePartida
            {
                modo = ModoDePartida.Partida,
                escena = Navegacion.EscenaDelMenu,
                bases = new Ocupante[bases],
                mapa = mapa,
            };

            var bot = rival == NivelDeIA.Facil ? Ocupante.BotFacil : rival == NivelDeIA.Dificil ? Ocupante.BotDificil : Ocupante.BotNormal;
            for (int i = 1; i < bases; i++) c.bases[i] = bot;
            c.bases[0] = _sinJugador ? bot : Ocupante.Jugador;
            if (_alternar && _partidaActual++ % 2 == 1)
                (c.bases[0], c.bases[bases - 1]) = (c.bases[bases - 1], c.bases[0]);
            c.colores = ReglasDeConfiguracion.ColoresDeFabrica(bases);
            if (_infinitos)
            {
                // La prueba de carga: los bots llegan al tope de población y pelean sin parar.
                c.modo = ModoDePartida.Sandbox;
                c.sandbox.recursos = RecursosIniciales.Infinitos;
            }
            return c;
        }
    }
}
