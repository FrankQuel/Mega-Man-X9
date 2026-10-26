using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Animation
{
    [CreateAssetMenu(menuName = "Mega Man X9/Sprite Animation Library")]
    public sealed class SpriteAnimationLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string category;
            public string unit;
            public string animationName;
            public AnimationClip clip;
            public RuntimeAnimatorController controller;
            public Sprite firstFrame;
            public int frameCount;
            public int frameWidth;
            public int frameHeight;
            [TextArea] public string notes;
        }

        public List<Entry> entries = new List<Entry>();
    }
}
