using System.Collections.Generic;
using MegaManX9.Animation;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Swept projectiles shared by the hero and ordinary enemies.</summary>
    public sealed class RainProjectile : MonoBehaviour
    {
        private static readonly HashSet<RainProjectile> live = new HashSet<RainProjectile>();
        private DamageTeam team;
        private GameObject owner;
        private Vector2 velocity;
        private int damage;
        private float remaining, radius;
        private bool retired;
        private Animator animator;
        private SpriteRenderer renderer;
        public DamageTeam Team => team;
        public GameObject Owner => owner;
        public Vector2 Velocity => velocity;
        public int DamageAmount=>damage;
        public string AnimationUnit {get;private set;}
        public string AnimationName {get;private set;}
        public bool SpriteFlipped=>renderer!=null&&renderer.flipX;
        public Color Tint { get => renderer != null ? renderer.color : Color.white; set { if (renderer != null) renderer.color = value; } }

        public static RainProjectile Spawn(RainLevelAssets assets, DamageTeam team, GameObject owner,
            Vector2 position, Vector2 velocity, int damage, float lifetime = 2f,
            string unit = "GigaDeathProjectiles", string clip = "SmallShot", float scale = 1.6f, float radius = .12f)
        {
            if (assets == null) return null;
            if (!assets.TryGet(unit, clip, out SpriteAnimationLibrary.Entry entry) &&
                !assets.TryGet("GigaDeathProjectiles", "SmallShot", out entry)) return null;
            var obj = new GameObject(team == DamageTeam.Player ? "Hero projectile" : "Enemy projectile");
            obj.layer = team == DamageTeam.Player ? 9 : 10;
            obj.transform.position = position; obj.transform.localScale = Vector3.one * Mathf.Max(.05f, scale);
            var shot = obj.AddComponent<RainProjectile>();
            shot.team = team; shot.owner = owner; shot.velocity = velocity; shot.damage = Mathf.Max(0, damage);
            shot.AnimationUnit=entry.unit;shot.AnimationName=entry.animationName;
            shot.remaining = Mathf.Max(.05f, lifetime); shot.radius = Mathf.Max(.025f, radius);
            shot.renderer = obj.AddComponent<SpriteRenderer>(); shot.renderer.sortingOrder = 25;
            shot.animator = obj.AddComponent<Animator>();
            assets.PlayOn(shot.animator, shot.renderer, entry.unit, entry.animationName, true);
            if (team == DamageTeam.Player) shot.renderer.color = new Color(.45f, .95f, 1f, 1f);
            // This original shot is painted toward the left; mirror its nose into its flight direction.
            bool paintedLeft=entry.unit=="GigaDeathProjectiles"&&entry.animationName=="SmallShot";
            shot.renderer.flipX = (velocity.x < 0)^paintedLeft;
            live.Add(shot); return shot;
        }

        public static void ClearTeam(DamageTeam team)
        {
            var copy = new List<RainProjectile>(live);
            foreach (var shot in copy) if (shot != null && shot.team == team) shot.DestroyProjectile();
        }

        private void Update()
        {
            if (retired) return;
            var game = RainLevelGame.Instance;
            bool frozen = game != null && game.InputLocked;
            if (animator != null) animator.speed = frozen ? 0 : 1;
            if (frozen) return;
            float delta = Time.deltaTime;
            remaining -= delta;
            if (remaining <= 0) { DestroyProjectile(); return; }
            Vector2 start = transform.position, displacement = velocity * delta;
            if (damage > 0)
            {
                float distance = displacement.magnitude;
                var hits = Physics2D.CircleCastAll(start, radius,
                    distance > .00001f ? displacement / distance : Vector2.right, distance);
                foreach (var hit in hits)
                {
                    if (hit.collider == null || hit.collider.transform == transform ||
                        owner != null && (hit.collider.transform == owner.transform || hit.collider.transform.IsChildOf(owner.transform))) continue;
                    var target = RainCombat.Receiver(hit.collider);
                    if (target != null)
                    {
                        if (target.Team == team || !target.IsAlive) continue;
                        target.TakeHit(damage, velocity.sqrMagnitude > .001f ? velocity.normalized : Vector2.up, owner);
                        transform.position = hit.centroid; DestroyProjectile(); return;
                    }
                    if (hit.collider.gameObject.layer == 8) { transform.position = hit.centroid; DestroyProjectile(); return; }
                }
            }
            transform.position = start + displacement;
        }

        public void DestroyProjectile()
        {
            if (retired) return;
            retired = true; live.Remove(this);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
        private void OnDestroy() { live.Remove(this); }
    }
}
