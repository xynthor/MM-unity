using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMWorldPolishPass
{
    sealed class Zone
    {
        public string key, display, scene;
        public Zone(string k,string d,string s){key=k;display=d;scene=s;}
    }

    static readonly Zone[] Zones={
        new Zone("NewSorpigal","New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Zone("CastleIronfist","Castle Ironfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Zone("MireOfTheDamned","Mire of the Damned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Zone("Dragonsand","Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Zone("HermitsIsle","Hermit's Isle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Zone("MistyIslands","Misty Islands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Zone("BootlegBay","Bootleg Bay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Zone("FreeHaven","Free Haven","Assets/Scenes/FreeHaven_SourceGrid.unity"),        new Zone("Blackshire","Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Zone("ParadiseValley","Paradise Valley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Zone("EelInfestedWaters","Eel Infested Waters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Zone("SilverCove","Silver Cove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Zone("FrozenHighlands","White Cap / Frozen Highlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Zone("Kriegspire","Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Zone("SweetWater","Sweet Water","Assets/Scenes/SweetWater_SourceGrid.unity")
    };

    const int N=128;
    const float Cell=4f;

    [MenuItem("MMUnity/Polish Blackshire Materials Only")]
    public static void ApplyBlackshireMaterialsOnly()
    {
        var z=Zones.First(x=>x.key=="Blackshire");
        var scene=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
                 ?? scene.GetRootGameObjects().FirstOrDefault();
        if(!root)throw new Exception("Blackshire root missing");
        var terrain=root.GetComponentInChildren<Terrain>(true);
        if(!terrain)throw new Exception("Blackshire terrain missing");
        string data="Assets/World/Blackshire/Data";
        byte[] tiles=File.ReadAllBytes(data+"/tilemap_u8.bin");
        byte[] groups=File.ReadAllBytes(data+"/tile_groups_u8.bin");
        byte[] sem=File.ReadAllBytes(data+"/tile_semantics_u8.bin");
        BlendTerrainMaterials(z,terrain,tiles,groups,sem);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene,z.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("WORLD_POLISH_BLACKSHIRE_MATERIALS_ONLY_DONE");
    }

    [MenuItem("MMUnity/Polish All Source Grid Regions")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh();
        FixSharedPlantMaterials();
        foreach(var z in Zones) ApplyZone(z);
        AssetDatabase.SaveAssets();
        Debug.Log("WORLD_POLISH_ALL_DONE zones="+Zones.Length);
    }

    static void ApplyZone(Zone z)
    {
        var scene=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
                 ?? scene.GetRootGameObjects().FirstOrDefault();
        if(!root) throw new Exception(z.display+" root missing");        var terrain=root.GetComponentInChildren<Terrain>(true);
        if(!terrain) throw new Exception(z.display+" terrain missing");
        string data=$"Assets/World/{z.key}/Data";
        byte[] heights=File.ReadAllBytes(data+"/heightmap_u8.bin");
        byte[] tiles=File.ReadAllBytes(data+"/tilemap_u8.bin");
        byte[] groups=File.ReadAllBytes(data+"/tile_groups_u8.bin");
        byte[] sem=File.ReadAllBytes(data+"/tile_semantics_u8.bin");

        PolishHeights(z,terrain,heights,tiles,groups,sem);
        BlendTerrainMaterials(z,terrain,tiles,groups,sem);
        int veg=PolishVegetation(z,root.transform,terrain,tiles,groups,sem);
        int obj=PolishObjects(z,root.transform,terrain);
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,z.scene)) throw new IOException("Could not save "+z.scene);
        Debug.Log($"WORLD_POLISH {z.display} vegetation={veg} objects={obj}");
    }

    static float Sample(byte[] a,float sx,float sy)
    {
        sx=Mathf.Clamp(sx,0,N-1); sy=Mathf.Clamp(sy,0,N-1);
        int x0=Mathf.FloorToInt(sx), y0=Mathf.FloorToInt(sy);
        int x1=Mathf.Min(N-1,x0+1), y1=Mathf.Min(N-1,y0+1);
        float tx=sx-x0, ty=sy-y0;
        float a0=Mathf.Lerp(a[y0*N+x0],a[y0*N+x1],tx);
        float a1=Mathf.Lerp(a[y1*N+x0],a[y1*N+x1],tx);
        return Mathf.Lerp(a0,a1,ty);
    }

    static Vector2 WorldToSource(float x,float z)
    {
        return new Vector2(Mathf.Clamp(x/Cell+64f,0,N-1),Mathf.Clamp(64f-z/Cell,0,N-1));
    }
    static byte FlagsAt(byte[] tiles,byte[] sem,float sx,float sy)
    {
        int x=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1), y=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        return sem[tiles[y*N+x]];
    }

    static byte GroupAt(byte[] tiles,byte[] groups,float sx,float sy)
    {
        int x=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1), y=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        return groups[tiles[y*N+x]];
    }

    static float SmoothSource(byte[] h,float sx,float sy)
    {
        float sum=0f,ws=0f;
        for(int oy=-2;oy<=2;oy++) for(int ox=-2;ox<=2;ox++)
        {
            float d2=ox*ox+oy*oy;
            float w=Mathf.Exp(-d2*.62f);
            sum+=Sample(h,sx+ox*.72f,sy+oy*.72f)*w;
            ws+=w;
        }
        return sum/Mathf.Max(.0001f,ws);
    }

    static float WaterDistanceCells(byte[] tiles,byte[] sem,float sx,float sy,int radius=4)
    {
        float best=999f;int cx=Mathf.RoundToInt(sx),cy=Mathf.RoundToInt(sy);
        for(int oy=-radius;oy<=radius;oy++)for(int ox=-radius;ox<=radius;ox++)
        {
            int x=Mathf.Clamp(cx+ox,0,N-1),y=Mathf.Clamp(cy+oy,0,N-1);
            if((sem[tiles[y*N+x]]&1)==0)continue;
            best=Mathf.Min(best,Mathf.Sqrt(ox*ox+oy*oy));
        }
        return best;
    }

    static void PolishHeights(Zone z,Terrain terrain,byte[] src,byte[] tiles,byte[] groups,byte[] sem)
    {
        var td=terrain.terrainData; int r=td.heightmapResolution;
        var next=new float[r,r]; Vector3 tp=terrain.transform.position,size=td.size;
        float ruggedBoost=(z.key=="Kriegspire"||z.key=="SweetWater"||z.key=="FrozenHighlands")?1.20f:1f;
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            float wx=tp.x+x/(float)(r-1)*size.x,wz=tp.z+y/(float)(r-1)*size.z;
            Vector2 q=WorldToSource(wx,wz); byte f=FlagsAt(tiles,sem,q.x,q.y);
            bool water=(f&1)!=0,road=(f&8)!=0,shore=(f&2)!=0;
            float center=Sample(src,q.x,q.y)*.25f;
            if(water){next[y,x]=Mathf.Clamp01((Mathf.Min(center,.10f)-tp.y)/size.y);continue;}
            float smooth=SmoothSource(src,q.x,q.y)*.25f;
            float mix=road?.08f:(shore?.30f:.34f);
            float target=Mathf.Lerp(center,smooth,mix);
            float high=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(10f,45f,center));
            float macro=(Mathf.PerlinNoise(q.x*.052f+13.7f,q.y*.049f+37.1f)-.5f)*.72f*high*ruggedBoost;
            float micro=(Mathf.PerlinNoise(q.x*.177f+91.3f,q.y*.191f+7.6f)-.5f)*.22f*high;
            target+=macro+micro;
            float wd=WaterDistanceCells(tiles,sem,q.x,q.y,4);
            if(!road&&wd<2.5f){float k=Mathf.Clamp01(1f-wd/2.5f);target=Mathf.Lerp(target,.28f,k*.36f);}
            float lim=road?.30f:(shore?.55f:1.05f);
            target=Mathf.Clamp(target,center-lim,center+lim);
            target=Mathf.Max(.24f,target);
            next[y,x]=Mathf.Clamp01((target-tp.y)/size.y);
        }
        td.SetHeights(0,0,next);terrain.Flush();EditorUtility.SetDirty(td);
        Debug.Log("WORLD_POLISH_HEIGHTS_SOURCE_FAITHFUL "+z.display+" res="+r);
    }

    static void CellWeights(float[] w,byte g,byte f)
    {
        if((f&8)!=0 || (g>=8&&g<255)){w[7]=1f;return;}
        if((f&1)!=0){w[8]=.58f;w[2]=.42f;return;}
        if((f&2)!=0 || g==5){w[2]=1f;return;}
        if(g==0){w[0]=1f;return;} if(g==1){w[1]=1f;return;}
        if(g==2){w[2]=1f;return;} if(g==3){w[3]=1f;return;}
        if(g==4){w[4]=1f;return;} if(g==6){w[5]=1f;return;}
        if(g==7){w[6]=1f;return;} w[4]=1f;
    }
    static void BlendTerrainMaterials(Zone z,Terrain terrain,byte[] tiles,byte[] groups,byte[] sem)
    {
        var td=terrain.terrainData; int r=td.alphamapResolution;
        var alpha=new float[r,r,td.terrainLayers.Length];
        for(int y=0;y<r;y++) for(int x=0;x<r;x++)
        {
            float sx=(x+.5f)/r*N;
            float sy=N-1-(y+.5f)/r*N;
            int cx=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1), cy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
            float[] sum=new float[td.terrainLayers.Length]; float ws=0f;
            for(int oy=-2;oy<=2;oy++) for(int ox=-2;ox<=2;ox++)
            {
                int ix=Mathf.Clamp(cx+ox,0,N-1), iy=Mathf.Clamp(cy+oy,0,N-1);
                float dx=sx-ix, dy=sy-iy, wt=Mathf.Exp(-(dx*dx+dy*dy)*.70f);
                float[] cw=new float[td.terrainLayers.Length];
                byte raw=tiles[iy*N+ix]; CellWeights(cw,groups[raw],sem[raw]);
                for(int k=0;k<sum.Length;k++) sum[k]+=cw[k]*wt;
                ws+=wt;
            }
            for(int k=0;k<sum.Length;k++) sum[k]/=Mathf.Max(.0001f,ws);
            byte cr=tiles[cy*N+cx], cf=sem[cr]; byte cg=groups[cr];
            float[] center=new float[sum.Length]; CellWeights(center,cg,cf);
            float centerBias=(cf&8)!=0 ? .72f : ((cf&1)!=0 ? .64f : .20f);
            for(int k=0;k<sum.Length;k++) sum[k]=Mathf.Lerp(sum[k],center[k],centerBias);
            float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));
            if((cf&9)==0 && sum.Length>8)
            {
                float rock=Mathf.Clamp01(Mathf.InverseLerp(29f,52f,steep))*.38f;
                for(int k=0;k<sum.Length;k++) sum[k]*=(1f-rock);
                sum[8]+=rock;
            }
            float total=sum.Sum();
            for(int k=0;k<sum.Length;k++) alpha[y,x,k]=sum[k]/Mathf.Max(.0001f,total);
        }
        td.SetAlphamaps(0,0,alpha); EditorUtility.SetDirty(td);
        Debug.Log("WORLD_POLISH_TRANSITIONS "+z.display+" alpha="+r);
    }
    static bool PlantName(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("tree")||n.Contains("pine")||n.Contains("cactus")||n.Contains("shrub")||n.Contains("fern");
    }

    static bool GeneratedPlant(string n)
    {
        n=n.ToLowerInvariant();
        return n.StartsWith("grovetree_")||n.StartsWith("forestfill")||n.StartsWith("wintertree_")||
               n.StartsWith("swampforest_")||n.StartsWith("desertcactus_")||n.StartsWith("desertshrub_")||
               n.StartsWith("shrub_")||n.StartsWith("fern_")||n.StartsWith("wateredge_");
    }

    static bool TreeName(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("tree")||n.Contains("pine");
    }

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0) return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); return b;
    }

    static bool NearArchitecture(Transform arch,float x,float z,float margin)
    {
        if(!arch) return false;
        foreach(var r in arch.GetComponentsInChildren<Renderer>(true))
        {
            Bounds b=r.bounds;
            if(x>=b.min.x-margin&&x<=b.max.x+margin&&z>=b.min.z-margin&&z<=b.max.z+margin)return true;
        }
        return false;
    }
    static float Hash01(string s,Vector3 p)
    {
        unchecked
        {
            int h=17; foreach(char c in s) h=h*31+c;
            h=h*31+Mathf.RoundToInt(p.x*10f); h=h*31+Mathf.RoundToInt(p.z*10f);
            uint x=(uint)h*747796405u+2891336453u;
            x=((x>>(int)((x>>28)+4))^x)*277803737u; x=(x>>22)^x;
            return (x&0x00FFFFFF)/16777215f;
        }
    }

    static bool PlantBlocked(Terrain terrain,Transform arch,byte[] tiles,byte[] groups,byte[] sem,Transform t)
    {
        Vector2 s=WorldToSource(t.position.x,t.position.z); byte f=FlagsAt(tiles,sem,s.x,s.y);
        if((f&1)!=0||(f&8)!=0) return true;
        float nx=(t.position.x-terrain.transform.position.x)/terrain.terrainData.size.x;
        float nz=(t.position.z-terrain.transform.position.z)/terrain.terrainData.size.z;
        if(nx<.005f||nz<.005f||nx>.995f||nz>.995f) return true;
        if(terrain.terrainData.GetSteepness(nx,nz)>41f) return true;
        if(NearArchitecture(arch,t.position.x,t.position.z,TreeName(t.name)?3.2f:1.6f)) return true;
        return false;
    }

    static void GroundPlant(Terrain terrain,Transform t)
    {
        Bounds b=BoundsOf(t.gameObject); if(b.size==Vector3.zero)return;
        float y=terrain.SampleHeight(new Vector3(t.position.x,0,t.position.z))+terrain.transform.position.y;
        t.position+=Vector3.up*(y-b.min.y);
        float yaw=t.eulerAngles.y; t.rotation=Quaternion.Euler(0,yaw,0);
    }

    static void ClampPlantSize(Transform t,byte group)
    {
        Bounds b=BoundsOf(t.gameObject); if(b.size.y<.01f)return;
        bool tree=TreeName(t.name), cactus=t.name.ToLowerInvariant().Contains("cactus");
        float h=Hash01(t.name,t.position);
        float target=tree?Mathf.Lerp(group==1?5.0f:4.8f,group==1?8.4f:9.3f,h):(cactus?Mathf.Lerp(1.4f,3.4f,h):Mathf.Lerp(.55f,2.1f,h));
        if(b.size.y>target*1.28f||b.size.y<target*.58f) t.localScale*=target/b.size.y;
    }
    static void AddPlantChildren(Transform container,List<Transform> dst)
    {
        if(!container)return;
        foreach(Transform c in container)
        {
            if(PlantName(c.name)) dst.Add(c);
            else foreach(Transform p in c) if(PlantName(p.name)) dst.Add(p);
        }
    }

    static int PolishVegetation(Zone z,Transform root,Terrain terrain,byte[] tiles,byte[] groups,byte[] sem)
    {
        var plants=new List<Transform>();
        AddPlantChildren(root.Find("Vegetation - MM Anchors + Natural Groves"),plants);
        AddPlantChildren(root.Find("Biome Vegetation - Source Grid"),plants);
        AddPlantChildren(root.Find("Vegetation - Preserved User Additions"),plants);
        plants=plants.Where(t=>t).Distinct().ToList();
        var treeAnchors=plants.Where(t=>t&&TreeName(t.name)&&!GeneratedPlant(t.name)).Select(t=>new Vector2(t.position.x,t.position.z)).ToList();
        var plantAnchors=plants.Where(t=>t&&!GeneratedPlant(t.name)).Select(t=>new Vector2(t.position.x,t.position.z)).ToList();
        var keptTrees=new Dictionary<long,List<Vector2>>(); int changed=0,removed=0;
        const float spacing=7.2f;
        Transform arch=root.Find("Architecture - MM6 Original Layout Expanded");
        foreach(var t in plants.OrderBy(t=>GeneratedPlant(t.name)?1:0).ThenBy(t=>t.name).ToList())
        {
            if(!t)continue; bool generated=GeneratedPlant(t.name);Vector3 p=t.position;Vector2 p2=new Vector2(p.x,p.z);
            Vector2 q=WorldToSource(p.x,p.z);byte g=GroupAt(tiles,groups,q.x,q.y);
            if(generated&&PlantBlocked(terrain,arch,tiles,groups,sem,t)){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;continue;}
            if(generated)
            {
                var anchors=TreeName(t.name)?treeAnchors:plantAnchors;
                float nearest=anchors.Count==0?999f:anchors.Min(v=>Vector2.Distance(v,p2));
                int local=anchors.Count(v=>Vector2.Distance(v,p2)<=42f);
                float radius=TreeName(t.name)?46f:28f;
                if(nearest>radius){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;continue;}
                float keep;
                if(TreeName(t.name)) keep=local>=5?.30f:local>=3?.20f:local>=2?.12f:.055f;
                else keep=local>=4?.42f:local>=2?.28f:.16f;
                if(g==1)keep*=.82f; else if(g==2)keep*=.72f; else if(g==7)keep*=1.15f; else if(g==3||g==6)keep*=.28f;
                if(Hash01(z.key+t.name,p)>keep){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;continue;}
            }
            if(TreeName(t.name))
            {
                int gx=Mathf.FloorToInt(p.x/spacing),gz=Mathf.FloorToInt(p.z/spacing);bool crowded=false;
                for(int dz=-1;dz<=1&&!crowded;dz++)for(int dx=-1;dx<=1&&!crowded;dx++)
                {
                    long k=((long)(gx+dx)<<32)^(uint)(gz+dz);if(!keptTrees.TryGetValue(k,out var list))continue;
                    foreach(var v in list)if(Vector2.Distance(v,p2)<spacing){crowded=true;break;}
                }
                if(crowded&&generated){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;continue;}
                long key=((long)gx<<32)^(uint)gz;if(!keptTrees.TryGetValue(key,out var l)){l=new List<Vector2>();keptTrees[key]=l;}l.Add(p2);
            }
            GroundPlant(terrain,t);ClampPlantSize(t,g);changed++;
        }
        Debug.Log($"WORLD_POLISH_VEGETATION_ANCHORED {z.display} anchors={treeAnchors.Count} kept={changed} removed={removed}");
        return changed;
    }
    static Material FallbackMaterial(bool wood)
    {
        string dir="Assets/Materials/SourceGridTerrain"; Directory.CreateDirectory(dir);
        string path=dir+(wood?"/ObjectFallbackWood.mat":"/ObjectFallbackStone.mat");
        var m=AssetDatabase.LoadAssetAtPath<Material>(path); var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} else m.shader=sh;
        string tex=wood?"Assets/Environment/PolyHaven/Textures/wood_planks/wood_planks_diff_1k.jpg":"Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png";
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f); EditorUtility.SetDirty(m); return m;
    }

    static bool WaterObject(string n)
    {
        n=n.ToLowerInvariant(); return n.Contains("bouy")||n.Contains("buoy")||n.Contains("ship")||n.Contains("shp");
    }

    static int PolishObjects(Zone z,Transform root,Terrain terrain)
    {
        int changed=0; var sprites=root.Find("Source Objects - OneToOne") ?? root.Find("Source Sprites - OneToOne");
        if(sprites) foreach(Transform t in sprites.Cast<Transform>().ToList())
        {
            var rs=t.GetComponentsInChildren<Renderer>(true); if(rs.Length==0)continue;
            bool proxy=rs.Any(r=>r.sharedMaterial && r.sharedMaterial.name.IndexOf("missing_proxy",StringComparison.OrdinalIgnoreCase)>=0);
            if(proxy){foreach(var r in rs)r.enabled=false;changed++;continue;}
            foreach(var r in rs) foreach(var m in r.sharedMaterials)
            {
                if(!m)continue; var sh=Shader.Find("Unlit/Transparent Cutout"); if(sh)m.shader=sh;
                if(m.HasProperty("_Cutoff"))m.SetFloat("_Cutoff",.08f); if(m.HasProperty("_Cull"))m.SetInt("_Cull",0); EditorUtility.SetDirty(m);
            }
            Bounds b=BoundsOf(t.gameObject); float y=WaterObject(t.name) ? .12f : terrain.SampleHeight(new Vector3(t.position.x,0,t.position.z))+terrain.transform.position.y;
            t.position+=Vector3.up*(y-b.min.y); changed++;
        }
        var arch=root.Find("Architecture - MM6 Original Layout Expanded");
        if(arch) foreach(var r in arch.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials; bool dirty=false; string n=r.gameObject.name.ToLowerInvariant();
            bool wood=n.Contains("bench")||n.Contains("sign")||n.Contains("dock")||n.Contains("barrel")||n.Contains("chest");
            for(int i=0;i<mats.Length;i++) if(!mats[i]||!mats[i].mainTexture){mats[i]=FallbackMaterial(wood);dirty=true;}
            if(dirty){r.sharedMaterials=mats;changed++;}
        }
        return changed;
    }
    static void FixSharedPlantMaterials()
    {
        string[] mats={
            "Assets/Environment/WinterVegetation/Materials/WinterTree5.mat",
            "Assets/Environment/WinterVegetation/Materials/WinterTree8.mat",
            "Assets/Materials/SourceGridTerrain/RealPineFoliage.mat",
            "Assets/Materials/SourceGridTerrain/RealPineBark.mat"
        };
        foreach(string p in mats)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(p); if(!m)continue;
            var sh=Shader.Find("Standard"); if(sh)m.shader=sh;
            bool cut=p.IndexOf("WinterTree",StringComparison.OrdinalIgnoreCase)>=0||p.IndexOf("Foliage",StringComparison.OrdinalIgnoreCase)>=0;
            if(cut)
            {
                if(m.HasProperty("_Mode"))m.SetFloat("_Mode",1f);
                if(m.HasProperty("_Cutoff"))m.SetFloat("_Cutoff",.32f);
                m.SetOverrideTag("RenderType","TransparentCutout"); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue=2450;
            }
            if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);
            if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.06f);
            EditorUtility.SetDirty(m);
        }
    }
}
