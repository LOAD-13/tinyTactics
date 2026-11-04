using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Nucleo
{
    /// <summary>Lo que hizo un bando, tal como queda guardado al acabar la partida.</summary>
    [System.Serializable]
    public class InformeDeBando
    {
        public int bando, color;
        public Ocupante ocupante;

        /// <summary>1 el ganador; después, el que más aguantó. Empatan los que seguían en pie.</summary>
        public int puesto;
        public float eliminadoEn = -1f;

        public int oroRecolectado, maderaRecolectada, carneRecolectada, oroGastado, maderaGastada;
        public int unidadesEntrenadas, unidadesPerdidas, unidadesEliminadas;
        public int edificiosConstruidos, edificiosPerdidos, edificiosDestruidos;
        public int picoDePoblacion, ordenes;
        public float primerCombate = -1f;
        public TipoUnidad mvp;
        public int bajasDelMvp;

        // Una muestra cada InformeDePartida.paso segundos: de aquí salen las gráficas.
        public List<int> poblacion = new List<int>();
        public List<int> recolectado = new List<int>();
        public List<int> oro = new List<int>();
        public List<int> madera = new List<int>();

        public int RecolectadoTotal => oroRecolectado + maderaRecolectada + carneRecolectada;
    }

    /// <summary>
    /// Una partida jugada, guardada en disco para el historial (semana 16).
    /// </summary>
    /// <remarks>
    /// Se guarda la <b>partida entera, todos los bandos</b>, y no solo la hoja del jugador:
    /// el detalle del historial enseña a los bots junto a ti, y las pruebas de la IA comparan
    /// bots entre sí con estos mismos archivos.
    /// </remarks>
    [System.Serializable]
    public class InformeDePartida
    {
        public int version = 1;
        public string fecha;
        public string mapa;
        public ModoDePartida modo;
        public float duracion;
        public int ganador = -1, jugador = -1;
        public Desenlace desenlace;

        /// <summary>Se fue al menú con la partida en marcha y sin haber caído.</summary>
        public bool abandonada;

        // Para las pruebas de la IA: una partida con bot en el bando del jugador no es suya,
        // y el reparto con que jugaron los bots decide con qué se compara.
        public bool botEnElJugador;
        public bool repartoPorReservas = true;

        /// <summary>Jugada sola por <see cref="PartidasAutomaticas"/> para el historial de ejemplo.</summary>
        public bool demostracion;

        public float paso = InformeDePartida.PasoDeMuestra;
        public List<InformeDeBando> bandos = new List<InformeDeBando>();

        public const float PasoDeMuestra = 10f;

        public InformeDeBando Bando(int b)
        {
            foreach (var x in bandos) if (x.bando == b) return x;
            return null;
        }

        public InformeDeBando DelJugador => Bando(jugador);

        /// <summary>
        /// Los puestos de cada bando: el ganador primero y el resto por cuánto aguantó.
        /// </summary>
        /// <remarks>
        /// Un bando que no llegó a caer (la partida se dejó a medias) va por delante de todos
        /// los eliminados y empata con los demás que siguen en pie.
        /// </remarks>
        public static int[] Puestos(float[] eliminadoEn, int ganador)
        {
            int n = eliminadoEn.Length;
            var valor = new float[n];
            for (int i = 0; i < n; i++)
                valor[i] = i == ganador ? float.MaxValue : eliminadoEn[i] < 0f ? float.MaxValue / 2f : eliminadoEn[i];

            var puestos = new int[n];
            for (int i = 0; i < n; i++)
            {
                int mejores = 0;
                for (int j = 0; j < n; j++) if (valor[j] > valor[i]) mejores++;
                puestos[i] = mejores + 1;
            }

            return puestos;
        }

        /// <summary>
        /// Segundos que un bando pasó con la economía torcida: sin un recurso y sobrado del otro.
        /// </summary>
        /// <remarks>
        /// Es la medida con la que se compara el reparto de pawns del bot (semana 16). Tener
        /// poco de todo es normal al gastar; lo que delata un mal reparto es quedarse sin oro
        /// con la madera amontonada, o al revés.
        /// </remarks>
        public static float SegundosDescompensado(IList<int> oro, IList<int> madera, float paso,
                                                  int falta = 50, int sobra = 250)
        {
            int muestras = 0;
            int n = Mathf.Min(oro.Count, madera.Count);
            for (int i = 0; i < n; i++)
                if ((oro[i] < falta && madera[i] >= sobra) || (madera[i] < falta && oro[i] >= sobra)) muestras++;

            return muestras * paso;
        }
    }

    /// <summary>Dónde viven los informes y cómo se leen.</summary>
    public static class HistorialDePartidas
    {
        public static string Carpeta => CarpetaForzada ?? Path.Combine(Application.persistentDataPath, "Historial");

        /// <summary>Otra carpeta para las partidas automáticas: así no se mezclan con las tuyas.</summary>
        public static string CarpetaForzada;
        public static string CarpetaDeEjemplo => Path.Combine(Application.streamingAssetsPath, "HistorialDeEjemplo");

        /// <summary>Cuántas partidas enseña el historial. En disco se guardan todas.</summary>
        public const int Maximo = 20;

        public static string Guardar(InformeDePartida informe)
        {
            try
            {
                Directory.CreateDirectory(Carpeta);
                string ruta = Path.Combine(Carpeta, System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".json");
                File.WriteAllText(ruta, JsonUtility.ToJson(informe));
                return ruta;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Tiny Tactics] No pude guardar el informe de partida: {e.Message}");
                return null;
            }
        }

        /// <summary>Las partidas más recientes primero. Un archivo roto se salta, no tumba la lista.</summary>
        public static List<InformeDePartida> Listar(int maximo = Maximo)
        {
            var lista = new List<InformeDePartida>();

            var archivos = new List<string>();
            if (Directory.Exists(Carpeta)) archivos.AddRange(Directory.GetFiles(Carpeta, "*.json"));
            // shortcut: el historial de ejemplo viaja con la build de la semana 16 para la
            // exposición; se borra la carpeta en la 17 y el juego sigue igual sin ella.
            if (Directory.Exists(CarpetaDeEjemplo)) archivos.AddRange(Directory.GetFiles(CarpetaDeEjemplo, "*.json"));

            // El nombre es la fecha, así que el orden alfabético es el cronológico.
            archivos.Sort((a, b) => string.CompareOrdinal(Path.GetFileName(b), Path.GetFileName(a)));

            foreach (var ruta in archivos)
            {
                if (lista.Count >= maximo) break;
                try
                {
                    var informe = JsonUtility.FromJson<InformeDePartida>(File.ReadAllText(ruta));
                    if (informe != null && informe.bandos != null && informe.bandos.Count > 0) lista.Add(informe);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Tiny Tactics] Informe ilegible {Path.GetFileName(ruta)}: {e.Message}");
                }
            }

            return lista;
        }
    }
}
