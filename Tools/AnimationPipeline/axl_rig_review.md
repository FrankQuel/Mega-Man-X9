# Axl continuity review and replacement rig

## Original illustrated sheet

The original is a pose/reference sheet rather than a consistent animation model. Helmet size, face position, chest and pelvis volume, shoulder placement, leg lengths, and weapon construction change between rows and within several strips. Cropping and pivots can remove neighboring fragments and align a root; they cannot repair this anatomical drift.

Recommended canonical design reference: `Idle[1]`, source rectangle `[100,136,58,82]`. The extracted reference is `QA/AxlPolish/canonical_idle_01.png`. The twelve-frame contact in `canonical_pose_references.png` labels which images describe action silhouettes; only the first image defines the model.

Keep the intentions of Idle, Run, Crouch, Dash, horizontal Shoot, aimed AirShoot, ChargeShot, Hover, Hurt, Death, Victory, Shield, and TeleportIn/Out. Rebuild their bodies from one model. Do not retain the original ChargedShot strip as a continuous action: it jumps between standing, airborne and crouching bodies. RunShoot lacks a coherent gait, AirCharge/AirSpreadShot change aim and construction, and Hover/Victory alter head and torso proportions. Those exported original strips remain reference assets, not the final continuity solution.

The original standalone energy, muzzle, hit and teleport effects can remain useful references or separate effects. Their existence does not imply that a matching gameplay attack, projectile collision, shield or teleport system has been implemented.

## Why a fixed-part rig

A new forty-two-clip raster sheet would require hundreds of consistently redrawn frames, accurate registration and another full silhouette review. A fixed set of articulated parts keeps the same head, armor, joints and limb lengths in every action. It is suitable for this boss at this stage. Its limits are rigid plate articulation, overlap seams, and limited foreshortening: future extreme camera-facing or fully rotated poses may require dedicated replacement parts/hand-drawn keys. Scale and pivots do not repair the old anatomy; the newly generated common body parts replace it.

## Implemented builder

- Editor: `Assets/Recursos/Editor/AnimationPipeline/AxlRigBuilder.cs`.
- Runtime adapter: `Assets/Recursos/Scripts/Animation/AxlRigActor.cs`.
- Input atlas: `Assets/Recursos/Sprites/Bosses/AxlRig/AxlRigParts-v2.png`, 1774 x 887 with alpha.
- Output prefab: `Assets/Recursos/Prefabs/Bosses/AxlRig/AxlRig_Animated.prefab`.
- Output controller/clips: `Assets/Recursos/Animations/Bosses/AxlRig/`.
- Build entry point: `MegaManX9.EditorTools.AxlRigBuilder.Build`.
- Visual capture entry point: `MegaManX9.EditorTools.AxlRigBuilder.CapturePreview`.

The eighteen sprites use explicit source rectangles because some parts cross the nominal grid borders. Muscles/thigh pieces are cropped before the duplicate shin, shin pieces begin at the knee, the head excludes its long lower neck and the torso excludes duplicated lower pelvis. The left-pointing far boot is flipped locally. Individual pieces receive one fixed uniform scale, custom anatomical pivots and articulated child joints. Every clip animates torso/head, shoulder/elbow, hip/knee/ankle rotations. Weapon flare/charge/projectile previews, boot thrust and shield glow are independent renderers. The root is not a single whole-character sprite.

`AxlRigActor.Play(name, restart)` requests a named state and `FaceLeft(bool)` mirrors the Facing parent while preserving the articulation. Gameplay movement, damage, attack timing, projectile spawning and collision are not supplied by this visual rig.

## Clip and alias inventory

Canonical articulated clips (25): Idle, Run, RunShoot, Jump, Dash, DashAttack, DashStop, Crouch, Aim, AimUp, Shoot, CrouchShoot, AirShoot, ChargeShot, AirChargeShot, AirAimUp, JetRise, Hover, Hurt, Death, Victory, ArmRaise, Shield, TeleportIn, TeleportOut.

The controller also has 23 compatibility aliases, preserving all 42 original Axl state names. Aliases share a canonical articulated clip and are not independent animation sets.

| Compatibility state | Canonical motion |
|---|---|
| PointDiagonal | AimUp |
| MaterializeSilhouette | TeleportIn |
| AirDash | Dash |
| ShootUp | AimUp |
| LowShoot | CrouchShoot |
| ShootRecover | Aim |
| Charge | ChargeShot |
| AirSpreadShot | AirShoot |
| DashAttackReady | Aim |
| DashAttackStartup | ChargeShot |
| AirCharge | AirChargeShot |
| DashAfterimage | Dash |
| ShieldReady | Aim |
| ShieldRecover | Idle |
| ChargedShot | ChargeShot |
| HurtRecover | Idle |
| Knockdown | Death |
| TeleportFinish | Idle |
| TeleportBeam | TeleportIn |
| TeleportArrivalPose | Idle |
| AlternateIdle | Idle |
| TeleportArrivalBeam | TeleportIn |
| TeleportArrivalEnergy | TeleportIn |

`AirSpreadShot` currently plays one aimed aerial shot; no distinct spread attack is implemented. `DashAfterimage` has no cloned afterimage renderer. Teleport beam/arrival aliases use the articulated energy fade instead of the old illustrated beam sequence. The build report records these limits.

## Verification status

Both new C# files compiled together successfully through the installed Roslyn compiler against the project's Unity and Sprite Editor assemblies. Only reference-policy warnings were emitted. All original Axl names resolve to a canonical or alias state in a static inventory check.

Unity was not launched by this agent. Prefab construction, atlas pivots, overlap/sorting, animation curves and the visual assembly still need Unity rendering. `CapturePreview()` creates a twelve-pose contact at `Tools/AnimationPipeline/QA/AxlPolish/rig_pose_contact.png` without changing the open scene. The rendering order is Idle, Run, Jump, Dash / Shoot, ChargeShot, Hover, Hurt / Death, Victory, Shield, TeleportIn. Review this before considering the boss visually finished.
