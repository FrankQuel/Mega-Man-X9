# Dragon enemy attacks

Entry point in Unity: `MegaManX9.EditorTools.DragonRigBuilder.Build()` preserves the existing DragonRig prefab/controller/library paths. It now imports 17 articulated clips, the original open-mouth head variant and the white hit-flash shader, adds `DragonBossAttackPreview`, and runs `DragonBossAttackValidation.Validate(root)` before saving the prefab.

## Stage integration

- Get `DragonBossAttackPreview` on the existing Dragon prefab.
- Set `AutoAdvance = false` and keep the Dragon Animator disabled; the driver samples the skeleton and effect timeline together.
- When selecting/restarting a clip: `BeginAttack(clipName)`.
- Every frame: `SetPaused(paused); Advance(Time.deltaTime * playbackSpeed);`.
- `Advance` expects time already scaled by the stage. `Speed` is used only by the optional automatic Update path. `SetPlayback(paused, speed)` is available for that path.
- `Flip(flipped)` records that the stage mirrored the common parent. It does not apply a second reflection. Moving effects are children of the same root, inherit scale/layer, and therefore mirror with the body. An independent game can use `DragonRigActor.FaceLeft(bool)` and the driver reads the Visual root's intrinsic direction.
- `Seek(seconds)` deterministically rebuilds pose and emitted effects from time zero for captures. It preserves pause state.
- `Duration`, `Elapsed`, `CurrentAttack`, `IsPlaying`, `EmittedCount`, `ActiveProjectileCount`, `ActiveProjectiles`, and `GetEmittedCount(kind)` expose useful preview evidence.
- Standalone enemy entry point: `DragonRigActor.PlayAttack(name, automaticallyAdvance:true)`.

## Attacks implemented

- FireBreath: opens the actual mouth artwork, changes the muzzle socket to the opening, crouches/leans, and emits a horizontal stream from that socket. Each flame travels independently for 0.48 seconds. It is not a fixed image attached to the face.
- Fireball / Hadouken: gathers energy at the hand and releases an independent horizontal projectile from the hand at 53% of the action. The fireball travels at 2.8 local units/s and expires after 1.6 seconds.
- RisingFlame / Uppercut / Shoryuken: raises the near fist, lifts and translates the articulated body, folds legs, emits upward flame particles and a moving hand hit zone, then lands with a ground ring.
- FlamingKick / DiveKick: jumps/lunges with the near leg extended, emits a burning foot hit zone and trailing moving flames, then returns through a landing.
- GroundFireWave: crouches and strikes; one flame front travels along the floor while eight timed pillars appear successively along its route. Pillars keep the source sprite's bottom pivot anchored at floor height.
- ClawAttack: shoulder/elbow strike using the original open claw, with a short moving slash/hit zone following the hand.
- HurtFlash / HitFlash: articulated recoil and alternating opaque white silhouette flashes via a shader property block. Original sprite textures are unchanged.
- Landing and Crouch: actual knee/hip bending with constant limb lengths; Landing emits the existing ring sequence. CombatIdle supplies a raised fighting guard.

All fire/slash/ring/charge sprites are references to existing DragonEffects assets. The open-mouth head is composed only from original detached horn/head `(456,259,22,28)` and open jaw/neck `(453,290,33,37)` with the offsets found in the sheet's full breathing pose. No new character illustration was generated.

## Damage and lifetime

Damage is off by default in the preview. Set `DamageEnabled` and a suitable layer mask only in gameplay. Targets may implement `IDragonBossDamageable.ReceiveDragonDamage(int, Vector2, GameObject)` on a MonoBehaviour. Each projectile records targets already hit, ignores colliders under the attacker, and advances its life/movement with the same pause clock. A pause therefore freezes both bones and all emitted attacks.

## Validation

The builder writes `dragon_boss_attack_validation_report.json` after executing all nine attack timelines, checking emission counts, projectile motion, life expiry, pause, parent mirroring, manual time scaling and the white flash property. It also retains the existing articulated-transform validation report. Both passed in Unity 6000.6.4f1. The real player-loop capture also passed, including the open-mouth state during FireBreath; six attack screenshots are in `Previews/X4References`, recorded by `x4_visual_qa.json`. The six C# files also compiled with Unity's bundled Roslyn compiler and actual project references.

The nine user screenshots were inspected. The downloaded local mmx4/GMS references were read for the separation of breath, hand fireball, rising punch, dive kick and ground-pillar states. Unity code and timing are independently authored; no external game code was copied or executed.

Rebuild authoring data: run `dragon_rig_author.py`, `dragon_add_claw.py`, then `dragon_add_breath_head.py`. The `dragon_attack_author.py` and `dragon_boss_builder_upgrade.py` files are one-time migration notes, not the routine rebuild commands.
