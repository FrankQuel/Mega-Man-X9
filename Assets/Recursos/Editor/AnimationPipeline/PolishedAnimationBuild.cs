using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MegaManX9.Animation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MegaManX9.EditorTools
{
    /// <summary>Builds and verifies the authored rigs and their review stage.</summary>
    public static class PolishedAnimationBuild
    {
        public const string StagePath="Assets/Recursos/Scenes/Testing/AnimationTestStage.unity";
        private static readonly string[] Builders={
            "MegaManX9.EditorTools.DragonRigBuilder",
            "MegaManX9.EditorTools.AxlRigBuilder",
            "MegaManX9.EditorTools.AnimationTestStageBuilder"
        };
        private static readonly string[] Prefabs={
            "Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab",
            "Assets/Recursos/Prefabs/Bosses/AxlRig/AxlRig_Animated.prefab"
        };

        [MenuItem("Mega Man X9/Animations/Build Polished Test Stage",priority=10)]
        public static void Build()
        {
            ReferenceCorrectionsBuild.Build();
        }

        private static List<string> BackupDirtyScenes()
        {
            var backups=new List<string>();
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(!scene.isLoaded||!scene.isDirty||EditorSceneManager.IsPreviewScene(scene))continue;
                string folder="Assets/Recursos/Scenes/Testing/WorkspaceBackups";
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                string name=string.IsNullOrEmpty(scene.name)?"Untitled":scene.name;
                foreach(char invalid in Path.GetInvalidFileNameChars())name=name.Replace(invalid,'_');
                string path=folder+"/"+name+"_before_polish_"+Guid.NewGuid().ToString("N").Substring(0,8)+".unity";
                // Unity cannot create additive scenes while an untitled scene is unsaved.
                bool saveAsCopy=!string.IsNullOrEmpty(scene.path);
                if(!EditorSceneManager.SaveScene(scene,path,saveAsCopy))throw new IOException("Could not preserve the current scene: "+name);
                backups.Add(path);
            }
            return backups;
        }

        private static RigReport ValidateRig(string path)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)throw new InvalidDataException("Rig prefab missing: "+path);
            var scene=EditorSceneManager.NewPreviewScene();
            GameObject actor=null;
            try
            {
                actor=UnityEngine.Object.Instantiate(prefab);
                SceneManager.MoveGameObjectToScene(actor,scene);
                var animator=actor.GetComponent<Animator>();
                if(animator==null||animator.runtimeAnimatorController==null)throw new InvalidDataException("Rig needs an Animator at its root: "+path);
                var renderers=actor.GetComponentsInChildren<SpriteRenderer>(true);
                if(renderers.Length<8||renderers.Any(r=>r.sprite==null))throw new InvalidDataException("Rig body pieces are missing: "+path);
                var clips=animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
                if(clips.Length<8)throw new InvalidDataException("Rig needs authored action clips: "+path);
                animator.Rebind();animator.Update(0);
                int sampled=0;var joints=new HashSet<string>();
                float minHeight=float.MaxValue,maxHeight=0;
                foreach(var clip in clips)
                {
                    if(clip.length<=0)throw new InvalidDataException("Zero-length rig action: "+clip.name);
                    var bindings=AnimationUtility.GetCurveBindings(clip);
                    foreach(var binding in bindings)
                    {
                        if(!string.IsNullOrEmpty(binding.path)&&actor.transform.Find(binding.path)==null)
                            throw new InvalidDataException("Unbound rig curve: "+clip.name+" / "+binding.path);
                        if(binding.type==typeof(Transform)&&!string.IsNullOrEmpty(binding.path))joints.Add(binding.path);
                    }
                    if((clip.name=="Walk"||clip.name=="Run")&&bindings.Where(b=>b.type==typeof(Transform)).Select(b=>b.path).Distinct().Count()<4)
                        throw new InvalidDataException("Locomotion must move multiple joints: "+clip.name);
                    for(int frame=0;frame<16;frame++)
                    {
                        clip.SampleAnimation(actor,clip.length*(frame+.25f)/16f);
                        Bounds bounds=renderers[0].bounds;
                        foreach(var renderer in renderers)if(renderer.enabled&&renderer.color.a>.01f)bounds.Encapsulate(renderer.bounds);
                        if(!Finite(bounds.center)||!Finite(bounds.size)||bounds.size.magnitude>100)
                            throw new InvalidDataException("Non-finite or exploded rig pose: "+clip.name);
                        minHeight=Mathf.Min(minHeight,bounds.size.y);maxHeight=Mathf.Max(maxHeight,bounds.size.y);
                        sampled++;
                    }
                }
                return new RigReport{prefab=path,actions=clips.Length,spriteParts=renderers.Length,animatedJoints=joints.Count,sampledPoses=sampled,minPoseHeight=minHeight,maxPoseHeight=maxHeight};
            }
            finally
            {
                if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static bool Finite(Vector3 value)=>!float.IsNaN(value.x)&&!float.IsNaN(value.y)&&!float.IsNaN(value.z)&&!float.IsInfinity(value.x)&&!float.IsInfinity(value.y)&&!float.IsInfinity(value.z);

        [MenuItem("Mega Man X9/Animations/Capture Test Stage Screenshot",priority=11)]
        public static void CaptureScreenshot()
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Pulsa Play en el escenario de prueba antes de capturar.");
            Directory.CreateDirectory("Previews");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Previews/AnimationTestStage.png"));
            Debug.Log("Stage screenshot requested: Previews/AnimationTestStage.png");
        }

        [Serializable] private sealed class RigReport
        {
            public string prefab;public int actions,spriteParts,animatedJoints,sampledPoses;public float minPoseHeight,maxPoseHeight;
        }
        [Serializable] private sealed class BuildReport
        {
            public bool success;public int originalLibraryEntries,originalUnits;public string stage;public string[] workspaceBackups;public RigReport[] rigs;
        }
    }
}
