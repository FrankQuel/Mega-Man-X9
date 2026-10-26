# Vida, HUD y reposo de armadura definitiva

## Vida real y API

`MegaManX9.UI.CharacterHealth` es un componente independiente de las animaciones. Inicializa 28/28 PV, expone `MaxHealth`, `CurrentHealth`, `Normalized`, `IsAlive` y el evento `Changed(CharacterHealth)`. `ApplyDamage(int)` y `Heal(int)` devuelven la cantidad aplicada, ignoran cantidades negativas y respetan los límites; curar con int.MaxValue no desborda. `ResetHealth()` y su alias `Reset()` restauran el máximo. `Configure(int maximum = 28, bool resetToFull = true)` permite ajustar el máximo.

`AnimationStageHealthHUD.Configure(HealthHUDAssets)` asigna los recursos. Desde OnGUI del escenario, después de establecer su matriz lógica, se llama:

```csharp
hud.Draw(new Rect(40, 170, 130, 240), unit, health, boss: false);
```

`SupportsUnit(string)` indica si una unidad tiene HUD; `IsBossUnit(string)` clasifica su barra. Los jugadores Azul, AzulUltimate y Rojo alinean la barra a la izquierda. Axl, AxlRig, Dragon y DragonRig usan barra de jefe alineada a la derecha. Se aceptan nombres de unidad solos o rutas como `Player/Blue`.

Cada unidad debe conservar su propio CharacterHealth entre cambios de clip; cambiar una animación no reinicia la vida. Los botones de prueba del escenario deben actuar sobre esos componentes mediante `ApplyDamage`, `Heal` o `ResetHealth`. El HUD lee el valor real, muestra la proporción vertical de abajo arriba, el contador PV y el indicador original Normal/Caution/Danger. Una franja de daño se retira en medio segundo después de perder vida. La muerte del modelo no introduce combate ni cambia animaciones por su cuenta.

## Arte original

`HealthHUDBuilder.Build()` genera `Assets/Recursos/UI/HUD/Generated/HealthHUDAssets.asset` y sus sprites derivados. Usa `Assets/Recursos/UI/HUD/hud.png` —228 × 151, con transparencia nativa— y `SourceArt/UI/HUD/HUD Elements.png`. Si se reorganizan los originales, también admite sus rutas alternativas en SourceArt o Assets.

- Marcos X y Z: recortes x=5 o45, y=27, ancho35, alto54. El atlas separa las piezas extensibles con filas vacías y=37 y54; al unirlas resulta un marco35×52 con un interior de 28filas útiles. No se dibujan iconos nuevos.
- Barra neutra de jefe: piezas originales del marco vertical y de la placa vacía inferior. Se evita asignar la cara de Sigma a los jefes Axl o Dragón.
- Ventana de vida: x=9,y=4,w=5,h=28 en marcos de jugador; x=5,y=4,w=5,h=28 en jefe.
- Unidad de vida verde: muestra original x=68,y=38,w=5,h=1 de HUD Elements.
- Unidad de daño roja: muestra original x=104,y=71,w=5,h=1.
- Indicadores circulares originales: recortes24×24 en x38/120/200,y339; se excluye únicamente la pequeña pestaña de texto que sobresale del círculo.

Los sprites usan Point, transparencia nativa, sin mipmaps y sin compresión. Las escalas del HUD se cuantizan a enteros siempre que cabe, conservando los píxeles del marco.

## Ultimate Idle

El antiguo Idle recorría siete poses que empezaban con un gesto de brazos. Ahora `BlueUltimate/Idle` utiliza únicamente los tres cuadros finales de brazos bajos:

```
[292,7,31,43], [332,7,32,43], [372,7,32,43]
```

Se reproduce a8FPS, en bucle y con anclaje centro-inferior nativo. No se escala el cuerpo para simular respiración. La secuencia anterior completa se conserva como `BlueUltimate/IntroSalute`, 7cuadros, 10FPS, sin bucle. Tanto `blue_specs.json` como `build_blue_specs.py` guardan esta decisión para que regenerar no restaure el gesto en Idle. El total de clips de la biblioteca aumenta en uno al importar IntroSalute.

## Verificación

`HealthHUDBuilder.ValidateModel()` comprueba ocho casos: estado inicial, daño, curación, límite inferior, límite superior con int.MaxValue, cantidades negativas y eventos, reinicio y cambio de máximo. Build lo ejecuta y escribe `Tools/AnimationPipeline/hud_v3_validation.json`.

Los cuatro C# compilaron mediante Roslyn de Unity contra las referencias actuales. Las ocho pruebas de modelo también pasaron en Unity 6000.6.4f1 y `x4_reference_validation.json` confirmó daño, curación y capacidad de28/48PV. `x4_visual_qa.json` registra las vistas de daño y reposo en el bucle real. `hud_v3_preview.py` prepara comparaciones estáticas del arte original para28,18,7y0PV y de los tres cuadros de Idle; esas imágenes estáticas no sustituyen la prueba del escenario.

Se consultaron únicamente como referencia los scripts locales GameMaker `player_draw_healthbar` y `player_state_intro`, además de la tabla C de distribución HUD de MMX4. La separación entre vida actual, máximo, capacidad gráfica y estados de entrada se respeta sin copiar su arquitectura al proyecto.
