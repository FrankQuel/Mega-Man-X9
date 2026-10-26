# Escenario de prueba de animaciones

La escena generada es `Assets/Recursos/Scenes/Testing/AnimationTestStage.unity`. Está dedicada a revisar arte y reproducción; no implementa movimiento del jugador, enemigos controlados ni combate.

## Requisitos y generación

1. Tener `Assets/Recursos/Animations/AnimationLibrary.asset` con sus clips y controladores completos.
2. Generar los prefabs `Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab` y `Assets/Recursos/Prefabs/Bosses/AxlRig/AxlRig_Animated.prefab`, ambos con Animator en la raíz y estados cuyo nombre coincida con los clips de su controlador.
3. Ejecutar `Mega Man X9 > Animations > Build Animation Test Stage` en el Editor, o `MegaManX9.EditorTools.AnimationTestStageBuilder.Build` mediante ejecución de Editor autorizada.
4. Abrir la escena guardada y pulsar Play.

El constructor crea una escena aditiva, guarda únicamente el resultado generado y vuelve a la escena que estaba activa. Las escenas de trabajo abiertas, incluidas las escenas sin nombre con cambios sin guardar, permanecen abiertas y no se guardan ni se cierran. Solo si la propia escena de prueba está abierta y modificada se ofrece guardarla antes de reconstruir. Si la escena de prueba ya estaba abierta, la cierra y vuelve a abrir con el resultado actualizado. Los archivos originales de arte no se sobrescriben.

## Uso

- **Todos:** seis estaciones simultáneas. La primera página reúne Azul, Azul con armadura, Rojo, Axl pulido, Dragón articulado y GrenadeMan. Las páginas cubren todas las unidades de la biblioteca y los dos rigs. Cada estación conserva su clip seleccionado y permite cambiarlo, reiniciarlo o ampliarlo.
- **Individual:** personaje grande con búsqueda de unidad y búsqueda de clip en el panel derecho. El axl original permanece disponible como **Axl · poses originales** para compararlo con **Axl pulido**.
- **Pausa / Reanudar:** detiene reproducción y avance automático; el tiempo pausado no cuenta para saltar al siguiente clip.
- **Velocidad:** de 0,1× a 2×. La progresión se calcula en tiempo de reproducción y respeta el cambio de velocidad.
- **Mirar a la izquierda:** refleja la imagen o el rig completo.
- **Recorrer todos los clips:** reproduce la secuencia de clips de cada estación. Los clips sin bucle se muestran una vez; los bucles tienen dos vueltas. Cada selección permanece visible al menos 1,5 segundos de reproducción real sin pausa.

Al comenzar, el recorrido automático está desactivado para poder observar el reposo de cada personaje. Los personajes, enemigos y rigs conservan una escala canónica calculada en Idle —o la primera pose disponible si no existe Idle— durante todos sus clips. Agacharse, deslizarse o extender el sable no cambia su tamaño. En los rigs, el centro horizontal y la línea de pies también se calculan en la pose neutral y permanecen fijos. El modo Individual aumenta toda la unidad con la misma proporción. Los efectos y piezas sueltas sí se ajustan a su propio cuadro.

Las estaciones contienen todos los clips de la biblioteca; el constructor comprueba la cobertura sin fijar una cifra que pueda quedar desactualizada. Los clips de los rigs se enumeran desde sus controladores y se añaden como unidades independientes.

## Escenografía

El archivo `escenario jugable.png` es un atlas de piezas de 5192 × 1248 píxeles con fondo magenta, no un fondo completo. Se reutilizan dos recortes comprobados visualmente:

- Ciudad industrial: x=4, y=1004, ancho=1200, alto=180, origen superior izquierdo.
- Carril de plataforma: x=850, y=180, ancho=560, alto=86. Se retira el magenta de este recorte.

Los recortes derivados se guardan en `Assets/Recursos/Sprites/Environment/TestStage`. El escenario añade plataformas con BoxCollider2D, luces de referencia dibujadas como sprites y fondo urbano. Los actores no tienen Rigidbody2D porque se están revisando sus animaciones. Se utiliza el material URP 2D sin iluminación ya creado por la biblioteca. La cámara y los objetos de prueba usan la capa 30 para que una captura aditiva no mezcle objetos de otras escenas.

## Verificación y captura

El resultado de construcción se escribe en `Tools/AnimationPipeline/test_stage_validation.json`, incluyendo cobertura de biblioteca, clips de rigs, unidades, páginas y seis estaciones simultáneas.

`Mega Man X9 > Animations > Capture Test Stage Preview` —o `MegaManX9.EditorTools.AnimationTestStageBuilder.CapturePreview`— genera `Tools/AnimationPipeline/QA/AnimationTestStage.png`. La captura renderiza escenografía y actores; los controles IMGUI se comprueban en Play Mode. Si la escena ya está abierta, captura su pose actual. Si no lo está, la abre de forma aditiva, prepara la primera página y la cierra después.

Pruebas recomendadas: primera página con los tres jugadores y ambos rigs visibles; segunda y últimas páginas accesibles; búsqueda y selección de un clip de cada unidad; pausa de un ataque sin bucle y reanudación sin saltos; cambio de velocidad durante la reproducción; comparación del axl original con el pulido. La generación de código por sí sola no confirma estas pruebas: deben ejecutarse en Unity.
