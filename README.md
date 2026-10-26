# Mega Man X9

Proyecto académico y no comercial de acción y plataformas 2D, desarrollado en **Unity 6000.6.4f1** e inspirado en Mega Man X4, X5 y X6. La campaña actual permite alternar libremente entre **X y Zero**, atravesar una fundición bajo la lluvia y completar dos encuentros finales enlazados por diálogos y cinemáticas.

El proyecto incluye un nivel jugable, una biblioteca de animaciones, un laboratorio para revisar recursos y herramientas de construcción y validación. Es una implementación propia para Unity; los repositorios estudiados como referencia no son dependencias de ejecución del juego.

**Para abrirlo correctamente hay que restaurar primero el paquete externo de recursos.** El código y los archivos `.meta` se mantienen en este repositorio; el arte, audio, video y otros recursos pesados se descargan desde la [Release v1.0.0-U1](https://github.com/FrankQuel/Mega-Man-X9/releases/tag/v1.0.0-U1).

## Requisitos

| Componente | Versión o uso |
|---|---|
| Unity Editor | **6000.6.4f1**, revisión `12bfff696524` |
| Renderizado | Universal Render Pipeline **17.6.0**, renderer 2D |
| Entrada | Input System **1.20.0**; el proyecto utiliza el sistema de entrada nuevo |
| Plataforma comprobada durante el desarrollo | Editor de Unity en Windows |
| Python | Python 3.10 o posterior para las herramientas de publicación, que usan la biblioteca estándar; no es necesario para ejecutar el juego |
| Paquetes de Unity | Se restauran desde `Packages/manifest.json` y `Packages/packages-lock.json` |

Usa la versión de Unity indicada en [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt). Abrir el proyecto con otra versión puede cambiar la importación de recursos y la serialización de escenas. No se declara una compilación distribuible ni compatibilidad comprobada con otras plataformas.

## Obtener el proyecto y restaurar los recursos

La distribución se divide en dos partes que deben corresponder a la misma versión:

1. **Repositorio de código:** scripts C#, shaders, herramientas, configuración, documentación y archivos `.meta` para conservar las identidades de Unity.
2. **Paquete externo:** recursos pesados de `Assets/Recursos` y las hojas fuente de `SourceArt`, necesarios para reconstruir y abrir el proyecto completo. Cada paquete incorpora un manifiesto y sumas de comprobación.

La [Release v1.0.0-U1](https://github.com/FrankQuel/Mega-Man-X9/releases/tag/v1.0.0-U1) contiene el [ZIP de recursos](https://github.com/FrankQuel/Mega-Man-X9/releases/download/v1.0.0-U1/resources-v1.0.0-U1.zip), el [manifiesto](https://github.com/FrankQuel/Mega-Man-X9/releases/download/v1.0.0-U1/resources-manifest-v1.0.0-U1.json), las [sumas SHA256](https://github.com/FrankQuel/Mega-Man-X9/releases/download/v1.0.0-U1/SHA256SUMS-v1.0.0-U1.txt) y la [guía de restauración](https://github.com/FrankQuel/Mega-Man-X9/releases/download/v1.0.0-U1/RESTORE-RESOURCES-v1.0.0-U1.md). Descargar solo el código no proporciona el juego completo.

### Restauración antes del primer inicio de Unity

1. Cierra Unity y obtén el código de la revisión elegida.
2. Obtén `resources-VERSION.zip`, `resources-manifest-VERSION.json` y `SHA256SUMS-VERSION.txt` de esa misma publicación. `VERSION` representa el identificador real del paquete.
3. Desde la raíz del proyecto, comprueba el ZIP contra el manifiesto:

   ```powershell
   python Tools/Publication/verify_sourcepack.py --archive RUTA_AL_ZIP --manifest RUTA_AL_MANIFIESTO
   ```

4. Sigue `RESTORE-RESOURCES-VERSION.md`, incluido entre los archivos de publicación, y restaura el contenido en la **raíz del proyecto**, conservando sus rutas. El resultado debe contener `Assets/Recursos`, no una carpeta adicional que anide otra copia del proyecto.
5. Comprueba también los archivos restaurados:

   ```powershell
   python Tools/Publication/verify_sourcepack.py --archive RUTA_AL_ZIP --manifest RUTA_AL_MANIFIESTO --project-root . --check-project
   ```

6. Solo después añade la carpeta raíz a Unity Hub, selecciona **6000.6.4f1** y abre el proyecto. Espera a que terminen la restauración de paquetes, la importación y la compilación.

Sustituye `RUTA_AL_ZIP` y `RUTA_AL_MANIFIESTO` por rutas existentes; usa comillas si contienen espacios. Conserva los `.meta` junto a sus archivos: perderlos o regenerarlos puede romper las referencias de escenas, materiales, prefabs y animaciones.

Para una descarga verificada se dispone de `download_sourcepack.py`. Recibe `--archive-url`, `--manifest-url` y `--archive-sha256`; admite `--project-root`, `--download-dir` y `--extract`. Deben suministrarse **direcciones publicadas reales y la suma esperada**, una vez disponibles. Consulta su ayuda antes de usarlo:

```powershell
python Tools/Publication/download_sourcepack.py --help
```

`Library`, `Temp`, `Logs` y los demás archivos de trabajo del Editor no sustituyen al paquete. Unity regenera sus cachés después de restaurar las fuentes.

## Abrir y jugar

Abre [RainyFoundry.unity](Assets/Recursos/Scenes/Levels/RainyFoundry.unity) y pulsa **Play**. Es la escena principal del nivel; también está incluida como primera escena en la configuración de construcción.

El recorrido de inicio es:

**Video de Capcom → portada → menú → selección X/Zero → guía de teclado → introducción ilustrada → READY → materialización → diálogo de llegada → juego.**

El video original dura aproximadamente **11,35 segundos**, conserva su proporción y audio y no se puede omitir con Enter, Escape ni pausa. La portada aparece al terminar la reproducción real. Volver al menú desde la partida no vuelve a reproducirlo. Un error de preparación o decodificación se muestra sobre negro y mantiene el bloqueo de inicio; no se considera una finalización correcta.

En la portada, **Enter** abre el menú. **Empezar** permite elegir el héroe inicial; ambos estarán disponibles durante la partida. **Opciones** permite remapear las teclas y **Salir** cierra el ejecutable o detiene Play dentro del Editor. Los menús admiten teclado y ratón; las acciones de navegación compatibles con mando usan sus botones por posición.

La guía previa muestra el teclado suministrado, con **13 acciones principales** resaltadas según la configuración vigente. Después, cuatro ilustraciones cuentan el descenso del elevador Jakob, la recuperación y desaparición de Axl y la localización de su señal. Se presentan expresamente en el orden **uno, dos, tres y cuatro**. La frase de destino antecede a READY y a la entrada del héroe. X y Zero intercambian dos turnos antes de entregar el control.

Los textos aparecen por palabras, a ocho por segundo. **Una pulsación de Enter completa la frase; otra continúa.** El Enter que confirma la selección debe soltarse antes de avanzar la guía. Los diálogos no tienen avance automático.

## Controles

Estos son los valores iniciales. Opciones guarda los cambios entre sesiones y la guía posterior refleja las teclas realmente asignadas.

| Acción | Tecla principal | Alternativa inicial | Mando |
|---|---|---|---|
| Mover izquierda/derecha | A / D | Flechas izquierda/derecha | Stick izquierdo / cruceta |
| Subir, apuntar arriba | W | Flecha arriba | Dirección arriba |
| Bajar, agacharse | S | Flecha abajo | Dirección abajo |
| Saltar | Espacio | Z | Botón inferior |
| Atacar; cargar Buster manteniendo y soltar | J | X | Botón izquierdo |
| Dash de suelo o aire | K | C / Mayús izquierda | Botón derecho |
| Especial contextual | L | V | Botón superior |
| Cambiar X / Zero | Tab | — | Botón superior derecho, RB/R1 |
| Alternar armadura de X | U | — | Botón superior izquierdo, LB/L1 |
| Alternar poder de Zero | H | — | Select / View |
| Pausa | P | Escape | Start / Menu |
| Reintentar desde el checkpoint | R | — | Acción del menú de pausa |

Mantener salto permite alcanzar mayor altura. **Arriba + especial** y **abajo + especial** seleccionan técnicas según el personaje y su estado en suelo o aire.

Las alternativas de teclado se habilitan mientras la acción conserve su tecla principal inicial y la alternativa no esté ocupada por otra acción. Al remapear se utiliza la nueva tecla principal. El mando mantiene su distribución por posición; las letras impresas dependen del dispositivo.

En Opciones, selecciona una acción con las flechas y pulsa Enter para capturar la nueva tecla. Una tecla ya asignada intercambia las dos acciones. **Aplicar y volver** guarda; **Volver sin cambios** o Escape descarta la edición. La pausa también admite flechas, Enter y ratón.

## Campaña actual

### Movimiento, formas y combate

X y Zero comparten un cuerpo físico y conservan la posición, la velocidad y el estado de desplazamiento al cambiar. Cada uno mantiene **28 PV propios**. El cambio funciona en suelo, salto, pared y escalera, y utiliza rayos de salida y entrada. Durante la transformación se congela brevemente la simulación de enemigos, proyectiles y física; los rayos siguen animándose y el juego continúa después.

X utiliza Buster normal y cargado, disparos contextuales y armadura definitiva. Zero dispone de combo de sable, ataques aéreos y técnicas direccionales. Su forma potenciada conserva **PinkFlameAura** y un definitivo con carga, golpe vertical y onda de sable. El salto, dash, deslizamiento/salto de pared, escalada, daño y agachado seleccionan cuadros propios de cada variante.

La sesión comienza con **tres vidas compartidas**. Si una forma pierde toda su energía, pasa a la otra. Agotar ambas energías o caer en un foso consume una vida y devuelve al checkpoint con recuperación. Con cero vidas aparece Game Over. El reintento manual y la nueva partida son operaciones diferentes: comenzar otra partida reinicia puntuación, recompensas y semilla de drops.

### Recorrido y recompensas

La fundición tiene **264 unidades de longitud**, **17 enemigos ordinarios de cuatro comportamientos**, **ocho escaleras**, **seis checkpoints**, fosos y peligros eléctricos. Combina azoteas, drenajes, mantenimiento, condensadores, hornos y torres, con alturas desde −2 hasta 9 unidades.

La carrera es de 3,25 unidades/s; el dash, de 7. El salto completo alcanza aproximadamente 1,85 unidades y los ascensos obligatorios se han adaptado a esta física. La cámara conserva el último suelo seguro al caer hacia un vacío y permite descender donde hay plataformas reales. La ciudad se repite solo horizontalmente sobre un cielo superior negro. La lluvia continua se recorta contra los sólidos sin borrar el aire abierto bajo las plataformas.

Los enemigos conceden **100 puntos por baja** y usan una semilla nueva en cada partida: **65 % orbe de puntos, 25 % sin drop y 10 % salud pequeña**. El orbe azul aporta 100 puntos y no cura. Salud grande y vidas extra son recompensas fijas en rutas elevadas; sus identificadores evitan volver a cobrarlas al reintentar en la misma partida.

### Encuentros finales

La puerta original da paso a una arena cerrada. **WARNING** precede a la caída y al aterrizaje de **Magma Dragoon**, que comienza con **240 PV**. Combina aproximación, golpes, fuego y ondas de suelo.

Al alcanzar **60 PV**, el combate se interrumpe: Axl aparece y elimina al dragón. Los diálogos revelan que Lumine controla su cuerpo. Después de la conversación y un nuevo WARNING se recargan X, Zero y Axl; comienza el segundo combate con **360 PV**, ráfagas, embestidas y ataques aéreos.

Axl se detiene a **1 PV**. Llega el compañero opuesto al héroe activo, se desarrolla la conversación final y salen primero Axl y luego ambos héroes. La misión termina automáticamente y muestra resultados. **No hay una puerta de salida final que deba cruzarse.**

El HUD de acción muestra las energías y vidas con arte original. Puntuación, tiempo, bajas y checkpoint se consultan en pausa y resultados. El HUD se oculta durante las conversaciones para dejar espacio a los retratos.

## Audio, animaciones y laboratorio

El banco integra **33 WAV y cuatro OGG**, con música de menú, nivel, ambos jefes, conversaciones y resultados, además del ambiente de lluvia y efectos por acción. El video utiliza su propio audio. Los cues vinculan acciones con grabaciones; el banco incluye respaldos para acciones sin un efecto específico.

La biblioteca registra **361 entradas**, con **255 secuencias de varios cuadros**, **106 poses o piezas estáticas** y **1.862 referencias a fotogramas**. Estos números describen recursos de revisión; no equivalen a 361 acciones jugables diferentes. La validación de extracción conserva **34 archivos originales sin cambios**.

[AnimationTestStage.unity](Assets/Recursos/Scenes/Testing/AnimationTestStage.unity) permite revisar grupos, clips, velocidad, pausa, reflejo, READY, entradas, salud y ataques del dragón. [AnimationGallery.unity](Assets/Recursos/Scenes/Testing/AnimationGallery.unity) conserva la galería anterior. Las reglas y cifras del laboratorio son independientes del balance de los jefes del nivel jugable.

El encuentro de Axl utiliza el arte original suministrado. El rig axl redibujado se conserva como historial de recursos y no sustituye al personaje del nivel.

## Organización del proyecto

```text
Assets/                         Raíz obligatoria de Unity
  Recursos/                     Contenedor del contenido importado por Unity
    Scripts/                    Componentes de ejecución y modelos
    Editor/                     Builders, validadores y utilidades del Editor
    Scenes/                     Nivel, laboratorios e ilustraciones narrativas
    Sprites/                    Personajes, enemigos, entorno y efectos
    Animations/                 Clips, controladores y biblioteca
    Prefabs/                    Actores y recursos reutilizables
    Audio/                      Música, ambiente y efectos
    UI/                         Menús, teclado, HUD y video de inicio
    Resources/                  Bancos y ajustes cargados mediante Resources.Load
    Settings/                   Configuración de arte y renderizado
Packages/                       Dependencias de Unity
ProjectSettings/                Configuración y versión del proyecto
SourceArt/                      Hojas fuente conservadas para la extracción
Tools/                          Pipelines, especificaciones, informes y documentación
Previews/                       Capturas locales regenerables, fuera de la distribución
```

**`Recursos` no reemplaza a `Assets`.** Unity necesita la raíz `Assets`; `Recursos` es una carpeta contenedora dentro de ella. Las carpetas anidadas `Editor` y `Resources` conservan su función especial. Los bancos se cargan por su nombre relativo a `Resources`, sin añadir `Assets/Recursos` al identificador de carga.

`Previews` y las imágenes de QA generadas dentro de `Tools` son **salidas locales regenerables**: quedan fuera del repositorio y del paquete principal de recursos. Las herramientas de captura pueden producirlas al repetir las pruebas. Los análisis en Markdown y los datos de revisiones de los repositorios estudiados sí se conservan en `Tools/ReferenceAnalysis`; sus árboles externos descargados no forman parte del código distribuido.

La sesión y sus transiciones se coordinan en `RainLevelGame`. `RainHeroController` gobierna la física y las acciones; los componentes visuales seleccionan animaciones. Salud, proyectiles y daño tienen datos y propietarios explícitos. Mundo, cámara, lluvia, audio, HUD y narrativa mantienen responsabilidades separadas. Las herramientas de Editor generan referencias y escenas a partir de los recursos restaurados.

## Reconstrucción y modo de presentación

Para jugar una copia completa ya restaurada no hace falta regenerar todos los recursos. Fuera de Play, los menús principales son:

| Menú de Unity | Resultado |
|---|---|
| **Mega Man X9 > Playable Rain Level > Build Rainy Foundry** | Reconstruye el nivel y sus referencias |
| **Mega Man X9 > Playable Rain Level > Build and Test Playable Level** | Reconstruye y ejecuta la suite nativa de integración |
| **Mega Man X9 > Animations > Apply X4 Reference Corrections** | Actualiza los recursos y la presentación del laboratorio |
| **Mega Man X9 > Animations > Validate Existing Library** | Valida la biblioteca importada |
| **Mega Man X9 > Presentation Mode > Production / Development** | Alterna presentación final y ayudas de desarrollo |

Los builders del nivel respaldan las escenas abiertas con cambios antes de sustituirlas. Revisa y guarda igualmente tu trabajo antes de reconstruir. **Production** es el modo inicial: mantiene el HUD y oculta balizas e indicadores de depuración; **Development** muestra ayudas de control y diagnóstico sin cambiar las reglas de salud, puntos o checkpoints.

Para preparar una distribución, `Tools/Publication/package_release.py` acepta:

```powershell
python Tools/Publication/package_release.py --project-root . --version VERSION
```

Opcionalmente admite `--output Artifacts/Publication` y `--code-ref SHA`. Genera `resources-VERSION.zip`, `resources-manifest-VERSION.json`, `SHA256SUMS-VERSION.txt` y `RESTORE-RESOURCES-VERSION.md`. Usa un identificador de versión real y, cuando exista, la revisión de código correspondiente. Generar estos archivos localmente no publica una Release ni confirma que exista un repositorio remoto.

## Validación y límites conocidos

La ejecución completa posterior a la reorganización aprobó **252 comprobaciones nativas y 69 capturas**, sin avisos de desbordamiento de audio. Incluye importación, video inicial, menús, controles, cinemáticas, físicas, combate, cierre narrativo y cobertura inferior del terreno. La validación Python de la biblioteca registra 361 entradas, 1.862 referencias y 34 originales intactos. La migración conserva 1.289 GUID y las 37 grabaciones originales; se retiraron 18 carpetas vacías sin uso.

El resultado vigente de la suite se consulta en [playtest_report.json](Tools/LevelPipeline/playtest_report.json), y la revisión audiovisual en [presentation_report.json](Tools/LevelPipeline/presentation_report.json). Los informes se reemplazan al repetir las pruebas: revisa `success`, el alcance registrado y las fechas de los archivos. Las capturas se generan localmente en `Previews`; no se incluyen en el repositorio ni en el paquete principal. Conviene repetir las comprobaciones al restaurar el paquete en otro equipo.

Las pruebas automatizadas usan el **controlador y la física reales** con entradas simuladas para comprobar saltos, colisiones, escaleras, pared, transformaciones, cámara, menús y combate. También comprueban daño mediante proyectiles reales del héroe. Para alcanzar de forma acotada los umbrales narrativos de 60 y 1 PV utilizan después el contrato público de daño; no simulan una batalla humana completa hasta esos puntos.

Estas pruebas no sustituyen jugar todo el recorrido con teclado o mando físicos. El equilibrio de dificultad, la fatiga durante una sesión completa, la compatibilidad de dispositivos y la reproducción audiovisual en un ejecutable distribuido requieren comprobaciones adicionales. Los análisis de los cuatro repositorios externos son referencias técnicas documentadas, no una garantía de fidelidad exacta a cada fotograma de X4/X5/X6.

## Documentación

| Documento | Contenido |
|---|---|
| [NIVEL_LLUVIA.md](NIVEL_LLUVIA.md) | Guía amplia del nivel, sistemas, reconstrucción y alcance de QA |
| [LEVEL_LAYOUT.md](Tools/LevelPipeline/LEVEL_LAYOUT.md) | Geometría, rutas verticales, escaleras y checkpoints |
| [GUIA_CONTROLES.md](Tools/LevelPipeline/GUIA_CONTROLES.md) | Teclado resaltado, remapeos y diálogo de llegada |
| [NARRATIVA_LLUVIA.md](Tools/LevelPipeline/NARRATIVA_LLUVIA.md) | Ilustraciones, mugshots, turnos y flujo narrativo |
| [VIDEO_INICIO.md](Tools/LevelPipeline/VIDEO_INICIO.md) | Reproducción del MP4 y comprobación del decodificador |
| [MENU_VIDAS_Y_CONTROLES.md](Tools/LevelPipeline/MENU_VIDAS_Y_CONTROLES.md) | Menú, vidas compartidas, opciones y pausa original |
| [MODO_PRESENTACION.md](Tools/LevelPipeline/MODO_PRESENTACION.md) | Producción, desarrollo y ayudas visibles |
| [ANIMACIONES_JUGABLES.md](Tools/LevelPipeline/ANIMACIONES_JUGABLES.md) | Selección contextual de acciones y cuadros |
| [ANIMACIONES.md](ANIMACIONES.md) y [ESCENARIO_PRUEBA.md](ESCENARIO_PRUEBA.md) | Biblioteca y laboratorio de animaciones |
| [AUDIO_INTEGRACION.md](Tools/AudioPipeline/AUDIO_INTEGRACION.md) | Banco, cues, música y efectos |
| [LLUVIA_CONTINUA.md](Tools/WeatherPipeline/LLUVIA_CONTINUA.md) | Shader de lluvia y recorte contra el terreno |
| [Análisis de cuatro repositorios](Tools/ReferenceAnalysis/ANALISIS_EXHAUSTIVO_4_REPOS.md) | Fuentes verificadas, aportaciones y límites de adaptación |
| [AUDITORIA_RECURSOS.md](AUDITORIA_RECURSOS.md) | Inventario y conservación de originales |

Los documentos del laboratorio pueden describir revisiones anteriores y valores propios de sus escenas de prueba. Para el comportamiento de la campaña, consulta la guía del nivel y el código de la revisión que se esté usando.

## Reconocimientos y derechos

**Mega Man, Mega Man X y sus personajes, nombres, logotipos y recursos asociados pertenecen a Capcom y a sus respectivos titulares.** Se reconoce también a los autores de recursos de terceros y de los proyectos estudiados, identificados en la documentación de referencias.

Este trabajo tiene finalidad **académica y no comercial** y no está afiliado, patrocinado ni aprobado por Capcom. La presencia de sprites, música, sonidos, ilustraciones o video en el proyecto o en un paquete de recursos no concede una licencia sobre esos medios. Cualquier licencia que se establezca para código propio debe distinguirse de los derechos de Capcom y de terceros; este README no concede derechos de redistribución o explotación de sus recursos.
