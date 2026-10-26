using System;
using UnityEngine;

namespace MegaManX9.Animation
{
    /// <summary>A screen-space stage intro. Gameplay can wait for Completed before spawning actors.</summary>
    [DisallowMultipleComponent]
    public sealed class StageReadySequence : MonoBehaviour
    {
        public enum Phase { Idle, LineSweep, ExpansionFlash, TextReveal, Hold, Split, Complete }

        [SerializeField] private Sprite readySprite;
        [SerializeField] private Vector2 logicalCanvasSize = new Vector2(1600, 900);
        [SerializeField] private Rect viewport = new Rect(0, 0, 1600, 900);
        [SerializeField, Range(.15f, .8f)] private float wordWidthRatio = .48f;
        [SerializeField, Range(.1f, .9f)] private float centerHeightRatio = .47f;
        [SerializeField] private float lineSeconds = .28f;
        [SerializeField] private float flashSeconds = .18f;
        [SerializeField] private float revealSeconds = .16f;
        [SerializeField] private float holdSeconds = .82f;
        [SerializeField] private float splitSeconds = .36f;
        [SerializeField] private bool drawAutomatically = true;
        [SerializeField] private bool autoAdvance = true;

        private float elapsed, playbackSpeed = 1;
        private bool paused, completionRaised;

        public event Action Completed;
        public bool IsPlaying { get; private set; }
        public Phase CurrentPhase { get; private set; } = Phase.Idle;
        public float Elapsed => elapsed;
        public float Duration => LineDuration + FlashDuration + RevealDuration + HoldDuration + SplitDuration;
        public float NormalizedTime => Mathf.Clamp01(elapsed / Duration);
        public float PhaseProgress { get; private set; }
        public bool IsPaused => paused;
        public float PlaybackSpeed => playbackSpeed;
        public Sprite ReadySprite => readySprite;
        public bool DrawAutomatically { get => drawAutomatically; set => drawAutomatically = value; }
        public bool AutoAdvance { get => autoAdvance; set => autoAdvance = value; }

        private float LineDuration => Mathf.Max(.001f, lineSeconds);
        private float FlashDuration => Mathf.Max(.001f, flashSeconds);
        private float RevealDuration => Mathf.Max(.001f, revealSeconds);
        private float HoldDuration => Mathf.Max(.001f, holdSeconds);
        private float SplitDuration => Mathf.Max(.001f, splitSeconds);

        /// <summary>The first/cyan sprite is held throughout. Orange palette frames are not cycled.</summary>
        public void Configure(Sprite cyanReadySprite, Material optionalMaterial = null)
        {
            readySprite = cyanReadySprite;
            // IMGUI draws the supplied RGBA directly; a SpriteRenderer material is unnecessary.
        }

        public void Configure(Sprite[] cyanFirstFrames, Material optionalMaterial = null)
        {
            Configure(cyanFirstFrames != null && cyanFirstFrames.Length > 0 ? cyanFirstFrames[0] : null, optionalMaterial);
        }

        public void SetLogicalCanvas(Vector2 size)
        {
            logicalCanvasSize = new Vector2(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }

        public void SetViewport(Rect logicalViewport)
        {
            if (logicalViewport.width <= 0 || logicalViewport.height <= 0)
                throw new ArgumentOutOfRangeException(nameof(logicalViewport), "READY viewport must have positive dimensions.");
            viewport = logicalViewport;
        }

        public void SetCanvasRect(Rect logicalViewport) => SetViewport(logicalViewport);

        public void SetPlayback(bool isPaused, float speed)
        {
            paused = isPaused;
            playbackSpeed = float.IsNaN(speed) || float.IsInfinity(speed) ? 1 : Mathf.Clamp(speed, 0, 8);
        }

        public void Play(Rect logicalViewport)
        {
            SetViewport(logicalViewport);
            Play();
        }

        public void Play()
        {
            if (readySprite == null)
                throw new InvalidOperationException("Configure the cyan READY sprite before starting the stage intro.");
            elapsed = 0;
            completionRaised = false;
            IsPlaying = true;
            UpdatePhase();
        }

        public void Replay() => Play();

        public void Reset()
        {
            elapsed = 0;
            completionRaised = false;
            IsPlaying = false;
            CurrentPhase = Phase.Idle;
            PhaseProgress = 0;
        }

        /// <summary>Deterministic inspection; seeking never emits the gameplay Completed event.</summary>
        public void Seek(float seconds)
        {
            elapsed = Mathf.Clamp(seconds, 0, Duration);
            IsPlaying = elapsed < Duration;
            completionRaised = elapsed >= Duration;
            UpdatePhase();
        }

        private void Update()
        {
            if (autoAdvance) Tick(Time.deltaTime);
        }

        /// <summary>Accepts raw time and applies this sequence's playback-speed multiplier.</summary>
        public void Tick(float deltaSeconds)
        {
            if (playbackSpeed <= 0) return;
            Advance(deltaSeconds * playbackSpeed);
        }

        /// <summary>External orchestration supplies time already scaled by the stage. No second speed multiplier.</summary>
        public void Advance(float deltaSeconds)
        {
            if (!IsPlaying || paused) return;
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return;
            elapsed = Mathf.Min(Duration, elapsed + Mathf.Max(0, deltaSeconds));
            UpdatePhase();
            if (elapsed < Duration) return;
            IsPlaying = false;
            if (completionRaised) return;
            completionRaised = true;
            Completed?.Invoke();
        }

        private void UpdatePhase()
        {
            if (elapsed >= Duration)
            {
                CurrentPhase = Phase.Complete;
                PhaseProgress = 1;
                return;
            }
            float time = elapsed;
            if (Take(ref time, LineDuration, Phase.LineSweep)) return;
            if (Take(ref time, FlashDuration, Phase.ExpansionFlash)) return;
            if (Take(ref time, RevealDuration, Phase.TextReveal)) return;
            if (Take(ref time, HoldDuration, Phase.Hold)) return;
            if (Take(ref time, SplitDuration, Phase.Split)) return;
            CurrentPhase = Phase.Complete;
            PhaseProgress = 1;
        }

        private bool Take(ref float time, float duration, Phase phase)
        {
            if (time < duration)
            {
                CurrentPhase = phase;
                PhaseProgress = Mathf.Clamp01(time / duration);
                return true;
            }
            time -= duration;
            return false;
        }

        private void OnGUI()
        {
            if (!drawAutomatically || !IsPlaying || readySprite == null || Event.current.type != EventType.Repaint) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            int oldDepth = GUI.depth;
            float scale = Mathf.Min(Screen.width / logicalCanvasSize.x, Screen.height / logicalCanvasSize.y);
            Vector3 offset = new Vector3((Screen.width - logicalCanvasSize.x * scale) / 2,
                (Screen.height - logicalCanvasSize.y * scale) / 2, 0);
            try
            {
                GUI.depth = -10000;
                GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
                DrawInCurrentGUI(viewport);
            }
            finally { GUI.matrix = oldMatrix; GUI.depth = oldDepth; }
        }

        /// <summary>Draws into an existing GUI coordinate system, including the standalone editor preview.</summary>
        public void Draw(Rect logicalViewport) => DrawInCurrentGUI(logicalViewport);

        /// <summary>Draws into an existing GUI coordinate system, including the standalone editor preview.</summary>
        public void DrawInCurrentGUI(Rect drawingViewport)
        {
            if (readySprite == null || CurrentPhase == Phase.Idle || CurrentPhase == Phase.Complete) return;
            Color oldColor = GUI.color;
            GUI.BeginGroup(drawingViewport);
            try
            {
                float width = drawingViewport.width, height = drawingViewport.height;
                float centerY = height * centerHeightRatio;
                float wordWidth = width * wordWidthRatio;
                float wordHeight = wordWidth * readySprite.rect.height / readySprite.rect.width;
                Rect word = new Rect((width - wordWidth) / 2, centerY - wordHeight / 2, wordWidth, wordHeight);
                float progress = PhaseProgress;
                switch (CurrentPhase)
                {
                    case Phase.LineSweep:
                        // A screen-wide blue streak enters from the left and leaves to the right.
                        float lineX = Mathf.Lerp(-width, width, progress);
                        Line(new Rect(lineX, centerY - 1, width, 2), 1, width / 320f);
                        break;
                    case Phase.ExpansionFlash:
                        float burst = Mathf.Sin(progress * Mathf.PI);
                        Fill(new Rect(0, 0, width, height), new Color(.72f, .98f, 1, burst * .64f));
                        Fill(new Rect(0, centerY - burst * height * .48f, width, burst * height * .96f),
                            new Color(.25f, .91f, 1, burst * .22f));
                        Line(new Rect(0, centerY - 1, width, 2), 1 - progress, width / 320f);
                        break;
                    case Phase.TextReveal:
                        float reveal = Mathf.SmoothStep(0, 1, progress);
                        float shownHeight = Mathf.Max(1, word.height * reveal);
                        Rect revealWindow = new Rect(word.x, centerY - shownHeight / 2, word.width, shownHeight);
                        GUI.BeginGroup(revealWindow);
                        DrawWord(new Rect(0, -(word.height - shownHeight) / 2, word.width, word.height), 1, 0, 1);
                        GUI.EndGroup();
                        Line(new Rect(0, centerY - 1, width, 2), (1 - reveal) * .65f, width / 320f);
                        break;
                    case Phase.Hold:
                        // A small settle keeps the pixel artwork intact; the orange palette is never sampled.
                        float settle = Mathf.Sin(Mathf.Clamp01(progress * 5) * Mathf.PI) * height / 240f;
                        word.y -= settle;
                        DrawShadowedWord(word, 1, width / 320f);
                        break;
                    case Phase.Split:
                        float slide = Mathf.Pow(progress, 1.35f);
                        float distance = (width + wordWidth) * .5f * slide;
                        float alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((progress - .58f) / .42f));
                        // The word and its echo begin coincident, then leave toward opposite sides.
                        // Both retain all five letters; the original cyan sprite is sampled in full.
                        Rect left = new Rect(word.x - distance, word.y, word.width, word.height);
                        Rect right = new Rect(word.x + distance, word.y, word.width, word.height);
                        DrawShadowedWord(left, alpha, width / 320f);
                        DrawShadowedWord(right, alpha, width / 320f);
                        Line(new Rect(0, centerY - 1, width, 2), (1 - progress) * .3f, width / 320f);
                        break;
                }
            }
            finally { GUI.EndGroup(); GUI.color = oldColor; }
        }

        private void DrawShadowedWord(Rect destination, float opacity, float pixelScale)
        {
            Rect shadow = destination;
            shadow.x += pixelScale * 1.2f;
            shadow.y += pixelScale * .7f;
            DrawWord(shadow, opacity * .65f, 0, 1, new Color(.10f, .28f, .36f, 1));
            DrawWord(destination, opacity, 0, 1);
        }

        private void DrawWord(Rect destination, float opacity, float horizontalStart, float horizontalFraction, Color? tint = null)
        {
            Texture2D texture = readySprite.texture;
            Rect source = readySprite.rect;
            Rect uv = new Rect((source.x + source.width * horizontalStart) / texture.width,
                source.y / texture.height, source.width * horizontalFraction / texture.width,
                source.height / texture.height);
            Color color = tint ?? Color.white;
            color.a *= opacity;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(destination, texture, uv, true);
        }

        private static void Line(Rect core, float alpha, float pixelScale)
        {
            if (alpha <= 0) return;
            float thickness = Mathf.Max(1, pixelScale);
            Fill(new Rect(core.x, core.center.y - thickness * 2, core.width, thickness * 4),
                new Color(.06f, .48f, 1, alpha * .35f));
            Fill(new Rect(core.x, core.center.y - thickness, core.width, thickness * 2),
                new Color(.08f, .94f, 1, alpha * .75f));
            Fill(new Rect(core.x, core.center.y - thickness * .35f, core.width, Mathf.Max(1, thickness * .7f)),
                new Color(.84f, 1, 1, alpha));
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }
    }
}
