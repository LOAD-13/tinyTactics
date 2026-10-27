using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Audio
{
    /// <summary>Todo lo que el juego sabe hacer sonar.</summary>
    public enum Sonido
    {
        // Combate
        GolpeCortante, GolpePerforante, ArcoDisparo, FlechaImpacto, Curacion, MuerteUnidad,
        TorreFlecha,

        // Economía
        HachaArbol, PicoOro, OvejaBalido, DepositoRecursos,

        // Construcción
        MartilloObra, EdificioTerminado, EdificioDerrumbe, ExplosionEscombros,

        // Interfaz
        SeleccionarUnidad, OrdenMover, OrdenAtacar, BotonPanel, AccionNoValida,
        EntrenarUnidad, Victoria, Derrota,

        // Semana 14. Siempre al final: el catálogo guarda el número, no el nombre.
        AlertaAtaque,

        // Semana 15: la voz de Bruno en el tutorial.
        Murmullo,
    }

    /// <summary>
    /// Qué archivos suenan para cada <see cref="Sonido"/> y cómo.
    /// </summary>
    /// <remarks>
    /// <b>Los sonidos se enganchan por NOMBRE DE ARCHIVO, no por referencia a mano.</b> El
    /// generador busca en <c>Assets/Audio/Efectos</c> todo lo que empiece por el prefijo de
    /// cada entrada —<c>golpe_cortante_01</c>, <c>_02</c>, <c>_03</c>— y lo mete como tomas.
    /// Así cambiar un sonido es sustituir el archivo por otro con el mismo nombre, y añadir
    /// una variación es soltar un <c>_04</c> al lado. Ninguna de las dos cosas toca código.
    ///
    /// Es la misma decisión que el balance (ADR-07) aplicada al audio, y por el mismo motivo:
    /// los sonidos de esta semana son una primera tanda y algunos se van a cambiar.
    /// </remarks>
    [CreateAssetMenu(fileName = "CatalogoDeSonidos", menuName = "Tiny Tactics/Catalogo de sonidos")]
    public class CatalogoDeSonidos : ScriptableObject
    {
        [System.Serializable]
        public class Entrada
        {
            public Sonido sonido;

            [Tooltip("Principio del nombre de archivo. Todo lo que empiece asi es una toma.")]
            public string prefijo;

            public AudioClip[] tomas = new AudioClip[0];

            [Range(0f, 1f)] public float volumen = 0.8f;

            [Tooltip("Cuanto puede subir o bajar el tono en cada toma. Diez espadazos " +
                     "identicos suenan a maquina; con un 6 % arriba o abajo suenan a pelea.")]
            [Range(0f, 0.3f)] public float variacionDeTono = 0.06f;

            [Tooltip("Cuantas copias de este sonido pueden sonar a la vez.")]
            [Range(1, 8)] public int maximoSimultaneo = 3;

            [Tooltip("Segundos minimos entre dos disparos de este sonido.")]
            [Range(0f, 2f)] public float separacion = 0.06f;

            [Tooltip("Suena en la cabeza del jugador, no en el mapa: ni se atenua con la " +
                     "camara ni lo tapa la niebla.")]
            public bool deInterfaz;
        }

        public List<Entrada> entradas = new List<Entrada>();

        Dictionary<Sonido, Entrada> _indice;

        public Entrada De(Sonido sonido)
        {
            if (_indice == null || _indice.Count != entradas.Count)
            {
                _indice = new Dictionary<Sonido, Entrada>();
                foreach (var e in entradas)
                    if (e != null) _indice[e.sonido] = e;
            }

            return _indice.TryGetValue(sonido, out var entrada) ? entrada : null;
        }

        /// <summary>
        /// La configuración de fábrica de cada sonido: su prefijo y cómo se comporta.
        /// </summary>
        /// <remarks>
        /// Los límites de simultaneidad son lo que más importa en un RTS. Treinta guerreros
        /// pegando a la vez son treinta espadazos en el mismo fotograma: sin tope, eso es un
        /// muro de ruido que además se come los avisos de la interfaz. Con tres a la vez y
        /// una separación mínima, una batalla grande suena a batalla y no a interferencia.
        /// </remarks>
        public static Entrada Fabrica(Sonido s)
        {
            var e = new Entrada { sonido = s };

            switch (s)
            {
                case Sonido.GolpeCortante: Poner(e, "golpe_cortante_", 0.55f, 0.08f, 4, 0.05f); break;
                case Sonido.GolpePerforante: Poner(e, "golpe_perforante_", 0.55f, 0.08f, 3, 0.05f); break;
                case Sonido.ArcoDisparo: Poner(e, "arco_disparo_", 0.45f, 0.07f, 3, 0.05f); break;
                case Sonido.FlechaImpacto: Poner(e, "flecha_impacto_", 0.4f, 0.08f, 3, 0.05f); break;
                case Sonido.Curacion: Poner(e, "curacion_", 0.5f, 0.04f, 2, 0.3f); break;
                case Sonido.MuerteUnidad: Poner(e, "muerte_unidad_", 0.6f, 0.08f, 2, 0.12f); break;
                case Sonido.TorreFlecha: Poner(e, "torre_flecha_", 0.5f, 0.06f, 2, 0.1f); break;

                case Sonido.HachaArbol: Poner(e, "hacha_arbol_", 0.35f, 0.07f, 2, 0.15f); break;
                case Sonido.PicoOro: Poner(e, "pico_oro_", 0.35f, 0.07f, 2, 0.15f); break;
                case Sonido.OvejaBalido: Poner(e, "oveja_balido_", 0.45f, 0.1f, 1, 1.5f); break;
                case Sonido.DepositoRecursos: Poner(e, "deposito_recursos_", 0.35f, 0.05f, 1, 0.4f); break;

                case Sonido.MartilloObra: Poner(e, "martillo_obra_", 0.4f, 0.07f, 2, 0.12f); break;
                case Sonido.EdificioTerminado: Poner(e, "edificio_terminado_", 0.6f, 0f, 1, 0.5f, true); break;
                case Sonido.EdificioDerrumbe: Poner(e, "edificio_derrumbe_", 0.75f, 0.05f, 2, 0.3f); break;
                case Sonido.ExplosionEscombros: Poner(e, "explosion_escombros_", 0.5f, 0.1f, 3, 0.08f); break;

                case Sonido.SeleccionarUnidad: Poner(e, "seleccionar_unidad_", 0.45f, 0.03f, 1, 0.12f, true); break;
                case Sonido.OrdenMover: Poner(e, "orden_mover_", 0.45f, 0.03f, 1, 0.12f, true); break;
                case Sonido.OrdenAtacar: Poner(e, "orden_atacar_", 0.5f, 0.03f, 1, 0.12f, true); break;
                case Sonido.BotonPanel: Poner(e, "boton_panel_", 0.45f, 0.03f, 1, 0.06f, true); break;
                case Sonido.AccionNoValida: Poner(e, "accion_no_valida_", 0.55f, 0f, 1, 0.3f, true); break;
                case Sonido.EntrenarUnidad: Poner(e, "entrenar_unidad_", 0.4f, 0.02f, 1, 0.6f, true); break;
                case Sonido.Victoria: Poner(e, "victoria_", 0.8f, 0f, 1, 1f, true); break;
                case Sonido.Derrota: Poner(e, "derrota_", 0.8f, 0f, 1, 1f, true); break;
                case Sonido.AlertaAtaque: Poner(e, "alerta_ataque_", 0.5f, 0f, 1, 2f, true); break;
                case Sonido.Murmullo: Poner(e, "murmullo_", 0.35f, 0.12f, 2, 0.05f, true); break;
            }

            return e;
        }

        static void Poner(Entrada e, string prefijo, float volumen, float tono, int maximo,
                          float separacion, bool interfaz = false)
        {
            e.prefijo = prefijo;
            e.volumen = volumen;
            e.variacionDeTono = tono;
            e.maximoSimultaneo = maximo;
            e.separacion = separacion;
            e.deInterfaz = interfaz;
        }
    }
}
