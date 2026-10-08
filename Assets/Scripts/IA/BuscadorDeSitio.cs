using UnityEngine;
using TinyTactics.Edificios;
using TinyTactics.Mundo;

namespace TinyTactics.IA
{
    /// <summary>
    /// Dónde plantar un edificio. Es la decisión que más se nota de un bot.
    /// </summary>
    /// <remarks>
    /// <b>Se busca en anillos desde la base hacia afuera</b>, no al azar ni en cuadrícula.
    /// Al azar, el bot desparrama su base por medio mapa y sus pawns se pasan la partida
    /// andando; en cuadrícula estricta, un solo árbol en medio rompe la formación y el
    /// siguiente edificio aparece muy lejos. Por anillos, el primer hueco válido siempre es
    /// el más cercano posible y la base crece hacia afuera como crecería la de una persona.
    ///
    /// <b>Y no basta con que quepa: no puede cerrar el paso.</b> La grilla ya sabe
    /// responder a esa pregunta desde la semana 06, y aquí es donde se paga: un bot que
    /// tapia su propia salida no pierde por jugar mal, se queda encerrado — y eso el jugador
    /// no lo lee como una IA floja, lo lee como un juego roto.
    ///
    /// Toda la clase es lógica pura sobre la grilla: no toca la escena, así que se prueba
    /// sin abrir Unity (ver <c>tools/pruebas/</c>).
    /// </remarks>
    public static class BuscadorDeSitio
    {
        /// <summary>Holgura en tiles entre el dibujo de un edificio y el siguiente.</summary>
        const float margenEntreEdificios = 0.5f;

        /// <summary>
        /// Primer hueco válido para una planta de <paramref name="tamano"/> celdas.
        /// </summary>
        /// <param name="grilla">La grilla del mapa.</param>
        /// <param name="centro">Desde dónde se busca; normalmente el castillo.</param>
        /// <param name="tamano">Celdas que ocupa el edificio en el suelo.</param>
        /// <param name="separacion">Anillo mínimo, para no pegar el edificio al castillo.</param>
        /// <param name="alcance">Anillo máximo antes de rendirse.</param>
        /// <returns>True si encontró sitio.</returns>
        public static bool Buscar(GrillaMapa grilla, Vector2Int centro, Vector2Int tamano,
                                  int separacion, int alcance, out RectInt celdas)
        {
            celdas = default;

            if (grilla == null || tamano.x <= 0 || tamano.y <= 0) return false;

            separacion = Mathf.Max(1, separacion);
            alcance = Mathf.Max(separacion, alcance);

            for (int radio = separacion; radio <= alcance; radio++)
            {
                if (BuscarEnAnillo(grilla, centro, tamano, radio, out celdas)) return true;
            }

            return false;
        }

        /// <summary>
        /// Recorre el borde de un cuadrado de lado 2·radio+1 alrededor del centro.
        /// </summary>
        /// <remarks>
        /// Solo el BORDE, no el cuadrado entero: el interior ya se miró en los anillos
        /// anteriores. Recorrer el cuadrado completo en cada vuelta convertiría una búsqueda
        /// lineal en una cuadrática, y con un alcance de treinta celdas eso son novecientas
        /// comprobaciones por edificio en vez de ciento veinte.
        /// </remarks>
        static bool BuscarEnAnillo(GrillaMapa grilla, Vector2Int centro, Vector2Int tamano,
                                   int radio, out RectInt celdas)
        {
            celdas = default;

            for (int dx = -radio; dx <= radio; dx++)
            {
                for (int dy = -radio; dy <= radio; dy++)
                {
                    // Solo el borde del anillo.
                    if (Mathf.Abs(dx) != radio && Mathf.Abs(dy) != radio) continue;

                    var origen = new Vector2Int(centro.x + dx - tamano.x / 2,
                                                centro.y + dy - tamano.y / 2);

                    var candidato = new RectInt(origen.x, origen.y, tamano.x, tamano.y);

                    if (!Vale(grilla, candidato)) continue;

                    celdas = candidato;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ¿Es un sitio válido? Las mismas reglas que se le aplican al jugador.
        /// </summary>
        /// <remarks>
        /// Se reutiliza <see cref="ColocadorEdificios.Cabe"/> a propósito, y no se reescribe
        /// la comprobación: el día que cambien las reglas de colocación —una nueva razón para
        /// no poder construir, un tipo de terreno que no admite obra— el bot se entera solo.
        /// Dos copias de la misma regla se separan a la primera corrección.
        /// </remarks>
        public static bool Vale(GrillaMapa grilla, RectInt celdas)
        {
            if (!ColocadorEdificios.Cabe(grilla, celdas)) return false;

            // Ni encima del DIBUJO de otro edificio. La planta de una torre son 2x2 celdas
            // pero su dibujo mide casi tres tiles de alto, asi que dos torres separadas por
            // planta pueden salir una encima de la otra en pantalla. El bot lo hacia sin
            // parar y el resultado era un amasijo de tejados imposible de leer.
            if (PisaElDibujoDeOtro(celdas)) return false;

            // Lo caro va al final: cerrar el paso se comprueba con una inundación sobre la
            // grilla, así que solo se paga por los sitios que ya pasaron el resto.
            return !grilla.CerrariaElPaso(celdas);
        }

        /// <summary>
        /// ¿El dibujo del edificio que iría aquí se montaría sobre el de otro?
        /// </summary>
        /// <remarks>
        /// Se mide contra la HUELLA —el dibujo— y no contra la planta, que es la distincion
        /// de la semana 06 (ADR-14). La planta dice donde no se puede andar; la huella, lo
        /// que se ve. Para que la base se lea, lo que no se puede solapar es lo segundo.
        ///
        /// Un margen de media celda por si acaso: dos edificios que se tocan justo por el
        /// borde no se solapan, pero tampoco se distinguen.
        /// </remarks>
        static bool PisaElDibujoDeOtro(RectInt celdas)
        {
            var sitio = new Rect(celdas.x - margenEntreEdificios,
                                 celdas.y - margenEntreEdificios,
                                 celdas.width + margenEntreEdificios * 2f,
                                 celdas.height + margenEntreEdificios * 2f);

            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null) continue;

                var centro = e.PuntoDeEntrega;
                var dibujo = new Rect(centro.x - e.huella.x * 0.5f,
                                      centro.y - e.huella.y * 0.5f,
                                      e.huella.x, e.huella.y);

                if (dibujo.Overlaps(sitio)) return true;
            }

            return false;
        }
    }
}
