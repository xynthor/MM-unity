using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMEcotoneSeamPass
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    sealed class P { public string a,b; public bool horizontal; public P(string aa,string bb,bool h){a=aa;b=bb;horizontal=h;} }
    sealed class I { public TerrainData td; public string path; }

    static readonly Z[] Zones={
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity")};
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
    const float BlendBand=48f;
    const int RoadLayer=7;

    [MenuItem("MMUnity/Witcher World - Blend Ecotone Seams")]
    public static void ApplyAll()
    {
        var info=new Dictionary<string,I>(StringComparer.OrdinalIgnoreCase);
        foreach(var z in Zones)info[z.key]=Load(z);
        var log=new List<string>{"pair,direction,band_m,skipped_road_samples"};
        foreach(var p in Pairs)
        {
            int skipped=Blend(info[p.a],info[p.b],p.horizontal);
            log.Add($"{p.a}|{p.b},{(p.horizontal?"E-W":"N-S")},{BlendBand:F0},{skipped}");
            Debug.Log($"ECOTONE {p.a}<->{p.b} roadSkipped={skipped}");
        }
        Directory.CreateDirectory("Validation");
        File.WriteAllLines("Validation/EcotoneSeamAudit.csv",log);
        AssetDatabase.SaveAssets();
        Debug.Log("ECOTONE_ALL_DONE pairs="+Pairs.Length);
    }
    static I Load(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        string path=AssetDatabase.GetAssetPath(t.terrainData);
        return new I{td=AssetDatabase.LoadAssetAtPath<TerrainData>(path),path=path};
    }

    static float[] Pixel(float[,,] a,int y,int x)
    {
        int l=a.GetLength(2);var v=new float[l];
        for(int k=0;k<l;k++)v[k]=a[y,x,k];
        return v;
    }

    static float[] Average(float[] a,float[] b)
    {
        int l=Mathf.Min(a.Length,b.Length);var v=new float[l];float s=0f;
        for(int k=0;k<l;k++){v[k]=(a[k]+b[k])*.5f;s+=v[k];}
        if(s>.0001f)for(int k=0;k<l;k++)v[k]/=s;
        return v;
    }

    static float[] SmoothTarget(float[][] src,int i,int radius)
    {
        int l=src[i].Length;var o=new float[l];float wsum=0f;
        for(int q=Mathf.Max(0,i-radius);q<=Mathf.Min(src.Length-1,i+radius);q++)
        {
            float w=1f/(1f+Mathf.Abs(q-i));
            for(int k=0;k<l;k++)o[k]+=src[q][k]*w;
            wsum+=w;
        }
        if(wsum>0f)for(int k=0;k<l;k++)o[k]/=wsum;
        return o;
    }
    static void MixPixel(float[,,] a,int y,int x,float[] target,float strength)
    {
        int l=a.GetLength(2);float road=a[y,x,RoadLayer];if(road>.20f)return;
        float s=0f;
        for(int k=0;k<l;k++)
        {
            float t=k==RoadLayer?0f:target[k];
            a[y,x,k]=Mathf.Lerp(a[y,x,k],t,strength);
            s+=a[y,x,k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)a[y,x,k]/=s;
    }

    static int Blend(I ia,I ib,bool horizontal)
    {
        var a=ia.td;var b=ib.td;
        if(a.alphamapLayers!=b.alphamapLayers||a.alphamapLayers<9)
            throw new Exception("Ecotone layers differ");
        int aw=a.alphamapWidth,ah=a.alphamapHeight,bw=b.alphamapWidth,bh=b.alphamapHeight;
        if(horizontal&&ah!=bh)throw new Exception("Ecotone edge resolutions differ");
        if(!horizontal&&aw!=bw)throw new Exception("Ecotone edge resolutions differ");
        var aa=a.GetAlphamaps(0,0,aw,ah);var bb=b.GetAlphamaps(0,0,bw,bh);
        int n=horizontal?Mathf.Min(ah,bh):Mathf.Min(aw,bw),skipped=0;
        int wa=Mathf.Clamp(Mathf.RoundToInt(BlendBand/(a.size.x/aw)),3,48);
        int wb=Mathf.Clamp(Mathf.RoundToInt(BlendBand/(b.size.x/bw)),3,48);
        var targets=new float[n][];var skip=new bool[n];
        for(int i=0;i<n;i++)
        {
            var va=horizontal?Pixel(aa,i,aw-1):Pixel(aa,ah-1,i);
            var vb=horizontal?Pixel(bb,i,0):Pixel(bb,0,i);
            skip[i]=va[RoadLayer]>.20f||vb[RoadLayer]>.20f;
            if(skip[i]){targets[i]=Average(va,vb);skipped++;continue;}
            targets[i]=Average(va,vb);
        }
        var smooth=new float[n][];
        for(int i=0;i<n;i++)smooth[i]=skip[i]?targets[i]:SmoothTarget(targets,i,2);

        for(int i=0;i<n;i++)
        {
            if(skip[i])continue;
            for(int d=0;d<wa;d++)
            {
                float u=1f-d/(float)Mathf.Max(1,wa-1);
                float w=Mathf.SmoothStep(0f,1f,u);
                if(horizontal)MixPixel(aa,i,aw-1-d,smooth[i],w);
                else MixPixel(aa,ah-1-d,i,smooth[i],w);
            }
            for(int d=0;d<wb;d++)
            {
                float u=1f-d/(float)Mathf.Max(1,wb-1);
                float w=Mathf.SmoothStep(0f,1f,u);
                if(horizontal)MixPixel(bb,i,d,smooth[i],w);
                else MixPixel(bb,d,i,smooth[i],w);
            }
        }
        a.SetAlphamaps(0,0,aa);b.SetAlphamaps(0,0,bb);
        EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
        return skipped;
    }
}
