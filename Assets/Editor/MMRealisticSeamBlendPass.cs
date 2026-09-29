using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMRealisticSeamBlendPass
{
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    sealed class P{public string a,b;public bool horizontal;public P(string aa,string bb,bool h){a=aa;b=bb;horizontal=h;}}
    sealed class I{public TerrainData td;}
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
        new P("SweetWater","Kriegspire",true),
        new P("Kriegspire","FrozenHighlands",true),
        new P("FrozenHighlands","SilverCove",true),
        new P("SilverCove","EelInfestedWaters",true),
        new P("ParadiseValley","Blackshire",true),
        new P("Blackshire","FreeHaven",true),
        new P("FreeHaven","BootlegBay",true),
        new P("BootlegBay","MistyIslands",true),
        new P("HermitsIsle","Dragonsand",true),
        new P("Dragonsand","MireOfTheDamned",true),
        new P("MireOfTheDamned","CastleIronfist",true),
        new P("CastleIronfist","NewSorpigal",true),
        new P("ParadiseValley","SweetWater",false),
        new P("Blackshire","Kriegspire",false),
        new P("FreeHaven","FrozenHighlands",false),
        new P("BootlegBay","SilverCove",false),
        new P("MistyIslands","EelInfestedWaters",false),
        new P("HermitsIsle","ParadiseValley",false),
        new P("Dragonsand","Blackshire",false),
        new P("MireOfTheDamned","FreeHaven",false),
        new P("CastleIronfist","BootlegBay",false),
        new P("NewSorpigal","MistyIslands",false)};

    static I Load(Z z)
    {
        var sc=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(z.scene,UnityEditor.SceneManagement.OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        return new I{td=t.terrainData};
    }

    static float[] EdgePixel(float[,,] a,int y,int x)
    {
        int l=a.GetLength(2);var v=new float[l];
        for(int k=0;k<l;k++)v[k]=a[y,x,k];
        return v;
    }
    static float[] Avg(float[] a,float[] b)
    {
        int l=Mathf.Min(a.Length,b.Length);var v=new float[l];float s=0f;
        int road=MMRealisticTerrainBiomePass.RoadOverlay;
        for(int k=0;k<l;k++)
        {
            if(k==road){v[k]=0f;continue;}
            v[k]=(a[k]+b[k])*.5f;s+=v[k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)v[k]/=s;
        return v;
    }
    static float[] Smooth(float[][] src,int i)
    {
        int l=src[i].Length;var o=new float[l];float ws=0f;
        int road=MMRealisticTerrainBiomePass.RoadOverlay;
        for(int q=Mathf.Max(0,i-4);q<=Mathf.Min(src.Length-1,i+4);q++)
        {
            float biome=0f;for(int k=0;k<l;k++)if(k!=road)biome+=src[q][k];
            if(biome<.0001f)continue; // never propagate road-only samples into biome ecotones
            float d=Mathf.Abs(q-i);float w=Mathf.Exp(-(d*d)/5f);
            for(int k=0;k<l;k++)if(k!=road)o[k]+=src[q][k]*w;
            ws+=w;
        }
        if(ws>.0001f)for(int k=0;k<l;k++)o[k]/=ws;
        o[road]=0f;
        return o;
    }

    static void BlendPixel(float[,,] a,int y,int x,float[] target,float strength)
    {
        int road=MMRealisticTerrainBiomePass.RoadOverlay;
        if(a[y,x,road]>.45f)return;
        int l=a.GetLength(2);float s=0f;
        for(int k=0;k<l;k++)
        {
            if(k==road){a[y,x,k]=0f;continue;}
            a[y,x,k]=Mathf.Lerp(a[y,x,k],target[k],strength);s+=a[y,x,k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)a[y,x,k]/=s;
    }

    static void BlendPair(I ia,I ib,bool horizontal,string key)
    {
        var a=ia.td;var b=ib.td;
        int aw=a.alphamapWidth,ah=a.alphamapHeight,bw=b.alphamapWidth,bh=b.alphamapHeight;
        if(a.alphamapLayers!=b.alphamapLayers)throw new Exception("Layer mismatch "+key);
        var aa=a.GetAlphamaps(0,0,aw,ah);var bb=b.GetAlphamaps(0,0,bw,bh);
        int n=horizontal?Mathf.Min(ah,bh):Mathf.Min(aw,bw);
        var targets=new float[n][];
        for(int i=0;i<n;i++)
        {
            var va=horizontal?EdgePixel(aa,i,aw-1):EdgePixel(aa,ah-1,i);
            var vb=horizontal?EdgePixel(bb,i,0):EdgePixel(bb,0,i);
            targets[i]=Avg(va,vb);
        }
        var smooth=new float[n][];
        for(int i=0;i<n;i++)smooth[i]=Smooth(targets,i);

        float pxA=horizontal?a.size.x/aw:a.size.z/ah;
        float pxB=horizontal?b.size.x/bw:b.size.z/bh;
        for(int i=0;i<n;i++)
        {
            float along=i/(float)Mathf.Max(1,n-1);
            float noise=Mathf.PerlinNoise(along*8.7f+key.GetHashCode()*.0001f,3.17f);
            float widthM=Mathf.Lerp(24f,46f,noise);
            int wa=Mathf.Clamp(Mathf.RoundToInt(widthM/Mathf.Max(.01f,pxA)),4,56);
            int wb=Mathf.Clamp(Mathf.RoundToInt(widthM/Mathf.Max(.01f,pxB)),4,56);
            for(int q=0;q<wa;q++)
            {
                float u=1f-q/(float)Mathf.Max(1,wa-1);
                float s=Mathf.SmoothStep(0f,1f,u);
                if(horizontal)BlendPixel(aa,i,aw-1-q,smooth[i],s);
                else BlendPixel(aa,ah-1-q,i,smooth[i],s);
            }
            for(int q=0;q<wb;q++)
            {
                float u=1f-q/(float)Mathf.Max(1,wb-1);
                float s=Mathf.SmoothStep(0f,1f,u);
                if(horizontal)BlendPixel(bb,i,q,smooth[i],s);
                else BlendPixel(bb,q,i,smooth[i],s);
            }
        }
        a.SetAlphamaps(0,0,aa);b.SetAlphamaps(0,0,bb);EditorUtility.SetDirty(a);EditorUtility.SetDirty(b);
    }

    [MenuItem("MMUnity/Realistic World/2 Blend Region Ecotones")]
    public static void ApplyAll()
    {
        var map=Zones.ToDictionary(z=>z.key,z=>Load(z),StringComparer.OrdinalIgnoreCase);
        var rows=new List<string>{"pair,direction"};
        foreach(var p in Pairs)
        {
            BlendPair(map[p.a],map[p.b],p.horizontal,p.a+"_"+p.b);
            rows.Add($"{p.a}|{p.b},{(p.horizontal?"E-W":"N-S")}");
            Debug.Log($"REALISTIC_ECOTONE {p.a}<->{p.b}");
        }
        Directory.CreateDirectory("Validation");
        File.WriteAllLines("Validation/RealisticEcotoneAudit.csv",rows);
        AssetDatabase.SaveAssets();
        Debug.Log("REALISTIC_ECOTONE_ALL_DONE pairs="+Pairs.Length);
    }
}
