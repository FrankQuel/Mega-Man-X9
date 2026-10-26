using System;
using System.Collections;
using System.Collections.Generic;
using MegaManX9.Animation;
using MegaManX9.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RainLevelGame : MonoBehaviour
    {
        [SerializeField] private RainLevelAssets assets;
        [SerializeField] private RainHeroController player;
        [SerializeField] private RainDragonBoss boss;
        [SerializeField] private RainAxlBoss axlBoss;
        [SerializeField] private StageReadySequence ready;
        private Vector2 checkpoint=new Vector2(4,0);
        private float deathTimer;
        private bool initialized;
        private int attempt;
        private HeroForm initialForm;
        private Coroutine bossIntroduction;
        private Coroutine bossStory;
        private RainBossCinematic cinematic;
        private RainBossCinematic.SpriteActor companion;
        private System.Random drops;
        private RainFrontEnd frontEnd;
        private RainNarrative narrative;
        private RainStartupVideo startupVideo;
        private RainControlsGuide controlsGuide;
        private bool campaignArrivalPending;
        private readonly HashSet<string> collectedPickupIds=new HashSet<string>(StringComparer.Ordinal);
        public static RainLevelGame Instance {get;private set;}
        public RainLevelAssets Assets=>assets;
        public RainHeroController Player=>player;
        public RainDragonBoss Boss=>boss;
        public RainAxlBoss AxlBoss=>axlBoss;
        public CharacterHealth ActiveBossHealth=>IsBossPhase?(UsesAxlBoss?axlBoss?.Health:boss?.Health):null;
        public string ActiveBossName=>UsesAxlBoss?"AXL":"MAGMA DRAGOON";
        public bool IsBossWarning=>Phase==RainLevelPhase.BossIntro||CinematicStage=="AxlWarning";
        public RainNarrative Narrative=>narrative;
        public RainStartupVideo StartupVideo=>startupVideo;
        public RainControlsGuide ControlsGuide=>controlsGuide;
        public string ActiveBossUnit=>UsesAxlBoss?RainAxlBoss.OriginalUnit:"DragonRig";
        public Transform ActiveBossTransform=>UsesAxlBoss&&axlBoss!=null?axlBoss.transform:boss!=null?boss.transform:null;
        private bool UsesAxlBoss=>Phase==RainLevelPhase.AxlBoss||Phase==RainLevelPhase.Ending||BossWasDefeated;
        private bool IsBossPhase=>Phase==RainLevelPhase.BossIntro||Phase==RainLevelPhase.Boss||Phase==RainLevelPhase.BossCinematic||Phase==RainLevelPhase.AxlBoss||Phase==RainLevelPhase.Ending;
        public string CinematicStage {get;private set;}="None";
        public bool CompanionVisible=>companion!=null&&companion.Object!=null&&companion.Object.activeInHierarchy&&companion.Renderer.enabled;
        public HeroForm CompanionForm {get;private set;}
        public bool EndingCompleted {get;private set;}
        public int RunSeed {get;private set;}
        public const int DragonStoryThreshold=60;
        public RainLevelPhase Phase {get;private set;}=RainLevelPhase.FrontEnd;
        public bool IsPaused {get;private set;}
        public bool IsTransformationPaused {get;private set;}
        public bool HasStarted {get;private set;}
        public bool IsGameOver=>HasStarted&&Lives<=0&&Phase==RainLevelPhase.Dead;
        public int Lives {get;private set;}=3;
        public bool InputLocked=>IsPaused||IsTransformationPaused||Phase==RainLevelPhase.FrontEnd||Phase==RainLevelPhase.Intro||Phase==RainLevelPhase.BossIntro||Phase==RainLevelPhase.BossCinematic||Phase==RainLevelPhase.Ending||Phase==RainLevelPhase.Dead||Phase==RainLevelPhase.Won;
        public float Elapsed {get;private set;}
        public int DefeatedEnemies {get;private set;}
        public int Score {get;private set;}
        public string ActiveHeroName=>player==null||player.CurrentForm==HeroForm.Blue?"X":"Zero";
        public string CheckpointName {get;private set;}="Azoteas exteriores";
        public string Message {get;private set;}="FUNDICIÓN BAJO LA TORMENTA";
        public float MessageRemaining {get;private set;}
        public bool BossWasDefeated {get;private set;}
        public RainFrontEnd FrontEnd=>frontEnd;
        public event Action CheckpointChanged,EncounterStarted,EncounterWon,LevelCompleted;

        public void Configure(RainLevelAssets source,RainHeroController hero,RainDragonBoss dragon,StageReadySequence sequence)
        {assets=source;player=hero;boss=dragon;ready=sequence;}
        public void ConfigureAxl(RainAxlBoss axl){axlBoss=axl;}
        private void Awake(){Instance=this;Time.timeScale=1;}
        private void Start()
        {
            EnsureInitialized();
            if(!HasStarted)
            {
                frontEnd.Close();player.SetVisible(false);
                startupVideo.Play(ReturnToMainMenu);
            }
        }
        private void EnsureInitialized()
        {
            if(initialized)return;
            if(assets==null||player==null||boss==null||ready==null)
                throw new InvalidOperationException("Reconstruye Rainy Foundry: faltan referencias del nivel.");
            player.Configure(assets);boss.Configure(assets);boss.ResetBoss();
            if(axlBoss==null)axlBoss=FindAnyObjectByType<RainAxlBoss>();
            if(axlBoss==null){var obj=new GameObject("Axl Original boss");obj.transform.position=new Vector2(256,0);axlBoss=obj.AddComponent<RainAxlBoss>();}
            axlBoss.Configure(assets);axlBoss.ResetBoss();
            boss.Health.Changed+=OnDragonHealthChanged;axlBoss.Health.Changed+=OnAxlHealthChanged;
            cinematic=GetComponent<RainBossCinematic>();if(cinematic==null)cinematic=gameObject.AddComponent<RainBossCinematic>();cinematic.Configure(assets);
            var hud=GetComponent<RainLevelHUD>();if(hud!=null)hud.Configure(assets);
            if(GetComponent<RainLevelAudio>()==null)gameObject.AddComponent<RainLevelAudio>();
            narrative=GetComponent<RainNarrative>();if(narrative==null)narrative=gameObject.AddComponent<RainNarrative>();narrative.Configure(this);
            frontEnd=GetComponent<RainFrontEnd>();if(frontEnd==null)frontEnd=gameObject.AddComponent<RainFrontEnd>();frontEnd.Configure(this);
            startupVideo=GetComponent<RainStartupVideo>();if(startupVideo==null)startupVideo=gameObject.AddComponent<RainStartupVideo>();startupVideo.Configure();
            controlsGuide=GetComponent<RainControlsGuide>();if(controlsGuide==null)controlsGuide=gameObject.AddComponent<RainControlsGuide>();controlsGuide.Configure(this);
            ready.AutoAdvance=false;ready.DrawAutomatically=true;ready.Completed+=OnReadyCompleted;
            initialized=true;
        }
        public void StartNewGame(HeroForm form)=>StartRun(form,false);
        public void StartCampaign(HeroForm form)=>StartRun(form,true);
        private void StartRun(HeroForm form,bool showIntroduction)
        {
            if(startupVideo!=null&&startupVideo.IsPlaying)return;
            EnsureInitialized();EndTransformationPause();SetPaused(false);HasStarted=true;Lives=3;initialForm=form;
            checkpoint=new Vector2(4,0);CheckpointName="Azoteas exteriores";Elapsed=0;DefeatedEnemies=0;Score=0;collectedPickupIds.Clear();RunSeed=BitConverter.ToInt32(Guid.NewGuid().ToByteArray(),0);drops=new System.Random(RunSeed);
            frontEnd.Close();ResetAttempt();player.SetInitialForm(form);player.Teleport(checkpoint,true);player.SetVisible(false);
            Phase=RainLevelPhase.Intro;
            campaignArrivalPending=showIntroduction;
            if(showIntroduction)
            {
                int current=attempt;CinematicStage="CampaignIntro";
                player.SetCinematicIdle(true);player.SetVisible(false);
                controlsGuide.Play(()=>
                {
                    if(current!=attempt||!HasStarted)return;
                    CinematicStage="CampaignIntro";
                    narrative.PlayIntroduction(()=>
                    {
                        if(current!=attempt||!HasStarted)return;
                        player.SetCinematicIdle(false);player.SetVisible(false);CinematicStage="None";
                        ready.Play();RainLevelAudio.Play("ready");
                    });
                });
                CinematicStage="ControlsGuide";
            }
            else{ready.Play();RainLevelAudio.Play("ready");}
        }
        public void ReturnToMainMenu()
        {
            if(startupVideo!=null&&startupVideo.IsPlaying)return;
            EnsureInitialized();SetPaused(false);HasStarted=false;ResetAttempt();Phase=RainLevelPhase.FrontEnd;
            player.Teleport(new Vector2(4,0),true);player.SetVisible(false);MessageRemaining=0;frontEnd.ShowCover();
        }
        public void ReturnFromResults(){ReturnToMainMenu();frontEnd.ConfirmSelection();}
        private void OnReadyCompleted()
        {
            if(Phase!=RainLevelPhase.Intro||!HasStarted)return;
            int current=attempt;
            player.BeginEntrance(()=>
            {
                if(current!=attempt||!HasStarted)return;
                if(campaignArrivalPending)
                {
                    campaignArrivalPending=false;CinematicStage="MissionArrival";player.SetCinematicIdle(true);
                    narrative.PlayMissionArrival(initialForm,()=>
                    {
                        if(current!=attempt||!HasStarted)return;
                        player.SetCinematicIdle(false);CinematicStage="None";Phase=RainLevelPhase.Playing;
                    });
                }
                else{Phase=RainLevelPhase.Playing;Notify("Avanza hasta la fundición · "+RainInputBindings.KeyLabel(RainInputAction.SwitchHero)+" cambia entre X y Zero",4);}
            });
        }
        private void Update()
        {
            if(!HasStarted||Phase==RainLevelPhase.FrontEnd)return;
            if(Phase==RainLevelPhase.Won)
            {
                if(Keyboard.current!=null&&(Keyboard.current.enterKey.wasPressedThisFrame||Keyboard.current.numpadEnterKey.wasPressedThisFrame)||
                    Gamepad.current!=null&&Gamepad.current.buttonSouth.wasPressedThisFrame)ReturnFromResults();
                return;
            }
            if(controlsGuide!=null&&controlsGuide.IsActive)return;
            if(narrative!=null&&narrative.IsActive){if(cinematic!=null)cinematic.Advance(StoryDelta);return;}
            if(RainInputBindings.Pressed(RainInputAction.Pause)&&Phase!=RainLevelPhase.Dead&&Phase!=RainLevelPhase.Won)SetPaused(!IsPaused);
            if(RainInputBindings.Pressed(RainInputAction.Retry))
            {
                if(IsGameOver||Phase==RainLevelPhase.Won)RestartLevel();else Respawn();
                return;
            }
            if(IsPaused)return;
            if(Phase==RainLevelPhase.Playing||Phase==RainLevelPhase.Boss||Phase==RainLevelPhase.AxlBoss)Elapsed+=Time.deltaTime;
            if(cinematic!=null)cinematic.Advance(StoryDelta);
            if(Phase==RainLevelPhase.Boss&&boss.Health.CurrentHealth<=DragonStoryThreshold)TryBeginAxlIntervention();
            MessageRemaining=Mathf.Max(0,MessageRemaining-Time.deltaTime);
            if(ready.IsPlaying)ready.Advance(Time.deltaTime);
            if(Phase==RainLevelPhase.Dead&&!IsGameOver&&(deathTimer-=Time.deltaTime)<=0)Respawn();
        }
        public void Notify(string text,float seconds=3){Message=text;MessageRemaining=seconds;}
        public void SetPaused(bool paused)
        {
            if(paused&&(!HasStarted||Phase==RainLevelPhase.FrontEnd||Phase==RainLevelPhase.Dead||Phase==RainLevelPhase.Won))return;
            IsPaused=paused;ApplyTimeScale();
        }
        private void ApplyTimeScale(){Time.timeScale=IsPaused||IsTransformationPaused?0:1;}
        public void BeginTransformationPause()
        {
            if(!HasStarted||Phase==RainLevelPhase.FrontEnd||Phase==RainLevelPhase.Dead||Phase==RainLevelPhase.Won)return;
            IsTransformationPaused=true;ApplyTimeScale();
        }
        public void EndTransformationPause(){IsTransformationPaused=false;ApplyTimeScale();}
        public void SetCheckpoint(Vector2 position,string name)
        {
            if(Phase!=RainLevelPhase.Playing||Vector2.Distance(checkpoint,position)<.1f)return;
            checkpoint=position;CheckpointName=name;player.BlueHealth.Heal(8);player.RedHealth.Heal(8);
            Notify("CHECKPOINT · "+name,3);CheckpointChanged?.Invoke();
        }
        public void BeginBossEncounter()
        {
            if(Phase!=RainLevelPhase.Playing||BossWasDefeated)return;
            Phase=RainLevelPhase.BossIntro;Notify("WARNING · MAGMA DRAGOON",2.5f);EncounterStarted?.Invoke();
            RainLevelAudio.Play("warning");
            bossIntroduction=StartCoroutine(BossEntrance(attempt));
        }
        private IEnumerator BossEntrance(int current)
        {
            float remaining=2.5f;
            while(remaining>0&&current==attempt){if(!IsPaused)remaining-=Time.deltaTime;yield return null;}
            if(current!=attempt||Phase!=RainLevelPhase.BossIntro)yield break;
            bossIntroduction=null;
            boss.BeginEntrance(()=>
            {
                if(current!=attempt||Phase!=RainLevelPhase.BossIntro)return;
                Phase=RainLevelPhase.Boss;boss.ActivateBoss();Notify("MAGMA DRAGOON · esquiva el fuego y ataca en sus recuperaciones",4);
            });
        }
        public void BossDefeated()
        {
            // Dragon is the first encounter. His defeat continues the story rather than opening an exit.
            if(BossWasDefeated)return;TryBeginAxlIntervention();
        }
        public void FinishLevel()
        {
            if(!EndingCompleted||Phase==RainLevelPhase.Won)return;
            EndTransformationPause();RainLevelAudio.SetConversation(false);Phase=RainLevelPhase.Won;CinematicStage="Complete";Notify("MISIÓN COMPLETADA",999);LevelCompleted?.Invoke();
        }

        private float StoryDelta=>IsPaused||IsTransformationPaused?0:Time.deltaTime;
        private bool StoryValid(int current)=>current==attempt&&HasStarted;
        private IEnumerator StoryWait(float seconds,int current)
        {
            while(seconds>0&&StoryValid(current)){seconds-=StoryDelta;yield return null;}
        }
        private void OnDragonHealthChanged(CharacterHealth value)
        {
            if(Phase==RainLevelPhase.Boss&&value.CurrentHealth<=DragonStoryThreshold)TryBeginAxlIntervention();
        }
        private void OnAxlHealthChanged(CharacterHealth value)
        {
            if(Phase==RainLevelPhase.AxlBoss&&value.CurrentHealth<=1)AxlBossYielded();
        }

        /// <summary>Also used by native QA; normal play reaches this through Dragon's health event.</summary>
        public bool TryBeginAxlIntervention()
        {
            if(Phase!=RainLevelPhase.Boss||axlBoss==null||bossStory!=null)return false;
            Phase=RainLevelPhase.BossCinematic;CinematicStage="HeroIdle";
            EndTransformationPause();RainProjectile.ClearTeam(DamageTeam.Enemy);RainProjectile.ClearTeam(DamageTeam.Player);
            boss.HoldForStory();
            player.Teleport(new Vector2(Mathf.Clamp(player.transform.position.x,239,258),0),false);
            player.SetCinematicIdle(true);player.SetCinematicFacing(false);
            Notify("Una señal desconocida se acerca…",2);
            bossStory=StartCoroutine(AxlIntervention(attempt));return true;
        }

        private IEnumerator AxlIntervention(int current)
        {
            yield return StoryWait(.55f,current);if(!StoryValid(current))yield break;
            CinematicStage="AxlArrival";
            float axlX=Mathf.Clamp(boss.transform.position.x+3.1f,241,258.4f);
            axlBoss.SetStoryPosition(new Vector2(axlX,0));axlBoss.BeginArrival();
            player.SetCinematicFacing(axlX<player.transform.position.x);
            while(axlBoss.IsPresenting&&StoryValid(current))yield return null;
            if(!StoryValid(current))yield break;
            CinematicStage="AxlAim";axlBoss.Face(boss.transform.position.x<axlBoss.transform.position.x);
            axlBoss.PlayPresentation("Aim");yield return StoryWait(.35f,current);if(!StoryValid(current))yield break;
            CinematicStage="AxlShot";axlBoss.PlayPresentation("Shoot");RainLevelAudio.Play("shot");
            yield return StoryWait(.13f,current);if(!StoryValid(current))yield break;
            Vector2 target=(Vector2)boss.transform.position+Vector2.up*.9f;
            Vector2 origin=(Vector2)axlBoss.transform.position+new Vector2(target.x<axlX?-.75f:.75f,1.15f);
            var shot=cinematic.Create("GigaDeathProjectiles","SmallShot",origin,3.2f);
            if(shot!=null){shot.Renderer.color=new Color(.35f,1,.8f);shot.Renderer.flipX=target.x>origin.x;}
            float travel=Mathf.Clamp(Vector2.Distance(origin,target)/18,.14f,.75f),time=0;
            while(time<travel&&StoryValid(current))
            {
                time+=StoryDelta;if(shot?.Transform!=null)shot.Transform.position=Vector2.Lerp(origin,target,Mathf.Clamp01(time/travel));yield return null;
            }
            if(!StoryValid(current))yield break;
            cinematic.Remove(shot);CinematicStage="DragonEliminated";boss.EliminateForStory();BossWasDefeated=true;AddScore(1000);
            RainLevelAudio.Play("explosion");
            var explosion=cinematic.Create("Explosion","FireBlast",target,3);
            time=0;
            while(time<.95f&&StoryValid(current))
            {float dt=StoryDelta;time+=dt;boss.AdvanceStoryDeath(dt);yield return null;}
            if(!StoryValid(current))yield break;
            cinematic.Remove(explosion);boss.HideForStory();
            CinematicStage="Refill";player.BlueHealth.ResetHealth();player.RedHealth.ResetHealth();axlBoss.Health.ResetHealth();
            axlBoss.Face(player.transform.position.x<axlBoss.transform.position.x);axlBoss.PlayPresentation("Idle");
            MessageRemaining=0;yield return StoryWait(.5f,current);if(!StoryValid(current))yield break;
            CinematicStage="AxlDialogue";
            bool dialogueDone=false;narrative.PlayConfrontation(false,()=>dialogueDone=true);
            while(!dialogueDone&&StoryValid(current))yield return null;
            if(!StoryValid(current))yield break;
            CinematicStage="AxlWarning";Notify("WARNING · AXL",2.5f);RainLevelAudio.Play("warning");
            yield return StoryWait(2.5f,current);if(!StoryValid(current))yield break;
            CinematicStage="AxlCombat";bossStory=null;Phase=RainLevelPhase.AxlBoss;player.SetCinematicIdle(false);axlBoss.BeginFight();
            Notify("AXL · rompe sus ráfagas y esquiva su embestida",4);
        }

        public void AxlBossYielded()
        {
            if(Phase!=RainLevelPhase.AxlBoss||axlBoss==null||bossStory!=null)return;
            Phase=RainLevelPhase.Ending;CinematicStage="AxlYield";
            EndTransformationPause();axlBoss.HoldAtOne();RainProjectile.ClearTeam(DamageTeam.Enemy);RainProjectile.ClearTeam(DamageTeam.Player);
            player.Teleport(new Vector2(Mathf.Clamp(player.transform.position.x,239,257),0),false);player.SetCinematicIdle(true);
            player.SetCinematicFacing(axlBoss.transform.position.x<player.transform.position.x);
            CompanionForm=player.CurrentForm==HeroForm.Blue?HeroForm.Red:HeroForm.Blue;
            bossStory=StartCoroutine(FinalEncounterEnding(attempt));
        }

        private IEnumerator FinalEncounterEnding(int current)
        {
            yield return StoryWait(.65f,current);if(!StoryValid(current))yield break;
            CinematicStage="CompanionArrival";string unit=CompanionForm==HeroForm.Blue?"Blue":"Red";
            string arrival=CompanionForm==HeroForm.Blue?"TeleportArrival":"TeleportIn";
            float partnerX=Mathf.Clamp(player.transform.position.x+(player.FacingLeft?-1.15f:1.15f),239,258.5f);
            companion=cinematic.Create(unit,arrival,new Vector2(partnerX,0),2);
            RainLevelAudio.Play("arrival");
            if(companion!=null)companion.Renderer.flipX=player.FacingLeft;
            yield return StoryWait(Mathf.Max(.6f,assets.Find(unit,arrival)?.clip.length??.6f),current);if(!StoryValid(current))yield break;
            cinematic.Play(companion,unit,"Idle",true);CinematicStage="HeroesTogether";
            axlBoss.PlayPresentation("Idle");Notify("X y Zero · juntos hasta el final",2);
            yield return StoryWait(.4f,current);if(!StoryValid(current))yield break;
            MessageRemaining=0;CinematicStage="AxlEndingDialogue";
            bool dialogueDone=false;narrative.PlayConfrontation(true,()=>{dialogueDone=true;RainLevelAudio.SetConversation(true);});
            while(!dialogueDone&&StoryValid(current))yield return null;
            if(!StoryValid(current))yield break;
            CinematicStage="AxlEscape";axlBoss.BeginEscape();
            while(axlBoss.IsPresenting&&StoryValid(current))yield return null;
            if(!StoryValid(current))yield break;
            AddScore(3000);EncounterWon?.Invoke();yield return StoryWait(.35f,current);if(!StoryValid(current))yield break;
            CinematicStage="HeroesDeparture";bool heroDeparted=false;
            player.BeginDeparture(()=>{if(StoryValid(current))heroDeparted=true;});
            // Both remain together during the salute, then use their own original departure sequences.
            yield return StoryWait(.8f,current);if(!StoryValid(current))yield break;
            string departure=CompanionForm==HeroForm.Blue?"TeleportDeparture":"TeleportOut";
            cinematic.Play(companion,unit,departure);
            yield return StoryWait(assets.Find(unit,departure)?.clip.length??.7f,current);if(!StoryValid(current))yield break;
            if(companion?.Object!=null)companion.Object.SetActive(false);
            while(!heroDeparted&&StoryValid(current))yield return null;
            if(!StoryValid(current))yield break;
            cinematic.Clear();companion=null;EndingCompleted=true;bossStory=null;FinishLevel();
        }

        public void EnemyDefeated(Vector2 position)
        {
            DefeatedEnemies++;AddScore(100);
            // A fresh run has its own random stream; authored route rewards remain independent.
            if(drops==null){RunSeed=BitConverter.ToInt32(Guid.NewGuid().ToByteArray(),0);drops=new System.Random(RunSeed);}
            RainPickupKind? reward=SelectEnemyDrop(drops.NextDouble());
            if(reward.HasValue)SpawnPickup(reward.Value,position);
        }
        /// <summary>65% score, 25% nothing, 10% small health. No random life or large-health drop.</summary>
        public static RainPickupKind? SelectEnemyDrop(double roll)=>roll<.65?RainPickupKind.ScoreOrb:roll<.90?(RainPickupKind?)null:RainPickupKind.SmallHealth;
        public RainPickup SpawnPickup(RainPickupKind kind,Vector2 position)
        {
            var item=new GameObject(kind.ToString()+" pickup").AddComponent<RainPickup>();item.Configure(this,kind,position);return item;
        }
        public RainPickup SpawnPickup(RainPickupKind kind,Vector2 position,string persistentId)
        {
            if(WasPickupCollected(persistentId))return null;
            var item=new GameObject(kind.ToString()+" reward - "+persistentId).AddComponent<RainPickup>();
            item.Configure(this,kind,position,persistentId);return item;
        }
        public bool WasPickupCollected(string id)=>!string.IsNullOrEmpty(id)&&collectedPickupIds.Contains(id);
        public bool RecordPickupCollected(string id)=>string.IsNullOrEmpty(id)||collectedPickupIds.Add(id);
        public void AddScore(int amount){if(amount>0)Score=(int)Math.Min(int.MaxValue,(long)Score+amount);}
        public void AddLife(int amount=1){if(amount>0)Lives=Mathf.Clamp(Lives+amount,0,99);}
        public void HeroDefeated()
        {
            if(!HasStarted||Phase==RainLevelPhase.Dead||Phase==RainLevelPhase.Won||Phase==RainLevelPhase.FrontEnd)return;
            if(player.OtherHealth.IsAlive){player.SwitchForm();Notify("Cambio de emergencia · "+ActiveHeroName+" continúa el combate",3);return;}
            LoseLifeAndRespawn();
        }
        public void LoseLifeAndRespawn()
        {
            if(!HasStarted||Phase==RainLevelPhase.Dead||Phase==RainLevelPhase.Won||Phase==RainLevelPhase.FrontEnd)return;
            CancelBossIntroduction();EndTransformationPause();SetPaused(false);Lives=Mathf.Max(0,Lives-1);Phase=RainLevelPhase.Dead;deathTimer=1.6f;player.PlayDefeat();
            Notify(IsGameOver?"GAME OVER · se agotaron las vidas":"VIDA PERDIDA · quedan "+Lives,IsGameOver?999:2);
        }
        private void CancelBossIntroduction()
        {
            attempt++;
            campaignArrivalPending=false;if(controlsGuide!=null)controlsGuide.Cancel();
            if(narrative!=null)narrative.Cancel();
            if(bossIntroduction!=null){StopCoroutine(bossIntroduction);bossIntroduction=null;}
            if(bossStory!=null){StopCoroutine(bossStory);bossStory=null;}
            if(cinematic!=null)cinematic.Clear();companion=null;CinematicStage="None";
            if(player!=null)player.SetCinematicIdle(false);
        }
        private void ResetAttempt()
        {
            CancelBossIntroduction();EndTransformationPause();ready.Reset();boss.ResetBoss();if(axlBoss!=null)axlBoss.ResetBoss();BossWasDefeated=false;EndingCompleted=false;
            foreach(var enemy in FindObjectsByType<RainEnemy>(FindObjectsSortMode.None))enemy.ResetEnemy();
            foreach(var shot in FindObjectsByType<RainProjectile>(FindObjectsSortMode.None))shot.DestroyProjectile();
            foreach(var item in FindObjectsByType<RainPickup>(FindObjectsSortMode.None))Destroy(item.gameObject);
            foreach(var world in FindObjectsByType<RainLevelWorld>(FindObjectsSortMode.None))world.ResetWorld();
        }
        /// <summary>Manual checkpoint retry and QA reset; actual defeats/falls go through LoseLifeAndRespawn.</summary>
        public void Respawn()
        {
            if(!HasStarted){StartNewGame(initialForm);return;}
            if(IsGameOver){RestartLevel();return;}
            SetPaused(false);ResetAttempt();player.Teleport(checkpoint,true);Phase=RainLevelPhase.Intro;
            int current=attempt;player.BeginEntrance(()=>{if(current!=attempt)return;Phase=RainLevelPhase.Playing;Notify("CHECKPOINT · "+CheckpointName,3);});
        }
        public void RestartLevel()=>StartNewGame(initialForm);
        private void OnDestroy()
        {
            if(ready!=null)ready.Completed-=OnReadyCompleted;
            if(boss!=null&&boss.Health!=null)boss.Health.Changed-=OnDragonHealthChanged;
            if(axlBoss!=null&&axlBoss.Health!=null)axlBoss.Health.Changed-=OnAxlHealthChanged;
            if(Instance==this){Time.timeScale=1;Instance=null;}
        }
    }
}
