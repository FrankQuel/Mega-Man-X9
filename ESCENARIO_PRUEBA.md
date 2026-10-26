# Escenario de prueba de animaciones

Abre `Assets/Recursos/Scenes/Testing/AnimationTestStage.unity` y pulsa Play. La revisión prepara **31 grupos**: catálogo de **361 entradas y 1.862 referencias a fotogramas**, más **17 acciones de DragonRig**. Las comprobaciones de integración y del bucle real de reproducción pasaron en Unity 6000.6.4f1. Hay 15 capturas actuales en Previews/X4References.

## Inicio

READY aparece mientras los tres jugables están ocultos. Recorre línea, expansión/flash, aparición y sostén; sale como **dos palabras completas con sombras en sentidos opuestos**. A velocidad 1× dura **1,8 segundos**. Después Azul, Ultimate y Rojo realizan sus entradas y terminan en su propio Idle.

Azul combina haz, orbe y materialización. Ultimate usa su materialización de armadura. Rojo combina energía roja y TeleportIn. Comparten el reloj de pausa/velocidad, conservando sus recursos. Ultimate respira con tres cuadros de brazos bajos; el gesto anterior se selecciona aparte como **IntroSalute**. El recorrido automático comienza desactivado.

La presentación está adaptada al proyecto. El [análisis exhaustivo de los cuatro repositorios](Tools/ReferenceAnalysis/ANALISIS_EXHAUSTIVO_4_REPOS.md) explica qué aprovechar y los límites de la comparación con X4.

## Controles

- **Todos:** seis estaciones simultáneas; seis páginas para los 31 grupos.
- **Individual / Ver individual:** personaje grande y búsqueda por unidad o clip.
- **Flechas de estación:** acción anterior/siguiente; se conserva la selección de cada unidad.
- **Pausa / Reanudar**, **Reiniciar todos**, velocidad y **Reflejar personaje:** revisión de poses y continuidad. El dragón original mira hacia la izquierda.
- **Recorrer todos los clips:** cambia automáticamente las acciones visibles.
- **READY + entradas:** repite la presentación y las entradas de los jugables visibles.
- **Entrada del seleccionado:** repite solo la entrada del jugable elegido.
- **Daño −4**, **Curar +4**, **Vida completa:** actúan sobre la vida real seleccionada; el indicador muestra su valor.

Maximiza Game para disponer de más espacio. Las plataformas y el ambiente industrial reutilizan recortes del atlas de escenario original.

## Vida y HUD

Azul, Ultimate y Rojo tienen **28 PV**; el dragón, **48 PV**. El axl original mantiene su propia vida y barra. Los jugadores muestran medidores a la izquierda; los jefes, a la derecha. En Todos acompañan a cada estación; en Individual aparecen junto al personaje grande.

CharacterHealth conserva actual/máximo, notifica cambios y limita daño/curación. El HUD usa marcos X/Z, piezas e indicadores originales, con relleno proporcional y estados Normal/Caution/Danger. La capacidad gráfica del marco no obliga a asignar 28 PV a todos: el dragón representa proporcionalmente sus 48 PV.

Cambiar una animación conserva la vida de esa unidad. Estos botones prueban dato y presentación; todavía no constituyen un sistema completo de combate.

## Axl original

El escenario usa **Axl**, extraído de la hoja blanca ORIGINAL proporcionada por el usuario, conservando sus poses y variaciones artísticas.

`Assets/Recursos/Prefabs/Bosses/AxlRig/AxlRig_Animated.prefab` y sus recursos redibujados se retienen únicamente como historial. **AxlRig no aparece en el escenario actual ni sustituye al original.**

## Dragón enemigo articulado

Prefab: `Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab`. Clips/controlador: `Assets/Recursos/Animations/Bosses/DragonRig`.

Acciones: **Idle, Breathe, Walk, Run, Jump, Crouch, ClawAttack, FireBreath, Hurt, Death, CombatIdle, Fireball, RisingFlame, FlamingKick, GroundFireWave, HurtFlash y Landing**.

- **FireBreath:** boca abierta y llamas que nacen en ella y viajan por separado.
- **Fireball:** carga en la mano y proyectil independiente, como ataque tipo hadouken.
- **RisingFlame:** puñetazo ascendente con cuerpo, fuego y recuperación coordinados.
- **FlamingKick:** salto/embestida con pierna extendida, zona de ataque y estelas.
- **GroundFireWave:** frente de fuego por el suelo y pilares sucesivos.
- **ClawAttack:** golpe articulado con garra abierta.
- **HurtFlash:** retroceso y silueta blanca mediante shader, sin alterar originales.
- **Landing / Crouch:** flexión de articulaciones y referencia de suelo con longitudes de extremidades constantes.

DragonBossAttackPreview coordina huesos, emisiones, pausa, reflejo y vida útil. El daño está desactivado por defecto en el laboratorio. Para gameplay existe DragonRigActor.PlayAttack; detalles en [dragon_boss_attacks.md](Tools/AnimationPipeline/dragon_boss_attacks.md).

## Continuidad

Personajes y dragón conservan escala y anclajes canónicos de la pose neutral. Agacharse o extender un sable no agranda el cuerpo para llenar una caja. Individual amplía la misma unidad con sus proporciones; los efectos y piezas independientes sí se ajustan a su cuadro.

La presentación controla las acciones de las instancias de prueba sin competir con retornos automáticos a Idle. Los prefabs conservan sus APIs para gameplay. El escenario aún no es un nivel completo con movimiento controlable, colisiones de combate e IA.

## Reconstrucción y pruebas realizadas

Fuera de Play, usa **Mega Man X9 > Animations > Apply X4 Reference Corrections**. Actualiza entrada/Idle del Ultimate, HUD, READY, dragón y escenario. Antes de sustituir trabajo modificado contempla guardar una copia en Assets/Recursos/Scenes/Testing/WorkspaceBackups.

Se comprobó esta revisión en Unity:

1. READY completa una sola vez y las tres entradas terminan en su propio Idle visible.
2. Ultimate permanece quieto y el gesto solo aparece al seleccionar IntroSalute.
3. Barras de 28 PV y 48 PV; daño, curación, reinicio y persistencia entre clips.
4. Efectos desde boca/manos/pies, proyectiles móviles con caducidad, reflejo y pausa.
5. Axl original presente, AxlRig ausente y acceso a las seis páginas.

Los informes aprobados están en Tools/AnimationPipeline: x4_reference_validation.json, test_stage_validation.json, hud_v3_validation.json y dragon_boss_attack_validation_report.json. x4_visual_qa.json registra 15 capturas del bucle real; también comprueba que Animator no reemplace el cuadro de entrada por Idle y que la boca esté abierta durante FireBreath.

Para repetirlas: maximiza Game y usa **Mega Man X9 > Animations > Capture X4 Reference Previews**. Las capturas incluyen línea, flash, READY, salida opuesta, tres entradas, seis ataques, daño y reposo con axl original. Puedes inspeccionar FireBreath a 1 segundo para ver simultáneamente boca abierta y llamas; al final del ataque las llamas ya emitidas siguen viajando aunque la boca se cierre.
