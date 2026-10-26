using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    /// <summary>Panels and the identified bitmap lettering extracted from the supplied pause sheet.</summary>
    [CreateAssetMenu(menuName = "Mega Man X9/Rain Pause Art")]
    public sealed class RainPauseAssets : ScriptableObject
    {
        [Serializable] public sealed class Glyph
        {
            public string character, sourceLabel;
            public RectInt sourceTopLeftRect;
            public Rect atlasRect;
        }

        public Texture2D sourceSheet, artwork, fontAtlas, xPortrait, zeroPortrait;
        public Sprite xPanel, zeroPanel, healthFrame, cursor, xLifeIcon, zeroLifeIcon;
        public List<Glyph> glyphs = new List<Glyph>();
        private readonly Dictionary<char, Glyph> indexed = new Dictionary<char, Glyph>();
        private int indexedCount = -1;
        public static RainPauseAssets Load() => Resources.Load<RainPauseAssets>("RainPauseAssets");
        public bool HasSuppliedArt => sourceSheet != null && artwork != null && xPanel != null && zeroPanel != null;
        public bool HasSuppliedFont => fontAtlas != null && glyphs.Count >= 35;
        private void OnEnable() { indexed.Clear(); indexedCount = -1; }
        public void Invalidate() { indexed.Clear(); indexedCount = -1; }
        public bool TryGetGlyph(char character, out Glyph glyph)
        {
            if (indexedCount != glyphs.Count)
            {
                indexed.Clear();
                foreach (var item in glyphs) if (item != null && item.character?.Length == 1) indexed[item.character[0]] = item;
                indexedCount = glyphs.Count;
            }
            return indexed.TryGetValue(character, out glyph);
        }
        public static string Normalize(string value)
        {
            var text = new StringBuilder();
            foreach (char c in (value ?? "").ToUpperInvariant().Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) text.Append(c);
            return text.ToString().Normalize(NormalizationForm.FormC);
        }
        public bool SupportsText(string value)
        {
            foreach (char c in Normalize(value)) if (!char.IsWhiteSpace(c) && c != ':' && !TryGetGlyph(c, out _)) return false;
            return true;
        }
        private float Advance(char character, float height)
        {
            if (character == ' ') return height * .42f;
            if (character == ':') return height * .34f;
            return TryGetGlyph(character, out var glyph) ? (glyph.atlasRect.width + 1) / glyph.atlasRect.height * height : height * .55f;
        }
        public float TextWidth(string value, float height)
        {
            float width = 0; foreach (char c in Normalize(value)) width += Advance(c, height); return width;
        }
        public void DrawText(Rect bounds, string value, float height, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            if (!HasSuppliedFont || string.IsNullOrEmpty(value)) return;
            string text = Normalize(value); float width = TextWidth(text, height);
            float x = bounds.x;
            if (alignment == TextAnchor.MiddleCenter || alignment == TextAnchor.UpperCenter || alignment == TextAnchor.LowerCenter) x += (bounds.width - width) * .5f;
            else if (alignment == TextAnchor.MiddleRight || alignment == TextAnchor.UpperRight || alignment == TextAnchor.LowerRight) x += bounds.width - width;
            float y = bounds.y + (bounds.height - height) * .5f;
            Color previous = GUI.color; GUI.color = color;
            try
            {
                foreach (char c in text)
                {
                    if (c == ':' && TryGetGlyph('.', out var dot))
                    {
                        DrawGlyph(new Rect(x, y - height * .46f, height * .25f, height), dot);
                        DrawGlyph(new Rect(x, y - height * .10f, height * .25f, height), dot);
                    }
                    else if (TryGetGlyph(c, out var glyph))
                        DrawGlyph(new Rect(x, y, glyph.atlasRect.width / glyph.atlasRect.height * height, height), glyph);
                    x += Advance(c, height);
                }
            }
            finally { GUI.color = previous; }
        }
        private void DrawGlyph(Rect bounds, Glyph glyph)
        {
            var r = glyph.atlasRect;
            GUI.DrawTextureWithTexCoords(bounds, fontAtlas, new Rect(r.x / fontAtlas.width, r.y / fontAtlas.height, r.width / fontAtlas.width, r.height / fontAtlas.height));
        }
    }
}
