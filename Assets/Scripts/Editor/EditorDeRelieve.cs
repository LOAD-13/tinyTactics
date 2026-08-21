using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TinyTactics.Mundo;

namespace TinyTactics.EditorHerramientas
{
    /// <summary>
    /// Pincel para dibujar mesetas y rampas directamente sobre la escena.
    ///
    /// El generador propone el relieve por ruido, pero dónde va un acantilado es una
    /// decisión de diseño de nivel, no de estadística. Con esta ventana abierta se pinta en
    /// la vista de escena, se guarda a un <see cref="RelieveMapa"/> y a partir de ahí el
    /// generador deja de inventar: regenerar la escena ya no cambia el escenario.
    ///
    /// Es el germen del editor de mapas de las semanas 12-13 (E13). De momento solo hace
    /// alturas, que es lo que hace falta hoy.
    /// </summary>
    public class EditorDeRelieve : EditorWindow
    {
        enum Pincel { Meseta, Llano, Rampa }

        enum Sentido { Automatico, Izquierda, Derecha }

        const string RutaRelieve = "Assets/Datos/Mapas/Relieve.asset";

        Pincel _pincel = Pincel.Meseta;
        Sentido _sentido = Sentido.Automatico;
        int _radio = 3;

        DefinicionMapa _definicion;
        MapaGenerado _mapa;
        RelieveMapa _relieve;

        byte[,] _alturas;
        readonly List<Rampa> _rampas = new List<Rampa>();
        int _ancho, _alto;

        bool _sucio;

        [MenuItem("Tiny Tactics/Editor de relieve", false, 20)]
        static void Abrir() => GetWindow<EditorDeRelieve>("Relieve").Show();

        void OnEnable()
        {
            SceneView.duringSceneGui += EnEscena;
            Enganchar();
        }

        void OnDisable() => SceneView.duringSceneGui -= EnEscena;

        // -----------------------------------------------------------------

        /// <summary>
        /// Reconstruye el mapa a partir de la definicion de la escena.
        ///
        /// No se lee de <c>MundoJuego</c> porque ese solo tiene el mapa montado en Play, y
        /// esto se usa con el editor parado. Da igual: el generador es determinista, asi
        /// que la misma definicion produce exactamente el mismo terreno que veras al jugar.
        /// </summary>
        void Enganchar()
        {
            var mundo = FindFirstObjectByType<MundoJuego>();
            _definicion = mundo != null ? mundo.definicion : null;

            if (_definicion == null)
            {
                _mapa = null;
                _alturas = null;
                return;
            }

            _mapa = GeneradorTerreno.Generar(_definicion);
            _ancho = _mapa.Ancho;
            _alto = _mapa.Alto;

            _alturas = new byte[_ancho, _alto];
            for (int x = 0; x < _ancho; x++)
                for (int y = 0; y < _alto; y++)
                    _alturas[x, y] = _mapa.Nivel[x, y];

            _rampas.Clear();
            _rampas.AddRange(_mapa.Rampas);
            _sucio = false;

            // Sincronizar la escena AHORA y no en la primera pincelada.
            //
            // El relieve pintado en la escena lo puso el generador en su momento; esta
            // ventana lo recalcula al engancharse. Si entre medias cambió el generador o el
            // asset de relieve, los dos no coinciden — y como el pincel repinta la capa
            // entera, la primera pincelada arrastraba el mapa al estado de la ventana y
            // parecía que aplanaba todo de golpe. Haciéndolo al abrir, lo que ves es
            // siempre lo que se va a guardar.
            Repintar();

            Debug.Log(
                $"[Tiny Tactics] Editor de relieve enganchado a \"{_definicion.name}\". " +
                $"Escena sincronizada: " +
                $"{GeneradorTerreno.ContarElevadas(_mapa.Nivel, _ancho, _alto)} tiles de " +
                $"meseta y {_rampas.Count} rampas.");
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Pinta sobre la vista de escena con el botón izquierdo.\n" +
                "Mayúsculas + clic borra.\n\n" +
                "Al abrir la ventana la escena se sincroniza con el relieve del editor. " +
                "Si ves un cambio en ese momento, es que la escena estaba pintada con una " +
                "versión anterior del generador.\n\n" +
                "La rampa ocupa un tile de ancho por dos de alto y va en el borde SUR de " +
                "una meseta: haz clic en la celda llana que queda justo debajo del " +
                "acantilado. Con el sentido en Automático, la cuesta cae sola hacia el " +
                "lado por donde se acaba el muro, que es como encaja a ras.",
                MessageType.Info);

            if (_mapa == null || _alturas == null)
            {
                EditorGUILayout.HelpBox(
                    "No encuentro el objeto Mundo con su definición de mapa. " +
                    "Abre la escena de juego y pulsa Recargar.",
                    MessageType.Warning);

                if (GUILayout.Button("Recargar")) Enganchar();
                return;
            }

            EditorGUILayout.LabelField("Mapa", $"{_ancho}×{_alto} · {_rampas.Count} rampas");

            _pincel = (Pincel)EditorGUILayout.EnumPopup("Pincel", _pincel);

            if (_pincel == Pincel.Rampa)
                _sentido = (Sentido)EditorGUILayout.EnumPopup("Sentido de la cuesta", _sentido);
            else
                _radio = EditorGUILayout.IntSlider("Radio", _radio, 1, 20);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!_sucio))
            {
                if (GUILayout.Button("Aplicar a la escena", GUILayout.Height(24)))
                    Repintar();
            }

            if (GUILayout.Button("Guardar el relieve", GUILayout.Height(28)))
                Guardar();

            EditorGUILayout.Space();

            if (GUILayout.Button("Recargar desde la escena")) Enganchar();

            if (GUILayout.Button("Aplanar todo"))
            {
                System.Array.Clear(_alturas, 0, _alturas.Length);
                _rampas.Clear();
                _sucio = true;
                Repintar();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Al guardar, el asset queda enlazado en la definición del mapa. A partir de " +
                "ahí «Generar escena de juego» respeta este relieve en vez de inventar otro.",
                MessageType.None);
        }

        // -----------------------------------------------------------------

        void EnEscena(SceneView vista)
        {
            if (_mapa == null || _alturas == null) return;

            var e = Event.current;

            // Sin esto, el clic lo captura la selección de objetos de Unity y el pincel
            // no llega a recibir el evento.
            int control = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(control);

            Vector2Int celda = CeldaBajoElRaton(e.mousePosition);
            DibujarGuia(celda);

            // Repintar el tilemap entero en cada arrastre sería lentísimo en 224×224:
            // se hace al soltar. La guía del cursor da la referencia mientras tanto.
            if (e.type == EventType.MouseUp && e.button == 0 && _sucio)
            {
                Repintar();
                e.Use();
                return;
            }

            bool pintando = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
                            e.button == 0 && !e.alt;

            if (!pintando) return;

            Pintar(celda, e.shift);
            e.Use();
        }

        Vector2Int CeldaBajoElRaton(Vector2 raton)
        {
            var rayo = HandleUtility.GUIPointToWorldRay(raton);

            // El mundo es plano en z = 0, así que basta con cortar el rayo contra ese plano.
            float t = Mathf.Approximately(rayo.direction.z, 0f) ? 0f : -rayo.origin.z / rayo.direction.z;
            Vector3 punto = rayo.origin + rayo.direction * t;

            return new Vector2Int(Mathf.FloorToInt(punto.x), Mathf.FloorToInt(punto.y));
        }

        void DibujarGuia(Vector2Int celda)
        {
            Handles.color = _pincel == Pincel.Rampa
                ? new Color(1f, 0.85f, 0.2f, 0.9f)
                : new Color(0.3f, 0.9f, 1f, 0.8f);

            if (_pincel == Pincel.Rampa)
            {
                // La cuesta ocupa la celda del pie y la de encima.
                Handles.DrawWireCube(new Vector3(celda.x + 0.5f, celda.y + 1f, 0f),
                                     new Vector3(1f, 2f, 0f));

                bool haciaLaDerecha = _sentido == Sentido.Automatico
                    ? GeneradorTerreno.SentidoAutomatico(_alturas, _ancho, _alto, celda.x, celda.y)
                    : _sentido == Sentido.Derecha;

                float punta = haciaLaDerecha ? 0.9f : 0.1f;
                Handles.DrawLine(new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f),
                                 new Vector3(celda.x + punta, celda.y + 0.5f, 0f));
            }
            else
            {
                Handles.DrawWireDisc(new Vector3(celda.x + 0.5f, celda.y + 0.5f, 0f),
                                     Vector3.forward, _radio);
            }

            SceneView.RepaintAll();
        }

        void Pintar(Vector2Int centro, bool borrando)
        {
            if (_pincel == Pincel.Rampa)
            {
                PintarRampa(centro, borrando);
                return;
            }

            bool alto = _pincel == Pincel.Meseta && !borrando;
            int radio2 = _radio * _radio;

            for (int x = centro.x - _radio; x <= centro.x + _radio; x++)
            {
                for (int y = centro.y - _radio; y <= centro.y + _radio; y++)
                {
                    if (x < 0 || y < 0 || x >= _ancho || y >= _alto) continue;

                    int dx = x - centro.x, dy = y - centro.y;
                    if (dx * dx + dy * dy > radio2) continue;

                    // Una meseta solo tiene sentido sobre tierra firme: elevar agua
                    // dibujaría un acantilado flotando en el mar.
                    if (alto && !_mapa.Tierra[x, y]) continue;

                    _alturas[x, y] = alto ? (byte)1 : (byte)0;
                    _sucio = true;
                }
            }
        }

        void PintarRampa(Vector2Int pie, bool borrando)
        {
            if (borrando)
            {
                // La cuesta ocupa dos celdas y se ve como una sola pieza, así que hay que
                // aceptar el clic en cualquiera de las dos. Exigir la de abajo obligaba a
                // adivinar dónde estaba el pie y parecía que el borrado no funcionaba.
                int quitadas = _rampas.RemoveAll(
                    r => r.x == pie.x && (r.y == pie.y || r.y == pie.y - 1));

                if (quitadas > 0) _sucio = true;
                return;
            }

            if (!EsPieValido(pie))
            {
                Debug.LogWarning(
                    "[Tiny Tactics] Ahí no cabe una rampa. El pie tiene que ser tierra llana " +
                    "con meseta justo encima.");
                return;
            }

            bool derecha = _sentido == Sentido.Automatico
                ? GeneradorTerreno.SentidoAutomatico(_alturas, _ancho, _alto, pie.x, pie.y)
                : _sentido == Sentido.Derecha;

            // Volver a hacer clic en la misma celda cambia el sentido en vez de duplicar.
            for (int i = 0; i < _rampas.Count; i++)
            {
                if (_rampas[i].x != pie.x || _rampas[i].y != pie.y) continue;
                if (_rampas[i].derecha == derecha) return;

                _rampas[i] = new Rampa(pie.x, pie.y, derecha);
                _sucio = true;
                return;
            }

            _rampas.Add(new Rampa(pie.x, pie.y, derecha));
            _sucio = true;
        }

        /// <summary>El pie es llano de tierra y justo encima hay meseta.</summary>
        bool EsPieValido(Vector2Int pie)
        {
            if (pie.x < 0 || pie.y < 0 || pie.x >= _ancho || pie.y + 1 >= _alto) return false;
            if (_alturas[pie.x, pie.y] != 0 || _alturas[pie.x, pie.y + 1] == 0) return false;

            return _mapa.Tierra[pie.x, pie.y];
        }

        // -----------------------------------------------------------------

        void Repintar()
        {
            if (_mapa == null) return;

            for (int x = 0; x < _ancho; x++)
                for (int y = 0; y < _alto; y++)
                    _mapa.Nivel[x, y] = _alturas[x, y];

            _mapa.Rampas.Clear();
            _mapa.Rampas.AddRange(_rampas);
            GeneradorTerreno.MarcarEscaleras(_mapa);

            ConstructorDeMapa.RepintarRelieveEnEscena(_mapa);
            SceneView.RepaintAll();
        }

        void Guardar()
        {
            // La ventana puede llevar abierta desde antes de regenerar la escena, y
            // entonces la referencia a la definición apunta a un objeto ya destruido.
            // Guardar sobre eso escribía en el vacío sin decir nada.
            if (_definicion == null)
            {
                Enganchar();

                if (_definicion == null)
                {
                    Debug.LogError(
                        "[Tiny Tactics] No hay definición de mapa a la que enlazar el relieve. " +
                        "Abre la escena de juego y pulsa Recargar.");
                    return;
                }
            }

            Repintar();

            var relieve = AssetDatabase.LoadAssetAtPath<RelieveMapa>(RutaRelieve);
            if (relieve == null)
            {
                relieve = CreateInstance<RelieveMapa>();
                AssetDatabase.CreateAsset(relieve, RutaRelieve);
            }

            relieve.mapa = _definicion != null ? _definicion.nombreMapa : "";
            relieve.Guardar(_alturas, _rampas, _ancho, _alto);

            EditorUtility.SetDirty(relieve);

            // Enlazarlo solo aquí y no al crearlo: mientras no lo guardes a propósito, el
            // generador sigue proponiendo relieve por ruido.
            if (_definicion != null && _definicion.relieve != relieve)
            {
                _definicion.relieve = relieve;
                EditorUtility.SetDirty(_definicion);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _relieve = relieve;
            _sucio = false;

            // Se relee del disco en vez de dar por bueno lo que hay en memoria. Si algo no
            // llegó a escribirse, es aquí donde se ve, y no tres pasos más tarde cuando la
            // escena aparece sin los cambios y parece que el editor no sirve.
            var comprobacion = AssetDatabase.LoadAssetAtPath<RelieveMapa>(RutaRelieve);
            int elevadas = 0;

            if (comprobacion != null && comprobacion.nivel != null)
                foreach (byte n in comprobacion.nivel)
                    if (n > 0) elevadas++;

            bool enlazado = _definicion != null && _definicion.relieve == comprobacion;

            Debug.Log(
                $"[Tiny Tactics] Relieve guardado en {RutaRelieve}\n" +
                $"Releído del disco: {elevadas} tiles de meseta, " +
                $"{(comprobacion != null ? comprobacion.rampas.Count : 0)} rampas.\n" +
                $"Enlazado en \"{(_definicion != null ? _definicion.name : "?")}\": " +
                $"{(enlazado ? "sí" : "NO — revísalo a mano en el Inspector")}");

            Selection.activeObject = _relieve;
        }
    }
}
