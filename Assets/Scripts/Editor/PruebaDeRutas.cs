using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using TinyTactics.Mundo;
using Debug = UnityEngine.Debug;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Banco de pruebas del pathfinding. Construye la grilla y lanza N búsquedas entre
    /// puntos transitables al azar, midiendo tiempos.
    ///
    /// Sirve para dos cosas: verificar que el A* encuentra camino donde debe, y tener
    /// un número real que enseñar en la sustentación en vez de decir "va rápido".
    /// </summary>
    public static class PruebaDeRutas
    {
        [MenuItem("Tiny Tactics/Probar pathfinding", false, 20)]
        public static void Probar()
        {
            var mundo = Object.FindFirstObjectByType<MundoJuego>();
            if (mundo == null)
            {
                EditorUtility.DisplayDialog(
                    "Tiny Tactics",
                    "No hay ningún objeto 'Mundo' en la escena.\n" +
                    "Genera la escena con Tiny Tactics → Generar escena de juego.",
                    "Entendido");
                return;
            }

            mundo.Construir();

            if (mundo.Grilla == null)
            {
                Debug.LogError("[Tiny Tactics] La grilla no se pudo construir. Revisa la consola.", mundo);
                return;
            }

            // Dejar el mundo seleccionado y el gizmo activo: la grilla es invisible
            // por naturaleza y sin esto no hay nada que mirar en la escena.
            mundo.mostrarGrilla = true;
            Selection.activeGameObject = mundo.gameObject;

            var grilla = mundo.Grilla;
            var rnd = new System.Random(1234);

            const int intentos = 200;
            int conRuta = 0, sinRuta = 0, descartados = 0;
            long pasosTotales = 0;
            double msTotal = 0, msPeor = 0;

            var cronometro = new Stopwatch();

            for (int i = 0; i < intentos; i++)
            {
                if (!PuntoTransitable(grilla, rnd, out var desde) ||
                    !PuntoTransitable(grilla, rnd, out var hasta))
                {
                    descartados++;
                    continue;
                }

                cronometro.Restart();
                var ruta = mundo.Rutas.Buscar(desde, hasta);
                cronometro.Stop();

                double ms = cronometro.Elapsed.TotalMilliseconds;
                msTotal += ms;
                if (ms > msPeor) msPeor = ms;

                if (ruta.Count > 0)
                {
                    conRuta++;
                    pasosTotales += ruta.Count;
                }
                else
                {
                    sinRuta++;
                }
            }

            int validos = conRuta + sinRuta;
            Debug.Log(
                $"[Tiny Tactics] Pathfinding — {validos} búsquedas sobre {grilla.Ancho}x{grilla.Alto}\n" +
                $"Con ruta: {conRuta} · sin ruta: {sinRuta} · descartadas: {descartados}\n" +
                $"Media: {msTotal / Mathf.Max(1, validos):F2} ms · peor caso: {msPeor:F2} ms\n" +
                $"Longitud media: {(conRuta > 0 ? pasosTotales / (float)conRuta : 0):F0} celdas\n" +
                $"Transitables: {grilla.ContarTransitables()} de {grilla.Ancho * grilla.Alto}");
        }

        static bool PuntoTransitable(GrillaMapa grilla, System.Random rnd, out Vector2Int celda)
        {
            for (int i = 0; i < 200; i++)
            {
                int x = rnd.Next(grilla.Ancho);
                int y = rnd.Next(grilla.Alto);
                if (grilla.Transitable(x, y))
                {
                    celda = new Vector2Int(x, y);
                    return true;
                }
            }

            celda = default;
            return false;
        }
    }
}
