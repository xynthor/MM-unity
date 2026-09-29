using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMSixTerrainAudit
{
    const int N=128;
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity")};

    static string F(float v)=>v.ToString("F4",CultureInfo.InvariantCulture);

    [MenuItem("MMUnity/Realistic World/Audit Every Terrain Square")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/SixTerrainAudit");
        var summary=new List<string>{"zone,cells,green,light_green,desert,volcanic,snow,arid,road,low_support,ecotone,micropatch_smoothed,real_fail,extra_layers"};
        foreach(var z in Zones)Audit(z,summary);
        File.WriteAllLines("Validation/SixTerrainAudit/summary.csv",summary);
        Debug.Log("SIX_TERRAIN_AUDIT_DONE cells="+(Zones.Length*N*N));
    }

    static void Audit(Z z,List<string> summary)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        string d=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(d+"/tilemap_u8.bin");var grp=File.ReadAllBytes(d+"/tile_groups_u8.bin");var sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var td=t.terrainData;var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
        int[] count=new int[7];int lowSupport=0,ecotone=0,micropatch=0,realFail=0,extra=0;
        var rows=new List<string>{"sx,sy,group,flags,expected,dominant,expectedWeight,green,light_green,desert,volcanic,snow,arid,road,status"};
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)
        {
            byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
            int exp=MMRealisticTerrainBiomePass.BaseClassAt(z.key,g,f,sy);
            int ax=Mathf.Clamp(sx*td.alphamapWidth/N+td.alphamapWidth/(N*2),0,td.alphamapWidth-1);
            int ay=Mathf.Clamp((N-1-sy)*td.alphamapHeight/N+td.alphamapHeight/(N*2),0,td.alphamapHeight-1);
            int dom=0;float best=-1f;for(int k=0;k<a.GetLength(2);k++)if(a[ay,ax,k]>best){best=a[ay,ax,k];dom=k;}
            if(exp>=0&&exp<7)count[exp]++;
            float ew=(exp>=0&&exp<a.GetLength(2))?a[ay,ax,exp]:0f;
            bool low=ew<.22f;string status="OK";
            if(low)
            {
                lowSupport++;
                int border=Mathf.Min(Mathf.Min(sx,127-sx),Mathf.Min(sy,127-sy));
                if(border<=6){status="ECOTONE_TRANSITION";ecotone++;}
                else
                {
                    int same=0,total=0;
                    for(int yy=Mathf.Max(0,sy-2);yy<=Mathf.Min(127,sy+2);yy++)
                    for(int xx=Mathf.Max(0,sx-2);xx<=Mathf.Min(127,sx+2);xx++)
                    {
                        byte rr=tile[yy*N+xx],gg=grp[rr],ff=sem[rr];
                        if(MMRealisticTerrainBiomePass.BaseClassAt(z.key,gg,ff,yy)==exp)same++;
                        total++;
                    }
                    if(same<=7){status="MICROPATCH_SMOOTHED";micropatch++;}
                    else{status="REAL_FAIL";realFail++;}
                }
            }
            if(a.GetLength(2)!=7)extra++;
            rows.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),g.ToString(),f.ToString(),MMRealisticTerrainBiomePass.ClassName(exp),MMRealisticTerrainBiomePass.ClassName(dom),F(ew),
                F(a[ay,ax,0]),F(a[ay,ax,1]),F(a[ay,ax,2]),F(a[ay,ax,3]),F(a[ay,ax,4]),F(a[ay,ax,5]),F(a[ay,ax,6]),status}));
        }
        File.WriteAllLines($"Validation/SixTerrainAudit/{z.key}.csv",rows);
        summary.Add($"{z.key},{N*N},{count[0]},{count[1]},{count[2]},{count[3]},{count[4]},{count[5]},{count[6]},{lowSupport},{ecotone},{micropatch},{realFail},{extra}");
        Debug.Log($"SIX_TERRAIN {z.key} low={lowSupport} ecotone={ecotone} micropatch={micropatch} realFail={realFail} layers={td.alphamapLayers}");
    }
}
