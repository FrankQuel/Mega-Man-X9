using UnityEngine;

namespace MegaManX9.Gameplay
{
    public enum RainPresentationMode { Production, Development }

    /// <summary>Global presentation mode. Production keeps the playable screen limited to its health HUD.</summary>
    [CreateAssetMenu(menuName = "Mega Man X9/Presentation Settings")]
    public sealed class RainPresentationSettings : ScriptableObject
    {
        [SerializeField] private RainPresentationMode mode = RainPresentationMode.Production;
        private static RainPresentationSettings cached;
        private static bool loaded;
        public RainPresentationMode Mode => mode;
        public static RainPresentationMode CurrentMode => Load() != null ? cached.mode : RainPresentationMode.Production;
        public static bool IsDevelopment => CurrentMode == RainPresentationMode.Development;
        public static bool ShowGameplayHelpers => IsDevelopment;
        public static RainPresentationSettings Load()
        {
            if (!loaded) { cached = Resources.Load<RainPresentationSettings>("RainPresentationSettings"); loaded = true; }
            return cached;
        }
        public void SetMode(RainPresentationMode value) => mode = value;
        public static void Reload() { loaded = false; cached = null; }
        private void OnEnable() { if (cached == this) Reload(); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => Reload();
    }
}
