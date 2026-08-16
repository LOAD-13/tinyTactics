using UnityEngine;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Animador por intercambio de sprites.
    ///
    /// Implementa el <b>ADR-03</b>: el componente <c>Animator</c> de Unity cuesta del
    /// orden de 0.1 ms por instancia, y con 250 unidades en pantalla es el primer
    /// cuello de botella del juego. Las animaciones del pack Tiny Swords son tiras de
    /// sprites planas, así que un índice de frame y un temporizador hacen exactamente
    /// lo mismo por una fracción del costo.
    ///
    /// Se estrena aquí con ovejas y arbustos; lo heredarán las unidades en la semana 04.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Tiny Tactics/Animador de sprite")]
    public class AnimadorSprite : MonoBehaviour
    {
        [Tooltip("Frames de la animación, en orden.")]
        public Sprite[] frames;

        [Tooltip("Frames por segundo.")]
        [Range(1f, 30f)] public float velocidad = 8f;

        [Tooltip("Frame inicial. Desfasarlo entre instancias evita que todo lata al unísono.")]
        public int frameInicial;

        [Tooltip("Variación aleatoria de velocidad por instancia, en porcentaje.")]
        [Range(0f, 0.5f)] public float dispersion = 0.15f;

        SpriteRenderer _renderizador;
        float _reloj;
        int _indice;
        float _factorDispersion = 1f;

        void Awake()
        {
            _renderizador = GetComponent<SpriteRenderer>();

            // Cada instancia corre a su ritmo: un rebaño perfectamente sincronizado
            // se ve artificial de inmediato. El factor se fija una vez y se conserva
            // aunque después se cambie de animación.
            _factorDispersion = 1f + Random.Range(-dispersion, dispersion);

            _indice = frames != null && frames.Length > 0
                ? Mathf.Abs(frameInicial) % frames.Length
                : 0;
        }

        void Update()
        {
            if (frames == null || frames.Length < 2) return;

            _reloj += Time.deltaTime * velocidad * _factorDispersion;

            // while, no if: si el juego dio un tirón hay que recuperar varios frames.
            while (_reloj >= 1f)
            {
                _reloj -= 1f;
                _indice = (_indice + 1) % frames.Length;
                _renderizador.sprite = frames[_indice];
            }
        }

        /// <summary>
        /// Configura la animación desde código. Sirve tanto al generar el mapa como
        /// para cambiar de animación en caliente (reposo ↔ caminar).
        /// </summary>
        public void Configurar(Sprite[] nuevosFrames, float fps, int inicio)
        {
            frames = nuevosFrames;
            velocidad = fps;
            frameInicial = inicio;

            // Al cambiar de tira hay que reencuadrar el índice: la nueva puede tener
            // menos frames que la anterior y quedaríamos fuera de rango.
            _indice = (frames != null && frames.Length > 0)
                ? Mathf.Abs(inicio) % frames.Length
                : 0;

            _reloj = 0f;
        }
    }
}
