using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MegaManX9.EditorTools
{
    /// <summary>Builds the authored level and tests physics, combat and session transitions in the real player loop.</summary>
    public static class RainLevelValidation
    {
        private const string Request="Tools/LevelPipeline/build_and_test.request";
        private const string CoverRequest="Tools/LevelPipeline/capture_cover.request";
        private const string PresentationRequest="Tools/LevelPipeline/capture_presentation.request";
        private const string PendingPlay="RainLevelValidation.PendingPlay";
        private const string PendingReport="RainLevelValidation.PendingReport";
        private static double nextDiagnostic;
        private static bool queued,running;
        private static Report report;
        private static RainEnemy[] originalEnemies;
        [Serializable] private sealed class Report
        {
            public bool success;public string scene=RainLevelBuilder.ScenePath;
            public int enemies,ladders,checkpoints,rainTiles;
            public float movementDistance,jumpHeight;
            public int audioBufferWarnings;
            public string[] checks,screenshots,observedAnimations;
            public string error;
        }
        private static readonly List<string> checks=new List<string>(),captures=new List<string>();

        [InitializeOnLoadMethod]
        private static void Initialize(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;}
        private static void Poll()
        {
            if(queued||running||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(SessionState.GetBool(PendingPlay,false)&&EditorApplication.isPlaying){BeginNativeSuite();return;}
            if(File.Exists(PresentationRequest)&&EditorApplication.isPlaying)
            {
                var game=UnityEngine.Object.FindFirstObjectByType<RainLevelGame>();
                if(game==null||game.Narrative==null)return;
                File.Delete(PresentationRequest);running=true;game.StartCoroutine(Safe(PresentationSuite(game)));return;
            }
            if(File.Exists(CoverRequest))
            {
                if(!EditorApplication.isPlaying){if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.isPlaying=true;return;}
                var game=UnityEngine.Object.FindFirstObjectByType<RainLevelGame>();
                if(game==null)return;
                File.Delete(CoverRequest);game.StartCoroutine(RecaptureCover(game));return;
            }
            if(!File.Exists(Request))return;
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            queued=true;EditorApplication.delayCall+=()=>{queued=false;File.Delete(Request);BuildAndTest();};
        }
        private static IEnumerator RecaptureCover(RainLevelGame game)
        {
            yield return WaitUntil(()=>game.StartupVideo==null||game.StartupVideo.HasCompleted,22,"Startup movie has not finished before the cover capture.");
            game.ReturnToMainMenu();
            for(int i=0;i<4;i++)yield return null;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Previews/RainyFoundry/00_Title_Cover.png"));
            for(int i=0;i<4;i++)yield return null;
            Debug.Log("RAIN_LEVEL_COVER_CAPTURE_SAVED");
        }
        [MenuItem("Mega Man X9/Playable Rain Level/Build and Test Playable Level",priority=0)]
        public static void BuildAndTest()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Sal de Play antes de construir el nivel.");
            Directory.CreateDirectory("Tools/LevelPipeline");Directory.CreateDirectory("Previews/RainyFoundry");
            checks.Clear();captures.Clear();report=new Report();
            try
            {
                var activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(string.IsNullOrEmpty(activeScene.path)&&!activeScene.isDirty&&File.Exists(RainLevelBuilder.ScenePath))
                    EditorSceneManager.OpenScene(RainLevelBuilder.ScenePath,OpenSceneMode.Single);
                HealthHUDBuilder.Build();
                DragonRigBuilder.Build();
                RainLevelBuilder.Build();
                var scene=EditorSceneManager.OpenScene(RainLevelBuilder.ScenePath,OpenSceneMode.Single);
                var roots=scene.GetRootGameObjects();
                report.enemies=roots.SelectMany(r=>r.GetComponentsInChildren<RainEnemy>(true)).Count();
                report.ladders=roots.SelectMany(r=>r.GetComponentsInChildren<RainLadder>(true)).Count();
                var world=roots.SelectMany(r=>r.GetComponentsInChildren<RainLevelWorld>(true)).Single();report.checkpoints=world.Checkpoints.Length;
                Check(roots.SelectMany(r=>r.GetComponentsInChildren<RainDragonBoss>(true)).Count()==1,"Exactly one dragon boss");
                Check(roots.SelectMany(r=>r.GetComponentsInChildren<RainAxlBoss>(true)).Count()==1,"Original Axl is configured as the second boss");
                Check(!roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="ExitGate"||t.name=="Exit arch"),"The automatic story ending has no final exit gate");
                Check(roots.SelectMany(r=>r.GetComponentsInChildren<RainHeroController>(true)).Count()==1,"One hero body with two interchangeable forms");
                Check(report.enemies>=14&&report.ladders>=6&&report.checkpoints>=6,"Expanded authored enemies, vertical routes and checkpoints");
                var foundation=roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Where(r=>r.name=="Foundation cover").ToArray();
                Check(foundation.Length>=15&&foundation.All(r=>r.bounds.min.y<=-31.99f&&r.GetComponent<Collider2D>()==null),
                    "Painted foundations cover the zoomed-out lower view without changing colliders or pits");
                report.checks=checks.ToArray();SessionState.SetString(PendingReport,JsonUtility.ToJson(report));SessionState.SetBool(PendingPlay,true);
                EditorApplication.playModeStateChanged-=OnPlay;EditorApplication.playModeStateChanged+=OnPlay;
                EditorApplication.isPlaying=true;
            }
            catch(Exception error){Fail(error);}
        }
        private static void OnPlay(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            EditorApplication.playModeStateChanged-=OnPlay;
            EditorApplication.delayCall+=()=>
            {
                BeginNativeSuite();
            };
        }
        private static void BeginNativeSuite()
        {
            if(running||!SessionState.GetBool(PendingPlay,false))return;
            var game=UnityEngine.Object.FindFirstObjectByType<RainLevelGame>();
            if(game==null||game.FrontEnd==null)
            {
                if(EditorApplication.timeSinceStartup>nextDiagnostic){nextDiagnostic=EditorApplication.timeSinceStartup+10;Debug.Log("RAIN_LEVEL_WAITING_FOR_SESSION game="+(game!=null)+" frontEnd="+(game!=null&&game.FrontEnd!=null));}
                return;
            }
            if(report==null)
            {
                report=JsonUtility.FromJson<Report>(SessionState.GetString(PendingReport,"{}"));
                checks.Clear();captures.Clear();if(report.checks!=null)checks.AddRange(report.checks);
            }
            SessionState.SetBool(PendingPlay,false);running=true;game.StartCoroutine(Safe(Suite(game)));
            Application.logMessageReceived-=CaptureEngineWarnings;Application.logMessageReceived+=CaptureEngineWarnings;
            Debug.Log("RAIN_LEVEL_NATIVE_SUITE_STARTED");
        }
        private static void CaptureEngineWarnings(string message,string trace,LogType type)
        {if(report!=null&&type==LogType.Warning&&message.StartsWith("AudioSampleProvider buffer overflow"))report.audioBufferWarnings++;}
        private static IEnumerator Safe(IEnumerator main)
        {
            var stack=new Stack<IEnumerator>();stack.Push(main);
            while(stack.Count>0)
            {
                object current=null;bool moved=false;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception error){failure=error;}
                if(failure!=null){Fail(failure);yield break;}
                if(!moved){stack.Pop();continue;}
                if(current is IEnumerator child)stack.Push(child);else yield return current;
            }
            running=false;
        }
        private static void Check(bool condition,string name)
        {if(!condition)throw new InvalidOperationException(name);checks.Add(name);Debug.Log("RAIN_LEVEL_CHECK "+name);}
        private static IEnumerator WaitUntil(Func<bool> condition,float seconds,string failure)
        {
            float deadline=Time.realtimeSinceStartup+seconds;
            while(!condition()){if(Time.realtimeSinceStartup>deadline)throw new InvalidOperationException(failure);yield return null;}
        }
        private static IEnumerator Capture(RainLevelGame game,string name,bool showPausePanel=false)
        {
            var hud=game.GetComponent<RainLevelHUD>();if(hud!=null)hud.SuppressPausePanel=!showPausePanel;
            bool freeze=(game.Narrative==null||!game.Narrative.IsActive)&&(game.ControlsGuide==null||!game.ControlsGuide.IsActive);
            if(freeze)game.SetPaused(true);
            for(int i=0;i<4;i++)yield return null;
            string path="Previews/RainyFoundry/"+name+".png";ScreenCapture.CaptureScreenshot(Path.GetFullPath(path));captures.Add(path);
            for(int i=0;i<4;i++)yield return null;
            if(hud!=null)hud.SuppressPausePanel=false;
            if(freeze)game.SetPaused(false);
        }
        private static IEnumerator PresentationSuite(RainLevelGame game)
        {
            yield return WaitUntil(()=>game.StartupVideo==null||game.StartupVideo.HasCompleted,22,"Startup movie has not finished before presentation QA.");
            game.Player.SetTestInput(default);game.StartCampaign(HeroForm.Blue);
            yield return WaitUntil(()=>game.ControlsGuide.IsActive,2,"Presentation controls guide did not open.");yield return null;yield return null;
            game.ControlsGuide.Advance();yield return WaitUntil(()=>game.Narrative.IsActive,2,"Presentation guide did not open the introduction.");
            yield return Capture(game,"27b_Introduction_Black_Lead");
            var narrative=game.Narrative;
            for(int page=0;page<5;page++)
            {
                yield return WaitUntil(()=>!narrative.IsTransitioning&&narrative.CurrentPage==page,3,"Presentation introduction transition failed.");
                if(!narrative.IsTextComplete)narrative.Advance();yield return null;
                yield return Capture(game,"28_Introduction_"+(page+1));
                narrative.Advance();yield return null;
            }
            yield return WaitUntil(()=>game.Narrative.IsMissionArrival,10,"Presentation intro did not reach the arrival dialogue.");
            yield return CompleteDialogue(game,"37b_Axl_Signal_Arrival_X",true);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,3,"Presentation arrival did not release the level.");
            var audio=game.GetComponent<RainLevelAudio>();
            game.Player.Teleport(new Vector2(4,1),true);
            yield return WaitUntil(()=>audio.LastPlayedCue=="land",2,"Landing fallback did not play.");
            Check(audio.EffectsSource.isPlaying&&!audio.HasImportedCue("land"),"Landing plays its generated fallback through the effects source");
            game.Player.SetTestInput(new RainHeroInput{specialPressed=true});yield return null;game.Player.SetTestInput(default);
            yield return WaitUntil(()=>audio.LastPlayedCue=="chargedShot",2,"X energy special did not use the charged-shot WAV.");
            Check(audio.EffectsSource.isPlaying&&audio.HasImportedCue("chargedShot"),"X energy special uses the supplied charged-shot WAV");
            game.Player.Teleport(new Vector2(4,0),true);
            var audioEnemyObject=new GameObject("QA enemy audio");audioEnemyObject.transform.position=new Vector2(7,0);
            var audioEnemy=audioEnemyObject.AddComponent<RainEnemy>();audioEnemy.Configure(game.Assets,RainEnemy.Kind.Gunner,"GrenadeMan",7,7.2f);
            yield return WaitUntil(()=>audio.LastPlayedCue=="enemyShot",4,"Enemy projectile audio did not play.");
            Check(audio.EffectsSource.isPlaying&&!audio.HasImportedCue("enemyShot"),"Real enemy fire plays its generated shot sound");
            UnityEngine.Object.Destroy(audioEnemyObject);RainProjectile.ClearTeam(DamageTeam.Enemy);
            game.Player.Teleport(new Vector2(RainLevelWorld.ArenaLeft+3,0),true);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Boss,8,"Presentation dragon did not arrive.");
            game.Boss.TakeHit(10000,Vector2.right,game.Player.gameObject);
            yield return WaitUntil(()=>narrative.IsActive,7,"Presentation confrontation did not open.");
            yield return CompleteDialogue(game,"29_Axl_Confrontation",true);
            yield return WaitUntil(()=>game.IsBossWarning,2,"Presentation Axl WARNING missing.");
            yield return Capture(game,"30_Axl_WARNING");
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.AxlBoss,5,"Presentation Axl fight missing.");
            yield return new WaitForSeconds(.4f);game.AxlBoss.TakeHit(10000,Vector2.right,game.Player.gameObject);
            yield return WaitUntil(()=>narrative.IsActive,6,"Presentation ending dialogue missing.");
            yield return CompleteDialogue(game,"31_Axl_Final_Dialogue",true);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Won,10,"Presentation ending did not reach results.");
            yield return WaitUntil(()=>game.GetComponent<RainLevelAudio>().CurrentMusicCue=="resultsTheme",2,"Presentation results audio missing.");
            yield return Capture(game,"06_Mission_Complete");
            game.Player.ClearTestInput();game.ReturnToMainMenu();
            File.WriteAllText("Tools/LevelPipeline/presentation_report.json","{\"success\":true,\"audioChecks\":3,\"scope\":\"Generated landing, X special and enemy fire; introduction, mugshot dialogues, Axl warning, outlined results and return to cover\"}");
            Debug.Log("RAIN_LEVEL_PRESENTATION_SUCCESS");
        }
        private static IEnumerator Suite(RainLevelGame game)
        {
            var hero=game.Player;
            hero.SetTestInput(default);
            yield return StartupMoviePath(game);
            yield return FrontEndPath(game);
            int oldSeed=game.RunSeed;
            game.StartNewGame(HeroForm.Blue);
            Check(game.RunSeed!=oldSeed,"Each new run uses a fresh random drop seed");
            int pointDrops=0,healthDrops=0,emptyDrops=0;
            for(int i=0;i<1000;i++)
            {var drop=RainLevelGame.SelectEnemyDrop((i+.5)/1000d);if(drop==RainPickupKind.ScoreOrb)pointDrops++;else if(drop==RainPickupKind.SmallHealth)healthDrops++;else if(!drop.HasValue)emptyDrops++;}
            Check(pointDrops==650&&healthDrops==100&&emptyDrops==250,"Drop policy favors points at 65 percent with only 10 percent small health");
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,9,"READY and materialization did not finish.");
            Check(hero.Visual.Renderer.enabled&&hero.Visual.Renderer.sprite!=null,"READY and full materialization completed in the real player loop");
            Check(hero.BlueHealth.CurrentHealth==28&&hero.RedHealth.CurrentHealth==28,"Both forms start with independent 28 HP");
            var weather=UnityEngine.Object.FindFirstObjectByType<RainWeather>();report.rainTiles=weather.ActiveTileCount;
            Check(report.rainTiles==1&&weather.IsContinuous&&weather.SurfaceOcclusionEnabled,"Rain is one continuous layer with terrain occlusion");
            Check(weather.SampleRainVisibleAt(new Vector2(20,.3f))&&!weather.SampleRainVisibleAt(new Vector2(20,-.3f)),"Rain stops at the exposed floor instead of passing through it");
            Check(weather.SampleRainVisibleAt(new Vector2(15,1))&&!weather.SampleRainVisibleAt(new Vector2(15,1.6f))&&weather.SampleRainVisibleAt(new Vector2(15,2.1f)),"Rain stays in open air below platforms without crossing their solid volume");
            Check(weather.SampleRainVisibleAt(new Vector2(252,3))&&!weather.SampleRainVisibleAt(new Vector2(252,10.8f)),"Rain remains in the arena air and is clipped inside its upper slab");
            var nativeDecor=game.gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SpriteRenderer>()).ToArray();
            Check(nativeDecor.Count(s=>s.name.StartsWith("Original building - "))>=20&&nativeDecor.Count(s=>s.name=="Original street lamp")>=8,
                "Supplied building and street-lamp sprites replace the procedural skyline structures");
            Check(nativeDecor.Any(s=>s.sprite!=null&&s.sprite.name.StartsWith("Spike")),"Supplied spike sprites replace the hazard rods");
            game.Player.Teleport(new Vector2(79,0),true);game.Player.SetTestInput(default);yield return new WaitForSeconds(.2f);
            yield return Capture(game,"40_Original_Environment");
            game.Player.Teleport(new Vector2(90.2f,-1.2f),true);yield return new WaitForSeconds(.2f);
            yield return Capture(game,"41_Original_Spikes");game.Player.Teleport(new Vector2(4,0),true);
            var presentation=RainPresentationSettings.Load();var worldPresentation=UnityEngine.Object.FindAnyObjectByType<RainLevelWorld>();var hudPresentation=game.GetComponent<RainLevelHUD>();
            Check(!hudPresentation.ShowsGameplayHelpers&&!worldPresentation.Checkpoints.Any(p=>p.beacon!=null&&p.beacon.enabled),"Production keeps only the health HUD and hides checkpoint beacons");
            presentation.SetMode(RainPresentationMode.Development);yield return null;yield return null;
            Check(hudPresentation.ShowsGameplayHelpers&&worldPresentation.Checkpoints.All(p=>p.beacon==null||p.beacon.enabled),"Development restores controls and checkpoint helpers through the global setting");
            yield return Capture(game,"38_Development_Overlay");presentation.SetMode(RainPresentationMode.Production);yield return null;yield return null;
            originalEnemies=UnityEngine.Object.FindObjectsByType<RainEnemy>(FindObjectsSortMode.None);
            foreach(var enemy in originalEnemies)enemy.enabled=false;
            yield return Capture(game,"01_Rooftops_Rain_Blue");

            float start=hero.transform.position.x;
            hero.SetTestInput(new RainHeroInput{move=Vector2.right});yield return new WaitForSeconds(.55f);
            report.movementDistance=hero.transform.position.x-start;
            Check(report.movementDistance>1.5f&&report.movementDistance<2.1f,"Reduced running speed advances the real body at a controlled pace");
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpPressed=true,jumpHeld=true});yield return null;
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true});yield return new WaitForSeconds(.22f);
            Vector3 before=hero.transform.position;Vector2 velocity=hero.Velocity;HeroForm oldForm=hero.CurrentForm;
            Check(!hero.IsGrounded&&before.y>.8f,"Jump is physically airborne");report.jumpHeight=before.y;
            Check(hero.SwitchForm()&&hero.CurrentForm!=oldForm&&(hero.transform.position-before).sqrMagnitude<.00001f&&(hero.Velocity-velocity).sqrMagnitude<.00001f,
                "Mid-air switching preserves position and velocity");
            float switchClock=game.Elapsed;
            yield return new WaitForSecondsRealtime(.25f);
            Check(game.IsTransformationPaused&&Vector3.Distance(hero.transform.position,before)<.001f&&Mathf.Approximately(switchClock,game.Elapsed),
                "Switching briefly freezes the world and level clock while its rays animate");
            yield return Capture(game,"02a_Switch_Pause_Beams");
            yield return WaitUntil(()=>!hero.IsTransforming,4,"Switch pause did not release.");
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true,dashPressed=true});yield return null;
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true});yield return new WaitForSeconds(.08f);
            Check(hero.IsDashing&&hero.Velocity.x>6,"Air dash changes the real horizontal velocity");
            yield return Capture(game,"02_Red_Air_Dash");
            hero.SetTestInput(default);yield return new WaitForSeconds(1.3f);
            hero.Teleport(new Vector2(4,0),true);if(hero.CurrentForm!=HeroForm.Blue)hero.SwitchForm();yield return WaitUntil(()=>!hero.IsTransforming,3,"Blue switching beam did not finish.");yield return new WaitForSeconds(.12f);

            var dummyObject=new GameObject("QA combat enemy");dummyObject.transform.position=new Vector3(7,0,0);
            var dummy=dummyObject.AddComponent<RainEnemy>();dummy.Configure(game.Assets,RainEnemy.Kind.Gunner,"GrenadeMan",7,7.2f);
            yield return null;int hp=dummy.Health.CurrentHealth;
            hero.SetTestInput(new RainHeroInput{attackPressed=true});yield return null;hero.SetTestInput(default);
            yield return new WaitForSeconds(.45f);
            Check(dummy.Health.CurrentHealth<hp,"Buster projectile hits a real enemy collider and applies damage");
            int rangedHP=dummy.Health.CurrentHealth;
            hero.Teleport(new Vector2(6.25f,0),false);hero.SwitchForm();yield return WaitUntil(()=>!hero.IsTransforming,3,"Red switching beam did not finish.");yield return new WaitForSeconds(.08f);
            hero.SetTestInput(new RainHeroInput{attackPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSeconds(.34f);
            Check(dummy.Health.CurrentHealth<rangedHP,"Red saber hit window damages a real enemy");
            if(dummy.IsAlive){hero.SetTestInput(new RainHeroInput{attackPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSeconds(.4f);}
            Check(!dummy.IsAlive&&game.DefeatedEnemies>0&&game.Score>=100,"Defeating an enemy updates both score and objective counter");
            UnityEngine.Object.Destroy(dummyObject);
            yield return HeroPresentation(game);
            yield return CrouchAndUltimateRevision(game);
            yield return DirectionalArt(game);
            yield return PlatformingRoute(game);

            var ladder=UnityEngine.Object.FindObjectsByType<RainLadder>(FindObjectsSortMode.None).OrderBy(l=>l.CenterX).First();
            hero.Teleport(new Vector2(ladder.CenterX,ladder.Bottom+.02f),true);
            hero.SetTestInput(new RainHeroInput{move=Vector2.up});yield return new WaitForSeconds(.25f);
            Check(hero.IsOnLadder,"Ladder accepts physical movement input");
            before=hero.transform.position;hero.SwitchForm();
            Check(hero.IsOnLadder&&(hero.transform.position-before).sqrMagnitude<.00001f,"Switching on a ladder preserves traversal state");
            yield return Capture(game,"03_Maintenance_Ladder");
            hero.SetTestInput(new RainHeroInput{move=Vector2.up});
            yield return WaitUntil(()=>!hero.IsOnLadder,4,"Ladder exit was blocked by the platform collider.");
            Check(hero.transform.position.y>=ladder.Top-.3f,"Ladder dismount clears the platform and retains the hero collider");
            hero.SetTestInput(default);yield return new WaitForSeconds(.55f);
            Check(hero.IsGrounded&&Mathf.Abs(hero.transform.position.y-ladder.Top)<.12f,"Ladder exit lands stably without requiring a timed sideways input");
            hero.Teleport(new Vector2(48.4f,1.3f),true);hero.SetTestInput(new RainHeroInput{move=Vector2.right});yield return new WaitForSeconds(.16f);
            Check(hero.IsWallSliding,"Holding toward a real wall enters wall slide");
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpPressed=true,jumpHeld=true});yield return null;
            hero.SetTestInput(new RainHeroInput{jumpHeld=true});yield return new WaitForSeconds(.08f);
            Check(hero.Velocity.x<-.5f&&hero.Velocity.y>5,"Wall jump pushes away from the collider and rises");
            yield return Capture(game,"03b_Wall_Jump");
            hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(new RainHeroInput{move=Vector2.right});yield return new WaitForSeconds(.06f);
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,dashPressed=true,jumpHeld=true});yield return null;
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpPressed=true,jumpHeld=true});yield return null;
            hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true});yield return new WaitForSeconds(.10f);
            Check(!hero.IsGrounded&&hero.Velocity.x>6.5f,"Ground dash jump retains its reduced momentum in the air");hero.SetTestInput(default);

            var world=UnityEngine.Object.FindFirstObjectByType<RainLevelWorld>();var checkpoint=world.Checkpoints[2];
            hero.Teleport(checkpoint.spawn,true);yield return new WaitForSeconds(.15f);
            Check(game.CheckpointName==checkpoint.label,"Entering the authored checkpoint stores a safe return location");
            hero.Teleport(new Vector2(130,-2),false);game.Respawn();
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,5,"Checkpoint entrance did not complete.");
            Check(Vector2.Distance(hero.transform.position,checkpoint.spawn)<.15f,"Respawn returns to checkpoint with both health pools restored");
            hero.Teleport(checkpoint.spawn,true);yield return new WaitForSeconds(1.1f);
            int blue=hero.BlueHealth.CurrentHealth,red=hero.RedHealth.CurrentHealth;
            hero.TakeDamage(4,Vector2.up);int damaged=hero.ActiveHealth.CurrentHealth;
            hero.SwitchForm();
            Check(damaged==24&&(hero.BlueHealth.CurrentHealth==blue-4||hero.RedHealth.CurrentHealth==red-4)&&hero.ActiveHealth.CurrentHealth==28,
                "Damage stays with its character when switching");
            yield return Capture(game,"04_Foundry_Health_HUD");
            Vector3 frozenPosition=hero.transform.position;float elapsed=game.Elapsed;game.SetPaused(true);
            for(int i=0;i<8;i++)yield return null;
            Check(Vector3.Distance(frozenPosition,hero.transform.position)<.001f&&Mathf.Approximately(elapsed,game.Elapsed),"Pause freezes physics and the level clock");game.SetPaused(false);
            yield return Capture(game,"21_Pause_Statistics",true);
            var pauseHUD=game.GetComponent<RainLevelHUD>();
            Check(pauseHUD.UsingSuppliedPauseArt&&pauseHUD.UsesSuppliedPauseFont,"Pause displays the supplied panels and bitmap lettering");

            yield return LivesAndPickups(game);
            yield return JumpAndVoidCamera(game);
            hero.Teleport(new Vector2(RainLevelWorld.BossDoorX-1,0),true);hero.SetTestInput(default);
            yield return WaitUntil(()=>world.DoorIsOpen&&world.DoorProgress>.99f,3,"Original boss door failed to open.");
            Check(world.DoorProgress>.99f,"Animated boss door opens before entering the arena");
            yield return Capture(game,"12_Boss_Door");
            hero.Teleport(new Vector2(RainLevelWorld.ArenaLeft+3,0),true);hero.SetTestInput(default);yield return null;
            Check(game.Phase==RainLevelPhase.BossIntro&&!game.Boss.IsActive&&world.ArenaSealed,"WARNING locks the arena before the boss appears");
            yield return WaitUntil(()=>game.GetComponent<RainLevelAudio>().CurrentMusicCue=="dragonTheme",1.2f,"Dragon music did not start during WARNING.");
            Check(game.IsBossWarning&&game.GetComponent<RainLevelHUD>().UsingBossSkull,"Dragon WARNING starts its theme and the original boss skull is configured");
            yield return Capture(game,"13_WARNING");
            yield return new WaitForSeconds(2.6f);
            Check(game.Boss.transform.position.y>1&&!game.Boss.IsActive,"Dragon descends from above before combat starts");
            yield return Capture(game,"14_Dragon_Entrance");
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Boss,5,"Boss landing did not start the dragon encounter.");
            Check(game.Boss.IsActive&&world.ArenaSealed,"Arena closes and activates the single dragon boss");
            Check(game.Boss.Health.MaxHealth>=240,"Boss health is greatly increased to 240 HP");
            yield return DragonPoseDirections(game);
            yield return GroundWaveTraversal(game);
            before=hero.transform.position;hero.SwitchForm();Check((hero.transform.position-before).sqrMagnitude<.00001f,"Switching remains available during the boss encounter");
            if(hero.CurrentForm!=HeroForm.Blue)hero.SwitchForm();
            yield return WaitUntil(()=>!hero.IsTransforming,3,"Switching inside the arena did not complete.");
            int bossHP=game.Boss.Health.CurrentHealth;
            for(int i=0;i<16;i++)
            {
                hero.SetTestInput(new RainHeroInput{attackPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSeconds(.23f);
            }
            Check(game.Boss.Health.CurrentHealth<bossHP,"Hero projectiles damage the moving articulated boss");
            yield return Capture(game,"05_Dragon_Arena_Rain");
            yield return AxlStoryAndEnding(game,HeroForm.Red,true);
            yield return ResultsReturnPath(game);
            yield return AlternateEndingAndCancellation(game);
            game.RestartLevel();
            hero.SetTestInput(new RainHeroInput{armorPressed=true});yield return null;hero.SetTestInput(default);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,9,"Restarted entrance did not finish after armor change.");
            yield return new WaitForSeconds(.9f);
            Check(hero.Visual.Renderer.enabled&&!hero.IsEntering,"Restart cancels the previous departure and keeps the new hero visible");
            Check(hero.UltimateArmor&&hero.Visual.Unit=="BlueUltimate","Armor changes during materialization finish in the correct bank");
            hero.SetTestInput(new RainHeroInput{armorPressed=true});yield return null;hero.SetTestInput(default);
            yield return KeyboardPath(game);
            Check(report.audioBufferWarnings==0,"Startup audio decoder finishes without AudioSampleProvider buffer overflow");
            Application.logMessageReceived-=CaptureEngineWarnings;
            report.observedAnimations=hero.Visual.PlayedClips.OrderBy(s=>s).ToArray();report.success=true;SaveReport();
            Debug.Log("RAIN_LEVEL_PLAYTEST_SUCCESS "+checks.Count+" native integration checks.");
            foreach(var enemy in originalEnemies)if(enemy!=null)enemy.enabled=true;
            hero.ClearTestInput();game.ReturnToMainMenu();
        }
        private static IEnumerator DragonPoseDirections(RainLevelGame game)
        {
            var boss=game.Boss;
            var driver=boss.GetComponent<MegaManX9.Animation.DragonBossAttackPreview>();
            var rig=boss.GetComponent<MegaManX9.Animation.DragonRigActor>();
            bool wasEnabled=boss.enabled,wasDamaging=driver.DamageEnabled;
            boss.enabled=false;driver.DamageEnabled=false;
            string[] attacks={"Crouch","Landing","GroundFireWave","FireBreath","Fireball","RisingFlame","FlamingKick"};
            string[] poses={"Crouch","CrouchShift","SweepLow","CrouchAttack","Slash","JumpAttack","KickForward"};
            float[] samples={.5f,.25f,.5f,.5f,.55f,.4f,.5f};
            try
            {
                foreach(bool left in new[]{true,false})
                {
                    rig.FaceLeft(left);
                    for(int index=0;index<attacks.Length;index++)
                    {
                        Check(driver.BeginAttack(attacks[index]),"Dragon direction test can sample "+attacks[index]);
                        driver.Seek(driver.Duration*samples[index]);
                        Check(driver.CurrentAuthoredPoseName==poses[index]&&driver.AuthoredArtFlipped==(index<2),
                            "Dragon "+poses[index]+" uses its reviewed artwork orientation facing "+(left?"left":"right"));
                        if(index==0||index==3)yield return Capture(game,"39_Dragon_"+poses[index]+(left?"_Left":"_Right"));
                    }
                }
            }
            finally
            {
                driver.ClearEffects();driver.BeginAttack("CombatIdle");driver.DamageEnabled=wasDamaging;
                boss.enabled=wasEnabled;rig.FaceLeft(game.Player.transform.position.x<boss.transform.position.x);
            }
        }
        private static IEnumerator StartupMoviePath(RainLevelGame game)
        {
            yield return WaitUntil(()=>game.StartupVideo!=null,3,"Startup movie component was not configured.");
            var movie=game.StartupVideo;
            Check(movie.IsPlaying&&!game.FrontEnd.IsOpen&&!game.HasStarted,"Original video plays before the cover and blocks the menus");
            yield return WaitUntil(()=>movie.IsPrepared||!string.IsNullOrEmpty(movie.Error),15,"Startup movie did not prepare.");
            Check(string.IsNullOrEmpty(movie.Error)&&movie.Duration>11&&movie.VideoClip!=null,"The supplied startup video is decoded with its original duration");
            var previous=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>("RainStartupValidationKeyboard");
            try
            {
                keyboard.MakeCurrent();yield return PressKey(keyboard,Key.Enter);yield return PressKey(keyboard,Key.Escape);
                Check(movie.IsPlaying&&!movie.HasCompleted&&!game.FrontEnd.IsOpen,"Enter and Escape cannot skip the startup video");
                yield return WaitUntil(()=>movie.CurrentFrame>1,10,"Startup video did not advance decoded frames while the editor was reloading.");
                yield return new WaitForSecondsRealtime(2.8f);
                Check(movie.CurrentFrame>1,"Startup video advances real decoded frames");
                var audio=game.GetComponent<RainLevelAudio>();
                Check(audio.MusicSource.volume<.001f&&audio.AmbienceSource.volume<.001f,"Only the movie soundtrack plays during startup");
                yield return Capture(game,"00a_Unskippable_Startup_Movie");
                yield return WaitUntil(()=>movie.HasCompleted,18,"Startup video did not finish through its decoder event.");
            Check(!movie.IsPlaying&&movie.DecoderStopped&&movie.UsesDirectAudio&&game.FrontEnd.Page==RainMenuPage.Cover,"Cover opens after playback and the direct-audio decoder is stopped");
            }
            finally{InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();}
        }
        private static IEnumerator FrontEndPath(RainLevelGame game)
        {
            yield return WaitUntil(()=>game.FrontEnd!=null,4,"Front end did not initialize.");
            var menu=game.FrontEnd;
            var audio=game.GetComponent<RainLevelAudio>();
            Check(audio!=null&&audio.ImportedClipCount>=33&&audio.MusicLoops,"Imported WAV bank and looping music source are available");
            Check(RainAudioAssets.Load().musicVolume>=.42f,"Music gain is increased from the previous mix");
            foreach(string cue in new[]{"mainTheme","levelTheme","dragonTheme","axlTheme","conversationTheme","resultsTheme","rain","jump","dash","shot","chargedShot","charge","saber1","saber2","saber3","hurt","death","arrival","departure","pickup","enemyDeath","dragonHurt","dragonAttack","axlHurt","ready","warning","doorOpen","doorClose","menuMove","menuConfirm","menuEnter"})
                Check(audio.HasImportedCue(cue),"Named WAV is connected: "+cue);
            var previous=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>("RainMenuValidationKeyboard");
            var originalBindings=RainInputBindings.CopyBindings();
            try
            {
            keyboard.MakeCurrent();yield return PressKey(keyboard,Key.None);
            Check(game.Phase==RainLevelPhase.FrontEnd&&menu.Page==RainMenuPage.Cover&&!game.Player.Visual.Renderer.enabled,"Cover opens before the playable mission");
            Check(!audio.AmbienceIsPlaying&&audio.AmbienceSource.volume<.001f,"Rain ambience is silent in the cover and menu");
            yield return Capture(game,"00_Title_Cover");
            yield return new WaitForSecondsRealtime(.1f);yield return PressKey(keyboard,Key.Enter);
            Check(menu.Page==RainMenuPage.Main,"Enter opens the darkened main menu from the cover");
            yield return Capture(game,"00b_Main_Menu");
            string original=JsonUtility.ToJson(RainInputBindings.CopyBindings());
            menu.OpenOptions();yield return new WaitForSecondsRealtime(.1f);
            for(int step=0;step<4;step++)yield return PressKey(keyboard,Key.DownArrow);
            yield return PressKey(keyboard,Key.Enter);
            Check(menu.OptionSelection==4&&menu.IsWaitingForKey&&menu.WaitingForAction==RainInputAction.Jump,"Options arrows and Enter open the selected action for rebinding");
            yield return PressKey(keyboard,Key.T);
            Check(menu.DraftBindings.Get(RainInputAction.Jump)==Key.T,"Options capture the requested keyboard binding");
            yield return Capture(game,"00c_Options");yield return PressKey(keyboard,Key.Escape);
            Check(JsonUtility.ToJson(RainInputBindings.CopyBindings())==original&&menu.Page==RainMenuPage.Main,"Leaving options discards uncommitted key changes");
            menu.OpenOptions();menu.DraftBindings.Set(RainInputAction.Jump,Key.T);yield return new WaitForSecondsRealtime(.1f);
            for(int step=0;step<3;step++)yield return PressKey(keyboard,Key.UpArrow);
            Check(menu.OptionSelection==13,"Options keyboard navigation reaches Apply and Back controls");yield return PressKey(keyboard,Key.Enter);
            Check(RainInputBindings.CopyBindings().Get(RainInputAction.Jump)==Key.T,"Applying options saves the requested controls");
            menu.SelectMenuOption(0);yield return new WaitForSecondsRealtime(.1f);yield return PressKey(keyboard,Key.RightArrow);
            Check(menu.SelectedHero==HeroForm.Red,"Character selection accepts keyboard navigation");
            yield return Capture(game,"00d_Character_Select");yield return PressKey(keyboard,Key.Enter);
            yield return WaitUntil(()=>game.ControlsGuide!=null&&game.ControlsGuide.IsActive,2,"Character selection did not open the supplied keyboard guide.");
            Check(game.ControlsGuide.DisplayedBindingCount==13&&game.ControlsGuide.HighlightedBindingCount>=13&&game.ControlsGuide.GetDisplayedKey(RainInputAction.Jump)==RainInputBindings.CopyBindings().Get(RainInputAction.Jump),"Keyboard guide marks the current thirteen configured controls");
            Check(!audio.AmbienceIsPlaying,"Controls guide has no rain ambience");
            yield return Capture(game,"36_Keyboard_Controls");RainInputBindings.Save(originalBindings);yield return PressKey(keyboard,Key.Enter);
            yield return WaitUntil(()=>game.Narrative!=null&&game.Narrative.IsActive,2,"Campaign did not start the new introduction.");
            Check(game.Phase==RainLevelPhase.Intro&&game.InputLocked&&!game.Player.Visual.Renderer.enabled,"Campaign begins with a short black transition and hidden hero");
            yield return Capture(game,"27b_Introduction_Black_Lead");
            yield return WaitUntil(()=>!game.Narrative.IsTransitioning,2,"Initial black transition lasted too long.");
            var narrative=game.Narrative;
            string[] orderedImages={"Cinematica uno","Cinematica dos","Cinematica tres","Cinematica cuatro"};
            for(int page=0;page<5;page++)
            {
                Check(narrative.CurrentPage==page,"Introduction page is presented in its intended order: "+page);
                if(page<4)Check(narrative.CurrentImage!=null&&narrative.CurrentImage.name==orderedImages[page],"Introduction uses the supplied ordered image: "+orderedImages[page]);
                int beforeWords=narrative.RevealedWordCount;
                yield return new WaitForSecondsRealtime(.16f);
                Check(narrative.RevealedWordCount>beforeWords||narrative.IsTextComplete,"Narrative reveals complete words progressively");
                yield return PressKey(keyboard,Key.Enter);
                Check(narrative.IsTextComplete&&narrative.CurrentPage==page,"First Enter completes the current explanation without changing image");
                yield return Capture(game,"28_Introduction_"+(page+1));
                Check(audio.CurrentMusicCue=="conversationTheme"&&audio.MusicLoops,"Introduction conversation WAV loops during its pages");
                yield return PressKey(keyboard,Key.Enter);
                if(page<4)yield return WaitUntil(()=>!narrative.IsTransitioning&&narrative.CurrentPage==page+1,2,"Introduction page transition did not finish.");
                else yield return WaitUntil(()=>!narrative.IsActive,2,"Introduction did not hand off to READY.");
            }
            yield return WaitUntil(()=>game.Narrative.IsMissionArrival,9,"Initial materialization did not open the arrival dialogue.");
            Check(game.Player.CinematicIdle&&game.Narrative.PageCount==2&&game.Narrative.ActiveSpeaker==RainNarrativeSpeaker.Zero,"Zero waits in idle for the two-turn Axl signal exchange");
            yield return CompleteDialogue(game,"37_Axl_Signal_Arrival",true);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,3,"Selected Red entrance did not complete.");
            Check(game.Player.CurrentForm==HeroForm.Red&&game.Player.Visual.Unit=="Red"&&game.Lives==3,"Character selection starts the chosen Red hero with shared lives");
            yield return WaitUntil(()=>audio.CurrentMusicCue=="levelTheme"&&audio.MusicIsPlaying,2,"Level music did not return after the arrival conversation.");
            Check(audio.CurrentMusicCue=="levelTheme"&&audio.MusicLoops&&audio.MusicIsPlaying,"Main level WAV is playing in a loop after the introduction");
            }
            finally
            {
                RainInputBindings.Save(originalBindings);InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();
            }
        }
        private static IEnumerator PressKey(Keyboard keyboard,Key key)
        {
            InputSystem.QueueStateEvent(keyboard,key==Key.None?new KeyboardState():new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
        }
        private static IEnumerator HeroPresentation(RainLevelGame game)
        {
            var hero=game.Player;hero.SetInitialForm(HeroForm.Blue);hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
            yield return new WaitForSeconds(8.3f);
            Check(hero.Visual.CurrentAnimation=="Idle"&&hero.ActiveHealth.CurrentHealth==28,"Healthy Blue keeps the breathing idle after eight seconds");
            hero.ActiveHealth.ApplyDamage(22);yield return null;yield return null;
            Check(hero.Visual.CurrentAnimation=="AlternateIdle","Tired Blue idle is reserved for low health");
            yield return Capture(game,"07_Low_Health_Idle");hero.Teleport(new Vector2(4,0),true);
            hero.SetTestInput(new RainHeroInput{armorPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSecondsRealtime(.50f);
            Check(hero.IsTransforming,"Ultimate armor uses a visible materialization sequence");
            yield return Capture(game,"08a_Ultimate_Blue_Beam");
            yield return WaitUntil(()=>!hero.IsTransforming,3,"Ultimate Blue armor did not finish materializing.");
            yield return new WaitForSeconds(8.3f);
            Check(hero.UltimateArmor&&hero.Visual.Unit=="BlueUltimate"&&hero.Visual.CurrentAnimation=="Idle","Healthy Ultimate Blue keeps its own quiet breathing idle");
            yield return Capture(game,"08b_Ultimate_Idle");
            for(int bank=0;bank<3;bank++)
            {
                if(bank!=1)hero.SetInitialForm(bank==2?HeroForm.Red:HeroForm.Blue);
                else
                {
                    hero.SetTestInput(new RainHeroInput{armorPressed=true});yield return null;hero.SetTestInput(default);
                    yield return WaitUntil(()=>!hero.IsTransforming,3,"Wall test armor did not finish.");
                }
                hero.Teleport(new Vector2(48.4f,1.3f),true);hero.SetTestInput(new RainHeroInput{move=Vector2.right});yield return new WaitForSeconds(.16f);
                Check(hero.IsWallSliding&&hero.Visual.HeldFrame==3&&hero.Visual.CurrentAnimation==(bank==2?"AirUppercutPose":"WallJump"),
                    "Reviewed fixed wall contact pose for "+hero.Visual.Unit);
                yield return Capture(game,"09_Wall_"+hero.Visual.Unit);
                hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);yield return new WaitForSeconds(.12f);
            }
            hero.SetInitialForm(HeroForm.Red);hero.Teleport(new Vector2(4,0),true);yield return new WaitForSeconds(.12f);
            hero.SetTestInput(new RainHeroInput{enhancedPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSecondsRealtime(.76f);
            yield return Capture(game,"10_Red_Power_Transformation");
            yield return WaitUntil(()=>!hero.IsTransforming,3,"Red power transformation did not finish.");
            Check(hero.EnhancedSaber&&hero.Visual.PlayedClips.Contains("RedEffects/FireUppercutPose"),"Red transformation uses the provided powered pose and red beam");
            yield return new WaitForSeconds(1.2f);
            Check(hero.EnhancedAuraActive&&hero.EnhancedAuraClip=="PinkFlameAura","Zero permanently keeps the requested pink flame aura without the ice ring");
            yield return Capture(game,"10b_Zero_Persistent_Aura");
            hero.SetTestInput(new RainHeroInput{specialPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSeconds(.08f);
            Check(hero.IsRedUltimate,"Powered Red begins the supplied ultimate sequence");
            yield return Capture(game,"11a_Red_Ultimate_Charge");
            yield return new WaitForSeconds(.27f);yield return Capture(game,"11b_Red_Ultimate_Energy");
            yield return new WaitForSeconds(.34f);
            Check(UnityEngine.Object.FindObjectsByType<RainProjectile>(FindObjectsSortMode.None).Any(p=>p.Owner==hero.gameObject&&p.Velocity.magnitude>9),"Red ultimate emits the large green saber wave");
            yield return Capture(game,"11c_Red_Ultimate_Wave");
            yield return WaitUntil(()=>!hero.IsRedUltimate,3,"Red ultimate did not recover.");
            hero.SetInitialForm(HeroForm.Blue);hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
        }
        private static IEnumerator CrouchAndUltimateRevision(RainLevelGame game)
        {
            var hero=game.Player;var audio=game.GetComponent<RainLevelAudio>();
            for(int bank=0;bank<4;bank++)
            {
                hero.SetInitialForm(bank<2?HeroForm.Blue:HeroForm.Red);hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
                if(bank==1||bank==3)
                {
                    hero.SetTestInput(new RainHeroInput{armorPressed=bank==1,enhancedPressed=bank==3});yield return null;hero.SetTestInput(default);
                    yield return WaitUntil(()=>!hero.IsTransforming,4,"Crouch form did not materialize.");
                }
                hero.SetTestInput(new RainHeroInput{move=Vector2.down});yield return new WaitForSeconds(.14f);
                string crouchClip=bank==0?"CrouchIdle":bank==1?"DefeatCollapse":bank==2?"DashSlash":"Enhanced_GroundSlideSlash";
                Check(hero.IsCrouching&&hero.Visual.HeldFrame==(bank==1?1:0)&&hero.Visual.CurrentAnimation==crouchClip&&hero.GetComponent<BoxCollider2D>().size.y<.5f,"Reviewed kneeling crouch and reduced hitbox are active for form "+bank);
                yield return Capture(game,"33_Crouch_Form_"+bank);
                if(bank<2)
                {
                    RainProjectile.ClearTeam(DamageTeam.Player);hero.SetTestInput(new RainHeroInput{move=Vector2.down,attackPressed=true});yield return null;
                    hero.SetTestInput(new RainHeroInput{move=Vector2.down});yield return new WaitForSeconds(.06f);
                    Check(hero.Visual.HeldFrame==(bank==1?1:0)&&UnityEngine.Object.FindObjectsByType<RainProjectile>(FindObjectsSortMode.None).Any(p=>p.Owner==hero.gameObject&&p.Velocity.x>10&&p.transform.position.y<hero.transform.position.y+.5f),"X can shoot forward from the kneeling muzzle in bank "+bank);
                    yield return Capture(game,"34_Crouch_Buster_Form_"+bank);
                }
            }
            for(int airborne=0;airborne<2;airborne++)
            for(int direction=1;direction>=-1;direction-=2)
            {
                hero.SetInitialForm(HeroForm.Red);hero.Teleport(new Vector2(4,0),true);
                hero.SetTestInput(new RainHeroInput{enhancedPressed=true});yield return null;hero.SetTestInput(default);
                yield return WaitUntil(()=>!hero.IsTransforming,4,"Zero ultimate form did not materialize.");
                RainProjectile.ClearTeam(DamageTeam.Player);hero.Teleport(new Vector2(4,airborne==1?2:0),true);
                hero.SetTestInput(new RainHeroInput{move=new Vector2(direction,0)});yield return new WaitForSeconds(.04f);
                hero.Visual.ClearPlayedClips();float started=Time.time;
                hero.SetTestInput(new RainHeroInput{move=new Vector2(direction,0),specialPressed=true});yield return null;
                hero.SetTestInput(new RainHeroInput{move=new Vector2(-direction,0)});yield return new WaitForSeconds(.06f);
                Check(hero.IsRedUltimate&&hero.Visual.CurrentAnimation=="AirPoseRaised"&&hero.FacingLeft==(direction<0)&&hero.Visual.Renderer.flipX==(direction>0),"Zero ultimate keeps the correct facing in ground/air case "+airborne+" direction "+direction);
                yield return Capture(game,"35_Zero_Ultimate_"+airborne+"_"+direction);
                hero.SetTestInput(default);
                RainProjectile wave=null;
                yield return WaitUntil(()=>{wave=UnityEngine.Object.FindObjectsByType<RainProjectile>(FindObjectsSortMode.None).FirstOrDefault(p=>p.Owner==hero.gameObject&&p.AnimationName=="LargeSaberWave"&&p.Velocity.magnitude>9);return wave!=null;},1,"Zero ultimate wave was not emitted quickly.");
                Check(Time.time-started<.9f&&wave.Velocity.x*direction>9&&!hero.Visual.PlayedClips.Contains("Red/Enhanced_SpinSlash"),"Zero launches the same fast forward wave in ground/air case "+airborne+" direction "+direction);
                Check(audio.LastPlayedCue=="saber","Zero ultimate emission uses the supplied saber WAV");
                yield return WaitUntil(()=>!hero.IsRedUltimate,2,"Zero ultimate did not recover.");
            }
            RainProjectile.ClearTeam(DamageTeam.Player);hero.SetInitialForm(HeroForm.Blue);hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
        }
        private static IEnumerator PlatformingRoute(RainLevelGame game)
        {
            var hero=game.Player;
            var starts=new[]{new Vector2(56.6f,4.8f),new Vector2(63.2f,6.2f),new Vector2(137.9f,1.4f),new Vector2(187,7.6f)};
            var goals=new[]{new Vector2(60.2f,6.2f),new Vector2(66.8f,7.6f),new Vector2(141.4f,2.8f),new Vector2(190.6f,9f)};
            for(int i=0;i<starts.Length;i++)
            {
                hero.Teleport(starts[i],true);hero.SetTestInput(default);yield return new WaitForSeconds(.12f);
                hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpPressed=true,jumpHeld=true});yield return null;
                hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true});yield return new WaitForSeconds(.28f);
                hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true,dashPressed=true});yield return null;
                hero.SetTestInput(new RainHeroInput{move=Vector2.right,jumpHeld=true});yield return new WaitForSeconds(.56f);
                hero.SetTestInput(default);yield return new WaitForSeconds(.14f);
                Check(hero.IsGrounded&&hero.transform.position.y>=goals[i].y-.15f&&hero.transform.position.x>=goals[i].x-.5f,
                    "Physical jump and air dash reach elevated route segment "+(i+1)+" (position "+hero.transform.position+", velocity "+hero.Velocity+")");
                if(i==3)yield return Capture(game,"11d_High_Towers_Platforming");
            }
            hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
        }
        private static IEnumerator DirectionalArt(RainLevelGame game)
        {
            var hero=game.Player;
            for(int armor=0;armor<2;armor++)
            {
                hero.SetInitialForm(HeroForm.Blue);hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
                if(armor==1)
                {
                    hero.SetTestInput(new RainHeroInput{armorPressed=true});yield return null;hero.SetTestInput(default);
                    yield return WaitUntil(()=>!hero.IsTransforming,4,"Directional armor transform did not finish.");
                }
                for(int direction=1;direction>=-1;direction-=2)
                {
                    RainProjectile.ClearTeam(DamageTeam.Player);hero.Teleport(new Vector2(4,1.2f),true);
                    hero.SetTestInput(new RainHeroInput{move=Vector2.right*direction});yield return new WaitForSeconds(.04f);
                    hero.SetTestInput(new RainHeroInput{move=Vector2.right*direction,dashPressed=true});yield return null;
                    hero.SetTestInput(new RainHeroInput{move=Vector2.right*direction});yield return new WaitForSeconds(.04f);
                    bool expectedFlip=(direction<0)^(armor==0);
                    Check(hero.IsDashing&&hero.Velocity.x*direction>6&&hero.Visual.Renderer.flipX==expectedFlip&&hero.Visual.CurrentAnimation==(armor==0?"AirDash":"Dash"),
                        "Forward airborne dash artwork matches travel for "+hero.Visual.Unit+" direction "+direction);
                    if(direction==1)yield return Capture(game,"22_Forward_AirDash_"+hero.Visual.Unit);
                    hero.SetTestInput(new RainHeroInput{move=Vector2.left*direction});yield return new WaitForSeconds(.025f);
                    Check(hero.FacingLeft==(direction<0),"Air dash keeps its facing locked to its travel direction");
                    hero.SetTestInput(new RainHeroInput{move=Vector2.right*direction,attackPressed=true});yield return null;
                    hero.SetTestInput(default);yield return null;
                    var shot=UnityEngine.Object.FindObjectsByType<RainProjectile>(FindObjectsSortMode.None).FirstOrDefault(p=>p.Owner==hero.gameObject&&p.AnimationName=="SmallShot"&&p.Velocity.x*direction>0);
                    Check(shot!=null&&shot.SpriteFlipped==(direction>0),"Original bullet artwork points toward its actual flight direction");
                    if(direction==1)yield return Capture(game,"23_Forward_Shots_"+hero.Visual.Unit);
                }
            }
            RainProjectile.ClearTeam(DamageTeam.Player);hero.SetInitialForm(HeroForm.Blue);hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
        }
        private static IEnumerator AxlStoryAndEnding(RainLevelGame game,HeroForm endingForm,bool verifyCombat)
        {
            var hero=game.Player;hero.SetTestInput(default);
            RainProjectile.ClearTeam(DamageTeam.Player);yield return new WaitForSeconds(.30f);
            hero.BlueHealth.ApplyDamage(3);hero.RedHealth.ApplyDamage(3);
            game.Boss.TakeHit(Mathf.Max(1,(game.Boss.Health.CurrentHealth-RainLevelGame.DragonStoryThreshold)*2),Vector2.right,hero.gameObject);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.BossCinematic,2,"Dragon threshold did not start Axl intervention.");
            Check(game.Boss.Health.CurrentHealth==RainLevelGame.DragonStoryThreshold&&hero.CinematicIdle&&game.InputLocked,"Dragon stops at 60 HP and the active hero holds an idle story pose");
            string frozenStage=game.CinematicStage;Vector3 frozenPosition=hero.transform.position;
            game.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);
            Check(game.CinematicStage==frozenStage&&Vector3.Distance(hero.transform.position,frozenPosition)<.001f,"Pausing freezes the boss intervention without advancing its story stage");game.SetPaused(false);
            yield return WaitUntil(()=>game.CinematicStage=="AxlShot",5,"Original Axl did not aim and shoot.");
            yield return new WaitForSeconds(.17f);
            if(verifyCombat)yield return Capture(game,"24_Axl_Intervention_Shot");
            yield return WaitUntil(()=>game.Narrative.IsActive,6,"Axl confrontation dialogue did not open.");
            Check(hero.CinematicIdle&&!game.AxlBoss.IsActive&&game.InputLocked,"Combat stays suspended throughout the Axl and Lumine revelation");
            yield return CompleteDialogue(game,"29_Axl_Confrontation",verifyCombat);
            yield return WaitUntil(()=>game.IsBossWarning,2,"Axl WARNING did not follow the conversation.");
            if(verifyCombat)yield return Capture(game,"30_Axl_WARNING");
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.AxlBoss,7,"Axl intervention did not hand control to the second encounter.");
            Check(game.ActiveBossName=="AXL"&&game.AxlBoss.transform.localScale.x<game.Boss.transform.localScale.x&&game.AxlBoss.transform.localScale.x>1.5f,"Axl has the requested name and sits between the dragon and hero in scale");
            Check(game.BossWasDefeated&&!game.Boss.IsAlive&&game.AxlBoss.IsActive&&game.ActiveBossUnit=="Axl","Original Axl replaces the defeated dragon as the active boss");
            Check(hero.BlueHealth.CurrentHealth==28&&hero.RedHealth.CurrentHealth==28&&game.AxlBoss.Health.CurrentHealth==game.AxlBoss.Health.MaxHealth,
                "Both heroes and Axl start the second fight with refilled health");
            Check(game.AxlBoss.Health.MaxHealth>=360,"Axl has a larger health pool than the dragon");
            Check(game.AxlBoss.ReceivedDamageMultiplier>=game.Boss.ReceivedDamageMultiplier,"Axl receives at least as much nominal damage as Magma Dragoon");
            if(verifyCombat)
            {
                yield return new WaitForSeconds(1.15f);
                Check(game.AxlBoss.ShotsFired>=3,"Axl opens with an aggressive aimed burst");
                int beforeHit=game.AxlBoss.Health.CurrentHealth;game.AxlBoss.TakeHit(10,Vector2.right,hero.gameObject);
                Check(beforeHit-game.AxlBoss.Health.CurrentHealth==11,"Axl accepts eleven HP from a ten-point hit");
                hero.SetInitialForm(HeroForm.Blue);hero.SetTestInput(default);
                int hp=game.AxlBoss.Health.CurrentHealth;
                for(int i=0;i<10;i++)
                {hero.SetTestInput(new RainHeroInput{attackPressed=true});yield return null;hero.SetTestInput(default);yield return new WaitForSeconds(.23f);}
                yield return new WaitForSeconds(.5f);
                Check(game.AxlBoss.Health.CurrentHealth<hp,"Real player projectiles damage the original moving Axl boss");
                yield return Capture(game,"25_Axl_Boss_Combat");
            }
            else yield return new WaitForSeconds(.4f);
            hero.BlueHealth.ResetHealth();hero.RedHealth.ResetHealth();
            if(hero.CurrentForm!=endingForm){hero.SwitchForm();yield return WaitUntil(()=>!hero.IsTransforming,4,"Ending hero switch did not finish.");}
            RainProjectile.ClearTeam(DamageTeam.Player);yield return new WaitForSeconds(.2f);
            game.AxlBoss.TakeHit(10000,Vector2.right,hero.gameObject);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Ending,2,"Axl did not yield at one HP.");
            Check(game.AxlBoss.Health.CurrentHealth==1&&hero.CinematicIdle&&!game.AxlBoss.IsActive,"Axl remains alive at exactly one HP while combat stops");
            yield return WaitUntil(()=>game.CinematicStage=="HeroesTogether",5,"The other playable hero did not arrive.");
            Check(game.CompanionVisible&&game.CompanionForm!=hero.CurrentForm&&hero.CinematicIdle,"The opposite hero appears beside the active hero in the final idle scene");
            yield return Capture(game,"26_Heroes_Together_"+(endingForm==HeroForm.Blue?"X_Active":"Zero_Active"));
            yield return WaitUntil(()=>game.Narrative.IsActive,3,"Final Lumine conversation did not open.");
            Check(game.Narrative.IsEndingDialogue&&game.CompanionVisible&&hero.CinematicIdle,"Both heroes stay idle while Lumine explains his survival inside Axl");
            yield return CompleteDialogue(game,"31_Axl_Final_Dialogue",verifyCombat);
            yield return WaitUntil(()=>game.CinematicStage=="AxlEscape",3,"Axl escape did not begin.");
            if(verifyCombat)yield return Capture(game,"27_Axl_Escape");
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Won,8,"Both heroes did not depart into the automatic level ending.");
            Check(game.EndingCompleted&&!game.AxlBoss.IsVisible&&!hero.Visual.Renderer.enabled&&!game.CompanionVisible,"Axl leaves first, both heroes depart, and the level ends without an exit trigger");
            if(verifyCombat)yield return Capture(game,"06_Mission_Complete");
            yield return null;yield return null;
            var audio=game.GetComponent<RainLevelAudio>();
            yield return WaitUntil(()=>audio.CurrentMusicCue=="resultsTheme",2,"Results music did not replace the conversation theme.");
            Check(audio.CurrentMusicCue=="resultsTheme"&&audio.MusicLoops,"Results use their own looping WAV theme");
        }
        private static IEnumerator CompleteDialogue(RainLevelGame game,string captureName,bool takeCapture)
        {
            var dialogue=game.Narrative;int safety=0;
            var audio=game.GetComponent<RainLevelAudio>();
            yield return WaitUntil(()=>audio.CurrentMusicCue=="conversationTheme",2,"Conversation WAV did not start for the mugshot scene.");
            while(dialogue.IsActive)
            {
                if(dialogue.IsTransitioning){yield return null;continue;}
                if(++safety>30)throw new InvalidOperationException("Dialogue failed to finish.");
                if(!dialogue.IsTextComplete)dialogue.Advance();
                if(takeCapture&&dialogue.CurrentPage==0)yield return Capture(game,captureName);
                dialogue.Advance();yield return null;
            }
        }
        private static IEnumerator ResultsReturnPath(RainLevelGame game)
        {
            var previous=Keyboard.current;var keyboard=InputSystem.AddDevice<Keyboard>("RainResultsValidationKeyboard");
            try
            {
                keyboard.MakeCurrent();yield return PressKey(keyboard,Key.None);
                yield return PressKey(keyboard,Key.R);
                Check(game.Phase==RainLevelPhase.Won,"Results offer no retry action through the old retry key");
                yield return PressKey(keyboard,Key.Enter);
                Check(game.Phase==RainLevelPhase.FrontEnd&&game.FrontEnd.Page==RainMenuPage.Main,"Enter returns from results directly to the main menu");
                yield return Capture(game,"32_Results_Return_To_Menu");
            }
            finally{InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();}
        }
        private static IEnumerator AlternateEndingAndCancellation(RainLevelGame game)
        {
            var hero=game.Player;game.StartNewGame(HeroForm.Blue);hero.SetTestInput(default);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,9,"Second story test could not start.");
            hero.Teleport(new Vector2(RainLevelWorld.ArenaLeft+3,0),true);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Boss,7,"Second story test could not activate Dragon.");
            game.Boss.TakeHit(1000,Vector2.right,hero.gameObject);yield return null;
            Check(game.Phase==RainLevelPhase.BossCinematic,"Dragon lethal burst still enters the intervention instead of skipping the story");
            game.Respawn();yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,5,"Respawn did not cancel the intervention.");
            yield return new WaitForSeconds(.9f);
            Check(!hero.CinematicIdle&&!game.AxlBoss.IsVisible&&!game.CompanionVisible&&game.CinematicStage=="None","Respawn cancels story actors and restores player control");
            hero.Teleport(new Vector2(RainLevelWorld.ArenaLeft+3,0),true);
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Boss,7,"Alternate ending could not activate Dragon.");
            yield return AxlStoryAndEnding(game,HeroForm.Blue,false);
        }
        private static IEnumerator LivesAndPickups(RainLevelGame game)
        {
            var hero=game.Player;hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);
            if(hero.CurrentForm!=HeroForm.Blue)hero.SetInitialForm(HeroForm.Blue);
            hero.BlueHealth.ApplyDamage(10);int red=hero.RedHealth.CurrentHealth;
            var health=game.SpawnPickup(RainPickupKind.SmallHealth,new Vector2(4,0));health.Collect();
            Check(health.AnimationName=="SmallPickupRotate"&&hero.BlueHealth.CurrentHealth==23&&hero.RedHealth.CurrentHealth==red,"Small energy tanks use the correct art and restore active hero energy");
            int score=game.Score,hp=hero.BlueHealth.CurrentHealth;
            var orb=game.SpawnPickup(RainPickupKind.ScoreOrb,new Vector2(4,0));orb.Collect();
            Check(orb.AnimationName=="BlueOrbRotate"&&game.Score==score+100&&hero.BlueHealth.CurrentHealth==hp,"Blue orbs award score without healing");
            var reward=game.SpawnPickup(RainPickupKind.LargeHealth,new Vector2(4,0),"qa_persistent_health");reward.Collect();
            Check(reward.AnimationName=="CapsulePulse"&&game.SpawnPickup(RainPickupKind.LargeHealth,new Vector2(4,0),"qa_persistent_health")==null,
                "Large authored health tanks use capsule art and cannot be farmed after collection");
            var placements=UnityEngine.Object.FindFirstObjectByType<RainLevelWorld>().PickupPlacements;
            Check(placements.Any(p=>p.kind==RainPickupKind.LargeHealth&&p.position.y>=5)&&placements.Any(p=>p.kind==RainPickupKind.ExtraLife&&p.position.y>=5),
                "Large tanks and extra lives reward elevated platforming routes");
            int reserve=game.Lives;var extra=game.SpawnPickup(RainPickupKind.ExtraLife,new Vector2(4,0));extra.Collect();
            Check(extra.AnimationName=="BlueEnergyPulse"&&game.Lives==reserve+1,"Original X life item increases the shared reserve");
            hero.SwitchForm();Check(game.Lives==reserve+1,"Switching heroes preserves the shared life reserve");
            hero.Teleport(new Vector2(4,-9),true);yield return null;yield return null;
            Check(game.Lives==reserve&&game.Phase==RainLevelPhase.Dead,"Falling into a pit consumes exactly one shared life");
            yield return WaitUntil(()=>game.Phase==RainLevelPhase.Playing,6,"Lost life did not return to a checkpoint.");
            Check(hero.BlueHealth.CurrentHealth==28&&hero.RedHealth.CurrentHealth==28,"Life retry restores both hero health pools");
            hero.SetInitialForm(HeroForm.Blue);hero.SetTestInput(default);
        }
        private static IEnumerator GroundWaveTraversal(RainLevelGame game)
        {
            var boss=game.Boss;var driver=boss.GetComponent<MegaManX9.Animation.DragonBossAttackPreview>();
            bool enabled=boss.enabled;boss.enabled=false;
            try
            {
                driver.ClearEffects();driver.SetPaused(false);driver.BeginAttack("GroundFireWave");driver.Advance(2.5f);
                var wave=driver.ActiveProjectiles.FirstOrDefault(p=>p.Kind=="GroundWave");
                Check(wave!=null&&wave.Alive&&wave.Age>1.1f&&wave.StopsAtWorldWalls,"Dragon ground fire survives its old short lifetime and keeps travelling");
                Check(driver.ActiveProjectiles.Count(p=>p.Kind=="GroundPillar"&&p.Alive&&p.LocalVelocity.sqrMagnitude>0)>=4,
                    "The full ground-fire column train travels with its leading wave");
                var columns=driver.ActiveProjectiles.Where(p=>p.Kind=="GroundPillar"&&p.Alive).ToArray();
                Check(columns.Max(p=>p.transform.position.x)-columns.Min(p=>p.transform.position.x)>2,
                    "Moving fire columns retain visible spacing instead of collapsing into one sprite");
                yield return Capture(game,"15_Dragon_Long_Fire_Wave");
                driver.SetPaused(false);
                for(int i=0;i<350&&wave.Alive;i++)driver.Advance(.02f);
                Check(!wave.Alive,"Travelling ground fire ends on the closed arena wall");
            }
            finally
            {
                driver.ClearEffects();driver.BeginAttack("CombatIdle");boss.enabled=enabled;
                game.Player.Teleport(new Vector2(RainLevelWorld.ArenaLeft+3,0),true);
            }
        }
        private static IEnumerator JumpAndVoidCamera(RainLevelGame game)
        {
            var hero=game.Player;hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);yield return new WaitForSeconds(.12f);
            hero.SetTestInput(new RainHeroInput{jumpPressed=true,jumpHeld=true});yield return null;
            hero.SetTestInput(new RainHeroInput{jumpHeld=true});float peak=0;
            for(float t=0;t<.9f;t+=Time.deltaTime){peak=Mathf.Max(peak,hero.transform.position.y);yield return null;}
            Check(peak>1.4f&&peak<2.05f,"Held jump apex is reduced to under two world units");hero.SetTestInput(default);
            var camera=UnityEngine.Object.FindFirstObjectByType<RainLevelCamera>();
            hero.Teleport(new Vector2(25.5f,0),true);yield return new WaitForSeconds(.12f);camera.SnapToTarget();
            float cameraY=camera.transform.position.y;
            hero.Teleport(new Vector2(27,-.3f),false);yield return new WaitForSeconds(.36f);
            Check(camera.IsHoldingOverVoid&&camera.transform.position.y>=cameraY-.10f,"Camera holds the last safe floor while the hero falls into a bottomless gap");
            yield return Capture(game,"11e_Static_Camera_Over_Void");
            hero.Teleport(new Vector2(4,0),true);hero.SetTestInput(default);camera.SnapToTarget();
        }
        private static IEnumerator KeyboardPath(RainLevelGame game)
        {
            var hero=game.Player;hero.Teleport(new Vector2(4,0),true);
            if(hero.CurrentForm!=HeroForm.Blue)hero.SwitchForm();
            var previous=Keyboard.current;
            var keyboard=InputSystem.AddDevice<Keyboard>("RainLevelValidationKeyboard");
            try
            {
                keyboard.MakeCurrent();hero.ClearTestInput();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Tab));yield return null;yield return null;
                Check(hero.CurrentForm==HeroForm.Red,"Input System keyboard Tab switches the playable form");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                float start=hero.transform.position.x;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));yield return new WaitForSeconds(.16f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                Check(hero.transform.position.x-start>.35f,"Input System keyboard movement reaches the real physics controller");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.P));yield return null;yield return null;
                bool paused=game.IsPaused;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.P));yield return null;yield return null;
                Check(paused&&!game.IsPaused,"Input System keyboard P pauses and resumes the session");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
                yield return PressKey(keyboard,Key.P);yield return PressKey(keyboard,Key.RightArrow);
                Check(game.IsPaused&&game.GetComponent<RainLevelHUD>().PauseMenuSelection==1,"Pause menu arrows select the supplied menu actions");
                yield return PressKey(keyboard,Key.LeftArrow);yield return PressKey(keyboard,Key.Enter);
                Check(!game.IsPaused,"Pause menu Enter resumes through its selected action");
            }
            finally
            {
                game.SetPaused(false);hero.SetTestInput(default);
                InputSystem.RemoveDevice(keyboard);if(previous!=null&&previous.added)previous.MakeCurrent();
            }
        }
        private static void SaveReport()
        {
            if(report==null)report=new Report();report.checks=checks.ToArray();report.screenshots=captures.ToArray();
            Directory.CreateDirectory("Tools/LevelPipeline");File.WriteAllText("Tools/LevelPipeline/playtest_report.json",JsonUtility.ToJson(report,true));
        }
        private static void Fail(Exception error)
        {
            Application.logMessageReceived-=CaptureEngineWarnings;
            SessionState.SetBool(PendingPlay,false);
            if(report==null)report=new Report();report.success=false;report.error=error.ToString();running=false;SaveReport();
            var game=RainLevelGame.Instance;if(game!=null){game.SetPaused(false);game.Player?.ClearTestInput();var hud=game.GetComponent<RainLevelHUD>();if(hud!=null)hud.SuppressPausePanel=false;}
            if(originalEnemies!=null)foreach(var enemy in originalEnemies)if(enemy!=null)enemy.enabled=true;
            Debug.LogException(error);Debug.LogError("RAIN_LEVEL_PLAYTEST_FAILED "+error.Message);
        }
    }
}
