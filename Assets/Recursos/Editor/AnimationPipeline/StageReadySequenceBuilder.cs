using System;
using System.IO;
using System.Linq;
using MegaManX9.Animation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    public static class StageReadySequenceBuilder
    {
        public const string TexturePath = "Assets/Recursos/UI/Animated/StageIntro/Generated/ReadyPulse.png";
        public const string PrefabPath = "Assets/Recursos/Prefabs/UI/StageReadySequence.prefab";

        public static Sprite LoadCyanReadySprite()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().ToArray();
            // The first cell is the original blue/aqua word. Later cells are gold palette variants.
            Sprite cyan = sprites.Where(s => s.rect.x < 1).OrderBy(s => s.name, StringComparer.Ordinal).FirstOrDefault();
            if (cyan == null)
                throw new InvalidDataException("The first/cyan READY sprite is unavailable. Rebuild the sprite library first.");
            return cyan;
        }

        /// <summary>Call before saving the stage scene. This does not play or modify the stage controller.</summary>
        public static StageReadySequence Configure(GameObject owner, Material optionalMaterial = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            var sequence = owner.GetComponent<StageReadySequence>();
            if (sequence == null) sequence = owner.AddComponent<StageReadySequence>();
            sequence.Configure(LoadCyanReadySprite(), optionalMaterial);
            sequence.SetLogicalCanvas(new Vector2(1600, 900));
            sequence.SetViewport(new Rect(0, 0, 1600, 900));
            EditorUtility.SetDirty(sequence);
            return sequence;
        }

        /// <summary>Stage-owned timing and drawing: call Advance(scaledDelta) and Draw(logicalRect).</summary>
        public static StageReadySequence AttachTo(GameObject host)
        {
            StageReadySequence sequence = Configure(host);
            sequence.AutoAdvance = false;
            sequence.DrawAutomatically = false;
            return sequence;
        }

        [MenuItem("Mega Man X9/Animations/Rebuild Stage READY Sequence")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding READY.");
            EnsureFolder("Assets/Recursos/Prefabs/UI");
            var owner = new GameObject("Stage READY Sequence");
            try
            {
                Configure(owner);
                PrefabUtility.SaveAsPrefabAsset(owner, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Stage READY sequence prefab: " + PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [MenuItem("Mega Man X9/Animations/Preview Stage READY Sequence")]
        public static void OpenPreview() => StageReadyPreviewWindow.Open();

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }

    public sealed class StageReadyPreviewWindow : EditorWindow
    {
        private StageReadySequence sequence;
        private GameObject owner;
        private UnityEngine.SceneManagement.Scene previewScene;
        private double previousTime;
        private bool paused = true;
        private float speed = 1;

        public static void Open()
        {
            var window = GetWindow<StageReadyPreviewWindow>("Stage READY");
            window.minSize = new Vector2(700, 410); window.Show();
        }

        private void OnEnable()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            owner = new GameObject("READY sequence preview") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(owner, previewScene);
            sequence = StageReadySequenceBuilder.Configure(owner);
            sequence.DrawAutomatically = false;
            sequence.Play(); sequence.SetPlayback(true, speed);
            previousTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
            owner = null; sequence = null;
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (sequence != null)
            {
                sequence.SetPlayback(paused, speed);
                sequence.Tick((float)Math.Min(.1, now - previousTime));
                if (!sequence.IsPlaying) paused = true;
            }
            previousTime = now; Repaint();
        }

        private void OnGUI()
        {
            if (sequence == null) return;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(paused ? "Play" : "Pause", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                if (!sequence.IsPlaying) sequence.Replay();
                paused = !paused;
            }
            if (GUILayout.Button("Replay", EditorStyles.toolbarButton, GUILayout.Width(70)))
            { sequence.Replay(); paused = false; }
            GUILayout.Label("Speed", GUILayout.Width(42)); speed = EditorGUILayout.Slider(speed, .1f, 3, GUILayout.Width(190));
            GUILayout.FlexibleSpace(); GUILayout.Label(sequence.CurrentPhase.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            float chosen = EditorGUILayout.Slider("Time", sequence.Elapsed, 0, sequence.Duration);
            if (Mathf.Abs(chosen - sequence.Elapsed) > .0001f) { sequence.Seek(chosen); paused = true; }
            EditorGUILayout.LabelField($"{sequence.CurrentPhase}  {sequence.PhaseProgress:P0}   /   {sequence.Elapsed:0.000}s of {sequence.Duration:0.000}s");
            Rect area = new Rect(0, 76, position.width, Mathf.Max(1, position.height - 76));
            EditorGUI.DrawRect(area, new Color(.045f, .08f, .12f));
            if (Event.current.type == EventType.Repaint) sequence.DrawInCurrentGUI(area);
        }
    }
}
