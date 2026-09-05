using UnityEngine;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// Los corchetes que rodean a la unidad seleccionada: al encenderse se cierran de golpe
    /// desde fuera, como el enfoque de una cámara.
    ///
    /// El pack no trae fotogramas de animación para la interfaz, así que el movimiento es
    /// procedural — es una interpolación de escala, no una tira de sprites. Sale gratis y
    /// se apaga poniendo <see cref="duracion"/> a cero.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Marcador de selección")]
    public class MarcadorSeleccion : MonoBehaviour
    {
        [Tooltip("Escala de la que parte el corchete al aparecer. 1 = sin animación.")]
        [Range(1f, 3f)] public float escalaInicial = 1.7f;

        [Tooltip("Segundos que tarda en cerrarse. 0 lo deja fijo.")]
        [Range(0f, 0.6f)] public float duracion = 0.13f;

        [Tooltip("Tamano de reposo del corchete. El castillo lo necesita mucho mayor que " +
                 "una unidad, y la animacion multiplica sobre este valor en vez de " +
                 "aplastarlo a uno.")]
        [Range(0.5f, 6f)] public float escalaBase = 1f;

        float _reloj;

        void OnEnable()
        {
            _reloj = 0f;
            Aplicar(0f);
        }

        void Update()
        {
            if (duracion <= 0.0001f || _reloj >= duracion) return;

            _reloj += Time.unscaledDeltaTime;
            Aplicar(Mathf.Clamp01(_reloj / duracion));
        }

        void Aplicar(float t)
        {
            if (duracion <= 0.0001f)
            {
                transform.localScale = new Vector3(escalaBase, escalaBase, 1f);
                return;
            }

            // Salida rápida y frenada al final: el corchete "aterriza" sobre la unidad en
            // vez de llegar a velocidad constante, que se lee como un salto.
            float suave = 1f - (1f - t) * (1f - t);
            float escala = Mathf.Lerp(escalaInicial, 1f, suave) * escalaBase;

            transform.localScale = new Vector3(escala, escala, 1f);
        }
    }
}
