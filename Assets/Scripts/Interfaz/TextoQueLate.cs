using UnityEngine;
using UnityEngine.UI;

namespace TinyTactics.Interfaz
{
    /// <summary>Un texto que late despacio: «Pulsa cualquier tecla».</summary>
    [AddComponentMenu("Tiny Tactics/Texto que late")]
    public class TextoQueLate : MonoBehaviour
    {
        Graphic _g;

        void OnEnable() => _g = GetComponent<Graphic>();

        void Update()
        {
            if (_g == null) return;

            var c = _g.color;
            c.a = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
            _g.color = c;
        }
    }
}
