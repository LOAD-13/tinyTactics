using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

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

        [Tooltip("Radio propio de la oveja para no meterse dentro de las unidades.")]
        [Range(0.2f, 1.5f)] public float radio = 0.45f;

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
            // Apartarse va antes que cualquier otra cosa y corre en TODOS los estados, no
            // solo mientras anda: una oveja pastando tambien tiene que dejar de ser un
            // fantasma cuando un pawn le pasa por encima.
            Apartarse();

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

        static readonly List<Unidades.Unidad> _cerca = new List<Unidades.Unidad>(16);

        /// <summary>
        /// Saca a la oveja de dentro de cualquier unidad con la que se solape.
        /// </summary>
        /// <remarks>
        /// La oveja no entra en el indice espacial —no es una unidad y no debe pagar ese
        /// coste— asi que la separacion es de un solo sentido: ella se aparta y la unidad ni
        /// se entera. Basta, porque el resultado que se ve es el mismo y evita meter
        /// decoracion dentro del bucle de simulacion.
        ///
        /// Se corrige la posicion en el momento en vez de acumular una fuerza: con un empuje
        /// blando la oveja tardaria varios fotogramas en salir y durante ese rato se la
        /// seguiria viendo atravesada.
        /// </remarks>
        void Apartarse()
        {
            Vector2 posicion = transform.position;
            RegistroDeUnidades.VecinasEnRadio(posicion, radio + 1f, _cerca);

            Vector2 correccion = Vector2.zero;

            for (int i = 0; i < _cerca.Count; i++)
            {
                var u = _cerca[i];
                if (u == null || !u.Viva) continue;

                Vector2 delta = posicion - (Vector2)u.transform.position;
                float minimo = radio + u.Radio;
                float d = delta.magnitude;

                if (d >= minimo) continue;

                // Justo encima: se elige una salida cualquiera, pero determinista, para que
                // dos ovejas apiladas no salgan las dos hacia el mismo lado.
                if (d < 0.0001f)
                {
                    float angulo = Siguiente(360) * Mathf.Deg2Rad;
                    correccion += new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * minimo;
                    continue;
                }

                correccion += delta / d * (minimo - d);
            }

            if (correccion.sqrMagnitude < 0.000001f) return;

            transform.position = new Vector3(posicion.x + correccion.x,
                                             posicion.y + correccion.y, 0f);
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
