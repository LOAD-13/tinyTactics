using UnityEngine;

namespace TinyTactics.IA
{
    /// <summary>Las tres dificultades.</summary>
    public enum NivelDeIA { Facil, Normal, Dificil }

    /// <summary>
    /// Qué cambia de una dificultad a otra.
    /// </summary>
    /// <remarks>
    /// <b>La dificultad es economía y ritmo, no cerebro</b> (ADR-06). Es como funcionaban de
    /// verdad Warcraft, StarCraft y Age of Empires: la dificultad alta no piensa mejor, tiene
    /// más y llega antes. Implementar «una IA más lista» es un problema de investigación
    /// abierto; implementar cuatro multiplicadores es una tarde, y para el jugador el
    /// resultado se siente igual de difícil.
    ///
    /// <b>Y ninguna de las tres ve a través de la niebla</b> (ADR-23). La ventaja se da en
    /// recursos y en ritmo, que el jugador percibe como «el rival es fuerte»; la ventaja de
    /// información se percibe como «el rival hace trampa», aunque los números sean los mismos.
    /// <see cref="veTodoElMapa"/> queda como interruptor de pruebas.
    ///
    /// Los valores de fábrica de cada perfil están en <see cref="ReglasDeDificultad.Valores"/>,
    /// y no aquí, para poder comprobarlos fuera de Unity.
    /// </remarks>
    [CreateAssetMenu(fileName = "DificultadIA", menuName = "Tiny Tactics/Dificultad de IA")]
    public class DatosIA : ScriptableObject
    {
        public NivelDeIA nivel = NivelDeIA.Normal;
        public string nombreVisible = "Normal";

        [Header("Economia")]
        [Tooltip("Multiplica lo que el bot recibe al depositar. En Normal vale 1: juega con " +
                 "las mismas reglas que tu.")]
        [Range(0.5f, 3f)] public float ventajaDeRecursos = 1f;

        [Header("Ritmo")]
        [Tooltip("Multiplica el tamano de cada oleada.")]
        [Range(0.3f, 2f)] public float tamanoDeOleada = 1f;

        [Tooltip("Segundos de espera antes de la primera oleada. Un bot que ataca en el " +
                 "minuto uno no da partida: gana o pierde antes de que haya juego.")]
        [Range(0f, 300f)] public float esperaInicial = 60f;

        [Tooltip("Multiplica los segundos entre decisiones de la capa estrategica. Mas de 1 " +
                 "es un bot que tarda en reaccionar; menos de 1, uno que no pierde el tiempo.")]
        [Range(0.5f, 2f)] public float ritmo = 1f;

        [Header("Percepcion")]
        [Tooltip("Si el bot hace trampa y ve el mapa entero. Apagado en las tres " +
                 "dificultades (ADR-23): solo existe para pruebas.")]
        public bool veTodoElMapa;

        /// <summary>Vuelca los valores de fábrica de su nivel.</summary>
        public void Calibrar(NivelDeIA nuevo)
        {
            var v = ReglasDeDificultad.Valores(nuevo);

            nivel = nuevo;
            nombreVisible = v.nombre;
            ventajaDeRecursos = v.ventajaDeRecursos;
            tamanoDeOleada = v.tamanoDeOleada;
            esperaInicial = v.esperaInicial;
            ritmo = v.ritmo;
            veTodoElMapa = v.veTodoElMapa;
        }
    }

    /// <summary>Los números de un perfil, sin nada de Unity alrededor.</summary>
    public struct PerfilDeDificultad
    {
        public string nombre;
        public float ventajaDeRecursos;
        public float tamanoDeOleada;
        public float esperaInicial;
        public float ritmo;

        /// <summary>Siempre falso en los tres niveles (ADR-23). Lo comprueba el arnés.</summary>
        public bool veTodoElMapa;
    }

    /// <summary>
    /// Las decisiones de la dificultad, como funciones puras. Las comprueba el arnés.
    /// </summary>
    public static class ReglasDeDificultad
    {
        /// <summary>
        /// Los valores de fábrica de cada nivel.
        /// </summary>
        /// <remarks>
        /// <b>Normal no se toca</b>: es el bot contra el que se jugaron las partidas de la
        /// semana 10, y el único calibrado jugando. Los otros dos se apartan de él en las
        /// cuatro palancas a la vez y en el mismo sentido, para que «más difícil» no pueda
        /// ser más difícil en una cosa y más fácil en otra.
        ///
        /// Fácil: recoge un 25 % menos, ataca con un 30 % menos de gente, espera cuatro
        /// minutos antes de atacar y decide la mitad de rápido. Es un bot contra el que se
        /// aprende a jugar.
        ///
        /// Difícil: recoge un 35 % más, ataca con un 30 % más, empieza a los treinta segundos
        /// —lo que en la práctica es «en cuanto tenga cinco»— y decide un 30 % más rápido.
        /// Nada de eso le deja ver lo que no ha explorado.
        /// </remarks>
        public static PerfilDeDificultad Valores(NivelDeIA nivel)
        {
            switch (nivel)
            {
                case NivelDeIA.Facil:
                    return new PerfilDeDificultad
                    {
                        nombre = "Facil", ventajaDeRecursos = 0.75f, tamanoDeOleada = 0.7f,
                        esperaInicial = 240f, ritmo = 1.5f,
                    };

                case NivelDeIA.Dificil:
                    return new PerfilDeDificultad
                    {
                        nombre = "Dificil", ventajaDeRecursos = 1.35f, tamanoDeOleada = 1.3f,
                        esperaInicial = 30f, ritmo = 0.7f,
                    };

                default:
                    return new PerfilDeDificultad
                    {
                        nombre = "Normal", ventajaDeRecursos = 1f, tamanoDeOleada = 1f,
                        esperaInicial = 60f, ritmo = 1f,
                    };
            }
        }

        /// <summary>
        /// Lo que entra al almacén de un bot al depositar una carga, con su ventaja.
        /// </summary>
        /// <remarks>
        /// Las cargas son pequeñas —cinco de madera, diez de oro—, así que redondear cada
        /// una por separado se comería la ventaja: 5 × 1,35 = 6,75, que redondeado da 7 y
        /// truncado da 6, y ninguno de los dos es 1,35. <paramref name="resto"/> guarda la
        /// fracción que no cupo y la suma a la siguiente: al cabo de cien cargas, el bot ha
        /// recibido exactamente lo que dice su multiplicador, con un error de menos de uno.
        /// </remarks>
        public static int ConVentaja(int cantidad, float ventaja, ref float resto)
        {
            if (cantidad <= 0) return 0;

            float total = cantidad * Mathf.Max(0f, ventaja) + resto;
            int entero = Mathf.FloorToInt(total + 1e-4f);

            resto = Mathf.Max(0f, total - entero);
            return entero;
        }

        /// <summary>Segundos entre decisiones estratégicas, con el ritmo aplicado.</summary>
        public static float PasoEstrategico(float paso, float ritmo) =>
            Mathf.Max(0.2f, paso * Mathf.Clamp(ritmo, 0.5f, 2f));

        /// <summary>El siguiente nivel, en bucle. Lo usa el botón del panel.</summary>
        public static NivelDeIA Siguiente(NivelDeIA nivel) =>
            nivel == NivelDeIA.Dificil ? NivelDeIA.Facil : nivel + 1;
    }
}
