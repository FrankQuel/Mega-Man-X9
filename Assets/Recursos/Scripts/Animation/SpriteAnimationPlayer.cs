using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>Small adapter for gameplay scripts to request the named library states.</summary>
    [RequireComponent(typeof(Animator),typeof(SpriteRenderer))]
    public sealed class SpriteAnimationPlayer : MonoBehaviour
    {
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private int currentState;

        private void Awake()
        {
            animator=GetComponent<Animator>();
            spriteRenderer=GetComponent<SpriteRenderer>();
        }

        public bool Play(string stateName,bool restart=false)
        {
            if(animator==null)Awake();
            int hash=Animator.StringToHash(stateName);
            if(!animator.HasState(0,hash))
            {
                Debug.LogWarning("Animation state is not available: "+stateName,this);
                return false;
            }
            if(currentState!=hash||restart)
            {
                animator.Play(hash,0,0);
                currentState=hash;
            }
            return true;
        }

        public void FaceLeft(bool faceLeft)
        {
            if(spriteRenderer==null)Awake();
            spriteRenderer.flipX=faceLeft;
        }
    }
}
