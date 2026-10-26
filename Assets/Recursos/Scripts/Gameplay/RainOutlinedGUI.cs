using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Readable screen-space lettering without adding a panel behind the text.</summary>
    public static class RainOutlinedGUI
    {
        public static void Label(Rect rect, string text, GUIStyle style, Color color, float outline = 2f)
        {
            if (style == null || string.IsNullOrEmpty(text)) return;
            Color previous = GUI.color;
            Color previousContent = GUI.contentColor;
            // Keep the outline style immutable after submission. Labels may use a
            // hover state, so every state needs the same ink rather than skin defaults.
            var stroke = new GUIStyle(style);
            var lettering = new GUIStyle(style);
            SetInk(stroke, new Color(0, .01f, .025f, color.a * .98f));
            SetInk(lettering, color);
            try
            {
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                float radius = Mathf.Max(0, outline);
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                    if (x != 0 || y != 0)
                        GUI.Label(new Rect(rect.x + x * radius, rect.y + y * radius, rect.width, rect.height), text, stroke);
                GUI.Label(rect, text, lettering);
            }
            finally { GUI.color = previous; GUI.contentColor = previousContent; }
        }

        private static void SetInk(GUIStyle style, Color color)
        {
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = color;
        }
    }
}
