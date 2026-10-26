using System.Collections.Generic;
using MegaManX9.Animation;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Fixed-size art child; physics, action timing and input belong to the hero controller.</summary>
    [DisallowMultipleComponent]
    public sealed class RainHeroVisual : MonoBehaviour
    {
        [SerializeField] private RainLevelAssets assets;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(.1f)] private float artScale = 2f;
        [SerializeField] private string unit = "Blue";
        private bool facingLeft;
        private Vector3 presentationOffset;
        public Vector3 PresentationOffset {get=>presentationOffset;set{presentationOffset=value;if(visualRoot!=null)visualRoot.localPosition=value;}}
        public int HeldFrame {get;private set;}=-1;
        private readonly HashSet<string> playedClips = new HashSet<string>();
        public IReadOnlyCollection<string> PlayedClips => playedClips;
        public string Unit => unit;
        public Animator Animator { get { EnsureVisual(); return animator; } }
        public SpriteRenderer Renderer { get { EnsureVisual(); return spriteRenderer; } }
        public Transform VisualRoot { get { EnsureVisual(); return visualRoot; } }
        public string CurrentAnimation { get; private set; }
        public SpriteAnimationLibrary.Entry CurrentEntry { get; private set; }
        public float CurrentDuration => CurrentEntry?.clip != null ? CurrentEntry.clip.length : 0;
        public bool FacingLeft
        {
            get => facingLeft;
            set { facingLeft = value; EnsureVisual(); ApplyFacing(); }
        }
        public bool ArtworkFacesLeft=>unit=="Blue"&&(CurrentAnimation=="AirDash"||CurrentAnimation=="CrouchIdle")||
            unit=="BlueUltimate"&&CurrentAnimation=="DefeatCollapse"||
            unit=="Red"&&(CurrentAnimation=="AirPoseRaised"||CurrentAnimation=="AirPoseLowered");
        private void ApplyFacing(){if(spriteRenderer!=null)spriteRenderer.flipX=facingLeft^ArtworkFacesLeft;}

        private void Awake()
        {
            EnsureVisual();
            if (assets != null) Play("Idle", true);
        }

        public void Configure(RainLevelAssets value)
        {
            assets = value; EnsureVisual();
            if (assets != null) Play("Idle", true);
        }

        private void EnsureVisual()
        {
            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visual");
                if (visualRoot == null)
                {
                    visualRoot = new GameObject("Visual").transform;
                    visualRoot.SetParent(transform, false);
                }
            }
            visualRoot.gameObject.layer = gameObject.layer;
            visualRoot.localPosition = presentationOffset;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = new Vector3(artScale, artScale, 1);
            if (spriteRenderer == null) spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = visualRoot.gameObject.AddComponent<SpriteRenderer>();
            if (animator == null) animator = visualRoot.GetComponent<Animator>();
            if (animator == null) animator = visualRoot.gameObject.AddComponent<Animator>();
            spriteRenderer.sortingOrder = 30;
            ApplyFacing();
            if (assets != null && assets.spriteMaterial != null) spriteRenderer.sharedMaterial = assets.spriteMaterial;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void SetForm(HeroForm form) => SetUnit(form == HeroForm.Red ? "Red" : "Blue");

        /// <summary>BlueUltimate is an optional Blue bank; Red's enhanced actions stay in its Red bank.</summary>
        public void SetUnit(string value)
        {
            if (value != "Blue" && value != "BlueUltimate" && value != "Red") value = "Blue";
            EnsureVisual();
            if (unit == value && CurrentEntry != null) return;
            unit = value; CurrentEntry = null; CurrentAnimation = null;
            Play("Idle", true);
        }

        public bool Play(string animationName, bool restart = false)
        {
            EnsureVisual();
            if (assets == null) return false;
            animator.enabled=true;HeldFrame=-1;
            var entry = assets.Find(unit, animationName);
            if (entry == null || !assets.PlayOn(animator, spriteRenderer, unit, animationName, restart)) return false;
            CurrentEntry = entry; CurrentAnimation = entry.animationName;
            ApplyFacing();
            playedClips.Add(entry.unit + "/" + entry.animationName);
            return true;
        }

        /// <summary>Holds a reviewed contact pose instead of looping the ladder or push-off sequence.</summary>
        public bool HoldFrame(string animationName,int frame)
        {
            if(!Play(animationName))return false;
            HeldFrame=Mathf.Clamp(frame,0,CurrentEntry.frameCount-1);
            animator.enabled=false;
            CurrentEntry.clip.SampleAnimation(animator.gameObject,(HeldFrame+.25f)/Mathf.Max(1,CurrentEntry.clip.frameRate));
            return true;
        }

        public void ClearPlayedClips() => playedClips.Clear();
        /// <summary>Records a clip sampled by an external entrance coordinator, without changing its playback.</summary>
        public bool RecordObservedClip(string bank, string animationName)
        {
            if (assets == null || !assets.TryGet(bank, animationName, out var entry)) return false;
            playedClips.Add(entry.unit + "/" + entry.animationName); return true;
        }

        public bool Has(string animationName) => assets != null && assets.TryGet(unit, animationName, out _);
        public float ClipLength(string animationName) => assets != null ? assets.Find(unit, animationName)?.clip.length ?? 0 : 0;
        public void SetVisible(bool visible) { EnsureVisual(); spriteRenderer.enabled = visible; }

        /// <summary>Allows physics-driven rise/fall poses without changing the character scale or bank.</summary>
        public void Seek(float normalizedTime)
        {
            EnsureVisual();
            if (CurrentEntry?.clip == null) return;
            animator.Play(CurrentEntry.animationName, 0, Mathf.Clamp01(normalizedTime)); animator.Update(0);
        }
    }
}
