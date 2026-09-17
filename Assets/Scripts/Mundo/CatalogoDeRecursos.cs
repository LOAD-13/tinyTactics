using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>Qué se puede sembrar con el pincel del editor.</summary>
    public enum SemillaMapa { Oro, Arbol, Piedra, Arbusto, Oveja }

    /// <summary>
    /// Plantillas de nodo de recurso, para poder sembrar el mapa en caliente.
    /// </summary>
    /// <remarks>
    /// Es el mismo patrón que <c>CatalogoDePlantillas</c> para unidades y
    /// <c>CatalogoDeEdificios</c> para edificios, y existe por la misma razón: los sprites de
    /// un árbol se resuelven con <c>AssetDatabase</c> al construir la escena, y eso <b>no
    /// existe en una partida</b>. Un árbol creado de cero en ejecución saldría sin dibujo.
    ///
    /// El generador deja una plantilla apagada de cada cosa y el editor las clona. Es la
    /// tercera vez que hace falta el mismo apaño, y la tercera confirma que es el patrón
    /// correcto para este proyecto y no un parche.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Catálogo de recursos")]
    public class CatalogoDeRecursos : MonoBehaviour
    {
        [System.Serializable]
        public class Entrada
        {
            public SemillaMapa semilla;

            /// <summary>Variante del dibujo. Los árboles tienen cuatro; el resto, varias.</summary>
            public int variante;

            public GameObject plantilla;
        }

        [SerializeField] List<Entrada> _entradas = new List<Entrada>();

        public static CatalogoDeRecursos Actual { get; private set; }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>La llama el generador de la escena.</summary>
        public void Registrar(SemillaMapa semilla, int variante, GameObject plantilla)
        {
            if (plantilla == null) return;
            _entradas.Add(new Entrada { semilla = semilla, variante = variante, plantilla = plantilla });
        }

        public int Variantes(SemillaMapa semilla)
        {
            int total = 0;
            for (int i = 0; i < _entradas.Count; i++)
                if (_entradas[i] != null && _entradas[i].semilla == semilla) total++;

            return total;
        }

        public GameObject Obtener(SemillaMapa semilla, int variante)
        {
            int total = Variantes(semilla);
            if (total == 0) return null;

            // El índice se envuelve en vez de salirse: la rueda del ratón pasa de una
            // variante a otra sin tope, igual que las fachadas de las casas.
            int quiere = ((variante % total) + total) % total;
            int visto = 0;

            for (int i = 0; i < _entradas.Count; i++)
            {
                var e = _entradas[i];
                if (e == null || e.semilla != semilla) continue;

                if (visto == quiere) return e.plantilla;
                visto++;
            }

            return null;
        }

        /// <summary>
        /// Siembra una copia en una celda del mapa. Devuelve el nodo, o null.
        /// </summary>
        /// <remarks>
        /// El nodo reclama su celda en la grilla igual que lo hace el generador: un árbol
        /// puesto a mano tiene que estorbar exactamente lo mismo que uno sembrado al crear el
        /// mapa, o el editor produciría mapas que se comportan distinto a los generados.
        /// </remarks>
        public NodoRecurso Sembrar(SemillaMapa semilla, int variante, Vector2Int celda)
        {
            var plantilla = Obtener(semilla, variante);
            if (plantilla == null) return null;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return null;

            var copia = Instantiate(plantilla, mundo.Grilla.CeldaAMundo(celda),
                                    Quaternion.identity, plantilla.transform.parent);

            copia.name = $"{semilla}_{celda.x}_{celda.y}";
            copia.SetActive(true);

            var nodo = copia.GetComponent<NodoRecurso>();
            if (nodo == null) return null;

            nodo.celda = celda;

            var profundidad = copia.GetComponent<OrdenPorProfundidad>();
            if (profundidad != null) profundidad.alto = mundo.Grilla.Alto;

            mundo.OcuparRecurso(nodo);
            return nodo;
        }
    }
}
