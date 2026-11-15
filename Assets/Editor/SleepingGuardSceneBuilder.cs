using System;
using System.IO;
using System.Linq;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SleepingGuardSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Level02_SleepingGuard.unity";
    private const string Art = "Assets/Art/Guragam/Level02/";
    private const string Audio = "Assets/Audio/Level02/";
    private static Vector3 Position(float x, float y) => new Vector3((x-836)/100f,(470.5f-y)/100f,0);

    [MenuItem("REVERBAT/Create Sleeping Guard Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) return;
        if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        ConvertSprites();
        var reference = EditorSceneManager.OpenScene(Level01PrototypeBuilder.ScenePath, OpenSceneMode.Additive);
        var l1 = reference.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Level01Manager>()).Single();
        var icons = l1.GetComponent<Level01ArtPresentation>();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root = new GameObject(SleepingGuardLevel.SceneName);
        var level = root.AddComponent<SleepingGuardLevel>();
        Transform Node(string name, float x, float y, Transform parent = null)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent != null ? parent : root.transform);
            node.position = Position(x,y);
            return node;
        }
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/ReverbatPrototype/PlaceholderUnlit.mat");
        Sprite SpriteAt(string file)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art+file+".png");
            if (sprite == null) throw new InvalidOperationException("Missing sprite: "+file);
            return sprite;
        }
        SpriteRenderer Draw(string name, string file, float x,float y,float w,float h,int order, Transform parent=null)
        {
            var sr = Node(name,x,y,parent).gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAt(file);
            sr.transform.localScale = new Vector3(w/sr.sprite.rect.width,h/sr.sprite.rect.height,1);
            sr.sharedMaterial = unlit;
            sr.sortingOrder = order;
            return sr;
        }
        BoxCollider2D Box(Transform target, Vector2 worldSize, int layer)
        {
            var col = target.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(worldSize.x/target.lossyScale.x,worldSize.y/target.lossyScale.y);
            target.gameObject.layer = layer;
            return col;
        }
        var cam = UnityEngine.Object.Instantiate(l1.bat.roomCamera.gameObject,root.transform).GetComponent<Camera>();
        cam.name = "Main Camera";
        cam.orthographicSize = 4.705f;
        cam.transform.position = new Vector3(0,0,-10);
        cam.backgroundColor = Color.black;
        if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

        string bg = "00_Background/Hallway_Gameplay";
        var ambient = Draw("Hallway in darkness",bg,836,470.5f,1672,941,0);
        ambient.enabled = false;
        var bright = Draw("Hallway pulse reveal",bg,836,470.5f,1672,941,1);
        string door = "02_Interactables/ExitDoor_StoneArch";
        var doorway = Draw("Exit arch",door,1520,606,181,274,3);
        doorway.enabled = false;
        var litDoor = Draw("Exit arch pulse reveal",door,1520,606,181,274,4);
        level.revealedEnvironment = new[]{bright,litDoor};

        level.bat = UnityEngine.Object.Instantiate(l1.bat.gameObject, root.transform).GetComponent<BatController2D>();
        level.bat.name = "Bat Dracula";
        level.bat.level = null;
        level.bat.hallway = level;
        level.bat.roomCamera = cam;
        level.bat.transform.position = Position(378,374);
        var batSr = level.bat.GetComponent<SpriteRenderer>();
        batSr.sprite = SpriteAt("01_Characters/Bat_Player");
        batSr.drawMode = SpriteDrawMode.Simple;
        batSr.sharedMaterial = unlit;
        float batScale = 148f/batSr.sprite.rect.width;
        batSr.transform.localScale = new Vector3(batScale,batScale,1);
        batSr.color = Color.white;
        batSr.sortingOrder = 10;
        var batCollider = level.bat.GetComponent<Collider2D>();
        if (batCollider == null)
        {
            var circle = level.bat.gameObject.AddComponent<CircleCollider2D>();
            circle.radius = level.bat.radius/batScale;
            circle.isTrigger = true;
        }
        level.bat.gameObject.layer = 2;
        var echo = level.bat.echo;
        echo.level = null;
        echo.hallway = level;
        echo.outgoing = UnityEngine.Object.Instantiate(l1.bat.echo.outgoing,root.transform);
        echo.returning = UnityEngine.Object.Instantiate(l1.bat.echo.returning,root.transform);
        echo.outgoing.name = "Outgoing Echo";
        echo.returning.name = "Returning Echo";
        echo.outgoing.sortingOrder = 20;
        echo.returning.sortingOrder = 21;
        level.pulse = root.AddComponent<EchoPulse2D>();
        EditorUtility.CopySerialized(l1.GetComponent<EchoPulse2D>(),level.pulse);
        level.voice = root.AddComponent<DialogueManager>();

        var chair = Node("Guard and chair",765,565);
        level.guard = Draw("Guard", "01_Characters/Guard_Sleeping_OnChair",765,568,295,340,7,chair);
        level.startledGuard = SpriteAt("01_Characters/Guard_Startled_OnChair");
        level.guardTarget = Box(chair,new Vector2(3.03f,3.47f),1);
        level.shield = Draw("Metal shield","02_Interactables/Shield_Metal_Round",843,587,117,134,10,chair);
        level.shieldTarget = Box(level.shield.transform,new Vector2(1.36f,1.49f),1);
        level.shieldTarget.enabled = false;
        level.shield.color = new Color(1,1,1,0);
        level.snoreLetters = Draw("Snoring","04_FX/Guard_SnoreZ_Visual",648,403,80,40,11);
        level.alert = Draw("Startled","04_FX/AlertExclamation_Large",683,383,71,57,12);
        level.impact = Draw("Echo impact","04_FX/Echo_HitSpark_White",843,587,65,61,23);
        level.resonanceWaves = Draw("Resonance waves","04_FX/Resonance_DirectionalWaves",378,374,129,56,21);

        var wall = Node("Partition",0,0);
        wall.position = Vector3.zero;
        var outline = wall.gameObject.AddComponent<PolygonCollider2D>();
        outline.isTrigger = true;
        outline.points = new[]{(Vector2)Position(1024,258),(Vector2)Position(1090,251),
            (Vector2)Position(1201,345),(Vector2)Position(1201,682),(Vector2)Position(1091,734),
            (Vector2)Position(1034,734)};
        level.partition = outline;
        level.cornerApproach = Node("Partition near corner",1001,220);
        level.cornerDeparture = Node("Partition far corner",1198,218);
        level.femaleStart = Node("Female bat start",1534,372);
        level.curtainLatch = Node("Curtain latch",1478,284);
        level.female = Draw("Female bat","01_Characters/FemaleBat_Flying",1534,372,132,95,11);
        level.heartEyes = SpriteAt("01_Characters/FemaleBat_HeartEyes");
        level.hearts = Draw("Floating hearts","04_FX/FemaleBat_Hearts",1500,310,63,30,12);
        level.openWindow = Draw("Open red curtains","02_Interactables/Window_Curtains_Open",1401,327,383,370,4);
        // Register the stone arches: in the open illustration the arch is inset beneath the drapes.
        level.openWindow.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "02_Interactables/EveningWindow.mat");
        level.closedWindow = Draw("Closed red curtains","02_Interactables/Window_Curtains_Closed",1400,325,239,333,5);
        level.rays = Draw("Evening sunlight","04_FX/Window_Rays_Clean",1342,603,284,244,3);
        level.rays.color = new Color(1,.68f,.22f,.55f);
        level.exitDestination = Node("Exit destination",1780,374);
        level.exitArrow = Draw("Exit arrow","05_UI/ExitArrow_Yellow",1505,600,74,81,15);
        level.exitTarget = Box(level.exitArrow.transform,new Vector2(1.17f,1.43f),2);
        level.exitTarget.enabled = false;
        level.exitArrow.enabled = level.closedWindow.enabled = level.hearts.enabled = false;
        level.alert.enabled = level.impact.enabled = level.resonanceWaves.enabled = false;

        level.snoring = Node("Guard snoring",765,565).gameObject.AddComponent<AudioSource>();
        level.snoring.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"Guard_Snore_Loop.wav");
        level.snoring.loop = true;
        level.snoring.volume = level.snoreVolume;
        level.snoring.playOnAwake = false;
        level.effects = Node("Reaction sounds",1534,372).gameObject.AddComponent<AudioSource>();
        level.effects.playOnAwake = false;
        level.level2AfterFirstPulseDialogue = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"1.wav");
        level.exitDialogue = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"2.wav");
        level.wakeThud = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"Guard_Wake_Thud.wav");
        level.whistle = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio+"FemaleBat_Whistle.wav");
        level.sightIcon = icons.echoSightIcon;
        level.pulseIcon = icons.echoPulseIcon;
        level.lockedIcon = icons.lockedIcon;
        level.resonanceIcon = level.resonanceWaves.sprite.texture;
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath)
            .Append(new EditorBuildSettingsScene(ScenePath,true)).ToArray();
        EditorSceneManager.CloseScene(reference,true);
        EditorSceneManager.OpenScene(ScenePath);
        AssetDatabase.SaveAssets();
    }

    private static void ConvertSprites()
    {
        foreach (var path in Directory.GetFiles(Art,"*.png",SearchOption.AllDirectories))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
