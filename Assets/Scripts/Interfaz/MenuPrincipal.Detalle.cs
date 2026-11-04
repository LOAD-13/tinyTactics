using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TinyTactics.Datos;
using TinyTactics.Nucleo;

namespace TinyTactics.Interfaz
{
    /// <summary>
    /// El detalle de una partida del historial: el marcador de todos los bandos y cómo
    /// evolucionó cada uno (semana 16).
    /// </summary>
    /// <remarks>
    /// Tres pestañas. <b>Mapa</b> es dónde se jugó, con la base de cada bando en su color.
    /// <b>Marcador</b> es una fila por bando en su color y por puesto, con lo
    /// que cada uno hizo. <b>Evolución</b> es la partida contada en el tiempo: una línea por
    /// bando, y la caída en vertical es el momento en que alguien perdió su ejército.
    /// Debajo, tres frases que la explican para que el número diga algo: cuánto recolectabas
    /// comparado con el mejor, cuándo empezó la pelea y cuándo tuviste más tropa.
    /// </remarks>
    public partial class MenuPrincipal
    {
        GameObject _detalle;
        RectTransform _hojaMarcador, _hojaEvolucion, _hojaMapa, _trazos, _marcasMapa;
        Text _subtitulo, _claves, _textoMapa;
        Image _mapaJugado;
        Button _pestanaMarcador, _pestanaEvolucion, _pestanaMapa, _serieEjercito, _serieRecursos;
        InformeDePartida _abierto;
        bool _verRecursos;

        // Columnas del marcador, desde el centro de la hoja.
        static readonly float[] Columnas = { -230f, -110f, 10f, 140f, 270f, 400f, 530f, 660f };

        void ConstruirDetalle(RectTransform mesa)
        {
            _detalle = KitInterfaz.Llenar(KitInterfaz.Hijo(mesa, "Detalle")).gameObject;
            var d = (RectTransform)_detalle.transform;

            _subtitulo = KitInterfaz.Texto(d, "Subtitulo", "", _fuente, 30, KitInterfaz.Crema, TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(_subtitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1400f, 44f), new Vector2(0f, 345f));

            _pestanaMarcador = BotonDeAccion(d, "Marcador", Azul, () => Pestana(0), new Vector2(0.5f, 0.5f),
                                             new Vector2(260f, 66f), new Vector2(-280f, 285f), 30);
            _pestanaEvolucion = BotonDeAccion(d, "Evolución", Negro, () => Pestana(1), new Vector2(0.5f, 0.5f),
                                              new Vector2(260f, 66f), new Vector2(0f, 285f), 30);
            _pestanaMapa = BotonDeAccion(d, "Mapa", Negro, () => Pestana(2), new Vector2(0.5f, 0.5f),
                                         new Vector2(260f, 66f), new Vector2(280f, 285f), 30);

            _hojaMarcador = KitInterfaz.Hijo(d, "Marcador");
            KitInterfaz.Colocar(_hojaMarcador, new Vector2(0.5f, 0.5f), new Vector2(1600f, 500f), new Vector2(0f, -10f));

            var papel = KitInterfaz.Imagen(d, "Evolucion", KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(papel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1600f, 500f), new Vector2(0f, -10f));
            _hojaEvolucion = papel.rectTransform;

            _serieEjercito = BotonDeAccion(_hojaEvolucion, "Población", Azul, () => Serie(false), new Vector2(1f, 1f),
                                           new Vector2(200f, 52f), new Vector2(-330f, -58f), 24);
            _serieRecursos = BotonDeAccion(_hojaEvolucion, "Recolectado", Negro, () => Serie(true), new Vector2(1f, 1f),
                                           new Vector2(200f, 52f), new Vector2(-120f, -58f), 24);

            _trazos = KitInterfaz.Hijo(_hojaEvolucion, "Trazos");
            KitInterfaz.Llenar(_trazos);

            ConstruirHojaMapa(d);

            var tira = KitInterfaz.Imagen(d, "Claves", KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(tira.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1500f, 100f), new Vector2(0f, -320f));
            _claves = KitInterfaz.TextoQueCabe(tira.transform, "Texto", _fuente, 24, 14, KitInterfaz.Tinta, TextAnchor.MiddleCenter);
            _claves.GetComponent<Outline>().enabled = false;
            KitInterfaz.Llenar(_claves.rectTransform).offsetMin = new Vector2(50f, 24f);
            _claves.rectTransform.offsetMax = new Vector2(-50f, -24f);

            _detalle.SetActive(false);
        }

        void AbrirDetalle(InformeDePartida informe)
        {
            _abierto = informe;
            _vistaLista.SetActive(false);
            _detalle.SetActive(true);

            int color = ColorDeResultado(informe);
            _cabeceraHistorial.text = Resultado(informe);
            if (tema != null) _cintaHistorial.sprite = tema.ListonPanelDeColor(color == Negro ? Morado : color);

            var rival = LecturaDeHistorial.Dificultad(informe);
            _subtitulo.text = $"{informe.mapa} · {LecturaDeHistorial.Formato(informe)}" +
                              (ReglasDeConfiguracion.EsBot(rival) ? $" · {ReglasDeConfiguracion.Nombre(rival)}" : "") +
                              $" · {LecturaDeHistorial.Reloj(informe.duracion)} · {FechaCorta(informe.fecha)}";

            _claves.text = string.Join("     ·     ", LecturaDeHistorial.Claves(informe));

            PintarMarcador(informe);
            PintarMapa(informe);
            Pestana(0);
        }

        void CerrarDetalle()
        {
            if (_detalle == null) return;
            _detalle.SetActive(false);
            _vistaLista.SetActive(_partidas.Count > 0);
            _cabeceraHistorial.text = "Historial";
            if (tema != null) _cintaHistorial.sprite = tema.ListonPanelDeColor(Morado);
        }

        /// <summary>0 marcador, 1 evolución, 2 mapa.</summary>
        void Pestana(int cual)
        {
            _hojaMarcador.gameObject.SetActive(cual == 0);
            _hojaEvolucion.gameObject.SetActive(cual == 1);
            _hojaMapa.gameObject.SetActive(cual == 2);
            Encender(_pestanaMarcador, cual == 0);
            Encender(_pestanaEvolucion, cual == 1);
            Encender(_pestanaMapa, cual == 2);
            if (cual == 1) Serie(_verRecursos);
        }

        // -----------------------------------------------------------------
        // Mapa
        // -----------------------------------------------------------------

        void ConstruirHojaMapa(RectTransform d)
        {
            _hojaMapa = KitInterfaz.Hijo(d, "Mapa");
            KitInterfaz.Colocar(_hojaMapa, new Vector2(0.5f, 0.5f), new Vector2(1600f, 500f), new Vector2(0f, -10f));

            // La misma miniatura que «Nueva partida», en el mismo pergamino, pero en grande.
            var papel = KitInterfaz.Imagen(_hojaMapa, "Pergamino", KitInterfaz.ConBorde(pergamino, new Vector4(28, 28, 28, 28)), Color.white);
            KitInterfaz.Colocar(papel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(560f, 520f), new Vector2(-330f, 0f));

            _mapaJugado = KitInterfaz.Imagen(papel.transform, "Miniatura", null, Color.white);
            _mapaJugado.preserveAspect = true;
            KitInterfaz.Colocar(_mapaJugado.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(430f, 430f));
            _marcasMapa = _mapaJugado.rectTransform;

            _textoMapa = KitInterfaz.TextoQueCabe(_hojaMapa, "Texto", _fuente, 28, 16, KitInterfaz.Crema, TextAnchor.MiddleLeft);
            KitInterfaz.Colocar(_textoMapa.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(640f, 420f), new Vector2(350f, 0f));
        }

        /// <summary>
        /// La ficha del mapa por su nombre: la escena trae Tres Coronas y el resto son archivos.
        /// </summary>
        /// <remarks>Un mapa que se borró después de jugarlo no tiene ficha: se dice, no se inventa.</remarks>
        bool BuscarMapa(string nombre, out Sprite miniatura, out Vector2[] bases, out string descripcion)
        {
            foreach (var f in mapas)
                if (f.nombre == nombre) { miniatura = f.miniatura; bases = f.bases; descripcion = f.descripcion; return true; }

            foreach (var e in Mundo.ArchivoDeMapas.Listar())
                if (e.mapa.nombre == nombre)
                {
                    miniatura = Mundo.MiniaturaDeMapa.Crear(e.mapa);
                    bases = Mundo.MiniaturaDeMapa.Bases(e.mapa);
                    descripcion = e.mapa.descripcion;
                    return true;
                }

            miniatura = null; bases = null; descripcion = "";
            return false;
        }

        void PintarMapa(InformeDePartida informe)
        {
            for (int i = _marcasMapa.childCount - 1; i >= 0; i--)
                Destroy(_marcasMapa.GetChild(i).gameObject);

            bool hay = BuscarMapa(informe.mapa, out var miniatura, out var bases, out var descripcion);
            _mapaJugado.sprite = miniatura;
            _mapaJugado.enabled = miniatura != null;

            // Cada base, en el color de quien la ocupó, con su cara y su puesto.
            foreach (var b in informe.bandos)
            {
                if (bases == null || b.bando >= bases.Length) continue;

                var caja = KitInterfaz.Imagen(_marcasMapa, "Base", CajaChica(b.color), Color.white);
                var rt = caja.rectTransform;
                rt.anchorMin = rt.anchorMax = bases[b.bando];
                rt.sizeDelta = new Vector2(64f, 64f);
                rt.anchoredPosition = Vector2.zero;
                var cara = KitInterfaz.Imagen(caja.transform, "Cara", CaraDe(b), Color.white);
                cara.preserveAspect = true;
                KitInterfaz.Colocar(cara.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(48f, 48f));

                var puesto = KitInterfaz.Texto(caja.transform, "Puesto", $"{b.puesto}º", _fuente, 24,
                                               b.puesto == 1 ? Oro : KitInterfaz.Crema, TextAnchor.MiddleCenter);
                KitInterfaz.Colocar(puesto.rectTransform, new Vector2(0.5f, 0f), new Vector2(60f, 30f), new Vector2(0f, -14f));
            }

            var yo = informe.DelJugador;
            string texto = $"{informe.mapa}\n\n";
            texto += hay ? $"{descripcion}\n\n" : "Este mapa ya no está en la lista: se borró después de jugarlo.\n\n";
            texto += $"{informe.bandos.Count} bandos · {LecturaDeHistorial.Reloj(informe.duracion)} de partida";
            if (yo != null && LecturaDeHistorial.EsDelJugador(informe))
                texto += $"\nJugaste en la base {yo.bando + 1} y quedaste {yo.puesto}º.";
            _textoMapa.text = texto;
        }

        /// <summary>La pestaña o serie elegida, en azul; la otra, en negro.</summary>
        void Encender(Button b, bool vivo) => ((Image)b.targetGraphic).sprite = Boton(vivo ? Azul : Negro);

        // -----------------------------------------------------------------
        // Marcador
        // -----------------------------------------------------------------

        void PintarMarcador(InformeDePartida informe)
        {
            for (int i = _hojaMarcador.childCount - 1; i >= 0; i--)
                Destroy(_hojaMarcador.GetChild(i).gameObject);

            // Cabecera: el icono del pack de cada columna, con su nombre debajo.
            var cabeceras = new (Sprite icono, string nombre)[]
            {
                (tema != null ? tema.iconoAtaque : null, "Bajas"), (null, "Caídas"),
                (tema != null ? tema.iconoConstruir : null, "Edificios"),
                (tema != null ? tema.iconoRecursoOro : null, "Oro"), (tema != null ? tema.iconoRecursoMadera : null, "Madera"),
                (tema != null ? tema.iconoRecursoCarne : null, "Carne"), (null, "Población"),
                (tema != null ? tema.iconoEntrenar : null, "Entrenó"),
            };
            for (int k = 0; k < cabeceras.Length; k++)
            {
                var (icono, nombre) = cabeceras[k];
                if (icono != null)
                {
                    var img = KitInterfaz.Imagen(_hojaMarcador, "Icono", icono, Color.white);
                    KitInterfaz.Colocar(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(38f, 38f), new Vector2(Columnas[k], -18f));
                }
                var t = KitInterfaz.Texto(_hojaMarcador, "Columna", nombre, _fuente, 20, KitInterfaz.Crema, TextAnchor.MiddleCenter);
                KitInterfaz.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(124f, 28f),
                                    new Vector2(Columnas[k], icono != null ? -50f : -34f));
            }

            var orden = new List<InformeDeBando>(informe.bandos);
            orden.Sort((a, b) => a.puesto != b.puesto ? a.puesto.CompareTo(b.puesto) : a.bando.CompareTo(b.bando));

            for (int i = 0; i < orden.Count; i++)
                FilaDeBando(informe, orden[i], -110f - i * 88f);
        }

        void FilaDeBando(InformeDePartida informe, InformeDeBando b, float y)
        {
            bool yo = b.bando == informe.jugador && LecturaDeHistorial.EsDelJugador(informe);

            var fila = KitInterfaz.Imagen(_hojaMarcador, "Bando", Caja(b.color), Color.white);
            KitInterfaz.Colocar(fila.rectTransform, new Vector2(0.5f, 1f), new Vector2(1600f, 82f), new Vector2(0f, y));
            var rt = fila.rectTransform;

            var puesto = KitInterfaz.Texto(rt, "Puesto", $"{b.puesto}º", _fuente, 36, b.puesto == 1 ? Oro : KitInterfaz.Crema,
                                           TextAnchor.MiddleCenter);
            KitInterfaz.Colocar(puesto.rectTransform, new Vector2(0f, 0.5f), new Vector2(80f, 50f), new Vector2(60f, 0f));

            var marco = KitInterfaz.Imagen(rt, "Marco", CajaChica(b.color), Color.white);
            KitInterfaz.Colocar(marco.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 70f), new Vector2(150f, 0f));
            var cara = KitInterfaz.Imagen(marco.transform, "Cara", CaraDe(b), Color.white);
            cara.preserveAspect = true;
            KitInterfaz.Colocar(cara.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(54f, 54f));

            var nombre = KitInterfaz.TextoQueCabe(rt, "Nombre", _fuente, 30, 16, yo ? Oro : KitInterfaz.Crema, TextAnchor.MiddleLeft);
            nombre.text = ReglasDeConfiguracion.Nombre(b.ocupante);
            KitInterfaz.Colocar(nombre.rectTransform, new Vector2(0f, 0.5f), new Vector2(300f, 38f), new Vector2(355f, 13f));

            string estado = b.bando == informe.ganador ? "Ganó la partida"
                          : b.eliminadoEn >= 0f ? $"Cayó al {LecturaDeHistorial.Reloj(b.eliminadoEn)}" : "Seguía en pie";
            var sub = KitInterfaz.Texto(rt, "Estado", estado, _fuente, 20, KitInterfaz.Crema, TextAnchor.MiddleLeft);
            KitInterfaz.Colocar(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(300f, 28f), new Vector2(355f, -20f));

            int[] valores =
            {
                b.unidadesEliminadas, b.unidadesPerdidas, b.edificiosDestruidos, b.oroRecolectado,
                b.maderaRecolectada, b.carneRecolectada, b.picoDePoblacion, b.unidadesEntrenadas,
            };
            for (int k = 0; k < valores.Length; k++)
            {
                var v = KitInterfaz.Texto(rt, "Valor", valores[k].ToString(), _fuente, 30, KitInterfaz.Crema, TextAnchor.MiddleCenter);
                KitInterfaz.Colocar(v.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(124f, 44f), new Vector2(Columnas[k], 0f));
            }
        }

        // -----------------------------------------------------------------
        // Evolución
        // -----------------------------------------------------------------

        void Serie(bool recursos)
        {
            _verRecursos = recursos;
            Encender(_serieEjercito, !recursos);
            Encender(_serieRecursos, recursos);
            if (_abierto != null) PintarEvolucion(_abierto, recursos);
        }

        void PintarEvolucion(InformeDePartida informe, bool recursos)
        {
            for (int i = _trazos.childCount - 1; i >= 0; i--)
                Destroy(_trazos.GetChild(i).gameObject);

            // El área de la gráfica dentro del papel: el pergamino trae margen transparente.
            var area = new Rect(130f, 95f, 1600f - 130f - 90f, 500f - 95f - 120f);
            var tinta = new Color(KitInterfaz.Tinta.r, KitInterfaz.Tinta.g, KitInterfaz.Tinta.b, 0.55f);

            int techo = 1, muestras = 0;
            foreach (var b in informe.bandos)
            {
                var s = recursos ? b.recolectado : b.poblacion;
                muestras = Mathf.Max(muestras, s.Count);
                foreach (int v in s) techo = Mathf.Max(techo, v);
            }
            techo = Redondo(techo);

            Trazo(new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin), 3f, tinta);
            Trazo(new Vector2(area.xMin, area.yMin), new Vector2(area.xMin, area.yMax), 3f, tinta);
            for (int k = 0; k <= 2; k++)
            {
                float y = area.yMin + area.height * k / 2f;
                if (k > 0) Trazo(new Vector2(area.xMin, y), new Vector2(area.xMax, y), 1f, new Color(tinta.r, tinta.g, tinta.b, 0.2f));
                Rotulo((techo * k / 2).ToString(), new Vector2(area.xMin - 50f, y), TextAnchor.MiddleRight, 90f);
            }

            float total = Mathf.Max(informe.duracion, informe.paso);
            int cada = Mathf.Max(1, Mathf.CeilToInt(total / 60f / 8f));
            for (int m = 0; m * 60f <= total; m += cada)
                Rotulo($"{m} min", new Vector2(area.xMin + area.width * (m * 60f / total), area.yMin - 26f), TextAnchor.MiddleCenter, 90f);

            // El jugador al final, para que su línea quede encima de las demás.
            var orden = new List<InformeDeBando>(informe.bandos);
            orden.Sort((a, b) => (a.bando == informe.jugador).CompareTo(b.bando == informe.jugador));

            foreach (var b in orden)
            {
                var s = recursos ? b.recolectado : b.poblacion;
                var color = Oscurecer(tema != null ? tema.TonoDeColor(b.color) : Color.gray);
                float grosor = b.bando == informe.jugador ? 6f : 4f;
                int salto = Mathf.Max(1, Mathf.CeilToInt(s.Count / 120f));

                Vector2 Punto(int k) => new Vector2(area.xMin + area.width * Mathf.Min(1f, k * informe.paso / total),
                                                    area.yMin + area.height * s[k] / techo);

                for (int k = salto; k < s.Count; k += salto) Trazo(Punto(k - salto), Punto(k), grosor, color);
                if (s.Count > 1 && (s.Count - 1) % salto != 0) Trazo(Punto(s.Count - 1 - (s.Count - 1) % salto), Punto(s.Count - 1), grosor, color);
            }

            // Leyenda: la cara y el nombre de cada bando, en su color.
            for (int i = 0; i < informe.bandos.Count; i++)
            {
                var b = informe.bandos[i];
                var x = 120f + i * 200f;
                var caja = KitInterfaz.Imagen(_trazos, "Leyenda", CajaChica(b.color), Color.white);
                caja.rectTransform.anchorMin = caja.rectTransform.anchorMax = new Vector2(0f, 1f);
                caja.rectTransform.sizeDelta = new Vector2(44f, 44f);
                caja.rectTransform.anchoredPosition = new Vector2(x, -58f);
                var c = KitInterfaz.Imagen(caja.transform, "Cara", CaraDe(b), Color.white);
                c.preserveAspect = true;
                KitInterfaz.Colocar(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

                var n = KitInterfaz.TextoQueCabe(_trazos, "Nombre", _fuente, 20, 12, KitInterfaz.Tinta, TextAnchor.MiddleLeft);
                n.GetComponent<Outline>().enabled = false;
                n.text = ReglasDeConfiguracion.Nombre(b.ocupante).Replace("Bot · ", "");
                n.rectTransform.anchorMin = n.rectTransform.anchorMax = new Vector2(0f, 1f);
                n.rectTransform.sizeDelta = new Vector2(140f, 30f);
                n.rectTransform.anchoredPosition = new Vector2(x + 96f, -58f);
            }
        }

        /// <summary>El techo de la gráfica, en un número redondo: 40 y no 37.</summary>
        static int Redondo(int v)
        {
            int paso = v <= 20 ? 5 : v <= 100 ? 10 : v <= 1000 ? 100 : 1000;
            return Mathf.Max(paso, (v + paso - 1) / paso * paso);
        }

        /// <summary>El amarillo del pack sobre el papel apenas se ve: todos los tonos, un poco más oscuros.</summary>
        static Color Oscurecer(Color c) => new Color(c.r * 0.78f, c.g * 0.78f, c.b * 0.78f, 1f);

        void Trazo(Vector2 a, Vector2 b, float grosor, Color color)
        {
            var img = KitInterfaz.Imagen(_trazos, "Trazo", null, color);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            var d = b - a;
            rt.sizeDelta = new Vector2(d.magnitude + grosor * 0.5f, grosor);
            rt.anchoredPosition = (a + b) / 2f;
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void Rotulo(string texto, Vector2 donde, TextAnchor alineado, float ancho)
        {
            var t = KitInterfaz.Texto(_trazos, "Rotulo", texto, _fuente, 18, KitInterfaz.Tinta, alineado, contorno: false);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(ancho, 26f);
            rt.anchoredPosition = donde;
        }
    }
}
