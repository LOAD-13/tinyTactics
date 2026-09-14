using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Movimiento;
using TinyTactics.Nucleo;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Decide en qué está la unidad y le dice al animador qué dibujar.
    ///
    /// Antes esto vivía repartido: <c>MovimientoUnidad</c> alternaba entre reposo y
    /// caminar por su cuenta. Con cuatro estados eso deja de escalar — nadie sabría quién
    /// manda cuando una unidad muere mientras camina. Aquí hay un solo dueño del estado y
    /// una sola función que resuelve las transiciones.
    ///
    /// Sigue el ADR-03: ni rastro del componente <c>Animator</c>. Un índice de estado, una
    /// tira de sprites y el aviso de fin de animación bastan.
    /// </summary>
    [RequireComponent(typeof(Unidad))]
    [RequireComponent(typeof(AnimadorSprite))]
    [AddComponentMenu("Tiny Tactics/Máquina de estados")]
    public class MaquinaDeEstados : MonoBehaviour
    {
        /// <summary>
        /// Una animación ya resuelta a sprites, lista para el animador.
        ///
        /// Es una clase serializable y no un diccionario porque Unity no serializa
        /// diccionarios, y los sprites TIENEN que sobrevivir al guardado de la escena:
        /// el generador los asigna al construirla y si no se serializan llegan nulos al
        /// darle a Play. Ya pasó con la animación de caminar en la semana 03.
        /// </summary>
        [System.Serializable]
        public class Tira
        {
            public EstadoUnidad estado;
            public DireccionAtaque direccion = DireccionAtaque.Ninguna;
            public TipoRecurso recurso = TipoRecurso.Ninguno;

            /// <summary>Tira de obra. Gana a la del recurso mientras el pawn construye.</summary>
            public bool martillo;

            public Sprite[] frames;
            public float fps = 8f;
            public bool enBucle = true;
        }

        [Header("Animaciones")]
        [SerializeField] Tira[] _tiras = new Tira[0];

        [Header("Muerte")]
        [Tooltip("Segundos que tarda la unidad en desvanecerse.")]
        [Range(0.1f, 2f)] public float duracionMuerte = 0.45f;

        [Tooltip("Nube de polvo al caer. El pack no trae animación de muerte; esto la suple.")]
        [SerializeField] Sprite[] _polvo;

        [Header("Efectos")]
        [Tooltip("Destello verde sobre el curado.")]
        [SerializeField] Sprite[] _efectoCura;

        [Tooltip("Proyectil del arquero.")]
        [SerializeField] Sprite _flecha;

        [Tooltip("Segundos entre curaciones. Obliga a elegir el momento en vez de curar sin parar.")]
        public float esperaCura = 15f;

        public EstadoUnidad Estado { get; private set; } = EstadoUnidad.Reposo;
        public bool Muerta => Estado == EstadoUnidad.Muriendo;

        /// <summary>
        /// Recurso con el que la unidad trabaja o que lleva encima.
        ///
        /// No es un dato del recolector sino de la máquina, porque cambia el <b>dibujo de
        /// todos los estados a la vez</b>: un pawn cargado de madera se dibuja distinto
        /// parado y andando. Si viviera fuera, el recolector tendría que reasignar tiras y
        /// dejaría de ser cierto que la máquina es la única que toca al animador (ADR-11).
        /// </summary>
        public TipoRecurso Recurso { get; private set; } = TipoRecurso.Ninguno;

        Unidad _unidad;
        AnimadorSprite _animador;
        SpriteRenderer _sprite;
        MovimientoUnidad _movimiento;

        bool _animacionTerminada;
        float _relojMuerte;
        Color _colorInicial;

        // -----------------------------------------------------------------

        /// <summary>La llama el generador de la escena con las tiras ya cargadas.</summary>
        public void Configurar(Tira[] tiras, Sprite[] polvo, Sprite[] efectoCura, Sprite flecha)
        {
            _tiras = tiras;
            _polvo = polvo;
            _efectoCura = efectoCura;
            _flecha = flecha;
        }

        /// <summary>Segundos que faltan para poder volver a curar. Lo lee el panel.</summary>
        public float EsperaRestante => Mathf.Max(0f, _listoParaCurar - Time.time);

        float _listoParaCurar;

        void Awake()
        {
            _unidad = GetComponent<Unidad>();
            _animador = GetComponent<AnimadorSprite>();
            _sprite = GetComponent<SpriteRenderer>();
            _movimiento = GetComponent<MovimientoUnidad>();

            if (_sprite != null) _colorInicial = _sprite.color;

            _animador.AlTerminar += AlTerminarAnimacion;
            _animador.AlDarVuelta += AlDarVuelta;
            Aplicar(EstadoUnidad.Reposo, true);

            // Un efecto que falta no rompe nada y por eso pasa desapercibido. Se avisa una
            // vez por unidad: es la diferencia entre «no se ve la flecha» y saber por qué.
            if (_unidad.datos == null) return;

            if (_unidad.datos.dano < 0 && (_efectoCura == null || _efectoCura.Length == 0))
                Debug.LogWarning($"[Tiny Tactics] {name}: cura sin destello. Regenera la escena.", this);

            if (_unidad.datos.alcance > 3f && _unidad.datos.dano > 0 && _flecha == null)
                Debug.LogWarning($"[Tiny Tactics] {name}: dispara sin flecha. Regenera la escena.", this);
        }

        void OnDestroy()
        {
            if (_animador != null)
            {
                _animador.AlTerminar -= AlTerminarAnimacion;
                _animador.AlDarVuelta -= AlDarVuelta;
            }
        }

        void AlTerminarAnimacion() => _animacionTerminada = true;

        void AlDarVuelta()
        {
            // Las DOS condiciones. Con solo el estado, el contador seguía sumando después
            // de cancelar: Cancelar apagaba la bandera pero el estado tardaba un fotograma
            // en cambiar, y en ese hueco cabía un golpe regalado.
            if (_trabajando && Estado == EstadoUnidad.Trabajando) _vueltasDeTrabajo++;
        }

        /// <summary>
        /// Pasadas completas de la animación de trabajo desde que empezó.
        ///
        /// Se pone a cero en cuanto la unidad cambia de estado, y ese detalle es justo lo
        /// que cierra un agujero: con un temporizador, interrumpir a un pawn y volver a
        /// mandarlo conservaba el rato ya invertido, así que picar-parar-picar sacaba el
        /// recurso en una fracción del tiempo. Contando golpes, un hachazo a medias no
        /// cuenta y no hay forma de acumular progreso a trocitos.
        /// </summary>
        public int VueltasDeTrabajo => _vueltasDeTrabajo;

        int _vueltasDeTrabajo;

        /// <summary>
        /// Pone el contador a cero. Lo llama quien encarga el trabajo al empezar cada tanda.
        /// </summary>
        /// <remarks>
        /// El contador ya se borra solo en cada cambio de estado, así que esto es
        /// redundante <i>si</i> todas las transiciones son correctas. Existe justamente
        /// porque esa condición es difícil de garantizar de un vistazo: con este cerrojo, el
        /// número de golpes que hacen falta no depende de haber trazado bien las seis rutas
        /// por las que se puede interrumpir a un pawn.
        /// </remarks>
        public void ReiniciarGolpes() => _vueltasDeTrabajo = 0;

        // -----------------------------------------------------------------

        void Update()
        {
            if (Estado == EstadoUnidad.Muriendo)
            {
                Desvanecer();
                return;
            }

            // El golpe se cobra cuando la animación termina, no cuando se ordena. Eso es
            // lo que hace que el ritmo sea el del dibujo y no el de los clics del jugador.
            if (_golpePendiente && _animacionTerminada)
            {
                _golpePendiente = false;
                AplicarEfecto(_objetivo);

                // El efecto puede haber matado a esta misma unidad — el poste devuelve
                // daño. Si pasó, aquí se acaba el frame.
                if (Estado == EstadoUnidad.Muriendo) return;
            }

            // Con objetivo vivo se vuelve a golpear en cuanto la tira anterior acaba: una
            // orden mantiene a la unidad pegando sola, sin tener que repetir el clic.
            Buscar();

            bool ocupada = Estado == EstadoUnidad.Atacando && !_animacionTerminada;
            if (_objetivo != null && !ocupada) Resolver();

            Aplicar(Decidir(), false);
        }

        /// <summary>
        /// Qué estado toca ahora. Una sola función, leída de arriba abajo por prioridad.
        /// </summary>
        EstadoUnidad Decidir()
        {
            if (Estado == EstadoUnidad.Muriendo) return EstadoUnidad.Muriendo;

            // Un golpe no se interrumpe a mitad: se aguanta hasta que la tira acaba. Si no,
            // bastaría con mover la unidad para cancelar el ataque y no se vería nunca
            // completo.
            if (Estado == EstadoUnidad.Atacando && !_animacionTerminada)
                return EstadoUnidad.Atacando;

            if (_movimiento != null && _movimiento.EnMovimiento)
                return EstadoUnidad.Moviendo;

            // Trabajar va por debajo de moverse: el pawn que camina hacia el árbol está
            // caminando, aunque el recolector ya haya encendido el trabajo.
            if (_trabajando) return EstadoUnidad.Trabajando;

            return EstadoUnidad.Reposo;
        }

        void Aplicar(EstadoUnidad nuevo, bool forzar)
        {
            // Morir es terminal y hay que blindarlo aquí, no solo en quien llama.
            //
            // Este era EL fallo: al golpear al poste, el daño de vuelta mataba a la unidad
            // a mitad de Update, y tres líneas más abajo el mismo Update llamaba a
            // Aplicar(Decidir()) — que devolvía Reposo y la resucitaba. Quedaba de pie,
            // con cero de vida, sin poder seleccionarse y sin desvanecerse nunca.
            if (Estado == EstadoUnidad.Muriendo && nuevo != EstadoUnidad.Muriendo) return;

            if (!forzar && nuevo == Estado) return;

            Estado = nuevo;
            _animacionTerminada = false;
            _vueltasDeTrabajo = 0;

            var tira = Buscar(nuevo) ?? Buscar(EstadoUnidad.Reposo);
            if (tira == null || tira.frames == null || tira.frames.Length == 0) return;

            // Desfase inicial solo en las animaciones que se repiten: un grupo de unidades
            // paradas al unísono se ve artificial, pero un golpe tiene que empezar por su
            // primer frame o se ve cortado.
            //
            // Trabajar queda fuera del desfase aunque se repita: sus vueltas se CUENTAN, y
            // arrancar por la mitad haría que el primer hachazo valiera medio golpe.
            bool desfasar = tira.enBucle && nuevo != EstadoUnidad.Trabajando;
            int inicio = desfasar ? Random.Range(0, tira.frames.Length) : 0;

            _animador.Configurar(tira.frames, tira.fps, inicio, tira.enBucle);
        }

        /// <summary>
        /// La tira que toca dibujar, resolviendo en este orden: dirección, carga, y por
        /// último el estado a secas. Cada escalón es una preferencia, no un requisito: si
        /// una unidad no tiene versión cargada de un estado, se dibuja la normal.
        /// </summary>
        Tira Buscar(EstadoUnidad estado)
        {
            if (_tiras == null) return null;

            // Con objetivo y tiras direccionales, gana la que apunta hacia él.
            if (estado == EstadoUnidad.Atacando && _direccionActual != DireccionAtaque.Ninguna)
            {
                for (int i = 0; i < _tiras.Length; i++)
                    if (_tiras[i] != null && _tiras[i].estado == estado &&
                        _tiras[i].direccion == _direccionActual)
                        return _tiras[i];
            }

            // El martillo va PRIMERO. Un pawn que va a construir sigue llevando encima el
            // saco de lo que estuviera recolectando, así que si mandara la carga se le
            // vería levantar el muro a hachazos.
            if (ConMartillo)
            {
                for (int i = 0; i < _tiras.Length; i++)
                    if (_tiras[i] != null && _tiras[i].estado == estado && _tiras[i].martillo)
                        return _tiras[i];
            }

            if (Recurso != TipoRecurso.Ninguno)
            {
                for (int i = 0; i < _tiras.Length; i++)
                    if (_tiras[i] != null && _tiras[i].estado == estado &&
                        _tiras[i].recurso == Recurso && !_tiras[i].martillo)
                        return _tiras[i];
            }

            for (int i = 0; i < _tiras.Length; i++)
                if (_tiras[i] != null && _tiras[i].estado == estado &&
                    _tiras[i].recurso == TipoRecurso.Ninguno && !_tiras[i].martillo)
                    return _tiras[i];

            for (int i = 0; i < _tiras.Length; i++)
                if (_tiras[i] != null && _tiras[i].estado == estado && !_tiras[i].martillo)
                    return _tiras[i];

            return null;
        }

        // -----------------------------------------------------------------
        // Recolección
        // -----------------------------------------------------------------

        bool _trabajando;

        /// <summary>
        /// Enciende o apaga la animación de trabajo. La llama el recolector, que es quien
        /// decide; aquí solo se dibuja.
        /// </summary>
        public void Trabajar(bool activo)
        {
            if (Muerta || _trabajando == activo) return;

            _trabajando = activo;
            Aplicar(Decidir(), true);
        }

        /// <summary>
        /// Cambia lo que la unidad lleva o trabaja y repinta.
        ///
        /// Se fuerza el repintado aunque el estado no cambie: pasar de andar con las manos
        /// vacías a andar con un saco es el mismo estado y un dibujo distinto, así que sin
        /// el <c>forzar</c> el pawn volvería del árbol sin la madera a cuestas.
        /// </summary>
        public void Cargar(TipoRecurso recurso)
        {
            if (Muerta || Recurso == recurso) return;

            Recurso = recurso;
            Aplicar(Estado, true);
        }

        /// <summary>True mientras la unidad lleva el martillo en la mano.</summary>
        public bool ConMartillo { get; private set; }

        /// <summary>
        /// Saca o guarda el martillo.
        /// </summary>
        /// <remarks>
        /// Es una bandera aparte y no otro valor de <see cref="Recurso"/> porque las dos
        /// cosas conviven: un pawn puede ir a construir con el saco de madera todavía
        /// encima, y al acabar la obra tiene que volver a vérsele el saco sin que nadie
        /// recuerde cuál era.
        /// </remarks>
        public void EmpunarMartillo(bool activo)
        {
            if (Muerta || ConMartillo == activo) return;

            ConMartillo = activo;
            Aplicar(Estado, true);
        }

        DireccionAtaque _direccionActual = DireccionAtaque.Ninguna;

        /// <summary>
        /// A cuál de las cinco tiras del pack corresponde el ángulo hacia el objetivo.
        ///
        /// Se mide sobre el valor absoluto de la componente horizontal: el pack solo dibuja
        /// el lado derecho, y el izquierdo sale de voltear el sprite. Cinco tiras más
        /// espejo dan las ocho orientaciones sin arte adicional.
        /// </summary>
        static DireccionAtaque DireccionHacia(Vector2 delta)
        {
            float grados = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;

            if (grados > 67.5f) return DireccionAtaque.Arriba;
            if (grados > 22.5f) return DireccionAtaque.ArribaDerecha;
            if (grados > -22.5f) return DireccionAtaque.Derecha;
            if (grados > -67.5f) return DireccionAtaque.AbajoDerecha;

            return DireccionAtaque.Abajo;
        }

        // -----------------------------------------------------------------
        // Órdenes externas
        // -----------------------------------------------------------------

        /// <summary>
        /// Encarga un golpe (o una cura) sobre un objetivo.
        ///
        /// Si está fuera de alcance, la unidad se acerca primero y golpea al llegar. Eso es
        /// lo mínimo para que los alcances signifiquen algo: sin acercarse, un guerrero con
        /// 0,8 de alcance nunca podría atacar nada y el arquero pegaría desde la otra punta.
        ///
        /// Esta es la puerta del <b>jugador</b>: lo que entra por aquí no lleva correa. Si
        /// el clic derecho también se rindiera a media persecución, la unidad se daría la
        /// vuelta sola y el jugador no tendría forma de entender por qué.
        /// </summary>
        public void OrdenarAtaque(IObjetivo objetivo)
        {
            if (Muerta || !Existe(objetivo)) return;

            // Repetir la orden sobre el mismo objetivo no hace nada: si no, machacar el
            // clic derecho reiniciaba la animación una y otra vez y no se veía un golpe
            // entero. Cambiar de objetivo sí interrumpe, que es lo que se espera.
            //
            // Se comprueba también el origen: si la unidad ya se había enganchado sola a
            // ese mismo enemigo, el clic del jugador tiene que ascender el enganche a orden
            // para que deje de estar atado por la correa.
            if (_objetivo == objetivo && _porOrden) return;

            _objetivo = objetivo;
            _porOrden = true;
            _persiguiendo = false;
            Resolver();
        }

        /// <summary>
        /// Engancha un objetivo por iniciativa propia: vigilancia, postura o un golpe
        /// recibido. A diferencia de <see cref="OrdenarAtaque"/>, esto <b>sí</b> lleva
        /// correa, y el ancla se clava aquí — donde estaba la unidad cuando decidió pelear.
        /// </summary>
        void Enganchar(IObjetivo objetivo)
        {
            if (Muerta || !Existe(objetivo) || _objetivo == objetivo) return;

            _objetivo = objetivo;
            _porOrden = false;
            _persiguiendo = false;
            _ancla = transform.position;
            Resolver();
        }

        /// <summary>Suelta el objetivo. La llaman las órdenes de mover y de detener.</summary>
        public void Cancelar()
        {
            _objetivo = null;
            _porOrden = false;
            _persiguiendo = false;
            _golpePendiente = false;
            _vigilando = false;

            if (!_trabajando) return;

            _trabajando = false;

            // Salir de «trabajando» AHORA y no en el siguiente Update. Mientras el estado
            // siga siendo ese, el contador de golpes sigue vivo, y un fotograma de margen
            // es todo lo que hace falta para acumular progreso a base de parar y reanudar.
            Aplicar(Decidir(), false);
        }

        // -----------------------------------------------------------------
        // Ataque automático
        // -----------------------------------------------------------------

        [Header("Ataque automático")]
        [Tooltip("Radio en el que busca enemigos por su cuenta. Corto a propósito: " +
                 "no debe cruzar el mapa a por alguien que ha visto de lejos.")]
        public float radioVigilancia = 5.5f;

        [Tooltip("Cada cuánto mira alrededor. No hace falta cada frame y sale caro.")]
        public float intervaloVigilancia = 0.3f;

        bool _vigilando;
        float _proximaBusqueda;

        static readonly System.Collections.Generic.List<Unidad> _cerca =
            new System.Collections.Generic.List<Unidad>(32);

        /// <summary>
        /// Enciende el <b>ataque al avanzar</b>: la unidad va a donde se le dijo, pero
        /// pegándole a lo que se cruce.
        ///
        /// Es una bandera de una orden concreta, no una forma de ser. Lo segundo es la
        /// postura, y las dos conviven: una unidad quieta a la que se le manda un ataque al
        /// avanzar ataca al avanzar, porque se lo acaban de mandar.
        /// </summary>
        public void Vigilar(bool activo)
        {
            _vigilando = activo;
            _proximaBusqueda = 0f;
        }

        public bool Vigilando => _vigilando;

        Postura? _posturaElegida;

        /// <summary>
        /// Postura efectiva: la que haya elegido el jugador y, si no ha tocado nada, la que
        /// trae la ficha.
        /// </summary>
        /// <remarks>
        /// Se resuelve al leerla en vez de copiarse en el <c>Awake</c> a propósito. Las
        /// unidades que salen de un edificio reciben sus datos <i>después</i> de despertar,
        /// así que una copia temprana se quedaría con la postura de la ficha vacía y todos
        /// los pawns nacerían agresivos.
        /// </remarks>
        public Postura PosturaActual =>
            _posturaElegida ?? (_unidad != null && _unidad.datos != null
                ? _unidad.datos.postura
                : Postura.Agresiva);

        public void CambiarPostura(Postura nueva)
        {
            _posturaElegida = nueva;

            // Pasar a quieta tiene efecto inmediato sobre lo que se estuviera haciendo por
            // iniciativa propia. Lo que mandó el jugador se respeta: si ordenó un ataque,
            // cambiar la postura no es motivo para desobedecer.
            if (nueva == Postura.Quieta && !_porOrden) Cancelar();
        }

        void Buscar()
        {
            if (_objetivo != null || Time.time < _proximaBusqueda) return;
            if (_unidad.datos == null || _unidad.datos.dano <= 0) return;

            Postura postura = PosturaActual;
            bool saleABuscar = _vigilando || postura == Postura.Agresiva;

            // La quieta no mira siquiera, salvo que se le haya ordenado atacar al avanzar.
            if (!saleABuscar && postura != Postura.Defensiva) return;

            _proximaBusqueda = Time.time + intervaloVigilancia;

            // Una unidad defensiva no sale a buscar: solo ve lo que se le pone a tiro. El
            // radio corto no es solo más barato, es lo que significa la postura — plantarse
            // a distancia de una unidad defensiva y no entrar en su alcance es una jugada
            // legítima, y con el radio largo dejaría de serlo.
            float radio = saleABuscar
                ? radioVigilancia
                : Mathf.Max(0.4f, _unidad.datos.alcance);

            RegistroDeUnidades.VecinasEnRadio(transform.position, radio, _cerca);

            Unidad mejor = null;
            float mejorDistancia = radio;

            for (int i = 0; i < _cerca.Count; i++)
            {
                var u = _cerca[i];
                if (u == null || !u.Viva || u.faccion == _unidad.faccion) continue;

                float d = Vector2.Distance(transform.position, u.transform.position);
                if (d > mejorDistancia) continue;

                mejorDistancia = d;
                mejor = u;
            }

            if (mejor != null) Enganchar(mejor);
        }

        /// <summary>
        /// Alguien acaba de herir a esta unidad. Decide si responde, si huye o si ni se
        /// inmuta.
        /// </summary>
        public void Agredida(Unidad agresor)
        {
            if (Muerta || agresor == null || !agresor.Viva) return;
            if (agresor == _unidad || agresor.faccion == _unidad.faccion) return;

            // Quien ya está peleando no cambia de objetivo por un golpe nuevo. Sin esto,
            // dos unidades que se hieren a la vez se turnan el objetivo cada golpe y
            // ninguna llega a rematar a nadie.
            if (_objetivo != null) return;

            // Una orden del jugador manda por encima del reflejo. Una unidad que abandona
            // su camino porque le rozó una flecha es una unidad que nunca llega, y es
            // exactamente para eso para lo que existe el ataque al avanzar: si el jugador
            // quería que peleara de camino, lo dice al dar la orden.
            if (_movimiento != null && _movimiento.EnMovimiento) return;

            // El pawn no pelea: se va. Su trabajo vale mucho más que los pocos puntos de
            // daño que haría, y perderlo por defenderse cuesta la mitad de la economía.
            if (_unidad.datos != null && _unidad.datos.tipo == TipoUnidad.Pawn)
            {
                var recolector = GetComponent<RecolectorPawn>();
                if (recolector != null) recolector.Refugiarse();
                return;
            }

            // Sin daño positivo no hay respuesta posible, y aquí la guarda no es cosmética:
            // el monje tiene daño NEGATIVO, así que engancharlo a su agresor lo mandaría a
            // curar al enemigo que lo está matando. Que huya es de la semana 08; que no
            // cure al rival, de ahora mismo.
            if (_unidad.datos == null || _unidad.datos.dano <= 0) return;

            if (PosturaActual == Postura.Quieta) return;

            Enganchar(agresor);
        }

        /// <summary>Lanza solo la animación de golpe, sin objetivo ni efecto.</summary>
        public void Atacar()
        {
            if (Muerta) return;
            Aplicar(EstadoUnidad.Atacando, true);
        }

        IObjetivo _objetivo;
        bool _persiguiendo;

        /// <summary>
        /// ¿Sigue existiendo y en pie lo que estamos golpeando?
        /// </summary>
        /// <remarks>
        /// <b>No basta con <c>_objetivo != null</c>.</b> Unity finge que un objeto destruido
        /// es nulo, pero ese truco vive en <c>UnityEngine.Object</c> y se pierde en cuanto la
        /// referencia se guarda como interfaz: la comparación pasa a ser la de C# y un objeto
        /// ya destruido sigue dando «no nulo». Es el mismo fallo que la semana pasada dejó al
        /// pawn plantado tras matar una oveja, y aquí habría vuelto disfrazado.
        ///
        /// Por eso se vuelve a pasar por <c>MonoBehaviour</c>: ahí la comparación con null es
        /// otra vez la de Unity, que es la que dice la verdad.
        /// </remarks>
        static bool Existe(IObjetivo objetivo)
        {
            var cuerpo = objetivo as MonoBehaviour;
            return cuerpo != null && objetivo.Vivo;
        }

        /// <summary>True si el objetivo lo puso el jugador. Los enganches propios llevan correa.</summary>
        bool _porOrden;

        /// <summary>Dónde estaba la unidad cuando decidió pelear por su cuenta.</summary>
        Vector3 _ancla;

        void Resolver()
        {
            if (!Existe(_objetivo) || _unidad.datos == null)
            {
                _objetivo = null;
                return;
            }

            float alcance = Mathf.Max(0.4f, _unidad.datos.alcance);
            float distancia = _objetivo.DistanciaDesde(transform.position);

            if (distancia > alcance)
            {
                // La defensiva aguanta el sitio incluso contra quien la está hiriendo. Es
                // la postura de las unidades que guardan algo: perseguir sería abandonarlo.
                if (!_porOrden && PosturaActual == Postura.Defensiva)
                {
                    _objetivo = null;
                    return;
                }

                // La correa. Sin ella, un solo arquero enemigo arrastra media base al otro
                // lado del mapa: cada unidad que entra en su radio engancha a la siguiente y
                // la cadena no termina nunca. Se mide contra el ancla, no contra la casa, y
                // se comprueba ANTES del cerrojo de persecución — si no, la primera ruta ya
                // pedida haría el viaje entero antes de que nadie mirase la distancia.
                if (!_porOrden && _unidad.datos.correa > 0f &&
                    Vector2.Distance(transform.position, _ancla) > _unidad.datos.correa)
                {
                    Volver();
                    return;
                }

                // Todavía lejos: caminar hacia él. Se pide la ruta una sola vez, no cada
                // frame; el buscador va en cola y pedirla sesenta veces por segundo la
                // saturaría para nada.
                if (_persiguiendo || _movimiento == null) return;

                _persiguiendo = true;

                var mundo = Mundo.MundoJuego.Actual;
                if (mundo == null || mundo.Grilla == null) { _objetivo = null; return; }

                var celda = mundo.Grilla.MundoACelda(_objetivo.PuntoDeAtaqueDesde(transform.position));
                if (mundo.Grilla.CeldaTransitableCercana(celda, 8, out celda))
                    _movimiento.IrA(celda);
                else
                    _objetivo = null;

                return;
            }

            // Ya en alcance: orientarse, parar, golpear y resolver el efecto.
            Vector2 delta = _objetivo.Posicion - transform.position;
            _direccionActual = DireccionHacia(delta);

            if (_sprite != null) _sprite.flipX = delta.x < 0f;

            if (_movimiento != null) _movimiento.Detener();

            // Una cura tiene espera; un golpe no. Sin la espera, el monje anula el
            // desgaste por completo y no hay decisión que tomar.
            if (_unidad.datos.dano < 0 && Time.time < _listoParaCurar) return;

            Aplicar(EstadoUnidad.Atacando, true);
            _golpePendiente = true;
            _persiguiendo = false;
        }

        /// <summary>
        /// Se acabó la correa: suelta al enemigo y vuelve a donde empezó.
        ///
        /// Volver importa tanto como dejar de perseguir. Una unidad que abandona pero se
        /// queda plantada donde estaba deja un hueco en la formación, y el jugador no se
        /// entera hasta que le entran por ahí.
        /// </summary>
        void Volver()
        {
            _objetivo = null;
            _persiguiendo = false;

            // También se apaga el ataque al avanzar: esa orden se dio para llegar a un
            // sitio, y la unidad acaba de demostrar que no lo está consiguiendo.
            _vigilando = false;

            var mundo = Mundo.MundoJuego.Actual;
            if (_movimiento == null || mundo == null || mundo.Grilla == null) return;

            var celda = mundo.Grilla.MundoACelda(_ancla);
            if (mundo.Grilla.CeldaTransitableCercana(celda, 8, out celda))
                _movimiento.IrA(celda);
        }

        bool _golpePendiente;

        /// <summary>
        /// Qué le pasa al objetivo. Daño negativo es curación — el monje es el único.
        ///
        /// Contra el muñeco de pruebas el efecto se invierte: el muñeco no se hiere y en
        /// cambio devuelve su propio daño al atacante. Es un poste de entrenamiento, y es
        /// lo que permite ver morir a nuestras propias unidades sin enemigos reales.
        /// </summary>
        void AplicarEfecto(IObjetivo objetivo)
        {
            var datos = _unidad.datos;
            if (datos == null || !Existe(objetivo)) { _objetivo = null; return; }

            // Sin fuego amigo. Nunca. Es una regla del juego, no una comprobacion de
            // seguridad: en Warcraft no existe y aqui tampoco.
            if (datos.dano > 0 && objetivo.Faccion == _unidad.faccion) { _objetivo = null; return; }

            // Curar es cosa de unidades. Un edificio no se repara —eso sería otra mecánica,
            // con su coste y su decisión— así que el monje simplemente no tiene nada que
            // hacer aquí y se suelta en vez de quedarse dando pases al muro.
            Unidad cuerpo = objetivo as Unidad;

            if (datos.dano < 0)
            {
                if (cuerpo == null) { _objetivo = null; return; }

                // Se cura por el valor efectivo, no por el de la ficha: un monje al que su
                // bando no alimenta cura menos, igual que un guerrero pega menos.
                cuerpo.Curar(-_unidad.Dano);
                _listoParaCurar = Time.time + esperaCura;

                SoltarEfecto(_efectoCura, objetivo.Posicion, 14f, 1f);

                // Una sola cura por orden: con espera de por medio, insistir no tendría
                // sentido y la unidad se quedaría plantada.
                _objetivo = null;
                return;
            }

            // El arquero manda una flecha en vez de tocar al objetivo. Es cosmética: el
            // daño ya está resuelto, la flecha solo lo explica.
            if (_flecha != null && datos.alcance > 3f)
                LanzarFlecha(objetivo.Posicion);

            // El muñeco de pruebas devuelve su propio daño al atacante. Es lo único del
            // juego que hace eso, y solo puede ser una unidad.
            if (cuerpo != null && cuerpo.datos != null && cuerpo.datos.invulnerable)
            {
                _unidad.RecibirDano(Mathf.Max(0, cuerpo.datos.dano), cuerpo);
                return;
            }

            objetivo.RecibirDano(_unidad.Dano, _unidad);
        }

        /// <summary>
        /// Empieza a morir: suelta el polvo, corta el movimiento y arranca el desvanecido.
        ///
        /// El pack no trae animación de muerte para ninguna unidad —lo comprobé buscando
        /// «death», «die» y «dead» en todo el paquete—, así que se resuelve sin arte
        /// nuevo: la unidad se apaga a gris mientras se vuelve transparente y deja una
        /// nube de polvo. Se lee como intencionado y no como una unidad que se esfuma.
        /// </summary>
        public void Morir()
        {
            if (Muerta) return;

            Estado = EstadoUnidad.Muriendo;
            _relojMuerte = 0f;

            if (_movimiento != null)
            {
                _movimiento.Detener();
                _movimiento.enabled = false;
            }

            // El marcador de selección y la barra de vida cuelgan de la unidad; se apagan
            // para que no queden flotando sobre un cadáver que se desvanece.
            var seleccion = transform.Find("Seleccion");
            if (seleccion != null) seleccion.gameObject.SetActive(false);

            var vida = transform.Find("Vida");
            if (vida != null) vida.gameObject.SetActive(false);

            SoltarPolvo();

            var tira = Buscar(EstadoUnidad.Muriendo);
            if (tira != null && tira.frames != null && tira.frames.Length > 0)
                _animador.Configurar(tira.frames, tira.fps, 0, false);
        }

        void Desvanecer()
        {
            _relojMuerte += Time.deltaTime;

            if (_sprite != null)
            {
                float t = Mathf.Clamp01(_relojMuerte / duracionMuerte);

                // Primero pierde el color y luego la opacidad: apagarse a gris antes de
                // desaparecer se lee como «ha caído», no como «se ha teletransportado».
                var color = Color.Lerp(_colorInicial, new Color(0.35f, 0.35f, 0.40f), t);
                color.a = _colorInicial.a * (1f - t * t);
                _sprite.color = color;
            }

            if (_relojMuerte >= duracionMuerte) Destroy(gameObject);
        }

        void SoltarPolvo()
        {
            if (_polvo == null || _polvo.Length == 0)
            {
                Debug.LogWarning(
                    $"[Tiny Tactics] {name} muere sin nube de polvo: no le llegaron los " +
                    "sprites. Regenera la escena para que el constructor los asigne.", this);
                return;
            }

            // Delante de la unidad, no detrás: la unidad se está desvaneciendo pero durante
            // los primeros fotogramas sigue siendo opaca y tapaba la nube por completo.
            SoltarEfecto(_polvo, transform.position + new Vector3(0f, -0.3f, 0f), 14f, 1.6f);
        }

        /// <summary>
        /// Suelta una animación suelta en el mundo y la retira al terminar.
        ///
        /// No cuelga de la unidad a propósito: la unidad puede destruirse durante la
        /// animación y se llevaría el efecto por delante a media reproducción.
        /// </summary>
        void SoltarEfecto(Sprite[] frames, Vector3 posicion, float fps, float escala)
        {
            if (frames == null || frames.Length == 0) return;

            var go = new GameObject("Efecto");
            go.transform.position = posicion;
            go.transform.localScale = new Vector3(escala, escala, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = (_sprite != null ? _sprite.sortingOrder : 0) + 50;

            var anim = go.AddComponent<AnimadorSprite>();
            anim.dispersion = 0f;
            anim.Configurar(frames, fps, 0, false);

            Destroy(go, frames.Length / fps + 0.15f);
        }

        /// <summary>Flecha que viaja del arquero al objetivo. Solo es decoración.</summary>
        void LanzarFlecha(Vector3 destino)
        {
            var go = new GameObject("Flecha");
            go.transform.position = transform.position + new Vector3(0f, 0.2f, 0f);

            Vector3 delta = destino - go.transform.position;
            go.transform.right = delta.normalized;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _flecha;
            sr.sortingOrder = (_sprite != null ? _sprite.sortingOrder : 0) + 50;

            go.AddComponent<Proyectil>().Configurar(destino, 14f);
        }
    }
}
