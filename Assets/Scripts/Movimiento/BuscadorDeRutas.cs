using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Mundo;

namespace TinyTactics.Movimiento
{
    /// <summary>
    /// A* sobre la grilla del mapa, con las peticiones en cola.
    ///
    /// La cola no es un adorno (ADR-05): dar una orden de movimiento a 50 unidades
    /// seleccionadas lanzaría 50 búsquedas en el mismo frame y produciría un tirón
    /// visible. Repartirlas en varios frames es imperceptible para el jugador.
    ///
    /// Los arreglos de trabajo se reservan una sola vez y se reutilizan mediante un
    /// sello de visita: limpiar 50 000 casillas en cada búsqueda costaría más que la
    /// búsqueda misma.
    /// </summary>
    public class BuscadorDeRutas
    {
        const int Recto = 10;      // coste de un paso ortogonal
        const int Diagonal = 14;   // ~10 * raíz de 2

        readonly GrillaMapa _grilla;
        readonly int _ancho, _alto, _total;

        readonly int[] _costeG;
        readonly int[] _costeF;
        readonly int[] _padre;
        readonly int[] _sello;
        readonly bool[] _cerrada;

        readonly MonticuloBinario _abierta;
        int _generacion;

        /// <summary>Rutas resueltas por llamada a <see cref="Procesar"/>.</summary>
        public int RutasPorFrame = 8;

        readonly Queue<Peticion> _cola = new Queue<Peticion>();

        public int PendientesEnCola => _cola.Count;

        class Peticion
        {
            public Vector2Int Desde;
            public Vector2Int Hasta;
            public Action<List<Vector2Int>> AlTerminar;
        }

        public BuscadorDeRutas(GrillaMapa grilla)
        {
            _grilla = grilla;
            _ancho = grilla.Ancho;
            _alto = grilla.Alto;
            _total = _ancho * _alto;

            _costeG = new int[_total];
            _costeF = new int[_total];
            _padre = new int[_total];
            _sello = new int[_total];
            _cerrada = new bool[_total];
            _abierta = new MonticuloBinario(_total);
        }

        // -----------------------------------------------------------------
        // Cola
        // -----------------------------------------------------------------

        public void PedirRuta(Vector2Int desde, Vector2Int hasta, Action<List<Vector2Int>> alTerminar)
        {
            _cola.Enqueue(new Peticion { Desde = desde, Hasta = hasta, AlTerminar = alTerminar });
        }

        /// <summary>Resuelve hasta <see cref="RutasPorFrame"/> peticiones. Llamar una vez por frame.</summary>
        public int Procesar()
        {
            int hechas = 0;
            while (hechas < RutasPorFrame && _cola.Count > 0)
            {
                var p = _cola.Dequeue();
                p.AlTerminar?.Invoke(Buscar(p.Desde, p.Hasta));
                hechas++;
            }
            return hechas;
        }

        // -----------------------------------------------------------------
        // Búsqueda
        // -----------------------------------------------------------------

        /// <summary>
        /// Ruta de celda a celda. Devuelve lista vacía si no existe camino.
        /// El primer elemento es la celda siguiente, no la de partida.
        /// </summary>
        public List<Vector2Int> Buscar(Vector2Int desde, Vector2Int hasta)
        {
            var ruta = new List<Vector2Int>();

            if (!_grilla.EnRango(desde.x, desde.y)) return ruta;

            // Ordenar moverse al agua o sobre un árbol no debe cancelar la orden:
            // la unidad va a lo más cerca que pueda.
            if (!_grilla.Transitable(hasta.x, hasta.y))
            {
                if (!_grilla.CeldaTransitableCercana(hasta, 12, out hasta)) return ruta;
            }

            if (desde == hasta) return ruta;

            _generacion++;
            _abierta.Limpiar();

            int inicio = Indice(desde);
            int meta = Indice(hasta);

            Preparar(inicio);
            _costeG[inicio] = 0;
            _costeF[inicio] = Heuristica(desde, hasta);
            _padre[inicio] = -1;
            _abierta.Insertar(inicio, _costeF[inicio]);

            while (_abierta.Cantidad > 0)
            {
                int actual = _abierta.Extraer();
                if (actual == meta) return Reconstruir(inicio, meta);

                _cerrada[actual] = true;

                int ax = actual % _ancho;
                int ay = actual / _ancho;

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = ax + dx, ny = ay + dy;
                        if (!_grilla.EnRango(nx, ny)) continue;
                        if (!_grilla.PuedePasar(ax, ay, nx, ny)) continue;

                        int vecino = nx + ny * _ancho;
                        Preparar(vecino);
                        if (_cerrada[vecino]) continue;

                        int paso = (dx != 0 && dy != 0) ? Diagonal : Recto;

                        // Subir una rampa cuesta más: las unidades prefieren rodear si
                        // el rodeo es corto, que es como se comportan en un RTS real.
                        if (_grilla.NivelDe(ax, ay) != _grilla.NivelDe(nx, ny)) paso += 6;

                        int nuevoG = _costeG[actual] + paso;

                        if (nuevoG >= _costeG[vecino]) continue;

                        _costeG[vecino] = nuevoG;
                        _costeF[vecino] = nuevoG + Heuristica(new Vector2Int(nx, ny), hasta);
                        _padre[vecino] = actual;

                        if (_abierta.Contiene(vecino)) _abierta.Actualizar(vecino, _costeF[vecino]);
                        else _abierta.Insertar(vecino, _costeF[vecino]);
                    }
                }
            }

            return ruta;   // sin camino
        }

        /// <summary>Inicializa la casilla si no se ha tocado en esta búsqueda.</summary>
        void Preparar(int indice)
        {
            if (_sello[indice] == _generacion) return;

            _sello[indice] = _generacion;
            _costeG[indice] = int.MaxValue;
            _costeF[indice] = int.MaxValue;
            _padre[indice] = -1;
            _cerrada[indice] = false;
        }

        int Indice(Vector2Int c) => c.x + c.y * _ancho;

        /// <summary>
        /// Distancia octile: la exacta para movimiento en 8 direcciones.
        /// Manhattan sobreestimaría en diagonal y dejaría de ser admisible,
        /// devolviendo rutas que no son las más cortas.
        /// </summary>
        static int Heuristica(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx > dy
                ? Diagonal * dy + Recto * (dx - dy)
                : Diagonal * dx + Recto * (dy - dx);
        }

        List<Vector2Int> Reconstruir(int inicio, int meta)
        {
            var ruta = new List<Vector2Int>();

            int actual = meta;
            while (actual != inicio && actual != -1)
            {
                ruta.Add(new Vector2Int(actual % _ancho, actual / _ancho));
                actual = _padre[actual];
            }

            ruta.Reverse();
            return ruta;
        }

        // -----------------------------------------------------------------
        // Montículo binario indexado
        // -----------------------------------------------------------------

        /// <summary>
        /// Cola de prioridad con índice inverso, para poder bajar la prioridad de un
        /// nodo ya insertado sin recorrer el montículo entero.
        /// </summary>
        class MonticuloBinario
        {
            readonly int[] _datos;
            readonly int[] _prioridad;
            readonly int[] _posicion;   // índice de celda -> posición en el montículo
            int _cantidad;

            public int Cantidad => _cantidad;

            public MonticuloBinario(int capacidad)
            {
                _datos = new int[capacidad];
                _prioridad = new int[capacidad];
                _posicion = new int[capacidad];
                for (int i = 0; i < capacidad; i++) _posicion[i] = -1;
            }

            public void Limpiar()
            {
                for (int i = 0; i < _cantidad; i++) _posicion[_datos[i]] = -1;
                _cantidad = 0;
            }

            public bool Contiene(int valor) => _posicion[valor] >= 0;

            public void Insertar(int valor, int prioridad)
            {
                int i = _cantidad++;
                _datos[i] = valor;
                _prioridad[i] = prioridad;
                _posicion[valor] = i;
                Subir(i);
            }

            public void Actualizar(int valor, int prioridad)
            {
                int i = _posicion[valor];
                if (i < 0) return;
                _prioridad[i] = prioridad;
                Subir(i);
            }

            public int Extraer()
            {
                int raiz = _datos[0];
                _posicion[raiz] = -1;
                _cantidad--;

                if (_cantidad > 0)
                {
                    _datos[0] = _datos[_cantidad];
                    _prioridad[0] = _prioridad[_cantidad];
                    _posicion[_datos[0]] = 0;
                    Bajar(0);
                }

                return raiz;
            }

            void Subir(int i)
            {
                while (i > 0)
                {
                    int padre = (i - 1) / 2;
                    if (_prioridad[i] >= _prioridad[padre]) break;
                    Intercambiar(i, padre);
                    i = padre;
                }
            }

            void Bajar(int i)
            {
                while (true)
                {
                    int izq = 2 * i + 1, der = 2 * i + 2, menor = i;

                    if (izq < _cantidad && _prioridad[izq] < _prioridad[menor]) menor = izq;
                    if (der < _cantidad && _prioridad[der] < _prioridad[menor]) menor = der;
                    if (menor == i) break;

                    Intercambiar(i, menor);
                    i = menor;
                }
            }

            void Intercambiar(int a, int b)
            {
                (_datos[a], _datos[b]) = (_datos[b], _datos[a]);
                (_prioridad[a], _prioridad[b]) = (_prioridad[b], _prioridad[a]);
                _posicion[_datos[a]] = a;
                _posicion[_datos[b]] = b;
            }
        }
    }
}
