using UnityEngine;
using TinyTactics.Unidades;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Barra de vida flotando sobre la unidad, con el marco y el relleno del pack.
    ///
    /// El relleno no se escala desde su centro sino desde el borde izquierdo de la cavidad
    /// del marco: por eso cuelga de un nodo intermedio. Escalar el sprite directamente lo
    /// encogería hacia el medio y se vería salir del marco por los dos lados.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Barra de vida")]
    public class BarraDeVida : MonoBehaviour
    {
        /// <summary>Cuándo se ve la barra.</summary>
        public enum Visibilidad { Siempre, SeleccionadaOHerida }

        [Header("Comportamiento")]
        public Visibilidad visibilidad = Visibilidad.Siempre;

        [Tooltip("Se oculta cuando la vida baja de esta fracción solo en modo SeleccionadaOHerida.")]
        [Range(0f, 1f)] public float umbralHerida = 0.999f;

        [Header("Piezas")]
        [SerializeField] SpriteRenderer _marco;
        [SerializeField] Transform _escala;
        [SerializeField] SpriteRenderer _relleno;

        [Header("Geometría de la cavidad")]
        [Tooltip("Primer píxel útil del interior del marco, sobre el sprite ya recortado.")]
        public float cavidadX = 11f;

        [Tooltip("Ancho en píxeles del interior del marco.")]
        public float cavidadAncho = 74f;

        Unidad _unidad;
        float _anchoBase;
        float _ultimaFraccion = -1f;

        /// <summary>La llama el constructor de la escena con las piezas ya creadas.</summary>
        public void Configurar(SpriteRenderer marco, Transform escala, SpriteRenderer relleno)
        {
            _marco = marco;
            _escala = escala;
            _relleno = relleno;
        }

        void Awake()
        {
            _unidad = GetComponentInParent<Unidad>();
            Medir();
        }

        /// <summary>
        /// Coloca el relleno pegado al borde izquierdo de la cavidad y calcula cuánto hay
        /// que escalarlo para que, al 100 % de vida, llegue justo al otro extremo.
        ///
        /// Todo sale de <c>Sprite.pivot</c> y <c>Sprite.bounds</c>, nunca de constantes en
        /// unidades de mundo: así el cálculo sigue valiendo si mañana se reimportan los
        /// sprites con otro "pixels per unit".
        /// </summary>
        void Medir()
        {
            if (_marco == null || _marco.sprite == null) return;
            if (_escala == null || _relleno == null || _relleno.sprite == null) return;

            var marco = _marco.sprite;
            float ppu = marco.pixelsPerUnit;

            float izquierda = (cavidadX - marco.pivot.x) / ppu;
            _escala.localPosition = new Vector3(izquierda, 0f, 0f);

            // El relleno se ancla por su borde izquierdo al origen del nodo de escala.
            var relleno = _relleno.sprite;
            _relleno.transform.localPosition = new Vector3(-relleno.bounds.min.x, 0f, 0f);

            float anchoNatural = relleno.bounds.size.x;
            _anchoBase = anchoNatural > 0.0001f
                ? (cavidadAncho / ppu) / anchoNatural
                : 1f;

            // Arranca lleno. Si no, el primer frame se vería la barra a la anchura cruda
            // del sprite, que no coincide con la cavidad.
            _escala.localScale = new Vector3(_anchoBase, 1f, 1f);
        }

        void LateUpdate()
        {
            if (_unidad == null || _unidad.datos == null) return;

            float fraccion = Mathf.Clamp01((float)_unidad.Vida / Mathf.Max(1, _unidad.datos.vidaMaxima));

            bool visible = visibilidad == Visibilidad.Siempre ||
                           _unidad.Seleccionada ||
                           fraccion < umbralHerida;

            if (_marco != null && _marco.enabled != visible) _marco.enabled = visible;
            if (_relleno != null && _relleno.enabled != visible) _relleno.enabled = visible;

            if (!visible || Mathf.Approximately(fraccion, _ultimaFraccion)) return;

            _ultimaFraccion = fraccion;

            if (_escala != null)
                _escala.localScale = new Vector3(_anchoBase * fraccion, 1f, 1f);
        }
    }
}
