using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public static class MMBlackshireTransitionRepair
{
    const string ScenePath="Assets/Scenes/Regions/Blackshire.unity";
    const string Report="Validation/EnvironmentRealism/blackshire_transition_repair.txt";
    const int N=128;

    [MenuItem("MMUnity/Environment/Repair Blackshire Biome Transitions")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var t=root?root.GetComponentInChildren<Terrain>(true):null;
        if(!t)throw new Exception("Blackshire terrain missing");
        var td=t.terrainData;
        string heightPre=HeightHash(td);

        string dd="Assets/World/Blackshire/Data";
        byte[] tile=File.ReadAllBytes(dd+"/tilemap_u8.bin");
        byte[] grp=File.ReadAllBytes(dd+"/tile_groups_u8.bin");
        byte[] sem=File.ReadAllBytes(dd+"/tile_semantics_u8.bin");

        int w=td.alphamapWidth,h=td.alphamapHeight,l=td.alphamapLayers;
        var a=td.GetAlphamaps(0,0,w,h);
        int pre=HardCount(a);
        int changed=0,iterations=0;
        var cur=a;
        while(iterations<3)
        {
            int before=HardCount(cur);
            var next=(float[,,])cur.Clone();
            int thisChanged=0;
            for(int y=1;y<h-1;y++)for(int x=1;x<w-1;x++)
            {
                if(Protected(x,y,w,h,t,tile,grp,sem))continue;
                float edge=LocalEdge(cur,x,y,l);
                if(edge<.90f)continue;
                float[] avg=new float[l];float ws=0f;
                for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
                {
                    int nx=x+ox,ny=y+oy;
                    if(Protected(nx,ny,w,h,t,tile,grp,sem))continue;
                    float wt=(ox==0&&oy==0)?2.2f:((ox==0||oy==0)?1f:.65f);
                    for(int k=0;k<l;k++)avg[k]+=cur[ny,nx,k]*wt;
                    ws+=wt;
                }
                if(ws<=0)continue;
                float factor=Mathf.Lerp(.18f,.38f,Mathf.InverseLerp(.9f,2f,edge));
                float total=0f;
                for(int k=0;k<l;k++){next[y,x,k]=Mathf.Lerp(cur[y,x,k],avg[k]/ws,factor);total+=next[y,x,k];}
                if(total>1e-6f)for(int k=0;k<l;k++)next[y,x,k]/=total;
                thisChanged++;
            }
            int after=HardCount(next);
            if(after>=before && iterations>0)break;
            cur=next;changed+=thisChanged;iterations++;
            if(after<=45)break;
        }
        int post=HardCount(cur);
        if(post>=pre)throw new Exception("Transition smoothing did not improve: "+pre+" -> "+post);

        td.SetAlphamaps(0,0,cur);
        EditorUtility.SetDirty(td);
        string heightPost=HeightHash(td);
        if(heightPre!=heightPost)throw new Exception("Height data changed unexpectedly");
        EditorSceneManager.MarkSceneDirty(sc);
        EditorSceneManager.SaveScene(sc);
        AssetDatabase.SaveAssets();

        File.WriteAllText(Report,
            "scene="+ScenePath+"\n"+
            "layers="+l+"\n"+
            "layer_names="+string.Join("|",td.terrainLayers.Select(x=>x?x.name:"null"))+"\n"+
            "hard_transitions_pre="+pre+"\n"+
            "hard_transitions_post="+post+"\n"+
            "iterations="+iterations+"\n"+
            "changed_alpha_pixels="+changed+"\n"+
            "height_hash_pre="+heightPre+"\n"+
            "height_hash_post="+heightPost+"\n"+
            "height_unchanged="+(heightPre==heightPost)+"\n");
        Debug.Log($"MM_BLACK_TRANSITIONS_DONE pre={pre} post={post} layers={l} changed={changed} heightsSame={heightPre==heightPost}");
    }

    static float LocalEdge(float[,,] a,int x,int y,int l)
    {
        float max=0f;
        int[,] q={{1,0},{-1,0},{0,1},{0,-1}};
        for(int n=0;n<4;n++)
        {
            float d=0;int nx=x+q[n,0],ny=y+q[n,1];
            for(int k=0;k<l;k++)d+=Mathf.Abs(a[y,x,k]-a[ny,nx,k]);
            if(d>max)max=d;
        }
        return max;
    }

    static int HardCount(float[,,] a)
    {
        int h=a.GetLength(0),w=a.GetLength(1),l=a.GetLength(2),n=0;
        for(int y=0;y<h-1;y+=2)for(int x=0;x<w-1;x+=2)
        {
            float dx=0,dz=0;
            for(int k=0;k<l;k++){dx+=Mathf.Abs(a[y,x,k]-a[y,x+1,k]);dz+=Mathf.Abs(a[y,x,k]-a[y+1,x,k]);}
            if(dx>1.55f)n++;if(dz>1.55f)n++;
        }
        return n;
    }

    static bool Protected(int ax,int ay,int w,int h,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        float wx=t.transform.position.x+(ax+.5f)/w*t.terrainData.size.x;
        float wz=t.transform.position.z+(ay+.5f)/h*t.terrainData.size.z;
        int sx=Mathf.Clamp(Mathf.RoundToInt(wx/4f+64f),0,N-1);
        int sy=Mathf.Clamp(Mathf.RoundToInt(64f-wz/4f),0,N-1);
        byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
        return (f&1)!=0||(f&8)!=0||(g>=8&&g<255);
    }

    static string HeightHash(TerrainData td)
    {
        int r=td.heightmapResolution;var h=td.GetHeights(0,0,r,r);
        var sb=new StringBuilder(r*r*8);
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)sb.Append(h[y,x].ToString("R",CultureInfo.InvariantCulture)).Append(';');
        using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Replace("-","");
    }
}
