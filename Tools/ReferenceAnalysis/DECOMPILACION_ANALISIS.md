# Qué aprovechar de sozud/mmx4

Revisión estática del 24 de octubre de 2026. Referencia fijada al commit [`6a6b4555`](https://github.com/sozud/mmx4/tree/6a6b4555e775820b5f4e45c6f50bd5859223a453). Las fuentes seleccionadas se verificaron mediante SHA-1 de objeto Git; `repo_revisions.json` confirma que el inventario coincide con el árbol canónico del commit. No se compiló ni ejecutó el proyecto externo.

Su mayor valor para Mega Man X9 es servir de **especificación de comportamiento de X4**: prioridades entre acciones, cambios de estado, duración de fases, cajas y propiedad de efectos. El resultado de esta revisión es una propuesta de implementación C# propia. No convierte el C de PlayStation en componentes de Unity.

## Movimiento y prioridades

En [player.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/player.c#L60), `player_update_normal` procesa bloqueo de entrada, daño, temporizadores, arma, estado principal, carga, flash y caja del cuadro. La entrada al estado normal no depende únicamente del nombre del clip.

`player_idle`, `player_walk_start`, `player_walk` y `player_land` prueban acciones en un orden explícito. Guion, escalera, disparo, técnica de Zero, dash/salto y sable pueden interceptar un estado antes de su reproducción ordinaria. **Aprovechamiento:** una tabla de prioridades y condiciones de cancelación para el controlador; Animator refleja el estado resuelto. Así se evita que un disparo o aterrizaje reinicie arbitrariamente caminar.

En `player_jump` se detiene el ascenso al soltar salto, chocar con techo o recibir determinados bloqueos; la caída y el aterrizaje tienen sus propias rutas. No basta reproducir Jump y esperar que la física resuelva todos los casos. Los estados de pared distinguen agarre, impulso, subida y liberación. Las escaleras separan agarrarse, entrar por arriba, subir, bajar, salir arriba, soltar y salir por abajo.

**Aplicación al proyecto:** reutilizar nuestros clips Jump/Land/Wall/Ladder y crear estados físicos que los seleccionen. Las hojas actuales aportan dibujos; todavía falta un motor jugable que produzca esos estados. Para un juego con respuesta cercana a X4, es más útil portar estas reglas que importar un Rigidbody2D con fuerza arbitraria de salto.

## Parámetros que sí se pueden extraer

[player_enter.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/player_enter.c#L235) y [player_check.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/player_check.c#L252) contienen valores concretos: impulso de salto `FIXED(5.8125)`, velocidad inicial de dash `FIXED(6.5)`, temporizador de dash de suelo `0x1E`, dash aéreo `0x12` y ventana de doble toque `0xC`. También aparecen velocidad aérea normal y velocidad con impulso de dash diferentes en `player_air_steer`.

Estos son **valores del sistema original**, no velocidades Unity listas para pegar. Hay que comprobar la frecuencia de simulación, unidades y signo vertical de la ruta estudiada. Para una conversión con frecuencia confirmada `f` y escala `PPU`, una velocidad en píxeles por tick equivale a `v × f / PPU` unidades por segundo. Una aceleración por tick requiere `a × f² / PPU`. Un contador de `n` ticks equivale a `n / f` segundos. Esta fórmula es una propuesta de conversión, no una afirmación de que todos los campos desconocidos sean aceleraciones.

Propondría un perfil de movimiento editable que guarde esos parámetros y otro perfil para ajustes de X5/X6. Ninguno de estos repositorios demuestra por sí solo la física exacta de ambos juegos posteriores. Separar perfiles permite conservar una base X4 y comparar cambios medidos.

## Animación, respiración y cajas

En [player_common.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/player_common.c#L1101), `player_set_idle_animation` elige una animación distinta cuando la vida cae por debajo de un umbral. Es una referencia útil para agregar reposo normal y cansado; no implica que debamos mezclar el saludo de la armadura definitiva con su respiración.

`player_update_frame_hitbox` consulta tablas por cuadro y personaje; [player_animations.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/player_animations.c) contiene datos que participan en esas selecciones. Hay una separación entre caja de terreno, zona vulnerable y ataque. **Aplicación:** mantener un collider estable para desplazamiento y datos de hurtbox/hitbox por estado o cuadro. Medir automáticamente la silueta del sprite para decidir colisión introduciría cambios por destellos, sable, fuego y poses agachadas.

Las rutinas de animación transmiten eventos y finalización al estado. En Unity puede expresarse con datos temporizados o eventos de AnimationClip, pero debe existir una sola autoridad temporal. Esta revisión ya corrigió un caso equivalente: durante la entrada manual se deshabilita la evaluación automática del Animator, evitando que Idle sobrescriba la materialización después de Update.

## Armas, daño y efectos

En [weapon_24_zero_saber.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/weapons/weapon_24_zero_saber.c#L26), el sable tiene objeto y ciclo de vida propios; se retira ante muerte, bloqueo o cambio de acción, y sigue al jugador. La lectura incluye referencias globales a `g_Player`: no es una arquitectura lista para varios jugadores independientes.

En [weapon_20_plasma_shot.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/weapons/weapon_20_plasma_shot.c), disparo y orbe persistente son entidades diferenciadas, con límites de capacidad y temporizadores. En [collision.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/collision.c#L37), el contacto consulta ataque, zona vulnerable, orientación e invulnerabilidad antes de aceptar un impacto.

**Qué trasladaría:** propietario explícito, equipo, identidad del ataque, caja activa, daño, duración, penetración y política de impactos repetidos. Cada proyectil debe sobrevivir o retirarse según sus propias reglas; cambiar el sprite del personaje no debe borrar ni multiplicar su daño accidentalmente. El pooling puede aplicarse después de definir ese contrato, conservando limpieza de efectos al morir o cambiar de escena.

La biblioteca de este proyecto ya tiene tiros y efectos visuales. El dragón ya emite objetos móviles. Todavía falta conectar las armas jugables y enemigos a una política completa de daño, invulnerabilidad, rebote y muerte. El medidor de vida actual no demuestra esa integración por sí mismo.

## Magma Dragoon y presentación

[main_65_magma_dragoon.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/mains/main_65_magma_dragoon.c) diferencia presentación, salto/aterrizaje, preparación de la barra, llenado de vida y fases de ataque. La lectura del llenado llega a 48; ese valor se adoptó para el dragón del laboratorio. No se extrapoló a los tres jugables.

[misc_38_dragoon_flame.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/misc/misc_38_dragoon_flame.c) permite estudiar la llama asociada al jefe. Para nuestra IA futura conviene registrar anticipación, emisión, recuperación, condiciones de suelo y distancia al objetivo, además de qué ataques no deben coexistir. Las 17 acciones actuales ofrecen presentación y efectos; no constituyen todavía un selector táctico completo del jefe.

READY y teletransporte se coordinan en [effect_27_teleport_intro.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/effects/effect_27_teleport_intro.c), [quad_07_ready_line.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/quads/quad_07_ready_line.c) y [misc_18_ready_text.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/misc/misc_18_ready_text.c). La salida estudiada desplaza palabras completas con sombras; no demuestra un recorte en mitades. La secuencia propia Unity implementa el destello, cian estable y separación opuesta solicitados, sin afirmar igualdad exacta de todos los tiempos nativos.

## Escenarios y pruebas

[stage_objects.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/src/main/stage_objects.c) enumera registros de objetos y checkpoints por fase/subfase. Es una guía de organización de datos. No entrega automáticamente Tilemaps Unity, colliders o un editor de niveles. Para un escenario propio usaría capas visuales, terreno, activadores, cámaras y puntos de reaparición separados, con identificadores estables para objetos destruidos y puertas.

Las pruebas de integración aportan casos concretos: [player_x_input_collision_handoffs.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/tests/integration/player_x_input_collision_handoffs.c), [player_zero_movement_cancels.c](https://github.com/sozud/mmx4/blob/6a6b4555e775820b5f4e45c6f50bd5859223a453/tests/integration/player_zero_movement_cancels.c) y otros archivos inventariados cubren cambios de acción, paredes, techo, escalera y ataques. Su infraestructura y datos son del motor original; lo aprovechable para nosotros es convertir esos casos en recorridos Unity reproducibles. Aquí no se ejecutaron esas pruebas externas.

## Límites y decisión

Hay rutas condicionadas por plataforma, funciones todavía expresadas mediante `INCLUDE_ASM`, campos sin nombre semántico y dependencias de estructuras, recursos y memoria propios del juego. El README requiere recursos del juego para una construcción coincidente. Por ello, no tomaría este repositorio como base compilable de Mega Man X9 ni trasladaría sus cargadores, GPU, direcciones o estructuras binarias a `Assets`.

El archivo raíz declara **AGPL-3.0**. La reutilización literal de código y la de activos deben evaluarse por separado según su procedencia; esta implementación no ha importado ese código ni recursos del repositorio al juego. Para esta etapa, la elección es usarlo como referencia precisa de reglas y casos de prueba, y conservar implementación y datos Unity propios.
