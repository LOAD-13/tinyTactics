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

        [Header("Aspecto")]
        [Tooltip("Multiplicador de color. Por encima de 1 aclara sin desteñir el dibujo.")]
        public Color tinte = new Color(1.35f, 1.32f, 1.15f, 1f);

        [Tooltip("Color del cartel de existencias.")]
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

        void OnDisable() => Soltar();

        void OnDestroy()
        {
            if (_fondo != null) Destroy(_fondo);
        }

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

            string texto = $"{NombreDe(_actual.recurso)}  ·  {_actual.Restantes}";

            var medida = _estilo.CalcSize(new GUIContent(texto));
            float ancho = medida.x + 16f;
            float alto = medida.y + 8f;

            // OnGUI mide desde arriba; el mundo, desde abajo.
            var caja = new Rect(pantalla.x - ancho * 0.5f,
                                Screen.height - pantalla.y - alturaCartel - alto,
                                ancho, alto);

            GUI.color = new Color(0.05f, 0.04f, 0.03f, 0.72f);
            GUI.DrawTexture(caja, _fondo);

            GUI.color = Color.white;
            GUI.Label(caja, texto, _estilo);
        }

        void PrepararEstilo()
        {
            if (_estilo != null) return;

            _fondo = new Texture2D(1, 1);
            _fondo.SetPixel(0, 0, Color.white);
            _fondo.Apply();

            _estilo = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
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
