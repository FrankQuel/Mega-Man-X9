using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.Gameplay
{
    /// <summary>Highlights the player's current primary bindings on the supplied keyboard illustration.</summary>
    [DisallowMultipleComponent]
    public sealed class RainControlsGuide : MonoBehaviour
    {
        private const float Width = 1600, Height = 900;
        [SerializeField] private RainControlsGuideAssets assets;
        private RainLevelGame game;
        private RainInputBindings.BindingSet bindings;
        private Action completed;
        private int openedFrame, lastAdvanceFrame = -1;
        private bool waitingForEnterRelease;
        private GUIStyle title, subtitle, legend, number, hint;
        public bool IsActive { get; private set; }
        public int DisplayedBindingCount => bindings?.keys?.Length ?? 0;
        public int HighlightedBindingCount
        {
            get
            {
                int count = 0;
                if (assets != null && bindings?.keys != null)
                    foreach (int key in bindings.keys) if (assets.TryGetKeyRect((Key)key, out _)) count++;
                return count;
            }
        }
        public bool IsWaitingForEnterRelease => waitingForEnterRelease;
        public RainControlsGuideAssets Assets => assets;
        private bool Paused => game != null && game.IsPaused;

        public void Configure(RainLevelGame session)
        {
            Cancel(); game = session;
            if (assets == null) assets = RainControlsGuideAssets.Load();
            RequireAssets();
        }

        private void RequireAssets()
        {
            if (assets == null) assets = RainControlsGuideAssets.Load();
            if (assets == null || !assets.IsReady)
                throw new InvalidOperationException("Falta la guía de teclado. Ejecuta RainControlsGuideAssetsBuilder.Build() antes de jugar.");
        }

        public void Play(Action onCompleted)
        {
            RequireAssets(); Cancel();
            if (game == null) game = RainLevelGame.Instance;
            completed = onCompleted; IsActive = true; openedFrame = Time.frameCount;
            waitingForEnterRelease = true; RefreshBindings();
        }

        public void Cancel()
        {
            IsActive = false; completed = null; bindings = null;
            waitingForEnterRelease = false; lastAdvanceFrame = -1;
        }

        private void RefreshBindings() => bindings = RainInputBindings.CopyBindings();

        public Key GetDisplayedKey(RainInputAction action)
        {
            if (IsActive) RefreshBindings();
            return bindings != null ? bindings.Get(action) : Key.None;
        }

        public bool TryGetDisplayedKeyRect(RainInputAction action, out Rect pixelsFromTopLeft)
        {
            Key key = GetDisplayedKey(action);
            if (assets != null) return assets.TryGetKeyRect(key, out pixelsFromTopLeft);
            pixelsFromTopLeft = default; return false;
        }

        private void Update()
        {
            if (!IsActive) return;
            RefreshBindings();
            if (Paused || Time.frameCount <= openedFrame) return;
            var keyboard = Keyboard.current;
            bool held = keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
            if (waitingForEnterRelease)
            {
                if (!held) waitingForEnterRelease = false;
                return;
            }
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)) Advance();
        }

        public void Advance()
        {
            if (!IsActive || Paused || waitingForEnterRelease || Time.frameCount <= openedFrame || lastAdvanceFrame == Time.frameCount) return;
            lastAdvanceFrame = Time.frameCount;
            var callback = completed; completed = null; IsActive = false;
            callback?.Invoke();
        }

        private void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            subtitle = new GUIStyle(title) { fontSize = 18, fontStyle = FontStyle.Normal };
            legend = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            number = new GUIStyle(title) { fontSize = 16 };
            hint = new GUIStyle(title) { fontSize = 23 };
        }

        private void OnGUI()
        {
            if (!IsActive || Event.current.type != EventType.Repaint) return;
            Styles(); RefreshBindings();
            var previousMatrix = GUI.matrix; var previousColor = GUI.color; int previousDepth = GUI.depth;
            try
            {
                GUI.matrix = Matrix4x4.identity; GUI.depth = -5500;
                Fill(new Rect(0, 0, Screen.width, Screen.height), Color.black);
                float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
                GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2,
                    (Screen.height - Height * scale) / 2), Quaternion.identity, Vector3.one * scale);
                RainOutlinedGUI.Label(new Rect(80, 25, 1440, 55), "CONTROLES · TECLAS PRINCIPALES", title, Color.white);
                RainOutlinedGUI.Label(new Rect(80, 82, 1440, 34), "Tus teclas actuales, incluidos los cambios guardados en Opciones", subtitle,
                    new Color(.78f, .89f, 1));
                Rect keyboard = Fit(new Rect(80, 132, 1440, 533), assets.keyboard.width / (float)assets.keyboard.height);
                GUI.color = Color.white; GUI.DrawTexture(keyboard, assets.keyboard, ScaleMode.ScaleToFit, true);
                for (int i = 0; i < bindings.keys.Length; i++)
                {
                    Color color = Color.HSVToRGB((i * .071f + .51f) % 1, .62f, 1);
                    Key key = (Key)bindings.keys[i];
                    bool mapped = assets.TryGetKeyRect(key, out var pixels);
                    if (mapped)
                    {
                        Rect rect = new Rect(keyboard.x + pixels.x / assets.sourceSize.x * keyboard.width,
                            keyboard.y + pixels.y / assets.sourceSize.y * keyboard.height,
                            pixels.width / assets.sourceSize.x * keyboard.width, pixels.height / assets.sourceSize.y * keyboard.height);
                        Highlight(rect, color, i + 1);
                    }
                    int column = i / 7, row = i % 7;
                    var marker = new Rect(92 + column * 742, 692 + row * 23, 22, 22);
                    Fill(marker, color);
                    RainOutlinedGUI.Label(marker, (i + 1).ToString(), number, Color.white, 1);
                    string text = RainInputBindings.KeyLabel(key) + " · " + RainInputBindings.Label((RainInputAction)i);
                    if (!mapped) text += " (solo leyenda)";
                    RainOutlinedGUI.Label(new Rect(marker.xMax + 12, marker.y - 2, 685, 26), text, legend, Color.white);
                }
                RainOutlinedGUI.Label(new Rect(480, 861, 640, 34), Paused ? "PAUSA" : "Enter · continuar", hint,
                    new Color(.86f, .95f, 1));
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.depth = previousDepth; }
        }

        private void Highlight(Rect rect, Color color, int index)
        {
            Fill(rect, new Color(color.r, color.g, color.b, .18f));
            Fill(new Rect(rect.x, rect.y, rect.width, 3), color); Fill(new Rect(rect.x, rect.yMax - 3, rect.width, 3), color);
            Fill(new Rect(rect.x, rect.y, 3, rect.height), color); Fill(new Rect(rect.xMax - 3, rect.y, 3, rect.height), color);
            var badge = new Rect(rect.xMax - 25, rect.yMax - 22, 22, 20);
            Fill(badge, new Color(color.r * .35f, color.g * .35f, color.b * .35f, .98f));
            RainOutlinedGUI.Label(badge, index.ToString(), number, Color.white, 1);
        }

        private static Rect Fit(Rect rect, float aspect)
        {
            float width = Mathf.Min(rect.width, rect.height * aspect), height = width / aspect;
            return new Rect(rect.center.x - width / 2, rect.center.y - height / 2, width, height);
        }

        private static void Fill(Rect rect, Color color)
        { GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); }

        private void OnDestroy() => Cancel();
    }
}
