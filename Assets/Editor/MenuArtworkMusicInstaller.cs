using System.Linq;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MenuArtworkMusicInstaller
{
    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        const string imagePath = "Assets/Art/MainMenu/MenuBackground.png";
        const string musicPath = "Assets/Audio/Level01/BG music.mp3";
        var imageImporter = (TextureImporter)AssetImporter.GetAtPath(imagePath);
        imageImporter.textureType = TextureImporterType.Default;
        imageImporter.mipmapEnabled = false;
        imageImporter.npotScale = TextureImporterNPOTScale.None;
        imageImporter.maxTextureSize = 2048;
        imageImporter.textureCompression = TextureImporterCompression.Uncompressed;
        imageImporter.SaveAndReimport();
        var audioImporter = (AudioImporter)AssetImporter.GetAtPath(musicPath);
        var sampleSettings = audioImporter.defaultSampleSettings;
        sampleSettings.loadType = AudioClipLoadType.Streaming;
        audioImporter.defaultSampleSettings = sampleSettings;
        audioImporter.SaveAndReimport();
        var music = AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
        if (music == null) { Debug.LogError("Missing supplied BG music recording."); return; }
        EditorSceneManager.OpenScene(Level01PrototypeBuilder.ScenePath);
        var level = Object.FindAnyObjectByType<Level01Manager>();
        var source = level.GetComponent<AudioSource>();
        if (source == null) source = Undo.AddComponent<AudioSource>(level.gameObject);
        source.clip = music;
        source.playOnAwake = true;
        source.loop = true;
        source.volume = Mathf.Pow(10, -9.5f / 20);
        source.spatialBlend = 0;
        source.ignoreListenerVolume = false;
        EditorUtility.SetDirty(source);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        EditorSceneManager.OpenScene(MainMenuInstaller.Path);
        var menu = Object.FindAnyObjectByType<MainMenu>();
        menu.background = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        // Keep existing other build scenes; ensure the menu and actual playable Level 1 are enabled.
        EditorBuildSettings.globalScenes = new[] {
            new EditorBuildSettingsScene(MainMenuInstaller.Path, true),
            new EditorBuildSettingsScene(Level01PrototypeBuilder.ScenePath, true)
        }.Concat(EditorBuildSettings.globalScenes.Where(s => s.path != MainMenuInstaller.Path && s.path != Level01PrototypeBuilder.ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
    }
}
