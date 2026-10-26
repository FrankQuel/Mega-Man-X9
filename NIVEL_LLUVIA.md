# Fundición bajo la tormenta

Nivel jugable para Unity **6000.6.4f1**. Escena: [RainyFoundry.unity](Assets/Recursos/Scenes/Levels/RainyFoundry.unity). Es independiente del laboratorio de animaciones y utiliza los sprites, el HUD, la lluvia, el dragón articulado y el personaje axl original, ahora llamado **Axl**.

## Cómo jugar

Abre la escena y pulsa **Play**. El arranque reproduce primero el **video original de Capcom, de unos 11,35 segundos, sin opción de omitirlo**. Después aparece la portada con el logotipo y **Presionar Enter**. Volver al menú desde la partida o los resultados no repite el video. Los textos tienen un contorno oscuro grueso; Empezar, Opciones y Salir son letras seleccionables sin cajas, sobre la imagen oscurecida. Empezar lleva a la selección de X/Zero. Después de elegir al héroe aparece la guía del teclado suministrado con las teclas actuales resaltadas. Enter continúa hacia **0,55 segundos de negro**, las cuatro ilustraciones de Cinematic en orden uno/dos/tres/cuatro y una última página negra: **«X y Zero se dirigen a Central Highway.»** Después comienzan READY y la entrada del héroe elegido; X y Zero intercambian dos turnos con mugshots sobre la señal de Axl, y se habilita el juego. La escena está configurada como inicio para Play y añadida a Build Settings. La [guía del video](Tools/LevelPipeline/VIDEO_INICIO.md) explica la reproducción y su comprobación real.

Las palabras aparecen a **ocho por segundo**. Enter completa el texto actual; una pulsación posterior continúa. Cada marco se ajusta al tamaño real de su ilustración en pantalla, conservando la proporción y dejando negro fuera de la imagen. El inicio explica el descenso del elevador Jakob tras derrotar a Lumine, la recuperación de Axl, el estruendo que lleva a X y Zero hasta su sala destruida y la alerta que localiza su señal. La [guía narrativa](Tools/LevelPipeline/NARRATIVA_LLUVIA.md) documenta los recursos, el orden y los diálogos.

En Opciones, usa las flechas para seleccionar una acción, Enter para capturar y pulsa la nueva tecla. Los botones inferiores también admiten flechas y Enter. **Aplicar y volver** guarda los controles entre sesiones; **Volver sin cambios** o Escape descarta la edición. Una tecla ya ocupada intercambia ambas acciones. El mando mantiene su distribución. Salir cierra el juego; dentro del Editor detiene Play.

| Acción | Teclado | Mando |
|---|---|---|
| Mover / orientar | A/D o flechas | Stick izquierdo / cruceta |
| Saltar; mantener para ganar altura | Espacio o Z | Botón inferior |
| Atacar | J o X | Botón izquierdo |
| Cargar Buster de X | Mantener ataque y soltar | Mantener ataque y soltar |
| Dash de suelo / aire | K, C o Mayús | Botón derecho |
| Especial según dirección y estado | L o V | Botón superior |
| Cambiar X/Zero | Tab | Botón superior derecho RB/R 1 |
| Alternar armadura de X | U | Botón superior izquierdo LB/L 1 |
| Transformar a Zero en su forma potenciada | H | Select / View |
| Escalera / agacharse | W/S o arriba/abajo | Dirección vertical |
| Pausa | P o Escape | Start / Menu |
| Volver al checkpoint | R | Botón de reintento en pausa |

Los botones del mando se leen por posición mediante Input System; sus letras dependen del dispositivo.

## Cambio de personaje y combate

Tab conserva la posición, la velocidad, el dash, el estado de escalera y el lugar del recorrido. Se puede cambiar durante saltos, daño, escalada y combate contra el jefe. El personaje saliente se desmaterializa y sube en su rayo; el entrante baja en su rayo azul o rojo. **La partida hace una pausa breve mientras se reproduce la transformación**: física, enemigos, proyectiles y reloj quedan congelados y luego continúan. Los rayos se animan durante esa pausa. Cada forma mantiene su propia energía de **28 PV**. Cambiar no cura ni borra disparos emitidos.

Ambos comparten **tres vidas iniciales**. Una forma sin energía da paso automáticamente a la otra. Agotar las dos energías o caer en un foso consume una vida compartida; el reintento recupera ambas energías en el checkpoint. Con cero vidas aparece Game Over y se puede comenzar otra partida o regresar al menú. R permite un reintento manual de checkpoint. El HUD usa barras horizontales pequeñas a **escala 2**, con placas azules para X y rojas para Zero, círculo de estado y círculo de vidas compartidas. La barra del jefe cambia entre Dragón y Axl. Puntos, tiempo, checkpoint y bajas aparecen **solo en pausa**. Durante los diálogos se oculta el HUD para dejar espacio a los retratos y al texto.

X dispone de Buster normal, carga media/completa, disparos desde carrera, salto, dash, pared y escalera, y un especial de embestida. Arriba + especial realiza una maniobra ascendente. U transforma a X mediante su rayo y materializa la armadura definitiva; utiliza cuadros propios y efectos de propulsión. Su dash aéreo usa la animación **Dash**, sin sustituirla por Hover. El arte del dash y los proyectiles se orienta según la dirección real del desplazamiento. X y su armadura conservan su reposo de respiración con energía completa. La postura cansada aparece únicamente con **9 PV o menos**, sin activarse por esperar quieto. Los especiales energéticos de X usan el WAV **chargedShot**; las acciones de Zero conservan sonido de sable.

Mantener abajo en suelo agacha a las cuatro variantes con una pose estable: **CrouchIdle, cuadro 0**, para X; **DefeatCollapse, cuadro 1**, para Ultimate; **DashSlash, cuadro 0**, para Zero y **Enhanced_GroundSlideSlash, cuadro 0**, para Zero potenciado. X puede disparar agachado sin abandonar la pose. Crouch_Review_2.png documenta la revisión de los cuadros propios y su orientación. Ultimate usa su postura de rodilla porque su banco no incluye CrouchIdle.

Zero encadena tres golpes de sable pulsando de nuevo durante un ataque. Sus golpes cambian en aire, pared y dash. Arriba + especial ejecuta un golpe ascendente; abajo + especial lanza una onda en suelo o realiza un ataque descendente en aire. H transforma a Zero con rayo rojo y la pose de aura proporcionada, seguida de sus cuadros potenciados. **PinkFlameAura permanece activa mientras conserve esta forma**, también después de recibir daño y al volver a Zero tras cambiar a X; no se añade IceRing. Con esta forma, **especial sin dirección tanto en suelo como en aire** ejecuta el mismo definitivo: elevación y carga, aura rosa, golpe de energía vertical y gran onda verde de sable. Las poses AirPoseRaised/AirPoseLowered se reflejan y la orientación queda fijada durante la acción. La onda sale a unos **0,64 s**, frente a unos 2,14 s antes, y la secuencia dura aproximadamente **1,24 s**; conserva ocho segundos de recuperación. No se sustituye por SpinSlash al hacerlo en el aire. Los otros especiales recuperan en 3,5 segundos.

El agarre de pared mantiene el cuadro de contacto 3 de WallJump en los bancos X y Ultimate y de AirUppercutPose (o su equivalente potenciado) en Zero. Las poses de escalera no se utilizan al deslizarse por pared. La carrera baja a 3,25 unidades/s; el dash a 7. El salto completo alcanza aproximadamente 1,85 unidades (impulso 10, gravedad 27); las plataformas obligatorias se adaptaron a ascensos de hasta 1,4. Las animaciones de movimiento se reproducen al 80% y los ataques al 85% de su ritmo anterior.

El fuego y los tiros tienen propietario, daño y vida útil. Hay protección breve tras recibir un golpe; cambiar de forma no renueva esa protección. Los enemigos ordinarios causan 3 de daño y los voladores 2. Cada partida usa una semilla nueva para sus drops: **65% puntos, 25% nada y 10% salud pequeña**. Los tanques grandes y las vidas extra son recompensas fijas en rutas elevadas con enemigos; se usan las cápsulas originales y los iconos animados X/Zero. Cada recompensa tiene un identificador y no vuelve a dar recursos al reintentar dentro de la misma partida. **Los orbes azules dan puntos y no curan**. Cada enemigo derrotado y cada orbe de puntos dan 100.

## Recorrido y objetivos

El recorrido tiene **264 unidades**, **17 enemigos** ordinarios de cuatro comportamientos, **ocho escaleras**, **seis checkpoints**, peligros eléctricos temporizados y fosos. Los tramos van desde drenajes a −2 hasta la cima de una torre a 9,0. El encuentro final reúne dos jefes consecutivos: **Dragón, con 240 PV**, y **Axl, con 360 PV e IA agresiva**. Axl usa el banco Axl original y escala **1,7**, frente a **2,5** del Dragón y **2** de los héroes; su dibujo conserva sus propias proporciones.

1. **Azoteas:** caminar, saltar sobre cajas, disparar y cruzar el primer drenaje. La pasarela pequeña deja espacio de carrera antes del salto.
2. **Mantenimiento:** subir por escalera o practicar saltos de pared junto al muro. Los desembarcos tienen suelo continuo; no exigen pulsar lateral en un instante preciso.
3. **Condensadores y drenaje:** subir por pasarelas hasta 7,6 y bajar por plataformas hasta el drenaje. Combina saltos, dash aéreo y ataques contra tiradores y voladores.
4. **Hornos y torres:** ascensos por plataformas, maquinaria electrificada y descensos entre torres. La cima llega a 9,0 y la cámara sigue la altura sin repetir la ciudad sobre el cielo negro.
5. **Acceso al jefe:** checkpoint antes del reactor y puerta original animada en x 236,5. Se desbloquea al acercarse. Al entrar en la arena aparece WARNING; después cae el dragón desde arriba, aterriza y comienza el combate.
6. **Dragón:** decide ataques por distancia y altura, corre para acercarse y acelera al bajar de media vida. Fuego de boca y manos, onda de suelo, garra, uppercut y patada tienen daño real. El tren de fuego avanza hasta una pared física o el límite del mapa, sin caducar en mitad de la arena ni desaparecer por tocar el suelo. Su presentación combina poses completas originales con el rig y sockets de emisión. El multiplicador del daño recibido subió de **0,5 a 0,65**, con redondeo hacia arriba y un mínimo de uno por impacto aceptado; conserva protección breve entre golpes. El contacto causa 4–5 PV y los ataques de fuego parten de 5 PV. **Al llegar a 60 PV se detiene el combate**: Axl aparece, dispara y elimina al Dragón.
7. **Axl y Lumine:** todos quedan en reposo y la escena se oscurece sin volverse negra. Aparecen los mugshots de X/Zero a la izquierda y Axl a la derecha, cada uno con su cuadro de diálogo. En una conversación breve descubren que **Lumine controla el cuerpo de Axl**. Los turnos avanzan con Enter. Tras el diálogo aparece **WARNING para Axl** y se recargan ambas energías de X/Zero y la de Axl, que comienza con **360 PV**. Usa ráfagas dirigidas, carga, embestidas y ataques desde el aire. Su multiplicador de daño recibido subió a **1,1**: acepta 11 PV de un golpe nominal de 10, con redondeo e inmunidad breve. Dragón conserva 0,65.
8. **Final automático:** Axl permanece vivo a **1 PV** y el combate se detiene. Llega el compañero opuesto al héroe activo —Zero si se juega con X, o X si se juega con Zero— y vuelve el diálogo con mugshots. Lumine revela que se introdujo en Axl antes de ser derrotado y anuncia que volverán a encontrarse. Axl se retira primero y después salen ambos héroes. La misión termina **sin puerta de salida ni cruce de un arco final**.
9. **Resultados:** se usa el fondo del menú oscurecido, letras con contorno y la música Results theme en bucle. Muestra puntuación, tiempo y enemigos derrotados, sin cajas ni botón de reintentar. **Presiona Enter** devuelve al menú principal.

La lluvia usa ahora **una sola capa continua de shader**, sin los cortes entre mosaicos. Los trazos siguen coordenadas del mundo, terminan en el suelo inferior y se recortan dentro de cada volumen sólido. La lluvia permanece en el aire bajo plataformas y el techo de la arena, sin atravesar su geometría. Los fotogramas originales se conservan como referencia. La [guía de lluvia](Tools/WeatherPipeline/LLUVIA_CONTINUA.md) explica el recorte y sus límites. La ciudad ocupa una sola franja de parallax repetida horizontalmente; el cielo superior es negro. La cámara mantiene el último suelo seguro al caer hacia un vacío y permite descender cuando existe terreno real debajo. El [mapa del recorrido](Tools/LevelPipeline/LEVEL_LAYOUT.md) incluye plataformas, escaleras y recompensas.

El modo global inicial es **Producción**, con solo el HUD durante la acción. **Mega Man X9 > Presentation Mode > Development** activa controles, notificaciones, medidores de carga/poder y balizas de prueba. Los puntos, vidas y checkpoints siguen funcionando en ambos modos; sus datos se consultan en pausa. La [guía de presentación](Tools/LevelPipeline/MODO_PRESENTACION.md) explica la configuración independiente. Los edificios, faroles y pinchos usan las nuevas hojas originales.

La pausa usa los paneles azul y rojo y la letra de **menu de pausa.png**. Muestra ambos héroes con salud, vidas compartidas, puntos, tiempo y checkpoint; sus acciones admiten ratón, flechas y Enter, además de P/Escape para continuar. La [documentación de la pausa original](Tools/LevelPipeline/MENU_VIDAS_Y_CONTROLES.md#pausa-con-el-arte-original) detalla sus 37 glifos extraídos y los datos visibles.

## Audio

Se integran **33 WAV y cuatro OGG** suministrados, con sus nombres de archivo y aliases de acción en un banco de Resources. Las **seis músicas se reproducen en bucle**: Main theme en menús, Main level theme en el nivel, Dragon boss theme contra el Dragón, Axl boss theme contra Axl, Conversation music durante introducción y diálogos y Results theme en resultados. El volumen base de música subió de **0,32 a 0,43**. Sonido de lluvia tiene un canal de ambiente separado; los efectos nominales cubren movimiento, Buster, sable, daño, recuperación, entradas/salidas, puerta, READY, WARNING y menús. X usa chargedShot para sus especiales energéticos y Zero usa sus grabaciones de sable. Los casos sin grabación propia tienen un sonido de respaldo. El [detalle de audio](Tools/AudioPipeline/AUDIO_INTEGRACION.md) relaciona cues, grabaciones y niveles de volumen.

## Animaciones

Las acciones seleccionan cuadros por contexto; no se reproducen todas como una lista durante la partida. X, su armadura y Zero comparten el motor físico y usan bancos propios. La variante potenciada de Zero conserva su banco y elige estados Enhanced. Los tiempos de acciones ajustan la velocidad del clip para mostrar preparación, golpe y recuperación sin recortar arbitrariamente la secuencia.

La [matriz de acciones](Tools/LevelPipeline/ANIMACIONES_JUGABLES.md) explica las familias utilizadas. Los personajes ajenos a este nivel, las piezas sueltas y poses de extracción permanecen en la biblioteca y en el laboratorio; no se convierten en enemigos adicionales ni en otros jefes. La hoja blanca original no se modifica.

## Reconstrucción y comprobación

Fuera de Play: **Mega Man X9 > Playable Rain Level > Build Rainy Foundry** reconstruye únicamente este nivel y sus referencias. **Build and Test Playable Level** además entra en Play y comprueba física y flujo de partida. Antes de reconstruir se respaldan escenas abiertas con cambios en `Assets/Recursos/Scenes/Levels/WorkspaceBackups`.

La ejecución completa guardada el **25/10/2026 a las 22:41:03** pasó **251 comprobaciones nativas de integración** y generó **69 capturas**. Incluye fotogramas reales del video de 11,35 s y rechazo de teclas para omitirlo; agachado de las cuatro variantes y disparo agachado de X; definitivo de Zero en suelo/aire y ambos sentidos, con onda antes de 0,9 s y sonido de sable; pausa con arte, glifos, datos y teclado; lluvia continua con recorte; y mayor daño recibido por Axl. El [informe de QA](Tools/LevelPipeline/playtest_report.json) registra el alcance funcional; [las capturas](Previews/RainyFoundry) permiten revisar la presentación. La [validación de sprites](Tools/AnimationPipeline/python_validation.json) conserva **34 archivos originales sin cambios** y la [biblioteca](Tools/AnimationPipeline/rain_level_asset_report.json) mantiene **361 entradas**.

La suite sitúa al personaje en puntos de prueba y utiliza entrada simulada en el **controlador y la física reales** para comprobar saltos, colisiones, escaleras, pared, cámara, cambio de forma, transformaciones y combate. También envía eventos de teclado virtual a Input System para menú, opciones y controles. Comprueba que los proyectiles reales del héroe dañen a **ambos jefes**; después usa el **contrato público de daño** para alcanzar de forma acotada los umbrales de 60 PV y 1 PV y verificar intervención, recarga, compañero opuesto y final automático. Incluye pausa y cancelación de las escenas al reintentar. **No es una partida humana completa ni una prueba física del teclado**: el balance de dificultad requiere jugar el recorrido entero.

La [revisión adicional de presentación y sonido](Tools/LevelPipeline/presentation_report.json), terminada a las **22:41:28**, registra **tres comprobaciones de sonido** desde acciones reales: aterrizaje, especial energético de X con chargedShot y disparo enemigo. Recorre además introducción, diálogos, WARNING y resultados. Su fecha y las fechas de las capturas permiten distinguirla de la ejecución completa.

## Archivos principales

- [RainHeroController.cs](Assets/Recursos/Scripts/Gameplay/RainHeroController.cs): entrada, física, formas, acciones y presentación del héroe.
- [RainLevelGame.cs](Assets/Recursos/Scripts/Gameplay/RainLevelGame.cs) y [RainBossCinematic.cs](Assets/Recursos/Scripts/Gameplay/RainBossCinematic.cs): READY, sesión, vidas, reintentos y secuencia final.
- [RainNarrative.cs](Assets/Recursos/Scripts/Gameplay/RainNarrative.cs) y [RainNarrativeAssets.cs](Assets/Recursos/Scripts/Gameplay/RainNarrativeAssets.cs): ilustraciones, mugshots, palabras y turnos de diálogo.
- [RainEnemy.cs](Assets/Recursos/Scripts/Gameplay/RainEnemy.cs), [RainDragonBoss.cs](Assets/Recursos/Scripts/Gameplay/RainDragonBoss.cs) y [RainAxlBoss.cs](Assets/Recursos/Scripts/Gameplay/RainAxlBoss.cs): enemigos y dos jefes.
- [RainLevelWorld.cs](Assets/Recursos/Scripts/Gameplay/RainLevelWorld.cs), [RainLevelCamera.cs](Assets/Recursos/Scripts/Gameplay/RainLevelCamera.cs) y [RainWeather.cs](Assets/Recursos/Scripts/Gameplay/RainWeather.cs): recorrido, cámara y lluvia.
- [RainLevelHUD.cs](Assets/Recursos/Scripts/Gameplay/RainLevelHUD.cs), [RainPickup.cs](Assets/Recursos/Scripts/Gameplay/RainPickup.cs) y [RainLevelAudio.cs](Assets/Recursos/Scripts/Gameplay/RainLevelAudio.cs): HUD, objetos y audio.
- [RainAudioAssets.cs](Assets/Recursos/Scripts/Gameplay/RainAudioAssets.cs) y [RainOutlinedGUI.cs](Assets/Recursos/Scripts/Gameplay/RainOutlinedGUI.cs): banco de sonidos y contorno de letras.
- [RainLevelBuilder.cs](Assets/Recursos/Editor/Gameplay/RainLevelBuilder.cs) y [RainLevelValidation.cs](Assets/Recursos/Editor/Gameplay/RainLevelValidation.cs): construcción y prueba nativa.

Las reglas estudiadas en los cuatro repositorios guiaron la separación de estado, presentación y daño. Los componentes de este nivel son implementación C# propia; no dependen de GameMaker ni de un runtime de recompilación PSX.





