# Rainy Foundry: ruta de 264 unidades

Versión del diseño: 25 de octubre de 2026, ajustada al movimiento 3.25/7.0, salto 10 y gravedad 27. La escena la genera [RainLevelBuilder.cs](../../Assets/Recursos/Editor/Gameplay/RainLevelBuilder.cs). La ejecución completa de las **22:41:03** pasó **251 comprobaciones nativas con 69 capturas**; el [informe](playtest_report.json) registra `success=true`. Las fórmulas siguientes son aproximaciones estáticas y se distinguen de las muestras físicas verificadas.

## Recorrido y dificultad

| Tramo X | Ruta principal y variación vertical | Recuperación |
|---|---|---|
| 0–40 | Azotea de inicio a Y 0; cajas de práctica a Y 0.8→1.1→1.8; foso 26–28 y dos enemigos. La plataforma baja termina en 24.2 para dejar aproximación antes del salto. | Inicio `(4,0)`; checkpoint `(38,0)`. |
| 40–78 | Primera escalera fácil en 46 hasta 4.8; muro 48.7; cadena de plataformas a 4.8→6.2→7.6, seguida de descenso 6.1→3→0. Dos enemigos terrestres y un volador cubren alturas distintas. El apoyo inferior 58.9–61.8 a Y 0.7 facilita la recuperación. | Checkpoint `(78,0)` bajo la pasarela; escalera 79 recupera su cubierta. |
| 78–122 | Condensadores: escalera 87 desde −1.2 hasta 4.4, salto a 5.8 sobre el muro 94, y descenso escalonado 4.5→2.6→0.8→−2. La electricidad está en la ruta inferior 91.1–93.2. | Checkpoint `(122,-2)`; escalera 87 permite volver a subir si se cae al piso inferior. |
| 122–164 | Fundición profunda: escalera 129 desde −2 hasta 0 y ascenso 0→1.4→2.8→4.2 para superar la barrera 153. Bajada por 2.8 y 0. Electricidad inferior 130–133 y 149–152; torreta sobre el puente. | Checkpoint `(164,-2)` y escalera 166.5 hasta 0. |
| 164–205 | Torre: escalera 173 hasta 4.8; muro intermedio 6.0; plataformas 6.2→7.6→9.0. Descenso 7.4→4.6→2 hacia el patio. La escalera 192 conecta el apoyo inferior 2.8 con la cima 9.0. | Checkpoint `(205,0)`; una caída fuera de plataformas consume una vida al pasarY −8. |
| 205–238 | Reactor: escalera 216 hasta 3; muro 219 a Y 4.2 y pasarela 4.4; descenso 2.4→0; últimos enemigos y checkpoint 232. Puerta original animada en 236.5 con colisión real, marco y arco superior. | Checkpoint `(232,0)` antes de la puerta. |
| 238–260 | Arena `[238,260]` a Y 0: Dragón con 240 PV en `(255,0)`; a 60 PV interviene Axl y le dispara. Sigue una conversación con Lumine, WARNING de Axl y recarga de ambos héroes/Axl antes de combatir contra sus 360 PV. A 1 PV llega el compañero opuesto, vuelve el diálogo, sale Axl y después ambos héroes. La pared oriental es permanente y el final automático, sin puerta ni meta en X 262. El techo a Y 11 deja seco su interior. | El checkpoint 232 permite repetir la secuencia final. |

Hay **17 enemigos: 6 Gunner, 5 Walker, 4 Flyer y 2 Turret**. Sus posiciones iniciales son 19, 35, 54, 61, 75, 95, 106, 119, 136, 151, 160, 174, 180, 193, 204, 223 y 229. Hay dos jefes consecutivos: Dragoon y Axl, que usa el banco Axl original. Los enemigos y la secuencia de jefes se reinician mediante el controlador de sesión.

## Escaleras y puntos de control

| Escalera X | Pie → desembarco Y |
|---|---|
| 46 | 0 → 4.8 |
| 79 | 0 → 3 |
| 87 | −1.2 → 4.4 |
| 129 | −2 → 0 |
| 166.5 | −2 → 0 |
| 173 | 0 → 4.8 |
| 192 | 2.8 → 9.0 |
| 216 | 0 → 3 |

Las cubiertas son sólidas. El héroe atraviesa la cubierta únicamente mientras está escalando y vuelve a colisión normal al salir. Los límites `RainLadder.Bottom`, `Top` y `Bounds` están en coordenadas del mundo. En la primera escalera, soltar arriba y dejar aproximadamente 0.55 s permite comprobar apoyo en la cubierta Y 4.8 antes de caminar.

Los seis puntos de control son `(38,0)`, `(78,0)`, `(122,-2)`, `(164,-2)`, `(205,0)` y `(232,0)`. Los disparadores abarcan la ruta superior e inferior, pero cada reaparición usa una posición con suelo y acceso a la siguiente escalera.

## Viabilidad estática y muestras para QA

El movimiento objetivo usa velocidad 3.25, salto 10 y gravedad 27. Sin corte de salto, `y(t)=10t−13.5t²`: ápice nominal 1.852 a 0.370 s y vuelo de 0.741 s al mismo piso, equivalente a 2.407 unidades horizontales. El salto desde dash conserva 7 unidades/s y alcanza aproximadamente 5.185 unidades al mismo nivel. El dash aéreo añade 0.19 s de desplazamiento a 7 y suspende momentáneamente la caída. El ancho físico del héroe es 0.52 y su altura 0.86. La integración de pasos físicos y las tolerancias de colisión pueden variar ligeramente respecto de esta aproximación continua.

La subida obligatoria entre plataformas contiguas es como máximo 1.4, por debajo del ápice. Para subir 1.4, el cruce descendente nominal ocurre a 0.553 s y da 1.796 unidades horizontales a velocidad normal, o 3.87 con salto desde dash. La mayoría de los huecos de ascenso se redujo a 1.2; el primero conserva 1.5. Los muros de torre y reactor también ofrecen un apoyo intermedio 1.2 por encima de la plataforma anterior. Los huecos inferiores más largos requieren dash: el máximo es 4.5 entre 197–201.5, con destino 2.8 unidades más abajo. El primer foso tiene 2 unidades. No se necesita armadura, ataque especial ni cambio de personaje para avanzar.

| Muestra de recorrido | Inicio del pie | Meta de apoyo | Observación |
|---|---|---|---|
| Primer foso | `(25.4,0)` | Azotea desde X 28, Y 0 | Mantener salto y derecha; el borde de apoyo se alcanza antes de agotar 0.741 s. |
| Primer ascenso alto | `(56.6,4.8)` | `(60.2,6.2)` | Verificado: aterrizaje estable en `(60.07,6.21)`, con salto y dash aéreo a 0.28 s. |
| Segundo ascenso alto | `(63.2,6.2)` | `(66.8,7.6)` | Verificado: aterrizaje estable en `(66.67,7.61)`, con el mismo patrón. |
| Horno | `(137.9,1.4)` | `(141.4,2.8)` | Verificado: aterrizaje estable en `(141.37,2.82)`. |
| Torre | `(187,7.6)` | `(190.6,9.0)` | Verificado: aterrizaje estable en `(190.47,9.01)`. |
| Muro seguro | `(48.4,1.3)` | Empujar derecha contraX 48.7 | Conserva la referencia anterior para WallSlide/WallJump. |

Como orientación estática, al saltar desde la cubierta de la escalera 173 hacia el primer nivel de torre, iniciar alrededor de X 175.0 y usar dash aéreo a 0.28 s permite ganar distancia sobre el muro 176.2. También se puede apoyar primero sobre el muro 6.0 y saltar después al nivel 6.2. En reactor, el mismo patrón desde X 217.6 permite aproximarse al muro 219, cuya cubierta 4.2 sirve de apoyo intermedio. Los saltos requieren mantener el botón; soltarlo reduce el ápice. La prueba nativa verificó los cuatro ascensos indicados, además de agarre/salto de pared, desembarco de escalera y conservación del impulso del dash; no recorrió de extremo a extremo todos los caminos posibles.

## Presentación, puerta y contratos

La ciudad mantiene una franja horizontal de parallax y añade 11 variantes de edificios de las nuevas hojas originales, junto con postes y pinchos. El fondo superior es negro. La [lluvia continua](../WeatherPipeline/LLUVIA_CONTINUA.md) usa **una sola malla de shader** que cubre también el encuadre amplio. Termina en el suelo inferior y se descarta dentro de plataformas y techos, conservándose en el aire abierto que queda debajo. Las referencias a Rain/Fall permanecen disponibles como material original.

La cámara conserva el piso de despegue durante saltos normales y sigue las escaleras. Para descender exige una plataforma sólida real detectada debajo del héroe; si este resbala hacia un hueco sin fondo, mantiene la última altura del encuadre y deja de seguir su caída. `SafeFloorY` e `IsHoldingOverVoid` permiten inspeccionarlo en QA. Durante BossIntro, BossCinematic y Ending encuadra toda la arena con tamaño ortográfico 6.2 y centro Y 4.1, incluyendo la caída desde Y 8 y los actores de la historia. En Boss/AxlBoss usa encuadre dinámico 4.2–6.2 según la separación del jugador y `Game.ActiveBossTransform`.

`RainLevelBuilder.Build()` construye y guarda sin iniciar Play. `RainLevelWorld` expone `LevelLength=264`, `ArenaLeft=238`, `ArenaRight=260`, `BossDoorX=236.5`, `FallLimit=-8`, `DoorProgress`, `DoorIsOpen`, `ArenaSealed`, `Checkpoints` y `ElectricZones`. La puerta tarda 1.15 s en desbloquearse/abrirse/retraerse al aproximarse; deja de colisionar a 1.05 s. En BossIntro/Boss/BossCinematic/AxlBoss/Ending vuelve sólida y permanece cerrada tras derrotar a Dragoon. El arco superior impide rodearla con un salto. Los PNG originales de Unlock/Open/RetractTop/RetractBottom permanecen intactos.

El builder instancia el prefab original `Assets/Recursos/Prefabs/Bosses/Axl/Axl_Animated.prefab`, le añade/configura `RainAxlBoss` para Axl y lo entrega mediante `Game.ConfigureAxl(white)`. No usa el rig AxlRig. La pared permanente X 260–261 cubre Y 0–11. No se crea ExitGate, arco, luz ni disparador de victoria en el extremo oriental. El controlador de historia decide la transición Dragoon→Axl y la misión completada tras el final automático.

Dragón inicia con 240 PV; al llegar a 60 PV se congela el combate para la intervención de Axl. Su disparo elimina al Dragón y comienza un diálogo con mugshots de X/Zero a la izquierda y Axl a la derecha, sobre la escena oscurecida. Reconocen que Lumine controla a Axl. Después de su WARNING se recargan los dos héroes y Axl, cuyo combate empieza con 360 PV y combina ráfagas dirigidas, carga, embestidas y ataques aéreos. A 1 PV queda vivo, llega el compañero opuesto y otra conversación explica la supervivencia de Lumine. Axl sale primero y luego los dos héroes. `BossCinematic`, `AxlBoss` y `Ending` mantienen la arena cerrada y el encuadre apropiado. Los resultados vuelven al menú mediante Enter y no ofrecen reintentar.

La presentación usa PinkFlameAura permanente para Zero potenciado, sin IceRing. Su definitivo neutro es el mismo en suelo y aire, refleja AirPoseRaised/AirPoseLowered, fija el frente y emite la onda a **0.54/0.85 ≈ 0.64 s** dentro de una acción de **1.05/0.85 ≈ 1.24 s**, con sonido de sable y ocho segundos de recuperación. El dash aéreo de X con armadura definitiva usa Dash y los disparos miran hacia su trayectoria. Sus especiales energéticos usan chargedShot. El agachado sostiene DashShoot cuadro 2 en X/Ultimate, DashSlash cuadro 0 en Zero y Enhanced_GroundSlideSlash cuadro 0 en Zero potenciado; X conserva el cuadro al disparar.

El HUD tiene barras pequeñas a escala 2 y placas azules/rojas; puntos, reloj, checkpoint y bajas se consultan al pausar. La [pausa original](MENU_VIDAS_Y_CONTROLES.md#pausa-con-el-arte-original) presenta ambos héroes con sus datos usando los paneles y **37 glifos bitmap** de la hoja suministrada. Ratón, flechas y Enter manejan sus acciones; P/Escape continúan. La portada muestra Presionar Enter sin recuadro.

Antes de la portada se reproduce el [video original de inicio](VIDEO_INICIO.md), de unos **11.35 s**, sin posibilidad de omitirlo. No se repite al volver al menú. Después de elegir héroe, las ilustraciones de la introducción conservan sus proporciones y sus marcos se ajustan al tamaño visible real, con texto debajo.

Controles de recorrido: A/D o flechas para mover; W/↑ y S/↓ en escaleras; Espacio/Z para salto; K/C/Shift izquierdo para dash; J/X para ataque; L/V para especial; Tab para X/Zero; U para armadura de X; H para mejora de Zero; P/Esc para pausa; R para reintento. El controlador de sesión conserva la autoridad sobre vidas, WARNING, jefes y victoria.

## Comprobación actual y límites de QA

La ejecución completa guardada el **25/10/2026 a las 22:41:03** pasó **251 comprobaciones nativas** y produjo **69 capturas**. El [informe de QA](playtest_report.json) registra `success=true` y ningún error; el [trazado generado](../Gameplay/rain_level_layout.json) contiene 17 enemigos, 8 escaleras, 6 checkpoints y 2 jefes. La lluvia usa una sola capa. Las [capturas](../../Previews/RainyFoundry) incluyen la [intervención de Axl](../../Previews/RainyFoundry/24_White_Intervention_Shot.png), la [confrontación](../../Previews/RainyFoundry/29_Axl_Confrontation.png), el [diálogo final](../../Previews/RainyFoundry/31_Axl_Final_Dialogue.png), los [resultados](../../Previews/RainyFoundry/06_Mission_Complete.png), la [pausa](../../Previews/RainyFoundry/21_Pause_Statistics.png) y la [auditoría de poses bajas](../../Previews/RainyFoundry/CrouchCandidates.png).

La campaña añade video de arranque real no omitible, introducción de cuatro ilustraciones y un destino negro, conversaciones por turnos y música propia de cada fase. Véanse [narrativa](NARRATIVA_LLUVIA.md), [video](VIDEO_INICIO.md) y [audio](../AudioPipeline/AUDIO_INTEGRACION.md). El volumen base musical subió de 0.32 a **0.43**. Axl conserva los dibujos originales de Axl, con escala **1.7** frente a **2.5** del Dragón. Recibe el **90% del daño nominal**, frente al **65% del Dragón**, redondeado hacia arriba y con protección entre impactos: un golpe nominal de 10 causa 9 a Axl.

La suite coloca al personaje en puntos de prueba y usa el controlador, las colisiones y la física reales con entrada simulada. Verifica daño de proyectiles a ambos jefes y aplica después daño mediante el contrato público para alcanzar de forma acotada los umbrales de 60 y 1 PV. Incluye avance real de fotogramas y fin del video, rechazo de teclas para omitirlo, cuatro agachados, disparo agachado de X, definitivo de Zero en suelo/aire y ambos sentidos, arte/glifos/datos/teclado de pausa y recorte de lluvia. La [revisión adicional de presentación](presentation_report.json) registra tres comprobaciones de sonido desde acciones reales. Esto verifica integración y segmentos físicos; **no constituye una partida humana completa ni una prueba física del teclado**.

La [validación de sprites](../AnimationPipeline/python_validation.json) registra **34 archivos originales sin cambios**. La [biblioteca del nivel](../AnimationPipeline/rain_level_asset_report.json) contiene **361 entradas** de animación y poses. La hoja original de Axl se conserva.

## Objetos y puntos

Los enemigos dan 100 puntos y pueden dejar ScoreOrb (65%, +100 puntos con `BlueOrbRotate`) o SmallHealth (10%, +5 de vida con `SmallPickupRotate`); el 25% restante no deja objeto. Cada partida usa su propia secuencia aleatoria. Los drops caen hasta terreno real, duran 16 s y parpadean al final. La intervención contra Dragoon da 1000 puntos y el final de White suma 3000. Ningún orbe azul cura.

Hay tres curaciones grandes fijas (+12, `CapsulePulse`, respaldo `LargePickupRotate`) en `(69.3,7.6)`, `(152.3,4.2)` y `(224.2,4.4)`, después de grupos de enemigos en altura. Hay vidas extra en `(96.1,5.8)` y `(194.2,9)`, con animación completa `BlueEnergyPulse` para X o `RedEnergyPulse` para Zero; no usan LifeTank ni un icono añadido. Otros cinco orbes de puntuación premian plataformas del tutorial, antenas, hornos, torre y reactor.

Estas diez recompensas tienen identificadores estables `rainy-foundry/...`. Si no se recogieron, reaparecen al reintentar; si se recogieron, permanecen ausentes durante toda la partida aunque se use Respawn. Una partida nueva limpia el registro y el marcador. APIs: `Game.Score`, `AddScore(int)`, `SpawnPickup(kind, position)`, sobrecarga con `persistentId`, `WasPickupCollected(id)`, `World.PickupPlacements`, `Pickup.AnimationName` y `Pickup.PersistentId`.

Las transformaciones exponen `BeginTransformationPause()`, `EndTransformationPause()` e `IsTransformationPaused`. Esta pausa congela física, enemigos y cronómetro mediante Time.timeScale, mientras el héroe controla su propia animación con tiempo no escalado. La pausa manual sigue siendo independiente. Reinicio, menú, derrota y victoria limpian el bloqueo para evitar una sesión congelada.


