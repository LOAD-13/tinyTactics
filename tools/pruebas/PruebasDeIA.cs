// Pruebas de la IA rival (semana 10).
//
// Todo lo que el bot DECIDE esta escrito como logica pura para poder comprobarlo aqui:
// que paso del plan toca, como reparte los pawns, cuanto crece la oleada y donde cabe un
// edificio. Lo que el bot HACE con esas decisiones —emitir ordenes— vive en la escena y
// se prueba jugando.
using UnityEngine;
using TinyTactics.IA;
using TinyTactics.Mundo;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeIA
    {
        static PasoDelPlan Paso(Objetivo que, int cuantos) =>
            new PasoDelPlan { que = que, cantidad = cuantos };

        static int[] Inventario() => new int[System.Enum.GetValues(typeof(Objetivo)).Length];

        /// <summary>Grilla llana y despejada, del tamano que se pida.</summary>
        static GrillaMapa Llano(int lado)
        {
            var g = new GrillaMapa(lado, lado);

            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                    g[x, y] = new Celda { Suelo = true, Nivel = 0 };

            return g;
        }

        public static void Registrar()
        {
            // ---------------------------------------------------------- el plan
            Arranque.Registrar("plan: el primer paso sin cumplir es el que toca", v =>
            {
                var pasos = new[]
                {
                    Paso(Objetivo.Pawns, 5),
                    Paso(Objetivo.Casas, 1),
                    Paso(Objetivo.Cuarteles, 1),
                };

                var inv = Inventario();

                v.Igual(PlanDeIA.PasoActual(pasos, inv), 0, "sin nada, toca el primero");

                inv[(int)Objetivo.Pawns] = 5;
                v.Igual(PlanDeIA.PasoActual(pasos, inv), 1, "con los pawns, toca la casa");

                inv[(int)Objetivo.Casas] = 1;
                v.Igual(PlanDeIA.PasoActual(pasos, inv), 2, "con la casa, toca el cuartel");

                inv[(int)Objetivo.Cuarteles] = 1;
                v.Igual(PlanDeIA.PasoActual(pasos, inv), -1, "cumplido el plan, no hay paso");
            });

            Arranque.Registrar("plan: un objetivo se puede volver a incumplir", v =>
            {
                // Es la razon de que el plan sean objetivos y no acciones: si le matan dos
                // pawns, el bot vuelve a por ellos en vez de seguir adelante.
                var pasos = new[] { Paso(Objetivo.Pawns, 5), Paso(Objetivo.Casas, 1) };

                var inv = Inventario();
                inv[(int)Objetivo.Pawns] = 5;
                inv[(int)Objetivo.Casas] = 1;

                v.Igual(PlanDeIA.PasoActual(pasos, inv), -1, "de entrada, cumplido");

                inv[(int)Objetivo.Pawns] = 3;
                v.Igual(PlanDeIA.PasoActual(pasos, inv), 0, "al perder pawns, vuelve a por ellos");
            });

            Arranque.Registrar("plan: un plan vacio no rompe nada", v =>
            {
                v.Igual(PlanDeIA.PasoActual(new PasoDelPlan[0], Inventario()), -1, "plan vacio");
                v.Igual(PlanDeIA.PasoActual(null, Inventario()), -1, "plan nulo");
                v.Igual(PlanDeIA.PasoActual(new[] { Paso(Objetivo.Pawns, 1) }, null), -1,
                        "inventario nulo");
            });

            // ------------------------------------------------------- las oleadas
            Arranque.Registrar("oleadas: cada una es mayor que la anterior", v =>
            {
                int primera = PlanDeIA.TamanoDeOleada(0, 5, 2, 16, 1f);
                int segunda = PlanDeIA.TamanoDeOleada(1, 5, 2, 16, 1f);
                int tercera = PlanDeIA.TamanoDeOleada(2, 5, 2, 16, 1f);

                v.Igual(primera, 5, "la primera es la del plan");
                v.Igual(segunda, 7, "la segunda crece");
                v.Igual(tercera, 9, "y la tercera tambien");
            });

            Arranque.Registrar("oleadas: el tope evita que deje de atacar", v =>
            {
                // Sin tope, en la oleada veinte el numero se le va por encima de lo que
                // puede producir y el bot se queda juntando ejercito para siempre.
                int lejana = PlanDeIA.TamanoDeOleada(20, 5, 2, 16, 1f);

                v.Igual(lejana, 16, "no pasa del tope");
                v.Igual(PlanDeIA.TamanoDeOleada(0, 5, 2, 16, 0f), 1, "nunca baja de uno");
            });

            Arranque.Registrar("oleadas: la dificultad multiplica", v =>
            {
                v.Igual(PlanDeIA.TamanoDeOleada(0, 6, 2, 30, 0.5f), 3, "en facil, la mitad");
                v.Igual(PlanDeIA.TamanoDeOleada(0, 6, 2, 30, 1.5f), 9, "en dificil, una vez y media");
            });

            // ------------------------------------------------------- la economia
            Arranque.Registrar("economia: el reparto respeta las proporciones", v =>
            {
                PlanDeIA.Reparto(10, 4, 2, out int madera, out int carne, out int oro);

                v.Igual(madera, 4, "cuatro de cada diez a la madera");
                v.Igual(carne, 2, "dos de cada diez a la carne");
                v.Igual(oro, 4, "y el resto al oro");
            });

            Arranque.Registrar("economia: el reparto nunca pierde ni inventa pawns", v =>
            {
                for (int total = 0; total <= 20; total++)
                {
                    for (int m = 0; m <= 10; m++)
                    {
                        for (int c = 0; c <= 10; c++)
                        {
                            PlanDeIA.Reparto(total, m, c, out int ma, out int ca, out int or);

                            v.Igual(ma + ca + or, total,
                                    $"suman {total} con proporciones {m}/{c}");

                            v.Entre(ma, 0, total, "madera en rango");
                            v.Entre(ca, 0, total, "carne en rango");
                            v.Entre(or, 0, total, "oro en rango");
                        }
                    }
                }
            });

            Arranque.Registrar("economia: ningun recurso pedido se queda a cero", v =>
            {
                // Con cuatro pawns y dos de cada diez a la carne, redondear la deja en cero.
                // Desde que el hambre quita vida, cero en la carne es el bando muriendose.
                for (int total = 3; total <= 12; total++)
                {
                    PlanDeIA.Reparto(total, 4, 2, out int madera, out int carne, out int oro);

                    v.Cierto(madera >= 1, $"con {total} pawns hay alguien en la madera");
                    v.Cierto(carne >= 1, $"con {total} pawns hay alguien en la carne");
                    v.Cierto(oro >= 1, $"con {total} pawns hay alguien en el oro");
                }
            });

            Arranque.Registrar("economia: una proporcion de cero se respeta", v =>
            {
                PlanDeIA.Reparto(10, 5, 0, out int madera, out int carne, out int oro);

                v.Igual(carne, 0, "si el plan no pide carne, nadie va a la carne");
                v.Igual(madera + oro, 10, "y los diez se reparten entre los otros dos");
            });

            // ---------------------------------------------- cuando atacar de verdad
            Arranque.Registrar("ofensiva: con bastante tropa, se ataca", v =>
            {
                v.Cierto(PlanDeIA.HoraDeAtacar(5, 5, 2, 0f, 30f), "justo el numero pedido");
                v.Cierto(PlanDeIA.HoraDeAtacar(9, 5, 2, 0f, 30f), "de sobra");
                v.Falso(PlanDeIA.HoraDeAtacar(3, 5, 2, 0f, 30f), "todavia no");
                v.Falso(PlanDeIA.HoraDeAtacar(0, 5, 2, 999f, 30f), "sin ejercito no se ataca");
            });

            Arranque.Registrar("ofensiva: la espera NO puede ser infinita", v =>
            {
                // Este es el fallo que dejo al bot plantado delante de la base enemiga: cada
                // oleada pide mas gente que la anterior, y si se la matan tan rapido como la
                // produce, el numero no se alcanza JAMAS.
                v.Falso(PlanDeIA.HoraDeAtacar(6, 15, 2, 10f, 30f), "al principio espera");
                v.Cierto(PlanDeIA.HoraDeAtacar(6, 15, 2, 31f, 30f), "pero se cansa y ataca");
            });

            Arranque.Registrar("ofensiva: no se bloquea con NINGUN numero", v =>
            {
                // La comprobacion que de verdad protege la partida: se recorre cada tamano de
                // oleada que el plan puede llegar a pedir y se exige que, con la tropa
                // minima y la paciencia agotada, el bot acabe atacando. Si alguna
                // combinacion devolviera false para siempre, la partida no terminaria.
                for (int pedidas = 1; pedidas <= 50; pedidas++)
                {
                    for (int tropa = 2; tropa <= 20; tropa++)
                    {
                        bool ataca = PlanDeIA.HoraDeAtacar(tropa, pedidas, 2, 999f, 30f);

                        v.Cierto(ataca,
                                 $"con {tropa} de tropa, {pedidas} pedidas y la paciencia "
                                 + "agotada, el bot ataca");
                    }
                }
            });

            // --------------------------------------------------- donde construir
            Arranque.Registrar("sitio: encuentra el hueco mas cercano", v =>
            {
                var g = Llano(40);
                var centro = new Vector2Int(20, 20);

                bool hay = BuscadorDeSitio.Buscar(g, centro, new Vector2Int(3, 3), 3, 12,
                                                  out var celdas);

                v.Cierto(hay, "encuentra sitio en un llano");
                v.Igual(celdas.width, 3, "la planta mide lo pedido");
                v.Igual(celdas.height, 3, "en las dos dimensiones");

                float distancia = Vector2Int.Distance(
                    new Vector2Int(celdas.x + 1, celdas.y + 1), centro);

                v.Entre(distancia, 2f, 7f, "sale cerca de la base, no en cualquier parte");
            });

            Arranque.Registrar("sitio: respeta la separacion minima", v =>
            {
                var g = Llano(40);
                var centro = new Vector2Int(20, 20);

                BuscadorDeSitio.Buscar(g, centro, new Vector2Int(2, 2), 6, 12, out var celdas);

                int dx = Mathf.Abs(celdas.x + 1 - centro.x);
                int dy = Mathf.Abs(celdas.y + 1 - centro.y);

                v.Cierto(Mathf.Max(dx, dy) >= 5,
                         "con separacion 6 no planta pegado al castillo");
            });

            Arranque.Registrar("sitio: se rinde si no cabe nada", v =>
            {
                var g = Llano(40);

                // Todo obstaculo salvo el centro: no hay un solo hueco de 3x3.
                for (int y = 0; y < 40; y++)
                    for (int x = 0; x < 40; x++)
                        g[x, y] = new Celda { Suelo = true, Obstaculo = true };

                bool hay = BuscadorDeSitio.Buscar(g, new Vector2Int(20, 20),
                                                  new Vector2Int(3, 3), 2, 10, out _);

                v.Falso(hay, "no se inventa un sitio que no existe");
            });

            Arranque.Registrar("sitio: no deja una bolsa sin salida", v =>
            {
                // Lo que CerrariaElPaso sabe detectar es una BOLSA pequena que se queda sin
                // salida, no que el mapa se parta en dos: inunda desde el borde del edificio
                // y, si se le acaban las celdas antes del limite, esa zona quedo encerrada.
                // En un mapa de 224x224 cualquier region de verdad supera el limite, asi que
                // lo unico que puede quedar aislado es una bolsa — que es justo el caso que
                // arruina una partida.
                var g = Llano(60);

                // Un patio de 6x6 rodeado de muro, con una sola puerta.
                for (int y = 2; y <= 9; y++)
                    for (int x = 2; x <= 9; x++)
                        if (x == 2 || x == 9 || y == 2 || y == 9)
                            g[x, y] = new Celda { Suelo = true, Obstaculo = true };

                var puerta = new RectInt(9, 5, 1, 1);
                g[9, 5] = new Celda { Suelo = true };

                v.Cierto(g.CerrariaElPaso(puerta),
                         "tapar la puerta deja el patio encerrado");

                v.Falso(BuscadorDeSitio.Vale(g, puerta),
                        "asi que ese sitio no vale para construir");

                v.Cierto(BuscadorDeSitio.Vale(g, new RectInt(30, 30, 2, 2)),
                         "pero en campo abierto si vale");
            });

            Arranque.Registrar("sitio: la busqueda nunca devuelve un sitio invalido", v =>
            {
                // La comprobacion que de verdad protege la partida: sea cual sea el mapa,
                // lo que el buscador devuelva tiene que pasar las mismas reglas que se le
                // exigen al jugador. Se prueba sobre varios mapas con obstaculos repartidos.
                for (int semilla = 1; semilla <= 8; semilla++)
                {
                    var g = Llano(50);
                    var rnd = new System.Random(semilla);

                    for (int k = 0; k < 400; k++)
                    {
                        int x = rnd.Next(50), y = rnd.Next(50);
                        g[x, y] = new Celda { Suelo = true, Obstaculo = true };
                    }

                    var centro = new Vector2Int(25, 25);
                    g[25, 25] = new Celda { Suelo = true };

                    if (!BuscadorDeSitio.Buscar(g, centro, new Vector2Int(3, 3), 3, 20,
                                                out var celdas))
                        continue;

                    v.Cierto(BuscadorDeSitio.Vale(g, celdas),
                             $"el sitio devuelto con la semilla {semilla} es valido");
                }
            });
        }
    }
}
