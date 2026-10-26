using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Three 2D buses driven by game state and the user's imported recordings.</summary>
    [DisallowMultipleComponent]
    public sealed class RainLevelAudio : MonoBehaviour
    {
        [SerializeField] private RainAudioAssets assets;
        private AudioSource music, ambience, effects, chargeVoice;
        private bool chargePaused;
        private readonly Dictionary<string,AudioClip> generated=new Dictionary<string,AudioClip>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,float> lastCueTimes=new Dictionary<string,float>(StringComparer.OrdinalIgnoreCase);
        private static RainLevelAudio instance;
        private static bool conversation;
        private float musicGain=1,reloadTimer,effectsDuckUntil;
        private string musicCue="";
        public string CurrentMusicCue=>musicCue;
        public string DesiredMusicCue {get;private set;}="";
        public bool MusicLoops=>music!=null&&music.loop;
        public bool MusicIsPlaying=>music!=null&&music.isPlaying;
        public bool AmbienceLoops=>ambience!=null&&ambience.loop;
        public bool AmbienceIsPlaying=>ambience!=null&&ambience.isPlaying;
        public string LastPlayedCue {get;private set;}="";
        public AudioClip LastPlayedClip {get;private set;}
        public int ImportedClipCount=>assets!=null?assets.ImportedClipCount:0;
        public AudioSource MusicSource=>music;
        public AudioSource AmbienceSource=>ambience;
        public AudioSource EffectsSource=>effects;
        public static bool ConversationActive=>conversation;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics(){instance=null;conversation=false;}
        private void Awake()
        {
            if(instance!=null&&instance!=this){enabled=false;return;}
            instance=this;music=CreateBus("Music",true,64);ambience=CreateBus("Ambience",true,96);effects=CreateBus("Effects2D",false,32);
            effects.volume=1;if(assets==null)assets=RainAudioAssets.Load();StartRain();
        }
        private AudioSource CreateBus(string name,bool loop,int priority)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.dopplerLevel=0;source.priority=priority;source.volume=0;return source;
        }
        public bool HasImportedCue(string cue)=>assets!=null&&assets.TryGet(ResolveRequest(cue),out _);
        public static void SetConversation(bool active)=>conversation=active;
        public static void Play(string cue){if(instance!=null&&instance.enabled)instance.PlayCue(cue);}
        private static string ResolveRequest(string value)
        {
            if(RainAudioAssets.Key(value)=="change")
            {
                var hero=RainLevelGame.Instance!=null?RainLevelGame.Instance.Player:null;
                return hero!=null&&hero.CurrentForm==HeroForm.Red?"voiceZeroSwitchOut":"voiceXSwitchOut";
            }
            return RainAudioAssets.CanonicalCue(value);
        }
        private void PlayCue(string request)
        {
            string cue=ResolveRequest(request);if(string.IsNullOrEmpty(cue))return;
            bool menu=cue.StartsWith("menu",StringComparison.OrdinalIgnoreCase);var game=RainLevelGame.Instance;
            if(!menu&&game!=null&&game.IsPaused)return;
            float now=Time.unscaledTime;
            // Several body hitboxes can report the same impact in one frame.
            if(lastCueTimes.TryGetValue(cue,out float last)&&now-last<.045f)return;lastCueTimes[cue]=now;
            var clip=ResolveClip(cue,out float gain);if(clip==null)return;
            LastPlayedCue=cue;LastPlayedClip=clip;
            if(cue=="charge")
            {
                if(chargeVoice==null)
                {
                    var child=new GameObject("Charge voice");child.transform.SetParent(effects.transform,false);chargeVoice=child.AddComponent<AudioSource>();
                    chargeVoice.playOnAwake=false;chargeVoice.spatialBlend=0;chargeVoice.dopplerLevel=0;chargeVoice.priority=40;
                }
                chargeVoice.Stop();chargeVoice.clip=clip;chargeVoice.volume=Mathf.Clamp01((assets!=null?assets.effectsVolume:.78f)*gain);
                chargePaused=false;chargeVoice.Play();
            }
            else
            {
                if(cue=="chargedShot"&&chargeVoice!=null){chargeVoice.Stop();chargePaused=false;}
                effects.PlayOneShot(clip,Mathf.Clamp01((assets!=null?assets.effectsVolume:.78f)*gain));
            }
            if(!menu)effectsDuckUntil=now+.18f;
        }
        private AudioClip ResolveClip(string cue,out float gain)
        {
            if(assets!=null&&assets.TryGet(cue,out var imported)){gain=imported.gain;return imported.clip;}
            gain=cue.EndsWith("Theme",StringComparison.Ordinal)?.12f:
                cue.Equals("land",StringComparison.OrdinalIgnoreCase)?.32f:
                cue.Equals("enemyShot",StringComparison.OrdinalIgnoreCase)?.65f:
                cue.Equals("special",StringComparison.OrdinalIgnoreCase)?.80f:1;
            if(generated.TryGetValue(cue,out var fallback))return fallback;
            if(cue=="rain")fallback=GenerateRain();
            else if(cue.EndsWith("Theme",StringComparison.Ordinal))fallback=Tone(cue,4,130,130,0,true);
            else if(cue=="score")fallback=Tone(cue,.09f,1050,1550,0);
            else if(cue=="extraLife")fallback=Tone(cue,.55f,440,1320,0);
            else if(cue.Equals("enemyShot",StringComparison.OrdinalIgnoreCase))fallback=Tone(cue,.095f,620,80,.24f);
            else if(cue.Equals("special",StringComparison.OrdinalIgnoreCase))fallback=Tone(cue,.46f,240,1850,.08f);
            else if(cue.Equals("land",StringComparison.OrdinalIgnoreCase))fallback=Tone(cue,.10f,95,35,.38f);
            else if(cue=="shot"||cue=="chargedShot")fallback=Tone(cue,.11f,900,150,0);
            else if(cue.StartsWith("saber",StringComparison.Ordinal))fallback=Tone(cue,.17f,400,60,.7f);
            else if(cue=="arrival"||cue=="departure")fallback=Tone(cue,.25f,420,1050,.05f);
            else fallback=Tone(cue,.13f,170,40,.8f);
            generated[cue]=fallback;return fallback;
        }
        private void StartRain(){ambience.Stop();ambience.clip=ResolveClip("rain",out _);ambience.loop=true;ambience.volume=0;}
        private static void EnsureLoopPlayback(AudioSource source)
        {
            if(source==null||source.clip==null)return;
            if(source.clip.loadState==AudioDataLoadState.Unloaded)source.clip.LoadAudioData();
            if(source.clip.loadState==AudioDataLoadState.Loaded&&!source.isPlaying)source.Play();
        }
        private void Update()
        {
            if(assets==null&&(reloadTimer-=Time.unscaledDeltaTime)<=0)
            {
                reloadTimer=1;var imported=RainAudioAssets.Load();if(imported!=null){assets=imported;musicCue="";StartRain();}
            }
            var game=RainLevelGame.Instance;bool paused=game!=null&&(game.IsPaused||game.IsTransformationPaused);
            if(game!=null&&game.StartupVideo!=null&&game.StartupVideo.IsPlaying)
            {
                music.Stop();music.volume=0;musicCue="";DesiredMusicCue="";
                if(ambience.isPlaying)ambience.Pause();ambience.volume=0;
                return;
            }
            DesiredMusicCue=SelectMusic(game);float fade=assets!=null?assets.musicFadeSeconds:.32f;
            float baseMusic=assets!=null?assets.musicVolume:.32f;
            float step=Mathf.Max(.01f,baseMusic)*Time.unscaledDeltaTime/Mathf.Max(.05f,fade);
            if(musicCue!=DesiredMusicCue)
            {
                music.volume=Mathf.MoveTowards(music.volume,0,step);
                if(music.volume<.001f){music.Stop();music.clip=ResolveClip(DesiredMusicCue,out musicGain);musicCue=DesiredMusicCue;music.loop=true;EnsureLoopPlayback(music);}
            }
            else
            {
                float duck=paused?(assets!=null?assets.pauseMusicFactor:.55f):1;if(Time.unscaledTime<effectsDuckUntil)duck*=.82f;
                music.volume=Mathf.MoveTowards(music.volume,baseMusic*musicGain*duck,step);
            }
            float rainGain=1;if(assets!=null&&assets.TryGet("rain",out var rainCue))rainGain=rainCue.gain;
            bool rainInLevel=game!=null&&game.HasStarted&&game.Phase!=RainLevelPhase.FrontEnd&&game.Phase!=RainLevelPhase.Won&&
                (game.ControlsGuide==null||!game.ControlsGuide.IsActive)&&(game.Narrative==null||!game.Narrative.IsIntroduction)&&
                (game.Phase!=RainLevelPhase.Intro||game.Player.IsEntering||game.CinematicStage=="MissionArrival");
            float rainVolume=(assets!=null?assets.ambienceVolume:.36f)*rainGain;
            if(conversation)rainVolume*=.65f;if(game!=null&&game.Phase==RainLevelPhase.FrontEnd)rainVolume*=.65f;
            if(game!=null&&game.Phase==RainLevelPhase.Won)rainVolume*=.35f;
            if(paused)rainVolume*=assets!=null?assets.pauseRainFactor:.25f;
            if(rainInLevel){ambience.volume=Mathf.MoveTowards(ambience.volume,rainVolume,Time.unscaledDeltaTime*.8f);EnsureLoopPlayback(ambience);}
            else{ambience.Stop();ambience.volume=0;}
            EnsureLoopPlayback(music);
            UpdateCharge(game,paused);
        }
        private void UpdateCharge(RainLevelGame game,bool paused)
        {
            if(chargeVoice==null)return;
            if(game==null||game.Player==null||!game.HasStarted||game.Player.CurrentForm!=HeroForm.Blue||game.Player.Charge01<.01f||game.InputLocked&&!paused)
            {chargeVoice.Stop();chargePaused=false;return;}
            if(paused&&!chargePaused&&chargeVoice.isPlaying){chargeVoice.Pause();chargePaused=true;}
            else if(!paused&&chargePaused){chargeVoice.UnPause();chargePaused=false;}
        }
        public static string SelectMusic(RainLevelGame game)
        {
            if(conversation)return "conversationTheme";
            if(game==null||game.Phase==RainLevelPhase.FrontEnd)return "mainTheme";
            if(game.ControlsGuide!=null&&game.ControlsGuide.IsActive)return "mainTheme";
            if(game.Phase==RainLevelPhase.Won)return "resultsTheme";
            if(game.IsBossWarning&&game.BossWasDefeated)return "axlTheme";
            if(game.Phase==RainLevelPhase.AxlBoss)return "axlTheme";
            if(game.Phase==RainLevelPhase.BossIntro||game.Phase==RainLevelPhase.Boss||game.Phase==RainLevelPhase.BossCinematic&&!game.BossWasDefeated)return "dragonTheme";
            return "levelTheme";
        }
        private static AudioClip Tone(string name,float duration,float start,float end,float noise,bool softLoop=false)
        {
            const int rate=22050;var data=new float[Mathf.CeilToInt(duration*rate)];var random=new System.Random(name.Length*17);float phase=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)data.Length;phase+=Mathf.Lerp(start,end,t)/rate*2*Mathf.PI;
                float envelope=softLoop?Mathf.Sin(t*Mathf.PI)*.35f:Mathf.Pow(1-t,2)*Mathf.Min(1,t*40);
                data[i]=(Mathf.Sin(phase)*(1-noise)+((float)random.NextDouble()*2-1)*noise)*envelope;
            }
            var clip=AudioClip.Create(name+" fallback generated",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        private static AudioClip GenerateRain()
        {
            const int rate=22050;var samples=new float[rate*3];var random=new System.Random(1937);float smooth=0;
            for(int i=0;i<samples.Length;i++){float noise=(float)random.NextDouble()*2-1;smooth=smooth*.88f+noise*.12f;samples[i]=(noise*.3f+smooth*.7f)*(.78f+.16f*Mathf.Sin(i/(float)rate*2.1f));}
            var rain=AudioClip.Create("Rain fallback generated",samples.Length,1,rate,false);rain.SetData(samples,0);return rain;
        }
        private void OnDestroy()
        {
            if(instance==this){instance=null;conversation=false;}
            if(music!=null)music.Stop();if(ambience!=null)ambience.Stop();if(effects!=null)effects.Stop();if(chargeVoice!=null)chargeVoice.Stop();
            // Imported clips belong to the asset database; destroy only transient fallbacks created here.
            foreach(var clip in generated.Values)if(clip!=null)Destroy(clip);generated.Clear();
        }
    }
}
