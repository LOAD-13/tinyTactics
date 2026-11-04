using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El historial de partidas: tus últimas veinte, y cómo te va con ellas (semana 16).
    /// </summary>
    /// <remarks>
    /// <b>Una crónica sobre la mesa de guerra.</b> Cada partida es una caja del pack —azul si
    /// ganaste, roja si perdiste—, con la cara de la unidad que más bajas hizo y las de todos
    /// los bandos que jugaron. A la derecha, lo que el jugador quiere saber de un vistazo:
    /// cuánto gana, si viene en racha y contra qué bot le va mejor.
    /// <para>
    /// Se relee del disco cada vez que se abre: así la partida que acabas de jugar ya está.
    /// </para>
    /// </remarks>
    public partial class MenuPrincipal
    {
        GameObject _vistaLista, _vacio;
        RectTransform _filasHistorial, _resumen;
        Text _cabeceraHistorial;
        Image _cintaHistorial;
        List<InformeDePartida> _partidas = new List<InformeDePartida>();

        static readonly Color Oro = new Color(1f, 0.86f, 0.42f);
        static readonly Color Salmon = new Color(1f, 0.70f, 0.62f);

        GameObject ConstruirHistorial(RectTransform raiz)
        {
            var p = KitInterfaz.Llenar(KitInterfaz.Hijo(raiz, "Historial"));
            var mesa = Mesa(p, new Vector2(1720f, 960f), "Historial", Morado, out _cabeceraHistorial, out _cintaHistorial);

            _vistaLista = KitInterfaz.Llenar(KitInterfaz.Hijo(mesa, "Lista")).gameObject;
            var lista = (RectTransform)_vistaLista.transform;

            // La lista, con desplazamiento: veinte filas no caben en una pantalla.
            var vista = KitInterfaz.Hijo(lista, "Vista");
            KitInterfaz.Colocar(vista, new Vector2(0.5f, 0.5f), new Vector2(1090f, 700f), new Vector2(-285f, 10f));
            vista.gameObject.AddComponent<RectMask2D>();
            var fondo = vista.gameObject.AddComponent<Image>();
            fondo.color = new Color(0f, 0f, 0f, 0.18f);

            _filasHistorial = KitInterfaz.Hijo(vista, "Filas");
            _filasHistorial.anchorMin = new Vector2(0f, 1f);
            _filasHistorial.anchorMax = new Vector2(1f, 1f);
            _filasHistorial.pivot = new Vector2(0.5f, 1f);
            _filasHistorial.sizeDelta = Vector2.zero;

            var columna = _filasHistorial.gameObject.AddComponent<VerticalLayoutGroup>();
            columna.spacing = 8f;
            columna.padding = new RectOffset(10, 10, 10, 10);
            columna.childControlWidth = columna.childControlHeight = true;
            columna.childForceExpandWidth = true;
            columna.childForceExpandHeight = false;
            _filasHistorial.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rueda = vista.gameObject.AddComponent<ScrollRect>();
            rueda.content = _filasHistorial;
            rueda.viewport = vista;
            rueda.horizontal = false;
            rueda.movementType = ScrollRect.MovementType.Clamped;
            rueda.scrollSensitivity = 45f;

            _resumen = Contenedor(lista, "Resumen", Morado, new Vector2(0.5f, 0.5f), new Vector2(510f, 700f),
                                  new Vector2(560f, 10f));

            ConstruirVacio(mesa);
            ConstruirDetalle(mesa);

            BotonDeAccion(mesa, "Volver", Rojo, VolverDelHistorial, new Vector2(0.5f, 0f),
                          new Vector2(300f, 84f), new Vector2(0f, 20f));

            return p.gameObject;
        }

        void VolverDelHistorial()
        {
            if (_detalle != null && _detalle.activeSelf) CerrarDetalle();
            else Ir(Pantalla.Principal);
        }

        /// <summary>Sin partidas todavía: el capataz del tutorial lo explica.</summary>
        void ConstruirVacio(RectTransform mesa)
        {
            var papel = KitInterfaz.Imagen(mesa, "Vacio", KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(papel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(980f, 330f), new Vector2(0f, 10f));
            _vacio = papel.gameObject;

            var marco = KitInterfaz.Imagen(papel.transform, "Marco", Caja(Amarillo), Color.white);
            KitInterfaz.Colocar(marco.rectTransform, new Vector2(0f, 0.5f), new Vector2(200f, 200f), new Vector2(190f, 0f));
            var cara = KitInterfaz.Imagen(marco.transform, "Cara", tema != null ? tema.RetratoDeColor(TipoUnidad.Pawn, Amarillo) : null, Color.white);
            cara.preserveAspect = true;
            KitInterfaz.Colocar(cara.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(130f, 130f));

            var t = KitInterfaz.TextoQueCabe(papel.transform, "Texto", _fuente, 32, 18, KitInterfaz.Tinta, TextAnchor.MiddleLeft);
            t.GetComponent<Outline>().enabled = false;
            t.text = "Todavía no hay crónicas.\nJuega una partida y aquí quedará cómo te fue: quién ganó, " +
                     "cuánto recolectaste y cuándo empezó la pelea.";
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(560f, 200f), new Vector2(620f, 0f));
        }

        void PintarHistorial()
        {
            CerrarDetalle();
            _partidas = HistorialDePartidas.Listar();

            bool hay = _partidas.Count > 0;
            _vacio.SetActive(!hay);
            _vistaLista.SetActive(hay);

            for (int i = _filasHistorial.childCount - 1; i >= 0; i--)
                Destroy(_filasHistorial.GetChild(i).gameObject);

            if (!hay) return;

            for (int i = 0; i < _partidas.Count; i++) FilaDePartida(_partidas[i]);

            PintarResumen(LecturaDeHistorial.Resumir(_partidas), _partidas.Count);
        }

        static string Resultado(InformeDePartida i) =>
            !LecturaDeHistorial.EsDelJugador(i) ? "Prueba"
            : i.abandonada ? "Abandonada"
            : i.desenlace == Desenlace.Victoria ? "Victoria" : "Derrota";

        static int ColorDeResultado(InformeDePartida i) =>
            !LecturaDeHistorial.EsDelJugador(i) || i.abandonada ? Negro
            : i.desenlace == Desenlace.Victoria ? Azul : Rojo;

        /// <summary>Cara de la unidad que más bajas hizo; un pawn si nadie cayó.</summary>
        Sprite CaraDe(InformeDeBando b) =>
            tema != null ? tema.RetratoDeColor(b.bajasDelMvp > 0 ? b.mvp : TipoUnidad.Pawn, b.color) : null;

        void FilaDePartida(InformeDePartida informe)
        {
            var i = informe;
            var b = KitInterfaz.Boton(_filasHistorial, "Partida", Caja(ColorDeResultado(i)), "", _fuente, 10, () => AbrirDetalle(i));
            b.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
            var fila = (RectTransform)b.transform;

            // Resultado, en grande: lo primero que se lee.
            var res = KitInterfaz.Texto(fila, "Resultado", Resultado(i), _fuente, 36,
                                        i.desenlace == Desenlace.Victoria && !i.abandonada ? Oro : KitInterfaz.Crema,
                                        TextAnchor.MiddleLeft);
            KitInterfaz.Colocar(res.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f, 50f), new Vector2(140f, 14f));
            var modo = KitInterfaz.Texto(fila, "Modo", i.modo == ModoDePartida.Sandbox ? "Sandbox" : "Partida", _fuente, 20,
                                         KitInterfaz.Crema, TextAnchor.MiddleLeft);
            KitInterfaz.Colocar(modo.rectTransform, new Vector2(0f, 0.5f), new Vector2(210f, 30f), new Vector2(140f, -24f));

            // Tu cara: la de tu unidad estrella de esa partida, en tu color.
            var yo = i.DelJugador ?? i.bandos[0];
            var marco = KitInterfaz.Imagen(fila, "Tu", CajaChica(yo.color), Color.white);
            KitInterfaz.Colocar(marco.rectTransform, new Vector2(0f, 0.5f), new Vector2(84f, 84f), new Vector2(300f, 0f));
            var cara = KitInterfaz.Imagen(marco.transform, "Cara", CaraDe(yo), Color.white);
            cara.preserveAspect = true;
            KitInterfaz.Colocar(cara.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(64f, 64f));

            var mapa = KitInterfaz.TextoQueCabe(fila, "Mapa", _fuente, 30, 18, KitInterfaz.Crema, TextAnchor.MiddleLeft);
            mapa.text = i.mapa;
            KitInterfaz.Colocar(mapa.rectTransform, new Vector2(0f, 0.5f), new Vector2(250f, 40f), new Vector2(485f, 16f));
            var rival = LecturaDeHistorial.Dificultad(i);
            var sub = KitInterfaz.TextoQueCabe(fila, "Formato", _fuente, 20, 12, KitInterfaz.Crema, TextAnchor.MiddleLeft);
            sub.text = LecturaDeHistorial.Formato(i) +
                       (ReglasDeConfiguracion.EsBot(rival) ? " · " + ReglasDeConfiguracion.Nombre(rival).Replace("Bot · ", "") : "");
            KitInterfaz.Colocar(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(250f, 30f), new Vector2(485f, -22f));

            // Quién jugó: una caja por bando, como los objetos de una partida de otros juegos.
            for (int k = 0; k < i.bandos.Count && k < 5; k++)
            {
                var otro = i.bandos[k];
                var caja = KitInterfaz.Imagen(fila, "Bando" + k, CajaChica(otro.color), Color.white);
                KitInterfaz.Colocar(caja.rectTransform, new Vector2(0f, 0.5f), new Vector2(46f, 46f), new Vector2(640f + k * 48f, 0f));
                var c = KitInterfaz.Imagen(caja.transform, "Cara", CaraDe(otro), Color.white);
                c.preserveAspect = true;
                KitInterfaz.Colocar(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(34f, 34f));
            }

            Dato(fila, $"{yo.unidadesEliminadas} / {yo.unidadesPerdidas}", "bajas / caídas", 880f);
            Dato(fila, LecturaDeHistorial.Reloj(i.duracion), FechaCorta(i.fecha), 1000f);
        }

        /// <summary>«2026-11-03 18:40» → «03/11 · 18:40».</summary>
        static string FechaCorta(string fecha) =>
            fecha != null && fecha.Length >= 16 ? $"{fecha.Substring(8, 2)}/{fecha.Substring(5, 2)} · {fecha.Substring(11, 5)}" : fecha;

        void Dato(RectTransform fila, string grande, string chico, float x)
        {
            var g = KitInterfaz.Texto(fila, "Dato", grande, _fuente, 30, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(g.rectTransform, new Vector2(0f, 0.5f), new Vector2(112f, 40f), new Vector2(x, 14f));
            var c = KitInterfaz.TextoQueCabe(fila, "Pie", _fuente, 18, 11, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            c.text = chico;
            KitInterfaz.Colocar(c.rectTransform, new Vector2(0f, 0.5f), new Vector2(112f, 26f), new Vector2(x, -24f));
        }

        // -----------------------------------------------------------------
        // El resumen de la derecha
        // -----------------------------------------------------------------

        void PintarResumen(ResumenDeHistorial r, int cuantas)
        {
            for (int i = _resumen.childCount - 1; i >= 0; i--)
                Destroy(_resumen.GetChild(i).gameObject);

            Linea(_resumen, cuantas == 1 ? "Tu última partida" : $"Tus últimas {cuantas}", 34, KitInterfaz.Crema, -42f);

            if (r.Terminadas == 0)
            {
                Linea(_resumen, "Aquí verás tu porcentaje de\nvictorias, tu racha y cómo te va\ncontra cada bot.", 26,
                      KitInterfaz.Crema, -190f, 130f);
                Linea(_resumen, "Solo cuentan las partidas normales\nterminadas: el sandbox y las\npruebas con bot en tu bando, no.", 22,
                      Oro, -340f, 110f);
                return;
            }

            Linea(_resumen, $"{Mathf.RoundToInt(r.PorcentajeDeVictorias)} %", 88, Oro, -135f, 100f);
            Linea(_resumen, "de victorias", 24, KitInterfaz.Crema, -195f);
            Barra(_resumen, r.PorcentajeDeVictorias / 100f, new Vector2(420f, 44f), new Vector2(0f, -235f));

            string detalle = $"{r.Victorias} {(r.Victorias == 1 ? "victoria" : "victorias")} · " +
                             $"{r.Derrotas} {(r.Derrotas == 1 ? "derrota" : "derrotas")}";
            if (r.Abandonadas > 0) detalle += $" · {r.Abandonadas} sin terminar";
            Linea(_resumen, detalle, 22, KitInterfaz.Crema, -275f);

            int n = Mathf.Abs(r.Racha);
            string racha = r.Racha > 1 ? $"¡{n} victorias seguidas!"
                         : r.Racha < -1 ? $"{n} derrotas seguidas"
                         : r.Racha > 0 ? "Vienes de ganar" : "Vienes de perder";
            Linea(_resumen, racha, 28, r.Racha > 0 ? Oro : Salmon, -320f);

            Linea(_resumen, "Contra cada bot", 28, KitInterfaz.Crema, -380f);
            string[] niveles = { "Fácil", "Normal", "Difícil" };
            for (int k = 0; k < 3; k++)
            {
                float y = -428f - k * 46f;
                var e = KitInterfaz.Texto(_resumen, "Nivel", niveles[k], _fuente, 24, KitInterfaz.Crema, TextAnchor.MiddleLeft);
                KitInterfaz.Colocar(e.rectTransform, new Vector2(0.5f, 1f), new Vector2(120f, 36f), new Vector2(-160f, y));
                float parte = r.Jugadas[k] == 0 ? 0f : (float)r.Ganadas[k] / r.Jugadas[k];
                Barra(_resumen, parte, new Vector2(200f, 36f), new Vector2(15f, y));
                var v = KitInterfaz.Texto(_resumen, "Cuenta", r.Jugadas[k] == 0 ? "—" : $"{r.Ganadas[k]} de {r.Jugadas[k]}",
                                          _fuente, 22, KitInterfaz.Crema, TextAnchor.MiddleRight);
                KitInterfaz.Colocar(v.rectTransform, new Vector2(0.5f, 1f), new Vector2(110f, 36f), new Vector2(185f, y));
            }

            if (r.VecesEstrella == 0) return;

            Linea(_resumen, "Tu unidad estrella", 28, KitInterfaz.Crema, -585f);
            var marco = KitInterfaz.Imagen(_resumen, "Estrella", CajaChica(Amarillo), Color.white);
            KitInterfaz.Colocar(marco.rectTransform, new Vector2(0.5f, 1f), new Vector2(76f, 76f), new Vector2(-150f, -640f));
            var cara = KitInterfaz.Imagen(marco.transform, "Cara", tema != null ? tema.RetratoDeColor(r.UnidadEstrella, Amarillo) : null, Color.white);
            cara.preserveAspect = true;
            KitInterfaz.Colocar(cara.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(58f, 58f));
            var t = KitInterfaz.TextoQueCabe(_resumen, "Texto", _fuente, 24, 14, KitInterfaz.Crema, TextAnchor.MiddleLeft);
            t.text = $"{NombreDe(r.UnidadEstrella)}\nla que más bajas hizo en {r.VecesEstrella}";
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(300f, 70f), new Vector2(50f, -640f));
        }

        static string NombreDe(TipoUnidad t) => t switch
        {
            TipoUnidad.Guerrero => "Guerrero",
            TipoUnidad.Lancero => "Lancero",
            TipoUnidad.Arquero => "Arquero",
            TipoUnidad.Monje => "Monje",
            _ => "Pawn",
        };

        Text Linea(RectTransform padre, string texto, int tamano, Color color, float y, float alto = 44f)
        {
            var t = KitInterfaz.Texto(padre, "Linea", texto, _fuente, tamano, color, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(470f, alto), new Vector2(0f, y));
            return t;
        }

        /// <summary>La barra grande del pack, llena en la parte que toque.</summary>
        void Barra(RectTransform padre, float parte, Vector2 tamano, Vector2 posicion)
        {
            // Las mismas piezas y bordes que el volumen de Opciones.
            var marco = KitInterfaz.Imagen(padre, "Barra",
                KitInterfaz.ConBorde(tema != null ? tema.barraGrandeMarco : null, new Vector4(16, 0, 16, 0)), Color.white);
            KitInterfaz.Colocar(marco.rectTransform, new Vector2(0.5f, 1f), tamano, posicion);

            var area = KitInterfaz.Llenar(KitInterfaz.Hijo(marco.transform, "Area"));
            float margen = tamano.y * 0.3f;
            area.offsetMin = new Vector2(16f, margen);
            area.offsetMax = new Vector2(-16f, -margen);

            if (parte <= 0.001f) return;
            var relleno = KitInterfaz.Imagen(area, "Relleno",
                KitInterfaz.ConBorde(tema != null ? tema.barraGrandeRelleno : null, new Vector4(6, 0, 6, 0)), Color.white);
            relleno.type = Image.Type.Sliced;
            var rt = relleno.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(Mathf.Clamp01(parte), 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
