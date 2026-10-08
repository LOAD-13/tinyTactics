// Pruebas de la grilla de visibilidad (semana 09).
//
// Es C# puro: solo toca Vector2Int y Mathf, asi que corre fuera de Unity sin mas.
using UnityEngine;
using TinyTactics.Mundo;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeVisibilidad
    {
        public static void Registrar()
        {
            Arranque.Registrar("visibilidad: nace toda oculta", v =>
            {
                var m = new MapaDeVisibilidad(32, 32);

                v.Cierto(m.En(0, 0) == EstadoVisible.Oculto, "la esquina empieza oculta");
                v.Cierto(m.En(16, 16) == EstadoVisible.Oculto, "el centro empieza oculto");
                v.Igual(m.Exploradas, 0, "no hay nada explorado");
            });

            Arranque.Registrar("visibilidad: iluminar abre un disco", v =>
            {
                var m = new MapaDeVisibilidad(32, 32);
                m.Iluminar(new Vector2Int(16, 16), 5f);

                v.Cierto(m.Visible(16, 16), "el centro se ve");
                v.Cierto(m.Visible(20, 16), "a 4 tiles se ve");
                v.Cierto(m.Visible(21, 16), "a 5 tiles se ve, que es el radio");
                v.Falso(m.Visible(22, 16), "a 6 tiles ya no se ve");

                // La penumbra queda POR FUERA del radio: si cayera por dentro, el radio
                // efectivo de la unidad seria menor que el numero de su ficha.
                v.Cierto(m.En(22, 16) == EstadoVisible.Penumbra, "justo fuera hay penumbra");
                v.Cierto(m.En(26, 16) == EstadoVisible.Oculto, "mas alla sigue oculto");
            });

            Arranque.Registrar("visibilidad: lo explorado no vuelve a cerrarse", v =>
            {
                var m = new MapaDeVisibilidad(32, 32);
                m.Iluminar(new Vector2Int(10, 10), 4f);

                v.Cierto(m.Visible(10, 10), "primero se ve");

                m.EmpezarRonda();

                v.Falso(m.Visible(10, 10), "tras la ronda ya no se vigila");
                v.Cierto(m.Explorado(10, 10), "pero se recuerda");
                v.Cierto(m.En(10, 10) == EstadoVisible.Explorado, "y queda como explorado");
            });

            Arranque.Registrar("visibilidad: dos discos no se pisan", v =>
            {
                var m = new MapaDeVisibilidad(32, 32);

                // El borde del primero cae donde el segundo ve de pleno. Si el orden del
                // enum no mandara, la penumbra del primero apagaria la vista del segundo.
                m.Iluminar(new Vector2Int(10, 10), 4f);
                m.Iluminar(new Vector2Int(15, 10), 4f);

                v.Cierto(m.Visible(14, 10), "la celda compartida sigue a la vista");
                v.Cierto(m.Visible(11, 10), "y las de cada disco tambien");
                v.Cierto(m.Visible(18, 10), "por los dos lados");
            });

            Arranque.Registrar("visibilidad: nadie se sale del mapa", v =>
            {
                var m = new MapaDeVisibilidad(16, 16);

                // Iluminar en una esquina con un radio mayor que el mapa. Si el recorte
                // estuviera mal, esto reventaria con un indice fuera de rango.
                m.Iluminar(new Vector2Int(0, 0), 40f);

                v.Cierto(m.Visible(0, 0), "la esquina se ve");
                v.Cierto(m.En(-1, 0) == EstadoVisible.Oculto, "fuera del mapa es oculto");
                v.Cierto(m.En(99, 99) == EstadoVisible.Oculto, "muy fuera tambien");
                v.Igual(m.Exploradas, 16 * 16, "con radio 40 se abre el mapa entero");
            });

            Arranque.Registrar("visibilidad: revelar terreno no regala vision", v =>
            {
                var m = new MapaDeVisibilidad(16, 16);
                m.RevelarTerreno();

                v.Cierto(m.Explorado(8, 8), "el terreno se conoce");
                v.Falso(m.Visible(8, 8), "pero nadie lo esta mirando");
                v.Igual(m.Exploradas, 16 * 16, "y cuenta el mapa entero");
            });

            Arranque.Registrar("visibilidad: olvidar deja el mapa como nuevo", v =>
            {
                var m = new MapaDeVisibilidad(16, 16);
                m.Iluminar(new Vector2Int(8, 8), 5f);
                m.Olvidar();

                v.Falso(m.Explorado(8, 8), "ya no se recuerda nada");
                v.Igual(m.Exploradas, 0, "y el contador vuelve a cero");
            });
        }
    }
}
