using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Construye un mapa guardado sobre la escena de juego, durante el arranque.
    /// </summary>
    /// <remarks>
    /// La escena de juego trae Tres Coronas ya montado por el generador. Para jugar otro mapa
    /// no se carga otra escena: se <b>vacía</b> el contenido de esta —terreno, recursos,
    /// bases— conservando todas las plantillas, y se <b>construye</b> el mapa del archivo con
    /// esas mismas plantillas (ADR-28).
    ///
    /// El vaciado se hace en el <c>Awake</c> del arranque, que corre antes que el de nadie, y
    /// apagando antes de destruir: un objeto apagado no llega a despertar. Si no, el castillo
    /// de Tres Coronas se registraría en el árbitro medio fotograma antes de desaparecer.
    /// </remarks>
    public static class ConstructorEnPartida
    {
        const string RaizContenido = "Contenido";
        const string RaizMapa = "Mapa";

        /// <summary>Quita el mapa de la escena dejando las plantillas a salvo.</summary>
        public static void Vaciar()
        {
            var contenido = GameObject.Find(RaizContenido);
            if (contenido != null)
            {
                var moldes = new GameObject("Moldes de bando").transform;
                var hijos = new List<Transform>();
                foreach (Transform h in contenido.transform) hijos.Add(h);

                foreach (var h in hijos)
                {
                    if (h.name == "PlantillasRecurso") continue;

                    // De cada base se salvan sus plantillas de unidad y de edificio: el
                    // cuartel y la obra las buscan ahí durante toda la partida.
                    if (h.name == "Bases")
                        foreach (Transform grupo in h)
                            foreach (var t in Hijos(grupo))
                                if (t.name.StartsWith("Plantillas_")) t.SetParent(moldes, true);

                    Quitar(h.gameObject);
                }
            }

            foreach (var suelto in new[] { "Patito", "MunecoDePruebas" })
            {
                var go = GameObject.Find(suelto);
                if (go != null) Quitar(go);
            }
        }

        static List<Transform> Hijos(Transform t)
        {
            var l = new List<Transform>();
            foreach (Transform h in t) l.Add(h);
            return l;
        }

        static void Quitar(GameObject go)
        {
            go.SetActive(false);
            Object.Destroy(go);
        }

        /// <summary>
        /// Pinta el terreno y coloca recursos, criaderos y bases. Corre en el <c>Start</c>
        /// del arranque, cuando la grilla ya existe.
        /// </summary>
        public static void Construir(MapaGuardado m, PaletaDeTerreno paleta, bool conBases)
        {
            var raiz = GameObject.Find(RaizMapa) ?? new GameObject(RaizMapa);
            var pintor = raiz.GetComponent<PintorDeTerreno>() ?? raiz.AddComponent<PintorDeTerreno>();
            pintor.paleta = paleta;
            pintor.Pintar(m);

            Sembrar(m);
            Criaderos(m);
            if (conBases) { Bases(m); EdificiosDeSalida(m); }

            var camara = Object.FindFirstObjectByType<CamaraRTS>();
            if (camara != null)
            {
                camara.ConfigurarLimites(Vector2.zero, new Vector2(m.ancho, m.alto));
                if (!conBases || m.bases.Count == 0) camara.CentrarEn(new Vector2(m.ancho * 0.5f, m.alto * 0.5f));
            }

            FiltroDeMapa.Aplicar(m.ambiente);
        }

        static void Sembrar(MapaGuardado m)
        {
            var catalogo = CatalogoDeRecursos.Actual ?? Object.FindFirstObjectByType<CatalogoDeRecursos>();
            if (catalogo == null) return;

            foreach (var o in m.objetos)
                catalogo.Sembrar(o.tipo, o.variante, new Vector2Int(o.x, o.y));
        }

        static void Criaderos(MapaGuardado m)
        {
            var criadero = Object.FindFirstObjectByType<CriaderoOvejas>();
            if (criadero == null) return;

            var catalogo = CatalogoDeRecursos.Actual ?? Object.FindFirstObjectByType<CatalogoDeRecursos>();
            var oveja = catalogo != null ? catalogo.Obtener(SemillaMapa.Oveja, 0) : null;

            var rebano = new GameObject("Ovejas").transform;
            criadero.Configurar(oveja, rebano, new List<Vector2Int>(m.criaderos));
            criadero.intervalo = m.segundosPorOveja;
            criadero.mundoAlto = m.alto;

            // Dos ovejas por criadero para empezar: un criadero vacío al principio de la
            // partida no se ve, y el jugador no sabe que está ahí.
            if (catalogo == null) return;
            foreach (var c in m.criaderos)
                for (int k = 0; k < 2; k++)
                    catalogo.Sembrar(SemillaMapa.Oveja, k, c + new Vector2Int(k * 2 - 1, 0));
        }

        /// <summary>Torres, casas y demás con las que empieza cada base.</summary>
        static void EdificiosDeSalida(MapaGuardado m)
        {
            var edificios = CatalogoDeEdificios.Actual ?? Object.FindFirstObjectByType<CatalogoDeEdificios>();
            if (edificios == null || m.edificios.Count == 0) return;

            var padre = new GameObject("Edificios de salida").transform;

            foreach (var e in m.edificios)
            {
                int dueno = m.BaseMasCercana(e.x, e.y);
                if (dueno < 0) continue;

                var c = ArranqueDePartida.Configuracion;
                if (c != null && c.De(dueno) == Ocupante.Cerrada) continue;

                var ficha = edificios.Obtener(e.tipo);
                var molde = ficha != null ? edificios.PlantillaDe(ficha, dueno, e.variante) : null;
                if (molde == null) continue;

                var go = Object.Instantiate(molde, padre);
                go.name = $"{e.tipo}_{e.x}_{e.y}";
                var edificio = go.GetComponent<Edificio>();
                if (edificio != null) edificio.Colocar(ficha, dueno, ficha.CeldasDesde(new Vector2Int(e.x, e.y)), e.variante);
                go.SetActive(true);
            }
        }

        /// <summary>Un castillo y dos pawns por base, del color que lleva cada bando.</summary>
        static void Bases(MapaGuardado m)
        {
            var edificios = CatalogoDeEdificios.Actual ?? Object.FindFirstObjectByType<CatalogoDeEdificios>();
            var plantillas = CatalogoDePlantillas.Actual ?? Object.FindFirstObjectByType<CatalogoDePlantillas>();
            if (edificios == null) return;

            var ficha = edificios.Obtener(TipoEdificio.Castillo);
            if (ficha == null) return;

            var padre = new GameObject("Bases").transform;

            for (int i = 0; i < m.bases.Count; i++)
            {
                var c = ArranqueDePartida.Configuracion;
                if (c != null && c.De(i) == Ocupante.Cerrada) continue;

                var celda = m.bases[i];
                var molde = edificios.PlantillaDe(ficha, i);
                if (molde == null) continue;

                var grupo = new GameObject($"Bando{i + 1}").transform;
                grupo.SetParent(padre, false);

                var castillo = Object.Instantiate(molde, grupo);
                castillo.name = "Castillo";
                var edificio = castillo.GetComponent<Edificio>();
                if (edificio != null) edificio.Colocar(ficha, i, ficha.CeldasDesde(celda));
                castillo.SetActive(true);

                if (plantillas == null) continue;
                var pawn = plantillas.Obtener(TipoUnidad.Pawn, i);
                var datos = pawn != null ? pawn.GetComponent<Unidad>()?.datos : null;
                if (datos == null) continue;

                for (int p = 0; p < 2; p++)
                {
                    float dx = (p - 0.5f) * 1.7f;
                    PreparacionDeBandos.Sacar(pawn, datos, i,
                                              new Vector3(celda.x + 0.5f + dx, celda.y - 2.6f, 0f), grupo);
                }
            }
        }
    }
}
