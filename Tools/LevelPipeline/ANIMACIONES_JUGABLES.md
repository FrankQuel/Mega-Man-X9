# Animaciones del nivel jugable: Fundición bajo la tormenta

La ejecución completa del **25 de octubre de 2026 a las 22:41:03** pasó **251 comprobaciones y generó 69 capturas**. Incluye el video de arranque real, lluvia continua, pausa original, cuatro poses de agachado y el definitivo acelerado de Zero en suelo/aire y ambos sentidos. La evidencia está en [playtest_report.json](playtest_report.json) y en [las capturas del nivel](../../Previews/RainyFoundry). La cobertura corresponde a los contextos probados; no equivale a recorrer cada clip del catálogo.

El nivel tiene **X y Zero intercambiables**, armadura Ultimate de X, forma potenciada de Zero, **17 enemigos normales y dos jefes consecutivos**: Dragón y **Axl**, que utiliza el banco **Axl original**. AxlRig queda como material histórico de revisión. Una introducción ilustrada enlaza la elección inicial con el nivel; dos conversaciones con mugshots revelan que Lumine controla a Axl. La historia termina automáticamente después de las salidas de los personajes, seguida por resultados; no requiere cruzar una salida final.

## Controles

| Acción | Teclado predeterminado | Mando |
| --- | --- | --- |
| Mover / subir o bajar escaleras | A/D/W/S o flechas | Stick izquierdo / cruceta |
| Agacharse en suelo | Mantener S o abajo | Mantener dirección abajo |
| Saltar, regular altura y saltar desde pared | Espacio o Z | Botón sur |
| Ataque / cargar Buster al mantener | J o X | Botón oeste |
| Dash terrestre o aéreo | K, C o Mayús izquierda | Botón este |
| Especial contextual | L o V + dirección | Botón norte + dirección |
| Cambiar X/Zero | Tab | Hombro derecho |
| Activar armadura Ultimate de X | U | Hombro izquierdo |
| Alternar poder de Zero | H | Select |
| Pausa / estadísticas | P o Esc | Start |
| Reintentar desde checkpoint | R | Botones del HUD |

Las teclas principales se pueden reasignar y guardar desde Opciones. El cambio conserva el cuerpo físico, posición, velocidad y estado de recorrido; cada héroe mantiene sus propios 28 PV. Una forma sin energía no se puede seleccionar. Durante los rayos de cambio se congela el mundo; la pausa manual también detiene la presentación.

## X: banco Blue

El banco contiene 36 clips. TeleportBeam, TeleportOrb y NovaStrikeTrail son efectos separados; el número de entradas no representa el número de habilidades.

| Contexto real | Clips conectados o disponibles para ese contexto |
| --- | --- |
| Reposo normal / energía baja (9 PV o menos) | Idle, AlternateIdle; esperar quieto no activa cansancio |
| Empezar a correr / carrera | RunStart, Run |
| Salto / aterrizaje / dash aéreo | Jump, AirRecover, AirDash |
| Agacharse / disparar agachado | CrouchIdle con índice 0 sostenido; compensa su orientación original y conserva la postura al disparar |
| Disparo quieto | Shoot |
| Disparo cargado al soltar ataque | ChargeShot |
| Apuntar hacia arriba quieto | IdleAim |
| Carga quieto, corriendo, en aire, en dash o pared | IdleAimFlash, RunShootFlash, JumpShootFlash, DashShootFlash, WallJumpShootFlash |
| Disparar en movimiento / en aire / en dash | RunShoot, JumpShoot, DashShoot |
| Deslizarse por pared / impulsarse / disparar | WallJump con índice 3 sostenido, WallJump, WallJumpShoot; WallCling se conserva para revisión |
| Escalera, salida superior y disparo | LadderClimb, LadderDismount, LadderShoot |
| Daño terrestre o eléctrico / daño aéreo / derrota | HitElectrical, AirHit |
| Especial hacia arriba | Somersault |
| Embestida sin dirección vertical | NovaStrikeStartup, NovaStrikeTravel |
| Llegada inicial o de checkpoint | TeleportBeam → TeleportOrb → TeleportArrival → Idle |
| Salida de la historia | IdleAim → TeleportDeparture |

El arte original AirDash de X mira en sentido contrario al resto del banco. El controlador visual compensa esa orientación y mantiene el frente bloqueado hacia el desplazamiento durante el dash. Se verificaron ambas direcciones en Play Mode. SmallShot también reconoce que su dibujo original mira a la izquierda y orienta el sprite hacia el vuelo real del proyectil.

Las variantes Flash muestran carga; no cuentan como disparos adicionales. NovaStrikeTrail permanece como arte de estela independiente del cuerpo.

Las poses bajas se revisaron en Crouch_Review_2.png. X básico usa su animación propia **CrouchIdle, cuadro 0**; Ultimate usa la postura de rodilla **DefeatCollapse, cuadro 1**, porque su banco no contiene una secuencia CrouchIdle propia. Ambas compensan el frente del dibujo y mantienen el cuadro al disparar desde el cañón bajo. Los especiales energéticos de X reproducen el WAV de **chargedShot**.

## Armadura Ultimate opcional

U cambia el banco visual a BlueUltimate y conserva la identidad y salud de X. Sus 38 clips incluyen equivalentes de movimiento, disparo, carga, pared y escalera. El cuerpo conserva escala 2× entre acciones: ninguna pose agachada se agranda para igualar la altura del reposo.

| Contexto adicional | Clips / resolución actual |
| --- | --- |
| Reposo de brazos bajos | Idle: tres cuadros de respiración |
| Saludo separado de Idle | IntroSalute |
| Materialización de la armadura | TeleportArrival |
| Dash aéreo | El rol AirDash resuelve a Dash |
| Disparo / carga durante dash aéreo | DashShoot / DashShootFlash |
| Agachado y disparo agachado | DefeatCollapse con índice 1 sostenido, postura de rodilla original |
| Disparo cargado durante dash | DashChargeShot |
| Embestida especial | NovaStrikeStartup, NovaStrikeBurst |
| Especial hacia arriba | AirArmRaise |
| Especial hacia abajo en suelo / pared / aire | SaberGroundSlash, SaberWallSlash, SaberAirSlash |
| Derrota | DefeatCollapse |

La pose de propulsión estacionaria se conserva en el catálogo, pero ya no representa el dash aéreo jugable: ese contexto usa el dibujo de Dash y sus variantes de disparo. Tanto X básico como Ultimate mantienen la dirección visual correcta durante el desplazamiento aéreo.

## Zero: sable normal y potenciado

El banco Red contiene 61 clips, incluidos 25 Enhanced. H activa esas variantes cuando existen, con FireUppercutPose y Enhanced_Ready durante su materialización. La forma potenciada mantiene **PinkFlameAura** de manera persistente mientras siga activa; no usa IceRing. La física y los PV pertenecen al mismo Zero.

| Contexto | Sable normal | Forma potenciada |
| --- | --- | --- |
| Reposo, comienzo de carrera y carrera | Idle, RunStart, Run | Enhanced_Idle, Enhanced_RunStart, Enhanced_Run |
| Salto ascendente, caída y aterrizaje | JumpRise, Fall, Land | Enhanced_JumpRise, Enhanced_Fall, Enhanced_Land |
| Combo terrestre | GroundSlash1, GroundSlash2, GroundSlash3 | Enhanced_GroundSlash1, Enhanced_GroundSlash2, Enhanced_RisingSlash |
| Ataques aéreos alternados | AirSlash1, AirSlash2 | Enhanced_AirSlash1, Enhanced_AirSlash2 |
| Inicio, sostén y fin del dash | DashStart, Dash, DashEnd | Enhanced_DashStart, Enhanced_Dash, Enhanced_DashEnd |
| Ataque durante dash | DashSlash | Enhanced_GroundSlideSlash |
| Agachado sostenido | DashSlash con índice 0 | Enhanced_GroundSlideSlash con índice 0 |
| Pared y salto desde pared | AirUppercutPose con índice 3 sostenido; secuencia al impulsarse | Variante Enhanced cuando existe; WallClimb se reserva a escaleras |
| Ataque de pared | WallSlash | Enhanced_WallSlash, Enhanced_WallSlashFollowThrough, Enhanced_WallSlashEnd |
| Golpe agachado | GroundHeavySlash | Acción correspondiente al contexto |
| Especial hacia arriba | AirUppercutPose | Enhanced_RisingSlash |
| Especial hacia abajo en el aire | AirSlashLongA/B/C, alternadas | Enhanced_AirSlashLong |
| Especial aéreo sin dirección vertical | AirSlashBackhand | Mismo definitivo que en suelo: AirPoseRaised → AirPoseLowered → GroundEnergyStrike → GroundHeavySlash |
| Especial hacia abajo en el suelo | GroundEnergyStrike y onda LargeSaberWave | Conserva cuerpo y emisión correspondientes |
| Especial neutro en suelo | GroundHeavySlash | AirPoseRaised → AirPoseLowered → GroundEnergyStrike → GroundHeavySlash, aura rosa y gran onda verde |
| Daño y electricidad | Hurt, Electrocuted | Enhanced_Electrocuted para electricidad; Hurt normal si no hay variante |
| Llegada | RedEffects/TeleportArrivalEnergy → TeleportIn → Idle | Entrada del mismo Zero |
| Salida de la historia | Enhanced_Ready → TurnAround → TeleportOut | Misma identidad y física |
| Derrota | ProneCollapse | Pose de derrota existente |

El definitivo potenciado es idéntico en suelo y aire al pulsar especial sin dirección vertical; no cae en Enhanced_SpinSlash. AirPoseRaised y AirPoseLowered compensan su orientación original y el frente queda bloqueado hasta terminar. La duración de acción es **1,05/0,85 ≈ 1,24 s**; emite la onda a **0,54/0,85 ≈ 0,64 s**, frente a aproximadamente 2,14 s antes. Conserva **ocho segundos de recuperación** y usa sonido de sable. La prueba nativa verificó ambas direcciones, ambos estados y emisión antes de 0,9 s.

Su aura persistente y la presentación del definitivo son efectos independientes del cuerpo. Jump completo y TurnAndTeleportOut siguen siendo secuencias compuestas de catálogo; el controlador usa sus fases para evitar reiniciar el despegue en mitad del salto o duplicar la salida.

## Enemigos, jefes y efectos

| Recurso del nivel | Conexión actual |
| --- | --- |
| Burnout, cinco patrullas | Roll, contacto y explosión de derrota |
| GrenadeMan / GrenadeManGreen, seis patrullas | Idle, Run, AimForward y ráfagas con proyectil/MuzzleFlash |
| BirdGreen / BirdBlue, cuatro enemigos aéreos | Idle, Fire y desplazamiento aéreo |
| GigaDeath, dos torretas normales | Idle, disparos, dispersión y MuzzleFlash; no cuentan como jefes |
| Curación pequeña / grande | SmallPickupRotate (+5 PV) / CapsulePulse (+12 PV) |
| Puntos | BlueOrbRotate, +100; no cura |
| Vida extra | BlueEnergyPulse o RedEnergyPulse completos según el héroe activo |
| Derrotas | Explosion/FireBlast |
| Lluvia | Una malla de shader continua, sin bordes de mosaico y recortada bajo suelo, plataformas y techos; originales conservados como referencia |
| Primer jefe, DragonRig | CombatIdle, Walk, ClawAttack, FireBreath, Fireball, RisingFlame, FlamingKick, GroundFireWave y Death según estado |
| Segundo jefe, Axl / Axl original | TeleportIn, Idle, Aim, Shoot, Charge, ChargedShot, DashAttackReady, DashAttack, DashStop, JetRise, AirSpreadShot y TeleportOut |

DragonRig conserva 17 acciones de catálogo; no se presentan todas como ataques independientes. Huesos, fuego de boca, proyectil de mano, patada y columnas de fuego avanzan con su reloj. El tren de fuego terrestre conserva la separación entre columnas y viaja hasta la pared de la arena.

Los enemigos normales sortean **65% orbe de puntos, 25% sin objeto y 10% curación pequeña**, usando una semilla nueva en cada partida. Las recompensas fijas de las rutas elevadas son independientes de ese sorteo y no se regeneran después de recogerlas durante la misma partida.

## Cadena de jefes y final automático

1. Tras elegir héroe, hay **0,55 segundos de negro** y cuatro imágenes en orden uno/dos/tres/cuatro. Se explica el descenso de Jakob, la recuperación de Axl, la sala destrozada y la alerta. Cierra una página negra: «X y Zero se dirigen a Central Highway.» Después comienzan READY y la materialización del héroe elegido.
2. Ante el primer jefe, WARNING bloquea el control. Dragon cae desde arriba y su IA se activa al aterrizar. Comienza con **240 PV** y escala **2,5**.
3. Al llegar a **60 PV**, Dragon deja de combatir y el héroe activo queda en reposo. Axl se materializa, apunta y dispara a Dragon; la historia completa su eliminación. Axl usa el banco Axl y escala **1,7**, conservando sus proporciones frente a los héroes de escala 2.
4. La escena se oscurece al 65% para una conversación corta: mugshots de X y Zero a la izquierda y Axl a la derecha, con cuadros de diálogo separados. X/Zero llaman a Axl, reconocen la presencia de Lumine y comprenden que controla su cuerpo. Después aparece **WARNING para Axl** y se recargan ambos héroes a **28 PV** y Axl a **360 PV** antes de combatir.
5. A **1 PV**, Axl deja de combatir y permanece vivo. Se materializa el compañero opuesto al héroe activo: Zero si se controla X, o X si se controla Zero. Ambos quedan en reposo durante otra conversación: Lumine revela que entró en Axl antes de ser derrotado y promete un nuevo encuentro.
6. Axl sale primero con TeleportOut; después ambos héroes usan sus propias animaciones de salida. La misión termina automáticamente, sin compuerta ni disparador de salida final. Los resultados usan el fondo del menú oscurecido, letras con contorno y **Presiona Enter** para volver al menú, sin botón de reintentar.

Las ilustraciones y los diálogos revelan **ocho palabras completas por segundo**. Enter completa el texto actual y otra pulsación pasa al siguiente turno o página; la pausa detiene palabras y transiciones. Los marcos de las ilustraciones se ajustan a su proporción y al tamaño visible real. Reintentar cancela actores y callbacks anteriores. El [documento de narrativa](NARRATIVA_LLUVIA.md) describe los recursos y su contrato de avance. Dragón conserva un multiplicador de daño recibido de **0,65**; Axl usa **1,1**, aceptando 11 de un golpe nominal de 10. Ambos mantienen redondeo hacia arriba e inmunidad breve entre impactos.

## Presentación y recursos fuera de la misión

El HUD del nivel usa **escala 2**: dos filas horizontales compactas, emblema X con placa azul y Zero con placa roja. Cada fila conserva el círculo de estado original y añade otro círculo para las vidas compartidas. La barra del jefe cambia entre Dragón y Axl. Título de zona, puntuación, reloj, checkpoint y bajas aparecen **solo en Pausa**; los diálogos ocultan el HUD jugable. La [pausa original](MENU_VIDAS_Y_CONTROLES.md#pausa-con-el-arte-original) usa los paneles de menu de pausa.png y **37 glifos bitmap extraídos** de sus letras y números para mostrar ambos héroes y sus datos. Portada, menú principal y selección usan letras con contorno grueso y zonas invisibles de clic, sin cuadros; la configuración de teclas conserva sus paneles.

El [video de Capcom](VIDEO_INICIO.md), de unos **11,35 s**, se reproduce antes de la portada y no tiene opción de omitirlo. Se comprueban fotogramas reales y el evento de fin; volver al menú no lo repite. La [lluvia continua](../WeatherPipeline/LLUVIA_CONTINUA.md) es una sola capa de shader con recorte por terreno: el techo de arena a Y=11 deja seco su interior.

El banco sonoro conecta **33 WAV y cuatro OGG** por sus nombres a acciones y estados. Las seis músicas —Main theme, Main level theme, Dragon boss theme, Axl boss theme, Conversation music y Results theme— están en bucle, con lluvia y efectos en canales separados. El volumen base de música sube de 0,32 a **0,43**. La introducción y ambos diálogos usan Conversation music; los especiales energéticos de X usan chargedShot y el definitivo de Zero usa sable. Véase [la integración de audio](../AudioPipeline/AUDIO_INTEGRACION.md) para sus cues y grabaciones.

AxlRig permanece como revisión histórica; el combate usa Axl original. DragonParts y GigaDeathParts son componentes de montaje, no actores autónomos. Paletas, láminas de autor y enemigos no situados siguen disponibles en la biblioteca y el escenario de revisión; no se declaran jugados para aumentar la cobertura.

## Evidencia de cobertura

RainHeroVisual.PlayedClips registra identidades reales Unit/Clip resueltas por el controlador. RecordObservedClip incluye entradas y salidas muestreadas por su coordinador. La revisión anterior registró **53 identidades de animación del héroe**; ese contador no incluye por sí solo todas las acciones de enemigos, jefes o efectos.

Las **251 comprobaciones y 69 capturas** de la ejecución completa cubren movimiento, rutas físicas, cuatro agachados y disparo agachado de X, orientación de dash/balas, definitivo de Zero en suelo/aire, video no omitible, pausa original, lluvia continua, audio, diálogos y secuencia de jefes. Se conservan **34 archivos originales sin cambios** y **361 entradas** en la biblioteca. La [prueba adicional de presentación y sonido](presentation_report.json) registra tres comprobaciones desde acciones reales. El detalle funcional está en [el informe de Play Mode](playtest_report.json); no sustituye una partida humana completa. Para el contexto de las referencias estudiadas, véase [el análisis de repositorios](../ReferenceAnalysis/ANALISIS_REPOSITORIOS.md).


