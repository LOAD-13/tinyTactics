using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;
using TinyTactics.Movimiento;
using TinyTactics.Datos;
using TinyTactics.Interfaz;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Construye la escena de juego completa a partir de una <see cref="DefinicionMapa"/>:
    /// corta el tileset en Tiles, genera el terreno y pinta las capas.
    ///
    /// El mapa no se pinta a mano. Se genera, y de la misma máscara saldrá la grilla
    /// lógica de pathfinding en la semana 03: una sola fuente, imposible que difieran.
    /// </summary>
    public static class ConstructorDeMapa
    {
        const string RutaTileset = "Assets/Tiny Swords/Terrain/Tileset/Tilemap_color1.png";
        const string RutaAgua = "Assets/Tiny Swords/Terrain/Tileset/Water Background color.png";
        const string RutaEspuma = "Assets/Tiny Swords/Terrain/Tileset/Water Foam.png";

        const string CarpetaDatos = "Assets/Datos";
        const string CarpetaTiles = "Assets/Datos/Tiles";
        const string CarpetaMapas = "Assets/Datos/Mapas";
        const string RutaDefinicion = "Assets/Datos/Mapas/TresCoronas.asset";
        const string RutaEscena = "Assets/Scenes/Juego.unity";

        /// <summary>Color de fondo del pack Tiny Swords (#47ABA9).</summary>
        static readonly Color ColorAgua = new Color(71f / 255f, 171f / 255f, 169f / 255f, 1f);

        [MenuItem("Tiny Tactics/Generar escena de juego", false, 10)]
        public static void GenerarEscena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var definicion = ObtenerODefinirMapa();
            if (definicion == null) return;

            var paleta = ConstruirPaleta();
            if (paleta == null) return;

            CamaraRTS camara;
            MapaGenerado mapa;
            int tierraTotal;

            try
            {
                EditorUtility.DisplayProgressBar("Tiny Tactics", "Generando terreno…", 0.3f);

                mapa = GeneradorTerreno.Generar(definicion);
                tierraTotal = GeneradorTerreno.ContarTierra(mapa.Tierra, mapa.Ancho, mapa.Alto);

                if (tierraTotal == 0)
                {
                    EditorUtility.DisplayDialog(
                        "Tiny Tactics",
                        "La generación no produjo tierra. Baja el margen de agua o sube la cobertura en " +
                        RutaDefinicion,
                        "Entendido");
                    return;
                }

                // Las fichas de edificio, con su huella medida sobre el PNG, antes de montar
                // nada que dependa de ellas.
                EditorUtility.DisplayProgressBar("Tiny Tactics", "Midiendo los edificios…", 0.55f);
                FichasDeEdificio.ObtenerTodas();

                EditorUtility.DisplayProgressBar("Tiny Tactics", "Pintando el terreno…", 0.6f);

                var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                CrearLuzGlobal();
                CrearMundo(definicion);
                camara = CrearCamara(definicion, mapa);
                PintarMapa(definicion, mapa, paleta);

                EditorUtility.DisplayProgressBar("Tiny Tactics", "Preparando la interfaz…", 0.8f);
                _tema = ConstructorDeInterfaz.ObtenerTema();

                EditorUtility.DisplayProgressBar("Tiny Tactics", "Poblando el mapa…", 0.85f);
                PoblarMapa(definicion, mapa);

                CrearPercepcion(definicion);

                ConstructorDeInterfaz.CrearLienzo(_tema, definicion.bandos);

                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena, RutaEscena);
                RegistrarEnBuildSettings();
            }
            finally
            {
                // Sin esto, una excepción a mitad de camino deja la barra de progreso
                // colgada y el editor inutilizable hasta reiniciarlo.
                EditorUtility.ClearProgressBar();
            }

            Selection.activeGameObject = camara.gameObject;
            SceneView.FrameLastActiveSceneView();

            float porcentaje = 100f * tierraTotal / (definicion.ancho * definicion.alto);
            Debug.Log(
                $"[Tiny Tactics] Escena generada en {RutaEscena}\n" +
                $"Mapa \"{definicion.nombreMapa}\": {definicion.ancho}x{definicion.alto} tiles, " +
                $"{tierraTotal} de tierra ({porcentaje:F1} %), semilla {definicion.semilla}, " +
                $"{definicion.bandos} bandos.\n" +
                $"Contenido: {mapa.Oro.Count} nodos de oro, {mapa.Arboles.Count} árboles, " +
                $"{mapa.Ovejas.Count} ovejas, {mapa.Rocas.Count} rocas, {mapa.Arbustos.Count} arbustos, " +
                $"{mapa.RocasAgua.Count} rocas de mar.\n" +
                $"Relieve {(mapa.RelieveDibujado ? "DIBUJADO A MANO" : "generado por ruido")}: " +
                $"{GeneradorTerreno.ContarElevadas(mapa.Nivel, mapa.Ancho, mapa.Alto)} " +
                $"tiles de meseta y {mapa.Rampas.Count} rampas.");
        }

        // -----------------------------------------------------------------
        // Definición del mapa
        // -----------------------------------------------------------------

        static DefinicionMapa ObtenerODefinirMapa()
        {
            AsegurarCarpeta(CarpetaDatos);
            AsegurarCarpeta(CarpetaMapas);

            var definicion = AssetDatabase.LoadAssetAtPath<DefinicionMapa>(RutaDefinicion);
            if (definicion != null) return definicion;

            definicion = ScriptableObject.CreateInstance<DefinicionMapa>();
            AssetDatabase.CreateAsset(definicion, RutaDefinicion);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Tiny Tactics] Definición de mapa creada en {RutaDefinicion}");
            return definicion;
        }

        // -----------------------------------------------------------------
        // Paleta de tiles
        // -----------------------------------------------------------------

        class Paleta
        {
            public TileBase Agua;
            public TileBase Espuma;
            public readonly Dictionary<int, TileBase> Suelo = new Dictionary<int, TileBase>();

            /// <summary>Las mismas 16 piezas con el borde rocoso, para la meseta.</summary>
            public readonly Dictionary<int, TileBase> Meseta = new Dictionary<int, TileBase>();

            public TileBase[] CaraEnTierra;
            public TileBase[] CaraEnAgua;

            /// <summary>
            /// Las dos mitades de la rampa: alta-der, alta-izq, baja-der, baja-izq.
            /// La que cae a la derecha usa 0+2; la que cae a la izquierda, 1+3.
            /// </summary>
            public TileBase[] Rampa;
        }

        static Paleta ConstruirPaleta()
        {
            var spritesTileset = CargarSpritesOrdenados(RutaTileset);
            if (spritesTileset.Count < 28)
            {
                EditorUtility.DisplayDialog(
                    "Tiny Tactics",
                    $"No pude leer el tileset en {RutaTileset}.\n" +
                    $"Esperaba al menos 28 sprites y encontré {spritesTileset.Count}.",
                    "Entendido");
                return null;
            }

            var spriteAgua = AssetDatabase.LoadAssetAtPath<Sprite>(RutaAgua);
            if (spriteAgua == null)
            {
                EditorUtility.DisplayDialog(
                    "Tiny Tactics", $"No encontré el sprite de agua en {RutaAgua}.", "Entendido");
                return null;
            }

            var framesEspuma = CargarSpritesOrdenados(RutaEspuma);

            // Regeneramos la carpeta entera: así un cambio en el mapeo de piezas no
            // deja tiles viejos apuntando al sprite equivocado.
            if (AssetDatabase.IsValidFolder(CarpetaTiles))
                AssetDatabase.DeleteAsset(CarpetaTiles);

            AsegurarCarpeta(CarpetaDatos);
            AsegurarCarpeta(CarpetaTiles);

            var paleta = new Paleta
            {
                Agua = CrearTile(spriteAgua, $"{CarpetaTiles}/Agua.asset")
            };

            foreach (int indice in GeneradorTerreno.IndicesSueloPlano)
            {
                paleta.Suelo[indice] = CrearTile(
                    spritesTileset[indice], $"{CarpetaTiles}/Suelo_{indice:D2}.asset");

                int elevado = indice + GeneradorTerreno.DesplazamientoElevado;
                if (elevado < spritesTileset.Count)
                {
                    paleta.Meseta[elevado] = CrearTile(
                        spritesTileset[elevado], $"{CarpetaTiles}/Meseta_{elevado:D2}.asset");
                }
            }

            paleta.CaraEnTierra = CrearJuego(spritesTileset, GeneradorTerreno.IndicesCaraEnTierra, "Cara");
            paleta.CaraEnAgua = CrearJuego(spritesTileset, GeneradorTerreno.IndicesCaraEnAgua, "CaraAgua");
            paleta.Rampa = CrearJuego(spritesTileset, GeneradorTerreno.IndicesRampa, "Rampa");

            if (framesEspuma.Count > 1)
                paleta.Espuma = CrearTileAnimado(framesEspuma, $"{CarpetaTiles}/Espuma.asset");
            else if (framesEspuma.Count == 1)
                paleta.Espuma = CrearTile(framesEspuma[0], $"{CarpetaTiles}/Espuma.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return paleta;
        }

        /// <summary>Convierte una lista de índices del tileset en tiles listos para pintar.</summary>
        static TileBase[] CrearJuego(List<Sprite> sprites, int[] indices, string prefijo)
        {
            var salida = new TileBase[indices.Length];

            for (int i = 0; i < indices.Length; i++)
            {
                int indice = indices[i];
                if (indice >= sprites.Count) continue;

                salida[i] = CrearTile(sprites[indice], $"{CarpetaTiles}/{prefijo}_{indice:D2}.asset");
            }

            return salida;
        }

        static Tile CrearTile(Sprite sprite, string ruta)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, ruta);
            return tile;
        }

        static TileBase CrearTileAnimado(List<Sprite> frames, string ruta)
        {
            var tile = ScriptableObject.CreateInstance<AnimatedTile>();
            tile.m_AnimatedSprites = frames.ToArray();

            // Rango de velocidad: cada tile elige la suya, así la costa entera no
            // late al unísono. Es lo que recomienda la documentación del pack.
            tile.m_MinSpeed = 0.6f;
            tile.m_MaxSpeed = 1.1f;
            tile.m_TileColliderType = Tile.ColliderType.None;

            AssetDatabase.CreateAsset(tile, ruta);
            return tile;
        }

        static List<Sprite> CargarSpritesOrdenados(string ruta)
        {
            var sprites = new List<Sprite>();

            foreach (var objeto in AssetDatabase.LoadAllAssetsAtPath(ruta))
                if (objeto is Sprite sprite) sprites.Add(sprite);

            sprites.Sort((a, b) => IndiceDeNombre(a.name).CompareTo(IndiceDeNombre(b.name)));
            return sprites;
        }

        /// <summary>Extrae el número final de nombres tipo "Tilemap_color1_17".</summary>
        static int IndiceDeNombre(string nombre)
        {
            int guion = nombre.LastIndexOf('_');
            if (guion < 0 || guion == nombre.Length - 1) return 0;
            return int.TryParse(nombre.Substring(guion + 1), out int valor) ? valor : 0;
        }

        // -----------------------------------------------------------------
        // Escena
        // -----------------------------------------------------------------

        static void CrearLuzGlobal()
        {
            // Sin una Light2D global, el renderer 2D de URP dibuja todos los sprites
            // en negro. Es el fallo silencioso más común al montar una escena por código.
            var go = new GameObject("Luz Global 2D");
            var luz = go.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Global;
            luz.intensity = 1f;
        }

        /// <summary>
        /// Objeto que reconstruye la grilla logica al arrancar la partida.
        /// Guarda la referencia a la MISMA definicion con la que se pinto la escena:
        /// si apuntaran a assets distintos, lo logico y lo visual discreparian.
        /// </summary>
        static void CrearMundo(DefinicionMapa definicion)
        {
            var go = new GameObject("Mundo");
            var mundo = go.AddComponent<MundoJuego>();
            mundo.definicion = definicion;

            // La tabla de contadores es una regla del juego, no un dato de unidad: se enchufa
            // una sola vez desde aqui y MundoJuego la publica al despertar.
            mundo.contadores = AssetDatabase.LoadAssetAtPath<TinyTactics.Datos.TablaDeContadores>(
                "Assets/Datos/Contadores.asset");

            // La economía cuelga del mismo objeto que el mundo: las dos son estado global
            // de la partida y tenerlas juntas evita buscar dónde vive cada cosa.
            var economia = go.AddComponent<Economia>();
            economia.datos = ObtenerDatosEconomia();
            economia.bandos = Mathf.Max(1, definicion.bandos);

            go.AddComponent<Sustento>();

            // La población no guarda contadores: recuenta las unidades vivas y los edificios
            // en pie. Por eso cuelga del mismo objeto y no necesita que nadie la avise.
            go.AddComponent<Poblacion>();

            // Las estadisticas se acumulan EN VIVO. Al final de la partida ya no quedan
            // cadaveres que contar, asi que si no se apunta cuando ocurre no hay de donde
            // sacarlo despues.
            go.AddComponent<EstadisticasPartida>();

            // El arbitro: quien sigue en pie y cuando se acabo.
            var arbitro = go.AddComponent<ArbitroDePartida>();
            arbitro.bandos = Mathf.Max(1, definicion.bandos);

            // Una sola plantilla por tipo y bando, que todos los edificios comparten.
            go.AddComponent<CatalogoDePlantillas>();

            // Y su gemelo para los edificios: qué se puede construir y con qué dibujo.
            go.AddComponent<CatalogoDeEdificios>();

            // El colocador vive en su propio objeto porque lleva hijos con SpriteRenderer
            // —la silueta y la mancha del suelo— y el objeto del mundo no dibuja nada.
            var colocador = new GameObject("Colocador");
            colocador.transform.SetParent(go.transform, false);
            colocador.AddComponent<ColocadorEdificios>();
        }

        /// <summary>
        /// El asset de balance económico. Se crea vacío la primera vez y luego <b>no se
        /// vuelve a tocar</b>, al revés que el catálogo de unidades.
        ///
        /// El motivo es que estos números son exactamente los que Raúl va a mover en las
        /// sesiones de balance: si regenerar la escena los devolviera a los de fábrica, cada
        /// cambio de mapa borraría una tarde de ajustes sin avisar.
        /// </summary>
        static DatosEconomia ObtenerDatosEconomia()
        {
            AsegurarCarpeta(CarpetaDatos);

            const string ruta = CarpetaDatos + "/Economia.asset";

            var datos = AssetDatabase.LoadAssetAtPath<DatosEconomia>(ruta);
            if (datos != null) return datos;

            datos = ScriptableObject.CreateInstance<DatosEconomia>();
            AssetDatabase.CreateAsset(datos, ruta);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Tiny Tactics] Datos de economía creados en {ruta}");
            return datos;
        }

        static CamaraRTS CrearCamara(DefinicionMapa definicion, MapaGenerado mapa)
        {
            var go = new GameObject("Camara Principal");
            go.tag = "MainCamera";

            var camara = go.AddComponent<Camera>();
            camara.orthographic = true;
            camara.orthographicSize = 11f;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = ColorAgua;
            camara.nearClipPlane = 0.3f;
            camara.farClipPlane = 100f;

            go.AddComponent<AudioListener>();

            var selector = go.AddComponent<SelectorDeUnidades>();
            selector.faccionJugador = 0;

            var controlador = go.AddComponent<CamaraRTS>();
            controlador.ConfigurarLimites(Vector2.zero, definicion.TamanoEnMundo);

            // El zoom de arranque es también el máximo: se puede acercar, nunca alejarse
            // más de lo que se ve al empezar la partida.
            controlador.zoomMaximo = camara.orthographicSize;
            controlador.zoomMinimo = 4f;

            // La partida empieza mirando la base propia, no el centro del mapa.
            // Es lo que hace cualquier RTS: apareces viendo lo tuyo.
            Vector2 centro = definicion.CentroEnMundo;
            if (mapa != null && mapa.Bases.Count > 0)
            {
                var propia = mapa.Bases[0];
                centro = new Vector2(propia.x + 0.5f, propia.y + 0.5f);
            }

            go.transform.position = new Vector3(centro.x, centro.y, -10f);

            return controlador;
        }

        static void PintarMapa(DefinicionMapa definicion, MapaGenerado mapa, Paleta paleta)
        {
            bool[,] tierra = mapa.Tierra;
            int ancho = definicion.ancho;
            int alto = definicion.alto;

            var raiz = new GameObject("Mapa");
            var grilla = raiz.AddComponent<Grid>();
            grilla.cellSize = new Vector3(1f, 1f, 0f);

            var capaAgua = CrearCapa(raiz.transform, "Agua", -30);
            var capaEspuma = CrearCapa(raiz.transform, "Espuma", -20);
            var capaSuelo = CrearCapa(raiz.transform, "Suelo", -10);
            var capaMeseta = CrearCapa(raiz.transform, "Meseta", -5);

            var limites = new BoundsInt(0, 0, 0, ancho, alto, 1);

            var agua = new TileBase[ancho * alto];
            var espuma = new TileBase[ancho * alto];
            var suelo = new TileBase[ancho * alto];
            var meseta = new TileBase[ancho * alto];

            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    int i = x + y * ancho;

                    agua[i] = paleta.Agua;

                    if (!tierra[x, y]) continue;

                    int indice = GeneradorTerreno.IndiceAutotileEn(tierra, ancho, alto, x, y);
                    paleta.Suelo.TryGetValue(indice, out suelo[i]);

                    if (paleta.Espuma != null &&
                        GeneradorTerreno.NecesitaEspuma(tierra, ancho, alto, x, y))
                    {
                        espuma[i] = paleta.Espuma;
                    }
                }
            }

            PintarRelieve(mapa, paleta, ancho, alto, meseta);

            capaAgua.SetTilesBlock(limites, agua);
            capaEspuma.SetTilesBlock(limites, espuma);
            capaSuelo.SetTilesBlock(limites, suelo);
            capaMeseta.SetTilesBlock(limites, meseta);
        }

        /// <summary>
        /// Pinta la meseta, la cara del acantilado y las rampas sobre una capa aparte.
        ///
        /// Va encima del suelo y no lo sustituye: bajo la meseta sigue habiendo hierba, y
        /// eso hace que el borde del acantilado encaje con lo que tiene al lado sin
        /// recalcular nada. La cara del acantilado ocupa la fila que queda justo al sur de
        /// la meseta — esa fila es terreno llano, pero el muro la vuelve intransitable, y
        /// de eso se encarga <see cref="MundoJuego"/> al construir la grilla.
        /// </summary>
        static void PintarRelieve(MapaGenerado mapa, Paleta paleta, int ancho, int alto,
                                  TileBase[] destino)
        {
            if (mapa.Nivel == null) return;

            // Índice de rampas montado una vez. EsMuro se llama cuatro veces por celda y
            // recorrer la lista dentro sería medio millón de comparaciones de más.
            _celdasDeRampa.Clear();
            foreach (var rampa in mapa.Rampas) _celdasDeRampa.Add(rampa.Pie);

            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    int i = x + y * ancho;

                    if (mapa.Nivel[x, y] > 0)
                    {
                        int indice = GeneradorTerreno.IndiceAutotileNivel(mapa.Nivel, ancho, alto, x, y);
                        paleta.Meseta.TryGetValue(indice, out destino[i]);
                        continue;
                    }

                    // Justo debajo de una meseta va la pared. Con musgo si el pie es
                    // tierra, con espuma si cae al agua.
                    if (!EsMuro(mapa, ancho, alto, x, y)) continue;

                    var juego = mapa.Tierra[x, y] ? paleta.CaraEnTierra : paleta.CaraEnAgua;
                    if (juego == null || juego.Length < 4) continue;

                    // La pieza depende de si el muro sigue a los lados, no del azar. Las
                    // cuatro no son variantes: son extremo izquierdo, medio, extremo
                    // derecho y bloque suelto.
                    int rol = GeneradorTerreno.RolDeMuro(
                        EsMuro(mapa, ancho, alto, x - 1, y),
                        EsMuro(mapa, ancho, alto, x + 1, y));

                    destino[i] = juego[rol];
                }
            }

            if (paleta.Rampa == null || paleta.Rampa.Length < 4) return;

            // Las rampas se pintan las últimas: sobrescriben la pared y el borde de meseta
            // que el bucle de arriba ya había puesto en esas dos celdas.
            foreach (var rampa in mapa.Rampas)
            {
                Poner(destino, ancho, alto, rampa.x, rampa.y + 1,
                      paleta.Rampa[rampa.derecha ? 0 : 1]);

                Poner(destino, ancho, alto, rampa.x, rampa.y,
                      paleta.Rampa[rampa.derecha ? 2 : 3]);
            }
        }

        /// <summary>
        /// ¿Esta celda lleva pared de acantilado?
        ///
        /// Lo lleva si es llano y justo encima hay meseta. Las celdas de rampa quedan
        /// fuera: ahí la pared se sustituye por la cuesta, y contarlas como muro haría que
        /// sus vecinas se dibujaran como si el muro continuara y dejaría un borde abierto
        /// mirando a la nada.
        /// </summary>
        static bool EsMuro(MapaGenerado mapa, int ancho, int alto, int x, int y)
        {
            if (x < 0 || y < 0 || x >= ancho || y + 1 >= alto) return false;
            if (mapa.Nivel[x, y] != 0 || mapa.Nivel[x, y + 1] == 0) return false;

            return !_celdasDeRampa.Contains(new Vector2Int(x, y));
        }

        static readonly HashSet<Vector2Int> _celdasDeRampa = new HashSet<Vector2Int>();

        /// <summary>
        /// Vuelve a pintar solo la capa de meseta de la escena abierta.
        ///
        /// La usa el editor de relieve para dar respuesta inmediata al pincel. Los tiles se
        /// cargan de <c>Assets/Datos/Tiles</c> en vez de volver a construir la paleta: esa
        /// borra y regenera la carpeta entera, que aquí sería carísimo y además dejaría
        /// referencias rotas en el resto de capas de la escena.
        /// </summary>
        public static void RepintarRelieveEnEscena(MapaGenerado mapa)
        {
            var objeto = GameObject.Find("Mapa/Meseta");
            if (objeto == null)
            {
                Debug.LogWarning(
                    "[Tiny Tactics] No hay capa \"Mapa/Meseta\" en la escena. " +
                    "Genera la escena de juego antes de editar el relieve.");
                return;
            }

            var capa = objeto.GetComponent<Tilemap>();
            if (capa == null) return;

            var paleta = new Paleta();

            foreach (int indice in GeneradorTerreno.IndicesSueloPlano)
            {
                int elevado = indice + GeneradorTerreno.DesplazamientoElevado;
                paleta.Meseta[elevado] =
                    AssetDatabase.LoadAssetAtPath<TileBase>($"{CarpetaTiles}/Meseta_{elevado:D2}.asset");
            }

            paleta.CaraEnTierra = CargarJuego(GeneradorTerreno.IndicesCaraEnTierra, "Cara");
            paleta.CaraEnAgua = CargarJuego(GeneradorTerreno.IndicesCaraEnAgua, "CaraAgua");
            paleta.Rampa = CargarJuego(GeneradorTerreno.IndicesRampa, "Rampa");

            var destino = new TileBase[mapa.Ancho * mapa.Alto];
            PintarRelieve(mapa, paleta, mapa.Ancho, mapa.Alto, destino);

            capa.SetTilesBlock(new BoundsInt(0, 0, 0, mapa.Ancho, mapa.Alto, 1), destino);
            EditorSceneManager.MarkSceneDirty(objeto.scene);
        }

        static TileBase[] CargarJuego(int[] indices, string prefijo)
        {
            var salida = new TileBase[indices.Length];

            for (int i = 0; i < indices.Length; i++)
                salida[i] = AssetDatabase.LoadAssetAtPath<TileBase>(
                    $"{CarpetaTiles}/{prefijo}_{indices[i]:D2}.asset");

            return salida;
        }

        static void Poner(TileBase[] destino, int ancho, int alto, int x, int y, TileBase tile)
        {
            if (x < 0 || y < 0 || x >= ancho || y >= alto) return;
            destino[x + y * ancho] = tile;
        }

        static Tilemap CrearCapa(Transform padre, string nombre, int orden)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);

            var tilemap = go.AddComponent<Tilemap>();
            var renderizador = go.AddComponent<TilemapRenderer>();
            renderizador.sortingOrder = orden;

            return tilemap;
        }

        // -----------------------------------------------------------------
        // Contenido del mapa: recursos, vegetación y ambiente
        // -----------------------------------------------------------------

        const string DirRecursos = "Assets/Tiny Swords/Pawn and Resources";
        const string DirDecoracion = "Assets/Tiny Swords/Terrain/Decorations";

        /// <summary>
        /// Desplazamiento vertical por tipo. Los sprites del pack tienen el pivote
        /// centrado, así que un árbol de 4 unidades de alto aparecería con el tronco
        /// dos unidades por debajo de su celda. Se sube para que la base apoye donde toca.
        /// </summary>
        const float AlturaArbol = 1.1f;
        const float AlturaOro = 0.2f;

        static void PoblarMapa(DefinicionMapa definicion, MapaGenerado mapa)
        {
            var rnd = new System.Random(definicion.semilla + 555);
            var raiz = new GameObject("Contenido").transform;

            _altoMapa = definicion.alto;

            var arboles = CargarVariantes(raiz, "Árboles", new[]
            {
                $"{DirRecursos}/Wood/Trees/Tree1.png",
                $"{DirRecursos}/Wood/Trees/Tree2.png",
                $"{DirRecursos}/Wood/Trees/Tree3.png",
                $"{DirRecursos}/Wood/Trees/Tree4.png",
            });

            var oro = CargarVariantes(raiz, "Oro", new[]
            {
                $"{DirRecursos}/Gold/Gold Stones/Gold Stone 1.png",
                $"{DirRecursos}/Gold/Gold Stones/Gold Stone 2.png",
                $"{DirRecursos}/Gold/Gold Stones/Gold Stone 3.png",
                $"{DirRecursos}/Gold/Gold Stones/Gold Stone 4.png",
                $"{DirRecursos}/Gold/Gold Stones/Gold Stone 5.png",
                $"{DirRecursos}/Gold/Gold Stones/Gold Stone 6.png",
            });

            var ovejas = CargarVariantes(raiz, "Ovejas", new[]
            {
                $"{DirRecursos}/Meat/Sheep/Sheep_Idle.png",
            });

            var rocas = CargarVariantes(raiz, "Rocas", new[]
            {
                $"{DirDecoracion}/Rocks/Rock1.png",
                $"{DirDecoracion}/Rocks/Rock2.png",
                $"{DirDecoracion}/Rocks/Rock3.png",
                $"{DirDecoracion}/Rocks/Rock4.png",
            });

            var arbustos = CargarVariantes(raiz, "Arbustos", new[]
            {
                $"{DirDecoracion}/Bushes/Bush 1.png",
                $"{DirDecoracion}/Bushes/Bush 2.png",
                $"{DirDecoracion}/Bushes/Bush 3.png",
                $"{DirDecoracion}/Bushes/Bush 4.png",
            });

            var rocasAgua = CargarVariantes(raiz, "Rocas de agua", new[]
            {
                $"{DirDecoracion}/Rocks in the Water/Water Rocks_01.png",
                $"{DirDecoracion}/Rocks in the Water/Water Rocks_02.png",
                $"{DirDecoracion}/Rocks in the Water/Water Rocks_03.png",
                $"{DirDecoracion}/Rocks in the Water/Water Rocks_04.png",
            });

            // Qué variante de árbol le tocó a cada uno, para darle su tocón al talarlo.
            var especies = new List<int>(mapa.Arboles.Count);

            Sembrar(mapa.Arboles, arboles, definicion.alto, rnd, AlturaArbol, elegidas: especies);
            Sembrar(mapa.Oro, oro, definicion.alto, rnd, AlturaOro);
            Sembrar(mapa.Rocas, rocas, definicion.alto, rnd, 0f);

            // Estas tres sí traen tira de frames: se animan por sprite-swap.
            Sembrar(mapa.Ovejas, ovejas, definicion.alto, rnd, 0f, animar: true, fps: 6f);
            SoltarAPastar(ovejas.Padre);
            Sembrar(mapa.Arbustos, arbustos, definicion.alto, rnd, 0f, animar: true, fps: 5f);

            // Entre el agua (-30) y la espuma (-20): se ven sobre el mar y la espuma
            // de la costa les pasa por encima.
            Sembrar(mapa.RocasAgua, rocasAgua, definicion.alto, rnd, 0f,
                    animar: true, fps: 7f, ordenExtra: -25, ordenAbsoluto: true);

            // Hasta aquí son decoración. A partir de aquí, economía: los mismos objetos
            // aprenden qué dan, cuánto les queda y qué dejan al agotarse.
            var mundo = Object.FindFirstObjectByType<MundoJuego>();

            MarcarNodos(arboles.Padre, mapa.Arboles, TipoRecurso.Madera,
                        mundo != null ? mundo.radioArbol : 1.1f, CargarTocones(),
                        especies: especies, segundosDeResto: SegundosDeTocon);

            MarcarNodos(oro.Padre, mapa.Oro, TipoRecurso.Oro,
                        mundo != null ? mundo.radioOro : 1.0f, null);

            // La oveja no bloquea la grilla porque se mueve, y marcar celdas con algo que
            // cambia de sitio ensuciaría el pathfinding sin arreglar nada.
            MarcarNodos(ovejas.Padre, mapa.Ovejas, TipoRecurso.Carne, 0f, null, seMueve: true);

            // Piedras y arbustos: estorbos que el pawn puede quitar. No dan nada y no
            // bloquean el paso —nunca lo hicieron—, pero sí estorban para construir, que es
            // justo lo que hace que despejar el terreno signifique algo.
            //
            // El recurso que llevan no es lo que dan, sino con qué se pican: el pico para la
            // piedra y el hacha para la maleza.
            MarcarEstorbos(rocas.Padre, mapa.Rocas, TipoRecurso.Oro);
            MarcarEstorbos(arbustos.Padre, mapa.Arbustos, TipoRecurso.Madera);

            // El rebaño pasta, o sea que se mueve: mismo tratamiento que las unidades.
            for (int i = 0; i < ovejas.Padre.childCount; i++)
                ovejas.Padre.GetChild(i).gameObject
                      .AddComponent<OrdenPorProfundidad>().alto = definicion.alto;

            PrepararCriadero(mundo, ovejas.Padre, mapa, definicion.alto);

            // Plantillas para que el editor pueda sembrar en caliente.
            PrepararSemillas(raiz, new[]
            {
                (SemillaMapa.Arbol, arboles.Padre),
                (SemillaMapa.Oro, oro.Padre),
                (SemillaMapa.Piedra, rocas.Padre),
                (SemillaMapa.Arbusto, arbustos.Padre),
                (SemillaMapa.Oveja, ovejas.Padre),
            }, definicion.alto);

            ColocarBases(raiz, mapa, definicion.alto, rnd);
            SembrarNubes(raiz, definicion, rnd);

            if (definicion.patitoDeGoma)
                SoltarPatito(raiz, definicion, mapa, rnd);
        }

        // -----------------------------------------------------------------
        // Nodos de recurso
        // -----------------------------------------------------------------

        /// <summary>
        /// Convierte en nodos explotables los objetos que <see cref="Sembrar"/> acaba de
        /// crear.
        /// </summary>
        /// <remarks>
        /// Se emparejan hijo e índice porque <c>Sembrar</c> recorre la lista de celdas en
        /// orden y crea exactamente un objeto por celda. Es la misma pareja que ya usa
        /// <see cref="SoltarAPastar"/>, y depende de ese contrato: si algún día se
        /// sembraran objetos salteados habría que guardar la celda en el propio objeto.
        /// </remarks>
        /// <summary>Cuánto aguanta un tocón en el mapa antes de irse.</summary>
        const float SegundosDeTocon = 30f;

        /// <summary>
        /// Radio con el que una piedra o un arbusto tapan la grilla.
        /// </summary>
        /// <remarks>
        /// Por debajo de 0,5 solo cae su propia celda: el marcado toma las celdas cuyo centro
        /// queda dentro del radio, y la vecina más próxima está a distancia 1. Es el mismo
        /// truco que usan el árbol (0,7) y la veta (0,6), que también tapan un solo cuadro.
        ///
        /// Da además el alcance de trabajo: el pawn pica desde <c>alcanceTrabajo + radio</c>,
        /// o sea 1,65, y la diagonal desde la celda de al lado son 1,41. Justo, pero cabe.
        /// </remarks>
        const float RadioEstorbo = 0.45f;

        /// <summary>
        /// Piedras y arbustos: se pueden quitar, no dan nada y estorban para construir.
        /// </summary>
        /// <remarks>
        /// Son nodos como los demás y no un sistema aparte a propósito. Despejar es el mismo
        /// gesto que talar —ir, dar unos golpes y que desaparezca— así que reutiliza el
        /// ciclo del recolector entero: la orden contextual, el resaltado del cursor y el
        /// reparto de varios pawns sobre lo mismo ya funcionan sin escribir nada.
        /// </remarks>
        static void MarcarEstorbos(Transform padre, List<Vector2Int> celdas, TipoRecurso herramienta)
        {
            if (padre == null || celdas == null) return;

            int total = Mathf.Min(padre.childCount, celdas.Count);

            for (int i = 0; i < total; i++)
            {
                var nodo = padre.GetChild(i).gameObject.AddComponent<NodoRecurso>();
                nodo.recurso = herramienta;
                nodo.celda = celdas[i];

                // Tapan SU celda, igual que un árbol o una veta.
                //
                // Se probó dejándolos sin bloquear —total, son decoración— y el resultado fue
                // que el pawn se plantaba literalmente encima de la piedra a picarla, porque
                // la celda estaba libre y era la más cercana. Con el terreno tapado se arrima
                // por fuera, que es lo que ya hacía bien con los árboles.
                //
                // De paso, «despejar la zona» pasa a significar algo también para el paso, y
                // no solo para construir. Son cuadros sueltos —medio por ciento de piedras y
                // uno y pico de arbustos— así que no forman muros ni cierran caminos.
                nodo.radioBloqueo = RadioEstorbo;
                nodo.soloDespejar = true;
                nodo.extraccionesFijas = 1;
            }
        }

        /// <summary>
        /// Deja una plantilla apagada de cada cosa sembrable, para el editor en caliente.
        /// </summary>
        /// <remarks>
        /// <b>Clona nodos que YA estan en el mapa</b> en vez de construir plantillas desde
        /// cero, y esa es toda la gracia. Configurar un arbol requiere acertar con el
        /// recurso, el radio de bloqueo, los tocones, los segundos de resto y la especie; una
        /// segunda copia de ese codigo se desincroniza del original a la primera correccion y
        /// el editor empezaria a producir arboles que no se comportan como los del generador.
        /// Copiando uno de verdad, la plantilla es correcta por construccion.
        ///
        /// Se toman hasta cuatro variantes de cada tipo, tomadas espaciadas a lo largo de los
        /// hijos: los arboles del pack son cuatro especies distintas y agarrar los cuatro
        /// primeros del mapa daria cuatro veces la misma con mucha probabilidad.
        /// </remarks>
        static void PrepararSemillas(Transform raiz,
                                     (SemillaMapa semilla, Transform padre)[] fuentes,
                                     int alto)
        {
            var mundo = Object.FindFirstObjectByType<MundoJuego>();
            if (mundo == null) return;

            var catalogo = mundo.GetComponent<CatalogoDeRecursos>();
            if (catalogo == null) catalogo = mundo.gameObject.AddComponent<CatalogoDeRecursos>();

            var carpeta = new GameObject("PlantillasRecurso").transform;
            carpeta.SetParent(raiz, false);

            const int Maximo = 4;

            foreach (var (semilla, padre) in fuentes)
            {
                if (padre == null || padre.childCount == 0) continue;

                int cuantas = Mathf.Min(Maximo, padre.childCount);
                int paso = Mathf.Max(1, padre.childCount / cuantas);

                for (int v = 0; v < cuantas; v++)
                {
                    var origen = padre.GetChild(v * paso);
                    if (origen == null) continue;

                    var copia = Object.Instantiate(origen.gameObject, carpeta);
                    copia.name = $"Semilla_{semilla}_{v}";

                    // Fuera de la pantalla y apagada: una plantilla encima del mapa se ve
                    // como un objeto duplicado en cualquier gizmo del editor.
                    copia.transform.position = new Vector3(-60f, -60f, 0f);

                    var profundidad = copia.GetComponent<OrdenPorProfundidad>();
                    if (profundidad != null) profundidad.alto = alto;

                    copia.SetActive(false);
                    catalogo.Registrar(semilla, v, copia);
                }
            }
        }

        static void MarcarNodos(Transform padre, List<Vector2Int> celdas, TipoRecurso recurso,
                                float radioBloqueo, Sprite[] restos, bool seMueve = false,
                                List<int> especies = null, float segundosDeResto = 0f)
        {
            if (padre == null || celdas == null) return;

            int total = Mathf.Min(padre.childCount, celdas.Count);

            for (int i = 0; i < total; i++)
            {
                var nodo = padre.GetChild(i).gameObject.AddComponent<NodoRecurso>();
                nodo.recurso = recurso;
                nodo.celda = celdas[i];
                nodo.radioBloqueo = radioBloqueo;
                nodo.seMueve = seMueve;
                nodo.segundosDeResto = segundosDeResto;

                if (restos == null || restos.Length == 0) continue;

                // Cada árbol se queda SOLO con el tocón de su especie.
                //
                // Antes se le pasaban los cuatro y al talarlo se sorteaba uno. El sorteo era
                // el error: el pack dibuja Tree1 y Tree2 en lienzos de 192x256 y Tree3 y
                // Tree4 en lienzos de 192x192, así que un pino talado que sacara el tocón de
                // un roble aparecía desplazado un tercio de tile, y encima era un tocón de
                // otro árbol. Emparejados, el tocón cae exactamente donde estaba el tronco.
                if (especies != null && i < especies.Count)
                {
                    int cual = Mathf.Clamp(especies[i], 0, restos.Length - 1);

                    // Sin tocón para esa especie, el árbol desaparece al talarlo. Es mejor
                    // que plantarle el de otro: un hueco se lee como un claro en el bosque.
                    if (restos[cual] != null) nodo.Configurar(new[] { restos[cual] });
                    continue;
                }

                nodo.Configurar(restos);
            }
        }

        /// <summary>
        /// Monta el criadero: una oveja apagada de molde y los focos donde pueden nacer.
        /// </summary>
        /// <remarks>
        /// La plantilla se saca <b>clonando una oveja ya sembrada</b> en vez de construir
        /// otra desde cero. Así se hereda de un golpe todo lo que ya se le hizo —las tres
        /// tiras de animación resueltas a sprites, el pastoreo, el nodo de recurso— y, sobre
        /// todo, es imposible que el molde y las ovejas del mapa acaben siendo cosas
        /// distintas porque alguien tocó una de las dos ramas y se olvidó de la otra.
        /// </remarks>
        static void PrepararCriadero(MundoJuego mundo, Transform padreOvejas,
                                     MapaGenerado mapa, int alto)
        {
            if (mundo == null || padreOvejas == null || padreOvejas.childCount == 0)
            {
                Debug.LogWarning("[Tiny Tactics] Sin ovejas sembradas: no hay criadero.");
                return;
            }

            var molde = Object.Instantiate(padreOvejas.GetChild(0).gameObject);
            molde.name = "PlantillaOveja";
            molde.transform.SetParent(mundo.transform, false);
            molde.SetActive(false);

            // Los focos: cada base, cada expansión y una muestra repartida de donde ya
            // pastaba el rebaño original. Con solo las bases, el centro del mapa se quedaría
            // pelado en cuanto se sacrificaran las primeras.
            var focos = new List<Vector2Int>();
            focos.AddRange(mapa.Bases);
            focos.AddRange(mapa.Expansiones);

            int paso = Mathf.Max(1, mapa.Ovejas.Count / 10);
            for (int i = 0; i < mapa.Ovejas.Count; i += paso) focos.Add(mapa.Ovejas[i]);

            var criadero = mundo.gameObject.AddComponent<CriaderoOvejas>();
            criadero.Configurar(molde, padreOvejas, focos);
            criadero.mundoAlto = alto;
        }

        /// <summary>
        /// Los tocones que quedan donde había un árbol.
        ///
        /// Es el único de los tres recursos que deja rastro, y no es capricho: un bosque que
        /// se evapora borra la historia de la partida, mientras que un claro lleno de
        /// tocones dice de un vistazo que por ahí ya pasó alguien.
        /// </summary>
        /// <summary>
        /// Los cuatro tocones, <b>en el mismo orden que los cuatro árboles</b>.
        /// </summary>
        /// <remarks>
        /// El índice es el contrato: <c>Stump 3</c> es el tocón de <c>Tree3</c>. Por eso un
        /// tocón que falte deja un hueco en vez de encoger la lista — al encogerla, perder
        /// <c>Stump 2</c> haría que el tocón del tercer árbol pasara a ser el del cuarto y
        /// todos los árboles del mapa quedarían emparejados con el tocón equivocado, sin
        /// un solo mensaje de error.
        /// </remarks>
        static Sprite[] CargarTocones()
        {
            var salida = new Sprite[4];
            int encontrados = 0;

            for (int i = 0; i < salida.Length; i++)
            {
                var lista = CargarSpritesOrdenados($"{DirRecursos}/Wood/Trees/Stump {i + 1}.png");
                if (lista.Count == 0) continue;

                salida[i] = lista[0];
                encontrados++;
            }

            if (encontrados < salida.Length)
                Debug.LogWarning(
                    $"[Tiny Tactics] Solo encuentro {encontrados} de 4 tocones; los árboles " +
                    "de las especies que falten desaparecerán al talarlos.");

            return salida;
        }

        // -----------------------------------------------------------------
        // Unidades
        // -----------------------------------------------------------------

        /// <summary>
        /// Composición de la escuadra inicial de cada bando: <b>dos pawns y nada más</b>.
        /// </summary>
        /// <remarks>
        /// Eran diez unidades, dos de cada tipo. Ese reparto era andamio: hasta esta semana
        /// no se podía entrenar nada, así que la única forma de ver animarse a un lancero
        /// era regalarlo al empezar.
        /// </remarks>
        /// <remarks>
        /// Ahora hay cuartel, campo de tiro y monasterio, así que regalar el ejército
        /// entero no solo sobra: <b>estorba</b>. Con diez unidades de salida y un tope de
        /// diez, el jugador empieza la partida sin sitio para nada y lo primero que aprende
        /// es que el botón de entrenar no funciona. Dos pawns dejan ocho puntos libres, que
        /// es espacio exacto para la apertura: recolectar, levantar una casa y empezar a
        /// producir.
        /// </remarks>
        static readonly TipoUnidad[] EscuadraInicial =
        {
            TipoUnidad.Pawn, TipoUnidad.Pawn,
        };

        /// <summary>
        /// Tema de interfaz de la generacion en curso. Es un campo estatico y no un
        /// parametro porque lo necesita <see cref="CrearUnidad"/>, cinco llamadas mas
        /// abajo, y encadenarlo por toda la cadena solo ensuciaria firmas.
        /// </summary>
        static TemaInterfaz _tema;

        /// <summary>Alto del mapa en curso. Lo necesita el orden por profundidad.</summary>
        static int _altoMapa = 128;

        /// <summary>
        /// Poste de entrenamiento delante de la escuadra: una oveja roja e indestructible.
        ///
        /// Es andamio de pruebas, no diseño de juego. Está aquí porque sin enemigos reales
        /// —que llegan con la épica E06— no habría manera de ver la animación de ataque ni
        /// la de muerte, y las dos se entregan esta semana. Se borra este método entero
        /// cuando haya con quién pelear de verdad.
        /// </summary>
        static void CrearMuneco(Transform grupo, Vector2Int celda, int alto)
        {
            var datos = CatalogoDeUnidades.ObtenerMuneco();

            var tiras = CatalogoDeUnidades.CargarTiras(
                datos, CatalogoDeUnidades.FaccionNeutral, CargarSpritesOrdenados);

            if (tiras == null) return;

            var go = new GameObject("MunecoDePruebas");
            go.transform.SetParent(grupo, false);
            // Centrado y por debajo de las dos filas de la escuadra: a tiro de todas, y
            // lo bastante lejos para que se vea a las de alcance largo acercarse.
            go.transform.position = new Vector3(celda.x + 0.5f, celda.y - 7.5f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tiras[0].frames[0];
            sr.sortingOrder = OrdenPorProfundidad.Calcular(alto, celda.y) + 3;

            // Teñida para que se distinga de las ovejas de verdad que pastan por el mapa.
            sr.color = new Color(1f, 0.45f, 0.45f);

            go.AddComponent<AnimadorSprite>();

            var unidad = go.AddComponent<Unidad>();
            unidad.Configurar(datos, CatalogoDeUnidades.FaccionNeutral);

            // Sin MovimientoUnidad a propósito: es un poste, no una unidad.
            go.AddComponent<MaquinaDeEstados>().Configurar(tiras, ObtenerPolvo(), ObtenerEfectoCura(), ObtenerFlecha());

            ConstructorDeInterfaz.AnadirBarraDeVida(go, _tema, alto - celda.y + 3);
        }

        // Efectos compartidos. Se cargan una vez por generación, no por unidad.
        static Sprite[] _polvo;
        static Sprite[] _efectoCura;
        static Sprite _flecha;
        static Sprite[] _explosion;

        static Sprite[] ObtenerExplosion()
        {
            if (_explosion == null)
                _explosion = CargarSpritesOrdenados(
                    "Assets/Tiny Swords/Particle FX/Explosion_01.png").ToArray();

            return _explosion;
        }

        static Sprite[] ObtenerPolvo()
        {
            if (_polvo == null)
                _polvo = CargarSpritesOrdenados(
                    "Assets/Tiny Swords/Particle FX/Dust_01.png").ToArray();

            return _polvo;
        }

        static Sprite[] ObtenerEfectoCura()
        {
            if (_efectoCura == null)
                _efectoCura = CargarSpritesOrdenados(
                    "Assets/Tiny Swords/Units/Extra/Heal Effect/Heal_Effect.png").ToArray();

            return _efectoCura;
        }

        static Sprite ObtenerFlecha()
        {
            if (_flecha == null)
            {
                var frames = CargarSpritesOrdenados(
                    "Assets/Tiny Swords/Units/Extra/Arrow/Arrow.png");

                _flecha = frames.Count > 0 ? frames[0] : null;
            }

            return _flecha;
        }

        static GameObject CrearUnidad(Transform padre, DatosUnidad datos, int faccion,
                                      MaquinaDeEstados.Tira[] tiras,
                                      Vector3 posicion, int orden, string nombre)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            go.transform.position = posicion;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tiras[0].frames[0];
            sr.sortingOrder = orden;

            go.AddComponent<AnimadorSprite>();

            var unidad = go.AddComponent<Unidad>();
            unidad.Configurar(datos, faccion);

            // Las unidades andan, asi que su profundidad no se puede dejar fijada aqui.
            var profundidad = go.AddComponent<OrdenPorProfundidad>();
            profundidad.alto = _altoMapa;
            profundidad.extra = 2;

            go.AddComponent<MovimientoUnidad>();

            // La máquina va después del movimiento: en Awake lo busca por GetComponent
            // para saber si la unidad está andando.
            go.AddComponent<MaquinaDeEstados>().Configurar(tiras, ObtenerPolvo(), ObtenerEfectoCura(), ObtenerFlecha());

            // Solo el pawn recolecta. Que el componente exista únicamente donde tiene
            // sentido es además lo que hace que un guerrero ignore la orden de talar sin
            // ninguna comprobación de tipo por el medio.
            if (datos != null && datos.tipo == TipoUnidad.Pawn && !datos.invulnerable)
            {
                go.AddComponent<RecolectorPawn>();

                // Recolectar y construir son dos faenas del mismo pawn, y por eso son dos
                // componentes: se interrumpen distinto y terminan distinto. Cada uno cancela
                // al otro al empezar, que es lo único que tienen que saber el uno del otro.
                go.AddComponent<ConstructorPawn>();
            }

            // Marcador y barra salen del pack, no de una textura dibujada por codigo.
            ConstructorDeInterfaz.AnadirMarcador(go, _tema, faccion, orden);
            ConstructorDeInterfaz.AnadirBarraDeVida(go, _tema, orden);

            return go;
        }

        /// <summary>
        /// Da vida al rebano: cada oveja alterna quieta, pastando y andando.
        ///
        /// El pack trae ademas un Animator con sus clips, pero no se usa — el ADR-03 lo
        /// prohibe. Aqui se le pasan las tres tiras a nuestro animador por intercambio
        /// de sprites, que es lo mismo por una fraccion del coste.
        /// </summary>
        static void SoltarAPastar(Transform padre)
        {
            if (padre == null) return;

            var quieta = CargarSpritesOrdenados($"{DirRecursos}/Meat/Sheep/Sheep_Idle.png");
            var pastando = CargarSpritesOrdenados($"{DirRecursos}/Meat/Sheep/Sheep_Grass.png");
            var andando = CargarSpritesOrdenados($"{DirRecursos}/Meat/Sheep/Sheep_Move.png");

            if (quieta.Count == 0) return;
            if (pastando.Count == 0) pastando = quieta;
            if (andando.Count == 0) andando = quieta;

            for (int i = 0; i < padre.childCount; i++)
            {
                var oveja = padre.GetChild(i).gameObject;

                if (oveja.GetComponent<AnimadorSprite>() == null)
                    oveja.AddComponent<AnimadorSprite>();

                oveja.AddComponent<PastarOveja>()
                     .Configurar(quieta.ToArray(), pastando.ToArray(), andando.ToArray());

                // Sembrar voltea algunas invirtiendo la escala. Eso arrastraria a
                // cualquier hijo y ademas pelea con el flipX que usa el pastoreo.
                var escala = oveja.transform.localScale;
                if (escala.x < 0f)
                {
                    oveja.transform.localScale = new Vector3(Mathf.Abs(escala.x), escala.y, escala.z);

                    var sr = oveja.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.flipX = true;
                }
            }
        }

        /// <summary>Un patito de goma flotando en el mar. Viene en el pack; sería una pena no usarlo.</summary>
        static void SoltarPatito(Transform raiz, DefinicionMapa definicion, MapaGenerado mapa, System.Random rnd)
        {
            var frames = CargarSpritesOrdenados($"{DirDecoracion}/Rubber Duck/Rubber duck.png");
            if (frames.Count == 0 || mapa.RocasAgua.Count == 0) return;

            var celda = mapa.RocasAgua[rnd.Next(mapa.RocasAgua.Count)];

            var go = new GameObject("Patito");
            go.transform.SetParent(raiz, false);
            go.transform.position = new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = -24;

            if (frames.Count > 1)
                go.AddComponent<AnimadorSprite>().Configurar(frames.ToArray(), 5f, 0);
        }

        /// <summary>Nombres de color de facción. La lista vive en el catálogo de unidades.</summary>
        const string DirUnidadesPack = "Assets/Tiny Swords/Units";

        static string[] ColoresFaccion => CatalogoDeUnidades.Colores;

        /// <summary>
        /// Castillo y dos pawns en cada punto de aparición.
        ///
        /// Todavía no hay lógica detrás — son sprites. Pero convierten una captura de
        /// "un mapa generado" en una de "una partida a punto de empezar", que es lo que
        /// hay que enseñar el lunes.
        /// </summary>
        /// <summary>
        /// Cuantas torres se le ponen a cada bando rival. Cero las quita.
        /// </summary>
        public static int TorresRivales = 3;

        /// <summary>
        /// Si se planta el poste de entrenamiento en cada base. Apagado desde la HU-053.
        /// </summary>
        /// <remarks>
        /// Era el andamio que permitia probar daño y muerte cuando no habia enemigos. Con dos
        /// bandos que se pegan de verdad, torres rivales y un panel que deja cambiarse de
        /// bando, ya sobra.
        ///
        /// Se deja como interruptor y no se borra el metodo por lo mismo que las torres: un
        /// codigo comentado es basura que nadie se atreve a quitar, y una opcion apagada es
        /// una decision que se puede revisar. Ademas seguira sirviendo en la semana 16, para
        /// medir daño por segundo sin que el blanco se defienda.
        /// </remarks>
        public static bool PosteDePruebas;

        /// <summary>
        /// Donde va cada torre respecto a la celda del castillo.
        /// </summary>
        /// <remarks>
        /// Los desplazamientos estan fuera de la planta del castillo a proposito: mide 5x3
        /// centrado, asi que ocupa de -2 a +2 en X y de -1 a +1 en Y. Dos torres flanqueando
        /// la entrada y una cubriendo la espalda es la forma en que cualquiera fortifica una
        /// base, y ademas deja el frente batido desde dos angulos.
        /// </remarks>
        static readonly Vector2Int[] SitiosDeTorre =
        {
            new Vector2Int(-5, -3),
            new Vector2Int(5, -3),
            new Vector2Int(0, 4),
        };

        static void ColocarTorres(Transform grupo, int faccion, Vector2Int celda, int alto)
        {
            var ficha = FichasDeEdificio.Obtener(TipoEdificio.Torre);
            if (ficha == null) return;

            int cuantas = Mathf.Min(TorresRivales, SitiosDeTorre.Length);

            for (int t = 0; t < cuantas; t++)
            {
                var donde = celda + SitiosDeTorre[t];
                CrearEdificio(grupo, ficha, faccion, donde, alto, $"Torre_{t + 1}");
            }
        }

        static void ColocarBases(Transform raiz, MapaGenerado mapa, int alto, System.Random rnd)
        {
            var padre = new GameObject("Bases").transform;
            padre.SetParent(raiz, false);

            for (int i = 0; i < mapa.Bases.Count; i++)
            {
                string color = ColoresFaccion[i % ColoresFaccion.Length];
                var celda = mapa.Bases[i];

                var grupo = new GameObject($"Bando{i + 1}_{color}").transform;
                grupo.SetParent(padre, false);

                CrearEdificio(grupo, FichasDeEdificio.Obtener(TipoEdificio.Castillo),
                              i, celda, alto, "Castillo");

                // Unidades reales: seleccionables, animadas y capaces de recibir órdenes.
                for (int p = 0; p < EscuadraInicial.Length; p++)
                {
                    var tipo = EscuadraInicial[p];
                    var datos = CatalogoDeUnidades.Obtener(tipo);

                    var tiras = CatalogoDeUnidades.CargarTiras(datos, i, CargarSpritesOrdenados);
                    if (tiras == null) continue;

                    // En fila delante del castillo y centradas sobre él, con holgura para
                    // que el empuje blando no las tenga peleando desde el primer frame.
                    //
                    // El reparto se calcula sobre el tamaño de la escuadra en vez de dar por
                    // hecho que son cinco por fila: con dos pawns, la fórmula antigua los
                    // dejaba a los dos pegados al borde izquierdo del castillo.
                    float dx = (p - (EscuadraInicial.Length - 1) * 0.5f) * 1.7f;
                    float dy = -2.6f;

                    CrearUnidad(grupo, datos, i, tiras,
                                new Vector3(celda.x + 0.5f + dx, celda.y + dy, 0f),
                                alto - celda.y + 2, $"{tipo}_{p + 1}");
                }

                // Torres solo en las bases que NO son la del jugador. Es un banco de pruebas
                // para el combate: sin IA hasta la semana 10, no habria forma de ver una
                // defensa reaccionando sola.
                //
                // Es una OPCION del generador y no una constante escrita a fuego, porque en
                // la semana 10 la IA decidira ella misma si construye torres y entonces esto
                // se apaga sin tocar una linea de codigo.
                if (i != 0 && TorresRivales > 0) ColocarTorres(grupo, i, celda, alto);

                PrepararProduccion(grupo, i, alto - celda.y + 2);

                if (PosteDePruebas) CrearMuneco(grupo, celda, alto);
            }

            // Marcadores de expansión: sin sprite, solo referencia para la IA y el diseño.
            for (int i = 0; i < mapa.Expansiones.Count; i++)
            {
                var celda = mapa.Expansiones[i];
                var go = new GameObject($"Expansion_{i + 1}");
                go.transform.SetParent(padre, false);
                go.transform.position = new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f);
            }
        }

        /// <summary>
        /// Monta un edificio completo: dibujo, huella, selección, producción y barra de obra.
        /// </summary>
        /// <remarks>
        /// Una sola función para el castillo que viene con el mapa y para las plantillas de
        /// lo que el jugador construye. Eran dos caminos distintos y ahí estaba la trampa:
        /// cualquier detalle que se arreglara en uno —el corchete de selección, el orden de
        /// dibujado, el punto de salida— habría que acordarse de arreglarlo en el otro, y la
        /// única señal de haberlo olvidado sería una casa que se ve rara en la captura del
        /// domingo.
        /// </remarks>
        static GameObject CrearEdificio(Transform padre, DatosEdificio datos, int faccion,
                                        Vector2Int celda, int alto, string nombre,
                                        int variante = 0)
        {
            if (datos == null) return null;

            string color = ColoresFaccion[faccion % ColoresFaccion.Length];
            string ruta = datos.RutaDe(color, variante);

            var sprites = CargarSpritesOrdenados(ruta);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[Tiny Tactics] No encuentro {ruta}.");
                return null;
            }

            Vector2 dibujo = datos.HuellaDe(variante);
            Vector2 centroDibujo = datos.HuellaCentroDe(variante);

            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];

            var edificio = go.AddComponent<Edificio>();

            // El retrato es el propio edificio. No hace falta dibujar un icono aparte: lo
            // que se ve en el panel es exactamente lo que hay en el mapa.
            edificio.retrato = sprites[0];

            // Las unidades nuevas salen por debajo de la planta, lejos de donde se
            // amontonan los pawns que vienen a depositar.
            edificio.puntoSalida = new Vector2(0f, -(datos.planta.y * 0.5f + 1.4f));

            // Los sprites SI se serializan, asi que la explosion se puede dejar puesta
            // desde el generador. Los delegados de la barra de vida no, y por eso aquellos
            // se enchufan en ejecucion y estos no.
            edificio.ConfigurarEfectos(ObtenerExplosion());

            // Guarnicion. El arquero se dibuja con el color del bando, igual que las tropas:
            // una torre amarilla con un arquero azul encima se leeria como capturada.
            if (datos.guarnicion)
            {
                go.AddComponent<TorreDefensiva>().Configurar(
                    CargarSpritesOrdenados($"{DirUnidadesPack}/{color} Units/Archer/Archer_Idle.png").ToArray(),
                    CargarSpritesOrdenados($"{DirUnidadesPack}/{color} Units/Archer/Archer_Shoot.png").ToArray(),
                    ObtenerFlecha());
            }

            var celdas = datos.CeldasDesde(celda);
            edificio.Colocar(datos, faccion, celdas, variante);

            // Un edificio se ordena por donde SE APOYA, no por el centro de su dibujo. El
            // castillo mide tres tiles de alto: usando su centro, todo lo que pasara por
            // delante de la puerta se dibujaba detrás del muro (ADR-12).
            //
            // El desplazamiento sale de la huella medida y no de la posición, para que
            // valga igual cuando el jugador plante el edificio en otro sitio en partida.
            float aLaBase = dibujo.y * 0.5f - centroDibujo.y;

            var profundidad = go.AddComponent<OrdenPorProfundidad>();
            profundidad.alto = alto;
            profundidad.desplazamientoY = aLaBase;

            int orden = OrdenPorProfundidad.Calcular(alto, go.transform.position.y - aLaBase);
            sr.sortingOrder = orden;

            go.AddComponent<ProduccionEdificio>();

            ConstructorDeInterfaz.AnadirMarcador(go, _tema, faccion, orden);

            // El corchete viene dimensionado para una unidad y hay que estirarlo a la huella
            // MEDIDA del edificio. El tamaño va por el campo del componente y no tocando la
            // escala del transform porque la animación de cierre reescribe la escala entera
            // al encenderse: puesta a mano duraría exactamente hasta el primer clic.
            var anillo = go.transform.Find("Seleccion");
            if (anillo != null)
            {
                anillo.localPosition = new Vector3(centroDibujo.x, centroDibujo.y - 0.03f, 0f);

                // El corchete del pack mide 1,33 unidades de ancho (128 px a 96 ppu).
                var marcador = anillo.GetComponent<MarcadorSeleccion>();
                if (marcador != null) marcador.escalaBase = dibujo.x / 1.33f;
            }

            // Barra al pie del edificio. Sirve para las dos cosas: el progreso mientras se
            // levanta y la vida cuando ya está en pie.
            //
            // El objeto se queda ENCENDIDO, al revés que antes. Apagarlo era lo correcto
            // mientras un edificio terminado no tuviera vida, pero ahora sí la tiene, y un
            // objeto apagado no ejecuta su LateUpdate: la barra nunca llegaría a aparecer al
            // recibir el primer golpe. Quien decide si se ve es el propio componente, que
            // enciende y apaga los dos SpriteRenderer según haga falta.
            ConstructorDeInterfaz.AnadirBarraDeVida(go, _tema, orden);

            var barra = go.transform.Find("Vida");
            if (barra != null)
            {
                barra.localPosition = new Vector3(0f, -aLaBase - 0.45f, 0f);
                barra.localScale = new Vector3(0.9f, 0.9f, 1f);
                barra.gameObject.SetActive(true);
            }

            return go;
        }

        /// <summary>
        /// Deja lista la plantilla con la que el castillo fabrica pawns.
        /// </summary>
        /// <remarks>
        /// Es una unidad de verdad, creada con el mismo código que las demás, pero apagada.
        /// Se hace así porque las animaciones se resuelven a sprites con
        /// <c>AssetDatabase</c>, que solo existe en el editor: un pawn construido en caliente
        /// saldría sin un solo dibujo. Apagada no se registra, no se puede seleccionar y no
        /// consume carne — para el juego, no está.
        /// </remarks>
        static void PrepararProduccion(Transform grupo, int faccion, int orden)
        {
            var edificio = grupo.GetComponentInChildren<Edificio>();
            if (edificio == null) return;

            var catalogo = Object.FindFirstObjectByType<CatalogoDePlantillas>();
            if (catalogo == null) return;

            // Una plantilla por TIPO y bando, no por edificio. El cuartel entrena guerreros y
            // lanceros, el campo de tiro arqueros: con plantillas por edificio habría copias
            // repetidas de la misma unidad y bastaría regenerar mal una para que dos guerreros
            // del mismo bando dejaran de ser iguales.
            var plantillas = new GameObject($"Plantillas_{faccion}").transform;
            plantillas.SetParent(grupo, false);

            foreach (TipoUnidad tipo in System.Enum.GetValues(typeof(TipoUnidad)))
            {
                if (catalogo.Obtener(tipo, faccion) != null) continue;

                var datos = CatalogoDeUnidades.Obtener(tipo);
                var tiras = CatalogoDeUnidades.CargarTiras(datos, faccion, CargarSpritesOrdenados);
                if (tiras == null) continue;

                var plantilla = CrearUnidad(plantillas, datos, faccion, tiras,
                                            edificio.PuntoDeSalida, orden, $"Plantilla{tipo}");

                plantilla.SetActive(false);
                catalogo.Registrar(tipo, faccion, plantilla);
            }

            PrepararEdificios(grupo, faccion, plantillas);
        }

        /// <summary>
        /// Una copia apagada de cada edificio construible, en el color del bando.
        /// </summary>
        /// <remarks>
        /// Mismo motivo que las plantillas de unidad, y la misma trampa evitada: el dibujo
        /// de cada edificio se resuelve con <c>AssetDatabase</c>, que no existe en partida.
        /// Sin esto, la casa que el jugador levantara en el minuto diez saldría sin sprite.
        ///
        /// El castillo también entra, aunque hoy no se construya. Cuesta lo mismo y el día
        /// que haya expansiones el catálogo ya lo tendrá.
        /// </remarks>
        static void PrepararEdificios(Transform grupo, int faccion, Transform plantillas)
        {
            var catalogo = Object.FindFirstObjectByType<CatalogoDeEdificios>();
            if (catalogo == null) return;

            // Fuera de la pantalla. Un edificio apagado no se dibuja, pero dejarlo encima
            // de la base hace que cualquier gizmo del editor parezca un objeto duplicado.
            var celdaMolde = new Vector2Int(-50, -50);

            foreach (var ficha in FichasDeEdificio.ObtenerTodas())
            {
                if (ficha == null) continue;

                // Una plantilla por FACHADA. Las tres casas del pack son tres dibujos del
                // mismo edificio, y el jugador elige cuál con la rueda al colocarla.
                for (int v = 0; v < ficha.Fachadas; v++)
                {
                    if (catalogo.PlantillaDe(ficha, faccion, v) != null) continue;

                    var plantilla = CrearEdificio(plantillas, ficha, faccion, celdaMolde,
                                                  _altoMapa, $"Plantilla{ficha.tipo}_{v}", v);

                    if (plantilla == null) continue;

                    // Sin celdas no reclama terreno al encenderse: se las pone el colocador.
                    var edificio = plantilla.GetComponent<Edificio>();
                    if (edificio != null) edificio.celdas = new RectInt(0, 0, 0, 0);

                    plantilla.SetActive(false);

                    var sr = plantilla.GetComponent<SpriteRenderer>();
                    catalogo.Registrar(ficha, faccion, v, sr != null ? sr.sprite : null, plantilla);
                }
            }
        }

        /// <summary>
        /// Grupo con las variantes de un tipo de objeto. Cada variante conserva su
        /// <b>tira completa</b> de frames: los arbustos, por ejemplo, son cuatro
        /// animaciones distintas, no cuatro sprites de la misma.
        /// </summary>
        class Variantes
        {
            public Transform Padre;
            public string Nombre;
            public List<Sprite[]> Tiras = new List<Sprite[]>();

            public bool Vacio => Tiras.Count == 0;
            public bool Animado => Tiras.Count > 0 && Tiras[0].Length > 1;
        }

        static Variantes CargarVariantes(Transform raiz, string nombre, string[] rutas)
        {
            var variantes = new Variantes { Nombre = nombre };

            foreach (var ruta in rutas)
            {
                var lista = CargarSpritesOrdenados(ruta);
                if (lista.Count > 0) variantes.Tiras.Add(lista.ToArray());
            }

            var padre = new GameObject(nombre).transform;
            padre.SetParent(raiz, false);
            variantes.Padre = padre;

            return variantes;
        }

        /// <summary>
        /// Instancia un tipo de objeto sobre sus celdas.
        /// Si se pasa <paramref name="rutaAnimacion"/>, además le cuelga un
        /// <see cref="AnimadorSprite"/> con la tira completa de frames.
        /// </summary>
        static void Sembrar(List<Vector2Int> celdas, Variantes variantes, int alto,
                            System.Random rnd, float desplazamientoY,
                            bool animar = false, float fps = 8f, int ordenExtra = 0,
                            bool ordenAbsoluto = false, List<int> elegidas = null)
        {
            if (variantes.Vacio) return;

            bool conAnimacion = animar && variantes.Animado;

            foreach (var celda in celdas)
            {
                // Cada instancia elige su variante, y usa la tira de ESA variante.
                int cual = rnd.Next(variantes.Tiras.Count);
                Sprite[] tira = variantes.Tiras[cual];

                // Y apunta CUÁL eligió, si alguien lo pide. El tocón de un árbol tiene que
                // ser el de su especie: Tree3 mide 192x192 y Tree1 192x256, así que
                // emparejar mal deja el tocón flotando cuatro píxeles por encima del suelo.
                elegidas?.Add(cual);

                var go = new GameObject($"{variantes.Nombre}_{celda.x}_{celda.y}");
                go.transform.SetParent(variantes.Padre, false);
                go.transform.position = new Vector3(celda.x + 0.5f, celda.y + 0.5f + desplazamientoY, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tira[rnd.Next(tira.Length)];

                // Orden por Y: lo que está más abajo se dibuja delante. Es lo que da
                // la sensación de profundidad en una vista cenital.
                // Orden absoluto para lo que va entre capas fijas del tilemap;
                // orden por Y para todo lo que se pisa con las unidades.
                sr.sortingOrder = ordenAbsoluto
                    ? ordenExtra
                    : OrdenPorProfundidad.Calcular(alto, celda.y) + ordenExtra;

                if (conAnimacion && tira.Length > 1)
                {
                    var anim = go.AddComponent<AnimadorSprite>();
                    // Frame inicial distinto por instancia: un rebaño sincronizado
                    // se nota artificial al instante.
                    anim.Configurar(tira, fps, rnd.Next(tira.Length));
                }

                // Espejo horizontal aleatorio: rompe la repetición sin costar nada.
                if (rnd.Next(2) == 0)
                    go.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        // -----------------------------------------------------------------
        // Percepcion: niebla, nubes y hora del dia
        // -----------------------------------------------------------------

        const string RutaShaderTinte = "Assets/Shaders/TinteMultiplicativo.shader";
        const string CarpetaNiebla = "Assets/Datos/Niebla";
        const string RutaMaterialTinte = CarpetaNiebla + "/TinteMultiplicativo.mat";

        /// <summary>
        /// Monta la niebla de guerra, su manto de nubes y el ciclo del dia.
        /// </summary>
        /// <remarks>
        /// Los tres van en el MISMO objeto de escena y no cada uno en el suyo porque los tres
        /// son la misma pregunta —que se ve del mapa y con que luz— y porque el orden de
        /// dibujado entre ellos importa: la niebla oscurece, las nubes tapan encima y la hora
        /// oscurece por encima de todo. Repartidos por la jerarquia, quien venga en la semana
        /// 13 a tocar uno no encontraria los otros dos.
        /// </remarks>
        static void CrearPercepcion(DefinicionMapa definicion)
        {
            var material = ObtenerMaterialDeTinte();

            var go = new GameObject("Percepcion");

            var niebla = go.AddComponent<NieblaDeGuerra>();
            niebla.material = material;

            var manto = go.AddComponent<MantoDeNubes>();
            manto.nubes = CargarNubes().ToArray();

            var ciclo = go.AddComponent<CicloDelDia>();
            ciclo.material = material;

            if (manto.nubes.Length == 0)
                Debug.LogWarning("[Tiny Tactics] No encuentro las nubes del pack: la niebla " +
                                 "se dibujara solo con la sombra, sin el manto.");
        }

        static List<Sprite> CargarNubes()
        {
            var sprites = new List<Sprite>();

            for (int i = 1; i <= 8; i++)
            {
                var lista = CargarSpritesOrdenados($"{DirDecoracion}/Clouds/Clouds_{i:D2}.png");
                if (lista.Count > 0) sprites.Add(lista[0]);
            }

            return sprites;
        }

        /// <summary>
        /// El material del tinte multiplicativo, creado la primera vez y reutilizado despues.
        /// </summary>
        /// <remarks>
        /// Se guarda como ASSET y no se crea en ejecucion con <c>Shader.Find</c> a proposito:
        /// un shader que no usa ningun material del proyecto se queda fuera de la build, y
        /// entonces la niebla funciona en el editor y desaparece justo en la version que se
        /// entrega. Existiendo el material, el shader tiene quien lo referencie.
        /// </remarks>
        static Material ObtenerMaterialDeTinte()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialTinte);
            if (material != null) return material;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShaderTinte);
            if (shader == null)
            {
                Debug.LogError($"[Tiny Tactics] No encuentro el shader en {RutaShaderTinte}.");
                return null;
            }

            AsegurarCarpeta(CarpetaNiebla);

            material = new Material(shader) { name = "Tinte multiplicativo" };
            AssetDatabase.CreateAsset(material, RutaMaterialTinte);
            AssetDatabase.SaveAssets();

            return material;
        }

        /// <summary>
        /// Indices de las nubes ANCHAS del pack, dentro de la lista de las ocho.
        /// </summary>
        /// <remarks>
        /// MEDIDO sobre el alfa de los PNG, no elegido a ojo. Las ocho vienen en un lienzo de
        /// 576x256, pero el dibujo de dentro no: Clouds_01 ocupa 495x157 pixeles y Clouds_05
        /// 416x152, mientras que Clouds_08 se queda en 88x55. A 64 pixeles por tile, eso son
        /// nubes de casi ocho tiles de ancho contra otras de poco mas de uno.
        ///
        /// Mezclarlas al azar hacia que las grandes se perdieran entre las pequenas. Separar
        /// los dos grupos es lo que permite darle a cada uno su tamano y su velocidad.
        /// </remarks>
        static readonly int[] NubesAnchas = { 0, 1, 4, 5 };

        /// <summary>
        /// Siembra el cielo: nubes pequenas rapidas y nubes grandes lentas.
        /// </summary>
        /// <remarks>
        /// <b>Las grandes van mas despacio, y esa es toda la gracia.</b> Una nube grande que
        /// cruza al mismo ritmo que una pequena se lee como un sprite grande moviendose; a
        /// menos de la mitad de velocidad se lee como una nube que esta mas alta. Es paralaje
        /// sin camara de paralaje: no hay capas ni profundidad, solo dos velocidades.
        ///
        /// Y mas transparentes que las pequenas, por lo mismo: lo que esta mas lejos tiene
        /// menos contraste.
        ///
        /// <b>Van por encima de la niebla.</b> Son cielo, no suelo: una nube que pasa sobre
        /// terreno sin explorar tiene que pasar POR DELANTE de la sombra, no oscurecerse con
        /// ella. Quedan por debajo de la lamina de la hora, asi que de noche se apagan con el
        /// resto del mapa, que es lo correcto.
        /// </remarks>
        static void SembrarNubes(Transform raiz, DefinicionMapa definicion, System.Random rnd)
        {
            if (definicion.cantidadNubes <= 0) return;

            var sprites = CargarNubes();
            if (sprites.Count == 0) return;

            var padre = new GameObject("Nubes").transform;
            padre.SetParent(raiz, false);

            int pequenas = definicion.cantidadNubes;

            // La mitad que de pequenas, y nunca menos de cinco: con dos o tres en un mapa de
            // 224 tiles no se cruza ninguna en toda la partida y el efecto no existe.
            int grandes = Mathf.Max(5, definicion.cantidadNubes / 2);

            for (int i = 0; i < pequenas + grandes; i++)
            {
                bool ancha = i >= pequenas;

                var go = new GameObject(ancha ? $"NubeGrande_{i - pequenas + 1}" : $"Nube_{i + 1}");
                go.transform.SetParent(padre, false);
                go.transform.position = new Vector3(
                    (float)rnd.NextDouble() * definicion.ancho,
                    (float)rnd.NextDouble() * definicion.alto, 0f);

                var sr = go.AddComponent<SpriteRenderer>();

                if (ancha)
                {
                    sr.sprite = sprites[NubesAnchas[rnd.Next(NubesAnchas.Length)] % sprites.Count];
                    sr.color = new Color(1f, 1f, 1f, 0.50f + (float)rnd.NextDouble() * 0.16f);

                    float escala = 2.2f + (float)rnd.NextDouble() * 0.9f;
                    go.transform.localScale = new Vector3(escala, escala, 1f);
                }
                else
                {
                    sr.sprite = sprites[rnd.Next(sprites.Count)];
                    sr.color = new Color(1f, 1f, 1f, 0.75f);
                }

                // Voltear la mitad: son ocho dibujos para veinte nubes, y el ojo las
                // reconoce enseguida si todas miran al mismo lado.
                sr.flipX = rnd.Next(2) == 0;

                sr.sortingOrder = 6500 + i;

                var deriva = go.AddComponent<DerivaNube>();
                deriva.velocidad = ancha
                    ? 0.14f + (float)rnd.NextDouble() * 0.16f
                    : 0.35f + (float)rnd.NextDouble() * 0.55f;

                deriva.direccion = new Vector2(1f, 0.18f);
                deriva.limiteMinimo = Vector2.zero;
                deriva.limiteMaximo = definicion.TamanoEnMundo;

                // Una nube grande mide casi ocho tiles: con el margen de las pequenas
                // reaparecia con medio cuerpo ya dentro de la pantalla.
                deriva.margen = ancha ? 26f : 12f;
            }
        }

        // -----------------------------------------------------------------
        // Utilidades
        // -----------------------------------------------------------------

        static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;

            int corte = ruta.LastIndexOf('/');
            string padre = ruta.Substring(0, corte);
            string nombre = ruta.Substring(corte + 1);

            AsegurarCarpeta(padre);
            AssetDatabase.CreateFolder(padre, nombre);
        }

        static void RegistrarEnBuildSettings()
        {
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            foreach (var escena in escenas)
                if (escena.path == RutaEscena) return;

            escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
