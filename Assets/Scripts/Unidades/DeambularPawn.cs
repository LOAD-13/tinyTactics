using UnityEngine;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Hace que un pawn se pasee cerca de su base y descanse entre paseos.
    ///
    /// No es la lógica de unidad definitiva — esa llega en la semana 04 con la máquina
    /// de estados y las órdenes. Esto es ambiente: una base con dos pawns quietos parece
    /// una captura de pantalla; con los pawns moviéndose parece una partida.
    ///
    /// Sí estrena el patrón que usará la unidad real: el estado decide qué tira de
    /// frames toca, y <see cref="AnimadorSprite"/> la reproduce.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(AnimadorSprite))]
    [AddComponentMenu("Tiny Tactics/Deambular (pawn)")]
    public class DeambularPawn : MonoBehaviour
    {
        [Header("Animaciones")]
        public Sprite[] framesReposo;
        public Sprite[] framesCaminar;

        [Header("Movimiento")]
        public float velocidad = 1.6f;

        [Tooltip("Radio alrededor del punto de origen en el que puede pasear.")]
        public float radio = 4f;

        [Tooltip("Segundos parado antes de elegir un nuevo destino.")]
        public Vector2 esperaEntrePaseos = new Vector2(1.5f, 5f);

        Vector2 _origen;
        Vector2 _destino;
        float _esperaRestante;
        bool _caminando;

        AnimadorSprite _animador;
        SpriteRenderer _renderizador;

        void Awake()
        {
            _animador = GetComponent<AnimadorSprite>();
            _renderizador = GetComponent<SpriteRenderer>();

            _origen = transform.position;
            _esperaRestante = Random.Range(esperaEntrePaseos.x, esperaEntrePaseos.y);

            Reposar();
        }

        void Update()
        {
            if (_caminando) Caminar();
            else Esperar();
        }

        void Esperar()
        {
            _esperaRestante -= Time.deltaTime;
            if (_esperaRestante > 0f) return;

            Vector2 salto = Random.insideUnitCircle * radio;
            _destino = _origen + salto;

            _caminando = true;
            AplicarFrames(framesCaminar, 10f);
        }

        void Caminar()
        {
            Vector2 posicion = transform.position;
            Vector2 haciaDestino = _destino - posicion;

            if (haciaDestino.sqrMagnitude < 0.04f)
            {
                _caminando = false;
                _esperaRestante = Random.Range(esperaEntrePaseos.x, esperaEntrePaseos.y);
                Reposar();
                return;
            }

            Vector2 paso = haciaDestino.normalized * velocidad * Time.deltaTime;
            transform.position = new Vector3(posicion.x + paso.x, posicion.y + paso.y, 0f);

            // El sprite mira hacia donde anda. El pack dibuja al pawn mirando a la derecha,
            // así que basta con voltear el renderer. La escala del transform no se toca:
            // arrastraría a los hijos de UI que cuelgan de la unidad.
            if (Mathf.Abs(haciaDestino.x) > 0.05f)
            {
                if (_renderizador != null) _renderizador.flipX = haciaDestino.x < 0f;
            }
        }

        void Reposar()
        {
            AplicarFrames(framesReposo, 7f);
        }

        void AplicarFrames(Sprite[] frames, float fps)
        {
            if (frames == null || frames.Length == 0) return;

            _animador.Configurar(frames, fps, 0);
            _renderizador.sprite = frames[0];
        }

        /// <summary>Configura el pawn desde el generador de mapas.</summary>
        public void Configurar(Sprite[] reposo, Sprite[] caminar, float radioPaseo)
        {
            framesReposo = reposo;
            framesCaminar = caminar;
            radio = radioPaseo;
        }
    }
}
