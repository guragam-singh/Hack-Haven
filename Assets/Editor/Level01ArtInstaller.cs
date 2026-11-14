using System.IO;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Level01ArtInstaller
{
    private const string Root = "Assets/Art/Guragam/Level01/";
    private const string Derived = Root + "SpriteRegions/";

    [MenuItem("REVERBAT/Apply Guragam Level 01 Art")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play Mode before applying art."); return; }
        var level = Object.FindFirstObjectByType<Level01Manager>();
        if (level == null || level.gameObject.scene.path != Level01PrototypeBuilder.ScenePath)
        { Debug.LogError("Open the existing Level01_Prototype scene first."); return; }
        Directory.CreateDirectory(Derived);
        AssetDatabase.Refresh();
        foreach (var file in Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories))
        {
            var path = file.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = !path.Contains("00_Background");
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        var material = level.coffin.sharedMaterial;
        var bat = level.bat.GetComponent<SpriteRenderer>();
        Assign(bat, Sprite("01_Characters/Bat_Dracula.png"), 1.12f, 1.12f * 415 / 607, 4);
        Assign(level.bell, Sprite("02_Interactables/Bell_Hanging.png"), 1.25f * 307 / 387, 1.25f, 2);
        Assign(level.coffin, Sprite("02_Interactables/Coffin_Closed.png"), 1.8f * 320 / 510, 1.8f, 2);
        Assign(level.reflector.GetComponent<SpriteRenderer>(), Sprite("02_Interactables/Reflector_Plate_Rotating.png"), 2.6f, .28f, 2);
        Assign(level.artifact.GetComponent<SpriteRenderer>(), Sprite("07_Extras/Collectible_BloodDrop_Red.png"), .42f, .42f * 403 / 281, 5);

        // Crop through Sprite rect only: leave source PNG untouched, excluding the lower patch remnant.
        var wallTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "00_Background/BG_Room_Clean_GameplayObjectsRemoved.png");
        var wall = Region("RoomWall", wallTexture, new Rect(0, 211, 1672, 730), new Vector2(.5f, .5f));
        var background = Visual(level, "Room Artwork", material, new Vector2(0, .1f));
        Assign(background, wall, 18, 18f * 730 / 1672, -20);

        // Use the top stone strip only, keeping foreground artwork out of the toolbar.
        var floorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "04_Foreground/FG_Cracked_Stone_Floor_Edge.png");
        var floor = Region("FloorSurface", floorTexture, new Rect(0, 169, 1401, 30), new Vector2(.5f, .5f));
        Assign(level.transform.Find("Floor").GetComponent<SpriteRenderer>(), floor, 18, .35f, 3);
        foreach (string name in new[] { "Left Wall", "Right Wall", "Ceiling" })
            level.transform.Find(name).GetComponent<SpriteRenderer>().sprite = null;

        // Stand belongs to the stationary level root, not the rotating plank transform.
        var stand = Visual(level, "Reflector Stand Artwork", material, new Vector2(0, -.5f));
        Assign(stand, Sprite("02_Interactables/Reflector_Stand_Static.png"), .8f, .8f * 400 / 321, 0);
        var presentation = level.GetComponent<Level01ArtPresentation>();
        if (presentation == null) presentation = level.gameObject.AddComponent<Level01ArtPresentation>();
        presentation.openCoffin = Sprite("02_Interactables/Coffin_Open_Interior.png");
        presentation.echoSightIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "06_UI/UI_Ability_EchoSight_Icon.png");
        presentation.echoPulseIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "06_UI/UI_Ability_EchoPulse_Icon.png");
        presentation.lockedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "06_UI/UI_Ability_Locked_Icon.png");
        EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        AssetDatabase.SaveAssets();
        if (AssetDatabase.LoadAssetAtPath<Sprite>(Derived + "FloorEdge.asset") != null)
            AssetDatabase.DeleteAsset(Derived + "FloorEdge.asset");
        Debug.Log("[REVERBAT] Applied Guragam art to the existing level; no colliders or gameplay transforms changed.");
    }

    private static Sprite Sprite(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + relative);
    private static Sprite Region(string name, Texture2D texture, Rect rect, Vector2 pivot)
    {
        string path = Derived + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        var sprite = UnityEngine.Sprite.Create(texture, rect, pivot, 100, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }
    private static SpriteRenderer Visual(Level01Manager level, string name, Material material, Vector2 position)
    {
        var found = level.transform.Find(name);
        var obj = found == null ? new GameObject(name) : found.gameObject;
        obj.transform.SetParent(level.transform, false);
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one;
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr == null) sr = obj.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = material;
        return sr;
    }
    private static void Assign(SpriteRenderer renderer, Sprite sprite, float width, float height, int order)
    {
        Vector3 originalScale = renderer.transform.localScale;
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        // Unity rescales the Transform when changing Simple -> Sliced. Undo that presentation side effect.
        renderer.transform.localScale = originalScale;
        renderer.size = new Vector2(width / Mathf.Abs(renderer.transform.lossyScale.x), height / Mathf.Abs(renderer.transform.lossyScale.y));
        renderer.color = Color.white;
        renderer.sortingOrder = order;
    }
}
