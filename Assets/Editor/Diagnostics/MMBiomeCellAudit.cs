using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMBiomeCellAudit
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity")};
    const int N=128;
    [MenuItem("MMUnity/Audit/Square-by-Square Biomes")]
    public static void Run()
    {
        string dir="Validation/BiomeCellAudit";Directory.CreateDirectory(dir);
        var summary=new List<string>{"zone,cells,ok,mismatch,layerMissing,roads,water,green,snow,sand,volcanic,dryAsh,mud,marsh,rock"};
        foreach(var z in Zones)Audit(z,dir,summary);
        File.WriteAllLines(Path.Combine(dir,"summary.csv"),summary);
        Debug.Log("BIOME_CELL_AUDIT_DONE zones=15 cells="+(15*N*N));
    }

    static void Audit(Z z,string dir,List<string> summary)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        string d=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(d+"/tilemap_u8.bin");var grp=File.ReadAllBytes(d+"/tile_groups_u8.bin");var sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var td=t.terrainData;var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
        var rows=new List<string>(N*N+1){"sx,sy,tile,group,semantics,expectedBiome,actualBiome,expectedLayer,actualLayer,actualWeight,road,water,status"};
        int ok=0,bad=0,missing=0,roads=0,water=0;int[] cnt=new int[9];
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)
        {
            byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
            int exp=MMWitcherTerrainPass.ExpectedLayerForCell(z.key,tile,grp,sem,sx,sy);
            int ax=Mathf.Clamp(sx*td.alphamapWidth/N+td.alphamapWidth/(N*2),0,td.alphamapWidth-1);
            int ay=Mathf.Clamp((N-1-sy)*td.alphamapHeight/N+td.alphamapHeight/(N*2),0,td.alphamapHeight-1);
            int act=0;float best=-1f;for(int k=0;k<a.GetLength(2);k++)if(a[ay,ax,k]>best){best=a[ay,ax,k];act=k;}
            bool lm=exp<0||exp>=a.GetLength(2);string status=lm?"LAYER_MISSING":act==exp?"OK":"MISMATCH";
            if(status=="OK")ok++;else {bad++;if(lm)missing++;}
            bool rd=(f&8)!=0||(g>=8&&g<255),wa=(f&1)!=0;if(rd)roads++;if(wa)water++;if(exp>=0&&exp<cnt.Length)cnt[exp]++;
            rows.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),raw.ToString(),g.ToString(),f.ToString(),
                MMWitcherTerrainPass.BiomeName(exp),MMWitcherTerrainPass.BiomeName(act),exp.ToString(),act.ToString(),
                best.ToString("F4",CultureInfo.InvariantCulture),rd?"1":"0",wa?"1":"0",status}));
        }
        File.WriteAllLines(Path.Combine(dir,z.key+".csv"),rows);
        summary.Add(string.Join(",",new[]{z.key,(N*N).ToString(),ok.ToString(),bad.ToString(),missing.ToString(),roads.ToString(),water.ToString(),
            cnt[0].ToString(),cnt[1].ToString(),cnt[2].ToString(),cnt[3].ToString(),cnt[4].ToString(),cnt[5].ToString(),cnt[6].ToString(),cnt[8].ToString()}));
        Debug.Log($"BIOME_CELL {z.key} ok={ok} mismatch={bad} layerMissing={missing}");
    }
}
