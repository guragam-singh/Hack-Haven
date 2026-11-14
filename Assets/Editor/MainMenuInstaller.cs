using System.Linq;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuInstaller
{
    public const string Path = "Assets/Scenes/MainMenu.unity";
    [MenuItem("REVERBAT/Create Main Menu")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) return;
        if (System.IO.File.Exists(Path)) EditorSceneManager.OpenScene(Path);
        else
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            new GameObject("Main Menu").AddComponent<MainMenu>();
            var camera = new GameObject("Menu Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .025f, .055f);
            camera.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, Path);
            // Close the new additive scene, then open it alone; existing scenes were not edited.
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.OpenScene(Path);
        }
        EditorBuildSettings.globalScenes = new[] { new EditorBuildSettingsScene(Path, true) }
            .Concat(EditorBuildSettings.globalScenes.Where(s => s.path != Path)).ToArray();
        Debug.Log("[REVERBAT] Main menu saved as first build scene.");
    }
}
