using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.Gameplay
{
    /// <summary>Word-based narrative playback. Manual QA and Enter use the same Advance operation.</summary>
    [DisallowMultipleComponent]
    public sealed class RainNarrative : MonoBehaviour
    {
        private enum Transition { None, BlackLead, FadeIn, FadeOut }
        private const float Width = 1600, Height = 900, FadeSeconds = .18f;
        [SerializeField] private RainNarrativeAssets assets;
        [SerializeField, Range(1, 24)] private float wordsPerSecond = 8;
        private RainLevelGame game;
        private RainNarrativePage[] pages = Array.Empty<RainNarrativePage>();
        private string[] words = Array.Empty<string>();
        private readonly string[] history = new string[4];
        private string visibleText = "";
        private Action completed;
        private Transition transition;
        private float transitionTime, wordTime;
        private int pendingPage, lastAdvanceFrame = -1;
        private bool finishAfterFade;
        private GUIStyle prose, dialogue, nameStyle, hint, destination;

        public bool IsActive { get; private set; }
        public bool IsIntroduction { get; private set; }
        public bool IsEndingDialogue { get; private set; }
        public bool IsMissionArrival { get; private set; }
        public int CurrentPage { get; private set; } = -1;
        public int RevealedWordCount { get; private set; }
        public int RevealedWords => RevealedWordCount;
        public int TotalWordCount => words.Length;
        public int PageCount => pages.Length;
        public string CurrentText => Page?.text ?? "";
        public string VisibleText => visibleText;
        public Texture2D CurrentImage => IsIntroduction ? Page?.image : null;
        public RainNarrativeAssets Assets => assets;
        public RainNarrativeSpeaker ActiveSpeaker => Page?.speaker ?? RainNarrativeSpeaker.Narrator;
        public bool IsTextComplete => IsActive && RevealedWordCount >= words.Length;
        public bool IsTransitioning => IsActive && transition != Transition.None;
        private RainNarrativePage Page => CurrentPage >= 0 && CurrentPage < pages.Length ? pages[CurrentPage] : null;
        private bool Paused => game != null && game.IsPaused;

        public void Configure(RainLevelGame session)
        {
            Cancel(); game = session;
            if (assets == null) assets = RainNarrativeAssets.Load();
            RequireAssets();
        }

        private void RequireAssets()
        {
            if (assets == null) assets = RainNarrativeAssets.Load();
            if (assets == null || !assets.IsReady)
                throw new InvalidOperationException("Faltan recursos narrativos. Ejecuta RainNarrativeAssetsBuilder.Build() antes de jugar.");
        }

        public void PlayIntroduction(Action onCompleted)
        {
            RequireAssets(); Begin(assets.introduction, true, false, onCompleted);
            transition = Transition.BlackLead; transitionTime = 0;
        }

        public void PlayConfrontation(bool ending, Action onCompleted)
        {
            RequireAssets(); Begin(ending ? assets.ending : assets.confrontation, false, ending, onCompleted);
        }

        public void PlayMissionArrival(HeroForm initial, Action onCompleted)
        {
            RequireAssets();
            Begin(initial == HeroForm.Red ? assets.missionArrivalZero : assets.missionArrivalX, false, false, onCompleted, true);
        }

        private void Begin(RainNarrativePage[] sequence, bool introduction, bool ending, Action onCompleted, bool missionArrival = false)
        {
            Cancel(); if (game == null) game = RainLevelGame.Instance;
            pages = sequence; completed = onCompleted; IsActive = true;
            IsIntroduction = introduction; IsEndingDialogue = ending; IsMissionArrival = missionArrival;
            Array.Clear(history, 0, history.Length); SetPage(0);
            transition = Transition.FadeIn; transitionTime = 0;
            RainLevelAudio.SetConversation(true);
        }

        public void Cancel()
        {
            IsActive = IsIntroduction = IsEndingDialogue = IsMissionArrival = false;
            completed = null; pages = Array.Empty<RainNarrativePage>(); words = Array.Empty<string>();
            CurrentPage = -1; RevealedWordCount = 0; visibleText = ""; wordTime = 0;
            transition = Transition.None; transitionTime = 0; finishAfterFade = false; lastAdvanceFrame = -1;
            RainLevelAudio.SetConversation(false);
        }

        private void SetPage(int index)
        {
            CurrentPage = index; RevealedWordCount = 0; wordTime = 0; visibleText = "";
            words = CurrentText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private void Update()
        {
            if (!IsActive || Paused) return;
            float delta = Time.unscaledDeltaTime;
            if (transition != Transition.None) UpdateTransition(delta);
            else
            {
                wordTime += delta * wordsPerSecond;
                Reveal(Mathf.Min(words.Length, Mathf.FloorToInt(wordTime)));
            }
            if (!IsActive) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                Advance();
        }

        private void UpdateTransition(float delta)
        {
            transitionTime += delta;
            if (transition == Transition.BlackLead)
            {
                if (transitionTime < .55f) return;
                transition = Transition.FadeIn; transitionTime = 0; return;
            }
            if (transitionTime < FadeSeconds) return;
            if (transition == Transition.FadeOut)
            {
                if (finishAfterFade) { Finish(); return; }
                SetPage(pendingPage); transition = Transition.FadeIn; transitionTime = 0;
            }
            else { transition = Transition.None; transitionTime = 0; }
        }

        private void Reveal(int count)
        {
            if (count == RevealedWordCount) return;
            RevealedWordCount = count;
            visibleText = count == 0 ? "" : string.Join(" ", words, 0, count);
        }

        /// <summary>One press completes the words; a later press advances. It cannot skip a transition or pause.</summary>
        public void Advance()
        {
            if (!IsActive || Paused || IsTransitioning || lastAdvanceFrame == Time.frameCount) return;
            lastAdvanceFrame = Time.frameCount;
            if (!IsTextComplete) { Reveal(words.Length); wordTime = words.Length; return; }
            if (Page != null) history[(int)Page.speaker] = CurrentText;
            int next = CurrentPage + 1;
            if (next >= pages.Length)
            {
                finishAfterFade = true; transition = Transition.FadeOut; transitionTime = 0;
            }
            else if (IsIntroduction)
            {
                pendingPage = next; finishAfterFade = false;
                transition = Transition.FadeOut; transitionTime = 0;
            }
            else SetPage(next);
        }

        private void Finish()
        {
            var callback = completed; completed = null;
            IsActive = IsIntroduction = IsEndingDialogue = IsMissionArrival = false; transition = Transition.None;
            RainLevelAudio.SetConversation(false); callback?.Invoke();
        }

        private float ContentAlpha => transition == Transition.BlackLead ? 0 : transition == Transition.FadeIn ?
            Mathf.Clamp01(transitionTime / FadeSeconds) : transition == Transition.FadeOut ?
            1 - Mathf.Clamp01(transitionTime / FadeSeconds) : 1;

        private void Styles()
        {
            if (prose != null) return;
            prose = new GUIStyle(GUI.skin.label) { fontSize = 26, wordWrap = true, richText = false, alignment = TextAnchor.UpperLeft };
            dialogue = new GUIStyle(prose) { fontSize = 23 };
            nameStyle = new GUIStyle(prose) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            hint = new GUIStyle(prose) { fontSize = 19, alignment = TextAnchor.MiddleCenter };
            destination = new GUIStyle(prose) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
        }

        private void OnGUI()
        {
            if (!IsActive || Event.current.type != EventType.Repaint) return;
            Styles(); var matrix = GUI.matrix; var color = GUI.color; int depth = GUI.depth;
            try
            {
                GUI.depth = -5000; GUI.matrix = Matrix4x4.identity;
                float alpha = ContentAlpha;
                Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, IsIntroduction ? 1 : .65f * alpha));
                if (transition == Transition.BlackLead) return;
                float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2,
                    (Screen.height - Height * scale) / 2), Quaternion.identity, Vector3.one * scale);
                if (IsIntroduction) DrawIntroduction(alpha); else DrawConversation(alpha);
                string instruction = Paused ? "PAUSA" : IsTextComplete ? "Enter · continuar" : "Enter · completar texto";
                RainOutlinedGUI.Label(new Rect(490, 845, 620, 34), instruction, hint, new Color(.82f, .92f, 1, alpha));
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
        }

        private void DrawIntroduction(float alpha)
        {
            if (CurrentImage == null)
            {
                RainOutlinedGUI.Label(new Rect(220, 335, 1160, 230), visibleText, destination, new Color(1, 1, 1, alpha));
                return;
            }
            var imageRect = Fit(new Rect(215, 38, 1170, 570), CurrentImage.width / (float)CurrentImage.height);
            Frame(imageRect, new Color(.25f, .66f, .85f, alpha), .12f * alpha);
            GUI.color = new Color(1, 1, 1, alpha); GUI.DrawTexture(imageRect, CurrentImage, ScaleMode.ScaleToFit, true);
            var textRect = new Rect(160, 640, 1280, 190);
            Frame(textRect, new Color(.28f, .73f, .92f, alpha), .94f * alpha);
            RainOutlinedGUI.Label(new Rect(190, 666, 1220, 140), visibleText, prose, new Color(1, 1, 1, alpha));
        }

        private void DrawConversation(float alpha)
        {
            DrawSpeaker(new Rect(42, 470, 718, 183), RainNarrativeSpeaker.X, "X", new Color(.18f, .73f, 1), false, alpha);
            DrawSpeaker(new Rect(42, 666, 718, 183), RainNarrativeSpeaker.Zero, "Zero", new Color(1, .28f, .32f), false, alpha);
            if (!IsMissionArrival)
                DrawSpeaker(new Rect(840, 545, 718, 228), RainNarrativeSpeaker.Axl, "Axl", new Color(.76f, .56f, 1), true, alpha);
        }

        private void DrawSpeaker(Rect rect, RainNarrativeSpeaker speaker, string defaultName, Color accent, bool right, float alpha)
        {
            bool active = ActiveSpeaker == speaker;
            float emphasis = (active ? 1 : .36f) * alpha;
            Frame(rect, new Color(accent.r, accent.g, accent.b, emphasis), (active ? .96f : .74f) * alpha);
            float portraitX = right ? rect.xMax - 162 : rect.x + 17;
            var portrait = assets.Portrait(speaker);
            var portraitRect = new Rect(portraitX, rect.y + 38, 145, 126);
            if (portrait != null)
            {
                // X/Zero face left in their source images; Axl faces right. Mirror at draw time to face inward.
                Rect fitted = Fit(portraitRect, portrait.width / (float)portrait.height);
                GUI.color = new Color(1, 1, 1, emphasis);
                GUI.DrawTextureWithTexCoords(fitted, portrait, new Rect(1, 0, -1, 1), true);
            }
            string label = active && !string.IsNullOrEmpty(Page.speakerName) ? Page.speakerName : defaultName;
            RainOutlinedGUI.Label(new Rect(rect.x + 18, rect.y + 5, rect.width - 36, 30), label, nameStyle,
                new Color(accent.r, accent.g, accent.b, emphasis));
            float textX = right ? rect.x + 20 : rect.x + 179;
            string text = active ? visibleText : history[(int)speaker] ?? "";
            RainOutlinedGUI.Label(new Rect(textX, rect.y + 43, 518, rect.height - 55), text, dialogue,
                new Color(1, 1, 1, emphasis));
        }

        private static Rect Fit(Rect rect, float aspect)
        {
            float width = Mathf.Min(rect.width, rect.height * aspect), height = width / aspect;
            return new Rect(rect.center.x - width / 2, rect.center.y - height / 2, width, height);
        }

        private static void Frame(Rect rect, Color border, float opacity)
        {
            Fill(rect, new Color(.018f, .035f, .075f, opacity));
            Fill(new Rect(rect.x, rect.y, rect.width, 2), border);
            Fill(new Rect(rect.x, rect.yMax - 2, rect.width, 2), border);
            Fill(new Rect(rect.x, rect.y, 2, rect.height), border);
            Fill(new Rect(rect.xMax - 2, rect.y, 2, rect.height), border);
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private void OnDestroy() => Cancel();
    }
}
