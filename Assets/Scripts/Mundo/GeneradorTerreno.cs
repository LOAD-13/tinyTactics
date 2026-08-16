using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>Resultado completo de generar un mapa: terreno + qué va encima.</summary>
    public class MapaGenerado
    {
        public bool[,] Tierra;
        public int Ancho;
        public int Alto;

        public readonly List<Vector2Int> Bases = new List<Vector2Int>();
        public readonly List<Vector2Int> Expansiones = new List<Vector2Int>();
        public readonly List<Vector2Int> Oro = new List<Vector2Int>();
        public readonly List<Vector2Int> Arboles = new List<Vector2Int>();
        public readonly List<Vector2Int> Ovejas = new List<Vector2Int>();
        public readonly List<Vector2Int> Rocas = new List<Vector2Int>();
        public readonly List<Vector2Int> Arbustos = new List<Vector2Int>();
        public readonly List<Vector2Int> RocasAgua = new List<Vector2Int>();

        public bool EsTierra(int x, int y) =>
            x >= 0 && y >= 0 && x < Ancho && y < Alto && Tierra[x, y];
    }

    /// <summary>
    /// Genera mapas de RTS con simetría rotacional.
    ///
    /// Principios de diseño aplicados (los mismos de Warcraft y StarCraft):
    ///   1. <b>Simetría de N pliegues</b> — ningún bando arranca en desventaja.
    ///   2. <b>Base periférica</b> con su mina y su bosque propios.
    ///   3. <b>Expansión natural</b> a medio camino del centro: más recursos, peor de defender.
    ///   4. <b>Centro disputado</b> con el premio gordo — el que llega primero se lo queda.
    ///   5. <b>Los bosques canalizan</b> el movimiento y hacen de muro natural.
    ///
    /// Es lógica pura y determinista: la misma <see cref="DefinicionMapa"/> produce
    /// siempre el mismo mapa. No toca escena ni assets, por eso se puede probar sola.
    /// </summary>
    public static class GeneradorTerreno
    {
        // ---------------------------------------------------------------------
        // Índices de sprite dentro de Tilemap_colorN.png
        //
        // La textura son 9x6 celdas de 64 px, cortada en 44 sprites en orden de
        // lectura (la columna 4 está vacía y se salta). El suelo plano ocupa las
        // columnas 0-3 filas 0-3 y forma una autotile de 16 piezas:
        //
        //     0  1  2  3      esquina sup-izq | borde sup | esquina sup-der | tira vertical arriba
        //     8  9 10 11      borde izq       | centro    | borde der       | tira vertical medio
        //    16 17 18 19      esquina inf-izq | borde inf | esquina inf-der | tira vertical abajo
        //    24 25 26 27      tira horiz izq  | horiz med | horiz der       | tile aislado
        // ---------------------------------------------------------------------

        public const int SupIzq = 0, Sup = 1, SupDer = 2, VertArriba = 3;
        public const int Izq = 8, Centro = 9, Der = 10, VertMedio = 11;
        public const int InfIzq = 16, Inf = 17, InfDer = 18, VertAbajo = 19;
        public const int HorizIzq = 24, HorizMedio = 25, HorizDer = 26, Aislado = 27;

        public static readonly int[] IndicesSueloPlano =
        {
            SupIzq, Sup, SupDer, VertArriba,
            Izq, Centro, Der, VertMedio,
            InfIzq, Inf, InfDer, VertAbajo,
            HorizIzq, HorizMedio, HorizDer, Aislado
        };

        // =====================================================================
        // Generación
        // =====================================================================

        public static MapaGenerado Generar(DefinicionMapa def)
        {
            var mapa = new MapaGenerado
            {
                Ancho = def.ancho,
                Alto = def.alto,
                Tierra = ConstruirTerreno(def)
            };

            ColocarContenido(def, mapa);
            return mapa;
        }

        static bool[,] ConstruirTerreno(DefinicionMapa def)
        {
            int ancho = def.ancho, alto = def.alto;

            float[,] puntaje = CalcularPuntaje(def);

            int total = ancho * alto;
            var plano = new float[total];
            int k = 0;
            for (int x = 0; x < ancho; x++)
                for (int y = 0; y < alto; y++)
                    plano[k++] = puntaje[x, y];

            System.Array.Sort(plano);

            int indiceUmbral = Mathf.Clamp(
                Mathf.RoundToInt((1f - def.cobertura) * (total - 1)), 0, total - 1);
            float umbral = plano[indiceUmbral];

            var tierra = new bool[ancho, alto];
            for (int x = 0; x < ancho; x++)
                for (int y = 0; y < alto; y++)
                    tierra[x, y] = puntaje[x, y] > umbral;

            for (int i = 0; i < def.pasosSuavizado; i++)
                tierra = Suavizar(tierra, ancho, alto);

            // Los brazos se tallan ANTES de garantizar las zonas de juego: así el mar
            // separa los lóbulos pero nunca puede comerse una base ni un corredor.
            if (def.tallarBrazos) TallarBrazos(def, tierra);

            GarantizarZonasDeJuego(def, tierra);
            ConectarAnillos(def, tierra);

            ForzarMargenDeAgua(tierra, ancho, alto, def.margenAgua);

            if (def.soloMasaPrincipal)
                tierra = ConservarMasaMasGrande(tierra, ancho, alto);

            if (def.rellenarLagosHasta > 0)
                RellenarLagosPequenos(tierra, ancho, alto, def.rellenarLagosHasta);

            return tierra;
        }

        /// <summary>
        /// Ruido de tres octavas <b>mezclado</b> con una caída radial, muestreado en
        /// coordenadas <b>plegadas</b> para lograr simetría rotacional exacta.
        ///
        /// La mezcla es deliberada: multiplicar el ruido por la caída aplasta su
        /// estructura y el umbral acaba recortando siempre un círculo. Interpolando,
        /// la caída solo sesga hacia el centro y el ruido conserva libertad para
        /// formar bahías y penínsulas.
        /// </summary>
        static float[,] CalcularPuntaje(DefinicionMapa def)
        {
            int ancho = def.ancho, alto = def.alto;
            var rnd = new System.Random(def.semilla);

            float despX = (float)rnd.NextDouble() * 1000f;
            float despY = (float)rnd.NextDouble() * 1000f;

            float centroX = (ancho - 1) * 0.5f;
            float centroY = (alto - 1) * 0.5f;
            float radio = Mathf.Max(1f, Mathf.Min(centroX, centroY));

            var puntaje = new float[ancho, alto];

            for (int x = 0; x < ancho; x++)
            {
                for (int y = 0; y < alto; y++)
                {
                    Vector2 p = Plegar(x, y, centroX, centroY, def.bandos);

                    float n1 = Mathf.PerlinNoise(
                        despX + p.x * def.escalaRuido, despY + p.y * def.escalaRuido);
                    float n2 = Mathf.PerlinNoise(
                        despX + 100f + p.x * def.escalaRuido * 2.3f,
                        despY + 100f + p.y * def.escalaRuido * 2.3f);
                    float n3 = Mathf.PerlinNoise(
                        despX + 200f + p.x * def.escalaRuido * 4.7f,
                        despY + 200f + p.y * def.escalaRuido * 4.7f);

                    float ruido = n1 * 0.55f + n2 * 0.30f + n3 * 0.15f;

                    float dx = (x - centroX) / radio;
                    float dy = (y - centroY) / radio;
                    float distancia = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float caida = Mathf.Clamp01(1f - Mathf.Pow(distancia, def.durezaBorde));

                    puntaje[x, y] = Mathf.Lerp(ruido, caida, def.pesoIsla);
                }
            }

            return puntaje;
        }

        /// <summary>
        /// Mapea un punto a su representante dentro del primer sector de 360/N grados.
        /// El espejo dentro del sector evita que se vea una costura en la frontera.
        /// </summary>
        static Vector2 Plegar(float x, float y, float centroX, float centroY, int bandos)
        {
            float dx = x - centroX;
            float dy = y - centroY;

            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angulo = Mathf.Atan2(dy, dx);

            float sector = 2f * Mathf.PI / Mathf.Max(1, bandos);

            float a = Mathf.Repeat(angulo, sector);
            if (a > sector * 0.5f) a = sector - a;

            return new Vector2(centroX + r * Mathf.Cos(a), centroY + r * Mathf.Sin(a));
        }

        // ---------------------------------------------------------------------
        // Disposición de juego
        // ---------------------------------------------------------------------

        /// <summary>Puntos equiespaciados en un anillo: uno por bando.</summary>
        public static List<Vector2> Posiciones(DefinicionMapa def, float radioRelativo, float desfase)
        {
            var salida = new List<Vector2>();

            float centroX = (def.ancho - 1) * 0.5f;
            float centroY = (def.alto - 1) * 0.5f;
            float radio = Mathf.Min(centroX, centroY) * radioRelativo;

            for (int k = 0; k < def.bandos; k++)
            {
                float ang = 2f * Mathf.PI * (k + desfase) / def.bandos - Mathf.PI * 0.5f;
                salida.Add(new Vector2(centroX + radio * Mathf.Cos(ang), centroY + radio * Mathf.Sin(ang)));
            }

            return salida;
        }

        static void GarantizarZonasDeJuego(DefinicionMapa def, bool[,] tierra)
        {
            int ancho = def.ancho, alto = def.alto;
            float escala = Mathf.Min(ancho, alto) / 192f;

            foreach (var p in Posiciones(def, def.radioBases, 0f))
                Disco(tierra, ancho, alto, p.x, p.y, 30f * escala, true);

            foreach (var p in Posiciones(def, def.radioExpansiones, 0f))
                Disco(tierra, ancho, alto, p.x, p.y, 17f * escala, true);

            Disco(tierra, ancho, alto, (ancho - 1) * 0.5f, (alto - 1) * 0.5f,
                  Mathf.Min(ancho, alto) * 0.5f * def.radioCentro * 1.25f, true);
        }

        /// <summary>
        /// Brazos de mar entre lóbulos, uno por frontera de sector.
        ///
        /// Es la pieza que da forma al mapa: cada bando queda en su propio lóbulo y el
        /// único paso hacia los demás es la meseta central. Eso convierte el centro en
        /// territorio obligatorio en vez de un adorno, que es justo lo que hace
        /// interesante una partida a tres.
        ///
        /// El brazo se ensancha hacia afuera, como un fiordo: cerca del centro es
        /// estrecho (paso disputado) y en la costa es ancho (lóbulos bien separados).
        /// </summary>
        static void TallarBrazos(DefinicionMapa def, bool[,] tierra)
        {
            int ancho = def.ancho, alto = def.alto;
            float centroX = (ancho - 1) * 0.5f, centroY = (alto - 1) * 0.5f;
            float radioMapa = Mathf.Min(centroX, centroY);

            float sector = 2f * Mathf.PI / def.bandos;
            float radioInterior = radioMapa * (def.radioCentro + 0.06f);
            float radioExterior = radioMapa * 1.05f;

            for (int k = 0; k < def.bandos; k++)
            {
                float angBrazo = 2f * Mathf.PI * (k + 0.5f) / def.bandos - Mathf.PI * 0.5f;

                for (int x = 0; x < ancho; x++)
                {
                    for (int y = 0; y < alto; y++)
                    {
                        float dx = x - centroX, dy = y - centroY;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);

                        if (r < radioInterior || r > radioExterior) continue;

                        float delta = Mathf.Abs(Mathf.DeltaAngle(
                            Mathf.Atan2(dy, dx) * Mathf.Rad2Deg,
                            angBrazo * Mathf.Rad2Deg)) * Mathf.Deg2Rad;

                        float anchoBrazo = sector * def.anchoBrazoAgua * (0.45f + 0.85f * (r / radioMapa));

                        if (delta < anchoBrazo) tierra[x, y] = false;
                    }
                }
            }
        }

        /// <summary>
        /// Corredor de tierra base → expansión → centro, uno por bando.
        ///
        /// Solo el corredor propio: cruzar hacia el lóbulo vecino atravesaría un brazo
        /// de mar y rompería el diseño.
        /// </summary>
        static void ConectarAnillos(DefinicionMapa def, bool[,] tierra)
        {
            int ancho = def.ancho, alto = def.alto;
            float centroX = (ancho - 1) * 0.5f, centroY = (alto - 1) * 0.5f;
            float escala = Mathf.Min(ancho, alto) / 192f;

            var bases = Posiciones(def, def.radioBases, 0f);
            var expansiones = Posiciones(def, def.radioExpansiones, 0f);

            for (int k = 0; k < def.bandos; k++)
            {
                Vector2 b = bases[k];
                Vector2 e = expansiones[k];

                Linea(tierra, ancho, alto, b.x, b.y, e.x, e.y, 9f * escala, true);
                Linea(tierra, ancho, alto, e.x, e.y, centroX, centroY, 9f * escala, true);
            }
        }

        // ---------------------------------------------------------------------
        // Contenido: recursos y decoración
        // ---------------------------------------------------------------------

        /// <summary>
        /// Reparte recursos en <b>bolsones</b>, no esparcidos.
        ///
        /// Es como funcionan los mapas de Warcraft: apareces y pegado a ti tienes una
        /// veta de oro apiñada en un recodo y un bosque denso al otro lado. Esparcir
        /// nodos sueltos por el mapa parece decoración; agruparlos crea economía —
        /// hay <i>sitios</i> que valen la pena y por los que merece la pena pelear.
        /// </summary>
        static void ColocarContenido(DefinicionMapa def, MapaGenerado mapa)
        {
            var rnd = new System.Random(def.semilla + 99);

            float centroX = (def.ancho - 1) * 0.5f;
            float centroY = (def.alto - 1) * 0.5f;

            var bases = Posiciones(def, def.radioBases, 0f);
            var expansiones = Posiciones(def, def.radioExpansiones, 0f);

            float escala = Mathf.Min(def.ancho, def.alto) / 192f;
            int huecos = Mathf.Max(2, def.bajadasPorBase);

            for (int k = 0; k < def.bandos; k++)
            {
                Vector2 b = bases[k];
                float ang = 2f * Mathf.PI * k / def.bandos - Mathf.PI * 0.5f;

                mapa.Bases.Add(ARejilla(b));

                // Anillo interior: N crecientes con N huecos entre ellas.
                // Los huecos son deliberados — ahí van las bajadas cuando lleguen
                // los desniveles. El diseño ya reserva el sitio.
                for (int h = 0; h < huecos; h++)
                {
                    float a = ang + 2f * Mathf.PI * h / huecos + Mathf.PI / huecos;
                    Creciente(mapa, rnd, b.x, b.y, a, 13.5f * escala, 1.35f, 5, 9);
                }

                // Ovejas sueltas por la meseta inicial.
                for (int j = 0; j < 7; j++)
                {
                    float a = Aleatorio(rnd, 0f, 2f * Mathf.PI);
                    float d = Aleatorio(rnd, 5f, 11f) * escala;
                    Agregar(mapa, mapa.Ovejas, b.x + d * Mathf.Cos(a), b.y + d * Mathf.Sin(a));
                }

                // Crecientes exteriores, alineadas con los huecos: sales por una
                // bajada y te encuentras la siguiente zona de recursos.
                for (int h = 0; h < huecos; h++)
                {
                    float a = ang + 2f * Mathf.PI * h / huecos;
                    Creciente(mapa, rnd,
                              b.x + 27f * escala * Mathf.Cos(a),
                              b.y + 27f * escala * Mathf.Sin(a),
                              a, 8.5f * escala, 1.9f, 6, 11);
                }

                // Rebaño grande hacia el exterior del lóbulo.
                Rebano(mapa, rnd,
                       b.x + 30f * escala * Mathf.Cos(ang),
                       b.y + 30f * escala * Mathf.Sin(ang), 13, 7f * escala);
            }

            // Expansión natural: dentro del lóbulo propio, camino al centro.
            for (int k = 0; k < def.bandos; k++)
            {
                Vector2 e = expansiones[k];
                float ang = 2f * Mathf.PI * k / def.bandos - Mathf.PI * 0.5f;

                mapa.Expansiones.Add(ARejilla(e));

                for (int h = 0; h < 2; h++)
                {
                    float a = ang + Mathf.PI * h + Mathf.PI * 0.5f;
                    Creciente(mapa, rnd, e.x, e.y, a, 7.5f * escala, 1.6f, 4, 8);
                }

                Rebano(mapa, rnd, e.x, e.y, 5, 6f * escala);
            }

            // Meseta central: la veta más rica, con entradas contadas.
            for (int h = 0; h < def.bandos; h++)
            {
                float a = 2f * Mathf.PI * h / def.bandos - Mathf.PI * 0.5f;
                Creciente(mapa, rnd, centroX, centroY, a, 9f * escala, 1.5f, 5, 10);
            }

            SembrarDecoracion(def, mapa, rnd);
            SembrarFormacionesDeRoca(def, mapa, rnd);
            SembrarRocasDeAgua(def, mapa, rnd);
        }

        /// <summary>
        /// "Luna" de recursos: vena de oro en el arco interior y muralla de árboles en
        /// el exterior, siguiendo la misma curva.
        ///
        /// Es la forma que aparece en los mapas de Warcraft y no es casual: el oro queda
        /// resguardado tras los árboles, así que explotarlo obliga a controlar la zona en
        /// vez de pasar de largo. Nodos sueltos esparcidos no generan esa decisión.
        /// </summary>
        static void Creciente(MapaGenerado mapa, System.Random rnd,
                              float cx, float cy, float angulo, float radio,
                              float arco, int nodosOro, int arboles)
        {
            for (int i = 0; i < nodosOro; i++)
            {
                float t = (i / (float)Mathf.Max(1, nodosOro - 1) - 0.5f) * arco;
                float d = radio + Aleatorio(rnd, -0.6f, 0.6f);
                Agregar(mapa, mapa.Oro,
                        cx + d * Mathf.Cos(angulo + t),
                        cy + d * Mathf.Sin(angulo + t));
            }

            for (int i = 0; i < arboles; i++)
            {
                float t = (i / (float)Mathf.Max(1, arboles - 1) - 0.5f) * arco * 1.18f;
                float d = radio + 3.4f + Aleatorio(rnd, -0.9f, 0.9f);

                Agregar(mapa, mapa.Arboles,
                        cx + d * Mathf.Cos(angulo + t),
                        cy + d * Mathf.Sin(angulo + t));

                // Segunda fila salteada: la pared se ve maciza sin duplicar el conteo.
                if (i % 2 == 0)
                {
                    Agregar(mapa, mapa.Arboles,
                            cx + (d + 2.7f) * Mathf.Cos(angulo + t + 0.03f),
                            cy + (d + 2.7f) * Mathf.Sin(angulo + t + 0.03f));
                }
            }
        }

        static void Rebano(MapaGenerado mapa, System.Random rnd,
                           float cx, float cy, int cantidad, float radio)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float a = Aleatorio(rnd, 0f, 2f * Mathf.PI);
                float d = radio * Mathf.Sqrt((float)rnd.NextDouble());
                Agregar(mapa, mapa.Ovejas, cx + d * Mathf.Cos(a), cy + d * Mathf.Sin(a));
            }
        }

        /// <summary>
        /// Formaciones de piedra en espiral. Puro adorno, pero un adorno con intención:
        /// una espiral se lee como algo construido, no como piedras caídas al azar.
        /// </summary>
        static void SembrarFormacionesDeRoca(DefinicionMapa def, MapaGenerado mapa, System.Random rnd)
        {
            if (def.formacionesDeRoca <= 0) return;

            float centroX = (def.ancho - 1) * 0.5f, centroY = (def.alto - 1) * 0.5f;
            float radioMapa = Mathf.Min(centroX, centroY);

            for (int i = 0; i < def.formacionesDeRoca; i++)
            {
                // Una por sector, para no romper la simetría entre bandos.
                float angulo = 2f * Mathf.PI * i / def.formacionesDeRoca + 0.4f;
                float distancia = radioMapa * Aleatorio(rnd, 0.30f, 0.52f);

                float cx = centroX + distancia * Mathf.Cos(angulo);
                float cy = centroY + distancia * Mathf.Sin(angulo);

                int piedras = rnd.Next(14, 22);
                float giro = Aleatorio(rnd, 0f, 2f * Mathf.PI);

                // Espiral de Arquímedes: el radio crece linealmente con el ángulo.
                for (int j = 0; j < piedras; j++)
                {
                    float t = j / (float)piedras;
                    float a = giro + t * 4.2f * Mathf.PI;
                    float r = 1.2f + t * 7f;

                    Agregar(mapa, mapa.Rocas, cx + r * Mathf.Cos(a), cy + r * Mathf.Sin(a));
                }
            }
        }

        /// <summary>
        /// Arrecifes: pequeñas formaciones de roca en <b>aguas abiertas</b>.
        ///
        /// El primer intento las sembró por densidad, con la probabilidad multiplicada
        /// cerca de la costa. Salieron demasiadas y pegadas a la orilla, de modo que
        /// parecían caídas sobre la tierra. Un arrecife necesita dos cosas: estar
        /// claramente en el mar y tener <i>forma</i>. Unas pocas figuras legibles hacen
        /// más por el océano que cientos de piedras sueltas.
        /// </summary>
        static void SembrarRocasDeAgua(DefinicionMapa def, MapaGenerado mapa, System.Random rnd)
        {
            if (def.arrecifes <= 0) return;

            float centroX = (def.ancho - 1) * 0.5f, centroY = (def.alto - 1) * 0.5f;
            float radioMapa = Mathf.Min(centroX, centroY);

            int colocados = 0;
            int intentos = 0;

            while (colocados < def.arrecifes && intentos < def.arrecifes * 120)
            {
                intentos++;

                float angulo = Aleatorio(rnd, 0f, 2f * Mathf.PI);
                float distancia = radioMapa * Aleatorio(rnd, 0.35f, 0.97f);

                float cx = centroX + distancia * Mathf.Cos(angulo);
                float cy = centroY + distancia * Mathf.Sin(angulo);

                // Aguas abiertas: nada de tierra en varios tiles a la redonda.
                if (HayTierraCerca(mapa, cx, cy, def.separacionArrecifes)) continue;

                DibujarArrecife(mapa, rnd, cx, cy, angulo);
                colocados++;
            }
        }

        /// <summary>Una figura corta y legible: arco, hilera o racimo.</summary>
        static void DibujarArrecife(MapaGenerado mapa, System.Random rnd, float cx, float cy, float angulo)
        {
            int forma = rnd.Next(3);
            int piedras = rnd.Next(3, 6);

            for (int i = 0; i < piedras; i++)
            {
                float t = piedras > 1 ? i / (float)(piedras - 1) - 0.5f : 0f;
                float px, py;

                if (forma == 0)
                {
                    // Arco.
                    float a = angulo + t * 1.5f;
                    float r = 2.6f;
                    px = cx + r * Mathf.Cos(a);
                    py = cy + r * Mathf.Sin(a) - t * t * 1.4f;
                }
                else if (forma == 1)
                {
                    // Hilera diagonal.
                    px = cx + t * 5.2f * Mathf.Cos(angulo);
                    py = cy + t * 5.2f * Mathf.Sin(angulo);
                }
                else
                {
                    // Racimo cerrado.
                    float a = 2f * Mathf.PI * i / piedras;
                    px = cx + 1.9f * Mathf.Cos(a);
                    py = cy + 1.9f * Mathf.Sin(a);
                }

                var celda = ARejilla(new Vector2(px, py));

                if (celda.x < 0 || celda.y < 0 || celda.x >= mapa.Ancho || celda.y >= mapa.Alto) continue;
                if (mapa.Tierra[celda.x, celda.y]) continue;

                mapa.RocasAgua.Add(celda);
            }
        }

        static bool HayTierraCerca(MapaGenerado mapa, float cx, float cy, int radio)
        {
            int x0 = Mathf.FloorToInt(cx) - radio, x1 = Mathf.CeilToInt(cx) + radio;
            int y0 = Mathf.FloorToInt(cy) - radio, y1 = Mathf.CeilToInt(cy) + radio;

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (mapa.EsTierra(x, y)) return true;

            return false;
        }

        /// <summary>Rocas y arbustos dispersos: rompen la uniformidad del pasto.</summary>
        static void SembrarDecoracion(DefinicionMapa def, MapaGenerado mapa, System.Random rnd)
        {
            for (int x = 0; x < def.ancho; x++)
            {
                for (int y = 0; y < def.alto; y++)
                {
                    if (!mapa.Tierra[x, y]) continue;
                    if (CercaDeAlgo(mapa, x, y, 3)) continue;

                    double v = rnd.NextDouble();

                    if (v < def.densidadRocas)
                        mapa.Rocas.Add(new Vector2Int(x, y));
                    else if (v < def.densidadRocas + def.densidadArbustos)
                        mapa.Arbustos.Add(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>Evita plantar decoración encima de recursos o bases.</summary>
        static bool CercaDeAlgo(MapaGenerado mapa, int x, int y, int radio)
        {
            return Cerca(mapa.Oro, x, y, radio)
                || Cerca(mapa.Arboles, x, y, radio)
                || Cerca(mapa.Bases, x, y, radio + 6)
                || Cerca(mapa.Ovejas, x, y, radio);
        }

        static bool Cerca(List<Vector2Int> puntos, int x, int y, int radio)
        {
            int r2 = radio * radio;
            for (int i = 0; i < puntos.Count; i++)
            {
                int dx = puntos[i].x - x, dy = puntos[i].y - y;
                if (dx * dx + dy * dy <= r2) return true;
            }
            return false;
        }

        static void Agregar(MapaGenerado mapa, List<Vector2Int> destino, float x, float y)
        {
            var celda = ARejilla(new Vector2(x, y));
            if (mapa.EsTierra(celda.x, celda.y)) destino.Add(celda);
        }

        static Vector2Int ARejilla(Vector2 p) =>
            new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));

        static float Aleatorio(System.Random rnd, float minimo, float maximo) =>
            minimo + (float)rnd.NextDouble() * (maximo - minimo);

        // ---------------------------------------------------------------------
        // Primitivas de dibujo sobre la máscara
        // ---------------------------------------------------------------------

        static void Disco(bool[,] mapa, int ancho, int alto, float cx, float cy, float radio, bool valor)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - radio));
            int x1 = Mathf.Min(ancho - 1, Mathf.CeilToInt(cx + radio));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - radio));
            int y1 = Mathf.Min(alto - 1, Mathf.CeilToInt(cy + radio));

            float r2 = radio * radio;

            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r2) mapa[x, y] = valor;
                }
            }
        }

        static void Linea(bool[,] mapa, int ancho, int alto,
                          float x0, float y0, float x1, float y1, float grosor, bool valor)
        {
            int pasos = Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 2f) + 1;

            for (int i = 0; i <= pasos; i++)
            {
                float t = (float)i / pasos;
                Disco(mapa, ancho, alto, Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), grosor, valor);
            }
        }

        // ---------------------------------------------------------------------
        // Limpieza topológica
        // ---------------------------------------------------------------------

        static bool[,] Suavizar(bool[,] origen, int ancho, int alto)
        {
            var destino = new bool[ancho, alto];

            for (int x = 0; x < ancho; x++)
            {
                for (int y = 0; y < alto; y++)
                {
                    int vecinos = ContarVecinosTierra(origen, ancho, alto, x, y);
                    if (vecinos > 4) destino[x, y] = true;
                    else if (vecinos < 4) destino[x, y] = false;
                    else destino[x, y] = origen[x, y];
                }
            }

            return destino;
        }

        static int ContarVecinosTierra(bool[,] mapa, int ancho, int alto, int x, int y)
        {
            int cuenta = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= ancho || ny >= alto) continue;
                    if (mapa[nx, ny]) cuenta++;
                }
            }

            return cuenta;
        }

        /// <summary>
        /// Anillo de agua garantizado, medido de forma <b>radial</b>.
        ///
        /// Con un borde cuadrado el lóbulo que apunta a un eje tiene menos sitio que los
        /// que apuntan a una diagonal (111 tiles frente a 128 en un mapa de 224), y se
        /// recorta. Medir por radio hace que todas las direcciones sean equivalentes,
        /// que es justo lo que exige la simetría rotacional.
        /// </summary>
        static void ForzarMargenDeAgua(bool[,] tierra, int ancho, int alto, int margen)
        {
            float centroX = (ancho - 1) * 0.5f, centroY = (alto - 1) * 0.5f;
            float limite = Mathf.Min(centroX, centroY) - margen;
            float limite2 = limite * limite;

            for (int x = 0; x < ancho; x++)
            {
                for (int y = 0; y < alto; y++)
                {
                    float dx = x - centroX, dy = y - centroY;
                    if (dx * dx + dy * dy > limite2) tierra[x, y] = false;
                }
            }
        }

        static bool[,] ConservarMasaMasGrande(bool[,] tierra, int ancho, int alto)
        {
            var visitado = new bool[ancho, alto];
            List<Vector2Int> mayor = null;

            for (int x = 0; x < ancho; x++)
            {
                for (int y = 0; y < alto; y++)
                {
                    if (!tierra[x, y] || visitado[x, y]) continue;
                    var region = InundarDesde(tierra, visitado, ancho, alto, x, y, true);
                    if (mayor == null || region.Count > mayor.Count) mayor = region;
                }
            }

            var resultado = new bool[ancho, alto];
            if (mayor == null) return resultado;

            foreach (var celda in mayor) resultado[celda.x, celda.y] = true;
            return resultado;
        }

        static void RellenarLagosPequenos(bool[,] tierra, int ancho, int alto, int tamanoMaximo)
        {
            var visitado = new bool[ancho, alto];

            for (int x = 0; x < ancho; x++)
            {
                for (int y = 0; y < alto; y++)
                {
                    if (tierra[x, y] || visitado[x, y]) continue;

                    var region = InundarDesde(tierra, visitado, ancho, alto, x, y, false);

                    bool tocaBorde = false;
                    foreach (var c in region)
                    {
                        if (c.x == 0 || c.y == 0 || c.x == ancho - 1 || c.y == alto - 1)
                        {
                            tocaBorde = true;
                            break;
                        }
                    }

                    if (tocaBorde || region.Count > tamanoMaximo) continue;

                    foreach (var c in region) tierra[c.x, c.y] = true;
                }
            }
        }

        static List<Vector2Int> InundarDesde(bool[,] mapa, bool[,] visitado, int ancho, int alto,
                                             int inicioX, int inicioY, bool objetivo)
        {
            var region = new List<Vector2Int>();
            var pila = new Stack<Vector2Int>();

            pila.Push(new Vector2Int(inicioX, inicioY));
            visitado[inicioX, inicioY] = true;

            while (pila.Count > 0)
            {
                var actual = pila.Pop();
                region.Add(actual);

                Apilar(pila, mapa, visitado, ancho, alto, actual.x + 1, actual.y, objetivo);
                Apilar(pila, mapa, visitado, ancho, alto, actual.x - 1, actual.y, objetivo);
                Apilar(pila, mapa, visitado, ancho, alto, actual.x, actual.y + 1, objetivo);
                Apilar(pila, mapa, visitado, ancho, alto, actual.x, actual.y - 1, objetivo);
            }

            return region;
        }

        static void Apilar(Stack<Vector2Int> pila, bool[,] mapa, bool[,] visitado,
                           int ancho, int alto, int x, int y, bool objetivo)
        {
            if (x < 0 || y < 0 || x >= ancho || y >= alto) return;
            if (visitado[x, y]) return;
            if (mapa[x, y] != objetivo) return;

            visitado[x, y] = true;
            pila.Push(new Vector2Int(x, y));
        }

        // =====================================================================
        // Selección de pieza
        // =====================================================================

        /// <summary>
        /// Elige el índice de sprite para una celda de tierra según qué vecinos
        /// cardinales también son tierra. El borde dibujado va del lado donde hay agua.
        /// </summary>
        public static int IndiceAutotile(bool norte, bool sur, bool este, bool oeste)
        {
            if (norte && sur && este && oeste) return Centro;

            if (!norte && sur && este && oeste) return Sup;
            if (norte && !sur && este && oeste) return Inf;
            if (norte && sur && !este && oeste) return Der;
            if (norte && sur && este && !oeste) return Izq;

            if (!norte && sur && este && !oeste) return SupIzq;
            if (!norte && sur && !este && oeste) return SupDer;
            if (norte && !sur && este && !oeste) return InfIzq;
            if (norte && !sur && !este && oeste) return InfDer;

            if (!norte && sur && !este && !oeste) return VertArriba;
            if (norte && sur && !este && !oeste) return VertMedio;
            if (norte && !sur && !este && !oeste) return VertAbajo;

            if (!norte && !sur && este && !oeste) return HorizIzq;
            if (!norte && !sur && este && oeste) return HorizMedio;
            if (!norte && !sur && !este && oeste) return HorizDer;

            return Aislado;
        }

        public static int IndiceAutotileEn(bool[,] tierra, int ancho, int alto, int x, int y)
        {
            return IndiceAutotile(
                EsTierra(tierra, ancho, alto, x, y + 1),
                EsTierra(tierra, ancho, alto, x, y - 1),
                EsTierra(tierra, ancho, alto, x + 1, y),
                EsTierra(tierra, ancho, alto, x - 1, y));
        }

        public static bool EsTierra(bool[,] tierra, int ancho, int alto, int x, int y)
        {
            if (x < 0 || y < 0 || x >= ancho || y >= alto) return false;
            return tierra[x, y];
        }

        /// <summary>
        /// Una celda de tierra necesita espuma si toca agua en cualquiera de sus 8 vecinos.
        /// El sprite de espuma es de 192x192 y va centrado en la celda de tierra:
        /// desborda hacia el agua y su centro queda tapado por el suelo.
        /// </summary>
        public static bool NecesitaEspuma(bool[,] tierra, int ancho, int alto, int x, int y)
        {
            if (!tierra[x, y]) return false;
            return ContarVecinosTierra(tierra, ancho, alto, x, y) < 8;
        }

        public static int ContarTierra(bool[,] tierra, int ancho, int alto)
        {
            int cuenta = 0;
            for (int x = 0; x < ancho; x++)
                for (int y = 0; y < alto; y++)
                    if (tierra[x, y]) cuenta++;
            return cuenta;
        }
    }
}
