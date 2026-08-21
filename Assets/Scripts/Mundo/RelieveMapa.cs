using System.Collections.Generic;
using UnityEngine;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// El relieve de un mapa, guardado a mano.
    ///
    /// El generador propone mesetas por ruido, pero el resultado es solo un punto de
    /// partida: dónde va un acantilado es una decisión de diseño de nivel, no de
    /// estadística. Cuando existe uno de estos assets, el generador deja de inventar y
    /// pinta exactamente lo que hay aquí — así el escenario deja de cambiar en cada
    /// regeneración de la escena.
    ///
    /// Solo se guardan las alturas y las rampas. La costa, los recursos y la decoración
    /// siguen saliendo de la semilla, que es determinista: mismo mapa, mismo terreno.
    /// </summary>
    [CreateAssetMenu(fileName = "Relieve", menuName = "Tiny Tactics/Relieve de mapa")]
    public class RelieveMapa : ScriptableObject
    {
        [Tooltip("Mapa al que pertenece. Solo informativo, para no confundir assets.")]
        public string mapa;

        public int ancho;
        public int alto;

        [Tooltip("Altura por celda, en fila mayor. Se guarda plano porque Unity no " +
                 "serializa arrays de dos dimensiones.")]
        public byte[] nivel;

        [Tooltip("Cuestas dibujadas. Cada una ocupa un tile de ancho por dos de alto.")]
        public List<Rampa> rampas = new List<Rampa>();

        public bool Coincide(int otroAncho, int otroAlto) =>
            nivel != null && nivel.Length == otroAncho * otroAlto &&
            ancho == otroAncho && alto == otroAlto;

        public byte NivelEn(int x, int y) =>
            x >= 0 && y >= 0 && x < ancho && y < alto ? nivel[x + y * ancho] : (byte)0;

        public void Guardar(byte[,] alturas, List<Rampa> nuevasRampas, int otroAncho, int otroAlto)
        {
            ancho = otroAncho;
            alto = otroAlto;
            nivel = new byte[ancho * alto];

            for (int x = 0; x < ancho; x++)
                for (int y = 0; y < alto; y++)
                    nivel[x + y * ancho] = alturas[x, y];

            rampas = new List<Rampa>(nuevasRampas);
        }
    }
}
