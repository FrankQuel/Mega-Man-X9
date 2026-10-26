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
    [RequireComponent(typeof(Rigidbody2D),typeof(BoxCollider2D),typeof(RainHeroVisual))]
    public sealed class RainHeroController : MonoBehaviour,IRainDamageable,IDragonBossDamageable
    {
        [SerializeField] private RainLevelAssets assets;
        [SerializeField] private LayerMask groundMask=1<<8;
        private Rigidbody2D body;private BoxCollider2D capsule;private RainHeroVisual visual;
        private CharacterHealth blueHealth,redHealth;
        private RainHeroInput input,testInput;private bool useTestInput,configured;
        private bool facingLeft,crouching,climbing,wallSliding,airDashUsed,ultimateArmor,enhancedSaber,attackQueued,entrancePlaying,presentationHidden;
        private float jumpBuffer,coyote,dashTime,dashDirection,wallLock,invincible,hurtTime,shotPose,charge,specialCooldown,idleTime;
        private float actionTime,actionDuration,actionAge,runStartTime,wallPose,dashStarting,dashEnding;
        private float specialCooldownMax=3.5f;
        private bool wasMoving,dashJumpMomentum,morphPlaying,morphShowActor,redUltimate,ultimateWaveEmitted,cinematicIdle;
        private bool ultimateFacingLeft;
        private Coroutine morph;
        private GameObject ultimateAura;
        private GameObject enhancedAura;
        private MegaManX9.Animation.SpriteAnimationLibrary.Entry enhancedAuraEntry;
        private float enhancedAuraClock;
        private int wallDirection,combo,airCombo;private string actionName="";private bool actionMelee,nova;
        private RainLadder ladder;private Coroutine entrance,departure;private GameObject entranceEffect;private Action entranceComplete;
        private readonly HashSet<IRainDamageable> attackHits=new HashSet<IRainDamageable>();
        private const float RunSpeed=3.25f,DashSpeed=7f,JumpSpeed=10f,Gravity=27f,MaxFall=14.5f,AnimationSpeed=.8f,ActionSpeed=.85f;
        public static float RunningSpeed=>RunSpeed;
        public static float JumpApex=>JumpSpeed*JumpSpeed/(2*Gravity);
        public HeroForm CurrentForm {get;private set;}
        public CharacterHealth BlueHealth=>blueHealth;
        public CharacterHealth RedHealth=>redHealth;
        public CharacterHealth ActiveHealth=>CurrentForm==HeroForm.Blue?blueHealth:redHealth;
        public CharacterHealth OtherHealth=>CurrentForm==HeroForm.Blue?redHealth:blueHealth;
        public DamageTeam Team=>DamageTeam.Player;
        public bool IsAlive=>ActiveHealth!=null&&ActiveHealth.IsAlive;
        public bool IsGrounded {get;private set;}
        public bool IsOnLadder=>climbing;
        public bool IsDashing=>dashTime>0;
        public bool IsEntering=>entrancePlaying;
        public bool IsTransforming=>morphPlaying;
        public bool IsRedUltimate=>redUltimate;
        public bool IsCrouching=>crouching;
        public bool IsWallSliding=>wallSliding;
        public bool UltimateArmor=>ultimateArmor;
        public bool EnhancedSaber=>enhancedSaber;
        public bool EnhancedAuraActive=>enhancedAura!=null&&enhancedAura.activeSelf;
        public string EnhancedAuraClip=>enhancedAuraEntry?.animationName;
        public bool CinematicIdle=>cinematicIdle;
        public float Charge01=>Mathf.Clamp01(charge/1.15f);
        public float SpecialCooldown01=>Mathf.Clamp01(specialCooldown/specialCooldownMax);
        public Vector2 Velocity=>body!=null?body.linearVelocity:Vector2.zero;
        public RainHeroVisual Visual=>visual;
        public bool FacingLeft=>facingLeft;

        public void Configure(RainLevelAssets source)
        {
            assets=source;EnsureComponents();visual.Configure(source);UpdateVisualUnit();configured=true;
        }
        private void Awake()=>EnsureComponents();
        private void EnsureComponents()
        {
            if(body!=null)return;
            body=GetComponent<Rigidbody2D>();capsule=GetComponent<BoxCollider2D>();visual=GetComponent<RainHeroVisual>();
            body.bodyType=RigidbodyType2D.Dynamic;body.gravityScale=0;body.freezeRotation=true;
            body.interpolation=RigidbodyInterpolation2D.Interpolate;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            capsule.size=new Vector2(.52f,.86f);capsule.offset=new Vector2(0,.43f);
            var frictionless=new PhysicsMaterial2D("Rain hero frictionless"){friction=0,bounciness=0};capsule.sharedMaterial=frictionless;
            gameObject.layer=9;
            blueHealth=HealthChild("Blue Health");redHealth=HealthChild("Red Health");
        }
        private CharacterHealth HealthChild(string name)
        {
            var child=transform.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(transform,false);}
            var health=child.GetComponent<CharacterHealth>();if(health==null)health=child.gameObject.AddComponent<CharacterHealth>();health.Configure(28);return health;
        }
        public void SetTestInput(RainHeroInput frame){useTestInput=true;testInput=frame;}
        public void ClearTestInput(){useTestInput=false;input=default;}
        private RainHeroInput ReadInput()
        {
            if(useTestInput)return testInput;
            return RainInputBindings.ReadHeroInput();
        }
        private void Update()
        {
            if(!configured)return;
            input=ReadInput();
            var game=RainLevelGame.Instance;
            if(game!=null&&(game.Phase==RainLevelPhase.Won||game.Phase==RainLevelPhase.Dead||game.Phase==RainLevelPhase.FrontEnd))return;
            if(cinematicIdle)return;
            if(morphPlaying)return;
            if(input.switchPressed)SwitchForm();
            if(input.armorPressed&&CurrentForm==HeroForm.Blue){ultimateArmor=!ultimateArmor;RefreshAppearance();}
            if(input.enhancedPressed&&CurrentForm==HeroForm.Red){enhancedSaber=!enhancedSaber;RefreshAppearance();}
            if(entrancePlaying)return;
            if(game!=null&&game.InputLocked)return;
            float dt=Time.deltaTime;
            invincible=Mathf.Max(0,invincible-dt);hurtTime=Mathf.Max(0,hurtTime-dt);shotPose=Mathf.Max(0,shotPose-dt);
            specialCooldown=Mathf.Max(0,specialCooldown-dt);wallPose=Mathf.Max(0,wallPose-dt);runStartTime=Mathf.Max(0,runStartTime-dt);
            dashStarting=Mathf.Max(0,dashStarting-dt);dashEnding=Mathf.Max(0,dashEnding-dt);
            bool moving=Mathf.Abs(input.move.x)>.1f;
            if(moving&&!wasMoving&&IsGrounded)runStartTime=.10f;wasMoving=moving;
            if(input.jumpPressed)jumpBuffer=.14f;
            if(redUltimate)facingLeft=ultimateFacingLeft;
            else if(dashTime>0)facingLeft=dashDirection<0;
            else if(input.move.x!=0&&wallLock<=0)facingLeft=input.move.x<0;
            visual.FacingLeft=facingLeft;
            if(hurtTime<=0&&!redUltimate)
            {
                if(input.dashPressed)TryDash();
                if(!morphPlaying)
                {
                    if(input.specialPressed&&specialCooldown<=0)Special();
                    if(CurrentForm==HeroForm.Blue)
                    {
                        if(input.attackPressed){FireBuster(0);charge=0;}
                        if(input.attackHeld){float previousCharge=charge;charge+=dt;if(previousCharge<.45f&&charge>=.45f)RainLevelAudio.Play("charge");}
                        if(input.attackReleased){if(charge>.45f)FireBuster(charge>1.05f?2:1);charge=0;}
                    }
                    else if(input.attackPressed){if(actionTime>0)attackQueued=true;else StartSaber();}
                }
            }
            UpdateAction(dt);
            if(input.move.sqrMagnitude<.01f&&IsGrounded)idleTime+=dt;else idleTime=0;
            SelectLocomotionAnimation();
        }
        private void LateUpdate()
        {
            if(visual==null||visual.Renderer==null)return;
            if(morphPlaying)visual.Renderer.enabled=morphShowActor&&!presentationHidden;
            else if(!entrancePlaying)visual.Renderer.enabled=!presentationHidden;
            visual.Renderer.color=invincible>0&&Mathf.FloorToInt(invincible*16)%2==0?new Color(.5f,.9f,1,.4f):Color.white;
            UpdateEnhancedAura();
        }
        private void FixedUpdate()
        {
            if(!configured)return;
            var game=RainLevelGame.Instance;
            if(cinematicIdle||entrancePlaying||game!=null&&game.InputLocked){body.linearVelocity=Vector2.zero;return;}
            if(redUltimate){body.linearVelocity=Vector2.zero;return;}
            float dt=Time.fixedDeltaTime;Vector2 velocity=body.linearVelocity;
            bool previouslyGrounded=IsGrounded;
            IsGrounded=velocity.y<=.15f&&Physics2D.BoxCast(body.position+Vector2.up*.45f,new Vector2(.44f,.82f),0,Vector2.down,.085f,groundMask).collider!=null;
            if(IsGrounded&&!previouslyGrounded&&velocity.y<-1.1f&&actionTime<=0)
            {RainLevelAudio.Play("land");StartAction(CurrentForm==HeroForm.Red?RedClip("Land"):"AirRecover",.12f,false);}
            if(IsGrounded){coyote=.11f;airDashUsed=false;airCombo=0;dashJumpMomentum=false;}else coyote-=dt;
            jumpBuffer-=dt;wallLock=Mathf.Max(0,wallLock-dt);
            if(dashTime>0&&dashTime<=dt)dashEnding=.10f;
            dashTime=Mathf.Max(0,dashTime-dt);
            wallDirection=Physics2D.BoxCast(body.position+Vector2.up*.48f,new Vector2(.50f,.64f),0,Vector2.right,.08f,groundMask).collider!=null?1:
                Physics2D.BoxCast(body.position+Vector2.up*.48f,new Vector2(.50f,.64f),0,Vector2.left,.08f,groundMask).collider!=null?-1:0;
            UpdateLadder(dt,ref velocity);
            capsule.isTrigger=climbing;
            if(climbing){body.linearVelocity=velocity;return;}
            bool wantsCrouch=input.move.y<-.4f&&IsGrounded;
            if(crouching&&!wantsCrouch&&Physics2D.OverlapBox(body.position+Vector2.up*.72f,new Vector2(.45f,.25f),0,groundMask)!=null)wantsCrouch=true;
            crouching=wantsCrouch;capsule.size=new Vector2(.52f,crouching?.48f:.86f);capsule.offset=new Vector2(0,crouching?.24f:.43f);
            wallSliding=!IsGrounded&&wallDirection!=0&&input.move.x*wallDirection>.1f&&velocity.y<0;
            if(hurtTime<=0)
            {
                if(nova&&actionTime>0){velocity=new Vector2((facingLeft?-1:1)*9,0);}
                else if(dashTime>0){velocity.x=dashDirection*DashSpeed;if(!IsGrounded)velocity.y=0;}
                else
                {
                    if(wallLock<=0)velocity.x=input.move.x*(dashJumpMomentum&&!IsGrounded?DashSpeed:RunSpeed)*(crouching?.35f:actionMelee&&IsGrounded?.3f:1);
                    if(IsGrounded&&velocity.y<=0)velocity.y=-1;
                    else velocity.y=Mathf.Max(-MaxFall,velocity.y-Gravity*dt);
                    if(!input.jumpHeld&&velocity.y>4&&!actionMelee)velocity.y=Mathf.Max(4,velocity.y-55*dt);
                    if(wallSliding)velocity.y=Mathf.Max(velocity.y,-1.8f);
                }
                if(jumpBuffer>0&&(coyote>0||wallDirection!=0))
                {
                    bool fromWall=!IsGrounded&&wallDirection!=0;
                    dashJumpMomentum=!fromWall&&(dashTime>0||dashJumpMomentum);
                    jumpBuffer=0;coyote=0;dashTime=0;IsGrounded=false;
                    velocity.y=JumpSpeed;
                    RainLevelAudio.Play("jump");
                    if(fromWall)
                    {velocity.x=-wallDirection*5.3f;facingLeft=wallDirection>0;wallLock=.17f;wallPose=.28f;visual.Play(CurrentForm==HeroForm.Red?RedClip("AirUppercutPose"):"WallJump",true);}
                    else visual.Play(CurrentForm==HeroForm.Red?RedClip("JumpRise"):"Jump",true);
                }
            }
            else velocity.y=Mathf.Max(-MaxFall,velocity.y-Gravity*dt);
            body.linearVelocity=velocity;
        }
        private void UpdateLadder(float dt,ref Vector2 velocity)
        {
            if(!climbing&&Mathf.Abs(input.move.y)>.35f&&actionTime<=0)
            {
                foreach(var hit in Physics2D.OverlapBoxAll(body.position+Vector2.up*.35f,new Vector2(.5f,.9f),0))
                {var candidate=hit.GetComponent<RainLadder>();if(candidate==null||input.move.y>0&&body.position.y>=candidate.Top-.12f)continue;ladder=candidate;climbing=true;dashTime=0;dashJumpMomentum=false;break;}
            }
            if(!climbing)return;
            if(ladder==null){climbing=false;return;}
            body.position=new Vector2(Mathf.MoveTowards(body.position.x,ladder.Bounds.center.x,dt*9),body.position.y);
            velocity=new Vector2(0,input.move.y*2.35f);
            if(input.jumpPressed){climbing=false;velocity.y=JumpSpeed;jumpBuffer=0;return;}
            if(input.move.y>0&&body.position.y>=ladder.Top-.04f)
            {body.position=new Vector2(body.position.x,ladder.Top+.025f);climbing=false;velocity=Vector2.zero;StartAction(CurrentForm==HeroForm.Blue?"LadderDismount":RedClip("Land"),.22f,false);}
            else if(input.move.y<0&&body.position.y<=ladder.Bottom+.03f){climbing=false;velocity.y=-1;}
        }
        private string RedClip(string name)
        {
            if(enhancedSaber&&assets.TryGet("Red","Enhanced_"+name,out _))return "Enhanced_"+name;
            return name;
        }
        private void UpdateVisualUnit()
        {
            if(assets==null||visual==null)return;
            visual.SetUnit(CurrentForm==HeroForm.Red?"Red":ultimateArmor?"BlueUltimate":"Blue");visual.FacingLeft=facingLeft;
        }
        private void RefreshAppearance()
        {
            string previousUnit=visual.Unit;
            actionTime=0;actionMelee=false;nova=false;attackQueued=false;charge=0;
            EndRedUltimate();idleTime=0;
            if(visual!=null&&visual.Animator!=null)visual.Animator.speed=AnimationSpeed;
            UpdateVisualUnit();
            if(entrancePlaying)BeginEntrance(entranceComplete);else BeginMorph(previousUnit,CurrentForm,true);
        }
        public void SetInitialForm(HeroForm form)
        {
            EnsureComponents();CancelEntrance();EndRedUltimate();
            CurrentForm=form;ultimateArmor=false;enhancedSaber=false;idleTime=0;facingLeft=false;wasMoving=false;cinematicIdle=false;
            specialCooldown=0;specialCooldownMax=3.5f;combo=airCombo=0;
            charge=actionTime=shotPose=dashTime=0;UpdateVisualUnit();
            if(visual!=null)visual.Play("Idle",true);
        }
        public bool SwitchForm()
        {
            EnsureComponents();if(OtherHealth==null||!OtherHealth.IsAlive)return false;
            string previousUnit=visual.Unit;HeroForm previousForm=CurrentForm;
            CurrentForm=CurrentForm==HeroForm.Blue?HeroForm.Red:HeroForm.Blue;
            RainLevelAudio.Play(previousForm==HeroForm.Blue?"voiceXSwitchOut":"voiceZeroSwitchOut");
            if(visual!=null&&visual.Animator!=null)visual.Animator.speed=AnimationSpeed;
            charge=shotPose=0;idleTime=0;actionTime=0;actionMelee=false;nova=false;attackQueued=false;EndRedUltimate();UpdateVisualUnit();
            if(entrancePlaying)BeginEntrance(entranceComplete);else BeginMorph(previousUnit,previousForm,false);
            return true;
        }
        private void BeginMorph(string previousUnit,HeroForm previousForm,bool armorChange)
        {
            CancelMorph();morphPlaying=true;morphShowActor=false;
            RainLevelGame.Instance?.BeginTransformationPause();
            morph=StartCoroutine(MorphRoutine(previousUnit,previousForm,armorChange));
        }
        private IEnumerator MorphRoutine(string previousUnit,HeroForm previousForm,bool armorChange)
        {
            visual.Renderer.enabled=false;
            RainLevelAudio.Play("departure");
            string exit=previousForm==HeroForm.Red?"TeleportOut":"TeleportDeparture";
            yield return EntranceStep(previousUnit,exit,.18f,true,false);
            yield return RisingBeam(previousForm,.18f);
            RainLevelAudio.Play("arrival");
            if(!armorChange)RainLevelAudio.Play(CurrentForm==HeroForm.Blue?"voiceXSwitchIn":"voiceZeroSwitchIn");
            yield return EntranceStep(CurrentForm==HeroForm.Red?"RedEffects":"Blue",CurrentForm==HeroForm.Red?"TeleportArrivalEnergy":"TeleportBeam",.28f,true,true);
            if(armorChange&&CurrentForm==HeroForm.Red&&enhancedSaber)
            {
                yield return EntranceStep("RedEffects","FireUppercutPose",.35f,true,false);
            }
            yield return EntranceStep(visual.Unit,CurrentForm==HeroForm.Red?"TeleportIn":"TeleportArrival",.48f,false,false);
            if(armorChange&&CurrentForm==HeroForm.Red&&enhancedSaber)
                yield return EntranceStep("Red","Enhanced_Ready",.25f,false,false);
            if(entranceEffect!=null)Destroy(entranceEffect);entranceEffect=null;
            morphPlaying=false;morphShowActor=false;morph=null;visual.Animator.enabled=true;visual.Animator.speed=AnimationSpeed;
            RainLevelGame.Instance?.EndTransformationPause();
            visual.Renderer.enabled=!presentationHidden;SelectLocomotionAnimation();
        }
        private IEnumerator RisingBeam(HeroForm form,float duration)
        {
            string unit=form==HeroForm.Red?"RedEffects":"Blue",clip=form==HeroForm.Red?"TeleportArrivalEnergy":"TeleportBeam";
            if(entranceEffect!=null)Destroy(entranceEffect);
            var entry=assets.Find(unit,clip);visual.RecordObservedClip(unit,clip);
            entranceEffect=new GameObject(form+" departure beam");entranceEffect.transform.SetParent(transform,false);entranceEffect.transform.localScale=Vector3.one*2;
            var render=entranceEffect.AddComponent<SpriteRenderer>();render.sharedMaterial=assets.spriteMaterial;render.sortingOrder=35;
            var animator=entranceEffect.AddComponent<Animator>();animator.runtimeAnimatorController=entry.controller;animator.Rebind();animator.Update(0);animator.enabled=false;
            for(float t=0;t<duration;t+=PresentationDelta)
            {
                entry.clip.SampleAnimation(entranceEffect,Mathf.Min(entry.clip.length-.0001f,(1-t/duration)*entry.clip.length));
                entranceEffect.transform.localPosition=Vector3.up*Mathf.Lerp(0,4,t/duration);yield return null;
            }
            Destroy(entranceEffect);entranceEffect=null;
        }
        private void CancelMorph()
        {
            if(morph!=null)StopCoroutine(morph);morph=null;morphPlaying=false;morphShowActor=false;
            if(entranceEffect!=null)Destroy(entranceEffect);entranceEffect=null;
            if(visual!=null){visual.Animator.enabled=true;visual.Animator.speed=AnimationSpeed;visual.PresentationOffset=Vector3.zero;}
            RainLevelGame.Instance?.EndTransformationPause();
        }
        private float PresentationDelta=>morphPlaying?(RainLevelGame.Instance!=null&&RainLevelGame.Instance.IsPaused?0:Time.unscaledDeltaTime):Time.deltaTime;
        private void TryDash()
        {
            if(climbing||(!IsGrounded&&airDashUsed)||dashTime>0)return;
            if(!IsGrounded)airDashUsed=true;
            dashDirection=input.move.x!=0?Mathf.Sign(input.move.x):facingLeft?-1:1;dashTime=IsGrounded?.28f:.19f;
            facingLeft=dashDirection<0;visual.FacingLeft=facingLeft;
            actionTime=0;actionMelee=false;nova=false;dashStarting=.07f;dashEnding=0;
            visual.Play(CurrentForm==HeroForm.Red?RedClip("DashStart"):IsGrounded?"Dash":"AirDash",true);
            if(CurrentForm==HeroForm.Blue&&ultimateArmor&&!IsGrounded)visual.Seek(.26f);
            RainLevelAudio.Play(IsGrounded?"dash":"dashAlternate");
        }
        private void FireBuster(int power)
        {
            if(actionTime>0||assets==null)return;
            if(power==0)
            {int live=0;foreach(var shot in FindObjectsByType<RainProjectile>(FindObjectsSortMode.None))if(shot.Owner==gameObject)live++;if(live>=3)return;}
            float direction=facingLeft?-1:1;
            Vector2 origin=body.position+new Vector2(direction*.40f,crouching?.34f:.57f);
            var projectile=RainProjectile.Spawn(assets,DamageTeam.Player,gameObject,origin,new Vector2(direction*12.5f,input.move.y>.4f?3.5f:0),
                power==2?8:power==1?4:2,1.8f,"GigaDeathProjectiles","SmallShot",power==2?4:power==1?2.6f:1.4f,power==2?.25f:.12f);
            projectile.Tint=new Color(.25f,.9f,1);shotPose=power>0?.40f:.23f;
            RainLevelAudio.Play(power>0?"chargedShot":"shot");
            string clip=power>0?(IsDashing&&ultimateArmor?"DashChargeShot":"ChargeShot"):climbing?"LadderShoot":wallSliding||wallPose>0?"WallJumpShoot":
                !IsGrounded?(IsDashing&&ultimateArmor?"DashShoot":"JumpShoot"):IsDashing?"DashShoot":Mathf.Abs(input.move.x)>.1f?"RunShoot":input.move.y>.3f?"IdleAim":"Shoot";
            visual.Play(clip,true);
            if(visual.CurrentEntry!=null)visual.Animator.speed=Mathf.Clamp(visual.CurrentEntry.clip.length/shotPose,.6f,4);
            if(crouching&&IsGrounded)HoldCrouchPresentation();
        }
        private void StartSaber()
        {
            combo=combo%3+1;string clip;
            if(wallSliding)clip=enhancedSaber?(combo%3==0?"Enhanced_WallSlashEnd":combo%3==1?"Enhanced_WallSlash":"Enhanced_WallSlashFollowThrough"):"WallSlash";
            else if(dashTime>0)clip=enhancedSaber?"Enhanced_GroundSlideSlash":"DashSlash";
            else if(!IsGrounded)clip=RedClip("AirSlash"+(++airCombo%2+1));
            else if(input.move.y<-.4f)clip="GroundHeavySlash";
            else clip=enhancedSaber&&combo==3?"Enhanced_RisingSlash":RedClip("GroundSlash"+combo);
            StartAction(clip,.28f+(combo==3?.12f:0),true);
        }
        private void StartAction(string clip,float duration,bool melee)
        {
            actionName=clip;actionTime=actionDuration=duration;actionAge=0;actionMelee=melee;attackQueued=false;attackHits.Clear();
            if(melee)RainLevelAudio.Play(CurrentForm==HeroForm.Blue&&(clip=="NovaStrikeStartup"||clip=="AirArmRaise"||clip=="Somersault")?"chargedShot":
                clip.Contains("GroundSlash")||clip=="Enhanced_RisingSlash"?"saber"+Mathf.Clamp(combo,1,3):"saber");
            visual.Play(clip,true);
            if(visual.CurrentEntry!=null)visual.Animator.speed=Mathf.Clamp(visual.CurrentEntry.clip.length/duration,.6f,4)*ActionSpeed;
        }
        private void UpdateAction(float dt)
        {
            if(actionTime<=0)return;
            actionTime-=dt*ActionSpeed;actionAge+=dt*ActionSpeed;
            if(redUltimate)UpdateRedUltimate();
            if(actionMelee&&actionAge>.055f&&actionAge<actionDuration*.9f)
            {
                float direction=facingLeft?-1:1;
                bool upper=actionName.Contains("Uppercut")||actionName.Contains("Rising");
                RainCombat.HitBox(body.position+new Vector2(direction*.65f,upper?1:.58f),new Vector2(1.55f,upper?1.8f:1.1f),
                    DamageTeam.Player,nova?7:enhancedSaber?5:combo==3?5:3,new Vector2(direction,.2f),gameObject,attackHits);
            }
            if(actionTime<=0)
            {
                EndRedUltimate();
                actionMelee=false;nova=false;visual.Animator.speed=AnimationSpeed;
                if(attackQueued&&CurrentForm==HeroForm.Red)StartSaber();
            }
        }
        private void Special()
        {
            specialCooldown=specialCooldownMax=3.5f;float direction=facingLeft?-1:1;charge=0;
            if(CurrentForm==HeroForm.Blue)
            {
                if(input.move.y>.4f)
                {body.linearVelocity=new Vector2(body.linearVelocity.x,JumpSpeed);StartAction(ultimateArmor?"AirArmRaise":"Somersault",.38f,true);}
                else if(input.move.y<-.4f&&ultimateArmor)
                    StartAction(IsGrounded?"SaberGroundSlash":wallSliding?"SaberWallSlash":"SaberAirSlash",.4f,true);
                else {StartAction("NovaStrikeStartup",.6f,true);nova=true;invincible=Mathf.Max(invincible,.55f);}
            }
            else
            {
                if(enhancedSaber&&Mathf.Abs(input.move.y)<.4f)
                {
                    ultimateFacingLeft=facingLeft;
                    StartAction("AirPoseRaised",1.05f,false);redUltimate=true;ultimateWaveEmitted=false;specialCooldown=specialCooldownMax=8;
                    visual.Animator.speed=visual.CurrentEntry.clip.length/.12f*ActionSpeed;
                    body.linearVelocity=Vector2.zero;dashTime=0;RainLevelAudio.Play("saber");
                    ultimateAura=SpawnEffect("RedEffects","PinkFlameAura",body.position,.52f/ActionSpeed,2);
                    if(ultimateAura!=null)ultimateAura.transform.SetParent(transform,true);
                }
                else if(input.move.y>.4f)
                {StartAction(enhancedSaber?"Enhanced_RisingSlash":"AirUppercutPose",.65f,true);body.linearVelocity=new Vector2(direction*2,9.5f);}
                else if(input.move.y<-.4f&&!IsGrounded)
                {StartAction(enhancedSaber?"Enhanced_AirSlashLong":"AirSlashLong"+(char)('A'+airCombo++%3),.65f,true);body.linearVelocity=new Vector2(direction*2,-12);}
                else if(input.move.y<-.4f)
                {
                    StartAction("GroundEnergyStrike",.5f,true);
                    var wave=RainProjectile.Spawn(assets,DamageTeam.Player,gameObject,body.position+new Vector2(direction*.7f,.5f),Vector2.right*direction*7,
                        7,1.8f,"RedEffects","LargeSaberWave",2.1f,.35f);if(wave!=null)wave.Tint=Color.white;
                }
                else if(!IsGrounded)StartAction(enhancedSaber?"Enhanced_SpinSlash":"AirSlashBackhand",.55f,true);
                else StartAction(enhancedSaber?"Enhanced_GroundSlideSlash":"GroundHeavySlash",.5f,true);
            }
        }
        private void UpdateRedUltimate()
        {
            string clip;float duration;
            facingLeft=ultimateFacingLeft;visual.FacingLeft=facingLeft;
            if(actionAge<.12f){clip="AirPoseRaised";duration=.12f;visual.PresentationOffset=Vector3.up*Mathf.SmoothStep(0,.45f,actionAge/.12f);}
            else if(actionAge<.24f){clip="AirPoseLowered";duration=.12f;visual.PresentationOffset=Vector3.up*.45f;}
            else if(actionAge<.52f){clip="GroundEnergyStrike";duration=.28f;visual.PresentationOffset=Vector3.zero;}
            else {clip="GroundHeavySlash";duration=.53f;visual.PresentationOffset=Vector3.zero;}
            if(visual.CurrentAnimation!=clip)
            {visual.Play(clip,true);visual.Animator.speed=Mathf.Max(.5f,visual.CurrentEntry.clip.length/duration)*ActionSpeed;}
            if(ultimateAura!=null)ultimateAura.transform.localPosition=visual.PresentationOffset;
            if(!ultimateWaveEmitted&&actionAge>=.54f)
            {
                ultimateWaveEmitted=true;RainLevelAudio.Play("saber");float direction=facingLeft?-1:1;
                var wave=RainProjectile.Spawn(assets,DamageTeam.Player,gameObject,body.position+new Vector2(direction*.65f,.85f),Vector2.right*direction*10,
                    12,2.1f,"RedEffects","LargeSaberWave",3.7f,.55f);if(wave!=null)wave.Tint=Color.white;
                RainCombat.HitBox(body.position+new Vector2(direction*.8f,.6f),new Vector2(2.4f,1.7f),DamageTeam.Player,8,new Vector2(direction,.3f),gameObject,attackHits);
                FindFirstObjectByType<RainLevelCamera>()?.Shake(.18f,.11f);
            }
        }
        private void EndRedUltimate()
        {
            redUltimate=false;
            if(visual!=null)visual.PresentationOffset=Vector3.zero;
            if(ultimateAura!=null)Destroy(ultimateAura);ultimateAura=null;
        }
        private GameObject SpawnEffect(string unit,string clip,Vector2 position,float duration,float scale)
        {
            var effect=RainProjectile.Spawn(assets,DamageTeam.Player,gameObject,position,Vector2.zero,0,duration,unit,clip,scale);
            if(effect==null)return null;effect.Tint=Color.white;return effect.gameObject;
        }
        private void UpdateEnhancedAura()
        {
            bool visible=enhancedSaber&&CurrentForm==HeroForm.Red&&IsAlive&&!presentationHidden&&!morphPlaying&&!entrancePlaying;
            if(visible&&enhancedAura==null&&assets.TryGet("RedEffects","PinkFlameAura",out enhancedAuraEntry))
            {
                enhancedAura=new GameObject("Zero permanent power aura");enhancedAura.transform.SetParent(transform,false);
                enhancedAura.transform.localScale=Vector3.one*2.1f;
                var renderer=enhancedAura.AddComponent<SpriteRenderer>();renderer.sharedMaterial=assets.spriteMaterial;renderer.sortingOrder=24;
                var animator=enhancedAura.AddComponent<Animator>();animator.runtimeAnimatorController=enhancedAuraEntry.controller;
                animator.Rebind();animator.Update(0);animator.enabled=false;visual.RecordObservedClip("RedEffects","PinkFlameAura");
            }
            if(enhancedAura==null)return;
            enhancedAura.SetActive(visible);
            if(!visible)return;
            enhancedAuraClock+=Time.deltaTime*AnimationSpeed;
            enhancedAura.transform.localPosition=visual.PresentationOffset;
            enhancedAuraEntry.clip.SampleAnimation(enhancedAura,Mathf.Repeat(enhancedAuraClock,enhancedAuraEntry.clip.length));
        }
        private void SelectLocomotionAnimation()
        {
            if(visual==null||entrancePlaying||morphPlaying||cinematicIdle)return;
            if(hurtTime>0){visual.Play(CurrentForm==HeroForm.Red?RedClip("Hurt"):IsGrounded?"HitElectrical":"AirHit");return;}
            if(actionTime>0)
            {
                if(nova&&actionAge>.15f){visual.Play(ultimateArmor?"NovaStrikeBurst":"NovaStrikeTravel");visual.Animator.speed=AnimationSpeed;}
                return;
            }
            if(crouching&&IsGrounded){HoldCrouchPresentation();return;}
            if(shotPose>0)return;
            visual.Animator.speed=AnimationSpeed;
            string clip;
            if(CurrentForm==HeroForm.Blue&&charge>.45f)
            {
                clip=climbing?"LadderShoot":wallSliding||wallPose>0?"WallJumpShootFlash":!IsGrounded?(dashTime>0&&ultimateArmor?"DashShootFlash":"JumpShootFlash"):dashTime>0?"DashShootFlash":
                    Mathf.Abs(input.move.x)>.1f?"RunShootFlash":"IdleAimFlash";
                visual.Play(clip);return;
            }
            if(climbing)clip=CurrentForm==HeroForm.Blue?"LadderClimb":"WallClimb";
            else if(dashTime>0)clip=CurrentForm==HeroForm.Red?RedClip(dashStarting>0?"DashStart":"Dash"):IsGrounded?"Dash":"AirDash";
            else if(dashEnding>0&&CurrentForm==HeroForm.Red)clip=RedClip("DashEnd");
            else if(wallSliding)
            {
                visual.HoldFrame(CurrentForm==HeroForm.Red?RedClip("AirUppercutPose"):"WallJump",3);
                return;
            }
            else if(wallPose>0)clip=CurrentForm==HeroForm.Red?RedClip("AirUppercutPose"):"WallJump";
            else if(!IsGrounded)clip=CurrentForm==HeroForm.Red?RedClip(body.linearVelocity.y>0?"JumpRise":"Fall"):"Jump";
            else if(crouching)clip=CurrentForm==HeroForm.Blue?"CrouchIdle":RedClip("Idle");
            else if(Mathf.Abs(input.move.x)>.1f)clip=CurrentForm==HeroForm.Red?RedClip(runStartTime>0?"RunStart":"Run"):runStartTime>0?"RunStart":"Run";
            else clip=CurrentForm==HeroForm.Red?RedClip("Idle"):ActiveHealth.Normalized<.34f?"AlternateIdle":input.move.y>.3f?"IdleAim":"Idle";
            visual.Play(clip);
        }
        private void HoldCrouchPresentation()
        {
            // Dedicated kneeling artwork, held above the same reduced collision body.
            if(CurrentForm==HeroForm.Blue)visual.HoldFrame(ultimateArmor?"DefeatCollapse":"CrouchIdle",ultimateArmor?1:0);
            else visual.HoldFrame(enhancedSaber?"Enhanced_GroundSlideSlash":"DashSlash",0);
        }
        public void TakeDamage(int amount,Vector2 direction)=>TakeHit(amount,direction,null);
        public void ReceiveDragonDamage(int amount,Vector2 direction,GameObject attacker)=>TakeHit(amount,direction,attacker);
        public void TakeHit(int amount,Vector2 direction,GameObject source)
        {
            var game=RainLevelGame.Instance;
            if(!IsAlive||invincible>0||entrancePlaying||game!=null&&game.InputLocked)return;
            if(ActiveHealth.ApplyDamage(amount)<=0)return;
            RainLevelAudio.Play("hurt");
            visual.Animator.speed=AnimationSpeed;
            invincible=1;hurtTime=.28f;charge=0;actionTime=0;actionMelee=false;nova=false;climbing=false;dashTime=0;
            EndRedUltimate();
            float knock=direction.x==0?(source!=null?Mathf.Sign(transform.position.x-source.transform.position.x):facingLeft?1:-1):Mathf.Sign(direction.x);
            body.linearVelocity=new Vector2(knock*4,4.5f);
            visual.Play(CurrentForm==HeroForm.Red?RedClip(direction.y>.8f?"Electrocuted":"Hurt"):direction.y>.8f?"HitElectrical":"AirHit",true);
            if(!ActiveHealth.IsAlive)game?.HeroDefeated();
        }
        public void Teleport(Vector2 position,bool heal=true)
        {
            EnsureComponents();CancelEntrance();cinematicIdle=false;body.position=position;transform.position=position;body.linearVelocity=Vector2.zero;
            dashTime=actionTime=charge=hurtTime=wallLock=jumpBuffer=idleTime=shotPose=0;climbing=wallSliding=nova=actionMelee=dashJumpMomentum=false;airDashUsed=false;presentationHidden=false;capsule.isTrigger=false;
            if(heal){blueHealth.ResetHealth();redHealth.ResetHealth();}
            invincible=1;UpdateVisualUnit();if(visual.Renderer!=null)visual.Renderer.enabled=true;
        }
        public void SetVisible(bool value){presentationHidden=!value;if(visual!=null&&visual.Renderer!=null)visual.Renderer.enabled=value;}
        public void BeginEntrance(Action completed)
        {
            CancelEntrance();entranceComplete=completed;entrancePlaying=true;presentationHidden=false;
            RainLevelAudio.Play("arrival");
            entrance=StartCoroutine(EntranceRoutine());
        }
        private IEnumerator EntranceRoutine()
        {
            body.linearVelocity=Vector2.zero;visual.Renderer.enabled=false;
            string unit=visual.Unit;
            if(CurrentForm==HeroForm.Blue)
            {
                yield return EntranceStep("Blue","TeleportBeam",.22f,true,true);
                if(!ultimateArmor)yield return EntranceStep("Blue","TeleportOrb",.25f,true,false);
                var arrival=assets.Find(unit,"TeleportArrival");yield return EntranceStep(unit,"TeleportArrival",arrival.clip.length,false,false);
            }
            else
            {
                yield return EntranceStep("RedEffects","TeleportArrivalEnergy",.48f,true,true);
                var arrival=assets.Find("Red","TeleportIn");yield return EntranceStep("Red","TeleportIn",arrival.clip.length,false,false);
            }
            if(entranceEffect!=null)Destroy(entranceEffect);
            entrancePlaying=false;presentationHidden=false;visual.Animator.enabled=true;visual.Renderer.enabled=true;
            visual.Play(CurrentForm==HeroForm.Red?RedClip("Idle"):"Idle",true);invincible=1;
            var done=entranceComplete;entranceComplete=null;entrance=null;done?.Invoke();
        }
        private IEnumerator EntranceStep(string unit,string clip,float duration,bool effect,bool descend)
        {
            var entry=assets.Find(unit,clip);visual.RecordObservedClip(unit,entry.animationName);GameObject target;
            if(morphPlaying)morphShowActor=!effect;
            if(effect)
            {
                if(entranceEffect!=null)Destroy(entranceEffect);
                entranceEffect=new GameObject("Arrival energy");entranceEffect.transform.SetParent(transform,false);entranceEffect.transform.localScale=Vector3.one*2;
                var renderer=entranceEffect.AddComponent<SpriteRenderer>();renderer.sharedMaterial=assets.spriteMaterial;renderer.sortingOrder=35;renderer.flipX=facingLeft;
                var animator=entranceEffect.AddComponent<Animator>();animator.runtimeAnimatorController=entry.controller;animator.Rebind();animator.Update(0);animator.enabled=false;
                target=entranceEffect;visual.Renderer.enabled=false;
            }
            else
            {
                if(entranceEffect!=null){Destroy(entranceEffect);entranceEffect=null;}
                visual.Animator.enabled=true;visual.Animator.runtimeAnimatorController=entry.controller;visual.Animator.Rebind();visual.Animator.Update(0);visual.Animator.enabled=false;
                visual.Renderer.enabled=true;target=visual.Animator.gameObject;
            }
            for(float time=0;time<duration;time+=PresentationDelta)
            {
                entry.clip.SampleAnimation(target,Mathf.Min(time/Mathf.Max(.01f,duration)*entry.clip.length,entry.clip.length-.0001f));
                if(effect&&descend)target.transform.localPosition=Vector3.up*Mathf.Lerp(3.5f,0,Mathf.Clamp01(time/.22f));
                yield return null;
            }
        }
        private void CancelEntrance()
        {
            if(entrance!=null)StopCoroutine(entrance);entrance=null;entrancePlaying=false;
            CancelMorph();EndRedUltimate();
            if(departure!=null)StopCoroutine(departure);departure=null;
            if(entranceEffect!=null)Destroy(entranceEffect);entranceEffect=null;
            if(visual!=null&&visual.Animator!=null){visual.Animator.enabled=true;visual.Animator.speed=AnimationSpeed;}
        }
        public void SetCinematicIdle(bool value)
        {
            EnsureComponents();cinematicIdle=value;
            if(value)
            {
                CancelEntrance();body.linearVelocity=Vector2.zero;dashTime=actionTime=shotPose=charge=hurtTime=invincible=0;
                climbing=wallSliding=nova=actionMelee=false;capsule.isTrigger=false;presentationHidden=false;
                visual.Animator.enabled=true;visual.Animator.speed=AnimationSpeed;visual.Renderer.enabled=true;visual.Renderer.color=Color.white;
                visual.Play(CurrentForm==HeroForm.Red?RedClip("Idle"):"Idle",true);
            }
            else SelectLocomotionAnimation();
        }
        public void SetCinematicFacing(bool left){facingLeft=left;visual.FacingLeft=left;}
        public void BeginDeparture(Action completed)
        {
            CancelEntrance();cinematicIdle=true;body.linearVelocity=Vector2.zero;departure=StartCoroutine(DepartureRoutine(completed));
        }
        public void Celebrate()=>BeginDeparture(null);
        private IEnumerator DepartureRoutine(Action completed)
        {
            visual.Play(CurrentForm==HeroForm.Red?"Enhanced_Ready":ultimateArmor?"IntroSalute":"IdleAim",true);
            yield return new WaitForSeconds(.8f);
            if(CurrentForm==HeroForm.Red)yield return EntranceStep("Red","TurnAround",.18f,false,false);
            string clip=CurrentForm==HeroForm.Red?"TeleportOut":"TeleportDeparture";
            RainLevelAudio.Play("departure");
            yield return EntranceStep(visual.Unit,clip,assets.Find(visual.Unit,clip).clip.length,false,false);
            SetVisible(false);yield return RisingBeam(CurrentForm,.30f);visual.Animator.enabled=true;departure=null;completed?.Invoke();
        }
        public void PlayDefeat(){CancelEntrance();body.linearVelocity=Vector2.zero;RainLevelAudio.Play("death");visual.Play(CurrentForm==HeroForm.Red?"ProneCollapse":ultimateArmor?"DefeatCollapse":"AirHit",true);}
        private void OnDestroy(){if(capsule!=null&&capsule.sharedMaterial!=null)Destroy(capsule.sharedMaterial);}
    }
}

