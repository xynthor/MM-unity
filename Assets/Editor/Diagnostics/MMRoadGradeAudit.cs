using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMRoadGradeAudit
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
    static bool Road(byte[] tile,byte[] grp,byte[] sem,int x,int y)
    {
        byte raw=tile[y*128+x],f=sem[raw],g=grp[raw];
        return (f&8)!=0||(g>=8&&g<255);
    }
    [MenuItem("MMUnity/Audit Road Grades")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation");
        var o=new List<string>{"zone,sx,sy,nx,ny,currentDelta,sourceDelta,currentH,neighborH,sourceH,sourceNeighborH"};
        foreach(var z in Zones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
            var t=root.GetComponentInChildren<Terrain>(true); string d=$"Assets/World/{z.key}/Data";
            var tile=File.ReadAllBytes(d+"/tilemap_u8.bin");var grp=File.ReadAllBytes(d+"/tile_groups_u8.bin");var sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");var h=File.ReadAllBytes(d+"/heightmap_u8.bin");
            int[,] q={{1,0},{-1,0},{0,1},{0,-1}};
            for(int sy=0;sy<128;sy++)for(int sx=0;sx<128;sx++)if(Road(tile,grp,sem,sx,sy))
            {
                float wx=(sx+.5f-64f)*4f,wz=(64f-(sy+.5f))*4f;
                float ch=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;
                for(int k=0;k<4;k++)
                {
                    int nx=sx+q[k,0],ny=sy+q[k,1];if(nx<0||ny<0||nx>=128||ny>=128||!Road(tile,grp,sem,nx,ny))continue;
                    if(ny<sy||(ny==sy&&nx<sx))continue;
                    float nwx=(nx+.5f-64f)*4f,nwz=(64f-(ny+.5f))*4f;
                    float nh=t.SampleHeight(new Vector3(nwx,0,nwz))+t.transform.position.y;
                    float sd=Mathf.Abs(h[sy*128+sx]-h[ny*128+nx])*.25f,cd=Mathf.Abs(ch-nh);
                    if(cd>1.75f)o.Add($"{z.key},{sx},{sy},{nx},{ny},{cd:F3},{sd:F3},{ch:F3},{nh:F3},{h[sy*128+sx]*.25f:F3},{h[ny*128+nx]*.25f:F3}");
                }
            }
        }
        File.WriteAllLines("Validation/RoadGradeAudit.csv",o);
        Debug.Log("ROAD_GRADE_AUDIT_DONE rows="+(o.Count-1));
    }
}