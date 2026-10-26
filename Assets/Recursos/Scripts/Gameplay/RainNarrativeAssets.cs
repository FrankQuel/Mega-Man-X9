using System;
using UnityEngine;

namespace MegaManX9.Gameplay
{
    public enum RainNarrativeSpeaker { Narrator, X, Zero, Axl }

    [Serializable]
    public sealed class RainNarrativePage
    {
        public RainNarrativeSpeaker speaker;
        public string speakerName;
        [TextArea(2, 6)] public string text;
        public Texture2D image;
    }

    /// <summary>References to the supplied illustrations and portraits, plus independently authored dialogue.</summary>
    [CreateAssetMenu(menuName = "Mega Man X9/Rain Narrative Assets")]
    public sealed class RainNarrativeAssets : ScriptableObject
    {
        public Texture2D xPortrait, zeroPortrait, axlPortrait;
        public RainNarrativePage[] introduction = Array.Empty<RainNarrativePage>();
        public RainNarrativePage[] confrontation = Array.Empty<RainNarrativePage>();
        public RainNarrativePage[] ending = Array.Empty<RainNarrativePage>();
        public RainNarrativePage[] missionArrivalX = Array.Empty<RainNarrativePage>();
        public RainNarrativePage[] missionArrivalZero = Array.Empty<RainNarrativePage>();

        public bool IsReady => xPortrait != null && zeroPortrait != null && axlPortrait != null &&
            introduction != null && introduction.Length == 5 && confrontation != null && confrontation.Length > 0 &&
            ending != null && ending.Length > 0 && missionArrivalX != null && missionArrivalX.Length == 2 &&
            missionArrivalZero != null && missionArrivalZero.Length == 2 && introduction[0]?.image != null && introduction[1]?.image != null &&
            introduction[2]?.image != null && introduction[3]?.image != null;

        public Texture2D Portrait(RainNarrativeSpeaker speaker)
        {
            switch (speaker)
            {
                case RainNarrativeSpeaker.X: return xPortrait;
                case RainNarrativeSpeaker.Zero: return zeroPortrait;
                case RainNarrativeSpeaker.Axl: return axlPortrait;
                default: return null;
            }
        }

        public static RainNarrativeAssets Load() => Resources.Load<RainNarrativeAssets>("RainNarrativeAssets");
    }
}
