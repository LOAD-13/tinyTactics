using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Mundo;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Lo que el arranque le hace a un bando antes de empezar: su color, sus recursos y, si
    /// toca, un ejército de salida.
    /// </summary>
    public static class PreparacionDeBandos
    {
        static readonly List<Unidad> _suyas = new List<Unidad>();
        static readonly List<Edificio> _suyos = new List<Edificio>();

        /// <summary>
        /// Viste a un bando con su color de esta partida.
        /// </summary>
        /// <remarks>
        /// Lo que se construya o entrene durante la partida ya sale del color correcto,
        /// porque los catálogos buscan por color. Esto es para lo que <b>ya estaba</b> en el
        /// mapa al generarlo: el castillo y los pawns de salida, dibujados con el color del
        /// número de su base.
        ///
        /// A los edificios se les copian los dibujos del molde de su color, pieza a pieza por
        /// su ruta en la jerarquía; no se sustituyen, porque el castillo ocupa celdas y lo
        /// conocen el árbitro, la IA y la niebla. A las unidades sí se las sustituye por una
        /// copia del molde: cada una lleva su tabla de animaciones entera, y copiarla pieza a
        /// pieza sería reescribir la máquina de estados desde fuera.
        /// </remarks>
        public static void Vestir(int bando)
        {
            int color = Bandos.ColorDe(bando);
            if (color == bando) return;

            var edificios = CatalogoDeEdificios.Actual ?? Object.FindFirstObjectByType<CatalogoDeEdificios>();
            if (edificios != null)
            {
                _suyos.Clear();
                foreach (var e in Edificio.Todos)
                    if (e != null && e.faccion == bando && e.datos != null) _suyos.Add(e);

                foreach (var e in _suyos)
                {
                    var molde = edificios.PlantillaDeColor(e.datos, color, e.variante);
                    if (molde != null) CopiarDibujos(molde.transform, e.transform);
                }
            }

            var plantillas = CatalogoDePlantillas.Actual ?? Object.FindFirstObjectByType<CatalogoDePlantillas>();
            if (plantillas == null) return;

            _suyas.Clear();
            var registro = RegistroDeUnidades.Todas;
            for (int i = 0; i < registro.Count; i++)
                if (registro[i] != null && registro[i].faccion == bando && registro[i].datos != null)
                    _suyas.Add(registro[i]);

            foreach (var u in _suyas)
            {
                var molde = plantillas.ObtenerDeColor(u.datos.tipo, color);
                if (molde == null) continue;

                Sacar(molde, u.datos, bando, u.transform.position, u.transform.parent);
                Object.Destroy(u.gameObject);
            }
        }

        static void CopiarDibujos(Transform molde, Transform destino)
        {
            foreach (var sr in destino.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var par = Buscar(molde, Ruta(sr.transform, destino));
                var suyo = par != null ? par.GetComponent<SpriteRenderer>() : null;
                if (suyo != null) sr.sprite = suyo.sprite;
            }

            foreach (var an in destino.GetComponentsInChildren<AnimadorSprite>(true))
            {
                var par = Buscar(molde, Ruta(an.transform, destino));
                var suyo = par != null ? par.GetComponent<AnimadorSprite>() : null;
                if (suyo != null && suyo.frames != null) an.frames = suyo.frames;
            }
        }

        static string Ruta(Transform t, Transform raiz)
        {
            if (t == raiz) return "";

            var partes = new List<string>();
            for (var x = t; x != null && x != raiz; x = x.parent) partes.Insert(0, x.name);
            return string.Join("/", partes);
        }

        static Transform Buscar(Transform raiz, string ruta) =>
            string.IsNullOrEmpty(ruta) ? raiz : raiz.Find(ruta);

        /// <summary>Saca una unidad de una plantilla, ya con su bando, y la enciende.</summary>
        public static Unidad Sacar(GameObject plantilla, DatosUnidad datos, int bando,
                                   Vector3 posicion, Transform padre)
        {
            var copia = Object.Instantiate(plantilla, posicion, Quaternion.identity, padre);
            copia.name = $"{datos.tipo}_{bando}";

            var u = copia.GetComponent<Unidad>();
            if (u != null) u.Configurar(datos, bando);

            copia.SetActive(true);
            return u;
        }

        /// <summary>Recursos de regalo para un bando.</summary>
        public static void Regalar(int bando, int oro, int madera, int carne)
        {
            var eco = Economia.Actual ?? Object.FindFirstObjectByType<Economia>();
            if (eco == null) return;

            if (oro > 0) eco.Depositar(bando, TipoRecurso.Oro, oro);
            if (madera > 0) eco.Depositar(bando, TipoRecurso.Madera, madera);
            if (carne > 0) eco.Depositar(bando, TipoRecurso.Carne, carne);
        }

        /// <summary>
        /// Un ejército de salida junto al castillo de un bando.
        /// </summary>
        /// <remarks>
        /// Lo usan el fondo del menú —para que haya batalla en cuanto se abre el juego y no
        /// a los cuatro minutos— y la regla «empezar con ejército» del sandbox. La mezcla es
        /// la de una oleada media del plan: más guerreros que arqueros, un lancero y un monje.
        /// </remarks>
        public static void Ejercito(int bando, int cuantos)
        {
            var plantillas = CatalogoDePlantillas.Actual ?? Object.FindFirstObjectByType<CatalogoDePlantillas>();
            var mundo = MundoJuego.Actual ?? Object.FindFirstObjectByType<MundoJuego>();
            if (plantillas == null || mundo == null || mundo.Grilla == null) return;

            Edificio castillo = null;
            foreach (var e in Edificio.Todos)
                if (e != null && e.faccion == bando && e.centroDeEntrega) { castillo = e; break; }
            if (castillo == null) return;

            TipoUnidad[] mezcla =
            {
                TipoUnidad.Guerrero, TipoUnidad.Arquero, TipoUnidad.Guerrero, TipoUnidad.Lancero,
                TipoUnidad.Guerrero, TipoUnidad.Arquero, TipoUnidad.Monje, TipoUnidad.Lancero,
            };

            var centro = mundo.Grilla.MundoACelda(castillo.PuntoDeEntrega);

            for (int i = 0; i < cuantos; i++)
            {
                var tipo = mezcla[i % mezcla.Length];
                var molde = plantillas.Obtener(tipo, bando);
                var datos = molde != null ? molde.GetComponent<Unidad>()?.datos : null;
                if (molde == null || datos == null) continue;

                // En corro alrededor del castillo, cada una en una celda libre.
                float angulo = i * Mathf.PI * 2f / Mathf.Max(1, cuantos);
                var deseada = centro + new Vector2Int(Mathf.RoundToInt(Mathf.Cos(angulo) * 5f),
                                                      Mathf.RoundToInt(Mathf.Sin(angulo) * 5f));

                if (!mundo.Grilla.CeldaTransitableCercana(deseada, 6, out var celda)) continue;

                Sacar(molde, datos, bando, mundo.Grilla.CeldaAMundo(celda), castillo.transform.parent);
            }
        }
    }
}
