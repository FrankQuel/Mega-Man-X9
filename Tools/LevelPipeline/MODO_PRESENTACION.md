# Presentación: Producción y Desarrollo

El recurso global Assets/Recursos/Resources/RainPresentationSettings.asset conserva el modo seleccionado. Su valor inicial es Production. Se cambia desde **Mega Man X9 > Presentation Mode > Production** o **Development**, fuera o dentro de Play. El builder conserva la elección al reconstruir el nivel.

RainPresentationSettings.IsDevelopment y ShowGameplayHelpers permiten consultar el modo desde otros componentes. SetMode cambia el valor de la instancia; Reload vuelve a cargar la referencia. No afecta a la salud, puntos, vidas, checkpoints ni a las reglas del juego.

En Producción se dibuja el HUD de salud de X/Zero y del jefe. Se ocultan instrucciones de control, mensajes transitorios de puntos/vidas/checkpoints, medidor de carga, recuperación de poderes, botón de pausa y balizas de depuración. La pausa mantiene salud, vidas, puntos, tiempo y checkpoint. READY, WARNING, diálogos y resultados conservan sus secuencias.

Desarrollo restaura las ayudas anteriores y los gizmos seleccionados del recorrido. El menú normal, las cinemáticas y la guía previa del teclado no muestran el HUD de juego.

## Figura blanca en el editor

La figura blanca lateral era un gizmo del editor. Desactivar **Gizmos** en la barra de la vista **Game** la elimina; se comprobó visualmente en el editor abierto. Este interruptor pertenece a Unity, no al juego compilado. El modo global de presentación no cambia las preferencias de gizmos del editor.

## Opciones con teclado

Flechas arriba/abajo recorren las 13 acciones y los tres botones inferiores. Enter inicia la captura de la tecla elegida o activa Aplicar, Volver o Predeterminados. Escape cancela la captura; fuera de captura vuelve sin guardar. El Enter que abre la captura debe soltarse antes de poder asignar otra tecla. La guía de teclado posterior a selección usa las teclas realmente guardadas.
