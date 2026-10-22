using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El brillo que cruza el logo cada pocos segundos, y un leve vaivén.
    /// </summary>
    /// <remarks>
    /// <b>Un GIF no sirve dentro de Unity</b>: el motor no reproduce GIF animados, y aunque
    /// lo hiciera, un GIF solo tiene transparencia de todo o nada y el borde del logo saldría
    /// dentado. Aquí el brillo es una franja de luz que se recorta con la forma del logo
    /// (una <c>Mask</c> sobre la propia imagen), así que respeta su contorno exacto a
    /// cualquier resolución.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Brillo de logo")]
    public class BrilloDeLogo : MonoBehaviour
    {
        public RectTransform franja;
        [Range(1f, 10f)] public float cadaCuanto = 4.5f;
        [Range(0.3f, 3f)] public float duracion = 1.1f;
        [Range(0f, 12f)] public float vaiven = 5f;

        RectTransform _rt;
        Vector2 _base;
        float _inicio;

        void OnEnable()
        {
            _rt = (RectTransform)transform;
            // El logo se coloca por sus anclas; el vaivén va sobre un desplazamiento cero.
            _base = Vector2.zero;
            _inicio = Time.unscaledTime + 0.6f;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            _rt.anchoredPosition = _base + new Vector2(0f, Mathf.Sin(t * 1.3f) * vaiven);

            if (franja == null) return;

            float ancho = _rt.rect.width;
            float p = ((t - _inicio) % cadaCuanto) / duracion;

            // Fuera de su tramo, la franja espera escondida a la izquierda.
            float x = p >= 0f && p <= 1f
                ? Mathf.Lerp(-ancho * 0.75f, ancho * 0.75f, Mathf.SmoothStep(0f, 1f, p))
                : -ancho;

            franja.anchoredPosition = new Vector2(x, 0f);
        }
    }
}
