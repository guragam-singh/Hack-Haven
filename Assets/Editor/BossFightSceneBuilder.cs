using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Reverbat.Boss;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class BossFightSceneBuilder
{
    public const string Path="Assets/Scenes/BossFight_3D.unity";
    private const string Art=BossCharacterBuilder.Art;
    private static Material stone,slate,edge,gold,velvet,carpet,shadow,glass,rose,flame,silver,wood,magic;
    private static Transform room;

    [MenuItem("REVERBAT/Open The Other Half")]
    public static void Open()
    {
        if(EditorApplication.isPlaying)return;
        if(File.Exists(Path)){EditorSceneManager.OpenScene(Path);return;}
        Build();
    }
    private static void Build()
    {
        Directory.CreateDirectory(Art+"Rig");Directory.CreateDirectory(BossCharacterBuilder.Materials);
        Directory.CreateDirectory(BossCharacterBuilder.Prefabs);AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var batPrefab=BossCharacterBuilder.Bat();var bossPrefab=BossCharacterBuilder.Dictator();
        int renderer=ForwardRenderer();
        Palette();
        var root=new GameObject("The Other Half");
        var fight=root.AddComponent<BossFightController>();
        var stage=fight.stage=root.AddComponent<BossBattleStage>();
        fight.hud=root.AddComponent<BossCombatUI>();fight.hud.fight=fight;
        var camera=Child(root.transform,"Duel camera",new Vector3(.15f,3.45f,-8.3f));
        camera.gameObject.tag="MainCamera";
        var lens=camera.gameObject.AddComponent<Camera>();camera.LookAt(new Vector3(0,1.92f,2.5f));lens.fieldOfView=39;lens.nearClipPlane=.08f;lens.farClipPlane=80;
        lens.clearFlags=CameraClearFlags.SolidColor;lens.backgroundColor=new Color(.038f,.021f,.073f);lens.allowHDR=true;
        camera.gameObject.AddComponent<AudioListener>();
        var settings=lens.GetUniversalAdditionalCameraData();settings.SetRenderer(renderer);settings.renderPostProcessing=true;settings.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        stage.cameraRig=camera.gameObject.AddComponent<BossCameraRig>();stage.cameraRig.lens=lens;
        stage.bat=Actor(batPrefab,root.transform,new Vector3(-1.76f,2f,-1.74f));
        stage.dictator=Actor(bossPrefab,root.transform,new Vector3(1.06f,.57f,3.64f));
        stage.mirrorAnchor=Child(root.transform,"Mirror reflection point",new Vector3(-.48f,3.7f,.88f));
        var effects=Child(root.transform,"Battle effects",Vector3.zero);
        stage.fx=effects.gameObject.AddComponent<BossBattleEffects>();
        stage.fx.lightInk=magic;stage.fx.mirrorGlass=silver;stage.fx.brass=gold;stage.fx.crateWood=wood;stage.fx.iron=edge;
        stage.fx.wing=BossCharacterBuilder.Surface("Swarm plum","391F55",.17f,.3f,.13f);stage.fx.eyes=flame;
        stage.audioPlayer=root.AddComponent<AudioSource>();stage.audioPlayer.playOnAwake=false;stage.audioPlayer.spatialBlend=0;stage.audioPlayer.volume=.76f;
        stage.sounds=Enum.GetNames(typeof(BossCue)).Select(n=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Boss/"+n+".wav")).ToArray();
        if(stage.sounds.Any(c=>!c))throw new InvalidOperationException("Missing a boss audio cue.");
        room=Child(root.transform,"Throne chamber",Vector3.zero);Arena();
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.51f,.45f,.65f);
        RenderSettings.ambientEquatorColor=new Color(.35f,.25f,.39f);
        RenderSettings.ambientGroundColor=new Color(.19f,.13f,.24f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(.105f,.06f,.155f);RenderSettings.fogStartDistance=13;RenderSettings.fogEndDistance=32;
        MakeLighting();
        DressCourt();
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,Path))throw new Exception("Could not save boss scene.");
        // Explicit scene registration allows standalone editor/build testing. Story scenes never load it.
        if(!EditorBuildSettings.globalScenes.Any(x=>x.path==Path))
            EditorBuildSettings.globalScenes=EditorBuildSettings.globalScenes.Append(new EditorBuildSettingsScene(Path,true)).ToArray();
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(Path);
    }
    private static int ForwardRenderer()
    {
        var rp=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var so=new SerializedObject(rp);var list=so.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue is UniversalRendererData)return i;
        const string path=Art+"BossForwardRenderer.asset";
        if(!AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path))
        {
            if(!AssetDatabase.CopyAsset("Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRendererData.asset",path))
                throw new InvalidOperationException("Could not copy the installed URP forward renderer.");
        }
        int slot=list.arraySize;list.arraySize++;
        list.GetArrayElementAtIndex(slot).objectReferenceValue=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(rp);return slot;
    }
    private static void Palette()
    {
        stone=BossCharacterBuilder.Surface("Basalt","49435D",.13f,.30f);
        slate=BossCharacterBuilder.Surface("Night slate","39334E",.15f,.38f);
        edge=BossCharacterBuilder.Surface("Wrought iron","292334",.44f,.39f);
        shadow=BossCharacterBuilder.Surface("Recesses","20182D",.02f,.22f);
        gold=BossCharacterBuilder.Surface("Aged gilt","B9864B",.50f,.53f);
        velvet=BossCharacterBuilder.Surface("Oxblood velvet","88213B",0,.23f);
        carpet=BossCharacterBuilder.Surface("Wine runner","651F38",0,.28f);
        glass=BossCharacterBuilder.Surface("Twilight glass","6550AF",.10f,.4f,.85f);
        rose=BossCharacterBuilder.Surface("Rose glass","BE527C",.13f,.42f,.75f);
        flame=BossCharacterBuilder.Surface("Candlelight","FFD698",0,.35f,1.6f);
        silver=BossCharacterBuilder.Surface("Echo silver","79DCE8",.42f,.75f,.39f);
        wood=BossCharacterBuilder.Surface("Crate walnut","9E5B37",.02f,.28f);
        string path=BossCharacterBuilder.Materials+"Spell light.mat";magic=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!magic){magic=new Material(Shader.Find("Reverbat/Boss Light"));AssetDatabase.CreateAsset(magic,path);}
    }
    private static BossDuelActor Actor(GameObject prefab,Transform parent,Vector3 position)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        go.transform.localPosition=position;PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        return go.GetComponent<BossDuelActor>();
    }
    private static Transform Child(Transform parent,string name,Vector3 pos)
    {
        var tr=new GameObject(name).transform;tr.SetParent(parent,false);tr.localPosition=pos;return tr;
    }
    private static Transform Part(Transform parent,string name,PrimitiveType primitive,Vector3 pos,Vector3 scale,Material m)
    {
        var go=GameObject.CreatePrimitive(primitive);go.name=name;var tr=go.transform;
        tr.SetParent(parent,false);tr.localPosition=pos;tr.localScale=scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=m;return tr;
    }
    private static Transform Box(Transform p,string name,float x,float y,float z,float w,float h,float d,Material m)
        =>Part(p,name,PrimitiveType.Cube,new Vector3(x,y,z),new Vector3(w,h,d),m);
    private static Transform ColumnPart(Transform p,string name,float y,float w,float h,Material m)
        =>Part(p,name,PrimitiveType.Cylinder,new Vector3(0,y,0),new Vector3(w,h/2,w),m);
    private static void Rib(Transform p,string name,Vector3 a,Vector3 b,float width,float depth,Material m)
    {
        var t=Part(p,name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),depth),m);
        t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
    }
    private static Mesh MeshAsset(string name,Vector3[] v,int[] indices)
    {
        string path=Art+"Rig/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh)return mesh;
        mesh=new Mesh{name=name,vertices=v,triangles=indices};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private static Transform Panel(Transform p,string name,Vector3 pos,Mesh mesh,Material m)
    {
        var t=Child(p,name,pos);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;return t;
    }
    private static void Arena()
    {
        var floor=Child(room,"Stone nave",Vector3.zero);
        Box(floor,"Subfloor",0,-.30f,1,14.4f,.44f,22.5f,shadow);
        for(int row=0;row<9;row++)for(int col=0;col<7;col++)
            Box(floor,"Paving",col*2.00f-6,-.058f,row*2.05f-7.5f,1.985f,.11f,2.035f,(col+row)%3==0?stone:slate);
        Box(floor,"Aisle runner",0,.009f,-2.2f,3.8f,.025f,11.5f,carpet);
        for(int sign=-1;sign<=1;sign+=2)
        {
            Box(floor,"Woven border",sign*1.82f,.025f,-2.2f,.040f,.01f,11.5f,gold);
            Box(floor,"Woven inner border",sign*1.72f,.026f,-2.2f,.018f,.01f,11.5f,gold);
        }
        var dais=Child(room,"Three steps to the throne",Vector3.zero);
        for(int i=0;i<3;i++)
        {
            float front=1.65f+i*.39f;
            Box(dais,"Stone step",.6f,.09f+i*.18f,(front+8.4f)*.5f,9.2f-i*.42f,.18f,8.4f-front,stone);
            Box(dais,"Brass nosing",.6f,.13f+i*.18f,front-.015f,9.22f-i*.42f,.026f,.045f,gold);
        }
        Box(dais,"Throne carpet",1.05f,.55f,5.23f,2.55f,.022f,5.38f,carpet);
        Box(room,"Apse wall",0,4.25f,9.15f,14.6f,9,.45f,slate);
        for(int sign=-1;sign<=1;sign+=2)
        {
            Box(room,"Side gallery",sign*7.05f,3.7f,2,.35f,8,16.5f,slate);
            Box(room,"Gallery plinth",sign*6.78f,.50f,2,.50f,1,16.6f,stone);
            Box(room,"Gilt cornice",sign*6.72f,4.15f,2,.25f,.095f,16.7f,gold);
        }
        for(int i=0;i<3;i++)
        {
            float z=-2+i*4.55f;
            for(int sign=-1;sign<=1;sign+=2)
            {
                var pillar=Child(room,"Engaged column",new Vector3(sign*5.55f,0,z));
                ColumnPart(pillar,"Pedestal",.24f,1.25f,.48f,stone);
                ColumnPart(pillar,"Lower ring",.52f,1.05f,.14f,gold);
                ColumnPart(pillar,"Shaft",3.2f,.79f,5.38f,stone);
                for(int j=0;j<6;j++){
                    float a=j*Mathf.PI/3;
                    Part(pillar,"Clustered shaft",PrimitiveType.Cylinder,new Vector3(Mathf.Sin(a)*.34f,3.2f,Mathf.Cos(a)*.34f),new Vector3(.26f,2.6f,.26f),slate);
                }
                ColumnPart(pillar,"Collar",5.86f,1.02f,.18f,gold);ColumnPart(pillar,"Capital",6.12f,1.30f,.35f,stone);
                Vector3 a0=new Vector3(sign*5.55f,6.2f,z),a1=new Vector3(sign*3.9f,7.55f,z),a2=new Vector3(0,8.9f,z);
                Rib(room,"Vault rib",a0,a1,.26f,.35f,stone);Rib(room,"Vault rib",a1,a2,.25f,.35f,stone);
            }
        }
        for(int i=0;i<3;i++) Window(i==0?1.05f:i==1?-3.2f:4.8f,i==0?2.35f:1.36f,i==0?4.45f:3.4f);
        Banner(-1.22f,8.5f,5.75f);Banner(3.2f,8.5f,5.75f);
        Throne();
        for(int sign=-1;sign<=1;sign+=2)
        {
            CandleStand(new Vector3(sign*3.40f,.55f,4.8f));CandleStand(new Vector3(sign*4.65f,0,-.35f));
        }
    }
    private static void Window(float x,float width,float height)
    {
        var p=Child(room,"Lancet window",new Vector3(x,2.80f,8.85f));
        Vector3[] v={new Vector3(-.5f,0,0),new Vector3(-.5f,.65f,0),new Vector3(-.34f,.82f,0),new Vector3(0,1,0),
            new Vector3(.34f,.82f,0),new Vector3(.5f,.65f,0),new Vector3(.5f,0,0)};
        var pane=Panel(p,"Dusklight",Vector3.zero,MeshAsset("Lancet",v,new[]{0,1,2,0,2,3,0,3,4,0,4,5,0,5,6}),glass);
        pane.localScale=new Vector3(width,height,1);
        for(int i=0;i<v.Length;i++){Vector3 A=Vector3.Scale(v[i],pane.localScale);Vector3 B=Vector3.Scale(v[(i+1)%v.Length],pane.localScale);A.z=B.z=-.06f;Rib(p,"Carved surround",A,B,.13f,.15f,stone);}
        for(int i=-1;i<=1;i++){float px=i*width*.23f;Rib(p,"Lead mullion",new Vector3(px,0,-.09f),new Vector3(px,height*(i==0?.97f:.8f),-.09f),.032f,.05f,gold);}
        for(int j=0;j<3;j++)for(int k=-1;k<=1;k+=2)
        {
            float h=.25f+j*.21f;
            Rib(p,"Diamond lead",new Vector3(0,height*h,-.10f),new Vector3(k*width*.46f,height*(h+.12f),-.10f),.035f,.04f,gold);
        }
        var med=Part(p,"Rose medallion",PrimitiveType.Cylinder,new Vector3(0,height*.69f,-.10f),new Vector3(width*.42f,.033f,width*.42f),rose);med.localRotation=Quaternion.Euler(90,0,0);
        var seal=Box(p,"Gilt star",0,height*.69f,-.16f,width*.16f,width*.16f,.04f,gold);seal.localRotation=Quaternion.Euler(0,0,45);
    }
    private static void Banner(float x,float z,float y)
    {
        var p=Child(room,"Royal standard",new Vector3(x,y,z));
        Box(p,"Standard crossbar",0,.09f,.05f,1.32f,.075f,.075f,gold);
        Vector3[] v={new Vector3(-.53f,0,0),new Vector3(.53f,0,0),new Vector3(.53f,-2.6f,0),new Vector3(0,-2.23f,-.05f),new Vector3(-.53f,-2.6f,0)};
        Panel(p,"Forked velvet",Vector3.zero,MeshAsset("Forked standard",v,new[]{0,1,3,1,2,3,0,3,4}),velvet);
        for(int i=-1;i<=1;i+=2){Box(p,"Stitched border",i*.46f,-1.17f,-.045f,.025f,2.35f,.02f,gold);Rib(p,"Heraldic wing",new Vector3(0,-1.0f,-.08f),new Vector3(i*.31f,-.73f,-.08f),.09f,.025f,gold);}
        Box(p,"Crest",0,-.95f,-.075f,.16f,.16f,.05f,gold).localRotation=Quaternion.Euler(0,0,45);
    }
    private static void Throne()
    {
        var p=Child(room,"Throne",new Vector3(1.06f,.55f,6.34f));
        Box(p,"Dais",0,.12f,0,2.62f,.24f,2.08f,edge);
        Box(p,"Velvet seat",0,1.0f,-.12f,1.92f,.38f,1.52f,velvet);
        Box(p,"Gilt back",0,2.06f,.57f,2.13f,3.62f,.27f,gold);
        Box(p,"Velvet back",0,2.13f,.38f,1.87f,3.27f,.21f,velvet);
        Box(p,"Crown",0,3.93f,.56f,1.48f,1.48f,.23f,gold).localRotation=Quaternion.Euler(0,0,45);
        Box(p,"Crown velvet",0,3.91f,.39f,1.28f,1.28f,.17f,velvet).localRotation=Quaternion.Euler(0,0,45);
        for(int i=-1;i<=1;i+=2)
        {
            Box(p,"Arm",i*1.01f,1.42f,-.1f,.32f,.24f,1.76f,gold);
            Box(p,"Arm support",i*1.01f,.73f,-.79f,.25f,1.32f,.24f,edge);
            Box(p,"Carved upright",i*1.06f,2.17f,.50f,.20f,3.92f,.34f,gold);
            Box(p,"Finial",i*1.06f,4.26f,.50f,.26f,.26f,.25f,rose).localRotation=Quaternion.Euler(0,0,45);
        }
    }
    private static void CandleStand(Vector3 at)
    {
        var p=Child(room,"Candelabrum",at);
        ColumnPart(p,"Foot",.085f,.55f,.17f,edge);ColumnPart(p,"Stem",.72f,.12f,1.35f,gold);
        for(int i=-1;i<=1;i++){float x=i*.23f,y=i==0?1.63f:1.41f;
            Rib(p,"Branch",new Vector3(0,1.08f,0),new Vector3(x,y-.18f,0),.055f,.06f,gold);
            Box(p,"Candle",x,y-.03f,0,.09f,.30f,.09f,flame);
            Part(p,"Flame",PrimitiveType.Sphere,new Vector3(x,y+.205f,0),new Vector3(.09f,.22f,.09f),flame);
        }
    }
    internal static void DressCourt()
    {
        var fight=Object.FindAnyObjectByType<BossFightController>();
        room=fight.transform.Find("Throne chamber");
        if(room.Find("Court insignia"))return;
        Palette();
        var decor=Child(room,"Court insignia",Vector3.zero);
        // Thin brass marquetry interrupts the long stone aisle without obscuring either fighter.
        for(int k=0;k<2;k++)
        {
            float radius=1.10f + .12f*k;
            for(int i=0;i<36;i++)
            {
                float a=i*Mathf.PI/18,b=(i+1)*Mathf.PI/18;
                Vector3 pa=new Vector3(Mathf.Cos(a)*radius,.041f,-.20f+Mathf.Sin(a)*radius);
                Vector3 pb=new Vector3(Mathf.Cos(b)*radius,.041f,-.20f+Mathf.Sin(b)*radius);
                var seg=Part(decor,"Inlay",PrimitiveType.Cube,(pa+pb)*.5f,new Vector3(k==0?.025f:.012f,.008f,(pb-pa).magnitude+.009f),gold);
                seg.localRotation=Quaternion.LookRotation(pb-pa);
            }
        }
        for(int i=0;i<8;i++)
        {
            float ang=i*Mathf.PI/4;
            var star=Box(decor,"Compass ray",Mathf.Sin(ang)*.40f,.044f,-.20f+Mathf.Cos(ang)*.40f,.052f,.009f,i%2==0?.83f:.51f,gold);
            star.localRotation=Quaternion.Euler(0,i*45,0);
        }
        var rig=fight.stage.cameraRig.lens.GetUniversalAdditionalCameraData();
        rig.renderPostProcessing=true;rig.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        var volume=decor.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=3;
        string path=Art+"ThroneAtmosphere.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile)
        {
            profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.22f);bloom.threshold.Override(.95f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.22f);vignette.smoothness.Override(.49f);
            var grade=profile.Add<ColorAdjustments>(true);grade.contrast.Override(7f);grade.postExposure.Override(-.12f);
            foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
            EditorUtility.SetDirty(profile);
        }
        volume.sharedProfile=profile;
    }
    private static void Lamp(string name,Vector3 p,Color color,float intensity,float range)
    {
        var light=Child(room,name,p).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;
    }
    private static void MakeLighting()
    {
        var key=Child(room,"Moon through the gallery",new Vector3(0,7,-4)).gameObject.AddComponent<Light>();
        key.type=LightType.Directional;key.color=new Color(.84f,.85f,1);key.intensity=1.6f;
        key.transform.rotation=Quaternion.Euler(44,-30,0);key.shadows=LightShadows.Hard;key.shadowStrength=.65f;RenderSettings.sun=key;
        Lamp("Crimson throne rim",new Vector3(3.7f,3.9f,4.8f),new Color(1,.17f,.29f),20,9);
        Lamp("Blue gallery",new Vector3(-3.0f,4,2.5f),new Color(.30f,.46f,1),17,9);
        Lamp("Footlights",new Vector3(.15f,2.95f,.62f),new Color(1,.70f,.52f),9,7);
        Lamp("Aisle rim",new Vector3(-2.9f,2,-3.8f),new Color(.25f,.72f,1),8,5);
        var dust=Child(room,"Drifting embers",new Vector3(0,.8f,3.9f)).gameObject.AddComponent<ParticleSystem>();
        var main=dust.main;main.startColor=new Color(.94f,.66f,.50f,.52f);main.maxParticles=40;main.startSize=new ParticleSystem.MinMaxCurve(.018f,.046f);
        main.startLifetime=5;main.startSpeed=0;main.prewarm=true;
        var emission=dust.emission;emission.rateOverTime=5;
        var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(8,3,6);
        var velocity=dust.velocityOverLifetime;velocity.enabled=true;velocity.x=.035f;velocity.y=.15f;velocity.z=.05f;
        dust.GetComponent<ParticleSystemRenderer>().sharedMaterial=magic;
    }
}
