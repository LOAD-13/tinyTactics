using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// Un árbol, una veta o una oveja: algo del mapa de lo que se puede sacar recursos.
    ///
    /// Los nodos <b>se agotan</b>, y esa es la mecánica que sostiene el arco de la partida.
    /// Sin agotamiento, un RTS son dos jugadores picando eternamente en su esquina sin
    /// motivo para encontrarse; con él, cuando tu bolsón se seca no te queda más remedio
    /// que salir a disputar el del medio. El conflicto lo fuerza la economía, no el guion.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Nodo de recurso")]
    public class NodoRecurso : MonoBehaviour
    {
        [Header("Qué da")]
        public TipoRecurso recurso = TipoRecurso.Madera;

        [Tooltip("Celda lógica que ocupa. Las ovejas no la usan: se mueven.")]
        public Vector2Int celda;

        [Tooltip("Radio que bloquea en la grilla. Cero si no estorba el paso.")]
        public float radioBloqueo;

        [Tooltip("Las ovejas se mueven; los árboles y las vetas, no.")]
        public bool seMueve;

        /// <summary>
        /// Dónde tiene que plantarse el pawn para trabajarlo.
        /// </summary>
        /// <remarks>
        /// No es la posición del objeto: los árboles se dibujan <b>1,6 tiles por encima</b>
        /// de la celda que ocupan, para que la copa tape lo que hay detrás y el tronco caiga
        /// donde el mapa dice que hay un árbol. Si el pawn se guiara por el dibujo, iría a
        /// picar al aire por encima de la copa. La oveja es la excepción: como pasta y
        /// cambia de sitio, su celda de origen deja de significar nada en cuanto se mueve.
        /// </remarks>
        public Vector3 PuntoDeTrabajo =>
            seMueve ? transform.position : new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f);

        [Header("Al agotarse")]
        [Tooltip("Sprites que quedan en su sitio, como los tocones. Vacío = desaparece.")]
        [SerializeField] Sprite[] _restos;

        int _restantes = 1;
        int _trabajando;

        public int Restantes => _restantes;
        public bool Agotado => _restantes <= 0;

        /// <summary>Cuántos pawns lo están trabajando ahora mismo.</summary>
        public int Trabajando => _trabajando;

        // -----------------------------------------------------------------
        // Registro
        // -----------------------------------------------------------------

        static readonly List<NodoRecurso> _todos = new List<NodoRecurso>();

        public static IReadOnlyList<NodoRecurso> Todos => _todos;

        void OnEnable() => _todos.Add(this);
        void OnDisable() => _todos.Remove(this);

        /// <summary>La llama el generador de la escena con lo que queda al agotarse.</summary>
        public void Configurar(Sprite[] restos) => _restos = restos;

        /// <summary>
        /// Las existencias se leen en <c>Start</c> y no en <c>Awake</c> a propósito: Unity
        /// garantiza que todos los <c>Awake</c> corren antes que cualquier <c>Start</c>, y
        /// eso es lo que asegura que la economía ya exista cuando el nodo pregunta.
        /// </summary>
        void Start()
        {
            var eco = Economia.Actual;
            _restantes = eco != null && eco.datos != null
                ? eco.datos.ExtraccionesDe(recurso)
                : 1;
        }

        // -----------------------------------------------------------------
        // Trabajo
        // -----------------------------------------------------------------

        /// <summary>Un pawn se pone a trabajarlo.</summary>
        public void Reservar()
        {
            _trabajando++;

            // La oveja deja de pastar mientras la están despiezando. Que siguiera dando
            // paseos con un pawn clavándole el cuchillo se leía como un fallo.
            var pastar = GetComponent<Unidades.PastarOveja>();
            if (pastar != null) pastar.enabled = false;
        }

        /// <summary>Un pawn lo deja, por la razón que sea.</summary>
        public void Soltar()
        {
            _trabajando = Mathf.Max(0, _trabajando - 1);

            if (_trabajando > 0 || Agotado) return;

            var pastar = GetComponent<Unidades.PastarOveja>();
            if (pastar != null) pastar.enabled = true;
        }

        /// <summary>
        /// Saca una carga. Devuelve false si ya no queda nada.
        /// </summary>
        public bool Extraer(out int cantidad)
        {
            cantidad = 0;
            if (Agotado) return false;

            var eco = Economia.Actual;
            cantidad = eco != null && eco.datos != null ? eco.datos.CargaDe(recurso) : 1;

            _restantes--;
            if (_restantes <= 0) Agotar();

            return true;
        }

        void Agotar()
        {
            _restantes = 0;

            // Lo primero es devolver el terreno: el pawn que lo estaba trabajando querrá
            // irse a otro sitio, y si la celda sigue bloqueada el bosque talado se queda
            // como un muro invisible que nadie puede cruzar y que no da ningún error.
            if (radioBloqueo > 0f)
            {
                var mundo = MundoJuego.Actual;
                if (mundo != null) mundo.LiberarRecurso(celda, radioBloqueo);
            }

            if (_restos != null && _restos.Length > 0)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = _restos[Random.Range(0, _restos.Length)];

                var animador = GetComponent<Unidades.AnimadorSprite>();
                if (animador != null) animador.enabled = false;

                return;
            }

            Destroy(gameObject);
        }

        // -----------------------------------------------------------------
        // Búsqueda
        // -----------------------------------------------------------------

        /// <summary>
        /// Nodo bajo un punto del mundo, para el clic derecho contextual.
        ///
        /// Recorrido lineal a propósito: ocurre una vez por clic, no por frame. La grilla
        /// espacial está para las consultas que sí pasan sesenta veces por segundo.
        /// </summary>
        public static NodoRecurso NodoEn(Vector3 punto, float radio)
        {
            NodoRecurso mejor = null;
            float mejorDistancia = radio * radio;

            for (int i = 0; i < _todos.Count; i++)
            {
                var nodo = _todos[i];
                if (nodo == null || nodo.Agotado) continue;

                float d = (nodo.transform.position - punto).sqrMagnitude;
                if (d > mejorDistancia) continue;

                mejorDistancia = d;
                mejor = nodo;
            }

            return mejor;
        }

        /// <summary>Nodo vivo del mismo tipo más cercano. Lo usa el pawn al agotar el suyo.</summary>
        public static NodoRecurso MasCercano(Vector3 punto, TipoRecurso tipo, float radio)
        {
            NodoRecurso mejor = null;
            float mejorDistancia = radio * radio;

            for (int i = 0; i < _todos.Count; i++)
            {
                var nodo = _todos[i];
                if (nodo == null || nodo.Agotado || nodo.recurso != tipo) continue;

                float d = (nodo.transform.position - punto).sqrMagnitude;
                if (d > mejorDistancia) continue;

                mejorDistancia = d;
                mejor = nodo;
            }

            return mejor;
        }
    }
}
