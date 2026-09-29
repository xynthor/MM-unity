using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMSorpigalOnlyRepair
{
    const int N=128;
    const string ScenePath="Assets/Scenes/NewSorpigal_OpenWorld.unity";
    const string DataDir="Assets/World/NewSorpigal/Data";
    const string AuditDir="Validation/SorpigalOnlyRepair";

    static GameObject LoadGO(string p)
    {
        var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if(g)return g;
        return AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }

    static string SourcePath(GameObject go)
    {
        var src=PrefabUtility.GetCorrespondingObjectFromOriginalSource(go) as GameObject;
        if(!src)src=PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
        return src?AssetDatabase.GetAssetPath(src):"";
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }

    static bool MaterialsOK(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return false;
        foreach(var r in rs)
        {
            var ms=r.sharedMaterials;if(ms==null||ms.Length==0)return false;
            foreach(var m in ms)if(!m||!m.shader||!m.shader.isSupported||m.shader.name.IndexOf("InternalError",StringComparison.OrdinalIgnoreCase)>=0)return false;
        }
        return true;
    }

    static float VerticalRatio(GameObject go)
    {
        var b=BoundsOf(go);return b.size.y/Mathf.Max(.05f,Mathf.Max(b.size.x,b.size.z));
    }

    static void Ground(GameObject go,Terrain t)
    {
        float x=go.transform.position.x,z=go.transform.position.z;
        var e=go.transform.eulerAngles;go.transform.rotation=Quaternion.Euler(0,e.y,0);
        var b=BoundsOf(go);if(b.size==Vector3.zero)return;
        float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(gy-b.min.y);
        go.transform.position=new Vector3(x,go.transform.position.y,z);
    }

    static void ScaleHeight(GameObject go,float h)
    {
        var b=BoundsOf(go);if(b.size.y>.05f)go.transform.localScale*=h/b.size.y;
    }

    static Material GreenAtlas()
    {
        string dir="Assets/Materials/RealisticWorld/Sorpigal";Directory.CreateDirectory(dir);
        string p=dir+"/SorpigalGreenBroadleaf.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}else m.shader=sh;
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/Gobkit/TreeAtlas.png");
        if(tex)m.SetTexture("_MainTex",tex);
        m.color=new Color(.72f,1f,.68f,1f);
        m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.34f);m.SetOverrideTag("RenderType","TransparentCutout");
        m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.025f);
        EditorUtility.SetDirty(m);return m;
    }

    static void ForceTreeHighGreen(GameObject go,Material m)
    {
        string p=SourcePath(go).ToLowerInvariant();
        if(!p.Contains("treehigh"))return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
            var a=new Material[n];for(int i=0;i<n;i++)a[i]=m;r.sharedMaterials=a;
        }
    }

    static GameObject[] GreenPool()=>new[]{
        LoadGO("Assets/EnvironmentAssets/Gobkit/TreeHigh001.fbx"),
        LoadGO("Assets/EnvironmentAssets/Gobkit/TreeHigh002.fbx"),
        LoadGO("Assets/EnvironmentAssets/Gobkit/TreeHigh003.fbx"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_L.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_M.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_M2.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_M3.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_002_new/Pine_002_S2.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_1.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_2.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_3.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_5.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_6.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_01.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_005/Pine_005_02.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_01.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_02.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_03.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_04.prefab"),
        LoadGO("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_007/pine_007_01.prefab")
    }.Where(x=>x).Distinct().ToArray();

    static int Hash(string s,float x,float z)
    {
        unchecked{return (s.GetHashCode()*397)^Mathf.RoundToInt(x*100f)*73856093^Mathf.RoundToInt(z*100f)*19349663;}
    }

    static GameObject SpawnGreenReplacement(GameObject old,Terrain terrain,GameObject[] pool,Material atlas,int seed,bool sourceAnchor)
    {
        Transform parent=old.transform.parent;string name=old.name;
        float x=old.transform.position.x,z=old.transform.position.z;
        float oldH=BoundsOf(old).size.y;
        float targetH=sourceAnchor?Mathf.Clamp(oldH,6.5f,13f):Mathf.Clamp(oldH,5.5f,11.5f);
        if(oldH<1f)targetH=sourceAnchor?9f:7.5f;
        float yaw=(Mathf.Abs(seed)%36000)/100f;

        for(int k=0;k<pool.Length;k++)
        {
            var src=pool[Mathf.Abs(seed+k*7919)%pool.Length];if(!src)continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
            go.name=name;go.transform.SetParent(parent,true);go.transform.position=new Vector3(x,0,z);
            go.transform.rotation=Quaternion.Euler(0f,yaw,0f);go.transform.localScale=Vector3.one;
            ForceTreeHighGreen(go,atlas);
            ScaleHeight(go,targetH*(.90f+((Mathf.Abs(seed+k*31)%200)/1000f)));
            Ground(go,terrain);
            go.transform.rotation=Quaternion.Euler(0f,yaw,0f);
            float vr=VerticalRatio(go);
            bool ok=vr>=.62f&&MaterialsOK(go)&&
                    Mathf.Abs(go.transform.position.x-x)<.002f&&Mathf.Abs(go.transform.position.z-z)<.002f;
            if(ok)
            {
                UnityEngine.Object.DestroyImmediate(old);
                return go;
            }
            UnityEngine.Object.DestroyImmediate(go);
        }
        throw new Exception("No upright green replacement for "+name);
    }

    static void RepairVegetation(UnityEngine.SceneManagement.Scene sc,Terrain terrain,List<string> rows)
    {
        var roots=sc.GetRootGameObjects();
        var veg=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                     .FirstOrDefault(t=>t.name=="Vegetation - MM Anchors + Natural Groves");
        if(!veg)throw new Exception("Sorpigal vegetation root missing");

        var pool=GreenPool();if(pool.Length<12)throw new Exception("Green tree pool too small "+pool.Length);
        var atlas=GreenAtlas();

        var source=veg.GetComponentsInChildren<Transform>(true)
                      .Where(t=>t!=veg&&t.name.StartsWith("6tree",StringComparison.OrdinalIgnoreCase))
                      .Select(t=>t.gameObject).ToList();
        var groves=veg.GetComponentsInChildren<Transform>(true)
                      .Where(t=>t!=veg&&t.name.StartsWith("GroveTree_",StringComparison.OrdinalIgnoreCase))
                      .Select(t=>t.gameObject).ToList();

        var sourceBefore=source.ToDictionary(g=>g.name,g=>new Vector2(g.transform.position.x,g.transform.position.z));
        int banyanBefore=source.Concat(groves).Count(g=>SourcePath(g).IndexOf("BanyanTree",StringComparison.OrdinalIgnoreCase)>=0);
        int groveBefore=groves.Count,sourceCount=source.Count,replacedSource=0,replacedGrove=0,removedGrove=0;

        foreach(var old in source.ToList())
        {
            float x=old.transform.position.x,z=old.transform.position.z;
            int seed=Hash(old.name,x,z);
            SpawnGreenReplacement(old,terrain,pool,atlas,seed,true);
            replacedSource++;
        }

        foreach(var old in groves.ToList())
        {
            float x=old.transform.position.x,z=old.transform.position.z;
            int seed=Hash(old.name,x,z);
            bool keep=(Mathf.Abs(seed)%1000)<350;
            if(!keep){UnityEngine.Object.DestroyImmediate(old);removedGrove++;continue;}
            SpawnGreenReplacement(old,terrain,pool,atlas,seed,false);
            replacedGrove++;
        }

        // Keep shrubs/ferns at their existing X/Z, but force them onto the terrain.
        var understory=veg.GetComponentsInChildren<Transform>(true)
                          .Where(t=>t!=veg&&(t.name.IndexOf("shrub",StringComparison.OrdinalIgnoreCase)>=0||
                                             t.name.IndexOf("fern",StringComparison.OrdinalIgnoreCase)>=0||
                                             t.name.IndexOf("under",StringComparison.OrdinalIgnoreCase)>=0))
                          .Select(t=>t.gameObject).Distinct().ToList();
        foreach(var go in understory)
        {
            if(!go||!go.GetComponentInChildren<Renderer>(true))continue;
            float x=go.transform.position.x,z=go.transform.position.z;Ground(go,terrain);
            go.transform.position=new Vector3(x,go.transform.position.y,z);
        }

        var now=veg.GetComponentsInChildren<Transform>(true);
        var sourceNow=now.Where(t=>t.name.StartsWith("6tree",StringComparison.OrdinalIgnoreCase)).Select(t=>t.gameObject).ToList();
        var groveNow=now.Where(t=>t.name.StartsWith("GroveTree_",StringComparison.OrdinalIgnoreCase)).Select(t=>t.gameObject).ToList();
        int banyanAfter=sourceNow.Concat(groveNow).Count(g=>SourcePath(g).IndexOf("BanyanTree",StringComparison.OrdinalIgnoreCase)>=0);
        if(banyanAfter!=0)throw new Exception("Banyan remains in Sorpigal: "+banyanAfter);

        foreach(var go in sourceNow)
        {
            if(!sourceBefore.TryGetValue(go.name,out var p))throw new Exception("Source anchor name changed "+go.name);
            if(Mathf.Abs(go.transform.position.x-p.x)>.002f||Mathf.Abs(go.transform.position.z-p.y)>.002f)
                throw new Exception("Source anchor moved "+go.name);
        }

        int horizontal=0,invalidMat=0,floaters=0;
        foreach(var go in sourceNow.Concat(groveNow))
        {
            if(VerticalRatio(go)<.62f)horizontal++;
            if(!MaterialsOK(go))invalidMat++;
            var b=BoundsOf(go);float gy=terrain.SampleHeight(go.transform.position)+terrain.transform.position.y;
            if(Mathf.Abs(b.min.y-gy)>.08f)floaters++;
        }
        if(horizontal>0||invalidMat>0||floaters>0)throw new Exception($"Sorpigal tree validation horizontal={horizontal} materials={invalidMat} floaters={floaters}");

        rows.Add($"vegetation,source,{sourceCount},{sourceNow.Count},0,{replacedSource},0");
        rows.Add($"vegetation,groves,{groveBefore},{groveNow.Count},{removedGrove},{replacedGrove},0");
        rows.Add($"vegetation,banyan,{banyanBefore},{banyanAfter},0,0,0");
        rows.Add($"vegetation,validation,{sourceNow.Count+groveNow.Count},{sourceNow.Count+groveNow.Count},0,{horizontal},{invalidMat+floaters}");
        Debug.Log($"SORPIGAL_VEG source={sourceNow.Count} groves={groveNow.Count} removedGroves={removedGrove} banyan=0 horizontal=0 floaters=0");
    }

    static bool[,] SelectVolcano(byte[] tile,byte[] grp,byte[] sem,byte[] hm)
    {
        var candidates=new HashSet<Vector2Int>();
        for(int y=0;y<N;y++)for(int x=0;x<N;x++)
        {
            byte raw=tile[y*N+x],f=sem[raw];
            if(grp[raw]==3&&(f&1)==0)candidates.Add(new Vector2Int(x,y));
        }
        var comps=new List<List<Vector2Int>>();
        while(candidates.Count>0)
        {
            var seed=candidates.First();candidates.Remove(seed);
            var q=new Queue<Vector2Int>();q.Enqueue(seed);var comp=new List<Vector2Int>{seed};
            while(q.Count>0)
            {
                var p=q.Dequeue();
                foreach(var n in new[]{new Vector2Int(p.x+1,p.y),new Vector2Int(p.x-1,p.y),new Vector2Int(p.x,p.y+1),new Vector2Int(p.x,p.y-1)})
                    if(n.x>=0&&n.x<N&&n.y>=0&&n.y<N&&candidates.Remove(n)){q.Enqueue(n);comp.Add(n);}
            }
            comps.Add(comp);
        }
        var best=comps.OrderByDescending(c=>c.Average(p=>(double)hm[p.y*N+p.x])).First();
        var mask=new bool[N,N];foreach(var p in best)mask[p.y,p.x]=true;
        Debug.Log($"SORPIGAL_VOLCANO cells={best.Count} avgH={best.Average(p=>(double)hm[p.y*N+p.x]):F2}");
        return mask;
    }

    static float[,] WaterDistance(byte[] tile,byte[] sem)
    {
        var d=new float[N,N];var q=new Queue<Vector2Int>();
        for(int y=0;y<N;y++)for(int x=0;x<N;x++)
        {
            byte f=sem[tile[y*N+x]];
            if((f&1)!=0){d[y,x]=0;q.Enqueue(new Vector2Int(x,y));}
            else d[y,x]=999f;
        }
        int[] dx={1,-1,0,0,1,1,-1,-1};int[] dy={0,0,1,-1,1,-1,1,-1};
        float[] cost={1,1,1,1,1.4142f,1.4142f,1.4142f,1.4142f};
        while(q.Count>0)
        {
            var p=q.Dequeue();
            for(int i=0;i<8;i++)
            {
                int x=p.x+dx[i],y=p.y+dy[i];if(x<0||x>=N||y<0||y>=N)continue;
                float nd=d[p.y,p.x]+cost[i];
                if(nd+0.001f<d[y,x]){d[y,x]=nd;q.Enqueue(new Vector2Int(x,y));}
            }
        }
        return d;
    }

    static float Bilinear(float[,] a,float x,float y)
    {
        x=Mathf.Clamp(x,0,N-1.001f);y=Mathf.Clamp(y,0,N-1.001f);
        int x0=Mathf.FloorToInt(x),y0=Mathf.FloorToInt(y),x1=Mathf.Min(N-1,x0+1),y1=Mathf.Min(N-1,y0+1);
        float tx=x-x0,ty=y-y0;
        return Mathf.Lerp(Mathf.Lerp(a[y0,x0],a[y0,x1],tx),Mathf.Lerp(a[y1,x0],a[y1,x1],tx),ty);
    }

    static float VolcanoWeight(bool[,] v,float x,float y)
    {
        int cx=Mathf.Clamp(Mathf.RoundToInt(x),0,N-1),cy=Mathf.Clamp(Mathf.RoundToInt(y),0,N-1);
        float s=0,w=0;
        for(int oy=-2;oy<=2;oy++)for(int ox=-2;ox<=2;ox++)
        {
            int xx=Mathf.Clamp(cx+ox,0,N-1),yy=Mathf.Clamp(cy+oy,0,N-1);
            float ww=Mathf.Exp(-(ox*ox+oy*oy)/3f);w+=ww;if(v[yy,xx])s+=ww;
        }
        return s/Mathf.Max(.001f,w);
    }

    static int LayerIndex(TerrainData td,string token)
    {
        for(int i=0;i<td.terrainLayers.Length;i++)if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0)return i;
        throw new Exception("Missing terrain layer "+token);
    }

    static void PaintTerrain(Terrain terrain,List<string> rows)
    {
        var tile=File.ReadAllBytes(DataDir+"/tilemap_u8.bin");
        var grp=File.ReadAllBytes(DataDir+"/tile_groups_u8.bin");
        var sem=File.ReadAllBytes(DataDir+"/tile_semantics_u8.bin");
        var hm=File.ReadAllBytes(DataDir+"/heightmap_u8.bin");
        var volcano=SelectVolcano(tile,grp,sem,hm);var waterDist=WaterDistance(tile,sem);
        var td=terrain.terrainData;int r=td.alphamapResolution,L=td.alphamapLayers;
        int green=LayerIndex(td,"Green"),light=LayerIndex(td,"LightGreen"),desert=LayerIndex(td,"Desert"),
            volc=LayerIndex(td,"Volcanic"),arid=LayerIndex(td,"Arid"),road=LayerIndex(td,"RoadOverlay");
        var a=new float[r,r,L];int volcanicPixels=0,shorePixels=0;
        for(int ay=0;ay<r;ay++)for(int ax=0;ax<r;ax++)
        {
            float sx=((ax+.5f)/r)*N-.5f, sy=N-1-(((ay+.5f)/r)*N-.5f);
            int ix=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),iy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
            byte raw=tile[iy*N+ix],g=grp[raw],f=sem[raw];
            if((f&8)!=0||(g>=8&&g<255)){a[ay,ax,road]=1f;continue;}
            if((f&1)!=0){a[ay,ax,arid]=1f;continue;}

            float vw=VolcanoWeight(volcano,sx,sy);
            float dist=Bilinear(waterDist,sx,sy);
            float noise=Mathf.PerlinNoise((sx+17.3f)*.11f,(sy+43.7f)*.11f);
            float coast=Mathf.Clamp01((2.55f+noise*.9f-dist)/2.25f);
            if((f&2)!=0)coast=Mathf.Max(coast,.72f);

            if(vw>.02f)
            {
                a[ay,ax,volc]=Mathf.Lerp(.45f,.96f,vw);
                a[ay,ax,arid]=1f-a[ay,ax,volc];
                volcanicPixels++;
            }
            else
            {
                float dirt=(g==255)?0.26f:0.06f;
                float sand=coast*.62f;
                a[ay,ax,desert]=sand;
                a[ay,ax,light]=Mathf.Clamp01(dirt+(1f-coast)*.05f);
                a[ay,ax,green]=Mathf.Max(0f,1f-a[ay,ax,desert]-a[ay,ax,light]);
                if(coast>.08f)shorePixels++;
            }
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);terrain.Flush();
        rows.Add($"terrain,volcano_pixels,{volcanicPixels},{volcanicPixels},0,0,0");
        rows.Add($"terrain,shore_blend_pixels,{shorePixels},{shorePixels},0,0,0");
        Debug.Log($"SORPIGAL_TERRAIN volcanoPixels={volcanicPixels} shoreBlendPixels={shorePixels}");
    }

    static string TPath(Transform t)
    {
        var s=new Stack<string>();for(var p=t;p;p=p.parent)s.Push(p.name+"#"+p.GetSiblingIndex());return string.Join("/",s.ToArray());
    }

    static Dictionary<string,Vector3> FixedXZ(UnityEngine.SceneManagement.Scene sc)
    {
        var d=new Dictionary<string,Vector3>();
        foreach(var r in sc.GetRootGameObjects())foreach(var t in r.GetComponentsInChildren<Transform>(true))
        {
            bool veg=false;for(var p=t;p;p=p.parent)if(p.name.IndexOf("Vegetation",StringComparison.OrdinalIgnoreCase)>=0||p.name.IndexOf("Ecosystem",StringComparison.OrdinalIgnoreCase)>=0){veg=true;break;}
            if(veg||t.name=="__AUDIT_CAMERA")continue;
            if(t.GetComponent<Renderer>()||t.GetComponent<Collider>())d[TPath(t)]=t.position;
        }
        return d;
    }

    static void VerifyFixed(Dictionary<string,Vector3> before,UnityEngine.SceneManagement.Scene sc,List<string> rows)
    {
        var after=FixedXZ(sc);int miss=0,add=0,move=0;
        foreach(var kv in before)
        {
            if(!after.TryGetValue(kv.Key,out var p)){miss++;continue;}
            if(Mathf.Abs(p.x-kv.Value.x)>.001f||Mathf.Abs(p.z-kv.Value.z)>.001f)move++;
        }
        foreach(var k in after.Keys)if(!before.ContainsKey(k))add++;
        rows.Add($"fixed_xz,all,{before.Count},{after.Count},{miss},{add},{move}");
        if(miss>0||add>0||move>0)throw new Exception($"Sorpigal fixed-object diff missing={miss} added={add} movedXZ={move}");
    }

    static void CaptureSorpigal(UnityEngine.SceneManagement.Scene sc)
    {
        Directory.CreateDirectory("C:/MMUnityPort/Validation/AuditScreens");
        var go=new GameObject("__AUDIT_CAMERA");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,sc);
        var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=300f;
        cam.transform.position=new Vector3(0,650,0);cam.transform.rotation=Quaternion.Euler(90,0,0);
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.11f,.12f);cam.farClipPlane=1200f;
        var rt=new RenderTexture(1024,1024,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1024,1024,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1024,1024),0,0);tex.Apply();
        File.WriteAllBytes("C:/MMUnityPort/Validation/AuditScreens/NewSorpigal_OpenWorld.png",tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
    }

    [MenuItem("MMUnity/Locked/SORPIGAL ONLY Repair")]
    public static void Run()
    {
        Directory.CreateDirectory(AuditDir);
        var rows=new List<string>{"section,item,before,after,removed,replaced_or_added,invalid"};
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var terrain=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception("Sorpigal terrain missing");
        var fixedBefore=FixedXZ(sc);

        PaintTerrain(terrain,rows);
        RepairVegetation(sc,terrain,rows);

        VerifyFixed(fixedBefore,sc,rows);
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,ScenePath);
        AssetDatabase.SaveAssets();
        CaptureSorpigal(sc);
        File.WriteAllLines(AuditDir+"/summary.csv",rows);
        Debug.Log("SORPIGAL_ONLY_REPAIR_DONE");
    }
}
