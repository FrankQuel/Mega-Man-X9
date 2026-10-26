from pathlib import Path
p=Path('Assets/Recursos/Editor/AnimationPipeline/DragonRigBuilder.cs');s=p.read_text()
start=s.index('                // The muzzle travels with the head.')
end=s.index('                library.controller = controller;',start)
s=s[:start]+'''                // Emitters create independent moving projectiles at runtime. No painted
                // flame is left parented to the mouth as a substitute for an actual attack.
                NewChild(bones["NearFoot"], "FootSocket", new Vector3(.12f, -.08f, 0));
                NewChild(root.transform, "BossAttackEffects", Vector3.zero);
                Sprite[] fireFrames = LoadEffectFrames("FireballRight");

'''+s[end:]
needle='                actor.Configure(library, visual, mouth, clawSocket);'
s=s.replace(needle,needle+'''
                var attacks = root.AddComponent<DragonBossAttackPreview>();
                attacks.Configure(actor, GetEffectMaterial(), material, fireFrames,
                    LoadEffectFrames("FireballDown"), LoadEffectFrames("SmallFlameUp"),
                    LoadEffectFrames("GroundFlameTall"), LoadEffectFrames("ShockRing"),
                    LoadEffectFrames("SlashArc"), LoadEffectFrames("EnergyOrbs"));''')
s=s.replace('                library.Find("Idle").clip.SampleAnimation(root, 0);','''                DragonBossAttackValidation.Validate(root);
                attacks.ClearEffects();
                library.Find("Idle").clip.SampleAnimation(root, 0);''')
start=s.index('            string mouthPath = AnimationUtility.CalculateTransformPath(')
end=s.index('            var settings = AnimationUtility.GetAnimationClipSettings(clip);',start)
s=s[:start]+'''            // Moving mouth/hand/foot emissions belong to DragonBossAttackPreview's
            // deterministic timeline, not enabled curves on fixed flame renderers.
'''+s[end:]
# The same existing hand artwork opens for the forward palm launch and uppercut.
s=s.replace('bool strike = source.name == "ClawAttack";','bool strike = source.name == "ClawAttack" || source.name == "Fireball" || source.name == "RisingFlame";')
start=s.index('        private static Sprite[] LoadFireFrames()')
end=s.index('        private static string Hash(',start)
s=s[:start]+'''        private static Sprite[] LoadEffectFrames(string name)
        {
            string path = "Assets/Recursos/Sprites/Effects/DragonEffects/Generated/" + name + ".png";
            var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
            if (frames.Length == 0) throw new InvalidDataException("Missing original Dragon effect sequence: " + name);
            return frames;
        }
        private static Material GetMaterial()
        {
            const string source = "Tools/AnimationPipeline/dragon_boss_flash.shader";
            string shaderPath = AnimationFolder + "/DragonBossFlash.shader";
            byte[] data = File.ReadAllBytes(source);
            if (!File.Exists(shaderPath) || Hash(File.ReadAllBytes(shaderPath)) != Hash(data)) File.WriteAllBytes(shaderPath, data);
            AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceSynchronousImport);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Dragon hit-flash shader did not compile.");
            string path = AnimationFolder + "/DragonRigSprites.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; material.SetFloat("_FlashAmount", 0); material.SetColor("_FlashColor", Color.white);
            EditorUtility.SetDirty(material); return material;
        }
        private static Material GetEffectMaterial()
        {
            string path = AnimationFolder + "/DragonBossEffects.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("No unlit effect shader available.");
            material = new Material(shader); AssetDatabase.CreateAsset(material, path); return material;
        }
'''+s[end:]
p.write_text(s)
