"""Dibuja el teclado y el raton de la pantalla de controles (semana 14).

Pixel art hecho a mano por codigo: cada tecla es una tapa con su cara de arriba y su
frente, como un teclado visto un poco desde delante. Las teclas que usa el juego van en
dorado. Al acabar escribe donde cae el centro de cada tecla usada, en fraccion de la
imagen, para que la interfaz tire las lineas hasta ella.

    python tools/dibujar_controles.py
"""
import json
import os

from PIL import Image, ImageDraw, ImageFont

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SALIDA = os.path.join(RAIZ, "Assets", "Arte", "Controles")

U = 21            # paso de una tecla, en pixeles
TAPA = 17         # lado de la cara de arriba
FRENTE = 4        # alto del frente de la tecla
MARGEN = 8

# Paleta: madera oscura del pack para el cuerpo, crema para las teclas, oro para las usadas.
CONTORNO = (38, 28, 30)
CUERPO, CUERPO_FRENTE, CUERPO_LUZ = (92, 70, 64), (62, 46, 44), (120, 94, 84)
TECLA, TECLA_FRENTE, TECLA_LUZ = (226, 214, 190), (168, 150, 128), (246, 238, 220)
ORO, ORO_FRENTE, ORO_LUZ = (238, 184, 64), (176, 116, 34), (255, 226, 140)
TINTA = (64, 48, 40)

# (etiqueta, ancho en teclas, nombre si el juego la usa)
FILAS = [
    [("ESC", 1, "esc"), (None, 1, None)] + [("", 1, None)] * 4 + [(None, 0.5, None)]
    + [("", 1, None)] * 4 + [(None, 0.5, None)] + [("", 1, None)] * 4,
    [("`", 1, None)] + [(str(i), 1, f"d{i}") for i in range(1, 10)] + [("0", 1, None), ("-", 1, None), ("=", 1, None), ("", 2, None)],
    [("", 1.5, None)] + [(c, 1, c.lower() if c in "TP" else None) for c in "QWERTYUIOP"] + [("", 1, None), ("", 1, None), ("", 1.5, None)],
    [("", 1.75, None)] + [(c, 1, c.lower() if c in "AS" else None) for c in "ASDFGHJKL"] + [("", 1, None), ("", 1, None), ("", 2.25, None)],
    [("SHIFT", 2.25, "shift")] + [(c, 1, c.lower() if c in "XVBM" else None) for c in "ZXCVBNM"] + [("", 1, None), ("", 1, None), ("", 1, None), ("", 2.75, None)],
    [("CTRL", 1.25, "ctrl"), ("", 1.25, None), ("ALT", 1.25, None), ("", 6.25, "espacio"), ("", 1.25, None), ("", 1.25, None), ("", 1.25, None), ("", 1.25, None)],
]
# Las flechas, aparte, a la derecha: (fila, columna en teclas, etiqueta, nombre)
FLECHAS = [(4, 16.5, "^", "arriba"), (5, 15.5, "<", "izquierda"), (5, 16.5, "v", "abajo"), (5, 17.5, ">", "derecha")]


FLECHA = ("^", "v", "<", ">")


def fuente(tamano=10):
    try:
        return ImageFont.load_default(size=tamano)
    except TypeError:
        return ImageFont.load_default()


def tapa(d, x, y, ancho, usada, texto, f):
    """Una tecla: contorno, frente y cara de arriba con su brillo."""
    cara, frente, luz = (ORO, ORO_FRENTE, ORO_LUZ) if usada else (TECLA, TECLA_FRENTE, TECLA_LUZ)
    w = int(round(ancho * U)) - (U - TAPA)
    d.rectangle([x - 1, y - 1, x + w, y + TAPA + FRENTE], fill=CONTORNO)
    d.rectangle([x, y + TAPA - 1, x + w - 1, y + TAPA + FRENTE - 1], fill=frente)
    d.rectangle([x, y, x + w - 1, y + TAPA - 2], fill=cara)
    d.line([x, y, x + w - 1, y], fill=luz)
    d.line([x, y, x, y + TAPA - 2], fill=luz)
    if texto in FLECHA:
        # Las flechas, como triángulos: los caracteres ^ v < > a este tamaño no se leen.
        cx, cy, r = x + w // 2, y + (TAPA - 1) // 2, 4
        puntas = {"^": [(cx - r, cy + 2), (cx + r, cy + 2), (cx, cy - 3)],
                  "v": [(cx - r, cy - 2), (cx + r, cy - 2), (cx, cy + 3)],
                  "<": [(cx + 2, cy - r), (cx + 2, cy + r), (cx - 3, cy)],
                  ">": [(cx - 2, cy - r), (cx - 2, cy + r), (cx + 3, cy)]}[texto]
        d.polygon(puntas, fill=TINTA)
    elif texto:
        # Un rótulo que no cabe en su tecla baja de letra.
        if d.textbbox((0, 0), texto, font=f)[2] > w - 2:
            f = fuente(7)
        caja = d.textbbox((0, 0), texto, font=f)
        tw, th = caja[2] - caja[0], caja[3] - caja[1]
        d.text((x + (w - tw) // 2 - caja[0], y + (TAPA - 1 - th) // 2 - caja[1]), texto, fill=TINTA, font=f)
    return (x + w / 2, y + TAPA / 2)


def teclado():
    columnas = 18.5
    ancho = int(MARGEN * 2 + columnas * U + 4)
    alto_cuerpo = MARGEN * 2 + len(FILAS) * U + FRENTE
    grosor = 8
    img = Image.new("RGBA", (ancho + 2, alto_cuerpo + grosor + 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.fontmode = "1"   # sin suavizado: pixel art
    f = fuente()

    # El cuerpo: tapa de arriba y un canto delantero grueso, que es lo que da volumen.
    d.rectangle([0, 0, ancho + 1, alto_cuerpo + grosor + 1], fill=CONTORNO)
    d.rectangle([1, alto_cuerpo, ancho, alto_cuerpo + grosor], fill=CUERPO_FRENTE)
    d.rectangle([1, 1, ancho, alto_cuerpo - 1], fill=CUERPO)
    d.line([1, 1, ancho, 1], fill=CUERPO_LUZ)
    d.line([1, 1, 1, alto_cuerpo - 1], fill=CUERPO_LUZ)
    # dos lucecitas, como en cualquier teclado
    d.rectangle([ancho - 18, 4, ancho - 16, 5], fill=(120, 220, 120))
    d.rectangle([ancho - 12, 4, ancho - 10, 5], fill=(240, 200, 90))

    centros = {}
    for fila, teclas in enumerate(FILAS):
        x = MARGEN + 1
        y = MARGEN + fila * U + (2 if fila > 0 else 0)
        for etiqueta, w, nombre in teclas:
            if etiqueta is None:   # hueco, no tecla
                x += w * U
                continue
            c = tapa(d, int(round(x)), y, w, nombre is not None, etiqueta, f)
            if nombre:
                centros[nombre] = c
            x += w * U
    for fila, col, etiqueta, nombre in FLECHAS:
        y = MARGEN + fila * U + 2
        centros[nombre] = tapa(d, int(round(MARGEN + 1 + col * U)), y, 1, True, etiqueta, f)

    return img, centros


def raton():
    w, h = 62, 92
    img = Image.new("RGBA", (w, h + 6), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # cuerpo con su canto, y la raya entre los dos botones
    d.rounded_rectangle([0, 0, w - 1, h + 5], radius=26, fill=CONTORNO)
    d.rounded_rectangle([1, 4, w - 2, h + 4], radius=25, fill=TECLA_FRENTE)
    d.rounded_rectangle([1, 1, w - 2, h], radius=25, fill=TECLA)
    d.line([w // 2, 1, w // 2, 34], fill=CONTORNO)
    d.line([1, 34, w - 2, 34], fill=CONTORNO)
    # los dos botones en dorado: los dos se usan
    d.rounded_rectangle([2, 2, w // 2 - 1, 33], radius=20, fill=ORO)
    d.rounded_rectangle([w // 2 + 1, 2, w - 3, 33], radius=20, fill=ORO)
    d.rectangle([2, 18, w // 2 - 1, 33], fill=ORO)
    d.rectangle([w // 2 + 1, 18, w - 3, 33], fill=ORO)
    d.line([6, 5, 14, 4], fill=ORO_LUZ)
    # la rueda
    d.rectangle([w // 2 - 4, 8, w // 2 + 4, 26], fill=CONTORNO)
    d.rectangle([w // 2 - 3, 9, w // 2 + 3, 25], fill=ORO_FRENTE)
    for yy in (12, 16, 20):
        d.line([w // 2 - 3, yy, w // 2 + 3, yy], fill=ORO_LUZ)
    centros = {"izquierdo": (w * 0.25, 18), "derecho": (w * 0.75, 18), "rueda": (w / 2, 17)}
    return img, centros


def fraccion(img, c):
    # Unity cuenta la y desde abajo.
    return [round(c[0] / img.width, 4), round(1 - c[1] / img.height, 4)]


if __name__ == "__main__":
    os.makedirs(SALIDA, exist_ok=True)
    t, ct = teclado()
    r, cr = raton()
    t.save(os.path.join(SALIDA, "Teclado.png"))
    r.save(os.path.join(SALIDA, "Raton.png"))
    print("teclado", t.size, "raton", r.size)
    print(json.dumps({k: fraccion(t, v) for k, v in ct.items()}))
    print(json.dumps({k: fraccion(r, v) for k, v in cr.items()}))
