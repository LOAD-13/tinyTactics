using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.IA
{
    /// <summary>
    /// La mitad del bot que recolecta, entrena y construye.
    /// </summary>
    /// <remarks>
    /// <b>Es la capa que decide si el bot existe.</b> Un rival que pelea mal pero tiene
    /// economía da partida; uno que pelea bien y no recolecta se queda en seis unidades y no
    /// vuelve a aparecer. Por eso lo primero de cada vuelta no es el plan: es comprobar que
    /// ningún pawn está parado.
    ///
    /// No es un <c>MonoBehaviour</c>: la conduce <see cref="CerebroIA"/> y no necesita su
    /// propio ciclo de vida. Lo que sí necesita es no reservar memoria en cada vuelta, porque
    /// se ejecuta una vez por segundo y por bando: las listas son campos, no variables
    /// locales.
    /// </remarks>
    public class MenteEconomica
    {
        readonly CerebroIA _cerebro;

        readonly List<Unidad> _uno = new List<Unidad>(1);
        readonly List<Unidad> _ociosos = new List<Unidad>();
        readonly List<Unidad> _pawns = new List<Unidad>();

        public MenteEconomica(CerebroIA cerebro) => _cerebro = cerebro;

        int Faccion => _cerebro.faccion;

        // -----------------------------------------------------------------
        // Pawns
        // -----------------------------------------------------------------

        /// <summary>
        /// Ningún pawn parado, y el reparto entre madera y oro como pide el plan.
        /// </summary>
        /// <remarks>
        /// El reparto se recalcula entero cada vuelta en vez de decidirse pawn a pawn al
        /// nacer. Así, cuando muere uno o nace otro, la proporción se corrige sola en la
        /// siguiente pasada; decidiéndolo al nacer, un mal reparto se queda torcido para el
        /// resto de la partida.
        /// </remarks>
        public void MantenerPawns()
        {
            var plan = _cerebro.plan;
            if (plan == null) return;

            _pawns.Clear();
            _ociosos.Clear();

            var todas = RegistroDeUnidades.Todas;

            int enMadera = 0, enCarne = 0;

            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva || u.faccion != Faccion) continue;
                if (u.datos == null || u.datos.tipo != TipoUnidad.Pawn) continue;

                _pawns.Add(u);

                var recolector = u.GetComponent<RecolectorPawn>();
                var constructor = u.GetComponent<ConstructorPawn>();

                bool ocupado = (recolector != null && recolector.Recolectando) ||
                               (constructor != null && constructor.Construyendo);

                if (!ocupado) { _ociosos.Add(u); continue; }

                if (recolector == null || !recolector.Recolectando) continue;

                if (recolector.Recurso == TipoRecurso.Madera) enMadera++;
                else if (recolector.Recurso == TipoRecurso.Carne) enCarne++;
            }

            if (_ociosos.Count == 0) return;

            int quiereCarne = plan.pawnsALaCarne;

            // Regla reactiva, fuera del plan: con la despensa vacia, la carne manda. Desde
            // que el hambre quita vida, un bando a cero de carne se esta muriendo entero
            // mientras tala madera que ya no va a poder gastar.
            var eco = Economia.Actual;
            if (eco != null && eco.Cantidad(Faccion, TipoRecurso.Carne) <= 0)
                quiereCarne = Mathf.Max(quiereCarne, 4);

            PlanDeIA.Reparto(_pawns.Count, plan.pawnsALaMadera, quiereCarne,
                             out int quiereEnMadera, out int quiereEnCarne, out _);

            for (int i = 0; i < _ociosos.Count; i++)
            {
                // El contador sube al asignar para que dos pawns ociosos en la misma vuelta
                // no acaben los dos en el mismo recurso.
                TipoRecurso recurso;

                if (enCarne < quiereEnCarne) { recurso = TipoRecurso.Carne; enCarne++; }
                else if (enMadera < quiereEnMadera) { recurso = TipoRecurso.Madera; enMadera++; }
                else recurso = TipoRecurso.Oro;

                MandarARecolectar(_ociosos[i], recurso);
            }
        }

        void MandarARecolectar(Unidad pawn, TipoRecurso recurso)
        {
            // Radio generoso: en un mapa de 224 tiles, el bosque puede quedar lejos de la
            // base, y un pawn que no encuentra nada es un pawn parado para siempre.
            var nodo = NodoRecurso.MasCercano(pawn.transform.position, recurso, 60f);

            // Sin nodos de ese tipo cerca, se prueban los otros dos antes de rendirse:
            // mejor un pawn recogiendo lo que no tocaba que un pawn mirando al horizonte.
            if (nodo == null) nodo = OtroQueNoSea(pawn, recurso);

            if (nodo == null) return;

            _uno.Clear();
            _uno.Add(pawn);

            Autoridad.Emitir(new OrdenRecolectar { Faccion = Faccion, Nodo = nodo }, _uno);
        }

        NodoRecurso OtroQueNoSea(Unidad pawn, TipoRecurso evitado)
        {
            for (int i = 0; i < Recursos.Length; i++)
            {
                if (Recursos[i] == evitado) continue;

                var nodo = NodoRecurso.MasCercano(pawn.transform.position, Recursos[i], 60f);
                if (nodo != null) return nodo;
            }

            return null;
        }

        static readonly TipoRecurso[] Recursos =
        {
            TipoRecurso.Oro, TipoRecurso.Madera, TipoRecurso.Carne,
        };

        // -----------------------------------------------------------------
        // Poblacion
        // -----------------------------------------------------------------

        /// <summary>
        /// ¿Se va a quedar sin sitio para más unidades?
        /// </summary>
        /// <remarks>
        /// Esta regla va FUERA del plan y antes que él. El plan dice lo que el bot quiere
        /// tener; la población es lo que le impide tenerlo. Un bot que sigue su plan sin
        /// mirar la población se queda a tope con oro de sobra y una cola parada, que desde
        /// fuera no se lee como una IA fácil sino como una IA rota.
        ///
        /// El margen es de dos y no de cero: una casa tarda en levantarse, y pedirla cuando
        /// ya no cabe nadie significa que el bot se pasa ese rato sin producir.
        /// </remarks>
        public bool HaceFaltaCasa()
        {
            var poblacion = Poblacion.Actual;
            if (poblacion == null) return false;

            if (poblacion.Libre(Faccion) > 2) return false;

            // Ya hay una casa en obras: esperar. Sin esta comprobación el bot planta cinco
            // casas seguidas mientras la primera se levanta.
            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e != null && e.faccion == Faccion && e.tipo == TipoEdificio.Casa &&
                    !e.Operativo) return false;
            }

            return true;
        }

        // -----------------------------------------------------------------
        // Perseguir un objetivo del plan
        // -----------------------------------------------------------------

        /// <summary>Hace lo que haga falta para acercarse a ese objetivo.</summary>
        public string Perseguir(Objetivo que)
        {
            switch (que)
            {
                case Objetivo.Pawns: return Entrenar(TipoUnidad.Pawn);
                case Objetivo.Guerreros: return Entrenar(TipoUnidad.Guerrero);
                case Objetivo.Lanceros: return Entrenar(TipoUnidad.Lancero);
                case Objetivo.Arqueros: return Entrenar(TipoUnidad.Arquero);
                case Objetivo.Monjes: return Entrenar(TipoUnidad.Monje);

                case Objetivo.Casas: return Construir(TipoEdificio.Casa);
                case Objetivo.Cuarteles: return Construir(TipoEdificio.Cuartel);
                case Objetivo.CamposDeTiro: return Construir(TipoEdificio.CampoDeTiro);
                case Objetivo.Monasterios: return Construir(TipoEdificio.Monasterio);
                case Objetivo.Torres: return Construir(TipoEdificio.Torre);
            }

            return "sin saber que hacer";
        }

        /// <summary>
        /// Terminado el plan, sigue entrenando lo que menos tenga del bucle.
        /// </summary>
        /// <remarks>
        /// Lo que MENOS tenga, y no en orden: así el ejército sale mezclado solo, sin escribir
        /// proporciones en ninguna parte. Y con el triángulo de contadores de la semana 08,
        /// un ejército mezclado es bastante más difícil de contrarrestar que veinte guerreros.
        /// </remarks>
        public void SeguirElBucle(Objetivo[] bucle, int[] inventario)
        {
            if (bucle == null || bucle.Length == 0) return;

            Objetivo elegido = bucle[0];
            int menos = int.MaxValue;

            for (int i = 0; i < bucle.Length; i++)
            {
                int cuantos = inventario[(int)bucle[i]];
                if (cuantos >= menos) continue;

                menos = cuantos;
                elegido = bucle[i];
            }

            Perseguir(elegido);
        }

        // -----------------------------------------------------------------
        // Entrenar
        // -----------------------------------------------------------------

        string Entrenar(TipoUnidad tipo)
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion != Faccion || !e.Operativo) continue;
                if (!e.TryGetComponent(out ProduccionEdificio produccion)) continue;

                var catalogo = produccion.Catalogo;
                if (catalogo == null) continue;

                for (int k = 0; k < catalogo.Length; k++)
                {
                    var datos = catalogo[k];
                    if (datos == null || datos.tipo != tipo) continue;

                    // Encolar ya comprueba coste, poblacion y cola llena, y lo dice en
                    // «motivo». El bot no repite esas reglas: las usa.
                    if (produccion.Encolar(datos, out _)) return $"entrenando {tipo}";

                    return $"esperando para {tipo}";
                }
            }

            return $"sin edificio para {tipo}";
        }

        // -----------------------------------------------------------------
        // Construir
        // -----------------------------------------------------------------

        /// <summary>
        /// Planta un edificio: busca sitio, paga y manda a un pawn.
        /// </summary>
        /// <remarks>
        /// Se cobra ANTES de emitir la orden, igual que hace el jugador con su clic. Si se
        /// cobrara al plantar la obra, dos pawns con la misma orden pagarían dos veces por un
        /// edificio; y si no se cobrara nada, el bot construiría gratis — que es una trampa
        /// que nadie ha decidido conceder.
        /// </remarks>
        public string Construir(TipoEdificio tipo)
        {
            var ficha = _cerebro.FichaDe(tipo);
            if (ficha == null) return $"sin ficha de {tipo}";

            var economia = Economia.Actual;
            if (economia == null) return "sin economia";

            if (!economia.PuedePagar(Faccion, ficha.oro, ficha.madera))
                return $"ahorrando para {tipo}";

            var pawn = PawnLibre();
            if (pawn == null) return $"sin pawn libre para {tipo}";

            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return "sin mundo";

            // Desde el castillo si lo hay, y si no desde cualquier cosa suya que siga en
            // pie. Antes esto exigia castillo, y ese «return» era la causa de que un bando
            // al que le derribaban la base se quedara paralizado: sin castillo no media,
            // sin medir no construia, y sin construir no volvia a tener castillo.
            var referencia = _cerebro.Castillo() ?? CualquierEdificio();
            if (referencia == null) return "sin nada desde lo que medir";

            var centro = mundo.Grilla.MundoACelda(referencia.PosicionDePlanta);

            // Las torres se plantan mas lejos: una torre pegada al castillo no defiende
            // nada que el castillo no defienda ya.
            int separacion = tipo == TipoEdificio.Torre ? 9 : 4;

            if (!BuscadorDeSitio.Buscar(mundo.Grilla, centro, ficha.planta,
                                        separacion, 26, out var celdas))
                return $"sin sitio para {tipo}";

            if (!economia.Cobrar(Faccion, ficha.oro, ficha.madera))
                return $"no pudo pagar {tipo}";

            _uno.Clear();
            _uno.Add(pawn);

            Autoridad.Emitir(new OrdenConstruir
            {
                Faccion = Faccion,
                Edificio = ficha,
                Celdas = celdas,
                Variante = 0,
            }, _uno);

            return $"levantando {tipo}";
        }

        /// <summary>Cualquier edificio propio en pie. El ancla de un bando sin castillo.</summary>
        Edificio CualquierEdificio()
        {
            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
                if (casas[i] != null && casas[i].faccion == Faccion) return casas[i];

            return null;
        }

        /// <summary>
        /// ¿Se ha quedado sin castillo y puede levantar otro?
        /// </summary>
        /// <remarks>
        /// Va antes que todo lo demas del plan, incluso antes que las casas. Sin castillo no
        /// hay donde depositar, asi que recolectar no sirve de nada y la economia entera
        /// esta parada: cualquier otra cosa que el bot haga mientras tanto es tiempo tirado.
        /// </remarks>
        public bool HaceFaltaCastillo()
        {
            if (_cerebro.Castillo() != null) return false;

            // Uno en obras ya cuenta: hay que esperarlo, no plantar otro al lado.
            var casas = Edificio.Todos;
            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e != null && e.faccion == Faccion && e.centroDeEntrega) return false;
            }

            return true;
        }

        /// <summary>
        /// Un pawn al que se le puede encargar una obra.
        /// </summary>
        /// <remarks>
        /// Se prefiere uno que ya esté construyendo a uno recolectando — no: se prefiere uno
        /// que NO esté construyendo. Sacar de la obra al que ya está levantando algo dejaría
        /// las dos obras a medias; sacar a un recolector cuesta unos segundos de recurso y se
        /// recupera solo, porque al terminar vuelve a su nodo.
        /// </remarks>
        Unidad PawnLibre()
        {
            var todas = RegistroDeUnidades.Todas;

            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva || u.faccion != Faccion) continue;
                if (u.datos == null || u.datos.tipo != TipoUnidad.Pawn) continue;

                var constructor = u.GetComponent<ConstructorPawn>();
                if (constructor == null || constructor.Construyendo) continue;

                return u;
            }

            return null;
        }
    }
}
