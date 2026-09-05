using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Unidades;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Crea y mantiene los cinco assets de datos de unidad, con sus estadísticas del GDD
    /// y su tabla de animaciones.
    ///
    /// Los assets se reescriben enteros en cada pasada, a propósito. Un asset creado a
    /// medias en una versión anterior del código conserva campos viejos que ya no
    /// significan nada, y localizar eso cuesta más que regenerarlo. El balance real se
    /// tocará en la semana 16 sobre estos mismos assets; hasta entonces la fuente es el GDD.
    /// </summary>
    public static class CatalogoDeUnidades
    {
        const string Carpeta = "Assets/Datos/Unidades";

        /// <summary>Nombres de color de facción, en el orden del pack Tiny Swords.</summary>
        public static readonly string[] Colores = { "Blue", "Red", "Yellow", "Purple", "Black" };

        // El pack no ordena las carpetas como el GDD: aquí vive la única traducción entre
        // el nombre de dominio en español y la ruta original en inglés.
        const string DirUnidades = "Assets/Tiny Swords/Units/{color} Units";
        const string DirPawn = "Assets/Tiny Swords/Pawn and Resources/Pawn/{color} Pawn";

        // -----------------------------------------------------------------

        [MenuItem("Tiny Tactics/Reconstruir catálogo de unidades", false, 31)]
        public static void Reconstruir()
        {
            var datos = ObtenerTodas();

            Debug.Log(
                $"[Tiny Tactics] Catálogo reconstruido: {datos.Count} unidades en {Carpeta}.");

            Selection.activeObject = datos.Count > 0 ? datos[0] : null;
        }

        /// <summary>Las cinco unidades, en el orden del enum.</summary>
        public static List<DatosUnidad> ObtenerTodas()
        {
            AsegurarCarpeta();

            var salida = new List<DatosUnidad>();

            foreach (TipoUnidad tipo in System.Enum.GetValues(typeof(TipoUnidad)))
                salida.Add(Escribir(tipo));

            AssetDatabase.SaveAssets();
            return salida;
        }

        public static DatosUnidad Obtener(TipoUnidad tipo)
        {
            AsegurarCarpeta();
            return Escribir(tipo);
        }

        // -----------------------------------------------------------------

        static DatosUnidad Escribir(TipoUnidad tipo)
        {
            string ruta = $"{Carpeta}/{tipo}.asset";

            var datos = AssetDatabase.LoadAssetAtPath<DatosUnidad>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosUnidad>();
                AssetDatabase.CreateAsset(datos, ruta);
            }

            datos.tipo = tipo;
            Rellenar(datos, tipo);

            EditorUtility.SetDirty(datos);
            return datos;
        }

        /// <summary>Estadísticas de la tabla §4 del GDD y tiras de animación del pack.</summary>
        static void Rellenar(DatosUnidad d, TipoUnidad tipo)
        {
            switch (tipo)
            {
                case TipoUnidad.Guerrero:
                    d.nombreVisible = "Guerrero";
                    d.vidaMaxima = 140; d.dano = 18; d.alcance = 0.8f;
                    d.velocidad = 2.6f; d.radio = 0.44f; d.carnePorSegundo = 0.20f;
                    d.oro = 90; d.madera = 10;
                    d.clips = new[]
                    {
                        Clip(EstadoUnidad.Reposo, $"{DirUnidades}/Warrior/Warrior_Idle.png", 7f),
                        Clip(EstadoUnidad.Moviendo, $"{DirUnidades}/Warrior/Warrior_Run.png", 11f),
                        Clip(EstadoUnidad.Atacando, $"{DirUnidades}/Warrior/Warrior_Attack1.png", 12f, false),
                    };
                    break;

                case TipoUnidad.Lancero:
                    d.nombreVisible = "Lancero";
                    d.vidaMaxima = 100; d.dano = 22; d.alcance = 1.6f;
                    d.velocidad = 2.8f; d.radio = 0.46f; d.carnePorSegundo = 0.20f;
                    d.oro = 80; d.madera = 0;
                    d.clips = new[]
                    {
                        Clip(EstadoUnidad.Reposo, $"{DirUnidades}/Lancer/Lancer_Idle.png", 8f),
                        Clip(EstadoUnidad.Moviendo, $"{DirUnidades}/Lancer/Lancer_Run.png", 11f),

                        // Cinco tiras de ataque, una por cuadrante del lado derecho. Las
                        // tres orientaciones que faltan salen de voltear el sprite, así que
                        // con cinco dibujos se cubren las ocho direcciones.
                        Ataque($"{DirUnidades}/Lancer/Lancer_Up_Attack.png", DireccionAtaque.Arriba),
                        Ataque($"{DirUnidades}/Lancer/Lancer_UpRight_Attack.png", DireccionAtaque.ArribaDerecha),
                        Ataque($"{DirUnidades}/Lancer/Lancer_Right_Attack.png", DireccionAtaque.Derecha),
                        Ataque($"{DirUnidades}/Lancer/Lancer_DownRight_Attack.png", DireccionAtaque.AbajoDerecha),
                        Ataque($"{DirUnidades}/Lancer/Lancer_Down_Attack.png", DireccionAtaque.Abajo),
                    };
                    break;

                case TipoUnidad.Arquero:
                    d.nombreVisible = "Arquero";
                    d.vidaMaxima = 70; d.dano = 14; d.alcance = 5.0f;
                    d.velocidad = 2.9f; d.radio = 0.40f; d.carnePorSegundo = 0.15f;
                    d.oro = 85; d.madera = 20;
                    d.clips = new[]
                    {
                        Clip(EstadoUnidad.Reposo, $"{DirUnidades}/Archer/Archer_Idle.png", 7f),
                        Clip(EstadoUnidad.Moviendo, $"{DirUnidades}/Archer/Archer_Run.png", 11f),
                        Clip(EstadoUnidad.Atacando, $"{DirUnidades}/Archer/Archer_Shoot.png", 13f, false),
                    };
                    break;

                case TipoUnidad.Monje:
                    d.nombreVisible = "Monje";
                    d.vidaMaxima = 65; d.dano = -20; d.alcance = 3.5f;
                    d.velocidad = 2.7f; d.radio = 0.40f; d.carnePorSegundo = 0.15f;
                    d.oro = 120; d.madera = 0;
                    d.clips = new[]
                    {
                        Clip(EstadoUnidad.Reposo, $"{DirUnidades}/Monk/Idle.png", 7f),
                        Clip(EstadoUnidad.Moviendo, $"{DirUnidades}/Monk/Run.png", 11f),
                        Clip(EstadoUnidad.Atacando, $"{DirUnidades}/Monk/Heal.png", 12f, false),
                    };
                    break;

                default: // Pawn
                    d.nombreVisible = "Pawn";
                    d.vidaMaxima = 60; d.dano = 5; d.alcance = 0.5f;
                    d.velocidad = 3.0f; d.radio = 0.42f; d.carnePorSegundo = 0.10f;
                    d.oro = 50; d.madera = 0;
                    // Doce tiras: la tabla del pawn se indexa por estado y por recurso.
                    // El pack ya trae las tres herramientas y los tres sacos, así que la
                    // economía entera se dibuja sin una sola pieza de arte nueva.
                    d.clips = new[]
                    {
                        Clip(EstadoUnidad.Reposo, $"{DirPawn}/Pawn_Idle.png", 7f),
                        Clip(EstadoUnidad.Moviendo, $"{DirPawn}/Pawn_Run.png", 11f),

                        // El pawn no pelea: su «ataque» es el gesto de trabajar.
                        Clip(EstadoUnidad.Atacando, $"{DirPawn}/Pawn_Interact Axe.png", 12f, false),

                        // Trabajando: cada recurso tiene su herramienta.
                        Faena(EstadoUnidad.Trabajando, TipoRecurso.Oro,
                              $"{DirPawn}/Pawn_Interact Pickaxe.png", 12f),
                        Faena(EstadoUnidad.Trabajando, TipoRecurso.Madera,
                              $"{DirPawn}/Pawn_Interact Axe.png", 12f),
                        Faena(EstadoUnidad.Trabajando, TipoRecurso.Carne,
                              $"{DirPawn}/Pawn_Interact Knife.png", 12f),

                        // De vuelta al castillo, con el saco a la espalda.
                        Faena(EstadoUnidad.Moviendo, TipoRecurso.Oro,
                              $"{DirPawn}/Pawn_Run Gold.png", 11f),
                        Faena(EstadoUnidad.Moviendo, TipoRecurso.Madera,
                              $"{DirPawn}/Pawn_Run Wood.png", 11f),
                        Faena(EstadoUnidad.Moviendo, TipoRecurso.Carne,
                              $"{DirPawn}/Pawn_Run Meat.png", 11f),

                        // Y parado, que también se le tiene que ver la carga: si solo
                        // cambiara al andar, un pawn cargado que se detiene soltaría el
                        // saco de golpe sin haber llegado a ninguna parte.
                        Faena(EstadoUnidad.Reposo, TipoRecurso.Oro,
                              $"{DirPawn}/Pawn_Idle Gold.png", 7f),
                        Faena(EstadoUnidad.Reposo, TipoRecurso.Madera,
                              $"{DirPawn}/Pawn_Idle Wood.png", 7f),
                        Faena(EstadoUnidad.Reposo, TipoRecurso.Carne,
                              $"{DirPawn}/Pawn_Idle Meat.png", 7f),
                    };
                    break;
            }
        }

        /// <summary>
        /// Facción del muñeco de pruebas. Fuera del rango de bandos jugables a propósito:
        /// así nunca se puede seleccionar y siempre cuenta como enemigo para cualquiera.
        /// </summary>
        public const int FaccionNeutral = 99;

        const string DirOveja = "Assets/Tiny Swords/Pawn and Resources/Meat/Sheep";

        /// <summary>
        /// El poste de entrenamiento: una oveja quieta e indestructible que devuelve golpes.
        ///
        /// Existe para poder <b>ver</b> el ataque y, sobre todo, la muerte de nuestras
        /// propias unidades. Va al revés de lo que parece: la oveja no se hiere nunca, y
        /// cada golpe que recibe le devuelve cinco de daño al que la golpeó. Así se puede
        /// matar a una unidad propia a base de insistir, que es la única forma de ver la
        /// animación de muerte sin enemigos reales.
        ///
        /// Cinco es fijo y pequeño a propósito: no es un enemigo y no debe inventarse un
        /// daño que no está en ninguna tabla de balance. Se retira con la épica E06.
        /// </summary>
        public static DatosUnidad ObtenerMuneco()
        {
            AsegurarCarpeta();

            const string ruta = Carpeta + "/MunecoDePruebas.asset";

            var datos = AssetDatabase.LoadAssetAtPath<DatosUnidad>(ruta);
            if (datos == null)
            {
                datos = ScriptableObject.CreateInstance<DatosUnidad>();
                AssetDatabase.CreateAsset(datos, ruta);
            }

            datos.tipo = TipoUnidad.Pawn;
            datos.nombreVisible = "Poste de entrenamiento";
            datos.vidaMaxima = 999;
            datos.dano = 5;
            datos.alcance = 0f;
            datos.invulnerable = true;
            datos.velocidad = 0f;
            datos.radio = 0.5f;
            datos.carnePorSegundo = 0f;
            datos.oro = 0;
            datos.madera = 0;

            datos.clips = new[]
            {
                Clip(EstadoUnidad.Reposo, $"{DirOveja}/Sheep_Idle.png", 6f),
                Clip(EstadoUnidad.Moviendo, $"{DirOveja}/Sheep_Idle.png", 6f),
            };

            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssets();
            return datos;
        }

        /// <summary>Tira de ataque orientada. Solo la usa el lancero.</summary>
        static ClipUnidad Ataque(string ruta, DireccionAtaque direccion) =>
            new ClipUnidad
            {
                estado = EstadoUnidad.Atacando,
                ruta = ruta,
                fps = 10f,
                enBucle = false,
                direccion = direccion,
            };

        static ClipUnidad Clip(EstadoUnidad estado, string ruta, float fps, bool bucle = true) =>
            new ClipUnidad { estado = estado, ruta = ruta, fps = fps, enBucle = bucle };

        /// <summary>Tira ligada a un recurso: la herramienta con la que pica o el saco que lleva.</summary>
        static ClipUnidad Faena(EstadoUnidad estado, TipoRecurso recurso, string ruta, float fps) =>
            new ClipUnidad
            {
                estado = estado,
                recurso = recurso,
                ruta = ruta,
                fps = fps,
                enBucle = true,
            };

        // -----------------------------------------------------------------

        /// <summary>
        /// Resuelve la tabla de animaciones a sprites, ya sustituido el color del bando.
        ///
        /// Devuelve null si falta la tira de reposo: sin ella la unidad no se puede dibujar
        /// y es mejor no crearla que dejar un objeto invisible en el mapa.
        /// </summary>
        public static MaquinaDeEstados.Tira[] CargarTiras(
            DatosUnidad datos, int faccion, System.Func<string, List<Sprite>> cargador)
        {
            if (datos == null || datos.clips == null) return null;

            string color = Colores[Mathf.Clamp(faccion, 0, Colores.Length - 1)];
            var tiras = new List<MaquinaDeEstados.Tira>();

            foreach (var clip in datos.clips)
            {
                if (clip == null || string.IsNullOrEmpty(clip.ruta)) continue;

                var frames = cargador(clip.ruta.Replace("{color}", color));
                if (frames == null || frames.Count == 0)
                {
                    Debug.LogWarning(
                        $"[Tiny Tactics] {datos.nombreVisible} ({color}): no encuentro la tira " +
                        $"de {clip.estado} en {clip.ruta.Replace("{color}", color)}");
                    continue;
                }

                tiras.Add(new MaquinaDeEstados.Tira
                {
                    estado = clip.estado,
                    direccion = clip.direccion,
                    recurso = clip.recurso,
                    frames = frames.ToArray(),
                    fps = clip.fps,
                    enBucle = clip.enBucle,
                });
            }

            foreach (var t in tiras)
                if (t.estado == EstadoUnidad.Reposo) return tiras.ToArray();

            return null;
        }

        static void AsegurarCarpeta()
        {
            if (AssetDatabase.IsValidFolder(Carpeta)) return;

            if (!AssetDatabase.IsValidFolder("Assets/Datos"))
                AssetDatabase.CreateFolder("Assets", "Datos");

            AssetDatabase.CreateFolder("Assets/Datos", "Unidades");
        }
    }
}
