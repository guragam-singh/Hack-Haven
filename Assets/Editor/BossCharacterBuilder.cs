using System;
using System.Collections.Generic;
using System.Linq;
using Reverbat.Boss;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

internal static class BossCharacterBuilder
{
    internal const string Art="Assets/Art/Boss/";
    internal const string Prefabs="Assets/Prefabs/Boss/";
    internal const string Materials=Art+"Materials/";
    static Material Lit(string name, Color c,string texture=null,float metallic=0,float gloss=.35f,bool clip=false)
    {
        string path=Materials+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
        m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",gloss);
        if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture));
        m.SetFloat("_Cull",0);
        if(clip){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.22f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    internal static Material Surface(string name,string hex,float metal=0,float gloss=.3f,float emission=0)
    {
        ColorUtility.TryParseHtmlString("#"+hex,out var color);
        var m=Lit(name,color,null,metal,gloss);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);EditorUtility.SetDirty(m);}
        return m;
    }
    private static Transform Child(Transform parent,string name,Vector3 position)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
    }
    private static Transform LoadModel(string file,Transform pivot)
    {
        var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(file));
        go.name=System.IO.Path.GetFileNameWithoutExtension(file);go.transform.SetParent(pivot,false);
        foreach(var a in go.GetComponentsInChildren<Animator>(true))a.enabled=false;
        foreach(var a in go.GetComponentsInChildren<Animation>(true))a.enabled=false;
        return go.transform;
    }
    private static BossDuelActor NewActor(string name,bool bat)
    {
        var actor=new GameObject(name).AddComponent<BossDuelActor>();actor.isBat=bat;
        actor.modelPivot=Child(actor.transform,"Performance pivot",Vector3.zero);return actor;
    }
    private static GameObject Save(BossDuelActor actor,string name)
    {
        var result=PrefabUtility.SaveAsPrefabAsset(actor.gameObject,Prefabs+name+".prefab");
        Object.DestroyImmediate(actor.gameObject);return result;
    }

    internal static GameObject Bat()
    {
        const string path=Art+"Bat/bat.FBX";
        var imp=(ModelImporter)AssetImporter.GetAtPath(path);
        if(imp.importCameras || imp.importLights){imp.importCameras=false;imp.importLights=false;imp.SaveAndReimport();}
        var actor=NewActor("Bat Dracula",true);
        var model=LoadModel(path,actor.modelPivot);
        var skin=model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        foreach(var helper in model.GetComponentsInChildren<MeshRenderer>(true))helper.enabled=false;
        skin.updateWhenOffscreen=true;
        var body=Lit("Bat fur",Color.white,"Bat/bat.jpg",0,.30f);
        var membrane=Lit("Bat wing membrane",new Color(.68f,.46f,.56f),"Bat/bat.jpg",0,.37f);
        var mouth=Surface("Bat mouth","873746");
        skin.sharedMaterials=new[]{body,mouth,membrane,membrane};
        actor.leftJoint=skin.bones.First(b=>b.name=="Wing_left_01");
        actor.rightJoint=skin.bones.First(b=>b.name=="Wing_right_01");
        actor.leftAxis=actor.leftJoint.parent.InverseTransformDirection(model.forward);
        actor.rightAxis=actor.rightJoint.parent.InverseTransformDirection(model.forward);
        var bounds=skin.bounds;float scale=2.45f/bounds.size.x;
        model.localRotation=Quaternion.Euler(-7,24,0)*model.localRotation;
        model.localScale*=scale;
        var bodyAnchor=skin.bones.First(b=>b.name=="Body");
        model.localPosition-=actor.modelPivot.InverseTransformPoint(bodyAnchor.position);
        return Save(actor,"BatDracula_Combat");
    }

    internal static GameObject Dictator()
    {
        var actor=NewActor("Dictator Dracula",false);
        var model=LoadModel(Art+"Dictator/Complete_Char_LP.fbx",actor.modelPivot);
        var still=Child(model,"Pose rig",Vector3.zero);
        Transform left=Child(still,"Left shoulder",new Vector3(-.206f,1.495f,0));
        Transform right=Child(still,"Right shoulder",new Vector3(.206f,1.495f,0));
        Transform le=Child(left,"Left elbow",new Vector3(-.355f,-.012f,0));
        Transform re=Child(right,"Right elbow",new Vector3(.355f,-.012f,0));
        var joints=new[]{still,left,right,le,re};
        var materials=new Dictionary<string,Material>(StringComparer.OrdinalIgnoreCase){
            {"Head_Body",Lit("Dictator skin",Color.white,"Dictator/SkinAtlas.jpg",0,.37f)},
            {"Shoulder_Guard_Boots",Lit("Engraved armor",Color.white,"Dictator/Armor.jpg",.43f,.46f)},
            {"Trench_Coat",Lit("Trench coat",Color.white,"Dictator/Coat.jpg",.07f,.34f)},
            {"Pants",Lit("Trousers",Color.white,"Dictator/Trousers.jpg",0,.24f)},
            {"Eye",Lit("Vampire iris",Color.white,"Dictator/Iris.jpg",0,.6f)},
            {"Teeth",Lit("Ivory teeth",Color.white,"Dictator/SkinAtlas.jpg")},
            {"Hair",Lit("Hair cards",Color.white,"Dictator/Hair.png",0,.32f,true)},
            {"Eyebrows",Lit("Eyebrow cards",Color.white,"Dictator/Hair.png",0,.26f,true)},
            {"Eyeleashes_Facial Hair",Lit("Facial hair",Color.white,"Dictator/Lashes.png",0,.25f,true)}
        };
        var filters=model.GetComponentsInChildren<MeshFilter>(true);
        foreach(var filter in filters)
        {
            var render=filter.GetComponent<MeshRenderer>();
            if(!render)continue;
            if(render.name.StartsWith("Eye_blend") || render.name.StartsWith("eyes_shade") || render.name.StartsWith("tear_line"))
            {render.enabled=false;continue;}
            Material Resolve(Material original)
            {
                string key=original.name.Split('.')[0];
                return materials.TryGetValue(key,out var mapped)?mapped:materials["Trench_Coat"];
            }
            var mapped=render.sharedMaterials.Select(Resolve).ToArray();
            string part=filter.name;
            bool skin=part=="Body" || part.StartsWith("Trench_Coat") || part.StartsWith("Shoulder_Guard") || part=="buttons" || part=="Coat_Details";
            if(!skin){render.sharedMaterials=mapped;continue;}
            // The original FBX is static. Skin a derived mesh to two shoulders/elbows in bind space.
            var mesh=Object.Instantiate(filter.sharedMesh);mesh.name="Stage "+part;
            if(part=="Body"){var uv=mesh.uv;for(int i=0;i<uv.Length;i++)uv[i].x*=.5f;mesh.uv=uv;}
            var verts=mesh.vertices;var weights=new BoneWeight[verts.Length];
            for(int i=0;i<verts.Length;i++)
            {
                var v=model.InverseTransformPoint(filter.transform.TransformPoint(verts[i]));
                float ax=Mathf.Abs(v.x);
                float shoulderLine=Mathf.Lerp(1.35f,1.08f,Mathf.InverseLerp(.15f,.42f,ax));
                float arm=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.145f,.29f,ax)) *
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(shoulderLine-.08f,shoulderLine+.04f,v.y));
                float forearm=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.62f,ax));
                weights[i]=new BoneWeight{boneIndex0=0,weight0=1-arm,boneIndex1=v.x<0?1:2,weight1=arm*(1-forearm),boneIndex2=v.x<0?3:4,weight2=arm*forearm};
            }
            mesh.boneWeights=weights;mesh.bindposes=joints.Select(j=>j.worldToLocalMatrix*filter.transform.localToWorldMatrix).ToArray();
            string path=Art+"Rig/"+part.Replace('/','_')+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!saved){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else {EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            var go=filter.gameObject;Object.DestroyImmediate(render);Object.DestroyImmediate(filter);
            var sk=go.AddComponent<SkinnedMeshRenderer>();sk.sharedMesh=saved;sk.sharedMaterials=mapped;sk.bones=joints;sk.rootBone=still;
            sk.updateWhenOffscreen=true;sk.localBounds=new Bounds(saved.bounds.center,saved.bounds.size+Vector3.one*.8f);
        }
        actor.leftJoint=left;actor.rightJoint=right;actor.restArmAngle=53;
        le.localRotation=Quaternion.Euler(0,20,9);re.localRotation=Quaternion.Euler(0,-22,-8);
        left.localRotation=Quaternion.AngleAxis(actor.restArmAngle,Vector3.forward);
        right.localRotation=Quaternion.AngleAxis(-actor.restArmAngle,Vector3.forward);
        actor.cape=Cape(model);
        model.localScale=Vector3.one*2.03f;model.localRotation=Quaternion.Euler(0,201,0);
        return Save(actor,"DictatorDracula_Combat");
    }
    private static Transform Cape(Transform parent)
    {
        var t=Child(parent,"Crimson mantle",new Vector3(0,1.50f,-.13f));
        var vertices=new List<Vector3>();var tris=new List<int>();
        for(int y=0;y<4;y++)for(int x=0;x<11;x++)
        {
            float u=x/10f*2-1,v=y/3f;
            vertices.Add(new Vector3(u*Mathf.Lerp(.22f,.58f,v),-v*(1.22f-.11f*Mathf.Abs(u)),
                -.015f-v*.31f+.09f*u*u+Mathf.Cos(x*Mathf.PI)*.032f*v));
            if(y<3 && x<10){int n=y*11+x;tris.AddRange(new[]{n,n+11,n+1,n+1,n+11,n+12});}
        }
        var mesh=new Mesh{name="Pleated cloak"};mesh.SetVertices(vertices);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+"Rig/Mantle.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!saved){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else {EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=saved;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Surface("Velvet lining","951B3E",.1f,.38f);
        return t;
    }
}
