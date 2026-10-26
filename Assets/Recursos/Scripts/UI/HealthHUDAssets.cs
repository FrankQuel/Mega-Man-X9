using System;
using UnityEngine;

namespace MegaManX9.UI
{
    [CreateAssetMenu(menuName = "Mega Man X9/Health HUD Assets")]
    public sealed class HealthHUDAssets : ScriptableObject
    {
        [Serializable]
        public sealed class Style
        {
            public string unit, label;
            public Sprite frame;
            [Tooltip("Coordinates in the frame image, measured from the top-left corner.")]
            public Rect fillWindow;
            public Rect statusWindow;
            public Sprite horizontalFrame;
            public Rect horizontalFillWindow, horizontalStatusWindow, horizontalLivesWindow, horizontalValueWindow;
            public bool boss;
        }

        public Style[] styles = Array.Empty<Style>();
        public Sprite healthUnit;
        public Sprite damageUnit;
        public Sprite normalIndicator;
        public Sprite cautionIndicator;
        public Sprite dangerIndicator;
        public Sprite lifeCounterRing;
        public Sprite bossSkull;
        public Rect horizontalBossEmblemWindow = new Rect(0, 5, 20, 18);
        public int nativeHealthUnits = 28;

        public Style Find(string unit)
        {
            if (string.IsNullOrEmpty(unit)) return null;
            int slash = unit.LastIndexOf('/');
            if (slash >= 0) unit = unit.Substring(slash + 1);
            foreach (var style in styles)
                if (string.Equals(style.unit, unit, StringComparison.OrdinalIgnoreCase)) return style;
            return null;
        }
    }
}
