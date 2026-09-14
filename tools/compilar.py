# -*- coding: utf-8 -*-
"""Compila los scripts del proyecto sin pasar por Unity.

Unity solo recompila cuando el editor recupera el foco, asi que trabajando seguido
hay que esperar a que alguien haga alt-tab para saber si algo rompio.

Esto usa el MISMO Roslyn que trae Unity y las MISMAS referencias que Unity ya
escribio en los .csproj, asi que un error aqui es un error alli.

Los .cs NO se leen del .csproj: se buscan en disco. El .csproj se queda viejo en
cuanto se anade un archivo nuevo, y los archivos nuevos son justo los que mas
falta hace comprobar.

Uso:  python tools/compilar.py
"""
import glob
import os
import re
import subprocess
import sys

# La raiz del proyecto es la carpeta padre de esta: tools/ cuelga de tinyTactics/.
RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Los binarios intermedios van a Temp/, que ya esta fuera del control de versiones.
SALIDA = os.path.join(RAIZ, "Temp", "ChequeoCompilacion")

DOTNET = os.path.join(os.environ.get("ProgramFiles", r"C:\Program Files"),
                      "dotnet", "dotnet.exe")


def buscar_roslyn():
    """El csc.dll que trae el propio Unity, sea cual sea la version instalada."""
    base = os.path.join(os.environ.get("ProgramFiles", r"C:\Program Files"),
                        "Unity", "Hub", "Editor")

    if not os.path.isdir(base):
        return None

    # De mas nueva a mas vieja: si hay varias instaladas, la ultima es la del proyecto.
    for version in sorted(os.listdir(base), reverse=True):
        patron = os.path.join(base, version, "Editor", "Data", "DotNetSdk",
                              "sdk", "*", "Roslyn", "bincore", "csc.dll")

        encontrados = glob.glob(patron)
        if encontrados:
            return encontrados[0]

    return None


CSC = buscar_roslyn()


def leer_proyecto(nombre):
    """Saca referencias y simbolos de definicion de un .csproj de Unity."""
    ruta = os.path.join(RAIZ, nombre)
    if not os.path.exists(ruta):
        return None, None

    with open(ruta, encoding="utf-8-sig") as f:
        texto = f.read()

    refs = re.findall(r"<HintPath>(.*?)</HintPath>", texto)

    defines = set()
    for bloque in re.findall(r"<DefineConstants>(.*?)</DefineConstants>", texto):
        for d in bloque.split(";"):
            d = d.strip()
            if d:
                defines.add(d)

    return refs, sorted(defines)


def fuentes(solo_editor):
    """Los .cs del ensamblado. Unity separa Editor y runtime en ensamblados distintos."""
    todos = glob.glob(os.path.join(RAIZ, "Assets", "**", "*.cs"), recursive=True)

    salida = []
    for f in todos:
        # El criterio de Unity: una carpeta llamada exactamente "Editor" en la ruta.
        partes = os.path.normpath(f).split(os.sep)
        if ("Editor" in partes) == solo_editor:
            salida.append(f)

    return sorted(salida)


def compilar(nombre_proyecto, solo_editor, extra_refs=()):
    refs, defines = leer_proyecto(nombre_proyecto)
    if refs is None:
        return None, [f"No encuentro {nombre_proyecto}. Abre Unity una vez para que lo genere."]

    archivos = fuentes(solo_editor)
    if not archivos:
        return None, []

    destino = os.path.join(SALIDA, nombre_proyecto.replace(".csproj", ".chequeo.dll"))
    rsp = os.path.join(SALIDA, nombre_proyecto.replace(".csproj", ".rsp"))

    with open(rsp, "w", encoding="utf-8") as f:
        f.write("-target:library\n")

        # Unity compila contra su propio mscorlib, que viene en las referencias del
        # .csproj. Sin nostdlib se colaria ademas el de .NET y todo saldria duplicado.
        f.write("-nostdlib+\n")
        f.write("-langversion:9.0\n")

        # Avisos de campo sin usar: el inspector de Unity asigna muchos campos que el
        # compilador no ve tocar nunca. Son ruido, no hallazgos.
        f.write("-nowarn:0169,0414,0649,1701,1702\n")

        f.write(f'-out:"{destino}"\n')

        for d in defines:
            f.write(f"-define:{d}\n")

        for r in list(refs) + list(extra_refs):
            f.write(f'-reference:"{r}"\n')

        for a in archivos:
            f.write(f'"{a}"\n')

    proc = subprocess.run([DOTNET, CSC, f"@{rsp}"],
                          capture_output=True, text=True, cwd=RAIZ)

    errores = [l.strip() for l in proc.stdout.splitlines() if ": error " in l]
    return destino, errores


def main():
    if CSC is None or not os.path.exists(DOTNET):
        print("No encuentro el compilador de Unity o dotnet. Revisa la instalacion.")
        return 2

    os.makedirs(SALIDA, exist_ok=True)

    print("Compilando Assembly-CSharp (runtime)...")
    dll, errores = compilar("Assembly-CSharp.csproj", solo_editor=False)

    if errores:
        print(f"\n{len(errores)} ERROR(ES) en runtime:\n")
        for e in errores[:25]:
            print("  " + e)
        return 1

    print("  OK\n")
    print("Compilando Assembly-CSharp-Editor...")

    # El ensamblado de editor ve al de runtime: hay que pasarle el que se acaba de
    # construir, no el que Unity dejo en Library, que puede ser de hace media hora.
    _, errores = compilar("Assembly-CSharp-Editor.csproj", solo_editor=True,
                          extra_refs=[dll])

    if errores:
        print(f"\n{len(errores)} ERROR(ES) en editor:\n")
        for e in errores[:25]:
            print("  " + e)
        return 1

    print("  OK\n")
    print("Todo compila.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
