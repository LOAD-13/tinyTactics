using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Crea y mantiene los assets de edificio, con su coste, lo que fabrican y una huella
    /// <b>medida sobre el PNG</b>.
    ///
    /// La medición no es un lujo. En la semana 03 se leyó un tileset a ojo tres veces
    /// seguidas y las tres salió mal; en la 05, el marco de selección del castillo no encajó
    /// hasta que se abrió el PNG y se contaron los píxeles. Un edificio cuya huella se
    /// estima a ojo produce exactamente el mismo síntoma: la silueta enseña un sitio y el
    /// muro acaba en otro, medio tile más allá.
    ///
    /// Igual que el catálogo de unidades, los assets se reescriben enteros en cada pasada.
    /// El balance de verdad llega en la semana 16 y se hará sobre estos mismos assets.
    /// </summary>
    public static class FichasDeEdificio
    {
        const string Carpeta = "Assets/Datos/Edificios";
        const string DirEdificios = "Assets/Tiny Swords/Buildings/{color} Buildings";

        /// <summary>
        /// Los edificios de la épica E05. La torre llega con el combate, en la E06: una
        /// torre que no dispara es un adorno caro.
        /// </summary>
        static readonly TipoEdificio[] Fichas =
        {
            TipoEdificio.Castillo,
            TipoEdificio.Casa,
            TipoEdificio.Cuartel,
            TipoEdificio.CampoDeTiro,
            TipoEdificio.Monasterio,
            TipoEdificio.Torre,
        };

        [MenuItem("Tiny Tactics/Reconstruir catálogo de edificios", false, 32)]
        public static void Reconstruir()
        {
            var lista = ObtenerTodas();

            Debug.Log($"[Tiny Tactics] Catálogo de edificios reconstruido: " +
                      $"{lista.Count} fichas medidas en {Carpeta}.");

            Selection.activeObject = lista.Count > 0 ? lista[0] : null;
        }

        public static List<DatosEdificio> ObtenerTodas()
        {
            AsegurarCarpeta();

            var salida = new List<DatosEdificio>();
            foreach (var tipo in Fichas) salida.Add(Escribir(tipo));

            AssetDatabase.SaveAssets();
            return salida;
        }

        public static DatosEdificio Obtener(TipoEdificio tipo)
        {
            AsegurarCarpeta();
            return Escribir(tipo);
        }

        // -----------------------------------------------------------------

        /// <summary>
        /// Reescribe la ficha entera, medición incluida, igual que el catálogo de unidades.
        /// </summary>
        static DatosEdificio Escribir(TipoEdificio tipo)
        {
            string ruta = $"{Carpeta}/{tipo}.asset";

            var datos = AssetDatabase.LoadAssetAtPath<DatosEdificio>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosEdificio>();
                AssetDatabase.CreateAsset(datos, ruta);
            }

            datos.tipo = tipo;
            Rellenar(datos, tipo);
            Medir(datos);

            EditorUtility.SetDirty(datos);
            return datos;
        }

        /// <summary>
        /// Coste, función y planta de cada edificio.
        /// </summary>
        /// <remarks>
        /// <b>La planta va en tiles enteros y la huella en decimales.</b> No es incoherencia:
        /// son dos medidas distintas. La huella es el dibujo, y sirve para saber a qué
        /// distancia está un pawn del borde; la planta es el suelo que ocupa, y tiene que
        /// caer en celdas completas porque la silueta se ajusta a la rejilla. Un edificio
        /// que ocupara 2,88 celdas dejaría un doceavo de celda pisable que nadie podría usar.
        /// </remarks>
        static void Rellenar(DatosEdificio d, TipoEdificio tipo)
        {
            d.carpeta = DirEdificios;
            d.variantes = FachadasDe(tipo);

            switch (tipo)
            {
                case TipoEdificio.Casa:
                    d.nombreVisible = "Casa";
                    d.oro = 0; d.madera = 60;
                    d.planta = new Vector2Int(2, 2);
                    d.golpesDeObra = 32;
                    d.vidaMaxima = 350;
                    d.poblacionQueAporta = 5;
                    d.radioVision = 6f;
                    d.centroDeEntrega = false;
                    d.construible = true;
                    d.fabrica = new DatosUnidad[0];
                    break;

                case TipoEdificio.Cuartel:
                    d.nombreVisible = "Cuartel";
                    d.oro = 40; d.madera = 100;
                    d.planta = new Vector2Int(3, 3);
                    d.golpesDeObra = 50;
                    d.vidaMaxima = 600;
                    d.poblacionQueAporta = 0;
                    d.radioVision = 7f;
                    d.centroDeEntrega = false;
                    d.construible = true;

                    // Dos unidades en el mismo edificio. Es el caso que obligó a que la
                    // rejilla del panel fuera dinámica: con un botón fijo por edificio, el
                    // lancero no habría tenido dónde salir.
                    d.fabrica = new[]
                    {
                        CatalogoDeUnidades.Obtener(TipoUnidad.Guerrero),
                        CatalogoDeUnidades.Obtener(TipoUnidad.Lancero),
                    };
                    break;

                case TipoEdificio.CampoDeTiro:
                    d.nombreVisible = "Campo de tiro";
                    d.oro = 40; d.madera = 90;
                    d.planta = new Vector2Int(3, 3);
                    d.golpesDeObra = 46;
                    d.vidaMaxima = 550;
                    d.poblacionQueAporta = 0;
                    d.radioVision = 7f;
                    d.centroDeEntrega = false;
                    d.construible = true;
                    d.fabrica = new[] { CatalogoDeUnidades.Obtener(TipoUnidad.Arquero) };
                    break;

                case TipoEdificio.Monasterio:
                    d.nombreVisible = "Monasterio";
                    d.oro = 80; d.madera = 80;
                    d.planta = new Vector2Int(3, 3);
                    d.golpesDeObra = 56;
                    d.vidaMaxima = 500;
                    d.poblacionQueAporta = 0;
                    d.radioVision = 7f;
                    d.centroDeEntrega = false;
                    d.construible = true;
                    d.fabrica = new[] { CatalogoDeUnidades.Obtener(TipoUnidad.Monje) };
                    break;

                case TipoEdificio.Torre:
                    d.nombreVisible = "Torre";
                    d.oro = 30; d.madera = 70;
                    d.planta = new Vector2Int(2, 2);
                    d.golpesDeObra = 34;
                    d.vidaMaxima = 450;
                    d.poblacionQueAporta = 0;

                    // Por encima de su alcance de 7,5: una torre que dispara mas lejos de
                    // lo que ve seria una torre que no dispara, porque con la niebla puesta
                    // nadie apunta a lo que no se ve.
                    d.radioVision = 9.5f;
                    d.centroDeEntrega = false;
                    d.construible = true;

                    // Defensa estatica: no fabrica nada y no come carne. Su valor es estar
                    // siempre ahi, que ya es mucho, asi que el intercambio por disparo tiene
                    // que ser malo para ella o rodear una base dejaria de ser una opcion.
                    d.guarnicion = true;

                    // MEDIDO, no estimado: Archer_Shoot.png son 1536x192, o sea ocho
                    // fotogramas, y el arquero los reproduce a 13 fps. Eso da un disparo cada
                    // 0,62 s. La torre tenia 1,6 y por eso se sentia floja pese a pegar mas
                    // fuerte: disparaba dos veces y media mas lento que un arquero a pie.
                    d.cadencia = 0.62f;

                    // Y por encima del arquero, que tiene 5,0. Una torre con menos alcance que
                    // la unidad que la ataca no defiende nada: la matan desde fuera sin que
                    // pueda responder, que es exactamente lo contrario de para lo que existe.
                    d.alcanceAtaque = 7.5f;
                    d.danoAtaque = 20;

                    // El arquero se planta en la almena. La torre dibuja 2,88 tiles de alto
                    // MEDIDOS sobre el PNG, y la almena cae a poco mas de un tile del centro.
                    d.puntoDisparo = new Vector2(0f, 1.05f);
                    break;

                default: // Castillo
                    d.nombreVisible = "Castillo";
                    d.oro = 0; d.madera = 0;
                    d.planta = new Vector2Int(5, 3);
                    d.golpesDeObra = 90;
                    // El castillo es la condicion de derrota: tiene que costar un asedio,
                    // no una incursion de dos guerreros mientras miras a otro lado.
                    d.vidaMaxima = 1500;

                    // La atalaya del bando. Con cinco por tres celdas de planta, este radio
                    // es lo que hace que la partida empiece con la base propia y su primer
                    // anillo de bosque a la vista, y no a oscuras dentro de tu propia casa.
                    d.radioVision = 11f;

                    // Los diez de población de salida. Es el número que decide cuántas
                    // unidades caben antes de tener que construir la primera casa, así que
                    // marca el ritmo entero de la apertura.
                    d.poblacionQueAporta = 10;
                    d.centroDeEntrega = true;

                    // No se construye: ya está puesto al empezar. El día que haya expansiones
                    // bastará con poner esto en true.
                    d.construible = false;
                    d.fabrica = new[] { CatalogoDeUnidades.Obtener(TipoUnidad.Pawn) };
                    break;
            }
        }

        /// <summary>
        /// Los PNG del pack que dibujan cada edificio. La única traducción español → inglés.
        /// </summary>
        /// <remarks>
        /// La casa tiene <b>tres</b> y el resto una. Son la misma casa con otra fachada:
        /// mismo coste, misma planta y los mismos cinco de población. Estaban en el pack
        /// desde el primer día y se quedaban sin usar.
        /// </remarks>
        static DatosEdificio.Variante[] FachadasDe(TipoEdificio tipo)
        {
            switch (tipo)
            {
                case TipoEdificio.Casa: return Fachadas("House1", "House2", "House3");
                case TipoEdificio.Cuartel: return Fachadas("Barracks");
                case TipoEdificio.CampoDeTiro: return Fachadas("Archery");
                case TipoEdificio.Monasterio: return Fachadas("Monastery");
                case TipoEdificio.Torre: return Fachadas("Tower");
                default: return Fachadas("Castle");
            }
        }

        static DatosEdificio.Variante[] Fachadas(params string[] archivos)
        {
            var salida = new DatosEdificio.Variante[archivos.Length];

            for (int i = 0; i < archivos.Length; i++)
                salida[i] = new DatosEdificio.Variante { archivo = archivos[i] };

            return salida;
        }

        // -----------------------------------------------------------------
        // Medición
        // -----------------------------------------------------------------

        /// <summary>
        /// Mide sobre el PNG cuánto ocupa de verdad el dibujo dentro de su lienzo.
        /// </summary>
        /// <remarks>
        /// Se mide el <b>recuadro de píxeles no transparentes</b>, no el tamaño del archivo.
        /// El pack deja aire alrededor de cada edificio —el castillo son 320×256 píxeles de
        /// lienzo para 312×208 de dibujo— y tomar el lienzo por bueno habría dado una huella
        /// medio tile más ancha de lo real por cada lado.
        ///
        /// El resultado se comprobó contra la medición manual que ya se había hecho del
        /// castillo en la semana 05: 4,88 × 3,25 con el centro 0,27 por debajo. Sale igual.
        /// </remarks>
        static void Medir(DatosEdificio datos)
        {
            if (datos.variantes == null) return;

            // Cada fachada se mide por separado: la casa más alta y la más baja se llevan un
            // tercio de tile, y compartir una sola medida dejaría a una de las tres flotando
            // sobre su propia sombra.
            for (int i = 0; i < datos.variantes.Length; i++)
                MedirFachada(datos, i);
        }

        static void MedirFachada(DatosEdificio datos, int variante)
        {
            var ficha = datos.variantes[variante];
            if (ficha == null) return;

            string ruta = datos.RutaDe("Blue", variante);

            if (!System.IO.File.Exists(ruta))
            {
                Debug.LogWarning($"[Tiny Tactics] No encuentro {ruta}; huella sin medir.");
                return;
            }

            // Se decodifica el PNG del disco en una textura temporal en vez de leer la que
            // Unity tiene importada.
            //
            // La textura importada NO se puede leer por píxeles a menos que se le active
            // «Read/Write», y activarla significa reescribir el .meta de un archivo del pack
            // — justo lo que la regla de `Assets/Tiny Swords/` prohíbe— y, si la escritura se
            // queda a medias, dejar al proyecto con cada textura duplicada en memoria. Una
            // copia temporal que se tira al acabar no toca nada de eso.
            var temporal = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (!temporal.LoadImage(System.IO.File.ReadAllBytes(ruta)))
                {
                    Debug.LogWarning($"[Tiny Tactics] No puedo decodificar {ruta}.");
                    return;
                }

                int ancho = temporal.width;
                int alto = temporal.height;

                var pixeles = temporal.GetPixels32();

                int minX = ancho, minY = alto, maxX = -1, maxY = -1;

                for (int y = 0; y < alto; y++)
                {
                    for (int x = 0; x < ancho; x++)
                    {
                        // 16 sobre 255, no cero: los bordes del pack llevan un halo casi
                        // transparente del antialiasing que estiraría la huella un píxel por
                        // cada lado sin que se vea nada ahí.
                        if (pixeles[x + y * ancho].a <= 16) continue;

                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (maxX < 0)
                {
                    Debug.LogWarning($"[Tiny Tactics] {ruta} está en blanco; huella sin medir.");
                    return;
                }

                // Un tile del pack mide 64 píxeles. Sale del importador y no de una constante
                // para que la medida siga valiendo si algún día se reimporta con otra escala.
                float ppu = PixelesPorUnidad(ruta);

                ficha.huella = new Vector2((maxX - minX + 1) / ppu, (maxY - minY + 1) / ppu);

                ficha.huellaCentro = new Vector2(
                    ((minX + maxX + 1) * 0.5f - ancho * 0.5f) / ppu,
                    ((minY + maxY + 1) * 0.5f - alto * 0.5f) / ppu);
            }
            finally
            {
                Object.DestroyImmediate(temporal);
            }
        }

        static float PixelesPorUnidad(string ruta)
        {
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            return importador != null && importador.spritePixelsPerUnit > 0f
                ? importador.spritePixelsPerUnit
                : 64f;
        }

        static void AsegurarCarpeta()
        {
            if (AssetDatabase.IsValidFolder(Carpeta)) return;

            if (!AssetDatabase.IsValidFolder("Assets/Datos"))
                AssetDatabase.CreateFolder("Assets", "Datos");

            AssetDatabase.CreateFolder("Assets/Datos", "Edificios");
        }
    }
}
