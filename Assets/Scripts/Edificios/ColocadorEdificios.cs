using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Edificios
{
    /// <summary>
    /// El modo de colocación: la silueta del edificio sigue al ratón y dice, antes de
    /// gastar nada, si ahí se puede o no.
    ///
    /// <b>Por qué la validación se ve y no se explica.</b> Un mensaje de «no se puede
    /// construir aquí» después del clic obliga al jugador a adivinar qué parte del terreno
    /// molestaba. La mancha verde o roja bajo la silueta responde esa pregunta mientras
    /// mueve el ratón, que es cuando le sirve.
    ///
    /// Este componente <b>no construye nada</b>: dibuja y valida. Plantar la obra es una
    /// orden, y la emite el selector por el mismo camino que mover o atacar (ADR-01).
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Colocador de edificios")]
    public class ColocadorEdificios : MonoBehaviour
    {
        [Header("Colores de la mancha del suelo")]
        public Color colorValido = new Color(0.35f, 0.95f, 0.45f, 0.30f);
        public Color colorInvalido = new Color(0.95f, 0.30f, 0.25f, 0.35f);

        [Tooltip("Opacidad de la silueta del edificio mientras se coloca.")]
        [Range(0.2f, 1f)] public float opacidadSilueta = 0.65f;

        public static ColocadorEdificios Actual { get; private set; }

        DatosEdificio _ficha;
        int _faccion;
        int _variante;

        SpriteRenderer _silueta;
        SpriteRenderer _mancha;
        Texture2D _blanco;

        RectInt _celdas;
        bool _valido;

        /// <summary>True mientras el jugador está eligiendo dónde poner el edificio.</summary>
        public bool Activo => _ficha != null;

        /// <summary>Qué se está colocando, para que el panel lo cuente.</summary>
        public DatosEdificio Ficha => _ficha;

        /// <summary>Celdas bajo el cursor ahora mismo, y si valen.</summary>
        public RectInt Celdas => _celdas;
        public bool Valido => _valido;

        /// <summary>Fachada elegida con la rueda.</summary>
        public int VarianteElegida => _variante;

        /// <summary>True si el edificio tiene más de una fachada entre las que elegir.</summary>
        public bool TieneFachadas => _ficha != null && _ficha.Fachadas > 1;

        void Awake()
        {
            Actual = this;
            Crear();
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
            if (_blanco != null) Destroy(_blanco);
        }

        // -----------------------------------------------------------------

        /// <summary>Entra en modo colocación. Devuelve false si no hay dibujo con el que hacerlo.</summary>
        public bool Empezar(DatosEdificio ficha, int faccion)
        {
            if (ficha == null) return false;

            var catalogo = CatalogoDeEdificios.Actual;
            var dibujo = catalogo != null ? catalogo.SpriteDe(ficha, faccion) : null;

            if (dibujo == null) return false;

            _ficha = ficha;
            _faccion = faccion;
            _variante = 0;

            _silueta.sprite = dibujo;
            _silueta.color = new Color(1f, 1f, 1f, opacidadSilueta);

            Mostrar(true);
            return true;
        }

        /// <summary>
        /// Pasa a la fachada siguiente o a la anterior. La mueve la rueda del ratón.
        /// </summary>
        /// <remarks>
        /// Cambiar de fachada cambia <b>solo el dibujo</b>: las tres casas cuestan lo mismo,
        /// ocupan lo mismo y dan lo mismo. Es una decisión estética y por eso vale un gesto
        /// tan barato como girar la rueda, sin confirmar nada.
        /// </remarks>
        public void Girar(int pasos)
        {
            if (!Activo || pasos == 0 || _ficha.Fachadas <= 1) return;

            _variante = _ficha.Ajustar(_variante + pasos);

            var catalogo = CatalogoDeEdificios.Actual;
            var dibujo = catalogo != null ? catalogo.SpriteDe(_ficha, _faccion, _variante) : null;

            if (dibujo != null) _silueta.sprite = dibujo;
        }

        public void Cancelar()
        {
            _ficha = null;
            Mostrar(false);
        }

        /// <summary>
        /// Recoloca la silueta bajo el puntero y revalida. La llama el selector cada frame.
        /// </summary>
        public void Apuntar(Vector3 mundo)
        {
            if (!Activo) return;

            var grilla = MundoJuego.Actual != null ? MundoJuego.Actual.Grilla : null;
            if (grilla == null) return;

            _celdas = _ficha.CeldasDesde(grilla.MundoACelda(mundo));
            _valido = Cabe(grilla, _celdas);

            // La posición sale del MISMO cálculo que usará el edificio de verdad. Si la
            // silueta se colocara «donde está el ratón» y el edificio «donde dicen las
            // celdas», los dos coincidirían casi siempre y discreparían medio tile justo en
            // los bordes, que es donde el jugador mira.
            transform.position = Edificio.PosicionPara(_ficha, _celdas, _variante);

            // La mancha marca el suelo, que no está en el centro del dibujo: el
            // desplazamiento entre los dos es justo el que el edificio usará al plantarse.
            _mancha.transform.localPosition = _ficha.DesplazamientoBase(_variante);
            _mancha.transform.localScale = new Vector3(_celdas.width, _celdas.height, 1f);
            _mancha.color = _valido ? colorValido : colorInvalido;

            // Por delante de todo lo del mundo: una silueta tapada por un árbol no informa.
            int orden = OrdenPorProfundidad.Calcular(AltoMapa, _celdas.y) + 500;
            _silueta.sortingOrder = orden + 1;
            _mancha.sortingOrder = orden;
        }

        static int AltoMapa
        {
            get
            {
                var mundo = MundoJuego.Actual;
                return mundo != null && mundo.Mapa != null ? mundo.Mapa.Alto : 128;
            }
        }

        /// <summary>
        /// ¿Se puede levantar el edificio en estas celdas?
        /// </summary>
        /// <remarks>
        /// Tres reglas, y las tres existen por un motivo distinto. <b>Dentro del mapa</b>,
        /// porque el borde no es terreno. <b>Todo libre</b>, porque construir encima de un
        /// bosque o de otro edificio los borraría sin avisar. <b>Todo al mismo nivel</b>,
        /// porque un edificio a caballo entre el llano y una meseta se dibuja flotando sobre
        /// el acantilado y, peor, puede partir en dos la única rampa de la zona.
        /// </remarks>
        public static bool Cabe(GrillaMapa grilla, RectInt celdas)
        {
            if (grilla == null) return false;

            if (!grilla.EnRango(celdas.xMin, celdas.yMin)) return false;
            if (!grilla.EnRango(celdas.xMax - 1, celdas.yMax - 1)) return false;

            // Las piedras y los arbustos no hacen falta comprobarlos aparte: tapan su propia
            // celda como cualquier árbol, así que CajaLibre ya los ve. Una sola regla.
            return grilla.CajaLibre(celdas) && grilla.CajaAlMismoNivel(celdas);
        }

        /// <summary>
        /// Motivo por el que no se puede construir, para el listón de avisos. Null si sí.
        /// </summary>
        public string Impedimento()
        {
            if (!Activo) return null;

            var grilla = MundoJuego.Actual != null ? MundoJuego.Actual.Grilla : null;

            if (!Cabe(grilla, _celdas)) return "Terreno ocupado o irregular";

            // Solo al confirmar: es un barrido caro de repetir cada fotograma, y además la
            // silueta ya dice que el terreno vale. Lo que falla aquí no es el sitio, es la
            // consecuencia de taparlo.
            if (grilla.CerrariaElPaso(_celdas)) return "Dejaría un hueco sin salida";

            var eco = Economia.Actual;
            if (eco != null && !eco.PuedePagar(_faccion, _ficha.oro, _ficha.madera))
                return $"Faltan recursos · {_ficha.CosteVisible()}";

            return null;
        }

        // -----------------------------------------------------------------

        void Mostrar(bool visible)
        {
            if (_silueta != null) _silueta.enabled = visible;
            if (_mancha != null) _mancha.enabled = visible;
        }

        /// <summary>
        /// Monta la silueta y la mancha del suelo.
        /// </summary>
        /// <remarks>
        /// La mancha usa una textura blanca de un píxel generada aquí mismo y no un sprite
        /// del pack. Es un rectángulo de color plano que hay que poder estirar a cualquier
        /// tamaño: cualquier dibujo con detalle se vería deformado al escalarlo de dos por
        /// dos a cinco por tres.
        /// </remarks>
        void Crear()
        {
            var hijoSilueta = new GameObject("Silueta");
            hijoSilueta.transform.SetParent(transform, false);
            _silueta = hijoSilueta.AddComponent<SpriteRenderer>();

            _blanco = new Texture2D(1, 1);
            _blanco.SetPixel(0, 0, Color.white);
            _blanco.Apply();

            var sprite = Sprite.Create(_blanco, new Rect(0f, 0f, 1f, 1f),
                                       new Vector2(0.5f, 0.5f), 1f);

            var hijoMancha = new GameObject("Planta");
            hijoMancha.transform.SetParent(transform, false);
            _mancha = hijoMancha.AddComponent<SpriteRenderer>();
            _mancha.sprite = sprite;

            Mostrar(false);
        }
    }
}
