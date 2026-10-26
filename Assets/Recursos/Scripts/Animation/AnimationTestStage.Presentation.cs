using System;
using System.Collections.Generic;
using System.Linq;
using MegaManX9.UI;
using UnityEngine;

namespace MegaManX9.Animation
{
    public sealed partial class AnimationTestStage
    {
        [SerializeField] private StageReadySequence readySequence;
        [SerializeField] private AnimationStageHealthHUD healthHUD;
        private readonly Dictionary<string,CharacterHealth> unitHealth=new Dictionary<string,CharacterHealth>();
        private readonly List<Entrance> entrances=new List<Entrance>();
        private bool presentationInitialized;

        private sealed class EntranceStep
        {
            public SpriteAnimationLibrary.Entry entry;
            public bool energy,descending;
            public float duration;
        }
        private sealed class Entrance
        {
            public Slot slot;
            public EntranceStep[] steps;
            public int step;
            public float elapsed;
            public GameObject effect;
            public SpriteRenderer renderer;
            public Animator animator;
        }

        public bool IsIntroductionPlaying=>(readySequence!=null&&readySequence.IsPlaying)||entrances.Count>0;
        public int ActiveEntranceCount=>entrances.Count;
        public StageReadySequence ReadySequence=>readySequence;
        public bool UsesOriginalAxl=>groups.Any(g=>g.unit=="Axl")&&!groups.Any(g=>g.unit=="AxlRig");

        public bool SelectPreview(string unit,string animationName)
        {
            if(!initialized)Initialize();
            int groupIndex=groups.FindIndex(g=>g.unit==unit);
            if(groupIndex<0)return false;
            int clipIndex=-1;
            for(int i=0;i<groups[groupIndex].Count;i++)if(groups[groupIndex].ClipName(i)==animationName){clipIndex=i;break;}
            if(clipIndex<0)return false;
            ShowIndividual(groupIndex);ApplyClip(slots[0],groupIndex,clipIndex);return true;
        }

        public void ConfigurePresentation(StageReadySequence ready,AnimationStageHealthHUD hud)
        {
            readySequence=ready;healthHUD=hud;
        }

        private static bool IsPlayable(string unit)=>unit=="Blue"||unit=="BlueUltimate"||unit=="Red";

        private void InitializePresentation()
        {
            if(presentationInitialized)return;
            if(readySequence==null||healthHUD==null||healthHUD.Assets==null)
                throw new InvalidOperationException("Faltan READY o las barras de vida. Reconstruye el escenario X4.");
            readySequence.AutoAdvance=false;readySequence.DrawAutomatically=false;
            readySequence.Completed+=StartVisibleEntrances;
            foreach(var group in groups)
            {
                if(!healthHUD.SupportsUnit(group.unit))continue;
                var existing=transform.Find("Health_"+group.unit);
                var owner=existing!=null?existing.gameObject:new GameObject("Health_"+group.unit);
                if(existing==null)owner.transform.SetParent(transform,false);
                var health=owner.GetComponent<CharacterHealth>()??owner.AddComponent<CharacterHealth>();
                health.Configure(group.unit=="Dragon"||group.unit=="DragonRig"?48:28);unitHealth[group.unit]=health;
            }
            presentationInitialized=true;
        }

        public CharacterHealth HealthFor(string unit)=>unitHealth.TryGetValue(unit,out var health)?health:null;

        public void ReplayStageIntroduction()
        {
            if(!initialized)Initialize();
            CancelStageIntroduction();paused=false;
            foreach(var slot in slots)
            {
                if(!slot.root.gameObject.activeSelf||slot.group<0||!IsPlayable(groups[slot.group].unit))continue;
                ApplyClip(slot,slot.group,IdleIndex(groups[slot.group]));
                slot.renderer.enabled=false;slot.activeAnimator.speed=0;
                HealthFor(groups[slot.group].unit)?.ResetHealth();
            }
            readySequence.SetPlayback(false,playbackSpeed);
            readySequence.Play(new Rect(0,0,CanvasWidth,CanvasHeight));
        }

        public void ReplaySelectedEntrance()
        {
            CancelStageIntroduction();paused=false;
            var slot=allMode?slots.FirstOrDefault(s=>s.root.gameObject.activeSelf&&s.group==selectedGroup):slots[0];
            if(slot==null||slot.group<0||!IsPlayable(groups[slot.group].unit))return;
            BeginEntrance(slot);
        }

        private void StartVisibleEntrances()
        {
            foreach(var slot in slots)
                if(slot.root.gameObject.activeSelf&&slot.group>=0&&IsPlayable(groups[slot.group].unit))BeginEntrance(slot);
        }

        private static int IdleIndex(Group group)
        {
            int index=group.entries==null?-1:group.entries.FindIndex(e=>e.animationName=="Idle");
            return Mathf.Max(0,index);
        }

        private SpriteAnimationLibrary.Entry RequiredEntry(string unit,string name)
        {
            return library.entries.FirstOrDefault(e=>e.unit==unit&&e.animationName==name)
                ??throw new InvalidOperationException("Falta la entrada "+unit+"/"+name+". Regenera los clips corregidos.");
        }

        private void BeginEntrance(Slot slot)
        {
            CancelSlotEntrance(slot);
            string unit=groups[slot.group].unit;
            var steps=new List<EntranceStep>();
            if(unit=="Red")
            {
                var energy=RequiredEntry("RedEffects","TeleportArrivalEnergy");
                steps.Add(new EntranceStep{entry=energy,energy=true,descending=true,duration=energy.clip.length});
                var materialize=RequiredEntry(unit,"TeleportIn");
                steps.Add(new EntranceStep{entry=materialize,duration=materialize.clip.length});
            }
            else
            {
                steps.Add(new EntranceStep{entry=RequiredEntry("Blue","TeleportBeam"),energy=true,descending=true,duration=.24f});
                if(unit=="Blue")
                {
                    var orb=RequiredEntry("Blue","TeleportOrb");
                    steps.Add(new EntranceStep{entry=orb,energy=true,duration=orb.clip.length});
                }
                var arrival=RequiredEntry(unit,"TeleportArrival");
                steps.Add(new EntranceStep{entry=arrival,duration=arrival.clip.length});
            }
            var effect=new GameObject("Entrance_"+unit);effect.transform.SetParent(slot.root,false);effect.layer=PreviewLayer;
            var renderer=effect.AddComponent<SpriteRenderer>();renderer.sharedMaterial=spriteMaterial;renderer.sortingOrder=40;
            var animator=effect.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var entrance=new Entrance{slot=slot,steps=steps.ToArray(),effect=effect,renderer=renderer,animator=animator};
            slot.entering=true;slot.renderer.enabled=false;slot.activeAnimator.speed=0;
            entrances.Add(entrance);ConfigureStep(entrance);
        }

        private void ConfigureStep(Entrance entrance)
        {
            var step=entrance.steps[entrance.step];
            entrance.effect.SetActive(step.energy);
            entrance.slot.renderer.enabled=!step.energy;
            if(step.energy)
            {
                entrance.animator.runtimeAnimatorController=step.entry.controller;
                entrance.animator.enabled=true;
                entrance.animator.Rebind();entrance.animator.Update(0);entrance.animator.speed=0;
                entrance.animator.enabled=false;
                entrance.renderer.sprite=step.entry.firstFrame;entrance.renderer.flipX=flipped;
            }
            else
            {
                entrance.slot.activeAnimator.enabled=true;
                entrance.slot.activeAnimator.runtimeAnimatorController=step.entry.controller;
                entrance.slot.activeAnimator.Rebind();entrance.slot.activeAnimator.Update(0);entrance.slot.activeAnimator.speed=0;
                entrance.slot.activeAnimator.enabled=false;
            }
            SampleEntrance(entrance);
        }

        private void SampleEntrance(Entrance entrance)
        {
            entrance.slot.renderer.flipX=flipped;
            var step=entrance.steps[entrance.step];
            float sample=Mathf.Min(entrance.elapsed,Mathf.Max(0,step.entry.clip.length-.0001f));
            if(step.energy)
            {
                step.entry.clip.SampleAnimation(entrance.effect,sample);
                float descent=step.descending?Mathf.Clamp01(entrance.elapsed/Mathf.Min(.24f,step.duration)):1;
                entrance.effect.transform.localPosition=Vector3.up*Mathf.Lerp(1.6f,0,descent);
                entrance.renderer.flipX=flipped;
            }
            else step.entry.clip.SampleAnimation(entrance.slot.root.gameObject,sample);
        }

        private void UpdatePresentation(float rawDelta)
        {
            float delta=paused?0:Mathf.Max(0,rawDelta)*playbackSpeed;
            if(readySequence!=null)
            {
                readySequence.SetPlayback(paused,playbackSpeed);
                bool wasReady=readySequence.IsPlaying;
                readySequence.Advance(delta);
                if(wasReady)return;
            }
            foreach(var entrance in entrances.ToArray())
            {
                if(!entrance.slot.root.gameObject.activeSelf){CancelSlotEntrance(entrance.slot);continue;}
                entrance.elapsed+=delta;
                while(entrance.elapsed>=entrance.steps[entrance.step].duration)
                {
                    entrance.elapsed-=entrance.steps[entrance.step].duration;
                    entrance.step++;
                    if(entrance.step>=entrance.steps.Length)
                    {
                        var slot=entrance.slot;CancelSlotEntrance(slot);
                        ApplyClip(slot,slot.group,IdleIndex(groups[slot.group]));
                        break;
                    }
                    ConfigureStep(entrance);
                }
                if(entrances.Contains(entrance))SampleEntrance(entrance);
            }
        }

        private void CancelSlotEntrance(Slot slot)
        {
            foreach(var entrance in entrances.Where(e=>e.slot==slot).ToArray())
            {
                entrances.Remove(entrance);
                if(entrance.effect!=null)
                {
                    entrance.effect.SetActive(false);
                    if(Application.isPlaying)Destroy(entrance.effect);else DestroyImmediate(entrance.effect);
                }
            }
            slot.entering=false;
        }

        private void CancelStageIntroduction()
        {
            if(readySequence!=null)readySequence.Reset();
            foreach(var entrance in entrances.ToArray())
            {
                var slot=entrance.slot;CancelSlotEntrance(slot);
                if(slot.group>=0)ApplyClip(slot,slot.group,IdleIndex(groups[slot.group]));
            }
            if(slots!=null)
                foreach(var slot in slots)
                    if(slot.group>=0&&IsPlayable(groups[slot.group].unit))slot.renderer.enabled=true;
        }

        private void DrawPresentationHUD()
        {
            if(healthHUD==null)return;
            if(allMode)
            {
                for(int i=0;i<slots.Length;i++)
                {
                    var slot=slots[i];if(!slot.root.gameObject.activeSelf||slot.group<0)continue;
                    string unit=groups[slot.group].unit;var health=HealthFor(unit);if(health==null)continue;
                    float x=40+i%3*520,y=153+i/3*310;bool boss=healthHUD.IsBossUnit(unit);
                    healthHUD.Draw(new Rect(boss?x+388:x+12,y+49,80,155),unit,health,boss);
                }
            }
            else
            {
                var group=groups[selectedGroup];var health=HealthFor(group.unit);
                if(health!=null)healthHUD.Draw(new Rect(healthHUD.IsBossUnit(group.unit)?882:76,241,140,260),group.unit,health,healthHUD.IsBossUnit(group.unit));
            }
        }

        private CharacterHealth SelectedHealth()
        {
            var health=groups.Count>selectedGroup?HealthFor(groups[selectedGroup].unit):null;
            if(health!=null)return health;
            foreach(var slot in slots)
                if(slot.group>=0&&slot.root.gameObject.activeSelf&&(health=HealthFor(groups[slot.group].unit))!=null)return health;
            return null;
        }

        private void DrawPresentationControls()
        {
            if(GUI.Button(new Rect(42,843,235,29),"READY + entradas")){ReplayStageIntroduction();GUIUtility.ExitGUI();}
            bool previous=GUI.enabled;
            GUI.enabled=previous&&groups.Count>selectedGroup&&IsPlayable(groups[selectedGroup].unit);
            if(GUI.Button(new Rect(289,843,235,29),"Entrada del seleccionado")){ReplaySelectedEntrance();GUIUtility.ExitGUI();}
            var health=SelectedHealth();GUI.enabled=previous&&health!=null;
            if(GUI.Button(new Rect(648,843,130,29),"Daño −4"))health.ApplyDamage(4);
            if(GUI.Button(new Rect(790,843,130,29),"Curar +4"))health.Heal(4);
            if(GUI.Button(new Rect(932,843,162,29),"Vida completa"))health.ResetHealth();
            GUI.enabled=previous;
            if(health!=null)GUI.Label(new Rect(1110,847,430,25),"Vida seleccionada: "+health.CurrentHealth+"/"+health.MaxHealth,tinyStyle);
        }

        private void OnDestroy()
        {
            if(readySequence!=null)readySequence.Completed-=StartVisibleEntrances;
        }
    }
}
