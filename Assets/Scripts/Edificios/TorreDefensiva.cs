using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Nucleo;
using TinyTactics.Unidades;

namespace TinyTactics.Edificios
{
    /// <summary>
    /// La guarnición de una torre: un arquero en la almena que dispara solo.
    ///
    /// Es un <b>edificio con guarnición</b>, no una unidad rara. Esa distinción es la que
    /// hace que salga casi gratis: el arte, el animador y el proyectil del arquero ya
    /// existen desde la semana 04, y lo único nuevo aquí es quién decide a quién disparar.
    /// </summary>
    /// <remarks>
    /// El arquero <b>no</b> es una <c>Unidad</c> de verdad, y no lo es a propósito. Si lo
    /// fuera contaría para el tope de población, se podría seleccionar, se podría mandar a
    /// caminar y habría que acordarse de matarlo aparte al caer la torre. Tendría además
    /// velocidad, hambre, rutas y empuje, todo a cero y todo estorbando. Lo que se necesita
    /// es un dibujo que apunte y una cuenta atrás.
    /// </remarks>
    [RequireComponent(typeof(Edificio))]
    [AddComponentMenu("Tiny Tactics/Torre defensiva")]
    public class TorreDefensiva : MonoBehaviour
    {
        [SerializeField] Sprite[] _reposo;
        [SerializeField] Sprite[] _disparo;
        [SerializeField] Sprite _flecha;

        [Tooltip("Cada cuánto mira alrededor buscando a quién disparar.")]
        [Range(0.1f, 1f)] public float intervaloVigilancia = 0.25f;

        Edificio _edificio;
        Transform _arquero;
        SpriteRenderer _pintor;
        AnimadorSprite _animador;

        float _proximoDisparo;
        float _proximaBusqueda;
        bool _disparando;

        static readonly List<Unidad> _cerca = new List<Unidad>(32);

        /// <summary>La llama el generador de la escena. Los sprites sí se serializan.</summary>
        public void Configurar(Sprite[] reposo, Sprite[] disparo, Sprite flecha)
        {
            _reposo = reposo;
            _disparo = disparo;
            _flecha = flecha;
        }

        void Awake() => _edificio = GetComponent<Edificio>();

        void Start() => Guarnecer();

        /// <summary>
        /// Planta el arquero en la almena.
        /// </summary>
        /// <remarks>
        /// Cuelga del propio edificio, así que se hunde, se apaga y se destruye con él sin
        /// una sola línea extra: cuando la torre cae, el arquero cae con ella. Eso es
        /// exactamente lo que se pedía, y sale de la jerarquía, no de código.
        /// </remarks>
        void Guarnecer()
        {
            if (_arquero != null) return;
            if (_reposo == null || _reposo.Length == 0) return;

            var datos = _edificio != null ? _edificio.datos : null;
            Vector2 punto = datos != null ? datos.puntoDisparo : new Vector2(0f, 1f);

            var go = new GameObject("Arquero");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(punto.x, punto.y, 0f);

            _pintor = go.AddComponent<SpriteRenderer>();
            _pintor.sprite = _reposo[0];

            // Por encima de la torre. El arquero está DELANTE de la almena, no detrás: si
            // compartiera orden con el muro, la mitad de los fotogramas quedaría tapada.
            var muro = GetComponent<SpriteRenderer>();
            _pintor.sortingOrder = (muro != null ? muro.sortingOrder : 0) + 2;

            _animador = go.AddComponent<AnimadorSprite>();
            _animador.dispersion = 0f;
            _animador.AlTerminar += AlTerminarDisparo;

            Reposar();
        }

        void OnDestroy()
        {
            if (_animador != null) _animador.AlTerminar -= AlTerminarDisparo;
        }

        void Reposar()
        {
            _disparando = false;
            if (_animador != null && _reposo != null && _reposo.Length > 0)
                _animador.Configurar(_reposo, 7f, 0, true);
        }

        void AlTerminarDisparo()
        {
            if (_disparando) Reposar();
        }

        void Update()
        {
            if (_arquero == null && _pintor != null) _arquero = _pintor.transform;
            if (_edificio == null || _pintor == null) return;

            // Una torre a medio construir no dispara. Sería regalar una defensa que todavía
            // no se ha pagado, y además el arquero se vería flotando sobre un andamio.
            if (!_edificio.Operativo) return;

            if (Time.time < _proximaBusqueda) return;
            _proximaBusqueda = Time.time + intervaloVigilancia;

            var presa = Presa();
            if (presa == null) return;

            Apuntar(presa.transform.position);

            if (Time.time < _proximoDisparo) return;

            var datos = _edificio.datos;
            _proximoDisparo = Time.time + (datos != null ? datos.cadencia : 1.6f);

            Disparar(presa);
        }

        /// <summary>
        /// A quién disparar: el enemigo vivo más cercano dentro del alcance.
        /// </summary>
        /// <remarks>
        /// Se pregunta a la grilla espacial, nunca a la lista entera de unidades. Con cinco
        /// bandos y varias torres cada uno, recorrer todas las unidades en cada torre y en
        /// cada vuelta es justo la forma de que el rendimiento se caiga sin que nadie sepa
        /// por qué.
        /// </remarks>
        Unidad Presa()
        {
            var datos = _edificio.datos;
            float alcance = datos != null ? datos.alcanceAtaque : 6.5f;

            Vector3 origen = _pintor.transform.position;
            RegistroDeUnidades.VecinasEnRadio(origen, alcance, _cerca);

            Unidad mejor = null;
            float mejorDistancia = alcance;

            for (int i = 0; i < _cerca.Count; i++)
            {
                var u = _cerca[i];
                if (u == null || !u.Viva || u.faccion == _edificio.faccion) continue;

                // La torre tampoco dispara a ciegas (ADR-20). Su radio de vision es mayor que
                // su alcance, asi que en la practica sigue disparando a todo lo que entra a
                // tiro; lo que ya no hace es acertarle a algo que su bando no ve.
                if (!Mundo.NieblaDeGuerra.Ve(_edificio.faccion, u.transform.position)) continue;

                // El muñeco de pruebas no cuenta: si contara, las torres se pasarían la
                // partida disparándole en vez de defender.
                if (u.datos != null && u.datos.invulnerable) continue;

                float d = Vector2.Distance(origen, u.transform.position);
                if (d > mejorDistancia) continue;

                mejorDistancia = d;
                mejor = u;
            }

            return mejor;
        }

        /// <summary>
        /// Orienta al arquero hacia su objetivo.
        /// </summary>
        /// <remarks>
        /// Solo se voltea, no hay cinco direcciones: el pack dibuja al arquero con una sola
        /// tira de disparo, al revés que al lancero, que sí trae las cinco. Comprobado en
        /// <c>Units/&lt;color&gt;/Archer</c>, donde solo hay <c>Idle</c>, <c>Run</c> y
        /// <c>Shoot</c>. Volteando se cubren los dos lados, que es todo lo que el arte da de sí.
        /// </remarks>
        void Apuntar(Vector3 objetivo)
        {
            if (_pintor == null) return;
            _pintor.flipX = objetivo.x < _pintor.transform.position.x;
        }

        void Disparar(Unidad presa)
        {
            var datos = _edificio.datos;

            if (_animador != null && _disparo != null && _disparo.Length > 0)
            {
                _disparando = true;
                _animador.Configurar(_disparo, 13f, 0, false);
            }

            LanzarFlecha(presa.transform.position);

            // El daño se resuelve ya, no al aterrizar la flecha. La flecha es cosmética
            // —igual que la del arquero a pie— y esa decisión es de la semana 08, cuando el
            // proyectil pase a impactar de verdad. Que las dos se comporten igual hoy es lo
            // que hará que el cambio de la semana que viene valga para las dos a la vez.
            // La torre dispara flechas, así que pasa por la misma tabla que un arquero: le
            // cuesta contra armadura pesada y le rinde contra las astas. Una torre exenta de
            // los contadores sería la única cosa del juego contra la que no hay respuesta
            // táctica, solo más números.
            int golpe = datos != null ? datos.danoAtaque : 20;

            float factor = TablaDeContadores.Factor(TipoAtaque.Flecha,
                                                    ((IObjetivo)presa).Armadura);

            presa.RecibirDano(Mathf.Max(1, Mathf.RoundToInt(golpe * factor)), null);
        }

        void LanzarFlecha(Vector3 destino)
        {
            if (_flecha == null || _pintor == null) return;

            var go = new GameObject("Flecha");
            go.transform.position = _pintor.transform.position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _flecha;
            sr.sortingOrder = _pintor.sortingOrder + 1;

            Vector3 delta = destino - go.transform.position;
            go.transform.rotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            go.AddComponent<Proyectil>().Configurar(destino, 16f);
        }
    }
}
