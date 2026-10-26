using System;
using System.Collections.Generic;
using MegaManX9.Animation;
using MegaManX9.UI;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Shared, existing art references for the playable rain level.</summary>
    [CreateAssetMenu(menuName = "Mega Man X9/Rain Level Assets")]
    public sealed class RainLevelAssets : ScriptableObject
    {
        public SpriteAnimationLibrary animationLibrary;
        public Material spriteMaterial;
        public HealthHUDAssets healthHUD;
        public GameObject dragonRigPrefab;

        private readonly Dictionary<string, SpriteAnimationLibrary.Entry> entries =
            new Dictionary<string, SpriteAnimationLibrary.Entry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<SpriteAnimationLibrary.Entry>> banks =
            new Dictionary<string, List<SpriteAnimationLibrary.Entry>>(StringComparer.OrdinalIgnoreCase);
        private SpriteAnimationLibrary indexedLibrary;
        private int indexedCount = -1;

        private void OnEnable() => Invalidate();
        private void OnValidate() => Invalidate();
        public void Invalidate() { indexedLibrary = null; indexedCount = -1; entries.Clear(); banks.Clear(); }

        private void EnsureIndex()
        {
            int count = animationLibrary == null ? 0 : animationLibrary.entries.Count;
            if (indexedLibrary == animationLibrary && indexedCount == count) return;
            entries.Clear(); banks.Clear(); indexedLibrary = animationLibrary; indexedCount = count;
            if (animationLibrary == null) return;
            foreach (var entry in animationLibrary.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.unit) || string.IsNullOrEmpty(entry.animationName) ||
                    entry.clip == null || entry.controller == null || entry.firstFrame == null) continue;
                entries[Key(entry.unit, entry.animationName)] = entry;
                if (!banks.TryGetValue(entry.unit, out var bank)) banks[entry.unit] = bank = new List<SpriteAnimationLibrary.Entry>();
                bank.Add(entry);
            }
        }

        private static string Key(string unit, string clip) => unit + "/" + clip;

        /// <summary>Exact lookup; never substitutes a different pose or character.</summary>
        public bool TryGet(string unit, string clip, out SpriteAnimationLibrary.Entry entry)
        {
            EnsureIndex();
            if (string.IsNullOrEmpty(unit) || string.IsNullOrEmpty(clip)) { entry = null; return false; }
            return entries.TryGetValue(Key(unit, clip), out entry);
        }

        /// <summary>Finds an exact clip, a documented role alias, Idle, then the first pose in that unit.</summary>
        public SpriteAnimationLibrary.Entry Find(string unit, string clip)
        {
            if (TryGet(unit, clip, out var entry)) return entry;
            string alias = HeroAlias(unit, clip);
            if (alias != null && TryGet(unit, alias, out entry)) return entry;
            if (TryGet(unit, "Idle", out entry)) return entry;
            return banks.TryGetValue(unit ?? "", out var bank) && bank.Count > 0 ? bank[0] : null;
        }

        public IReadOnlyList<SpriteAnimationLibrary.Entry> UnitAnimations(string unit)
        {
            EnsureIndex();
            return banks.TryGetValue(unit ?? "", out var bank) ? bank : (IReadOnlyList<SpriteAnimationLibrary.Entry>)Array.Empty<SpriteAnimationLibrary.Entry>();
        }

        /// <summary>Rebinds only when changing controller banks; repeated locomotion requests keep their time.</summary>
        public bool PlayOn(Animator animator, SpriteRenderer renderer, string unit, string clip, bool restart = false)
        {
            if (animator == null || renderer == null) return false;
            var entry = Find(unit, clip); if (entry == null) return false;
            bool changedBank = animator.runtimeAnimatorController != entry.controller;
            if (changedBank)
            {
                animator.runtimeAnimatorController = entry.controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
            }
            if (spriteMaterial != null && renderer.sharedMaterial != spriteMaterial) renderer.sharedMaterial = spriteMaterial;
            int hash = Animator.StringToHash(entry.animationName);
            if (!animator.HasState(0, hash)) return false;
            if (!restart && !changedBank && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash) return true;
            renderer.sprite = entry.firstFrame;
            animator.Play(hash, 0, 0);
            animator.Update(0);
            return true;
        }

        private static string HeroAlias(string unit, string requested)
        {
            bool blue = string.Equals(unit, "Blue", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(unit, "BlueUltimate", StringComparison.OrdinalIgnoreCase);
            if (blue)
            {
                switch (requested)
                {
                    case "Hurt": case "Hit": case "Damage": return "HitElectrical";
                    case "Death": case "Defeat": return "AirHit";
                    case "Crouch": return "CrouchIdle";
                    case "Fall": case "JumpRise": case "JumpStart": return "Jump";
                    case "Land": return "AirRecover";
                    case "Walk": return "Run";
                    case "WallShoot": return "WallJumpShoot";
                    case "AirShoot": return "JumpShoot";
                }
                if (unit == "BlueUltimate")
                {
                    if (requested == "AirDash") return "Dash";
                    if (requested == "NovaStrikeTravel") return "NovaStrikeBurst";
                }
            }
            if (string.Equals(unit, "Red", StringComparison.OrdinalIgnoreCase))
            {
                switch (requested)
                {
                    case "Shoot": return "GroundSlash1";
                    case "ChargeShot": return "GroundEnergyStrike";
                    case "JumpShoot": case "AirShoot": return "AirSlash1";
                    case "RunShoot": case "DashShoot": case "DashAttack": return "DashSlash";
                    case "WallCling": return "WallClimb";
                    case "WallJump": return "WallPushOff";
                    case "WallShoot": case "WallJumpShoot": return "WallSlash";
                    case "AirDash": return "Dash";
                    case "HitElectrical": return "Electrocuted";
                    case "Death": case "Defeat": return "ProneCollapse";
                    case "TeleportArrival": return "TeleportIn";
                    case "TeleportDeparture": return "TeleportOut";
                    case "Walk": return "Run";
                }
            }
            return null;
        }
    }
}
