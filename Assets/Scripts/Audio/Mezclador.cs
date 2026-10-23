using UnityEngine;

namespace TinyTactics.Audio
{
    /// <summary>Los canales que el jugador puede subir y bajar por separado.</summary>
    public enum Canal { Musica, Efectos, Ambiente, Interfaz }

    /// <summary>
    /// El volumen final de cada canal: general × canal, o nada si el sonido está apagado.
    /// </summary>
    /// <remarks>
    /// <b>Un mezclador propio y no un <c>AudioMixer</c> de Unity</b> (ADR-29). El mezclador del
    /// motor es un asset que solo se crea a mano desde el editor —no hay forma pública de
    /// crearlo ni de añadirle grupos por código— y en este proyecto toda la escena se genera
    /// con un script. Para lo que se pide, cuatro deslizadores, basta una multiplicación; los
    /// efectos del mezclador (ecualizador, «ducking») no se usan.
    /// </remarks>
    public static class Mezclador
    {
        public static float Volumen(Canal canal) =>
            Nucleo.Opciones.Silencio ? 0f : Nucleo.Opciones.Volumen * Nucleo.Opciones.VolumenDe(canal);
    }

    /// <summary>
    /// Cuándo suena la música de combate. Puro, para probarlo fuera de Unity.
    /// </summary>
    /// <remarks>
    /// La regla es la del oído (ADR-25): <b>solo cuenta la pelea que se ve</b>. Un golpe que
    /// el filtro de cámara y niebla deja sonar es un golpe visto. Entra con unos cuantos
    /// golpes seguidos —un arquero suelto contra una oveja no es una batalla— y sale tras un
    /// rato de calma, con un mínimo de permanencia para que la música no vaya y venga con
    /// cada escaramuza.
    /// </remarks>
    public class ReglasDeMusica
    {
        /// <summary>Golpes vistos dentro de <see cref="ventana"/> para que empiece el combate.</summary>
        public int golpesParaEntrar = 4;

        /// <summary>Segundos en los que se cuentan los golpes.</summary>
        public float ventana = 6f;

        /// <summary>Segundos sin ver golpes para volver a la música de partida.</summary>
        public float calmaParaSalir = 10f;

        /// <summary>Lo mínimo que dura la música de combate una vez entra.</summary>
        public float minimoEnCombate = 18f;

        readonly float[] _golpes = new float[16];
        int _cuantos, _siguiente;
        float _ultimoGolpe = -999f, _entro = -999f;

        public bool EnCombate { get; private set; }

        /// <summary>Un golpe que el jugador ha visto (u oído, que aquí es lo mismo).</summary>
        public void Golpe(float ahora)
        {
            _golpes[_siguiente] = ahora;
            _siguiente = (_siguiente + 1) % _golpes.Length;
            if (_cuantos < _golpes.Length) _cuantos++;
            _ultimoGolpe = ahora;
        }

        /// <summary>
        /// Un aviso que no se puede ignorar: atacan algo del jugador. Entra directamente.
        /// </summary>
        public void Alarma(float ahora)
        {
            _ultimoGolpe = ahora;
            if (!EnCombate) { EnCombate = true; _entro = ahora; }
        }

        public bool Actualizar(float ahora)
        {
            if (!EnCombate)
            {
                int recientes = 0;
                for (int i = 0; i < _cuantos; i++)
                    if (ahora - _golpes[i] <= ventana) recientes++;
                if (recientes >= golpesParaEntrar) { EnCombate = true; _entro = ahora; }
            }
            else if (ahora - _ultimoGolpe >= calmaParaSalir && ahora - _entro >= minimoEnCombate)
            {
                EnCombate = false;
                _cuantos = 0;
            }

            return EnCombate;
        }

        /// <summary>
        /// Un paso de fundido: acerca <paramref name="actual"/> a <paramref name="objetivo"/>
        /// en <paramref name="segundos"/> para recorrer de 0 a 1.
        /// </summary>
        public static float Fundir(float actual, float objetivo, float segundos, float dt) =>
            segundos <= 0f ? objetivo : Mathf.MoveTowards(actual, objetivo, dt / segundos);
    }
}
