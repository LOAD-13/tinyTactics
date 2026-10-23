using UnityEngine;
using TinyTactics.Nucleo;

namespace TinyTactics.Audio
{
    /// <summary>
    /// La música: la del menú, la de partida en lista, y la de combate cuando hay pelea a la
    /// vista. Cambia siempre con un fundido cruzado.
    /// </summary>
    /// <remarks>
    /// <b>Dos altavoces que se turnan.</b> El que suena baja mientras el otro sube con la
    /// pista nueva; al acabar el fundido se intercambian los papeles. Con uno solo, el cambio
    /// sería un corte en seco, que es justo lo que delata una transición mal hecha.
    /// <para>
    /// La pista de partida que se interrumpe por un combate se recuerda y se retoma <b>por
    /// donde iba</b>: volver siempre al principio hace que la misma intro suene diez veces en
    /// una partida con escaramuzas.
    /// </para>
    /// </remarks>
    public class MusicaDeJuego : MonoBehaviour
    {
        public CatalogoDeMusica catalogo;

        public static MusicaDeJuego Actual { get; private set; }

        enum Pista { Ninguna, Menu, Partida, Combate, Final }

        readonly ReglasDeMusica _reglas = new ReglasDeMusica();
        AudioSource _a, _b;
        Pista _pista = Pista.Ninguna;
        int _indicePartida;
        float _tiempoPartida;
        float _nivelA, _nivelB;

        /// <summary>El volumen propio de la pista de cada altavoz: el menú no suena como el combate.</summary>
        float _pesoA, _pesoB;

        public bool EnCombate => _pista == Pista.Combate;

        void Awake()
        {
            Actual = this;
            _a = Altavoz("Musica_A");
            _b = Altavoz("Musica_B");
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        AudioSource Altavoz(string nombre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var f = go.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.spatialBlend = 0f;
            f.ignoreListenerPause = true;   // la pausa para el juego, no la música
            return f;
        }

        void Start()
        {
            var c = ConfiguracionDePartida.Actual;
            Poner(c != null && c.modo == ModoDePartida.Menu ? Pista.Menu : Pista.Partida);
        }

        // -----------------------------------------------------------------
        // Lo que avisa al resto del juego
        // -----------------------------------------------------------------

        /// <summary>Un golpe que ha pasado el filtro de cámara y niebla.</summary>
        public static void GolpeVisto()
        {
            if (Actual != null) Actual._reglas.Golpe(Time.unscaledTime);
        }

        /// <summary>Atacan algo del jugador: el combate empieza aunque no se vea.</summary>
        public static void Alarma()
        {
            if (Actual != null && Actual._pista == Pista.Partida) Actual._reglas.Alarma(Time.unscaledTime);
        }

        /// <summary>El cartel final: la música se apaga y suena la del desenlace, una vez.</summary>
        public static void Final(bool victoria)
        {
            if (Actual == null || Actual.catalogo == null) return;
            var lista = victoria ? Actual.catalogo.victoria : Actual.catalogo.derrota;
            Actual._pista = Pista.Final;
            Actual.Cambiar(CatalogoDeMusica.Primera(lista), false, 0f, Actual.catalogo.volumenFinal);
        }

        // -----------------------------------------------------------------

        void Update()
        {
            if (catalogo == null) return;
            float ahora = Time.unscaledTime;

            if (_pista == Pista.Partida || _pista == Pista.Combate)
            {
                bool combate = _reglas.Actualizar(ahora);
                if (combate && _pista == Pista.Partida) Poner(Pista.Combate);
                else if (!combate && _pista == Pista.Combate) Poner(Pista.Partida);
                else if (_pista == Pista.Partida && !_a.isPlaying && _nivelA > 0.5f && _a.clip != null)
                {
                    // Acabó la pista de partida: la siguiente de la lista, desde el principio.
                    _indicePartida++;
                    _tiempoPartida = 0f;
                    Poner(Pista.Partida);
                }
            }

            float dt = Time.unscaledDeltaTime;
            _nivelA = ReglasDeMusica.Fundir(_nivelA, 1f, catalogo.fundido, dt);
            _nivelB = ReglasDeMusica.Fundir(_nivelB, 0f, catalogo.fundido, dt);
            if (_nivelB <= 0f && _b.isPlaying) _b.Stop();

            float v = Mezclador.Volumen(Canal.Musica);
            _a.volume = v * _pesoA * _nivelA;
            _b.volume = v * _pesoB * _nivelB;
        }

        void Poner(Pista pista)
        {
            if (catalogo == null) return;

            // Antes de dejar la de partida se apunta por dónde iba.
            if (_pista == Pista.Partida && _a.clip != null && _a.isPlaying) _tiempoPartida = _a.time;
            _pista = pista;

            switch (pista)
            {
                case Pista.Menu:
                    Cambiar(CatalogoDeMusica.Primera(catalogo.menu), true, 0f, catalogo.volumenMenu);
                    break;
                case Pista.Combate:
                    var combate = catalogo.combate;
                    Cambiar(combate.Length == 0 ? null : combate[Random.Range(0, combate.Length)], true, 0f, catalogo.volumenCombate);
                    break;
                case Pista.Partida:
                    var lista = catalogo.partida;
                    if (lista.Length == 0) { Cambiar(null, false, 0f, 0f); break; }
                    // Con una sola pista se repite en bucle; con varias, cada una suena entera
                    // y deja paso a la siguiente.
                    Cambiar(lista[_indicePartida % lista.Length], lista.Length == 1, _tiempoPartida, catalogo.volumenPartida);
                    break;
            }
        }

        /// <summary>El altavoz libre arranca la pista nueva; el que sonaba pasa a apagarse.</summary>
        void Cambiar(AudioClip clip, bool bucle, float desde, float peso)
        {
            // El que sonaba pasa a ser B con el nivel que tenía. El que se estaba apagando,
            // si lo había, se corta: a esas alturas ya casi no se oía.
            float sonaba = _nivelA;
            (_a, _b) = (_b, _a);
            _nivelB = sonaba;
            _nivelA = 0f;
            _pesoB = _pesoA;
            _pesoA = peso;

            _a.Stop();
            _a.clip = clip;
            _a.loop = bucle;
            if (clip == null) return;
            _a.time = Mathf.Clamp(desde, 0f, Mathf.Max(0f, clip.length - 0.1f));
            _a.Play();
        }
    }
}
