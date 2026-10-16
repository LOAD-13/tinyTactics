// Pruebas de las tres dificultades (semana 11).
//
// La regla que se comprueba por encima de todas: «mas dificil» es mas dificil en TODAS las
// palancas a la vez, nunca mas dificil en una y mas facil en otra. Y ninguna dificultad ve a
// traves de la niebla (ADR-23).
using TinyTactics.IA;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeDificultad
    {
        static readonly NivelDeIA[] Niveles = { NivelDeIA.Facil, NivelDeIA.Normal, NivelDeIA.Dificil };

        public static void Registrar()
        {
            Arranque.Registrar("ia: la oleada responde a quien tiene encima", v =>
            {
                var propias = new[] { new UnityEngine.Vector2(0, 0), new UnityEngine.Vector2(10, 0) };

                // Nadie cerca: sigue a lo suyo.
                v.Igual(PlanDeIA.ElegirAmenaza(propias, new[] { new UnityEngine.Vector2(30, 30) }, 6f),
                        -1, "enemigo lejos");
                v.Igual(PlanDeIA.ElegirAmenaza(propias, new UnityEngine.Vector2[0], 6f),
                        -1, "sin enemigos");

                // El que pega a la retaguardia cuenta aunque el centro del grupo quede lejos.
                v.Igual(PlanDeIA.ElegirAmenaza(propias,
                            new[] { new UnityEngine.Vector2(40, 0), new UnityEngine.Vector2(12, 1) }, 6f),
                        1, "el que esta encima de la retaguardia");

                // De dos amenazas, la mas cercana a cualquiera de los suyos.
                v.Igual(PlanDeIA.ElegirAmenaza(propias,
                            new[] { new UnityEngine.Vector2(4, 0), new UnityEngine.Vector2(9, 0) }, 6f),
                        1, "la mas pegada");

                // Justo en el borde del radio cuenta; un poco mas alla, no.
                v.Igual(PlanDeIA.ElegirAmenaza(propias, new[] { new UnityEngine.Vector2(0, 6) }, 6f),
                        0, "en el borde");
                v.Igual(PlanDeIA.ElegirAmenaza(propias, new[] { new UnityEngine.Vector2(0, 6.1f) }, 6f),
                        -1, "pasado el borde");
            });

            Arranque.Registrar("dificultad: Normal es el bot de la semana 10, sin tocar", v =>
            {
                var n = ReglasDeDificultad.Valores(NivelDeIA.Normal);
                v.Cerca(n.ventajaDeRecursos, 1f, 0f, "ventaja de Normal");
                v.Cerca(n.tamanoDeOleada, 1f, 0f, "oleada de Normal");
                v.Cerca(n.ritmo, 1f, 0f, "ritmo de Normal");
                v.Cerca(n.esperaInicial, 60f, 0f, "espera de Normal");
            });

            Arranque.Registrar("dificultad: cada nivel es mas duro en las cuatro palancas", v =>
            {
                for (int i = 1; i < Niveles.Length; i++)
                {
                    var antes = ReglasDeDificultad.Valores(Niveles[i - 1]);
                    var ahora = ReglasDeDificultad.Valores(Niveles[i]);
                    string par = $"{Niveles[i - 1]} -> {Niveles[i]}";

                    v.Cierto(ahora.ventajaDeRecursos > antes.ventajaDeRecursos, $"{par}: mas recursos");
                    v.Cierto(ahora.tamanoDeOleada > antes.tamanoDeOleada, $"{par}: oleadas mayores");
                    v.Cierto(ahora.esperaInicial < antes.esperaInicial, $"{par}: ataca antes");
                    v.Cierto(ahora.ritmo < antes.ritmo, $"{par}: decide mas rapido");
                }
            });

            Arranque.Registrar("dificultad: las oleadas crecen con el nivel en toda la partida", v =>
            {
                // Con el mismo plan, para cada oleada de la 0 a la 30, el nivel de arriba
                // nunca pide MENOS gente que el de abajo. Si el redondeo invirtiera el orden
                // en alguna oleada, Dificil atacaria flojo justo ahi.
                for (int oleada = 0; oleada <= 30; oleada++)
                    for (int i = 1; i < Niveles.Length; i++)
                    {
                        float a = ReglasDeDificultad.Valores(Niveles[i - 1]).tamanoDeOleada;
                        float b = ReglasDeDificultad.Valores(Niveles[i]).tamanoDeOleada;

                        int menos = PlanDeIA.TamanoDeOleada(oleada, 5, 2, 16, a);
                        int mas = PlanDeIA.TamanoDeOleada(oleada, 5, 2, 16, b);

                        v.Cierto(mas >= menos, $"oleada {oleada}, {Niveles[i]} >= {Niveles[i - 1]}");
                        v.Cierto(menos >= 1, $"oleada {oleada}, {Niveles[i - 1]} pide al menos uno");
                    }
            });

            Arranque.Registrar("dificultad: la ventaja no se pierde redondeando cargas pequenas", v =>
            {
                foreach (var nivel in Niveles)
                {
                    float ventaja = ReglasDeDificultad.Valores(nivel).ventajaDeRecursos;

                    foreach (int carga in new[] { 1, 3, 5, 7, 10 })
                    {
                        float resto = 0f;
                        long total = 0;

                        for (int i = 0; i < 1000; i++)
                        {
                            int entra = ReglasDeDificultad.ConVentaja(carga, ventaja, ref resto);
                            v.Cierto(entra >= 0, $"{nivel}, carga {carga}: nunca negativo");
                            v.Entre(resto, 0f, 1f, $"{nivel}, carga {carga}: el resto es una fraccion");
                            total += entra;
                        }

                        v.Cerca(total, carga * 1000f * ventaja, 1.01f,
                                $"{nivel}, carga {carga}: mil cargas dan exactamente su multiplicador");
                    }
                }
            });

            Arranque.Registrar("dificultad: con ventaja 1 entra lo que se recoge, ni uno mas", v =>
            {
                float resto = 0f;
                for (int carga = 0; carga <= 50; carga++)
                    v.Igual(ReglasDeDificultad.ConVentaja(carga, 1f, ref resto), carga, $"carga {carga}");
            });

            Arranque.Registrar("dificultad: el ritmo nunca deja al bot pensando cada fotograma", v =>
            {
                for (float paso = 0f; paso <= 5f; paso += 0.1f)
                    for (float ritmo = 0f; ritmo <= 3f; ritmo += 0.1f)
                        v.Cierto(ReglasDeDificultad.PasoEstrategico(paso, ritmo) >= 0.2f,
                                 $"paso {paso:0.0}, ritmo {ritmo:0.0}");
            });

            Arranque.Registrar("dificultad: ninguna ve a traves de la niebla", v =>
            {
                foreach (var nivel in Niveles)
                    v.Falso(ReglasDeDificultad.Valores(nivel).veTodoElMapa, $"{nivel} respeta la niebla");
            });

            Arranque.Registrar("dificultad: el boton del panel recorre los tres y vuelve", v =>
            {
                var n = NivelDeIA.Facil;
                n = ReglasDeDificultad.Siguiente(n); v.Cierto(n == NivelDeIA.Normal, "Facil -> Normal");
                n = ReglasDeDificultad.Siguiente(n); v.Cierto(n == NivelDeIA.Dificil, "Normal -> Dificil");
                n = ReglasDeDificultad.Siguiente(n); v.Cierto(n == NivelDeIA.Facil, "Dificil -> Facil");
            });
        }
    }
}
