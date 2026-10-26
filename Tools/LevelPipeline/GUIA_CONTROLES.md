# Guía de teclado antes de la introducción

La guía utiliza [teclado.png](../../Assets/Recursos/UI/Controls/teclado.png), inspeccionado a su tamaño original de 1200×444. El [builder](../../Assets/Recursos/Editor/Gameplay/RainControlsGuideAssetsBuilder.cs) contiene 104 rectángulos de teclas, en píxeles desde la esquina superior izquierda. No modifica la imagen ni su importador.

[RainControlsGuide](../../Assets/Recursos/Scripts/Gameplay/RainControlsGuide.cs) dibuja el teclado y marca las 13 teclas principales que devuelve `RainInputBindings.CopyBindings()`. Cada marca tiene un número y color que coincide con la leyenda de acciones. La configuración se vuelve a leer mientras está abierta, incluidos remapeos e intercambios de teclas guardados en Opciones.

| Acción | Tecla principal inicial |
|---|---|
| Mover a la izquierda / derecha | A / D |
| Subir o apuntar / bajar o agacharse | W / S |
| Saltar | Espacio |
| Atacar o cargar | J |
| Dash | K |
| Especial | L |
| Cambiar X / Zero | Tab |
| Armadura de X | U |
| Poder de Zero | H |
| Pausa | P |
| Reintentar | R |

La tabla describe los valores iniciales; la pantalla muestra los valores actuales. Se resaltan las teclas principales. Los alias del controlador siguen sujetos a las reglas de `RainInputBindings`. Si una tecla de la configuración no aparece en la imagen, su nombre y acción se muestran con la indicación «solo leyenda», sin marcar otra tecla incorrectamente.

## Integración

`RainControlsGuideAssetsBuilder.Build()` devuelve y guarda `Assets/Recursos/Resources/RainControlsGuideAssets.asset`, conservando su GUID. Debe ejecutarse antes de jugar una escena que configure la guía. Su menú es **Mega Man X9 > Playable Rain Level > Build Controls Guide Assets**. El [asset de datos](../../Assets/Recursos/Scripts/Gameplay/RainControlsGuideAssets.cs) contiene la textura, tamaño nativo y mapa de rectángulos.

APIs: `Configure(game)`, `Play(completed)`, `Cancel()`, `Advance()`, `IsActive` y `DisplayedBindingCount`. Para QA también se exponen `GetDisplayedKey(action)`, `TryGetDisplayedKeyRect(action, out rect)`, `HighlightedBindingCount`, `IsWaitingForEnterRelease` y `Assets`.

Game conserva la autoridad sobre entrada, menús, pausa y flujo: selección de personaje → guía → introducción ilustrada → READY/materialización → diálogo de llegada → Playing. La guía exige soltar el Enter que confirmó la selección antes de aceptar una nueva pulsación. `Advance()` sigue el mismo guardado de entrada que el teclado, respeta pausa y no puede aceptar dos avances en un frame. `Cancel()` elimina el callback anterior.

## Diálogo al llegar

[RainNarrative.PlayMissionArrival](../../Assets/Recursos/Scripts/Gameplay/RainNarrative.cs) recibe el héroe inicial y un callback. Tiene dos turnos con los retratos de X y Zero:

1. El héroe activo: «La señal de Axl está más adelante.»
2. El compañero contrario: «Entonces vamos a buscarlo.»

`IsMissionArrival` identifica esta secuencia. No aparece el panel de Axl. El avance por palabras, Enter, pausa y cancelación usa el mismo mecanismo que los otros diálogos. Los textos y el orden X→Zero o Zero→X se guardan en [RainNarrativeAssetsBuilder](../../Assets/Recursos/Editor/Gameplay/RainNarrativeAssetsBuilder.cs); hay que reconstruir también esos recursos antes de Play. Game mantiene al actor en Idle y habilita Playing después del callback; los reintentos y el inicio técnico de QA deciden su flujo desde Game.

## Verificación

La validación local revisa los rectángulos contra el PNG, la unicidad de sus 104 claves y la compilación integral de C#. La QA nativa debe comprobar una tecla remapeada, el contador 13, una tecla de fallback, Enter real para continuar y la secuencia de dos turnos con ambos personajes iniciales. No se inicia Unity desde estos componentes ni sus builders.

La figura blanca lateral se confirmó como gizmo del editor: desapareció al desactivar Gizmos en la vista Game. Las capturas finales de guía y diálogos ya no la muestran. No forma parte del juego compilado.
