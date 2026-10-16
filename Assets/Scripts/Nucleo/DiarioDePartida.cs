using System.IO;
using UnityEngine;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Un cuaderno de lo que pasa en la partida, escrito a un archivo de texto.
    /// </summary>
    /// <remarks>
    /// <b>Para poder discutir una partida con datos y no de memoria.</b> «El bot se fue
    /// cuando derribó el castillo» es una observación; el diario dice a qué segundo cayó,
    /// quién lo tiró, qué estaba haciendo cada ejército antes y después, y cuántos recursos
    /// tenía cada bando. Con eso un fallo de la IA se demuestra en vez de suponerse.
    ///
    /// Se reescribe al empezar cada partida, en <c>Logs/partida_ia.log</c> junto al
    /// proyecto (fuera de <c>Assets</c>, y git ya ignora los <c>.log</c>). Solo existe en
    /// el editor y en las builds de desarrollo: la build final no escribe nada.
    /// </remarks>
    public static class DiarioDePartida
    {
        static StreamWriter _escritor;
        static bool _fallo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar()
        {
            Cerrar();
            _fallo = false;
            Application.quitting -= Cerrar;
            Application.quitting += Cerrar;
        }

        /// <summary>Ruta del archivo. La enseña el panel para saber dónde mirar.</summary>
        public static string Ruta =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "partida_ia.log"));

        [System.Diagnostics.Conditional("UNITY_EDITOR"),
         System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Anotar(string texto)
        {
            if (_fallo) return;

            try
            {
                if (_escritor == null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
                    _escritor = new StreamWriter(Ruta, false) { AutoFlush = true };
                }

                _escritor.WriteLine($"[{FormatoReloj(Time.time)}] {texto}");
            }
            catch (System.Exception e)
            {
                // Un diario que no se puede escribir no puede romper la partida.
                _fallo = true;
                Debug.LogWarning($"No se pudo escribir el diario de partida: {e.Message}");
            }
        }

        static string FormatoReloj(float segundos)
        {
            int s = Mathf.FloorToInt(segundos);
            return $"{s / 60:00}:{s % 60:00}";
        }

        static void Cerrar()
        {
            if (_escritor == null) return;
            _escritor.Dispose();
            _escritor = null;
        }
    }
}
