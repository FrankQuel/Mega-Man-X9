using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MegaManX9.Animation;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    /// <summary>Captures the real player loop after Animator evaluation, including the HUD.</summary>
    public static class ReferenceVisualQA
    {
        private const string Request="Tools/AnimationPipeline/run_visual_qa.request";
        private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly List<string> captures=new List<string>(), errors=new List<string>();
        [Serializable] private sealed class Report { public bool success;public string[] captures,errors;public string scene; }

        [InitializeOnLoadMethod]
        private static void ResumeRequestedCapture()
        {
            if(File.Exists(Request))EditorApplication.delayCall+=Run;
        }

        [MenuItem("Mega Man X9/Animations/Capture X4 Reference Previews",priority=12)]
        public static void Run()
        {
            if(File.Exists(Request))File.Delete(Request);
            if(!EditorApplication.isPlaying)
            {
                EditorApplication.playModeStateChanged-=OnPlay;
                EditorApplication.playModeStateChanged+=OnPlay;
                EditorApplication.isPlaying=true;return;
            }
            var stage=UnityEngine.Object.FindFirstObjectByType<AnimationTestStage>();
            if(stage==null)throw new InvalidOperationException("Open the animation test stage first.");
            captures.Clear();errors.Clear();stage.StartCoroutine(Capture(stage));
        }

        private static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            EditorApplication.playModeStateChanged-=OnPlay;
            EditorApplication.delayCall+=Run;
        }

        private static void Pause(AnimationTestStage stage,bool value)=>typeof(AnimationTestStage).GetField("paused",Private).SetValue(stage,value);
        private static void Advance(AnimationTestStage stage,float seconds)=>typeof(AnimationTestStage).GetMethod("UpdatePresentation",Private).Invoke(stage,new object[]{seconds});
        private static IEnumerator Save(string name)
        {
            for(int i=0;i<4;i++){EditorApplication.QueuePlayerLoopUpdate();yield return null;}
            string path="Previews/X4References/"+name+".png";
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(path));captures.Add(path);
            for(int i=0;i<4;i++){EditorApplication.QueuePlayerLoopUpdate();yield return null;}
        }

        private static IEnumerator Capture(AnimationTestStage stage)
        {
            Directory.CreateDirectory("Previews/X4References");
            yield return null;
            stage.PrepareForCapture(0,0);stage.ReplayStageIntroduction();Pause(stage,true);
            float[] readyTimes={.14f,.37f,.9f,1.56f};
            string[] readyNames={"01_READY_Line","02_READY_Flash","03_READY_Hold","04_READY_Exit"};
            for(int i=0;i<readyTimes.Length;i++)
            {
                stage.ReadySequence.Seek(readyTimes[i]);yield return Save(readyNames[i]);
            }
            string[] units={"Blue","BlueUltimate","Red"};float[] times={.9f,.55f,.4f};
            for(int i=0;i<units.Length;i++)
            {
                stage.SelectPreview(units[i],"Idle");stage.ReplaySelectedEntrance();Advance(stage,times[i]);Pause(stage,true);
                yield return Save("05_Entrance_"+units[i]);
                CheckSampledEntrance(stage,units[i]);
                if(!stage.IsIntroductionPlaying)errors.Add("Entrance completed too early: "+units[i]);
            }
            string[] attacks={"FireBreath","Fireball","RisingFlame","FlamingKick","GroundFireWave","HurtFlash"};
            for(int i=0;i<attacks.Length;i++)
            {
                stage.SelectPreview("DragonRig",attacks[i]);Pause(stage,true);
                var driver=UnityEngine.Object.FindObjectsByType<DragonBossAttackPreview>(FindObjectsSortMode.None).Single(d=>d.gameObject.activeInHierarchy);
                driver.Seek(driver.Duration*(attacks[i]=="HurtFlash"?.04f:.48f));
                yield return Save("06_Dragon_"+attacks[i]);
                if(attacks[i]=="FireBreath")
                {
                    var mouth=driver.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="OpenMouthArt");
                    if(!mouth.enabled)errors.Add("Mouth did not open during fire emission in the player loop.");
                }
                if(attacks[i]!="HurtFlash"&&driver.ActiveProjectileCount==0)errors.Add("Missing visible fire: "+attacks[i]);
            }
            stage.SelectPreview("DragonRig","CombatIdle");
            stage.PrepareForCapture(0,0);stage.HealthFor("Blue").ApplyDamage(4);stage.HealthFor("Red").ApplyDamage(8);
            yield return Save("07_Health_Damage");
            foreach(string unit in units)stage.HealthFor(unit).ResetHealth();
            stage.PrepareForCapture(0,0);yield return Save("08_All_OriginalWhite_QuietUltimate");
            var report=new Report{success=errors.Count==0,captures=captures.ToArray(),errors=errors.ToArray(),scene=stage.gameObject.scene.path};
            File.WriteAllText("Tools/AnimationPipeline/x4_visual_qa.json",JsonUtility.ToJson(report,true));
            if(errors.Count>0)Debug.LogError("X4_VISUAL_QA_FAILED "+string.Join("; ",errors));
            else Debug.Log("X4_VISUAL_QA_SUCCESS "+captures.Count+" captures from the actual player loop.");
            Pause(stage,false);
        }

        private static void CheckSampledEntrance(AnimationTestStage stage,string unit)
        {
            // Observe the sprite after several real Unity frames; catches an enabled Animator
            // restoring default Idle after the manual materialization sample.
            var list=(IEnumerable)typeof(AnimationTestStage).GetField("entrances",Private).GetValue(stage);
            foreach(var entrance in list)
            {
                var type=entrance.GetType();var steps=(Array)type.GetField("steps").GetValue(entrance);
                int index=(int)type.GetField("step").GetValue(entrance);var step=steps.GetValue(index);
                var entry=(SpriteAnimationLibrary.Entry)step.GetType().GetField("entry").GetValue(step);
                float elapsed=(float)type.GetField("elapsed").GetValue(entrance);
                bool energy=(bool)step.GetType().GetField("energy").GetValue(step);
                var target=energy?(GameObject)type.GetField("effect").GetValue(entrance):GameObject.Find("Station 1");
                var renderer=target.GetComponent<SpriteRenderer>();
                var binding=AnimationUtility.GetObjectReferenceCurveBindings(entry.clip).Single(b=>b.type==typeof(SpriteRenderer)&&b.propertyName=="m_Sprite");
                var keys=AnimationUtility.GetObjectReferenceCurve(entry.clip,binding);
                var expected=keys.Where(k=>k.time<=elapsed+.00001f).Last().value;
                if(renderer.sprite!=expected)errors.Add("Animator overwrote the entrance sprite: "+unit);
                if(target.GetComponent<Animator>().enabled)errors.Add("Manual entrance animator remained enabled: "+unit);
            }
        }
    }
}
