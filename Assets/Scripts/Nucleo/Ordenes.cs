using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Movimiento;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Una acción que se le pide a una unidad.
    ///
    /// Es el <b>patrón Command</b> y el ADR-01 del proyecto. Ni el jugador ni la IA mueven
    /// unidades: ambos emiten órdenes que la autoridad valida y aplica. De ahí salen tres
    /// cosas: la IA usa exactamente el mismo camino que el jugador, las partidas se pueden
    /// repetir, y el día que se añada red basta con transportar órdenes en vez de
    /// reescribir el núcleo.
    /// </summary>
    public abstract class Orden
    {
        /// <summary>Bando que la emite. Sirve para validar que nadie mande unidades ajenas.</summary>
        public int Faccion;

        public abstract void Aplicar(Unidad unidad);
    }

    /// <summary>Ir a una celda del mapa.</summary>
    public class OrdenMover : Orden
    {
        public Vector2Int Destino;

        public override void Aplicar(Unidad unidad)
        {
            // Moverse cancela el objetivo: si no, la unidad daría dos pasos y volvería
            // corriendo a pegarle a lo que estuviera atacando antes. Lo mismo con la
            // recolección: un pawn al que mandas a otro sitio deja el árbol, no vuelve.
            Soltar(unidad);

            var movimiento = unidad.GetComponent<MovimientoUnidad>();
            if (movimiento != null) movimiento.IrA(Destino);
        }

        /// <summary>
        /// Corta todo lo que la unidad estuviera haciendo por su cuenta.
        ///
        /// Vive aquí y no en cada orden porque el orden de las dos llamadas importa y ya
        /// se pagó una vez: el recolector se apoya en la máquina para dejar de dibujar el
        /// trabajo, así que tiene que cancelarse antes de que la máquina se limpie.
        /// </summary>
        internal static void Soltar(Unidad unidad)
        {
            var recolector = unidad.GetComponent<RecolectorPawn>();
            if (recolector != null) recolector.Cancelar();

            var maquina = unidad.GetComponent<MaquinaDeEstados>();
            if (maquina != null) maquina.Cancelar();
        }
    }

    /// <summary>
    /// Mandar a un pawn a explotar un nodo del mapa.
    ///
    /// Entra por el mismo sitio que atacar o moverse, y no como una llamada suelta desde
    /// el ratón, por el ADR-01: el día que la IA tenga que gestionar su economía, emitirá
    /// exactamente esta orden y no hará falta tocar nada más.
    /// </summary>
    public class OrdenRecolectar : Orden
    {
        public Mundo.NodoRecurso Nodo;

        public override void Aplicar(Unidad unidad)
        {
            var recolector = unidad.GetComponent<RecolectorPawn>();

            // Las unidades militares ignoran la orden en vez de irse encima del árbol.
            // Seleccionar un grupo mixto y hacer clic en un bosque tiene que mandar a los
            // pawns y dejar quieto al resto, no arrastrar al ejército entero a talar.
            if (recolector == null || Nodo == null) return;

            var maquina = unidad.GetComponent<MaquinaDeEstados>();
            if (maquina != null) maquina.Cancelar();

            recolector.Recolectar(Nodo);
        }
    }

    /// <summary>
    /// Atacar a un objetivo.
    ///
    /// Esta semana solo reproduce la animación del golpe: el daño, el alcance y la
    /// respuesta del que lo recibe son la épica E06. Se modela ya como orden y no como
    /// una llamada suelta porque el punto del ADR-01 es que toda acción entre por el
    /// mismo sitio — cuando llegue el combate real, lo único que cambia es lo que hace
    /// <see cref="Aplicar"/>, no quién la emite.
    /// </summary>
    public class OrdenAtacar : Orden
    {
        public Unidad Objetivo;

        public override void Aplicar(Unidad unidad)
        {
            // La orden se limita a encargar el trabajo. Quien decide si hay que acercarse
            // primero, cuándo llega y qué le pasa al objetivo es la máquina de estados:
            // una orden puede tardar, y el ADR-01 no dice que tenga que resolverse en el
            // mismo fotograma, solo que todo entre por aquí.
            var recolector = unidad.GetComponent<RecolectorPawn>();
            if (recolector != null) recolector.Cancelar();

            var maquina = unidad.GetComponent<MaquinaDeEstados>();
            if (maquina != null) maquina.OrdenarAtaque(Objetivo);
        }
    }

    /// <summary>
    /// Curar a un aliado. Solo hace algo en unidades con daño negativo — el monje.
    ///
    /// Se resuelve por el mismo camino que el ataque porque para la máquina de estados es
    /// el mismo gesto: acercarse hasta el alcance y aplicar un efecto. Solo cambia el signo.
    /// </summary>
    public class OrdenCurar : Orden
    {
        public Unidad Objetivo;

        public override void Aplicar(Unidad unidad)
        {
            if (unidad.datos == null || unidad.datos.dano >= 0) return;

            var maquina = unidad.GetComponent<MaquinaDeEstados>();
            if (maquina != null) maquina.OrdenarAtaque(Objetivo);
        }
    }

    /// <summary>
    /// Llevar al castillo lo que se tenga en las manos.
    ///
    /// Es el clic derecho sobre el propio centro de entrega, como en cualquier RTS. Sin
    /// ella, un pawn al que has movido a mitad del viaje se queda con el saco puesto y sin
    /// forma directa de decirle que lo suelte.
    /// </summary>
    public class OrdenEntregar : Orden
    {
        public override void Aplicar(Unidad unidad)
        {
            var recolector = unidad.GetComponent<RecolectorPawn>();
            if (recolector == null) return;

            var maquina = unidad.GetComponent<MaquinaDeEstados>();
            if (maquina != null) maquina.Cancelar();

            recolector.Entregar();
        }
    }

    /// <summary>Quedarse quieto y cancelar lo que estuviera haciendo.</summary>
    public class OrdenDetener : Orden
    {
        public override void Aplicar(Unidad unidad)
        {
            OrdenMover.Soltar(unidad);

            var movimiento = unidad.GetComponent<MovimientoUnidad>();
            if (movimiento != null) movimiento.Detener();
        }
    }

    /// <summary>
    /// Único punto por el que pasan las órdenes.
    ///
    /// Hoy solo comprueba que la unidad esté viva y sea del bando que ordena. Cuando
    /// llegue el multijugador, este es el sitio donde las órdenes se enviarían por la red
    /// en vez de aplicarse directamente — y nada más del juego tendría que cambiar.
    /// </summary>
    public static class Autoridad
    {
        public static int OrdenesAplicadas { get; private set; }

        public static void Emitir(Orden orden, IReadOnlyList<Unidad> destinatarias)
        {
            if (orden == null || destinatarias == null) return;

            for (int i = 0; i < destinatarias.Count; i++)
            {
                var unidad = destinatarias[i];
                if (unidad == null || !unidad.Viva) continue;
                if (unidad.faccion != orden.Faccion) continue;

                orden.Aplicar(unidad);
                OrdenesAplicadas++;
            }
        }

        /// <summary>
        /// Reparte un grupo alrededor de un destino en anillos, para que no peleen todas
        /// por la misma celda. Sin esto, cincuenta unidades se amontonan en un punto.
        /// </summary>
        public static Vector2Int DestinoParaMiembro(Vector2Int centro, int indice)
        {
            if (indice == 0) return centro;

            // Espiral cuadrada: anillo 1 son 8 celdas, anillo 2 son 16, etc.
            int anillo = 1;
            int restante = indice - 1;

            while (restante >= anillo * 8)
            {
                restante -= anillo * 8;
                anillo++;
            }

            int lado = anillo * 2;
            int cara = restante / lado;
            int paso = restante % lado;

            switch (cara)
            {
                case 0: return centro + new Vector2Int(-anillo + paso, anillo);
                case 1: return centro + new Vector2Int(anillo, anillo - paso);
                case 2: return centro + new Vector2Int(anillo - paso, -anillo);
                default: return centro + new Vector2Int(-anillo, -anillo + paso);
            }
        }
    }
}
