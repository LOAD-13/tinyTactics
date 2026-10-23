// Pruebas de la eleccion de rival en el todos contra todos (semana 14).
//
// Con un solo rival no hay nada que elegir y el bot se porta como en el 1v1 de siempre. Con
// varios, lo que se comprueba es que cada motivo empuja hacia donde debe y que el reparto
// evita que todos los bots vayan a por el mismo.
using System.Collections.Generic;
using TinyTactics.IA;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeRivales
    {
        static PlanDeIA.CandidatoARival C(int f, float d, int e, float r = 0f, int ya = 0) =>
            new PlanDeIA.CandidatoARival { faccion = f, distancia = d, edificios = e, rencor = r, yaLoAtacan = ya };

        public static void Registrar()
        {
            Arranque.Registrar("rival: sin nadie conocido no hay rival; con uno, ese", v =>
            {
                v.Igual(PlanDeIA.ElegirRival(new List<PlanDeIA.CandidatoARival>()), -1, "nadie a la vista");
                v.Igual(PlanDeIA.ElegirRival(new[] { C(3, 120f, 9) }), 3, "en el 1v1, el unico");
            });

            Arranque.Registrar("rival: a igualdad, el mas cercano y el mas debil", v =>
            {
                v.Igual(PlanDeIA.ElegirRival(new[] { C(1, 100f, 5), C(2, 40f, 5) }), 2, "el cercano");
                v.Igual(PlanDeIA.ElegirRival(new[] { C(1, 60f, 9), C(2, 60f, 3) }), 2, "el debil");
            });

            Arranque.Registrar("rival: quien me ataca pasa delante", v =>
            {
                var sin = new[] { C(1, 50f, 5), C(2, 70f, 5) };
                v.Igual(PlanDeIA.ElegirRival(sin), 1, "sin rencor, el cercano");
                var con = new[] { C(1, 50f, 5), C(2, 70f, 5, r: 1f) };
                v.Igual(PlanDeIA.ElegirRival(con), 2, "con rencor fresco, el que me ataco");
            });

            Arranque.Registrar("rival: el rencor se olvida", v =>
            {
                v.Cerca(PlanDeIA.Rencor(0f, 90f), 1f, 0.001f, "recien atacado");
                v.Cerca(PlanDeIA.Rencor(45f, 90f), 0.5f, 0.001f, "a la mitad");
                v.Cerca(PlanDeIA.Rencor(200f, 90f), 0f, 0.001f, "olvidado");
                v.Cerca(PlanDeIA.Rencor(-1f, 90f), 0f, 0.001f, "nunca");
            });

            Arranque.Registrar("rival: no van todos a por el mismo", v =>
            {
                // Cuatro bots, el jugador (0) es el mas cercano para todos.
                var elegidos = new int[5];
                int[] cuenta = new int[5];
                for (int bot = 1; bot <= 4; bot++)
                {
                    var c = new List<PlanDeIA.CandidatoARival>();
                    for (int f = 0; f <= 4; f++)
                    {
                        if (f == bot) continue;
                        c.Add(C(f, f == 0 ? 50f : 60f + 5f * ((f + bot) % 3), 5, ya: cuenta[f]));
                    }
                    elegidos[bot] = PlanDeIA.ElegirRival(c);
                    cuenta[elegidos[bot]]++;
                }
                v.Cierto(cuenta[0] < 4, $"al jugador no van los cuatro (van {cuenta[0]})");
                v.Cierto(cuenta[0] >= 1, "pero alguno si: es el mas cercano");
            });
        }
    }
}
