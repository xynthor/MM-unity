using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMNeighborSeamPass
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    sealed class P { public string a,b; public bool horizontal; public P(string aa,string bb,bool h){a=aa;b=bb;horizontal=h;} }
    sealed class I { public TerrainData td; public float baseY; public string path; }

    static readonly Z[] Zones={
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("SilverCove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Z("Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Z("FreeHaven","Assets/Scenes/FreeHaven_SourceGrid.unity"),
        new Z("BootlegBay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Z("MistyIslands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Z("HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Z("Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Z("NewSorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity")};

    static readonly P[] Pairs={
        new P("SweetWater","Kriegspire",true),new P("Kriegspire","FrozenHighlands",true),
        new P("FrozenHighlands","SilverCove",true),new P("SilverCove","EelInfestedWaters",true),
        new P("ParadiseValley","Blackshire",true),new P("Blackshire","FreeHaven",true),
        new P("FreeHaven","BootlegBay",true),new P("BootlegBay","MistyIslands",true),
        new P("HermitsIsle","Dragonsand",true),new P("Dragonsand","MireOfTheDamned",true),
        new P("MireOfTheDamned","CastleIronfist",true),new P("CastleIronfist","NewSorpigal",true),
        new P("ParadiseValley","SweetWater",false),new P("Blackshire","Kriegspire",false),
        new P("FreeHaven","FrozenHighlands",false),new P("BootlegBay","SilverCove",false),
        new P("MistyIslands","EelInfestedWaters",false),new P("HermitsIsle","ParadiseValley",false),
        new P("Dragonsand","Blackshire",false),new P("MireOfTheDamned","FreeHaven",false),
        new P("CastleIronfist","BootlegBay",false),new P("NewSorpigal","MistyIslands",false)};

    const float HeightBand=16f;
    const float TextureBand=0f;

    [MenuItem("MMUnity/Stitch All Neighbor Region Seams")]
    public static void ApplyAll()
    {
        var info=new Dictionary<string,I>(StringComparer.OrdinalIgnoreCase);
        foreach(var z in Zones) info[z.key]=LoadInfo(z);
        var report=new List<string>{"pair,direction,max_before,max_after,height_band_m,texture_band_m"};
        foreach(var p in Pairs)
        {
            var a=info[p.a];var b=info[p.b];
            float before=MaxEdgeMismatch(a,b,p.horizontal);
            StitchHeights(a,b,p.horizontal);
            float after=MaxEdgeMismatch(a,b,p.horizontal);
            report.Add($"{p.a}|{p.b},{(p.horizontal?"E-W":"N-S")},{before:F3},{after:F3},{HeightBand:F0},{TextureBand:F0}");
            Debug.Log($"NEIGHBOR_SEAM {p.a}<->{p.b} dir={(p.horizontal?"E-W":"N-S")} before={before:F3} after={after:F3}");
        }
        Directory.CreateDirectory("Validation");
        File.WriteAllLines("Validation/NeighborSeamAudit.csv",report);
        AssetDatabase.SaveAssets();
        Debug.Log("NEIGHBOR_SEAMS_DONE pairs="+Pairs.Length);
    }

    static I LoadInfo(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);if(!t)throw new Exception(z.key+" terrain missing");
        string path=AssetDatabase.GetAssetPath(t.terrainData);
        return new I{td=AssetDatabase.LoadAssetAtPath<TerrainData>(path),baseY=t.transform.position.y,path=path};
    }
    static TerrainLayer SharedLayer()
    {
        string path="Assets/Materials/SourceGridTerrain/Enroth_Shared_Border.terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
        l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg");
        l.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_nor_gl_1k.jpg");
        l.tileSize=new Vector2(8f,8f);l.tileOffset=Vector2.zero;l.normalScale=1f;l.metallic=0f;l.smoothness=.04f;
        EditorUtility.SetDirty(l);return l;
    }

    static int EnsureLayer(TerrainData td,TerrainLayer shared)
    {
        var layers=td.terrainLayers;
        for(int i=0;i<layers.Length;i++)if(layers[i]==shared)return i;
        int oldN=layers.Length;var old=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
        var next=new TerrainLayer[oldN+1];Array.Copy(layers,next,oldN);next[oldN]=shared;td.terrainLayers=next;
        var a=new float[td.alphamapHeight,td.alphamapWidth,oldN+1];
        for(int y=0;y<td.alphamapHeight;y++)for(int x=0;x<td.alphamapWidth;x++)for(int k=0;k<oldN;k++)a[y,x,k]=old[y,x,k];
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);return oldN;
    }

    static int LayerIndex(TerrainData td,TerrainLayer layer)
    {
        var a=td.terrainLayers;for(int i=0;i<a.Length;i++)if(a[i]==layer)return i;return -1;
    }
    static float WorldY(I i,float n)=>i.baseY+n*i.td.size.y;
    static float NormY(I i,float w)=>Mathf.Clamp01((w-i.baseY)/i.td.size.y);

    static float MaxEdgeMismatch(I a,I b,bool horizontal)
    {
        int ra=a.td.heightmapResolution,rb=b.td.heightmapResolution;
        var ha=a.td.GetHeights(0,0,ra,ra);var hb=b.td.GetHeights(0,0,rb,rb);
        int n=Mathf.Min(ra,rb);float m=0f;
        for(int i=0;i<n;i++)
        {
            float ya=horizontal?WorldY(a,ha[i,ra-1]):WorldY(a,ha[ra-1,i]);
            float yb=horizontal?WorldY(b,hb[i,0]):WorldY(b,hb[0,i]);
            m=Mathf.Max(m,Mathf.Abs(ya-yb));
        }
        return m;
    }

    static float[] Smooth(float[] p,int radius)
    {
        var q=new float[p.Length];
        for(int i=0;i<p.Length;i++)
        {
            float s=0f;int n=0;
            for(int j=Mathf.Max(0,i-radius);j<=Mathf.Min(p.Length-1,i+radius);j++){s+=p[j];n++;}
            q[i]=s/Mathf.Max(1,n);
        }
        return q;
    }
    static void StitchHeights(I a,I b,bool horizontal)
    {
        int ra=a.td.heightmapResolution,rb=b.td.heightmapResolution;if(ra!=rb)throw new Exception("Neighbor height resolutions differ");
        var ha=a.td.GetHeights(0,0,ra,ra);var hb=b.td.GetHeights(0,0,rb,rb);int n=ra;
        var target=new float[n];
        for(int i=0;i<n;i++)
        {
            float ya=horizontal?WorldY(a,ha[i,n-1]):WorldY(a,ha[n-1,i]);
            float yb=horizontal?WorldY(b,hb[i,0]):WorldY(b,hb[0,i]);
            target[i]=(ya+yb)*.5f;
        }
        target=Smooth(target,4);
        int wa=Mathf.Clamp(Mathf.RoundToInt(HeightBand/(a.td.size.x/(n-1))),2,n/3);
        int wb=Mathf.Clamp(Mathf.RoundToInt(HeightBand/(b.td.size.x/(n-1))),2,n/3);
        for(int i=0;i<n;i++)
        {
            for(int d=0;d<wa;d++)
            {
                float u=1f-d/(float)(wa-1),w=Mathf.SmoothStep(0f,1f,u);int e=n-1-d;
                if(horizontal){float cur=WorldY(a,ha[i,e]);ha[i,e]=NormY(a,Mathf.Lerp(cur,target[i],w));}
                else {float cur=WorldY(a,ha[e,i]);ha[e,i]=NormY(a,Mathf.Lerp(cur,target[i],w));}
            }
            for(int d=0;d<wb;d++)
            {
                float u=1f-d/(float)(wb-1),w=Mathf.SmoothStep(0f,1f,u);int e=d;
                if(horizontal){float cur=WorldY(b,hb[i,e]);hb[i,e]=NormY(b,Mathf.Lerp(cur,target[i],w));}
                else {float cur=WorldY(b,hb[e,i]);hb[e,i]=NormY(b,Mathf.Lerp(cur,target[i],w));}
            }
        }
        a.td.SetHeights(0,0,ha);b.td.SetHeights(0,0,hb);EditorUtility.SetDirty(a.td);EditorUtility.SetDirty(b.td);
    }
    static void BlendPixel(float[,,] a,int y,int x,int common,float strength)
    {
        strength=Mathf.Clamp01(strength);int l=a.GetLength(2);float keep=1f-strength;
        for(int k=0;k<l;k++)a[y,x,k]*=keep;
        a[y,x,common]+=strength;
        float s=0f;for(int k=0;k<l;k++)s+=a[y,x,k];
        if(s>.0001f)for(int k=0;k<l;k++)a[y,x,k]/=s;
    }

    static void PaintSharedBorder(TerrainData a,TerrainData b,TerrainLayer shared,bool horizontal)
    {
        int ia=LayerIndex(a,shared),ib=LayerIndex(b,shared);if(ia<0||ib<0)throw new Exception("Shared border layer missing");
        int aw=a.alphamapWidth,ah=a.alphamapHeight,bw=b.alphamapWidth,bh=b.alphamapHeight;
        var aa=a.GetAlphamaps(0,0,aw,ah);var ab=b.GetAlphamaps(0,0,bw,bh);
        int wa=Mathf.Clamp(Mathf.RoundToInt(TextureBand/(a.size.x/aw)),2,(horizontal?aw:ah)/3);
        int wb=Mathf.Clamp(Mathf.RoundToInt(TextureBand/(b.size.x/bw)),2,(horizontal?bw:bh)/3);
        if(horizontal)
        {
            for(int y=0;y<ah;y++)for(int d=0;d<wa;d++){float u=1f-d/(float)(wa-1);BlendPixel(aa,y,aw-1-d,ia,Mathf.SmoothStep(0,1,u));}
            for(int y=0;y<bh;y++)for(int d=0;d<wb;d++){float u=1f-d/(float)(wb-1);BlendPixel(ab,y,d,ib,Mathf.SmoothStep(0,1,u));}
        }
        else
        {
            for(int x=0;x<aw;x++)for(int d=0;d<wa;d++){float u=1f-d/(float)(wa-1);BlendPixel(aa,ah-1-d,x,ia,Mathf.SmoothStep(0,1,u));}
            for(int x=0;x<bw;x++)for(int d=0;d<wb;d++){float u=1f-d/(float)(wb-1);BlendPixel(ab,d,x,ib,Mathf.SmoothStep(0,1,u));}
        }
        a.SetAlphamaps(0,0,aa);b.SetAlphamaps(0,0,ab);EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
    }
}

