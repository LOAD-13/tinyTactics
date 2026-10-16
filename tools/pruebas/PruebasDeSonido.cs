// Pruebas del audio (semana 11).
//
// Lo que se comprueba es lo que decide el audio: que toma suena, si puede sonar ya, cuanto
// se atenua con la distancia y que sonido corresponde a cada golpe de trabajo. Que el
// archivo suene bien no lo puede comprobar nadie mas que un oido.
using TinyTactics.Audio;
using TinyTactics.Datos;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeSonido
    {
        public static void Registrar()
        {
            Arranque.Registrar("sonido: nunca repite la toma anterior si hay otra", v =>
            {
                for (int cuantas = 2; cuantas <= 6; cuantas++)
                    for (int anterior = 0; anterior < cuantas; anterior++)
                        for (int k = 0; k <= 100; k++)
                        {
                            float azar = k / 100f;
                            int toma = ReglasDeSonido.ElegirToma(cuantas, anterior, azar);

                            v.Cierto(toma != anterior, $"{cuantas} tomas, antes {anterior}, azar {azar:0.00}");
                            v.Cierto(toma >= 0 && toma < cuantas, $"{cuantas} tomas: {toma} dentro de rango");
                        }
            });

            Arranque.Registrar("sonido: todas las tomas acaban sonando", v =>
            {
                // Saltarse la anterior no puede dejar ninguna toma fuera para siempre.
                for (int cuantas = 2; cuantas <= 6; cuantas++)
                    for (int anterior = 0; anterior < cuantas; anterior++)
                    {
                        var vistas = new bool[cuantas];
                        for (int k = 0; k < 200; k++)
                            vistas[ReglasDeSonido.ElegirToma(cuantas, anterior, k / 200f)] = true;

                        for (int t = 0; t < cuantas; t++)
                            if (t != anterior)
                                v.Cierto(vistas[t], $"{cuantas} tomas, antes {anterior}: sale la {t}");
                    }
            });

            Arranque.Registrar("sonido: con una sola toma, siempre esa", v =>
            {
                for (int anterior = -1; anterior <= 1; anterior++)
                    v.Igual(ReglasDeSonido.ElegirToma(1, anterior, 0.7f), 0, $"antes {anterior}");
            });

            Arranque.Registrar("sonido: respeta el tope de copias y la separacion", v =>
            {
                v.Cierto(ReglasDeSonido.PuedeSonar(1f, 0f, 0.5f, 0, 3), "libre y a tiempo");
                v.Falso(ReglasDeSonido.PuedeSonar(1f, 0.8f, 0.5f, 0, 3), "demasiado pronto");
                v.Falso(ReglasDeSonido.PuedeSonar(1f, 0f, 0.5f, 3, 3), "tope alcanzado");
                v.Falso(ReglasDeSonido.PuedeSonar(1f, 0f, 0f, 1, 0), "un tope de 0 cuenta como 1");

                // Treinta guerreros pegando en el mismo fotograma: suena uno, no treinta.
                float ultimo = -999f;
                int sonaron = 0;
                for (int i = 0; i < 30; i++)
                    if (ReglasDeSonido.PuedeSonar(5f, ultimo, 0.05f, sonaron, 4))
                    {
                        sonaron++;
                        ultimo = 5f;
                    }

                v.Igual(sonaron, 1, "treinta golpes en el mismo instante");
            });

            Arranque.Registrar("sonido: se apaga con la distancia y nunca sube", v =>
            {
                float antes = 1f;
                for (float d = 0f; d <= 3f; d += 0.01f)
                {
                    float a = ReglasDeSonido.Atenuacion(d, 0.75f, 1.6f);
                    v.Entre(a, 0f, 1f, $"distancia {d:0.00} dentro de [0,1]");
                    v.Cierto(a <= antes + 1e-5f, $"distancia {d:0.00}: no sube al alejarse");
                    antes = a;
                }

                v.Cerca(ReglasDeSonido.Atenuacion(0f, 0.75f, 1.6f), 1f, 0f, "en el centro, entero");
                v.Cerca(ReglasDeSonido.Atenuacion(1f, 0.75f, 1.6f), 0.8f, 0.2f, "en el borde, se oye");
                v.Cerca(ReglasDeSonido.Atenuacion(1.6f, 0.75f, 1.6f), 0f, 0f, "en el limite, mudo");
            });

            Arranque.Registrar("sonido: lo que ves suena entero y lo de fuera se apaga pronto", v =>
            {
                // Los valores de fabrica del componente (semana 11, tras probarlo): 1 y 1,25.
                v.Cerca(ReglasDeSonido.Atenuacion(0.99f, 1f, 1.25f), 1f, 0f, "dentro del recuadro");
                v.Cerca(ReglasDeSonido.Atenuacion(1f, 1f, 1.25f), 1f, 0f, "en el borde");

                float alLado = ReglasDeSonido.Atenuacion(1.1f, 1f, 1.25f);
                v.Cierto(alLado > 0f && alLado < 1f, $"justo al lado, flojito ({alLado:0.00})");

                v.Cerca(ReglasDeSonido.Atenuacion(1.3f, 1f, 1.25f), 0f, 0f, "pasado el margen, mudo");
            });

            Arranque.Registrar("sonido: el tono varia dentro de su margen", v =>
            {
                for (int k = 0; k <= 100; k++)
                    v.Entre(ReglasDeSonido.Tono(0.08f, k / 100f), 0.92f, 1.08f, $"azar {k / 100f:0.00}");

                v.Cerca(ReglasDeSonido.Tono(0f, 0.9f), 1f, 0f, "sin variacion, tono fijo");
            });

            Arranque.Registrar("sonido: cada trabajo suena a su herramienta", v =>
            {
                v.Cierto(ReglasDeSonido.SonidoDeTrabajo(false, TipoRecurso.Madera, out var s) &&
                         s == Sonido.HachaArbol, "madera -> hacha");
                v.Cierto(ReglasDeSonido.SonidoDeTrabajo(false, TipoRecurso.Oro, out s) &&
                         s == Sonido.PicoOro, "oro -> pico");
                v.Cierto(ReglasDeSonido.SonidoDeTrabajo(false, TipoRecurso.Carne, out s) &&
                         s == Sonido.OvejaBalido, "carne -> oveja");

                // El martillo gana aunque el pawn lleve encima el saco de madera: es la
                // misma prioridad que el dibujo (semana 06).
                v.Cierto(ReglasDeSonido.SonidoDeTrabajo(true, TipoRecurso.Madera, out s) &&
                         s == Sonido.MartilloObra, "obra con madera a cuestas -> martillo");

                v.Falso(ReglasDeSonido.SonidoDeTrabajo(false, TipoRecurso.Ninguno, out s),
                        "sin herramienta, sin sonido");
            });

            Arranque.Registrar("sonido: cada sonido tiene prefijo y tope en el catalogo de fabrica", v =>
            {
                var vistos = new System.Collections.Generic.HashSet<string>();

                foreach (Sonido s in System.Enum.GetValues(typeof(Sonido)))
                {
                    var e = CatalogoDeSonidos.Fabrica(s);
                    v.Cierto(!string.IsNullOrEmpty(e.prefijo), $"{s} tiene prefijo");
                    v.Cierto(e.maximoSimultaneo >= 1, $"{s} puede sonar al menos una vez");
                    v.Cierto(vistos.Add(e.prefijo ?? ""), $"{s}: prefijo no repetido");
                }
            });
        }
    }
}
