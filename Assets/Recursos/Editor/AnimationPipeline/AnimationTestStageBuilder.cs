using System;
using System.IO;
using System.Linq;
using MegaManX9.Animation;
using MegaManX9.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MegaManX9.EditorTools
{
    /// <summary>Builds only the generated rehearsal scene; existing work stays open.</summary>
    public static class AnimationTestStageBuilder
    {
        public const string ScenePath = "Assets/Recursos/Scenes/Testing/AnimationTestStage.unity";
        private const string LibraryPath = "Assets/Recursos/Animations/AnimationLibrary.asset";
        private const string DragonPath = "Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab";
        private const string WhitePath = "Assets/Recursos/Prefabs/Bosses/AxlRig/AxlRig_Animated.prefab";
        private const string SourcePath = "Assets/Recursos/Sprites/Environment/escenario jugable.png";
        private const string OutputFolder = "Assets/Recursos/Sprites/Environment/TestStage";
        private const string MaterialPath = "Assets/Recursos/Settings/Rendering/PreviewSpriteUnlit.mat";

        [MenuItem("Mega Man X9/Animations/Build Animation Test Stage")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Sal de Play antes de reconstruir el escenario.");
            var library = AssetDatabase.LoadAssetAtPath<SpriteAnimationLibrary>(LibraryPath);
            if (library == null || library.entries.Count == 0)
                throw new InvalidDataException("Falta la biblioteca de animaciones. Ejecuta Rebuild Animation Library primero.");
            var dragon = RequireRig(DragonPath, "Dragón articulado");
            GameObject axl = null; // The user selected the original white sprite sheet.
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) throw new InvalidDataException("Falta el material 2D sin iluminación: " + MaterialPath);
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (existing.IsValid() && existing.isLoaded && existing.isDirty && !Application.isBatchMode &&
                !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { existing })) return;
            EnsureFolder(OutputFolder);
            Sprite city = CreateCrop("CityBackdrop", new RectInt(4, 1004, 1200, 180), false);
            Sprite platform = CreateCrop("PlatformRail", new RectInt(850, 180, 560, 86), true);
            Sprite pixel = CreatePixel();
            var previous = SceneManager.GetActiveScene();
            bool reopenExisting = existing.IsValid() && existing.isLoaded;
            bool previousWasStage = reopenExisting && previous == existing;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                if (reopenExisting) EditorSceneManager.CloseScene(existing, true);
                var cameraObject = NewObject("Stage Camera"); cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true; camera.orthographicSize = 6.5f;
                camera.transform.position = new Vector3(0, 0, -20);
                camera.backgroundColor = new Color(.025f, .043f, .075f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.cullingMask = 1 << AnimationTestStage.PreviewLayer;
                cameraObject.AddComponent<AudioListener>();
                var environment = NewObject("Industrial Environment").transform;
                AddSprite("Distant industrial skyline", city, new Vector3(0, .8f, 2),
                    new Vector2(23.1f, 3.465f), new Color(.53f, .66f, .83f), -30, material, environment);
                AddSprite("Upper city reflection", city, new Vector3(0, 4.15f, 3),
                    new Vector2(23.1f, 3.465f), new Color(.18f, .28f, .42f), -40, material, environment);
                foreach (float x in new[] { -11.2f, 11.2f })
                {
                    AddSprite("Steel tower", pixel, new Vector3(x, -.2f, 1), new Vector2(.36f, 10.6f), new Color(.08f, .15f, .23f), -20, material, environment);
                    AddSprite("Tower light", pixel, new Vector3(x - .06f, -.2f, .9f), new Vector2(.045f, 10.6f), new Color(.06f, .4f, .49f), -19, material, environment);
                }
                var platforms = NewObject("Six Rehearsal Platforms"); platforms.transform.SetParent(environment);
                for (int i = 0; i < 6; i++)
                {
                    var anchor = AnimationTestStage.PositionFromCanvas(280 + i % 3 * 520, 356 + i / 3 * 310);
                    CreatePlatform("Station platform " + (i + 1), platform, pixel, anchor, 6.3f, material, platforms.transform);
                }
                var individualPlatform = NewObject("Individual Rehearsal Platform"); individualPlatform.transform.SetParent(environment);
                CreatePlatform("Large rehearsal platform", platform, pixel,
                    AnimationTestStage.PositionFromCanvas(565, 627), 10.8f, material, individualPlatform.transform);
                individualPlatform.SetActive(false);
                var roots = new Transform[6];
                for (int i = 0; i < roots.Length; i++)
                {
                    var actor = NewObject("Station " + (i + 1)); roots[i] = actor.transform;
                    var renderer = actor.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = 10;
                    actor.AddComponent<Animator>();
                }
                var stage = NewObject("Animation Test Stage").AddComponent<AnimationTestStage>();
                stage.Configure(library, camera, roots, dragon, axl, material, platforms, individualPlatform);
                var hudAssets=AssetDatabase.LoadAssetAtPath<HealthHUDAssets>(HealthHUDBuilder.AssetsPath);
                if(hudAssets==null)throw new InvalidDataException("Build the character health HUD first.");
                var hud=stage.gameObject.AddComponent<AnimationStageHealthHUD>();hud.Configure(hudAssets);
                stage.ConfigurePresentation(StageReadySequenceBuilder.AttachTo(stage.gameObject),hud);
                stage.PrepareForCapture(0, 0);
                stage.ValidateCoverage();
                camera.rect=new Rect(0,0,1,1);camera.aspect=16f/9f;
                // Preparation above also leaves useful character poses visible before Play.
                EnsureFolder("Assets/Recursos/Scenes/Testing");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("No se pudo guardar " + ScenePath);
                var buildScenes = EditorBuildSettings.scenes.Where(item => File.Exists(item.path) && item.path != ScenePath).ToList();
                buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Directory.CreateDirectory("Tools/AnimationPipeline");
                var report = new StageReport { success = true, scene = ScenePath, libraryClips = stage.LibraryClipCount,
                    rigClips = stage.RigClipCount, units = stage.GroupCount, pages = stage.PageCount, simultaneousStations = 6,
                    backgroundSource = SourcePath };
                File.WriteAllText("Tools/AnimationPipeline/test_stage_validation.json", JsonUtility.ToJson(report, true));
                AssetDatabase.SaveAssets();
                Debug.Log("ANIMATION_TEST_STAGE_SUCCESS " + JsonUtility.ToJson(report));
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (reopenExisting)
                {
                    var reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                    if (previousWasStage) SceneManager.SetActiveScene(reopened);
                }
                if (!previousWasStage && previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static GameObject RequireRig(string path, string label)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidDataException("Falta " + label + ": " + path + ". Genera su prefab antes de construir la escena.");
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null || animator.runtimeAnimatorController.animationClips.Length == 0)
                throw new InvalidDataException("El prefab " + label + " no tiene un Animator con clips.");
            return prefab;
        }

        private static Sprite CreateCrop(string name, RectInt crop, bool removeMagenta)
        {
            string source = File.Exists(SourcePath) ? SourcePath : "SourceArt/Sprites/Environment/escenario jugable.png";
            if (!File.Exists(source)) throw new FileNotFoundException("Falta el atlas del escenario.", source);
            var original = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var result = new Texture2D(crop.width, crop.height, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(original, File.ReadAllBytes(source), false)) throw new InvalidDataException("El atlas no es una imagen válida.");
                if (crop.xMax > original.width || crop.yMax > original.height) throw new InvalidDataException("El recorte excede el atlas.");
                var pixels = original.GetPixels(crop.x, original.height - crop.yMax, crop.width, crop.height);
                if (removeMagenta)
                    for (int i = 0; i < pixels.Length; i++) if (pixels[i].r > .95f && pixels[i].g < .05f && pixels[i].b > .95f) pixels[i].a = 0;
                result.SetPixels(pixels); result.Apply();
                string path = OutputFolder + "/" + name + ".png";
                File.WriteAllBytes(path, result.EncodeToPNG());
                return ImportSingle(path);
            }
            finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(result); }
        }

        private static Sprite CreatePixel()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixel(0, 0, Color.white); texture.Apply();
                string path = OutputFolder + "/StagePixel.png";
                File.WriteAllBytes(path, texture.EncodeToPNG()); return ImportSingle(path);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Sprite ImportSingle(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
            importer.spritePivot = new Vector2(.5f, .5f); importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static GameObject NewObject(string name)
        {
            return new GameObject(name) { layer = AnimationTestStage.PreviewLayer };
        }

        private static GameObject AddSprite(string name, Sprite sprite, Vector3 position, Vector2 size, Color color,
            int order, Material material, Transform parent)
        {
            var obj = NewObject(name); obj.transform.SetParent(parent, false); obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            renderer.sortingOrder = order; renderer.sharedMaterial = material; return obj;
        }

        private static void CreatePlatform(string name, Sprite rail, Sprite pixel, Vector3 top, float width, Material material, Transform parent)
        {
            float height = width * rail.rect.height / rail.rect.width;
            var obj = AddSprite(name, rail, top + new Vector3(0, -height * .5f, .4f), new Vector2(width, height),
                new Color(.7f, .82f, .94f), -5, material, parent);
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(rail.bounds.size.x, rail.bounds.size.y * .28f);
            collider.offset = new Vector2(0, rail.bounds.size.y * .32f);
            AddSprite(name + " guide light", pixel, top + new Vector3(0, -.015f, .3f), new Vector2(width, .035f),
                new Color(.16f, .78f, .85f), -4, material, parent);
        }

        /// <summary>Renders the first six stations into a PNG; IMGUI remains visible in Play Mode.</summary>
        [MenuItem("Mega Man X9/Animations/Capture Test Stage Preview")]
        public static void CapturePreview()
        {
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Construye el escenario de prueba primero.", ScenePath);
            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool ownsScene = !scene.IsValid() || !scene.isLoaded;
            var previous = SceneManager.GetActiveScene();
            if (ownsScene) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            RenderTexture target = null; Texture2D image = null; RenderTexture previousTarget = null;
            Camera camera = null; RenderTexture previousCameraTarget = null; Rect previousRect = default;
            try
            {
                var stage = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<AnimationTestStage>(true)).Single();
                if (ownsScene) stage.PrepareForCapture(0, .25f);
                camera = stage.StageCamera;
                previousCameraTarget = camera.targetTexture; previousRect = camera.rect;
                target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target; camera.rect = new Rect(0, 0, 1, 1); camera.aspect = 16f / 9f;
                if (GraphicsSettings.currentRenderPipeline != null)
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                else camera.Render();
                previousTarget = RenderTexture.active; RenderTexture.active = target;
                image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
                Directory.CreateDirectory("Tools/AnimationPipeline/QA");
                File.WriteAllBytes("Tools/AnimationPipeline/QA/AnimationTestStage.png", image.EncodeToPNG());
                Debug.Log("ANIMATION_TEST_STAGE_CAPTURE Tools/AnimationPipeline/QA/AnimationTestStage.png");
            }
            finally
            {
                if (camera != null) { camera.targetTexture = previousCameraTarget; camera.rect = previousRect; }
                RenderTexture.active = previousTarget;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                if (ownsScene && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }

        [Serializable] private sealed class StageReport
        {
            public bool success;
            public string scene, backgroundSource;
            public int libraryClips, rigClips, units, pages, simultaneousStations;
        }
    }
}
