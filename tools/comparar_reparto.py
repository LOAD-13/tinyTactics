"""Compara el reparto de pawns del bot, regla vieja contra nueva (semana 16).

Lee los informes del historial (persistentDataPath/Historial) de las partidas jugadas con
«Bot contra bot» en el panel de pruebas, y resume por regla: segundos con la economía
torcida (sin un recurso y sobrado del otro), lo recolectado y la duración.

    python tools/comparar_reparto.py [carpeta]
"""
import glob, json, os, sys

CARPETA = sys.argv[1] if len(sys.argv) > 1 else os.path.expandvars(
    r"%USERPROFILE%\AppData\LocalLow\Grupo 01 UTP\Tiny Tactics\Historial")


def descompensado(oro, madera, paso, falta=50, sobra=250):
    # La misma cuenta que InformeDePartida.SegundosDescompensado.
    return paso * sum(1 for o, m in zip(oro, madera) if (o < falta and m >= sobra) or (m < falta and o >= sobra))


def main():
    filas = {True: [], False: []}
    for ruta in sorted(glob.glob(os.path.join(CARPETA, "*.json"))):
        with open(ruta, encoding="utf-8") as f:
            inf = json.load(f)
        if not inf.get("botEnElJugador"):
            continue
        for b in inf["bandos"]:
            minutos = max(inf["duracion"] / 60.0, 0.01)
            filas[inf.get("repartoPorReservas", True)].append((
                os.path.basename(ruta), inf["mapa"], b["bando"] + 1,
                descompensado(b["oro"], b["madera"], inf["paso"]),
                (b["oroRecolectado"] + b["maderaRecolectada"] + b["carneRecolectada"]) / minutos,
                b["puesto"], inf["duracion"] / 60.0))

    for regla, nombre in ((False, "FIJO (antiguo)"), (True, "POR RESERVAS (nuevo)")):
        f = filas[regla]
        print(f"\n== {nombre}: {len(f)} bandos en {len({x[0] for x in f})} partidas")
        for x in f:
            print(f"  {x[0]}  {x[1]:<16} bando {x[2]}  torcida {x[3]:>5.0f} s  "
                  f"recolecta {x[4]:>5.0f}/min  puesto {x[5]}  ({x[6]:.1f} min)")
        if f:
            print(f"  MEDIA: torcida {sum(x[3] for x in f) / len(f):.0f} s · "
                  f"recolecta {sum(x[4] for x in f) / len(f):.0f}/min")


if __name__ == "__main__":
    main()
