using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Interfaz;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Prepara los assets de interfaz del pack y monta el lienzo en la escena.
    ///
    /// El pack dibuja cada pieza centrada dentro de un lienzo mucho mayor y transparente
    /// —una barra de 94 px vive en una imagen de 192×64—. Eso está bien para un sprite
    /// suelto y es un estorbo para la UI: los márgenes invisibles se estiran con el resto
    /// y desplazan lo que sí se ve. Por eso aquí se recortan una vez a su contenido útil y
    /// se guardan en <c>Assets/Datos/UI</c>. Los originales no se tocan.
    /// </summary>
    public static class ConstructorDeInterfaz
    {
        const string CarpetaUI = "Assets/Datos/UI";
        const string RutaTema = CarpetaUI + "/TemaInterfaz.asset";
        const string DirPack = "Assets/Tiny Swords/UI Elements";
        const string DirRecursos = "Assets/Tiny Swords/Pawn and Resources";

        // Recortes en coordenadas de Unity (origen abajo a la izquierda).
        // Los verticales están elegidos para que marco y relleno queden centrados en el
        // mismo eje: así encajan sin ajustes a mano por muy distinto que sea su ancho.
        static readonly RectInt CajaPanel = new RectInt(44, 25, 232, 252);
        static readonly RectInt CajaListonPanel = new RectInt(30, 5, 259, 103);
        static readonly RectInt CajaListonNombre = new RectInt(2, 0, 188, 60);
        static readonly RectInt CajaAzulGrande = new RectInt(19, 17, 154, 158);
        static readonly RectInt CajaAzulChica = new RectInt(19, 17, 90, 94);

        // El boton pequeno del pack ya viene sin margenes: se usa entero.
        static readonly RectInt CajaBoton = new RectInt(0, 0, 64, 64);

        // Recorte comun de los 25 retratos, calculado como la union de sus contornos. Se
        // usa el mismo para todos y no el de cada uno: recortando cada cara a su medida,
        // unas saldrian mas grandes que otras y la rejilla de grupo bailaria.
        static readonly RectInt CajaRetrato = new RectInt(22, 43, 197, 184);
        static readonly RectInt CajaBarraGrandeMarco = new RectInt(40, 8, 112, 48);
        static readonly RectInt CajaBarraGrandeRelleno = new RectInt(0, 8, 64, 48);
        static readonly RectInt CajaBarraChicaMarco = new RectInt(48, 16, 96, 32);
        static readonly RectInt CajaBarraChicaRelleno = new RectInt(0, 16, 64, 32);

        /// <summary>Grosor del borde 9-slice del panel: cubre las escuadras metálicas.</summary>
        const float BordePanel = 48f;

        /// <summary>
        /// Los sprites de interfaz se importan a 100 px por unidad, que es el
        /// <c>referencePixelsPerUnit</c> del lienzo. Con eso, un píxel del sprite equivale
        /// exactamente a un píxel de la resolución de referencia y los bordes 9-slice se
        /// dibujan del grosor con el que están pintados. A 64 salían un 56 % más gruesos.
        /// </summary>
        const int PpuInterfaz = 100;

        /// <summary>Las barras del mundo sí van a 64: comparten escala con los tiles.</summary>
        const int PpuMundo = 64;

        // El pack solo trae botones azules y rojos, pero las facciones son cinco. Medido
        // sobre los sprites, el azul del botón tiene exactamente el mismo matiz y la misma
        // saturación que `BigRibbons 1` — o sea que el autor pintó caja y listón con el
        // mismo color. Así que basta con llevar el matiz del botón al del listón de cada
        // bando para que las cinco cajas peguen con sus cinco listones.
        const float MatizOrigen = 0.52f;
        const float SaturacionOrigen = 0.60f;

        static readonly Vector2[] TintesFaccion =
        {
            new Vector2(0.52f, 0.60f), // Azul
            new Vector2(0.02f, 0.50f), // Rojo
            new Vector2(0.16f, 0.60f), // Amarillo
            new Vector2(0.90f, 0.20f), // Morado
            new Vector2(0.65f, 0.20f), // Negro
        };

        // -----------------------------------------------------------------
        // Tema
        // -----------------------------------------------------------------

        [MenuItem("Tiny Tactics/Reconstruir tema de interfaz", false, 30)]
        public static void ReconstruirTema()
        {
            var tema = ConstruirTema(true);
            if (tema == null) return;

            Selection.activeObject = tema;
            Debug.Log($"[Tiny Tactics] Tema de interfaz reconstruido en {RutaTema}.");
        }

        public static TemaInterfaz ObtenerTema() => ConstruirTema(false);

        /// <summary>Con esto puesto los recortes se rehacen aunque ya existan.</summary>
        static bool _forzar;

        static TemaInterfaz ConstruirTema(bool forzar)
        {
            AsegurarCarpeta(CarpetaUI);

            _forzar = forzar;

            var tema = AssetDatabase.LoadAssetAtPath<TemaInterfaz>(RutaTema);
            if (tema != null && !forzar) return tema;

            if (tema == null)
            {
                tema = ScriptableObject.CreateInstance<TemaInterfaz>();
                AssetDatabase.CreateAsset(tema, RutaTema);
            }

            // Punteros: hace falta lectura habilitada y sin comprimir, es lo que exige
            // Cursor.SetCursor para poder subirlos al puntero del sistema.
            tema.cursorNormal = PrepararTextura($"{DirPack}/Cursors/Cursor_01.png", 64, true);
            tema.cursorMano = PrepararTextura($"{DirPack}/Cursors/Cursor_02.png", 64, true);
            tema.cursorProhibido = PrepararTextura($"{DirPack}/Cursors/Cursor_03.png", 64, true);

            // El cuarto puntero es la mira de corchetes. Se usa como textura de cursor
            // ademas de como marcador de seleccion: son dos assets distintos generados del
            // mismo PNG, asi que no se pisan.
            tema.cursorAccion = PrepararTextura($"{DirPack}/Cursors/Cursor_04.png", 64, true);

            // Los corchetes vienen en 128 px. A 96 px por unidad ocupan 1,33 tiles: rodean
            // al pawn sin taparlo y sin invadir la casilla vecina.
            tema.marcadorSeleccion = PrepararSprite($"{DirPack}/Cursors/Cursor_04.png", 96);

            tema.panelFondo = Recortar($"{DirPack}/Wood Table/WoodTable.png",
                                       $"{CarpetaUI}/PanelMadera.png", CajaPanel,
                                       Borde(BordePanel), PpuInterfaz);

            tema.cajas = new Sprite[5];
            tema.cajasChicas = new Sprite[5];

            for (int f = 0; f < 5; f++)
            {
                var tinte = TintesFaccion[f];

                tema.cajas[f] = Recortar($"{DirPack}/Buttons/BigBlueButton_Regular.png",
                                         $"{CarpetaUI}/Caja{f}.png", CajaAzulGrande,
                                         Borde(28f), PpuInterfaz, tinte);

                tema.cajasChicas[f] = Recortar(
                    $"{DirPack}/Buttons/SmallBlueSquareButton_Regular.png",
                    $"{CarpetaUI}/CajaChica{f}.png", CajaAzulChica,
                    Borde(16f), PpuInterfaz, tinte);
            }

            // Los listones del pack ya vienen en los cinco colores y en el mismo orden que
            // las facciones (azul, rojo, amarillo, morado, negro). Los estrechos alternan
            // dos formas, y nos quedamos con los impares: los de cola en pico.
            tema.listonesPanel = new Sprite[5];
            tema.listonesNombre = new Sprite[5];

            for (int f = 0; f < 5; f++)
            {
                tema.listonesPanel[f] = Recortar(
                    $"{DirPack}/Ribbons/BigRibbons {f + 1}.png",
                    $"{CarpetaUI}/ListonPanel{f}.png", CajaListonPanel,
                    new Vector4(98f, 0f, 97f, 0f), PpuInterfaz);

                tema.listonesNombre[f] = Recortar(
                    $"{DirPack}/Ribbons/SmallRibbons {f * 2 + 1}.png",
                    $"{CarpetaUI}/ListonNombre{f}.png", CajaListonNombre,
                    new Vector4(30f, 0f, 30f, 0f), PpuInterfaz);
            }

            tema.barraGrandeMarco = Recortar($"{DirPack}/Bars/BigBar_Base.png",
                                             $"{CarpetaUI}/BarraGrandeMarco.png",
                                             CajaBarraGrandeMarco, Vector4.zero, PpuInterfaz);

            tema.barraGrandeRelleno = Recortar($"{DirPack}/Bars/BigBar_Fill.png",
                                               $"{CarpetaUI}/BarraGrandeRelleno.png",
                                               CajaBarraGrandeRelleno, Vector4.zero, PpuInterfaz);

            tema.barraGrandeRellenoAzul = Recortar($"{DirPack}/Bars/BigBar_Fill.png",
                                                   $"{CarpetaUI}/BarraGrandeRellenoAzul.png",
                                                   CajaBarraGrandeRelleno, Vector4.zero,
                                                   PpuInterfaz, matizFijo: 0.56f);

            // Estas dos también viven en el mundo, colgando de cada unidad.
            tema.barraChicaMarco = Recortar($"{DirPack}/Bars/SmallBar_Base.png",
                                            $"{CarpetaUI}/BarraChicaMarco.png",
                                            CajaBarraChicaMarco, Vector4.zero, PpuMundo);

            tema.barraChicaRelleno = Recortar($"{DirPack}/Bars/SmallBar_Fill.png",
                                              $"{CarpetaUI}/BarraChicaRelleno.png",
                                              CajaBarraChicaRelleno, Vector4.zero, PpuMundo);

            tema.botones = new Sprite[5];
            for (int f = 0; f < 5; f++)
                tema.botones[f] = Recortar($"{DirPack}/Buttons/TinySquareBlueButton.png",
                                           $"{CarpetaUI}/Boton{f}.png", CajaBoton,
                                           Borde(14f), PpuInterfaz, TintesFaccion[f]);

            tema.iconoMover = PrepararSprite($"{DirPack}/Icons/Icon_08.png", PpuInterfaz);
            tema.iconoDetener = PrepararSprite($"{DirPack}/Icons/Icon_09.png", PpuInterfaz);
            tema.iconoAtaqueAuto = PrepararSprite($"{DirPack}/Icons/Icon_06.png", PpuInterfaz);
            tema.iconoCurar = PrepararSprite($"{DirPack}/Icons/Icon_07.png", PpuInterfaz);
            tema.iconoConstruir = PrepararSprite($"{DirPack}/Icons/Icon_01.png", PpuInterfaz);
            tema.iconoEntrenar = PrepararSprite($"{DirPack}/Icons/Icon_02.png", PpuInterfaz);

            tema.iconoAtaque = PrepararSprite($"{DirPack}/Icons/Icon_05.png", PpuInterfaz);
            tema.iconoOro = PrepararSprite($"{DirPack}/Icons/Icon_03.png", PpuInterfaz);
            tema.iconoVelocidad = PrepararSprite($"{DirPack}/Icons/Icon_08.png", PpuInterfaz);

            // Los contadores llevan los sacos reales del pack en vez de los iconos
            // genéricos: es exactamente el mismo dibujo que el pawn carga a la espalda, así
            // que la relación entre lo que se ve en el mapa y el número de arriba es
            // inmediata y no hay que explicarla.
            tema.iconoRecursoOro =
                PrepararSprite($"{DirRecursos}/Gold/Gold Resource/Gold_Resource.png", PpuInterfaz);
            tema.iconoRecursoMadera =
                PrepararSprite($"{DirRecursos}/Wood/Wood Resource/Wood Resource.png", PpuInterfaz);
            tema.iconoRecursoCarne =
                PrepararSprite($"{DirRecursos}/Meat/Meat Resource/Meat Resource.png", PpuInterfaz);

            // Los retratos vienen con un margen transparente enorme: la cara ocupa 197 de
            // los 256 px del lienzo. Sin recortar, el hueco del panel se ve medio vacio por
            // mucho que se agrande la caja.
            tema.retratos = new Sprite[25];
            for (int i = 0; i < 25; i++)
                tema.retratos[i] = Recortar($"{DirPack}/Human Avatars/Avatars_{i + 1:00}.png",
                                            $"{CarpetaUI}/Retrato{i:00}.png",
                                            CajaRetrato, Vector4.zero, PpuInterfaz);

            EditorUtility.SetDirty(tema);
            AssetDatabase.SaveAssets();
            return tema;
        }

        // -----------------------------------------------------------------
        // Assets derivados
        // -----------------------------------------------------------------

        /// <summary>
        /// Copia un trozo del PNG original a un asset nuevo. Se lee el archivo en crudo en
        /// vez de <c>GetPixels</c> sobre el asset importado para no tener que marcar como
        /// legibles las texturas del pack, que se quedarían ocupando memoria en la build.
        /// </summary>
        static Vector4 Borde(float grosor) => new Vector4(grosor, grosor, grosor, grosor);

        /// <summary>
        /// Lleva el verdeazulado del botón al matiz del bando. Solo se tocan los píxeles
        /// que ya son de ese color: el marco crema y el contorno oscuro se dejan intactos,
        /// porque teñirlos volvería el borde rosado y arruinaría la lectura del pixel art.
        /// </summary>
        static Color Retenir(Color c, Vector2 tinte)
        {
            if (c.a < 0.004f) return c;

            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (s < 0.15f) return c;

            float distancia = Mathf.Abs(Mathf.DeltaAngle(h * 360f, MatizOrigen * 360f)) / 360f;
            if (distancia > 0.12f) return c;

            float escala = SaturacionOrigen > 0.001f ? tinte.y / SaturacionOrigen : 1f;

            var salida = Color.HSVToRGB(tinte.x, Mathf.Clamp01(s * escala), v);
            salida.a = c.a;
            return salida;
        }

        /// <summary>
        /// Lleva cualquier pixel con color a un matiz concreto, conservando lo claro y lo
        /// saturado que ya era. Se diferencia de <see cref="Retenir"/> en que aquella solo
        /// toca los pixeles cercanos a un matiz de partida —vale para reteñir el mismo
        /// boton en cinco colores— mientras que esta cambia el color de algo que ya era de
        /// otro, como pasar la barra de vida de roja a azul.
        /// </summary>
        static Color ForzarMatiz(Color c, float matiz)
        {
            if (c.a < 0.004f) return c;

            Color.RGBToHSV(c, out _, out float s, out float v);

            // Los grises y los negros del contorno se quedan como estan: darles matiz
            // convertiria el borde de la barra en una silueta de color.
            if (s < 0.12f) return c;

            var salida = Color.HSVToRGB(matiz, s, v);
            salida.a = c.a;
            return salida;
        }

        static Sprite Recortar(string origen, string destino, RectInt caja, Vector4 borde,
                               int ppu, Vector2? tinte = null, float? matizFijo = null)
        {
            var existente = AssetDatabase.LoadAssetAtPath<Sprite>(destino);
            if (existente != null && !_forzar) return existente;

            if (!File.Exists(origen))
            {
                Debug.LogWarning($"[Tiny Tactics] No encuentro {origen}; la interfaz saldrá incompleta.");
                return null;
            }

            var fuente = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!fuente.LoadImage(File.ReadAllBytes(origen)))
            {
                Object.DestroyImmediate(fuente);
                Debug.LogWarning($"[Tiny Tactics] No pude decodificar {origen}.");
                return null;
            }

            if (caja.xMax > fuente.width || caja.yMax > fuente.height)
            {
                Debug.LogWarning(
                    $"[Tiny Tactics] El recorte {caja} no cabe en {origen} " +
                    $"({fuente.width}x{fuente.height}). ¿Cambió el pack?");
                Object.DestroyImmediate(fuente);
                return null;
            }

            var pixeles = fuente.GetPixels(caja.x, caja.y, caja.width, caja.height);

            if (tinte.HasValue)
                for (int i = 0; i < pixeles.Length; i++)
                    pixeles[i] = Retenir(pixeles[i], tinte.Value);

            if (matizFijo.HasValue)
                for (int i = 0; i < pixeles.Length; i++)
                    pixeles[i] = ForzarMatiz(pixeles[i], matizFijo.Value);

            var recorte = new Texture2D(caja.width, caja.height, TextureFormat.RGBA32, false);
            recorte.SetPixels(pixeles);
            recorte.Apply();

            File.WriteAllBytes(destino, recorte.EncodeToPNG());

            Object.DestroyImmediate(fuente);
            Object.DestroyImmediate(recorte);

            AssetDatabase.ImportAsset(destino, ImportAssetOptions.ForceUpdate);
            Configurar(destino, ppu, borde, false);

            return AssetDatabase.LoadAssetAtPath<Sprite>(destino);
        }

        static Sprite PrepararSprite(string ruta, int ppu)
        {
            Configurar(ruta, ppu, Vector4.zero, false);
            return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        }

        static Texture2D PrepararTextura(string ruta, int ppu, bool legible)
        {
            Configurar(ruta, ppu, Vector4.zero, legible);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        /// <summary>
        /// Filtro Point y sin comprimir: es pixel art, cualquier interpolación o bloque DXT
        /// lo emborrona. Los bordes 9-slice se escriben vía <c>TextureImporterSettings</c>,
        /// que es la ruta que respeta el resto de ajustes del importador.
        /// </summary>
        static void Configurar(string ruta, int ppu, Vector4 borde, bool legible)
        {
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null) return;

            var ajustes = new TextureImporterSettings();
            importador.ReadTextureSettings(ajustes);

            ajustes.textureType = TextureImporterType.Sprite;
            ajustes.spriteMode = (int)SpriteImportMode.Single;
            ajustes.spriteBorder = borde;
            ajustes.spritePixelsPerUnit = ppu;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            ajustes.alphaIsTransparency = true;
            ajustes.mipmapEnabled = false;
            ajustes.readable = legible;
            ajustes.filterMode = FilterMode.Point;
            ajustes.wrapMode = TextureWrapMode.Clamp;

            importador.SetTextureSettings(ajustes);
            importador.textureCompression = TextureImporterCompression.Uncompressed;
            importador.SaveAndReimport();
        }

        static void AsegurarCarpeta(string ruta)
        {
            if (AssetDatabase.IsValidFolder(ruta)) return;

            string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
            AsegurarCarpeta(padre);
            AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
        }

        // -----------------------------------------------------------------
        // Escena
        // -----------------------------------------------------------------

        /// <summary>Lienzo con el panel inferior y el puntero contextual.</summary>
        public static GameObject CrearLienzo(TemaInterfaz tema)
        {
            var go = new GameObject("Interfaz",
                                    typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var lienzo = go.GetComponent<Canvas>();
            lienzo.renderMode = RenderMode.ScreenSpaceOverlay;
            lienzo.sortingOrder = 100;

            // Escalado por resolución: el panel ocupa la misma fracción de pantalla en el
            // portátil de Kiara y en el monitor grande. Sin esto sería enorme o minúsculo.
            var escalador = go.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            go.AddComponent<PanelDeUnidad>().tema = tema;
            go.AddComponent<HudRecursos>().tema = tema;
            go.AddComponent<CursorJuego>().tema = tema;

            // Sin EventSystem los botones de uGUI no reciben un solo clic. No hacía falta
            // hasta ahora porque toda la entrada se leía del ratón directamente.
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem",
                               typeof(UnityEngine.EventSystems.EventSystem),
                               typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            return go;
        }

        /// <summary>Corchetes de selección: hijo apagado que la unidad enciende al elegirse.</summary>
        public static void AnadirMarcador(GameObject unidad, TemaInterfaz tema, int faccion, int orden)
        {
            if (tema == null || tema.marcadorSeleccion == null) return;

            var go = new GameObject("Seleccion");
            go.transform.SetParent(unidad.transform, false);
            go.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = tema.marcadorSeleccion;
            sr.color = tema.ColorDe(faccion);
            sr.sortingOrder = orden + 9000;

            go.AddComponent<MarcadorSeleccion>();
            go.SetActive(false);
        }

        /// <summary>Barra de vida flotando sobre la unidad.</summary>
        public static void AnadirBarraDeVida(GameObject unidad, TemaInterfaz tema, int orden)
        {
            if (tema == null || tema.barraChicaMarco == null || tema.barraChicaRelleno == null) return;

            var raiz = new GameObject("Vida");
            raiz.transform.SetParent(unidad.transform, false);
            // Debajo de los pies, no sobre la cabeza. A esa altura no choca con el
            // marcador de seleccion, que llega hasta -0,72.
            raiz.transform.localPosition = new Vector3(0f, -0.80f, 0f);
            raiz.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

            var marco = new GameObject("Marco");
            marco.transform.SetParent(raiz.transform, false);
            var srMarco = marco.AddComponent<SpriteRenderer>();
            srMarco.sprite = tema.barraChicaMarco;

            // Muy por encima de cualquier unidad: si la barra compartiera el rango de
            // ordenación de los sprites del mundo, la taparía la unidad de delante.
            srMarco.sortingOrder = orden + 10000;

            var escala = new GameObject("Escala");
            escala.transform.SetParent(raiz.transform, false);

            var relleno = new GameObject("Relleno");
            relleno.transform.SetParent(escala.transform, false);
            var srRelleno = relleno.AddComponent<SpriteRenderer>();
            srRelleno.sprite = tema.barraChicaRelleno;
            srRelleno.sortingOrder = orden + 10001;

            var barra = raiz.AddComponent<BarraDeVida>();
            barra.Configurar(srMarco, escala.transform, srRelleno);
        }
    }
}
