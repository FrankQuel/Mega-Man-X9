# Revisión del jugador rojo

Se inspeccionaron visualmente las cinco hojas de acciones y efectos del jugador rojo. Los GIF son hojas estáticas de un solo cuadro, no animaciones GIF temporizadas.

Se registraron 69 clips con 518 referencias a cuadros (algunos clips reutilizan cuadros para separar inicio, bucle y final). Los nombres describen las poses visibles; no son nombres oficiales ni implican daño o lógica de combate. Los FPS son una configuración inicial de trabajo, ya que las hojas no incluyen tiempos originales.

- `principal`: aparición, reposo, salto y aterrizaje, carrera, tres golpes terrestres de sable, variantes aéreas, dash, acciones de pared, daño, descarga y retirada.
- `extra 1`: ataque de dash, caída al suelo, poses aéreas y tres variantes de sable largo.
- `extra 2`: poses aéreas, aura rosa, ataque energético terrestre, sable pesado y onda grande.
- `extra 3`: variante de efectos mejorados con locomoción, cortes terrestres/aéreos, giro, deslizamiento, pared y descarga.
- `ataques especiales`: pose de fuego, aro de hielo, ondas de sable, proyectiles de hielo y picos de hielo.

Los dos archivos cuyo nombre contiene `paleta` son referencias de color y no secuencias. Conviene conservarlos en `SourceArt/Palettes/Player/Red`. No se han convertido muestras de paleta en clips.

La hoja extra 1 también contiene seis marcas pequeñas de impacto sueltas a la derecha de la fila de poses aéreas (x484–517, y164–205). Son piezas aisladas y no se han presentado como una secuencia temporal confirmada.

Las cajas están en coordenadas del original desde la esquina superior izquierda. Se corrigieron manualmente dos cuadros del final del combo principal: los arcos del sable de la fila superior invaden verticalmente el espacio de la fila siguiente. En extra 3 se usa separación de una columna para no fusionar fotogramas de carrera ni golpes. Los anclajes se estimaron mediante la mediana horizontal de la armadura roja y el extremo inferior de sus píxeles; son una base para revisión artística, especialmente en poses con pies elevados.

La hoja de ataques especiales tiene fondo blanco compartido con ciertos brillos. Se marca `backgroundMode: edge` para retirar solo blanco exterior conectado al borde del recorte y conservar blancos interiores.
