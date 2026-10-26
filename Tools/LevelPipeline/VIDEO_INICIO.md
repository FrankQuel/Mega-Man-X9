# Video previo a la portada

El arranque usa el [MP4 original](../../Assets/Recursos/UI/Intro/Capcom%20Logo%20%28Mega%20Man%20X%20Collection%29.mp4), antes de mostrar la portada. No tiene botones, teclas ni método público para omitirlo. Enter, Escape, pausa o reintento no deben adelantarlo; Game mantiene cerrados los menús mientras `IsPlaying` sea verdadero.

El contenedor del archivo suministrado registra **11.3546 segundos**, imagen **478×360**, una pista de video y otra de audio, con 412496 bytes. La espera no usa una duración fija de 10 segundos: termina exclusivamente al recibir `VideoPlayer.loopPointReached` del archivo realmente importado.

## Componentes y recursos

- [RainStartupVideo.cs](../../Assets/Recursos/Scripts/Gameplay/RainStartupVideo.cs): VideoPlayer, salida RenderTexture, audio directo del VideoPlayer y overlay sobre negro.
- [RainStartupVideoAssets.cs](../../Assets/Recursos/Scripts/Gameplay/RainStartupVideoAssets.cs): referencia al VideoClip y volumen, cargados mediante Resources.
- [RainStartupVideoAssetsBuilder.cs](../../Assets/Recursos/Editor/Gameplay/RainStartupVideoAssetsBuilder.cs): importa el archivo si hace falta y crea/actualiza `Assets/Recursos/Resources/RainStartupVideoAssets.asset`, conservando su GUID. No modifica los bytes del MP4 ni sus opciones de importación.

Ejecutar `RainStartupVideoAssetsBuilder.Build()` antes de iniciar Play. Devuelve el asset y guarda únicamente ese recurso. Menú: **Mega Man X9 > Playable Rain Level > Build Startup Video Assets**. La importación y el asset generado los realiza Unity cuando el orquestador invoca el builder; no se ejecuta Unity desde este cambio.

`Configure()` prepara las referencias y componentes; `Play(completed)` inicia una reproducción desde el principio. `IsPlaying` también cubre la preparación. `HasCompleted` solo se activa por el evento de final; el callback no depende de un temporizador. Se exponen `IsPrepared`, `Duration`, `CurrentFrame`, `VideoClip` y `Error` para inspección nativa. El video usa tiempo no escalado, velocidad 1, `isLooping=false`, `skipOnDrop=false` y espera al primer fotograma preparado.

La presentación cubre la pantalla con negro y ajusta el video a su proporción completa, sin recortarlo ni deformarlo. El audio propio usa VideoAudioOutputMode.Direct. Al terminar se detiene y deshabilita el decodificador, evitando que siga acumulando muestras después del video. No se reproduce mediante el bus de música del nivel. Game debe silenciar los otros buses durante el arranque.

Los errores del decodificador se muestran en axl sobre negro y permanecen disponibles en `Error`. Un fallo de preparación tras 20 segundos también se muestra; **no abre el menú ni simula una finalización**. El bloqueo permanece activo para que el error no se convierta en una omisión silenciosa.

## Marco de las ilustraciones narrativas

El único cambio en [RainNarrative.cs](../../Assets/Recursos/Scripts/Gameplay/RainNarrative.cs) está en `DrawIntroduction`: primero calcula el rectángulo ajustado a la proporción de `CurrentImage` dentro de los límites superiores y después dibuja su marco. Cada ilustración tiene un contorno de su tamaño real en pantalla; los márgenes negros exteriores quedan fuera del marco. La caja de texto inferior conserva su posición.

## Verificación

La inspección local comprueba metadatos del MP4, rutas, metas y compilación C#. La QA nativa debe comprobar que `CurrentFrame` avanza, que hay preparación real y que Enter/Escape durante la reproducción no activan la portada. Después debe esperar `HasCompleted` y el callback del evento final, con una duración de reproducción coherente con `VideoClip.length`. No se expone un atajo de QA para evitar el video.

