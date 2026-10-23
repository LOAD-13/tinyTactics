// Pruebas de la musica (semana 14).
//
// Lo que se comprueba es cuando cambia la musica, no como suena: que una escaramuza suelta
// no la dispare, que una pelea de verdad si, que no vaya y venga, y que un ataque a lo tuyo
// entre aunque no se vea.
using TinyTactics.Audio;
using TinyTactics.Nucleo;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeMusica
    {
        public static void Registrar()
        {
            Arranque.Registrar("musica: un golpe suelto no es una batalla", v =>
            {
                var r = new ReglasDeMusica();
                r.Golpe(0f);
                r.Golpe(3f);
                v.Falso(r.Actualizar(3f), "dos golpes en seis segundos no bastan");
                r.Golpe(20f);
                r.Golpe(30f);
                r.Golpe(40f);
                v.Falso(r.Actualizar(40f), "golpes espaciados tampoco");
            });

            Arranque.Registrar("musica: una pelea vista la pone y la calma la quita", v =>
            {
                var r = new ReglasDeMusica();
                for (int i = 0; i < 4; i++) r.Golpe(i);
                v.Cierto(r.Actualizar(3f), "cuatro golpes seguidos: combate");

                v.Cierto(r.Actualizar(12f), "nueve segundos de calma: sigue");
                v.Cierto(r.Actualizar(15f), "con calma pero sin el minimo: sigue");
                v.Falso(r.Actualizar(21f), "con calma y pasado el minimo: vuelve la de partida");
            });

            Arranque.Registrar("musica: no va y viene con cada golpe", v =>
            {
                var r = new ReglasDeMusica();
                for (int i = 0; i < 4; i++) r.Golpe(i);
                r.Actualizar(3f);
                // Golpes sueltos cada ocho segundos: nunca diez de calma, nunca sale.
                bool siempre = true;
                for (float t = 8f; t < 120f; t += 8f)
                {
                    r.Golpe(t);
                    if (!r.Actualizar(t + 1f)) siempre = false;
                }
                v.Cierto(siempre, "mientras dure la pelea, se queda");
            });

            Arranque.Registrar("musica: un ataque a lo tuyo entra aunque no se vea", v =>
            {
                var r = new ReglasDeMusica();
                r.Alarma(5f);
                v.Cierto(r.Actualizar(5f), "entra al momento");
                v.Cierto(r.Actualizar(20f), "y respeta el minimo");
                v.Falso(r.Actualizar(24f), "luego se calma");
            });

            Arranque.Registrar("musica: el fundido llega y no se pasa", v =>
            {
                float n = 0f;
                for (int i = 0; i < 10; i++) n = ReglasDeMusica.Fundir(n, 1f, 2f, 0.1f);
                v.Cerca(n, 0.5f, 0.001f, "en un segundo, a medias");
                for (int i = 0; i < 50; i++) n = ReglasDeMusica.Fundir(n, 1f, 2f, 0.1f);
                v.Cerca(n, 1f, 0f, "y se queda en uno");
                v.Cerca(ReglasDeMusica.Fundir(0.3f, 0f, 0f, 0.1f), 0f, 0f, "sin fundido, corte");
            });
        }
    }
}
