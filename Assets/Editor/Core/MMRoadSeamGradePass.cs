using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMRoadSeamGradePass
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity")};
    const int N=128;
    static bool Road(byte[] tile,byte[] grp,byte[] sem,int x,int y)
    {
        byte raw=tile[y*N+x],f=sem[raw],g=grp[raw];
        return (f&8)!=0||(g>=8&&g<255);
    }
    static bool NearEdge(int x,int y)=>x<=5||x>=N-6||y<=5||y>=N-6;
    static int HX(TerrainData td,int sx)=>Mathf.Clamp(Mathf.RoundToInt(((sx+.5f)*4f)/td.size.x*(td.heightmapResolution-1)),0,td.heightmapResolution-1);
    static int HZ(TerrainData td,int sy)=>Mathf.Clamp(Mathf.RoundToInt(((N-sy-.5f)*4f)/td.size.z*(td.heightmapResolution-1)),0,td.heightmapResolution-1);
    static float WorldH(float[,] hm,Terrain t,int sx,int sy)
    {
        int ix=HX(t.terrainData,sx),iz=HZ(t.terrainData,sy);
        return t.transform.position.y+hm[iz,ix]*t.terrainData.size.y;
    }
    static bool BorderCell(int sx,int sy)=>sx==0||sx==N-1||sy==0||sy==N-1;
    static void Nudge(float[,] hm,Terrain t,int sx,int sy,float target)
    {
        int cx=HX(t.terrainData,sx),cz=HZ(t.terrainData,sy),r=2;
        float by=t.transform.position.y,sySize=t.terrainData.size.y;
        for(int dz=-r;dz<=r;dz++)for(int dx=-r;dx<=r;dx++)
        {
            int x=cx+dx,z=cz+dz;
            if(x<=0||z<=0||x>=t.terrainData.heightmapResolution-1||z>=t.terrainData.heightmapResolution-1)continue;
            float d=Mathf.Sqrt(dx*dx+dz*dz)/r;
            if(d>1f)continue;
            float w=1f-Mathf.SmoothStep(0f,1f,d);
            float cur=by+hm[z,x]*sySize,ny=Mathf.Lerp(cur,target,w);
            hm[z,x]=Mathf.Clamp01((ny-by)/sySize);
        }
    }
    [MenuItem("MMUnity/Fix Seam Road Grades")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory("Validation");
        var log=new List<string>{"zone,sx,sy,nx,ny,before,source,after,adjustedCell"};
        int total=0;
        foreach(var z in Zones)total+=Apply(z,log);
        File.WriteAllLines("Validation/RoadSeamGradeFix.csv",log);
        AssetDatabase.SaveAssets();
        Debug.Log("ROAD_SEAM_GRADE_DONE fixes="+total);
    }
    static int Apply(Z z,List<string> log)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);
        if(!t)return 0;
        string d=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(d+"/tilemap_u8.bin");
        var grp=File.ReadAllBytes(d+"/tile_groups_u8.bin");
        var sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var src=File.ReadAllBytes(d+"/heightmap_u8.bin");
        var hm=t.terrainData.GetHeights(0,0,t.terrainData.heightmapResolution,t.terrainData.heightmapResolution);
        int fixes=0;
        int[,] step={{1,0},{0,1}};
        for(int iter=0;iter<6;iter++)
        {
            bool changed=false;
            for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)if(Road(tile,grp,sem,sx,sy))
            for(int k=0;k<2;k++)
            {
                int nx=sx+step[k,0],ny=sy+step[k,1];
                if(nx>=N||ny>=N||!Road(tile,grp,sem,nx,ny))continue;
                float a=WorldH(hm,t,sx,sy),b=WorldH(hm,t,nx,ny);
                float sd=Mathf.Abs(src[sy*N+sx]-src[ny*N+nx])*.25f;
                float allowed=Mathf.Min(1.75f,Mathf.Max(1.20f,sd+.35f)),before=Mathf.Abs(a-b);
                if(before<=allowed+.04f)continue;
                float da=Mathf.Abs(a-src[sy*N+sx]*.25f),db=Mathf.Abs(b-src[ny*N+nx]*.25f);
                bool adjustA=da>=db;
                if(BorderCell(sx,sy)&&!BorderCell(nx,ny))adjustA=false;
                if(BorderCell(nx,ny)&&!BorderCell(sx,sy))adjustA=true;
                int ax=adjustA?sx:nx,ay=adjustA?sy:ny;
                float other=adjustA?b:a,cur=adjustA?a:b;
                float target=cur>other?other+allowed:other-allowed;
                Nudge(hm,t,ax,ay,target);
                float after=Mathf.Abs(WorldH(hm,t,sx,sy)-WorldH(hm,t,nx,ny));
                log.Add($"{z.key},{sx},{sy},{nx},{ny},{before:F3},{sd:F3},{after:F3},{ax}:{ay}");
                fixes++;changed=true;
            }
            if(!changed)break;
        }
        if(fixes>0)
        {
            t.terrainData.SetHeights(0,0,hm);t.Flush();
            EditorUtility.SetDirty(t.terrainData);
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc,z.scene);
        }
        return fixes;
    }
}
