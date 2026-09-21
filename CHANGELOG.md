# Changelog

Este archivo registra los cambios relevantes de **basket-foot**.

El formato se basa en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/)
y el proyecto adopta [Versionado Semántico](https://semver.org/lang/es/). Los
hitos versionados hasta ahora se reconstruyeron retrospectivamente a partir del
historial de Git; todavía no representan lanzamientos etiquetados.

## [0.6.0-dev] - En desarrollo

### Documentación

- README actualizado para reflejar el estado jugable real, los controles, las
  mecánicas implementadas, la estructura del proyecto y los próximos objetivos.
- Creado este changelog para registrar versiones y progreso futuro.

### Cambiado

- Asignado un aro defensivo fijo y un aro de ataque contrario a cada equipo.
- La IA usa la asignación de aros para atacar, apoyar y replegarse en defensa.
- Cada zona de enceste acredita los puntos al equipo atacante correspondiente;
  los encestes en el aro propio cuentan para el rival.

### Corregido

- Corregida la puntuación de los aros: el aro sur suma para Azul y el aro norte
  para Rojo, con una resolución de respaldo si la escena pierde la configuración.

### Mejorado

- La IA busca apoyos por delante y abiertos para construir ataques mediante
  pases progresivos.
- Aumentada la frecuencia de los pases cuando el receptor mejora la posición y
  la línea está razonablemente libre.
- Añadido un intervalo entre pases para evitar devoluciones instantáneas y
  permitir secuencias de pase, recepción y nuevo pase.
- Los saques de fondo ahora identifican al último equipo que tocó el balón,
  distinguen saque de arco y córner, y buscan un compañero para el pase bajo.

### Planeado

- Balancear la IA, los robos, los pases y la precisión de tiro.
- Ajustar la física del balón y la sensación de conducción.
- Implementar faltas y tiros libres de 1 punto.
- Incorporar audio, menús y un flujo completo de partido.
- Continuar el pulido visual de animaciones, cámara y escenario.

## [0.5.0] - 2026-09-13

### Añadido

- Reinicios de juego por salidas de banda y de fondo.
- Preparación y posicionamiento de jugadores para ejecutar los saques.
- Nuevas acciones de pase y barrida mediante entradas dedicadas.

### Mejorado

- Juego colectivo de la IA con roles de ataque, apoyo y defensa.
- Selección de compañeros, pases adelantados y recuperación de balones sueltos.
- Comportamiento defensivo, robos y evasión de postes.
- Control del balón durante interrupciones y reanudaciones.

## [0.4.0] - 2026-09-09

### Añadido

- Partido 2v2 entre los equipos Azul y Rojo.
- Compañero controlado por IA para el jugador humano.
- Marcador para ambos equipos y aviso visual del último enceste.
- Detección de límites de la cancha y avisos de reinicio.
- Pases entre compañeros, robos y robos fuertes en carrera.
- Pintado procedural de líneas, zonas y marcas de la cancha.

### Cambiado

- Cada equipo pasó a atacar una canasta fija.
- La IA comenzó a distinguir compañeros, rivales y funciones de equipo.
- La lógica de puntuación pasó a atribuir los puntos al equipo del último toque.

## [0.3.0] - 2026-08-23

### Añadido

- Cancha reglamentaria de futsal de 40 x 20 metros.
- Cabeceo de balones sueltos dentro de un rango de altura y distancia.
- Animación procedural de piernas al caminar y correr.
- Geometría auxiliar para dibujar semicírculos de la cancha.

### Mejorado

- Movimiento relativo a la orientación, incluyendo strafe y desplazamiento
  hacia atrás.
- IA de persecución, control, posicionamiento y tiro.
- Apariencia de los jugadores mediante maniquíes construidos con primitivas 3D.

## [0.2.0] - 2026-08-16

### Añadido

- Cámara de persecución detrás del jugador.
- Segundo jugador controlado por IA.
- Giro y apuntado fino de 360 grados con Q/E.
- Dispersión horizontal al tirar en movimiento.
- Cálculo de potencia de tiro para que la IA alcance la canasta.

### Mejorado

- Orientación visual del jugador y dirección de los disparos.
- Seguimiento del balón mientras está controlado.
- Comportamiento de tiro y aproximación de la IA.

### Legal

- Proyecto licenciado bajo PolyForm Noncommercial 1.0.0.

## [0.1.0] - 2026-08-09

### Añadido

- Primer prototipo jugable en Godot 4.7 .NET y C#.
- Cancha 3D, dos canastas, jugador y balón con física Jolt.
- Patada básica y puntuación FIBA de 2 o 3 puntos según la distancia del último
  contacto.
- Tiro cargable con potencia mínima y máxima.
- Elevación del balón y ventana de volea.
- Detección válida de enceste al cruzar hacia abajo el plano del aro.
- Suelo exterior y respawn de seguridad para el jugador y el balón.

[0.6.0-dev]: https://github.com/jzorrilla-dev/basket-foot/compare/724dfea...HEAD
[0.5.0]: https://github.com/jzorrilla-dev/basket-foot/compare/cf4c2d9...724dfea
[0.4.0]: https://github.com/jzorrilla-dev/basket-foot/compare/08a99a5...cf4c2d9
[0.3.0]: https://github.com/jzorrilla-dev/basket-foot/compare/d3ebdd5...08a99a5
[0.2.0]: https://github.com/jzorrilla-dev/basket-foot/compare/4c75949...d3ebdd5
[0.1.0]: https://github.com/jzorrilla-dev/basket-foot/commit/4c75949
