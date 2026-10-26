using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    public enum HeroForm { Blue, Red }
    public enum DamageTeam { Player, Enemy }
    public enum RainLevelPhase { Intro, Playing, Boss, Dead, Won, FrontEnd, BossIntro, BossCinematic, AxlBoss, Ending }

    public interface IRainDamageable
    {
        DamageTeam Team { get; }
        bool IsAlive { get; }
        void TakeHit(int damage, Vector2 direction, GameObject source);
    }

    public static class RainCombat
    {
        public static IRainDamageable Receiver(Collider2D collider)
        {
            foreach(var component in collider.GetComponentsInParent<MonoBehaviour>())
                if(component is IRainDamageable receiver)return receiver;
            return null;
        }

        public static int HitBox(Vector2 center,Vector2 size,DamageTeam attackingTeam,int damage,
            Vector2 direction,GameObject owner,HashSet<IRainDamageable> alreadyHit=null)
        {
            int hits=0;
            var seen=alreadyHit??new HashSet<IRainDamageable>();
            foreach(var collider in Physics2D.OverlapBoxAll(center,size,0))
            {
                var target=Receiver(collider);
                if(target==null||target.Team==attackingTeam||!target.IsAlive||!seen.Add(target))continue;
                target.TakeHit(damage,direction,owner);hits++;
            }
            return hits;
        }
    }

    public struct RainHeroInput
    {
        public Vector2 move;
        public bool jumpPressed,jumpHeld,dashPressed,attackPressed,attackHeld,attackReleased,specialPressed,switchPressed;
        public bool armorPressed,enhancedPressed;
    }
}
