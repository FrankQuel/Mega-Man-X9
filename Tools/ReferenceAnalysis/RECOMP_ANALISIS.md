# MegaManX4Recomp: análisis para Unity 6

Revisión del 24 de octubre de 2026 sobre el commit [`803259663dce7251b1a80c5646df2cb18eeaf308`](https://github.com/mstan/MegaManX4Recomp/tree/803259663dce7251b1a80c5646df2cb18eeaf308) de mstan/MegaManX4Recomp. Su árbol canónico es `b21269e3601f524228f01b20eb241e5b16636b3d`, comprobado por separado en `repo_revisions.json`. Se recalcularon los SHA-1 Git —cabecera `blob`, longitud y bytes— de los **21 archivos de este repositorio enumerados en [source_inventory.json](source_inventory.json)**: todos coinciden. También coinciden lifecycle.c y el escenario de Eregion, descargados en la revisión anterior.

Se inspeccionaron README, CMake, submódulos, licencia, documentación, los límites funcionales de los nueve archivos de mods disponibles, dos pruebas C y el escenario Python. No se ejecutó, compiló ni importó su código. Es una revisión temática del material descargado, no una auditoría de todas las rutas del árbol ni una validación de sus resultados privados.

## Naturaleza y dependencias

El [README](MegaManX4Recomp/README.md) del snapshot presenta una beta 1.0.6 de X4 para Windows/Linux con cooperativo de dos jugadores. Es una recompilación que necesita el disco USA SLUS-00561 y los submódulos PSXRecomp y recomp-ui; no es un motor Unity ni una biblioteca de sprites. [DEVELOPMENT](MegaManX4Recomp/docs/DEVELOPMENT.md), `.gitmodules` y CMake declaran C99/C++17, Python, CMake, SDL2, OpenGL, herramientas de generación y OpenBIOS. Los fuentes generados y recursos del disco no están incluidos en nuestro análisis.

**Aplicación:** aprovechar reglas y casos de prueba en C# propio. No trasladar direcciones MIPS, registros CPU, escrituras de RAM, bancos VAB, parches de instrucciones o el renderizador PSX. LICENSE declara MIT para código original del proyecto; README distingue las licencias de dependencias y la propiedad del juego y sus imágenes.

## Campaña compartida y estado individual

[lifecycle.c](MegaManX4Recomp/src/mods/mmx4_coop_lifecycle.c), líneas 1–107 y 186–228, separa la campaña de P1 de un inventario por personaje: identidad, HP, munición, arma y contenido opcional de E-tanks. Adquirir un tanque pertenece a la campaña; gastar su energía puede ser personal. Puertas, escenas, recompensas y guardado tienen propietario y guardas para no ejecutar dos veces una operación compartida.

**Para Unity:** distinguir un estado de sesión —nivel, checkpoint, progreso y reintentos— del estado de cada actor —vida, arma, recursos, entrada y muerte—. No usar una variable global «jugador actual» para todo. El escenario ya tiene CharacterHealth por unidad; esta separación debe mantenerse al añadir inventario y checkpoints. Tener tres personajes en nuestro laboratorio es una adaptación: el código leído modela dos asientos.

## Muerte, entradas y transiciones

Las funciones `seat_was_dead`, `native_initial_arrival`, `start_arrival`, `death_detection` y `death_animation` distinguen muerto, oculto, entrando y retirado. Un personaje sano oculto durante teletransporte no se considera muerto. Una muerte no debe terminar la partida si queda un compañero vivo; un cambio normal de sección tampoco debe resucitar al fallecido. Las entradas comprueban posición, suelo, controles y bloqueos de escena; no basta copiar una coordenada Y o un indicador antiguo.

**Para Unity:** estados explícitos de entrada/salida y una finalización única antes de habilitar acciones. Conservar vida y propiedad de efectos durante transiciones. La documentación nativa leída describe HP máximo inicial 32; nuestros 28 PV son una decisión del proyecto, no un valor demostrado por esta recompilación.

## Combate, propiedad y balance

[combat.c](MegaManX4Recomp/src/mods/mmx4_coop_combat.c), líneas 180–256, resuelve contactos sin repetir la actualización completa del enemigo. Se identifica al atacante que obtuvo el golpe, distinto del objetivo de IA y del dueño de campaña. Mantiene historial de contactos por enemigo/asiento y propietario de proyectiles, efectos y partículas; la reutilización de un slot debe invalidar su historial.

[balance.c](MegaManX4Recomp/src/mods/mmx4_coop_balance.c), líneas 308–341, conserva la escala nativa del HP y reduce daño opcionalmente: X usa peso 5/10 y Zero 4/10. Acumula décimas sobrantes para evitar que golpes pequeños queden redondeados a cero indefinidamente. La recuperación puede ser compartida o por atacante; no elimina invulnerabilidad de fases guionizadas.

**Para Unity:** un contexto de ataque con dueño, identidad de emisión, objetivos golpeados y vida útil; aplicar un resultado una vez. Configurar daño y recuperación por datos, manteniendo aparte debilidades, bloqueo de fase y reacción. No copiar automáticamente esas proporciones cooperativas a nuestro juego individual. [bosses.c](MegaManX4Recomp/src/mods/mmx4_coop_bosses.c) también muestra cómo añadir un encuentro opcional sin cambiar la identidad de campaña.

## Cámara, activación y presentación

[views.c](MegaManX4Recomp/src/mods/mmx4_coop_views.c) contiene seguimiento con zona muerta y límites, selección del participante cercano y regiones de activación. `split.c` conserva cámaras y controladores locales; `widescreen_plugin.c` diferencia primitivas del mundo y HUD y mantiene parallax entero. Que un actor salga de una cámara no implica que deba desaparecer del mundo si otra lo observa. Dibujar una segunda vista tampoco debe avanzar IA o emitir ataques de nuevo.

**Para Unity:** separar simulación, visibilidad y generación de enemigos; usar límites de sala y anclaje de HUD a pantalla. Empezar con cámara individual y probar cambios de aspecto antes de implementar cámara compartida/dividida. El modo Split del README es experimental: no aporta una solución Unity terminada.

## Menús, HUD y audio

`lifecycle.c`, desde la línea 1199, conserva selección y recursos personales, mientras pausa, reanudación, fundido y salida de campaña permanecen coordinados. Curar puede bloquear la reanudación sin impedir que el compañero navegue. No debe reinstalarse equipo sobre un cadáver para cerrar un menú.

**Para Unity:** HUD ligado al actor y un coordinador de pausa, con solicitudes individuales y confirmaciones compartidas. [audio.c](MegaManX4Recomp/src/mods/mmx4_coop_audio.c) conserva bancos por personaje y una frontera de reinicialización de audio. La idea transferible es identificar emisor/personaje y restaurar recursos tras una recarga; el tratamiento SPU/VAB es específico de PSX y no debe reproducirse en C#.

## Diagnósticos y pruebas

[PLAYTESTING](MegaManX4Recomp/docs/PLAYTESTING.md) propone registros desactivados por defecto: versión, modo, opciones, entradas aceptadas y estado inicial. El escritor detecta saturación/errores y no omite silenciosamente datos. Es útil para nuestro escenario registrar unidad, clip, fase READY/entrada, HP y emisiones, junto con una captura reproducible.

Los dos [tests C](MegaManX4Recomp/tests/test_mmx4_coop_lifecycle.c) modelan límites de propiedad; el de [combate](MegaManX4Recomp/tests/test_mmx4_coop_combat.c) comprueba efectos diferidos, golpes persistentes y preservación del otro actor. Sus propios comentarios advierten que no prueban el juego original. Esa distinción debe mantenerse: modelo, integración Unity y revisión visual son evidencias diferentes. PARITY_QUALIFICATION describe checks acotados, no rendimiento general. El escenario `coop_intro_dragon_scenario.py` observa **Eregion**, no Magma Dragoon; su nombre no permite reutilizarlo como prueba de nuestro jefe bípedo.

## Localización y red

[LOCALIZATION](MegaManX4Recomp/docs/LOCALIZATION.md) separa diálogos, arte de menús, FMV y voces. Usa claves estables, identidad del original y errores explícitos para glifos ausentes, palabras demasiado largas o catálogos obsoletos. Sus traducciones se declaran borradores. **Para Unity:** textos externos con IDs estables, pruebas de longitud y fuentes con acentos; no suponer que traducir diálogos traduce imágenes o audio.

[NETPLAY_EVIDENCE](MegaManX4Recomp/docs/COOP_NETPLAY_EVIDENCE.md) describe delay-sync, compatibilidad mediante huellas de implementación y separación entre simulación y render. Rechaza rollback, savestates y rewind durante la partida. Los checks documentados de CRC, menús y jitter no equivalen a campañas completas ni reproducción íntegra de sesiones de red. **Para Unity:** primero entradas, reloj y estados deterministas en local; elegir arquitectura de red como proyecto independiente. No portar un protocolo basado en memoria invitada ni prometer netplay por mostrar tres actores.

## Orden práctico de aprovechamiento

1. Consolidar CharacterHealth, estados de entrada/muerte y progreso de sesión independiente.
2. Vincular cuerpo y efectos del dragón a un reloj, dueño y emisión únicos.
3. Añadir combate con historial por ataque y pruebas de daño/invulnerabilidad.
4. Integrar checkpoints, cámara de sala, menús y guardado con reglas explícitas.
5. Mantener diagnósticos reproducibles; evaluar cooperativo/red solo después.

Los archivos originales permanecen como referencia de lectura. Las afirmaciones de pruebas externas se atribuyen a sus documentos: no se han repetido aquí ni demuestran que nuestro código Unity ya tenga esos sistemas.
