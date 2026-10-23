using UnityEngine;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Audio
{
    /// <summary>
    /// El fondo de la partida: pájaros de día, grillos de noche y una capa de viento.
    /// </summary>
    /// <remarks>
    /// Sigue al ciclo de día y noche que ya pinta la luz: no tiene reloj propio, pregunta la
    /// hora a <see cref="CicloDelDia"/>. Así lo que se ve y lo que se oye no pueden
    /// desincronizarse. Sin ciclo (el editor de mapas lo apaga) se queda en el día.
    /// <para>No suena en el menú: allí manda la música.</para>
    /// </remarks>
    public class AmbienteSonoro : MonoBehaviour
    {
        public CatalogoDeMusica catalogo;

        [Tooltip("Segundos que tarda en cambiar del día a la noche.")]
        [Range(0.5f, 20f)] public float cambio = 6f;

        AudioSource _dia, _noche, _viento;
        float _nivelDia, _nivelNoche;
        bool _activo;

        void Start()
        {
            var c = ConfiguracionDePartida.Actual;
            _activo = catalogo != null && (c == null || c.modo != ModoDePartida.Menu);
            if (!_activo) return;

            _dia = Bucle("Ambiente_Dia", CatalogoDeMusica.Primera(catalogo.dia));
            _noche = Bucle("Ambiente_Noche", CatalogoDeMusica.Primera(catalogo.noche));
            _viento = Bucle("Ambiente_Viento", CatalogoDeMusica.Primera(catalogo.viento));
            _nivelDia = EsDeNoche() ? 0f : 1f;
            _nivelNoche = 1f - _nivelDia;
        }

        AudioSource Bucle(string nombre, AudioClip clip)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var f = go.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.spatialBlend = 0f;
            f.loop = true;
            f.clip = clip;
            f.volume = 0f;
            if (clip != null)
            {
                // Cada bucle empieza en un punto distinto: si no, al arrancar todos a la vez
                // se nota el corte de los tres al mismo tiempo cada vuelta.
                f.time = Random.Range(0f, clip.length * 0.9f);
                f.Play();
            }
            return f;
        }

        static bool EsDeNoche()
        {
            var ciclo = CicloDelDia.Actual;
            return ciclo != null && ciclo.isActiveAndEnabled && ciclo.Hora == HoraDelDia.Noche;
        }

        void Update()
        {
            if (!_activo) return;

            bool noche = EsDeNoche();
            float dt = Time.unscaledDeltaTime;
            _nivelDia = ReglasDeMusica.Fundir(_nivelDia, noche ? 0f : 1f, cambio, dt);
            _nivelNoche = ReglasDeMusica.Fundir(_nivelNoche, noche ? 1f : 0f, cambio, dt);

            float v = Mezclador.Volumen(Canal.Ambiente);
            _dia.volume = v * catalogo.volumenDia * _nivelDia;
            _noche.volume = v * catalogo.volumenNoche * _nivelNoche;
            _viento.volume = v * catalogo.volumenViento;
        }
    }
}
