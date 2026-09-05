using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Repuebla el mapa de ovejas cada cierto tiempo.
    ///
    /// <b>Por qué la carne se renueva y el oro no.</b> Una veta agotada empuja al jugador a
    /// salir a disputar la siguiente: ese es justo el conflicto que el agotamiento existe
    /// para provocar. Con la carne el efecto sería el contrario — el sustento es un gasto
    /// continuo, así que una despensa que se seca del todo no crea una decisión, crea una
    /// partida perdida sin remedio y sin nada que hacer al respecto.
    ///
    /// Por eso el rebaño se repone: la carne es <i>renta</i>, no <i>reserva</i>. Hay que
    /// atenderla siempre, pero nunca se acaba para siempre.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Criadero de ovejas")]
    public class CriaderoOvejas : MonoBehaviour
    {
        [Header("Ritmo")]
        [Tooltip("Segundos entre nacimientos.")]
        [Min(1f)] public float intervalo = 20f;

        [Tooltip("Tope de ovejas vivas. Sin él, dejar la partida quieta llena el mapa.")]
        [Min(1)] public int tope = 45;

        [Header("Dónde")]
        [Tooltip("Radio alrededor de cada punto de cría en el que puede nacer una.")]
        [Range(2f, 20f)] public float dispersion = 7f;

        [Tooltip("Copia inactiva de una oveja, con sus tiras ya resueltas a sprites.")]
        public GameObject plantilla;

        [Tooltip("De dónde cuelgan las ovejas nuevas.")]
        public Transform padre;

        [Tooltip("Focos de cría: las bases y unos cuantos puntos repartidos por el mapa.")]
        [SerializeField] List<Vector2Int> _puntos = new List<Vector2Int>();

        float _proxima;
        int _semilla = 20260906;

        /// <summary>La llama el generador de la escena.</summary>
        public void Configurar(GameObject plantillaOveja, Transform contenedor,
                               List<Vector2Int> puntos)
        {
            plantilla = plantillaOveja;
            padre = contenedor;
            _puntos = puntos;
        }

        void Start() => _proxima = Time.time + intervalo;

        void Update()
        {
            if (plantilla == null || _puntos == null || _puntos.Count == 0) return;
            if (Time.time < _proxima) return;

            _proxima = Time.time + intervalo;

            if (Contar() >= tope) return;

            Nacer();
        }

        /// <summary>
        /// Cuántas ovejas quedan vivas.
        ///
        /// Se cuentan sobre el registro de nodos, que es la única lista que ya existe y
        /// que además es la que dice la verdad: una oveja sacrificada se destruye y sale
        /// de ahí sola, sin que nadie tenga que acordarse de descontarla.
        /// </summary>
        int Contar()
        {
            var nodos = NodoRecurso.Todos;
            int n = 0;

            for (int i = 0; i < nodos.Count; i++)
            {
                var nodo = nodos[i];
                if (nodo != null && !nodo.Agotado && nodo.recurso == TipoRecurso.Carne) n++;
            }

            return n;
        }

        void Nacer()
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            var foco = _puntos[Siguiente(_puntos.Count)];

            // Cuatro intentos y se deja para la próxima vuelta. Insistir hasta encontrar
            // sitio podría recorrer medio mapa dentro de un solo fotograma, y no pasa nada
            // por que un nacimiento se salte su turno.
            for (int intento = 0; intento < 4; intento++)
            {
                float angulo = Siguiente(360) * Mathf.Deg2Rad;
                float radio = 1f + Siguiente(Mathf.CeilToInt(dispersion));

                var celda = new Vector2Int(
                    foco.x + Mathf.RoundToInt(Mathf.Cos(angulo) * radio),
                    foco.y + Mathf.RoundToInt(Mathf.Sin(angulo) * radio));

                if (!mundo.Grilla.Transitable(celda.x, celda.y)) continue;

                Soltar(celda);
                return;
            }
        }

        void Soltar(Vector2Int celda)
        {
            var copia = Instantiate(plantilla,
                                    new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f),
                                    Quaternion.identity,
                                    padre != null ? padre : transform);

            copia.name = $"Oveja_{celda.x}_{celda.y}";

            var sr = copia.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = OrdenPorProfundidad.Calcular(mundoAlto, celda.y);

            var nodo = copia.GetComponent<NodoRecurso>();
            if (nodo != null)
            {
                nodo.celda = celda;
                nodo.recurso = TipoRecurso.Carne;
                nodo.seMueve = true;
            }

            // Activar va al final: al encenderse corren los Awake, y para entonces la oveja
            // ya tiene que estar en su sitio y saber qué es.
            copia.SetActive(true);
        }

        [Tooltip("Alto del mapa. Solo se usa para el orden de dibujo por Y.")]
        public int mundoAlto = 128;

        // Generador propio, barato y determinista: la misma partida repuebla igual.
        int Siguiente(int tope)
        {
            _semilla = _semilla * 1103515245 + 12345;
            return Mathf.Abs(_semilla / 65536) % Mathf.Max(1, tope);
        }
    }
}
