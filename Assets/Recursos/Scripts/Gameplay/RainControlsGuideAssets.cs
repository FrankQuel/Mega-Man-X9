using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.Gameplay
{
    [Serializable]
    public struct RainKeyboardKeyRect
    {
        public Key key;
        public Rect pixelsFromTopLeft;
        public RainKeyboardKeyRect(Key key, float x, float y, float width, float height)
        { this.key = key; pixelsFromTopLeft = new Rect(x, y, width, height); }
    }

    [CreateAssetMenu(menuName = "Mega Man X9/Controls Guide Assets")]
    public sealed class RainControlsGuideAssets : ScriptableObject
    {
        public Texture2D keyboard;
        public Vector2 sourceSize = new Vector2(1200, 444);
        public RainKeyboardKeyRect[] keys = Array.Empty<RainKeyboardKeyRect>();
        public bool IsReady => keyboard != null && sourceSize.x > 0 && sourceSize.y > 0 && keys != null && keys.Length > 0;

        public bool TryGetKeyRect(Key key, out Rect pixels)
        {
            foreach (var entry in keys)
                if (entry.key == key) { pixels = entry.pixelsFromTopLeft; return true; }
            pixels = default; return false;
        }

        public static RainControlsGuideAssets Load() => Resources.Load<RainControlsGuideAssets>("RainControlsGuideAssets");
    }
}
