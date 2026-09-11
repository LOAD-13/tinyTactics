using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Datos;

namespace TinyTactics.Unidades
{
    /// <summary>
    /// Una copia apagada de cada unidad, por tipo y por bando, lista para clonar.
    ///
    /// <b>Por qué existe.</b> Las animaciones se resuelven a sprites en el editor, con
    /// herramientas que no existen al ejecutar: una unidad construida en caliente saldría
    /// muda de dibujos. La plantilla ya los lleva serializados.
    ///
    /// <b>Por qué está centralizado y no una plantilla por edificio.</b> El cuartel entrena
    /// guerreros y lanceros, el campo de tiro arqueros y el castillo pawns. Con plantillas
    /// por edificio habría copias repetidas de la misma unidad en varios sitios, y bastaría
    /// con regenerar mal una para que dos guerreros del mismo bando no fueran iguales. Aquí
    /// hay exactamente una por tipo y bando, y todos los edificios piden a la misma.
    /// </summary>
    [AddComponentMenu("Tiny Tactics/Catálogo de plantillas")]
    public class CatalogoDePlantillas : MonoBehaviour
    {
        [System.Serializable]
        public class Entrada
        {
            public TipoUnidad tipo;
            public int faccion;
            public GameObject plantilla;
        }

        [SerializeField] List<Entrada> _entradas = new List<Entrada>();

        public static CatalogoDePlantillas Actual { get; private set; }

        void Awake() => Actual = this;

        void OnDestroy()
        {
            if (Actual == this) Actual = null;
        }

        /// <summary>La llama el generador de la escena.</summary>
        public void Registrar(TipoUnidad tipo, int faccion, GameObject plantilla)
        {
            _entradas.Add(new Entrada { tipo = tipo, faccion = faccion, plantilla = plantilla });
        }

        public GameObject Obtener(TipoUnidad tipo, int faccion)
        {
            for (int i = 0; i < _entradas.Count; i++)
            {
                var e = _entradas[i];
                if (e != null && e.tipo == tipo && e.faccion == faccion) return e.plantilla;
            }

            return null;
        }
    }
}
