using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Datos;
using TinyTactics.Mundo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El recurso bajo el puntero se ilumina y dice cuánto le queda.
    ///
    /// <b>Por qué hace falta.</b> Los nodos se agotan, y esa es la mecánica que empuja a los
    /// jugadores fuera de su esquina. Pero un árbol al que le queda una tala se dibuja
    /// exactamente igual que uno entero: sin esto, la decisión de cuándo mudar a los pawns
    /// se toma a ciegas y el agotamiento se nota como una sorpresa en vez de como una
    /// cuenta atrás.
    ///
    /// <b>Por qué se tiñe por código y no se cambia el sprite.</b> El pack trae un
    /// <c>_Highlight</c> para las piedras de oro, pero no para los árboles ni para las
    /// ovejas. Con el sprite alternativo, un tercio de los recursos del mapa se iluminaría y
    /// los otros dos tercios no, y el jugador aprendería que el resaltado no es de fiar.
    /// Teñir el que ya está puesto funciona igual en los tres.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Resaltado de nodo")]
    public class ResaltadoDeNodo : MonoBehaviour
    {
        [Header("Detección")]
        [Tooltip("Mismo radio que usa el clic derecho, para que se ilumine exactamente lo " +
                 "que se va a seleccionar. Con radios distintos, el jugador apuntaría a un " +
                 "árbol iluminado y la orden caería en el de al lado.")]
        public float radio = 1.1f;

        [Header("Tema")]
        [Tooltip("De aquí sale la caja de madera del cartel. Sin tema, el cartel no se pinta.")]
        public TemaInterfaz tema;

        [Tooltip("Bando cuyo color lleva la caja del cartel.")]
        public int faccion;

        [Header("Aspecto")]
        [Tooltip("Multiplicador de color. Por encima de 1 aclara; el tono cálido lo acerca " +
                 "al dorado del pack en vez de dejarlo en un blanco de foco de quirófano.")]
        public Color tinte = new Color(1.45f, 1.34f, 1.02f, 1f);

        [Tooltip("Color del texto del cartel.")]
        public Color colorTexto = new Color(1f, 0.97f, 0.88f);

        [Tooltip("Píxeles por encima del recurso a los que flota el cartel.")]
        public float alturaCartel = 26f;

        Camera _camara;
        NodoRecurso _actual;
        SpriteRenderer _pintado;
        Color _original;

        GUIStyle _estilo;
        Texture2D _fondo;

        void Awake() => _camara = Camera.main;

        // El fondo es la textura del tema, prestada. No se destruye aquí: destruirla se
        // llevaría por delante las cajas del HUD y del panel, que usan la misma.
        void OnDisable() => Soltar();

        void Update()
        {
            if (_camara == null) { _camara = Camera.main; return; }

            var raton = Mouse.current;
            if (raton == null) { Soltar(); return; }

            Vector2 pantalla = raton.position.ReadValue();

            // Sobre el panel no hay mapa debajo, y con una silueta en la mano el jugador
            // está mirando el terreno, no los recursos.
            var colocador = Edificios.ColocadorEdificios.Actual;

            if ((PanelDeUnidad.Actual != null && PanelDeUnidad.Actual.CapturaPuntero(pantalla)) ||
                (colocador != null && colocador.Activo))
            {
                Soltar();
                return;
            }

            Vector3 punto = _camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 0f));
            punto.z = 0f;

            Tomar(NodoRecurso.NodoEn(punto, radio));
        }

        // -----------------------------------------------------------------

        void Tomar(NodoRecurso nodo)
        {
            if (nodo == _actual)
            {
                // La oveja se mueve mientras está resaltada, así que el renderizador puede
                // seguir siendo el mismo y el color no. Nada que hacer aquí: el tinte ya
                // está puesto y se quita al soltarla.
                return;
            }

            Soltar();

            if (nodo == null || nodo.Agotado) return;

            _actual = nodo;
            _pintado = nodo.GetComponent<SpriteRenderer>();

            if (_pintado == null) return;

            _original = _pintado.color;
            _pintado.color = _original * tinte;
        }

        /// <summary>
        /// Devuelve el color de siempre.
        /// </summary>
        /// <remarks>
        /// Se guarda el color <b>que tenía</b> y no se asume blanco: las ovejas del criadero
        /// y el poste de pruebas nacen teñidos, y devolverlos a blanco los habría
        /// despintado con solo pasarles el ratón por encima.
        /// </remarks>
        void Soltar()
        {
            if (_pintado != null) _pintado.color = _original;

            _pintado = null;
            _actual = null;
        }

        // -----------------------------------------------------------------

        void OnGUI()
        {
            if (_actual == null || _camara == null) return;

            // El nodo puede haberse agotado entre el Update y el pintado.
            if (_actual.Agotado) return;

            Vector3 pantalla = _camara.WorldToScreenPoint(_actual.transform.position);
            if (pantalla.z < 0f) return;

            PrepararEstilo();

            string texto = _actual.soloDespejar
                ? "Despejar"
                : $"{NombreDe(_actual.recurso)}  ·  {_actual.Restantes}";

            var medida = _estilo.CalcSize(new GUIContent(texto));

            // Relleno generoso: la caja del pack tiene un reborde de madera grueso, y un
            // margen ajustado dejaría el texto montado encima del marco.
            float ancho = medida.x + 34f;
            float alto = medida.y + 26f;

            // OnGUI mide desde arriba; el mundo, desde abajo.
            var caja = new Rect(pantalla.x - ancho * 0.5f,
                                Screen.height - pantalla.y - alturaCartel - alto,
                                ancho, alto);

            GUI.color = Color.white;
            if (_fondo != null) GUI.DrawTexture(caja, _fondo, ScaleMode.StretchToFill, true);

            // El texto sube un poco: el reborde inferior de la caja es más grueso que el
            // superior, así que un centrado geométrico se ve caído. Es el mismo ajuste que
            // ya llevan los contadores del HUD.
            GUI.Label(new Rect(caja.x, caja.y - 3f, caja.width, caja.height), texto, _estilo);
        }

        /// <summary>
        /// Prepara el cartel: la caja de madera del propio pack en vez de un rectángulo
        /// negro.
        /// </summary>
        /// <remarks>
        /// El negro plano funcionaba —se leía— pero no es del juego: cualquier otra cosa que
        /// el jugador ve en pantalla está hecha de madera y tinta. Un solo elemento con
        /// estética de menú de depuración basta para que toda la interfaz parezca provisional.
        /// </remarks>
        void PrepararEstilo()
        {
            if (_estilo != null) return;

            var sprite = tema != null ? tema.CajaChicaDe(faccion) : null;
            _fondo = sprite != null ? sprite.texture : null;

            _estilo = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 15,
            };

            _estilo.normal.textColor = colorTexto;
        }

        static string NombreDe(TipoRecurso recurso)
        {
            switch (recurso)
            {
                case TipoRecurso.Oro: return "Oro";
                case TipoRecurso.Madera: return "Madera";
                case TipoRecurso.Carne: return "Carne";
                default: return "Recurso";
            }
        }
    }
}
