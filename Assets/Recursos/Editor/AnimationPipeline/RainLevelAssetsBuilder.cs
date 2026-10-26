using System;
using System.IO;
using System.Linq;
using MegaManX9.Animation;
using MegaManX9.Gameplay;
using MegaManX9.UI;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    public static class RainLevelAssetsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Settings/Gameplay/RainLevelAssets.asset";
        [MenuItem("Mega Man X9/Playable Rain Level/Build Shared Art References")]
        private static void BuildMenu() => Build();

        public static RainLevelAssets Build()
        {
            RainFrontEndAssetsBuilder.Build();
            RainAudioAssetsBuilder.Build();
            RainNarrativeAssetsBuilder.Build();
            RainStartupVideoAssetsBuilder.Build();
            RainPauseAssetsBuilder.Build();
            RainControlsGuideAssetsBuilder.Build();
            RainPresentationSettingsBuilder.Build();
            var library = AssetDatabase.LoadAssetAtPath<SpriteAnimationLibrary>("Assets/Recursos/Animations/AnimationLibrary.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Recursos/Settings/Rendering/PreviewSpriteUnlit.mat");
            var hud = AssetDatabase.LoadAssetAtPath<HealthHUDAssets>(HealthHUDBuilder.AssetsPath);
            var dragon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Recursos/Prefabs/Bosses/DragonRig/DragonRig_Animated.prefab");
            if (library == null || material == null || hud == null || dragon == null)
                throw new InvalidDataException("Faltan biblioteca, material, HUD o DragonRig. Genera los recursos existentes antes del nivel de lluvia.");
            EnsureFolder("Assets/Recursos/Settings/Gameplay");
            var assets = AssetDatabase.LoadAssetAtPath<RainLevelAssets>(AssetPath);
            if (assets == null) { assets = ScriptableObject.CreateInstance<RainLevelAssets>(); AssetDatabase.CreateAsset(assets, AssetPath); }
            assets.animationLibrary = library; assets.spriteMaterial = material; assets.healthHUD = hud; assets.dragonRigPrefab = dragon;
            assets.Invalidate();
            foreach (string unit in new[] { "Blue", "BlueUltimate", "Red" })
            {
                if (!assets.TryGet(unit, "Idle", out var idle) || idle.frameCount < 1)
                    throw new InvalidDataException("Falta el Idle de " + unit);
                if (assets.UnitAnimations(unit).Count < 25) throw new InvalidDataException("Banco de acciones incompleto para " + unit);
            }
            if (assets.Find("Blue", "Hurt")?.animationName != "HitElectrical" ||
                assets.Find("Red", "Death")?.animationName != "ProneCollapse")
                throw new InvalidDataException("No se pudieron resolver los roles de animación del jugador.");
            EditorUtility.SetDirty(assets); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Tools/AnimationPipeline");
            File.WriteAllText("Tools/AnimationPipeline/rain_level_asset_report.json", JsonUtility.ToJson(new Report
            {
                success = true, asset = AssetPath, blueClips = assets.UnitAnimations("Blue").Count,
                ultimateClips = assets.UnitAnimations("BlueUltimate").Count, redClips = assets.UnitAnimations("Red").Count,
                libraryEntries = library.entries.Count
            }, true));
            Debug.Log("RAIN_LEVEL_ASSETS_SUCCESS " + AssetPath);
            return assets;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        [Serializable] private sealed class Report
        {
            public bool success;
            public string asset;
            public int blueClips, ultimateClips, redClips, libraryEntries;
        }
    }
}
