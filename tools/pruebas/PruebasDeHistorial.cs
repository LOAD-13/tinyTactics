// Pruebas del informe de partida (semana 16): los puestos del marcador y la medida con la
// que se compara el reparto de pawns del bot.
using System.Collections.Generic;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeHistorial
    {

        static InformeDePartida Partida(Desenlace d, Ocupante rival, bool abandonada = false, TipoUnidad mvp = TipoUnidad.Arquero)
        {
            var i = new InformeDePartida { jugador = 0, desenlace = d, abandonada = abandonada, duracion = 600f };
            i.bandos.Add(new InformeDeBando { bando = 0, ocupante = Ocupante.Jugador, mvp = mvp, bajasDelMvp = 3 });
            i.bandos.Add(new InformeDeBando { bando = 1, ocupante = rival });
            return i;
        }

        static void RegistrarResumen()
        {
            Arranque.Registrar("historial: victorias, derrotas y racha desde la ultima", v =>
            {
                var lista = new List<InformeDePartida>
                {
                    Partida(Desenlace.Victoria, Ocupante.BotDificil),
                    Partida(Desenlace.EnJuego, Ocupante.BotNormal, abandonada: true),
                    Partida(Desenlace.Victoria, Ocupante.BotNormal),
                    Partida(Desenlace.Derrota, Ocupante.BotDificil),
                    Partida(Desenlace.Victoria, Ocupante.BotFacil),
                };
                var r = LecturaDeHistorial.Resumir(lista);
                v.Igual(r.Victorias, 3, "victorias");
                v.Igual(r.Derrotas, 1, "derrotas");
                v.Igual(r.Abandonadas, 1, "abandonadas, aparte");
                v.Igual(r.Racha, 2, "dos seguidas: la abandonada no corta");
                v.Cerca(r.PorcentajeDeVictorias, 75f, 0.01f, "porcentaje sin las abandonadas");
                v.Igual(r.Jugadas[2], 2, "dos contra el dificil");
                v.Igual(r.Ganadas[2], 1, "una ganada al dificil");
                v.Igual((int)r.UnidadEstrella, (int)TipoUnidad.Arquero, "unidad estrella");
            });

            Arranque.Registrar("historial: racha negativa y las pruebas de bots no cuentan", v =>
            {
                var prueba = Partida(Desenlace.Victoria, Ocupante.BotDificil);
                prueba.botEnElJugador = true;
                var lista = new List<InformeDePartida>
                {
                    prueba,
                    Partida(Desenlace.Derrota, Ocupante.BotNormal),
                    Partida(Desenlace.Derrota, Ocupante.BotNormal),
                    Partida(Desenlace.Victoria, Ocupante.BotNormal),
                };
                var r = LecturaDeHistorial.Resumir(lista);
                v.Igual(r.Racha, -2, "dos derrotas seguidas");
                v.Igual(r.Victorias, 1, "la prueba no suma");
                v.Igual(LecturaDeHistorial.Resumir(new List<InformeDePartida>()).Racha, 0, "sin partidas, sin racha");
                var libre = Partida(Desenlace.Victoria, Ocupante.BotFacil);
                libre.modo = ModoDePartida.Sandbox;
                v.Igual(LecturaDeHistorial.Resumir(new List<InformeDePartida> { libre }).Victorias, 0, "el sandbox no cuenta");
            });

            Arranque.Registrar("rendimiento: media, 1 % lento y peor fotograma", v =>
            {
                var ms = new List<float>();
                for (int k = 0; k < 99; k++) ms.Add(10f);
                ms.Add(50f);
                MedidorDeRendimiento.Resumir(ms, out float media, out float lento, out float peor);
                v.Cerca(media, 1000f * 100 / 1040f, 0.01f, "media");
                v.Cerca(lento, 20f, 0.01f, "el 1 %: un fotograma de 50 ms");
                v.Cerca(peor, 50f, 0.01f, "peor");
            });

            Arranque.Registrar("historial: claves de la partida", v =>
            {
                var i = Partida(Desenlace.Derrota, Ocupante.BotDificil);
                i.paso = 10f;
                i.bandos[0].oroRecolectado = 1000; i.bandos[0].primerCombate = 250f;
                i.bandos[0].poblacion.AddRange(new[] { 5, 12, 30, 22 });
                i.bandos[1].oroRecolectado = 2000;
                var c = LecturaDeHistorial.Claves(i);
                v.Igual(c.Count, 3, "tres frases");
                v.Igual(c[0], "Recolectaste 100 por minuto; el mejor rival, 200.", "ritmo");
                v.Igual(c[1], "Tu primer combate fue al 4:10.", "primer combate");
                v.Igual(c[2], "Tu mayor población: 30, al 0:20.", "pico");
                v.Igual(LecturaDeHistorial.Reloj(3725f), "62:05", "reloj");
                v.Igual(LecturaDeHistorial.Formato(i), "1 contra 1", "formato");
            });
        }

        public static void Registrar()
        {
            RegistrarResumen();

            Arranque.Registrar("historial: el ganador primero, el resto por cuanto aguanto", v =>
            {
                var p = InformeDePartida.Puestos(new[] { 300f, -1f, 120f, 500f }, 1);
                v.Igual(p[1], 1, "ganador");
                v.Igual(p[3], 2, "cayo el ultimo");
                v.Igual(p[0], 3, "cayo en medio");
                v.Igual(p[2], 4, "cayo el primero");
            });

            Arranque.Registrar("historial: en una partida abandonada empatan los que seguian en pie", v =>
            {
                var p = InformeDePartida.Puestos(new[] { -1f, 200f, -1f }, -1);
                v.Igual(p[0], 1, "en pie");
                v.Igual(p[2], 1, "en pie, empatado");
                v.Igual(p[1], 3, "el eliminado, detras de los dos");
            });

            Arranque.Registrar("historial: la economia torcida se mide en segundos", v =>
            {
                var oro    = new[] { 20, 30, 400, 100, 10 };
                var madera = new[] { 300, 500, 10, 100, 20 };
                // Sin oro y sobrado de madera (2), sin madera y sobrado de oro (1); lo demas no cuenta.
                v.Cerca(InformeDePartida.SegundosDescompensado(oro, madera, 10f), 30f, 0.001f, "tres muestras");
                v.Cerca(InformeDePartida.SegundosDescompensado(new int[0], new int[0], 10f), 0f, 0.001f, "sin muestras");
            });
        }
    }
}
