using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>A real multi-joint sprite rig. Gameplay owns world movement and damage.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class DragonRigActor : MonoBehaviour
    {
        [SerializeField] private DragonRigLibrary library;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform mouthSocket;
        [SerializeField] private Transform clawSocket;
        [SerializeField] private bool autoReturnToIdle = true;
        private Animator animator;
        private DragonRigLibrary.Entry currentEntry;
        private int currentHash;

        public DragonRigLibrary Library => library;
        public Animator Animator => animator != null ? animator : (animator = GetComponent<Animator>());
        public Transform MouthSocket => mouthSocket;
        public Transform ClawSocket => clawSocket;
        public Transform HandSocket => clawSocket;
        public Transform FootSocket => transform.Find("Visual/Hips/NearThigh/NearShin/NearFoot/FootSocket");
        public string CurrentClip
        {
            get
            {
                var attacks = GetComponent<DragonBossAttackPreview>();
                return attacks != null && !string.IsNullOrEmpty(attacks.CurrentAttack) ? attacks.CurrentAttack : currentEntry != null ? currentEntry.name : string.Empty;
            }
        }
        public IReadOnlyList<string> ClipNames => library != null ? library.ClipNames : Array.Empty<string>();
        public bool AutoReturnToIdle { get => autoReturnToIdle; set => autoReturnToIdle = value; }

        public void Configure(DragonRigLibrary source, Transform visualRoot, Transform mouth, Transform claw)
        {
            library = source; visual = visualRoot; mouthSocket = mouth; clawSocket = claw;
            if (source != null) Animator.runtimeAnimatorController = source.controller;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (visual == null) visual = transform.Find("Visual");
        }

        private void Start()
        {
            var attacks = GetComponent<DragonBossAttackPreview>();
            if (attacks != null && !string.IsNullOrEmpty(attacks.CurrentAttack)) return;
            if (string.IsNullOrEmpty(CurrentClip)) Play("Idle");
        }

        /// <summary>Standalone enemy entry point. The rehearsal stage calls BeginAttack/Advance directly.</summary>
        public bool PlayAttack(string attackName, bool automaticallyAdvance = true)
        {
            var attacks = GetComponent<DragonBossAttackPreview>();
            if (attacks == null) return false;
            attacks.AutoAdvance = automaticallyAdvance;
            attacks.AutoReturnToCombatIdle = automaticallyAdvance;
            return attacks.BeginAttack(attackName, automaticallyAdvance);
        }

        public bool Play(string clipName, bool restart = false)
        {
            var attacks = GetComponent<DragonBossAttackPreview>();
            if (attacks != null && !string.IsNullOrEmpty(attacks.CurrentAttack))
            {
                if (!restart && attacks.CurrentAttack == clipName) return true;
                return attacks.BeginAttack(clipName, attacks.AutoAdvance);
            }
            var entry = library != null ? library.Find(clipName) : null;
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning("Dragon rig clip unavailable: " + clipName, this);
                return false;
            }
            int hash = UnityEngine.Animator.StringToHash("Base Layer." + clipName);
            if (!Animator.HasState(0, hash)) return false;
            if (currentEntry != entry || restart)
            {
                currentEntry = entry; currentHash = hash;
                Animator.Play(hash, 0, 0f);
            }
            return true;
        }

        /// <summary>The painted bind pose faces left. Mirror the entire skeleton as one unit.</summary>
        public void FaceLeft(bool faceLeft)
        {
            if (visual == null) visual = transform.Find("Visual");
            if (visual != null) visual.localScale = new Vector3(faceLeft ? 1f : -1f, 1f, 1f);
        }

        private void Update()
        {
            if (!autoReturnToIdle || currentEntry == null || currentEntry.loop || currentEntry.holdLastPose) return;
            var state = Animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == currentHash && state.normalizedTime >= 1f) Play("Idle");
        }
    }
}
