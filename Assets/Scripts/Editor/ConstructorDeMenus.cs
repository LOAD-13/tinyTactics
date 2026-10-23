using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TinyTactics.Interfaz;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// El flujo de partida en la escena: arranque, menú principal, pausa y escena de inicio.
    /// </summary>
    /// <remarks>
    /// Lo llama el generador de la escena de juego al final, como al resto de piezas. Así
    /// regenerar el mapa nunca deja el menú desenchufado.
    /// </remarks>
    public static class ConstructorDeMenus
    {
        const string RutaLogo = "Assets/Arte/Logo.png";
        const string RutaIcono = "Assets/Arte/Icono.png";
        public const string RutaInicio = "Assets/Scenes/Inicio.unity";

        const string DirUI = "Assets/Tiny Swords/UI Elements";
        const string CarpetaMiniaturas = "Assets/Arte/Mapas";

        public static void CrearFlujo(TemaInterfaz tema, DefinicionMapa definicion, string escena,
                                      MapaGenerado mapa)
        {
            var raiz = new GameObject("Flujo de partida").transform;

            var paleta = ConstructorDePaleta.Obtener();

            var arranque = new GameObject("Arranque").AddComponent<ArranqueDePartida>();
            arranque.transform.SetParent(raiz, false);
            arranque.escena = escena;
            arranque.paleta = paleta;

            // El editor de mapas vive apagado en la escena; el arranque lo enciende en su modo.
            var editor = new GameObject("Editor de mapas").AddComponent<EditorDeMapas>();
            editor.transform.SetParent(raiz, false);
            editor.tema = tema;
            editor.paleta = paleta;
            editor.iconoBorrar = DelPack($"{DirUI}/Icons/Icon_09.png");
            editor.mesa = DelPack($"{DirUI}/Wood Table/WoodTable.png");
            editor.iconoSubir = SpriteDe(paleta, 1, GeneradorTerreno.Centro + GeneradorTerreno.DesplazamientoElevado);
            editor.iconoBajar = SpriteDe(paleta, 1, GeneradorTerreno.IndicesCaraEnTierra[1]);
            editor.gameObject.SetActive(false);

            var menu = new GameObject("Menu principal").AddComponent<MenuPrincipal>();
            menu.transform.SetParent(raiz, false);
            menu.tema = tema;
            menu.logo = CargarSprite(RutaLogo, 1024, FilterMode.Bilinear);

            menu.espadas = new Sprite[5];
            for (int i = 0; i < 5; i++) menu.espadas[i] = DelPack($"{DirUI}/Swords/Swords {i + 1}.png");
            menu.pergamino = DelPack($"{DirUI}/Papers/RegularPaper.png");
            menu.perilla = DelPack($"{DirUI}/Buttons/TinyRoundBlueButton.png");
            menu.teclado = CargarSprite("Assets/Arte/Controles/Teclado.png", 512, FilterMode.Point);
            menu.raton = CargarSprite("Assets/Arte/Controles/Raton.png", 128, FilterMode.Point);

            var bases = new Vector2[mapa.Bases.Count];
            for (int i = 0; i < bases.Length; i++)
                bases[i] = new Vector2((mapa.Bases[i].x + 0.5f) / mapa.Ancho, (mapa.Bases[i].y + 0.5f) / mapa.Alto);

            menu.mapas = new[]
            {
                new MenuPrincipal.FichaDeMapa
                {
                    nombre = definicion.nombreMapa,
                    descripcion = definicion.descripcion,
                    escena = escena,
                    bandos = definicion.bandos,
                    miniatura = Miniatura(mapa, escena),
                    bases = bases,
                },
            };

            var pausa = new GameObject("Menu de pausa").AddComponent<MenuDePausa>();
            pausa.transform.SetParent(raiz, false);
            pausa.tema = tema;
        }

        /// <summary>
        /// La escena de inicio: una cámara negra y el componente que abre el menú.
        /// </summary>
        /// <remarks>
        /// Se crea aditiva y se cierra al guardarla, para no sacar al usuario de la escena de
        /// juego que acaba de generar.
        /// </remarks>
        public static void CrearEscenaDeInicio(string escenaDelMenu)
        {
            var activa = SceneManager.GetActiveScene();
            var inicio = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(inicio);

            var go = new GameObject("Camara");
            var camara = go.AddComponent<Camera>();
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = Color.black;
            camara.orthographic = true;

            new GameObject("Inicio").AddComponent<Inicio>().escenaDelMenu = escenaDelMenu;

            EditorSceneManager.SaveScene(inicio, RutaInicio);
            SceneManager.SetActiveScene(activa);
            EditorSceneManager.CloseScene(inicio, true);
        }

        /// <summary>Inicio primero, juego después: la build arranca en el menú.</summary>
        public static void RegistrarEscenas(string rutaJuego)
        {
            var escenas = new List<EditorBuildSettingsScene>();
            escenas.Add(new EditorBuildSettingsScene(RutaInicio, true));
            escenas.Add(new EditorBuildSettingsScene(rutaJuego, true));

            foreach (var e in EditorBuildSettings.scenes)
                if (e.path != RutaInicio && e.path != rutaJuego) escenas.Add(e);

            EditorBuildSettings.scenes = escenas.ToArray();
        }

        /// <summary>Nombre del producto e icono del ejecutable.</summary>
        public static void AjustarProyecto()
        {
            PlayerSettings.productName = "Tiny Tactics";
            PlayerSettings.companyName = "Grupo 01 UTP";
            // La ventana se puede estirar: el juego recoloca la interfaz al tamaño que tenga.
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

            var icono = CargarTextura(RutaIcono);
            if (icono != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icono }, IconKind.Any);
        }

        static Sprite SpriteDe(PaletaDeTerreno p, int color, int indice) =>
            p != null && p.Pieza(color, indice) is UnityEngine.Tilemaps.Tile t ? t.sprite : null;

        static Sprite DelPack(string ruta)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (s == null) Debug.LogWarning($"[Tiny Tactics] No encuentro el sprite {ruta}.");
            return s;
        }

        /// <summary>
        /// La miniatura del mapa para la pantalla de preparar partida.
        /// </summary>
        /// <remarks>
        /// Se pinta aquí, al generar, y no en partida: así existe también para los mapas que
        /// no son la escena abierta, que es justo lo que se elige en esa pantalla. Un píxel
        /// por celda y filtro de punto, que es como se ve el pixel art ampliado.
        /// </remarks>
        static Sprite Miniatura(MapaGenerado mapa, string nombre)
        {
            int w = mapa.Ancho, h = mapa.Alto;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            var agua = new Color32(56, 102, 158, 255);
            var orilla = new Color32(74, 128, 176, 255);
            var tierra = new Color32(87, 140, 71, 255);
            var meseta = new Color32(115, 168, 89, 255);
            var arbol = new Color32(41, 87, 48, 255);
            var oro = new Color32(219, 181, 64, 255);
            var roca = new Color32(120, 120, 128, 255);

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color32 c;
                    if (mapa.Tierra[x, y]) c = mapa.Nivel != null && mapa.Nivel[x, y] > 0 ? meseta : tierra;
                    else c = CercaDeTierra(mapa, x, y) ? orilla : agua;
                    px[y * w + x] = c;
                }

            foreach (var a in mapa.Arboles) Poner(px, w, h, a, arbol);
            foreach (var r in mapa.Rocas) Poner(px, w, h, r, roca);
            foreach (var o in mapa.Oro)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        Poner(px, w, h, o + new Vector2Int(dx, dy), oro);

            tex.SetPixels32(px);
            tex.Apply();

            AsegurarCarpeta(CarpetaMiniaturas);
            string ruta = $"{CarpetaMiniaturas}/{nombre}.png";
            System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(ruta);
            return CargarSprite(ruta, 512, FilterMode.Point);
        }

        static bool CercaDeTierra(MapaGenerado m, int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < m.Ancho && ny < m.Alto && m.Tierra[nx, ny]) return true;
                }
            return false;
        }

        static void Poner(Color32[] px, int w, int h, Vector2Int c, Color32 color)
        {
            if (c.x < 0 || c.y < 0 || c.x >= w || c.y >= h) return;
            px[c.y * w + c.x] = color;
        }

        static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;
            int corte = ruta.LastIndexOf('/');
            AsegurarCarpeta(ruta.Substring(0, corte));
            AssetDatabase.CreateFolder(ruta.Substring(0, corte), ruta.Substring(corte + 1));
        }

        static Sprite CargarSprite(string ruta, int maximo, FilterMode filtro)
        {
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                Debug.LogWarning($"[Tiny Tactics] No encuentro {ruta}. El menú saldrá sin logo.");
                return null;
            }

            bool cambia = importador.textureType != TextureImporterType.Sprite ||
                          importador.spriteImportMode != SpriteImportMode.Single ||
                          importador.maxTextureSize != maximo ||
                          importador.filterMode != filtro ||
                          importador.mipmapEnabled;

            if (cambia)
            {
                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.maxTextureSize = maximo;
                importador.filterMode = filtro;
                importador.mipmapEnabled = false;
                importador.alphaIsTransparency = true;
                importador.textureCompression = TextureImporterCompression.Uncompressed;
                importador.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }

        static Texture2D CargarTextura(string ruta)
        {
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null) return null;

            if (importador.textureType != TextureImporterType.Default || importador.mipmapEnabled)
            {
                importador.textureType = TextureImporterType.Default;
                importador.mipmapEnabled = false;
                importador.alphaIsTransparency = true;
                importador.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }
    }
}
