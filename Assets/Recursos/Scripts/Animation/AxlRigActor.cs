using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>Plays the fixed-part Axl rig. Gameplay owns movement and damage.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class AxlRigActor : MonoBehaviour
    {
        [SerializeField] private Transform facing;
        private Animator animator;
        private int currentState;

        public string CurrentAnimation { get; private set; } = "Idle";

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (facing == null) facing = transform.Find("Facing");
        }

        public bool Play(string stateName, bool restart = false)
        {
            if (animator == null) Awake();
            if (string.IsNullOrWhiteSpace(stateName)) return false;
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash))
            {
                Debug.LogWarning("Axl rig state is not available: " + stateName, this);
                return false;
            }
            if (restart || currentState != hash)
            {
                animator.Play(hash, 0, 0f);
                currentState = hash;
                CurrentAnimation = stateName;
            }
            return true;
        }

        public void FaceLeft(bool faceLeft)
        {
            if (facing == null) Awake();
            if (facing == null) return;
            Vector3 scale = facing.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceLeft ? -1f : 1f);
            facing.localScale = scale;
        }

        public void Configure(Transform facingTransform)
        {
            facing = facingTransform;
        }
    }
}
