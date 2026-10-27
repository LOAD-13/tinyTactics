using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.IA;
using TinyTactics.Interfaz;
using TinyTactics.Mundo;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Convierte la escena de juego en la partida que se eligió en el menú.
    /// </summary>
    /// <remarks>
    /// <b>El mapa no se genera aquí: ya está generado.</b> Los mapas salen del generador del
    /// editor como escenas terminadas, con una base por bando. Lo que hace el arranque es
    /// <i>configurar</i> esa escena antes del primer fotograma: cerrar las bases que nadie
    /// ocupa, sentar al jugador en la suya, poner un cerebro con su dificultad en cada base de
    /// bot y decidir qué interfaz se ve.
    ///
    /// Por eso el menú y la partida son la misma escena. De fondo del menú hay una partida
    /// de bots de verdad, en el mismo mapa en el que luego se juega: no hay que mantener una
    /// escena de menú aparte que se quede vieja cada vez que cambia el mapa.
    ///
    /// Corre antes que nadie (<c>DefaultExecutionOrder</c>) porque el árbitro decide en su
    /// <c>Start</c> qué bandos empiezan, y una base cerrada no puede llegar a contar.
    /// </remarks>
    [DefaultExecutionOrder(-500)]
    [AddComponentMenu("Tiny Tactics/Arranque de partida")]
    public class ArranqueDePartida : MonoBehaviour
    {
        [Tooltip("Nombre de la escena tal como está en Build Settings.")]
        public string escena = "Juego";

        [Tooltip("Segundos que el fondo del menú espera tras terminar antes de empezar otra.")]
        [Range(1f, 30f)] public float esperaEntrePartidasDeFondo = 6f;

        [Tooltip("Velocidad de la partida de fondo del menú.")]
        [Range(1f, 3f)] public float velocidadDeFondo = 1.5f;

        public static ConfiguracionDePartida Configuracion { get; private set; }

        public static bool EsMenu => Configuracion != null && Configuracion.modo == ModoDePartida.Menu;

        readonly List<Unidad> _condenadas = new List<Unidad>();
        float _reinicioDeFondo = -1f;

        [Tooltip("Los tiles del terreno en los cinco colores, para construir mapas de archivo.")]
        public PaletaDeTerreno paleta;

        /// <summary>¿Se juega o edita un mapa de archivo en vez del de la escena?</summary>
        public static bool MapaDeArchivo => Configuracion != null && Configuracion.mapa != null;

        void Awake()
        {
            var arbitro = Object.FindFirstObjectByType<ArbitroDePartida>();
            int bandos = arbitro != null ? arbitro.bandos : 3;

            Navegacion.EscenaDelMenu = escena;
            if (ConfiguracionDePartida.Actual == null || ConfiguracionDePartida.Actual.mapa == null)
                Navegacion.BandosDelMenu = bandos;

            // Un mapa de archivo manda sobre el de la escena: su número de bases, su grilla y
            // su contenido. Todo eso se decide aquí, antes de que nadie más despierte.
            MundoJuego.MapaForzado = null;
            var pedida = ConfiguracionDePartida.Actual;
            if (pedida != null && pedida.mapa != null && pedida.escena == escena)
            {
                bandos = Mathf.Clamp(pedida.mapa.bases.Count, 1, 5);
                if (arbitro != null) arbitro.bandos = bandos;

                var eco = Object.FindFirstObjectByType<Economia>();
                if (eco != null) eco.bandos = bandos;

                // El panel de pruebas también: venía con los tres de Tres Coronas y en Cuatro
                // Vientos no dejaba pasarse al cuarto bando (semana 14).
                var panel = Object.FindFirstObjectByType<Pruebas.PanelDePruebas>(FindObjectsInactive.Include);
                if (panel != null) panel.bandos = bandos;

                MundoJuego.MapaForzado = pedida.mapa.AMapaGenerado();
                ConstructorEnPartida.Vaciar();
            }

            var c = ConfiguracionDePartida.Actual;
            if (c != null && c.modo == ModoDePartida.Menu && c.bases.Length != bandos)
                c = ConfiguracionDePartida.DeMenu(bandos, escena);
            if (c != null && c.modo == ModoDePartida.Editor && c.mapa != null)
            {
                // El editor no tiene bandos que validar: se queda con su mapa tal cual.
            }
            else if (c == null || c.escena != escena || c.bases.Length != bandos)
                c = ConfiguracionDePartida.PorDefecto(bandos, escena);

            Configuracion = c;
            ConfiguracionDePartida.Actual = c;

            DiarioDePartida.Activo = c.modo != ModoDePartida.Menu;
            if (DiarioDePartida.Activo) DiarioDePartida.NuevaPartida();

            DiarioDePartida.Anotar($"Arranque en modo {c.modo}: {string.Join(", ", c.bases)}");
        }

        void Start()
        {
            var c = Configuracion;

            if (c.mapa != null) ConstructorEnPartida.Construir(c.mapa, paleta, conBases: c.modo != ModoDePartida.Editor);

            if (c.modo == ModoDePartida.Editor)
            {
                PrepararEditor();
                Opciones.Aplicar();
                return;
            }

            for (int bando = 0; bando < c.bases.Length; bando++)
            {
                var ocupante = c.bases[bando];

                if (ocupante == Ocupante.Cerrada) Cerrar(bando);
                else if (ReglasDeConfiguracion.EsBot(ocupante)) PonerBot(bando, ReglasDeConfiguracion.Nivel(ocupante));
                else QuitarBot(bando);
            }

            // El color de cada base, antes de que nadie lea un dibujo. Un mapa de archivo ya
            // construye sus bases con las plantillas de su color: no hay nada que vestir.
            if (c.mapa == null)
                for (int bando = 0; bando < c.bases.Length; bando++)
                    if (c.bases[bando] != Ocupante.Cerrada) PreparacionDeBandos.Vestir(bando);

            int jugador = c.Jugador;
            if (jugador >= 0) SentarJugador(jugador);

            PrepararInterfaz(c.modo);

            if (c.modo == ModoDePartida.Menu) PrepararFondo(c);
            if (c.modo == ModoDePartida.Sandbox) AplicarSandbox(c);
            if (c.modo == ModoDePartida.Tutorial) PrepararTutorial();

            Opciones.Aplicar();
        }

        /// <summary>
        /// El editor de mapas: ni bandos, ni IA, ni niebla, ni final de partida.
        /// </summary>
        void PrepararEditor()
        {
            var cerebros = CerebroIA.Todos;
            for (int i = cerebros.Count - 1; i >= 0; i--)
                if (cerebros[i] != null) Destroy(cerebros[i].gameObject);

            var arbitro = Object.FindFirstObjectByType<ArbitroDePartida>();
            if (arbitro != null) arbitro.enabled = false;

            var niebla = NieblaDeGuerra.Actual ?? Object.FindFirstObjectByType<NieblaDeGuerra>();
            if (niebla != null) niebla.ignorarNiebla = true;

            var selector = Object.FindFirstObjectByType<SelectorDeUnidades>();
            if (selector != null) selector.soloObservar = true;

            var panel = Object.FindFirstObjectByType<Pruebas.PanelDePruebas>(FindObjectsInactive.Include);
            if (panel != null) panel.enabled = false;

            var menu = Object.FindFirstObjectByType<MenuPrincipal>(FindObjectsInactive.Include);
            if (menu != null) menu.gameObject.SetActive(false);

            var editor = Object.FindFirstObjectByType<EditorDeMapas>(FindObjectsInactive.Include);

            foreach (var lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (editor == null || !lienzo.transform.IsChildOf(editor.transform))
                    lienzo.enabled = false;

            Apagar<CartelDeFinal>();
            Apagar<Minimapa>();
            Apagar<ResaltadoDeNodo>();
            Apagar<GruposDeControl>();
            Apagar<MenuDePausa>();
            Apagar<CriaderoOvejas>();

            // El ciclo del día y las nubes solo cubren el mapa: al alejarse en el editor se
            // les veía el borde sobre la mesa. Mientras se dibuja, siempre es de día.
            Apagar<CicloDelDia>();
            foreach (var n in Object.FindObjectsByType<MantoDeNubes>(FindObjectsSortMode.None)) n.gameObject.SetActive(false);

            if (editor != null)
            {
                editor.gameObject.SetActive(true);
                editor.Abrir(Configuracion.mapa);
            }

            Time.timeScale = 1f;
        }

        /// <summary>
        /// El fondo del menú empieza a mitad de partida: con ejército y con recursos.
        /// </summary>
        /// <remarks>
        /// Una partida que empieza de cero tarda cuatro minutos en tener su primera pelea, y
        /// quien abre el juego no espera cuatro minutos mirando pawns talar. Con un ejército
        /// de salida, recursos y la espera del perfil Difícil, la primera batalla llega en
        /// menos de un minuto, y se oye.
        /// </remarks>
        void PrepararFondo(ConfiguracionDePartida c)
        {
            for (int bando = 0; bando < c.bases.Length; bando++)
            {
                if (c.bases[bando] == Ocupante.Cerrada) continue;

                var cerebro = CerebroIA.De(bando) ?? CerebroIA.Crear(bando);
                if (cerebro != null) cerebro.PonerDificultad(NivelDeIA.Dificil);

                PreparacionDeBandos.Regalar(bando, 600, 800, 600);
                PreparacionDeBandos.Ejercito(bando, ejercitoDeFondo);
            }
        }

        [Tooltip("Unidades militares con las que empieza cada bando del fondo del menú.")]
        [Range(0, 16)] public int ejercitoDeFondo = 7;

        /// <summary>
        /// El tutorial: el rival no piensa (su castillo es solo una diana), no hay niebla
        /// para que el castillo enemigo se vea desde el principio, y el jugador empieza con
        /// lo justo para construir y entrenar sin esperar.
        /// </summary>
        void PrepararTutorial()
        {
            foreach (var cerebro in CerebroIA.Todos)
                if (cerebro != null) cerebro.pensando = false;

            var niebla = NieblaDeGuerra.Actual ?? Object.FindFirstObjectByType<NieblaDeGuerra>();
            if (niebla != null) niebla.ignorarNiebla = true;

            var eco = Economia.Actual ?? Object.FindFirstObjectByType<Economia>();
            if (eco != null)
            {
                eco.Depositar(0, Datos.TipoRecurso.Oro, 400);
                eco.Depositar(0, Datos.TipoRecurso.Madera, 300);
            }
            PreparacionDeBandos.Ejercito(0, 3);

            var hud = Object.FindFirstObjectByType<HudRecursos>();
            var lienzo = hud != null ? hud.GetComponentInParent<Canvas>() : null;
            if (lienzo != null)
                TutorialGuiado.Crear(lienzo.transform, hud.tema, PanelDeUnidad.PrimeraInstalada(hud.fuentes));
        }

        /// <summary>Las reglas del sandbox, una por una.</summary>
        void AplicarSandbox(ConfiguracionDePartida c)
        {
            var r = c.sandbox ?? new ReglasDeSandbox();

            if (!r.niebla)
            {
                var niebla = NieblaDeGuerra.Actual ?? Object.FindFirstObjectByType<NieblaDeGuerra>();
                if (niebla != null) niebla.ignorarNiebla = true;
            }

            for (int bando = 0; bando < c.bases.Length; bando++)
            {
                if (c.bases[bando] == Ocupante.Cerrada) continue;

                if (r.recursos == RecursosIniciales.Abundantes) PreparacionDeBandos.Regalar(bando, 2000, 2000, 2000);
                if (r.ejercitoInicial) PreparacionDeBandos.Ejercito(bando, 8);

                var cerebro = CerebroIA.De(bando);
                if (cerebro != null) cerebro.soloDefender = !r.botsAtacan;
            }

            Time.timeScale = Mathf.Clamp(r.velocidad, 1, 4);
        }

        float _proximoRelleno;

        /// <summary>Recursos infinitos: cada segundo, todos los almacenes vuelven arriba.</summary>
        void Rellenar()
        {
            if (Time.unscaledTime < _proximoRelleno) return;
            _proximoRelleno = Time.unscaledTime + 1f;

            var eco = Economia.Actual;
            if (eco == null) return;

            for (int bando = 0; bando < Configuracion.bases.Length; bando++)
            {
                if (Configuracion.bases[bando] == Ocupante.Cerrada) continue;

                foreach (var tipo in new[] { Datos.TipoRecurso.Oro, Datos.TipoRecurso.Madera, Datos.TipoRecurso.Carne })
                {
                    int falta = 5000 - eco.Cantidad(bando, tipo);
                    if (falta > 0) eco.Depositar(bando, tipo, falta);
                }
            }
        }

        void Update()
        {
            if (Configuracion != null && Configuracion.modo == ModoDePartida.Sandbox &&
                Configuracion.sandbox != null && Configuracion.sandbox.recursos == RecursosIniciales.Infinitos)
                Rellenar();

            if (Configuracion != null && Configuracion.modo == ModoDePartida.Partida) LeerAtajoOculto();

            if (!EsMenu) return;

            // La partida de fondo no tiene final: cuando alguien gana, se empieza otra.
            var arbitro = ArbitroDePartida.Actual;
            if (arbitro == null || !arbitro.Terminada) return;

            if (_reinicioDeFondo < 0f) _reinicioDeFondo = Time.unscaledTime + esperaEntrePartidasDeFondo;
            else if (Time.unscaledTime >= _reinicioDeFondo) Navegacion.IrAlMenu();
        }

        /// <summary>
        /// Ctrl + Mayús + F12 enciende el panel de partida libre en una partida normal.
        /// </summary>
        /// <remarks>
        /// Para probar, no para jugar: nadie lo pulsa sin saber que existe, y en la build de
        /// entrega se queda donde está porque es la herramienta con la que el equipo enseña
        /// y depura en la exposición.
        /// </remarks>
        static void LeerAtajoOculto()
        {
            var t = UnityEngine.InputSystem.Keyboard.current;
            if (t == null || !t.f12Key.wasPressedThisFrame) return;
            if (!t.ctrlKey.isPressed || !t.shiftKey.isPressed) return;

            var panel = Object.FindFirstObjectByType<Pruebas.PanelDePruebas>(FindObjectsInactive.Include);
            if (panel != null) panel.enabled = !panel.enabled;
        }

        // -----------------------------------------------------------------
        // Bases
        // -----------------------------------------------------------------

        /// <summary>
        /// Una base sin ocupante desaparece antes de empezar: edificios, unidades y cerebro.
        /// </summary>
        void Cerrar(int bando)
        {
            var arbitro = ArbitroDePartida.Actual;
            if (arbitro != null) arbitro.Excluir(bando);

            QuitarBot(bando);

            var casas = new List<Edificio>(Edificio.Todos);
            foreach (var e in casas)
                if (e != null && e.faccion == bando) Destroy(e.gameObject);

            _condenadas.Clear();
            var registro = RegistroDeUnidades.Todas;
            for (int i = 0; i < registro.Count; i++)
                if (registro[i] != null && registro[i].faccion == bando) _condenadas.Add(registro[i]);

            foreach (var u in _condenadas) Destroy(u.gameObject);
        }

        void PonerBot(int bando, NivelDeIA nivel)
        {
            var cerebro = CerebroIA.De(bando) ?? CerebroIA.Crear(bando);
            if (cerebro == null) return;

            cerebro.PonerDificultad(nivel);
            cerebro.pensando = true;
        }

        static void QuitarBot(int bando)
        {
            var cerebros = CerebroIA.Todos;
            for (int i = cerebros.Count - 1; i >= 0; i--)
                if (cerebros[i] != null && cerebros[i].faccion == bando)
                    Destroy(cerebros[i].gameObject);
        }

        static void SentarJugador(int bando)
        {
            var selector = SelectorDeUnidades.Actual ?? Object.FindFirstObjectByType<SelectorDeUnidades>();
            if (selector != null) selector.faccionJugador = bando;

            var hud = Object.FindFirstObjectByType<HudRecursos>();
            if (hud != null) hud.CambiarFaccion(bando);

            var camara = Object.FindFirstObjectByType<CamaraRTS>();
            if (camara == null) return;

            foreach (var e in Edificio.Todos)
                if (e != null && e.faccion == bando && e.centroDeEntrega)
                {
                    camara.CentrarEn(e.PuntoDeEntrega);
                    return;
                }
        }

        // -----------------------------------------------------------------
        // Interfaz
        // -----------------------------------------------------------------

        /// <summary>Qué se ve en cada modo.</summary>
        /// <remarks>
        /// <b>El panel de partida libre no se borra en una partida normal: se apaga.</b> Sigue
        /// en la escena para que el atajo oculto lo pueda encender —es la herramienta con la
        /// que se prueba todo—, pero quien juega no lo ve ni sabe que existe.
        /// </remarks>
        void PrepararInterfaz(ModoDePartida modo)
        {
            var editor = Object.FindFirstObjectByType<EditorDeMapas>(FindObjectsInactive.Include);
            if (editor != null) editor.gameObject.SetActive(false);

            var panel = Object.FindFirstObjectByType<Pruebas.PanelDePruebas>(FindObjectsInactive.Include);
            if (panel != null) panel.enabled = modo == ModoDePartida.Sandbox;

            var menu = Object.FindFirstObjectByType<MenuPrincipal>(FindObjectsInactive.Include);
            if (menu != null) menu.gameObject.SetActive(modo == ModoDePartida.Menu);

            if (modo != ModoDePartida.Menu) return;

            // El fondo del menú: sin jugador, sin niebla, sin HUD y con la cámara de paseo.
            var niebla = NieblaDeGuerra.Actual ?? Object.FindFirstObjectByType<NieblaDeGuerra>();
            if (niebla != null) niebla.ignorarNiebla = true;

            var selector = Object.FindFirstObjectByType<SelectorDeUnidades>();
            if (selector != null) selector.soloObservar = true;

            var camara = Object.FindFirstObjectByType<CamaraRTS>();
            if (camara != null)
            {
                camara.enabled = false;
                var paseo = camara.GetComponent<PaseoDeCamara>() ?? camara.gameObject.AddComponent<PaseoDeCamara>();
                paseo.enabled = true;
            }

            foreach (var lienzo in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (menu == null || !lienzo.transform.IsChildOf(menu.transform))
                    lienzo.enabled = false;

            Apagar<CartelDeFinal>();
            Apagar<Minimapa>();
            Apagar<ResaltadoDeNodo>();
            Apagar<GruposDeControl>();
            Apagar<MenuDePausa>();

            Time.timeScale = velocidadDeFondo;
        }

        static void Apagar<T>() where T : Behaviour
        {
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsSortMode.None)) c.enabled = false;
        }
    }
}
