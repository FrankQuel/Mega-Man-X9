using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>
    /// Boss attack state machine and deterministic effect timeline. Advance receives already
    /// scaled time from the rehearsal stage. AutoAdvance is optional for standalone gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DragonRigActor))]
    public sealed class DragonBossAttackPreview : MonoBehaviour
    {
        [SerializeField] private DragonRigActor actor;
        [SerializeField] private Material effectMaterial, flashMaterial;
        [SerializeField] private Sprite[] fireballFrames, risingFrames, footFlameFrames, groundFrames, ringFrames, clawFrames, orbFrames;
        [SerializeField] private bool autoAdvance;
        [SerializeField] private bool damageEnabled;
        [SerializeField] private LayerMask damageTargets = ~0;
        [SerializeField] private float floorLocalY = 0;
        [SerializeField] private float damageMultiplier = 1;
        [SerializeField] private int minimumAttackDamage;
        [SerializeField] private bool worldSpaceProjectiles;
        [SerializeField] private bool useAuthoredPoses;
        [SerializeField] private float projectileLevelMinX = 0, projectileLevelMaxX = 264;
        private readonly List<DragonBossProjectile> projectiles = new List<DragonBossProjectile>();
        private readonly List<TimedEvent> events = new List<TimedEvent>();
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private readonly HashSet<int> firedEvents = new HashSet<int>();
        private MaterialPropertyBlock flashBlock;
        private SpriteRenderer[] bodyRenderers;
        private Transform effectsRoot, footSocket;
        private Transform visualRoot, hips, poseMouthSocket, poseHandSocket, poseFootSocket;
        private SpriteRenderer authoredArt;
        private DragonRigLibrary.AuthoredPose currentAuthoredPose;
        private DragonRigLibrary.Entry entry;
        private bool paused, mirroredByStage;
        private float elapsed, speed = 1;
        private struct TimedEvent { public float time; public string kind; public float parameter; }
        public static readonly string[] SupportedAttacks = { "FireBreath", "Fireball", "RisingFlame", "FlamingKick", "GroundFireWave", "ClawAttack", "HurtFlash", "Landing", "Crouch" };

        public bool AutoAdvance { get => autoAdvance; set => autoAdvance = value; }
        public bool AutoReturnToCombatIdle { get; set; }
        public bool DamageEnabled { get => damageEnabled; set => damageEnabled = value; }
        public float DamageMultiplier { get => damageMultiplier; set => damageMultiplier = Mathf.Clamp(value, 0, 5); }
        public int MinimumAttackDamage { get => minimumAttackDamage; set => minimumAttackDamage = Mathf.Clamp(value, 0, 20); }
        public bool WorldSpaceProjectiles { get => worldSpaceProjectiles; set => worldSpaceProjectiles = value; }
        public bool UseAuthoredPoses { get => useAuthoredPoses; set => useAuthoredPoses = value; }
        public bool AuthoredPoseVisible => currentAuthoredPose != null && authoredArt != null && authoredArt.enabled;
        public string CurrentAuthoredPoseName=>currentAuthoredPose?.name;
        public bool AuthoredArtFlipped=>authoredArt!=null&&authoredArt.flipX;
        public static bool AuthoredPosePaintedRight(string pose)=>pose=="Crouch"||pose=="CrouchShift";
        public void SetProjectileBounds(float minimumX, float maximumX)
        {
            projectileLevelMinX = Mathf.Min(minimumX, maximumX);
            projectileLevelMaxX = Mathf.Max(projectileLevelMinX + 1, Mathf.Max(minimumX, maximumX));
        }
        public Transform AimTarget { get; set; }
        public float Speed { get => speed; set => speed = Mathf.Clamp(value, 0, 4); }
        public bool IsPaused => paused;
        public bool IsPlaying { get; private set; }
        public string CurrentAttack { get; private set; } = string.Empty;
        public float Elapsed => elapsed;
        public float Duration => entry != null ? entry.duration : 0;
        public int ActiveProjectileCount => projectiles.Count;
        public int EmittedCount { get; private set; }
        public Vector3 LastEmissionWorldPosition { get; private set; }
        public Vector3 LastEmissionWorldVelocity { get; private set; }
        public IReadOnlyList<DragonBossProjectile> ActiveProjectiles => projectiles;
        public bool MirroredByStage => mirroredByStage;

        public void Configure(DragonRigActor rig, Material vfxMaterial, Material bodyFlashMaterial,
            Sprite[] fireballs, Sprite[] rising, Sprite[] footFlames, Sprite[] ground,
            Sprite[] rings, Sprite[] claws, Sprite[] chargeOrbs)
        {
            actor = rig; effectMaterial = vfxMaterial; flashMaterial = bodyFlashMaterial;
            fireballFrames = fireballs; risingFrames = rising; footFlameFrames = footFlames;
            groundFrames = ground; ringFrames = rings; clawFrames = claws; orbFrames = chargeOrbs;
            autoAdvance = false; damageEnabled = false;
        }

        private void EnsureReady()
        {
            if (flashBlock == null) flashBlock = new MaterialPropertyBlock();
            if (actor == null) actor = GetComponent<DragonRigActor>();
            if (visualRoot == null) visualRoot = transform.Find("Visual");
            if (hips == null) hips = transform.Find("Visual/Hips");
            if (authoredArt == null && visualRoot != null)
            {
                var node = visualRoot.Find("AuthoredPoseArt");
                if (node != null) authoredArt = node.GetComponent<SpriteRenderer>();
                poseMouthSocket = visualRoot.Find("MouthPoseSocket"); poseHandSocket = visualRoot.Find("HandPoseSocket"); poseFootSocket = visualRoot.Find("FootPoseSocket");
            }
            if (effectsRoot == null)
            {
                effectsRoot = transform.Find("BossAttackEffects");
                if (effectsRoot == null)
                {
                    effectsRoot = new GameObject("BossAttackEffects").transform;
                    effectsRoot.SetParent(transform, false); effectsRoot.gameObject.layer = gameObject.layer;
                }
            }
            if (worldSpaceProjectiles && effectsRoot.parent != null) effectsRoot.SetParent(null, true);
            else if (!worldSpaceProjectiles && effectsRoot.parent != transform)
            {
                effectsRoot.SetParent(transform, false); effectsRoot.localPosition = Vector3.zero;
                effectsRoot.localRotation = Quaternion.identity; effectsRoot.localScale = Vector3.one;
            }
            if (footSocket == null)
                footSocket = transform.Find("Visual/Hips/NearThigh/NearShin/NearFoot/FootSocket");
            if (bodyRenderers == null)
            {
                var list = new List<SpriteRenderer>();
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.transform.name == "Art" || renderer.transform.name == "OpenClawArt" || renderer.transform.name == "OpenMouthArt") list.Add(renderer);
                bodyRenderers = list.ToArray();
            }
        }

        public bool BeginAttack(string requestedName, bool preserveExistingProjectiles = false)
        {
            EnsureReady();
            string name = CanonicalName(requestedName);
            entry = actor.Library != null ? actor.Library.Find(name) : null;
            if (entry == null || entry.clip == null) return false;
            if (!preserveExistingProjectiles) ClearEffects();
            events.Clear(); firedEvents.Clear(); counts.Clear(); EmittedCount = 0;
            CurrentAttack = name; elapsed = 0; IsPlaying = true;
            actor.AutoReturnToIdle = false;
            // This component owns sampling so effects and bones stop on exactly the same frame.
            actor.Animator.enabled = false;
            if (flashMaterial != null)
                foreach (var renderer in bodyRenderers) renderer.sharedMaterial = flashMaterial;
            BuildTimeline(name); SamplePose(0); ApplyFlash();
            return true;
        }

        public void SetPaused(bool value) { paused = value; }
        public void SetPlayback(bool isPaused, float playbackSpeed) { paused = isPaused; Speed = playbackSpeed; }

        /// <summary>The stage mirrors the common parent. No second transform mirror is applied.</summary>
        public void Flip(bool mirrorAlreadyAppliedByStage) { mirroredByStage = mirrorAlreadyAppliedByStage; }

        public void Advance(float deltaSeconds)
        {
            if (paused || entry == null || deltaSeconds <= 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return;
            float remaining = Mathf.Min(deltaSeconds, 10f);
            while (remaining > .000001f)
            {
                float step = Mathf.Min(remaining, 1f / 60f);
                Step(step); remaining -= step;
            }
        }

        private void Step(float delta)
        {
            float previous = elapsed, next = elapsed + delta;
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                if (projectile == null) { projectiles.RemoveAt(i); continue; }
                projectile.Advance(delta);
                if (!projectile.Alive) { Remove(projectile.gameObject); projectiles.RemoveAt(i); }
            }
            for (int i = 0; i < events.Count; i++)
            {
                TimedEvent timed = events[i];
                if (firedEvents.Contains(i) || timed.time < previous - .00001f || timed.time > next + .00001f) continue;
                firedEvents.Add(i); SamplePose(timed.time);
                int before = projectiles.Count;
                Emit(timed.kind, timed.parameter);
                for (int spawned = before; spawned < projectiles.Count; spawned++) projectiles[spawned].Advance(Mathf.Max(0, next - timed.time));
            }
            elapsed = next; SamplePose(next); ApplyFlash();
            foreach (var projectile in projectiles) if (projectile != null) projectile.RefreshFollow();
            IsPlaying = entry.loop || elapsed < Duration;
        }

        /// <summary>Replays from zero, including all emissions, for reproducible screenshots.</summary>
        public void Seek(float seconds)
        {
            string name = CurrentAttack;
            bool wasPaused = paused;
            if (!BeginAttack(name)) return;
            paused = false; Advance(Mathf.Max(0, seconds)); paused = wasPaused;
        }

        private void SamplePose(float time)
        {
            if (entry?.clip == null) return;
            // Sampled clips only bind enabled curves on alternate head/hand paint.
            // Restore the normal segments before optionally showing authored full poses.
            if (bodyRenderers != null) foreach (var renderer in bodyRenderers) renderer.enabled = renderer.transform.name == "Art";
            if (authoredArt != null) authoredArt.enabled = false;
            currentAuthoredPose = null;
            float sample = entry.loop && entry.duration > 0 ? time % entry.duration : Mathf.Min(time, entry.duration);
            entry.clip.SampleAnimation(gameObject, sample);
            ApplyAuthoredPose(sample);
        }

        private void ApplyAuthoredPose(float sample)
        {
            if (!useAuthoredPoses || authoredArt == null || actor.Library == null || poseMouthSocket == null || poseHandSocket == null || poseFootSocket == null) return;
            float q = entry.duration > 0 ? sample / entry.duration : 0;
            string poseName = PoseFor(CurrentAttack, q);
            var pose = poseName == null ? null : actor.Library.FindPose(poseName);
            if (pose?.sprite == null) return;
            Vector3 motion = hips != null ? hips.localPosition - actor.Library.bindHipPosition : Vector3.zero;
            bool airborne = CurrentAttack == "Jump" || CurrentAttack == "RisingFlame" || CurrentAttack == "FlamingKick";
            motion.y = airborne ? Mathf.Max(0, motion.y) : 0; motion.z = 0;
            authoredArt.sprite = pose.sprite; authoredArt.flipX=AuthoredPosePaintedRight(pose.name);
            authoredArt.transform.localPosition = pose.localOffset + motion;
            if (flashMaterial != null) authoredArt.sharedMaterial = flashMaterial;
            authoredArt.enabled = true; currentAuthoredPose = pose;
            poseMouthSocket.localPosition = pose.mouth + motion; poseHandSocket.localPosition = pose.hand + motion; poseFootSocket.localPosition = pose.foot + motion;
            foreach (var renderer in bodyRenderers) renderer.enabled = false;
        }

        private static string PoseFor(string action, float q)
        {
            switch (action)
            {
                case "Idle": case "CombatIdle": case "Breathe": case "Hurt": case "HurtFlash": return "Guard";
                case "IntroDrop": return "Airborne";
                case "Crouch": return q < .18f ? "Guard" : "Crouch";
                case "Landing": return q < .58f ? "CrouchShift" : "Guard";
                case "Jump": return q < .18f ? "Turn" : q < .80f ? "Airborne" : "CrouchShift";
                case "ClawAttack": return q < .25f ? "Windup" : q < .44f ? "Slash" : q < .65f ? "Sweep" : q < .83f ? "AttackRaised" : "Guard";
                case "Fireball": return q < .24f ? "Windup" : q < .48f ? "AttackRaised" : q < .72f ? "Slash" : q < .89f ? "AttackRaised" : "Guard";
                case "FireBreath": return q < .23f ? "Windup" : q < .30f ? "AttackRaised" : q < .82f ? "CrouchAttack" : q < .94f ? "Recover" : "Guard";
                case "RisingFlame": return q < .20f ? "Windup" : q < .68f ? "JumpAttack" : q < .84f ? "Airborne" : q < .95f ? "CrouchShift" : "Guard";
                case "FlamingKick": return q < .17f ? "Guard" : q < .30f ? "KickHigh" : q < .70f ? "KickForward" : q < .86f ? "Turn" : q < .95f ? "CrouchShift" : "Guard";
                case "GroundFireWave": return q < .20f ? "Windup" : q < .35f ? "CrouchAttack" : q < .74f ? "SweepLow" : q < .90f ? "Recover" : "Guard";
                default: return null;
            }
        }

        private void BuildTimeline(string name)
        {
            switch (name)
            {
                case "FireBreath":
                    for (float q = .32f; q <= .80f; q += .045f) Event(q, "MouthFlame");
                    break;
                case "Fireball": Event(.24f, "HandCharge"); Event(.53f, "HandFireball"); break;
                case "RisingFlame":
                    Event(.28f, "UppercutHit");
                    for (float q = .28f; q <= .62f; q += .06f) Event(q, "RisingFlame");
                    Event(.88f, "LandingRing"); break;
                case "FlamingKick":
                    Event(.32f, "KickHit");
                    for (float q = .30f; q <= .67f; q += .045f) Event(q, "KickFlame");
                    Event(.84f, "LandingRing"); break;
                case "GroundFireWave":
                    Event(.39f, "GroundWave");
                    for (int i = 0; i < 8; i++) Event(.40f + i * .045f, "GroundPillar", i);
                    break;
                case "ClawAttack": Event(.39f, "ClawHit"); break;
                case "Landing": Event(.18f, "LandingRing"); break;
            }
            events.Sort((a, b) => a.time.CompareTo(b.time));
        }

        private void Event(float fraction, string kind, float parameter = 0)
        { events.Add(new TimedEvent { time = fraction * Duration, kind = kind, parameter = parameter }); }

        private float Facing
        {
            get
            {
                Transform visual = transform.Find("Visual");
                return visual == null || visual.localScale.x >= 0 ? -1 : 1;
            }
        }
        private Transform PaintedSocket(Transform socket)
        {
            if (currentAuthoredPose == null) return socket;
            if (socket == actor.MouthSocket) return poseMouthSocket;
            if (socket == actor.ClawSocket) return poseHandSocket;
            if (socket == footSocket) return poseFootSocket;
            return socket;
        }
        private Vector3 At(Transform socket)
        {
            socket = PaintedSocket(socket);
            return socket != null ? transform.InverseTransformPoint(socket.position) : new Vector3(Facing * .12f, .55f, 0);
        }

        private Vector3 AimedHorizontalVelocity(Vector3 origin, float speed)
        {
            if (!damageEnabled || AimTarget == null) return new Vector3(Facing * speed, 0, 0);
            Vector3 delta = transform.InverseTransformPoint(AimTarget.position + Vector3.up * .43f) - origin;
            if (delta.x * Facing <= .02f) return new Vector3(Facing * speed, 0, 0);
            float angle = Mathf.Clamp(Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg, -15f, 15f) * Mathf.Deg2Rad;
            return new Vector3(Facing * Mathf.Cos(angle), Mathf.Sin(angle), 0) * speed;
        }

        private void Emit(string kind, float parameter)
        {
            float facing = Facing;
            Vector3 mouth = At(actor.MouthSocket), hand = At(actor.ClawSocket), foot = At(footSocket);
            switch (kind)
            {
                case "MouthFlame":
                    Vector3 breathOrigin = mouth + Vector3.right * facing * .055f;
                    Spawn(kind, fireballFrames, breathOrigin, AimedHorizontalVelocity(breathOrigin, 2.5f), .48f, .48f, 0, 2); break;
                case "HandCharge":
                    Spawn(kind, orbFrames, hand + Vector3.right * facing * .05f, Vector3.zero, Duration * .30f, .42f, 0, 0, actor.ClawSocket); break;
                case "HandFireball":
                    Vector3 fireballOrigin = hand + Vector3.right * facing * .08f;
                    Spawn(kind, fireballFrames, fireballOrigin, AimedHorizontalVelocity(fireballOrigin, 2.8f), 1.6f, .72f, 0, 4); break;
                case "RisingFlame":
                    Spawn(kind, risingFrames, hand, new Vector3(facing * .18f, 1.4f, 0), .46f, .50f, 180, 3); break;
                case "UppercutHit":
                    Spawn(kind, risingFrames, hand, Vector3.zero, Duration * .34f, .54f, 180, 4, actor.ClawSocket); break;
                case "KickFlame":
                    Spawn(kind, foot, footFlameFrames, new Vector3(-facing * .35f, .15f, 0), .31f, .58f, 0, 2); break;
                case "KickHit":
                    Spawn(kind, footFlameFrames, foot, Vector3.zero, Duration * .34f, .65f, 0, 4, footSocket); break;
                case "GroundWave":
                    Spawn(kind, groundFrames, new Vector3(facing * .28f, floorLocalY + .015f), new Vector3(facing * 2, 0, 0), 1.1f, .85f, 0, 4, null, true); break;
                case "GroundPillar":
                    // Moving columns leave one emitter at staggered times, preserving a visible flame train.
                    Spawn(kind, groundFrames, new Vector3(facing * (damageEnabled ? .30f : .30f + parameter * .17f), floorLocalY + .015f), new Vector3(damageEnabled ? facing * 2 : 0, 0, 0), .50f, .70f, 0, 3, null, true); break;
                case "ClawHit":
                    Spawn(kind, clawFrames, hand, Vector3.zero, .20f, .62f, 0, 3, actor.ClawSocket); break;
                case "LandingRing":
                    Spawn(kind, ringFrames, new Vector3(0, floorLocalY + .02f), Vector3.zero, .30f, .65f, 0, 0, null, true); break;
            }
        }

        // Alternate argument order makes the foot-trail call read from origin to painted frame.
        private void Spawn(string kind, Vector3 position, Sprite[] sprites, Vector3 velocity, float life, float scale, float rotation, int damage)
        { Spawn(kind, sprites, position, velocity, life, scale, rotation, damage); }

        private void Spawn(string kind, Sprite[] sprites, Vector3 position, Vector3 velocity,
            float life, float scale, float rotation, int damage, Transform follow = null, bool onGround = false)
        {
            if (sprites == null || sprites.Length == 0) return;
            follow = PaintedSocket(follow);
            var item = new GameObject("DragonAttack_" + kind); item.layer = gameObject.layer;
            var projectile = item.AddComponent<DragonBossProjectile>();
            Vector3 worldPosition = transform.TransformPoint(position), worldVelocity = transform.TransformVector(velocity);
            Vector3 effectPosition = effectsRoot.InverseTransformPoint(worldPosition), effectVelocity = effectsRoot.InverseTransformVector(worldVelocity);
            bool travellingGroundWave = damageEnabled && (kind == "GroundWave" || kind == "GroundPillar") && Mathf.Abs(worldVelocity.x) > .01f;
            if (travellingGroundWave)
            {
                float edge = worldVelocity.x < 0 ? projectileLevelMinX - 2f : projectileLevelMaxX + 2f;
                life = Mathf.Max(life, Mathf.Max(0, (edge - worldPosition.x) / worldVelocity.x) + .4f);
            }
            int actualDamage = damageEnabled && damage > 0 ? Mathf.Max(minimumAttackDamage, Mathf.RoundToInt(damage * damageMultiplier)) : damage;
            projectile.Initialize(gameObject, effectsRoot, kind, sprites, effectMaterial, effectPosition, effectVelocity,
                life, scale, Facing < 0, rotation, damageEnabled && actualDamage > 0, actualDamage, damageTargets,
                kind == "HandFireball" ? .14f : .10f, onGround, follow);
            if (travellingGroundWave) projectile.ConfigureWorldTraversal(projectileLevelMinX, projectileLevelMaxX, 1 << 8);
            projectiles.Add(projectile); EmittedCount++;
            counts[kind] = GetEmittedCount(kind) + 1;
            LastEmissionWorldPosition = worldPosition;
            LastEmissionWorldVelocity = worldVelocity;
        }

        public int GetEmittedCount(string kind) => counts.TryGetValue(kind, out int count) ? count : 0;

        private void ApplyFlash()
        {
            bool active = (CurrentAttack == "HurtFlash" || CurrentAttack == "Hurt") && elapsed < Duration;
            float flash = active && (Mathf.FloorToInt(elapsed * 18) & 1) == 0 ? 1 : 0;
            flashBlock.SetFloat("_FlashAmount", flash); flashBlock.SetColor("_FlashColor", Color.white);
            if (bodyRenderers == null) return;
            foreach (var renderer in bodyRenderers) if (renderer != null) renderer.SetPropertyBlock(flashBlock);
            if (authoredArt != null) authoredArt.SetPropertyBlock(flashBlock);
        }

        public void ClearEffects()
        {
            foreach (var projectile in projectiles) if (projectile != null) Remove(projectile.gameObject);
            projectiles.Clear();
        }
        private static void Remove(GameObject item)
        {
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
        private void Update()
        {
            if (!autoAdvance) return;
            Advance(Time.deltaTime * Speed);
            if (!paused && AutoReturnToCombatIdle && entry != null && !IsPlaying && !entry.loop &&
                CurrentAttack != "Death" && CurrentAttack != "Crouch") BeginAttack("CombatIdle", true);
        }
        private void OnDisable() { ClearEffects(); }
        private void OnDestroy()
        {
            if (effectsRoot != null && effectsRoot.parent != transform) Remove(effectsRoot.gameObject);
        }

        private static string CanonicalName(string name)
        {
            switch ((name ?? string.Empty).ToLowerInvariant())
            {
                case "hadouken": case "fireball": return "Fireball";
                case "uppercut": case "risingflame": case "shoryuken": return "RisingFlame";
                case "flamingkick": case "divekick": return "FlamingKick";
                case "groundfirewave": case "flamepillar": return "GroundFireWave";
                case "hurtflash": case "hitflash": return "HurtFlash";
                case "landing": case "land": return "Landing";
                default: return name;
            }
        }
    }
}
