using System.IO;
using System.Linq;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level01PrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/Level01_Prototype.unity";
    private const string AssetFolder = "Assets/ReverbatPrototype";
    private static Sprite square, circle;
    private static Material material;

    [MenuItem("REVERBAT/Build Level 01 Prototype")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop play mode before rebuilding."); return; }
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }
        Directory.CreateDirectory(AssetFolder);
        square = MakeSprite("Square", false);
        circle = MakeSprite("Circle", true);
        material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        AssetDatabase.CreateAsset(material, AssetFolder + "/PlaceholderUnlit.mat");

        // Preserve a copy of any unsaved work before opening our separate prototype scene.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var open = SceneManager.GetSceneAt(i);
            if (!open.isDirty) continue;
            Directory.CreateDirectory("Assets/Scenes/Backups");
            EditorSceneManager.SaveScene(open, "Assets/Scenes/Backups/BeforeReverbat_" + i + "_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity", true);
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Level01_Prototype");
        var level = root.AddComponent<Level01Manager>();
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.075f, .08f, .1f);
        camera.allowHDR = false;
        camera.allowMSAA = false;

        Surface("Floor", new Vector2(0, -4), new Vector2(18, .35f), 0, new Color(.33f, .35f, .39f), SoundResponse.Wall, level, root.transform);
        Surface("Ceiling", new Vector2(0, 4.15f), new Vector2(18, .25f), 0, new Color(.33f, .35f, .39f), SoundResponse.Wall, level, root.transform);
        Surface("Left Wall", new Vector2(-8.9f, 0), new Vector2(.3f, 8.2f), 0, new Color(.33f, .35f, .39f), SoundResponse.Wall, level, root.transform);
        Surface("Right Wall", new Vector2(8.9f, 0), new Vector2(.3f, 8.2f), 0, new Color(.33f, .35f, .39f), SoundResponse.Wall, level, root.transform);
        level.reflector = Surface("Reflector", Vector2.zero, new Vector2(2.6f, .18f), 45, new Color(.12f, .75f, .83f), SoundResponse.Reflector, level, root.transform).transform;
        level.bell = Surface("Bell", new Vector2(0, 2.8f), new Vector2(1.3f, .85f), 0, new Color(1, .82f, .15f), SoundResponse.Bell, level, root.transform);
        level.coffin = Surface("Coffin", new Vector2(6, -2.8f), new Vector2(1.65f, 1.8f), 0, new Color(.32f, .08f, .13f), SoundResponse.HiddenMechanism, level, root.transform);
        var artifact = Shape("Blood Artifact", new Vector2(6, -2.8f), Vector2.one * .5f, new Color(.95f, .08f, .12f), root.transform, circle);
        artifact.sortingOrder = 5;
        level.artifact = artifact.gameObject;
        level.artifact.SetActive(false);

        var batSprite = Shape("Bat", new Vector2(-6, 0), new Vector2(.62f, .43f), new Color(.57f, .3f, .86f), root.transform, circle);
        var bat = batSprite.gameObject.AddComponent<BatController2D>();
        var echo = batSprite.gameObject.AddComponent<EchoSystem2D>();
        bat.roomCamera = camera;
        bat.level = level;
        bat.echo = echo;
        echo.level = level;
        level.bat = bat;
        echo.outgoing = Line("Outgoing Echo", new Color(.05f, .95f, 1), .055f, root.transform, 10);
        echo.returning = Line("Returning Echo", new Color(1, .88f, .1f), .035f, root.transform, 11);
        level.mechanism = Line("Hidden Bell Coffin Connection", new Color(1, .55f, .65f), .025f, root.transform, 7);
        level.mechanism.positionCount = 3;
        level.mechanism.SetPositions(new[] { new Vector3(0, 2.8f), new Vector3(6, 2.8f), new Vector3(6, -2.8f) });
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("[REVERBAT] Built and saved " + ScenePath);
    }

    private static Sprite MakeSprite(string name, bool round)
    {
        string path = AssetFolder + "/" + name + ".png";
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            texture.SetPixel(x, y, !round || Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)) <= 32 ? Color.white : Color.clear);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static SpriteRenderer Shape(string name, Vector2 position, Vector2 size, Color color, Transform parent, Sprite sprite = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = size;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite == null ? square : sprite;
        sr.sharedMaterial = material;
        sr.color = color;
        return sr;
    }

    private static SpriteRenderer Surface(string name, Vector2 pos, Vector2 size, float angle, Color color, SoundResponse response, Level01Manager level, Transform parent)
    {
        var sr = Shape(name, pos, size, color, parent);
        sr.transform.rotation = Quaternion.Euler(0, 0, angle);
        sr.gameObject.AddComponent<BoxCollider2D>();
        var sound = sr.gameObject.AddComponent<SoundInteractable>();
        sound.response = response;
        sound.level = level;
        return sr;
    }

    private static LineRenderer Line(string name, Color color, float width, Transform parent, int order)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.startWidth = line.endWidth = width;
        line.startColor = line.endColor = color;
        line.sortingOrder = order;
        line.numCapVertices = 0;
        line.positionCount = 0;
        line.enabled = false;
        return line;
    }
}
