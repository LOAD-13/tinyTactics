using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Unidades;

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
                camara = CrearCamara(definicion);
                PintarMapa(definicion, mapa, paleta);

                EditorUtility.DisplayProgressBar("Tiny Tactics", "Poblando el mapa…", 0.85f);
                PoblarMapa(definicion, mapa);

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
                $"{mapa.RocasAgua.Count} rocas de mar.");
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
            }

            if (framesEspuma.Count > 1)
                paleta.Espuma = CrearTileAnimado(framesEspuma, $"{CarpetaTiles}/Espuma.asset");
            else if (framesEspuma.Count == 1)
                paleta.Espuma = CrearTile(framesEspuma[0], $"{CarpetaTiles}/Espuma.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return paleta;
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

        static CamaraRTS CrearCamara(DefinicionMapa definicion)
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

            var controlador = go.AddComponent<CamaraRTS>();
            controlador.ConfigurarLimites(Vector2.zero, definicion.TamanoEnMundo);

            // El zoom de arranque es también el máximo: se puede acercar, nunca alejarse
            // más de lo que se ve al empezar la partida.
            controlador.zoomMaximo = camara.orthographicSize;
            controlador.zoomMinimo = 4f;

            Vector2 centro = definicion.CentroEnMundo;
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

            var limites = new BoundsInt(0, 0, 0, ancho, alto, 1);

            var agua = new TileBase[ancho * alto];
            var espuma = new TileBase[ancho * alto];
            var suelo = new TileBase[ancho * alto];

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

            capaAgua.SetTilesBlock(limites, agua);
            capaEspuma.SetTilesBlock(limites, espuma);
            capaSuelo.SetTilesBlock(limites, suelo);
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
            Sembrar(mapa.Arbustos, arbustos, definicion.alto, rnd, 0f, animar: true, fps: 5f);

            // Entre el agua (-30) y la espuma (-20): se ven sobre el mar y la espuma
            // de la costa les pasa por encima.
            Sembrar(mapa.RocasAgua, rocasAgua, definicion.alto, rnd, 0f,
                    animar: true, fps: 7f, ordenExtra: -25, ordenAbsoluto: true);

            ColocarBases(raiz, mapa, definicion.alto, rnd);
            SembrarNubes(raiz, definicion, rnd);

            if (definicion.patitoDeGoma)
                SoltarPatito(raiz, definicion, mapa, rnd);
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

        /// <summary>Nombres de color de facción, en el orden del pack Tiny Swords.</summary>
        static readonly string[] ColoresFaccion = { "Blue", "Red", "Yellow", "Purple", "Black" };

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
                {
                    var go = new GameObject("Castillo");
                    go.transform.SetParent(grupo, false);
                    go.transform.position = new Vector3(celda.x + 0.5f, celda.y + 1.4f, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = castillo[0];
                    sr.sortingOrder = alto - celda.y;
                }

                // Dos pawns que se pasean junto al castillo.
                var reposo = CargarSpritesOrdenados($"{DirRecursos}/Pawn/{color} Pawn/Pawn_Idle.png");
                var caminar = CargarSpritesOrdenados($"{DirRecursos}/Pawn/{color} Pawn/Pawn_Run.png");

                for (int p = 0; p < 2; p++)
                {
                    if (reposo.Count == 0) break;

                    var go = new GameObject($"Pawn_{p + 1}");
                    go.transform.SetParent(grupo, false);
                    go.transform.position = new Vector3(
                        celda.x + 0.5f + (p == 0 ? -2.4f : 2.4f), celda.y - 2.2f, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = reposo[0];
                    sr.sortingOrder = alto - celda.y + 2;

                    go.AddComponent<AnimadorSprite>();

                    var paseo = go.AddComponent<DeambularPawn>();
                    paseo.Configurar(
                        reposo.ToArray(),
                        caminar.Count > 1 ? caminar.ToArray() : reposo.ToArray(),
                        4.5f);
                }
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
                sr.sortingOrder = ordenAbsoluto ? ordenExtra : alto - celda.y + ordenExtra;

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
                sr.sortingOrder = 1000 + i;

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
