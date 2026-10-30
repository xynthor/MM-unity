using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class MMTerrainTransitionPolish
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
    static bool Road(byte[] tile,byte[] grp,byte[] sem,int sx,int sy)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);
        byte raw=tile[sy*N+sx],f=sem[raw],g=grp[raw];
        return (f&8)!=0||(g>=8&&g<255);
    }
    static int RoadLayer(TerrainData td)
    {
        for(int i=0;i<td.terrainLayers.Length;i++)
            if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf("road",StringComparison.OrdinalIgnoreCase)>=0)return i;
        return td.alphamapLayers>2?2:-1;
    }
    static float L1(float[,,] a,int y0,int x0,int y1,int x1,int l)
    {
        float d=0f;for(int k=0;k<l;k++)d+=Mathf.Abs(a[y0,x0,k]-a[y1,x1,k]);return d;
    }
    static void Normalize(float[] w)
    {
        float s=0f;for(int i=0;i<w.Length;i++){w[i]=Mathf.Max(0f,w[i]);s+=w[i];}
        if(s<.0001f){w[0]=1f;return;}for(int i=0;i<w.Length;i++)w[i]/=s;
    }
    [MenuItem("MMUnity/Polish Castle Ironfist Terrain Transitions")]
    public static void ApplyCastleIronfist()
    {
        var z=Zones.First(x=>x.key=="CastleIronfist");
        int changed=Apply(z);
        AssetDatabase.SaveAssets();
        Debug.Log("TERRAIN_TRANSITION_CASTLE_DONE changed="+changed);
    }

    [MenuItem("MMUnity/Polish Source Terrain Transitions")]
    public static void ApplyAll()
    {
        int changed=0;foreach(var z in Zones)changed+=Apply(z);
        AssetDatabase.SaveAssets();
        Debug.Log("TERRAIN_TRANSITION_POLISH_DONE changed="+changed+" sourceBiomesPreserved=true");
    }
    static int Apply(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);if(!t)return 0;
        string d=$"Assets/World/{z.key}/Data";
        byte[] tile=File.ReadAllBytes(d+"/tilemap_u8.bin"),grp=File.ReadAllBytes(d+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var td=t.terrainData;int r=td.alphamapResolution,l=td.alphamapLayers,road=RoadLayer(td);
        var original=td.GetAlphamaps(0,0,r,r);var cur=(float[,,])original.Clone();int changed=0;
        for(int iter=0;iter<3;iter++)
        {
            var next=(float[,,])cur.Clone();float blend=iter==0?.34f:(iter==1?.27f:.20f);
            for(int y=1;y<r-1;y++)for(int x=1;x<r-1;x++)
            {
                float jump=Mathf.Max(Mathf.Max(L1(cur,y,x,y,x-1,l),L1(cur,y,x,y,x+1,l)),Mathf.Max(L1(cur,y,x,y-1,x,l),L1(cur,y,x,y+1,x,l)));
                if(jump<1.15f)continue;
                var w=new float[l];for(int k=0;k<l;k++){float avg=(cur[y,x-1,k]+cur[y,x+1,k]+cur[y-1,x,k]+cur[y+1,x,k])*.25f;w[k]=Mathf.Lerp(cur[y,x,k],avg,blend);}
                int dominant=0;for(int k=1;k<l;k++)if(original[y,x,k]>original[y,x,dominant])dominant=k;
                float sx=(x+.5f)/r*N-.5f,sy=N-1-((y+.5f)/r*N-.5f);bool isRoad=Road(tile,grp,sem,Mathf.RoundToInt(sx),Mathf.RoundToInt(sy));
                Normalize(w);
                if(road>=0)
                {
                    if(isRoad){float keep=.86f,rest=Mathf.Max(.0001f,1f-w[road]);for(int k=0;k<l;k++)if(k!=road)w[k]=w[k]/rest*(1f-keep);w[road]=keep;}
                    else if(w[road]>.12f){float cut=w[road]-.12f;w[road]=.12f;w[dominant]+=cut;}
                }
                if(!isRoad&&w[dominant]<.48f){float need=.48f-w[dominant],other=Mathf.Max(.0001f,1f-w[dominant]);for(int k=0;k<l;k++)if(k!=dominant)w[k]*=Mathf.Max(0f,(other-need)/other);w[dominant]=.48f;}
                Normalize(w);for(int k=0;k<l;k++)next[y,x,k]=w[k];changed++;
            }
            cur=next;
        }
        td.SetAlphamaps(0,0,cur);EditorUtility.SetDirty(td);
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        return changed;
    }
}
