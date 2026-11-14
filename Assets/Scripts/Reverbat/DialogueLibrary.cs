using System;
using UnityEngine;

namespace Reverbat
{
    public static class DialogueIds
    {
        public const string LevelBegin = "level01.begin";
        public const string FirstPulse = "level01.first-pulse";
        public const string Reflector = "level01.reflector-discovered";
        public const string Tilt = "level01.reflector-tilted";
        public const string Coffin = "level01.coffin-open";
        public const string Artifact = "level01.artifact-collected";
        public const string Announcement = "level01.dictator-announcement";
    }

    [Serializable]
    public sealed class DialogueEntry
    {
        public string triggerId;
        public string characterName;
        [TextArea(2, 5)] public string text;
        public AudioClip voiceClip;
        [Min(.1f)] public float duration = 5;
    }

    [CreateAssetMenu(menuName = "REVERBAT/Dialogue Library")]
    public sealed class DialogueLibrary : ScriptableObject
    {
        public DialogueEntry[] entries = Array.Empty<DialogueEntry>();
        public DialogueEntry Find(string id)
        {
            foreach (var entry in entries)
                if (entry != null && entry.triggerId == id) return entry;
            return null;
        }
    }
}
