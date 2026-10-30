using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
public static class MMEnrothOuterGridSeamRecovery {
    sealed class Cell {public Terrain terrain;public float[,] heights;public bool editable;}
    static Cell[,] grid;
    static float maxCorrection;
    const int N=513,BAND=64;
    static float World(Cell c,int z,int x) {
        return c.terrain.transform.position.y+c.heights[z,x]*c.terrain.terrainData.size.y;
    }
    static float Fade(int dist) {
        float t=Mathf.Clamp01(dist/(float)BAND);
        return 1f-t*t*(3f-2f*t);
    }
    static void Correct(Cell edit,int z,int x,float target,int axis,int dir,List<string> report,string tag) {
        float before=World(edit,z,x),delta=target-before;
        maxCorrection=Mathf.Max(maxCorrection,Mathf.Abs(delta));
        float dn=delta/edit.terrain.terrainData.size.y;
        for(int k=0;k<=BAND;k++){
            int zz=axis==0?z+dir*k:z,xx=axis==1?x+dir*k:x;
            edit.heights[zz,xx]+=dn*Fade(k);
        }
    }
    static void LockNorth(Cell edge,Cell canonical,List<string> report,string name) {
        float max=0f;
        for(int x=0;x<N;x++) {
            float target=World(canonical,N-1,x);
            max=Mathf.Max(max,Mathf.Abs(target-World(edge,0,x)));
            Correct(edge,0,x,target,0,1,report,name);
        }
        report.Add(name+",north_source,max_correction_m="+max.ToString("F6"));
    }
    static void LockSouth(Cell edge,Cell canonical,List<string> report,string name) {
        float max=0f;
        for(int x=0;x<N;x++) {
            float target=World(canonical,0,x);
            max=Mathf.Max(max,Mathf.Abs(target-World(edge,N-1,x)));
            Correct(edge,N-1,x,target,0,-1,report,name);
        }
        report.Add(name+",south_source,max_correction_m="+max.ToString("F6"));
    }
    static void LockWest(Cell edge,Cell canonical,List<string> report,string name) {
        float max=0f;
        for(int z=0;z<N;z++) {
            float target=World(canonical,z,0);
            max=Mathf.Max(max,Mathf.Abs(target-World(edge,z,N-1)));
            Correct(edge,z,N-1,target,1,-1,report,name);
        }
        report.Add(name+",west_source,max_correction_m="+max.ToString("F6"));
    }
    static void LockEast(Cell corner,Cell east,List<string> report,string name) {
        float max=0f;
        for(int z=0;z<N;z++) {
            float target=World(east,z,0);
            max=Mathf.Max(max,Mathf.Abs(target-World(corner,z,N-1)));
            Correct(corner,z,N-1,target,1,-1,report,name);
        }
        report.Add(name+",east_neighbor,max_correction_m="+max.ToString("F6"));
    }
    static void LockNorthNeighbor(Cell southwest,Cell north,List<string> report,string name){
        float max=0f;
        for(int x=0;x<N;x++){
            float target=World(north,0,x);
            max=Mathf.Max(max,Mathf.Abs(target-World(southwest,N-1,x)));
            Correct(southwest,N-1,x,target,0,-1,report,name);
        }
        report.Add(name+",north_neighbor,max_correction_m="+max.ToString("F6"));
    }
    static void LockSouthNeighbor(Cell northwest,Cell south,List<string> report,string name){
        float max=0f;
        for(int x=0;x<N;x++){
            float target=World(south,N-1,x);
            max=Mathf.Max(max,Mathf.Abs(target-World(northwest,0,x)));
            Correct(northwest,0,x,target,0,1,report,name);
        }
        report.Add(name+",south_neighbor,max_correction_m="+max.ToString("F6"));
    }
    [MenuItem("MMUnity/World/Lock 15 Outer Tiles to Canonical World")]
    public static void Run() {
        var sc=SceneManager.GetActiveScene();
        if(sc.path!="Assets/Scenes/World/Enroth.unity")
            throw new Exception("Open saved linked Enroth first, active="+sc.path);
        grid=new Cell[6,5];int count=0;maxCorrection=0f;
        foreach(var root in sc.GetRootGameObjects())
        foreach(var t in root.GetComponentsInChildren<Terrain>(true)){
            if(t.name.Contains("DragonIsle"))continue;
            int c=Mathf.RoundToInt((t.transform.position.x+t.terrainData.size.x*.5f)/512f)+3;
            int r=Mathf.RoundToInt((t.transform.position.z+t.terrainData.size.z*.5f)/512f)+2;
            if(c<0||c>5||r<0||r>4)throw new Exception("Unexpected terrain placement "+t.name);
            if(grid[c,r]!=null)throw new Exception("Duplicate grid terrain "+c+","+r);
            var td=t.terrainData;
            if(td.heightmapResolution!=N)throw new Exception("Unexpected heightmap "+t.name);
            bool editable=c==0||r==0||r==4;
            if(editable&&!AssetDatabase.GetAssetPath(td).Contains("WorldExtensions/Generated"))
                throw new Exception("Refusing to modify non-edge TerrainData "+t.name);
            grid[c,r]=new Cell{terrain=t,heights=td.GetHeights(0,0,N,N),editable=editable};
            count++;
        }
        if(count!=30)throw new Exception("Expected 30 outer-grid tiles, found "+count);
        var report=new List<string>{"edge,reference,max_correction"};
        for(int c=1;c<=5;c++)LockNorth(grid[c,4],grid[c,3],report,"North_"+(c-1));
        for(int r=1;r<=3;r++)LockWest(grid[0,r],grid[1,r],report,"West_"+(r-1));
        for(int c=1;c<=5;c++)LockSouth(grid[c,0],grid[c,1],report,"South_"+(c-1));
        LockEast(grid[0,4],grid[1,4],report,"NW_Ocean");
        LockSouthNeighbor(grid[0,4],grid[0,3],report,"NW_Ocean");
        LockEast(grid[0,0],grid[1,0],report,"SW_Ocean");
        LockNorthNeighbor(grid[0,0],grid[0,1],report,"SW_Ocean");
        int saved=0;
        for(int c=0;c<6;c++)for(int r=0;r<5;r++)if(grid[c,r].editable){
            var cell=grid[c,r];var td=cell.terrain.terrainData;
            td.SetHeights(0,0,cell.heights);EditorUtility.SetDirty(td);saved++;
        }
        if(saved!=15)throw new Exception("Unexpected modified count "+saved);
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        report.Add("SUMMARY,touched_only_edge_terrain="+saved+",peak_correction_m="+maxCorrection.ToString("F6"));
        File.WriteAllLines("Validation/EdgeGrid20260923/seam_repair_report.csv",report);
        AssetDatabase.SaveAssets();
        Debug.Log("EDGEGRID_CANONICAL_SEAMS_RELOCKED edges="+saved+" peakCorrection_m="+maxCorrection.ToString("F6"));
    }
}
