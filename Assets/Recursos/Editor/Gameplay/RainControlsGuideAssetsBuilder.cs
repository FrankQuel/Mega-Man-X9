using System;
using System.Collections.Generic;
using System.IO;
using MegaManX9.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.EditorTools
{
    public static class RainControlsGuideAssetsBuilder
    {
        public const string AssetPath = "Assets/Recursos/Resources/RainControlsGuideAssets.asset";
        public const string KeyboardPath = "Assets/Recursos/UI/Controls/teclado.png";

        [MenuItem("Mega Man X9/Playable Rain Level/Build Controls Guide Assets", priority = 9)]
        private static void BuildMenu() => Build();

        public static RainControlsGuideAssets Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Construye la guía de controles fuera de Play.");
            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(KeyboardPath);
            if (image == null) throw new FileNotFoundException("Falta el teclado original.", KeyboardPath);
            if (!AssetDatabase.IsValidFolder("Assets/Recursos/Resources")) AssetDatabase.CreateFolder("Assets/Recursos", "Resources");
            var asset = AssetDatabase.LoadAssetAtPath<RainControlsGuideAssets>(AssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RainControlsGuideAssets>();
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            asset.keyboard = image; asset.sourceSize = new Vector2(1200, 444);
            var map = new List<RainKeyboardKeyRect>();
            Add(map, Key.Escape, 39, 47, 57, 56);
            Row(map, new[] { Key.F1, Key.F2, Key.F3, Key.F4 }, new[] { 124f, 175, 224, 274 }, 47, 49, 56);
            Row(map, new[] { Key.F5, Key.F6, Key.F7, Key.F8 }, new[] { 345f, 395, 445, 495 }, 47, 49, 56);
            Row(map, new[] { Key.F9, Key.F10, Key.F11, Key.F12 }, new[] { 568f, 618, 668, 718 }, 47, 49, 56);
            Row(map, new[] { Key.PrintScreen, Key.ScrollLock, Key.Pause }, new[] { 788f, 840, 892 }, 47, 50, 56);

            Row(map, new[] { Key.Backquote, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
                Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals },
                new[] { 39f, 89, 138, 187, 236, 285, 334, 383, 432, 481, 530, 579, 628 }, 146, 46, 48);
            Add(map, Key.Backspace, 677, 146, 92, 48);
            Add(map, Key.Tab, 39, 196, 73, 49);
            Row(map, new[] { Key.Q, Key.W, Key.E, Key.R, Key.T, Key.Y, Key.U, Key.I, Key.O, Key.P, Key.LeftBracket, Key.RightBracket },
                new[] { 115f, 164, 213, 262, 311, 360, 409, 458, 507, 556, 605, 654 }, 196, 46, 49);
            Add(map, Key.Backslash, 703, 196, 66, 49);
            Add(map, Key.CapsLock, 39, 247, 87, 48);
            Row(map, new[] { Key.A, Key.S, Key.D, Key.F, Key.G, Key.H, Key.J, Key.K, Key.L, Key.Semicolon, Key.Quote },
                new[] { 129f, 178, 227, 276, 325, 374, 423, 472, 521, 570, 619 }, 247, 46, 48);
            Add(map, Key.Enter, 668, 247, 101, 48);
            Add(map, Key.LeftShift, 39, 297, 103, 48);
            Row(map, new[] { Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M, Key.Comma, Key.Period, Key.Slash },
                new[] { 145f, 194, 243, 292, 341, 390, 439, 488, 537, 586 }, 297, 46, 48);
            Add(map, Key.RightShift, 635, 297, 134, 48);
            Add(map, Key.LeftCtrl, 39, 347, 74, 51); Add(map, Key.LeftMeta, 116, 347, 57, 51);
            Add(map, Key.LeftAlt, 176, 347, 63, 51); Add(map, Key.Space, 242, 347, 261, 51);
            Add(map, Key.RightAlt, 506, 347, 55, 51); Add(map, Key.RightMeta, 564, 347, 57, 51);
            Add(map, Key.ContextMenu, 624, 347, 65, 51); Add(map, Key.RightCtrl, 692, 347, 77, 51);

            Row(map, new[] { Key.Insert, Key.Home, Key.PageUp }, new[] { 790f, 842, 894 }, 146, 48, 48);
            Row(map, new[] { Key.Delete, Key.End, Key.PageDown }, new[] { 790f, 842, 894 }, 196, 48, 49);
            Add(map, Key.UpArrow, 838, 295, 54, 51);
            Row(map, new[] { Key.LeftArrow, Key.DownArrow, Key.RightArrow }, new[] { 789f, 840, 892 }, 347, 50, 51);
            Row(map, new[] { Key.NumLock, Key.NumpadDivide, Key.NumpadMultiply, Key.NumpadMinus }, new[] { 961f, 1011, 1061, 1111 }, 146, 47, 48);
            Row(map, new[] { Key.Numpad7, Key.Numpad8, Key.Numpad9 }, new[] { 961f, 1011, 1061 }, 196, 47, 48);
            Row(map, new[] { Key.Numpad4, Key.Numpad5, Key.Numpad6 }, new[] { 961f, 1011, 1061 }, 246, 47, 48);
            Row(map, new[] { Key.Numpad1, Key.Numpad2, Key.Numpad3 }, new[] { 961f, 1011, 1061 }, 296, 47, 48);
            Add(map, Key.NumpadPlus, 1111, 196, 47, 98); Add(map, Key.NumpadEnter, 1111, 296, 47, 102);
            Add(map, Key.Numpad0, 961, 347, 97, 51); Add(map, Key.NumpadPeriod, 1061, 347, 47, 51);
            var seen = new HashSet<Key>();
            foreach (var entry in map)
            {
                var rect = entry.pixelsFromTopLeft;
                if (!seen.Add(entry.key) || rect.xMin < 0 || rect.yMin < 0 || rect.xMax > 1200 || rect.yMax > 444)
                    throw new InvalidDataException("Mapa de teclado inválido para " + entry.key);
            }
            asset.keys = map.ToArray(); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static void Add(List<RainKeyboardKeyRect> map, Key key, float x, float y, float width, float height)
            => map.Add(new RainKeyboardKeyRect(key, x, y, width, height));
        private static void Row(List<RainKeyboardKeyRect> map, Key[] keys, float[] x, float y, float width, float height)
        { for (int i = 0; i < keys.Length; i++) Add(map, keys[i], x[i], y, width, height); }
    }
}
