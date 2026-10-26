using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MegaManX9.EditorTools
{
    public static class RainPresentationSettingsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Resources/RainPresentationSettings.asset";
        private const string DevelopmentMenu = "Mega Man X9/Presentation Mode/Development";
        private const string ProductionMenu = "Mega Man X9/Presentation Mode/Production";
        public static RainPresentationSettings Build()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RainPresentationSettings>(AssetPath);
            if (asset != null) return asset;
            if (!AssetDatabase.IsValidFolder("Assets/Recursos/Resources")) AssetDatabase.CreateFolder("Assets/Recursos", "Resources");
            asset = ScriptableObject.CreateInstance<RainPresentationSettings>();
            asset.SetMode(RainPresentationMode.Production);
            AssetDatabase.CreateAsset(asset, AssetPath); AssetDatabase.SaveAssetIfDirty(asset);
            RainPresentationSettings.Reload(); return asset;
        }
        [MenuItem(DevelopmentMenu)] public static void UseDevelopment() => Set(RainPresentationMode.Development);
        [MenuItem(ProductionMenu)] public static void UseProduction() => Set(RainPresentationMode.Production);
        private static void Set(RainPresentationMode mode)
        {
            var asset = Build(); asset.SetMode(mode); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            RainPresentationSettings.Reload(); SceneView.RepaintAll();
        }
        [MenuItem(DevelopmentMenu, true)] private static bool DevelopmentSelected()
        { Menu.SetChecked(DevelopmentMenu, RainPresentationSettings.IsDevelopment); return true; }
        [MenuItem(ProductionMenu, true)] private static bool ProductionSelected()
        { Menu.SetChecked(ProductionMenu, !RainPresentationSettings.IsDevelopment); return true; }
    }
}
