using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Combate
{
    /// <summary>
    /// «Te atacan»: una campana y un anillo que late en el minimapa cuando un enemigo golpea
    /// algo del jugador <b>fuera de la pantalla</b>.
    /// </summary>
    /// <remarks>
    /// Lo que ya está en pantalla no avisa: el jugador lo está viendo. Y no avisa a cada
    /// golpe: una pelea son docenas por segundo. Avisa la primera vez, y otra solo si pasa un
    /// rato o si el ataque es en otro sitio (ver <see cref="DebeAvisar"/>).
    /// <para>
    /// No mira la niebla: lo que atacan es tuyo, y lo tuyo siempre se ve. El aviso es
    /// información que el jugador tiene en cualquier RTS, no una trampa.
    /// </para>
    /// </remarks>
    public static class AvisoDeAtaque
    {
        /// <summary>Segundos antes de volver a avisar en el mismo sitio.</summary>
        public const float Espera = 12f;

        /// <summary>A partir de cuántas casillas del último aviso es «otro sitio».</summary>
        public const float OtroSitio = 18f;

        /// <summary>Lo que dura el anillo en el minimapa.</summary>
        public const float DuracionDelAnillo = 4f;

        public struct Anillo
        {
            public Vector2 donde;
            public float desde;
        }

        static readonly List<Anillo> _anillos = new List<Anillo>();
        static float _ultimo = -999f;
        static Vector2 _dondeUltimo;

        /// <summary>Los anillos vivos, para que el minimapa los pinte.</summary>
        public static IReadOnlyList<Anillo> Anillos
        {
            get
            {
                float ahora = Time.unscaledTime;
                _anillos.RemoveAll(a => ahora - a.desde > DuracionDelAnillo);
                return _anillos;
            }
        }

        /// <summary>
        /// ¿Se avisa de este golpe? Puro, para las pruebas.
        /// </summary>
        public static bool DebeAvisar(float ahora, float ultimo, float distanciaAlUltimo, bool enPantalla) =>
            !enPantalla && (ahora - ultimo >= Espera || distanciaAlUltimo >= OtroSitio);

        /// <summary>Le han pegado a algo de <paramref name="victima"/>.</summary>
        public static void Golpe(int victima, int agresor, Vector3 donde)
        {
            if (agresor < 0 || agresor == victima) return;
            var selector = Entrada.SelectorDeUnidades.Actual;
            if (selector == null || selector.faccionJugador != victima || selector.soloObservar) return;

            // Para la música vale aunque se vea: la pelea es contra ti.
            Audio.MusicaDeJuego.Alarma();

            float ahora = Time.unscaledTime;
            if (!DebeAvisar(ahora, _ultimo, Vector2.Distance(donde, _dondeUltimo), EnPantalla(donde))) return;

            _ultimo = ahora;
            _dondeUltimo = donde;
            _anillos.Add(new Anillo { donde = donde, desde = ahora });
            Audio.SonidoJuego.SonarInterfaz(Audio.Sonido.AlertaAtaque);
            Interfaz.MensajesEnPantalla.Decir("¡Te atacan! Mira el minimapa", Interfaz.TipoMensaje.Aviso);
        }

        static bool EnPantalla(Vector3 donde)
        {
            var camara = Camera.main;
            if (camara == null) return false;
            var p = camara.WorldToViewportPoint(donde);
            return p.x >= 0f && p.x <= 1f && p.y >= 0f && p.y <= 1f;
        }

        /// <summary>
        /// Cada carga de escena es una partida nueva (ADR-26: menú y partida son la misma
        /// escena) y no puede heredar anillos de la anterior.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Enganchar() =>
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, __) => Olvidar();

        public static void Olvidar()
        {
            _anillos.Clear();
            _ultimo = -999f;
        }
    }
}
