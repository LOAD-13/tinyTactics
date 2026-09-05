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

                ConstructorDeInterfaz.CrearLienzo(_tema);

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

            // La economía cuelga del mismo objeto que el mundo: las dos son estado global
            // de la partida y tenerlas juntas evita buscar dónde vive cada cosa.
            var economia = go.AddComponent<Economia>();
            economia.datos = ObtenerDatosEconomia();
            economia.bandos = Mathf.Max(1, definicion.bandos);

            go.AddComponent<Sustento>();
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

            Sembrar(mapa.Arboles, arboles, definicion.alto, rnd, AlturaArbol);
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
                        mundo != null ? mundo.radioArbol : 1.1f, CargarTocones());

            MarcarNodos(oro.Padre, mapa.Oro, TipoRecurso.Oro,
                        mundo != null ? mundo.radioOro : 1.0f, null);

            // La oveja no bloquea la grilla porque se mueve, y marcar celdas con algo que
            // cambia de sitio ensuciaría el pathfinding sin arreglar nada.
            MarcarNodos(ovejas.Padre, mapa.Ovejas, TipoRecurso.Carne, 0f, null, seMueve: true);

            // El rebaño pasta, o sea que se mueve: mismo tratamiento que las unidades.
            for (int i = 0; i < ovejas.Padre.childCount; i++)
                ovejas.Padre.GetChild(i).gameObject
                      .AddComponent<OrdenPorProfundidad>().alto = definicion.alto;

            PrepararCriadero(mundo, ovejas.Padre, mapa, definicion.alto);

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
        static void MarcarNodos(Transform padre, List<Vector2Int> celdas, TipoRecurso recurso,
                                float radioBloqueo, Sprite[] restos, bool seMueve = false)
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

                if (restos != null && restos.Length > 0) nodo.Configurar(restos);
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
        static Sprite[] CargarTocones()
        {
            var salida = new List<Sprite>();

            for (int i = 1; i <= 4; i++)
            {
                var lista = CargarSpritesOrdenados($"{DirRecursos}/Wood/Trees/Stump {i}.png");
                if (lista.Count > 0) salida.Add(lista[0]);
            }

            if (salida.Count == 0)
                Debug.LogWarning("[Tiny Tactics] No encuentro los tocones; los árboles talados desaparecerán.");

            return salida.ToArray();
        }

        // -----------------------------------------------------------------
        // Unidades
        // -----------------------------------------------------------------

        /// <summary>
        /// Composicion de la escuadra inicial de cada bando.
        ///
        /// No es equilibrio de juego: las unidades todavia no se entrenan (eso es E05) ni
        /// pelean (E06). Es una escuadra de demostracion, elegida para que en la escena se
        /// vean los cinco tipos a la vez y se pueda comprobar que cada uno anima lo suyo.
        /// </summary>
        static readonly TipoUnidad[] EscuadraInicial =
        {
            TipoUnidad.Pawn, TipoUnidad.Pawn,
            TipoUnidad.Guerrero, TipoUnidad.Guerrero,
            TipoUnidad.Lancero, TipoUnidad.Lancero,
            TipoUnidad.Arquero, TipoUnidad.Arquero,
            TipoUnidad.Monje, TipoUnidad.Monje,
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
                go.AddComponent<RecolectorPawn>();

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
        static string[] ColoresFaccion => CatalogoDeUnidades.Colores;

        /// <summary>
        /// Castillo y dos pawns en cada punto de aparición.
        ///
        /// Todavía no hay lógica detrás — son sprites. Pero convierten una captura de
        /// "un mapa generado" en una de "una partida a punto de empezar", que es lo que
        /// hay que enseñar el lunes.
        /// </summary>
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

                var castillo = CargarSpritesOrdenados(
                    $"Assets/Tiny Swords/Buildings/{color} Buildings/Castle.png");

                if (castillo.Count > 0)
                    CrearCastillo(grupo, castillo[0], celda, alto, i);

                // Unidades reales: seleccionables, animadas y capaces de recibir órdenes.
                for (int p = 0; p < EscuadraInicial.Length; p++)
                {
                    var tipo = EscuadraInicial[p];
                    var datos = CatalogoDeUnidades.Obtener(tipo);

                    var tiras = CatalogoDeUnidades.CargarTiras(datos, i, CargarSpritesOrdenados);
                    if (tiras == null) continue;

                    // En fila doble delante del castillo, con algo de holgura para que
                    // el empuje blando no las tenga peleando desde el primer frame.
                    float dx = (p % 5 - 2f) * 1.7f;
                    float dy = -2.4f - (p / 5) * 1.6f;

                    CrearUnidad(grupo, datos, i, tiras,
                                new Vector3(celda.x + 0.5f + dx, celda.y + dy, 0f),
                                alto - celda.y + 2, $"{tipo}_{p + 1}");
                }

                PrepararProduccion(grupo, i, alto - celda.y + 2);
                CrearMuneco(grupo, celda, alto);
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
        /// El castillo deja de ser un dibujo: sabe de qué bando es, recibe recursos y se
        /// puede seleccionar.
        /// </summary>
        static void CrearCastillo(Transform grupo, Sprite sprite, Vector2Int celda,
                                  int alto, int faccion)
        {
            var go = new GameObject("Castillo");
            go.transform.SetParent(grupo, false);
            go.transform.position = new Vector3(celda.x + 0.5f, celda.y + 1.4f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            // Un edificio se ordena por donde SE APOYA, no por el centro de su dibujo. El
            // castillo mide tres tiles de alto: usando su centro, todo lo que pasara por
            // delante de la puerta se dibujaba detrás del muro.
            sr.sortingOrder = OrdenPorProfundidad.Calcular(alto, celda.y - 0.5f);

            var edificio = go.AddComponent<Edificio>();
            edificio.tipo = TipoEdificio.Castillo;
            edificio.nombreVisible = "Castillo";
            edificio.faccion = faccion;

            // El retrato es el propio castillo. No hace falta dibujar un icono aparte: lo
            // que se ve en el panel es exactamente lo que hay en el mapa.
            edificio.retrato = sprite;

            // Huella MEDIDA sobre el PNG: dentro del lienzo de 320x256 el dibujo ocupa
            // 4,88 x 3,25 unidades y su centro cae 0,27 por debajo del centro del lienzo.
            // Es lo que permite entregar arrimándose por cualquier lado.
            edificio.huella = new Vector2(4.88f, 3.25f);
            edificio.huellaCentro = new Vector2(0f, -0.27f);

            // Las unidades nuevas salen por debajo, lejos de donde se amontonan los que
            // vienen a depositar.
            edificio.puntoSalida = new Vector2(0f, -3.2f);

            go.AddComponent<ProduccionEdificio>();

            ConstructorDeInterfaz.AnadirMarcador(go, _tema, faccion, alto - celda.y);

            // El marcador viene dimensionado para una unidad. Las dos cifras de abajo están
            // MEDIDAS sobre el PNG del castillo, no estimadas a ojo: dentro de su lienzo de
            // 320x256 el dibujo ocupa 4,88 x 3,25 unidades y su centro cae 0,27 por debajo
            // del centro del lienzo. Deducirlo mirando la escena fue lo que dejó el corchete
            // colgando por debajo del edificio.
            //
            // El tamaño va por el campo del componente y no tocando la escala del transform
            // porque la animación de cierre reescribe la escala entera al encenderse: puesta
            // a mano duraría exactamente hasta el primer clic.
            var anillo = go.transform.Find("Seleccion");
            if (anillo != null)
            {
                anillo.localPosition = new Vector3(0f, -0.3f, 0f);

                // 4,88 unidades de ancho entre las 1,33 que mide el corchete (128 px a 96 ppu).
                var marcador = anillo.GetComponent<MarcadorSeleccion>();
                if (marcador != null) marcador.escalaBase = 3.7f;
            }
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

            var produccion = edificio.GetComponent<ProduccionEdificio>();
            if (produccion == null) return;

            var datos = CatalogoDeUnidades.Obtener(TipoUnidad.Pawn);
            var tiras = CatalogoDeUnidades.CargarTiras(datos, faccion, CargarSpritesOrdenados);
            if (tiras == null) return;

            var plantilla = CrearUnidad(grupo, datos, faccion, tiras,
                                        edificio.PuntoDeSalida, orden, "PlantillaPawn");

            plantilla.SetActive(false);

            produccion.datosUnidad = datos;
            produccion.plantilla = plantilla;
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
                            bool ordenAbsoluto = false)
        {
            if (variantes.Vacio) return;

            bool conAnimacion = animar && variantes.Animado;

            foreach (var celda in celdas)
            {
                // Cada instancia elige su variante, y usa la tira de ESA variante.
                Sprite[] tira = variantes.Tiras[rnd.Next(variantes.Tiras.Count)];

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

        static void SembrarNubes(Transform raiz, DefinicionMapa definicion, System.Random rnd)
        {
            if (definicion.cantidadNubes <= 0) return;

            var sprites = new List<Sprite>();
            for (int i = 1; i <= 8; i++)
            {
                var lista = CargarSpritesOrdenados($"{DirDecoracion}/Clouds/Clouds_{i:D2}.png");
                if (lista.Count > 0) sprites.Add(lista[0]);
            }

            if (sprites.Count == 0) return;

            var padre = new GameObject("Nubes").transform;
            padre.SetParent(raiz, false);

            for (int i = 0; i < definicion.cantidadNubes; i++)
            {
                var go = new GameObject($"Nube_{i + 1}");
                go.transform.SetParent(padre, false);
                go.transform.position = new Vector3(
                    (float)rnd.NextDouble() * definicion.ancho,
                    (float)rnd.NextDouble() * definicion.alto, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[rnd.Next(sprites.Count)];
                sr.color = new Color(1f, 1f, 1f, 0.75f);

                // Por encima de todo: las nubes pasan sobre el terreno y las unidades.
                sr.sortingOrder = 5000 + i;

                var deriva = go.AddComponent<DerivaNube>();
                deriva.velocidad = 0.35f + (float)rnd.NextDouble() * 0.55f;
                deriva.direccion = new Vector2(1f, 0.18f);
                deriva.limiteMinimo = Vector2.zero;
                deriva.limiteMaximo = definicion.TamanoEnMundo;
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
