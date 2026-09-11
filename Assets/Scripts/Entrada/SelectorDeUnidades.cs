using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Entrada
{
    /// <summary>
    /// Selección y órdenes del jugador: clic para elegir una unidad, arrastre para elegir
    /// un grupo, shift para acumular y clic derecho para mandarlas.
    ///
    /// Este componente <b>no mueve nada</b>. Traduce el input a órdenes y se las pasa a la
    /// autoridad (ADR-01). Si mañana la IA quiere mover unidades, emite las mismas órdenes
    /// por el mismo sitio.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Selector de unidades")]
    public class SelectorDeUnidades : MonoBehaviour
    {
        [Header("Bando")]
        [Tooltip("Facción que controla este jugador.")]
        public int faccionJugador;

        [Header("Selección")]
        [Tooltip("Píxeles que hay que arrastrar para que cuente como caja y no como clic.")]
        public float umbralArrastre = 8f;

        [Tooltip("Radio en unidades de mundo para atrapar una unidad con un clic simple.")]
        public float radioClic = 0.55f;

        [Tooltip("Radio para atrapar un árbol, una veta o una oveja. Más generoso que el " +
                 "de unidad: los recursos son objetos grandes y con mucho aire alrededor.")]
        public float radioNodo = 1.1f;

        [Header("Apariencia de la caja")]
        public Color colorRelleno = new Color(0.30f, 0.85f, 0.75f, 0.15f);
        public Color colorBorde = new Color(0.35f, 0.95f, 0.85f, 0.9f);

        readonly List<Unidad> _seleccionadas = new List<Unidad>();
        readonly List<Unidad> _temporal = new List<Unidad>();

        // Lista aparte para la busqueda de enemigos. Compartir _temporal funciona hoy
        // por el orden en que se llaman, pero es la clase de dependencia invisible que
        // revienta en cuanto alguien mueve una linea.
        readonly List<Unidad> _cercanas = new List<Unidad>();

        Camera _camara;
        Vector2 _inicioArrastre;
        bool _arrastrando;

        Texture2D _pixel;
        bool _avisado;

        public IReadOnlyList<Unidad> Seleccionadas => _seleccionadas;

        /// <summary>
        /// Edificio elegido, o null. Es <b>excluyente</b> con la selección de unidades:
        /// en cualquier RTS clásico un edificio no forma grupo con las tropas, porque las
        /// órdenes que acepta no tienen nada que ver con las de una unidad.
        /// </summary>
        public Edificios.Edificio EdificioSeleccionado { get; private set; }

        /// <summary>
        /// Instancia activa. La usan el panel y el cursor para leer la selección sin
        /// buscarla por escena cada frame, igual que <c>MundoJuego.Actual</c>.
        /// </summary>
        public static SelectorDeUnidades Actual { get; private set; }

        /// <summary>
        /// Sube en cada cambio de selección. Quien dibuje la selección compara este número
        /// contra el suyo y solo se redibuja cuando difieren: sin esto habría que reasignar
        /// retratos y textos sesenta veces por segundo para nada.
        /// </summary>
        public int VersionSeleccion { get; private set; }

        void Awake()
        {
            Actual = this;
            _camara = Camera.main;
            _pixel = new Texture2D(1, 1);
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();

            Debug.Log(
                $"[Selector] Awake OK. camara={(_camara != null ? _camara.name : "NULA")} " +
                $"faccion={faccionJugador}", this);
        }

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
            if (_pixel != null) Destroy(_pixel);
        }

        void Update()
        {
            // El índice espacial se reconstruye una vez por frame, antes de que
            // cualquiera lo consulte para separarse o para seleccionar.
            RegistroDeUnidades.Reconstruir();
            PurgarSeleccion();

            var raton = Mouse.current;
            if (raton == null || _camara == null)
            {
                if (_avisado) return;
                _avisado = true;
                Debug.LogError(
                    $"[Selector] Sin entrada: raton={(raton != null ? "ok" : "NULO")} " +
                    $"camara={(_camara != null ? "ok" : "NULA")}", this);
                return;
            }

            LeerAtajos();

            // La colocación se come el ratón entero mientras dura. Es un modo, no una
            // acción: mientras arrastras una silueta por el mapa, ni se selecciona ni se
            // dan órdenes, igual que en cualquier RTS del género.
            if (LeerColocacion(raton)) return;

            LeerBotonIzquierdo(raton);
            LeerBotonDerecho(raton);
        }

        /// <summary>
        /// Mueve la silueta y resuelve el clic que planta el edificio.
        /// Devuelve true si ha consumido la entrada de este fotograma.
        /// </summary>
        bool LeerColocacion(Mouse raton)
        {
            var colocador = Edificios.ColocadorEdificios.Actual;
            if (colocador == null || !colocador.Activo) return false;

            Vector2 pantalla = raton.position.ReadValue();

            // Sobre el panel la silueta se congela donde estaba: seguir al ratón por detrás
            // de la interfaz enseña un sitio del mapa que el clic no va a poder elegir.
            bool sobrePanel = SobreLaInterfaz(pantalla);
            if (!sobrePanel) colocador.Apuntar(PuntoEnMundo(pantalla));

            // Clic derecho o Escape cancelan, que son los dos gestos que cualquiera prueba.
            if (raton.rightButton.wasPressedThisFrame)
            {
                CancelarColocacion();
                return true;
            }

            if (raton.leftButton.wasPressedThisFrame && !sobrePanel)
                ResolverColocacion(colocador);

            return true;
        }

        // -----------------------------------------------------------------
        // Panel de acciones
        // -----------------------------------------------------------------

        /// <summary>Acción pedida desde la rejilla de comandos, esperando su objetivo.</summary>
        Interfaz.PanelDeUnidad.Accion? _apuntando;

        /// <summary>¿El siguiente clic izquierdo elige objetivo en vez de seleccionar?</summary>
        public bool Apuntando => _apuntando.HasValue;

        /// <summary>
        /// Recibe una orden del panel o de un atajo.
        ///
        /// Las que necesitan un punto en el mapa —atacar, mover— dejan el cursor "armado"
        /// y esperan al siguiente clic. Las inmediatas se aplican y ya. Es el mismo
        /// comportamiento del menú de comandos de Warcraft.
        /// </summary>
        public void PedirAccion(Interfaz.PanelDeUnidad.Accion accion)
        {
            // Entrenar es la única acción de edificio, y no necesita objetivo: se pulsa y
            // se encola. Va antes de la comprobación de unidades porque cuando hay un
            // edificio elegido no hay ninguna unidad seleccionada.
            if (accion == Interfaz.PanelDeUnidad.Accion.EntrenarPawn)
            {
                Entrenar();
                return;
            }

            if (_seleccionadas.Count == 0) return;

            switch (accion)
            {
                case Interfaz.PanelDeUnidad.Accion.Detener:
                    Autoridad.Emitir(new OrdenDetener { Faccion = faccionJugador }, _seleccionadas);
                    _apuntando = null;
                    return;

                case Interfaz.PanelDeUnidad.Accion.Construir:
                    // No coloca nada todavía: abre la rejilla con lo que un pawn sabe
                    // levantar. Elegir QUÉ y elegir DÓNDE son dos decisiones distintas, y
                    // juntarlas en un clic obligaría a cancelar y volver a empezar cada vez
                    // que el jugador se equivoca de edificio.
                    AbrirConstruccion(true);
                    return;

                case Interfaz.PanelDeUnidad.Accion.Cancelar:
                    AbrirConstruccion(false);
                    CancelarColocacion();
                    return;

                default:
                    _apuntando = accion;
                    return;
            }
        }

        // -----------------------------------------------------------------
        // Construcción
        // -----------------------------------------------------------------

        /// <summary>¿Está el panel enseñando la rejilla de edificios?</summary>
        public bool EligiendoEdificio { get; private set; }

        void AbrirConstruccion(bool abierta)
        {
            if (EligiendoEdificio == abierta) return;

            EligiendoEdificio = abierta;
            VersionSeleccion++;
        }

        /// <summary>
        /// El jugador ha elegido qué construir: ahora toca elegir dónde.
        /// </summary>
        public void PedirConstruir(Datos.DatosEdificio ficha)
        {
            if (ficha == null || !HayRecolector()) return;

            var panel = Interfaz.PanelDeUnidad.Actual;

            var eco = Economia.Actual;
            if (eco != null && !eco.PuedePagar(faccionJugador, ficha.oro, ficha.madera))
            {
                // Se avisa aquí y no al soltar el clic en el mapa: llevar al jugador a
                // pasear una silueta por el mapa para decirle al final que no le alcanza es
                // hacerle perder el tiempo dos veces.
                if (panel != null) panel.Avisar($"Faltan recursos · {ficha.CosteVisible()}");
                return;
            }

            var colocador = Edificios.ColocadorEdificios.Actual;
            if (colocador == null || !colocador.Empezar(ficha, faccionJugador))
            {
                if (panel != null) panel.Avisar("No encuentro el dibujo. Regenera la escena");
                return;
            }

            _apuntando = null;
            AbrirConstruccion(false);
        }

        /// <summary>Resuelve el clic que planta el edificio.</summary>
        void ResolverColocacion(Edificios.ColocadorEdificios colocador)
        {
            string impedimento = colocador.Impedimento();
            var panel = Interfaz.PanelDeUnidad.Actual;

            if (impedimento != null)
            {
                if (panel != null) panel.Avisar(impedimento);
                return;
            }

            var ficha = colocador.Ficha;

            var eco = Economia.Actual;
            if (eco != null && !eco.Cobrar(faccionJugador, ficha.oro, ficha.madera))
            {
                if (panel != null) panel.Avisar("Faltan recursos");
                return;
            }

            // Una sola orden para todo el grupo: la obra se planta una vez y todos los
            // pawns se suman a ella. Emitirla por unidad plantaría una casa por pawn.
            Autoridad.Emitir(
                new OrdenConstruir
                {
                    Faccion = faccionJugador,
                    Edificio = ficha,
                    Celdas = colocador.Celdas,
                },
                _seleccionadas);

            // Con shift se encadenan varios sin volver al panel, como en cualquier RTS.
            if (!Acumulando()) CancelarColocacion();
        }

        void CancelarColocacion()
        {
            var colocador = Edificios.ColocadorEdificios.Actual;
            if (colocador != null) colocador.Cancelar();
        }

        /// <summary>Encola una unidad en el edificio elegido y avisa si no se ha podido.</summary>
        void Entrenar()
        {
            if (EdificioSeleccionado == null) return;

            var produccion = EdificioSeleccionado.GetComponent<Edificios.ProduccionEdificio>();
            if (produccion == null) return;

            var catalogo = produccion.Catalogo;
            if (catalogo.Length == 0) return;

            // El atajo de teclado encola lo primero que el edificio sepa hacer. La rejilla
            // del panel sí deja elegir cuál de todas.
            PedirFabricar(catalogo[0]);
        }

        /// <summary>Encola una unidad concreta en el edificio elegido.</summary>
        public void PedirFabricar(Datos.DatosUnidad datos)
        {
            if (EdificioSeleccionado == null || datos == null) return;

            var produccion = EdificioSeleccionado.GetComponent<Edificios.ProduccionEdificio>();
            if (produccion == null) return;

            if (produccion.Encolar(datos, out string motivo)) return;

            // El aviso viaja al mismo listón donde se lee qué hace cada botón. Sin él, un
            // clic sin oro no hace absolutamente nada y el jugador no sabe si el botón está
            // roto o si es que no le alcanza.
            var panel = Interfaz.PanelDeUnidad.Actual;
            if (panel != null) panel.Avisar(motivo);
        }

        void LeerAtajos()
        {
            var teclado = Keyboard.current;

            if (teclado == null) return;

            // Escape deshace, en este orden: primero la silueta que está en el aire, luego
            // la rejilla de edificios, y solo después la acción apuntada. Al revés, cancelar
            // una colocación cerraría además el menú y habría que volver a abrirlo.
            if (teclado.escapeKey.wasPressedThisFrame)
            {
                var enCurso = Edificios.ColocadorEdificios.Actual;
                if (enCurso != null && enCurso.Activo) { CancelarColocacion(); return; }

                if (EligiendoEdificio) { AbrirConstruccion(false); return; }

                _apuntando = null;
                return;
            }

            if (EdificioSeleccionado != null && _seleccionadas.Count == 0)
            {
                if (teclado.pKey.wasPressedThisFrame)
                    PedirAccion(Interfaz.PanelDeUnidad.Accion.EntrenarPawn);

                return;
            }

            if (_seleccionadas.Count == 0) return;

            if (teclado.aKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Atacar);

            if (teclado.tKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.AtacarAuto);

            if (teclado.mKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Mover);

            if (teclado.sKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Detener);

            if (teclado.bKey.wasPressedThisFrame && HayRecolector())
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Construir);
        }

        /// <summary>Resuelve el clic que da objetivo a una acción armada.</summary>
        void ResolverApuntado(Vector2 pantalla)
        {
            var accion = _apuntando.Value;
            _apuntando = null;

            Vector3 punto = PuntoEnMundo(pantalla);

            if (accion == Interfaz.PanelDeUnidad.Accion.Curar)
            {
                var herido = UnidadEn(punto, true);
                if (herido != null)
                    Autoridad.Emitir(
                        new OrdenCurar { Faccion = faccionJugador, Objetivo = herido },
                        _seleccionadas);

                return;
            }

            if (accion == Interfaz.PanelDeUnidad.Accion.Atacar)
            {
                var victima = UnidadEn(punto, false);
                if (victima != null)
                {
                    Autoridad.Emitir(
                        new OrdenAtacar { Faccion = faccionJugador, Objetivo = victima },
                        _seleccionadas);

                    return;
                }

                // Sobre suelo vacío, Atacar avanza atacando: es lo que hace Warcraft y
                // evita que el clic se pierda. Nunca cae sobre un aliado — sin fuego amigo.
                MoverA(punto, true);
                return;
            }

            // Mover y atacar-automático van los dos a un punto del mapa; el segundo deja
            // además la vigilancia encendida durante el trayecto.
            bool vigilando = accion == Interfaz.PanelDeUnidad.Accion.AtacarAuto;
            MoverA(punto, vigilando);
        }

        // -----------------------------------------------------------------

        /// <summary>
        /// La UI se come el clic. Sin esto, pulsar sobre el panel inferior seleccionaría o
        /// mandaría unidades a la celda que quedara detrás de la caja.
        /// </summary>
        static bool SobreLaInterfaz(Vector2 pantalla)
        {
            var panel = Interfaz.PanelDeUnidad.Actual;
            return panel != null && panel.CapturaPuntero(pantalla);
        }

        void LeerBotonIzquierdo(Mouse raton)
        {
            if (raton.leftButton.wasPressedThisFrame)
            {
                if (SobreLaInterfaz(raton.position.ReadValue())) return;

                // Con una accion armada, el clic izquierdo elige objetivo y no selecciona.
                if (_apuntando.HasValue)
                {
                    ResolverApuntado(raton.position.ReadValue());
                    return;
                }

                _inicioArrastre = raton.position.ReadValue();
                _arrastrando = true;
            }

            if (!raton.leftButton.wasReleasedThisFrame || !_arrastrando) return;

            _arrastrando = false;

            Vector2 fin = raton.position.ReadValue();
            bool acumula = Acumulando();

            if (Vector2.Distance(_inicioArrastre, fin) < umbralArrastre)
                SeleccionarEnPunto(fin, acumula);
            else
                SeleccionarEnCaja(_inicioArrastre, fin, acumula);
        }

        void LeerBotonDerecho(Mouse raton)
        {
            if (!raton.rightButton.wasPressedThisFrame) return;
            if (SobreLaInterfaz(raton.position.ReadValue())) return;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            Vector3 punto = PuntoEnMundo(raton.position.ReadValue());

            // Con un edificio elegido, el clic derecho no manda tropas: fija dónde salen
            // las que fabrique. Es el punto de reunión de toda la vida.
            if (EdificioSeleccionado != null && _seleccionadas.Count == 0)
            {
                FijarPuntoDeReunion(punto);
                return;
            }

            if (_seleccionadas.Count == 0) return;

            // Sobre el propio centro de entrega, los pawns cargados van a soltar. Es la
            // pareja natural del clic sobre un recurso, y la única forma de decirle a un
            // peón al que has interrumpido a mitad de viaje que termine lo que llevaba.
            var propio = Edificios.Edificio.EdificioEn(punto, faccionJugador);

            // Sobre una obra a medias, los pawns se ponen a martillar. Va antes que la
            // entrega porque una obra todavía no es centro de entrega de nada, y antes que
            // el movimiento porque es lo que el jugador quiere decir al hacer clic ahí.
            if (propio != null && !propio.Operativo && HayRecolector())
            {
                var obra = propio.GetComponent<Edificios.ObraEnConstruccion>();
                if (obra != null)
                {
                    Autoridad.Emitir(
                        new OrdenAyudarObra { Faccion = faccionJugador, Obra = obra },
                        _seleccionadas);
                    return;
                }
            }

            if (propio != null && propio.centroDeEntrega && propio.Operativo && HayCargado())
            {
                Autoridad.Emitir(new OrdenEntregar { Faccion = faccionJugador }, _seleccionadas);
                return;
            }

            // Sobre un árbol, una veta o una oveja, los pawns se ponen a trabajar. El resto
            // del grupo ignora la orden: mandar a un guerrero a talar no significa nada, y
            // arrastrar al ejército entero al bosque sería peor que no hacer nada.
            var nodo = NodoRecurso.NodoEn(punto, radioNodo);
            if (nodo != null && HayRecolector())
            {
                Autoridad.Emitir(
                    new OrdenRecolectar { Faccion = faccionJugador, Nodo = nodo },
                    _seleccionadas);
                return;
            }

            // Clic derecho sobre un enemigo = atacar; sobre el suelo = moverse. Es el
            // gesto contextual de cualquier RTS: un solo botón, varias órdenes distintas
            // según lo que haya debajo.
            var victima = UnidadEn(punto, false);
            if (victima != null)
            {
                Autoridad.Emitir(
                    new OrdenAtacar { Faccion = faccionJugador, Objetivo = victima },
                    _seleccionadas);
                return;
            }

            // Sobre un aliado, los que sepan curar curan. Los demás ignoran la orden, así
            // que seleccionar un grupo mixto y hacer clic sobre un herido funciona: actúa
            // el monje y el resto se queda quieto en vez de irse encima.
            var aliado = UnidadEn(punto, true);
            if (aliado != null && HayCurandero())
            {
                Autoridad.Emitir(
                    new OrdenCurar { Faccion = faccionJugador, Objetivo = aliado },
                    _seleccionadas);
                return;
            }

            MoverA(punto, false);
        }

        /// <summary>
        /// Manda al grupo a un punto. Con vigilancia, ademas atacan lo que se cruce.
        /// </summary>
        void MoverA(Vector3 punto, bool vigilando)
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            Vector2Int centro = mundo.Grilla.MundoACelda(punto);
            if (!mundo.Grilla.CeldaTransitableCercana(centro, 12, out centro)) return;

            // Cada unidad recibe su propia celda alrededor del punto pedido: si todas
            // fueran a la misma, se amontonarían peleando por ella.
            for (int i = 0; i < _seleccionadas.Count; i++)
            {
                var unidad = _seleccionadas[i];
                if (unidad == null || !unidad.Viva) continue;

                Vector2Int destino = Autoridad.DestinoParaMiembro(centro, i);
                if (!mundo.Grilla.CeldaTransitableCercana(destino, 6, out destino)) continue;

                _temporal.Clear();
                _temporal.Add(unidad);

                Autoridad.Emitir(
                    new OrdenMover { Faccion = faccionJugador, Destino = destino },
                    _temporal);

                // La vigilancia se enciende DESPUES de emitir el movimiento, no antes:
                // OrdenMover llama a Cancelar() para soltar el objetivo anterior, y eso
                // tambien apagaba la vigilancia que se acababa de activar. Por eso el
                // ataque al avanzar caminaba hasta el punto sin pegarle a nada.
                var maquina = unidad.GetComponent<Unidades.MaquinaDeEstados>();
                if (maquina != null) maquina.Vigilar(vigilando);
            }
        }

        static bool Acumulando()
        {
            var teclado = Keyboard.current;
            return teclado != null &&
                   (teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed);
        }

        Vector3 PuntoEnMundo(Vector2 pantalla)
        {
            Vector3 p = _camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 0f));
            p.z = 0f;
            return p;
        }

        // -----------------------------------------------------------------

        void SeleccionarEnPunto(Vector2 pantalla, bool acumula)
        {
            Vector3 mundo = PuntoEnMundo(pantalla);

            Unidad elegida = null;
            float mejor = radioClic * radioClic;

            var todas = RegistroDeUnidades.Todas;
            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (!EsSeleccionable(u)) continue;

                float d2 = ((Vector2)(u.transform.position - mundo)).sqrMagnitude;
                if (d2 <= mejor)
                {
                    mejor = d2;
                    elegida = u;
                }
            }

            if (!acumula) LimpiarSeleccion();

            if (elegida != null)
            {
                SeleccionarEdificio(null);
                Anadir(elegida);
                return;
            }

            // Solo si el clic no ha cogido ninguna unidad se mira si hay un edificio
            // debajo. Al revés, el castillo se comería los clics de los pawns que tiene
            // delante, que es justo donde se amontonan al depositar.
            if (!acumula) SeleccionarEdificio(Edificios.Edificio.EdificioEn(mundo, faccionJugador));
        }

        void SeleccionarEdificio(Edificios.Edificio edificio)
        {
            if (EdificioSeleccionado == edificio) return;

            if (EdificioSeleccionado != null) EdificioSeleccionado.Seleccionar(false);

            EdificioSeleccionado = edificio;

            if (edificio != null) edificio.Seleccionar(true);

            VersionSeleccion++;
        }

        void SeleccionarEnCaja(Vector2 esquinaA, Vector2 esquinaB, bool acumula)
        {
            // La caja de arrastre nunca coge edificios. Es la convención del género: si
            // arrastrar por encima de la base metiera el castillo en el grupo, la mitad de
            // las órdenes del jugador irían dirigidas a algo que no se mueve.
            if (!acumula) SeleccionarEdificio(null);

            if (!acumula) LimpiarSeleccion();

            Vector3 a = PuntoEnMundo(esquinaA);
            Vector3 b = PuntoEnMundo(esquinaB);

            float minX = Mathf.Min(a.x, b.x), maxX = Mathf.Max(a.x, b.x);
            float minY = Mathf.Min(a.y, b.y), maxY = Mathf.Max(a.y, b.y);

            var todas = RegistroDeUnidades.Todas;
            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (!EsSeleccionable(u)) continue;

                Vector3 p = u.transform.position;
                if (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY) continue;

                Anadir(u);
            }
        }

        /// <summary>¿Hay algún pawn con las manos ocupadas entre lo seleccionado?</summary>
        bool HayCargado()
        {
            for (int i = 0; i < _seleccionadas.Count; i++)
            {
                var u = _seleccionadas[i];
                if (u == null) continue;

                var r = u.GetComponent<RecolectorPawn>();
                if (r != null && r.Carga != Datos.TipoRecurso.Ninguno) return true;
            }

            return false;
        }

        /// <summary>¿Hay algún pawn entre lo seleccionado?</summary>
        bool HayRecolector()
        {
            for (int i = 0; i < _seleccionadas.Count; i++)
            {
                var u = _seleccionadas[i];
                if (u != null && u.GetComponent<RecolectorPawn>() != null) return true;
            }

            return false;
        }

        /// <summary>
        /// Fija dónde salen las unidades que fabrique el edificio elegido.
        ///
        /// Si el punto cae sobre un recurso, el pawn nuevo se pone a recolectarlo nada más
        /// nacer. Es un detalle pequeño y es lo que separa tener que recoger a mano cada
        /// pawn de poder dejar la base produciendo mientras peleas en la otra punta.
        /// </summary>
        void FijarPuntoDeReunion(Vector3 punto)
        {
            var produccion = EdificioSeleccionado.GetComponent<Edificios.ProduccionEdificio>();
            if (produccion == null) return;

            produccion.tienePuntoDeReunion = true;
            produccion.puntoDeReunion = punto;
            produccion.nodoDeReunion = NodoRecurso.NodoEn(punto, radioNodo);
        }

        /// <summary>¿Hay algún curandero entre lo seleccionado?</summary>
        bool HayCurandero()
        {
            for (int i = 0; i < _seleccionadas.Count; i++)
            {
                var u = _seleccionadas[i];
                if (u != null && u.datos != null && u.datos.dano < 0) return true;
            }

            return false;
        }

        /// <summary>Unidad viva bajo el punto, del propio bando o de otro.</summary>
        Unidad UnidadEn(Vector3 mundo, bool propia)
        {
            RegistroDeUnidades.Vecinas(mundo, _cercanas);

            Unidad elegida = null;
            float mejor = radioClic * radioClic;

            for (int i = 0; i < _cercanas.Count; i++)
            {
                var u = _cercanas[i];
                if (u == null || !u.Viva) continue;
                if ((u.faccion == faccionJugador) != propia) continue;
                if (propia && _seleccionadas.Contains(u)) continue;

                float d2 = ((Vector2)(u.transform.position - mundo)).sqrMagnitude;
                if (d2 > mejor) continue;

                mejor = d2;
                elegida = u;
            }

            return elegida;
        }

        /// <summary>Solo unidades vivas del propio bando. Las enemigas no se seleccionan.</summary>
        bool EsSeleccionable(Unidad u) =>
            u != null && u.Viva && u.gameObject.activeInHierarchy && u.faccion == faccionJugador;

        void Anadir(Unidad u)
        {
            if (_seleccionadas.Contains(u)) return;
            _seleccionadas.Add(u);
            u.Seleccionar(true);
            VersionSeleccion++;
        }

        void LimpiarSeleccion()
        {
            for (int i = 0; i < _seleccionadas.Count; i++)
                if (_seleccionadas[i] != null) _seleccionadas[i].Seleccionar(false);

            if (_seleccionadas.Count > 0) VersionSeleccion++;
            _seleccionadas.Clear();
        }

        /// <summary>
        /// Saca de la selección lo que ya no existe. Una unidad muerta se desactiva, y sin
        /// esto seguiría ocupando su hueco en el panel con la cara de un cadáver.
        /// </summary>
        void PurgarSeleccion()
        {
            // Un edificio destruido o apagado no puede seguir elegido. Se compara con
            // ReferenceEquals porque un objeto destruido de Unity finge ser null en el
            // operador normal: sin esto la referencia muerta se quedaría dentro para
            // siempre y el panel intentaría dibujar un castillo que ya no existe.
            if (!ReferenceEquals(EdificioSeleccionado, null) &&
                (EdificioSeleccionado == null ||
                 !EdificioSeleccionado.gameObject.activeInHierarchy))
            {
                EdificioSeleccionado = null;
                VersionSeleccion++;
            }

            for (int i = _seleccionadas.Count - 1; i >= 0; i--)
            {
                var u = _seleccionadas[i];
                if (u != null && u.Viva && u.gameObject.activeInHierarchy) continue;

                _seleccionadas.RemoveAt(i);
                VersionSeleccion++;
            }

            // La rejilla de edificios pertenece al pawn que la abrió. Si deja de estar
            // seleccionado —porque el jugador eligió otra cosa o porque murió— el menú se
            // cierra solo en vez de quedarse ofreciendo casas que nadie puede levantar.
            if (EligiendoEdificio && !HayRecolector()) AbrirConstruccion(false);
        }

        // -----------------------------------------------------------------

        void OnGUI()
        {
            if (!_arrastrando) return;

            var raton = Mouse.current;
            if (raton == null) return;

            Vector2 actual = raton.position.ReadValue();
            if (Vector2.Distance(_inicioArrastre, actual) < umbralArrastre) return;

            // OnGUI usa el origen arriba a la izquierda; el ratón, abajo a la izquierda.
            Rect r = Rect.MinMaxRect(
                Mathf.Min(_inicioArrastre.x, actual.x),
                Screen.height - Mathf.Max(_inicioArrastre.y, actual.y),
                Mathf.Max(_inicioArrastre.x, actual.x),
                Screen.height - Mathf.Min(_inicioArrastre.y, actual.y));

            GUI.color = colorRelleno;
            GUI.DrawTexture(r, _pixel);

            GUI.color = colorBorde;
            const float g = 1.5f;
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, r.width, g), _pixel);
            GUI.DrawTexture(new Rect(r.xMin, r.yMax - g, r.width, g), _pixel);
            GUI.DrawTexture(new Rect(r.xMin, r.yMin, g, r.height), _pixel);
            GUI.DrawTexture(new Rect(r.xMax - g, r.yMin, g, r.height), _pixel);

            GUI.color = Color.white;
        }
    }
}
