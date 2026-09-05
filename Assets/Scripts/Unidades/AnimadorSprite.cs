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

        [Tooltip("Al desactivarlo, la tira se reproduce una vez y se queda en el último frame.")]
        public bool enBucle = true;

        /// <summary>
        /// Aviso de que una tira sin bucle llegó al final.
        ///
        /// Es lo que permite que un golpe dure lo que dura su animación en vez de un
        /// número inventado: la máquina de estados se suscribe y vuelve a reposo cuando
        /// el dibujo termina, no cuando expira un temporizador que hay que mantener a mano.
        /// </summary>
        public event System.Action AlTerminar;

        /// <summary>
        /// Salta cada vez que una tira <b>en bucle</b> completa una pasada.
        ///
        /// Es el equivalente de <see cref="AlTerminar"/> para las animaciones que se
        /// repiten: sin esto no hay forma de saber cuántos hachazos ha dado un pawn, solo
        /// cuánto rato lleva delante del árbol, que no es lo mismo.
        /// </summary>
        public event System.Action AlDarVuelta;

        SpriteRenderer _renderizador;
        float _reloj;
        int _indice;
        float _factorDispersion = 1f;
        bool _terminado;

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
            if (frames == null || frames.Length < 2 || _terminado) return;

            _reloj += Time.deltaTime * velocidad * _factorDispersion;

            // while, no if: si el juego dio un tirón hay que recuperar varios frames.
            while (_reloj >= 1f)
            {
                _reloj -= 1f;

                if (!enBucle && _indice >= frames.Length - 1)
                {
                    _terminado = true;
                    AlTerminar?.Invoke();
                    return;
                }

                _indice = (_indice + 1) % frames.Length;
                _renderizador.sprite = frames[_indice];

                if (_indice == 0) AlDarVuelta?.Invoke();
            }
        }

        /// <summary>
        /// Configura la animación desde código. Sirve tanto al generar el mapa como
        /// para cambiar de animación en caliente (reposo ↔ caminar).
        /// </summary>
        public void Configurar(Sprite[] nuevosFrames, float fps, int inicio) =>
            Configurar(nuevosFrames, fps, inicio, true);

        public void Configurar(Sprite[] nuevosFrames, float fps, int inicio, bool repite)
        {
            frames = nuevosFrames;
            velocidad = fps;
            frameInicial = inicio;
            enBucle = repite;
            _terminado = false;

            // Al cambiar de tira hay que reencuadrar el índice: la nueva puede tener
            // menos frames que la anterior y quedaríamos fuera de rango.
            _indice = (frames != null && frames.Length > 0)
                ? Mathf.Abs(inicio) % frames.Length
                : 0;

            _reloj = 0f;

            // Pintar ya el primer frame. Sin esto, una tira de un golpe se vería un
            // instante con el sprite de la animación anterior.
            if (_renderizador != null && frames != null && frames.Length > 0)
                _renderizador.sprite = frames[_indice];
        }
    }
}
