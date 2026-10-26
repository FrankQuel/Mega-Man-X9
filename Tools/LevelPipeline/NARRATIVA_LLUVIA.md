# Introducción y conversaciones

La narrativa usa [RainNarrative.cs](../../Assets/Recursos/Scripts/Gameplay/RainNarrative.cs), [RainNarrativeAssets.cs](../../Assets/Recursos/Scripts/Gameplay/RainNarrativeAssets.cs) y el [builder de recursos](../../Assets/Recursos/Editor/Gameplay/RainNarrativeAssetsBuilder.cs). No depende de un sistema externo de diálogos ni modifica las ilustraciones o sus importadores.

## Recursos y orden

El builder carga expresamente las cuatro imágenes en orden numérico español, sin ordenarlas alfabéticamente:

1. [Cinematica uno](../../Assets/Recursos/Scenes/Cinematic/Cinematica%20uno.png): X lleva a Axl y Zero espera junto al elevador. El texto sitúa el descenso del elevador Jakob tras derrotar a Lumine.
2. [Cinematica dos](../../Assets/Recursos/Scenes/Cinematic/Cinematica%20dos.png): Axl está en una cápsula médica. La explicación presenta su recuperación y la espera de X y Zero.
3. [Cinematica tres](../../Assets/Recursos/Scenes/Cinematic/Cinematica%20tres.png): X y Zero observan la sala destrozada y una gran brecha en la pared. Un estruendo los conduce allí y descubren que Axl ha desaparecido.
4. [Cinematica cuatro](../../Assets/Recursos/Scenes/Cinematic/Cinematica%20cuatro.png): ambos revisan un mapa con una alerta de ubicación. Detectan la señal de Axl y preparan el rescate.
5. Página negra: «X y Zero se dirigen a Central Highway.» El callback se ejecuta tras continuar desde esta página y cerrar su transición.

Los retratos son [x.png](../../Assets/Recursos/Sprites/Mugshot/x.png), [zero.png](../../Assets/Recursos/Sprites/Mugshot/zero.png) y [axl.png](../../Assets/Recursos/Sprites/Mugshot/axl.png). El último muestra el Axl axl/verde utilizado en el encuentro. Los tres se reflejan únicamente al dibujarlos para mirar hacia el centro; los archivos permanecen intactos.

## Presentación y texto

La introducción empieza con 0.55 segundos de negro, seguidos de una entrada suave. Las ilustraciones conservan su proporción dentro de un marco centrado arriba, con márgenes negros; el texto aparece en un recuadro inferior. La última página permanece negra y muestra la frase de destino centrada.

Las conversaciones oscurecen la escena con negro al 65%, sin ocultarla por completo. X y Zero tienen paneles separados a la izquierda y Axl uno a la derecha. Cada panel contiene nombre, retrato y texto. Solo el interlocutor activo se enfatiza; los demás conservan atenuada su última intervención. La tipografía usa el helper compartido `RainOutlinedGUI.Label`.

El texto se revela por **palabras completas**, a 8 palabras por segundo de forma predeterminada. Usa tiempo no escalado, pero `Game.IsPaused` detiene tanto palabras como transiciones. Una pulsación de Enter completa la frase y una pulsación posterior continúa. No hay avance automático ni salto durante la pausa o las transiciones. Enter mantenido no genera páginas adicionales; el avance manual también admite como máximo una acción por frame.

La confrontación tiene 7 turnos: X y Zero llaman a Axl, preguntan por el ataque, Axl ríe, reconocen la presencia de Lumine y este amenaza con quedarse con su cuerpo. El cierre tiene 6 turnos: preguntan cómo sobrevivió, Lumine admite haberse introducido en Axl antes de su derrota, anuncia que volverán a encontrarse y X/Zero prometen rescatar a su compañero. Son diálogos propios escritos para esta secuencia.

## Contrato de integración y QA

`RainNarrativeAssetsBuilder.Build()` es idempotente, devuelve el asset y conserva su GUID al reconstruirlo. Escribe `Assets/Recursos/Resources/RainNarrativeAssets.asset`; debe ejecutarse antes de iniciar una escena que configure la narrativa. Su menú es **Mega Man X9 > Playable Rain Level > Build Narrative Assets**. `RainNarrativeAssets.Load()` lo carga desde Resources.

El componente expone:

- `Configure(game)` y `PlayIntroduction(completed)`.
- `PlayConfrontation(ending, completed)`: `false` para la confrontación y `true` para el diálogo final.
- `PlayMissionArrival(initial, completed)` e `IsMissionArrival`: dos turnos entre el héroe inicial y su compañero, después de READY y la materialización, antes de Playing. El activo dice «La señal de Axl está más adelante» y el otro responde «Entonces vamos a buscarlo». Solo aparecen los paneles de X y Zero.
- `Cancel()`: cancela sin ejecutar el callback anterior y libera el modo de conversación del audio.
- `Advance()`: usa exactamente el mismo camino que Enter; respeta pausa, transición y una acción por frame.
- `IsActive`, `IsIntroduction`, `IsEndingDialogue`, `IsTransitioning`, `IsTextComplete`.
- `CurrentPage` con índice desde 0; `PageCount` de 5/7/6 en introducción/confrontación/cierre y 2 en llegada; `CurrentText`, `VisibleText`, `RevealedWordCount`, alias `RevealedWords` y `TotalWordCount`.
- `CurrentImage`, `Assets` y `ActiveSpeaker` para inspeccionar los recursos y el interlocutor sin alterar el recorrido.

Al comenzar y terminar se llama a `RainLevelAudio.SetConversation(bool)`. Game conserva la autoridad sobre bloqueo de entrada, menús, pausas y callbacks de READY, WARNING, segundo combate y despedida. Este componente no cambia la escala de tiempo global ni controla la vida de los actores.

Para QA, esperar a `!IsTransitioning`, observar el aumento de palabras, pulsar Enter una vez y comprobar que la página no cambió; pulsarlo de nuevo y comprobar la siguiente página. Las páginas 0–3 exponen las imágenes uno/dos/tres/cuatro; la 4 expone `CurrentImage=null` y la frase de Central Highway. En diálogos, se puede llamar a `Advance()` en frames separados para reproducir los mismos pasos, sin omitir el texto por una vía alternativa. Pausa y Cancel deben detener el avance y evitar callbacks atrasados.

Validación local: inspección visual de las cuatro ilustraciones y los tres retratos, comprobación de sus rutas y compilación C# integral de Assets+Editor con 0 errores. La validación nativa en Unity corresponde al orquestador; este cambio no inicia Unity ni reconstruye escenas por su cuenta.
