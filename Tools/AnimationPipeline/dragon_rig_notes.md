# Articulated Dragon rig

Integration: call `MegaManX9.EditorTools.DragonRigBuilder.Build()` in the Unity Editor. It does not open, close, or save the user's scene. It creates:

- `Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab`
- `Assets/Recursos/Animations/Bosses/DragonRig/DragonRig.controller`
- `Assets/Recursos/Animations/Bosses/DragonRig/DragonRigLibrary.asset`
- Ten `.anim` assets beside the controller.
- Dedicated single sprites in `Assets/Recursos/Sprites/Bosses/DragonRig/Generated`.
- `Tools/AnimationPipeline/dragon_rig_validation_report.json` after real editor sampling.

Runtime: `DragonRigActor.Play("Walk")`, `.Play("ClawAttack", true)`, `.ClipNames`, `.FaceLeft(bool)`, `.MouthSocket` and `.ClawSocket`. The painted bind pose faces left. `AutoReturnToIdle` defaults true for Jump/ClawAttack/FireBreath/Hurt; Crouch and Death hold their final pose. External gameplay controls world movement and damage. Jump currently has a local visual arc, while the GameObject root stays fixed; avoid applying an additional full-height visual jump if using physics to drive the same demonstration.

The rig has 13 separately painted body segments with a real transform hierarchy: hips, torso, head, upper arms/forearms, thighs/shins/feet. Sources are the sheet's detached head `(214,8,24,22)`, torso `(198,34,57,23)`, legs `(196,64,61,46)`, and fists `(178,51,16,20)` / `(259,54,14,21)`. Their placement was matched to the reference neutral pose using exact opaque color correspondence (head/torso 100%; legs 99%; fists 94–96%). Existing Part_XX exports and their original coordinates were inspected; source pixels are copied directly into dedicated clean segment sprites so imported pivots are anatomical rather than the generic library's bottom center.

A separate existing open claw `(554,81,21,27)` replaces only the near forearm during ClawAttack. FireBreath moves head, torso and arms and enables three moving muzzle flame renderers using existing DragonEffects/FireballRight frames. It does not replace the body with a complete flat character frame.

Animations: Idle (2 s), Breathe (1.6 s), Walk (1 s), Run (0.6 s), Jump (1 s), Crouch (0.5 s), ClawAttack (0.72 s), FireBreath (1.4 s), Hurt (0.4 s), Death (1.2 s). Curves are newly authored for this project, sampled at 30 fps. Both leg segment lengths remain fixed through a two-bone IK solve during Walk/Run/Crouch. Rotation and position animate independently; no body scale channels are authored. Death collapses onto the floor and remains there. Tiny original-paint hinge patches cover seams; no extra whole arm or leg is overlaid.

Authoring reproduction: run `dragon_rig_author.py`, then `dragon_add_claw.py`. No original image is changed. All generated authoring sprites remain under Tools until the Unity builder copies them into its dedicated Assets folder.

Verification performed before integration: Python output structure validation, no missing original body pixels, ten clip loops/poses rendered to `QA/Dragon/dragon_rig_contact.png` and per-clip GIFs. FullRect sprite bound corners sampled through the hierarchy remain within approximately 3.1 pixels of the floor during Walk/Run (includes two pixels of transparent texture padding), and 2.8 pixels for Death. The three C# files were compiled successfully with Unity's bundled Roslyn compiler and actual project references; Unity Editor Build() and live Animator playback remain for the integrating parent agent to execute. The builder will sample 17 poses per clip in Unity, reject invalid transforms/body scale curves/floor penetration, and save its validation report before saving the prefab.

QA contact sheets show the actual multi-joint body construction. Claw and fire renderer swaps are applied by the Unity builder, so the offline body contact sheet intentionally shows skeletal motion alone.
