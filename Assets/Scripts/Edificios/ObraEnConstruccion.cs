using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Edificios
{
    /// <summary>
    /// Un edificio a medio levantar: se ve el dibujo final translúcido y se va opacando a
    /// martillazos.
    ///
    /// <b>El progreso se cuenta en martillazos, no en segundos</b> (ADR-13). No es un
    /// capricho de estilo: con un cronómetro, dos pawns en la misma obra no la acelerarían
    /// —o habría que escribir una regla aparte para que lo hicieran— y parar la obra a mitad
    /// para reanudarla seguiría descontando tiempo. Contando golpes, que dos pawns tarden la
    /// mitad sale gratis y un martillazo a medias no cuenta. Es exactamente el mismo
    /// razonamiento que cerró el exploit de la tala en la semana 05.
    ///
    /// El edificio existe desde el primer momento —ocupa terreno y se puede seleccionar—
    /// pero no está <see cref="Edificio.Operativo"/>: no recibe recursos, no fabrica y no
    /// suma población. Eso es lo que hace que construir cueste algo más que el oro.
    /// </summary>
    [RequireComponent(typeof(Edificio))]
    [AddComponentMenu("Tiny Tactics/Obra en construcción")]
    public class ObraEnConstruccion : MonoBehaviour
    {
        [Tooltip("Opacidad de la silueta al plantar la obra. No cero: un edificio invisible " +
                 "ocupando terreno se lee como un fallo del juego, no como una obra.")]
        [Range(0.1f, 0.8f)] public float opacidadInicial = 0.35f;

        [Min(1)] public int golpes = 24;

        int _dados;
        int _obreros;

        Edificio _edificio;
        SpriteRenderer _sprite;
        Interfaz.BarraDeVida _barra;

        public float Progreso => golpes <= 0 ? 1f : Mathf.Clamp01((float)_dados / golpes);

        /// <summary>Cuántos pawns están martillando ahora mismo. Lo lee el panel.</summary>
        public int Obreros => _obreros;

        public bool Terminada => _dados >= golpes;

        void Awake()
        {
            _edificio = GetComponent<Edificio>();
            _sprite = GetComponent<SpriteRenderer>();

            _edificio.MarcarEnObras();
            Repintar();
        }

        /// <summary>La llama el colocador con la ficha del catálogo.</summary>
        public void Configurar(DatosEdificio datos, Interfaz.BarraDeVida barra)
        {
            if (datos != null) golpes = Mathf.Max(1, datos.golpesDeObra);

            _barra = barra;

            // La barra pinta lo que le den. Al terminar la obra este componente desaparece,
            // así que la lambda comprueba que sigue vivo antes de leerse.
            if (_barra != null) _barra.fuente = () => this != null ? Progreso : 1f;

            Repintar();
        }

        // -----------------------------------------------------------------

        /// <summary>Un pawn se pone a martillar.</summary>
        public void Fichar() => _obreros++;

        /// <summary>Un pawn lo deja, por la razón que sea.</summary>
        public void Despedirse() => _obreros = Mathf.Max(0, _obreros - 1);

        /// <summary>
        /// Un martillazo. Devuelve true si con este la obra queda terminada.
        /// </summary>
        public bool Martillazo()
        {
            if (Terminada) return true;

            _dados++;
            Repintar();

            if (!Terminada) return false;

            Inaugurar();
            return true;
        }

        void Repintar()
        {
            if (_sprite == null) return;

            var c = _sprite.color;
            c.a = Mathf.Lerp(opacidadInicial, 1f, Progreso);
            _sprite.color = c;
        }

        /// <summary>
        /// La obra acaba y el edificio entra en servicio.
        /// </summary>
        /// <remarks>
        /// Este componente se destruye a sí mismo en vez de quedarse apagado. Así nadie
        /// puede preguntarle a un edificio terminado «¿cuánto te queda?» y recibir una
        /// respuesta con sentido: o hay obra, o no la hay.
        /// </remarks>
        void Inaugurar()
        {
            if (_sprite != null)
            {
                var c = _sprite.color;
                c.a = 1f;
                _sprite.color = c;
            }

            if (_barra != null)
            {
                _barra.fuente = null;
                Destroy(_barra.gameObject);
            }

            _edificio.Inaugurar();
            Destroy(this);
        }

        /// <summary>
        /// Planta una obra nueva sobre unas celdas. Devuelve null si no se pudo.
        /// </summary>
        /// <remarks>
        /// Clona la plantilla del catálogo en vez de montar el objeto aquí, por lo mismo que
        /// las unidades: el sprite del bando, el marcador de selección y la barra se
        /// resuelven con <c>AssetDatabase</c> al generar la escena, y eso no existe en
        /// partida. Un edificio construido en caliente saldría sin un solo dibujo.
        /// </remarks>
        public static ObraEnConstruccion Plantar(DatosEdificio datos, int faccion, RectInt celdas)
        {
            var catalogo = CatalogoDeEdificios.Actual;
            var plantilla = catalogo != null ? catalogo.PlantillaDe(datos, faccion) : null;

            if (plantilla == null)
            {
                Debug.LogWarning(
                    $"[Tiny Tactics] Sin plantilla de {datos?.tipo} para el bando {faccion}. " +
                    "Regenera la escena.");
                return null;
            }

            // Cuelga del grupo del bando, no de la carpeta de plantillas: un edificio de
            // verdad dentro de «Plantillas_0» confunde a quien abra la jerarquía buscando
            // por qué hay dos casas.
            var carpeta = plantilla.transform.parent;
            var padre = carpeta != null ? carpeta.parent : null;

            var copia = Instantiate(plantilla, padre);
            copia.name = $"{datos.tipo}_{celdas.x}_{celdas.y}";

            var edificio = copia.GetComponent<Edificio>();
            if (edificio != null) edificio.Colocar(datos, faccion, celdas);

            // La barra viene apagada en la plantilla: un edificio terminado no lleva barra.
            var barra = copia.GetComponentInChildren<Interfaz.BarraDeVida>(true);
            if (barra != null) barra.gameObject.SetActive(true);

            // El componente se añade ANTES de encender el objeto para que su Awake marque
            // «en obras» antes de que nadie pueda preguntar si el edificio ya sirve.
            var obra = copia.AddComponent<ObraEnConstruccion>();
            obra.Configurar(datos, barra);

            copia.SetActive(true);
            return obra;
        }

        /// <summary>Obra propia más cercana a un punto, para mandar a un pawn a echar una mano.</summary>
        public static ObraEnConstruccion MasCercana(Vector3 punto, int faccion, float radio)
        {
            ObraEnConstruccion mejor = null;
            float mejorDistancia = radio * radio;

            var edificios = Edificio.Todos;
            for (int i = 0; i < edificios.Count; i++)
            {
                var e = edificios[i];
                if (e == null || e.faccion != faccion || e.Operativo) continue;

                var obra = e.GetComponent<ObraEnConstruccion>();
                if (obra == null) continue;

                float d = ((Vector2)(e.transform.position - punto)).sqrMagnitude;
                if (d > mejorDistancia) continue;

                mejorDistancia = d;
                mejor = obra;
            }

            return mejor;
        }
    }
}
