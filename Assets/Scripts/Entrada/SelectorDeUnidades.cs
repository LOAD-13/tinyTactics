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
            LeerBotonIzquierdo(raton);
            LeerBotonDerecho(raton);
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
            if (_seleccionadas.Count == 0) return;

            switch (accion)
            {
                case Interfaz.PanelDeUnidad.Accion.Detener:
                    Autoridad.Emitir(new OrdenDetener { Faccion = faccionJugador }, _seleccionadas);
                    _apuntando = null;
                    return;

                case Interfaz.PanelDeUnidad.Accion.Construir:
                    // Sin funcionalidad hasta la semana 06. El botón está para que la
                    // rejilla no cambie de forma cuando llegue.
                    Debug.Log("[Tiny Tactics] Construir llega en la semana 06 (épica E05).");
                    return;

                default:
                    _apuntando = accion;
                    return;
            }
        }

        void LeerAtajos()
        {
            var teclado = Keyboard.current;
            if (teclado == null || _seleccionadas.Count == 0) return;

            if (teclado.aKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Atacar);

            if (teclado.tKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.AtacarAuto);

            if (teclado.mKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Mover);

            if (teclado.sKey.wasPressedThisFrame)
                PedirAccion(Interfaz.PanelDeUnidad.Accion.Detener);

            if (teclado.escapeKey.wasPressedThisFrame) _apuntando = null;
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
            if (_seleccionadas.Count == 0) return;
            if (SobreLaInterfaz(raton.position.ReadValue())) return;

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            Vector3 punto = PuntoEnMundo(raton.position.ReadValue());

            // Clic derecho sobre un enemigo = atacar; sobre el suelo = moverse. Es el
            // gesto contextual de cualquier RTS: un solo botón, dos órdenes distintas
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

            if (elegida != null) Anadir(elegida);
        }

        void SeleccionarEnCaja(Vector2 esquinaA, Vector2 esquinaB, bool acumula)
        {
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
            for (int i = _seleccionadas.Count - 1; i >= 0; i--)
            {
                var u = _seleccionadas[i];
                if (u != null && u.Viva && u.gameObject.activeInHierarchy) continue;

                _seleccionadas.RemoveAt(i);
                VersionSeleccion++;
            }
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
