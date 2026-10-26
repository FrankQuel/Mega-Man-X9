# Blue player animation inventory

- `blue_specs.json`: 72 clips / 522 source frames: 29 clips from the main blue sheet, 7 from the extra blue sheet, and 36 from the Ultimate armor sheet.
- All rectangles use top-left coordinates in the original image, without resampling the sources.
- Main sheet: every nontransparent source pixel lies inside an exported frame rectangle.
- Ultimate sheet: every nontransparent source pixel lies inside an exported frame rectangle.
- Extra sheet: the only excluded opaque pixels are the 1,810 pixels of the color swatch at x=184..266, y=461..483. All character and effect pixels are covered.
- The three PNG sheets have native transparency. The extra PNG is RGB with a transparent magenta palette entry; conversion to RGBA preserves its transparency. No key color removal is needed.

## Palette references (static, not animation clips)

- `paleta de colores para robot futuristico azul.gif`: one frame, 386 x 90. Three standing/aiming reference pairs and swatches labeled X4/X5, X4 Ultimate Command Mission, and X6.
- `paleta de colores para la armadura definitiva del robot futuristico azul.gif`: one frame, 386 x 90. Three Ultimate armor reference pairs and swatches labeled X4, X5, and X6.
- The extra PNG also has a bottom two-row color swatch, excluded from animations.

## Interpretation and playback notes

- Clip names describe visible poses. The sheets supply no gameplay metadata. `AirRecover`, `AirArmRaise`, and the division of ladder traversal are descriptive interpretations and may be renamed after gameplay integration.
- FPS values are editorial starting values, not extracted original-game timings. Movement loops and single-use actions are marked separately.
- Alternating base/bright shooting poses are separated into normal and `Flash` clips rather than interleaved into one long sequence.
- Beam/orb materialization effects and the standalone Nova Strike trails remain separate from actor movement.
- The first somersault pose in the extra sheet touches the original image's upper boundary; no missing pixels were fabricated.
- Main-sheet airborne hit frame 4 includes its detached left electrical spark, which is not a separate animation frame.
- Wide attacks have explicit body anchors; these are visual estimates and should be checked in the Unity preview against intended root motion.
- Ultimate `DashShoot` and `HoverShoot` clips set `isolateComponent: true`. Some neighboring silhouettes overlap in X projection while remaining separate 8-connected components. Keeping the largest connected component inside each rectangle removes neighbor fringe pixels. Contact sheets built by `build_blue_specs.py` show raw crops, so a few neighbor fringe pixels remain visible there before isolation.

## Reproduction and QA

- `blue_analyze.py` generated row candidates for visual review; it does not import or alter Unity assets.
- `build_blue_specs.py` applies the manually verified grouping, repairs cut band boundaries, builds JSON, checks bounds/nonempty alpha, and creates clip contacts in `QA/Blue/`.
- Original images, Unity project settings, and Unity metadata were not modified.
