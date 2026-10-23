using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.IA
{
    /// <summary>
    /// Las cosas que un plan puede pedir tener.
    /// </summary>
    /// <remarks>
    /// Un plan se escribe en objetivos —«ten cinco pawns», «ten un cuartel»— y no en
    /// acciones —«entrena un pawn»—. La diferencia importa cuando algo sale mal: si al bot
    /// le matan dos pawns, un plan de acciones sigue adelante como si nada y un plan de
    /// objetivos vuelve a entrenarlos solo. El plan deja de ser una lista que se recorre una
    /// vez y pasa a ser una descripcion de como quiere estar el bot.
    /// </remarks>
    public enum Objetivo
    {
        Pawns,
        Casas,
        Cuarteles,
        CamposDeTiro,
        Monasterios,
        Torres,
        Guerreros,
        Lanceros,
        Arqueros,
        Monjes,
    }

    /// <summary>Un renglon del plan: cuantos de algo quiere tener el bot.</summary>
    [System.Serializable]
    public class PasoDelPlan
    {
        public Objetivo que = Objetivo.Pawns;

        [Tooltip("Cuantos quiere tener EN TOTAL, no cuantos anade.")]
        [Min(1)] public int cantidad = 1;
    }

    /// <summary>
    /// El plan de juego de un bot, editable sin recompilar.
    /// </summary>
    /// <remarks>
    /// <b>El plan es un dato, no un <c>switch</c></b> (ADR-21). Cambiar como abre el rival
    /// —mas pawns antes del cuartel, torres antes que arqueros— no puede costar una
    /// recompilacion, por la misma razon que el balance vive en <c>ScriptableObject</c>
    /// desde la semana 04: en la semana de ajuste se haran cien iteraciones, no diez.
    ///
    /// Y de paso resuelve lo de las tres dificultades de la semana 11 casi solo: una
    /// dificultad distinta puede ser, literalmente, otro plan.
    /// </remarks>
    [CreateAssetMenu(fileName = "PlanIA", menuName = "Tiny Tactics/Plan de IA")]
    public class PlanDeIA : ScriptableObject
    {
        [Header("Identidad")]
        public string nombreVisible = "Estandar";

        [Header("Apertura")]
        [Tooltip("Se recorre en orden. El primer paso que no se cumple es lo que el bot " +
                 "esta intentando conseguir ahora mismo.")]
        public PasoDelPlan[] pasos = new PasoDelPlan[0];

        [Header("Cuando el plan se acaba")]
        [Tooltip("Terminada la apertura, el bot sigue entrenando esto en bucle. Sin esto, " +
                 "un bot que cumple su plan se queda quieto para siempre y la partida no " +
                 "termina nunca.")]
        public Objetivo[] bucleMilitar =
        {
            Objetivo.Guerreros, Objetivo.Arqueros, Objetivo.Lanceros,
        };

        [Header("Economia")]
        [Tooltip("De cada diez pawns, cuantos van a la madera.")]
        [Range(0, 10)] public int pawnsALaMadera = 4;

        [Tooltip("De cada diez pawns, cuantos van a la carne. El resto va al oro. " +
                 "Desde la semana 10 el hambre quita vida, asi que un bot que ignore la " +
                 "carne se muere solo. Dos de cada diez bastan para un ejercito mediano.")]
        [Range(0, 10)] public int pawnsALaCarne = 2;

        [Tooltip("Pawns como mucho. Mas alla de esto, la poblacion se gasta en ejercito.")]
        [Range(2, 30)] public int topePawns = 10;

        [Header("Oleadas")]
        [Tooltip("Unidades militares que junta antes de lanzar la primera oleada.")]
        [Range(2, 30)] public int primeraOleada = 5;

        [Tooltip("Cuanto crece cada oleada respecto a la anterior.")]
        [Range(0, 10)] public int crecimientoPorOleada = 2;

        [Tooltip("Tope de una oleada. Sin tope, en una partida larga el bot junta cuarenta " +
                 "unidades y no ataca nunca porque nunca llega al numero.")]
        [Range(4, 50)] public int oleadaMaxima = 16;

        // -----------------------------------------------------------------
        // Logica pura — probada en tools/pruebas/PruebasDeIA.cs
        // -----------------------------------------------------------------

        /// <summary>
        /// Indice del primer paso sin cumplir, o -1 si el plan esta completo.
        /// </summary>
        /// <param name="pasos">Los renglones del plan, en orden.</param>
        /// <param name="inventario">Cuantos hay de cada cosa, indexado por <see cref="Objetivo"/>.</param>
        /// <remarks>
        /// Estatica y sin tocar la escena a proposito: es la decision central del bot y tiene
        /// que poder comprobarse sin abrir Unity. Todo lo que el bot decide de verdad esta
        /// escrito asi.
        /// </remarks>
        public static int PasoActual(IReadOnlyList<PasoDelPlan> pasos, int[] inventario)
        {
            if (pasos == null || inventario == null) return -1;

            for (int i = 0; i < pasos.Count; i++)
            {
                var paso = pasos[i];
                if (paso == null) continue;

                int indice = (int)paso.que;
                if (indice < 0 || indice >= inventario.Length) continue;

                if (inventario[indice] < paso.cantidad) return i;
            }

            return -1;
        }

        /// <summary>
        /// Cuantas unidades junta la oleada numero <paramref name="lanzadas"/> + 1.
        /// </summary>
        /// <remarks>
        /// Crece con cada oleada porque si no, el bot que pierde la primera manda otras
        /// cinco iguales contra una base que ya sabe defenderse. Y tiene tope porque sin el,
        /// en una partida larga el numero se le escapa por encima de lo que puede producir y
        /// deja de atacar del todo — que es peor que atacar mal.
        /// </remarks>
        public static int TamanoDeOleada(int lanzadas, int primera, int crecimiento,
                                         int maxima, float multiplicador)
        {
            int bruto = primera + Mathf.Max(0, lanzadas) * Mathf.Max(0, crecimiento);
            int conDificultad = Mathf.RoundToInt(bruto * Mathf.Max(0.1f, multiplicador));

            return Mathf.Clamp(conDificultad, 1, Mathf.Max(1, maxima));
        }

        /// <summary>
        /// ¿Toca atacar ya?
        /// </summary>
        /// <param name="tropa">Unidades militares vivas.</param>
        /// <param name="hacenFalta">Las que pide la oleada que toca.</param>
        /// <param name="minimo">Por debajo de esto no se ataca ni harto de esperar.</param>
        /// <param name="esperando">Segundos sin que la tropa crezca.</param>
        /// <param name="paciencia">Segundos tras los que se ataca con lo que haya.</param>
        /// <remarks>
        /// <b>La segunda condicion es la que impide el bloqueo, y existe por un fallo real.</b>
        /// Con solo la primera, el bot se quedaba esperando para siempre: cada oleada pide
        /// mas unidades que la anterior, y si se las matan tan rapido como las produce, el
        /// numero nunca se alcanza. El ejercito entero se quedaba parado delante de la base
        /// enemiga esperando a un refuerzo que no iba a llegar.
        ///
        /// Si la tropa no ha crecido en medio minuto, esperar mas no va a cambiar nada:
        /// atacar con lo que hay es peor jugada que atacar con quince, pero es infinitamente
        /// mejor que no atacar nunca.
        /// </remarks>
        /// <summary>
        /// ¿A cuál de los enemigos cercanos hay que responder antes de seguir?
        /// </summary>
        /// <remarks>
        /// Devuelve el enemigo que esté más cerca de CUALQUIERA de las unidades propias, si
        /// está dentro del radio; −1 si ninguno lo está. Se mide contra cada unidad y no
        /// contra el centro del grupo porque una oleada en marcha va estirada: al que pegan
        /// en la retaguardia le están pegando aunque el centro quede lejos.
        /// </remarks>
        public static int ElegirAmenaza(IReadOnlyList<Vector2> propias,
                                        IReadOnlyList<Vector2> enemigas, float radio)
        {
            int mejor = -1;
            float mejorDistancia = radio * radio;

            for (int e = 0; e < enemigas.Count; e++)
                for (int p = 0; p < propias.Count; p++)
                {
                    float d = (enemigas[e] - propias[p]).sqrMagnitude;
                    if (d > mejorDistancia) continue;

                    mejorDistancia = d;
                    mejor = e;
                }

            return mejor;
        }

        /// <summary>Lo que un bot sabe de cada rival posible al elegir contra quién ir.</summary>
        public struct CandidatoARival
        {
            public int faccion;

            /// <summary>Del castillo propio a su edificio descubierto más cercano, en casillas.</summary>
            public float distancia;

            /// <summary>Edificios suyos que este bot ha visto: a menos, más débil parece.</summary>
            public int edificios;

            /// <summary>1 si acaba de atacarme, 0 si nunca o hace mucho.</summary>
            public float rencor;

            /// <summary>Cuántos otros bots van ya a por él.</summary>
            public int yaLoAtacan;
        }

        /// <summary>Peso de cada motivo al elegir rival. Menos puntos, mejor presa.</summary>
        public const float PesoDistancia = 1f, PesoFuerza = 0.5f, PesoRencor = 0.7f, PesoReparto = 0.4f;

        /// <summary>
        /// Contra quién va la próxima ofensiva en un todos contra todos (semana 14).
        /// </summary>
        /// <remarks>
        /// Cuatro motivos, los que usaría una persona: <b>lo cerca</b> que está (cruzar el
        /// mapa entero dejando la casa vacía es mala idea), <b>lo débil</b> que parece,
        /// <b>quién me ha atacado</b> (el rencor se olvida con el tiempo) y <b>cuántos van ya
        /// a por él</b>. El último es el que hace que un FFA sea un FFA: sin él, cuatro bots
        /// eligen al mismo —casi siempre al jugador, que suele ser el que más molesta— y la
        /// partida se convierte en cuatro contra uno.
        /// <para>
        /// Distancia y fuerza se normalizan contra el máximo de los candidatos, para que los
        /// pesos signifiquen lo mismo en un mapa de 96 casillas y en uno de 224. Con un único
        /// candidato —el 1v1 de siempre— no hay nada que elegir.
        /// </para>
        /// </remarks>
        public static int ElegirRival(IReadOnlyList<CandidatoARival> candidatos)
        {
            if (candidatos == null || candidatos.Count == 0) return -1;

            float maxDistancia = 0.01f, maxEdificios = 1f;
            foreach (var c in candidatos)
            {
                maxDistancia = Mathf.Max(maxDistancia, c.distancia);
                maxEdificios = Mathf.Max(maxEdificios, c.edificios);
            }

            int mejor = -1;
            float mejorPuntos = float.MaxValue;
            for (int i = 0; i < candidatos.Count; i++)
            {
                var c = candidatos[i];
                float puntos = PesoDistancia * c.distancia / maxDistancia
                             + PesoFuerza * c.edificios / maxEdificios
                             - PesoRencor * Mathf.Clamp01(c.rencor)
                             + PesoReparto * c.yaLoAtacan;
                if (puntos >= mejorPuntos) continue;
                mejorPuntos = puntos;
                mejor = c.faccion;
            }

            return mejor;
        }

        /// <summary>Lo que queda del rencor <paramref name="segundos"/> después del ataque.</summary>
        public static float Rencor(float segundos, float memoria) =>
            segundos < 0f || memoria <= 0f ? 0f : Mathf.Clamp01(1f - segundos / memoria);

        public static bool HoraDeAtacar(int tropa, int hacenFalta, int minimo,
                                        float esperando, float paciencia)
        {
            if (tropa <= 0) return false;
            if (tropa >= hacenFalta) return true;

            return tropa >= Mathf.Max(1, minimo) && esperando > paciencia;
        }

        /// <summary>
        /// Reparte los pawns entre los tres recursos segun la proporcion del plan.
        /// </summary>
        /// <remarks>
        /// Devuelve cuantos DEBERIA haber en cada sitio, y se recalcula entero cada vuelta
        /// en vez de decidirse pawn a pawn al nacer: asi, al morir uno o nacer otro, la
        /// proporcion se corrige sola en la siguiente pasada. Decidiendolo al nacer, un mal
        /// reparto se queda torcido el resto de la partida.
        ///
        /// Y hay una garantia que no es cosmetica: <b>mientras haya pawns suficientes,
        /// ningun recurso con proporcion pedida se queda a cero</b>. Con cuatro pawns y una
        /// proporcion de 2 sobre 10, redondear deja la carne en cero — y desde que el hambre
        /// quita vida, cero en la carne es el bando entero muriendose despacio.
        /// </remarks>
        public static void Reparto(int total, int deCadaDiezMadera, int deCadaDiezCarne,
                                   out int madera, out int carne, out int oro)
        {
            madera = carne = oro = 0;
            if (total <= 0) return;

            int pMadera = Mathf.Clamp(deCadaDiezMadera, 0, 10);
            int pCarne = Mathf.Clamp(deCadaDiezCarne, 0, 10 - pMadera);

            madera = Mathf.RoundToInt(total * pMadera / 10f);
            carne = Mathf.RoundToInt(total * pCarne / 10f);
            oro = total - madera - carne;

            // El redondeo puede pasarse: si se pasa, lo paga el que mas tenga.
            while (oro < 0)
            {
                if (madera >= carne) madera--;
                else carne--;

                oro = total - madera - carne;
            }

            int pedidos = (pMadera > 0 ? 1 : 0) + (pCarne > 0 ? 1 : 0) +
                          (pMadera + pCarne < 10 ? 1 : 0);

            if (total < pedidos) return;

            Asegurar(ref madera, ref carne, ref oro, pMadera > 0);
            Asegurar(ref carne, ref madera, ref oro, pCarne > 0);
            Asegurar(ref oro, ref madera, ref carne, pMadera + pCarne < 10);
        }

        /// <summary>Le da uno al que se quedo sin nada, quitandoselo al que mas tiene.</summary>
        static void Asegurar(ref int flaco, ref int otroA, ref int otroB, bool loPide)
        {
            if (!loPide || flaco > 0) return;

            if (otroA >= otroB && otroA > 1) { otroA--; flaco++; }
            else if (otroB > 1) { otroB--; flaco++; }
        }
    }
}
