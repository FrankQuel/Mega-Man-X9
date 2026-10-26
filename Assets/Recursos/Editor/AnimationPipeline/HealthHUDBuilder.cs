using System;
using System.IO;
using MegaManX9.UI;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    /// <summary>Reuses extracted PS1 HUD pieces and color swatches; original files stay intact.</summary>
    public static class HealthHUDBuilder
    {
        public const string AssetsPath = "Assets/Recursos/UI/HUD/Generated/HealthHUDAssets.asset";
        private const string Folder = "Assets/Recursos/UI/HUD/Generated";
        private const string HudSource = "Assets/Recursos/UI/HUD/hud.png";
        private const string ElementsSource = "SourceArt/UI/HUD/HUD Elements.png";

        [MenuItem("Mega Man X9/Animations/Build Health HUD")]
        public static void Build()
        {
            EnsureFolder(Folder);
            var original = LoadSource(HudSource, "SourceArt/UI/HUD/hud.png");
            var elements = LoadSource(ElementsSource, "Assets/Recursos/UI/HUD/HUD Elements.png");
            try
            {
                var blueTexture = PlayerFrame(original, 5);
                var redTexture = PlayerFrame(original, 45);
                var bossTexture = BossFrame(original);
                Sprite blue, red, boss, horizontalBlue, horizontalRed, horizontalBoss;
                try
                {
                    blue = SaveSprite(blueTexture, "HealthFrameBlue");
                    red = SaveSprite(redTexture, "HealthFrameRed");
                    boss = SaveSprite(bossTexture, "HealthFrameBoss");
                    horizontalBlue = SaveHorizontalFrame(blueTexture, elements, "HealthFrameXHorizontal", false, false);
                    horizontalRed = SaveHorizontalFrame(redTexture, elements, "HealthFrameZeroHorizontal", true, false);
                    horizontalBoss = SaveHorizontalFrame(bossTexture, elements, "HealthFrameBossHorizontal", false, true);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(blueTexture); UnityEngine.Object.DestroyImmediate(redTexture);
                    UnityEngine.Object.DestroyImmediate(bossTexture);
                }
                var assets = AssetDatabase.LoadAssetAtPath<HealthHUDAssets>(AssetsPath);
                if (assets == null) { assets = ScriptableObject.CreateInstance<HealthHUDAssets>(); AssetDatabase.CreateAsset(assets, AssetsPath); }
                assets.healthUnit = Extract(elements, "HealthUnit", new RectInt(68, 38, 5, 1));
                assets.damageUnit = Extract(elements, "DamageUnit", new RectInt(104, 71, 5, 1));
                assets.normalIndicator = Indicator(elements, "HealthNormal", 38, 339);
                assets.cautionIndicator = Indicator(elements, "HealthCaution", 120, 339);
                assets.dangerIndicator = Indicator(elements, "HealthDanger", 200, 339);
                assets.lifeCounterRing = LifeCounterRing(elements);
                assets.bossSkull = Extract(original, "HealthBossSkull", new RectInt(197, 64, 26, 19));
                assets.horizontalBossEmblemWindow = new Rect(0, 5, 20, 18);
                assets.nativeHealthUnits = 28;
                assets.styles = new[]
                {
                    Style("Blue", "X", blue, horizontalBlue, false), Style("BlueUltimate", "X · armadura definitiva", blue, horizontalBlue, false),
                    Style("Red", "Zero", red, horizontalRed, false),
                    Style("Axl", "Axl · poses originales", boss, horizontalBoss, true),
                    Style("AxlRig", "Axl", boss, horizontalBoss, true),
                    Style("Dragon", "Dragón · poses originales", boss, horizontalBoss, true), Style("DragonRig", "Dragón", boss, horizontalBoss, true)
                };
                EditorUtility.SetDirty(assets); AssetDatabase.SaveAssets();
                ValidateModel();
                Directory.CreateDirectory("Tools/AnimationPipeline");
                File.WriteAllText("Tools/AnimationPipeline/hud_v3_validation.json", JsonUtility.ToJson(new Report
                    { success = true, healthUnits = 28, styles = assets.styles.Length, modelChecks = 8, source = HudSource,
                        elements = ElementsSource, assets = AssetsPath }, true));
                Debug.Log("HEALTH_HUD_BUILD_SUCCESS " + AssetsPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(elements); }
        }

        private static HealthHUDAssets.Style Style(string unit, string label, Sprite frame, Sprite horizontal, bool boss)
        {
            return new HealthHUDAssets.Style { unit = unit, label = label, frame = frame, boss = boss,
                fillWindow = new Rect(boss ? 5 : 9, 4, 5, 28),
                statusWindow = boss ? new Rect(3, 37, 11, 11) : new Rect(20, 37, 12, 12),
                horizontalFrame = horizontal,
                horizontalFillWindow = new Rect(boss ? 53 : 93, 11, 80, 5),
                horizontalStatusWindow = new Rect(boss ? 21 : 35, 2, 24, 24),
                horizontalLivesWindow = boss ? Rect.zero : new Rect(62, 2, 24, 24),
                horizontalValueWindow = new Rect(boss ? 49 : 89, 20, 88, 8) };
        }

        private static Sprite SaveHorizontalFrame(Texture2D vertical, Texture2D elements, string name, bool red, bool boss)
        {
            var frame = NewTransparent(boss ? 140 : 180, 28);
            try
            {
                if (boss)
                {
                    // The horizontal drawer places the original skull here. An absent
                    // skull leaves the slot transparent rather than displaying the old empty plaque.
                }
                else
                {
                    // Keep the complete X/Z badge upright. Only the extendable HP channel changes orientation.
                    int sx = red ? 239 : 68, sy = red ? 209 : 177, width = red ? 34 : 31;
                    for (int y = 0; y < 28; y++) for (int x = 0; x < width; x++)
                        SetTop(frame, x, y, GetTop(elements, sx + x, sy + y));
                    // Recolor only the empty badge plate, keeping its original bevel and silhouette.
                    Color plate = GetTop(elements, red ? 246 : 79, red ? 216 : 188);
                    var plateRect = red ? new RectInt(13, 16, 13, 7) : new RectInt(12, 17, 13, 7);
                    for (int y = plateRect.y; y < plateRect.yMax; y++) for (int x = plateRect.x; x < plateRect.xMax; x++)
                        SetTop(frame, x, y, plate);
                }
                int barX = boss ? 49 : 89;
                int nativeLeft = boss ? 2 : 6;
                // The top cap is mirrored at the other end, and one original middle row extends the channel.
                // This avoids turning the character emblem sideways or stretching the cap thickness.
                for (int y = 0; y < 12; y++) for (int x = 0; x < 88; x++)
                {
                    int sourceY = x < 4 ? x : x >= 84 ? 87 - x : 16;
                    SetTop(frame, barX + x, 8 + y, GetTop(vertical, nativeLeft + y, sourceY));
                }
                Clear(frame, new RectInt(barX + 4, 11, 80, 5)); frame.Apply();
                return SaveSprite(frame, name);
            }
            finally { UnityEngine.Object.DestroyImmediate(frame); }
        }

        private static Sprite LifeCounterRing(Texture2D elements)
        {
            var texture = Crop(elements, new RectInt(38, 339, 24, 24));
            try
            {
                int[,] cuts = { {23,16,1}, {23,17,1}, {22,18,2}, {21,19,3}, {20,20,4}, {19,21,5}, {18,22,6}, {16,23,8} };
                for (int i = 0; i < cuts.GetLength(0); i++) Clear(texture, new RectInt(cuts[i, 0], cuts[i, 1], cuts[i, 2], 1));
                // Only the separate life counter gets a clear center. HealthNormal remains untouched.
                Clear(texture, new RectInt(8, 8, 8, 8)); texture.Apply();
                return SaveSprite(texture, "SharedLifeCounterRing");
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Texture2D PlayerFrame(Texture2D source, int sourceX)
        {
            // The atlas intentionally separates its extendable bar pieces by one blank row.
            // Joining those pieces gives a continuous 28-pixel interior and preserves the X/Z icon.
            var frame = NewTransparent(35, 52);
            int destinationY = 0;
            for (int y = 27; y < 81; y++)
            {
                if (y == 37 || y == 54) continue;
                for (int x = 0; x < 35; x++) SetTop(frame, x, destinationY, GetTop(source, sourceX + x, y));
                destinationY++;
            }
            Clear(frame, new RectInt(9, 4, 5, 28)); frame.Apply(); return frame;
        }

        private static Texture2D BossFrame(Texture2D source)
        {
            // Neutral original frame pieces avoid labelling Axl/Dragon with Sigma's face.
            var frame = NewTransparent(17, 49);
            int destinationY = 0;
            for (int y = 27; y < 65; y++)
            {
                if (y == 37 || y == 54) continue;
                for (int x = 0; x < 12; x++) SetTop(frame, x + 2, destinationY, GetTop(source, x + 11, y));
                destinationY++;
            }
            for (int y = 0; y < 13; y++)
                for (int x = 0; x < 17; x++) SetTop(frame, x, y + 36, GetTop(source, x + 117, y + 133));
            Clear(frame, new RectInt(5, 4, 5, 28)); frame.Apply(); return frame;
        }

        private static Sprite Indicator(Texture2D source, string name, int x, int y)
        {
            var texture = Crop(source, new RectInt(x, y, 24, 24));
            try
            {
                // Exclude only the text tab protruding beyond the native circular silhouette.
                int[,] cuts = { {23,16,1}, {23,17,1}, {22,18,2}, {21,19,3}, {20,20,4}, {19,21,5}, {18,22,6}, {16,23,8} };
                for (int i = 0; i < cuts.GetLength(0); i++) Clear(texture, new RectInt(cuts[i, 0], cuts[i, 1], cuts[i, 2], 1));
                texture.Apply(); return SaveSprite(texture, name);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Sprite Extract(Texture2D source, string name, RectInt rect)
        {
            var texture = Crop(source, rect);
            try { return SaveSprite(texture, name); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Texture2D Crop(Texture2D source, RectInt rect)
        {
            if (rect.x < 0 || rect.y < 0 || rect.xMax > source.width || rect.yMax > source.height)
                throw new InvalidDataException("Recorte HUD fuera del original: " + rect);
            var texture = NewTransparent(rect.width, rect.height);
            texture.SetPixels(source.GetPixels(rect.x, source.height - rect.yMax, rect.width, rect.height));
            texture.Apply(); return texture;
        }

        private static Texture2D NewTransparent(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[width * height]); return texture;
        }
        private static Color GetTop(Texture2D texture, int x, int y) => texture.GetPixel(x, texture.height - 1 - y);
        private static void SetTop(Texture2D texture, int x, int y, Color color) => texture.SetPixel(x, texture.height - 1 - y, color);
        private static void Clear(Texture2D texture, RectInt rect)
        {
            for (int y = rect.y; y < rect.yMax; y++) for (int x = rect.x; x < rect.xMax; x++) SetTop(texture, x, y, Color.clear);
        }

        private static Sprite SaveSprite(Texture2D texture, string name)
        {
            string path = Folder + "/" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None; importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidDataException("No se importó el sprite HUD " + path);
            return sprite;
        }

        private static Texture2D LoadSource(string preferred, string fallback)
        {
            string path = File.Exists(preferred) ? preferred : fallback;
            if (!File.Exists(path)) throw new FileNotFoundException("Falta un original del HUD.", preferred);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            { UnityEngine.Object.DestroyImmediate(texture); throw new InvalidDataException("No se pudo leer " + path); }
            return texture;
        }

        [MenuItem("Mega Man X9/Animations/Validate Health Model")]
        public static void ValidateModel()
        {
            var obj = new GameObject("HealthModelValidation") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var health = obj.AddComponent<CharacterHealth>(); health.Configure(28);
                int changes = 0; health.Changed += _ => changes++;
                Require(health.CurrentHealth == 28 && health.MaxHealth == 28, "initial state");
                Require(health.ApplyDamage(5) == 5 && health.CurrentHealth == 23, "damage");
                Require(health.Heal(3) == 3 && health.CurrentHealth == 26, "healing");
                Require(health.ApplyDamage(int.MaxValue) == 26 && health.CurrentHealth == 0 && !health.IsAlive, "lower bound");
                Require(health.Heal(int.MaxValue) == 28 && health.CurrentHealth == 28, "upper bound / overflow");
                Require(health.ApplyDamage(-1) == 0 && health.Heal(-1) == 0 && changes == 4, "negative amounts / events");
                health.ApplyDamage(4); health.ResetHealth();
                Require(health.CurrentHealth == 28 && changes == 6, "reset / events");
                health.Configure(14, false);
                Require(health.MaxHealth == 14 && health.CurrentHealth == 14 && changes == 7 && Mathf.Approximately(health.Normalized, 1), "maximum change");
                Debug.Log("HEALTH_MODEL_VALIDATION_SUCCESS 8 checks");
            }
            finally { UnityEngine.Object.DestroyImmediate(obj); }
        }

        private static void Require(bool result, string check)
        { if (!result) throw new InvalidOperationException("Health model validation failed: " + check); }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        [Serializable] private sealed class Report
        {
            public bool success;
            public int healthUnits, styles, modelChecks;
            public string source, elements, assets;
        }
    }
}
