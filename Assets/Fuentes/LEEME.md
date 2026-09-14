# Fuentes

## MedievalSharp

**Licencia:** SIL Open Font License 1.1 — ver `LICENCIA-OFL.txt` y `FONTLOG.txt`.

La OFL permite expresamente redistribuir la fuente, incluirla en un producto y publicarla en
un repositorio público, con dos condiciones que aquí se cumplen:

1. La licencia viaja con la fuente. Por eso `LICENCIA-OFL.txt` y `FONTLOG.txt` están en esta
   misma carpeta y no deben borrarse.
2. No se vende la fuente por separado ni se usa el nombre reservado en un derivado. No
   hacemos ninguna de las dos cosas.

### Por qué esta fuente sí y las de Windows no

El proyecto tiene la norma de **no versionar tipografías**, y esa norma existe por las fuentes
del sistema: las de Windows son de Microsoft y su licencia no permite redistribuirlas en un
repositorio público. MedievalSharp es un caso distinto — es libre por diseño — así que la
excepción está justificada y documentada aquí para que nadie la revierta por error.

### Dónde se usa y dónde no

- **Sí:** titulares grandes de interfaz, empezando por el cartel de victoria y derrota.
- **No:** cifras pequeñas del HUD. MedievalSharp es vectorial, no pixel: a tamaño pequeño se
  suaviza y desentona junto al pixel art del pack. Las cifras se quedan como están.

Se incluyen solo los dos pesos que se usan, `Book` y `Bold`. Las variantes oblicuas del
paquete original no se versionan porque no se usan.
