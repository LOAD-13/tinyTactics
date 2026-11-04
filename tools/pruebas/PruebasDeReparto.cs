// Pruebas del reparto de pawns por reservas (semana 16).
//
// El bot difícil perdió una partida sin oro y con la madera amontonada: el reparto era una
// proporción fija y solo se aplicaba a los pawns parados. Lo que se comprueba aquí es que la
// proporción se tuerce hacia lo que falta, que lo hace de forma gradual y que mover pawns
// ya ocupados tiene margen para no ir y venir.
using TinyTactics.IA;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeReparto
    {
        // Indices de TipoRecurso: 1 oro, 2 madera, 3 carne.
        static int[] R(int oro, int madera, int carne) => new[] { 0, oro, madera, carne };

        public static void Registrar()
        {
            Arranque.Registrar("reparto: con el almacen equilibrado manda el plan", v =>
            {
                v.Igual(PlanDeIA.MaderaSegunReservas(5, 200, 200), 5, "iguales");
                v.Igual(PlanDeIA.MaderaSegunReservas(5, 100, 240), 5, "menos de 150 de diferencia");
            });

            Arranque.Registrar("reparto: sin oro y con madera de sobra, menos al bosque", v =>
            {
                v.Igual(PlanDeIA.MaderaSegunReservas(5, 0, 160), 4, "un poco");
                v.Igual(PlanDeIA.MaderaSegunReservas(5, 0, 320), 3, "mas");
                v.Igual(PlanDeIA.MaderaSegunReservas(5, 0, 2000), 2, "como mucho tres de diez");
                v.Igual(PlanDeIA.MaderaSegunReservas(2, 0, 2000), 1, "nunca nadie en la madera no");
            });

            Arranque.Registrar("reparto: sin madera y con oro de sobra, mas al bosque", v =>
            {
                v.Igual(PlanDeIA.MaderaSegunReservas(5, 450, 0), 8, "tres mas");
                v.Igual(PlanDeIA.MaderaSegunReservas(7, 2000, 0), 8, "tope de ocho");
            });

            Arranque.Registrar("reparto: se mueve un pawn de donde sobran dos a donde falta", v =>
            {
                v.Cierto(PlanDeIA.QuienCambia(R(2, 6, 2), R(4, 4, 2), out int desde, out int hacia), "hay cambio");
                v.Igual(desde, 2, "desde la madera");
                v.Igual(hacia, 1, "hacia el oro");
            });

            Arranque.Registrar("reparto: con uno de margen no se mueve nadie", v =>
            {
                v.Falso(PlanDeIA.QuienCambia(R(3, 5, 2), R(4, 4, 2), out _, out _), "sobra uno solo");
                v.Falso(PlanDeIA.QuienCambia(R(4, 4, 2), R(4, 4, 2), out _, out _), "ya esta bien");
            });

            Arranque.Registrar("reparto: con la despensa llena, menos a la carne", v =>
            {
                v.Igual(PlanDeIA.CarneSegunReservas(2, 100), 2, "poca carne: el plan");
                v.Igual(PlanDeIA.CarneSegunReservas(2, 250), 1, "bastante: uno");
                v.Igual(PlanDeIA.CarneSegunReservas(2, 600), 0, "de sobra: ninguno");
                v.Igual(PlanDeIA.CarneSegunReservas(0, 250), 0, "nunca sube");
            });

            Arranque.Registrar("defensa: si son mas, se espera junto al castillo", v =>
            {
                v.Cierto(PlanDeIA.SalirADefender(0, 1, 0), "un explorador: se sale");
                v.Cierto(PlanDeIA.SalirADefender(5, 5, 0), "parejos: se sale");
                v.Cierto(PlanDeIA.SalirADefender(3, 5, 0), "tres contra cinco: se sale");
                v.Falso(PlanDeIA.SalirADefender(2, 5, 0), "dos contra cinco: se espera");
                v.Cierto(PlanDeIA.SalirADefender(1, 5, 1), "con una torre, uno vale por tres");
            });

            Arranque.Registrar("retirada: solo si la pelea esta claramente perdida", v =>
            {
                v.Cierto(PlanDeIA.HayQueRetirarse(5, 8), "cinco contra ocho: se retira");
                v.Falso(PlanDeIA.HayQueRetirarse(5, 7), "cinco contra siete: aguanta");
                v.Falso(PlanDeIA.HayQueRetirarse(2, 3), "uno de diferencia: aguanta");
                v.Cierto(PlanDeIA.HayQueRetirarse(2, 4), "dos contra cuatro: se retira");
                v.Falso(PlanDeIA.HayQueRetirarse(0, 5), "sin tropa no hay retirada");
            });

            Arranque.Registrar("reparto: la carne con hambre tambien recibe", v =>
            {
                v.Cierto(PlanDeIA.QuienCambia(R(6, 4, 0), R(4, 4, 2), out int desde, out int hacia), "hay cambio");
                v.Igual(desde, 1, "desde el oro");
                v.Igual(hacia, 3, "hacia la carne");
            });
        }
    }
}
