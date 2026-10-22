using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// La pausa mínima: reanudar, revancha, menú principal y salir.
    /// </summary>
    /// <remarks>
    /// <b>Mínima a propósito.</b> La pausa completa, con opciones dentro, es de la semana 15.
    /// Esta existe porque sin ella no hay forma de volver al menú desde una partida, y un
    /// juego con menú del que no se puede salir no tiene menú.
    ///
    /// Se abre con Escape, pero solo cuando Escape no tenía otra cosa que deshacer —una
    /// silueta de edificio, la rejilla de construcción, una acción apuntada—. Lo decide el
    /// selector, que es quien sabe qué hay a medias.
    ///
    /// Recuerda la velocidad que había: pausar una partida a x4 y reanudar la deja a x4.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Menú de pausa")]
    public class MenuDePausa : MonoBehaviour
    {
        public TemaInterfaz tema;

        public static MenuDePausa Actual { get; private set; }

        public bool Abierta { get; private set; }

        GameObject _raiz;
        float _velocidadPrevia = 1f;

        void Awake()
        {
            var fuente = tema != null && tema.titular != null
                ? tema.titular
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var lienzo = KitInterfaz.Lienzo(transform, "LienzoPausa", 60);
            _raiz = lienzo.gameObject;

            var raiz = (RectTransform)lienzo.transform;
            KitInterfaz.Velo(raiz, 0.55f);

            var caja = KitInterfaz.Imagen(raiz, "Caja", tema != null ? tema.CajaDe(0) : null, Color.white);
            caja.raycastTarget = true;
            KitInterfaz.Colocar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 600f));

            var titulo = KitInterfaz.Texto(caja.transform, "Titulo", "Pausa", fuente, 56, KitInterfaz.Crema,
                                           TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(500f, 90f),
                                new Vector2(0f, -75f));

            var col = KitInterfaz.Columna(caja.transform, "Botones", 14f);
            KitInterfaz.Colocar(col, new Vector2(0.5f, 0.5f), new Vector2(400f, 330f), new Vector2(0f, -40f));

            Boton(col, "Reanudar", Cerrar, fuente, 0);
            Boton(col, "Revancha", Navegacion.Revancha, fuente, 0);
            Boton(col, "Menú principal", Navegacion.IrAlMenu, fuente, 0);
            Boton(col, "Salir del juego", Navegacion.Salir, fuente, 1);

            _raiz.SetActive(false);
        }

        void Boton(Transform padre, string texto, System.Action accion, Font fuente, int color)
        {
            var b = KitInterfaz.Boton(padre, texto, tema != null ? tema.BotonDe(color) : null, texto, fuente, 32, accion);
            KitInterfaz.Alto(b, 66f);
        }

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;
            if (Abierta) Cerrar();
        }

        public void Alternar()
        {
            if (!isActiveAndEnabled) return;

            if (Abierta) Cerrar();
            else Abrir();
        }

        void Abrir()
        {
            // Con la partida terminada ya está el cartel de final, que tiene sus botones.
            var arbitro = ArbitroDePartida.Actual;
            if (arbitro != null && arbitro.Terminada) return;

            Abierta = true;
            _velocidadPrevia = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            _raiz.SetActive(true);
        }

        void Cerrar()
        {
            Abierta = false;
            Time.timeScale = _velocidadPrevia;
            if (_raiz != null) _raiz.SetActive(false);
        }
    }
}
