using UnityEngine;
using TinyTactics.Mundo;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Una oveja pastando: se queda quieta un rato, da unos pasos y vuelve a bajar la
    /// cabeza, siempre sin alejarse de donde nació.
    ///
    /// No es una unidad. No entra en el registro espacial, no recibe órdenes y no bloquea
    /// celdas — es decoración viva, y por eso su coste tiene que ser prácticamente cero:
    /// una máquina de tres estados y una comprobación de grilla por destino, nada más.
    /// Con un centenar sobre el mapa, eso importa.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [AddComponentMenu("Tiny Tactics/Oveja")]
    public class PastarOveja : MonoBehaviour
    {
        enum Estado { Quieta, Pastando, Andando }

        [Header("Movimiento")]
        [Tooltip("Radio en tiles del que la oveja no se aleja.")]
        [Range(1f, 12f)] public float radioDePastoreo = 4f;

        [Range(0.1f, 3f)] public float velocidad = 0.55f;

        [Tooltip("Segundos parada antes de decidir otra cosa.")]
        public Vector2 descanso = new Vector2(1.5f, 5f);

        [Header("Animación")]
        [Tooltip("Fotogramas de estar quieta, de pastar y de andar.")]
        [SerializeField] Sprite[] _framesQuieta;
        [SerializeField] Sprite[] _framesPastando;
        [SerializeField] Sprite[] _framesAndando;

        public float fpsQuieta = 6f;
        public float fpsPastando = 6f;
        public float fpsAndando = 9f;

        SpriteRenderer _sprite;
        AnimadorSprite _animador;

        Vector2 _origen;
        Vector2 _destino;
        Estado _estado = Estado.Quieta;
        float _reloj;
        int _semilla;

        /// <summary>Las tres tiras las inyecta el generador de la escena.</summary>
        public void Configurar(Sprite[] quieta, Sprite[] pastando, Sprite[] andando)
        {
            _framesQuieta = quieta;
            _framesPastando = pastando;
            _framesAndando = andando;
        }

        void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _animador = GetComponent<AnimadorSprite>();

            _origen = transform.position;

            // Cada oveja lleva su propia secuencia. Con un Random compartido, todo el
            // rebaño decidiría a la vez y se movería como un banco de peces.
            _semilla = (int)(_origen.x * 73856093f) ^ (int)(_origen.y * 19349663f);

            Cambiar(Estado.Quieta);
        }

        void Update()
        {
            _reloj -= Time.deltaTime;

            if (_estado == Estado.Andando)
            {
                Andar();
                return;
            }

            if (_reloj > 0f) return;

            // Pasta más de lo que camina: un rebaño en movimiento constante distrae la
            // vista del jugador, que es justo lo que no debe hacer la decoración.
            Cambiar(Siguiente(3) == 0 ? Estado.Andando : Estado.Pastando);
        }

        void Andar()
        {
            Vector2 posicion = transform.position;
            Vector2 hacia = _destino - posicion;

            if (hacia.sqrMagnitude < 0.02f || _reloj <= 0f)
            {
                Cambiar(Estado.Quieta);
                return;
            }

            Vector2 paso = hacia.normalized * velocidad * Time.deltaTime;
            transform.position = new Vector3(posicion.x + paso.x, posicion.y + paso.y, 0f);

            if (Mathf.Abs(hacia.x) > 0.05f && _sprite != null)
                _sprite.flipX = hacia.x < 0f;
        }

        void Cambiar(Estado nuevo)
        {
            if (nuevo == Estado.Andando && !ElegirDestino())
                nuevo = Estado.Pastando;

            _estado = nuevo;

            switch (nuevo)
            {
                case Estado.Andando:
                    // Tope de tiempo además del de distancia: si algo la deja empujada
                    // contra un obstáculo, no se queda andando contra él para siempre.
                    _reloj = 6f;
                    Aplicar(_framesAndando, fpsAndando);
                    break;

                case Estado.Pastando:
                    _reloj = Rango(descanso.x, descanso.y);
                    Aplicar(_framesPastando, fpsPastando);
                    break;

                default:
                    _reloj = Rango(descanso.x * 0.5f, descanso.y * 0.5f);
                    Aplicar(_framesQuieta, fpsQuieta);
                    break;
            }
        }

        /// <summary>Un punto al azar dentro del radio, siempre que sea terreno pisable.</summary>
        bool ElegirDestino()
        {
            var mundo = MundoJuego.Actual;

            for (int intento = 0; intento < 4; intento++)
            {
                float angulo = Rango(0f, Mathf.PI * 2f);
                float radio = Rango(0.8f, radioDePastoreo);

                Vector2 candidato = _origen + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio;

                if (mundo != null && mundo.Grilla != null)
                {
                    var celda = mundo.Grilla.MundoACelda(candidato);
                    if (!mundo.Grilla.Transitable(celda.x, celda.y)) continue;
                }

                _destino = candidato;
                return true;
            }

            return false;
        }

        void Aplicar(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return;

            if (_animador != null) _animador.Configurar(frames, fps, Siguiente(frames.Length));
            else if (_sprite != null) _sprite.sprite = frames[0];
        }

        // Generador congruente propio: barato, determinista y sin reservar memoria.
        int Siguiente(int tope)
        {
            _semilla = _semilla * 1103515245 + 12345;
            return Mathf.Abs(_semilla / 65536) % Mathf.Max(1, tope);
        }

        float Rango(float minimo, float maximo) =>
            minimo + (maximo - minimo) * (Siguiente(1000) / 1000f);
    }
}
