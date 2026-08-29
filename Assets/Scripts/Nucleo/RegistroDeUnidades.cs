using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Unidades;

namespace TinyTactics.Nucleo
{
    /// <summary>
    /// Índice espacial de todas las unidades vivas.
    ///
    /// Implementa el ADR-04. Preguntar "¿qué unidades tengo cerca?" recorriendo la lista
    /// completa es O(n²): con 250 unidades son 62 500 comparaciones <b>por frame</b> solo
    /// para el empuje. Con una rejilla de cubos se consultan tres celdas y punto.
    ///
    /// El tamaño de cubo debe ser algo mayor que el radio de separación más grande;
    /// si es menor, dos unidades vecinas podrían caer en cubos no adyacentes.
    /// </summary>
    public static class RegistroDeUnidades
    {
        const float LadoCubo = 2f;

        static readonly List<Unidad> _todas = new List<Unidad>();
        static readonly Dictionary<long, List<Unidad>> _cubos = new Dictionary<long, List<Unidad>>();

        public static IReadOnlyList<Unidad> Todas => _todas;

        public static void Registrar(Unidad u)
        {
            if (!_todas.Contains(u)) _todas.Add(u);
        }

        public static void Olvidar(Unidad u)
        {
            _todas.Remove(u);
        }

        /// <summary>Reconstruye el índice. Se llama una vez por frame, no por consulta.</summary>
        public static void Reconstruir()
        {
            foreach (var lista in _cubos.Values) lista.Clear();

            for (int i = 0; i < _todas.Count; i++)
            {
                var u = _todas[i];
                if (u == null) continue;

                long clave = Clave(u.transform.position);
                if (!_cubos.TryGetValue(clave, out var lista))
                {
                    lista = new List<Unidad>(8);
                    _cubos[clave] = lista;
                }
                lista.Add(u);
            }
        }

        /// <summary>Vecinos en los nueve cubos que rodean un punto.</summary>
        /// <summary>
        /// Vecinas dentro de un radio concreto, no solo las del cubo de al lado.
        ///
        /// <see cref="Vecinas"/> mira un bloque de 3x3 cubos, o sea que solo garantiza
        /// radio 2. Pedirle mas era pedirle lo que no puede dar: el ataque automatico
        /// buscaba a 5,5 tiles y no encontraba nada mas alla de 2. Aqui el bloque se
        /// agranda hasta cubrir el radio pedido.
        /// </summary>
        public static void VecinasEnRadio(Vector3 punto, float radio, List<Unidad> salida)
        {
            salida.Clear();

            int alcance = Mathf.Max(1, Mathf.CeilToInt(radio / LadoCubo));
            int cx = Mathf.FloorToInt(punto.x / LadoCubo);
            int cy = Mathf.FloorToInt(punto.y / LadoCubo);

            for (int dx = -alcance; dx <= alcance; dx++)
            {
                for (int dy = -alcance; dy <= alcance; dy++)
                {
                    if (_cubos.TryGetValue(Clave(cx + dx, cy + dy), out var lista))
                        salida.AddRange(lista);
                }
            }
        }

        public static void Vecinas(Vector3 punto, List<Unidad> salida)
        {
            salida.Clear();

            int cx = Mathf.FloorToInt(punto.x / LadoCubo);
            int cy = Mathf.FloorToInt(punto.y / LadoCubo);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (_cubos.TryGetValue(Clave(cx + dx, cy + dy), out var lista))
                        salida.AddRange(lista);
                }
            }
        }

        static long Clave(Vector3 p) =>
            Clave(Mathf.FloorToInt(p.x / LadoCubo), Mathf.FloorToInt(p.y / LadoCubo));

        static long Clave(int x, int y) => ((long)x << 32) ^ (uint)y;

        /// <summary>Al cambiar de escena las referencias viejas quedan colgando.</summary>
        public static void Limpiar()
        {
            _todas.Clear();
            _cubos.Clear();
        }
    }
}
