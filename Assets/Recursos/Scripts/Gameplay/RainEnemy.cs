using MegaManX9.UI;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RainEnemy : MonoBehaviour, IRainDamageable
    {
        public enum Kind { Walker, Gunner, Flyer, Turret }
        [SerializeField] private RainLevelAssets assets;
        [SerializeField] private Kind kind;
        [SerializeField] private string unit = "Burnout";
        [SerializeField] private float patrolMin, patrolMax;
        [SerializeField] private Vector2 spawn;
        [SerializeField] private bool configured;
        private CharacterHealth health;
        private Rigidbody2D body;
        private BoxCollider2D hitbox;
        private Transform visual;
        private SpriteRenderer art;
        private Animator animator;
        private int direction = -1, burstRemaining;
        private float drive, cooldown, shotTimer, hurtTimer, contactTimer, deathTimer;
        private bool shooting, defeated;
        private Vector2 shotDirection;
        private const int GroundMask = 1 << 8;
        public DamageTeam Team => DamageTeam.Enemy;
        public bool IsAlive => health != null && health.IsAlive && !defeated;
        public CharacterHealth Health => health != null ? health : GetComponent<CharacterHealth>();
        public Kind EnemyKind => kind;

        /// <summary>Safe to call while authoring the scene; the singleton is only used at runtime.</summary>
        public void Configure(RainLevelAssets source, Kind behaviour, string animationUnit, float left, float right)
        {
            assets = source; kind = behaviour; unit = animationUnit;
            patrolMin = Mathf.Min(left, right); patrolMax = Mathf.Max(left, right);
            spawn = transform.position; configured = true;
            EnsureComponents(); ConfigureBody(); health.Configure(MaximumHealth(), true);
            Play(IdleClip(), true); Face(direction);
        }

        private void EnsureComponents()
        {
            health = GetComponent<CharacterHealth>(); if (health == null) health = gameObject.AddComponent<CharacterHealth>();
            body = GetComponent<Rigidbody2D>(); if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>(); if (hitbox == null) hitbox = gameObject.AddComponent<BoxCollider2D>();
            visual = transform.Find("Visual");
            if (visual == null) { visual = new GameObject("Visual").transform; visual.SetParent(transform, false); }
            art = visual.GetComponent<SpriteRenderer>(); if (art == null) art = visual.gameObject.AddComponent<SpriteRenderer>();
            animator = visual.GetComponent<Animator>(); if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            gameObject.layer = 10; visual.gameObject.layer = 10; art.sortingOrder = 12;
            visual.localScale = Vector3.one * 2f;
            visual.localPosition = kind == Kind.Flyer ? new Vector3(0, -.24f, 0) : Vector3.zero;
        }

        private void ConfigureBody()
        {
            body.bodyType = kind == Kind.Flyer || kind == Kind.Turret ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            body.gravityScale = kind == Kind.Flyer || kind == Kind.Turret ? 0 : 3;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (kind == Kind.Flyer) { hitbox.size = new Vector2(.62f, .42f); hitbox.offset = Vector2.zero; }
            else if (kind == Kind.Turret) { hitbox.size = new Vector2(1.45f, 1.25f); hitbox.offset = Vector2.up * .625f; }
            else if (kind == Kind.Gunner) { hitbox.size = new Vector2(.70f, 1.06f); hitbox.offset = Vector2.up * .53f; }
            else { hitbox.size = new Vector2(1.05f, .88f); hitbox.offset = Vector2.up * .44f; }
        }

        private int MaximumHealth() => kind == Kind.Turret ? 8 : kind == Kind.Gunner ? 6 : kind == Kind.Flyer ? 3 : 4;
        private string IdleClip() => kind == Kind.Walker ? "Roll" : kind == Kind.Turret ? "Idle" : "Idle";
        private void Awake()
        {
            EnsureComponents(); ConfigureBody();
            if (!configured) { spawn = transform.position; patrolMin = spawn.x - 3; patrolMax = spawn.x + 3; }
        }
        private void Start()
        {
            if (assets == null && RainLevelGame.Instance != null) assets = RainLevelGame.Instance.Assets;
            ResetEnemy(); IgnoreHeroBody();
        }

        private void IgnoreHeroBody()
        {
            var game = RainLevelGame.Instance;
            if (game == null || game.Player == null) return;
            foreach (var collider in game.Player.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(hitbox, collider, true);
        }

        public void ResetEnemy()
        {
            if (body == null) { EnsureComponents(); ConfigureBody(); }
            defeated = false; shooting = false; drive = 0; direction = -1;
            cooldown = .7f + Mathf.Repeat(spawn.x * .13f, .8f); hurtTimer = 0; contactTimer = 0; deathTimer = 0;
            transform.position = spawn; body.position = spawn; body.linearVelocity = Vector2.zero;
            body.simulated = true; hitbox.enabled = true; visual.gameObject.SetActive(true); art.enabled = true; art.color = Color.white;
            health.Configure(MaximumHealth(), true); Play(IdleClip(), true); Face(direction); IgnoreHeroBody();
        }

        private void Update()
        {
            if (defeated)
            {
                deathTimer += Time.deltaTime;
                if (deathTimer > .18f) visual.gameObject.SetActive(false);
                return;
            }
            var game = RainLevelGame.Instance;
            bool frozen = game == null || game.InputLocked || game.Player == null;
            animator.speed = frozen ? 0 : 1;
            if (frozen) { drive = 0; return; }
            float dt = Time.deltaTime;
            hurtTimer = Mathf.Max(0, hurtTimer - dt); contactTimer = Mathf.Max(0, contactTimer - dt);
            art.color = hurtTimer > 0 ? new Color(1, .35f, .35f) : Color.white;
            Vector2 toPlayer = game.Player.transform.position + Vector3.up * .55f - (transform.position + Vector3.up * MuzzleHeight());
            bool nearby = Mathf.Abs(toPlayer.x) < 16 && Mathf.Abs(toPlayer.y) < 9;
            if (!nearby) { drive = 0; shooting = false; return; }
            cooldown -= dt;
            if (hurtTimer > .06f) { drive = 0; return; }
            if (shooting)
            {
                drive = 0; shotTimer -= dt;
                if (shotTimer <= 0)
                {
                    Fire(); burstRemaining--; shotTimer = .18f;
                    if (burstRemaining <= 0) { shooting = false; cooldown = kind == Kind.Turret ? 1.8f : kind == Kind.Gunner ? 1.9f : 2.2f; }
                }
            }
            else if (kind != Kind.Walker && cooldown <= 0 && toPlayer.magnitude < (kind == Kind.Turret ? 13 : 11) &&
                !Physics2D.Raycast(transform.position + Vector3.up * MuzzleHeight(), toPlayer.normalized, toPlayer.magnitude, GroundMask))
            {
                direction = toPlayer.x < 0 ? -1 : 1; Face(direction);
                shotDirection = toPlayer.normalized; shooting = true; shotTimer = kind == Kind.Flyer ? .22f : .38f;
                burstRemaining = kind == Kind.Turret ? 3 : kind == Kind.Gunner ? 2 : 1;
                Play(kind == Kind.Flyer ? "Fire" : kind == Kind.Gunner ? "AimForward" : "Idle", true); drive = 0;
            }
            else
            {
                if (kind == Kind.Turret) { drive = 0; Face(toPlayer.x < 0 ? -1 : 1); }
                else
                {
                    if (transform.position.x <= patrolMin + .08f) direction = 1;
                    if (transform.position.x >= patrolMax - .08f) direction = -1;
                    drive = direction * (kind == Kind.Flyer ? 1.8f : kind == Kind.Walker ? 1.8f : .9f);
                    Face(direction); Play(kind == Kind.Gunner ? "Run" : IdleClip());
                }
            }
            ContactDamage();
        }

        private void FixedUpdate()
        {
            if (!IsAlive || RainLevelGame.Instance == null || RainLevelGame.Instance.InputLocked) { if (body != null) body.linearVelocity = new Vector2(0, body.linearVelocity.y); return; }
            if (kind == Kind.Flyer)
            {
                var player = RainLevelGame.Instance.Player;
                float targetY = spawn.y;
                if (player != null && Mathf.Abs(player.transform.position.x - transform.position.x) < 7)
                    targetY = Mathf.Clamp(player.transform.position.y + 1.55f, spawn.y - 2.5f, spawn.y + .2f);
                targetY += Mathf.Sin(Time.time * 2.1f + spawn.x) * .22f;
                Vector2 next = body.position + Vector2.right * drive * Time.fixedDeltaTime;
                next.y = Mathf.MoveTowards(body.position.y, targetY, 2 * Time.fixedDeltaTime); body.MovePosition(next);
            }
            else if (kind != Kind.Turret)
            {
                if (Mathf.Abs(drive) > .01f)
                {
                    float ahead = Mathf.Sign(drive) * (hitbox.size.x * .5f + .18f);
                    bool wall = Physics2D.Raycast(body.position + Vector2.up * .45f, Vector2.right * Mathf.Sign(drive), Mathf.Abs(ahead), GroundMask);
                    bool floor = Physics2D.Raycast(body.position + new Vector2(ahead, .32f), Vector2.down, .9f, GroundMask);
                    if (wall || !floor) { direction *= -1; drive = direction * Mathf.Abs(drive); }
                }
                body.linearVelocity = new Vector2(drive, body.linearVelocity.y);
            }
        }

        private float MuzzleHeight() => kind == Kind.Flyer ? 0 : kind == Kind.Turret ? 1.05f : .90f;
        private void Fire()
        {
            Vector2 position = (Vector2)transform.position + new Vector2(direction * (kind == Kind.Turret ? .78f : .46f), MuzzleHeight());
            Vector2 aim = shotDirection.sqrMagnitude > .01f ? shotDirection : Vector2.right * direction;
            if (kind == Kind.Turret) aim = Quaternion.Euler(0, 0, (burstRemaining - 2) * 7) * aim;
            RainProjectile.Spawn(assets, DamageTeam.Enemy, gameObject, position, aim * (kind == Kind.Flyer ? 5.5f : 7f), kind == Kind.Flyer ? 2 : 3, 2.8f);
            RainLevelAudio.Play("enemyShot");
            RainProjectile.Spawn(assets, DamageTeam.Enemy, gameObject, position, Vector2.zero, 0, .14f, "GigaDeathProjectiles", "MuzzleFlash", 1.4f);
            if (kind == Kind.Flyer) Play("Fire", true);
        }

        private void ContactDamage()
        {
            if (contactTimer > 0 || !hitbox.enabled) return;
            var bounds = hitbox.bounds;
            foreach (var collider in Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0, 1 << 9))
            {
                var receiver = RainCombat.Receiver(collider);
                if (receiver == null || receiver.Team == Team || !receiver.IsAlive) continue;
                receiver.TakeHit(kind == Kind.Flyer ? 2 : 3, ((Vector2)collider.transform.position - (Vector2)transform.position).normalized, gameObject);
                contactTimer = .45f; break;
            }
        }

        private void Face(int facing)
        {
            if (art == null) return;
            bool paintedRight = kind == Kind.Flyer || kind == Kind.Walker;
            art.flipX = paintedRight ? facing < 0 : facing > 0;
        }
        private void Play(string name, bool restart = false)
        { if (assets != null && animator != null && art != null) assets.PlayOn(animator, art, unit, name, restart); }

        public void TakeHit(int damage, Vector2 directionOfHit, GameObject source)
        {
            if (!IsAlive || damage <= 0 || hurtTimer > 0) return;
            health.ApplyDamage(damage); hurtTimer = .10f;
            if (!health.IsAlive)
            {
                defeated = true; drive = 0; shooting = false; body.linearVelocity = Vector2.zero;
                RainLevelAudio.Play("enemyDeath");
                body.simulated = false; hitbox.enabled = false;
                RainProjectile.Spawn(assets, DamageTeam.Enemy, gameObject, transform.position + Vector3.up * .5f,
                    Vector2.zero, 0, .65f, "Explosion", "FireBlast", 2f);
                RainLevelGame.Instance?.EnemyDefeated(transform.position);
            }
        }
    }
}
