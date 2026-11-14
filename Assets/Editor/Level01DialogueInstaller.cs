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
        if (AssetDatabase.LoadAssetAtPath<DialogueLibrary>(path) != null) return;
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        var library = ScriptableObject.CreateInstance<DialogueLibrary>();
        library.entries = new[] {
            Entry(DialogueIds.LevelBegin, "BAT DRACULA", "Great. He gets the castle, the throne, and the entire kingdom... and I get wings the size of teaspoons."),
            Entry(DialogueIds.FirstPulse, "BAT DRACULA", "Wonderful. I've lost my body AND my eyesight. What's next, taxes?"),
            Entry(DialogueIds.Reflector, "BAT DRACULA", "A perfectly good mirror, and not a reflection of me in sight. Typical."),
            Entry(DialogueIds.Tilt, "BAT DRACULA", "Behold! The terrifying power of... sitting on things."),
            Entry(DialogueIds.Coffin, "BAT DRACULA", "HA! Still the greatest mind in Transylvania. Technically half of it, but still."),
            Entry(DialogueIds.Artifact, "BAT DRACULA", "Finally, some of my power back. Now, where's that oversized impostor?"),
            Entry(DialogueIds.Announcement, "DICTATOR DRACULA", "Attention, citizens. Reports of a flying rodent impersonating your glorious ruler are entirely false.")
        };
        AssetDatabase.CreateAsset(library, path);
        AssetDatabase.SaveAssets();
        Debug.Log("[REVERBAT] Created editable Level01Dialogue asset with seven voice-ready entries.");
    }
    private static DialogueEntry Entry(string id, string speaker, string text)
    {
        return new DialogueEntry { triggerId = id, characterName = speaker, text = text, duration = 5 };
    }
}
