// Pruebas del feedback de combate (semana 14): el fuego de los edificios y el aviso de
// «te atacan». Lo que se comprueba es cuando aparecen, no como se ven.
using TinyTactics.Combate;

namespace TinyTactics.Pruebas
{
    public static class PruebasDeFeedback
    {
        public static void Registrar()
        {
            Arranque.Registrar("fuego: mas llamas cuanta menos vida", v =>
            {
                v.Igual(FuegoDeEdificio.LlamasPara(1f), 0, "entero no arde");
                v.Igual(FuegoDeEdificio.LlamasPara(0.61f), 0, "rozado tampoco");
                v.Igual(FuegoDeEdificio.LlamasPara(0.6f), 1, "a la mitad larga, una");
                v.Igual(FuegoDeEdificio.LlamasPara(0.3f), 2, "a un tercio, dos");
                v.Igual(FuegoDeEdificio.LlamasPara(0.1f), 3, "casi caido, tres");
                v.Igual(FuegoDeEdificio.LlamasPara(0f), 0, "derribado ya no: lo tapa la explosion");

                int antes = 0;
                bool nuncaBaja = true;
                for (float f = 1f; f > 0.001f; f -= 0.01f)
                {
                    int n = FuegoDeEdificio.LlamasPara(f);
                    if (n < antes) nuncaBaja = false;
                    antes = n;
                }
                v.Cierto(nuncaBaja, "al perder vida nunca se apaga una llama");
            });

            Arranque.Registrar("aviso: solo lo que pasa fuera de la pantalla", v =>
            {
                v.Falso(AvisoDeAtaque.DebeAvisar(100f, -999f, 0f, true), "en pantalla nunca");
                v.Cierto(AvisoDeAtaque.DebeAvisar(100f, -999f, 0f, false), "fuera, el primero si");
            });

            Arranque.Registrar("aviso: no repite en el mismo sitio, si en otro", v =>
            {
                float e = AvisoDeAtaque.Espera;
                v.Falso(AvisoDeAtaque.DebeAvisar(10f + e * 0.5f, 10f, 3f, false), "misma pelea, poco despues: calla");
                v.Cierto(AvisoDeAtaque.DebeAvisar(10f + e, 10f, 3f, false), "pasado el rato, avisa otra vez");
                v.Cierto(AvisoDeAtaque.DebeAvisar(10f + 1f, 10f, AvisoDeAtaque.OtroSitio, false), "en otro sitio, al momento");
            });
        }
    }
}
