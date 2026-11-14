using System.IO;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Small deterministic UI drawings: transparent keycaps with uneven ink strokes and purple accents.
public static class TutorialControlAssetInstaller
{
    private const string Folder = "Assets/Art/Guragam/Level01/Tutorial/";
    private static readonly Color Ink = new Color(.83f, .71f, .94f);
    private static readonly Color Paper = new Color(.95f, .9f, .79f);
    private static readonly Color Shadow = new Color(.11f, .065f, .16f);

    public static void Apply()
    {
        if (EditorApplication.isPlaying) return;
        var level = Object.FindFirstObjectByType<Level01Manager>();
        if (level == null) return;
        Directory.CreateDirectory(Folder);
        foreach (char letter in "WASD") MakeKey(letter);
        MakeMouse();
        AssetDatabase.Refresh();
        var art = level.GetComponent<TutorialPresentation>();
        if (art == null) art = Undo.AddComponent<TutorialPresentation>(level.gameObject);
        art.keyW = Load("Key_W"); art.keyA = Load("Key_A");
        art.keyS = Load("Key_S"); art.keyD = Load("Key_D"); art.mouseIcon = Load("Mouse");
        EditorUtility.SetDirty(art);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        EditorSceneManager.SaveScene(level.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static Texture2D Load(string name)
    {
        string path = Folder + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Texture2D Canvas(int height = 128)
    {
        var t = new Texture2D(128, height, TextureFormat.RGBA32, false);
        t.SetPixels(new Color[128 * height]);
        return t;
    }

    private static void MakeKey(char letter)
    {
        var t = Canvas();
        Vector2[] edge = {new Vector2(14,12),new Vector2(110,10),new Vector2(117,20),new Vector2(114,111),new Vector2(105,118),new Vector2(15,115),new Vector2(10,105),new Vector2(11,23)};
        Polygon(t, edge, Shadow);
        for(int i=0;i<edge.Length;i++) Line(t,edge[i],edge[(i+1)%edge.Length],Ink,3);
        Line(t,new Vector2(21,22),new Vector2(106,20),new Color(.4f,.24f,.5f),4);
        Line(t,new Vector2(23,108),new Vector2(44,109),Paper,2);
        Line(t,new Vector2(95,31),new Vector2(105,41),Ink,1);
        Vector2[] strokes;
        switch(letter)
        {
            case 'W': strokes=new[]{new Vector2(35,88),new Vector2(44,40),new Vector2(44,40),new Vector2(63,70),new Vector2(63,70),new Vector2(80,39),new Vector2(80,39),new Vector2(92,89)};break;
            case 'A': strokes=new[]{new Vector2(36,38),new Vector2(62,90),new Vector2(62,90),new Vector2(89,39),new Vector2(47,57),new Vector2(78,58)};break;
            case 'S': strokes=new[]{new Vector2(86,85),new Vector2(47,89),new Vector2(47,89),new Vector2(37,68),new Vector2(37,68),new Vector2(81,60),new Vector2(81,60),new Vector2(86,40),new Vector2(86,40),new Vector2(38,39)};break;
            default: strokes=new[]{new Vector2(40,38),new Vector2(40,89),new Vector2(40,89),new Vector2(69,88),new Vector2(69,88),new Vector2(88,73),new Vector2(88,73),new Vector2(88,53),new Vector2(88,53),new Vector2(68,39),new Vector2(68,39),new Vector2(40,38)};break;
        }
        for(int i=0;i<strokes.Length;i+=2) Line(t,strokes[i],strokes[i+1],Paper,5);
        Save(t,"Key_"+letter);
    }

    private static void MakeMouse()
    {
        var t=Canvas(176);
        Vector2[] edge={new Vector2(64,13),new Vector2(38,20),new Vector2(21,43),new Vector2(19,112),new Vector2(28,142),new Vector2(45,158),new Vector2(64,163),new Vector2(85,157),new Vector2(103,140),new Vector2(110,112),new Vector2(108,44),new Vector2(90,20)};
        Polygon(t,edge,Shadow);
        for(int i=0;i<edge.Length;i++) Line(t,edge[i],edge[(i+1)%edge.Length],Ink,3);
        Line(t,new Vector2(64,161),new Vector2(63,103),Paper,3);
        Line(t,new Vector2(20,103),new Vector2(108,103),Ink,3);
        Line(t,new Vector2(63,143),new Vector2(63,124),Paper,8);
        Line(t,new Vector2(32,122),new Vector2(36,140),Paper,3);
        Line(t,new Vector2(37,43),new Vector2(40,33),Ink,1);
        Save(t,"Mouse");
    }

    private static void Save(Texture2D texture,string name)
    {
        texture.Apply();
        File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static void Polygon(Texture2D t,Vector2[] vertices,Color color)
    {
        for(int y=0;y<t.height;y++) for(int x=0;x<t.width;x++)
        {
            bool inside=false;
            for(int i=0,j=vertices.Length-1;i<vertices.Length;j=i++)
            {
                var a=vertices[i];var b=vertices[j];
                if((a.y>y)!=(b.y>y) && x<(b.x-a.x)*(y-a.y)/(b.y-a.y)+a.x) inside=!inside;
            }
            if(inside)t.SetPixel(x,y,color);
        }
    }

    private static void Line(Texture2D t,Vector2 a,Vector2 b,Color color,float width)
    {
        Vector2 delta=b-a;
        for(int y=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,b.y)-width));y<=Mathf.Min(t.height-1,Mathf.CeilToInt(Mathf.Max(a.y,b.y)+width));y++)
        for(int x=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,b.x)-width));x<=Mathf.Min(t.width-1,Mathf.CeilToInt(Mathf.Max(a.x,b.x)+width));x++)
        {
            float along=Mathf.Clamp01(Vector2.Dot(new Vector2(x,y)-a,delta)/delta.sqrMagnitude);
            if(Vector2.Distance(new Vector2(x,y),a+delta*along)<=width*.5f)t.SetPixel(x,y,color);
        }
    }
}
