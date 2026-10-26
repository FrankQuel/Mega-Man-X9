# Biblioteca de animaciones — Mega Man X9

Unity 6000.6.4f1. Referencia: acción y plataformas 2D de Mega Man X4, X5 y X6.

## Nivel jugable con lluvia

La escena **Assets/Recursos/Scenes/Levels/RainyFoundry.unity** permite jugar con Azul y Rojo intercambiables, combate, enemigos, escaleras, checkpoints y el dragón como único jefe. La [guía del nivel](NIVEL_LLUVIA.md) contiene los controles y la explicación de sus pruebas. AnimationTestStage sigue disponible como laboratorio de recursos.

## Revisión actual

El exportador registra **361 entradas y 1.862 referencias a fotogramas** en 30 grupos de sprites. Incluye secuencias y poses/piezas de un solo cuadro, no 361 acciones de gameplay diferentes. El escenario añade **DragonRig, con 17 acciones articuladas**, para un total de **31 grupos de revisión**.

Escena recomendada: `Assets/Recursos/Scenes/Testing/AnimationTestStage.unity`. Utiliza al **androide axl ORIGINAL**. La versión redibujada `AxlRig` se conserva como historial y no aparece entre sus grupos.

Esta revisión incorpora READY, las tres entradas, vida conectada al HUD y ataques del dragón. Pasaron las comprobaciones en Unity y se capturaron 15 vistas del bucle real de reproducción. Las entradas se comprobaron después de la evaluación de Animator, evitando que Idle sobrescriba la materialización.

Consulta [ESCENARIO_PRUEBA.md](ESCENARIO_PRUEBA.md) y el [análisis exhaustivo de los cuatro repositorios](Tools/ReferenceAnalysis/ANALISIS_EXHAUSTIVO_4_REPOS.md), que distingue comportamiento leído, adaptación propia y límites de la evidencia.

## Presentación y vida

- **READY:** 1,8 segundos a velocidad 1×: línea, expansión/flash, aparición, sostén y salida de **dos palabras READY completas**, con sus sombras en sentidos opuestos. No se corta la palabra por la mitad. Después comienzan las entradas.
- **Azul:** haz descendente, orbe y materialización, seguidos de su Idle.
- **Ultimate:** energía y materialización de su propia armadura, seguidas de su Idle.
- **Rojo:** energía roja y TeleportIn, seguidos de su Idle.
- **Ultimate Idle:** tres cuadros quietos de brazos bajos a 8 FPS, en bucle; no se escala el cuerpo para simular respiración. El gesto anterior permanece como IntroSalute: siete cuadros, 10 FPS, sin bucle.
- **Vida:** los tres jugables comienzan con 28 PV; el dragón, con 48 PV. El axl original conserva su componente y barra de jefe. Cambiar de clip no reinicia la vida de esa unidad.

Los tiempos y la coordinación son implementación propia; no se afirma identidad exacta con el código nativo de X4.

## Organización

| Ruta | Contenido |
| --- | --- |
| `Assets/Recursos/Sprites/Player/Blue`, `BlueUltimate`, `Red` | Tiras PNG y anclajes de cuadros. |
| `Assets/Recursos/Sprites/Enemies`, `Bosses`, `Effects`, `Environment`, `Items` | Arte preparado por recurso. |
| `Assets/Recursos/UI/Animated` | Menú y arte READY. |
| `Assets/Recursos/UI/HUD/Generated` | Marcos e indicadores originales y HealthHUDAssets.asset. |
| `Assets/Recursos/Animations/AnimationLibrary.asset` | Catálogo de 361 entradas de sprites. |
| `Assets/Recursos/Animations/<categoría>/<unidad>` | Clips y controladores; DragonRig es adicional al catálogo de sprites. |
| `Assets/Recursos/Prefabs/<categoría>/<unidad>` | Prefabs reutilizables. |
| `Assets/Recursos/Scripts/Animation`, `Assets/Recursos/Scripts/UI` | Reproducción, escenario, READY, entradas, vida y HUD. |
| `Assets/Recursos/Settings/Rendering` | URP 2D y material de sprites sin iluminación. |
| `SourceArt` | Hojas y paletas originales conservadas. |
| `Tools/AnimationPipeline` | Especificaciones, exportación, informes, notas y QA. |
| `Tools/ReferenceAnalysis` | Fuentes de referencia y análisis. |
| `Backups`, `Assets/Recursos/Scenes/Testing/WorkspaceBackups` | Respaldos de recursos y escenas. |

`source_moves.json` registra traslados; `original_inventory.json` conserva dimensiones y SHA-256 de los 34 originales inventariados. Las paletas no se presentan como animaciones.

## Uso de los componentes

En prefabs con SpriteAnimationPlayer, `Play("Run")` solicita el bucle; `Play("Shoot", true)` reinicia la acción y `FaceLeft(true)` cambia la orientación. Usa nombres existentes en el controlador. Las transiciones de movimiento y combate se integrarán por separado.

CharacterHealth expone CurrentHealth, MaxHealth, Normalized, IsAlive y Changed. ApplyDamage, Heal y ResetHealth modifican el dato real respetando sus límites. El HUD consulta ese componente.

DragonBossAttackPreview coordina cuerpo y efectos con el mismo reloj. Véase [dragon_boss_attacks.md](Tools/AnimationPipeline/dragon_boss_attacks.md). AxlRig permanece como revisión histórica, sin sustituir al axl original.

Los GIF originales son atlas estáticos. Los FPS de sprites son valores iniciales porque las hojas no incluyen tiempos. Algunos anclajes son estimaciones visuales documentadas; los clips Flash conservan variantes iluminadas. Los nombres de acciones no definen por sí solos daño, hitboxes o invulnerabilidad.

## Reconstrucción y comprobación

Fuera de Play, utiliza **Mega Man X9 > Animations > Apply X4 Reference Corrections**. Actualiza la presentación del Ultimate, genera HUD y READY, reconstruye dragón y escenario y contempla respaldar el trabajo abierto antes de cambiar de escena.

Las especificaciones están en blue_specs.json, red_specs.json, world_specs.json, hud_specs.json y extra_specs.json. generate_assets.py localiza originales en Assets o SourceArt. Reconstruir toda la biblioteca recrea también la galería anterior; no es necesario para importar únicamente esta revisión.

`generation_report.json` registra el recuento exportado. Pasaron x4_reference_validation.json, test_stage_validation.json, hud_v3_validation.json y dragon_boss_attack_validation_report.json. x4_visual_qa.json registra 15 capturas actuales en Previews/X4References. python_validation.json confirma 255 secuencias de varios cuadros, 106 entradas estáticas, 1.862 referencias válidas y los SHA-256 intactos de los 34 originales.

Para repetir las capturas usa **Mega Man X9 > Animations > Capture X4 Reference Previews**, con Game maximizado. La herramienta entra en Play y revisa READY, entradas, seis ataques, daño/curación y reposo final. Estos checks acreditan el laboratorio; no equivalen a un nivel completo con IA y combate.

La galería anterior AnimationGallery.unity sigue disponible para revisar los sprites. Para READY, entradas, HUD y ataques se recomienda AnimationTestStage.unity.
