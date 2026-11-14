using System.IO;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Level01ReadabilityInstaller
{
    [MenuItem("REVERBAT/Apply Bat Highlight and Smooth Pulse")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying){Debug.LogError("Stop Play Mode first.");return;}
        var level=Object.FindAnyObjectByType<Level01Manager>();
        if(level==null){Debug.LogError("Open Level01_Prototype first.");return;}
        const string folder="Assets/Art/Guragam/Level01/Materials/";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var revealShader=Shader.Find("Reverbat/Echo Reveal Sprite");
        var outlineShader=Shader.Find("Reverbat/Bat Highlight");
        if(revealShader==null || outlineShader==null){Debug.LogError("Reveal/highlight shaders are not ready.");return;}
        var reveal=Material(folder+"EchoReveal.mat",revealShader);
        var outline=Material(folder+"BatHighlight.mat",outlineShader);
        outline.SetColor("_OutlineColor",new Color(.82f,.74f,1,1));
        outline.SetFloat("_OutlinePixels",10);
        var bat=level.bat.GetComponent<SpriteRenderer>();
        bat.sharedMaterial=outline;
        const float width=1.12f;
        bat.size=new Vector2(width/Mathf.Abs(bat.transform.lossyScale.x),(width*415/607)/Mathf.Abs(bat.transform.lossyScale.y));
        var pulse=level.GetComponent<EchoPulse2D>();
        if(pulse==null) pulse=level.gameObject.AddComponent<EchoPulse2D>();
        pulse.revealMaterial=reveal;
        EditorUtility.SetDirty(pulse); EditorUtility.SetDirty(outline);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=level.gameObject;
        Debug.Log("[REVERBAT] Bat enlarged/highlighted. Echo Pulse cooldown and duration are editable on the level root.");
    }
    private static Material Material(string path,Shader shader)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        else material.shader=shader;
        return material;
    }
}
