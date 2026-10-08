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
    /// La mitad del bot que decide a dónde va el ejército.
    /// </summary>
    /// <remarks>
    /// <b>Junta, y luego va.</b> Mandar cada unidad en cuanto sale del cuartel es la forma
    /// más rápida de regalarlas: llegan de una en una y mueren de una en una. El bot espera a
    /// tener una oleada del tamaño que pide su plan, y ese tamaño crece con cada oleada
    /// lanzada — si la primera no bastó, la segunda es mayor.
    ///
    /// <b>Y antes de atacar, mira su casa.</b> Si hay enemigos rondando su base, la oleada se
    /// da la vuelta. Defender no es un sistema aparte: es la misma decisión —a dónde mando el
    /// ejército— con otra respuesta. Por eso entra aquí y no en un componente nuevo.
    ///
    /// <b>Respeta su propia niebla.</b> Solo ataca lo que su bando ha descubierto. Como las
    /// bases de salida se ven desde el primer fotograma (semana 09), sabe a dónde ir desde el
    /// principio, pero no sabe qué hay entre medias ni qué le espera.
    /// </remarks>
    public class MenteMilitar
    {
        readonly CerebroIA _cerebro;

        readonly List<Unidad> _tropa = new List<Unidad>();
        readonly List<Unidad> _cerca = new List<Unidad>();

        float _nacimiento;

        /// <summary>El edificio al que va la oleada en marcha, o null si no hay ninguna.</summary>
        Edificio _presa;

        float _proximoEmpujon;

        /// <summary>Oleadas que llegaron a cumplir su objetivo. Cada una es mayor.</summary>
        public int Lanzadas { get; private set; }

        /// <summary>Qué está haciendo el ejército. Lo enseña el panel.</summary>
        public string Estado { get; private set; } = "reuniendo";

        public MenteMilitar(CerebroIA cerebro)
        {
            _cerebro = cerebro;
            _nacimiento = Time.time;
        }

        int Faccion => _cerebro.faccion;

        public void Pensar()
        {
            var plan = _cerebro.plan;
            if (plan == null) return;

            Reunir();

            if (_tropa.Count == 0)
            {
                Estado = "sin ejercito";
                return;
            }

            // Defender gana siempre. Una oleada que sigue camino del enemigo mientras le
            // queman la base cambia un cuartel por una casa, y esa es una mala idea que
            // ningun jugador cometeria dos veces.
            if (HayIntrusos(out var donde))
            {
                Estado = "defendiendo la base";

                // Defender corta la ofensiva: cuando se vuelva a atacar, sera una nueva.
                _presa = null;
                _enOfensiva = false;
                _desdeQueEspera = Time.time;

                MandarA(donde);
                return;
            }

            float espera = _cerebro.dificultad != null ? _cerebro.dificultad.esperaInicial : 60f;
            if (Time.time - _nacimiento < espera)
            {
                Estado = "creciendo";
                Quedarse();
                return;
            }

            float multiplicador = _cerebro.dificultad != null
                ? _cerebro.dificultad.tamanoDeOleada
                : 1f;

            int hacenFalta = PlanDeIA.TamanoDeOleada(Lanzadas, plan.primeraOleada,
                                                     plan.crecimientoPorOleada,
                                                     plan.oleadaMaxima, multiplicador);

            if (_tropa.Count < hacenFalta)
            {
                Estado = $"juntando {_tropa.Count}/{hacenFalta}";
                Quedarse();
                return;
            }

            SeguirOVolverALanzar(hacenFalta);
        }

        /// <summary>
        /// Mantiene viva la oleada que ya va en camino, o lanza la siguiente.
        /// </summary>
        /// <remarks>
        /// <b>Este metodo es el arreglo del fallo que dejaba al bot plantado.</b> Antes, el
        /// contador de oleadas subia cada vez que se emitia la orden de atacar — y eso corre
        /// tres veces por segundo. La IA atacaba, sumaba una oleada, y 0,35 s despues ya
        /// necesitaba dos unidades mas para volver a atacar. En dos segundos el numero que
        /// pedia superaba a su ejercito entero y dejaba de atacar PARA SIEMPRE, con las
        /// tropas paradas donde estuvieran.
        ///
        /// Ahora una oleada es un ESTADO, no un instante: se lanza una vez, se mantiene
        /// mientras su objetivo siga en pie, y solo cuando cae se cuenta como lanzada y la
        /// siguiente crece. Mientras tanto, a quien se queda ocioso se le vuelve a dar la
        /// orden — que es lo que hace que un grupo que acaba de derribar algo no se quede
        /// mirando al horizonte.
        /// </remarks>
        void SeguirOVolverALanzar(int hacenFalta)
        {
            if (_enOfensiva) Continuar();
            else Reunirse(hacenFalta);
        }

        /// <summary>
        /// La ofensiva sigue: en cuanto cae un edificio se pasa al siguiente.
        /// </summary>
        /// <remarks>
        /// <b>Una ofensiva es un periodo, no un edificio.</b> Esa confusion es la que dejaba
        /// al ejercito plantado: se contaba una oleada por cada edificio derribado, y como
        /// cada oleada exige mas unidades que la anterior, el numero que el bot pedia crecia
        /// mas rapido de lo que podia reponer. Tres o cuatro edificios despues pedia quince
        /// unidades, tenia nueve, y se quedaba esperando a unas refuerzos que nunca llegaban
        /// — con el ejercito entero parado delante de la base enemiga.
        ///
        /// Ahora, una vez empezada, la ofensiva no vuelve a mirar ese numero: sigue
        /// encadenando objetivos hasta que la tropa baja del umbral de retirada. Que es, de
        /// paso, lo que hace cualquier jugador: si acabas de tirar un cuartel y te quedan
        /// ocho unidades sanas, sigues con el siguiente edificio, no las mandas a casa.
        /// </remarks>
        void Continuar()
        {
            int retirada = Mathf.Max(1, _cerebro.plan.primeraOleada / 2);

            if (_tropa.Count < retirada)
            {
                // La ofensiva se acabo. AHORA se cuenta, y la siguiente pedira mas gente.
                _enOfensiva = false;
                _presa = null;
                Lanzadas++;

                _desdeQueEspera = Time.time;
                _mejorTropa = _tropa.Count;

                Estado = "replegandose";
                return;
            }

            // Si la presa cayo o no habia, se busca la siguiente sin pasar por la reunion.
            if (_presa == null || !((IObjetivo)_presa).Vivo)
            {
                _presa = ObjetivoConocido();

                if (_presa == null)
                {
                    // Nada conocido que atacar NO significa que no quede nada: significa que
                    // no se ve. Antes esto paraba la ofensiva y el ejercito se quedaba
                    // plantado entre los escombros de la base que acababa de arrasar — y la
                    // partida no terminaba nunca, porque al rival le quedaba una casa en una
                    // esquina que el bot no habia visto.
                    Estado = "buscando al rival";
                    Explorar();
                    return;
                }

                Estado = $"encadenando ({_tropa.Count})";
                Atacar(_presa);
                _proximoEmpujon = Time.time + segundosEntreEmpujones;
                return;
            }

            Estado = $"atacando ({_tropa.Count})";

            // El empujon es para quien se haya quedado ocioso. A quien sigue con el objetivo
            // puesto, la orden repetida no le hace nada —la maquina de estados la ignora por
            // ser la misma— asi que no interrumpe a nadie que este peleando.
            if (Time.time < _proximoEmpujon) return;
            _proximoEmpujon = Time.time + segundosEntreEmpujones;

            Atacar(_presa);
        }

        /// <summary>
        /// Junta ejercito hasta tener bastante — o hasta cansarse de esperar.
        /// </summary>
        /// <remarks>
        /// <b>La paciencia tiene limite, y ese limite es lo que impide el bloqueo.</b> Si la
        /// tropa no crece en medio minuto, esperar mas no va a servir: o le estan matando las
        /// unidades a la misma velocidad que las produce, o se quedo sin recursos. En los dos
        /// casos, atacar con lo que hay es mejor que no atacar nunca.
        /// </remarks>
        void Reunirse(int hacenFalta)
        {
            if (_tropa.Count > _mejorTropa)
            {
                _mejorTropa = _tropa.Count;
                _desdeQueEspera = Time.time;
            }

            int minimo = Mathf.Max(2, _cerebro.plan.primeraOleada / 2);

            bool bastante = _tropa.Count >= hacenFalta;
            bool ataca = PlanDeIA.HoraDeAtacar(_tropa.Count, hacenFalta, minimo,
                                               Time.time - _desdeQueEspera, pacienciaMaxima);

            if (!ataca)
            {
                Estado = $"juntando {_tropa.Count}/{hacenFalta}";
                return;
            }

            var presa = ObjetivoConocido();
            if (presa == null)
            {
                Estado = "buscando al rival";
                Explorar();
                return;
            }

            _enOfensiva = true;
            _presa = presa;
            _proximoEmpujon = Time.time + segundosEntreEmpujones;

            Estado = !bastante
                ? $"atacando igual ({_tropa.Count})"
                : $"atacando ({_tropa.Count})";

            Atacar(presa);
        }

        bool _enOfensiva;
        int _mejorTropa;
        float _desdeQueEspera;

        /// <summary>Segundos que espera a que crezca la tropa antes de atacar igualmente.</summary>
        const float pacienciaMaxima = 30f;

        /// <summary>Cada cuanto se le recuerda a la tropa a donde iba.</summary>
        /// <remarks>
        /// Tres segundos: lo bastante seguido para que nadie se quede parado tras derribar
        /// un edificio, y lo bastante espaciado para no interrumpir a quien esta peleando.
        /// </remarks>
        const float segundosEntreEmpujones = 3f;

        // -----------------------------------------------------------------
        // La tropa
        // -----------------------------------------------------------------

        /// <summary>Las unidades militares vivas del bando. Los pawns no cuentan.</summary>
        void Reunir()
        {
            _tropa.Clear();

            var todas = RegistroDeUnidades.Todas;

            for (int i = 0; i < todas.Count; i++)
            {
                var u = todas[i];
                if (u == null || !u.Viva || u.faccion != Faccion || u.datos == null) continue;

                // El pawn no es ejercito, y el monje tampoco pelea — pero acompana, porque
                // una oleada con un monje detras aguanta bastante mas que una sin el.
                if (u.datos.tipo == TipoUnidad.Pawn) continue;

                _tropa.Add(u);
            }
        }

        // -----------------------------------------------------------------
        // Defensa
        // -----------------------------------------------------------------

        /// <summary>
        /// ¿Hay enemigos rondando la base?
        /// </summary>
        /// <remarks>
        /// Se pregunta por la grilla espacial y no recorriendo todas las unidades, que es la
        /// regla del proyecto desde la semana 04. Y se filtra por la niebla del PROPIO bando:
        /// el bot no puede reaccionar a un ataque que no ve, igual que tú tampoco.
        /// </remarks>
        bool HayIntrusos(out Vector3 donde)
        {
            donde = default;

            var castillo = _cerebro.Castillo();
            if (castillo == null) return false;

            var casa = castillo.PosicionDePlanta;

            RegistroDeUnidades.VecinasEnRadio(casa, radioDeAlarma, _cerca);

            for (int i = 0; i < _cerca.Count; i++)
            {
                var u = _cerca[i];
                if (u == null || !u.Viva || u.faccion == Faccion) continue;
                if (u.datos == null) continue;

                if (!NieblaDeGuerra.Ve(Faccion, u.transform.position)) continue;

                donde = u.transform.position;
                return true;
            }

            return false;
        }

        /// <summary>Hasta dónde llega «mi casa», en tiles.</summary>
        /// <remarks>
        /// Bastante más que el radio de visión del castillo: si la alarma solo sonara cuando
        /// ya le están pegando al castillo, defender llegaría tarde siempre. Y no tanto como
        /// para que un explorador de paso por el mapa haga volver al ejército entero.
        /// </remarks>
        const float radioDeAlarma = 22f;

        // -----------------------------------------------------------------
        // Ataque
        // -----------------------------------------------------------------

        /// <summary>
        /// El edificio enemigo más cercano que este bando haya descubierto.
        /// </summary>
        /// <remarks>
        /// El más cercano y no el castillo: ir directo al castillo pasando por delante de un
        /// cuartel y dos torres es como pierde el ejército entero sin llevarse nada. Y el
        /// castillo cae solo al final, porque cuando no quede otra cosa será el más cercano.
        /// </remarks>
        Edificio ObjetivoConocido()
        {
            var tropa = _tropa.Count > 0 ? _tropa[0].transform.position : Vector3.zero;

            Edificio mejor = null;
            float mejorDistancia = float.MaxValue;

            var casas = Edificio.Todos;

            for (int i = 0; i < casas.Count; i++)
            {
                var e = casas[i];
                if (e == null || e.faccion == Faccion) continue;
                if (!NieblaDeGuerra.Descubierto(Faccion, e)) continue;

                float d = e.DistanciaA(tropa);
                if (d >= mejorDistancia) continue;

                mejorDistancia = d;
                mejor = e;
            }

            return mejor;
        }

        /// <summary>
        /// Barre el mapa cuando no queda nada conocido a lo que atacar.
        /// </summary>
        /// <remarks>
        /// Cuatro esquinas y el centro, en bucle, cambiando de destino cada pocos segundos.
        /// No es exploracion inteligente y no pretende serlo: es la garantia minima de que
        /// <b>una partida termina</b>. Un ejercito que camina destapa niebla, y al destapar
        /// niebla encuentra lo que quedaba en pie.
        ///
        /// Lo que si hace falta que sea es barato: cinco puntos fijos y un reloj, nada de
        /// buscar la celda sin explorar mas cercana recorriendo cincuenta mil celdas.
        /// </remarks>
        void Explorar()
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            if (Time.time >= _proximaRonda)
            {
                _proximaRonda = Time.time + segundosPorRonda;
                _rincon = (_rincon + 1) % 5;
            }
            else if (_proximaRonda > 0f)
            {
                return;
            }
            else
            {
                _proximaRonda = Time.time + segundosPorRonda;
            }

            float ancho = mundo.Grilla.Ancho;
            float alto = mundo.Grilla.Alto;

            var destino = _rincon switch
            {
                0 => new Vector3(ancho * 0.2f, alto * 0.2f, 0f),
                1 => new Vector3(ancho * 0.8f, alto * 0.2f, 0f),
                2 => new Vector3(ancho * 0.8f, alto * 0.8f, 0f),
                3 => new Vector3(ancho * 0.2f, alto * 0.8f, 0f),
                _ => new Vector3(ancho * 0.5f, alto * 0.5f, 0f),
            };

            MandarA(destino);
        }

        int _rincon;
        float _proximaRonda;

        /// <summary>Segundos antes de probar en otra esquina del mapa.</summary>
        const float segundosPorRonda = 25f;

        void Atacar(Edificio presa)
        {
            Autoridad.Emitir(new OrdenAtacar { Faccion = Faccion, Estructura = presa }, _tropa);
        }

        void MandarA(Vector3 punto)
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null) return;

            // La orden de mover viaja en CELDAS y no en coordenadas de mundo: es la misma
            // orden que emite el clic derecho, y una orden que va a poder viajar por la red
            // algun dia lleva enteros, no decimales (ADR-01).
            var celda = mundo.Grilla.MundoACelda(punto);

            if (!mundo.Grilla.CeldaTransitableCercana(celda, 8, out celda)) return;

            Autoridad.Emitir(new OrdenMover { Faccion = Faccion, Destino = celda }, _tropa);
        }

        /// <summary>
        /// Las que esperan, esperan en casa.
        /// </summary>
        /// <remarks>
        /// No se les manda nada: quedarse quieto es no emitir ninguna orden. Emitir una orden
        /// de «ir a casa» tres veces por segundo cancelaría cualquier pelea en la que ya
        /// estén metidas, incluida la de una unidad defendiéndose sola.
        /// </remarks>
        void Quedarse()
        {
        }
    }
}
