using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>Opt-in damage receiver; the animation preview never invents player health.</summary>
    public interface IDragonBossDamageable
    {
        void ReceiveDragonDamage(int amount, Vector2 worldDirection, GameObject attacker);
    }

    /// <summary>A deterministic moving attack. Its owner advances it, so pause and seek include VFX.</summary>
    public sealed class DragonBossProjectile : MonoBehaviour
    {
        private SpriteRenderer art;
        private Sprite[] frames;
        private GameObject owner;
        private Vector3 velocity;
        private float gravity, life, age, rate, radius, visualScale;
        private bool damageEnabled, expireOnHit, groundAnchored;
        private int damage;
        private LayerMask targetMask;
        private Transform follow;
        private Vector3 followOffset;
        private bool stopAtWorldWalls;
        private float levelMinX, levelMaxX;
        private LayerMask terrainMask;
        private readonly HashSet<MonoBehaviour> hitTargets = new HashSet<MonoBehaviour>();

        public string Kind { get; private set; }
        public bool Alive => age < life;
        public float Age => age;
        public Vector3 LocalVelocity => velocity;
        public Vector3 SpawnLocalPosition { get; private set; }
        public float Lifetime => life;
        public bool StopsAtWorldWalls => stopAtWorldWalls;

        public void ConfigureWorldTraversal(float minimumX, float maximumX, LayerMask terrain)
        {
            stopAtWorldWalls = true; levelMinX = minimumX; levelMaxX = maximumX; terrainMask = terrain;
        }

        public void Initialize(GameObject attacker, Transform effectsRoot, string kind, Sprite[] sprites,
            Material material, Vector3 position, Vector3 initialVelocity, float lifetime,
            float scale, bool flip, float rotation, bool canDamage, int hitDamage,
            LayerMask mask, float collisionRadius = .10f, bool onGround = false,
            Transform followSocket = null, float accelerationY = 0, bool destroyOnHit = false)
        {
            owner = attacker; Kind = kind; frames = sprites; velocity = initialVelocity;
            life = Mathf.Max(.03f, lifetime); visualScale = scale; rate = 16;
            damageEnabled = canDamage; damage = hitDamage; targetMask = mask;
            radius = collisionRadius; gravity = accelerationY; groundAnchored = onGround;
            expireOnHit = destroyOnHit; follow = followSocket;
            transform.SetParent(effectsRoot, false); transform.localPosition = position;
            SpawnLocalPosition = position;
            if (follow != null) followOffset = position - effectsRoot.InverseTransformPoint(follow.position);
            gameObject.layer = effectsRoot.gameObject.layer;
            var child = new GameObject("Paint").transform; child.gameObject.layer = gameObject.layer; child.SetParent(transform, false);
            art = child.gameObject.AddComponent<SpriteRenderer>(); art.sharedMaterial = material;
            art.sortingOrder = 20; art.flipX = flip;
            child.localScale = Vector3.one * visualScale; child.localRotation = Quaternion.Euler(0, 0, rotation);
            SetFrame();
        }

        public void Advance(float delta)
        {
            if (!Alive || delta <= 0) return;
            Vector3 previousWorld = transform.position;
            age += delta;
            if (follow == null)
            {
                transform.localPosition += velocity * delta + Vector3.up * (.5f * gravity * delta * delta);
                velocity.y += gravity * delta;
            }
            RefreshFollow();
            if (stopAtWorldWalls && HitWorldLimit(previousWorld, transform.position)) age = life;
            SetFrame();
            if (damageEnabled && Alive) CheckDamage();
        }

        private bool HitWorldLimit(Vector2 previous, Vector2 next)
        {
            if (next.x < levelMinX - 2f || next.x > levelMaxX + 2f) return true;
            Vector2 travel = next - previous;
            if (travel.sqrMagnitude < .000001f) return false;
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            float castRadius = Mathf.Clamp(radius * scale * .65f, .04f, .22f);
            Vector2 lift = Vector2.up * (castRadius + .10f);
            Vector2 origin = previous + lift;
            foreach (var hit in Physics2D.CircleCastAll(origin, castRadius, travel.normalized, travel.magnitude, terrainMask))
            {
                if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(owner.transform)) continue;
                // A column's painted base sits on the floor. Start the cast above it,
                // then reject upward/downward contacts and low floor-overlap results.
                if (hit.collider.bounds.max.y <= origin.y - castRadius + .08f) continue;
                if (Mathf.Abs(hit.normal.x) < .65f) continue;
                transform.position = hit.centroid - lift; return true;
            }
            return false;
        }

        public void RefreshFollow()
        {
            if (follow != null) transform.localPosition = transform.parent.InverseTransformPoint(follow.position) + followOffset;
        }

        private void SetFrame()
        {
            if (frames == null || frames.Length == 0 || art == null) return;
            int tick = Mathf.FloorToInt(age * rate);
            // The first painted fireball cells are its birth/growth, not a loop that
            // should repeatedly shrink a travelling Hadouken back into a spark.
            int growth = Mathf.Min(3, frames.Length - 1);
            int index = Kind == "HandFireball" && tick >= growth
                ? growth + (tick - growth) % (frames.Length - growth)
                : tick % frames.Length;
            art.sprite = frames[index];
            // Strip sprites have a bottom pivot; a flying projectile is centered on its hitbox.
            art.transform.localPosition = groundAnchored ? Vector3.zero : -(art.transform.localRotation * art.sprite.bounds.center) * visualScale;
            float fade = Mathf.Clamp01((life - age) / Mathf.Min(.14f, life));
            art.color = new Color(1, 1, 1, fade);
        }

        private void CheckDamage()
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, radius * scale, targetMask))
            {
                if (collider.transform == owner.transform || collider.transform.IsChildOf(owner.transform)) continue;
                foreach (var behaviour in collider.GetComponentsInParent<MonoBehaviour>())
                {
                    if (!(behaviour is IDragonBossDamageable receiver) || !hitTargets.Add(behaviour)) continue;
                    Vector3 world = transform.parent.TransformVector(velocity);
                    receiver.ReceiveDragonDamage(damage, new Vector2(world.x, world.y).normalized, owner);
                    if (expireOnHit) age = life;
                }
            }
        }
    }
}
