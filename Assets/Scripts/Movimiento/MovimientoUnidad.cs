using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Movimiento
{
    /// <summary>
    /// Lleva a la unidad por la ruta que le devuelve el buscador, interpolando entre
    /// waypoints y separándose de sus vecinas.
    ///
    /// Implementa el ADR-02: A* sobre la grilla decide <i>por dónde</i>, pero la unidad
    /// no salta de celda en celda — se desplaza de forma continua. Se ve como un RTS
    /// moderno y cuesta como una grilla.
    /// </summary>
    [RequireComponent(typeof(Unidad))]
    [AddComponentMenu("Tiny Tactics/Movimiento de unidad")]
    public class MovimientoUnidad : MonoBehaviour
    {
        [Header("Llegada")]
        [Tooltip("Distancia a la que se da por alcanzado un waypoint.")]
        public float toleranciaWaypoint = 0.12f;

        [Header("Separación")]
        [Tooltip("Fuerza del empuje entre unidades.")]
        [Range(0f, 8f)] public float fuerzaEmpuje = 3.2f;

        public bool EnMovimiento => _ruta != null && _indice < _ruta.Count;

        Unidad _unidad;
        SpriteRenderer _sprite;
        AnimadorSprite _animador;

        List<Vector2Int> _ruta;
        int _indice;
        bool _esperandoRuta;
        bool _caminabaAntes;

        // [SerializeField] es imprescindible: el generador las asigna al construir la
        // escena, y sin serializar se perderian al guardarla. En Play llegarian nulas
        // y la unidad se moveria sin animacion de caminar.
        [SerializeField] Sprite[] _framesReposo;
        [SerializeField] Sprite[] _framesCaminar;

        static readonly List<Unidad> _vecinas = new List<Unidad>(32);

        void Awake()
        {
            _unidad = GetComponent<Unidad>();
            _sprite = GetComponent<SpriteRenderer>();
            _animador = GetComponent<AnimadorSprite>();
        }

        /// <summary>Las tiras de animación las inyecta el generador de la escena.</summary>
        public void ConfigurarAnimacion(Sprite[] reposo, Sprite[] caminar)
        {
            _framesReposo = reposo;
            _framesCaminar = caminar;
            AplicarAnimacion(false);
        }

        // -----------------------------------------------------------------

        /// <summary>Pide ruta hasta una celda. La búsqueda va en cola, no es inmediata.</summary>
        public void IrA(Vector2Int destino)
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Rutas == null) return;

            Vector2Int origen = mundo.Grilla.MundoACelda(transform.position);

            _esperandoRuta = true;
            _ruta = null;
            _indice = 0;

            mundo.Rutas.PedirRuta(origen, destino, ruta =>
            {
                // La unidad puede haber muerto o recibido otra orden mientras esperaba.
                if (this == null || !_esperandoRuta) return;

                _esperandoRuta = false;
                _ruta = ruta;
                _indice = 0;
            });
        }

        public void Detener()
        {
            _esperandoRuta = false;
            _ruta = null;
            _indice = 0;
        }

        // -----------------------------------------------------------------

        void Update()
        {
            Avanzar();
            ActualizarAnimacion();
        }

        void LateUpdate()
        {
            Separarse();
        }

        void Avanzar()
        {
            if (!EnMovimiento) return;

            var mundo = MundoJuego.Actual;
            if (mundo == null) return;

            Vector3 objetivo = mundo.Grilla.CeldaAMundo(_ruta[_indice]);
            Vector3 posicion = transform.position;
            Vector3 hacia = objetivo - posicion;
            hacia.z = 0f;

            if (hacia.sqrMagnitude <= toleranciaWaypoint * toleranciaWaypoint)
            {
                _indice++;
                if (_indice >= _ruta.Count) Detener();
                return;
            }

            Vector3 paso = hacia.normalized * _unidad.Velocidad * Time.deltaTime;

            // No pasarse del waypoint cuando el paso es mayor que lo que falta:
            // produciría un temblor de ida y vuelta alrededor del punto.
            if (paso.sqrMagnitude > hacia.sqrMagnitude) paso = hacia;

            transform.position = posicion + paso;

            // Se voltea el SpriteRenderer, nunca la escala del transform. Invertir la escala
            // arrastraría a los hijos — barra de vida y marcador de selección — y la barra
            // acabaría vaciándose al revés cada vez que la unidad camina hacia la izquierda.
            if (Mathf.Abs(hacia.x) > 0.02f && _sprite != null)
                _sprite.flipX = hacia.x < 0f;
        }

        /// <summary>
        /// Empuje blando: las unidades demasiado juntas se separan un poco cada frame.
        ///
        /// No es física de Unity (ADR-08). Es una corrección de posición directa, lo que
        /// la hace determinista y barata. Los vecinos salen del índice espacial, nunca de
        /// recorrer la lista completa (ADR-04).
        /// </summary>
        void Separarse()
        {
            if (fuerzaEmpuje <= 0f) return;

            RegistroDeUnidades.Vecinas(transform.position, _vecinas);
            if (_vecinas.Count <= 1) return;

            Vector2 desplazamiento = Vector2.zero;
            Vector2 propia = transform.position;
            float radioPropio = _unidad.Radio;

            for (int i = 0; i < _vecinas.Count; i++)
            {
                var otra = _vecinas[i];
                if (otra == null || otra == _unidad) continue;

                Vector2 delta = propia - (Vector2)otra.transform.position;
                float minimo = radioPropio + otra.Radio;
                float distancia2 = delta.sqrMagnitude;

                if (distancia2 >= minimo * minimo || distancia2 < 1e-6f) continue;

                float distancia = Mathf.Sqrt(distancia2);
                desplazamiento += delta / distancia * (minimo - distancia);
            }

            if (desplazamiento.sqrMagnitude < 1e-6f) return;

            Vector3 destino = transform.position +
                              (Vector3)(desplazamiento * fuerzaEmpuje * Time.deltaTime);

            // El empuje nunca puede meter a una unidad en el agua o dentro de un árbol.
            var mundo = MundoJuego.Actual;
            if (mundo != null && mundo.Grilla != null)
            {
                var celda = mundo.Grilla.MundoACelda(destino);
                if (!mundo.Grilla.Transitable(celda.x, celda.y)) return;
            }

            transform.position = destino;
        }

        void ActualizarAnimacion()
        {
            bool camina = EnMovimiento;
            if (camina == _caminabaAntes) return;

            _caminabaAntes = camina;
            AplicarAnimacion(camina);
        }

        void AplicarAnimacion(bool caminando)
        {
            if (_animador == null) return;

            var frames = caminando ? _framesCaminar : _framesReposo;
            if (frames == null || frames.Length == 0) return;

            var datos = _unidad.datos;
            float fps = datos == null ? 8f : (caminando ? datos.fpsCaminar : datos.fpsReposo);

            _animador.Configurar(frames, fps, 0);
            if (_sprite != null) _sprite.sprite = frames[0];
        }
    }
}
