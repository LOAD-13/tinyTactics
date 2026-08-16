using UnityEngine;
using UnityEngine.InputSystem;

namespace TinyTactics.Entrada
{
    /// <summary>
    /// Cámara de estrategia en tiempo real.
    /// Paneo con WASD/flechas o empujando el cursor contra el borde de la pantalla,
    /// zoom con la rueda del mouse y confinamiento estricto a los límites del mapa.
    ///
    /// Usa el Input System nuevo (Keyboard.current / Mouse.current) porque el proyecto
    /// está configurado con activeInputHandler = 1: la API vieja (Input.GetAxis) lanza excepción.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Tiny Tactics/Cámara RTS")]
    public class CamaraRTS : MonoBehaviour
    {
        [Header("Paneo")]
        [Tooltip("Unidades por segundo con el zoom más cercano. Escala al alejarse.")]
        public float velocidadPaneo = 16f;

        [Tooltip("Mover la cámara al acercar el cursor al borde de la pantalla.")]
        public bool paneoPorBorde = true;

        [Tooltip("Distancia en píxeles al borde que activa el paneo.")]
        [Range(4f, 80f)] public float margenBorde = 20f;

        [Header("Zoom")]
        public float zoomMinimo = 4f;
        public float zoomMaximo = 20f;

        [Tooltip("Unidades de tamaño ortográfico que cambia por cada muesca de la rueda.")]
        public float pasoZoom = 1.5f;

        [Header("Suavizado")]
        [Tooltip("Mayor = la cámara alcanza su destino más rápido.")]
        [Range(1f, 30f)] public float suavizado = 12f;

        [Header("Límites del mapa (en unidades de mundo)")]
        public Vector2 limiteMinimo = Vector2.zero;
        public Vector2 limiteMaximo = new Vector2(48f, 48f);

        Camera _camara;
        Vector3 _posicionObjetivo;
        float _zoomObjetivo;

        void Awake()
        {
            _camara = GetComponent<Camera>();
            _camara.orthographic = true;

            _zoomObjetivo = Mathf.Clamp(_camara.orthographicSize, zoomMinimo, ZoomMaximoUtil());
            _posicionObjetivo = transform.position;
        }

        void Update()
        {
            ActualizarZoom();
            ActualizarPaneo();
            Aplicar();
        }

        /// <summary>
        /// Ajusta los límites al tamaño real del mapa. La llama el generador de mapas.
        /// </summary>
        public void ConfigurarLimites(Vector2 minimo, Vector2 maximo)
        {
            limiteMinimo = minimo;
            limiteMaximo = maximo;
        }

        /// <summary>
        /// Centra la cámara de golpe, sin interpolación.
        /// </summary>
        public void CentrarEn(Vector2 punto)
        {
            _posicionObjetivo = new Vector3(punto.x, punto.y, transform.position.z);
            transform.position = ConfinarPosicion(_posicionObjetivo, _zoomObjetivo);
        }

        void ActualizarZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float rueda = mouse.scroll.ReadValue().y;

            // El valor bruto de la rueda varía mucho entre plataformas (120 en Windows,
            // fracciones en trackpads). Usamos solo el signo para que el paso sea estable.
            if (Mathf.Abs(rueda) > 0.01f)
                _zoomObjetivo -= Mathf.Sign(rueda) * pasoZoom;

            _zoomObjetivo = Mathf.Clamp(_zoomObjetivo, zoomMinimo, ZoomMaximoUtil());
        }

        void ActualizarPaneo()
        {
            Vector2 direccion = LeerTeclado() + LeerBorde();

            if (direccion.sqrMagnitude > 1f)
                direccion.Normalize();

            if (direccion.sqrMagnitude < 0.0001f) return;

            // Alejado se recorre más terreno por segundo: el desplazamiento se siente
            // constante en pantalla en vez de constante en unidades de mundo.
            float velocidad = velocidadPaneo * (_camara.orthographicSize / zoomMinimo);

            _posicionObjetivo += (Vector3)(direccion * velocidad * Time.unscaledDeltaTime);
        }

        Vector2 LeerTeclado()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return Vector2.zero;

            Vector2 d = Vector2.zero;
            if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) d.y += 1f;
            if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) d.y -= 1f;
            if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) d.x += 1f;
            if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) d.x -= 1f;
            return d;
        }

        Vector2 LeerBorde()
        {
            if (!paneoPorBorde) return Vector2.zero;
            if (!Application.isFocused) return Vector2.zero;

            var mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;

            Vector2 p = mouse.position.ReadValue();

            // Si el cursor salió de la ventana, no paneamos: evita que la cámara
            // se dispare sola cuando el usuario cambia de aplicación.
            if (p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height)
                return Vector2.zero;

            Vector2 d = Vector2.zero;
            if (p.x <= margenBorde) d.x -= 1f;
            if (p.x >= Screen.width - margenBorde) d.x += 1f;
            if (p.y <= margenBorde) d.y -= 1f;
            if (p.y >= Screen.height - margenBorde) d.y += 1f;
            return d;
        }

        void Aplicar()
        {
            float t = 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime);

            _camara.orthographicSize = Mathf.Lerp(_camara.orthographicSize, _zoomObjetivo, t);

            // Confinamos el objetivo además de la posición: si no, el destino se escapa
            // fuera del mapa y la cámara queda pegada al borde con una fuerza residual.
            _posicionObjetivo = ConfinarPosicion(_posicionObjetivo, _camara.orthographicSize);

            transform.position = Vector3.Lerp(transform.position, _posicionObjetivo, t);
        }

        /// <summary>
        /// Tamaño ortográfico máximo que sigue cabiendo dentro del mapa.
        /// Impide alejarse tanto que se vea el vacío fuera del terreno.
        /// </summary>
        float ZoomMaximoUtil()
        {
            float aspecto = _camara != null && _camara.aspect > 0.01f ? _camara.aspect : 16f / 9f;

            float mitadAncho = (limiteMaximo.x - limiteMinimo.x) * 0.5f;
            float mitadAlto = (limiteMaximo.y - limiteMinimo.y) * 0.5f;

            float limitePorAncho = mitadAncho / aspecto;
            float tope = Mathf.Min(limitePorAncho, mitadAlto);

            return Mathf.Max(zoomMinimo, Mathf.Min(zoomMaximo, tope));
        }

        Vector3 ConfinarPosicion(Vector3 posicion, float tamanoOrtografico)
        {
            float aspecto = _camara != null && _camara.aspect > 0.01f ? _camara.aspect : 16f / 9f;

            float mitadAlto = tamanoOrtografico;
            float mitadAncho = tamanoOrtografico * aspecto;

            float minX = limiteMinimo.x + mitadAncho;
            float maxX = limiteMaximo.x - mitadAncho;
            float minY = limiteMinimo.y + mitadAlto;
            float maxY = limiteMaximo.y - mitadAlto;

            // Si la vista es más ancha que el mapa, centramos en ese eje en vez de
            // dejar que Mathf.Clamp reciba min > max (que devolvería el valor de min).
            posicion.x = minX > maxX
                ? (limiteMinimo.x + limiteMaximo.x) * 0.5f
                : Mathf.Clamp(posicion.x, minX, maxX);

            posicion.y = minY > maxY
                ? (limiteMinimo.y + limiteMaximo.y) * 0.5f
                : Mathf.Clamp(posicion.y, minY, maxY);

            return posicion;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Vector3 centro = new Vector3(
                (limiteMinimo.x + limiteMaximo.x) * 0.5f,
                (limiteMinimo.y + limiteMaximo.y) * 0.5f,
                0f);
            Vector3 tamano = new Vector3(
                limiteMaximo.x - limiteMinimo.x,
                limiteMaximo.y - limiteMinimo.y,
                0f);
            Gizmos.DrawWireCube(centro, tamano);
        }
    }
}
