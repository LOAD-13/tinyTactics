using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TinyTactics.Mundo
{
    /// <summary>
    /// El filtro de color de un mapa: tono, saturación y brillo sobre toda la imagen.
    /// </summary>
    /// <remarks>
    /// Es un ajuste de color de posprocesado de URP, no un tinte de sprites: así afecta por
    /// igual al terreno, a las unidades y a los efectos, y el editor lo enseña en vivo mientras
    /// se mueven las barras. Un mapa en tonos fríos y desaturados se siente invernal; uno cálido
    /// y saturado, de verano. Con las mismas piezas del pack, cada mapa tiene su personalidad.
    ///
    /// La interfaz no se ve afectada: va en un lienzo superpuesto que se dibuja después.
    /// </remarks>
    public static class FiltroDeMapa
    {
        static Volume _volumen;
        static ColorAdjustments _ajuste;

        public static void Aplicar(AmbienteDeMapa a)
        {
            if (!Preparar()) return;

            bool neutro = a.tono == Color.white && Mathf.Approximately(a.saturacion, 1f) &&
                          Mathf.Approximately(a.brillo, 1f);
            _volumen.weight = neutro ? 0f : 1f;

            _ajuste.colorFilter.Override(a.tono);
            _ajuste.saturation.Override(Mathf.Clamp((a.saturacion - 1f) * 100f, -100f, 100f));
            _ajuste.postExposure.Override(Mathf.Log(Mathf.Max(0.05f, a.brillo), 2f));
        }

        static bool Preparar()
        {
            if (_volumen != null) return true;

            var camara = Camera.main;
            if (camara == null) return false;

            // Sin posprocesado activado en la cámara, el volumen existe pero no se ve.
            var datos = camara.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;

            var go = new GameObject("Filtro del mapa");
            _volumen = go.AddComponent<Volume>();
            _volumen.isGlobal = true;
            _volumen.priority = 10f;

            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            _ajuste = perfil.Add<ColorAdjustments>(true);
            _volumen.sharedProfile = perfil;
            return true;
        }
    }
}
