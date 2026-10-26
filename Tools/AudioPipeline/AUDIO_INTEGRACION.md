# Integración de audio de Rainy Foundry

El banco `Assets/Recursos/Resources/RainAudioAssets.asset` referencia **37 grabaciones distintas: 33 WAV y 4 OGG**. Los archivos originales permanecen intactos. `RainAudioAssets.Load()` usa `Resources.Load`, por lo que las referencias también están disponibles en el jugador compilado. El nombre técnico White/Axl identifica el archivo suministrado, aunque el diálogo del nivel presente al personaje como Axl.

`MegaManX9.EditorTools.RainAudioAssetsBuilder.Build()` crea o actualiza el banco sin cambiar su GUID. Configura importadores únicamente si sus ajustes difieren, conserva GUIDs de grabaciones y escribe `Tools/AudioPipeline/audio_build_report.json` con rutas, duraciones y política de carga efectivamente leídas por Unity. El inventario previo a Unity está en `Tools/AudioPipeline/source_inventory.json`.

## Música y ambiente

| Cue canónica | Archivo bajo Assets/Recursos/Audio | Uso automático |
| --- | --- | --- |
| mainTheme | Music/Theme_Title.wav | Portada y menús |
| levelTheme | Music/Theme_Stage.wav | Nivel y READY |
| dragonTheme | Music/Theme_DragoonBoss.wav | WARNING del dragón, combate e intervención antes de su eliminación |
| axlTheme | Music/Theme_AxlBoss.wav | WARNING de Axl y segundo combate |
| conversationTheme | Music/Theme_Conversation.wav | `RainLevelAudio.SetConversation(true)` durante diálogos |
| resultsTheme | Music/Theme_Results.wav | Misión completada |
| rain | Ambience/AMB_Rain.wav | Solo en el nivel; atenuado durante diálogo y pausa; apagado en video, menús, guía, introducción ilustrada y resultados |

Las siete fuentes de música/ambiente tienen `loop=true`, importación Streaming/Vorbis, calidad 0.85 y carga en segundo plano. No precargan el archivo completo. El director solicita `LoadAudioData()` y espera el estado Loaded antes de reproducir. Los WAV de música van de 109 a 1799 segundos; streaming evita cargar pistas de hasta 317 MB descomprimidas a la vez.

Las transiciones de música atenúan la pista anterior y elevan la siguiente en unos 0.32 s por tramo, usando tiempo no escalado. La música sigue sonando atenuada durante pausa. La lluvia queda al 25% de su volumen habitual durante pausa normal o transformación. Los volúmenes globales iniciales del banco son música 0.43, lluvia 0.55 y efectos 0.78; cada grabación conserva además un gain propio. La música principal del nivel y el cierre de puerta, más fuertes en origen, reciben ganancias menores.

## Efectos y voces: nombres exactos para Play

Todas estas llamadas usan `RainLevelAudio.Play("cue")`. Las rutas de esta tabla son relativas a `Assets/Recursos/Audio/`.

| Cue | Grabación |
| --- | --- |
| jump | SFX/SFX_Jump.wav |
| dash | SFX/SFX_DashGround.wav |
| dashAlternate | SFX/SFX_DashAir.wav |
| shot | SFX/SFX_BusterShot.wav |
| chargedShot | SFX/SFX_BusterChargedShot.wav |
| charge | SFX/SFX_BusterCharge.wav |
| saber | SFX/SFX_SaberSwing.wav |
| saber1 | SFX/SFX_SaberCombo01.wav |
| saber2 | SFX/SFX_SaberCombo02.wav |
| saber3 | SFX/SFX_SaberCombo03.wav |
| hurt | SFX/SFX_HeroHurt.wav |
| death | SFX/SFX_HeroDeath.wav |
| arrival | SFX/SFX_HeroArrival.wav |
| departure | SFX/SFX_TeleportDeparture.wav |
| pickup | SFX/SFX_Heal.wav |
| enemyDeath | SFX/SFX_EnemyDeath.wav |
| dragonHurt | SFX/SFX_DragoonHurt.wav |
| dragonAttack | SFX/SFX_DragoonAttack.wav |
| axlHurt | SFX/SFX_AxlHurt.wav |
| ready | SFX/SFX_StageReady.wav |
| warning | SFX/SFX_BossWarning.wav |
| doorOpen | SFX/SFX_BossDoorOpen.wav |
| doorClose | SFX/SFX_BossDoorClose.wav |
| menuMove | SFX/SFX_MenuMove.wav |
| menuConfirm | SFX/SFX_MenuConfirm.wav |
| menuEnter | SFX/SFX_MenuEnter.wav |
| voiceXSwitchOut | Voice/X/VO_XSwitchOut.ogg |
| voiceXSwitchIn | Voice/X/VO_XSwitchIn.ogg |
| voiceZeroSwitchOut | Voice/Zero/VO_ZeroSwitchOut.ogg |
| voiceZeroSwitchIn | Voice/Zero/VO_ZeroSwitchIn.ogg |

Los efectos y voces se importan como PCM, DecompressOnLoad y precarga activa, conservando frecuencia y canales originales. Son 2D mediante `spatialBlend=0`. El director crea los buses Music, Ambience y Effects2D; Charge voice es una voz hija de Effects2D para poder detener el WAV largo de carga sin cortar otros efectos. Sigue la carga real del héroe: termina al soltar el disparo/cambiar de forma/entrar en cinemática, y se pausa durante pausas del juego.

Compatibilidad: `hit` → hurt; `explosion` → enemyDeath; `buster`/`whiteShot` → shot; `chargeShot` → chargedShot; `heal` → pickup; `spawn` → arrival; `escape` → departure. `change` reproduce la voz saliente de la forma activa; no debe acompañarse de otra llamada saliente idéntica. Las mayúsculas y separadores no afectan la búsqueda y también se admiten los nombres de archivo sin extensión.

`score`, `extraLife`, `land`, `enemyShot` y `special` no tienen una grabación nominal: usan efectos generados distintos. `enemyShot` se activa al emitir el proyectil enemigo, `land` al aterrizar y `special` en los ataques de energía de X y en la emisión de la onda definitiva de Zero. Para cualquier cue con grabación disponible se utiliza el AudioClip importado. Los fallbacks se crean bajo demanda; al destruir el director solo se destruyen estos clips transitorios, nunca las grabaciones del banco.

## Observabilidad y validación

El director expone `CurrentMusicCue`, `DesiredMusicCue`, `MusicLoops`, `MusicIsPlaying`, `AmbienceLoops`, `AmbienceIsPlaying`, `LastPlayedCue`, `LastPlayedClip`, `ImportedClipCount`, `HasImportedCue(string)`, y las tres fuentes principales. El banco expone `cues`, `ImportedClipCount` y `TryGet`. `CurrentMusicCue` describe la pista asignada; `MusicIsPlaying` permite diferenciarla de una pista aún cargando.

Inspección de fuentes: 33 WAV PCM16 completos y no silenciosos; cuatro OGG con cabeceras Vorbis y páginas completas. Los picos de los OGG no se han medido porque no había decodificador disponible. Se verificaron 37 rutas, 37 GUIDs distintos y cobertura de todos los archivos de audio. Se añadió la metadata que faltaba para Theme_Results.wav.

Hay características de origen que el director conserva: silencio de entrada de aproximadamente 0.305 s en salto, 0.311 s en daño y 0.225 s en curación; silencio final de 9.787 s en Conversation music y pausas menores al final de otras canciones. `loop=true` conserva esos silencios; no se afirma que sean bucles musicales sin pausa. Main level theme contiene 52873 muestras a escala completa, con grupos de hasta 17: bajar volumen evita sumas excesivas durante reproducción, pero no repara esa saturación ya presente en el WAV. No se recortaron ni procesaron muestras.

Validación Csc del conjunto completo (runtime y editor): **0 errores**. El [banco validado por Unity](audio_build_report.json) registra 37 referencias únicas, 33 WAV, 4 OGG y ninguna grabación ausente. La [prueba nativa del nivel](../LevelPipeline/playtest_report.json) pasó 251 comprobaciones: incluye resolución de los cues nombrados, fuente musical reproduciendo el tema del nivel, música de conversación en la introducción y tema propio de resultados, con loop activo. No sustituye una escucha humana de toda la mezcla ni recorre completas las pistas de hasta treinta minutos.

API primaria consultada: [AudioImporterSampleSettings](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioImporterSampleSettings.html), [AudioImporter](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioImporter.html).

