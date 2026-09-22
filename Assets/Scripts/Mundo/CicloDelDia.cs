using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Las tres horas del mapa. El nombre marca <b>dónde entra</b> el ciclo, y la luz viaja
    /// desde ahí hacia la hora siguiente.
    /// </summary>
    /// <remarks>
    /// Tres y no veinticuatro: lo que hace falta es que se note el paso del tiempo, y con
    /// tres paradas y una transición continua entre ellas ya se nota. Cada parada añadida es
    /// un color más que cuadrar con la paleta del pack y una franja más de partida en la que
    /// el jugador ve algo distinto de lo que vio en la captura.
    /// </remarks>
    public enum HoraDelDia
    {
        /// <summary>Luz de mañana, dorada y baja. Va hacia el mediodía.</summary>
        Dia,

        /// <summary>Luz plena, sin tinte. Va hacia la noche: es la tarde que cae.</summary>
        Mediodia,

        /// <summary>Azul y oscuro. Va hacia el día: es el amanecer.</summary>
        Noche,
    }

    /// <summary>
    /// Ciclo de día y noche: oscurece el mapa por encima, sin tocar un solo sprite.
    /// </summary>
    /// <remarks>
    /// <b>Multiplica una lámina por encima del mapa; no tiñe las unidades</b> (ADR-19). La
    /// alternativa evidente —recorrer los <c>SpriteRenderer</c> y bajarles el color— rompe el
    /// ADR-11: la máquina de estados es la única dueña del color de su unidad, y ahí es donde
    /// vive el destello blanco del impacto y el desvanecido de la muerte. Con dos sistemas
    /// escribiendo el mismo campo, gana el último que escriba: o la noche apaga el destello,
    /// o el destello enciende la noche. Multiplicando por encima, la noche no necesita
    /// permiso de nadie y funciona igual sobre el terreno, las unidades, los edificios y las
    /// nubes.
    ///
    /// La lámina se dibuja por encima de la niebla a propósito: de noche, lo explorado
    /// también se oscurece, y eso es justo lo que se quiere contar.
    ///
    /// <b>La hora no cambia los radios de visión, y es una decisión.</b> Recortar la vista de
    /// noche es una mecánica de verdad y es tentadora, pero movería en silencio el balance
    /// que se cerró en la semana 08 —donde cada radio de visión se eligió mayor que el
    /// alcance de ataque de su unidad— y lo movería solo durante un tercio de cada partida.
    /// Queda anotado para la semana de balance, con la niebla ya asentada.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Ciclo del dia")]
    public class CicloDelDia : MonoBehaviour
    {
        public static CicloDelDia Actual { get; private set; }

        [Header("Ritmo")]
        [Tooltip("Segundos de luz de manana. Es la hora mas larga: es con la que se juega.")]
        [Min(4f)] public float duracionDia = 115f;

        [Tooltip("Segundos de mediodia. La luz plena, sin tinte.")]
        [Min(4f)] public float duracionMediodia = 95f;

        [Tooltip("Segundos de noche. La mas corta a proposito: sin antorchas ni unidades " +
                 "que vean de noche, la oscuridad no anade decision, solo incomodidad. " +
                 "Dura lo justo para que se note que ha pasado el tiempo.")]
        [Min(4f)] public float duracionNoche = 55f;

        [Tooltip("Que parte de cada hora se va en el cambio a la siguiente.\n\n" +
                 "Con 0,35, una hora pasa dos tercios de su tiempo con su luz y el ultimo " +
                 "tercio amaneciendo o anocheciendo. Sin esta meseta, la luz no se para " +
                 "nunca en ningun sitio: cambiar la duracion de la noche solo cambiaba lo " +
                 "rapido que se cruzaba, no cuanto tiempo estaba oscuro.")]
        [Range(0.05f, 1f)] public float parteDeCambio = 0.35f;

        [Tooltip("Congela el reloj en la hora actual. Lo usa el panel de partida libre para " +
                 "tomar capturas y para probar una hora concreta.")]
        public bool detenido;

        [Header("Luz")]
        [Tooltip("Tinte de la manana. Dorado: el sol bajo calienta los colores.")]
        public Color luzDia = new Color(1f, 0.90f, 0.74f, 1f);

        [Tooltip("Tinte del mediodia. Blanco puro, que en multiplicativo es no tocar nada.")]
        public Color luzMediodia = Color.white;

        [Tooltip("Tinte de la noche. Azul y oscuro, nunca negro: en negro no se distinguen " +
                 "los bandos, y el color del bando es el dato que no se puede perder.")]
        public Color luzNoche = new Color(0.36f, 0.42f, 0.68f, 1f);

        [Header("Dibujo")]
        [Tooltip("Material con el shader de tinte multiplicativo. Lo asigna el generador.")]
        public Material material;

        [Tooltip("Orden de dibujo. Por encima de la niebla y de sus nubes.")]
        public int ordenDeDibujo = 7000;

        /// <summary>Hora en la que está el ciclo. El nombre de la parada de la que viene.</summary>
        public HoraDelDia Hora { get; private set; } = HoraDelDia.Mediodia;

        /// <summary>Cuánto se ha recorrido de la hora actual, de 0 a 1.</summary>
        public float Avance { get; private set; }

        /// <summary>El tinte que se está aplicando ahora. Lo lee el minimapa.</summary>
        public Color Luz { get; private set; } = Color.white;

        SpriteRenderer _lamina;
        Material _propio;

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;
        }

        void Start()
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null)
            {
                enabled = false;
                return;
            }

            PrepararLamina(mundo.Grilla.Ancho, mundo.Grilla.Alto);
            Aplicar();
        }

        void Update()
        {
            if (!detenido)
            {
                // Tiempo escalado, al contrario que la niebla: la hora es parte de la
                // partida, así que con el juego en pausa el sol tampoco avanza, y a x4 el
                // día pasa cuatro veces más rápido. Es lo que se espera de un reloj de
                // partida.
                Avance += Time.deltaTime / DuracionDe(Hora);

                while (Avance >= 1f)
                {
                    Avance -= 1f;
                    Hora = Siguiente(Hora);
                }
            }

            Aplicar();
        }

        /// <summary>Salta a una hora concreta. Lo llama el panel de partida libre.</summary>
        public void Ir(HoraDelDia hora)
        {
            Hora = hora;
            Avance = 0f;
            Aplicar();
        }

        float DuracionDe(HoraDelDia hora) => hora switch
        {
            HoraDelDia.Dia => Mathf.Max(4f, duracionDia),
            HoraDelDia.Mediodia => Mathf.Max(4f, duracionMediodia),
            _ => Mathf.Max(4f, duracionNoche),
        };

        static HoraDelDia Siguiente(HoraDelDia hora) => hora switch
        {
            HoraDelDia.Dia => HoraDelDia.Mediodia,
            HoraDelDia.Mediodia => HoraDelDia.Noche,
            _ => HoraDelDia.Dia,
        };

        Color TinteDe(HoraDelDia hora) => hora switch
        {
            HoraDelDia.Dia => luzDia,
            HoraDelDia.Mediodia => luzMediodia,
            _ => luzNoche,
        };

        void Aplicar()
        {
            // Cada hora es una MESETA y luego un cambio. La meseta es lo que hace que
            // «la noche dura menos» signifique algo: sin ella, la luz esta siempre viajando
            // y acortar la noche solo acorta el trayecto, no el rato que esta oscuro.
            float mezcla = Mathf.InverseLerp(1f - Mathf.Clamp01(parteDeCambio), 1f, Avance);

            Luz = Color.Lerp(TinteDe(Hora), TinteDe(Siguiente(Hora)), Suave(mezcla));

            if (_lamina == null) return;

            _lamina.color = Luz;

            // Blanco puro no oscurece nada, así que la lámina se apaga del todo: un dibujado
            // que no cambia ni un píxel no tiene por qué pagarse.
            bool neutra = Luz.r > 0.995f && Luz.g > 0.995f && Luz.b > 0.995f;
            _lamina.enabled = !neutra;

            if (_propio != null) _propio.SetColor(IdColor, Luz);
        }

        static readonly int IdColor = Shader.PropertyToID("_Color");

        /// <summary>Entrada y salida suaves, para que la hora no cambie a ritmo de metrónomo.</summary>
        static float Suave(float t) => t * t * (3f - 2f * t);

        void PrepararLamina(int ancho, int alto)
        {
            var go = new GameObject("LaminaDeLuz");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(ancho * 0.5f, alto * 0.5f, 0f);

            var blanco = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "LuzDelDia",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            blanco.SetPixel(0, 0, Color.white);
            blanco.Apply(false);

            _lamina = go.AddComponent<SpriteRenderer>();
            _lamina.sprite = Sprite.Create(blanco, new Rect(0f, 0f, 1f, 1f),
                                           new Vector2(0.5f, 0.5f), 1f, 0,
                                           SpriteMeshType.FullRect);
            _lamina.sortingOrder = ordenDeDibujo;

            // Un 6% de holgura: la camara esta confinada al mapa, pero el confinamiento se
            // calcula con el tamano ortografico y un fotograma de desfase dejaria una tira
            // sin oscurecer en el borde. Sobra mapa, no falta.
            _lamina.transform.localScale = new Vector3(ancho * 1.06f, alto * 1.06f, 1f);

            if (material != null)
            {
                // Copia propia del material: la niebla usa el mismo shader y comparten asset.
                // Escribiendo en el compartido, el tinte de la noche se aplicaria tambien a
                // la mascara de la niebla y la niebla entera se volveria azul.
                _propio = new Material(material) { name = "Luz del dia" };
                _lamina.sharedMaterial = _propio;
            }
            else
            {
                Debug.LogWarning("[Tiny Tactics] El ciclo del dia no tiene material: la hora " +
                                 "tapara el mapa en vez de tenirlo.", this);
            }
        }

        void OnDestroy()
        {
            if (_propio != null) Destroy(_propio);
        }

        /// <summary>Nombre para la interfaz, con la primera en mayúscula.</summary>
        public static string Nombre(HoraDelDia hora) => hora switch
        {
            HoraDelDia.Dia => "Dia",
            HoraDelDia.Mediodia => "Mediodia",
            _ => "Noche",
        };
    }
}
