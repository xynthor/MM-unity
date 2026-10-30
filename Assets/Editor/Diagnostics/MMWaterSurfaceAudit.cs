using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWaterSurfaceAudit
{
    const int N=128; const float WaterY=.10f;
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

    static bool SourceWater(byte[] tile,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.FloorToInt(x/4f+64f),0,N-1);
        int sy=Mathf.Clamp(Mathf.FloorToInt(64f-z/4f),0,N-1);
        byte f=sem[tile[sy*N+sx]];
        return (f&1)!=0||(f&2)!=0;
    }

    [MenuItem("MMUnity/Locked/Audit Water Surface _F9")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/WaterSurfaceAudit");
        var sum=new List<string>{"zone,triangles,dry_centroids,above_land_centroids,material_mismatch,status"};
        foreach(var z in Zones)Audit(z,sum);
        File.WriteAllLines("Validation/WaterSurfaceAudit/summary.csv",sum);
        Debug.Log("WATER_SURFACE_AUDIT_DONE");
    }

    static void Audit(Z z,List<string> sum)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var roots=sc.GetRootGameObjects();
        var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        var water=roots.SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))
            .FirstOrDefault(m=>m.name.IndexOf("Internal Water - Smooth",StringComparison.OrdinalIgnoreCase)>=0);
        if(!terrain||!water||!water.sharedMesh){sum.Add($"{z.key},0,0,0,1,MISSING");return;}

        string dd=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(dd+"/tilemap_u8.bin");var sem=File.ReadAllBytes(dd+"/tile_semantics_u8.bin");
        var mesh=water.sharedMesh;var v=mesh.vertices;var tr=mesh.triangles;
        int dry=0,above=0;
        var rows=new List<string>{"tri,cx,cz,groundY,sourceWater,dry,aboveLand"};
        for(int i=0;i<tr.Length;i+=3)
        {
            Vector3 a=water.transform.TransformPoint(v[tr[i]]),b=water.transform.TransformPoint(v[tr[i+1]]),c=water.transform.TransformPoint(v[tr[i+2]]);
            Vector3 p=(a+b+c)/3f;
            bool sw=SourceWater(tile,sem,p.x,p.z);
            float gy=terrain.SampleHeight(new Vector3(p.x,0,p.z))+terrain.transform.position.y;
            bool d=!sw; bool al=gy>WaterY+.081f;
            if(d)dry++;if(al)above++;
            if(d||al)rows.Add($"{i/3},{p.x:F3},{p.z:F3},{gy:F3},{(sw?1:0)},{(d?1:0)},{(al?1:0)}");
        }
        var mr=water.GetComponent<MeshRenderer>();
        var expected=MMUnifiedWorldWaterPass.SharedWaterMaterial();
        int mm=(mr&&mr.sharedMaterial==expected)?0:1;
        File.WriteAllLines($"Validation/WaterSurfaceAudit/{z.key}.csv",rows);
        string st=(dry==0&&above==0&&mm==0)?"PASS":"FAIL";
        sum.Add($"{z.key},{tr.Length/3},{dry},{above},{mm},{st}");
        Debug.Log($"WATER_SURFACE_AUDIT {z.key} dry={dry} above={above} materialMismatch={mm}");
    }
}