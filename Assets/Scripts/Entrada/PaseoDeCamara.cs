using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Nucleo;

namespace TinyTactics.Entrada
{
    /// <summary>
    /// La cámara del fondo del menú: pasea sola por donde está pasando algo.
    /// </summary>
    /// <remarks>
    /// Alterna entre dos tipos de plano: una base, para que se vea crecer, y la unidad
    /// militar que más lejos esté de su casa, que casi siempre es la que va a pelear. Un
    /// paseo por puntos fijos enseñaría hierba la mitad del tiempo; seguir la acción es lo
    /// que hace que el fondo parezca un juego y no un salvapantallas.
    ///
    /// Se mueve con tiempo sin escalar: el fondo corre más rápido que una partida, pero la
    /// cámara no tiene por qué marearse con él.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Paseo de cámara")]
    public class PaseoDeCamara : MonoBehaviour
    {
        [Range(4f, 30f)] public float segundosPorPlano = 11f;
        [Range(0.5f, 6f)] public float suavidad = 2.6f;
        [Range(6f, 20f)] public float zoom = 9.5f;

        Camera _camara;
        Vector3 _destino;
        Vector3 _velocidad;
        float _proximoPlano;
        int _plano;

        void OnEnable()
        {
            _camara = GetComponent<Camera>();
            _destino = transform.position;
            _proximoPlano = 0f;
        }

        void Update()
        {
            if (Time.unscaledTime >= _proximoPlano)
            {
                _proximoPlano = Time.unscaledTime + segundosPorPlano;
                ElegirPlano();
            }

            float dt = Time.unscaledDeltaTime;
            var pos = Vector3.SmoothDamp(transform.position, _destino, ref _velocidad, suavidad,
                                         Mathf.Infinity, dt);
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);

            if (_camara != null)
                _camara.orthographicSize = Mathf.Lerp(_camara.orthographicSize, zoom, 1f - Mathf.Exp(-dt));
        }

        void ElegirPlano()
        {
            _plano++;
            Vector2 punto;

            if (_plano % 2 == 0 && UnidadMasAdelantada(out punto)) Ir(punto);
            else if (BaseAlAzar(out punto)) Ir(punto);
        }

        void Ir(Vector2 punto) => _destino = new Vector3(punto.x, punto.y, transform.position.z);

        static bool BaseAlAzar(out Vector2 punto)
        {
            punto = default;
            var casas = Edificio.Todos;
            int vistos = 0;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || !e.centroDeEntrega) continue;

                vistos++;
                if (Random.Range(0, vistos) == 0) punto = e.PuntoDeEntrega;
            }

            return vistos > 0;
        }

        static bool UnidadMasAdelantada(out Vector2 punto)
        {
            punto = default;
            float mejor = 0f;
            var todas = RegistroDeUnidades.Todas;

            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva || u.datos == null || u.datos.tipo == TipoUnidad.Pawn) continue;

                var casa = IA.CerebroIA.De(u.faccion)?.Base();
                if (casa == null) continue;

                float d = ((Vector2)u.transform.position - (Vector2)casa.PuntoDeEntrega).sqrMagnitude;
                if (d <= mejor) continue;

                mejor = d;
                punto = u.transform.position;
            }

            return mejor > 0f;
        }
    }
}
