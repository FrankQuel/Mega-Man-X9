using UnityEngine;

namespace MegaManX9.Gameplay
{
    public enum RainPickupKind { SmallHealth, LargeHealth, ExtraLife, ScoreOrb }

    /// <summary>Collectable original pickup animation; health belongs to the active hero, lives to the shared run.</summary>
    public sealed class RainPickup : MonoBehaviour
    {
        private RainLevelGame game;
        private RainPickupKind kind;
        private SpriteRenderer art;
        private Animator animator;
        private Vector3 restingPosition;
        private float remaining=16,age,fallVelocity;
        private string persistentId;
        private HeroForm lifeForm;
        public RainPickupKind Kind=>kind;
        public bool Collected {get;private set;}
        public string PersistentId=>persistentId;
        public bool IsAuthored=>!string.IsNullOrEmpty(persistentId);
        public string AnimationName {get;private set;}
        public void Configure(RainLevelGame session,RainPickupKind value,Vector2 position)
            =>Configure(session,value,position,null);
        public void Configure(RainLevelGame session,RainPickupKind value,Vector2 position,string rewardId)
        {
            game=session;kind=value;persistentId=rewardId;transform.position=position+Vector2.up*.25f;
            var hit=Physics2D.Raycast((Vector3)position+Vector3.up*.12f,Vector2.down,.4f,1<<8);
            if(hit.collider!=null)transform.position=new Vector3(position.x,hit.point.y+(IsAuthored?.35f:.18f),0);
            restingPosition=transform.position;
            art=gameObject.AddComponent<SpriteRenderer>();art.sharedMaterial=game.Assets.spriteMaterial;art.sortingOrder=24;
            animator=gameObject.AddComponent<Animator>();transform.localScale=Vector3.one*2;
            UpdateAnimation(true);
        }
        private void UpdateAnimation(bool force=false)
        {
            HeroForm form=game.Player==null?HeroForm.Blue:game.Player.CurrentForm;
            if(!force&&(kind!=RainPickupKind.ExtraLife||form==lifeForm))return;
            lifeForm=form;
            string clip=kind==RainPickupKind.ExtraLife?(form==HeroForm.Blue?"BlueEnergyPulse":"RedEnergyPulse"):
                kind==RainPickupKind.SmallHealth?"SmallPickupRotate":kind==RainPickupKind.LargeHealth?"CapsulePulse":"BlueOrbRotate";
            if(kind==RainPickupKind.LargeHealth&&!game.Assets.TryGet("Pickups",clip,out _))clip="LargePickupRotate";
            AnimationName=clip;game.Assets.PlayOn(animator,art,"Pickups",clip,true);
        }
        private void Update()
        {
            if(game==null||game.Player==null){Destroy(gameObject);return;}
            if(game.InputLocked)return;
            age+=Time.deltaTime;if(!IsAuthored)remaining-=Time.deltaTime;
            if(!IsAuthored)SettleDrop(Time.deltaTime);
            transform.position=restingPosition+Vector3.up*(Mathf.Sin(age*3.2f)*.07f);
            if(!IsAuthored&&remaining<=0){Destroy(gameObject);return;}
            art.enabled=IsAuthored||remaining>3||Mathf.Repeat(remaining,.22f)>.08f;UpdateAnimation();
            if(Vector2.Distance(game.Player.transform.position+Vector3.up*.43f,transform.position)<.60f)Collect();
        }
        private void SettleDrop(float delta)
        {
            fallVelocity=Mathf.Min(12,fallVelocity+20*delta);
            var hit=Physics2D.Raycast(restingPosition+Vector3.up*.05f,Vector2.down,fallVelocity*delta+.23f,1<<8);
            if(hit.collider!=null&&hit.normal.y>.65f)
            {restingPosition.y=hit.point.y+.18f;fallVelocity=0;}
            else restingPosition.y-=fallVelocity*delta;
        }
        public bool Collect()
        {
            if(Collected||game==null||game.InputLocked)return false;
            if(!game.RecordPickupCollected(persistentId)){Destroy(gameObject);return false;}
            Collected=true;
            RainLevelAudio.Play(kind==RainPickupKind.ScoreOrb?"score":kind==RainPickupKind.ExtraLife?"extraLife":"pickup");
            if(kind==RainPickupKind.ExtraLife){game.AddLife();game.Notify("VIDA EXTRA · "+game.ActiveHeroName,2.3f);}
            else if(kind==RainPickupKind.ScoreOrb){game.AddScore(100);game.Notify("PUNTOS +100",1.5f);}
            else
            {
                int restored=game.Player.ActiveHealth.Heal(kind==RainPickupKind.LargeHealth?12:5);
                game.Notify("ENERGÍA +"+restored+" · "+game.ActiveHeroName,1.8f);
            }
            Destroy(gameObject);return true;
        }
    }
}
