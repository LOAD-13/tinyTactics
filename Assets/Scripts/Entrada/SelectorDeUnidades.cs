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

            LeerBotonIzquierdo(raton);
            LeerBotonDerecho(raton);
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
