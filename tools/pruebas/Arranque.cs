// Arnes de pruebas del proyecto. Se compila junto al juego y se ejecuta fuera de Unity.
//
// No usa NUnit a proposito: NUnit vive dentro del Test Runner de Unity, y el Test Runner
// exige que las pruebas esten en un ensamblado con .asmdef — que no puede referenciar
// Assembly-CSharp, donde esta todo el codigo del juego. Cuarenta lineas de arnes propio
// resuelven el problema entero sin tocar la estructura del proyecto.
//
// Ver tools/probar.py para el porque largo y para lo que estas pruebas NO cubren.
using System;
using System.Collections.Generic;

namespace TinyTactics.Pruebas
{
    /// <summary>Un bloque de comprobaciones con nombre.</summary>
    public delegate void Suite(Verificador v);

    /// <summary>
    /// Lo que las pruebas usan para comprobar. Acumula fallos en vez de cortar al primero:
    /// una tanda que se para en el primer error obliga a correrla cinco veces para ver
    /// cinco problemas.
    /// </summary>
    public class Verificador
    {
        public readonly List<string> Fallos = new List<string>();
        public int Comprobaciones;

        string _bloque = "";

        public void Bloque(string nombre) => _bloque = nombre;

        void Anotar(string mensaje) => Fallos.Add($"{_bloque}: {mensaje}");

        public void Cierto(bool condicion, string que)
        {
            Comprobaciones++;
            if (!condicion) Anotar($"{que} — se esperaba cierto");
        }

        public void Falso(bool condicion, string que)
        {
            Comprobaciones++;
            if (condicion) Anotar($"{que} — se esperaba falso");
        }

        public void Igual(int obtenido, int esperado, string que)
        {
            Comprobaciones++;
            if (obtenido != esperado) Anotar($"{que} — esperaba {esperado} y salio {obtenido}");
        }

        public void Igual(string obtenido, string esperado, string que)
        {
            Comprobaciones++;
            if (obtenido != esperado) Anotar($"{que} — esperaba «{esperado}» y salio «{obtenido}»");
        }

        public void Cerca(float obtenido, float esperado, float holgura, string que)
        {
            Comprobaciones++;
            if (Math.Abs(obtenido - esperado) > holgura)
                Anotar($"{que} — esperaba {esperado} (±{holgura}) y salio {obtenido}");
        }

        public void Entre(float obtenido, float minimo, float maximo, string que)
        {
            Comprobaciones++;
            if (obtenido < minimo || obtenido > maximo)
                Anotar($"{que} — esperaba entre {minimo} y {maximo}, salio {obtenido}");
        }
    }

    public static class Arranque
    {
        static readonly List<(string nombre, Suite suite)> Suites = new List<(string, Suite)>();

        public static void Registrar(string nombre, Suite suite) => Suites.Add((nombre, suite));

        public static int Main()
        {
            PruebasDeVisibilidad.Registrar();
            PruebasDeIA.Registrar();

            var v = new Verificador();
            int rotas = 0;

            foreach (var (nombre, suite) in Suites)
            {
                v.Bloque(nombre);
                int antes = v.Fallos.Count;

                try
                {
                    suite(v);
                }
                catch (Exception e)
                {
                    v.Fallos.Add($"{nombre}: EXCEPCION — {e.GetType().Name}: {e.Message}");
                }

                bool bien = v.Fallos.Count == antes;
                if (!bien) rotas++;

                Console.WriteLine($"  {(bien ? "ok  " : "FALLA")}  {nombre}");
            }

            Console.WriteLine();

            if (v.Fallos.Count == 0)
            {
                Console.WriteLine($"Todo bien: {Suites.Count} bloques, "
                                  + $"{v.Comprobaciones} comprobaciones.");
                return 0;
            }

            Console.WriteLine($"{v.Fallos.Count} fallo(s) en {rotas} de {Suites.Count} bloques:");
            Console.WriteLine();

            foreach (var f in v.Fallos) Console.WriteLine("  - " + f);

            return 1;
        }
    }
}
