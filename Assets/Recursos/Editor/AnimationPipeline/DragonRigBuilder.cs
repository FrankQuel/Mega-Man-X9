using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MegaManX9.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace MegaManX9.EditorTools
{
    /// <summary>Builds the articulated rig without opening, closing, or saving user scenes.</summary>
    public static class DragonRigBuilder
    {
        private const string DefinitionPath = "Tools/AnimationPipeline/dragon_rig.json";
        private const string SpriteFolder = "Assets/Recursos/Sprites/Bosses/DragonRig/Generated";
        private const string AnimationFolder = "Assets/Recursos/Animations/Bosses/DragonRig";
        public const string PrefabPath = "Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab";
        public const string LibraryPath = AnimationFolder + "/DragonRigLibrary.asset";

        // These authoring DTO fields are populated by JsonUtility, not assignments in C#.
#pragma warning disable 0649
        [Serializable] private sealed class Definition
        {
            public float pixelsPerUnit;
            public string source, sourceSha256;
            public Part[] parts;
            public Clip[] clips;
            public AuthoredPoseData[] authoredPoses;
            public string mouthParent, clawParent, openClawArtPath, openMouthArtPath;
            public float[] mouthLocalPosition, clawLocalPosition, openClawArtLocalPosition, openMouthArtLocalPosition, breathMouthLocalPosition;
        }
        [Serializable] private sealed class Part
        {
            public string name, parent, artPath;
            public float[] localPosition, artLocalPosition;
            public int sortingOrder;
        }
        [Serializable] private sealed class Clip
        {
            public string name;
            public float length;
            public bool loop;
            public Track[] tracks;
        }
        [Serializable] private sealed class Track
        {
            public string path;
            public float[] times, x, y, angle;
        }
        [Serializable] private sealed class AuthoredPoseData
        {
            public string name, artPath;
            public float[] localOffset, mouth, hand, foot;
        }
#pragma warning restore 0649
        [Serializable] private sealed class Report
        {
            public string prefab, library, sourceSha256;
            public int bones, clips, scalarTransformCurves, sampledPoses;
            public bool sourceUnchanged, separateLimbTransforms, noBodyScaleCurves;
            public string[] clipNames;
            public string[] errors;
        }

        [MenuItem("Mega Man X9/Animations/Rebuild Articulated Dragon Rig")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building DragonRig.");
            if (!File.Exists(DefinitionPath)) throw new FileNotFoundException("Run dragon_rig_author.py and dragon_add_claw.py first.", DefinitionPath);
            var data = JsonUtility.FromJson<Definition>(File.ReadAllText(DefinitionPath));
            if (data?.parts == null || data.clips == null || data.parts.Length < 10 || data.clips.Length < 9)
                throw new InvalidDataException("Articulated Dragon definition is incomplete.");
            if (!File.Exists(data.source) || Hash(File.ReadAllBytes(data.source)) != data.sourceSha256)
                throw new InvalidDataException("Original Dragon source differs from the audited artwork.");

            EnsureFolder(SpriteFolder);
            EnsureFolder(AnimationFolder);
            EnsureFolder("Assets/Recursos/Prefabs/Bosses/DragonRig");
            var sprites = new Dictionary<string, Sprite>();
            foreach (Part part in data.parts) sprites[part.name] = ImportSprite(part.artPath, part.name, data.pixelsPerUnit);
            Sprite claw = ImportSprite(data.openClawArtPath, "NearOpenClaw", data.pixelsPerUnit);
            Sprite openMouthSprite = ImportSprite(data.openMouthArtPath, "OpenMouthHead", data.pixelsPerUnit);
            Material material = GetMaterial();
            var controller = BuildController(data.clips);
            var library = AssetDatabase.LoadAssetAtPath<DragonRigLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<DragonRigLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.authoredPoses = (data.authoredPoses ?? Array.Empty<AuthoredPoseData>()).Select(p => new DragonRigLibrary.AuthoredPose
            {
                name = p.name, sprite = ImportSprite(p.artPath, "Pose_" + p.name, data.pixelsPerUnit),
                localOffset = Vector(p.localOffset), mouth = Vector(p.mouth), hand = Vector(p.hand), foot = Vector(p.foot)
            }).ToArray();
            library.bindHipPosition = Vector(data.parts.First(p => p.name == "Hips").localPosition);

            var root = new GameObject("DragonRig_Animated");
            try
            {
                root.AddComponent<SortingGroup>();
                Transform visual = NewChild(root.transform, "Visual", Vector3.zero);
                var bones = new Dictionary<string, Transform> { { "Visual", visual } };
                foreach (Part part in data.parts)
                {
                    if (!bones.TryGetValue(part.parent, out Transform parent)) throw new InvalidDataException("Missing parent for " + part.name);
                    Transform bone = NewChild(parent, part.name, Vector(part.localPosition));
                    bones.Add(part.name, bone);
                    var art = NewChild(bone, "Art", Vector(part.artLocalPosition)).gameObject.AddComponent<SpriteRenderer>();
                    art.sprite = sprites[part.name]; art.sortingOrder = part.sortingOrder; art.sharedMaterial = material;
                }
                Transform mouth = NewChild(bones[data.mouthParent], "MouthSocket", Vector(data.mouthLocalPosition));
                Transform clawSocket = NewChild(bones[data.clawParent], "ClawSocket", Vector(data.clawLocalPosition));
                var openClaw = NewChild(bones["NearForearm"], "OpenClawArt", Vector(data.openClawArtLocalPosition)).gameObject.AddComponent<SpriteRenderer>();
                openClaw.sprite = claw; openClaw.sortingOrder = 8; openClaw.sharedMaterial = material; openClaw.enabled = false;
                var openMouth = NewChild(bones["Head"], "OpenMouthArt", Vector(data.openMouthArtLocalPosition)).gameObject.AddComponent<SpriteRenderer>();
                openMouth.sprite = openMouthSprite; openMouth.sortingOrder = 5; openMouth.sharedMaterial = material; openMouth.enabled = false;
                var authored = NewChild(visual, "AuthoredPoseArt", Vector3.zero).gameObject.AddComponent<SpriteRenderer>();
                authored.sharedMaterial = material; authored.sortingOrder = 12; authored.enabled = false;
                NewChild(visual, "MouthPoseSocket", Vector3.zero); NewChild(visual, "HandPoseSocket", Vector3.zero); NewChild(visual, "FootPoseSocket", Vector3.zero);

                // Emitters create independent moving projectiles at runtime. No painted
                // flame is left parented to the mouth as a substitute for an actual attack.
                NewChild(bones["NearFoot"], "FootSocket", new Vector3(.12f, -.08f, 0));
                NewChild(root.transform, "BossAttackEffects", Vector3.zero);
                Sprite[] fireFrames = LoadEffectFrames("FireballRight");

                library.controller = controller;
                var entries = new List<DragonRigLibrary.Entry>();
                foreach (Clip source in data.clips)
                {
                    AnimationClip clip = BuildClip(source, bones, mouth, data);
                    var stateMachine = controller.layers[0].stateMachine;
                    var state = stateMachine.states.FirstOrDefault(s => s.state.name == source.name).state;
                    if (state == null) state = stateMachine.AddState(source.name, new Vector3((entries.Count % 5) * 230, (entries.Count / 5) * 80));
                    state.motion = clip; state.writeDefaultValues = false;
                    if (source.name == "Idle") stateMachine.defaultState = state;
                    entries.Add(new DragonRigLibrary.Entry { name = source.name, clip = clip, duration = source.length, loop = source.loop, holdLastPose = source.name == "Death" || source.name == "Crouch" });
                }
                library.clips = entries.ToArray();
                EditorUtility.SetDirty(library); EditorUtility.SetDirty(controller);
                var animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var actor = root.AddComponent<DragonRigActor>();
                actor.Configure(library, visual, mouth, clawSocket);
                var attacks = root.AddComponent<DragonBossAttackPreview>();
                attacks.Configure(actor, GetEffectMaterial(), material, fireFrames,
                    LoadEffectFrames("FireballDown").Skip(5).ToArray(), LoadEffectFrames("SmallFlameUp").Skip(3).ToArray(),
                    LoadEffectFrames("GroundFlameTall"), LoadEffectFrames("ShockRing"),
                    LoadEffectFrames("SlashArc").Skip(3).Take(5).ToArray(), LoadEffectFrames("EnergyOrbs"));
                var collider = root.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.61f, .81f); collider.offset = new Vector2(0, .405f);

                Report report = Validate(root, data, library);
                string reportPath = "Tools/AnimationPipeline/dragon_rig_validation_report.json";
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
                if (report.errors.Length > 0) throw new InvalidDataException("DragonRig validation failed: " + string.Join("; ", report.errors));
                DragonBossAttackValidation.Validate(root);
                attacks.ClearEffects();
                library.Find("Idle").clip.SampleAnimation(root, 0);
                animator.enabled = true;
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null) throw new IOException("Could not save articulated Dragon prefab.");
                AssetDatabase.SaveAssets();
                Debug.Log("DragonRig built: 13 separate body segments, " + entries.Count + " articulated clips. " + PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static AnimationClip BuildClip(Clip source, Dictionary<string, Transform> bones, Transform mouth, Definition definition)
        {
            string path = AnimationFolder + "/" + source.name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)) AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)) AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            clip.name = source.name; clip.frameRate = 30;
            foreach (Track track in source.tracks)
            {
                if (track.times.Length < 2 || track.times.Length != track.x.Length || track.times.Length != track.y.Length || track.times.Length != track.angle.Length)
                    throw new InvalidDataException("Mismatched keyframes in " + source.name + "/" + track.path);
                Curve(clip, track.path, typeof(Transform), "m_LocalPosition.x", track.times, track.x);
                Curve(clip, track.path, typeof(Transform), "m_LocalPosition.y", track.times, track.y);
                Curve(clip, track.path, typeof(Transform), "localEulerAnglesRaw.z", track.times, track.angle);
            }
            string forearm = AnimationUtility.CalculateTransformPath(bones["NearForearm"], bones["Visual"].parent);
            bool strike = source.name == "ClawAttack" || source.name == "Fireball" || source.name == "RisingFlame";
            float[] clawTimes = strike ? new[] { 0f, source.length * .24f, source.length * .82f, source.length } : new[] { 0f, source.length };
            Curve(clip, forearm + "/Art", typeof(SpriteRenderer), "m_Enabled", clawTimes, strike ? new[] { 1f, 0f, 1f, 1f } : new[] { 1f, 1f }, true);
            Curve(clip, forearm + "/OpenClawArt", typeof(SpriteRenderer), "m_Enabled", clawTimes, strike ? new[] { 0f, 1f, 0f, 0f } : new[] { 0f, 0f }, true);
            string headPath = AnimationUtility.CalculateTransformPath(bones["Head"], bones["Visual"].parent);
            bool breathing = source.name == "FireBreath";
            float[] breathTimes = breathing ? new[] { 0f, source.length * .26f, source.length * .84f, source.length } : new[] { 0f, source.length };
            Curve(clip, headPath + "/Art", typeof(SpriteRenderer), "m_Enabled", breathTimes, breathing ? new[] { 1f, 0f, 1f, 1f } : new[] { 1f, 1f }, true);
            Curve(clip, headPath + "/OpenMouthArt", typeof(SpriteRenderer), "m_Enabled", breathTimes, breathing ? new[] { 0f, 1f, 0f, 0f } : new[] { 0f, 0f }, true);
            string mouthPath = AnimationUtility.CalculateTransformPath(mouth, bones["Visual"].parent);
            Vector3 closed = Vector(definition.mouthLocalPosition), open = Vector(definition.breathMouthLocalPosition);
            Curve(clip, mouthPath, typeof(Transform), "m_LocalPosition.x", breathTimes, breathing ? new[] { closed.x, open.x, closed.x, closed.x } : new[] { closed.x, closed.x }, true);
            Curve(clip, mouthPath, typeof(Transform), "m_LocalPosition.y", breathTimes, breathing ? new[] { closed.y, open.y, closed.y, closed.y } : new[] { closed.y, closed.y }, true);
            // Moving mouth/hand/foot emissions belong to DragonBossAttackPreview's
            // deterministic timeline, not enabled curves on fixed flame renderers.
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = source.loop; settings.loopBlend = false; settings.startTime = 0; settings.stopTime = source.length;
            AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void Curve(AnimationClip clip, string path, Type type, string property, float[] times, float[] values, bool stepped = false)
        {
            var curve = new AnimationCurve(times.Select((t, i) => new Keyframe(t, values[i])).ToArray());
            for (int i = 0; i < times.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, stepped ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, stepped ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
        }

        private static Report Validate(GameObject root, Definition data, DragonRigLibrary library)
        {
            var errors = new List<string>(); int samples = 0, curves = 0;
            var animator = root.GetComponent<Animator>(); animator.enabled = false;
            foreach (var entry in library.clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(entry.clip);
                curves += bindings.Count(b => b.type == typeof(Transform));
                if (bindings.Any(b => b.type == typeof(Transform) && b.propertyName.Contains("Scale"))) errors.Add("Body scaling curve in " + entry.name);
                for (int sample = 0; sample <= 16; sample++)
                {
                    entry.clip.SampleAnimation(root, entry.duration * sample / 16f); samples++;
                    foreach (Part part in data.parts)
                    {
                        var bone = root.GetComponentsInChildren<Transform>(true).First(t => t.name == part.name);
                        if (!Finite(bone.localPosition) || !Finite(bone.localEulerAngles)) errors.Add("Invalid transform in " + entry.name + "/" + part.name);
                        if ((bone.localScale - Vector3.one).sqrMagnitude > .000001f) errors.Add("Changed body volume in " + entry.name + "/" + part.name);
                    }
                    if (entry.name == "Walk" || entry.name == "Run" || entry.name == "Death")
                    {
                        foreach (var art in root.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if (!art.enabled || art.sprite == null || art.transform.name != "Art") continue;
                            if (art.bounds.min.y < -.045f) { errors.Add("Artwork below floor in " + entry.name + " sample " + sample); break; }
                        }
                    }
                }
            }
            return new Report { prefab = PrefabPath, library = LibraryPath, bones = data.parts.Length, clips = library.clips.Length, scalarTransformCurves = curves, sampledPoses = samples, sourceSha256 = data.sourceSha256, sourceUnchanged = true, separateLimbTransforms = data.parts.Length >= 13, noBodyScaleCurves = true, clipNames = library.clips.Select(c => c.name).ToArray(), errors = errors.Distinct().ToArray() };
        }

        private static bool Finite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        private static Vector3 Vector(float[] value) => value != null && value.Length >= 3 ? new Vector3(value[0], value[1], value[2]) : Vector3.zero;
        private static Transform NewChild(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform; child.SetParent(parent, false); child.localPosition = position; child.localScale = Vector3.one; return child;
        }
        private static Sprite ImportSprite(string source, string name, float ppu)
        {
            if (string.IsNullOrEmpty(source) || !File.Exists(source)) throw new FileNotFoundException("Missing rig sprite", source);
            string path = SpriteFolder + "/" + name + ".png";
            byte[] data = File.ReadAllBytes(source);
            if (!File.Exists(path) || Hash(File.ReadAllBytes(path)) != Hash(data)) File.WriteAllBytes(path, data);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidDataException("No texture importer for " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu; importer.spritePivot = new Vector2(.5f, .5f);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings); importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidDataException("Missing imported sprite " + path);
            return sprite;
        }
        private static AnimatorController BuildController(Clip[] clips)
        {
            string path = AnimationFolder + "/DragonRig.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path) ?? AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var old in machine.states.Where(s => !clips.Any(c => c.name == s.state.name)).ToArray()) machine.RemoveState(old.state);
            return controller;
        }
        private static Sprite[] LoadEffectFrames(string name)
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
        private static string Hash(byte[] data)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
