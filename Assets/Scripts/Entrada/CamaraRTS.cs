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
        [Tooltip("Medido: a 4 solo se ven 16x8 tiles y el juego deja de leerse. 8 es el " +
                 "encuadre util mas cercano en un mapa de 224.")]
        public float zoomMinimo = 8f;
        public float zoomMaximo = 20f;

        [Tooltip("Unidades de tamaño ortográfico que cambia por cada muesca de la rueda.")]
        public float pasoZoom = 1.5f;

        [Header("Suavizado")]
        [Tooltip("Mayor = la cámara alcanza su destino más rápido.")]
        [Range(1f, 30f)] public float suavizado = 12f;

        [Header("Diagnóstico")]
        [Tooltip("Escribe en consola lo que lee el paneo. Activar solo para depurar.")]
        public bool registrarEntrada;

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

            // Traza incondicional al arrancar. Si esta linea no sale en consola, el
            // componente no se esta ejecutando y el problema no esta en el paneo.
            Debug.Log(
                $"[CamaraRTS] Awake OK. camara={_camara != null} ortho={_camara.orthographic} " +
                $"pos={transform.position} zoom={_camara.orthographicSize:F1} " +
                $"limites={limiteMinimo}..{limiteMaximo} " +
                $"teclado={(Keyboard.current != null ? Keyboard.current.displayName : "NULO")} " +
                $"raton={(Mouse.current != null ? "si" : "NULO")}", this);
        }

        bool _primerUpdate = true;

        void Update()
        {
            if (_primerUpdate)
            {
                _primerUpdate = false;
                Debug.Log($"[CamaraRTS] Update corriendo. timeScale={Time.timeScale}", this);
            }

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

            Diagnostico(direccion);

            if (direccion.sqrMagnitude < 0.0001f) return;

            // Alejado se recorre más terreno por segundo: el desplazamiento se siente
            // constante en pantalla en vez de constante en unidades de mundo.
            float velocidad = velocidadPaneo * (_camara.orthographicSize / zoomMinimo);

            _posicionObjetivo += (Vector3)(direccion * velocidad * Time.unscaledDeltaTime);
        }

        /// <summary>
        /// Paneo con las flechas. WASD ya no mueve la cámara.
        ///
        /// Chocaba con los comandos: la S de «abajo» era también la de Detener, así que
        /// bajar la vista paraba en seco a todo lo seleccionado, y la A de «izquierda»
        /// disparaba Atacar. Es el reparto de cualquier RTS — flechas para la cámara,
        /// letras para las órdenes — y el borde de la pantalla sigue paneando igual.
        /// </summary>
        Vector2 LeerTeclado()
        {
            var teclado = Keyboard.current;
            if (teclado == null) return Vector2.zero;

            Vector2 d = Vector2.zero;
            if (teclado.upArrowKey.isPressed) d.y += 1f;
            if (teclado.downArrowKey.isPressed) d.y -= 1f;
            if (teclado.rightArrowKey.isPressed) d.x += 1f;
            if (teclado.leftArrowKey.isPressed) d.x -= 1f;
            return d;
        }

        Vector2 LeerBorde()
        {
            if (!paneoPorBorde) return Vector2.zero;
            if (!Application.isFocused) return Vector2.zero;

            var mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;

            Vector2 p = mouse.position.ReadValue();

            // Se trabaja en coordenadas normalizadas (0..1) en vez de píxeles.
            //
            // El motivo es el deslizador Scale de la Game view: cuando no está en 1x,
            // Screen.width deja de coincidir con el rango real del cursor, y una
            // comparación en píxeles descarta el paneo siempre. Normalizando, el margen
            // sigue siendo proporcional y da igual a qué resolución se renderice.
            float ancho = Mathf.Max(1, Screen.width);
            float alto = Mathf.Max(1, Screen.height);

            Vector2 n = new Vector2(p.x / ancho, p.y / alto);

            // El cursor tiene que estar DENTRO de la vista. Sin margen de tolerancia.
            //
            // Antes se admitía un 5 % de holgura para "tolerar el desfase", y eso causaba
            // el problema de siempre: al darle a Play con el ratón sobre la Jerarquía —que
            // está a la izquierda de la vista de juego— la coordenada normalizada salía
            // ligeramente negativa, caía dentro de la holgura, y la cámara empezaba a
            // panear hacia la izquierda sola. Por eso el juego arrancaba mirando bastante
            // más a la izquierda de donde quedó colocada la cámara en la escena.
            if (n.x < 0f || n.y < 0f || n.x > 1f || n.y > 1f)
                return Vector2.zero;

            float margen = Mathf.Clamp01(margenBorde / ancho);
            float margenY = Mathf.Clamp01(margenBorde / alto);

            Vector2 d = Vector2.zero;
            if (n.x <= margen) d.x -= 1f;
            if (n.x >= 1f - margen) d.x += 1f;
            if (n.y <= margenY) d.y -= 1f;
            if (n.y >= 1f - margenY) d.y += 1f;
            return d;
        }

        float _relojDiagnostico;

        void Diagnostico(Vector2 direccion)
        {
            if (!registrarEntrada) return;

            _relojDiagnostico += Time.unscaledDeltaTime;
            if (_relojDiagnostico < 0.5f) return;
            _relojDiagnostico = 0f;

            float mitadAlto = _camara.orthographicSize;
            float mitadAncho = mitadAlto * _camara.aspect;

            var teclado = Keyboard.current;
            var raton = Mouse.current;
            Vector2 posRaton = raton != null ? raton.position.ReadValue() : Vector2.zero;

            // Si el raton se sale del rango de Screen, el deslizador Scale de la Game
            // view no esta en 1x: todo el calculo en espacio de pantalla queda desfasado.
            bool ratonFueraDeRango =
                posRaton.x < 0f || posRaton.y < 0f ||
                posRaton.x > Screen.width || posRaton.y > Screen.height;

            Debug.Log(
                $"[CamaraRTS] W={teclado?.wKey.isPressed} A={teclado?.aKey.isPressed} " +
                $"S={teclado?.sKey.isPressed} D={teclado?.dKey.isPressed} " +
                $"foco={Application.isFocused}\n" +
                $"raton={posRaton} pantalla=({Screen.width}x{Screen.height}) " +
                $"fueraDeRango={ratonFueraDeRango}" +
                (ratonFueraDeRango ? "  <-- pon el Scale de la Game view en 1x" : "") + "\n" +
                $"direccion={direccion}  pos={transform.position}  objetivo={_posicionObjetivo}\n" +
                $"zoom={_camara.orthographicSize:F1} aspecto={_camara.aspect:F2} " +
                $"mitad=({mitadAncho:F1},{mitadAlto:F1})\n" +
                $"rangoX=[{limiteMinimo.x + mitadAncho:F1},{limiteMaximo.x - mitadAncho:F1}] " +
                $"rangoY=[{limiteMinimo.y + mitadAlto:F1},{limiteMaximo.y - mitadAlto:F1}]");
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
