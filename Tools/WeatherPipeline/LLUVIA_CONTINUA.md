# Lluvia y entorno original

RainWeather conserva su firma Configure y las referencias originales. Una única malla y el shader RainSeamlessRain dibujan dos profundidades de trazos diagonales en coordenadas mundiales. El patrón no se reinicia con la cámara ni repite cuadros de textura. El material de Resources incluye el shader en el jugador compilado.

## Suelos y plataformas

El mapa de alturas de hasta 8192 muestras guarda el suelo inferior de cada columna. Solo los colliders profundos, de al menos 1,5 unidades, delimitan esta base. La lluvia termina al llegar a esa superficie, con un margen de 0,025 unidades.

Los colliders sólidos de la capa 8 aportan además hasta 128 rectángulos de volumen. El shader descarta los trazos dentro de esos sólidos. Una plataforma o techo suspendido no elimina la lluvia del aire que queda debajo: continúa en los espacios abiertos, sin dibujarse dentro de la plataforma. La capa usa orden 1, entre la ciudad del fondo y los cuerpos de terreno de orden 3.

Las consultas usan el PhysicsScene2D de la escena. El mapa flotante RFloat cubre X=-8 hasta LevelLength+8, se refresca cada 0,25 segundos en la región visible y conserva un respaldo RGBAFloat. La cobertura se ajusta después de la cámara. El reloj de lluvia se detiene en pausa y durante bloqueos de juego.

Consultas comprobadas en Unity:

- (20; 0,3): lluvia; (20; -0,3): bajo el suelo, sin lluvia.
- (15; 1): aire bajo plataforma, con lluvia; (15; 1,6): sólido, sin lluvia; (15; 2,1): aire superior, con lluvia.
- (252; 3): aire de la arena, con lluvia; (252; 10,8): techo sólido, sin lluvia.

APIs observables: ActiveTileCount, IsContinuous, SurfaceOcclusionEnabled, AirGapRainEnabled, SolidOcclusionCount, RainClock, SampleRainVisibleAt y SampleRainFloorHeight. La consulta CPU indica si el lugar permite lluvia; no implica que un trazo aleatorio ocupe ese píxel en cada instante.

## Edificios, postes y pinchos

RainLevelBuilder extrae 18 recortes de edificios.png, pinchos.png y postes, pinchos y edificios extra.png. Conserva intactos los originales y genera sprites transparentes en Assets/Recursos/Sprites/Environment/RainyFoundry/NativeEnvironment. El informe environment_crops.json registra sus coordenadas y las semillas de fondo blanco.

Hay 11 variantes de edificio, dos faroles, un poste de bandera y cuatro grupos de pinchos. Sustituyen los bloques decorativos y las varillas de peligro. No cambian el recorrido ni los colliders. La bandera del checkpoint es decoración física; el indicador de depuración se controla con RainPresentationSettings.

La prueba nativa del nivel comprueba el recorte de lluvia y la presencia del arte suministrado. Las capturas 40_Original_Environment y 41_Original_Spikes están en Previews/RainyFoundry. El shader se compiló en el editor Unity 6000.6.4f1 con URP 17.6.
