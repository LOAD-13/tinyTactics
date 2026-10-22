using UnityEngine;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Audio
{
    /// <summary>
    /// Las decisiones del audio, como funciones puras. Las comprueba el arnés.
    /// </summary>
    public static class ReglasDeSonido
    {
        /// <summary>
        /// Qué toma suena, sin repetir la anterior.
        /// </summary>
        /// <remarks>
        /// Con dos o más tomas, la anterior queda fuera del sorteo: dos hachazos idénticos
        /// seguidos son justo lo que hace que un sonido de juego suene a grabación. Se
        /// sortea entre las demás y se salta la anterior, en vez de repetir el sorteo hasta
        /// que salga otra, que con mala suerte no termina nunca.
        /// </remarks>
        public static int ElegirToma(int cuantas, int anterior, float azar01)
        {
            if (cuantas <= 1) return 0;

            float a = Mathf.Clamp(azar01, 0f, 0.99999f);

            if (anterior < 0 || anterior >= cuantas)
                return Mathf.Min(cuantas - 1, (int)(a * cuantas));

            int k = Mathf.Min(cuantas - 2, (int)(a * (cuantas - 1)));
            return k >= anterior ? k + 1 : k;
        }

        /// <summary>Si un sonido puede dispararse ahora mismo.</summary>
        public static bool PuedeSonar(float ahora, float ultimo, float separacion,
                                      int sonando, int maximo)
        {
            if (sonando >= Mathf.Max(1, maximo)) return false;
            return ahora - ultimo >= separacion;
        }

        /// <summary>
        /// Volumen según lo lejos que esté de lo que enseña la cámara.
        /// </summary>
        /// <remarks>
        /// <paramref name="distancia"/> va normalizada al borde de la pantalla: 0 es el
        /// centro, 1 el borde. Hasta <paramref name="cerca"/> suena entero; desde ahí cae
        /// suave hasta <paramref name="lejos"/>, donde ya no se oye. Que caiga más allá del
        /// borde y no en él es a propósito: una pelea que está justo fuera de la pantalla
        /// tiene que oírse, flojita, porque es el aviso de que hay que mirar ahí.
        /// </remarks>
        public static float Atenuacion(float distancia, float cerca, float lejos)
        {
            if (distancia <= cerca) return 1f;
            if (distancia >= lejos || lejos <= cerca) return 0f;

            float t = (distancia - cerca) / (lejos - cerca);
            return 1f - t * t * (3f - 2f * t);
        }

        /// <summary>El tono de una toma, con su variación.</summary>
        public static float Tono(float variacion, float azar01) =>
            1f + (Mathf.Clamp01(azar01) * 2f - 1f) * Mathf.Max(0f, variacion);

        /// <summary>El sonido de un golpe de trabajo, según con qué se trabaja.</summary>
        public static bool SonidoDeTrabajo(bool martillo, Datos.TipoRecurso recurso,
                                           out Sonido sonido)
        {
            sonido = Sonido.MartilloObra;
            if (martillo) return true;

            switch (recurso)
            {
                case Datos.TipoRecurso.Madera: sonido = Sonido.HachaArbol; return true;
                case Datos.TipoRecurso.Oro: sonido = Sonido.PicoOro; return true;
                case Datos.TipoRecurso.Carne: sonido = Sonido.OvejaBalido; return true;
                default: return false;
            }
        }
    }

    /// <summary>
    /// Quien hace sonar el juego. Uno por escena.
    /// </summary>
    /// <remarks>
    /// <b>Solo suena lo que ves.</b> Un sonido del mapa pasa dos filtros: la niebla (si el
    /// jugador no ve esa celda, no lo oye) y la cámara (cuanto más lejos de la pantalla, más
    /// flojo). Lo primero es una regla de juego, no estética: una pelea en la niebla que se
    /// oye le dice al jugador dónde está el rival, que es justo lo que la niebla esconde
    /// (ADR-20). Lo segundo es lo que hace que una base entera de pawns trabajando no suene
    /// a fábrica cuando estás mirando otra cosa.
    ///
    /// <b>Un reparto fijo de altavoces, no uno por sonido.</b> Se crean
    /// <see cref="altavoces"/> fuentes al empezar y se reutilizan; si están todas ocupadas,
    /// se roba la que lleva más rato sonando. Crear y destruir una fuente por cada espadazo
    /// es basura para el recolector en el peor momento: en mitad de una batalla.
    ///
    /// La música y el ambiente llegan en la semana 14, con el mezclador por buses.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Sonido del juego")]
    public class SonidoJuego : MonoBehaviour
    {
        public CatalogoDeSonidos catalogo;

        [Range(4, 48)] public int altavoces = 24;

        [Tooltip("Bajo de fabrica hasta que haya menu de opciones (semana 15).")]
        [Range(0f, 1f)] public float volumenGeneral = 0.25f;

        [Tooltip("Lo cambia el panel de partida libre.")]
        public bool silenciado;

        [Header("Distancia a la camara")]
        [Tooltip("Hasta aqui suena entero. 1 es el borde de la pantalla: todo lo que se " +
                 "ve, suena entero.")]
        [Range(0f, 2f)] public float cerca = 1f;

        [Tooltip("A partir de aqui ya no se oye. Un poco mas alla del borde, para que lo " +
                 "que esta justo al lado de la pantalla se oiga flojito y nada mas.")]
        [Range(0.5f, 4f)] public float lejos = 1.25f;

        [Tooltip("Cuanto se va el sonido al altavoz de su lado.")]
        [Range(0f, 1f)] public float panoramica = 0.55f;

        public static SonidoJuego Actual { get; private set; }

        AudioSource[] _fuentes;
        Sonido[] _queSuena;
        float[] _empezo;

        float[] _ultimo;
        int[] _tomaAnterior;

        void Start() => Nucleo.Opciones.Aplicar();

        void Awake()
        {
            Actual = this;

            int n = System.Enum.GetValues(typeof(Sonido)).Length;
            _ultimo = new float[n];
            _tomaAnterior = new int[n];
            for (int i = 0; i < n; i++) { _ultimo[i] = -999f; _tomaAnterior[i] = -1; }

            _fuentes = new AudioSource[altavoces];
            _queSuena = new Sonido[altavoces];
            _empezo = new float[altavoces];

            for (int i = 0; i < altavoces; i++)
            {
                var go = new GameObject($"Altavoz_{i:00}");
                go.transform.SetParent(transform, false);

                var f = go.AddComponent<AudioSource>();
                f.playOnAwake = false;
                f.spatialBlend = 0f;   // la distancia la calcula el juego, no el motor
                _fuentes[i] = f;
            }
        }

        void OnEnable() => Autoridad.AlEmitir += AlEmitirOrden;

        void OnDisable() => Autoridad.AlEmitir -= AlEmitirOrden;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        // -----------------------------------------------------------------
        // Puertas de entrada. Estáticas para que nadie tenga que buscar el componente.
        // -----------------------------------------------------------------

        /// <summary>Un sonido en un punto del mapa.</summary>
        public static void Sonar(Sonido sonido, Vector3 donde)
        {
            if (Actual != null) Actual.Reproducir(sonido, donde, true);
        }

        /// <summary>Un sonido de interfaz: ni niebla ni distancia.</summary>
        public static void SonarInterfaz(Sonido sonido)
        {
            if (Actual != null) Actual.Reproducir(sonido, Vector3.zero, false);
        }

        /// <summary>
        /// Un aviso que solo le interesa al dueño: «tu edificio está listo», «tu unidad ha
        /// salido». El de un rival no suena, aunque esté a la vista.
        /// </summary>
        /// <remarks>
        /// Y suena <b>donde pasa</b>, con la niebla y la cámara como cualquier otro: un
        /// cuartel que entrena al otro lado del mapa no se oye. La regla del juego es una
        /// sola, solo suena lo que ves.
        /// </remarks>
        public static void SonarSiEsMio(Sonido sonido, int faccion, Vector3 donde)
        {
            if (Actual == null || !EsDelJugador(faccion)) return;
            Actual.Reproducir(sonido, donde, true);
        }

        static bool EsDelJugador(int faccion)
        {
            var selector = SelectorDeUnidades.Actual;
            return selector != null && selector.faccionJugador == faccion;
        }

        /// <summary>
        /// Las órdenes del jugador suenan; las del bot, no.
        /// </summary>
        /// <remarks>
        /// Se escucha a la autoridad en vez de poner un sonido en cada uno de los once
        /// sitios del selector que emiten órdenes: un sitio que se olvide es una orden muda,
        /// y la autoridad es el único paso por el que pasan todas (ADR-01). Con «bot contra
        /// bot» el bando del jugador lo lleva un cerebro, y sus órdenes tampoco suenan.
        /// </remarks>
        void AlEmitirOrden(Orden orden)
        {
            if (orden == null || !EsDelJugador(orden.Faccion)) return;
            if (IA.CerebroIA.De(orden.Faccion) != null) return;

            if (orden is OrdenDetener) return;

            SonarInterfaz(orden is OrdenAtacar ? Sonido.OrdenAtacar : Sonido.OrdenMover);
        }

        // -----------------------------------------------------------------

        void Reproducir(Sonido sonido, Vector3 donde, bool enElMapa)
        {
            if (silenciado || catalogo == null || _fuentes == null) return;

            var entrada = catalogo.De(sonido);
            if (entrada == null || entrada.tomas == null || entrada.tomas.Length == 0) return;

            float volumen = entrada.volumen * volumenGeneral;
            float pan = 0f;

            if (enElMapa && !entrada.deInterfaz && !Escuchable(donde, ref volumen, ref pan))
                return;

            if (volumen <= 0.01f) return;

            int i = (int)sonido;
            float ahora = Time.unscaledTime;

            if (!ReglasDeSonido.PuedeSonar(ahora, _ultimo[i], entrada.separacion,
                                           Sonando(sonido), entrada.maximoSimultaneo))
                return;

            int toma = ReglasDeSonido.ElegirToma(entrada.tomas.Length, _tomaAnterior[i],
                                                 Random.value);
            var clip = entrada.tomas[toma];
            if (clip == null) return;

            var fuente = Libre(out int hueco);

            fuente.clip = clip;
            fuente.volume = volumen;
            fuente.panStereo = pan;
            fuente.pitch = ReglasDeSonido.Tono(entrada.variacionDeTono, Random.value);
            fuente.Play();

            _queSuena[hueco] = sonido;
            _empezo[hueco] = ahora;
            _ultimo[i] = ahora;
            _tomaAnterior[i] = toma;
        }

        /// <summary>Los dos filtros del mapa: la niebla y la cámara.</summary>
        bool Escuchable(Vector3 donde, ref float volumen, ref float pan)
        {
            var niebla = NieblaDeGuerra.Actual;
            if (niebla != null && niebla.EstadoParaJugador(donde) != EstadoVisible.Visible)
                return false;

            var camara = Camera.main;
            if (camara == null || !camara.orthographic) return true;

            float medioAlto = Mathf.Max(0.01f, camara.orthographicSize);
            float medioAncho = medioAlto * Mathf.Max(0.01f, camara.aspect);

            Vector3 ojo = camara.transform.position;
            float dx = (donde.x - ojo.x) / medioAncho;
            float dy = (donde.y - ojo.y) / medioAlto;

            // Distancia «de pantalla»: la que llega antes al borde, sea por arriba o por el
            // lado. Con la euclídea, una esquina contaría como más lejos que el borde.
            float distancia = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));

            volumen *= ReglasDeSonido.Atenuacion(distancia, cerca, lejos);
            pan = Mathf.Clamp(dx, -1f, 1f) * panoramica;
            return true;
        }

        int Sonando(Sonido sonido)
        {
            int n = 0;
            for (int i = 0; i < _fuentes.Length; i++)
                if (_queSuena[i] == sonido && _fuentes[i].isPlaying) n++;

            return n;
        }

        AudioSource Libre(out int hueco)
        {
            hueco = 0;
            float masViejo = float.MaxValue;

            for (int i = 0; i < _fuentes.Length; i++)
            {
                if (!_fuentes[i].isPlaying) { hueco = i; return _fuentes[i]; }

                if (_empezo[i] < masViejo)
                {
                    masViejo = _empezo[i];
                    hueco = i;
                }
            }

            _fuentes[hueco].Stop();
            return _fuentes[hueco];
        }
    }
}
