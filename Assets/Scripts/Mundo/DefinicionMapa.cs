using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Definición de un mapa. Es la <b>única fuente de verdad</b>: de acá salen
    /// tanto el tilemap visual como la grilla lógica de pathfinding.
    ///
    /// Tenerlos separados es el error clásico en un RTS: tarde o temprano se
    /// desincronizan y acabas con unidades caminando sobre el agua.
    /// </summary>
    [CreateAssetMenu(fileName = "Mapa", menuName = "Tiny Tactics/Definición de mapa")]
    public class DefinicionMapa : ScriptableObject
    {
        [Header("Identidad")]
        public string nombreMapa = "Tres Coronas";

        [TextArea(2, 4)]
        public string descripcion =
            "Tres lóbulos separados por brazos de mar, unidos únicamente por la meseta " +
            "central. Para atacar a alguien hay que pasar por el medio, y el medio guarda " +
            "la veta más rica del mapa.";

        [Tooltip("Bandos que soporta. Define la simetría rotacional del mapa.")]
        [Range(2, 5)] public int bandos = 3;

        [Header("Dimensiones (en tiles de 64 px)")]
        [Range(48, 320)] public int ancho = 224;
        [Range(48, 320)] public int alto = 224;

        [Header("Generación del terreno")]
        [Tooltip("Cambiar la semilla produce un mapa distinto con los mismos parámetros.")]
        public int semilla = 20260816;

        [Tooltip("Proporción del mapa que será tierra firme.")]
        [Range(0.2f, 0.95f)] public float cobertura = 0.86f;

        [Tooltip("Menor = costa más suave. Mayor = costa más recortada.")]
        [Range(0.02f, 0.25f)] public float escalaRuido = 0.045f;

        [Tooltip("Pasadas de suavizado. Más pasadas = costa más redondeada.")]
        [Range(0, 8)] public int pasosSuavizado = 2;

        [Tooltip("Qué tan rápido cae el terreno hacia los bordes.")]
        [Range(1f, 6f)] public float durezaBorde = 4f;

        [Tooltip("Cuánto manda la forma de isla frente al ruido.\n" +
                 "0 = ruido puro (archipiélago). 1 = círculo perfecto.")]
        [Range(0f, 1f)] public float pesoIsla = 0.34f;

        [Tooltip("Anillo de agua garantizado en el borde del mapa, en tiles.")]
        [Range(1, 12)] public int margenAgua = 5;

        [Header("Disposición de juego (radios relativos al centro)")]
        [Tooltip("Dónde arranca cada bando. 1 = borde del mapa.")]
        [Range(0.3f, 0.9f)] public float radioBases = 0.60f;

        [Tooltip("Expansión natural, dentro del lóbulo propio, camino al centro.")]
        [Range(0.2f, 0.7f)] public float radioExpansiones = 0.40f;

        [Tooltip("Tamaño de la meseta central disputada.")]
        [Range(0.05f, 0.3f)] public float radioCentro = 0.16f;

        [Tooltip("Crecientes de recursos alrededor de cada base. Los huecos entre ellas " +
                 "quedan reservados para las bajadas cuando se añadan los desniveles.")]
        [Range(2, 5)] public int bajadasPorBase = 3;

        [Tooltip("Tallar brazos de mar entre lóbulos. Es lo que obliga a pasar por el centro.")]
        public bool tallarBrazos = true;

        [Tooltip("Ancho angular del brazo de mar, como fracción del sector. " +
                 "Más ancho = lóbulos más separados y corredores más largos.")]
        [Range(0.1f, 0.6f)] public float anchoBrazoAgua = 0.33f;

        [Header("Limpieza")]
        [Tooltip("Conservar solo la masa de tierra más grande.")]
        public bool soloMasaPrincipal = true;

        [Tooltip("Rellenar lagos interiores más pequeños que este número de tiles.")]
        [Range(0, 40)] public int rellenarLagosHasta = 10;

        [Header("Decoración ambiental")]
        [Range(0f, 0.05f)] public float densidadRocas = 0.005f;
        [Range(0f, 0.05f)] public float densidadArbustos = 0.012f;

        [Tooltip("Arrecifes: pequeñas formaciones de roca en aguas abiertas.")]
        [Range(0, 40)] public int arrecifes = 14;

        [Tooltip("Tiles de agua libre que debe haber alrededor de un arrecife. " +
                 "Evita que queden pegados a la costa y parezcan caídos sobre la tierra.")]
        [Range(2, 12)] public int separacionArrecifes = 5;

        [Tooltip("Espirales de piedra repartidas por el mapa, una por sector.")]
        [Range(0, 8)] public int formacionesDeRoca = 3;

        [Range(0, 64)] public int cantidadNubes = 34;

        [Tooltip("Patito de goma escondido en el mar. Sí, viene en el pack.")]
        public bool patitoDeGoma = true;

        /// <summary>Extensión del mapa en unidades de mundo (1 tile = 1 unidad).</summary>
        public Vector2 TamanoEnMundo => new Vector2(ancho, alto);

        /// <summary>Centro del mapa en unidades de mundo.</summary>
        public Vector2 CentroEnMundo => new Vector2(ancho * 0.5f, alto * 0.5f);

        void OnValidate()
        {
            // Un margen que se come el mapa entero produciría cero tierra.
            margenAgua = Mathf.Min(margenAgua, Mathf.Min(ancho, alto) / 6);

            // Las expansiones tienen que quedar entre el centro y las bases.
            radioExpansiones = Mathf.Min(radioExpansiones, radioBases - 0.12f);
            radioCentro = Mathf.Min(radioCentro, radioExpansiones - 0.08f);
        }
    }
}
