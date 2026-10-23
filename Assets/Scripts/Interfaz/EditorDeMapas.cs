using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Edificios;
using TinyTactics.Entrada;
using TinyTactics.Mundo;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El editor de mapas: pinceles sobre un <see cref="MapaGuardado"/>, con respuesta inmediata.
    /// </summary>
    /// <remarks>
    /// <b>El mapa que se edita es el dato, no la escena.</b> Cada pincelada cambia el
    /// <see cref="MapaGuardado"/> y repinta solo la zona tocada; lo que se ve es siempre el
    /// resultado de construir ese dato. Por eso lo que se dibuja aquí se juega exactamente
    /// igual: no hay una versión «del editor» y otra «del juego».
    ///
    /// La interfaz sigue la mesa de guerra del menú (semana 12): una barra de madera arriba
    /// con lo que afecta al archivo, y un muelle abajo con las herramientas, agrupadas por
    /// categoría y enseñadas con su propio dibujo. Una herramienta es lo que ves que vas a
    /// poner, no un nombre que hay que leer.
    /// </remarks>
    [AddComponentMenu("Tiny Tactics/Editor de mapas")]
    public class EditorDeMapas : MonoBehaviour
    {
        public TemaInterfaz tema;
        public PaletaDeTerreno paleta;
        public Sprite iconoBorrar, iconoSubir, iconoBajar;

        [Tooltip("La mesa de madera del pack: lo que se ve alrededor del mapa al alejarse.")]
        public Sprite mesa;

        GameObject _mesa;

        enum Categoria { Terreno, Relieve, Naturaleza, Ovejas, Bases, Borrar }
        enum Accion { Tierra, Agua, Nivel, EscaleraDer, EscaleraIzq, Objeto, Criadero, Base, Edificio, Borrar }

        struct Herramienta
        {
            public string nombre;
            public Sprite icono;
            public Accion accion;
            public byte color;
            public SemillaMapa semilla;
            public TipoEdificio edificio;
        }

        int _indiceHerramienta;
        readonly List<Image> _dibujos = new List<Image>();

        MapaGuardado _mapa;
        PintorDeTerreno _pintor;
        Camera _camara;
        Font _fuente;

        Categoria _categoria = Categoria.Terreno;
        Herramienta _herramienta;
        int _tamano = 3, _variante;
        static readonly int[] Tamanos = { 1, 2, 3, 5, 8, 12 };

        readonly Dictionary<Vector2Int, GameObject> _objetos = new Dictionary<Vector2Int, GameObject>();
        readonly Dictionary<Vector2Int, GameObject> _marcas = new Dictionary<Vector2Int, GameObject>();
        readonly Stack<string> _deshacer = new Stack<string>();
        bool _trazando;
        Vector2Int _ultimaCelda = new Vector2Int(int.MinValue, 0);

        // Interfaz
        RectTransform _raiz, _rejilla, _ambiente, _dialogo;
        Text _titulo, _info, _aviso, _valorTamano, _valorVariante, _nombreHerramienta;
        InputField _campoNombre;
        readonly List<(Button boton, Categoria cat)> _pestanas = new List<(Button, Categoria)>();
        readonly List<Button> _botonesHerramienta = new List<Button>();
        SpriteRenderer _silueta;
        LineRenderer _marco;
        float _avisoHasta;

        public static EditorDeMapas Actual { get; private set; }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        // -----------------------------------------------------------------
        // Abrir
        // -----------------------------------------------------------------

        public void Abrir(MapaGuardado mapa)
        {
            _mapa = mapa;
            _guardado = mapa.AJson();
            _camara = Camera.main;
            _pintor = PintorDeTerreno.Actual ?? FindFirstObjectByType<PintorDeTerreno>();
            _fuente = tema != null && tema.titular != null ? tema.titular : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (_raiz == null) ConstruirInterfaz();
            PrepararCamara();
            ReconstruirObjetos();
            PrepararCursor();

            ElegirCategoria(Categoria.Terreno);
            PintarCabecera();
        }

        int Margen => Mathf.Max(_mapa.ancho, _mapa.alto) / 3;

        /// <summary>
        /// En el editor la cámara se aleja hasta ver el mapa entero y un buen trozo de mar
        /// alrededor. Ese mar se pinta de verdad: nunca se ve el vacío detrás del mapa.
        /// </summary>
        void PrepararCamara()
        {
            MontarMesa();

            var camara = FindFirstObjectByType<CamaraRTS>();
            if (camara == null) return;

            camara.ConfigurarLimites(new Vector2(-Margen, -Margen), new Vector2(_mapa.ancho + Margen, _mapa.alto + Margen));
            camara.zoomMaximo = 400f;
            camara.pasoZoom = Mathf.Max(camara.pasoZoom, 4f);
        }

        /// <summary>
        /// El mapa sobre la mesa de guerra.
        /// </summary>
        /// <remarks>
        /// Un mapa es cuadrado y una pantalla no: al alejarse sobra espacio a los lados. En vez
        /// de rellenarlo con mar o con negro, se ve lo que hay de verdad cuando uno dibuja un
        /// mapa: la mesa del comandante, de madera, con el mapa enmarcado encima. Es la misma
        /// madera de los menús, así que el editor es una pantalla más de la mesa de guerra.
        /// </remarks>
        void MontarMesa()
        {
            if (_mesa != null) Destroy(_mesa);
            _mesa = new GameObject("Mesa del editor");

            float w = _mapa.ancho, h = _mapa.alto;
            var centro = new Vector3(w * 0.5f, h * 0.5f, 0f);

            if (mesa != null)
            {
                // Solo las tablas del centro de la pieza, repetidas: las esquinas de metal
                // repetidas parecerían remaches por todas partes.
                var r = mesa.rect;
                float b = r.width * 0.14f;
                var tablas = Sprite.Create(mesa.texture, new Rect(r.x + b, r.y + b, r.width - 2 * b, r.height - 2 * b),
                                           new Vector2(0.5f, 0.5f), 12f, 0, SpriteMeshType.FullRect);

                var tablero = new GameObject("Tablero").AddComponent<SpriteRenderer>();
                tablero.transform.SetParent(_mesa.transform, false);
                tablero.transform.position = centro;
                tablero.sprite = tablas;
                tablero.drawMode = SpriteDrawMode.Tiled;
                tablero.size = new Vector2(w + Margen * 2f + 60f, h + Margen * 2f + 60f);
                tablero.color = new Color(0.82f, 0.78f, 0.74f);
                tablero.sortingOrder = -80;
            }

            var madera = tema != null ? tema.panelFondo : null;
            if (madera != null)
            {
                var marco = new GameObject("Marco").AddComponent<SpriteRenderer>();
                marco.transform.SetParent(_mesa.transform, false);
                marco.transform.position = centro;
                marco.sprite = KitInterfaz.ConBorde(madera, madera.border == Vector4.zero ? new Vector4(48, 48, 48, 48) : madera.border);
                marco.drawMode = SpriteDrawMode.Sliced;
                marco.size = new Vector2(w + 6f, h + 6f);
                marco.sortingOrder = -60;
            }
        }

        // -----------------------------------------------------------------
        // Entrada
        // -----------------------------------------------------------------

        void Update()
        {
            if (_mapa == null) return;

            var raton = Mouse.current;
            var teclado = Keyboard.current;
            if (raton == null) return;

            // Con la pregunta en pantalla el mapa no se toca: el velo ya se come los clics,
            // pero no el teclado.
            if (_dialogo != null && _dialogo.gameObject.activeSelf) return;

            if (teclado != null && teclado.ctrlKey.isPressed && teclado.zKey.wasPressedThisFrame) Deshacer();

            bool sobreInterfaz = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // La rueda cambia de variante sobre los paneles o con Mayús; sobre el mapa, sin
            // Mayús, es el zoom de siempre.
            float rueda = raton.scroll.ReadValue().y;
            bool mayus = teclado != null && teclado.shiftKey.isPressed;
            if (Mathf.Abs(rueda) > 0.01f && (sobreInterfaz || mayus) && TieneVariantes)
                CambiarVariante(rueda > 0f ? 1 : -1);
            Vector2Int celda = CeldaBajoRaton(raton.position.ReadValue());
            MoverCursor(celda, sobreInterfaz);

            if (_avisoHasta > 0f && Time.unscaledTime > _avisoHasta) { _aviso.text = ""; _avisoHasta = 0f; }

            if (raton.leftButton.wasPressedThisFrame && !sobreInterfaz && _mapa.Dentro(celda.x, celda.y))
            {
                _deshacer.Push(_mapa.AJson());
                if (_deshacer.Count > 40) RecortarDeshacer();
                _trazando = true;
                _ultimaCelda = new Vector2Int(int.MinValue, 0);
            }

            if (_trazando && raton.leftButton.isPressed && celda != _ultimaCelda && _mapa.Dentro(celda.x, celda.y))
            {
                _ultimaCelda = celda;
                Aplicar(celda);
            }

            if (raton.leftButton.wasReleasedThisFrame && _trazando)
            {
                _trazando = false;
                PintarCabecera();
            }
        }

        void RecortarDeshacer()
        {
            var lista = new List<string>(_deshacer);
            lista.RemoveAt(lista.Count - 1);
            _deshacer.Clear();
            for (int i = lista.Count - 1; i >= 0; i--) _deshacer.Push(lista[i]);
        }

        Vector2Int CeldaBajoRaton(Vector2 pantalla)
        {
            if (_camara == null) return new Vector2Int(-1, -1);
            var w = _camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, -_camara.transform.position.z));
            return new Vector2Int(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y));
        }

        RectInt Zona(Vector2Int c)
        {
            int t = UsaTamano ? _tamano : 1;
            return new RectInt(c.x - (t - 1) / 2, c.y - (t - 1) / 2, t, t);
        }

        bool UsaTamano => _herramienta.accion == Accion.Tierra || _herramienta.accion == Accion.Agua ||
                          _herramienta.accion == Accion.Nivel || _herramienta.accion == Accion.Borrar;

        bool TieneVariantes => _herramienta.accion == Accion.Objeto ||
                               (_herramienta.accion == Accion.Edificio && Fachadas(_herramienta.edificio) > 1);

        // -----------------------------------------------------------------
        // Pinceles
        // -----------------------------------------------------------------

        void Aplicar(Vector2Int c)
        {
            var zona = Zona(c);

            switch (_herramienta.accion)
            {
                case Accion.Tierra:
                case Accion.Agua:
                    foreach (var p in zona.allPositionsWithin)
                    {
                        if (!_mapa.Dentro(p.x, p.y)) continue;
                        _mapa.PonerTerreno(p.x, p.y, _herramienta.accion == Accion.Tierra ? _herramienta.color : (byte)0);
                        if (_herramienta.accion == Accion.Agua)
                        {
                            _mapa.PonerNivel(p.x, p.y, 0);
                            QuitarEn(p);
                        }
                    }
                    if (_herramienta.accion == Accion.Agua) QuitarRampasEn(zona);
                    _pintor.Repintar(_mapa, zona);
                    break;

                case Accion.Nivel:
                    // Se pinta una altura concreta, no se sube «uno más»: arrastrar sobre lo
                    // ya pintado no lo sigue subiendo. Y la tercera altura solo se apoya en
                    // la segunda, como una meseta de verdad.
                    byte objetivo = _herramienta.color;
                    foreach (var p in zona.allPositionsWithin)
                    {
                        if (_mapa.TerrenoEn(p.x, p.y) == 0) continue;
                        if (objetivo == 2 && _mapa.NivelEn(p.x, p.y) < 1) continue;
                        _mapa.PonerNivel(p.x, p.y, objetivo);
                    }
                    QuitarRampasEn(new RectInt(zona.x - 1, zona.y - 2, zona.width + 2, zona.height + 3));
                    _pintor.Repintar(_mapa, new RectInt(zona.x, zona.y - 1, zona.width, zona.height + 1));
                    break;

                case Accion.EscaleraDer:
                case Accion.EscaleraIzq:
                    if (!ReglasDeMapa.CabeEscalera(_mapa, c.x, c.y)) { Avisar("La escalera va al pie de un escalón de una altura."); break; }
                    _mapa.rampas.RemoveAll(r => r.x == c.x && (r.y == c.y || r.y == c.y - 1 || r.y == c.y + 1));
                    _mapa.rampas.Add(new Rampa(c.x, c.y, _herramienta.accion == Accion.EscaleraDer));
                    _pintor.Repintar(_mapa, new RectInt(c.x, c.y, 1, 2));
                    break;

                case Accion.Objeto:
                    if (_mapa.TerrenoEn(c.x, c.y) == 0) { Avisar("Eso va en tierra."); break; }
                    if (Ocupado(c)) break;
                    var o = new ObjetoDeMapa(_herramienta.semilla, c.x, c.y, _variante);
                    _mapa.objetos.Add(o);
                    Sembrar(o);
                    break;

                case Accion.Criadero:
                    if (_mapa.TerrenoEn(c.x, c.y) == 0) { Avisar("Un criadero va en tierra."); break; }
                    if (Ocupado(c)) break;
                    _mapa.criaderos.Add(c);
                    Marcar(c, IconoDe(SemillaMapa.Oveja), "Criadero", Color.white);
                    break;

                case Accion.Base:
                    if (_mapa.bases.Count >= ReglasDeMapa.BasesMaximas) { Avisar("Ya hay cinco puntos de inicio, el máximo."); break; }
                    if (!HuecoLibre(Planta(TipoEdificio.Castillo, c))) { Avisar("El castillo no cabe ahí: pisa agua u otra cosa."); break; }
                    _mapa.bases.Add(c);
                    RehacerMarcasDeBase();
                    break;

                case Accion.Edificio:
                    if (_mapa.bases.Count == 0) { Avisar("Pon primero un punto de inicio: el edificio será de la base más cercana."); break; }
                    if (!HuecoLibre(Planta(_herramienta.edificio, c))) { Avisar("No cabe ahí: pisa agua u otra cosa."); break; }
                    _mapa.edificios.Add(new EdificioDeMapa(_herramienta.edificio, c.x, c.y, _variante));
                    RehacerMarcasDeBase();
                    break;

                case Accion.Borrar:
                    foreach (var p in zona.allPositionsWithin) QuitarEn(p);
                    RehacerMarcasDeBase();
                    break;
            }
        }

        DatosEdificio Ficha(TipoEdificio t)
        {
            var ed = CatalogoDeEdificios.Actual;
            return ed != null ? ed.Obtener(t) : null;
        }

        int Fachadas(TipoEdificio t) => Ficha(t)?.Fachadas ?? 1;

        RectInt Planta(TipoEdificio t, Vector2Int c)
        {
            var f = Ficha(t);
            return f != null ? f.CeldasDesde(c) : new RectInt(c.x, c.y, 1, 1);
        }

        /// <summary>
        /// ¿Cabe aquí? Toda la planta en tierra y sin pisar nada: ni recursos, ni criaderos,
        /// ni la planta de otro castillo o edificio. No hay distancia mínima: el diseñador
        /// decide lo cerca que pone las cosas.
        /// </summary>
        bool HuecoLibre(RectInt planta)
        {
            foreach (var p in planta.allPositionsWithin)
            {
                if (_mapa.TerrenoEn(p.x, p.y) == 0) return false;
                if (_objetos.ContainsKey(p) || _mapa.criaderos.Contains(p)) return false;
            }

            foreach (var b in _mapa.bases)
                if (Planta(TipoEdificio.Castillo, b).Overlaps(planta)) return false;
            foreach (var e in _mapa.edificios)
                if (Planta(e.tipo, new Vector2Int(e.x, e.y)).Overlaps(planta)) return false;

            return true;
        }

        bool Ocupado(Vector2Int c, int radio = 1)
        {
            for (int dx = -radio; dx <= radio; dx++)
                for (int dy = -radio; dy <= radio; dy++)
                {
                    var p = c + new Vector2Int(dx, dy);
                    if (_objetos.ContainsKey(p) || _marcas.ContainsKey(p) || _mapa.bases.Contains(p)) return true;
                }
            return false;
        }

        void QuitarEn(Vector2Int p)
        {
            if (_objetos.TryGetValue(p, out var go)) { Destroy(go); _objetos.Remove(p); }
            _mapa.objetos.RemoveAll(o => o.x == p.x && o.y == p.y);

            if (_marcas.TryGetValue(p, out var m) && !m.name.StartsWith("Marca_Base") && !m.name.StartsWith("Marca_Edificio"))
            { Destroy(m); _marcas.Remove(p); }
            _mapa.criaderos.Remove(p);

            // Castillos y edificios se borran tocando cualquier celda de su planta.
            _mapa.bases.RemoveAll(b => Planta(TipoEdificio.Castillo, b).Contains(p));
            _mapa.edificios.RemoveAll(e => Planta(e.tipo, new Vector2Int(e.x, e.y)).Contains(p));
        }

        void QuitarRampasEn(RectInt zona) =>
            _mapa.rampas.RemoveAll(r => zona.Contains(new Vector2Int(r.x, r.y)) || zona.Contains(new Vector2Int(r.x, r.y + 1)));

        void Deshacer()
        {
            if (_deshacer.Count == 0) { Avisar("No hay nada que deshacer."); return; }

            var anterior = MapaGuardado.DeJson(_deshacer.Pop());
            if (anterior == null) return;

            // Mismo objeto: la configuración de partida lo referencia.
            _mapa.terreno = anterior.terreno; _mapa.nivel = anterior.nivel;
            CopiarDe(anterior);
            _pintor.Pintar(_mapa);
            ReconstruirObjetos();
            PintarCabecera();
        }

        void CopiarDe(MapaGuardado otro)
        {
            System.Array.Copy(otro.Terreno, _mapa.Terreno, Mathf.Min(otro.Terreno.Length, _mapa.Terreno.Length));
            System.Array.Copy(otro.Nivel, _mapa.Nivel, Mathf.Min(otro.Nivel.Length, _mapa.Nivel.Length));
            _mapa.rampas = new List<Rampa>(otro.rampas);
            _mapa.objetos = new List<ObjetoDeMapa>(otro.objetos);
            _mapa.criaderos = new List<Vector2Int>(otro.criaderos);
            _mapa.bases = new List<Vector2Int>(otro.bases);
            _mapa.edificios = new List<EdificioDeMapa>(otro.edificios);
            _mapa.ambiente = otro.ambiente;
            _mapa.segundosPorOveja = otro.segundosPorOveja;
        }

        // -----------------------------------------------------------------
        // Lo que se ve encima del terreno
        // -----------------------------------------------------------------

        void ReconstruirObjetos()
        {
            foreach (var go in _objetos.Values) if (go != null) Destroy(go);
            foreach (var go in _marcas.Values) if (go != null) Destroy(go);
            _objetos.Clear();
            _marcas.Clear();

            // Lo que el constructor de partida sembró al abrir no es nuestro: se recoge.
            foreach (var nodo in FindObjectsByType<NodoRecurso>(FindObjectsSortMode.None))
                if (nodo.gameObject.activeInHierarchy) Destroy(nodo.gameObject);

            foreach (var o in _mapa.objetos) Sembrar(o);
            foreach (var c in _mapa.criaderos) Marcar(c, IconoDe(SemillaMapa.Oveja), "Criadero", Color.white);
            RehacerMarcasDeBase();
        }

        void Sembrar(ObjetoDeMapa o)
        {
            var catalogo = CatalogoDeRecursos.Actual;
            if (catalogo == null) return;

            var nodo = catalogo.Sembrar(o.tipo, o.variante, new Vector2Int(o.x, o.y));
            if (nodo != null) _objetos[new Vector2Int(o.x, o.y)] = nodo.gameObject;
        }

        void Marcar(Vector2Int c, Sprite sprite, string nombre, Color color, Vector3? donde = null)
        {
            var go = new GameObject($"Marca_{nombre}_{c.x}_{c.y}");
            go.transform.position = donde ?? new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 500;
            _marcas[c] = go;
        }

        /// <summary>Las bases llevan el castillo en el color que tendrían por su número.</summary>
        void RehacerMarcasDeBase()
        {
            var quitar = new List<Vector2Int>();
            foreach (var par in _marcas)
                if (par.Value != null && (par.Value.name.StartsWith("Marca_Base") || par.Value.name.StartsWith("Marca_Edificio")))
                    quitar.Add(par.Key);
            foreach (var c in quitar) { Destroy(_marcas[c]); _marcas.Remove(c); }

            var edificios = CatalogoDeEdificios.Actual;
            var ficha = edificios != null ? edificios.Obtener(TipoEdificio.Castillo) : null;

            for (int i = 0; i < _mapa.bases.Count; i++)
            {
                var b = _mapa.bases[i];
                var sprite = ficha != null ? edificios.SpriteDe(ficha, i) : null;
                Marcar(b, sprite, "Base", Color.white, ficha != null ? Edificio.PosicionPara(ficha, ficha.CeldasDesde(b)) : (Vector3?)null);
            }

            // Cada edificio, en el color del punto de inicio más cercano: así se ve de quién es.
            foreach (var e in _mapa.edificios)
            {
                var f = Ficha(e.tipo);
                if (f == null) continue;
                var c = new Vector2Int(e.x, e.y);
                int dueno = Mathf.Max(0, _mapa.BaseMasCercana(e.x, e.y));
                Marcar(c, edificios.SpriteDe(f, dueno, e.variante), "Edificio", Color.white,
                       Edificio.PosicionPara(f, f.CeldasDesde(c), e.variante));
            }

            PintarCabecera();
        }

        Sprite IconoDe(SemillaMapa s)
        {
            var catalogo = CatalogoDeRecursos.Actual;
            var molde = catalogo != null ? catalogo.Obtener(s, _variante) : null;
            var sr = molde != null ? molde.GetComponentInChildren<SpriteRenderer>(true) : null;
            return sr != null ? sr.sprite : null;
        }

        // -----------------------------------------------------------------
        // Cursor: la silueta de lo que se va a poner
        // -----------------------------------------------------------------

        void PrepararCursor()
        {
            if (_silueta != null) return;

            var go = new GameObject("Silueta del editor");
            _silueta = go.AddComponent<SpriteRenderer>();
            _silueta.sortingOrder = 900;

            var marco = new GameObject("Marco del pincel");
            _marco = marco.AddComponent<LineRenderer>();
            _marco.useWorldSpace = true;
            _marco.loop = true;
            _marco.positionCount = 4;
            _marco.widthMultiplier = 0.12f;
            _marco.material = new Material(Shader.Find("Sprites/Default"));
            _marco.sortingOrder = 901;
        }

        void MoverCursor(Vector2Int c, bool sobreInterfaz)
        {
            bool visible = !sobreInterfaz && _mapa.Dentro(c.x, c.y);
            _silueta.enabled = visible && !UsaTamano;
            _marco.enabled = visible;
            if (!visible) return;

            var zona = Zona(c);
            bool valido = EsValido(c);
            var tinte = valido ? new Color(0.55f, 1f, 0.55f, 0.95f) : new Color(1f, 0.45f, 0.4f, 0.95f);

            _marco.startColor = _marco.endColor = tinte;
            _marco.SetPosition(0, new Vector3(zona.xMin, zona.yMin, 0f));
            _marco.SetPosition(1, new Vector3(zona.xMax, zona.yMin, 0f));
            _marco.SetPosition(2, new Vector3(zona.xMax, zona.yMax, 0f));
            _marco.SetPosition(3, new Vector3(zona.xMin, zona.yMax, 0f));

            if (_silueta.enabled)
            {
                _silueta.sprite = _herramienta.icono;
                _silueta.color = new Color(tinte.r, tinte.g, tinte.b, 0.6f);
                _silueta.transform.position = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
            }
        }

        bool EsValido(Vector2Int c)
        {
            switch (_herramienta.accion)
            {
                case Accion.Objeto:
                case Accion.Criadero: return _mapa.TerrenoEn(c.x, c.y) > 0 && !Ocupado(c);
                case Accion.Base: return _mapa.bases.Count < ReglasDeMapa.BasesMaximas && HuecoLibre(Planta(TipoEdificio.Castillo, c));
                case Accion.Edificio: return _mapa.bases.Count > 0 && HuecoLibre(Planta(_herramienta.edificio, c));
                case Accion.EscaleraDer:
                case Accion.EscaleraIzq: return ReglasDeMapa.CabeEscalera(_mapa, c.x, c.y);
                default: return true;
            }
        }

        // -----------------------------------------------------------------
        // Herramientas
        // -----------------------------------------------------------------

        List<Herramienta> HerramientasDe(Categoria c)
        {
            var l = new List<Herramienta>();
            switch (c)
            {
                case Categoria.Terreno:
                    for (byte k = 1; paleta != null && k <= paleta.Colores; k++)
                        l.Add(new Herramienta { nombre = $"Tierra {k}", icono = paleta.colores[k - 1]?.muestra, accion = Accion.Tierra, color = k });
                    l.Add(new Herramienta { nombre = "Agua", icono = MuestraAgua(), accion = Accion.Agua });
                    break;
                case Categoria.Relieve:
                    l.Add(new Herramienta { nombre = "Llano", icono = paleta != null ? paleta.colores[0]?.muestra : null, accion = Accion.Nivel, color = 0 });
                    l.Add(new Herramienta { nombre = "Altura 2", icono = iconoSubir, accion = Accion.Nivel, color = 1 });
                    l.Add(new Herramienta { nombre = "Altura 3", icono = iconoSubir, accion = Accion.Nivel, color = 2 });
                    l.Add(new Herramienta { nombre = "Escalera ↗", icono = PiezaComoSprite(1, GeneradorTerreno.RampaAltaDer), accion = Accion.EscaleraDer });
                    l.Add(new Herramienta { nombre = "Escalera ↖", icono = PiezaComoSprite(1, GeneradorTerreno.RampaAltaIzq), accion = Accion.EscaleraIzq });
                    break;
                case Categoria.Naturaleza:
                    l.Add(Objeto("Árbol", SemillaMapa.Arbol));
                    l.Add(Objeto("Mena de oro", SemillaMapa.Oro));
                    l.Add(Objeto("Piedra", SemillaMapa.Piedra));
                    l.Add(Objeto("Arbusto", SemillaMapa.Arbusto));
                    break;
                case Categoria.Ovejas:
                    l.Add(new Herramienta { nombre = "Criadero", icono = IconoDe(SemillaMapa.Oveja), accion = Accion.Criadero });
                    break;
                case Categoria.Bases:
                    var ed = CatalogoDeEdificios.Actual;
                    var ficha = ed != null ? ed.Obtener(TipoEdificio.Castillo) : null;
                    l.Add(new Herramienta { nombre = "Punto de inicio", icono = ficha != null ? ed.SpriteDe(ficha, 0) : null, accion = Accion.Base });
                    foreach (var (t, n) in new[] { (TipoEdificio.Casa, "Casa"), (TipoEdificio.Torre, "Torre"), (TipoEdificio.Cuartel, "Cuartel"),
                                                   (TipoEdificio.CampoDeTiro, "Campo de tiro"), (TipoEdificio.Monasterio, "Monasterio") })
                    {
                        var f = ed != null ? ed.Obtener(t) : null;
                        if (f == null) continue;
                        l.Add(new Herramienta { nombre = n, icono = ed.SpriteDe(f, 0, 0), accion = Accion.Edificio, edificio = t });
                    }
                    break;
                case Categoria.Borrar:
                    l.Add(new Herramienta { nombre = "Borrador", icono = iconoBorrar, accion = Accion.Borrar });
                    break;
            }
            return l;
        }

        Herramienta Objeto(string nombre, SemillaMapa s) =>
            new Herramienta { nombre = nombre, icono = IconoDe(s), accion = Accion.Objeto, semilla = s };

        Sprite PiezaComoSprite(int color, int indice) =>
            paleta != null && paleta.Pieza(color, indice) is UnityEngine.Tilemaps.Tile t ? t.sprite : null;

        Sprite MuestraAgua() => paleta != null && paleta.agua is UnityEngine.Tilemaps.Tile t ? t.sprite : null;

        void ElegirCategoria(Categoria c)
        {
            _categoria = c;
            foreach (var (b, cat) in _pestanas) ((Image)b.targetGraphic).color = cat == c ? Color.white : new Color(0.65f, 0.65f, 0.65f);

            foreach (var b in _botonesHerramienta) Destroy(b.gameObject);
            _botonesHerramienta.Clear();
            _dibujos.Clear();

            var lista = HerramientasDe(c);
            for (int i = 0; i < lista.Count; i++)
            {
                var h = lista[i];
                var b = Ficha(_rejilla, h, i);
                _botonesHerramienta.Add(b);
            }

            if (lista.Count > 0) ElegirHerramienta(lista[0], 0);
        }

        void ElegirHerramienta(Herramienta h, int indice)
        {
            _herramienta = h;
            _indiceHerramienta = indice;
            _variante = 0;
            _nombreHerramienta.text = h.nombre;
            for (int i = 0; i < _botonesHerramienta.Count; i++)
                ((Image)_botonesHerramienta[i].targetGraphic).sprite = tema != null ? tema.CajaChicaDeColor(i == indice ? 2 : 0) : null;
            PintarAjustes();
        }

        // -----------------------------------------------------------------
        // Archivo
        // -----------------------------------------------------------------

        /// <summary>
        /// Guardar pregunta antes (semana 14): escribir un archivo no se deshace con Ctrl+Z, y
        /// con el mismo nombre se pisa el mapa anterior sin remedio.
        /// </summary>
        void Guardar()
        {
            TomarNombre();
            Preguntar($"¿Guardar «{_mapa.nombre}»?", DetalleDeGuardar(), "Guardar", () => GuardarYa());
        }

        void TomarNombre() =>
            _mapa.nombre = string.IsNullOrWhiteSpace(_campoNombre.text) ? _mapa.nombre : _campoNombre.text.Trim();

        /// <summary>Lo que conviene saber antes de decir que sí.</summary>
        string DetalleDeGuardar()
        {
            var partes = new List<string>();
            string ruta = System.IO.Path.Combine(ArchivoDeMapas.CarpetaPropia, ArchivoDeMapas.NombreDeArchivo(_mapa.nombre) + ".json");
            if (System.IO.File.Exists(ruta)) partes.Add("Ya hay un mapa tuyo con ese nombre: se sustituirá.");
            if (!ReglasDeMapa.EsJugable(_mapa, out var motivo)) partes.Add("Aún no se puede jugar. " + motivo);
            int sinAcceso = ReglasDeMapa.ZonasAltasSinAcceso(_mapa);
            if (sinAcceso > 0)
                partes.Add(sinAcceso == 1 ? "Hay una zona alta sin escalera: nadie podrá subir."
                                          : $"Hay {sinAcceso} zonas altas sin escalera: nadie podrá subir.");
            return partes.Count == 0 ? "Se podrá jugar desde Sandbox." : string.Join("\n", partes);
        }

        bool GuardarYa()
        {
            bool jugable = ReglasDeMapa.EsJugable(_mapa, out var motivo);
            var ruta = ArchivoDeMapas.Guardar(_mapa);

            if (ruta == null) { Avisar("No se pudo guardar el mapa."); return false; }
            _guardado = _mapa.AJson();
            Avisar(jugable ? $"Guardado: {_mapa.nombre}" : $"Guardado, pero aún no se puede jugar. {motivo}", 6f);
            return true;
        }

        void Probar()
        {
            if (!ReglasDeMapa.EsJugable(_mapa, out var motivo)) { Avisar(motivo, 6f); return; }

            TomarNombre();
            if (!HayCambios) { Jugar(); return; }
            Preguntar("¿Guardar y probar?", "Para probar el mapa hay que guardarlo.\n" +DetalleDeGuardar(), "Sí",
                      () => { if (GuardarYa()) Jugar(); });
        }

        void Jugar()
        {
            var c = ConfiguracionDePartida.PorDefecto(_mapa.bases.Count, Navegacion.EscenaDelMenu);
            c.modo = ModoDePartida.Sandbox;
            c.mapa = MapaGuardado.DeJson(_mapa.AJson());
            Navegacion.Empezar(c);
        }

        void Salir()
        {
            if (!HayCambios) { Navegacion.IrAlMenu(); return; }
            Preguntar("¿Salir sin guardar?", "Los cambios desde la última vez que guardaste se perderán.", "Salir",
                      Navegacion.IrAlMenu);
        }

        /// <summary>El mapa tal como se guardó o se abrió, para saber si hay algo que perder.</summary>
        string _guardado;

        bool HayCambios => _mapa != null && _mapa.AJson() != _guardado;

        Text _tituloDialogo, _textoDialogo;
        Button _botonSi;
        System.Action _alAceptar;

        /// <summary>
        /// La pregunta de sí o no, con la misma madera que el resto. Se monta la primera vez
        /// y luego solo se cambian sus textos.
        /// </summary>
        void Preguntar(string titulo, string detalle, string si, System.Action alAceptar)
        {
            if (_dialogo == null) ConstruirDialogo();
            _tituloDialogo.text = titulo;
            _textoDialogo.text = detalle;
            KitInterfaz.CambiarTexto(_botonSi, si);
            _alAceptar = alAceptar;
            _dialogo.gameObject.SetActive(true);
            _dialogo.SetAsLastSibling();
        }

        void ConstruirDialogo()
        {
            var velo = KitInterfaz.Velo(_raiz, 0.55f);
            _dialogo = velo.rectTransform;

            var caja = KitInterfaz.Imagen(velo.transform, "Caja", tema != null ? tema.panelFondo : null, Color.white);
            caja.raycastTarget = true;
            KitInterfaz.Colocar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760f, 400f));

            _tituloDialogo = KitInterfaz.Texto(caja.transform, "Titulo", "", _fuente, 42, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_tituloDialogo.rectTransform, new Vector2(0.5f, 1f), new Vector2(680f, 70f), new Vector2(0f, -70f));

            _textoDialogo = KitInterfaz.TextoQueCabe(caja.transform, "Texto", _fuente, 26, 16, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_textoDialogo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640f, 130f), new Vector2(0f, 10f));

            var no = KitInterfaz.Boton(caja.transform, "No", tema != null ? tema.BotonDeColor(1) : null, "No", _fuente, 30, Cerrar);
            KitInterfaz.Colocar((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(230f, 70f), new Vector2(-135f, 75f));

            _botonSi = KitInterfaz.Boton(caja.transform, "Si", tema != null ? tema.BotonDeColor(0) : null, "Sí", _fuente, 30, () =>
            {
                var accion = _alAceptar;
                Cerrar();
                accion?.Invoke();
            });
            KitInterfaz.Colocar((RectTransform)_botonSi.transform, new Vector2(0.5f, 0f), new Vector2(230f, 70f), new Vector2(135f, 75f));

            _dialogo.gameObject.SetActive(false);
        }

        void Cerrar()
        {
            _alAceptar = null;
            if (_dialogo != null) _dialogo.gameObject.SetActive(false);
        }

        void Avisar(string texto, float segundos = 3.5f)
        {
            _aviso.text = texto;
            _avisoHasta = Time.unscaledTime + segundos;
        }

        void PintarCabecera()
        {
            if (_info == null || _mapa == null) return;

            bool jugable = ReglasDeMapa.EsJugable(_mapa, out var motivo);
            _info.text = $"{_mapa.ancho}×{_mapa.alto}  ·  Bases {_mapa.bases.Count}/{ReglasDeMapa.BasesMaximas}  ·  " +
                         (jugable ? "Listo para jugar" : motivo);
            if (_campoNombre != null && !_campoNombre.isFocused) _campoNombre.text = _mapa.nombre;
        }

        void PintarAjustes()
        {
            if (_valorTamano == null) return;
            _valorTamano.text = UsaTamano ? $"{_tamano}×{_tamano}" : "—";
            _valorVariante.text = TieneVariantes ? $"{_variante + 1}" : "—";
            if (_herramienta.accion == Accion.Criadero) _valorVariante.text = $"cada {_mapa.segundosPorOveja:0} s";
        }

        void CambiarTamano(int paso)
        {
            int i = System.Array.IndexOf(Tamanos, _tamano);
            _tamano = Tamanos[Mathf.Clamp(i + paso, 0, Tamanos.Length - 1)];
            PintarAjustes();
        }

        void CambiarVariante(int paso)
        {
            if (_herramienta.accion == Accion.Criadero)
            {
                _mapa.segundosPorOveja = Mathf.Clamp(_mapa.segundosPorOveja + paso * 5f, 5f, 90f);
            }
            else if (TieneVariantes)
            {
                int total;
                if (_herramienta.accion == Accion.Objeto)
                {
                    var catalogo = CatalogoDeRecursos.Actual;
                    total = catalogo != null ? Mathf.Max(1, catalogo.Variantes(_herramienta.semilla)) : 1;
                }
                else total = Mathf.Max(1, Fachadas(_herramienta.edificio));

                _variante = (_variante + paso + total) % total;

                // La ficha enseña la variante que tienes en la mano, no la primera.
                if (_herramienta.accion == Accion.Objeto) _herramienta.icono = IconoDe(_herramienta.semilla);
                else
                {
                    var f = Ficha(_herramienta.edificio);
                    var ed = CatalogoDeEdificios.Actual;
                    if (f != null && ed != null) _herramienta.icono = ed.SpriteDe(f, 0, _variante);
                }

                if (_indiceHerramienta < _dibujos.Count) _dibujos[_indiceHerramienta].sprite = _herramienta.icono;
            }
            PintarAjustes();
        }

        // -----------------------------------------------------------------
        // Interfaz: la mesa de guerra, versión taller
        // -----------------------------------------------------------------

        void ConstruirInterfaz()
        {
            var lienzo = KitInterfaz.Lienzo(transform, "LienzoEditor", 40);
            _raiz = (RectTransform)lienzo.transform;

            ConstruirBarra();
            ConstruirMuelle();
            ConstruirAmbiente();
        }

        void ConstruirBarra()
        {
            var barra = KitInterfaz.Imagen(_raiz, "Barra", tema != null ? tema.panelFondo : null, Color.white);
            barra.raycastTarget = true;
            var rt = barra.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-40f, 110f);
            rt.anchoredPosition = new Vector2(0f, -10f);

            // Nombre del mapa, editable.
            var caja = KitInterfaz.Imagen(rt, "CajaNombre", tema != null ? tema.CajaChicaDeColor(0) : null, Color.white);
            KitInterfaz.Colocar(caja.rectTransform, new Vector2(0f, 0.5f), new Vector2(420f, 64f), new Vector2(250f, 0f));
            var texto = KitInterfaz.Texto(caja.transform, "Texto", "", _fuente, 30, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Llenar(texto.rectTransform).offsetMin = new Vector2(16f, 0f);
            texto.rectTransform.offsetMax = new Vector2(-16f, 0f);
            _campoNombre = caja.gameObject.AddComponent<InputField>();
            _campoNombre.textComponent = texto;
            _campoNombre.characterLimit = 32;
            caja.raycastTarget = true;

            _info = KitInterfaz.TextoQueCabe(rt, "Info", _fuente, 24, 14, KitInterfaz.Crema, TextAnchor.MiddleLeft);
            KitInterfaz.Colocar(_info.rectTransform, new Vector2(0f, 0.5f), new Vector2(620f, 60f), new Vector2(800f, 0f));

            var acciones = new (string, int, System.Action)[]
            {
                ("Ambiente", 3, () => _ambiente.gameObject.SetActive(!_ambiente.gameObject.activeSelf)),
                ("Guardar", 0, Guardar),
                ("Probar", 2, Probar),
                ("Salir", 1, Salir),
            };
            for (int i = 0; i < acciones.Length; i++)
            {
                var (nombre, color, accion) = acciones[i];
                var b = KitInterfaz.Boton(rt, nombre, tema != null ? tema.BotonDeColor(color) : null, nombre, _fuente, 30, accion);
                KitInterfaz.Colocar((RectTransform)b.transform, new Vector2(1f, 0.5f), new Vector2(190f, 66f),
                                    new Vector2(-120f - (acciones.Length - 1 - i) * 205f, 0f));
            }

            _aviso = KitInterfaz.Texto(_raiz, "Aviso", "", _fuente, 30, new Color(1f, 0.86f, 0.6f), TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_aviso.rectTransform, new Vector2(0.5f, 1f), new Vector2(1400f, 50f), new Vector2(0f, -150f));
        }

        void ConstruirMuelle()
        {
            var muelle = KitInterfaz.Imagen(_raiz, "Muelle", tema != null ? tema.panelFondo : null, Color.white);
            muelle.raycastTarget = true;
            KitInterfaz.Colocar(muelle.rectTransform, new Vector2(0.5f, 0f), new Vector2(1640f, 270f), new Vector2(0f, 145f));
            var m = muelle.rectTransform;

            // Izquierda: las categorías, como pestañas con su dibujo.
            var categorias = new (Categoria, string)[]
            {
                (Categoria.Terreno, "Terreno"), (Categoria.Relieve, "Relieve"), (Categoria.Naturaleza, "Naturaleza"),
                (Categoria.Ovejas, "Ovejas"), (Categoria.Bases, "Bases"), (Categoria.Borrar, "Borrar"),
            };
            for (int i = 0; i < categorias.Length; i++)
            {
                var (cat, nombre) = categorias[i];
                var b = KitInterfaz.Boton(m, nombre, tema != null ? tema.BotonDeColor(i % 5) : null, nombre, _fuente, 24,
                                          () => ElegirCategoria(cat));
                int col = i % 2, fila = i / 2;
                KitInterfaz.Colocar((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(170f, 58f),
                                    new Vector2(115f + col * 180f, -55f - fila * 66f));
                _pestanas.Add((b, cat));
            }

            // Centro: las herramientas de la categoría, en fichas con su dibujo.
            var centro = KitInterfaz.Imagen(m, "Herramientas", tema != null ? tema.CajaDeColor(0) : null, Color.white);
            KitInterfaz.Colocar(centro.rectTransform, new Vector2(0f, 0.5f), new Vector2(820f, 220f), new Vector2(830f, 0f));
            _rejilla = centro.rectTransform;

            // Derecha: ajustes del pincel y el nombre de lo que tienes en la mano.
            var ajustes = KitInterfaz.Imagen(m, "Ajustes", tema != null ? tema.CajaDeColor(3) : null, Color.white);
            KitInterfaz.Colocar(ajustes.rectTransform, new Vector2(1f, 0.5f), new Vector2(340f, 220f), new Vector2(-195f, 0f));
            var a = ajustes.rectTransform;

            _nombreHerramienta = KitInterfaz.TextoQueCabe(a, "Herramienta", _fuente, 30, 16, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_nombreHerramienta.rectTransform, new Vector2(0.5f, 1f), new Vector2(300f, 50f), new Vector2(0f, -40f));

            KitInterfaz.Etiqueta(a, "Tamaño", _fuente, 22, new Vector2(0f, 1f), new Vector2(110f, 44f), new Vector2(75f, -100f));
            _valorTamano = KitInterfaz.Ciclador(a, "Tamano", _fuente, tema != null ? tema.CajaChicaDeColor(3) : null,
                                                tema != null ? tema.BotonDeColor(3) : null, CambiarTamano,
                                                new Vector2(1f, 1f), new Vector2(190f, 44f), new Vector2(-110f, -100f), 22);

            KitInterfaz.Etiqueta(a, "Variante", _fuente, 22, new Vector2(0f, 1f), new Vector2(110f, 44f), new Vector2(75f, -160f));
            _valorVariante = KitInterfaz.Ciclador(a, "Variante", _fuente, tema != null ? tema.CajaChicaDeColor(3) : null,
                                                  tema != null ? tema.BotonDeColor(3) : null, CambiarVariante,
                                                  new Vector2(1f, 1f), new Vector2(190f, 44f), new Vector2(-110f, -160f), 22);
        }

        Button Ficha(RectTransform padre, Herramienta h, int indice)
        {
            var b = KitInterfaz.Boton(padre, h.nombre, tema != null ? tema.CajaChicaDeColor(0) : null, "", _fuente, 10,
                                      () => ElegirHerramienta(h, indice));
            int col = indice % 6, fila = indice / 6;
            KitInterfaz.Colocar((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(118f, 118f),
                                new Vector2(78f + col * 128f, -78f - fila * 128f));

            var dibujo = KitInterfaz.Imagen(b.transform, "Dibujo", h.icono, Color.white);
            _dibujos.Add(dibujo);
            dibujo.preserveAspect = true;
            KitInterfaz.Llenar(dibujo.rectTransform).offsetMin = new Vector2(14f, 26f);
            dibujo.rectTransform.offsetMax = new Vector2(-14f, -10f);

            var nombre = KitInterfaz.TextoQueCabe(b.transform, "Nombre", _fuente, 18, 11, KitInterfaz.Crema, TextAnchor.LowerCenter);
            nombre.text = h.nombre;
            KitInterfaz.Llenar(nombre.rectTransform).offsetMin = new Vector2(4f, 4f);
            nombre.rectTransform.offsetMax = new Vector2(-4f, -4f);
            return b;
        }

        void ConstruirAmbiente()
        {
            var caja = KitInterfaz.Imagen(_raiz, "Ambiente", tema != null ? tema.panelFondo : null, Color.white);
            caja.raycastTarget = true;
            KitInterfaz.Colocar(caja.rectTransform, new Vector2(1f, 0.5f), new Vector2(460f, 420f), new Vector2(-250f, 60f));
            _ambiente = caja.rectTransform;

            var t = KitInterfaz.Texto(_ambiente, "Titulo", "Ambiente", _fuente, 36, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(400f, 60f), new Vector2(0f, -50f));

            Sprite marco = KitInterfaz.ConBorde(tema != null ? tema.barraGrandeMarco : null, new Vector4(16, 0, 16, 0));
            Sprite relleno = KitInterfaz.ConBorde(tema != null ? tema.barraGrandeRellenoAzul : null, new Vector4(6, 0, 6, 0));
            Sprite perilla = tema != null ? tema.BotonDeColor(0) : null;

            Barra("Tono", 0, () => { Color.RGBToHSV(_mapa.ambiente.tono, out var h, out _, out _); return h; },
                  v => { var a = _mapa.ambiente; a.tono = v < 0.01f ? Color.white : Color.HSVToRGB(v, 0.28f, 1f); _mapa.ambiente = a; },
                  marco, relleno, perilla);
            Barra("Saturación", 1, () => _mapa.ambiente.saturacion / 2f,
                  v => { var a = _mapa.ambiente; a.saturacion = v * 2f; _mapa.ambiente = a; }, marco, relleno, perilla);
            Barra("Brillo", 2, () => Mathf.InverseLerp(0.4f, 1.6f, _mapa.ambiente.brillo),
                  v => { var a = _mapa.ambiente; a.brillo = Mathf.Lerp(0.4f, 1.6f, v); _mapa.ambiente = a; }, marco, relleno, perilla);

            var neutro = KitInterfaz.Boton(_ambiente, "Neutro", tema != null ? tema.BotonDeColor(4) : null, "Sin filtro", _fuente, 26,
                                           () => { _mapa.ambiente = AmbienteDeMapa.Neutro; FiltroDeMapa.Aplicar(_mapa.ambiente); _ambiente.gameObject.SetActive(false); _ambiente.gameObject.SetActive(true); });
            KitInterfaz.Colocar((RectTransform)neutro.transform, new Vector2(0.5f, 0f), new Vector2(240f, 60f), new Vector2(0f, 55f));

            _ambiente.gameObject.SetActive(false);
        }

        void Barra(string nombre, int fila, System.Func<float> leer, System.Action<float> escribir,
                   Sprite marco, Sprite relleno, Sprite perilla)
        {
            KitInterfaz.Etiqueta(_ambiente, nombre, _fuente, 24, new Vector2(0f, 1f), new Vector2(150f, 44f),
                                 new Vector2(100f, -120f - fila * 70f));
            var s = KitInterfaz.Deslizador(_ambiente, nombre, marco, relleno, perilla, 0.5f,
                                           v => { if (_mapa == null) return; escribir(v); FiltroDeMapa.Aplicar(_mapa.ambiente); });
            KitInterfaz.Colocar((RectTransform)s.transform, new Vector2(1f, 1f), new Vector2(230f, 44f),
                                new Vector2(-145f, -120f - fila * 70f));

            // Al abrir el panel, la barra enseña el valor del mapa, no el de la última vez.
            s.gameObject.AddComponent<AlActivarse>().accion = () => { if (_mapa != null) s.SetValueWithoutNotify(leer()); };
        }
    }

    /// <summary>Llama a una acción cada vez que el objeto se enciende.</summary>
    public class AlActivarse : MonoBehaviour
    {
        public System.Action accion;
        void OnEnable() => accion?.Invoke();
    }
}
