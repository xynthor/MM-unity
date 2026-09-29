using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMDragonIsleFullRealismPass
{
    const string ScenePath="Assets/Scenes/Regions/DragonIsle.unity";
    const string LinkedPath="Assets/Scenes/World/Enroth.unity";
    const string GroupName="Dragon Isle - Full Realism";
    const string OldGroupName="Dragon Isle - Realism Additions";
    const string VolcanicLayerPath="Assets/Materials/RealisticWorld/Volcanic.terrainlayer";
    const string ShrubMatPath="Assets/Materials/RealisticWorld/Shrub02.mat";
    const string RockMatPath="Assets/Materials/Source3D/Stone.mat";
    static readonly string[] RockPaths={
        "Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx",
        "Assets/EnvironmentAssets/Gobkit/Rock001.fbx",
        "Assets/EnvironmentAssets/Gobkit/Rock002.fbx"
    };
    const string ShrubPath="Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx";
    const string StumpPath="Assets/Art/Environment/Vegetation/Trees/Generic_Stump/Stump_04/Stump_04.prefab";
    const string DeadTrunkPath="Assets/Environment/PolyHaven/Models/dead_quiver_trunk/dead_quiver_trunk_1k.fbx";

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static bool ValidMaterials(GameObject go)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
            if(r.sharedMaterials==null||r.sharedMaterials.Length==0)return false;
            foreach(var m in r.sharedMaterials)
                if(!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader")return false;
        }
        return true;
    }
    static GameObject Inst(string path,Transform parent)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        if(!go)go=UnityEngine.Object.Instantiate(src);
        go.transform.SetParent(parent,false);return go;
    }
    static void ScaleToMax(GameObject go,float target)
    {
        var b=BoundsOf(go);float m=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(m>.001f)go.transform.localScale*=target/m;
    }
    static void Ground(GameObject go,Terrain t,float x,float z,float sink=0f)
    {
        var b=BoundsOf(go);float gy=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
        go.transform.position+=Vector3.up*(gy-b.min.y-sink);
    }
    static float Height(Terrain t,float x,float z)=>t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
    static float Slope(Terrain t,float x,float z)
    {
        Vector3 p=new Vector3(x,0,z)-t.transform.position;var s=t.terrainData.size;
        return t.terrainData.GetSteepness(Mathf.Clamp01(p.x/s.x),Mathf.Clamp01(p.z/s.z));
    }
    static bool NearBounds(Bounds[] bs,float x,float z,float margin)
    {
        foreach(var b in bs)if(x>=b.min.x-margin&&x<=b.max.x+margin&&z>=b.min.z-margin&&z<=b.max.z+margin)return true;
        return false;
    }
    static bool Near(List<Vector2> pts,float x,float z,float min)
    {
        float d=min*min;var q=new Vector2(x,z);
        foreach(var p in pts)if((p-q).sqrMagnitude<d)return true;
        return false;
    }
    static float Smooth01(float a,float b,float x)
    {
        if(b<=a)return x>=b?1f:0f;
        float t=Mathf.Clamp01((x-a)/(b-a));return t*t*(3f-2f*t);
    }
    static void AddTerrainLayer(TerrainData td,TerrainLayer layer)
    {
        if(!layer)return;
        var l=td.terrainLayers??new TerrainLayer[0];
        if(l.Contains(layer))return;
        var n=new TerrainLayer[l.Length+1];Array.Copy(l,n,l.Length);n[l.Length]=layer;td.terrainLayers=n;
    }
    static int LayerIndex(TerrainData td,string key)
    {
        for(int i=0;i<td.terrainLayers.Length;i++)
            if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf(key,StringComparison.OrdinalIgnoreCase)>=0)return i;
        return -1;
    }
    static void ApplyMicroRelief(Terrain t,float waterY,Bounds[] protectedBounds,out float maxDelta,out int changed)
    {
        var td=t.terrainData;int r=td.heightmapResolution;var h=td.GetHeights(0,0,r,r);
        float sx=td.size.x,sz=td.size.z,sy=td.size.y;maxDelta=0;changed=0;
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            float wx=t.transform.position.x+x/(float)(r-1)*sx;
            float wz=t.transform.position.z+y/(float)(r-1)*sz;
            float wh=t.transform.position.y+h[y,x]*sy;
            float land=Smooth01(waterY+.55f,waterY+4.0f,wh);
            if(land<=.001f||NearBounds(protectedBounds,wx,wz,12f))continue;
            float edge=Mathf.Min(Mathf.Min(x,y),Mathf.Min(r-1-x,r-1-y))/(float)(r-1);
            float edgeMask=Smooth01(.015f,.08f,edge);
            float n1=Mathf.PerlinNoise((wx+713.2f)*.018f,(wz-191.7f)*.018f)-.5f;
            float n2=Mathf.PerlinNoise((wx-91.4f)*.057f,(wz+881.3f)*.057f)-.5f;
            float n3=Mathf.PerlinNoise((wx+19.8f)*.115f,(wz+43.1f)*.115f)-.5f;
            float delta=(n1*2.4f+n2*.85f+n3*.28f)*land*edgeMask;
            // Keep the island silhouette fixed: coastal samples receive virtually no relief.
            delta*=Smooth01(waterY+1.0f,waterY+5.5f,wh);
            if(Mathf.Abs(delta)<.01f)continue;
            h[y,x]=Mathf.Clamp01(h[y,x]+delta/sy);
            maxDelta=Mathf.Max(maxDelta,Mathf.Abs(delta));changed++;
        }
        td.SetHeights(0,0,h);t.Flush();EditorUtility.SetDirty(td);
    }
    static void PaintTerrain(Terrain t,float waterY,out double[] pct)
    {
        var td=t.terrainData;var volcanic=AssetDatabase.LoadAssetAtPath<TerrainLayer>(VolcanicLayerPath);AddTerrainLayer(td,volcanic);
        int grass=LayerIndex(td,"Grass"),sand=LayerIndex(td,"ShoreSand"),dirt=LayerIndex(td,"Dirt"),rock=LayerIndex(td,"Rock"),volc=LayerIndex(td,"Volcanic");
        int w=td.alphamapWidth,h=td.alphamapHeight,L=td.alphamapLayers;var a=new float[h,w,L];var sums=new double[L];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float nx=x/(float)(w-1),nz=y/(float)(h-1);
            float wx=t.transform.position.x+nx*td.size.x,wz=t.transform.position.z+nz*td.size.z;
            float wh=Height(t,wx,wz),sl=td.GetSteepness(nx,nz);
            float land=Smooth01(waterY+.15f,waterY+2.8f,wh);
            float shore=1f-Smooth01(waterY+.2f,waterY+4.8f,wh);
            float steep=Smooth01(13f,38f,sl);
            float high=Smooth01(waterY+10f,waterY+28f,wh);
            float n1=Mathf.PerlinNoise((wx+311f)*.024f,(wz-117f)*.024f);
            float n2=Mathf.PerlinNoise((wx-87f)*.081f,(wz+619f)*.081f);
            float patch=.65f*n1+.35f*n2;
            float wg=land*(1f-steep)*(1f-high)*(.06f+.16f*patch);
            float ws=shore*(.72f-.28f*steep)+(1f-land)*.18f;
            float wd=land*(1f-steep)*(.20f+.23f*(1f-patch));
            float wr=.15f+.58f*steep+.22f*high+.12f*shore;
            float wv=land*(.18f+.28f*high+.23f*patch+.14f*steep);
            float sum=0f;
            if(grass>=0){a[y,x,grass]=Mathf.Max(0,wg);sum+=a[y,x,grass];}
            if(sand>=0){a[y,x,sand]=Mathf.Max(0,ws);sum+=a[y,x,sand];}
            if(dirt>=0){a[y,x,dirt]=Mathf.Max(0,wd);sum+=a[y,x,dirt];}
            if(rock>=0){a[y,x,rock]=Mathf.Max(.02f,wr);sum+=a[y,x,rock];}
            if(volc>=0){a[y,x,volc]=Mathf.Max(0,wv);sum+=a[y,x,volc];}
            if(sum<=0)sum=1;
            for(int k=0;k<L;k++){a[y,x,k]/=sum;sums[k]+=a[y,x,k];}
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);
        pct=sums.Select(s=>s/(w*(double)h)*100.0).ToArray();
    }
    static void RegroundContainer(Transform container,Terrain t)
    {
        if(!container)return;
        foreach(Transform c in container)
        {
            var b=BoundsOf(c.gameObject);if(b.size==Vector3.zero)continue;
            float gy=Height(t,b.center.x,b.center.z);
            c.position+=Vector3.up*(gy-b.min.y);
        }
    }
    static GameObject SpawnRock(Transform parent,Terrain t,System.Random rng,float x,float z,float target,string name)
    {
        string path=RockPaths[rng.Next(RockPaths.Length)];
        var go=Inst(path,parent);if(!go)return null;go.name=name;
        var rockMat=AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
        if(rockMat)foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
            int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
            var a=new Material[n];for(int k=0;k<n;k++)a[k]=rockMat;r.sharedMaterials=a;
        }
        go.transform.position=new Vector3(x,0,z);go.transform.rotation=Quaternion.Euler((float)rng.NextDouble()*10f-5f,(float)rng.NextDouble()*360f,(float)rng.NextDouble()*10f-5f);
        ScaleToMax(go,target);Ground(go,t,x,z,target>.0f?Mathf.Min(.18f,target*.06f):0f);
        var b=BoundsOf(go);float m=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(!ValidMaterials(go)||m>7.0f||m<.25f){UnityEngine.Object.DestroyImmediate(go);return null;}
        return go;
    }
    static GameObject SpawnShrub(Transform parent,Terrain t,System.Random rng,float x,float z,float target,string name,Material shrubMat)
    {
        var go=Inst(ShrubPath,parent);if(!go)return null;go.name=name;
        go.transform.position=new Vector3(x,0,z);go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
        ScaleToMax(go,target);
        if(shrubMat)foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            int n=(r.sharedMaterials==null||r.sharedMaterials.Length==0)?1:r.sharedMaterials.Length;
            var a=new Material[n];for(int i=0;i<n;i++)a[i]=shrubMat;r.sharedMaterials=a;
        }
        Ground(go,t,x,z,.02f);var b=BoundsOf(go);float m=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(!ValidMaterials(go)||m>1.7f||m<.2f){UnityEngine.Object.DestroyImmediate(go);return null;}
        return go;
    }
    static GameObject SpawnDeadwood(Transform parent,Terrain t,System.Random rng,float x,float z,float target,string name)
    {
        string path=rng.NextDouble()<.55?StumpPath:DeadTrunkPath;var go=Inst(path,parent);if(!go)return null;go.name=name;
        go.transform.position=new Vector3(x,0,z);go.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
        ScaleToMax(go,target);Ground(go,t,x,z,.04f);
        var b=BoundsOf(go);float m=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(!ValidMaterials(go)||m>4.5f||m<.25f){UnityEngine.Object.DestroyImmediate(go);return null;}
        return go;
    }
    static bool FindPoint(Terrain t,System.Random rng,float waterY,Bounds[] avoid,float minH,float maxH,float maxSlope,out float x,out float z)
    {
        Vector3 tp=t.transform.position,s=t.terrainData.size;
        for(int k=0;k<300;k++)
        {
            x=tp.x+10f+(float)rng.NextDouble()*(s.x-20f);
            z=tp.z+10f+(float)rng.NextDouble()*(s.z-20f);
            float h=Height(t,x,z),sl=Slope(t,x,z);
            if(h<waterY+minH||h>waterY+maxH||sl>maxSlope||NearBounds(avoid,x,z,10f))continue;
            return true;
        }
        x=z=0;return false;
    }
    static void Populate(Terrain t,Transform root,float waterY,Bounds[] avoid,out int rocks,out int shrubs,out int deadwood)
    {
        var rng=new System.Random(610662);
        var shore=new GameObject("Shoreline Boulder Clusters");shore.transform.SetParent(root,false);
        var interior=new GameObject("Interior Rock Fields");interior.transform.SetParent(root,false);
        var ground=new GameObject("Sparse Ground Vegetation");ground.transform.SetParent(root,false);
        var dead=new GameObject("Deadwood");dead.transform.SetParent(root,false);
        var occupied=new List<Vector2>();rocks=shrubs=deadwood=0;
        var shrubMat=AssetDatabase.LoadAssetAtPath<Material>(ShrubMatPath);

        // 14 visible shoreline clusters, 7-11 boulders each.
        for(int c=0;c<14;c++)
        {
            if(!FindPoint(t,rng,waterY,avoid,.7f,5.0f,32f,out float cx,out float cz))continue;
            int n=7+rng.Next(5);
            for(int i=0;i<n;i++)
            {
                float ang=(float)rng.NextDouble()*Mathf.PI*2f,rad=1.5f+(float)rng.NextDouble()*8f;
                float x=cx+Mathf.Cos(ang)*rad,z=cz+Mathf.Sin(ang)*rad;
                float h=Height(t,x,z);if(h<waterY+.15f||h>waterY+7f||NearBounds(avoid,x,z,8f))continue;
                var go=SpawnRock(shore.transform,t,rng,x,z,.8f+(float)rng.NextDouble()*2.6f,$"DI_ShoreRock_{c:00}_{i:00}");
                if(go){rocks++;occupied.Add(new Vector2(x,z));}
            }
        }

        // 12 interior outcrop fields with mixed stone sizes.
        for(int c=0;c<12;c++)
        {
            if(!FindPoint(t,rng,waterY,avoid,4.0f,60f,34f,out float cx,out float cz))continue;
            int n=8+rng.Next(6);
            for(int i=0;i<n;i++)
            {
                float ang=(float)rng.NextDouble()*Mathf.PI*2f,rad=(float)rng.NextDouble()*11f;
                float x=cx+Mathf.Cos(ang)*rad,z=cz+Mathf.Sin(ang)*rad;
                if(Height(t,x,z)<waterY+2f||Slope(t,x,z)>40f||NearBounds(avoid,x,z,8f))continue;
                float target=(i<2?3.5f:1.0f)+(float)rng.NextDouble()*(i<2?2.2f:2.6f);
                var go=SpawnRock(interior.transform,t,rng,x,z,target,$"DI_Outcrop_{c:00}_{i:00}");
                if(go){rocks++;occupied.Add(new Vector2(x,z));}
            }
        }

        // Additional scattered stones to break up broad empty surfaces.
        int tries=0;while(rocks<235&&tries++<5000)
        {
            if(!FindPoint(t,rng,waterY,avoid,.8f,60f,39f,out float x,out float z))continue;
            if(Near(occupied,x,z,2.8f))continue;
            float target=.55f+(float)rng.NextDouble()*2.5f;
            var go=SpawnRock(interior.transform,t,rng,x,z,target,$"DI_ScatterRock_{rocks:000}");
            if(go){rocks++;occupied.Add(new Vector2(x,z));}
        }

        tries=0;while(shrubs<78&&tries++<4000)
        {
            if(!FindPoint(t,rng,waterY,avoid,2.0f,28f,23f,out float x,out float z))continue;
            if(Near(occupied,x,z,3.6f))continue;
            float target=.55f+(float)rng.NextDouble()*.82f;
            var go=SpawnShrub(ground.transform,t,rng,x,z,target,$"DI_LowShrub_{shrubs:000}",shrubMat);
            if(go){shrubs++;occupied.Add(new Vector2(x,z));}
        }

        tries=0;while(deadwood<26&&tries++<2500)
        {
            if(!FindPoint(t,rng,waterY,avoid,2.5f,34f,25f,out float x,out float z))continue;
            if(Near(occupied,x,z,5.0f))continue;
            float target=.8f+(float)rng.NextDouble()*2.4f;
            var go=SpawnDeadwood(dead.transform,t,rng,x,z,target,$"DI_Deadwood_{deadwood:000}");
            if(go){deadwood++;occupied.Add(new Vector2(x,z));}
        }
    }
    [MenuItem("MMUnity/Environment/Dragon Isle Full Realism")]
    public static void Apply()
    {
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var region=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(!region)throw new Exception("Dragon Isle region root missing");
        var t=region.GetComponentInChildren<Terrain>(true);if(!t)throw new Exception("Dragon Isle terrain missing");
        var water=region.transform.Find("Ocean + Central Lake Water");float waterY=water?water.position.y:.1f;
        var landmarks=region.transform.Find("Landmarks - Reference");
        var protectedBounds=landmarks?landmarks.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Select(r=>r.bounds).ToArray():new Bounds[0];

        foreach(string n in new[]{GroupName,OldGroupName})
        {
            var old=region.transform.Find(n);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        ApplyMicroRelief(t,waterY,protectedBounds,out float maxDelta,out int changedHeights);
        PaintTerrain(t,waterY,out double[] pct);

        // Existing vegetation gets re-grounded to the modest new relief, but is otherwise left intact.
        RegroundContainer(region.transform.Find("Forest - Natural Sparse"),t);

        var group=new GameObject(GroupName);group.transform.SetParent(region.transform,false);
        Populate(t,group.transform,waterY,protectedBounds,out int rocks,out int shrubs,out int deadwood);

        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,ScenePath))throw new IOException("Could not save Dragon Isle scene");
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var td=t.terrainData;
        var report=new List<string>{
            "Dragon Isle Full Realism",
            $"water_y={waterY:F3}",
            $"height_samples_changed={changedHeights}",
            $"max_microrelief_delta_m={maxDelta:F3}",
            $"rocks={rocks}",
            $"shrubs={shrubs}",
            $"deadwood={deadwood}",
            $"total_new_objects={rocks+shrubs+deadwood}",
            "terrain_layers="+string.Join(";",td.terrainLayers.Select(x=>x?x.name:"NULL"))
        };
        for(int i=0;i<pct.Length;i++)report.Add($"layer_{td.terrainLayers[i].name}_pct={pct[i]:F2}");
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_isle_full_realism.txt",report);
        Debug.Log($"DRAGON_ISLE_FULL_REALISM rocks={rocks} shrubs={shrubs} deadwood={deadwood} maxRelief={maxDelta:F2}");
    }

    [MenuItem("MMUnity/Environment/Sync Dragon Isle Realism To Linked")]
    public static void SyncLinked()
    {
        // TerrainData is a shared asset, so terrain surface changes already propagate.
        // This copies only the scene-level realism group into the existing linked Dragon Isle root.
        var linked=EditorSceneManager.OpenScene(LinkedPath,OpenSceneMode.Single);
        var lroot=linked.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name.StartsWith("Dragon Isle",StringComparison.OrdinalIgnoreCase)&&t.name.IndexOf("LINKED",StringComparison.OrdinalIgnoreCase)>=0);
        if(!lroot)throw new Exception("Linked Dragon Isle root missing");

        foreach(string n in new[]{GroupName,OldGroupName})
        {
            var old=lroot.Find(n);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        var srcScene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var sroot=srcScene.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var src=sroot?sroot.transform.Find(GroupName):null;if(!src)throw new Exception("Standalone realism group missing");
        var clone=UnityEngine.Object.Instantiate(src.gameObject);clone.name=GroupName;
        SceneManager.MoveGameObjectToScene(clone,linked);clone.transform.SetParent(lroot,false);
        clone.transform.localPosition=src.localPosition;clone.transform.localRotation=src.localRotation;clone.transform.localScale=src.localScale;
        EditorSceneManager.CloseScene(srcScene,true);

        EditorSceneManager.MarkSceneDirty(linked);
        if(!EditorSceneManager.SaveScene(linked,LinkedPath))throw new IOException("Could not save linked scene");
        AssetDatabase.SaveAssets();
        Debug.Log("DRAGON_ISLE_REALISM_SYNC_LINKED objects="+clone.transform.childCount);
    }

    [MenuItem("MMUnity/Validation/Dragon Isle Full Realism")]
    public static void Validate()
    {
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var lines=new List<string>{"scope,group_exists,rocks,shrubs,deadwood,invalid_material_objects,oversized_gt7m"};
        foreach(var q in new[]{("standalone",ScenePath),("linked",LinkedPath)})
        {
            var sc=EditorSceneManager.OpenScene(q.Item2,OpenSceneMode.Single);
            Transform region;
            if(q.Item1=="standalone")region=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true))?.transform;
            else region=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t=>t.name.StartsWith("Dragon Isle",StringComparison.OrdinalIgnoreCase)&&t.name.IndexOf("LINKED",StringComparison.OrdinalIgnoreCase)>=0);
            var g=region?region.Find(GroupName):null;int rock=0,shrub=0,dead=0,invalid=0,big=0;
            if(g)foreach(Transform c in g.GetComponentsInChildren<Transform>(true))
            {
                if(c==g)continue;
                if(c.name.StartsWith("DI_")){if(c.name.Contains("Rock"))rock++;else if(c.name.Contains("Shrub"))shrub++;else if(c.name.Contains("Deadwood"))dead++;}
                if(c.name.StartsWith("DI_")&&!ValidMaterials(c.gameObject))invalid++;
                var b=BoundsOf(c.gameObject);if(c.name.StartsWith("DI_")&&Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z))>7f)big++;
            }
            lines.Add($"{q.Item1},{(g?1:0)},{rock},{shrub},{dead},{invalid},{big}");
        }
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_isle_full_validation.csv",lines);
    }
}
