using UnityEngine;
using TinyTactics.Unidades;

namespace TinyTactics.Combate
{
    /// <summary>
    /// Los efectos de partículas del pack: polvo, fuego. Uno por escena; el generador le
    /// pone los fotogramas.
    /// </summary>
    /// <remarks>
    /// <b>«Partículas» de sprites, no un <c>ParticleSystem</c>.</b> El pack trae los efectos
    /// dibujados fotograma a fotograma (<c>Particle FX</c>), y un sistema de partículas de
    /// Unity con puntos o texturas genéricas desentonaría con el pixel art. Cada efecto es un
    /// objeto con el mismo <see cref="AnimadorSprite"/> de las unidades, que se borra al
    /// acabar. La explosión al derribar un edificio ya funcionaba así desde la semana 7.
    /// </remarks>
    public class EfectosVisuales : MonoBehaviour
    {
        public static EfectosVisuales Actual { get; private set; }

        public Sprite[] polvo = new Sprite[0];
        public Sprite[] polvoGrande = new Sprite[0];

        [Tooltip("Tres llamas de distinto tamaño: Fire_01, 02 y 03.")]
        public Sprite[] fuegoChico = new Sprite[0];
        public Sprite[] fuegoMediano = new Sprite[0];
        public Sprite[] fuegoGrande = new Sprite[0];

        [Range(4f, 30f)] public float fps = 14f;

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>Una nube de polvo que se deshace sola.</summary>
        public static void Polvo(Vector3 donde, int orden, bool grande = false, float escala = 1f)
        {
            var e = Actual;
            if (e == null) return;
            var tira = grande && e.polvoGrande.Length > 0 ? e.polvoGrande : e.polvo;
            if (tira.Length == 0) return;

            var go = Suelto("Polvo", tira, donde, orden, e.fps, false);
            go.transform.localScale = Vector3.one * escala;
            Destroy(go, tira.Length / e.fps + 0.1f);
        }

        /// <summary>Un efecto animado colgado de nada, con su propio dibujo.</summary>
        public static GameObject Suelto(string nombre, Sprite[] tira, Vector3 donde, int orden, float fps, bool bucle)
        {
            var go = new GameObject(nombre);
            go.transform.position = donde;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tira[0];
            sr.sortingOrder = orden;
            var anim = go.AddComponent<AnimadorSprite>();
            anim.dispersion = bucle ? 1f : 0f;
            anim.Configurar(tira, fps, 0, bucle);
            return go;
        }
    }
}
