using System.Collections.Generic;
using MegaManX9.Animation;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Original sprite clips sampled on the same scaled clock as the story. No physics or damage.</summary>
    public sealed class RainBossCinematic : MonoBehaviour
    {
        public sealed class SpriteActor
        {
            public GameObject Object { get; internal set; }
            public SpriteRenderer Renderer { get; internal set; }
            internal Animator Animator;
            internal SpriteAnimationLibrary.Entry Entry;
            internal float Age;
            internal bool Loop;
            public Transform Transform => Object != null ? Object.transform : null;
        }

        private RainLevelAssets assets;
        private readonly List<SpriteActor> actors = new List<SpriteActor>();
        public void Configure(RainLevelAssets source) => assets = source;

        public SpriteActor Create(string unit, string clip, Vector2 position, float scale = 2)
        {
            if (assets == null || !assets.TryGet(unit, clip, out var entry)) return null;
            var obj = new GameObject("Story original sprite · " + unit);
            obj.transform.SetParent(transform, false); obj.transform.position = position;
            obj.transform.localScale = Vector3.one * scale;
            var art = obj.AddComponent<SpriteRenderer>();
            art.sharedMaterial = assets.spriteMaterial; art.sortingOrder = 38; art.sprite = entry.firstFrame;
            // Unity's native sprite sampler needs an initialized Animator even while manually sampled.
            var animator = obj.AddComponent<Animator>();animator.runtimeAnimatorController = entry.controller;
            animator.applyRootMotion = false;animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();animator.Update(0);animator.enabled = false;
            var actor = new SpriteActor { Object = obj, Renderer = art, Animator = animator, Entry = entry, Loop = entry.clip.isLooping };
            actors.Add(actor); Sample(actor); return actor;
        }

        public bool Play(SpriteActor actor, string unit, string clip, bool loop = false)
        {
            if (actor == null || actor.Object == null || !assets.TryGet(unit, clip, out var entry)) return false;
            if (actor.Animator.runtimeAnimatorController != entry.controller)
            {
                actor.Animator.enabled = true;actor.Animator.runtimeAnimatorController = entry.controller;
                actor.Animator.Rebind();actor.Animator.Update(0);actor.Animator.enabled = false;
            }
            actor.Entry = entry; actor.Age = 0; actor.Loop = loop; Sample(actor); return true;
        }

        public void Advance(float delta)
        {
            foreach (var actor in actors)
            {
                if (actor.Object == null) continue;
                actor.Age += Mathf.Max(0, delta); Sample(actor);
            }
        }

        private static void Sample(SpriteActor actor)
        {
            if (actor.Entry?.clip == null || actor.Object == null) return;
            float length = Mathf.Max(.01f, actor.Entry.clip.length);
            float time = actor.Loop ? Mathf.Repeat(actor.Age, length) : Mathf.Min(actor.Age, Mathf.Max(0, length - .001f));
            actor.Entry.clip.SampleAnimation(actor.Object, time);
        }

        public void Remove(SpriteActor actor)
        {
            if (actor == null) return; actors.Remove(actor);
            if (actor.Object != null) Destroy(actor.Object);
        }
        public void Clear()
        {
            foreach (var actor in actors) if (actor.Object != null) Destroy(actor.Object);
            actors.Clear();
        }
        private void OnDestroy() => Clear();
    }
}
