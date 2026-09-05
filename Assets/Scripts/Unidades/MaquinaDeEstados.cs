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

            if (Recurso != TipoRecurso.Ninguno)
            {
                for (int i = 0; i < _tiras.Length; i++)
                    if (_tiras[i] != null && _tiras[i].estado == estado &&
                        _tiras[i].recurso == Recurso)
                        return _tiras[i];
            }

            for (int i = 0; i < _tiras.Length; i++)
                if (_tiras[i] != null && _tiras[i].estado == estado &&
                    _tiras[i].recurso == TipoRecurso.Ninguno)
                    return _tiras[i];

            for (int i = 0; i < _tiras.Length; i++)
                if (_tiras[i] != null && _tiras[i].estado == estado) return _tiras[i];

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
        /// Sigue sin ser combate: se resuelve <b>un solo golpe</b> por orden. No hay
        /// búsqueda automática de objetivo, ni cadencia, ni el golpeado responde. Eso es E06.
        /// </summary>
        public void OrdenarAtaque(Unidad objetivo)
        {
            if (Muerta || objetivo == null) return;

            // Repetir la orden sobre el mismo objetivo no hace nada: si no, machacar el
            // clic derecho reiniciaba la animación una y otra vez y no se veía un golpe
            // entero. Cambiar de objetivo sí interrumpe, que es lo que se espera.
            if (_objetivo == objetivo) return;

            _objetivo = objetivo;
            _persiguiendo = false;
            Resolver();
        }

        /// <summary>Suelta el objetivo. La llaman las órdenes de mover y de detener.</summary>
        public void Cancelar()
        {
            _objetivo = null;
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
        /// Enciende la vigilancia: la unidad busca enemigos cerca y ataca sola.
        ///
        /// El radio es corto y no se persigue fuera de él. Un «atacar automático» que
        /// arrastra a la unidad media pantalla detrás del primero que asoma es la forma
        /// más rápida de perder un ejército, y no es lo que hace Warcraft.
        /// </summary>
        public void Vigilar(bool activo)
        {
            _vigilando = activo;
            _proximaBusqueda = 0f;
        }

        public bool Vigilando => _vigilando;

        void Buscar()
        {
            if (!_vigilando || _objetivo != null || Time.time < _proximaBusqueda) return;
            if (_unidad.datos == null || _unidad.datos.dano <= 0) return;

            _proximaBusqueda = Time.time + intervaloVigilancia;

            RegistroDeUnidades.VecinasEnRadio(transform.position, radioVigilancia, _cerca);

            Unidad mejor = null;
            float mejorDistancia = radioVigilancia;

            for (int i = 0; i < _cerca.Count; i++)
            {
                var u = _cerca[i];
                if (u == null || !u.Viva || u.faccion == _unidad.faccion) continue;

                float d = Vector2.Distance(transform.position, u.transform.position);
                if (d > mejorDistancia) continue;

                mejorDistancia = d;
                mejor = u;
            }

            if (mejor != null) OrdenarAtaque(mejor);
        }

        /// <summary>Lanza solo la animación de golpe, sin objetivo ni efecto.</summary>
        public void Atacar()
        {
            if (Muerta) return;
            Aplicar(EstadoUnidad.Atacando, true);
        }

        Unidad _objetivo;
        bool _persiguiendo;

        void Resolver()
        {
            if (_objetivo == null || !_objetivo.Viva || _unidad.datos == null)
            {
                _objetivo = null;
                return;
            }

            float alcance = Mathf.Max(0.4f, _unidad.datos.alcance);
            float distancia = Vector2.Distance(transform.position, _objetivo.transform.position);

            if (distancia > alcance)
            {
                // Todavía lejos: caminar hacia él. Se pide la ruta una sola vez, no cada
                // frame; el buscador va en cola y pedirla sesenta veces por segundo la
                // saturaría para nada.
                if (_persiguiendo || _movimiento == null) return;

                _persiguiendo = true;

                var mundo = Mundo.MundoJuego.Actual;
                if (mundo == null || mundo.Grilla == null) { _objetivo = null; return; }

                var celda = mundo.Grilla.MundoACelda(_objetivo.transform.position);
                if (mundo.Grilla.CeldaTransitableCercana(celda, 8, out celda))
                    _movimiento.IrA(celda);
                else
                    _objetivo = null;

                return;
            }

            // Ya en alcance: orientarse, parar, golpear y resolver el efecto.
            Vector2 delta = _objetivo.transform.position - transform.position;
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

        bool _golpePendiente;

        /// <summary>
        /// Qué le pasa al objetivo. Daño negativo es curación — el monje es el único.
        ///
        /// Contra el muñeco de pruebas el efecto se invierte: el muñeco no se hiere y en
        /// cambio devuelve su propio daño al atacante. Es un poste de entrenamiento, y es
        /// lo que permite ver morir a nuestras propias unidades sin enemigos reales.
        /// </summary>
        void AplicarEfecto(Unidad objetivo)
        {
            var datos = _unidad.datos;
            if (datos == null || objetivo == null || !objetivo.Viva) { _objetivo = null; return; }

            // Sin fuego amigo. Nunca. Es una regla del juego, no una comprobacion de
            // seguridad: en Warcraft no existe y aqui tampoco.
            if (datos.dano > 0 && objetivo.faccion == _unidad.faccion) { _objetivo = null; return; }

            if (datos.dano < 0)
            {
                // Se cura por el valor efectivo, no por el de la ficha: un monje al que su
                // bando no alimenta cura menos, igual que un guerrero pega menos.
                objetivo.Curar(-_unidad.Dano);
                _listoParaCurar = Time.time + esperaCura;

                SoltarEfecto(_efectoCura, objetivo.transform.position, 14f, 1f);

                // Una sola cura por orden: con espera de por medio, insistir no tendría
                // sentido y la unidad se quedaría plantada.
                _objetivo = null;
                return;
            }

            // El arquero manda una flecha en vez de tocar al objetivo. Es cosmética: el
            // daño ya está resuelto, la flecha solo lo explica.
            if (_flecha != null && datos.alcance > 3f)
                LanzarFlecha(objetivo.transform.position);

            if (objetivo.datos != null && objetivo.datos.invulnerable)
            {
                _unidad.RecibirDano(Mathf.Max(0, objetivo.datos.dano));
                return;
            }

            objetivo.RecibirDano(_unidad.Dano);
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
