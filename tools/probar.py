# -*- coding: utf-8 -*-
"""Corre las pruebas del proyecto sin abrir Unity.

Por que no el Test Runner de Unity: sus pruebas viven en un ensamblado propio con su
.asmdef, y **un .asmdef no puede referenciar Assembly-CSharp**, que es donde esta todo
el codigo del juego. Meter el juego entero en asmdefs a mitad de proyecto es una
refactorizacion con su propio riesgo, y no es lo que hace falta.

Lo que hace falta es poder comprobar la LOGICA —la grilla de visibilidad, el plan de la
IA, la busqueda de sitio para un edificio— y esa logica es C# puro que solo toca structs
y matematicas de Unity. Eso se compila con el mismo Roslyn que usa Unity y se ejecuta en
.NET normal, en segundos y sin editor.

Lo que NO se puede probar asi: nada que toque GameObject, Debug.Log, corrutinas o el
ciclo de vida de un MonoBehaviour. Esas llamadas son nativas y no existen fuera del
motor. Es un limite real y esta bien tenerlo claro: estas pruebas cubren las decisiones,
no el dibujado.

Uso:  python tools/probar.py
"""
import glob
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import compilar as unity  # noqa: E402  (reutiliza su Roslyn y sus referencias)

RAIZ = unity.RAIZ
SALIDA = os.path.join(RAIZ, "Temp", "Pruebas")
PRUEBAS = os.path.join(RAIZ, "tools", "pruebas")

# La 8.0 es la que hay instalada y sobra: el codigo bajo prueba es aritmetica.
RUNTIME = "8.0.0"


def fuentes_del_juego():
    """Todo el runtime del juego. El codigo de Editor no entra: no existe en una build."""
    return unity.fuentes(solo_editor=False)


def fuentes_de_prueba():
    return sorted(glob.glob(os.path.join(PRUEBAS, "*.cs")))


def construir():
    refs, defines = unity.leer_proyecto("Assembly-CSharp.csproj")
    if refs is None:
        return None, ["No encuentro Assembly-CSharp.csproj. Abre Unity una vez."]

    archivos = fuentes_del_juego() + fuentes_de_prueba()
    if not fuentes_de_prueba():
        return None, ["No hay ningun archivo de prueba en tools/pruebas/."]

    os.makedirs(SALIDA, exist_ok=True)
    destino = os.path.join(SALIDA, "Pruebas.dll")
    rsp = os.path.join(SALIDA, "Pruebas.rsp")

    with open(rsp, "w", encoding="utf-8") as f:
        f.write("-target:exe\n")
        f.write("-nostdlib+\n")
        f.write("-langversion:9.0\n")
        f.write("-nowarn:0169,0414,0649,1701,1702\n")
        f.write(f'-out:"{destino}"\n')
        f.write("-main:TinyTactics.Pruebas.Arranque\n")

        for d in defines:
            f.write(f"-define:{d}\n")

        # Las pruebas no son una build de entrega, asi que el panel de partida libre
        # sigue compilando igual que en el editor.
        f.write("-define:UNITY_EDITOR\n")

        for r in refs:
            f.write(f'-reference:"{r}"\n')

        for a in archivos:
            f.write(f'"{a}"\n')

    proc = subprocess.run([unity.DOTNET, unity.CSC, f"@{rsp}"],
                          capture_output=True, text=True, cwd=RAIZ)

    errores = [l.strip() for l in proc.stdout.splitlines() if ": error " in l]
    return destino, errores


def escribir_runtimeconfig(destino):
    """Sin esto, dotnet no sabe con que runtime arrancar un .dll suelto."""
    config = {
        "runtimeOptions": {
            "tfm": "net8.0",
            "framework": {"name": "Microsoft.NETCore.App", "version": RUNTIME},
            "configProperties": {"System.Runtime.TieredPGO": False},
        }
    }

    ruta = destino.replace(".dll", ".runtimeconfig.json")
    with open(ruta, "w", encoding="utf-8") as f:
        json.dump(config, f, indent=2)


def copiar_dependencias(refs):
    """Las DLL de Unity tienen que estar junto al exe para que el runtime las encuentre."""
    import shutil

    for r in refs:
        if not os.path.exists(r):
            continue

        destino = os.path.join(SALIDA, os.path.basename(r))
        if os.path.exists(destino):
            continue

        try:
            shutil.copy2(r, destino)
        except OSError:
            pass


def main():
    if unity.CSC is None or not os.path.exists(unity.DOTNET):
        print("No encuentro el compilador de Unity o dotnet.")
        return 2

    print("Compilando las pruebas...")
    destino, errores = construir()

    if errores:
        print(f"\n{len(errores)} ERROR(ES) al compilar las pruebas:\n")
        for e in errores[:20]:
            print("  " + e)
        return 1

    refs, _ = unity.leer_proyecto("Assembly-CSharp.csproj")
    copiar_dependencias(refs)
    escribir_runtimeconfig(destino)

    print("Ejecutando...\n")
    proc = subprocess.run([unity.DOTNET, destino], text=True, cwd=SALIDA)

    return proc.returncode


if __name__ == "__main__":
    sys.exit(main())
