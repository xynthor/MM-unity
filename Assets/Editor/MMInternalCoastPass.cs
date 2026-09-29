using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMInternalCoastPass
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
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity")};
    const int N=128; const float WaterY=.10f;
    [MenuItem("MMUnity/Rebuild Smooth Internal Coasts")]
    public static void ApplyAll()
    {
        foreach(var z in Zones) Apply(z);
        AssetDatabase.SaveAssets();
        Debug.Log("INTERNAL_COAST_ALL_DONE zones="+Zones.Length);
    }
    static void Apply(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var roots=sc.GetRootGameObjects();
        var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception(z.key+" terrain missing");
        var root=roots.FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true)==terrain)??roots.First();
        string d=$"Assets/World/{z.key}/Data";
        byte[] tile=File.ReadAllBytes(d+"/tilemap_u8.bin"),sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        Material mat=MMUnifiedWorldWaterPass.SharedWaterMaterial();
        var old=root.transform.Find("Water");
        if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var old2=root.transform.Find("Internal Water - Smooth");if(old2)UnityEngine.Object.DestroyImmediate(old2.gameObject);
        int tris=BuildWater(z,root.transform,tile,sem,mat,terrain);
        int paint=BlendShore(terrain,tile,sem);
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"INTERNAL_COAST {z.key} waterTris={tris} painted={paint}");
    }
    static float Raw(byte[] tile,byte[] sem,float sx,float sy)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);
        int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy),x1=Mathf.Min(N-1,x0+1),y1=Mathf.Min(N-1,y0+1);
        float tx=sx-x0,ty=sy-y0;
        Func<int,int,float> q=(x,y)=>(sem[tile[y*N+x]]&1)!=0?1f:0f;
        return Mathf.Lerp(Mathf.Lerp(q(x0,y0),q(x1,y0),tx),Mathf.Lerp(q(x0,y1),q(x1,y1),tx),ty);
    }
    static float Soft(byte[] tile,byte[] sem,float sx,float sy)
    {
        float sum=Raw(tile,sem,sx,sy)*4f,ws=4f;
        Vector2[] a={new Vector2(.6f,0),new Vector2(-.6f,0),new Vector2(0,.6f),new Vector2(0,-.6f),
                     new Vector2(.55f,.55f),new Vector2(-.55f,.55f),new Vector2(.55f,-.55f),new Vector2(-.55f,-.55f),
                     new Vector2(1.2f,0),new Vector2(-1.2f,0),new Vector2(0,1.2f),new Vector2(0,-1.2f)};
        for(int i=0;i<a.Length;i++){float w=i<4?1.5f:(i<8?1f:.65f);sum+=Raw(tile,sem,sx+a[i].x,sy+a[i].y)*w;ws+=w;}
        return Mathf.Clamp01(sum/ws);
    }
    static float SoftWorld(byte[] tile,byte[] sem,float x,float z)=>Soft(tile,sem,x/4f+64f,64f-z/4f);
    static bool SourceWaterOrShore(byte[] tile,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.FloorToInt(x/4f+64f),0,N-1);
        int sy=Mathf.Clamp(Mathf.FloorToInt(64f-z/4f),0,N-1);
        byte f=sem[tile[sy*N+sx]];
        return (f&1)!=0||(f&2)!=0;
    }
    static bool ConfirmedCanal(string key,float x,float z,float groundY)
    {
        if(groundY>=WaterY-.02f)return false;
        if(key=="FreeHaven") return x>=5f&&x<=42f&&z>=-11f&&z<=10f;
        if(key=="MireOfTheDamned") return x>=-3f&&x<=54f&&z>=122f&&z<=133f;
        if(key=="FrozenHighlands") return x>=-163f&&x<=-154f&&z>=5f&&z<=35f;
        return false;
    }

    static Material WaterMaterial()
    {
        string p="Assets/Materials/SourceGridTerrain/GlobalOcean.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh=Shader.Find("MMUnity/EnrothMasterWater")??Shader.Find("MMUnity/DepthWater")??Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}else m.shader=sh;
        if(m.HasProperty("_DepthRange"))m.SetFloat("_DepthRange",18f);
        if(m.HasProperty("_FoamDepth"))m.SetFloat("_FoamDepth",1.0f);
        if(m.HasProperty("_NormalTex"))m.SetTexture("_NormalTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TerrainDemoScene_HDRP/Prefabs/Water/Textures/Water_Normal.png"));
        if(m.HasProperty("_NormalStrength"))m.SetFloat("_NormalStrength",.45f);
        m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
    }
    static void Emit(List<Vector2> poly,List<Vector3> v,List<Vector2> uv,List<int> tr)
    {
        if(poly.Count<3)return;int s=v.Count;
        foreach(var p in poly){v.Add(new Vector3(p.x,WaterY,p.y));uv.Add(new Vector2((p.x+256f)/32f,(p.y+256f)/32f));}
        for(int k=1;k<poly.Count-1;k++){tr.Add(s);tr.Add(s+k+1);tr.Add(s+k);}
    }
    static Vector2 Edge(Vector2 a,Vector2 b,float va,float vb,float th)
    {
        float u=Mathf.Abs(vb-va)<.0001f?.5f:Mathf.Clamp01((th-va)/(vb-va));return Vector2.Lerp(a,b,u);
    }
    static int BuildWater(Z z,Transform root,byte[] tile,byte[] sem,Material mat,Terrain terrain)
    {
        const int grid=256;const float step=2f,origin=-256f,th=.43f;
        var v=new List<Vector3>(450000);var uv=new List<Vector2>(450000);var tr=new List<int>(650000);
        for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
        {
            float x0=origin+x*step,x1=x0+step,z0=origin+y*step,z1=z0+step;
            Vector2[] p={new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)};float[] w=new float[4];int bits=0;
            for(int i=0;i<4;i++)
            {
                w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);
                float gy=terrain.SampleHeight(new Vector3(p[i].x,0f,p[i].y))+terrain.transform.position.y;
                bool canal=ConfirmedCanal(z.key,p[i].x,p[i].y,gy);
                // Never smooth water into source land cells, except the three explicitly approved
                // hand-carved canals. Canal water is still limited to terrain already below WaterY.
                if(!SourceWaterOrShore(tile,sem,p[i].x,p[i].y) && !canal)w[i]=0f;
                if(canal)w[i]=1f;
                if(gy>WaterY+.08f)w[i]=0f;
                if(w[i]>=th)bits|=1<<i;
            }
            if(bits==0)continue;if(bits==15){Emit(new List<Vector2>(p),v,uv,tr);continue;}
            var poly=new List<Vector2>(8);
            for(int i=0;i<4;i++){int j=(i+1)%4;bool a=w[i]>=th,b=w[j]>=th;if(a)poly.Add(p[i]);if(a!=b)poly.Add(Edge(p[i],p[j],w[i],w[j],th));}
            Emit(poly,v,uv,tr);
        }
        // Final triangle-level guard: reject any triangle whose centroid rises above land
        // or falls outside source water/shore. This eliminates edge-fan spill onto terrain.
        var kept=new List<int>(tr.Count);
        for(int i=0;i<tr.Count;i+=3)
        {
            Vector3 ca=v[tr[i]],cb=v[tr[i+1]],cc=v[tr[i+2]];
            Vector3 cp=(ca+cb+cc)/3f;
            float gy=terrain.SampleHeight(new Vector3(cp.x,0f,cp.z))+terrain.transform.position.y;
            bool source=SourceWaterOrShore(tile,sem,cp.x,cp.z)||ConfirmedCanal(z.key,cp.x,cp.z,gy);
            if(!source||gy>WaterY+.08f)continue;
            kept.Add(tr[i]);kept.Add(tr[i+1]);kept.Add(tr[i+2]);
        }
        tr=kept;
        string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);string path=$"{dir}/InternalWaterSmooth.asset";
        var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);}m.Clear();m.indexFormat=IndexFormat.UInt32;
        m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);
        var go=new GameObject("Internal Water - Smooth");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=m;
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;return tr.Count/3;
    }
    static int BlendShore(Terrain t,byte[] tile,byte[] sem)
    {
        var td=t.terrainData;if(td.alphamapLayers<9)return 0;int r=td.alphamapResolution,changed=0;
        var a=td.GetAlphamaps(0,0,r,r);Vector3 tp=t.transform.position,sz=td.size;
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            float wx=tp.x+(x+.5f)/r*sz.x,wz=tp.z+(y+.5f)/r*sz.z,w=SoftWorld(tile,sem,wx,wz);
            if(w<.035f)continue;float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));
            float k=w>=.48f?.88f:Mathf.Clamp01(w/.48f)*.72f;int target=steep>32f?8:2;
            for(int i=0;i<td.alphamapLayers;i++)a[y,x,i]*=(1f-k);
            a[y,x,target]+=k;float sum=0f;for(int i=0;i<td.alphamapLayers;i++)sum+=a[y,x,i];
            for(int i=0;i<td.alphamapLayers;i++)a[y,x,i]/=Mathf.Max(.0001f,sum);changed++;
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);return changed;
    }
}
