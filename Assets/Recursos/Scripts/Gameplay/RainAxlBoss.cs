using System;
using MegaManX9.UI;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Second encounter, using only the original Axl sprite bank.</summary>
    [DisallowMultipleComponent]
    public sealed class RainAxlBoss : MonoBehaviour, IRainDamageable
    {
        private enum State { Hidden, Arrival, Idle, Charge, Burst, DashReady, Dash, Recover, Hover, Yielded, Escape }
        [SerializeField] private RainLevelAssets assets;
        [SerializeField] private Vector2 spawn = new Vector2(256, 0);
        [SerializeField] private float arenaLeft = 238, arenaRight = 260;
        [SerializeField] private int maximumHealth = 360;
        [SerializeField] private bool configured;
        private CharacterHealth health;
        private Animator animator;
        private SpriteRenderer art;
        private BoxCollider2D hitbox;
        private Rigidbody2D body;
        private State state;
        private float timer, immunity, contactTimer, shotTimer, phaseTime, dashDirection;
        private int cycle, shots;
        private bool active, faceLeft, yielded;
        private Action presentationCompleted;
        private Vector2 hoverOrigin;
        public const string OriginalUnit = "Axl";
        public DamageTeam Team => DamageTeam.Enemy;
        public CharacterHealth Health => health != null ? health : GetComponent<CharacterHealth>();
        public bool IsAlive => Health != null && Health.CurrentHealth > 0;
        public bool IsActive => active;
        public bool IsVisible => art != null && art.enabled;
        public bool IsPresenting => state == State.Arrival || state == State.Escape;
        public bool HasYielded => yielded;
        public string CurrentAction { get; private set; } = "Idle";
        public int ShotsFired { get; private set; }
        public float ReceivedDamageMultiplier=>1.1f;

        public void Configure(RainLevelAssets source)
        {
            assets = source;
            if (!configured) { spawn = transform.position; configured = true; }
            EnsureComponents(); transform.localScale = Vector3.one * 1.7f;
            if (assets == null || !assets.TryGet(OriginalUnit, "Idle", out _))
                throw new InvalidOperationException("Axl boss requires the original Axl animation bank.");
            maximumHealth = Mathf.Max(360, maximumHealth); health.Configure(maximumHealth, true); ResetBoss();
        }

        public void ConfigureBounds(float left, float right) { arenaLeft = Mathf.Min(left, right); arenaRight = Mathf.Max(left, right); }
        private void Awake() => EnsureComponents();
        private void EnsureComponents()
        {
            if (art == null) art = GetComponent<SpriteRenderer>();
            if (art == null) art = gameObject.AddComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null) animator = gameObject.AddComponent<Animator>();
            if (health == null) health = GetComponent<CharacterHealth>();
            if (health == null) health = gameObject.AddComponent<CharacterHealth>();
            if (hitbox == null) hitbox = GetComponent<BoxCollider2D>();
            if (hitbox == null) hitbox = gameObject.AddComponent<BoxCollider2D>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            body.constraints = RigidbodyConstraints2D.FreezeRotation; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            hitbox.isTrigger = true; hitbox.size = new Vector2(.48f, .75f); hitbox.offset = new Vector2(0, .38f);
            gameObject.layer = 10; art.sortingOrder = 34;
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void ResetBoss()
        {
            EnsureComponents(); active = false; yielded = false; state = State.Hidden; presentationCompleted = null;
            immunity = contactTimer = phaseTime = 0; cycle = shots = ShotsFired = 0;
            health.Configure(maximumHealth, true); body.position = spawn; transform.position = spawn; body.linearVelocity = Vector2.zero;
            hitbox.enabled = false; Play("Idle", true); art.color = Color.white; art.enabled = false;
        }

        public void BeginArrival(Action completed = null)
        {
            active = false; state = State.Arrival; presentationCompleted = completed;
            art.enabled = true; art.color = Color.white; hitbox.enabled = false;
            FacePlayer(); timer = Play("TeleportIn", true); timer = Mathf.Max(.65f, timer);
            RainLevelAudio.Play("arrival");
        }

        public void SetStoryPosition(Vector2 position) { EnsureComponents(); Move(position); }

        public float PlayPresentation(string clip)
        {
            art.enabled = true; return Play(clip, true);
        }

        public void BeginFight()
        {
            active = true; yielded = false; state = State.Idle; timer = .30f;
            immunity = .35f; contactTimer = 0; health.ResetHealth(); art.enabled = true; art.color = Color.white;
            hitbox.enabled = true; animator.speed = 1.18f; FacePlayer(); Play("Idle", true);
        }

        public void HoldAtOne()
        {
            active = false; yielded = true; state = State.Yielded; hitbox.enabled = false;
            if (health.CurrentHealth < 1) health.Heal(1);
            body.position = new Vector2(body.position.x, spawn.y); body.linearVelocity = Vector2.zero;
            art.color = Color.white; animator.speed = 1; Play("Idle", true);
        }

        public void BeginEscape(Action completed = null)
        {
            HoldAtOne(); state = State.Escape; presentationCompleted = completed;
            RainLevelAudio.Play("departure");
            timer = Mathf.Max(.65f, Play("TeleportOut", true));
        }

        private float Play(string clip, bool restart = false)
        {
            if (assets == null) return .5f;
            assets.PlayOn(animator, art, OriginalUnit, clip, restart); CurrentAction = assets.Find(OriginalUnit, clip)?.animationName ?? clip;
            return assets.Find(OriginalUnit, clip)?.clip.length ?? .5f;
        }
        private void FacePlayer()
        {
            var player = RainLevelGame.Instance != null ? RainLevelGame.Instance.Player : null;
            if (player != null) Face(player.transform.position.x < transform.position.x);
        }
        public void Face(bool left) { faceLeft = left; if (art != null) art.flipX = left; }

        private void Update()
        {
            var game = RainLevelGame.Instance;
            if (game == null) return;
            bool stopped = game.IsPaused || game.IsTransformationPaused;
            animator.speed = stopped ? 0 : active ? 1.18f : 1;
            if (stopped) return;
            float dt = Time.deltaTime;
            if (IsPresenting)
            {
                timer -= dt;
                if (timer > 0) return;
                bool escape = state == State.Escape; state = escape ? State.Hidden : State.Idle;
                if (escape) art.enabled = false; else Play("Idle", true);
                var callback = presentationCompleted; presentationCompleted = null; callback?.Invoke(); return;
            }
            if (!active || game.InputLocked || game.Player == null) return;
            immunity = Mathf.Max(0, immunity - dt); contactTimer = Mathf.Max(0, contactTimer - dt); timer -= dt; phaseTime += dt;
            art.color = immunity > 0 && ((int)(Time.time * 30) & 1) == 0 ? new Color(.5f, 1, 1, .55f) : Color.white;
            switch (state)
            {
                case State.Idle:
                case State.Recover:
                    if (timer <= 0) Decide();
                    break;
                case State.Charge:
                    if (timer <= 0) { state = State.Burst; shots = 3; shotTimer = 0; Play("ChargedShot", true); }
                    break;
                case State.Burst:
                    shotTimer -= dt;
                    if (shotTimer <= 0 && shots > 0)
                    { FireAimed(shots == 2 ? .06f : 0, 5, 12.5f); shots--; shotTimer = .19f; Play("Shoot", true); }
                    if (shots == 0 && shotTimer <= 0) Recover(.20f);
                    break;
                case State.DashReady:
                    if (timer <= 0) { state = State.Dash; timer = .62f; dashDirection = faceLeft ? -1 : 1; Play("DashAttack", true); }
                    break;
                case State.Dash:
                    Move(new Vector2(Mathf.Clamp(body.position.x + dashDirection * 9 * dt, arenaLeft + .8f, arenaRight - .8f), spawn.y));
                    if (timer <= 0 || body.position.x <= arenaLeft + .81f || body.position.x >= arenaRight - .81f) { Play("DashStop", true); Recover(.18f, false); }
                    break;
                case State.Hover:
                    float height = Mathf.Sin(Mathf.Clamp01(phaseTime / 1.22f) * Mathf.PI) * 2.6f;
                    float x = Mathf.Clamp(hoverOrigin.x + dashDirection * phaseTime * 3.2f, arenaLeft + 1, arenaRight - 1);
                    Move(new Vector2(x, spawn.y + height)); shotTimer -= dt;
                    if (shotTimer <= 0 && phaseTime > .22f && phaseTime < .95f)
                    { FireAimed(-.22f, 4, 10); FireAimed(.22f, 4, 10); shotTimer = .28f; Play("AirSpreadShot", true); }
                    if (phaseTime >= 1.22f) { Move(new Vector2(x, spawn.y)); Recover(.16f); }
                    break;
            }
            ContactDamage();
        }

        private void Decide()
        {
            FacePlayer(); float distance = Mathf.Abs(RainLevelGame.Instance.Player.transform.position.x - transform.position.x);
            int choice = cycle++ % 4;
            if (choice == 2)
            {
                state = State.Hover; phaseTime = 0; shotTimer = .18f; hoverOrigin = body.position;
                dashDirection = faceLeft ? -1 : 1; Play("JetRise", true); return;
            }
            if (choice == 1 || distance < 3)
            { state = State.DashReady; timer = .20f; Play("DashAttackReady", true); return; }
            state = State.Charge; timer = .28f; Play("Charge", true);
        }
        private void Recover(float seconds, bool idle = true) { state = State.Recover; timer = seconds; if (idle) Play("Idle", true); }
        private void Move(Vector2 position) { body.position = position; transform.position = position; }
        private void FireAimed(float angle, int damage, float speed)
        {
            var hero = RainLevelGame.Instance.Player; if (hero == null) return;
            FacePlayer();
            Vector2 origin = (Vector2)transform.position + new Vector2(faceLeft ? -.7f : .7f, 1.15f);
            Vector2 aim = ((Vector2)hero.transform.position + Vector2.up * .43f - origin).normalized;
            aim = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg) * aim;
            var projectile = RainProjectile.Spawn(assets, DamageTeam.Enemy, gameObject, origin, aim * speed, damage,
                2.9f, "GigaDeathProjectiles", "SmallShot", 2.6f, .18f);
            if (projectile != null) { projectile.Tint = new Color(.35f, 1, .78f); ShotsFired++; }
            RainLevelAudio.Play("shot");
        }
        private void ContactDamage()
        {
            if (contactTimer > 0 || !hitbox.enabled) return;
            foreach (var other in Physics2D.OverlapBoxAll(hitbox.bounds.center, hitbox.bounds.size, 0, 1 << 9))
            {
                var target = RainCombat.Receiver(other);
                if (target == null || target.Team != DamageTeam.Player || !target.IsAlive) continue;
                target.TakeHit(state == State.Dash ? 5 : 3, ((Vector2)other.transform.position - body.position).normalized, gameObject);
                contactTimer = .35f; break;
            }
        }
        public void TakeHit(int damage, Vector2 direction, GameObject source)
        {
            if (!active || yielded || damage <= 0 || immunity > 0) return;
            int applied = Mathf.Min(health.CurrentHealth - 1, Mathf.Max(1, Mathf.CeilToInt(damage * ReceivedDamageMultiplier)));
            if (applied > 0) {RainLevelAudio.Play("axlHurt");health.ApplyDamage(applied);}
            immunity = .16f;
            if (health.CurrentHealth <= 1)
            {
                HoldAtOne(); RainProjectile.ClearTeam(DamageTeam.Enemy);
                RainLevelGame.Instance?.AxlBossYielded();
            }
        }
    }
}
