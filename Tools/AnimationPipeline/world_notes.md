# World animation audit

- Reviewed all seven enemy sheets, five effect sheets, Items sheet and Dragon sheet visually.
- Original GIFs are static sprite sheets, not animated GIFs. Their authored transparent palette entries survive RGBA conversion; no broad white/magenta color removal is necessary.
- `world_specs.json` contains 166 clip/pose entries and 617 source frames. Single-frame entries are explicitly reusable poses or detached parts, not newly invented animation.
- GrenadeMan and GrenadeManGreen are two visual variants from the filenames. Clip/action names are inferred from visible poses; timing is an initial configurable 8–15 fps rather than original game timing.
- SpikeBall and Burnout sprites are separated by one-pixel gutters; bounding boxes were detected via connected alpha components and verified visually to avoid merged adjacent frames.
- Dragon has 18 whole-body poses plus a two-frame crouching cycle. Many other upper-sheet elements are detached limbs/assemblies, preserved as DragonParts Part_XX. Complete walking, running or articulated combat cannot be reconstructed automatically from these parts without an explicit skeleton and pose design.
- Dragon Effects are real slash, fireball, flame, blast and ring sequences. Color palette swatches at the bottom of the sheet are excluded.
- Small flame clips use the main alpha component so adjacent packed-frame tips do not contaminate them.
- MiniLaserCannon LaserFire includes tiny local excludeRects on four frames because neighboring packed sprite bounds overlap. This preserves the body plus beam while removing a stray row from the next sprite.
- Explosion flashes are separate from orange/gray blast cycles. Meteor up/down orientations remain separate.
- Rain consists of five 160×240 cells. The uniform dark background RGB(24,24,24) is keyed out; raindrop shade pixels remain intact. Sheet credits are excluded.
- BossDoor is categorized Environment. READY stage-intro lettering is categorized UI; static preview/background art below it is not treated as a temporal sequence.
- Item rows contain visible rotational/pulsing frames and were mapped as independent cycles. Name labels are descriptive rather than claims about their intended gameplay.
- All source rectangles were checked against original image bounds; no original resource was edited.

Reproduction of mapping JSON: run QA/World/build_specs.py, then finalize_specs.py, then last_fixes.py. QA/World/qa.py creates contact sheets for visual review. The final JSON is the integration source of truth.
