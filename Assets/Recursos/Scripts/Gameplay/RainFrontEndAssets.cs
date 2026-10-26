using UnityEngine;

namespace MegaManX9.Gameplay
{
    [CreateAssetMenu(menuName="Mega Man X9/Rain Front End Art")]
    public sealed class RainFrontEndAssets : ScriptableObject
    {
        public Texture2D cover, title, bluePortrait, redPortrait;
        public Sprite blueLifeIcon, redLifeIcon;
        public Sprite LifeIcon(HeroForm form)=>form==HeroForm.Red?redLifeIcon:blueLifeIcon;
        public static RainFrontEndAssets Load()=>Resources.Load<RainFrontEndAssets>("RainFrontEndAssets");
    }
}
