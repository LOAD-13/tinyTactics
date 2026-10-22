using UnityEngine;
using UnityEngine.EventSystems;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Una entrada del menú principal dibujada como espada: entra desde la derecha y se
    /// adelanta al pasar el ratón.
    /// </summary>
    /// <remarks>
    /// Las espadas entran en cascada, una detrás de otra, y no todas a la vez: es el único
    /// momento coreografiado del menú y es lo que le da peso. Al pasar el ratón la espada
    /// «se desenvaina» hacia el centro unos píxeles; es la respuesta que el jugador espera
    /// de algo que se puede pulsar, y sin ella una espada parece un adorno.
    ///
    /// Todo con tiempo sin escalar: el fondo del menú corre más rápido que una partida y el
    /// menú no tiene por qué ir con él.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Efecto de espada")]
    public class EfectoDeEspada : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Orden en la cascada de entrada.")]
        public int orden;

        [Range(0f, 0.3f)] public float retrasoPorOrden = 0.07f;
        [Range(0.1f, 1f)] public float duracionEntrada = 0.45f;
        [Range(0f, 80f)] public float adelanto = 34f;
        [Range(100f, 900f)] public float distanciaEntrada = 520f;

        RectTransform _rt;
        Vector2 _reposo;
        float _inicio;
        float _encima;
        bool _apuntado;

        void Awake()
        {
            _rt = (RectTransform)transform;
            _reposo = _rt.anchoredPosition;
        }

        void OnEnable()
        {
            _inicio = Time.unscaledTime + orden * retrasoPorOrden;
            _encima = 0f;
            _apuntado = false;
            Aplicar(0f);
        }

        public void OnPointerEnter(PointerEventData _) => _apuntado = true;
        public void OnPointerExit(PointerEventData _) => _apuntado = false;

        void Update()
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _inicio) / duracionEntrada);
            _encima = Mathf.MoveTowards(_encima, _apuntado ? 1f : 0f, Time.unscaledDeltaTime * 7f);
            Aplicar(t);
        }

        void Aplicar(float t)
        {
            // Salida con rebote corto: llega un poco más allá y vuelve.
            float e = 1f - Mathf.Pow(1f - t, 3f);
            float rebote = Mathf.Sin(t * Mathf.PI) * 0.06f;

            float x = Mathf.Lerp(distanciaEntrada, 0f, e) - rebote * distanciaEntrada * 0.2f;
            x -= Mathf.SmoothStep(0f, 1f, _encima) * adelanto;

            _rt.anchoredPosition = _reposo + new Vector2(x, 0f);
            _rt.localScale = Vector3.one * (1f + 0.04f * _encima);
        }
    }
}
