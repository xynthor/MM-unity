using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMBootlegBayCurrentAudit20261001
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string TilePath="Assets/World/BootlegBay/Data/tilemap_u8.bin";
    const string SemPath="Assets/World/BootlegBay/Data/tile_semantics_u8.bin";
    const int N=128;

    static byte[] sem;
    static bool Water(byte tile)=>(sem[tile]&1)!=0;
    static bool Road(byte tile)=>(sem[tile]&8)!=0;

    static bool[] Mainland(byte[] tile)
    {
        var comp=Enumerable.Repeat(-1,N*N).ToArray();var sizes=new List<int>();int cid=0;
        int[] dx={1,-1,0,0},dy={0,0,1,-1};
        for(int y=0;y<N;y++)for(int x=0;x<N;x++){
            int s=y*N+x;if(Water(tile[s])||comp[s]>=0)continue;
            var q=new Queue<int>();q.Enqueue(s);comp[s]=cid;int c=0;
            while(q.Count>0){int v=q.Dequeue();c++;int vx=v%N,vy=v/N;for(int k=0;k<4;k++){int nx=vx+dx[k],ny=vy+dy[k];if(nx<0||ny<0||nx>=N||ny>=N)continue;int ni=ny*N+nx;if(!Water(tile[ni])&&comp[ni]<0){comp[ni]=cid;q.Enqueue(ni);}}}
            sizes.Add(c);cid++;
        }
        int main=sizes.Count==0?-1:Enumerable.Range(0,sizes.Count).OrderByDescending(i=>sizes[i]).First();
        var m=new bool[N*N];for(int i=0;i<m.Length;i++)m[i]=comp[i]==main;return m;
    }

    public static void Run()
    {
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .Single(t=>t.name=="Bootleg Bay - LINKED REFERENCE");
        var t=root.GetComponentInChildren<Terrain>(true);var td=t.terrainData;
        sem=File.ReadAllBytes(SemPath);var tiles=File.ReadAllBytes(TilePath);var mainland=Mainland(tiles);
        var layers=td.terrainLayers;var roadIds=Enumerable.Range(0,layers.Length)
            .Where(i=>layers[i]&&(layers[i].name.IndexOf("Road",StringComparison.OrdinalIgnoreCase)>=0||(layers[i].diffuseTexture&&AssetDatabase.GetAssetPath(layers[i].diffuseTexture).IndexOf("stone_ground",StringComparison.OrdinalIgnoreCase)>=0))).ToArray();
        var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
        long islandRoadPx=0, mainlandRoadPx=0;double islandRoadW=0,mainRoadW=0;int islandDomRoad=0,mainDomRoad=0;
        var img=new Texture2D(td.alphamapWidth,td.alphamapHeight,TextureFormat.RGBA32,false,true);
        var pix=new Color32[td.alphamapWidth*td.alphamapHeight];
        for(int y=0;y<td.alphamapHeight;y++)for(int x=0;x<td.alphamapWidth;x++){
            float sx=(x+.5f)/td.alphamapWidth*N, sy=(td.alphamapHeight-y-.5f)/td.alphamapHeight*N;
            int ix=Mathf.Clamp(Mathf.FloorToInt(sx),0,N-1),iy=Mathf.Clamp(Mathf.FloorToInt(sy),0,N-1);
            byte tile=tiles[iy*N+ix];bool road=Road(tile),main=mainland[iy*N+ix];
            float rw=roadIds.Sum(i=>a[y,x,i]);int best=0;for(int k=1;k<td.alphamapLayers;k++)if(a[y,x,k]>a[y,x,best])best=k;
            bool dom=roadIds.Contains(best);
            Color32 c=new Color32(30,45,35,255);
            if(road&&main){mainlandRoadPx++;mainRoadW+=rw;if(dom)mainDomRoad++;c=dom?new Color32(80,220,80,255):new Color32(40,100,40,255);}
            else if(road&&!main){islandRoadPx++;islandRoadW+=rw;if(dom)islandDomRoad++;c=dom?new Color32(250,40,40,255):new Color32(220,150,30,255);}
            else if(Water(tile))c=new Color32(40,90,180,255);
            pix[y*td.alphamapWidth+x]=c;
        }
        img.SetPixels32(pix);img.Apply();Directory.CreateDirectory("Validation/EdgeGrid20260923/BootlegBay20261001");
        File.WriteAllBytes("Validation/EdgeGrid20260923/BootlegBay20261001/road_island_audit.png",img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);
        var rows=new List<string>();
        rows.Add("terrain="+td.name+" asset="+AssetDatabase.GetAssetPath(td)+" alpha="+td.alphamapWidth+"x"+td.alphamapHeight+" layers="+td.alphamapLayers);
        rows.Add("layers="+string.Join(" | ",layers.Select((l,i)=>i+":"+(l?l.name:"NULL"))));
        rows.Add("roadLayerIds="+string.Join(",",roadIds));
        rows.Add($"islandRoadPixels={islandRoadPx} islandMeanRoadWeight={(islandRoadPx>0?islandRoadW/islandRoadPx:0):F4} islandDominantRoad={islandDomRoad}");
        rows.Add($"mainlandRoadPixels={mainlandRoadPx} mainlandMeanRoadWeight={(mainlandRoadPx>0?mainRoadW/mainlandRoadPx:0):F4} mainlandDominantRoad={mainDomRoad}");
        File.WriteAllLines("Validation/EdgeGrid20260923/BootlegBay20261001/current_audit.txt",rows);
        Debug.Log(string.Join("\n",rows));
    }
}