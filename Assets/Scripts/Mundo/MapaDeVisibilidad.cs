using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// En qué estado de conocimiento está una celda para un bando.
    /// </summary>
    /// <remarks>
    /// El orden de los valores <b>no es decorativo</b>: al iluminar se usa
    /// <c>if (loQueHay &lt; loNuevo)</c>, así que dos unidades cuyos radios se solapan no
    /// pueden pisarse el resultado. Sin ese orden, la penumbra del borde de un guerrero
    /// apagaría la vista plena de otro que estuviera al lado.
    /// </remarks>
    public enum EstadoVisible : byte
    {
        /// <summary>Nunca se ha visto. Se dibuja tapado por el manto de nubes.</summary>
        Oculto = 0,

        /// <summary>Se vio y ya no hay nadie mirando. Se ve el terreno, no lo que se mueve.</summary>
        Explorado = 1,

        /// <summary>Anillo exterior de un radio de visión. Solo existe para el dibujo.</summary>
        Penumbra = 2,

        /// <summary>Hay alguien de este bando mirando ahora mismo.</summary>
        Visible = 3,
    }

    /// <summary>
    /// Lo que un bando sabe del mapa: una celda, un byte.
    /// </summary>
    /// <remarks>
    /// <b>Por qué un byte por celda y no un objeto por celda.</b> El mapa por defecto mide
    /// 224×224, que son 50 176 celdas. Un <c>GameObject</c> con su <c>SpriteRenderer</c> por
    /// celda son cincuenta mil objetos que Unity tiene que recorrer, ordenar y dibujar cada
    /// fotograma para tapar un mapa que casi nunca cambia. Un <c>byte[]</c> de 50 KB se
    /// recorre entero en una fracción de milisegundo y se sube a una textura de una vez.
    ///
    /// Es una clase normal y no un <c>MonoBehaviour</c> a propósito: no necesita fotogramas,
    /// ni escena, ni inspector. Quien la conduce es <see cref="NieblaDeGuerra"/>.
    /// </remarks>
    public class MapaDeVisibilidad
    {
        public readonly int Ancho;
        public readonly int Alto;

        readonly byte[] _estado;

        /// <summary>Cuántas celdas se han llegado a explorar. Lo usa el minimapa y las cifras.</summary>
        public int Exploradas { get; private set; }

        public MapaDeVisibilidad(int ancho, int alto)
        {
            Ancho = Mathf.Max(1, ancho);
            Alto = Mathf.Max(1, alto);
            _estado = new byte[Ancho * Alto];
        }

        public bool EnRango(int x, int y) => x >= 0 && y >= 0 && x < Ancho && y < Alto;

        public EstadoVisible En(int x, int y) =>
            EnRango(x, y) ? (EstadoVisible)_estado[x + y * Ancho] : EstadoVisible.Oculto;

        /// <summary>¿Hay alguien de este bando mirando esta celda ahora?</summary>
        /// <remarks>
        /// La penumbra <b>no cuenta como ver</b>. Es un tono intermedio para que el borde de
        /// la niebla no sea un escalón seco, y dejar que valga como vista significaría que el
        /// alcance efectivo de la visión es un tile mayor del que dicen los datos.
        /// </remarks>
        public bool Visible(int x, int y) => En(x, y) == EstadoVisible.Visible;

        public bool Explorado(int x, int y) => En(x, y) != EstadoVisible.Oculto;

        /// <summary>
        /// Arranca una ronda: lo que estaba a la vista pasa a recordado.
        /// </summary>
        /// <remarks>
        /// Lo explorado <b>nunca</b> vuelve a oculto. Es la diferencia entre una niebla que
        /// informa y una que marea: si el mapa se cerrara en negro detrás de la patrulla, el
        /// jugador perdería la costa y los bosques que ya había pagado por descubrir.
        /// </remarks>
        public void EmpezarRonda()
        {
            for (int i = 0; i < _estado.Length; i++)
                if (_estado[i] > (byte)EstadoVisible.Explorado)
                    _estado[i] = (byte)EstadoVisible.Explorado;
        }

        /// <summary>
        /// Enciende un disco de visión alrededor de una celda.
        /// </summary>
        /// <param name="centro">Celda desde la que se mira.</param>
        /// <param name="radio">Radio de visión en tiles.</param>
        /// <remarks>
        /// El anillo de penumbra va <b>por fuera</b> del radio, no por dentro. Metido por
        /// dentro tendría que robarle un tile a la visión real de la unidad para que se
        /// notara, y entonces el número del <c>ScriptableObject</c> dejaría de ser el número
        /// de verdad. Por fuera, el dato se respeta y el escalón se suaviza igual.
        /// </remarks>
        public void Iluminar(Vector2Int centro, float radio)
        {
            if (radio <= 0f) return;

            float borde = radio + AnchoPenumbra;

            int desdeX = Mathf.Max(0, Mathf.FloorToInt(centro.x - borde));
            int hastaX = Mathf.Min(Ancho - 1, Mathf.CeilToInt(centro.x + borde));
            int desdeY = Mathf.Max(0, Mathf.FloorToInt(centro.y - borde));
            int hastaY = Mathf.Min(Alto - 1, Mathf.CeilToInt(centro.y + borde));

            float radioCuadrado = radio * radio;
            float bordeCuadrado = borde * borde;

            for (int y = desdeY; y <= hastaY; y++)
            {
                int fila = y * Ancho;
                int dy = y - centro.y;
                int dy2 = dy * dy;

                for (int x = desdeX; x <= hastaX; x++)
                {
                    int dx = x - centro.x;
                    float d2 = dx * dx + dy2;

                    if (d2 > bordeCuadrado) continue;

                    byte nuevo = d2 <= radioCuadrado
                        ? (byte)EstadoVisible.Visible
                        : (byte)EstadoVisible.Penumbra;

                    int i = x + fila;
                    if (_estado[i] >= nuevo) continue;

                    if (_estado[i] == (byte)EstadoVisible.Oculto) Exploradas++;
                    _estado[i] = nuevo;
                }
            }
        }

        /// <summary>Grosor del anillo de penumbra, en tiles.</summary>
        public const float AnchoPenumbra = 1.35f;

        /// <summary>
        /// Deja el mapa entero como explorado sin regalar visión.
        /// </summary>
        /// <remarks>
        /// Es la trampa clásica de «revelar el mapa» y no es lo mismo que apagar la niebla:
        /// se ve el terreno, los bosques y dónde está cada base, pero no los ejércitos, que
        /// siguen necesitando a alguien delante. Separadas se puede enseñar el mapa en una
        /// captura sin que la captura mienta sobre lo que el jugador sabría.
        /// </remarks>
        public void RevelarTerreno()
        {
            for (int i = 0; i < _estado.Length; i++)
                if (_estado[i] == (byte)EstadoVisible.Oculto)
                {
                    _estado[i] = (byte)EstadoVisible.Explorado;
                    Exploradas++;
                }
        }

        public void Olvidar()
        {
            System.Array.Clear(_estado, 0, _estado.Length);
            Exploradas = 0;
        }
    }
}
