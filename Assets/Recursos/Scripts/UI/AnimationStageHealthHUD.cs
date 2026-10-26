using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.UI
{
    /// <summary>Draws the original HUD art against live health; galleries retain their vertical layout.</summary>
    public sealed class AnimationStageHealthHUD : MonoBehaviour
    {
        [SerializeField] private HealthHUDAssets assets;
        private GUIStyle valueStyle;
        private sealed class DamageTrail
        {
            public int value;
            public float normalized, trailNormalized, changedAt;
        }
        private readonly Dictionary<CharacterHealth, DamageTrail> trails = new Dictionary<CharacterHealth, DamageTrail>();
        public HealthHUDAssets Assets => assets;
        public void Configure(HealthHUDAssets value) { assets = value; trails.Clear(); }
        public bool SupportsUnit(string unit) => assets != null && assets.Find(unit) != null;
        public bool IsBossUnit(string unit) => assets != null && assets.Find(unit)?.boss == true;
        public bool UsesBossSkull => assets != null && assets.bossSkull != null;

        /// <summary>Call from OnGUI after the stage has applied its logical-canvas GUI.matrix.</summary>
        public void Draw(Rect logicalRect, string unit, CharacterHealth health, bool boss = false)
        {
            if (assets == null || health == null || logicalRect.width <= 0 || logicalRect.height <= 0) return;
            var style = assets.Find(unit);
            if (style == null || style.frame == null || assets.healthUnit == null) return;
            if (valueStyle == null)
            {
                valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                valueStyle.normal.textColor = Color.white;
            }
            float frameWidth = style.frame.rect.width, frameHeight = style.frame.rect.height;
            float availableScale = Mathf.Min(logicalRect.width / frameWidth, Mathf.Max(1, logicalRect.height - 24) / frameHeight);
            float scale = availableScale >= 1 ? Mathf.Floor(availableScale) : availableScale;
            bool onRight = boss || style.boss;
            var frameRect = new Rect(onRight ? logicalRect.xMax - frameWidth * scale : logicalRect.x,
                logicalRect.y, frameWidth * scale, frameHeight * scale);
            var fillRect = InFrame(frameRect, style.fillWindow, scale);
            Color previousColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                DrawSprite(frameRect, style.frame);
                GUI.color = new Color(.055f, .075f, .12f, .98f);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                var state = GetTrail(health);
                if (assets.damageUnit != null && state.trailNormalized > health.Normalized)
                {
                    float fade = Mathf.Clamp01((Time.unscaledTime - state.changedAt - .18f) / .32f);
                    float trail = Mathf.Lerp(state.trailNormalized, health.Normalized, fade);
                    DrawFill(fillRect, assets.damageUnit, trail, assets.nativeHealthUnits);
                }
                DrawFill(fillRect, assets.healthUnit, health.Normalized, assets.nativeHealthUnits);
                Sprite indicator = health.Normalized <= .25f ? assets.dangerIndicator :
                    health.Normalized <= .5f ? assets.cautionIndicator : assets.normalIndicator;
                if (indicator != null && style.statusWindow.width > 0)
                    DrawSprite(InFrame(frameRect, style.statusWindow, scale), indicator);
                valueStyle.alignment = onRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                GUI.Label(new Rect(logicalRect.x, frameRect.yMax + 3, logicalRect.width, 22),
                    health.CurrentHealth + "/" + health.MaxHealth + " PV", valueStyle);
            }
            finally { GUI.color = previousColor; }
        }

        /// <summary>Upright native emblem and status circle, a separate shared-life ring, then the horizontal HP channel.</summary>
        public void DrawHorizontal(Rect logicalRect, string unit, CharacterHealth health, int sharedLives = -1, bool boss = false)
        {
            if (assets == null || health == null || logicalRect.width <= 0 || logicalRect.height <= 0) return;
            var style = assets.Find(unit);
            if (style == null || style.horizontalFrame == null || assets.healthUnit == null) return;
            float width = style.horizontalFrame.rect.width, height = style.horizontalFrame.rect.height;
            float available = Mathf.Min(logicalRect.width / width, logicalRect.height / height);
            float scale = available >= 1 ? Mathf.Floor(available) : available;
            bool onRight = boss || style.boss;
            var frame = new Rect(onRight ? logicalRect.xMax - width * scale : logicalRect.x, logicalRect.y,
                width * scale, height * scale);
            Rect fill = InFrame(frame, style.horizontalFillWindow, scale);
            Color previous = GUI.color;
            try
            {
                GUI.color = Color.white; DrawSprite(frame, style.horizontalFrame);
                if (onRight && assets.bossSkull != null)
                    DrawSprite(InFrame(frame, assets.horizontalBossEmblemWindow, scale), assets.bossSkull);
                GUI.color = new Color(.035f, .06f, .09f, .98f); GUI.DrawTexture(fill, Texture2D.whiteTexture);
                GUI.color = Color.white;
                var trail = GetTrail(health);
                if (assets.damageUnit != null && trail.trailNormalized > health.Normalized)
                {
                    float fade = Mathf.Clamp01((Time.unscaledTime - trail.changedAt - .18f) / .32f);
                    DrawHorizontalFill(fill, assets.damageUnit, Mathf.Lerp(trail.trailNormalized, health.Normalized, fade), assets.nativeHealthUnits);
                }
                DrawHorizontalFill(fill, assets.healthUnit, health.Normalized, assets.nativeHealthUnits);
                Sprite status = health.Normalized <= .25f ? assets.dangerIndicator :
                    health.Normalized <= .5f ? assets.cautionIndicator : assets.normalIndicator;
                if (status != null) DrawSprite(InFrame(frame, style.horizontalStatusWindow, scale), status);
                if (!onRight && sharedLives >= 0 && style.horizontalLivesWindow.width > 0)
                {
                    Rect lives = InFrame(frame, style.horizontalLivesWindow, scale);
                    Sprite ring = assets.lifeCounterRing != null ? assets.lifeCounterRing : assets.normalIndicator;
                    if (ring != null) DrawSprite(lives, ring);
                    var digits = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(scale * 9),
                        fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                    digits.normal.textColor = Color.white;
                    GUI.Label(new Rect(lives.x, lives.y + scale, lives.width, lives.height - scale), Mathf.Max(0, sharedLives).ToString(), digits);
                }
                var values = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Mathf.RoundToInt(scale * 5)),
                    fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                values.normal.textColor = Color.white;
                GUI.Label(InFrame(frame, style.horizontalValueWindow, scale), health.CurrentHealth + "/" + health.MaxHealth + " PV", values);
            }
            finally { GUI.color = previous; }
        }

        private static void DrawHorizontalFill(Rect rect, Sprite unit, float normalized, int units)
        {
            units = Mathf.Max(1, units);
            int visible = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(normalized) * units), 0, units);
            float width = rect.width / units;
            for (int i = 0; i < visible; i++) DrawSprite(new Rect(rect.x + i * width, rect.y, width, rect.height), unit);
        }

        private DamageTrail GetTrail(CharacterHealth health)
        {
            if (!trails.TryGetValue(health, out var state))
            {
                state = new DamageTrail { value = health.CurrentHealth, normalized = health.Normalized,
                    trailNormalized = health.Normalized, changedAt = Time.unscaledTime };
                trails[health] = state;
            }
            else if (state.value != health.CurrentHealth || !Mathf.Approximately(state.normalized, health.Normalized))
            {
                state.trailNormalized = health.CurrentHealth < state.value ? state.normalized : health.Normalized;
                state.value = health.CurrentHealth; state.normalized = health.Normalized;
                state.changedAt = Time.unscaledTime;
            }
            return state;
        }

        private static Rect InFrame(Rect frame, Rect native, float scale)
        {
            return new Rect(frame.x + native.x * scale, frame.y + native.y * scale, native.width * scale, native.height * scale);
        }

        private static void DrawFill(Rect rect, Sprite unit, float normalized, int units)
        {
            units = Mathf.Max(1, units);
            int visible = Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(normalized) * units), 0, units);
            float height = rect.height / units;
            for (int i = 0; i < visible; i++)
                DrawSprite(new Rect(rect.x, rect.yMax - (i + 1) * height, rect.width, height), unit);
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            Rect textureRect = sprite.textureRect;
            var uv = new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
        }
    }
}
