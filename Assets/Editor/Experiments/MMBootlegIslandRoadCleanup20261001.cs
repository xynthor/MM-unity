using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMBootlegIslandRoadCleanup20261001
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string TerrainPath="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/945f7afc4026cbb418809d32f2d282cd.asset";
    const string BackupManifest="Backups/BeforeBootlegIslandRoadCleanup_20261001/manifest.json";
    const string ExpectedSha="16cf6af5699aca2125c58b90802c5e327604e5ddc4bf072ecf49e493ff8d88e6";
    const string TilePath="Assets/World/BootlegBay/Data/tilemap_u8.bin";
    const string SemPath="Assets/World/BootlegBay/Data/tile_semantics_u8.bin";
    const int N=128;
    static byte[] sem;
    static bool Water(byte tile)=>(sem[tile]&1)!=0;
    static bool Shore(byte tile)=>(sem[tile]&2)!=0;
    static bool Road(byte tile)=>(sem[tile]&8)!=0;

    static string Sha(string p){
        using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();
    }
    static bool[] Mainland(byte[] tile){
        var comp=Enumerable.Repeat(-1,N*N).ToArray();var sizes=new List<int>();int cid=0;int[] dx={1,-1,0,0},dy={0,0,1,-1};
        for(int y=0;y<N;y++)for(int x=0;x<N;x++){int s=y*N+x;if(Water(tile[s])||comp[s]>=0)continue;var q=new Queue<int>();q.Enqueue(s);comp[s]=cid;int c=0;
            while(q.Count>0){int v=q.Dequeue();c++;int vx=v%N,vy=v/N;for(int k=0;k<4;k++){int nx=vx+dx[k],ny=vy+dy[k];if(nx<0||ny<0||nx>=N||ny>=N)continue;int ni=ny*N+nx;if(!Water(tile[ni])&&comp[ni]<0){comp[ni]=cid;q.Enqueue(ni);}}}sizes.Add(c);cid++;}
        int main=sizes.Count==0?-1:Enumerable.Range(0,sizes.Count).OrderByDescending(i=>sizes[i]).First();var m=new bool[N*N];for(int i=0;i<m.Length;i++)m[i]=comp[i]==main;return m;
    }
    static int FindLayer(TerrainLayer[] l,Func<TerrainLayer,bool> f,string label){
        int[] ids=Enumerable.Range(0,l.Length).Where(i=>l[i]&&f(l[i])).ToArray();if(ids.Length!=1)throw new Exception(label+" layer count="+ids.Length);return ids[0];
    }
    public static void Run(){
        if(!File.Exists(BackupManifest))throw new Exception("Backup manifest missing");
        if(Sha(TerrainPath)!=ExpectedSha)throw new Exception("Bootleg terrain baseline changed; inspect before applying");
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Bootleg Bay - LINKED REFERENCE");
        var t=root.GetComponentInChildren<Terrain>(true);var td=t.terrainData;
        if(AssetDatabase.GetAssetPath(td)!=TerrainPath||td.alphamapWidth!=512||td.alphamapHeight!=512||td.alphamapLayers!=7)throw new Exception("Unexpected Bootleg TerrainData");
        var l=td.terrainLayers;
        int road=FindLayer(l,x=>x.diffuseTexture&&AssetDatabase.GetAssetPath(x.diffuseTexture).IndexOf("stone_ground",StringComparison.OrdinalIgnoreCase)>=0,"road");
        int grass=FindLayer(l,x=>x.diffuseTexture&&AssetDatabase.GetAssetPath(x.diffuseTexture).IndexOf("ground_grass",StringComparison.OrdinalIgnoreCase)>=0,"grass");
        int green=FindLayer(l,x=>x.name=="Realistic_LightGreen","lightgreen");
        sem=File.ReadAllBytes(SemPath);var tiles=File.ReadAllBytes(TilePath);var mainland=Mainland(tiles);
        var a=td.GetAlphamaps(0,0,512,512);
        int touched=0,dominantBefore=0,dominantAfter=0;double moved=0,maxMainlandDelta=0,maxOtherDelta=0;
        // snapshot only mainland-road road alpha and non-island-road pixels for guards
        var roadBefore=new float[512,512];for(int y=0;y<512;y++)for(int x=0;x<512;x++)roadBefore[y,x]=a[y,x,road];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++){
            float sx=(x+.5f)/512f*N,sy=(512-y-.5f)/512f*N;int ix=Mathf.Clamp(Mathf.FloorToInt(sx),0,N-1),iy=Mathf.Clamp(Mathf.FloorToInt(sy),0,N-1);
            byte tile=tiles[iy*N+ix];if(!Road(tile)||mainland[iy*N+ix])continue;
            float r=a[y,x,road];if(r<=.00001f)continue;
            int best=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,best])best=k;if(best==road)dominantBefore++;
            float gShare=Shore(tile)?.62f:.88f;
            a[y,x,road]=0;a[y,x,grass]+=r*gShare;a[y,x,green]+=r*(1f-gShare);
            touched++;moved+=r;
            best=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,best])best=k;if(best==road)dominantAfter++;
        }
        // guards: mainland road and all pixels not belonging to island-road semantic cells retain road alpha exactly.
        for(int y=0;y<512;y++)for(int x=0;x<512;x++){
            float sx=(x+.5f)/512f*N,sy=(512-y-.5f)/512f*N;int ix=Mathf.Clamp(Mathf.FloorToInt(sx),0,N-1),iy=Mathf.Clamp(Mathf.FloorToInt(sy),0,N-1);
            byte tile=tiles[iy*N+ix];float d=Mathf.Abs(a[y,x,road]-roadBefore[y,x]);
            if(Road(tile)&&mainland[iy*N+ix])maxMainlandDelta=Math.Max(maxMainlandDelta,d);
            if(!(Road(tile)&&!mainland[iy*N+ix]))maxOtherDelta=Math.Max(maxOtherDelta,d);
        }
        if(touched<300||moved<150||dominantBefore<300||dominantAfter!=0||maxMainlandDelta>1e-7||maxOtherDelta>1e-7)throw new Exception($"Guard failed touched={touched} moved={moved} domBefore={dominantBefore} domAfter={dominantAfter} mainlandDelta={maxMainlandDelta} otherDelta={maxOtherDelta}");
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Validation/EdgeGrid20260923/BootlegBay20261001");
        string report=$"PASS islandRoadPixelsChanged={touched} removedRoadAlpha={moved:F3} dominantRoadBefore={dominantBefore} dominantRoadAfter={dominantAfter} mainlandRoadMaxDelta={maxMainlandDelta:F8} otherRoadMaxDelta={maxOtherDelta:F8} roadLayer={road} grassLayer={grass} lightGreenLayer={green} heightWrites=0 terrainLayerWrites=0 sceneWrites=0";
        File.WriteAllText("Validation/EdgeGrid20260923/BootlegBay20261001/island_road_cleanup.txt",report+"\n");Debug.Log(report);
    }
}