using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TinyTactics.Nucleo;

namespace TinyTactics.Pruebas
{
    /// <summary>
    /// Mide el rendimiento durante un minuto y lo apunta (semana 16).
    /// </summary>
    /// <remarks>
    /// La prueba de carga de la semana 14 se midió en el editor, que ya pesa por sí solo. Esta
    /// se lanza desde el panel de pruebas en la build de entrega, sobre una partida real, y da
    /// las mismas tres cifras para poder compararlas: FPS de media, FPS del 1 % de fotogramas
    /// más lentos y el peor fotograma.
    /// </remarks>
    public class MedidorDeRendimiento : MonoBehaviour
    {
        public const float Duracion = 60f;

        public static MedidorDeRendimiento Actual { get; private set; }

        /// <summary>El último resultado, para el panel. Vacío si aún no se midió nada.</summary>
        public static string Resultado = "";

        readonly List<float> _ms = new List<float>(8000);
        float _restante;
        int _unidadesMax;

        public float Restante => _restante;

        public static void Empezar()
        {
            if (Actual != null) return;
            var go = new GameObject("MedidorDeRendimiento");
            Actual = go.AddComponent<MedidorDeRendimiento>();
            Actual._restante = Duracion;
        }

        void Update()
        {
            // Tiempo real: con el juego a x4 el reloj de la partida corre cuatro veces más.
            _ms.Add(Time.unscaledDeltaTime * 1000f);
            _unidadesMax = Mathf.Max(_unidadesMax, RegistroDeUnidades.Todas.Count);

            _restante -= Time.unscaledDeltaTime;
            if (_restante > 0f) return;

            Resumir(_ms, out float media, out float lento, out float peor);
            Resultado = $"{_unidadesMax} unidades · {media:0} FPS de media · 1 % lento {lento:0} · peor {peor:0} ms";

            try
            {
                string ruta = Path.Combine(HistorialDePartidas.CarpetaForzada ?? Application.persistentDataPath, "rendimiento.txt");
                File.AppendAllText(ruta, $"{System.DateTime.Now:yyyy-MM-dd HH:mm} · {Resultado}\n");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tiny Tactics] No pude guardar la medida: {e.Message}");
            }

            Actual = null;
            Destroy(gameObject);
        }

        /// <summary>
        /// Las tres cifras a partir de los tiempos de fotograma, en milisegundos.
        /// </summary>
        /// <remarks>
        /// El «1 % lento» son los FPS que saldrían si todos los fotogramas fueran como el 1 %
        /// más lento: es la cifra que delata los tirones que la media esconde.
        /// </remarks>
        public static void Resumir(List<float> ms, out float fpsMedia, out float fpsLento, out float peorMs)
        {
            fpsMedia = fpsLento = peorMs = 0f;
            if (ms.Count == 0) return;

            float suma = 0f;
            foreach (float m in ms) suma += m;
            fpsMedia = 1000f * ms.Count / suma;

            var orden = new List<float>(ms);
            orden.Sort();
            peorMs = orden[orden.Count - 1];

            int cuantos = Mathf.Max(1, orden.Count / 100);
            float lentos = 0f;
            for (int i = orden.Count - cuantos; i < orden.Count; i++) lentos += orden[i];
            fpsLento = 1000f * cuantos / lentos;
        }
    }
}
