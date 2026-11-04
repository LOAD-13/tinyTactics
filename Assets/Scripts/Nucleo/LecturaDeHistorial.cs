using System.Collections.Generic;
using TinyTactics.Datos;

namespace TinyTactics.Nucleo
{
    /// <summary>Lo que el historial cuenta de tus últimas partidas, todo junto.</summary>
    public class ResumenDeHistorial
    {
        public int Victorias, Derrotas, Abandonadas;

        /// <summary>Positiva si vienes ganando, negativa si vienes perdiendo. 0 sin partidas.</summary>
        public int Racha;

        /// <summary>Por dificultad del rival más duro: [0] Fácil, [1] Normal, [2] Difícil.</summary>
        public readonly int[] Jugadas = new int[3], Ganadas = new int[3];

        public TipoUnidad UnidadEstrella = TipoUnidad.Pawn;
        public int VecesEstrella;

        public int Terminadas => Victorias + Derrotas;
        public float PorcentajeDeVictorias => Terminadas == 0 ? 0f : 100f * Victorias / Terminadas;
    }

    /// <summary>
    /// Cómo se lee un informe de partida para el jugador (semana 16).
    /// </summary>
    /// <remarks>
    /// Va aparte de la pantalla, como lógica pura, por lo mismo que la IA: la interfaz solo
    /// pinta, y lo que dice —la racha, el porcentaje, las claves de una partida— se puede
    /// comprobar con las pruebas sin abrir Unity.
    /// </remarks>
    public static class LecturaDeHistorial
    {
        /// <summary>Una partida que cuenta para tu resumen: tuya y no una prueba de bots.</summary>
        public static bool EsDelJugador(InformeDePartida i) => i.jugador >= 0 && !i.botEnElJugador;

        public static string Reloj(float segundos)
        {
            int s = (int)System.Math.Max(0f, segundos);
            return $"{s / 60}:{s % 60:00}";
        }

        /// <summary>El rival más duro de la partida: es la dificultad con la que se recuerda.</summary>
        public static Ocupante Dificultad(InformeDePartida i)
        {
            var dura = Ocupante.Jugador;
            foreach (var b in i.bandos)
                if (ReglasDeConfiguracion.EsBot(b.ocupante) && b.ocupante > dura) dura = b.ocupante;

            return dura;
        }

        /// <summary>«1 contra 1», «todos contra todos (4)».</summary>
        public static string Formato(InformeDePartida i) =>
            i.bandos.Count <= 2 ? "1 contra 1" : $"todos contra todos ({i.bandos.Count})";

        /// <param name="recientes">La más reciente primero, como las devuelve el historial.</param>
        public static ResumenDeHistorial Resumir(IList<InformeDePartida> recientes)
        {
            var r = new ResumenDeHistorial();
            var estrellas = new Dictionary<TipoUnidad, int>();
            bool rachaCerrada = false;

            foreach (var i in recientes)
            {
                // El sandbox se enseña en la lista pero no cuenta: con recursos infinitos o
                // bots que no atacan, una victoria no dice nada de cómo juegas.
                if (!EsDelJugador(i) || i.modo == ModoDePartida.Sandbox) continue;

                if (i.abandonada) { r.Abandonadas++; continue; }

                bool gano = i.desenlace == Desenlace.Victoria;
                if (gano) r.Victorias++; else r.Derrotas++;

                // La racha se cuenta desde la última partida hacia atrás y se corta en la
                // primera que no siga el mismo signo. Las abandonadas no la cortan ni suman.
                if (!rachaCerrada)
                {
                    if (r.Racha == 0) r.Racha = gano ? 1 : -1;
                    else if (gano == r.Racha > 0) r.Racha += gano ? 1 : -1;
                    else rachaCerrada = true;
                }

                int nivel = (int)Dificultad(i) - (int)Ocupante.BotFacil;
                if (nivel >= 0 && nivel < 3)
                {
                    r.Jugadas[nivel]++;
                    if (gano) r.Ganadas[nivel]++;
                }

                var yo = i.DelJugador;
                if (yo != null && yo.bajasDelMvp > 0)
                {
                    estrellas.TryGetValue(yo.mvp, out int n);
                    estrellas[yo.mvp] = n + 1;
                }
            }

            foreach (var par in estrellas)
                if (par.Value > r.VecesEstrella) { r.VecesEstrella = par.Value; r.UnidadEstrella = par.Key; }

            return r;
        }

        /// <summary>
        /// Tres frases que explican la partida: cómo recolectaste comparado con el mejor,
        /// cuándo empezó la pelea y cuándo tuviste tu mayor ejército.
        /// </summary>
        /// <remarks>
        /// Es lo que convierte el marcador en algo que se aprende: no «perdiste», sino «el
        /// ganador recolectaba un tercio más que tú».
        /// </remarks>
        public static List<string> Claves(InformeDePartida i)
        {
            var claves = new List<string>();
            var yo = i.DelJugador;
            if (yo == null || i.duracion < 1f) return claves;

            float minutos = i.duracion / 60f;
            InformeDeBando mejor = null;
            foreach (var b in i.bandos)
                if (b != yo && (mejor == null || b.RecolectadoTotal > mejor.RecolectadoTotal)) mejor = b;

            int mio = (int)(yo.RecolectadoTotal / minutos);
            if (mejor == null || yo.RecolectadoTotal >= mejor.RecolectadoTotal)
                claves.Add($"Fuiste quien más recolectó: {mio} por minuto.");
            else
                claves.Add($"Recolectaste {mio} por minuto; el mejor rival, {(int)(mejor.RecolectadoTotal / minutos)}.");

            claves.Add(yo.primerCombate >= 0f
                ? $"Tu primer combate fue al {Reloj(yo.primerCombate)}."
                : "No llegaste a combatir.");

            int pico = -1, cuando = 0;
            for (int k = 0; k < yo.poblacion.Count; k++)
                if (yo.poblacion[k] > pico) { pico = yo.poblacion[k]; cuando = k; }
            if (pico > 0) claves.Add($"Tu mayor población: {pico}, al {Reloj(cuando * i.paso)}.");

            return claves;
        }
    }
}
