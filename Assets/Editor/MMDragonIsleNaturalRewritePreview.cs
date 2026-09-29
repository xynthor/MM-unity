using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMDragonIsleNaturalRewritePreview
{
    const string ScenePath="Assets/Scenes/__PREVIEW_DragonIsle_Geology.unity";
    const string TerrainPath="Assets/TempDragonRework/BASE_DragonIsleTerrain.asset";
    const string GroupName="Dragon Isle - Natural Rewrite PREVIEW";
    const string MatDir="Assets/TempDragonRework/Materials";

    static readonly string[] CliffPrefabs={
        "Assets/Art/Environment/Cliffs/SmallCliff_01/SmallCliff_A.prefab",
        "Assets/Art/Environment/Cliffs/RockSlussen_01/RockSlussen_01.prefab",
        "Assets/Art/Environment/Cliffs/FlatRock_01/FlatRock_01.prefab",
        "Assets/Art/Environment/Cliffs/Rock_06/Rock_06.prefab"
    };
    const string ShrubPath="Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx";
    const string ShrubMatPath="Assets/Materials/RealisticWorld/Shrub02.mat";

    static float G(float x,float z,float cx,float cz,float sx,float sz)
    {
        float dx=(x-cx)/sx,dz=(z-cz)/sz;
        return Mathf.Exp(-(dx*dx+dz*dz)*.5f);
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static bool Near(Bounds[] bs,float x,float z,float pad)
    {
        foreach(var b in bs)if(x>=b.min.x-pad&&x<=b.max.x+pad&&z>=b.min.z-pad&&z<=b.max.z+pad)return true;
        return false;
    }
    static float WorldH(Terrain t,float x,float z)=>t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;
    static float Slope(Terrain t,float x,float z)
    {
        var td=t.terrainData;Vector3 l=new Vector3(x,0,z)-t.transform.position;
        return td.GetSteepness(Mathf.Clamp01(l.x/td.size.x),Mathf.Clamp01(l.z/td.size.z));
    }
    static Material MakeCompatMaterial(int idx)
    {
        Directory.CreateDirectory(MatDir);
        string path=$"{MatDir}/DragonRock_{idx}.mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(existing)return existing;
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(CliffPrefabs[idx]);
        if(!src)throw new Exception("Missing rock prefab "+CliffPrefabs[idx]);
        var sm=src.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m);
        if(!sm)throw new Exception("Missing rock source material "+idx);
        var diffuse=sm.GetTexture("_BaseColorMap")??sm.GetTexture("_MainTex");
        var normal=sm.GetTexture("_BumpMap")??sm.GetTexture("_NormalMap");
        if(!diffuse)throw new Exception("Missing rock diffuse "+idx);
        var m=new Material(Shader.Find("Standard")){name="DragonRock_"+idx};
        m.SetTexture("_MainTex",diffuse);
        if(normal){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");}
        m.SetFloat("_Glossiness",.04f);m.SetFloat("_Metallic",0f);
        m.color=new Color(.82f,.80f,.76f,1f);
        AssetDatabase.CreateAsset(m,path);
        return m;
    }
    static GameObject SpawnRock(int idx,Transform parent,Terrain t,float x,float z,float yaw,float target,float sink)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(CliffPrefabs[idx]);
        if(!src)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        go.transform.SetParent(parent,false);
        var mat=MakeCompatMaterial(idx);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            int n=Mathf.Max(1,r.sharedMaterials.Length);
            r.sharedMaterials=Enumerable.Repeat(mat,n).ToArray();
        }
        go.transform.position=new Vector3(x,WorldH(t,x,z),z);
        go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var b=BoundsOf(go);float span=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(span<.01f){UnityEngine.Object.DestroyImmediate(go);return null;}
        go.transform.localScale*=target/span;
        b=BoundsOf(go);
        go.transform.position+=Vector3.up*(WorldH(t,x,z)-b.min.y-sink);
        return go;
    }
    static GameObject SpawnShrub(Transform parent,Terrain t,float x,float z,float yaw,float target)
    {
        var src=AssetDatabase.LoadAssetAtPath<GameObject>(ShrubPath);
        var mat=AssetDatabase.LoadAssetAtPath<Material>(ShrubMatPath);
        if(!src||!mat||!mat.mainTexture)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
        go.transform.SetParent(parent,false);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials=Enumerable.Repeat(mat,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
        go.transform.position=new Vector3(x,WorldH(t,x,z),z);
        go.transform.rotation=Quaternion.Euler(0,yaw,0);
        var b=BoundsOf(go);float span=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
        if(span>.01f)go.transform.localScale*=target/span;
        b=BoundsOf(go);go.transform.position+=Vector3.up*(WorldH(t,x,z)-b.min.y-.03f);
        return go;
    }
    static void SculptTerrain(Terrain t,Bounds[] protectedBounds,out float maxDelta)
    {
        var td=t.terrainData;int r=td.heightmapResolution;
        var h=td.GetHeights(0,0,r,r);maxDelta=0f;
        for(int iz=0;iz<r;iz++)for(int ix=0;ix<r;ix++)
        {
            float x=t.transform.position.x+ix/(float)(r-1)*td.size.x;
            float z=t.transform.position.z+iz/(float)(r-1)*td.size.z;
            float wh=t.transform.position.y+h[iz,ix]*td.size.y;
            if(wh<.75f||Near(protectedBounds,x,z,17f))continue;

            float delta=0f;
            // Off-centre island backbone: broad, asymmetrical, low-frequency form.
            delta+=2.20f*G(x,z,-35,-62,82,145);
            delta+=1.10f*G(x,z,-88,-118,45,60);
            delta+=0.75f*G(x,z,-78,75,42,55);
            // Two erosion/cove cuts.
            delta-=1.15f*G(x,z,77,-84,36,48);
            delta-=0.85f*G(x,z,-145,-18,31,45);
            // Secondary shelf breaks, not all-over noise.
            delta+=0.55f*G(x,z,-118,-72,25,34);
            delta-=0.42f*G(x,z,-102,-52,19,27);
            delta+=0.48f*G(x,z,-14,-138,24,33);
            delta-=0.36f*G(x,z,8,-122,18,24);

            float shore=Mathf.Clamp01((wh-.75f)/5.0f);
            shore=shore*shore*(3f-2f*shore);
            delta*=shore;
            if(Mathf.Abs(delta)<.005f)continue;
            h[iz,ix]=Mathf.Clamp01(h[iz,ix]+delta/td.size.y);
            maxDelta=Mathf.Max(maxDelta,Mathf.Abs(delta));
        }
        // One restrained smoothing pass to remove mathematical edges while preserving macro shapes.
        var copy=(float[,])h.Clone();
        for(int z=1;z<r-1;z++)for(int x=1;x<r-1;x++)
        {
            float wh=t.transform.position.y+h[z,x]*td.size.y;
            if(wh<1.0f)continue;
            float avg=(h[z,x]+h[z-1,x]+h[z+1,x]+h[z,x-1]+h[z,x+1])/5f;
            copy[z,x]=Mathf.Lerp(h[z,x],avg,.22f);
        }
        td.SetHeights(0,0,copy);t.Flush();EditorUtility.SetDirty(td);
    }
    static int LayerIndex(TerrainData td,string key)
    {
        for(int i=0;i<td.terrainLayers.Length;i++)
            if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf(key,StringComparison.OrdinalIgnoreCase)>=0)return i;
        return -1;
    }
    static void PaintTerrain(Terrain t,out double[] percentages)
    {
        var td=t.terrainData;
        int grass=LayerIndex(td,"Grass"),sand=LayerIndex(td,"Shore"),dirt=LayerIndex(td,"Dirt"),rock=LayerIndex(td,"Rock");
        if(grass<0||sand<0||dirt<0||rock<0)throw new Exception("Expected base terrain layers missing");
        int w=td.alphamapWidth,h=td.alphamapHeight,L=td.alphamapLayers;
        var a=new float[h,w,L];var sums=new double[L];
        for(int z=0;z<h;z++)for(int x=0;x<w;x++)
        {
            float nx=x/(float)(w-1),nz=z/(float)(h-1);
            float wx=t.transform.position.x+nx*td.size.x,wz=t.transform.position.z+nz*td.size.z;
            float y=WorldH(t,wx,wz),s=td.GetSteepness(nx,nz);
            float shore=Mathf.Clamp01(1f-(y-.55f)/5.6f);
            float steep=Mathf.Clamp01((s-9f)/25f);
            float high=Mathf.Clamp01((y-14f)/28f);
            // Low frequency variation only modulates ecological rules.
            float n=Mathf.PerlinNoise((wx+210f)*.010f,(wz-115f)*.010f);
            float shelter=Mathf.Clamp01(1f-steep)*(0.65f+0.35f*n);

            float ws=shore*shore*(1f-steep)*1.8f;
            float wg=(1f-shore)*shelter*(1f-high)*(.40f+.28f*n);
            float wr=.15f+1.45f*steep+.55f*high+.22f*(1f-shelter);
            float wd=.24f+(1f-steep)*(.30f+.26f*(1f-n))+.15f*high;
            float sum=ws+wg+wr+wd;
            a[z,x,sand]=ws/sum;a[z,x,grass]=wg/sum;a[z,x,rock]=wr/sum;a[z,x,dirt]=wd/sum;
            for(int k=0;k<L;k++)sums[k]+=a[z,x,k];
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);
        percentages=sums.Select(v=>v/(w*(double)h)*100d).ToArray();
    }
    static void FixForestMaterials(Transform forest)
    {
        if(!forest)return;
        var firDiff=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Models/fir_sapling/textures/fir_sapling_twigs_diff_1k.png");
        var firNorm=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Models/fir_sapling/textures/fir_sapling_twigs_nor_gl_1k.png");
        var searRGBA=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Models/searsia_lucida/textures/searsia_lucida_rgba_1k.png");
        var searNorm=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Models/searsia_lucida/textures/searsia_lucida_nor_gl_1k.exr");
        Directory.CreateDirectory(MatDir);
        Material Make(string name,Texture2D d,Texture2D n)
        {
            string p=$"{MatDir}/{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
            if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,p);}
            m.shader=Shader.Find("Standard");m.SetTexture("_MainTex",d);
            if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
            m.SetFloat("_Mode",1);m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.Zero);m.SetInt("_ZWrite",1);
            m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.36f);m.renderQueue=2450;
            m.SetFloat("_Glossiness",.02f);EditorUtility.SetDirty(m);return m;
        }
        var fir=Make("FirTwigs_Fixed",firDiff,firNorm);
        var sear=Make("SearsiaLeaves_Fixed",searRGBA,searNorm);
        foreach(var r in forest.GetComponentsInChildren<Renderer>(true))
        {
            var arr=r.sharedMaterials.ToArray();bool changed=false;
            for(int i=0;i<arr.Length;i++)
            {
                if(!arr[i])continue;
                if(arr[i].name.IndexOf("fir_sapling_twigs",StringComparison.OrdinalIgnoreCase)>=0){arr[i]=fir;changed=true;}
                if(arr[i].name.IndexOf("searsia_lucida_leaves",StringComparison.OrdinalIgnoreCase)>=0){arr[i]=sear;changed=true;}
            }
            if(changed)r.sharedMaterials=arr;
        }
    }
    static void BuildFormations(Terrain t,Transform parent,Bounds[] avoid,out int formations,out int rocks)
    {
        formations=rocks=0;
        // Hand-composed anchor points chosen from the island's actual height audit.
        var pts=new[]{
            new Vector2(-118,-118),new Vector2(-105,-58),new Vector2(-78,72),
            new Vector2(-26,-145),new Vector2(18,92),new Vector2(72,-82),
            new Vector2(-70,-18),new Vector2(-26,54),new Vector2(18,-58)
        };
        int id=0;
        foreach(var c in pts)
        {
            float cy=WorldH(t,c.x,c.y);if(cy<1.5f||Near(avoid,c.x,c.y,18f))continue;
            var g=new GameObject("Formation_"+id.ToString("00"));g.transform.SetParent(parent,false);
            float yaw=Mathf.Atan2(c.x,c.y)*Mathf.Rad2Deg+90f;
            int local=0;
            var main=SpawnRock(0,g.transform,t,c.x,c.y,yaw,13.5f,1.8f);
            if(main){main.name="Anchor";local++;}
            var offsets=new[]{new Vector2(-7,3),new Vector2(7,-2),new Vector2(-3,-6),new Vector2(5,6)};
            for(int j=0;j<offsets.Length;j++)
            {
                var q=c+offsets[j];float y=WorldH(t,q.x,q.y);
                if(y<1f)continue;
                int kind=1+(j%3);
                var r=SpawnRock(kind,g.transform,t,q.x,q.y,yaw+(j-2)*13f,4.2f+j*.55f,.45f);
                if(r){r.name="Support_"+j;local++;}
            }
            if(local>0){formations++;rocks+=local;id++;}
            else UnityEngine.Object.DestroyImmediate(g);
        }
    }
    static int BuildVegetationClusters(Terrain t,Transform parent,Bounds[] avoid)
    {
        var centers=new[]{new Vector2(-74,-94),new Vector2(-34,-26),new Vector2(-18,-118),new Vector2(16,22),new Vector2(-82,30)};
        int n=0;var rng=new System.Random(55331);
        foreach(var c in centers)
        {
            if(WorldH(t,c.x,c.y)<3f||Near(avoid,c.x,c.y,14f))continue;
            var g=new GameObject("VegetationPocket_"+n.ToString("00"));g.transform.SetParent(parent,false);
            for(int j=0;j<7;j++)
            {
                float ang=(float)rng.NextDouble()*Mathf.PI*2f,rad=2.5f+(float)rng.NextDouble()*8f;
                float x=c.x+Mathf.Cos(ang)*rad,z=c.y+Mathf.Sin(ang)*rad;
                if(WorldH(t,x,z)<2f||Slope(t,x,z)>22f||Near(avoid,x,z,9f))continue;
                var go=SpawnShrub(g.transform,t,x,z,(float)rng.NextDouble()*360f,.55f+(float)rng.NextDouble()*.65f);
                if(go){go.name="Shrub_"+j;n++;}
            }
        }
        return n;
    }
    [MenuItem("MMUnity/Dragon Isle/Build Natural Rewrite PREVIEW")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        if(!root)throw new Exception("Preview Dragon Isle root missing");
        var t=root.GetComponentInChildren<Terrain>(true);
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if(!t||!td)throw new Exception("Preview TerrainData missing");
        t.terrainData=td;var tc=t.GetComponent<TerrainCollider>();if(tc)tc.terrainData=td;

        foreach(string n in new[]{GroupName,"Dragon Isle - Geology Preview","Dragon Isle - Full Realism","Dragon Isle - Realism Additions"})
        {
            var old=root.transform.Find(n);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        }

        var landmarks=root.transform.Find("Landmarks - Reference");
        var avoid=landmarks?landmarks.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Select(r=>r.bounds).ToArray():new Bounds[0];

        SculptTerrain(t,avoid,out float maxDelta);
        PaintTerrain(t,out double[] pct);
        var forest=root.transform.Find("Forest - Natural Sparse");FixForestMaterials(forest);

        var group=new GameObject(GroupName);group.transform.SetParent(root.transform,false);
        var geo=new GameObject("Geological formations");geo.transform.SetParent(group.transform,false);
        var veg=new GameObject("Sheltered vegetation pockets");veg.transform.SetParent(group.transform,false);
        BuildFormations(t,geo.transform,avoid,out int formations,out int rocks);
        int shrubs=BuildVegetationClusters(t,veg.transform,avoid);

        int invalid=0,textureless=0;
        foreach(var r in group.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy))
        {
            if(r.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported||m.shader.name=="Hidden/InternalErrorShader"))invalid++;
            if(r.sharedMaterials.Any(m=>!m||m.GetTexture("_MainTex")==null))textureless++;
        }
        if(invalid>0||textureless>0)throw new Exception($"Preview material validation failed invalid={invalid} textureless={textureless}");

        EditorSceneManager.MarkSceneDirty(sc);
        if(!EditorSceneManager.SaveScene(sc,ScenePath))throw new IOException("Preview save failed");
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var lines=new List<string>{
            "Dragon Isle Natural Rewrite PREVIEW",
            $"max_height_delta_m={maxDelta:F3}",
            $"formations={formations}",
            $"formation_rock_roots={rocks}",
            $"new_shrubs={shrubs}",
            $"invalid_material_renderers={invalid}",
            $"textureless_renderers={textureless}"
        };
        for(int i=0;i<pct.Length;i++)lines.Add($"layer_{td.terrainLayers[i].name}_pct={pct[i]:F2}");
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_natural_rewrite_preview.txt",lines);
        Debug.Log($"DRAGON_NATURAL_REWRITE_PREVIEW formations={formations} rocks={rocks} shrubs={shrubs}");
    }
}
