# basket-foot

Juego 3D hecho en **Godot 4.7 .NET** con scripts en **C#**. Es un híbrido entre
fútbol sala y baloncesto: se juega el balón con los pies o la cabeza, pero se
anota encestando en canastas grandes.

## Estado actual

El proyecto ya es un prototipo jugable. La escena principal incluye una cancha
tipo futsal de 40 x 20 m, dos canastas, balón físico, cámara de seguimiento,
marcador, reinicios por salida de banda/fondo y un partido 2v2 con equipos azul
y rojo.

El jugador humano controla al equipo azul junto a un compañero con IA. El equipo
rojo está controlado por IA. Los jugadores pueden conducir el balón, cargar
tiros, tirar, volear, cabecear, pasar, robar y barrerse para disputar la pelota.

El historial de versiones y avances se mantiene en [CHANGELOG.md](CHANGELOG.md).

## Reglas del juego

- Se juega sin manos durante el juego abierto.
- El balón se controla cerca de los pies cuando un jugador lo alcanza.
- Los tiros se hacen cargando potencia y apuntando con la orientación del
  jugador.
- Se puede levantar el balón para una volea.
- Se puede cabecear un balón suelto a altura adecuada.
- Hay saques laterales y saques de fondo cuando el balón sale de la cancha.
- Cada equipo tiene una canasta propia que debe defender y ataca únicamente la
  canasta rival: Azul defiende el norte y ataca el sur; Rojo hace lo contrario.
- Un enceste en la canasta propia se acredita al equipo rival como autogol.
- El marcador sigue reglas inspiradas en FIBA:
  - 2 puntos si el último contacto fue dentro de la línea de tres.
  - 3 puntos si el último contacto fue desde más allá de 6,75 m.
  - 1 punto queda reservado para futuros tiros libres.

## Controles

| Acción | Tecla |
| --- | --- |
| Avanzar | W o flecha arriba |
| Strafe izquierda/derecha | A/D o flechas izquierda/derecha |
| Girar 180 grados | S o flecha abajo |
| Apuntar/girar fino | Q/E |
| Saltar | Espacio |
| Cargar y soltar tiro | K |
| Levantar balón para volea | J |
| Pasar | I |
| Robar | L |
| Barrida/robo fuerte | Shift + L, o robar mientras corres |
| Caminar | Ctrl |
| Correr | Shift |

## Mecánicas implementadas

- **Movimiento relativo al jugador**: W avanza hacia donde mira el jugador,
  A/D hacen strafe y Q/E permiten apuntar con precisión.
- **Tiro manual**: mantener K carga la potencia; soltar K dispara a 65 grados.
- **Dispersión de tiro**: tirar corriendo agrega error horizontal; tirar quieto
  es más preciso.
- **Volea**: J levanta el balón y abre una ventana breve para patearlo con K.
- **Cabeceo**: K golpea un balón suelto si está cerca y a altura de cabeza.
- **Pases**: I busca un compañero disponible y lanza un pase adelantado. La IA
  ofrece apoyos por delante, encadena pases progresivos y espera una mejor
  línea de tiro en lugar de resolver todas las posesiones individualmente.
- **Robos**: L disputa la pelota; corriendo o con Shift se convierte en barrida.
- **IA 2v2**: los jugadores controlados por IA persiguen, apoyan, defienden,
  pasan, roban, evitan postes y tiran con potencia calculada.
- **Reinicios**: el balón que sale por banda genera saque lateral. Por fondo,
  el equipo contrario al último toque ejecuta un saque de arco o córner según
  la jugada, con pase bajo al pie de un compañero.
- **Marcador**: suma puntos para Azul/Rojo y muestra el último enceste.

## Estructura del proyecto

- `scenes/main.tscn`: escena principal del juego. Contiene cancha, canastas,
  balón, cuatro jugadores, cámara, marcador y detectores de límites.
- `scripts/Player.cs`: movimiento, control del balón, tiro, pase, robo, cabeceo,
  animación simple de piernas e IA de jugadores.
- `scripts/Ball.cs`: física del balón, conducción, congelado para reinicios,
  último contacto y prevención de doble puntuación.
- `scripts/ScoreZone.cs`: detección de enceste al cruzar el plano del aro hacia
  abajo y cálculo de 2/3 puntos.
- `scripts/BoundaryDetector.cs`: detección de salidas y ejecución de saques.
- `scripts/ScoreboardUI.cs`: marcador de equipos y aviso del último puntaje.
- `scripts/RestartUI.cs`: aviso visual de saques.
- `scripts/PlayerCamera.cs`: cámara de seguimiento detrás del jugador humano.
- `scripts/CourtPainter.cs`: generación de líneas, pintura y marcas de cancha.
- `basket-foot.csproj` / `basket-foot.sln`: proyecto .NET para Godot.

## Stack técnico

- Godot 4.7 .NET edition.
- C# / .NET 8.
- Renderer: Forward Plus.
- Física: Jolt Physics.
- Driver Windows: D3D12.

## Verificación

Desde la raíz del proyecto:

```powershell
dotnet build
```

Para comprobar que la escena principal carga en modo headless:

```powershell
godot --headless --path . --quit-after 5
```

Si `godot` no está disponible en el PATH, usar el ejecutable de Godot 4.7 .NET
instalado localmente.

## Pendientes probables

- Pulir balance de IA, robos, pases y precisión de tiro.
- Ajustar física del balón y sensación de conducción.
- Agregar faltas y tiros libres.
- Mejorar presentación visual, audio, menús y flujo de partido.
- Probar visualmente en el editor y ajustar animaciones/cámara con gameplay real.
