# basket-foot

Juego 3D hecho en Godot 4.7 (.NET/C#). Es un híbrido entre fútbol y baloncesto:
se juega con el balón al pie y por cabeza, pero se encesta en una canasta.

## Concepto

- Por ahora es un juego **para un jugador**.
- Cada partida enfrenta a **dos equipos** de **2 jugadores** por equipo (equipo
  local contra equipo rival controlado por la IA — por implementar).

## Reglas

- La pelota **no se puede tocar con las manos**, solo con los **pies** o la **cabeza**.
- Excepciones: **saques** y **tiros de esquina** se pueden hacer con la mano.
- Se puede jugar de cabeza, por ejemplo cabeceando un centro.
- **No hay porterías**: las porterías son **cestas** como en el baloncesto.
  La cesta es más grande que una de baloncesto para facilitar el encestado.
- Gana el equipo que consiga más puntos.

## Puntuación

Reglas de puntuación **alineadas con la FIBA** para que los aficionados al
baloncesto encuentren el sistema familiar:

- **2 puntos**: enceste desde dentro de la línea de tres puntos (incluye el área pequeña).
- **3 puntos**: enceste desde más allá de la línea de tres (a 6,75 m de la canasta, referencia FIBA).
- **1 punto**: tiro libre (regla FIBA; solo aplicará si se implementan faltas).

## Física del balón

- El balón es **especialmente ligero** para que rebote mucho; la física de
  rebote es uno de los puntos clave a ajustar.

## Estado actual

Proyecto en fase inicial. Todavía no hay escenas, scripts ni reglas de juego
implementadas.

## Stack técnico

- Godot 4.7 .NET edition — scripts en C#.
- Renderer: Forward Plus · Física: Jolt Physics · Driver Windows: D3D12.
