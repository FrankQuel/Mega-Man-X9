using System;
using UnityEngine;
using UnityEngine.Video;

namespace MegaManX9.Gameplay
{
    /// <summary>Unskippable startup movie. Only the decoder's loopPointReached event completes it.</summary>
    [DisallowMultipleComponent]
    public sealed class RainStartupVideo : MonoBehaviour
    {
        [SerializeField] private RainStartupVideoAssets assets;
        private VideoPlayer player;
        private RenderTexture target;
        private Action completed;
        private bool prepared;
        private float preparationStarted;
        private GUIStyle errorStyle;

        public bool IsPlaying { get; private set; }
        public bool HasCompleted { get; private set; }
        public bool IsPrepared => prepared;
        public double Duration => VideoClip != null ? VideoClip.length : player != null ? player.length : 0;
        public long CurrentFrame => player != null ? player.frame : -1;
        public VideoClip VideoClip => assets != null ? assets.video : null;
        public string Error { get; private set; } = "";
        public bool UsesDirectAudio=>player!=null&&player.audioOutputMode==VideoAudioOutputMode.Direct;
        public bool DecoderStopped=>player!=null&&!player.enabled;

        public void Configure()
        {
            if (IsPlaying) return;
            try { ConfigurePlayer(); }
            catch (Exception exception) { Error = exception.Message; }
        }

        private void ConfigurePlayer()
        {
            Error = "";
            if (assets == null) assets = RainStartupVideoAssets.Load();
            if (player == null)
            {
                player = gameObject.AddComponent<VideoPlayer>();
                player.prepareCompleted += OnPrepared;
                player.loopPointReached += OnPlaybackFinished;
                player.errorReceived += OnPlaybackError;
            }
            player.enabled=true;
            player.playOnAwake = false; player.isLooping = false; player.waitForFirstFrame = true;
            player.skipOnDrop = false; player.playbackSpeed = 1;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.source = VideoSource.VideoClip; player.clip = VideoClip;
            player.renderMode = VideoRenderMode.RenderTexture; player.aspectRatio = VideoAspectRatio.FitInside;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            if (VideoClip == null)
            {
                Error = "No está configurado el video original. Reconstruye RainStartupVideoAssets antes de iniciar.";
                return;
            }
            player.controlledAudioTrackCount = (ushort)(VideoClip.audioTrackCount > 0 ? 1 : 0);
            if (VideoClip.audioTrackCount > 0)
            {
                player.EnableAudioTrack(0, true);player.SetDirectAudioMute(0,false);player.SetDirectAudioVolume(0,assets!=null?assets.volume:1);
            }
            EnsureTarget((int)VideoClip.width, (int)VideoClip.height);
        }

        public void Play(Action onCompleted)
        {
            if (IsPlaying) return;
            Configure(); completed = onCompleted; HasCompleted = false; prepared = false; IsPlaying = true;
            preparationStarted = Time.realtimeSinceStartup;
            if (!string.IsNullOrEmpty(Error)) return;
            try
            {
                player.Stop(); player.targetTexture = target; player.Prepare();
            }
            catch (Exception exception) { Fail(exception.Message); }
        }

        private void EnsureTarget(int width, int height)
        {
            width = Mathf.Max(2, width); height = Mathf.Max(2, height);
            if (target != null && target.width == width && target.height == height && target.IsCreated()) return;
            if (target != null) { target.Release(); Destroy(target); }
            target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "Startup original movie output", filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp, useMipMap = false, autoGenerateMips = false
            };
            if (!target.Create()) throw new InvalidOperationException("No se pudo crear la salida del video de inicio.");
            var previous = RenderTexture.active;
            try { RenderTexture.active = target; GL.Clear(false, true, Color.black); }
            finally { RenderTexture.active = previous; }
            player.targetTexture = target;
        }

        private void OnPrepared(VideoPlayer source)
        {
            if (!IsPlaying || !string.IsNullOrEmpty(Error)) return;
            try
            {
                EnsureTarget((int)source.width, (int)source.height); prepared = true; source.Play();
            }
            catch (Exception exception) { Fail(exception.Message); }
        }

        private void OnPlaybackFinished(VideoPlayer source)
        {
            if (!IsPlaying || HasCompleted || !string.IsNullOrEmpty(Error)) return;
            // There is no timer, input, or public shortcut to this completion path.
            HasCompleted = true; IsPlaying = false;
            source.Stop();source.enabled=false;
            var callback = completed; completed = null; callback?.Invoke();
        }

        private void OnPlaybackError(VideoPlayer source, string message) => Fail(message);

        private void Fail(string message)
        {
            Error = string.IsNullOrWhiteSpace(message) ? "El decodificador no pudo reproducir el video de inicio." : message;
            prepared = false; HasCompleted = false;
            if (player != null){player.Stop();player.enabled=false;}
            // Keep the overlay and startup lock active. An error must never silently open the menu.
            IsPlaying = true;
        }

        private void Update()
        {
            if (IsPlaying && !prepared && string.IsNullOrEmpty(Error) && Time.realtimeSinceStartup - preparationStarted > 20)
                Fail("El video no pudo prepararse en 20 segundos. Comprueba su importación y el decodificador disponible.");
        }

        private void OnGUI()
        {
            if (!IsPlaying || Event.current.type != EventType.Repaint) return;
            var matrix = GUI.matrix; var color = GUI.color; int depth = GUI.depth;
            try
            {
                GUI.matrix = Matrix4x4.identity; GUI.depth = -10000;
                var screen = new Rect(0, 0, Screen.width, Screen.height);
                GUI.color = Color.black; GUI.DrawTexture(screen, Texture2D.whiteTexture);
                if (!string.IsNullOrEmpty(Error))
                {
                    if (errorStyle == null) errorStyle = new GUIStyle(GUI.skin.label)
                    { fontSize = Mathf.Max(18, Mathf.RoundToInt(Screen.height * .033f)), wordWrap = true, alignment = TextAnchor.MiddleCenter };
                    RainOutlinedGUI.Label(new Rect(40, Screen.height * .32f, Screen.width - 80, Screen.height * .36f),
                        "Error al reproducir el video de inicio.\n" + Error, errorStyle, Color.white);
                }
                else if (prepared && target != null)
                {
                    GUI.color = Color.white; GUI.DrawTexture(screen, target, ScaleMode.ScaleToFit, false);
                }
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
        }

        private void OnDestroy()
        {
            completed = null;
            if (player != null)
            {
                player.prepareCompleted -= OnPrepared; player.loopPointReached -= OnPlaybackFinished;
                player.errorReceived -= OnPlaybackError; player.Stop();
            }
            if (target != null) { target.Release(); Destroy(target); target = null; }
        }
    }
}
