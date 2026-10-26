using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MegaManX9.Animation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MegaManX9.EditorTools
{
    public static class AnimationAssetBuilder
    {
        private const string ManifestPath="Assets/Recursos/Editor/AnimationPipeline/animation_manifest.json";
        private const string LibraryPath="Assets/Recursos/Animations/AnimationLibrary.asset";
        private const string ScenePath="Assets/Recursos/Scenes/Testing/AnimationGallery.unity";
        [Serializable] private sealed class Manifest { public float pixelsPerUnit=100; public ClipSpec[] clips; }
        [Serializable] private sealed class ClipSpec
        {
            public string category,unit,name,texture,source,notes;
            public float frameRate;
            public bool loop;
            public FrameSpec[] frames;
        }
        [Serializable] private sealed class FrameSpec
        {
            public string name;
            public int x,y,width,height;
            public float pivotX,pivotY;
        }

        [MenuItem("Mega Man X9/Animations/Rebuild Animation Library")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding animations.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ManifestPath)) throw new FileNotFoundException("Generate the sprite strips with Tools/AnimationPipeline/generate_assets.py first.",ManifestPath);
            Manifest manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if(manifest?.clips==null || manifest.clips.Length==0) throw new InvalidDataException("Animation manifest is empty.");
            var library=AssetDatabase.LoadAssetAtPath<SpriteAnimationLibrary>(LibraryPath);
            if(library==null){EnsureFolder("Assets/Recursos/Animations");library=ScriptableObject.CreateInstance<SpriteAnimationLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);}
            library.entries.Clear();
            var errors=new List<string>();
            foreach(ClipSpec spec in manifest.clips)
            {
                try
                {
                    library.entries.Add(BuildEntry(spec,manifest.pixelsPerUnit));
                }
                catch(Exception e){errors.Add(spec.unit+"/"+spec.name+": "+e.Message);Debug.LogException(e);}
            }
            if(errors.Count>0)throw new InvalidOperationException("Animation generation failed:\n"+string.Join("\n",errors));
            ConfigureStaticArt();
            ConfigureRenderer();
            foreach(var group in library.entries.GroupBy(e=>e.category+"/"+e.unit))BuildControllerAndPrefab(group.ToArray());
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            CreateGallery(library);
            ValidateLibrary(library);
            WriteValidationReport(library);
        }

        private static SpriteAnimationLibrary.Entry BuildEntry(ClipSpec spec,float pixelsPerUnit)
        {
            ImportStrip(spec,pixelsPerUnit);
            var sprites=AssetDatabase.LoadAllAssetsAtPath(spec.texture).OfType<Sprite>().ToDictionary(s=>s.name);
            var ordered=spec.frames.Select(f=>sprites.TryGetValue(f.name,out var sprite)?sprite:throw new InvalidDataException("Missing sprite "+f.name)).ToArray();
            string folder=$"Assets/Recursos/Animations/{spec.category}/{spec.unit}";EnsureFolder(folder);
            string path=$"{folder}/{spec.name}.anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}
            clip.name=spec.name;clip.frameRate=Mathf.Max(1,spec.frameRate);
            var keys=ordered.Select((sprite,i)=>new ObjectReferenceKeyframe{time=i/clip.frameRate,value=sprite}).ToArray();
            AnimationUtility.SetObjectReferenceCurve(clip,EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite"),keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime=spec.loop;settings.loopBlend=false;settings.startTime=0;settings.stopTime=ordered.Length/clip.frameRate;
            AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            return new SpriteAnimationLibrary.Entry{category=spec.category,unit=spec.unit,animationName=spec.name,clip=clip,firstFrame=ordered[0],frameCount=ordered.Length,frameWidth=spec.frames[0].width,frameHeight=spec.frames[0].height,notes=spec.notes};
        }

        public static void UpdatePlayerPresentation()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before updating player presentation.");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            var library=AssetDatabase.LoadAssetAtPath<SpriteAnimationLibrary>(LibraryPath);
            if(library==null)throw new InvalidDataException("Animation library is missing.");
            var entries=manifest.clips.Where(c=>c.unit=="BlueUltimate").Select(c=>BuildEntry(c,manifest.pixelsPerUnit)).ToArray();
            if(!entries.Any(e=>e.animationName=="TeleportArrival")||entries.Single(e=>e.animationName=="Idle").frameCount!=3)
                throw new InvalidDataException("The corrected Ultimate idle and arrival must be generated first.");
            library.entries.RemoveAll(e=>e.unit=="BlueUltimate");library.entries.AddRange(entries);
            BuildControllerAndPrefab(entries);
            foreach(var entry in library.entries.Where(e=>e.unit=="Axl"))entry.notes="Personaje axl original seleccionado por el usuario.";
            EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();ValidateLibrary(library);WriteValidationReport(library);
        }

        private static GUID StableGuid(string name)
        {
            using(var md5=MD5.Create())return new GUID(BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(name))).Replace("-","").ToLowerInvariant());
        }

        private static void ImportStrip(ClipSpec spec,float ppu)
        {
            AssetDatabase.ImportAsset(spec.texture,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(spec.texture) as TextureImporter;
            if(importer==null)throw new InvalidDataException("Texture importer not found: "+spec.texture);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=ppu;
            importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.maxTextureSize=8192;
            importer.isReadable=false;
            var factories=new SpriteDataProviderFactories();factories.Init();
            var provider=factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects=spec.frames.Select(f=>new SpriteRect{name=f.name,rect=new Rect(f.x,f.y,f.width,f.height),pivot=new Vector2(f.pivotX,f.pivotY),alignment=SpriteAlignment.Custom,spriteID=StableGuid(spec.texture+"/"+f.name)}).ToArray();
            provider.SetSpriteRects(rects);
            var names=provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names.SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void BuildControllerAndPrefab(SpriteAnimationLibrary.Entry[] entries)
        {
            var first=entries.FirstOrDefault(e=>e.animationName=="Idle")??entries[0];
            string folder=$"Assets/Recursos/Animations/{first.category}/{first.unit}";
            string controllerPath=$"{folder}/{first.unit}.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine=controller.layers[0].stateMachine;
            // Reuse states on rebuild so references and controller GUID remain stable.
            foreach(var old in machine.states.Where(s=>!entries.Any(e=>e.animationName==s.state.name)).ToArray())machine.RemoveState(old.state);
            foreach(var entry in entries)
            {
                var state=machine.states.FirstOrDefault(s=>s.state.name==entry.animationName).state;
                if(state==null)state=machine.AddState(entry.animationName,new Vector3((Array.IndexOf(entries,entry)%5)*220,(Array.IndexOf(entries,entry)/5)*60,0));
                state.motion=entry.clip;state.writeDefaultValues=false;
                entry.controller=controller;
                if(entry==first)machine.defaultState=state;
            }
            EditorUtility.SetDirty(controller);
            string prefabFolder=$"Assets/Recursos/Prefabs/{first.category}/{first.unit}";EnsureFolder(prefabFolder);
            var obj=new GameObject(first.unit+"_Animated");
            try
            {
                var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=first.firstFrame;
                renderer.sharedMaterial=GetSpriteMaterial();
                var animator=obj.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                obj.AddComponent<SpriteAnimationPlayer>();
                PrefabUtility.SaveAsPrefabAsset(obj,$"{prefabFolder}/{first.unit}_Animated.prefab");
            }
            finally{UnityEngine.Object.DestroyImmediate(obj);}
        }

        private static void ConfigureRenderer()
        {
            EnsureFolder("Assets/Recursos/Settings/Rendering");
            const string oldGlobal="Assets/Recursos/UniversalRenderPipelineGlobalSettings.asset";
            const string newGlobal="Assets/Recursos/Settings/Rendering/UniversalRenderPipelineGlobalSettings.asset";
            if(File.Exists(oldGlobal)&&!File.Exists(newGlobal))
            {
                string moveError=AssetDatabase.MoveAsset(oldGlobal,newGlobal);
                if(!string.IsNullOrEmpty(moveError))throw new IOException(moveError);
            }
            const string oldVolume="Assets/Recursos/DefaultVolumeProfile.asset";
            const string newVolume="Assets/Recursos/Settings/Rendering/DefaultVolumeProfile.asset";
            if(File.Exists(oldVolume)&&!File.Exists(newVolume))
            {
                string moveError=AssetDatabase.MoveAsset(oldVolume,newVolume);
                if(!string.IsNullOrEmpty(moveError))throw new IOException(moveError);
            }
            const string rendererPath="Assets/Recursos/Settings/Rendering/Renderer2D.asset";
            const string pipelinePath="Assets/Recursos/Settings/Rendering/UniversalRP2D.asset";
            var renderer=AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
            if(renderer==null){renderer=ScriptableObject.CreateInstance<Renderer2DData>();AssetDatabase.CreateAsset(renderer,rendererPath);}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,pipelinePath);}
            GraphicsSettings.defaultRenderPipeline=pipeline;
            int current=QualitySettings.GetQualityLevel();
            for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=pipeline;}
            QualitySettings.SetQualityLevel(current,false);
        }

        private static void ConfigureStaticArt()
        {
            foreach(string path in Directory.GetFiles("Assets","*.png",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')))
            {
                if(path.Contains("/Generated/"))continue;
                // Preserve authored rig atlases and dedicated test-stage sprite metadata.
                if(path!="Assets/Recursos/Sprites/Environment/escenario jugable.png"&&!path.StartsWith("Assets/Recursos/UI/HUD/",StringComparison.Ordinal)&&!path.StartsWith("Assets/Recursos/UI/Menus/",StringComparison.Ordinal))continue;
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)continue;
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;
                importer.filterMode=path.Contains("Back-to-Back")?FilterMode.Bilinear:FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=8192;importer.wrapMode=TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }

        private static void CreateGallery(SpriteAnimationLibrary library)
        {
            EnsureFolder("Assets/Recursos/Scenes/Testing");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObj=new GameObject("Main Camera");cameraObj.tag="MainCamera";
            var camera=cameraObj.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3;
            camera.backgroundColor=new Color(.025f,.045f,.08f);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.transform.position=new Vector3(0,0,-10);cameraObj.AddComponent<AudioListener>();
            var actor=new GameObject("Animation Preview");actor.transform.position=new Vector3(-1.8f,-1.15f,0);
            var initial=library.entries.FirstOrDefault(e=>e.unit=="Blue"&&e.animationName=="Idle")??library.entries[0];
            var sprite=actor.AddComponent<SpriteRenderer>();sprite.sprite=initial.firstFrame;sprite.sharedMaterial=GetSpriteMaterial();
            actor.transform.localScale=Vector3.one*4;
            var animator=actor.AddComponent<Animator>();animator.runtimeAnimatorController=initial.controller;
            var gallery=new GameObject("Animation Gallery").AddComponent<AnimationGallery>();gallery.Configure(library,animator,sprite);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=EditorBuildSettings.scenes.Where(s=>File.Exists(s.path)&&s.path!=ScenePath).ToList();
            scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private static Material GetSpriteMaterial()
        {
            const string path="Assets/Recursos/Settings/Rendering/PreviewSpriteUnlit.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null)return material;
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if(shader==null)throw new InvalidDataException("URP 2D unlit sprite shader is unavailable.");
            material=new Material(shader);AssetDatabase.CreateAsset(material,path);
            return material;
        }

        private static void ValidateLibrary(SpriteAnimationLibrary library)
        {
            foreach(var entry in library.entries)
            {
                var binding=new EditorCurveBinding{type=typeof(SpriteRenderer),path="",propertyName="m_Sprite"};
                var keys=AnimationUtility.GetObjectReferenceCurve(entry.clip,binding);
                if(keys==null||keys.Length!=entry.frameCount||keys.Any(k=>k.value==null))throw new InvalidDataException("Invalid animation keys: "+entry.unit+"/"+entry.animationName);
                if(entry.controller==null||entry.firstFrame==null)throw new InvalidDataException("Missing references: "+entry.animationName);
                if(Mathf.Abs(entry.clip.length-entry.frameCount/entry.clip.frameRate)>.0001f)throw new InvalidDataException("Incorrect clip duration: "+entry.animationName+" length="+entry.clip.length);
                var actor=new GameObject("Validation_"+entry.animationName);
                try
                {
                    var renderer=actor.AddComponent<SpriteRenderer>();
                    var animator=actor.AddComponent<Animator>();
                    animator.runtimeAnimatorController=entry.controller;animator.Rebind();animator.Update(0);
                    if(!animator.HasState(0,Animator.StringToHash(entry.animationName)))throw new InvalidDataException("Animator state missing: "+entry.animationName);
                    for(int i=0;i<entry.frameCount;i++)
                    {
                        entry.clip.SampleAnimation(actor,(i+.25f)/entry.clip.frameRate);
                        if(renderer.sprite!=keys[i].value)throw new InvalidDataException("Sprite sampling failed: "+entry.animationName+" frame "+i);
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(actor);}
            }
            if(!File.Exists(ScenePath))throw new InvalidDataException("Preview scene was not saved.");
        }

        [MenuItem("Mega Man X9/Animations/Validate Existing Library")]
        public static void FinalizeExisting()
        {
            var library=AssetDatabase.LoadAssetAtPath<SpriteAnimationLibrary>(LibraryPath);
            if(library==null)throw new InvalidDataException("Animation library is missing.");
            foreach(var entry in library.entries)
            {
                var binding=EditorCurveBinding.PPtrCurve("",typeof(SpriteRenderer),"m_Sprite");
                var keys=AnimationUtility.GetObjectReferenceCurve(entry.clip,binding);
                if(keys==null||keys.Length<entry.frameCount)throw new InvalidDataException("Animation keys missing: "+entry.animationName);
                AnimationUtility.SetObjectReferenceCurve(entry.clip,binding,keys.Take(entry.frameCount).ToArray());
                var settings=AnimationUtility.GetAnimationClipSettings(entry.clip);
                settings.startTime=0;settings.stopTime=entry.frameCount/entry.clip.frameRate;
                AnimationUtility.SetAnimationClipSettings(entry.clip,settings);EditorUtility.SetDirty(entry.clip);
            }
            ConfigureRenderer();AssetDatabase.SaveAssets();ValidateLibrary(library);WriteValidationReport(library);
        }

        private static void WriteValidationReport(SpriteAnimationLibrary library)
        {
            string report=JsonUtility.ToJson(new ValidationReport{success=true,clips=library.entries.Count,frames=library.entries.Sum(e=>e.frameCount),controllers=library.entries.Select(e=>e.controller).Distinct().Count(),scene=ScenePath},true);
            Directory.CreateDirectory("Tools/AnimationPipeline");File.WriteAllText("Tools/AnimationPipeline/unity_validation.json",report);
            Debug.Log("ANIMATION_PIPELINE_SUCCESS "+report);
        }

        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }

        [Serializable] private sealed class ValidationReport { public bool success;public int clips,frames,controllers;public string scene; }
    }
}
