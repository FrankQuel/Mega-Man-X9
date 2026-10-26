using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MegaManX9.Animation;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    /// <summary>Executes the enemy timelines in the Editor; no external code or scene is run.</summary>
    public static class DragonBossAttackValidation
    {
        [Serializable] private sealed class AttackResult
        {
            public string attack;
            public int emitted, activeAtMiddle;
            public Vector3 lastWorldOrigin, lastWorldVelocity;
            public bool pauseStopsTimeline, projectileLifetimeClean;
        }
        [Serializable] private sealed class Report
        {
            public AttackResult[] attacks;
            public bool projectileMoved, mirroredProjectileReversed, manualAdvanceNotDoubleScaled, flashShaderActive;
            public string[] errors;
        }

        public static void Validate(GameObject rig)
        {
            var driver = rig.GetComponent<DragonBossAttackPreview>();
            if (driver == null) throw new InvalidOperationException("Dragon enemy attack driver is missing.");
            var errors = new List<string>(); var results = new List<AttackResult>();
            driver.AutoAdvance = false; driver.DamageEnabled = false; driver.SetPaused(false);
            bool projectileMoved = false;
            foreach (string name in DragonBossAttackPreview.SupportedAttacks)
            {
                if (!driver.BeginAttack(name)) { errors.Add("Missing attack " + name); continue; }
                driver.Advance(driver.Duration * .60f);
                var result = new AttackResult { attack = name, emitted = driver.EmittedCount,
                    activeAtMiddle = driver.ActiveProjectileCount, lastWorldOrigin = driver.LastEmissionWorldPosition,
                    lastWorldVelocity = driver.LastEmissionWorldVelocity };
                bool needsEmission = name != "HurtFlash" && name != "Crouch";
                if (needsEmission && driver.EmittedCount == 0) errors.Add("Attack emitted nothing: " + name);
                if ((name == "FireBreath" || name == "Fireball" || name == "GroundFireWave") &&
                    (driver.LastEmissionWorldVelocity.x > .001f || Mathf.Abs(driver.LastEmissionWorldVelocity.y) > .001f))
                    errors.Add("Horizontal attack direction is wrong: " + name);
                float before = driver.Elapsed;
                driver.SetPaused(true); driver.Advance(.1f);
                result.pauseStopsTimeline = Mathf.Approximately(before, driver.Elapsed);
                if (!result.pauseStopsTimeline) errors.Add("Pause did not stop " + name);
                driver.SetPaused(false);
                if (name == "Fireball")
                {
                    var projectile = driver.ActiveProjectiles.FirstOrDefault(p => p.Kind == "HandFireball");
                    if (projectile != null)
                    {
                        Vector3 position = projectile.transform.localPosition;
                        driver.Advance(.05f);
                        projectileMoved = projectile.transform.localPosition.x < position.x - .05f;
                    }
                }
                driver.Advance(driver.Duration + 3f);
                result.projectileLifetimeClean = driver.ActiveProjectileCount == 0;
                if (!result.projectileLifetimeClean) errors.Add("Expired projectile survived: " + name);
                results.Add(result);
            }

            driver.Speed = 2;
            driver.BeginAttack("Fireball"); driver.Advance(.1f);
            bool noDoubleSpeed = Mathf.Abs(driver.Elapsed - .1f) < .001f;
            driver.Speed = 1;
            Vector3 originalScale = rig.transform.localScale;
            rig.transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            driver.BeginAttack("Fireball"); driver.Advance(driver.Duration * .60f);
            bool mirrored = driver.LastEmissionWorldVelocity.x > 0;
            rig.transform.localScale = originalScale;
            driver.BeginAttack("HurtFlash");
            var body = rig.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.transform.name == "Art");
            var block = new MaterialPropertyBlock();
            if (body != null) body.GetPropertyBlock(block);
            bool flash = body != null && body.sharedMaterial != null && body.sharedMaterial.HasProperty("_FlashAmount") && block.GetFloat("_FlashAmount") > .9f;
            if (!projectileMoved) errors.Add("Hadouken did not travel away from the hand.");
            if (!mirrored) errors.Add("Mirroring did not reverse projectile world direction.");
            if (!noDoubleSpeed) errors.Add("Manual stage time was multiplied twice.");
            if (!flash) errors.Add("HurtFlash did not activate the white flash shader.");
            driver.ClearEffects(); driver.BeginAttack("Idle"); driver.AutoAdvance = false;
            var report = new Report { attacks = results.ToArray(), projectileMoved = projectileMoved,
                mirroredProjectileReversed = mirrored, manualAdvanceNotDoubleScaled = noDoubleSpeed,
                flashShaderActive = flash, errors = errors.ToArray() };
            File.WriteAllText("Tools/AnimationPipeline/dragon_boss_attack_validation_report.json", JsonUtility.ToJson(report, true));
            if (errors.Count > 0) throw new InvalidOperationException("Dragon enemy validation failed: " + string.Join("; ", errors));
        }
    }
}
