using UnityEngine;
using UnityEngine.Video;

namespace MegaManX9.Gameplay
{
    [CreateAssetMenu(menuName = "Mega Man X9/Startup Video Assets")]
    public sealed class RainStartupVideoAssets : ScriptableObject
    {
        public VideoClip video;
        [Range(0, 1)] public float volume = 1;
        public static RainStartupVideoAssets Load() => Resources.Load<RainStartupVideoAssets>("RainStartupVideoAssets");
    }
}
