using System;
using System.IO;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace MegaManX9.EditorTools
{
    public static class RainStartupVideoAssetsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Resources/RainStartupVideoAssets.asset";
        public const string MoviePath = "Assets/Recursos/UI/Intro/Capcom Logo (Mega Man X Collection).mp4";

        [MenuItem("Mega Man X9/Playable Rain Level/Build Startup Video Assets", priority = 8)]
        private static void BuildMenu() => Build();

        public static RainStartupVideoAssets Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Construye los recursos de video fuera de Play.");
            if (!File.Exists(MoviePath)) throw new FileNotFoundException("Falta el video original de inicio.", MoviePath);
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(MoviePath);
            if (clip == null)
            {
                AssetDatabase.ImportAsset(MoviePath, ImportAssetOptions.ForceSynchronousImport);
                clip = AssetDatabase.LoadAssetAtPath<VideoClip>(MoviePath);
            }
            if (clip == null) throw new InvalidDataException("Unity no pudo importar el video original: " + MoviePath);
            if (!AssetDatabase.IsValidFolder("Assets/Recursos/Resources")) AssetDatabase.CreateFolder("Assets/Recursos", "Resources");
            var asset = AssetDatabase.LoadAssetAtPath<RainStartupVideoAssets>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RainStartupVideoAssets>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.video = clip; asset.volume = 1;
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }
    }
}
