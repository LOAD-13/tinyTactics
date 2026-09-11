using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Edificios
{
    /// <summary>
    /// Qué se puede construir y con qué dibujo, por bando.
    ///
    /// <b>Por qué existe.</b> El sprite de un edificio depende del color del bando y se
    /// resuelve con <c>AssetDatabase</c>, que solo vive en el editor. En partida no hay
    /// forma de cargar «la casa roja» por su ruta, así que los cinco colores se resuelven
    /// al generar la escena y se guardan aquí.
    ///
    /// Es el gemelo de <see cref="Unidades.CatalogoDePlantillas"/> y por la misma razón:
    /// una sola lista que todos consultan, en vez de que cada pawn y cada botón del panel
    /// lleve su propia copia de qué edificios hay.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Catálogo de edificios")]
    public class CatalogoDeEdificios : MonoBehaviour
    {
        [System.Serializable]
        public class Entrada
        {
            public DatosEdificio datos;
            public int faccion;
            public Sprite sprite;

            [Tooltip("Copia apagada lista para clonar, con su marcador de selección y su " +
                     "producción ya montados. Mismo motivo que las plantillas de unidad: en " +
                     "partida no hay AssetDatabase con la que resolver piezas del pack.")]
            public GameObject plantilla;
        }

        [SerializeField] List<Entrada> _entradas = new List<Entrada>();

        [Tooltip("Fichas en el orden en que salen en la rejilla de construcción.")]
        [SerializeField] List<DatosEdificio> _fichas = new List<DatosEdificio>();

        public static CatalogoDeEdificios Actual { get; private set; }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>La llama el generador de la escena, una vez por edificio y bando.</summary>
        public void Registrar(DatosEdificio datos, int faccion, Sprite sprite, GameObject plantilla)
        {
            if (datos == null) return;

            _entradas.Add(new Entrada
            {
                datos = datos,
                faccion = faccion,
                sprite = sprite,
                plantilla = plantilla,
            });

            if (!_fichas.Contains(datos)) _fichas.Add(datos);
        }

        /// <summary>Copia apagada de un edificio en el color de un bando, lista para clonar.</summary>
        public GameObject PlantillaDe(DatosEdificio datos, int faccion)
        {
            for (int i = 0; i < _entradas.Count; i++)
            {
                var e = _entradas[i];
                if (e != null && e.datos == datos && e.faccion == faccion) return e.plantilla;
            }

            return null;
        }

        /// <summary>Dibujo de un edificio en el color de un bando.</summary>
        public Sprite SpriteDe(DatosEdificio datos, int faccion)
        {
            for (int i = 0; i < _entradas.Count; i++)
            {
                var e = _entradas[i];
                if (e != null && e.datos == datos && e.faccion == faccion) return e.sprite;
            }

            return null;
        }

        public Sprite SpriteDe(TipoEdificio tipo, int faccion)
        {
            for (int i = 0; i < _entradas.Count; i++)
            {
                var e = _entradas[i];
                if (e != null && e.datos != null && e.datos.tipo == tipo && e.faccion == faccion)
                    return e.sprite;
            }

            return null;
        }

        public DatosEdificio Obtener(TipoEdificio tipo)
        {
            for (int i = 0; i < _fichas.Count; i++)
                if (_fichas[i] != null && _fichas[i].tipo == tipo) return _fichas[i];

            return null;
        }

        /// <summary>
        /// Lo que un pawn puede levantar, en el orden del catálogo.
        /// </summary>
        /// <remarks>
        /// La lista se compone una vez y se guarda: la rejilla del panel la pide en cada
        /// refresco y componerla cada vez generaría un array por fotograma para devolver
        /// siempre lo mismo.
        /// </remarks>
        public IReadOnlyList<DatosEdificio> Construibles
        {
            get
            {
                if (_construibles != null) return _construibles;

                var lista = new List<DatosEdificio>();
                for (int i = 0; i < _fichas.Count; i++)
                    if (_fichas[i] != null && _fichas[i].construible) lista.Add(_fichas[i]);

                _construibles = lista;
                return _construibles;
            }
        }

        List<DatosEdificio> _construibles;
    }
}
