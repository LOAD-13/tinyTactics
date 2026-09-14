using UnityEngine;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El destello que aparece donde acabas de hacer clic derecho.
    ///
    /// Parece un adorno y no lo es: sin él, una orden dada sobre terreno lejano no tiene
    /// acuse de recibo. Entre que el jugador suelta el botón y que la unidad arranca pasan
    /// varios fotogramas —hay que pedir la ruta, y el buscador va en cola—, y en ese hueco
    /// no hay forma de saber si el clic se registró o cayó en el vacío. La reacción del
    /// jugador es volver a clicar, y eso encola otra orden.
    ///
    /// El color dice además QUÉ se ordenó, que es la otra mitad del mensaje: verde para
    /// moverse, rojo para atacar, ámbar para trabajar.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Marca de orden")]
    public class MarcaDeOrden : MonoBehaviour
    {
        public static MarcaDeOrden Actual { get; private set; }

        public static readonly Color Movimiento = new Color(0.45f, 0.92f, 0.42f, 1f);
        public static readonly Color Ataque = new Color(1f, 0.32f, 0.28f, 1f);
        public static readonly Color Trabajo = new Color(1f, 0.80f, 0.30f, 1f);

        [Tooltip("Corchete del pack. Es el mismo dibujo del marcador de selección.")]
        public Sprite marca;

        [Tooltip("Cuánto dura el destello. Corto: es un acuse de recibo, no una decoración.")]
        [Range(0.15f, 1.5f)] public float duracion = 0.42f;

        [Tooltip("Tamaño inicial, en múltiplos del final. Entra grande y se cierra.")]
        [Range(1f, 3f)] public float escalaInicial = 1.9f;

        [Range(0.1f, 2f)] public float escalaFinal = 0.75f;

        [Tooltip("Orden de dibujo. Muy alto: va por encima de todo el mundo.")]
        public int ordenDeDibujo = 12000;

        void OnEnable() => Actual = this;

        void OnDisable()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>Suelta un destello en un punto del mundo.</summary>
        public void Marcar(Vector3 punto, Color color)
        {
            if (marca == null) return;

            var go = new GameObject("MarcaDeOrden");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(punto.x, punto.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = marca;
            sr.color = color;
            sr.sortingOrder = ordenDeDibujo;

            go.AddComponent<Destello>().Arrancar(this, color);
        }

        /// <summary>Atajo para no repetir la comprobación de nulo en cada sitio que la usa.</summary>
        public static void Soltar(Vector3 punto, Color color)
        {
            if (Actual != null) Actual.Marcar(punto, color);
        }

        /// <summary>
        /// La animación de un destello suelto: se cierra sobre sí mismo y se desvanece.
        /// </summary>
        /// <remarks>
        /// Se anima por código y no con el componente <c>Animator</c>, igual que todo lo
        /// demás (ADR-03). Aquí la razón es aún más simple que en las unidades: son dos
        /// curvas sobre dos propiedades, y montar un controlador de animación para eso
        /// cuesta más de mantener que las diez líneas que hacen falta.
        /// </remarks>
        class Destello : MonoBehaviour
        {
            SpriteRenderer _sprite;
            MarcaDeOrden _dueno;
            Color _color;
            float _reloj;

            public void Arrancar(MarcaDeOrden dueno, Color color)
            {
                _dueno = dueno;
                _color = color;
                _sprite = GetComponent<SpriteRenderer>();

                transform.localScale = Vector3.one * dueno.escalaInicial;
            }

            void Update()
            {
                if (_dueno == null || _sprite == null) { Destroy(gameObject); return; }

                _reloj += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(_reloj / Mathf.Max(0.05f, _dueno.duracion));

                // Se cierra deprisa y se apaga despacio. Con las dos curvas iguales el
                // destello parecía una pompa que se encoge; así se lee como un golpe seco.
                float cierre = 1f - (1f - t) * (1f - t);

                transform.localScale = Vector3.one *
                    Mathf.Lerp(_dueno.escalaInicial, _dueno.escalaFinal, cierre);

                _sprite.color = new Color(_color.r, _color.g, _color.b, 1f - t);

                if (t >= 1f) Destroy(gameObject);
            }
        }
    }
}
