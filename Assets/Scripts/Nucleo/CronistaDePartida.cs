using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Toma muestras durante la partida y, al acabar, guarda su informe para el historial
    /// (semana 16).
    /// </summary>
    /// <remarks>
    /// No cuenta nada por su cuenta: las cifras ya las lleva <see cref="EstadisticasPartida"/>
    /// desde la semana 08 para el cartel final. Aquí solo se añade lo que el cartel no
    /// necesitaba —las muestras de oro y madera de cada bando— y se escribe todo a disco.
    /// <para>
    /// Se guarda también si el jugador se va al menú a mitad de partida, como abandonada:
    /// <c>OnDestroy</c> salta al cambiar de escena y al cerrar el juego. Menos de un minuto
    /// no se guarda, para que entrar y salir no llene el historial de partidas vacías.
    /// </para>
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Cronista de partida")]
    public class CronistaDePartida : MonoBehaviour
    {
        const float MinimoParaGuardar = 60f;

        readonly InformeDePartida _informe = new InformeDePartida();
        ConfiguracionDePartida _config;
        ArbitroDePartida _arbitro;
        float _proximaMuestra;
        bool _guardado;

        /// <summary>Ruta del último informe guardado, para el diario y las pruebas de la IA.</summary>
        public static string UltimaRuta { get; private set; }

        void Start()
        {
            _config = ConfiguracionDePartida.Actual;
            _arbitro = ArbitroDePartida.Actual;
            if (_config == null) { enabled = false; return; }

            _informe.modo = _config.modo;
            _informe.jugador = _config.Jugador;
            _informe.fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            _informe.mapa = _config.mapa != null ? _config.mapa.nombre
                : Mundo.MundoJuego.Actual != null && Mundo.MundoJuego.Actual.definicion != null
                    ? Mundo.MundoJuego.Actual.definicion.nombreMapa : "Mapa";

            for (int b = 0; b < _config.bases.Length; b++)
            {
                if (_config.bases[b] == Ocupante.Cerrada) continue;
                _informe.bandos.Add(new InformeDeBando
                {
                    bando = b,
                    ocupante = _config.bases[b],
                    color = b < _config.colores.Length ? _config.colores[b] : b,
                });
            }

            if (_arbitro != null) _arbitro.AlTerminar += AlTerminar;
        }

        void OnDestroy()
        {
            if (_arbitro != null) _arbitro.AlTerminar -= AlTerminar;
            if (!_guardado && enabled && Duracion >= MinimoParaGuardar) Guardar();
        }

        // Al cerrar el juego también: en Linux el OnDestroy no siempre llega a correr al salir
        // con la partida a medias, y el informe se perdía (semana 17).
        void OnApplicationQuit()
        {
            if (!_guardado && enabled && Duracion >= MinimoParaGuardar) Guardar();
        }

        void AlTerminar(int ganador) => Guardar();

        static float Duracion => EstadisticasPartida.Actual != null ? EstadisticasPartida.Actual.Duracion : 0f;

        void Update()
        {
            if (_guardado || Time.time < _proximaMuestra) return;
            _proximaMuestra = Time.time + InformeDePartida.PasoDeMuestra;
            Muestrear();
        }

        void Muestrear()
        {
            var censo = Poblacion.Actual;
            var eco = Economia.Actual;
            var libro = EstadisticasPartida.Actual;

            foreach (var b in _informe.bandos)
            {
                if (libro != null && libro.Hoja(b.bando).EliminadoEn >= 0f) continue;
                b.poblacion.Add(censo != null ? censo.Usada(b.bando) : 0);
                b.recolectado.Add(libro != null ? libro.Hoja(b.bando).RecolectadoTotal : 0);
                b.oro.Add(eco != null ? eco.Cantidad(b.bando, TipoRecurso.Oro) : 0);
                b.madera.Add(eco != null ? eco.Cantidad(b.bando, TipoRecurso.Madera) : 0);
            }
        }

        void Guardar()
        {
            _guardado = true;
            Muestrear();

            var libro = EstadisticasPartida.Actual;
            _informe.duracion = Duracion;
            _informe.ganador = _arbitro != null ? _arbitro.Ganador : -1;
            _informe.desenlace = _arbitro != null && _informe.jugador >= 0
                ? _arbitro.DesenlacePara(_informe.jugador) : Desenlace.EnJuego;
            _informe.abandonada = _informe.desenlace == Desenlace.EnJuego;
            _informe.botEnElJugador = _informe.jugador >= 0 && IA.CerebroIA.De(_informe.jugador) != null &&
                                      !PartidasAutomaticas.ComoJugador;
            _informe.demostracion = PartidasAutomaticas.ComoJugador;
            _informe.repartoPorReservas = IA.PlanDeIA.RepartoPorReservas;

            var eliminados = new float[_informe.bandos.Count];
            for (int i = 0; i < _informe.bandos.Count; i++)
            {
                var b = _informe.bandos[i];
                if (libro != null) Copiar(libro.Hoja(b.bando), b);
                eliminados[i] = b.eliminadoEn;
            }

            int ganadorEnLista = _informe.bandos.FindIndex(b => b.bando == _informe.ganador);
            var puestos = InformeDePartida.Puestos(eliminados, ganadorEnLista);
            for (int i = 0; i < puestos.Length; i++) _informe.bandos[i].puesto = puestos[i];

            UltimaRuta = HistorialDePartidas.Guardar(_informe);
            DiarioDePartida.Anotar($"Informe de partida guardado en {UltimaRuta}");
        }

        static void Copiar(HojaDeBando h, InformeDeBando b)
        {
            b.eliminadoEn = h.EliminadoEn;
            b.oroRecolectado = h.OroRecolectado;
            b.maderaRecolectada = h.MaderaRecolectada;
            b.carneRecolectada = h.CarneRecolectada;
            b.oroGastado = h.OroGastado;
            b.maderaGastada = h.MaderaGastada;
            b.unidadesEntrenadas = h.UnidadesEntrenadas;
            b.unidadesPerdidas = h.UnidadesPerdidas;
            b.unidadesEliminadas = h.UnidadesEliminadas;
            b.edificiosConstruidos = h.EdificiosConstruidos;
            b.edificiosPerdidos = h.EdificiosPerdidos;
            b.edificiosDestruidos = h.EdificiosDestruidos;
            b.picoDePoblacion = h.PicoDePoblacion;
            b.ordenes = h.Ordenes;
            b.primerCombate = h.PrimerCombate;
            b.mvp = h.Mvp;
            b.bajasDelMvp = h.BajasDelMvp;
        }
    }
}
