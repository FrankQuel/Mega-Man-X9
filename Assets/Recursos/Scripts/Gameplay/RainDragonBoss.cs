using MegaManX9.Animation;
using MegaManX9.UI;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>First encounter: distance-based decisions, authored attacks and the Axl intervention handoff.</summary>
    [DisallowMultipleComponent]
    public sealed class RainDragonBoss : MonoBehaviour, IRainDamageable
    {
        private enum BrainState { Waiting, IntroFalling, IntroLanding, Approach, Attacking, Recovery, Dying, Defeated }
        [SerializeField] private RainLevelAssets assets;
        [SerializeField] private Vector2 spawn = new Vector2(255, 0);
        [SerializeField] private float arenaLeft = 238, arenaRight = 260;
        [SerializeField] private int maximumHealth = 240;
        [SerializeField, Range(.1f, 1f)] private float receivedDamageMultiplier = .65f;
        [SerializeField, Range(1f, 3f)] private float outgoingDamageMultiplier = 1.75f;
        [SerializeField] private bool configured;
        private DragonRigActor rig;
        private DragonBossAttackPreview attacks;
        private CharacterHealth health;
        private Rigidbody2D body;
        private BoxCollider2D collider;
        private SpriteRenderer[] bodyArt;
        private MaterialPropertyBlock flash;
        private BrainState state;
        private float stateTimer, contactTimer, hitImmunity, moveDirection, deathTimer, nextExplosion;
        private int attackCycle;
        private bool active, defeatReported;
        private Transform visualRoot;
        private float entranceY, entranceVelocity;
        private Vector2 attackOrigin;
        private float attackDirection;
        private System.Action entranceCompleted;
        public DamageTeam Team => DamageTeam.Enemy;
        public CharacterHealth Health => health != null ? health : GetComponent<CharacterHealth>();
        public bool IsActive => active;
        public bool IsEntering => state == BrainState.IntroFalling || state == BrainState.IntroLanding;
        public bool IsAlive => Health != null && Health.IsAlive;
        public string CurrentAction => attacks != null ? attacks.CurrentAttack : string.Empty;
        public int ProjectilesInFlight => attacks != null ? attacks.ActiveProjectileCount : 0;
        public float ReceivedDamageMultiplier=>receivedDamageMultiplier;

        public void Configure(RainLevelAssets source)
        {
            assets = source;
            maximumHealth = Mathf.Max(240, maximumHealth);
            receivedDamageMultiplier = .65f;
            if (!configured) { spawn = transform.position; configured = true; }
            EnsureComponents();
            transform.localScale = Vector3.one * 2.5f;
            health.Configure(maximumHealth, true);
            attacks.AutoAdvance = false; attacks.AutoReturnToCombatIdle = false;
            rig.AutoReturnToIdle = false;
            attacks.SetProjectileBounds(0, 264);
            ResetBoss();
        }

        public void ConfigureBounds(float left, float right)
        {
            arenaLeft = Mathf.Min(left, right); arenaRight = Mathf.Max(left, right);
            if (arenaRight - arenaLeft < 6) arenaRight = arenaLeft + 6;
        }

        private void EnsureComponents()
        {
            if (rig == null) rig = GetComponent<DragonRigActor>();
            if (attacks == null) attacks = GetComponent<DragonBossAttackPreview>();
            if (rig == null || attacks == null) throw new System.InvalidOperationException("RainDragonBoss requires the existing articulated Dragon prefab.");
            if (visualRoot == null) visualRoot = transform.Find("Visual");
            if (health == null) health = GetComponent<CharacterHealth>();
            if (health == null) health = gameObject.AddComponent<CharacterHealth>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            if (collider == null) collider = GetComponent<BoxCollider2D>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider2D>();
            body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (flash == null) flash = new MaterialPropertyBlock();
            var list = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var art in GetComponentsInChildren<SpriteRenderer>(true))
                if (art.transform.name == "Art" || art.transform.name == "OpenClawArt" || art.transform.name == "OpenMouthArt" || art.transform.name == "AuthoredPoseArt") list.Add(art);
            bodyArt = list.ToArray();
            foreach (var child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 10;
            rig.AutoReturnToIdle = false; attacks.AutoAdvance = false; attacks.AutoReturnToCombatIdle = false;
        }

        private void Awake() { EnsureComponents(); }
        private void Start() { IgnoreHeroBody(); }
        private void IgnoreHeroBody()
        {
            var game = RainLevelGame.Instance;
            if (game == null || game.Player == null || collider == null) return;
            foreach (var heroCollider in game.Player.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(collider, heroCollider, true);
        }

        public void ResetBoss()
        {
            EnsureComponents(); active = false; defeatReported = false; state = BrainState.Waiting;
            stateTimer = .55f; contactTimer = 0; hitImmunity = 0; attackCycle = 0; moveDirection = 0; deathTimer = 0; nextExplosion = 0;
            entranceCompleted = null;
            health.Configure(maximumHealth, true); transform.position = spawn; body.position = spawn; body.linearVelocity = Vector2.zero;
            collider.enabled = false; body.simulated = true;
            foreach (var art in bodyArt) art.color = Color.white;
            attacks.DamageEnabled = false; attacks.AimTarget = null; attacks.DamageMultiplier = 1;
            attacks.MinimumAttackDamage = 0; attacks.WorldSpaceProjectiles = false; attacks.ClearEffects(); attacks.SetPaused(false);
            attacks.UseAuthoredPoses = true;
            rig.FaceLeft(true); attacks.BeginAttack("CombatIdle");
            SetFlash(0); IgnoreHeroBody(); UpdateBodyBounds();
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        }

        public void BeginEntrance(System.Action onLanded)
        {
            EnsureComponents(); entranceCompleted = onLanded; active = false; state = BrainState.IntroFalling;
            entranceY = spawn.y + 8f; entranceVelocity = -1.5f; moveDirection = 0;
            body.position = new Vector2(spawn.x, entranceY); transform.position = body.position; collider.enabled = false;
            if (visualRoot != null) visualRoot.gameObject.SetActive(true);
            foreach (var art in bodyArt) art.color = Color.white;
            attacks.DamageEnabled = false; attacks.AimTarget = null; attacks.WorldSpaceProjectiles = true;
            attacks.UseAuthoredPoses = true;
            attacks.ClearEffects(); attacks.SetPaused(false); rig.FaceLeft(true);
            attacks.BeginAttack(rig.Library.Find("IntroDrop") != null ? "IntroDrop" : "Jump");
        }

        private void UpdateEntrance(float dt)
        {
            attacks.SetPaused(false); attacks.Advance(dt);
            if (state == BrainState.IntroFalling)
            {
                entranceVelocity -= 22f * dt; entranceY += entranceVelocity * dt;
                if (entranceY > spawn.y) return;
                entranceY = spawn.y; body.position = spawn; transform.position = spawn;
                state = BrainState.IntroLanding; attacks.BeginAttack("Landing");
                RainLevelAudio.Play("land");
            }
            else if (!attacks.IsPlaying)
            {
                state = BrainState.Waiting; stateTimer = .4f; attacks.BeginAttack("CombatIdle", true);
                System.Action callback = entranceCompleted; entranceCompleted = null; callback?.Invoke();
            }
        }

        public void ActivateBoss()
        {
            if (active || !IsAlive || defeatReported) return;
            active = true; state = BrainState.Waiting; stateTimer = .48f;
            if (visualRoot != null) visualRoot.gameObject.SetActive(true);
            collider.enabled = true;
            attacks.WorldSpaceProjectiles = true; attacks.DamageMultiplier = outgoingDamageMultiplier; attacks.MinimumAttackDamage = 5;
            attacks.DamageEnabled = true; attacks.AimTarget = RainLevelGame.Instance != null && RainLevelGame.Instance.Player != null ? RainLevelGame.Instance.Player.transform : null;
            attacks.SetPaused(false); FacePlayer();
            attacks.BeginAttack("CombatIdle"); IgnoreHeroBody();
        }

        private void Update()
        {
            var game = RainLevelGame.Instance;
            if (game == null) return;
            if (IsEntering)
            {
                attacks.SetPaused(game.IsPaused);
                if (!game.IsPaused) UpdateEntrance(Time.deltaTime);
                return;
            }
            bool frozen = game.InputLocked;
            attacks.SetPaused(frozen);
            if (frozen) { moveDirection = 0; return; }
            float dt = Time.deltaTime;
            hitImmunity = Mathf.Max(0, hitImmunity - dt); contactTimer = Mathf.Max(0, contactTimer - dt);
            if (state == BrainState.Dying) { UpdateDeath(dt); return; }
            if (!active || !IsAlive || game.Player == null) return;
            attacks.Advance(dt * (health.Normalized < .5f ? 1.22f : 1.10f));
            switch (state)
            {
                case BrainState.Waiting:
                case BrainState.Recovery:
                    stateTimer -= dt;
                    if (stateTimer <= 0) Decide();
                    break;
                case BrainState.Approach:
                    stateTimer -= dt;
                    if (stateTimer <= 0 || Mathf.Abs(game.Player.transform.position.x - transform.position.x) < 5.0f)
                    {
                        moveDirection = 0; state = BrainState.Recovery; stateTimer = .12f;
                        attacks.BeginAttack("CombatIdle", true);
                    }
                    break;
                case BrainState.Attacking:
                    if (!attacks.IsPlaying)
                    {
                        state = BrainState.Recovery; stateTimer = health.Normalized < .5f ? .24f : .42f;
                        attacks.BeginAttack("CombatIdle", true);
                    }
                    break;
            }
            ContactDamage();
        }

        private void Decide()
        {
            var player = RainLevelGame.Instance.Player;
            float dx = player.transform.position.x - transform.position.x;
            float distance = Mathf.Abs(dx), height = player.transform.position.y - transform.position.y;
            FacePlayer(); moveDirection = 0;
            if (distance > 7f && attackCycle % 3 == 0)
            {
                state = BrainState.Approach; stateTimer = 1.0f; moveDirection = Mathf.Sign(dx);
                attacks.BeginAttack("Run", true); attackCycle++; return;
            }
            string attack;
            if (height > 1.6f && distance < 6.5f) attack = "RisingFlame";
            else if (distance < 2.8f) attack = attackCycle % 3 == 0 ? "ClawAttack" : attackCycle % 3 == 1 ? "RisingFlame" : "FlamingKick";
            else if (distance > 7) attack = "Fireball";
            else
            {
                int pattern = attackCycle % 5;
                attack = pattern == 0 ? "GroundFireWave" : pattern == 1 ? "Fireball" :
                    pattern == 2 && distance < 5 ? "FireBreath" : pattern == 3 && distance < 4.5f ? "FlamingKick" : "GroundFireWave";
            }
            attackCycle++; state = BrainState.Attacking;
            attackOrigin = body.position; attackDirection = dx < 0 ? -1 : 1;
            attacks.BeginAttack(attack, true);
            RainLevelAudio.Play("dragonAttack");
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            if (state == BrainState.IntroFalling)
            {
                if (RainLevelGame.Instance != null && !RainLevelGame.Instance.IsPaused) body.MovePosition(new Vector2(spawn.x, entranceY));
                return;
            }
            if (!active || RainLevelGame.Instance == null || RainLevelGame.Instance.InputLocked) return;
            if (state == BrainState.Attacking)
            {
                float q = attacks.Duration > 0 ? attacks.Elapsed / attacks.Duration : 0;
                float distance = attacks.CurrentAttack == "FlamingKick" ? 4f : attacks.CurrentAttack == "RisingFlame" ? 1.8f :
                    attacks.CurrentAttack == "ClawAttack" ? 1f : attacks.CurrentAttack == "GroundFireWave" ? .65f : 0;
                if (distance > 0)
                {
                    float travel = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.23f, .72f, q));
                    float x = Mathf.Clamp(attackOrigin.x + attackDirection * distance * travel, arenaLeft + 1.4f, arenaRight - 1.4f);
                    body.MovePosition(new Vector2(x, spawn.y));
                }
                return;
            }
            if (state != BrainState.Approach) return;
            Vector2 position = body.position;
            position.x = Mathf.Clamp(position.x + moveDirection * 4.1f * Time.fixedDeltaTime, arenaLeft + 1.4f, arenaRight - 1.4f);
            position.y = spawn.y; body.MovePosition(position);
        }

        private void FacePlayer()
        {
            var game = RainLevelGame.Instance;
            if (game != null && game.Player != null) rig.FaceLeft(game.Player.transform.position.x < transform.position.x);
        }
        private void LateUpdate()
        {
            if (collider != null && collider.enabled) UpdateBodyBounds();
            SetFlash(hitImmunity > .18f && (Mathf.FloorToInt(Time.time * 30) & 1) == 0 ? 1 : 0);
        }
        private void UpdateBodyBounds()
        {
            bool found = false; Bounds bounds = default;
            foreach (var art in bodyArt)
            {
                if (!art.enabled || art.sprite == null || art.color.a < .01f) continue;
                if (!found) { bounds = art.bounds; found = true; } else bounds.Encapsulate(art.bounds);
            }
            if (!found) return;
            Vector3 low = transform.InverseTransformPoint(bounds.min), high = transform.InverseTransformPoint(bounds.max);
            collider.offset = (low + high) * .5f;
            collider.size = new Vector2(Mathf.Max(.24f, Mathf.Abs(high.x - low.x) * .78f), Mathf.Max(.36f, Mathf.Abs(high.y - low.y) * .88f));
        }

        private void ContactDamage()
        {
            if (contactTimer > 0 || !collider.enabled) return;
            var bounds = collider.bounds;
            foreach (var other in Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0, 1 << 9))
            {
                var receiver = RainCombat.Receiver(other);
                if (receiver == null || receiver.Team != DamageTeam.Player || !receiver.IsAlive) continue;
                receiver.TakeHit(state == BrainState.Attacking ? 5 : 4,
                    ((Vector2)other.transform.position - (Vector2)transform.position).normalized, gameObject);
                contactTimer = .45f; break;
            }
        }

        public void TakeHit(int damage, Vector2 direction, GameObject source)
        {
            if (!active || !IsAlive || damage <= 0 || hitImmunity > 0) return;
            int amount = Mathf.Max(1, Mathf.CeilToInt(damage * receivedDamageMultiplier));
            var game = RainLevelGame.Instance;
            if (game != null && game.Phase == RainLevelPhase.Boss && game.AxlBoss != null)
                amount = Mathf.Min(amount, Mathf.Max(0, health.CurrentHealth - RainLevelGame.DragonStoryThreshold));
            if(amount>0)RainLevelAudio.Play("dragonHurt");
            health.ApplyDamage(amount); hitImmunity = .24f;
            // Changed can synchronously begin the story and suspend this actor.
            if (!active) return;
            if (!health.IsAlive) BeginDeath();
        }

        public void HoldForStory()
        {
            active = false; state = BrainState.Waiting; moveDirection = 0; entranceCompleted = null;
            body.linearVelocity = Vector2.zero; collider.enabled = false; hitImmunity = 0;
            attacks.DamageEnabled = false; attacks.AimTarget = null; attacks.ClearEffects();
            attacks.SetPaused(false); attacks.BeginAttack("CombatIdle"); SetFlash(0);
        }
        public void EliminateForStory()
        {
            HoldForStory(); defeatReported = true; state = BrainState.Defeated;
            health.ApplyDamage(health.CurrentHealth); attacks.BeginAttack("Death");
        }
        public void AdvanceStoryDeath(float deltaSeconds)
        {
            if (state != BrainState.Defeated) return;
            attacks.SetPaused(false); attacks.Advance(Mathf.Max(0, deltaSeconds));
        }
        public void HideForStory()
        {
            active = false; collider.enabled = false; attacks.ClearEffects();
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        }

        private void BeginDeath()
        {
            active = false; state = BrainState.Dying; deathTimer = 0; nextExplosion = 0; moveDirection = 0;
            body.linearVelocity = Vector2.zero; collider.enabled = false;
            attacks.DamageEnabled = false; attacks.AimTarget = null; attacks.ClearEffects(); RainProjectile.ClearTeam(DamageTeam.Enemy);
            attacks.BeginAttack("Death");
        }
        private void UpdateDeath(float dt)
        {
            attacks.Advance(dt); deathTimer += dt;
            if (deathTimer >= nextExplosion && deathTimer < 1.65f)
            {
                nextExplosion += .23f;
                Vector2 offset = new Vector2(Mathf.Sin(deathTimer * 19) * .55f, .35f + Mathf.Repeat(deathTimer * 1.7f, 1.2f));
                RainProjectile.Spawn(assets, DamageTeam.Enemy, gameObject, (Vector2)transform.position + offset,
                    Vector2.zero, 0, .60f, "Explosion", "FireBlast", 2.4f);
            }
            if (deathTimer <= 1.9f || defeatReported) return;
            defeatReported = true; state = BrainState.Defeated;
            foreach (var art in bodyArt) { var color = art.color; color.a = .45f; art.color = color; }
            RainLevelGame.Instance?.BossDefeated();
        }
        private void SetFlash(float amount)
        {
            if (flash == null || bodyArt == null) return;
            flash.SetFloat("_FlashAmount", amount); flash.SetColor("_FlashColor", Color.white);
            foreach (var art in bodyArt) if (art != null) art.SetPropertyBlock(flash);
        }
    }
}
