using System.IO;
using Reverbat;
using UnityEditor;
using UnityEngine;

public static class Level01DialogueInstaller
{
    [MenuItem("REVERBAT/Install Level 01 Dialogue Content")]
    public static void Install()
    {
        const string path = "Assets/Resources/Level01Dialogue.asset";
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        var clips = new AudioClip[4];
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Level01/L1 0" + (i + 1) + ".wav");
            if (clips[i] == null) { Debug.LogError("[REVERBAT] Import all four L1 audio recordings before installing."); return; }
        }
        var library = AssetDatabase.LoadAssetAtPath<DialogueLibrary>(path);
        bool create = library == null;
        if (create) library = ScriptableObject.CreateInstance<DialogueLibrary>();
        library.entries = new[] {
            Entry(DialogueIds.LevelBegin, "Great. He gets the castle, the throne, and the entire kingdom... and I get wings the size of teaspoons.", clips[0]),
            Entry(DialogueIds.FirstPulse, "Wonderful. I've lost my body AND my eyesight. What's next, taxes?", clips[1]),
            Entry(DialogueIds.Tilt, "Behold! The terrifying power of... sitting on things.", clips[2]),
            Entry(DialogueIds.Artifact, "Finally, some of my power back. Now, where's that oversized impostor?", clips[3])
        };
        if (create) AssetDatabase.CreateAsset(library, path);
        else EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("[REVERBAT] Installed four original Level 1 recordings; removed unused dialogue entries.");
    }
    private static DialogueEntry Entry(string id, string text, AudioClip clip)
    {
        return new DialogueEntry { triggerId = id, characterName = "BAT DRACULA", text = text, voiceClip = clip, duration = clip.length };
    }
}
