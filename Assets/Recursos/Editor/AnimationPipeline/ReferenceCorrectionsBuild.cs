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
    public static class ReferenceCorrectionsBuild
    {
        [MenuItem("Mega Man X9/Animations/Apply X4 Reference Corrections",priority=9)]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Sal de Play antes de aplicar las correcciones.");
            BackupOpenWork();
            AnimationAssetBuilder.UpdatePlayerPresentation();
            HealthHUDBuilder.Build();StageReadySequenceBuilder.Build();DragonRigBuilder.Build();
            AnimationTestStageBuilder.Build();
            var scene=EditorSceneManager.OpenScene(AnimationTestStageBuilder.ScenePath,OpenSceneMode.Single);
            var stage=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<AnimationTestStage>(true)).Single();
            stage.PrepareForCapture(0,0);stage.ValidateCoverage();
            var report=ValidatePresentation(stage);
            stage.PrepareForCapture(0,0);
            stage.StageCamera.rect=new Rect(0,0,1,1);stage.StageCamera.aspect=16f/9f;
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the corrected animation test stage.");
            EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(AnimationTestStageBuilder.ScenePath);
            Directory.CreateDirectory("Tools/AnimationPipeline");
            File.WriteAllText("Tools/AnimationPipeline/x4_reference_validation.json",JsonUtility.ToJson(report,true));
            Debug.Log("X4_REFERENCE_CORRECTIONS_SUCCESS "+JsonUtility.ToJson(report));
        }

        private static Report ValidatePresentation(AnimationTestStage stage)
        {
            if(!stage.UsesOriginalAxl)throw new InvalidDataException("The scene must use the original white character.");
            var library=AssetDatabase.LoadAssetAtPath<SpriteAnimationLibrary>("Assets/Recursos/Animations/AnimationLibrary.asset");
            var idle=library.entries.Single(e=>e.unit=="BlueUltimate"&&e.animationName=="Idle");
            if(idle.frameCount!=3||!idle.clip.isLooping)throw new InvalidDataException("Ultimate Idle must be the three quiet breathing frames.");
            string[] players={"Blue","BlueUltimate","Red"};
            foreach(string player in players)
            {
                var health=stage.HealthFor(player);
                if(health==null||health.MaxHealth!=28)throw new InvalidDataException("Player health missing: "+player);
                health.ApplyDamage(4);if(health.CurrentHealth!=24)throw new InvalidDataException("Damage did not reach the HUD health model.");
                health.Heal(2);if(health.CurrentHealth!=26)throw new InvalidDataException("Healing failed.");health.ResetHealth();
            }
            if(stage.HealthFor("Axl")==null||stage.HealthFor("DragonRig")?.MaxHealth!=48)
                throw new InvalidDataException("Original white and enemy boss health are required.");
            int completed=0;Action complete=()=>completed++;
            stage.ReadySequence.Completed+=complete;
            var phases=new HashSet<string>();
            var update=typeof(AnimationTestStage).GetMethod("UpdatePresentation",BindingFlags.Instance|BindingFlags.NonPublic);
            if(update==null)throw new InvalidOperationException("Presentation clock unavailable.");
            stage.ReplayStageIntroduction();int mostEntrances=0;
            for(int i=0;i<420;i++)
            {
                phases.Add(stage.ReadySequence.CurrentPhase.ToString());
                update.Invoke(stage,new object[]{1f/60f});
                mostEntrances=Math.Max(mostEntrances,stage.ActiveEntranceCount);
            }
            stage.ReadySequence.Completed-=complete;
            string[] expected={"LineSweep","ExpansionFlash","TextReveal","Hold","Split","Complete"};
            if(expected.Any(p=>!phases.Contains(p))||completed!=1||mostEntrances!=3||stage.IsIntroductionPlaying)
                throw new InvalidDataException("READY must finish once, then all three entrances must finish.");
            for(int i=0;i<players.Length;i++)
            {
                var renderer=GameObject.Find("Station "+(i+1))?.GetComponent<SpriteRenderer>();
                var expectedIdle=library.entries.Single(e=>e.unit==players[i]&&e.animationName=="Idle");
                if(renderer==null||!renderer.enabled||renderer.sprite!=expectedIdle.firstFrame)
                    throw new InvalidDataException("Player did not materialize into its own Idle: "+players[i]);
            }
            var ready=stage.ReadySequence;ready.Play();ready.SetPlayback(true,1);float before=ready.Elapsed;ready.Advance(.25f);
            if(ready.Elapsed!=before)throw new InvalidDataException("READY must respect pause.");ready.Reset();
            return new Report{success=true,originalWhite=true,quietUltimateFrames=idle.frameCount,libraryEntries=library.entries.Count,
                units=stage.GroupCount,bossActions=stage.RigClipCount,playerHealth=28,bossHealth=48,simultaneousEntrances=mostEntrances,
                readyCompletedEvents=completed,readyPhases=expected,scene=AnimationTestStageBuilder.ScenePath};
        }

        private static void BackupOpenWork()
        {
            for(int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if(!scene.isLoaded||!scene.isDirty||EditorSceneManager.IsPreviewScene(scene))continue;
                string folder="Assets/Recursos/Scenes/Testing/WorkspaceBackups";
                Directory.CreateDirectory(folder);AssetDatabase.Refresh();
                string path=folder+"/Before_X4_Corrections_"+Guid.NewGuid().ToString("N").Substring(0,8)+".unity";
                bool copy=!string.IsNullOrEmpty(scene.path);
                if(!EditorSceneManager.SaveScene(scene,path,copy))throw new IOException("Could not back up the open scene.");
                if(scene.path==AnimationTestStageBuilder.ScenePath)EditorSceneManager.SaveScene(scene);
            }
        }

        [Serializable] private sealed class Report
        {
            public bool success,originalWhite;public int quietUltimateFrames,libraryEntries,units,bossActions,playerHealth,bossHealth,simultaneousEntrances,readyCompletedEvents;
            public string scene;public string[] readyPhases;
        }
    }
}
