using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Entrada;
using TinyTactics.Unidades;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// La caja inferior con la ficha de lo seleccionado.
    ///
    /// El montaje es el del boceto: un listón del color del bando asomando por detrás, la
    /// caja de madera delante, la cara pegada a la izquierda sin marco, y a su derecha una
    /// caja azul que contiene el listón del nombre y, debajo, las cajas de vida y de stats.
    /// Con varias unidades seleccionadas esas dos se sustituyen por una sola con los
    /// retratos y el total.
    ///
    /// Se construye a sí mismo por código en <c>Awake</c>. Es la misma decisión que con el
    /// mapa (ADR-09): la escena se regenera entera desde el editor, así que un prefab de UI
    /// sería una segunda fuente de verdad que habría que mantener sincronizada a mano.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Panel de unidad")]
    public class PanelDeUnidad : MonoBehaviour
    {
        [Header("Tema")]
        public TemaInterfaz tema;

        [Header("Medidas (píxeles de la resolución de referencia, 1920×1080)")]
        [Tooltip("Ancho total incluyendo lo que asoma el listón por los lados.")]
        public Vector2 tamanoTotal = new Vector2(1080f, 230f);   // solo informativo: el ancho real se calcula

        [Tooltip("Caja de madera. Más estrecha que el total: la diferencia es el listón.")]
        public Vector2 tamanoCaja = new Vector2(740f, 230f);

        [Tooltip("Separación entre el borde inferior de la pantalla y el panel.")]
        public float margenInferior = 16f;

        public float ladoCara = 178f;

        [Tooltip("Ancho del marco de comandos, el de la derecha.")]
        public float anchoComandos = 290f;

        const float SeparacionMarcos = 8f;

        [Tooltip("Ajuste fino del nombre dentro del listón. La fuente gótica cae baja.")]
        public float subirNombre = 5f;

        [Header("Tipografía")]
        [Tooltip("Se prueban en orden y gana la primera instalada en el sistema. " +
                 "No se incluye ningún .ttf en el repo: son fuentes con licencia de terceros.")]
        public string[] fuentesTitulo = { "Old English Text MT", "Cambria", "Georgia" };

        public string[] fuentesTexto = { "Cambria", "Constantia", "Georgia" };

        [Tooltip("Color de todo el texto del panel. Blanco con contorno: la madera y las " +
                 "cajas del pack son oscuras, y el negro se perdía sobre ellas.")]
        public Color colorTexto = new Color(1f, 0.98f, 0.92f);

        [Tooltip("Contorno del texto. Es lo que lo despega del fondo sea cual sea el bando.")]
        public Color colorContorno = new Color(0.05f, 0.04f, 0.03f, 0.95f);

        [Header("Animación")]
        [Tooltip("El panel entra deslizándose desde abajo. Apagar para que aparezca de golpe.")]
        public bool animaciones = true;

        [Range(0.05f, 0.6f)] public float duracionEntrada = 0.16f;

        [Header("Rejilla de grupo")]
        [Tooltip("Retratos que caben en la caja. El resto solo cuenta para el total.")]
        public int retratosVisibles = 7;

        // Geometría de los sprites de barra ya recortados por el constructor de interfaz.
        const float BarraGrandeAncho = 112f;
        const float BarraGrandeAlto = 48f;
        const float BarraGrandeCavidadX = 11f;
        const float BarraGrandeCavidadAncho = 90f;

        const float BarraChicaAncho = 96f;
        const float BarraChicaAlto = 32f;
        const float BarraChicaCavidadX = 11f;
        const float BarraChicaCavidadAncho = 74f;

        RectTransform _raiz;
        CanvasGroup _opacidad;
        Image _liston;
        Image _cara;
        Image _caraProduccion;
        Image _cajaGrande;
        readonly List<Image> _cajasChicas = new List<Image>();

        GameObject _vistaIndividual;
        GameObject _vistaGrupo;

        Image _listonNombre;
        Text _nombre;

        Text _vida;
        Image _rellenoVida;
        Text _ataque;
        Text _velocidad;
        Text _oro;

        readonly List<Celda> _celdas = new List<Celda>();
        Text _total;

        Font _fuenteTitulo;
        Font _fuenteTexto;
        int _versionVista = -1;
        int _faccionPintada = -1;

        float _objetivo;
        float _mostrado;

        class Celda
        {
            public GameObject Raiz;
            public Image Cara;
            public Image Relleno;
        }

        /// <summary>Instancia activa, para que el resto de la entrada sepa esquivarla.</summary>
        public static PanelDeUnidad Actual { get; private set; }

        /// <summary>
        /// ¿El puntero está sobre la caja? Sin esta comprobación, un clic derecho sobre el
        /// panel mandaría a las unidades a la casilla que quedara detrás.
        /// </summary>
        public bool CapturaPuntero(Vector2 pantalla)
        {
            if (_raiz == null || !_raiz.gameObject.activeInHierarchy) return false;

            // Cámara nula porque el lienzo es Screen Space - Overlay.
            return RectTransformUtility.RectangleContainsScreenPoint(_raiz, pantalla, null);
        }

        // -----------------------------------------------------------------

        void Awake()
        {
            _fuenteTitulo = PrimeraInstalada(fuentesTitulo);
            _fuenteTexto = PrimeraInstalada(fuentesTexto);

            Actual = this;
            Construir();
            Mostrar(false, false);

            // Sin esto el panel se vería un frame entero, entero y opaco, antes de que la
            // primera transición lo esconda.
            AplicarTransicion();
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>
        /// Primera fuente de la lista que esté instalada en el sistema.
        ///
        /// Se cargan del sistema operativo y no se copia ningún <c>.ttf</c> al proyecto a
        /// propósito: las fuentes de Windows tienen licencia de Microsoft y el repositorio
        /// es público. Si ninguna está, se cae a la fuente incrustada en el motor, que no
        /// es medieval pero siempre existe.
        /// </summary>
        internal static Font PrimeraInstalada(string[] candidatas)
        {
            if (candidatas != null && candidatas.Length > 0)
            {
                var instaladas = new HashSet<string>(Font.GetOSInstalledFontNames());

                foreach (var nombre in candidatas)
                {
                    if (string.IsNullOrEmpty(nombre) || !instaladas.Contains(nombre)) continue;

                    var fuente = Font.CreateDynamicFontFromOSFont(nombre, 32);
                    if (fuente != null) return fuente;
                }
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        void LateUpdate()
        {
            CaducarAviso();

            var selector = SelectorDeUnidades.Actual;
            var seleccion = selector != null ? selector.Seleccionadas : null;

            // El edificio manda cuando no hay tropas elegidas. Son excluyentes, así que no
            // hay que decidir prioridades: o una cosa o la otra.
            var edificio = selector != null ? selector.EdificioSeleccionado : null;

            if (edificio != null && (seleccion == null || seleccion.Count == 0))
            {
                MostrarEdificio(selector, edificio);
                return;
            }

            if (seleccion == null || seleccion.Count == 0)
            {
                if (selector != null) _versionVista = selector.VersionSeleccion;
                Mostrar(false, false);
                AplicarTransicion();
                return;
            }

            bool individual = seleccion.Count == 1;
            Mostrar(individual, !individual);
            AplicarTransicion();

            // Retratos y nombres solo cambian cuando cambia la selección; la vida, cada
            // frame. Separarlo evita reasignar sprites y strings sesenta veces por segundo,
            // que es lo que genera basura en el heap.
            if (_versionVista != selector.VersionSeleccion)
            {
                _versionVista = selector.VersionSeleccion;
                RefrescarCabecera(seleccion);

                if (individual) RefrescarFicha(seleccion[0]);
                else RefrescarRejilla(seleccion);

                RefrescarAcciones(seleccion);
            }

            RefrescarEspera(seleccion);

            if (individual) RefrescarVida(seleccion[0]);
            else RefrescarVidasRejilla(seleccion);
        }

        void Mostrar(bool individual, bool grupo)
        {
            _objetivo = individual || grupo ? 1f : 0f;

            if (_vistaIndividual != null && _vistaIndividual.activeSelf != individual)
                _vistaIndividual.SetActive(individual);

            if (_vistaGrupo != null && _vistaGrupo.activeSelf != grupo)
                _vistaGrupo.SetActive(grupo);
        }

        /// <summary>
        /// Entrada y salida del panel: sube desde debajo del borde mientras se funde.
        ///
        /// Corre con <c>unscaledDeltaTime</c> para que la animación siga siendo la misma si
        /// algún día el juego se pausa o va a cámara lenta — la interfaz no forma parte de
        /// la simulación. Y el objeto solo se desactiva cuando la salida ha terminado; si se
        /// apagara al soltar la selección no habría nada que animar.
        /// </summary>
        void AplicarTransicion()
        {
            if (_raiz == null) return;

            if (animaciones && duracionEntrada > 0.001f)
                _mostrado = Mathf.MoveTowards(_mostrado, _objetivo,
                                              Time.unscaledDeltaTime / duracionEntrada);
            else
                _mostrado = _objetivo;

            bool activo = _mostrado > 0.001f;
            if (_raiz.gameObject.activeSelf != activo) _raiz.gameObject.SetActive(activo);
            if (!activo) return;

            float suave = _mostrado * _mostrado * (3f - 2f * _mostrado);

            _raiz.anchoredPosition = new Vector2(
                0f, Mathf.Lerp(margenInferior - tamanoTotal.y * 0.55f, margenInferior, suave));

            if (_opacidad != null) _opacidad.alpha = suave;
        }

        // -----------------------------------------------------------------
        // Refresco
        // -----------------------------------------------------------------

        /// <summary>Cara, listones y nombre: lo que es común a las dos vistas.</summary>
        void RefrescarCabecera(IReadOnlyList<Unidad> seleccion)
        {
            var primera = seleccion[0];
            if (primera == null || primera.datos == null || tema == null) return;

            if (_cara != null)
            {
                var cara = tema.RetratoDe(primera.datos.tipo, primera.faccion);
                _cara.sprite = cara;
                _cara.enabled = cara != null;
            }

            AplicarFaccion(primera.faccion);

            // Se vuelve del modo edificio: barra de vida roja otra vez y sin miniatura.
            if (_rellenoVida != null && tema.barraGrandeRelleno != null)
                _rellenoVida.sprite = tema.barraGrandeRelleno;

            if (_caraProduccion != null) _caraProduccion.enabled = false;

            if (_nombre != null) _nombre.text = NombreDe(seleccion);
        }

        /// <summary>
        /// Tiñe listones y marcos del color del bando.
        ///
        /// Solo se reasignan al cambiar de bando. Hoy siempre es el mismo, pero en cuanto
        /// haya observador o repetición el panel seguirá al jugador activo.
        /// </summary>
        void AplicarFaccion(int faccion)
        {
            if (tema == null || faccion == _faccionPintada) return;

            _faccionPintada = faccion;

            if (_liston != null)
            {
                _liston.sprite = tema.ListonPanelDe(faccion);
                _liston.enabled = _liston.sprite != null;
            }

            if (_listonNombre != null)
            {
                _listonNombre.sprite = tema.ListonNombreDe(faccion);
                _listonNombre.enabled = _listonNombre.sprite != null;
            }

            if (_listonAviso != null) _listonAviso.sprite = tema.ListonNombreDe(faccion);

            if (_cajaGrande != null)
            {
                _cajaGrande.sprite = tema.CajaDe(faccion);
                _cajaGrande.enabled = _cajaGrande.sprite != null;
            }

            var chica = tema.CajaChicaDe(faccion);
            for (int i = 0; i < _cajasChicas.Count; i++)
            {
                if (_cajasChicas[i] == null) continue;
                _cajasChicas[i].sprite = chica;
                _cajasChicas[i].enabled = chica != null;
            }
        }

        /// <summary>Un solo tipo se nombra; una mezcla no tiene nombre que valga.</summary>
        static string NombreDe(IReadOnlyList<Unidad> seleccion)
        {
            var primera = seleccion[0];
            if (primera == null || primera.datos == null) return "Selección";

            TipoUnidad tipo = primera.datos.tipo;

            for (int i = 1; i < seleccion.Count; i++)
            {
                var u = seleccion[i];
                if (u == null || u.datos == null || u.datos.tipo != tipo) return "Selección";
            }

            return primera.datos.nombreVisible;
        }

        // -----------------------------------------------------------------
        // Ficha de edificio
        // -----------------------------------------------------------------

        /// <summary>
        /// La ficha de un edificio reutiliza la vista individual entera.
        ///
        /// No hay una segunda vista porque la información se corresponde una a una: el
        /// retrato pasa a ser el del castillo y la barra grande, que en una unidad muestra
        /// la vida, muestra aquí lo que lleva hecho lo que está fabricando. Duplicar la
        /// vista para cambiar dos textos habría sido cuatrocientas líneas de calco.
        /// </summary>
        void MostrarEdificio(SelectorDeUnidades selector, Edificios.Edificio edificio)
        {
            Mostrar(true, false);
            AplicarTransicion();

            var produccion = edificio.GetComponent<Edificios.ProduccionEdificio>();

            if (_versionVista != selector.VersionSeleccion)
            {
                _versionVista = selector.VersionSeleccion;

                AplicarFaccion(edificio.faccion);

                if (_cara != null)
                {
                    _cara.sprite = edificio.retrato;
                    _cara.enabled = edificio.retrato != null;
                }

                if (_nombre != null) _nombre.text = edificio.nombreVisible;

                var datos = produccion != null ? produccion.datosUnidad : null;

                // Azul, no rojo. La barra roja del panel significa vida, y una fabricacion
                // a medias no es un edificio a medio matar.
                if (_rellenoVida != null && tema != null && tema.barraGrandeRellenoAzul != null)
                    _rellenoVida.sprite = tema.barraGrandeRellenoAzul;

                if (_caraProduccion != null)
                {
                    var retrato = datos != null && tema != null
                        ? tema.RetratoDe(datos.tipo, edificio.faccion)
                        : null;

                    _caraProduccion.sprite = retrato;
                    _caraProduccion.enabled = retrato != null;
                }

                if (_ataque != null) _ataque.text = "—";
                if (_velocidad != null) _velocidad.text = "—";
                if (_oro != null) _oro.text = datos != null ? datos.oro.ToString() : "—";

                RefrescarAccionesEdificio(produccion);
            }

            RefrescarProduccion(produccion);
        }

        void RefrescarProduccion(Edificios.ProduccionEdificio produccion)
        {
            float progreso = produccion != null ? produccion.Progreso : 0f;
            int cola = produccion != null ? produccion.EnCola : 0;

            if (_rellenoVida != null && !Mathf.Approximately(_rellenoVida.fillAmount, progreso))
                _rellenoVida.fillAmount = progreso;

            // La barra de fabricación va muda. El texto de dentro nació siendo el «60 / 60»
            // de la vida, y ahí encaja porque una cifra de vida es un dato que se consulta;
            // el progreso de un entrenamiento ya se lee entero en lo que ha avanzado la
            // barra, así que la frase solo añadía ruido sobre el relleno.
            if (_vida != null && _vida.enabled) _vida.enabled = false;
        }

        /// <summary>Un edificio solo enseña lo que sabe fabricar.</summary>
        void RefrescarAccionesEdificio(Edificios.ProduccionEdificio produccion)
        {
            bool produce = produccion != null && produccion.datosUnidad != null;
            var color = tema != null ? tema.BotonDe(_faccionPintada) : null;

            for (int i = 0; i < _botones.Count; i++)
            {
                var b = _botones[i];
                bool visible = b.Accion == Accion.EntrenarPawn && produce;

                if (b.Raiz.activeSelf != visible) b.Raiz.SetActive(visible);
                if (color != null && b.Fondo.sprite != color) b.Fondo.sprite = color;
            }
        }

        void RefrescarFicha(Unidad unidad)
        {
            if (unidad == null || unidad.datos == null) return;

            var datos = unidad.datos;

            if (_ataque != null) _ataque.text = datos.dano.ToString();
            if (_velocidad != null) _velocidad.text = datos.velocidad.ToString("0.0");
            if (_oro != null) _oro.text = datos.oro.ToString();
        }

        void RefrescarVida(Unidad unidad)
        {
            if (unidad == null || unidad.datos == null) return;

            // Se vuelve del modo edificio, donde la barra va sin texto.
            if (_vida != null && !_vida.enabled) _vida.enabled = true;

            int max = Mathf.Max(1, unidad.datos.vidaMaxima);
            float f = Mathf.Clamp01((float)unidad.Vida / max);

            if (_rellenoVida != null && !Mathf.Approximately(_rellenoVida.fillAmount, f))
                _rellenoVida.fillAmount = f;

            if (_vida != null)
            {
                string texto = unidad.Vida + " / " + max;
                if (_vida.text != texto) _vida.text = texto;
            }
        }

        void RefrescarRejilla(IReadOnlyList<Unidad> seleccion)
        {
            int visibles = Mathf.Min(seleccion.Count, _celdas.Count);

            for (int i = 0; i < _celdas.Count; i++)
            {
                var celda = _celdas[i];
                bool activa = i < visibles;

                if (celda.Raiz.activeSelf != activa) celda.Raiz.SetActive(activa);
                if (!activa) continue;

                var unidad = seleccion[i];
                var cara = unidad != null && unidad.datos != null && tema != null
                    ? tema.RetratoDe(unidad.datos.tipo, unidad.faccion)
                    : null;

                celda.Cara.sprite = cara;
                celda.Cara.enabled = cara != null;
            }

            // El total va siempre, quepan o no todos los retratos: es el dato que el jugador
            // necesita para saber con cuánta gente está a punto de hacer algo.
            if (_total != null) _total.text = "×" + seleccion.Count;
        }

        void RefrescarVidasRejilla(IReadOnlyList<Unidad> seleccion)
        {
            int visibles = Mathf.Min(seleccion.Count, _celdas.Count);

            for (int i = 0; i < visibles; i++)
            {
                var unidad = seleccion[i];
                if (unidad == null || unidad.datos == null) continue;

                float f = Mathf.Clamp01((float)unidad.Vida / Mathf.Max(1, unidad.datos.vidaMaxima));
                var relleno = _celdas[i].Relleno;

                if (relleno != null && !Mathf.Approximately(relleno.fillAmount, f))
                    relleno.fillAmount = f;
            }
        }

        // -----------------------------------------------------------------
        // Construcción
        // -----------------------------------------------------------------

        void Construir()
        {
            if (GetComponent<Canvas>() == null) return;

            _raiz = Nodo("Panel", (RectTransform)transform);

            // Pivote en la base y anclaje al borde inferior: así el panel se apoya en el
            // borde de la pantalla en vez de quedarse a caballo, que es lo que pasaba con
            // el pivote centrado — la mitad de la caja caía fuera del encuadre.
            _raiz.anchorMin = new Vector2(0.5f, 0f);
            _raiz.anchorMax = new Vector2(0.5f, 0f);
            _raiz.pivot = new Vector2(0.5f, 0f);
            // El listón asoma por detrás de los dos marcos juntos.
            _raiz.sizeDelta = new Vector2(
                tamanoCaja.x + SeparacionMarcos + anchoComandos + 90f, tamanoCaja.y);
            _raiz.anchoredPosition = new Vector2(0f, margenInferior);

            _opacidad = _raiz.gameObject.AddComponent<CanvasGroup>();

            // Los dos en true, y no es un descuido heredado: un CanvasGroup con
            // blocksRaycasts o interactable en false anula el raycast y la interacción de
            // TODOS sus hijos. Con los valores anteriores los botones de comandos habrían
            // quedado dibujados pero muertos, sin ningún error que lo delatara.
            _opacidad.blocksRaycasts = true;
            _opacidad.interactable = true;

            // Orden de creación = orden de dibujo. El listón primero para que asome por
            // detrás de la madera.
            _liston = Caja("Liston", _raiz, null, true).GetComponent<Image>();
            Colocar((RectTransform)_liston.transform, Vector2.zero,
                    new Vector2(_raiz.sizeDelta.x, tamanoCaja.y * 0.65f));

            // Dos marcos pegados pero independientes, como el menú de comandos de
            // Warcraft: la ficha de la unidad a la izquierda y los comandos en su propia
            // caja a la derecha. Meterlos en el mismo marco hacía que la rejilla pareciera
            // parte de la ficha, y no lo es: los comandos son del jugador, no de la unidad.
            float anchoContenido = tamanoCaja.x + SeparacionMarcos + anchoComandos;

            var caja = Caja("Caja", _raiz, tema != null ? tema.panelFondo : null, true);
            Colocar(caja, new Vector2(-anchoContenido * 0.5f + tamanoCaja.x * 0.5f, 0f),
                    tamanoCaja);

            var comandos = Caja("CajaComandos", _raiz,
                                tema != null ? tema.panelFondo : null, true);
            Colocar(comandos, new Vector2(anchoContenido * 0.5f - anchoComandos * 0.5f, 0f),
                    new Vector2(anchoComandos, tamanoCaja.y));

            float mitadCaja = tamanoCaja.x * 0.5f;
            const float MargenCaja = 36f;

            // Cara suelta, sin marco: pegada al borde izquierdo de la madera.
            var cara = Caja("Cara", caja, null, false);
            Colocar(cara, new Vector2(-mitadCaja + MargenCaja + ladoCara * 0.5f, 6f),
                    new Vector2(ladoCara, ladoCara));
            _cara = cara.GetComponent<Image>();
            _cara.preserveAspect = true;

            // Miniatura de lo que se esta fabricando, en la esquina de la cara del edificio.
            // Es donde Warcraft pone la cola de produccion: ahi se lee sin estorbar, y no
            // toca en absoluto la ficha de una unidad, que simplemente la deja apagada.
            float ladoMini = ladoCara * 0.44f;
            var miniatura = Caja("Miniatura", cara, null, false);
            Colocar(miniatura,
                    new Vector2(ladoCara * 0.5f - ladoMini * 0.5f,
                                -ladoCara * 0.5f + ladoMini * 0.5f),
                    new Vector2(ladoMini, ladoMini));

            _caraProduccion = miniatura.GetComponent<Image>();
            _caraProduccion.preserveAspect = true;
            _caraProduccion.enabled = false;

            float izquierdaAzul = -mitadCaja + MargenCaja + ladoCara + 12f;
            float derechaAzul = mitadCaja - MargenCaja;
            float anchoAzul = derechaAzul - izquierdaAzul;
            float altoAzul = tamanoCaja.y - 62f;

            var azul = Caja("CajaColor", caja, tema != null ? tema.CajaDe(0) : null, true);
            Colocar(azul, new Vector2((izquierdaAzul + derechaAzul) * 0.5f, -6f),
                    new Vector2(anchoAzul, altoAzul));
            _cajaGrande = azul.GetComponent<Image>();

            ConstruirIndividual(azul, anchoAzul, altoAzul);
            ConstruirGrupo(azul, anchoAzul, altoAzul);
            ConstruirAcciones(comandos, anchoComandos - MargenCaja * 2f, altoAzul);

            // El listón del nombre se crea el último: monta por encima del borde superior
            // de la caja azul, y así ninguna de las dos vistas lo tapa.
            ConstruirNombre(azul, anchoAzul, altoAzul);
        }

        void ConstruirNombre(RectTransform azul, float ancho, float alto)
        {
            var liston = Caja("ListonNombre", azul, null, true);
            Colocar(liston, new Vector2(0f, alto * 0.5f - 36f), new Vector2(ancho * 0.8f, 48f));
            _listonNombre = liston.GetComponent<Image>();

            _nombre = Etiqueta("Nombre", liston, 28, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            Estirar((RectTransform)_nombre.transform);

            var rtNombre = (RectTransform)_nombre.transform;
            rtNombre.offsetMin = new Vector2(0f, subirNombre);
            rtNombre.offsetMax = new Vector2(0f, subirNombre);
        }

        void ConstruirIndividual(RectTransform azul, float ancho, float alto)
        {
            _vistaIndividual = new GameObject("Individual", typeof(RectTransform));
            var raiz = (RectTransform)_vistaIndividual.transform;
            raiz.SetParent(azul, false);
            Estirar(raiz);

            float y = -alto * 0.5f + 55f;
            float altoFila = 82f;
            float util = ancho - 28f;
            float anchoVida = util * 0.46f;
            float anchoStats = util - anchoVida - 16f;
            float izquierda = -util * 0.5f;

            // --- Vida
            var cajaVida = CajaChica("CajaVida", raiz);
            Colocar(cajaVida, new Vector2(izquierda + anchoVida * 0.5f, y),
                    new Vector2(anchoVida, altoFila));

            // La barra se estira para llenar la caja a lo ancho y se aplasta a lo alto. Se
            // rompe la proporción del sprite a propósito: dejar que el alto salga del ancho
            // (112×48) la hacía desbordar la caja por arriba y por abajo.
            float anchoBarra = anchoVida - 26f;
            float altoBarra = altoFila - 22f;

            var barra = Caja("Barra", cajaVida, tema != null ? tema.barraGrandeMarco : null, false);
            Colocar(barra, Vector2.zero, new Vector2(anchoBarra, altoBarra));

            _rellenoVida = Relleno(barra, tema != null ? tema.barraGrandeRelleno : null,
                                   anchoBarra / BarraGrandeAncho,
                                   BarraGrandeCavidadX, BarraGrandeCavidadAncho, altoBarra);

            _vida = Etiqueta("TextoVida", barra, 18, TextAnchor.MiddleCenter, FontStyle.Bold);
            Estirar((RectTransform)_vida.transform);

            // --- Estadísticas
            var cajaStats = CajaChica("CajaStats", raiz);
            Colocar(cajaStats, new Vector2(izquierda + anchoVida + 16f + anchoStats * 0.5f, y),
                    new Vector2(anchoStats, altoFila));

            float paso = (anchoStats - 28f) / 3f;
            float x0 = -(anchoStats - 28f) * 0.5f + paso * 0.5f;

            _ataque = Estadistica("Ataque", cajaStats, tema != null ? tema.iconoAtaque : null,
                                  x0, paso);
            _velocidad = Estadistica("Velocidad", cajaStats, tema != null ? tema.iconoVelocidad : null,
                                     x0 + paso, paso);
            _oro = Estadistica("Coste", cajaStats, tema != null ? tema.iconoOro : null,
                               x0 + paso * 2f, paso);
        }

        void ConstruirGrupo(RectTransform azul, float ancho, float alto)
        {
            _vistaGrupo = new GameObject("Grupo", typeof(RectTransform));
            var raiz = (RectTransform)_vistaGrupo.transform;
            raiz.SetParent(azul, false);
            Estirar(raiz);

            float y = -alto * 0.5f + 55f;
            float altoFila = 82f;
            float anchoCaja = ancho - 28f;

            var caja = CajaChica("CajaUnidades", raiz);
            Colocar(caja, new Vector2(0f, y), new Vector2(anchoCaja, altoFila));

            // El total ocupa el hueco de la derecha; los retratos se reparten el resto.
            const float AnchoTotal = 52f;
            float util = anchoCaja - 24f - AnchoTotal;

            int n = Mathf.Max(1, retratosVisibles);
            float paso = util / n;
            float ladoRetrato = Mathf.Min(paso - 6f, 46f);
            float altoBarra = ladoRetrato * BarraChicaAlto / BarraChicaAncho;
            float x0 = -anchoCaja * 0.5f + 12f + paso * 0.5f;

            for (int i = 0; i < n; i++)
            {
                var celda = new Celda { Raiz = new GameObject($"Celda{i}", typeof(RectTransform)) };

                var rt = (RectTransform)celda.Raiz.transform;
                rt.SetParent(caja, false);
                Colocar(rt, new Vector2(x0 + i * paso, 0f),
                        new Vector2(ladoRetrato, ladoRetrato + altoBarra + 2f));

                var cara = Caja("Cara", rt, null, false);
                Colocar(cara, new Vector2(0f, altoBarra * 0.5f + 1f),
                        new Vector2(ladoRetrato, ladoRetrato));
                celda.Cara = cara.GetComponent<Image>();
                celda.Cara.preserveAspect = true;

                var barra = Caja("Barra", rt, tema != null ? tema.barraChicaMarco : null, false);
                Colocar(barra, new Vector2(0f, -ladoRetrato * 0.5f),
                        new Vector2(ladoRetrato, altoBarra));

                celda.Relleno = Relleno(barra, tema != null ? tema.barraChicaRelleno : null,
                                        ladoRetrato / BarraChicaAncho,
                                        BarraChicaCavidadX, BarraChicaCavidadAncho, altoBarra);

                _celdas.Add(celda);
            }

            _total = Etiqueta("Total", caja, 20, TextAnchor.MiddleCenter, FontStyle.Bold);
            Colocar((RectTransform)_total.transform,
                    new Vector2(anchoCaja * 0.5f - 12f - AnchoTotal * 0.5f, 0f),
                    new Vector2(AnchoTotal, 30f));
        }

        // -----------------------------------------------------------------
        // Panel de acciones
        // -----------------------------------------------------------------

        /// <summary>Qué hace un botón de la rejilla de comandos.</summary>
        public enum Accion { Atacar, AtacarAuto, Mover, Detener, Curar, Construir, EntrenarPawn }

        class Boton
        {
            public GameObject Raiz;
            public Image Fondo;
            public Image Icono;
            public Text Espera;
            public Accion Accion;
        }

        readonly List<Boton> _botones = new List<Boton>();

        void ConstruirAcciones(RectTransform caja, float ancho, float alto)
        {
            var marco = CajaChica("CajaAcciones", caja);
            Colocar(marco, new Vector2(0f, -6f), new Vector2(ancho, alto));

            // Aviso de lo que hace cada botón. Va sobre el marco de comandos, no dentro:
            // dentro taparía la propia rejilla justo cuando la estás mirando.
            var listonAviso = Caja("ListonAviso", caja, null, true);
            Colocar(listonAviso, new Vector2(0f, alto * 0.5f - 8f), new Vector2(ancho + 40f, 44f));
            _listonAviso = listonAviso.GetComponent<Image>();

            _aviso = Etiqueta("Aviso", listonAviso, 22, TextAnchor.MiddleCenter, FontStyle.Bold);
            Estirar((RectTransform)_aviso.transform);

            // Crema y con contorno, no negro como el resto del panel: el listón es de un
            // tono oscuro y el texto negro encima se perdía. El contorno lo despega del
            // fondo sin depender de que el color del bando sea claro u oscuro.
            _aviso.color = new Color(1f, 0.97f, 0.88f);

            var contorno = _aviso.gameObject.AddComponent<Outline>();
            contorno.effectColor = new Color(0.05f, 0.04f, 0.03f, 0.9f);
            contorno.effectDistance = new Vector2(1.6f, -1.6f);

            MostrarAviso(null);

            // Tres columnas por dos filas. Warcraft usa cuatro por tres; con seis basta
            // para lo que hay hoy y para construir, que es lo próximo.
            const int Columnas = 3;
            const float Lado = 62f;
            const float Paso = 70f;

            // Cada acción declara su hueco en la rejilla, en vez de deducirlo de su posición
            // en la lista. Es lo que permite que entrenar y atacar compartan casilla: nunca
            // se ven a la vez —una es de edificio y la otra de tropa— y así el comando
            // principal cae siempre en la misma esquina, se haya seleccionado lo que sea.
            var orden = new (Accion accion, int ranura)[]
            {
                (Accion.Atacar, 0), (Accion.AtacarAuto, 1), (Accion.Detener, 2),
                (Accion.Mover, 3), (Accion.Curar, 4), (Accion.Construir, 5),
                (Accion.EntrenarPawn, 0),
            };

            float x0 = -(Columnas - 1) * Paso * 0.5f;
            float y0 = Paso * 0.5f;

            for (int i = 0; i < orden.Length; i++)
            {
                var boton = new Boton { Accion = orden[i].accion };
                int ranura = orden[i].ranura;

                boton.Raiz = new GameObject(orden[i].accion.ToString(), typeof(RectTransform));
                var rt = (RectTransform)boton.Raiz.transform;
                rt.SetParent(marco, false);
                Colocar(rt,
                        new Vector2(x0 + (ranura % Columnas) * Paso,
                                    y0 - (ranura / Columnas) * Paso),
                        new Vector2(Lado, Lado));

                var fondo = Caja("Fondo", rt, tema != null ? tema.BotonDe(0) : null, true);
                Estirar(fondo);
                boton.Fondo = fondo.GetComponent<Image>();

                // Este sí recibe clics: el resto del panel los tiene desactivados.
                boton.Fondo.raycastTarget = true;

                var icono = Caja("Icono", rt, IconoDe(orden[i].accion), false);
                Colocar(icono, Vector2.zero, new Vector2(Lado * 0.62f, Lado * 0.62f));
                boton.Icono = icono.GetComponent<Image>();
                boton.Icono.preserveAspect = true;

                boton.Espera = Etiqueta("Espera", rt, 20, TextAnchor.MiddleCenter, FontStyle.Bold);
                Estirar((RectTransform)boton.Espera.transform);
                boton.Espera.enabled = false;

                boton.Raiz.AddComponent<AvisoDeBoton>()
                     .Configurar(TextoDe(orden[i].accion), MostrarAviso);

                var interactivo = boton.Raiz.AddComponent<Button>();
                interactivo.targetGraphic = boton.Fondo;

                var copia = orden[i].accion;
                interactivo.onClick.AddListener(() =>
                {
                    var selector = SelectorDeUnidades.Actual;
                    if (selector != null) selector.PedirAccion(copia);
                });

                _botones.Add(boton);
            }
        }

        Image _listonAviso;
        Text _aviso;

        /// <summary>Qué se lee al pasar el ratón por encima de cada comando.</summary>
        static string TextoDe(Accion accion)
        {
            switch (accion)
            {
                case Accion.Atacar: return "Atacar  ·  A";
                case Accion.AtacarAuto: return "Atacar al avanzar  ·  T";
                case Accion.Mover: return "Mover  ·  M";
                case Accion.Detener: return "Detener  ·  S";
                case Accion.Curar: return "Curar";
                case Accion.EntrenarPawn: return "Entrenar pawn  ·  50 oro  ·  P";
                default: return "Construir  ·  semana 06";
            }
        }

        /// <summary>
        /// Escribe un aviso en el listón durante unos segundos.
        ///
        /// Lo usa la producción para explicar por qué no ha pasado nada al pulsar. Un botón
        /// que se pulsa y no responde parece roto, y el jugador no tiene forma de saber si
        /// le falta oro o si la cola está llena.
        /// </summary>
        public void Avisar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;

            MostrarAviso(texto);
            _avisoHasta = Time.unscaledTime + 2.5f;
        }

        float _avisoHasta;

        void CaducarAviso()
        {
            if (_avisoHasta <= 0f || Time.unscaledTime < _avisoHasta) return;

            _avisoHasta = 0f;
            MostrarAviso(null);
        }

        /// <summary>
        /// Enseña u oculta el aviso. Con texto nulo se apaga el listón entero en vez de
        /// dejar una cinta vacía flotando sobre los comandos.
        /// </summary>
        void MostrarAviso(string texto)
        {
            bool hay = !string.IsNullOrEmpty(texto);

            if (_aviso != null)
            {
                _aviso.enabled = hay;
                if (hay) _aviso.text = texto;
            }

            if (_listonAviso != null) _listonAviso.enabled = hay && _listonAviso.sprite != null;
        }

        Sprite IconoDe(Accion accion)
        {
            if (tema == null) return null;

            switch (accion)
            {
                case Accion.Atacar: return tema.iconoAtaque;
                case Accion.AtacarAuto: return tema.iconoAtaqueAuto;
                case Accion.Mover: return tema.iconoMover;
                case Accion.Detener: return tema.iconoDetener;
                case Accion.Curar: return tema.iconoCurar;
                case Accion.EntrenarPawn: return tema.iconoEntrenar;
                default: return tema.iconoConstruir;
            }
        }

        /// <summary>
        /// Qué botones tienen sentido para lo que hay seleccionado.
        ///
        /// Curar solo aparece si hay un monje, y construir solo si hay un pawn. Un botón
        /// que no hace nada enseña al jugador a ignorar la rejilla entera.
        /// </summary>
        void RefrescarAcciones(IReadOnlyList<Unidad> seleccion)
        {
            bool hayCurandero = false;
            bool hayConstructor = false;

            for (int i = 0; i < seleccion.Count; i++)
            {
                var d = seleccion[i] != null ? seleccion[i].datos : null;
                if (d == null) continue;

                if (d.dano < 0) hayCurandero = true;
                if (d.tipo == TipoUnidad.Pawn) hayConstructor = true;
            }

            var color = tema != null ? tema.BotonDe(_faccionPintada) : null;

            for (int i = 0; i < _botones.Count; i++)
            {
                var b = _botones[i];

                bool visible = b.Accion != Accion.Curar &&
                               b.Accion != Accion.Construir &&
                               b.Accion != Accion.EntrenarPawn;

                if (b.Accion == Accion.Curar) visible = hayCurandero;
                if (b.Accion == Accion.Construir) visible = hayConstructor;

                if (b.Raiz.activeSelf != visible) b.Raiz.SetActive(visible);
                if (color != null && b.Fondo.sprite != color) b.Fondo.sprite = color;
            }
        }

        /// <summary>Cuenta atrás sobre el botón de curar. Se refresca cada frame.</summary>
        void RefrescarEspera(IReadOnlyList<Unidad> seleccion)
        {
            float espera = 0f;

            for (int i = 0; i < seleccion.Count; i++)
            {
                var u = seleccion[i];
                if (u == null || u.datos == null || u.datos.dano >= 0) continue;

                var maquina = u.GetComponent<MaquinaDeEstados>();
                if (maquina != null) espera = Mathf.Max(espera, maquina.EsperaRestante);
            }

            for (int i = 0; i < _botones.Count; i++)
            {
                if (_botones[i].Accion != Accion.Curar) continue;

                var texto = _botones[i].Espera;
                bool enEspera = espera > 0.05f;

                if (texto.enabled != enEspera) texto.enabled = enEspera;
                if (enEspera) texto.text = Mathf.CeilToInt(espera).ToString();

                // El icono se apaga mientras corre la espera: se lee de un vistazo.
                var icono = _botones[i].Icono;
                var c = icono.color;
                c.a = enEspera ? 0.35f : 1f;
                icono.color = c;
            }
        }

        // -----------------------------------------------------------------
        // Piezas
        // -----------------------------------------------------------------

        /// <summary>Caja pequeña del bando. Se registra para poder reteñirla al cambiar.</summary>
        RectTransform CajaChica(string nombre, RectTransform padre)
        {
            var rt = Caja(nombre, padre, tema != null ? tema.CajaChicaDe(0) : null, true);
            _cajasChicas.Add(rt.GetComponent<Image>());
            return rt;
        }

        static RectTransform Nodo(string nombre, RectTransform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);
            return rt;
        }

        /// <summary>
        /// Imagen del panel. En modo <c>Sliced</c> las esquinas conservan su tamaño y solo
        /// se estira el centro: es lo que permite que una misma caja de 154 px sirva para
        /// un recuadro de cualquier proporción sin deformar el marco pintado.
        /// </summary>
        static RectTransform Caja(string nombre, RectTransform padre, Sprite sprite, bool rebanada)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.enabled = sprite != null;
            img.raycastTarget = false;
            if (rebanada) img.type = Image.Type.Sliced;

            return rt;
        }

        /// <summary>
        /// Relleno recortable colocado exactamente sobre la cavidad interior del marco.
        ///
        /// La cavidad se mide en píxeles del sprite y se multiplica por la escala a la que
        /// se está dibujando el marco. Así la barra encaja igual de bien a cualquier tamaño
        /// y no hay que recalcular nada a mano si se cambian las medidas del panel.
        /// </summary>
        static Image Relleno(RectTransform marco, Sprite sprite, float escala,
                             float cavidadX, float cavidadAncho, float alto)
        {
            var rt = Caja("Relleno", marco, sprite, false);

            // Ancla y pivote en el borde izquierdo del marco. El pivote va antes que la
            // posición: cambiarlo después desplazaría el rectángulo ya colocado.
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(cavidadAncho * escala, alto);
            rt.anchoredPosition = new Vector2(cavidadX * escala, 0f);

            var img = rt.GetComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;

            return img;
        }

        Text Etiqueta(string nombre, RectTransform padre, int tamano,
                      TextAnchor alineacion, FontStyle estilo, bool titulo = false)
        {
            var go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(padre, false);

            var texto = go.GetComponent<Text>();
            texto.font = titulo ? _fuenteTitulo : _fuenteTexto;
            texto.fontSize = tamano;
            texto.fontStyle = estilo;
            texto.alignment = alineacion;
            texto.color = colorTexto;
            texto.raycastTarget = false;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;

            // El contorno se pone aquí, en la única fábrica de textos del panel, para que
            // ninguna etiqueta futura se quede sin él por descuido.
            var contorno = go.AddComponent<Outline>();
            contorno.effectColor = colorContorno;
            contorno.effectDistance = new Vector2(1.6f, -1.6f);

            return texto;
        }

        Text Estadistica(string nombre, RectTransform padre, Sprite icono, float x, float ancho)
        {
            var ranura = Nodo(nombre, padre);
            Colocar(ranura, new Vector2(x, 0f), new Vector2(ancho, 40f));

            var img = Caja("Icono", ranura, icono, false);
            Colocar(img, new Vector2(-ancho * 0.5f + 20f, 0f), new Vector2(36f, 36f));
            img.GetComponent<Image>().preserveAspect = true;

            // El texto arranca donde acaba el icono. Antes se solapaban y el número
            // quedaba escondido detrás del dibujo.
            var texto = Etiqueta("Valor", ranura, 20, TextAnchor.MiddleLeft, FontStyle.Bold);
            Colocar((RectTransform)texto.transform,
                    new Vector2(19f, 0f), new Vector2(ancho - 38f, 28f));

            return texto;
        }

        static void Colocar(RectTransform rt, Vector2 posicion, Vector2 tamano)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamano;
            rt.anchoredPosition = posicion;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
