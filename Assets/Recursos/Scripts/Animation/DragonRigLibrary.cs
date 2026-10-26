using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaManX9.Animation
{
    [CreateAssetMenu(menuName = "Mega Man X9/Dragon Rig Animation Library")]
    public sealed class DragonRigLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string name;
            public AnimationClip clip;
            public float duration;
            public bool loop;
            public bool holdLastPose;
        }

        public RuntimeAnimatorController controller;
        public Entry[] clips = Array.Empty<Entry>();
        [Serializable]
        public sealed class AuthoredPose
        {
            public string name;
            public Sprite sprite;
            public Vector3 localOffset, mouth, hand, foot;
        }
        public AuthoredPose[] authoredPoses = Array.Empty<AuthoredPose>();
        public Vector3 bindHipPosition = new Vector3(-.01f, .41f, 0);
        private string[] names;
        public IReadOnlyList<string> ClipNames
        {
            get
            {
                if (names == null || names.Length != clips.Length)
                {
                    names = new string[clips.Length];
                    for (int i = 0; i < clips.Length; i++) names[i] = clips[i].name;
                }
                return names;
            }
        }

        public Entry Find(string clipName)
        {
            foreach (var entry in clips)
                if (string.Equals(entry.name, clipName, StringComparison.Ordinal)) return entry;
            return null;
        }

        public AuthoredPose FindPose(string poseName)
        {
            if (authoredPoses == null) return null;
            foreach (var pose in authoredPoses) if (pose != null && pose.name == poseName) return pose;
            return null;
        }

        private void OnEnable() { names = null; }
        private void OnValidate() { names = null; }
    }
}
