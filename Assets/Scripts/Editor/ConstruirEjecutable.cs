using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Construye el ejecutable de Windows desde la línea de comandos (semana 16).
    /// </summary>
    /// <remarks>
    /// Es lo mismo que File → Build Profiles → Build, pero sin abrir el editor: así las
    /// partidas automáticas se pueden lanzar sobre el código del momento.
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod
    ///     TinyTactics.EditorHerramientas.ConstruirEjecutable.Windows -salida "ruta\Tiny Tactics.exe"
    /// </code>
    /// </remarks>
    public static class ConstruirEjecutable
    {
        public static void Windows()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-salida");
            string salida = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Builds/Tiny Tactics.exe";

            var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            // -desarrollo: build de desarrollo, que escribe el diario de la IA (Logs/partida_ia.log).
            var opciones = Array.IndexOf(args, "-desarrollo") >= 0 ? BuildOptions.Development : BuildOptions.None;
            // -linux: la misma build para Linux de 64 bits (semana 17, para la release 1.0).
            var destino = Array.IndexOf(args, "-linux") >= 0 ? BuildTarget.StandaloneLinux64 : BuildTarget.StandaloneWindows64;
            var informe = BuildPipeline.BuildPlayer(escenas, salida, destino, opciones);

            Debug.Log($"[Tiny Tactics] Build: {informe.summary.result}, {informe.summary.totalSize / 1048576} MB, {salida}");
            if (informe.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
