using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMDragonIsleReferencePolish20261006
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string ReportPath="Validation/EdgeGrid20260923/DragonReference20261006/dragon_polish.txt";

    static float Smooth(float a,float b,float v){
        float t=Mathf.Clamp01((v-a)/Mathf.Max(.001f,b-a));
        return t*t*(3f-2f*t);
    }

    static int FindLayer(TerrainData d,string token){
        for(int i=0;i<d.terrainLayers.Length;i++){
            var l=d.terrainLayers[i];
            if(l && l.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0) return i;
        }
        return -1;
    }

    static int PaintFeature(Terrain t, Vector2 center, Vector2 radius, float strength){
        var d=t.terrainData;
        var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
        int sand=1;
        int rock=FindLayer(d,"Trace_Rock");
        int ash=FindLayer(d,"_5");
        int arid=FindLayer(d,"Realistic_Arid");
        if(arid<0) arid=FindLayer(d,"_2");
        if(rock<0||ash<0||arid<0||sand>=d.alphamapLayers)
            throw new Exception("Dragon expected terrain layers missing on "+t.name);

        int changed=0;
        for(int z=0;z<d.alphamapHeight;z++) for(int x=0;x<d.alphamapWidth;x++){
            float dx=(x-center.x)/radius.x;
            float dz=(z-center.y)/radius.y;
            float r=Mathf.Sqrt(dx*dx+dz*dz);
            if(r>=1.35f) continue;

            float u=x/(float)(d.alphamapWidth-1);
            float v=z/(float)(d.alphamapHeight-1);
            float y=t.transform.position.y+d.GetInterpolatedHeight(u,v);
            if(y<.5f) continue;

            float shape=1f-Smooth(.72f,1.35f,r);
            float slope=d.GetSteepness(u,v);
            float rugged=Mathf.Lerp(.72f,1f,Smooth(8f,26f,slope));
            float w=strength*shape*rugged;
            float available=a[z,x,sand];
            float move=Mathf.Min(available, available*w);
            if(move<.002f) continue;

            a[z,x,sand]-=move;
            a[z,x,rock]+=move*.58f;
            a[z,x,ash]+=move*.31f;
            a[z,x,arid]+=move*.11f;
            changed++;
        }
        d.SetAlphamaps(0,0,a);
        d.SetBaseMapDirty();
        EditorUtility.SetDirty(d);
        t.Flush();
        return changed;
    }

    [MenuItem("MMUnity/Reference 2026/Polish Dragon Isle From New Reference")]
    public static void Run(){
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        if(sc.isDirty) throw new Exception("Dragon polish requires saved Enroth scene");

        var ts=Terrain.activeTerrains
            .Where(t=>Mathf.Abs(t.transform.position.x+1792f)<1f &&
                (Mathf.Abs(t.transform.position.z-768f)<1f || Mathf.Abs(t.transform.position.z-256f)<1f))
            .OrderByDescending(t=>t.transform.position.z).ToArray();

        if(ts.Length!=2) throw new Exception("Expected exactly two Dragon Isle linked terrain halves, found "+ts.Length);

        var north=ts.First(t=>Mathf.Abs(t.transform.position.z-768f)<1f);
        var south=ts.First(t=>Mathf.Abs(t.transform.position.z-256f)<1f);

        int northFeature=PaintFeature(north,new Vector2(222f,150f),new Vector2(92f,78f),.78f);
        int southFeature=PaintFeature(south,new Vector2(250f,225f),new Vector2(70f,118f),.88f);

        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllLines(ReportPath,new[]{
            "PASS Dragon Isle new-reference localized polish",
            "coastline_height_changes=0",
            "north_volcanic_pixels="+northFeature,
            "south_signature_ridge_pixels="+southFeature,
            "scene="+ScenePath,
            "northTerrain="+AssetDatabase.GetAssetPath(north.terrainData),
            "southTerrain="+AssetDatabase.GetAssetPath(south.terrainData)
        });
        Debug.Log("DRAGON_ISLE_REFERENCE_POLISH_20261006_DONE north="+northFeature+" south="+southFeature);
    }
}
