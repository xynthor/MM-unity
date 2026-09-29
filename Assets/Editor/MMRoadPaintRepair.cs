using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMRoadPaintRepair
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Z("Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Z("HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Z("MistyIslands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Z("BootlegBay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Z("FreeHaven","Assets/Scenes/FreeHaven_SourceGrid.unity"),
        new Z("Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Z("SilverCove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity")};
    const int N=128;

    static bool Road(byte[] tile,byte[] grp,byte[] sem,int sx,int sy)
    {
        byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
        return (f&8)!=0||(g>=8&&g<255);
    }

    static int RoadLayer(TerrainData td)
    {
        for(int i=0;i<td.terrainLayers.Length;i++)
            if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf("road",StringComparison.OrdinalIgnoreCase)>=0)return i;
        return -1;
    }

    static void SetRoad(float[,,] a,int y,int x,int road,float target)
    {
        int L=a.GetLength(2);float other=0f;
        for(int k=0;k<L;k++)if(k!=road)other+=a[y,x,k];
        if(other<.0001f)
        {
            int baseLayer=road==0?1:0;
            for(int k=0;k<L;k++)a[y,x,k]=0f;
            a[y,x,baseLayer]=1f-target;a[y,x,road]=target;return;
        }
        float scale=(1f-target)/other;
        for(int k=0;k<L;k++)if(k!=road)a[y,x,k]*=scale;
        a[y,x,road]=target;
    }    static int Apply(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var t=root?root.GetComponentInChildren<Terrain>(true):null;if(!t)return 0;
        string d="Assets/World/"+z.key+"/Data";
        byte[] tile=File.ReadAllBytes(d+"/tilemap_u8.bin"),grp=File.ReadAllBytes(d+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var td=t.terrainData;int road=RoadLayer(td);if(road<0)return 0;
        int w=td.alphamapWidth,h=td.alphamapHeight;var a=td.GetAlphamaps(0,0,w,h);int changed=0;
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)
        {
            if(!Road(tile,grp,sem,sx,sy))continue;
            float wx0=(sx-64f)*4f,wx1=(sx+1-64f)*4f;
            float wz0=(64f-(sy+1))*4f,wz1=(64f-sy)*4f;
            int ax0=Mathf.Clamp(Mathf.FloorToInt((wx0-t.transform.position.x)/td.size.x*w),0,w-1);
            int ax1=Mathf.Clamp(Mathf.CeilToInt((wx1-t.transform.position.x)/td.size.x*w)-1,0,w-1);
            int ay0=Mathf.Clamp(Mathf.FloorToInt((wz0-t.transform.position.z)/td.size.z*h),0,h-1);
            int ay1=Mathf.Clamp(Mathf.CeilToInt((wz1-t.transform.position.z)/td.size.z*h)-1,0,h-1);
            for(int ay=ay0;ay<=ay1;ay++)for(int ax=ax0;ax<=ax1;ax++){SetRoad(a,ay,ax,road,.68f);changed++;}
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);t.Flush();
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        return changed;
    }

    [MenuItem("MMUnity/Environment/Repair Source Road Paint")]
    public static void Run()
    {
        int changed=0;foreach(var z in Zones)changed+=Apply(z);
        AssetDatabase.SaveAssets();
        Debug.Log("MM_ROAD_PAINT_REPAIR_DONE pixels="+changed);
    }
}