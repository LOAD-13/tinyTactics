using UnityEngine;
using UnityEngine.SceneManagement;
using TinyTactics.IA;

namespace TinyTactics.Nucleo
{
    /// <summary>Para qué se abre la escena de juego.</summary>
    public enum ModoDePartida
    {
        /// <summary>De fondo del menú: solo bots, sin jugador, sin niebla.</summary>
        Menu,
        /// <summary>Una partida normal. El panel de partida libre no se ve.</summary>
        Partida,
        /// <summary>Una partida con el panel de partida libre a la vista.</summary>
        Sandbox,
        /// <summary>El editor de mapas: sin bandos, sin IA, solo el mapa y los pinceles.</summary>
        Editor,
    }

    /// <summary>Quién se sienta en cada base del mapa.</summary>
    public enum Ocupante { Jugador, BotFacil, BotNormal, BotDificil, Cerrada }

    /// <summary>
    /// Cómo es la partida que se va a jugar: modo, mapa y quién ocupa cada base.
    /// </summary>
    /// <remarks>
    /// <b>Viaja de una escena a otra en un campo estático</b>, y es deliberado. El menú y la
    /// partida son la misma escena cargada dos veces con distinta configuración, y lo único
    /// que sobrevive a un <c>LoadScene</c> sin <c>DontDestroyOnLoad</c> es lo estático. Un
    /// objeto que no se destruye habría que vigilar que no se duplique al volver al menú;
    /// un campo estático no puede duplicarse.
    ///
    /// Si se abre la escena de juego directamente desde Unity no hay configuración, y se
    /// juega como hasta la semana 11: tú en la primera base, el resto bots en Normal y el
    /// panel a la vista. <see cref="PorDefecto"/> existe para que ese camino siga vivo.
    /// </remarks>
    public class ConfiguracionDePartida
    {
        public ModoDePartida modo = ModoDePartida.Partida;
        public string escena = "Juego";
        public Ocupante[] bases = new Ocupante[0];

        /// <summary>Color de cada base (0..4: azul, rojo, amarillo, morado, negro).</summary>
        public int[] colores = new int[0];

        /// <summary>Reglas que solo existen en el sandbox. En partida normal, las de fábrica.</summary>
        public ReglasDeSandbox sandbox = new ReglasDeSandbox();

        /// <summary>
        /// El mapa de un archivo (semana 13). Null es el mapa de la escena: Tres Coronas.
        /// </summary>
        public Mundo.MapaGuardado mapa;

        /// <summary>La que se va a jugar o se está jugando. Null si nadie la eligió.</summary>
        public static ConfiguracionDePartida Actual;

        /// <summary>La base del jugador, o −1 si no hay jugador (el fondo del menú).</summary>
        public int Jugador => ReglasDeConfiguracion.Jugador(bases);

        public Ocupante De(int bando) =>
            bando >= 0 && bando < bases.Length ? bases[bando] : Ocupante.Cerrada;

        /// <summary>Lo de siempre: tú en la primera base y bots en Normal. Con el panel.</summary>
        public static ConfiguracionDePartida PorDefecto(int bandos, string escena)
        {
            var c = new ConfiguracionDePartida
            {
                modo = ModoDePartida.Sandbox,
                escena = escena,
                bases = new Ocupante[Mathf.Max(1, bandos)],
            };

            c.bases[0] = Ocupante.Jugador;
            for (int i = 1; i < c.bases.Length; i++) c.bases[i] = Ocupante.BotNormal;

            c.colores = ReglasDeConfiguracion.ColoresDeFabrica(c.bases.Length);
            return c;
        }

        /// <summary>El fondo del menú: todas las bases para bots en Normal.</summary>
        public static ConfiguracionDePartida DeMenu(int bandos, string escena)
        {
            var c = new ConfiguracionDePartida
            {
                modo = ModoDePartida.Menu,
                escena = escena,
                bases = new Ocupante[Mathf.Max(1, bandos)],
            };

            for (int i = 0; i < c.bases.Length; i++) c.bases[i] = Ocupante.BotNormal;
            c.colores = ReglasDeConfiguracion.ColoresDeFabrica(c.bases.Length);
            return c;
        }

        public ConfiguracionDePartida Copia() => new ConfiguracionDePartida
        {
            modo = modo,
            escena = escena,
            bases = (Ocupante[])bases.Clone(),
            colores = (int[])colores.Clone(),
            sandbox = sandbox.Copia(),
            mapa = mapa,
        };
    }

    /// <summary>
    /// Las reglas de una configuración, como funciones puras. Las comprueba el arnés.
    /// </summary>
    public static class ReglasDeConfiguracion
    {
        public static int Jugador(Ocupante[] bases)
        {
            if (bases == null) return -1;

            for (int i = 0; i < bases.Length; i++)
                if (bases[i] == Ocupante.Jugador) return i;

            return -1;
        }

        /// <summary>Cada base con el color de su número: azul, rojo, amarillo…</summary>
        public static int[] ColoresDeFabrica(int bases)
        {
            var c = new int[Mathf.Max(0, bases)];
            for (int i = 0; i < c.Length; i++) c[i] = i % Bandos.Colores;
            return c;
        }

        /// <summary>
        /// El siguiente color libre para una base: ninguna otra base abierta puede llevarlo.
        /// </summary>
        /// <remarks>
        /// Dos bandos del mismo color no se distinguen en el mapa, y eso no es una opción
        /// estética: es una partida que no se puede jugar. Por eso el botón salta los colores
        /// ocupados en vez de dejar elegirlos y quejarse después. Las bases cerradas no
        /// cuentan: no hay nada suyo en el mapa que confundir.
        /// </remarks>
        public static int SiguienteColor(int[] colores, Ocupante[] bases, int base_, int paso = 1)
        {
            if (colores == null || base_ < 0 || base_ >= colores.Length) return 0;

            int actual = colores[base_];
            for (int k = 1; k <= Bandos.Colores; k++)
            {
                int candidato = ((actual + paso * k) % Bandos.Colores + Bandos.Colores) % Bandos.Colores;
                if (!Ocupado(colores, bases, base_, candidato)) return candidato;
            }

            return actual;
        }

        static bool Ocupado(int[] colores, Ocupante[] bases, int base_, int color)
        {
            for (int i = 0; i < colores.Length; i++)
            {
                if (i == base_) continue;
                if (bases != null && i < bases.Length && bases[i] == Ocupante.Cerrada) continue;
                if (colores[i] == color) return true;
            }
            return false;
        }

        /// <summary>¿Hay dos bases abiertas con el mismo color?</summary>
        public static bool ColoresRepetidos(int[] colores, Ocupante[] bases)
        {
            if (colores == null) return false;
            for (int i = 0; i < colores.Length; i++)
            {
                if (bases != null && i < bases.Length && bases[i] == Ocupante.Cerrada) continue;
                if (Ocupado(colores, bases, i, colores[i])) return true;
            }
            return false;
        }

        public static int Activas(Ocupante[] bases)
        {
            int n = 0;
            if (bases != null)
                foreach (var b in bases)
                    if (b != Ocupante.Cerrada) n++;
            return n;
        }

        /// <summary>
        /// ¿Se puede empezar? Exactamente un jugador y al menos dos bandos en el mapa.
        /// </summary>
        /// <remarks>
        /// El motivo se devuelve en palabras porque es lo que enseña la pantalla: un botón
        /// «Empezar» apagado sin decir por qué es un botón roto para quien lo pulsa.
        /// </remarks>
        public static bool EsValida(Ocupante[] bases, out string motivo)
        {
            int jugadores = 0;
            if (bases != null)
                foreach (var b in bases)
                    if (b == Ocupante.Jugador) jugadores++;

            if (jugadores == 0) { motivo = "Elige tu base."; return false; }
            if (jugadores > 1) { motivo = "Solo puede haber un jugador."; return false; }

            if (Activas(bases) < 2) { motivo = "Hace falta al menos un rival."; return false; }

            motivo = "";
            return true;
        }

        /// <summary>
        /// El siguiente ocupante al pulsar sobre una base: Tú → Fácil → Normal → Difícil →
        /// Cerrada → Tú.
        /// </summary>
        /// <remarks>
        /// Un solo botón por base que recorre todo, en vez de un desplegable: con tres o cuatro
        /// bases, cinco valores se recorren en un par de clics. Cuando el ciclo cae en «Tú»,
        /// la pantalla llama a <see cref="Sentarse"/>, que quita al jugador de donde estuviera.
        /// </remarks>
        public static Ocupante Siguiente(Ocupante actual)
        {
            switch (actual)
            {
                case Ocupante.Jugador: return Ocupante.BotFacil;
                case Ocupante.BotFacil: return Ocupante.BotNormal;
                case Ocupante.BotNormal: return Ocupante.BotDificil;
                case Ocupante.BotDificil: return Ocupante.Cerrada;
                default: return Ocupante.Jugador;
            }
        }

        /// <summary>Aplica el siguiente ocupante a una base, respetando que haya un solo jugador.</summary>
        public static void Pulsar(Ocupante[] bases, int base_)
        {
            if (bases == null || base_ < 0 || base_ >= bases.Length) return;

            var nuevo = Siguiente(bases[base_]);
            if (nuevo == Ocupante.Jugador) Sentarse(bases, base_);
            else bases[base_] = nuevo;
        }

        /// <summary>
        /// El jugador se sienta en una base; la que dejó pasa a ser un bot en Normal.
        /// </summary>
        public static void Sentarse(Ocupante[] bases, int base_)
        {
            if (bases == null || base_ < 0 || base_ >= bases.Length) return;

            for (int i = 0; i < bases.Length; i++)
                if (bases[i] == Ocupante.Jugador) bases[i] = Ocupante.BotNormal;

            bases[base_] = Ocupante.Jugador;
        }

        public static bool EsBot(Ocupante o) =>
            o == Ocupante.BotFacil || o == Ocupante.BotNormal || o == Ocupante.BotDificil;

        public static NivelDeIA Nivel(Ocupante o) =>
            o == Ocupante.BotFacil ? NivelDeIA.Facil :
            o == Ocupante.BotDificil ? NivelDeIA.Dificil : NivelDeIA.Normal;

        public static string Nombre(Ocupante o)
        {
            switch (o)
            {
                case Ocupante.Jugador: return "Tú";
                case Ocupante.BotFacil: return "Bot · Fácil";
                case Ocupante.BotNormal: return "Bot · Normal";
                case Ocupante.BotDificil: return "Bot · Difícil";
                default: return "Cerrada";
            }
        }
    }

    /// <summary>Cuántos recursos tiene cada bando al empezar.</summary>
    public enum RecursosIniciales { Normales, Abundantes, Infinitos }

    /// <summary>
    /// Lo que se puede cambiar en el sandbox y no en una partida normal.
    /// </summary>
    /// <remarks>
    /// El sandbox es para experimentar: ver un bot contra otro sin niebla, probar un
    /// ejército entero sin recolectar, o mirar cómo juega la IA cuando no ataca. Cada regla
    /// es una palanca que en una partida normal arruinaría el juego, y por eso vive aquí.
    /// </remarks>
    [System.Serializable]
    public class ReglasDeSandbox
    {
        public bool niebla = true;
        public RecursosIniciales recursos = RecursosIniciales.Normales;
        [Range(1, 4)] public int velocidad = 1;
        public bool botsAtacan = true;
        public bool ejercitoInicial;

        public ReglasDeSandbox Copia() => (ReglasDeSandbox)MemberwiseClone();
    }

    /// <summary>
    /// Ir de una pantalla a otra. Un solo sitio, para que el tiempo y la pausa se limpien
    /// siempre igual.
    /// </summary>
    public static class Navegacion
    {
        /// <summary>Escena de juego del primer mapa: el fondo del menú. La fija el arranque.</summary>
        public static string EscenaDelMenu = "Juego";

        public static int BandosDelMenu = 3;

        /// <summary>
        /// La pantalla de título se enseña una vez por sesión. Al volver de una partida se
        /// va directo al menú principal.
        /// </summary>
        public static bool TituloVisto;

        public static void IrAlMenu()
        {
            Cargar(ConfiguracionDePartida.DeMenu(BandosDelMenu, EscenaDelMenu));
        }

        public static void Empezar(ConfiguracionDePartida configuracion)
        {
            TituloVisto = true;
            Cargar(configuracion);
        }

        /// <summary>Abre un mapa en el editor (semana 13).</summary>
        public static void Editar(Mundo.MapaGuardado mapa)
        {
            TituloVisto = true;
            Cargar(new ConfiguracionDePartida
            {
                modo = ModoDePartida.Editor,
                escena = EscenaDelMenu,
                bases = new Ocupante[0],
                colores = new int[0],
                mapa = mapa,
            });
        }

        /// <summary>La misma partida otra vez, con la misma configuración.</summary>
        public static void Revancha()
        {
            var actual = ConfiguracionDePartida.Actual;
            if (actual == null)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            Cargar(actual);
        }

        public static void Salir()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Cargar(ConfiguracionDePartida configuracion)
        {
            // El tiempo vuelve a la normalidad antes de cargar: una partida que termina en
            // pausa o a x4 arrancaría la siguiente igual, y no habría forma de saber por qué.
            Time.timeScale = 1f;

            ConfiguracionDePartida.Actual = configuracion;
            SceneManager.LoadScene(configuracion.escena);
        }
    }

    /// <summary>
    /// Las opciones del jugador, guardadas entre sesiones.
    /// </summary>
    /// <remarks>
    /// En <c>PlayerPrefs</c> y no en un archivo propio: son tres números, y es lo que el
    /// motor ofrece para exactamente esto. El volumen por canal llegó con el mezclador de la
    /// semana 14 (ver <see cref="Audio.Mezclador"/>).
    /// </remarks>
    public static class Opciones
    {
        const string ClaveVolumen = "tt.volumen";
        const string ClaveSilencio = "tt.silencio";
        const string ClaveCamara = "tt.camara";
        const string ClaveBorde = "tt.borde";
        const string ClaveFps = "tt.fps";
        const string ClaveVsync = "tt.vsync";

        /// <summary>Los límites de fotogramas que se ofrecen. 0 es «sin límite».</summary>
        public static readonly int[] LimitesDeFps = { 30, 60, 120, 0 };

        /// <summary>Bajo de fábrica, como se pidió en la semana 11.</summary>
        public const float VolumenDeFabrica = 0.25f;

        public static float Volumen
        {
            get => PlayerPrefs.GetFloat(ClaveVolumen, VolumenDeFabrica);
            set { PlayerPrefs.SetFloat(ClaveVolumen, Mathf.Clamp01(value)); Aplicar(); }
        }

        /// <summary>
        /// El volumen de un canal, de 0 a 1, que se multiplica por el general. De fábrica la
        /// música y el ambiente van por debajo de los efectos: acompañan, no tapan.
        /// </summary>
        public static float VolumenDe(Audio.Canal canal) =>
            PlayerPrefs.GetFloat(ClaveCanal + (int)canal, CanalDeFabrica(canal));

        public static void PonerVolumen(Audio.Canal canal, float valor)
        {
            PlayerPrefs.SetFloat(ClaveCanal + (int)canal, Mathf.Clamp01(valor));
            Aplicar();
        }

        public static float CanalDeFabrica(Audio.Canal canal) => canal switch
        {
            Audio.Canal.Musica => 0.6f,
            Audio.Canal.Ambiente => 0.5f,
            _ => 1f,
        };

        const string ClaveCanal = "tt.canal.";

        public static bool Silencio
        {
            get => PlayerPrefs.GetInt(ClaveSilencio, 0) == 1;
            set { PlayerPrefs.SetInt(ClaveSilencio, value ? 1 : 0); Aplicar(); }
        }

        /// <summary>Multiplicador de la velocidad de la cámara: 0,5 a 2.</summary>
        public static float VelocidadDeCamara
        {
            get => PlayerPrefs.GetFloat(ClaveCamara, 1f);
            set { PlayerPrefs.SetFloat(ClaveCamara, Mathf.Clamp(value, 0.5f, 2f)); Aplicar(); }
        }

        /// <summary>¿Se mueve la cámara al acercar el ratón al borde de la pantalla?</summary>
        public static bool DesplazarConElBorde
        {
            get => PlayerPrefs.GetInt(ClaveBorde, 1) == 1;
            set { PlayerPrefs.SetInt(ClaveBorde, value ? 1 : 0); Aplicar(); }
        }

        /// <summary>Índice en <see cref="LimitesDeFps"/>. De fábrica, 60.</summary>
        public static int LimiteDeFps
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(ClaveFps, 1), 0, LimitesDeFps.Length - 1);
            set { PlayerPrefs.SetInt(ClaveFps, Mathf.Clamp(value, 0, LimitesDeFps.Length - 1)); Aplicar(); }
        }

        public static bool SincronizacionVertical
        {
            get => PlayerPrefs.GetInt(ClaveVsync, 1) == 1;
            set { PlayerPrefs.SetInt(ClaveVsync, value ? 1 : 0); Aplicar(); }
        }

        public static string TextoFps(int indice)
        {
            int f = LimitesDeFps[Mathf.Clamp(indice, 0, LimitesDeFps.Length - 1)];
            return f == 0 ? "Sin límite" : $"{f} FPS";
        }

        /// <summary>
        /// Pantalla completa o ventana. La ventana es <b>maximizada</b>: ocupa el escritorio
        /// respetando la barra de tareas, con su barra de título, como cualquier aplicación.
        /// </summary>
        /// <remarks>
        /// <c>Screen.fullScreen = false</c> a secas dejaba la ventana a 1920×1080 en un monitor
        /// de 1920×1080: con la barra de título y la de tareas no cabía y se salía por abajo
        /// (visto en la build de la semana 13). <c>MaximizedWindow</c> le pide a Windows su
        /// tamaño de ventana maximizada, que ya descuenta la barra de tareas.
        /// El cambio, además, no se ve en <c>Screen</c> hasta el fotograma siguiente; por eso
        /// lo pedido se recuerda ese fotograma, para que el botón de opciones no mienta.
        /// </remarks>
        public static bool PantallaCompleta
        {
            get => Time.frameCount <= _fotogramaPedido + 1 ? _completaPedida : EsPantallaCompleta(Screen.fullScreenMode);
            set
            {
                _completaPedida = value;
                _fotogramaPedido = Time.frameCount;
                var escritorio = Screen.currentResolution;
                Screen.SetResolution(escritorio.width, escritorio.height,
                                     value ? FullScreenMode.FullScreenWindow : FullScreenMode.MaximizedWindow);
            }
        }

        static bool EsPantallaCompleta(FullScreenMode modo) =>
            modo == FullScreenMode.FullScreenWindow || modo == FullScreenMode.ExclusiveFullScreen;

        static bool _completaPedida;
        static int _fotogramaPedido = -10;

        /// <summary>
        /// Si el juego arranca en una ventana normal (Unity recuerda el modo de la última vez,
        /// también el de builds anteriores), pasa a maximizada. Una vez, al cargar la primera escena.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EncajarVentanaAlArrancar()
        {
            if (Application.isEditor || Screen.fullScreenMode != FullScreenMode.Windowed) return;
            var escritorio = Screen.currentResolution;
            Screen.SetResolution(escritorio.width, escritorio.height, FullScreenMode.MaximizedWindow);
        }

        /// <summary>Vuelca lo guardado sobre el sonido de la escena.</summary>
        public static void Aplicar()
        {
            // Con la sincronización vertical puesta, el motor ignora el límite de FPS: manda
            // el refresco del monitor. Se aplican los dos y gana el que corresponde.
            QualitySettings.vSyncCount = SincronizacionVertical ? 1 : 0;
            int fps = LimitesDeFps[LimiteDeFps];
            Application.targetFrameRate = fps == 0 ? -1 : fps;

            var camara = Object.FindFirstObjectByType<Entrada.CamaraRTS>();
            if (camara != null)
            {
                camara.multiplicadorPaneo = VelocidadDeCamara;
                camara.paneoPorBorde = DesplazarConElBorde;
            }

            var sonido = Audio.SonidoJuego.Actual;
            if (sonido == null) return;

            sonido.volumenGeneral = Volumen;
            sonido.silenciado = Silencio;
            sonido.canalEfectos = VolumenDe(Audio.Canal.Efectos);
            sonido.canalInterfaz = VolumenDe(Audio.Canal.Interfaz);
        }
    }
}
