using UnityEngine;
using TinyTactics.Edificios;

namespace TinyTactics.Combate
{
    /// <summary>
    /// Llamas sobre un edificio herido: más y más grandes cuanta menos vida le queda.
    /// </summary>
    /// <remarks>
    /// Se añade el primer golpe que recibe el edificio, no al construirlo: la mayoría de
    /// edificios de una partida nunca arden y no tienen por qué cargar con el componente.
    /// Las llamas cuelgan del edificio, así que la niebla las tapa con él.
    /// <para>
    /// Un edificio que se repara se apaga solo: el fuego sigue a la vida, no a los golpes.
    /// </para>
    /// </remarks>
    public class FuegoDeEdificio : MonoBehaviour
    {
        /// <summary>Por debajo de cada umbral de vida se enciende una llama más.</summary>
        public static readonly float[] Umbrales = { 0.6f, 0.35f, 0.15f };

        /// <summary>Dónde va cada llama, en fracción del dibujo del edificio.</summary>
        static readonly Vector2[] Sitios = { new Vector2(0.62f, 0.55f), new Vector2(0.32f, 0.45f), new Vector2(0.5f, 0.72f) };

        Edificio _edificio;
        SpriteRenderer _dibujo;
        readonly GameObject[] _llamas = new GameObject[3];
        readonly SpriteRenderer[] _pintores = new SpriteRenderer[3];

        /// <summary>Cuántas llamas tocan con esta vida. Puro, para las pruebas.</summary>
        public static int LlamasPara(float fraccionDeVida)
        {
            int n = 0;
            foreach (var u in Umbrales) if (fraccionDeVida > 0f && fraccionDeVida <= u) n++;
            return n;
        }

        void Awake()
        {
            _edificio = GetComponent<Edificio>();
            _dibujo = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            if (_edificio == null) return;
            int toca = LlamasPara(_edificio.FraccionDeVida);

            for (int i = 0; i < _llamas.Length; i++)
            {
                bool debe = i < toca;
                if (debe && _llamas[i] == null) Encender(i);
                if (_llamas[i] != null && _llamas[i].activeSelf != debe) _llamas[i].SetActive(debe);
                // Delante de su edificio, que cambia de orden al moverse la cámara de altura.
                if (_pintores[i] != null && _dibujo != null) _pintores[i].sortingOrder = _dibujo.sortingOrder + 1;
            }
        }

        void Encender(int i)
        {
            var e = EfectosVisuales.Actual;
            if (e == null) return;
            var tira = i == 0 ? e.fuegoMediano : i == 1 ? e.fuegoChico : e.fuegoGrande;
            if (tira == null || tira.Length == 0) return;

            Vector3 donde = transform.position;
            if (_dibujo != null && _dibujo.sprite != null)
            {
                var b = _dibujo.bounds;
                donde = new Vector3(Mathf.Lerp(b.min.x, b.max.x, Sitios[i].x), Mathf.Lerp(b.min.y, b.max.y, Sitios[i].y), transform.position.z);
            }

            var go = EfectosVisuales.Suelto("Llama", tira, donde, _dibujo != null ? _dibujo.sortingOrder + 1 : 0, e.fps, true);
            go.transform.SetParent(transform, true);
            _llamas[i] = go;
            _pintores[i] = go.GetComponent<SpriteRenderer>();
        }
    }
}
