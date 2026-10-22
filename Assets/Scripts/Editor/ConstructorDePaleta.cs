using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using TinyTactics.Mundo;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Fabrica la paleta de terreno en los cinco colores del pack, para pintar en partida.
    /// </summary>
    /// <remarks>
    /// Va en su propia carpeta y no en <c>Assets/Datos/Tiles</c>: aquella la borra y rehace el
    /// generador de escena cada vez, y una paleta que apuntara ahí se quedaría con referencias
    /// rotas tras regenerar. Esta solo crea lo que falta.
    /// </remarks>
    public static class ConstructorDePaleta
    {
        const string Carpeta = "Assets/Datos/Paleta";
        const string RutaPaleta = Carpeta + "/PaletaDeTerreno.asset";
        const string DirTiles = "Assets/Tiny Swords/Terrain/Tileset";

        public static PaletaDeTerreno Obtener()
        {
            Asegurar("Assets/Datos", "Paleta");

            var paleta = AssetDatabase.LoadAssetAtPath<PaletaDeTerreno>(RutaPaleta);
            if (paleta == null)
            {
                paleta = ScriptableObject.CreateInstance<PaletaDeTerreno>();
                AssetDatabase.CreateAsset(paleta, RutaPaleta);
            }

            paleta.agua = Tile(AssetDatabase.LoadAssetAtPath<Sprite>($"{DirTiles}/Water Background color.png"), $"{Carpeta}/Agua.asset");
            paleta.espuma = Espuma();

            if (paleta.colores == null || paleta.colores.Length != 5) paleta.colores = new PaletaDeTerreno.JuegoDeColor[5];

            for (int c = 1; c <= 5; c++)
            {
                var sprites = Ordenados($"{DirTiles}/Tilemap_color{c}.png");
                if (sprites.Count < 44)
                    Debug.LogWarning($"[Tiny Tactics] El tileset de color {c} tiene {sprites.Count} piezas; esperaba 44.");

                Asegurar(Carpeta, $"Color{c}");
                var juego = paleta.colores[c - 1] ?? new PaletaDeTerreno.JuegoDeColor();
                juego.nombre = $"Tierra {c}";
                juego.piezas = new TileBase[48];

                for (int i = 0; i < sprites.Count && i < 48; i++)
                    juego.piezas[i] = Tile(sprites[i], $"{Carpeta}/Color{c}/Pieza_{i:D2}.asset");

                juego.muestra = sprites.Count > GeneradorTerreno.Centro ? sprites[GeneradorTerreno.Centro] : null;
                paleta.colores[c - 1] = juego;
            }

            EditorUtility.SetDirty(paleta);
            AssetDatabase.SaveAssets();
            return paleta;
        }

        static TileBase Tile(Sprite sprite, string ruta)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(ruta);
            if (tile != null) { if (tile.sprite != sprite) { tile.sprite = sprite; EditorUtility.SetDirty(tile); } return tile; }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, ruta);
            return tile;
        }

        static TileBase Espuma()
        {
            string ruta = $"{Carpeta}/Espuma.asset";
            var existente = AssetDatabase.LoadAssetAtPath<TileBase>(ruta);
            if (existente != null) return existente;

            var frames = Ordenados($"{DirTiles}/Water Foam.png");
            if (frames.Count == 0) return null;
            if (frames.Count == 1) return Tile(frames[0], ruta);

            var tile = ScriptableObject.CreateInstance<AnimatedTile>();
            tile.m_AnimatedSprites = frames.ToArray();
            tile.m_MinSpeed = 0.6f;
            tile.m_MaxSpeed = 1.1f;
            tile.m_TileColliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, ruta);
            return tile;
        }

        public static List<Sprite> Ordenados(string ruta)
        {
            var sprites = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ruta))
                if (o is Sprite s) sprites.Add(s);

            sprites.Sort((a, b) => Indice(a.name).CompareTo(Indice(b.name)));
            return sprites;
        }

        static int Indice(string nombre)
        {
            int g = nombre.LastIndexOf('_');
            return g >= 0 && int.TryParse(nombre.Substring(g + 1), out int v) ? v : 0;
        }

        static void Asegurar(string padre, string nombre)
        {
            if (!AssetDatabase.IsValidFolder($"{padre}/{nombre}")) AssetDatabase.CreateFolder(padre, nombre);
        }
    }
}
