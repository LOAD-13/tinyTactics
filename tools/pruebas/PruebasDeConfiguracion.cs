// Pruebas de la configuracion de partida (semana 12).
//
// Lo que se comprueba es lo que decide la pantalla de preparar partida: que no se pueda
// empezar sin jugador ni sin rival, que nunca haya dos jugadores, y que el boton de cada
// base recorra todos los ocupantes y vuelva al principio.
using TinyTactics.IA;
using TinyTactics.Nucleo;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeConfiguracion
    {
        static Ocupante[] B(params Ocupante[] o) => o;

        public static void Registrar()
        {
            Arranque.Registrar("partida: por defecto, tu en la primera base y bots en Normal", v =>
            {
                for (int bandos = 2; bandos <= 5; bandos++)
                {
                    var c = ConfiguracionDePartida.PorDefecto(bandos, "Juego");
                    v.Igual(c.bases.Length, bandos, $"{bandos} bandos: una base por bando");
                    v.Igual(c.Jugador, 0, $"{bandos} bandos: el jugador en la primera");
                    for (int i = 1; i < bandos; i++)
                        v.Cierto(c.bases[i] == Ocupante.BotNormal, $"{bandos} bandos: base {i} bot Normal");
                    v.Cierto(ReglasDeConfiguracion.EsValida(c.bases, out _), $"{bandos} bandos: se puede empezar");
                }
            });

            Arranque.Registrar("partida: el fondo del menu no tiene jugador", v =>
            {
                var c = ConfiguracionDePartida.DeMenu(3, "Juego");
                v.Igual(c.Jugador, -1, "sin jugador");
                v.Cierto(c.modo == ModoDePartida.Menu, "modo menu");
                foreach (var b in c.bases) v.Cierto(ReglasDeConfiguracion.EsBot(b), "todas bots");
            });

            Arranque.Registrar("partida: no se empieza sin jugador, con dos, ni sin rival", v =>
            {
                v.Falso(ReglasDeConfiguracion.EsValida(B(Ocupante.BotNormal, Ocupante.BotNormal), out _), "sin jugador");
                v.Falso(ReglasDeConfiguracion.EsValida(B(Ocupante.Jugador, Ocupante.Jugador), out _), "dos jugadores");
                v.Falso(ReglasDeConfiguracion.EsValida(B(Ocupante.Jugador, Ocupante.Cerrada, Ocupante.Cerrada), out _), "sin rival");
                v.Cierto(ReglasDeConfiguracion.EsValida(B(Ocupante.Cerrada, Ocupante.Jugador, Ocupante.BotDificil), out _), "1v1 en mapa de tres");

                ReglasDeConfiguracion.EsValida(B(Ocupante.Jugador, Ocupante.Cerrada), out var motivo);
                v.Cierto(motivo.Length > 0, "cuando no se puede, dice por que");
            });

            Arranque.Registrar("partida: el boton de una base recorre los cinco y vuelve", v =>
            {
                var o = Ocupante.Jugador;
                var vistos = new System.Collections.Generic.HashSet<Ocupante>();
                for (int i = 0; i < 5; i++) { vistos.Add(o); o = ReglasDeConfiguracion.Siguiente(o); }

                v.Igual(vistos.Count, 5, "pasa por los cinco ocupantes");
                v.Cierto(o == Ocupante.Jugador, "al quinto clic vuelve al principio");
            });

            Arranque.Registrar("partida: sentarse en otra base nunca deja dos jugadores", v =>
            {
                var bases = B(Ocupante.Jugador, Ocupante.BotFacil, Ocupante.Cerrada);

                // Pulsar la base cerrada la convierte en «Tu»: el jugador se muda y su base
                // anterior pasa a bot.
                ReglasDeConfiguracion.Pulsar(bases, 2);
                v.Igual(ReglasDeConfiguracion.Jugador(bases), 2, "el jugador se muda a la base pulsada");
                v.Cierto(bases[0] == Ocupante.BotNormal, "la base que deja pasa a bot Normal");

                int jugadores = 0;
                foreach (var b in bases) if (b == Ocupante.Jugador) jugadores++;
                v.Igual(jugadores, 1, "un solo jugador");

                // Pulsar sobre «Tu» lo convierte en bot: no hay jugador y no se puede empezar.
                ReglasDeConfiguracion.Pulsar(bases, 2);
                v.Igual(ReglasDeConfiguracion.Jugador(bases), -1, "quitarse deja la partida sin jugador");
                v.Falso(ReglasDeConfiguracion.EsValida(bases, out _), "y entonces no se puede empezar");

                // Fuera de rango no rompe nada.
                ReglasDeConfiguracion.Pulsar(bases, 9);
                ReglasDeConfiguracion.Pulsar(null, 0);
                v.Cierto(true, "indices fuera de rango se ignoran");
            });

            Arranque.Registrar("partida: cada ocupante de bot tiene su dificultad", v =>
            {
                v.Cierto(ReglasDeConfiguracion.Nivel(Ocupante.BotFacil) == NivelDeIA.Facil, "facil");
                v.Cierto(ReglasDeConfiguracion.Nivel(Ocupante.BotNormal) == NivelDeIA.Normal, "normal");
                v.Cierto(ReglasDeConfiguracion.Nivel(Ocupante.BotDificil) == NivelDeIA.Dificil, "dificil");
                v.Falso(ReglasDeConfiguracion.EsBot(Ocupante.Jugador), "el jugador no es bot");
                v.Falso(ReglasDeConfiguracion.EsBot(Ocupante.Cerrada), "una base cerrada no es bot");
            });

            Arranque.Registrar("color: de fabrica cada base lleva el color de su numero", v =>
            {
                var c = ConfiguracionDePartida.PorDefecto(5, "Juego");
                for (int i = 0; i < 5; i++) v.Igual(c.colores[i], i, $"base {i}");
                v.Falso(ReglasDeConfiguracion.ColoresRepetidos(c.colores, c.bases), "sin repetidos");
            });

            Arranque.Registrar("color: el boton nunca da un color que ya lleva otra base abierta", v =>
            {
                var bases = B(Ocupante.Jugador, Ocupante.BotNormal, Ocupante.BotNormal);
                var colores = new[] { 0, 1, 2 };

                // Recorrer los colores de la base 0 una vuelta entera, en los dos sentidos.
                for (int paso = -1; paso <= 1; paso += 2)
                    for (int k = 0; k < 10; k++)
                    {
                        colores[0] = ReglasDeConfiguracion.SiguienteColor(colores, bases, 0, paso);
                        v.Cierto(colores[0] != 1 && colores[0] != 2, $"paso {paso}: salta los ocupados ({colores[0]})");
                        v.Entre(colores[0], 0, 4, "dentro de los cinco");
                    }

                // Con tres bases abiertas quedan tres colores para la primera: 0, 3 y 4.
                var vistos = new System.Collections.Generic.HashSet<int>();
                colores[0] = 0;
                for (int k = 0; k < 6; k++)
                {
                    colores[0] = ReglasDeConfiguracion.SiguienteColor(colores, bases, 0);
                    vistos.Add(colores[0]);
                }
                v.Igual(vistos.Count, 3, "recorre los tres libres");
            });

            Arranque.Registrar("color: una base cerrada no ocupa su color", v =>
            {
                var bases = B(Ocupante.Jugador, Ocupante.Cerrada, Ocupante.BotNormal);
                var colores = new[] { 0, 1, 2 };

                colores[0] = ReglasDeConfiguracion.SiguienteColor(colores, bases, 0);
                v.Igual(colores[0], 1, "el rojo de la base cerrada queda libre");
                v.Falso(ReglasDeConfiguracion.ColoresRepetidos(colores, bases), "y no cuenta como repetido");

                bases[1] = Ocupante.BotFacil;
                v.Cierto(ReglasDeConfiguracion.ColoresRepetidos(colores, bases), "al abrirla, si choca, se detecta");
            });

            Arranque.Registrar("partida: la copia no comparte las bases con la original", v =>
            {
                var a = ConfiguracionDePartida.PorDefecto(3, "Juego");
                var b = a.Copia();
                b.bases[1] = Ocupante.Cerrada;
                b.colores[1] = 4;
                b.sandbox.niebla = false;
                v.Cierto(a.bases[1] == Ocupante.BotNormal, "cambiar la copia no toca la original");
                v.Igual(a.colores[1], 1, "ni sus colores");
                v.Cierto(a.sandbox.niebla, "ni sus reglas de sandbox");
            });
        }
    }
}
