using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// El manto de nubes que tapa lo que nunca se ha visto.
    /// </summary>
    /// <remarks>
    /// <b>La niebla se dibuja con el arte del pack, no con un degradado.</b> Tiny Swords trae
    /// ocho nubes pintadas a mano —las mismas que ya pasan por el mapa como ambiente— y son
    /// las que tapan lo inexplorado. Un difuminado programado sobre pixel art se delata
    /// solo: sus bordes no caen en la misma rejilla que los tiles y su gris no sale de la
    /// paleta del pack. Usando las nubes de verdad, la frontera de la niebla la dibujó el
    /// mismo artista que dibujó el bosque de al lado.
    ///
    /// <b>Las nubes no se instancian por celda, se reparten por rejilla y se reciclan.</b> El
    /// mapa mide 224×224, así que taparlo entero serían miles de sprites, casi todos fuera de
    /// la pantalla. Solo existen las que caen en el encuadre de la cámara más un margen, y al
    /// salir vuelven a la reserva. Cada hueco de la rejilla tiene su propia nube, escala y
    /// desfase sacados de sus coordenadas, así que una nube es siempre la misma nube por
    /// mucho que la cámara vaya y venga.
    ///
    /// <b>Se balancean en el sitio; no van a la deriva.</b> Una nube que viaja acaba
    /// cruzando la frontera y tapando terreno que el jugador sí ha explorado, y hacerla
    /// reaparecer donde le toca deja un salto visible. El vaivén no tiene ese problema y da
    /// la misma sensación de estar vivo. La deriva de verdad ya la hacen las nubes de
    /// ambiente sobre el mapa descubierto (<see cref="DerivaNube"/>).
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Manto de nubes")]
    public class MantoDeNubes : MonoBehaviour
    {
        [Tooltip("APAGADO por defecto desde la semana 09.\n\n" +
                 "El manto se construyo, se probo y se apago jugando: tapaba tanto que la " +
                 "partida empezaba sin poder leer el mapa, y eso agobia en vez de intrigar. " +
                 "No se borra porque la idea sigue siendo buena para un modo de exploracion " +
                 "de verdad, y porque apagar algo que funciona es una decision reversible " +
                 "mientras que borrarlo no. Se enciende desde el panel de partida libre.")]
        public bool activo;

        [Tooltip("Las ocho nubes del pack. Las asigna el generador de la escena.")]
        public Sprite[] nubes = new Sprite[0];

        [Header("Reparto")]
        [Tooltip("Separacion horizontal entre nubes, en tiles. Las del pack miden unos 8 de " +
                 "ancho, asi que con menos de eso se solapan y no quedan claros.")]
        [Range(2f, 12f)] public float pasoX = 5.8f;

        [Tooltip("Separacion vertical. Mucho menor que la horizontal porque las nubes del " +
                 "pack son anchas y bajas: unos 2,5 tiles de alto.")]
        [Range(1f, 8f)] public float pasoY = 2.5f;

        [Tooltip("Desorden del reparto, en tiles. Sin esto se ve la rejilla.")]
        [Range(0f, 4f)] public float desorden = 1.6f;

        [Tooltip("Tiles de margen fuera del encuadre. Evita que una nube aparezca de golpe " +
                 "en el borde de la pantalla al mover la camara.")]
        [Range(0f, 20f)] public float margen = 7f;

        [Tooltip("Tope de nubes vivas a la vez. Es un seguro de rendimiento, no un ajuste " +
                 "estetico: con el encuadre mas amplio salen unas 250.")]
        [Range(20, 400)] public int tope = 260;

        [Header("Aspecto")]
        [Tooltip("Nubes mas grandes desde que el fondo de lo no explorado es gris y no negro: " +
                 "con el fondo oscuro daba igual lo que asomara entre nube y nube, y con el " +
                 "gris lo que asoma es terreno reconocible. Se tapa con tamano y no con mas " +
                 "nubes porque una nube grande cuesta lo mismo de dibujar que una pequena.")]
        [Range(0.5f, 3f)] public float escalaMinima = 1.35f;
        [Range(0.5f, 3f)] public float escalaMaxima = 2.25f;

        [Tooltip("Opacidad de una nube sobre terreno nunca visto.")]
        [Range(0.2f, 1f)] public float opacidad = 0.98f;

        [Tooltip("Lo que tarda una nube en desvanecerse cuando se explora lo que tapaba. " +
                 "Es lo que hace suave la frontera: no la desdibuja un filtro, se apartan " +
                 "las nubes.")]
        [Range(0.05f, 3f)] public float fundido = 0.45f;

        [Header("Vaiven")]
        [Range(0f, 1.5f)] public float balanceo = 0.35f;
        [Range(1f, 20f)] public float periodo = 7f;

        [Tooltip("Orden de dibujo. Por encima del lienzo de niebla, por debajo del ciclo " +
                 "del dia: de noche, las nubes tambien se apagan.")]
        public int ordenDeDibujo = 6100;

        class Nube
        {
            public Transform sitio;
            public SpriteRenderer dibujo;
            public float alfa;
            public float objetivo;
            public int visto;
            public Vector3 origen;
            public float desfase;
        }

        readonly Dictionary<long, Nube> _vivas = new Dictionary<long, Nube>();
        readonly Stack<Nube> _reserva = new Stack<Nube>();
        readonly List<long> _aRetirar = new List<long>();

        Camera _camara;
        Transform _corral;
        int _fotograma;
        int _ancho, _alto;

        void Start()
        {
            var mundo = MundoJuego.Actual;
            if (mundo == null || mundo.Grilla == null || nubes == null || nubes.Length == 0)
            {
                enabled = false;
                return;
            }

            // El componente sigue vivo aunque arranque apagado: el interruptor del panel
            // tiene que poder encenderlo en mitad de la partida, y un componente
            // deshabilitado no escucha a nadie.

            _ancho = mundo.Grilla.Ancho;
            _alto = mundo.Grilla.Alto;

            _corral = new GameObject("Nubes de niebla").transform;
            _corral.SetParent(transform, false);
        }

        void LateUpdate()
        {
            var niebla = NieblaDeGuerra.Actual;
            if (!activo || niebla == null || niebla.ignorarNiebla)
            {
                if (_vivas.Count > 0) RecogerTodo();
                return;
            }

            if (_camara == null) _camara = Camera.main;
            if (_camara == null) return;

            _fotograma++;

            Repartir(niebla);
            Retirar();
            Mover();
        }

        void Repartir(NieblaDeGuerra niebla)
        {
            float mitadAlto = _camara.orthographicSize + margen;
            float mitadAncho = mitadAlto * _camara.aspect + margen;
            Vector3 ojo = _camara.transform.position;

            int desdeX = Mathf.FloorToInt((ojo.x - mitadAncho) / pasoX);
            int hastaX = Mathf.CeilToInt((ojo.x + mitadAncho) / pasoX);
            int desdeY = Mathf.FloorToInt((ojo.y - mitadAlto) / pasoY);
            int hastaY = Mathf.CeilToInt((ojo.y + mitadAlto) / pasoY);

            for (int iy = desdeY; iy <= hastaY; iy++)
            {
                for (int ix = desdeX; ix <= hastaX; ix++)
                {
                    float rx = Aleatorio(ix, iy, 1);
                    float ry = Aleatorio(ix, iy, 2);

                    float x = ix * pasoX + (rx - 0.5f) * 2f * desorden;
                    float y = iy * pasoY + (ry - 0.5f) * 2f * desorden;

                    // Fuera del mapa no hay nada que tapar, y una nube colgando del borde
                    // delata dónde acaba el terreno.
                    if (x < -1f || y < -1f || x > _ancho + 1f || y > _alto + 1f) continue;

                    long clave = Clave(ix, iy);
                    bool tapa = niebla.EstadoParaJugador(new Vector3(x, y, 0f)) == EstadoVisible.Oculto;

                    if (_vivas.TryGetValue(clave, out var nube))
                    {
                        nube.visto = _fotograma;
                        nube.objetivo = tapa ? opacidad : 0f;
                        continue;
                    }

                    if (!tapa || _vivas.Count >= tope) continue;

                    nube = Tomar();
                    nube.visto = _fotograma;
                    nube.objetivo = opacidad;
                    nube.alfa = 0f;
                    nube.origen = new Vector3(x, y, 0f);
                    nube.desfase = Aleatorio(ix, iy, 3) * periodo;

                    int cual = Mathf.Clamp((int)(Aleatorio(ix, iy, 4) * nubes.Length),
                                           0, nubes.Length - 1);
                    nube.dibujo.sprite = nubes[cual];

                    float escala = Mathf.Lerp(escalaMinima, escalaMaxima, Aleatorio(ix, iy, 5));
                    nube.sitio.localScale = new Vector3(escala, escala, 1f);
                    nube.sitio.position = nube.origen;

                    // Medio reparto de las nubes sale volteado: con ocho dibujos y doscientas
                    // nubes en pantalla, el ojo empieza a reconocerlas. Voltear duplica el
                    // repertorio sin pedirle un dibujo mas al pack.
                    nube.dibujo.flipX = Aleatorio(ix, iy, 6) > 0.5f;

                    _vivas[clave] = nube;
                }
            }
        }

        void Retirar()
        {
            _aRetirar.Clear();

            foreach (var par in _vivas)
            {
                var nube = par.Value;

                // La que no ha pasado por el reparto de este fotograma se ha quedado fuera
                // del encuadre: se apaga igual que las exploradas, y asi tampoco desaparece
                // de golpe si la camara vuelve a tiempo.
                if (nube.visto != _fotograma) nube.objetivo = 0f;

                if (nube.objetivo <= 0f && nube.alfa <= 0.01f) _aRetirar.Add(par.Key);
            }

            for (int i = 0; i < _aRetirar.Count; i++)
            {
                var nube = _vivas[_aRetirar[i]];
                _vivas.Remove(_aRetirar[i]);
                Guardar(nube);
            }
        }

        void Mover()
        {
            float paso = Time.unscaledDeltaTime / Mathf.Max(0.02f, fundido);
            float reloj = Time.unscaledTime;

            foreach (var par in _vivas)
            {
                var nube = par.Value;

                nube.alfa = Mathf.MoveTowards(nube.alfa, nube.objetivo, paso);

                var color = nube.dibujo.color;
                color.a = nube.alfa;
                nube.dibujo.color = color;

                if (balanceo <= 0f) continue;

                float t = (reloj + nube.desfase) * (Mathf.PI * 2f / Mathf.Max(1f, periodo));
                nube.sitio.position = nube.origen + new Vector3(Mathf.Sin(t) * balanceo,
                                                               Mathf.Cos(t * 0.6f) * balanceo * 0.4f,
                                                               0f);
            }
        }

        Nube Tomar()
        {
            if (_reserva.Count > 0)
            {
                var guardada = _reserva.Pop();
                guardada.sitio.gameObject.SetActive(true);
                return guardada;
            }

            var go = new GameObject("NubeDeNiebla");
            go.transform.SetParent(_corral, false);

            var dibujo = go.AddComponent<SpriteRenderer>();

            // Todas con el MISMO orden de dibujo, y no uno por nube: son un manto plano y
            // les da igual quien queda delante, mientras que ordenes distintos impiden que
            // Unity agrupe los dibujados.
            dibujo.sortingOrder = ordenDeDibujo;
            dibujo.color = new Color(1f, 1f, 1f, 0f);

            return new Nube { sitio = go.transform, dibujo = dibujo };
        }

        void Guardar(Nube nube)
        {
            nube.sitio.gameObject.SetActive(false);
            _reserva.Push(nube);
        }

        void RecogerTodo()
        {
            foreach (var par in _vivas) Guardar(par.Value);
            _vivas.Clear();
        }

        static long Clave(int x, int y) => ((long)x << 32) ^ (uint)y;

        /// <summary>
        /// Número estable entre 0 y 1 para un hueco de la rejilla.
        /// </summary>
        /// <remarks>
        /// No se usa <c>Random</c>: la nube de un hueco tiene que salir idéntica cada vez que
        /// la cámara vuelve a pasar por ahí. Con un generador de secuencia, la misma nube
        /// cambiaría de dibujo y de tamaño al volver, y el mapa parecería otro.
        /// </remarks>
        static float Aleatorio(int x, int y, int sal)
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(sal * 83492791);
            h ^= h >> 13;
            h *= 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777215f;
        }
    }
}
