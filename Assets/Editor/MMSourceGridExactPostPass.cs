using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMSourceGridExactPostPass
{
    sealed class Zone
    {
        public string key;
        public string display;
        public string scene;
        public Zone(string k,string d,string s){key=k;display=d;scene=s;}
    }

    static readonly Zone[] Zones = new[]
    {
        new Zone("NewSorpigal","New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Zone("CastleIronfist","Castle Ironfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Zone("MireOfTheDamned","Mire of the Damned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Zone("Dragonsand","Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Zone("HermitsIsle","Hermit's Isle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Zone("MistyIslands","Misty Islands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Zone("BootlegBay","Bootleg Bay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Zone("FreeHaven","Free Haven","Assets/Scenes/FreeHaven_SourceGrid.unity"),
        new Zone("Blackshire","Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Zone("ParadiseValley","Paradise Valley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Zone("EelInfestedWaters","Eel Infested Waters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Zone("SilverCove","Silver Cove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Zone("FrozenHighlands","White Cap / Frozen Highlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Zone("Kriegspire","Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Zone("SweetWater","Sweet Water","Assets/Scenes/SweetWater_SourceGrid.unity")
    };

    const int N=128;
    const string LayerFolder="Assets/Materials/SourceGridTerrain";
    const string SpriteTexFolder="Assets/Textures/MM6SpritesOriginal";
    const string SpriteMatFolder="Assets/Materials/MM6SpritesOriginal";
    const string SharedFolder="Assets/World/Shared";

    [MenuItem("MMUnity/Apply Exact Source Grid To All Regions")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory(LayerFolder);
        Directory.CreateDirectory(SpriteMatFolder);
        Directory.CreateDirectory(SharedFolder);
        AssetDatabase.Refresh();
        foreach(var z in Zones) ApplyZone(z);
        AssetDatabase.SaveAssets();
        Debug.Log("SOURCE_GRID_EXACT_ALL_DONE zones="+Zones.Length);
    }
    static void ApplyZone(Zone z)
    {
        if(!File.Exists(Path.GetFullPath(z.scene))) throw new FileNotFoundException(z.scene);
        var scene=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
                 ?? scene.GetRootGameObjects().FirstOrDefault();
        if(!root) throw new Exception(z.display+" root missing");
        var terrain=root.GetComponentInChildren<Terrain>(true);
        if(!terrain) throw new Exception(z.display+" terrain missing");

        string data=$"Assets/World/{z.key}/Data";
        byte[] tilemap=File.ReadAllBytes(data+"/tilemap_u8.bin");
        byte[] groups=File.ReadAllBytes(data+"/tile_groups_u8.bin");
        byte[] sem=File.ReadAllBytes(data+"/tile_semantics_u8.bin");
        ApplyExactTerrain(z,terrain,tilemap,groups,sem);
        int archMats=RepairSourceArchitectureMaterials(z,root.transform);
        int spriteCount=BuildExactSourceSprites(z,root.transform,terrain);
        int biomeCount=ApplyBiomeVegetation(z,root.transform,terrain,tilemap,groups);
        int treeMats=ApplyRealisticTreeMaterials(root.transform);
        int upright=FixSidewaysVegetation(root.transform,terrain);
        BuildOuterShoreExtensions(z,root.transform,terrain,tilemap,groups,sem);
        int modelSource=CountSourceModels(z);
        int modelScene=CountSceneModels(root.transform);
        if(modelSource!=modelScene)
            Debug.LogWarning($"SOURCE_MODEL_COUNT_MISMATCH {z.display} source={modelSource} scene={modelScene}");
        EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,z.scene)) throw new IOException("Could not save "+z.scene);
        Debug.Log($"SOURCE_GRID_EXACT {z.display} models={modelScene}/{modelSource} sourceSprites={spriteCount} biomePlants={biomeCount} treeMaterials={treeMats} architectureMaterials={archMats} uprightFixed={upright}");
    }

    [MenuItem("MMUnity/Rebuild Curved Outer Shores Only")]
    public static void RebuildCurvedOuterShoresOnly()
    {
        AssetDatabase.Refresh();
        foreach(var z in Zones)
        {
            var scene=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0) ?? scene.GetRootGameObjects().FirstOrDefault();
            if(!root) throw new Exception(z.display+" root missing");
            var terrain=root.GetComponentInChildren<Terrain>(true); if(!terrain) throw new Exception(z.display+" terrain missing");
            string data=$"Assets/World/{z.key}/Data";
            byte[] tilemap=File.ReadAllBytes(data+"/tilemap_u8.bin"),groups=File.ReadAllBytes(data+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(data+"/tile_semantics_u8.bin");
            BuildOuterShoreExtensions(z,root.transform,terrain,tilemap,groups,sem);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene,z.scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("CURVED_OUTER_SHORES_ALL_DONE zones="+Zones.Length);
    }
    static int RepairSourceArchitectureMaterials(Zone z,Transform root)
    {
        string folder=$"Assets/Textures/{z.key}/Buildings";
        if(!Directory.Exists(folder)) return 0;
        int changed=0;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials; bool dirty=false;
            for(int i=0;i<mats.Length;i++)
            {
                var m=mats[i]; if(!m || m.mainTexture) continue;
                string key=m.name.ToLowerInvariant().Replace(" (instance)","");
                string tex=$"{folder}/{key}_albedo.png";
                var t=AssetDatabase.LoadAssetAtPath<Texture2D>(tex); if(!t) continue;
                string mp=$"Assets/Materials/MMOriginal/{z.key}_{key}.mat";
                Directory.CreateDirectory("Assets/Materials/MMOriginal");
                var fixedMat=AssetDatabase.LoadAssetAtPath<Material>(mp);
                if(!fixedMat){fixedMat=new Material(Shader.Find("Standard"));fixedMat.name=z.key+"_"+key;AssetDatabase.CreateAsset(fixedMat,mp);}
                fixedMat.shader=Shader.Find("Standard"); fixedMat.mainTexture=t;
                var n=AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{key}_normal.png");
                if(n && fixedMat.HasProperty("_BumpMap")){fixedMat.SetTexture("_BumpMap",n);fixedMat.EnableKeyword("_NORMALMAP");}
                if(fixedMat.HasProperty("_Glossiness")) fixedMat.SetFloat("_Glossiness",.08f);
                EditorUtility.SetDirty(fixedMat); mats[i]=fixedMat; dirty=true; changed++;
            }
            if(dirty) r.sharedMaterials=mats;
        }
        return changed;
    }

    static TerrainLayer MakeLayer(string key,string texturePath,float tile)
    {
        string path=$"{LayerFolder}/{key}.terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
        l.name=key;
        l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        l.tileSize=new Vector2(tile,tile);
        l.tileOffset=Vector2.zero;
        l.metallic=0f;
        l.smoothness=key.Contains("Road")?0.18f:0.06f;
        EditorUtility.SetDirty(l);
        return l;
    }

    static TerrainLayer MakeGrassLayer(Zone z,Texture2D tex)
    {
        string path=$"{LayerFolder}/Grass_{z.key}.terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
        l.name="Grass_"+z.key;
        l.diffuseTexture=tex?tex:AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/Biomes/temperate_grass.png");
        l.tileSize=new Vector2(14f,14f);
        l.tileOffset=Vector2.zero;
        l.metallic=0f;l.smoothness=0.05f;
        EditorUtility.SetDirty(l);
        return l;
    }

    static void ApplyExactTerrain(Zone z,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem)
    {
        var td=terrain.terrainData;
        Texture2D oldGrass=null;
        if(td.terrainLayers!=null && td.terrainLayers.Length>0 && td.terrainLayers[0]) oldGrass=td.terrainLayers[0].diffuseTexture;
        var layers=new[]{
            MakeGrassLayer(z,oldGrass),
            MakeLayer("Snow","Assets/EnvironmentAssets/Biomes/snow.png",14f),
            MakeLayer("DesertSand","Assets/EnvironmentAssets/Biomes/sand.png",12f),
            MakeLayer("VolcanicAsh","Assets/EnvironmentAssets/Biomes/ash_ground.png",12f),
            MakeLayer("Dirt","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",11f),
            MakeLayer("BlackSoil","Assets/EnvironmentAssets/Biomes/black_soil.png",10f),
            MakeLayer("Swamp","Assets/EnvironmentAssets/Biomes/swamp_ground.png",11f),
            MakeLayer("Road","Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png",8f),
            MakeLayer("Rock","Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png",10f)
        };
        td.terrainLayers=layers;
        if(td.alphamapResolution!=512) td.alphamapResolution=512;
        int r=td.alphamapResolution;
        var alpha=new float[r,r,layers.Length];
        for(int y=0;y<r;y++)
        for(int x=0;x<r;x++)
        {
            int sx=Mathf.Clamp(Mathf.FloorToInt(((x+.5f)/r)*N),0,N-1);
            int sy=Mathf.Clamp(N-1-Mathf.FloorToInt(((y+.5f)/r)*N),0,N-1);
            byte raw=tilemap[sy*N+sx];
            byte g=groups[raw];
            byte f=sem[raw];
            PaintCell(alpha,y,x,g,f);
        }
        td.SetAlphamaps(0,0,alpha);
        EditorUtility.SetDirty(td);
    }

    static void PaintCell(float[,,] a,int y,int x,byte g,byte f)
    {
        // bit 3 is source road. Group >=8 is also a road tileset in MM6.
        if((f&8)!=0 || (g>=8 && g<255)){a[y,x,7]=1f;return;}
        // True water uses a rocky/sandy seabed, while shoreline stays sand.
        if((f&1)!=0){a[y,x,8]=0.62f;a[y,x,2]=0.38f;return;}
        if((f&2)!=0 || g==5){a[y,x,2]=1f;return;}
        if(g==0){a[y,x,0]=1f;return;}
        if(g==1){a[y,x,1]=1f;return;}
        if(g==2){a[y,x,2]=1f;return;}
        if(g==3){a[y,x,3]=1f;return;}
        if(g==4){a[y,x,4]=1f;return;}
        if(g==6){a[y,x,5]=1f;return;}
        if(g==7){a[y,x,6]=1f;return;}
        // Unmapped/special terrain keeps a neutral dirt treatment.
        a[y,x,4]=1f;
    }

    static bool IsStartMarker(string n)
    {
        n=n.Trim().ToLowerInvariant();
        return n=="party start" || n.EndsWith(" start");
    }
    static bool IsBuilder3DAnchor(string n)
    {
        n=n.ToLowerInvariant();
        return n.StartsWith("6tree") || n.StartsWith("tree") || n.StartsWith("6rock");
    }

    static bool TryFloat(string s,out float v)
    {
        return float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    }

    static float GroundY(string name,float terrainY,float sourceY)
    {
        if(name.StartsWith("bouy") || name.StartsWith("shp")) return 0.05f;
        return Mathf.Max(terrainY,sourceY);
    }

    static int BuildExactSourceSprites(Zone z,Transform root,Terrain terrain)
    {
        var old=root.Find("Source Sprites - OneToOne");
        if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var group=new GameObject("Source Sprites - OneToOne");
        group.transform.SetParent(root,false);
        string path=$"Assets/World/{z.key}/Data/decorations.csv";
        string[] lines=File.ReadAllLines(path);
        int made=0;
        for(int i=1;i<lines.Length;i++)
        {
            string[] q=lines[i].Split(',');
            if(q.Length<10) continue;
            string name=q[1].Trim().ToLowerInvariant();
            if(string.IsNullOrEmpty(name) || IsStartMarker(name) || IsBuilder3DAnchor(name)) continue;
            if(!TryFloat(q[4],out float ox) || !TryFloat(q[5],out float oy) || !TryFloat(q[6],out float oz)) continue;
            TryFloat(q[7],out float yaw);
            float x=ox,zp=-oz;
            float ground=terrain.SampleHeight(new Vector3(x,0f,zp))+terrain.transform.position.y;
            var go=CreateSourceVisual(name,i,group.transform);
            if(!go) continue;
            go.transform.position=new Vector3(x,GroundY(name,ground,oy),zp);
            if(!go.GetComponent<MMBillboardToCamera>()) go.transform.rotation=Quaternion.Euler(0f,-yaw,0f);
            made++;
        }
        return made;
    }
    static GameObject CreateSourceVisual(string name,int index,Transform parent)
    {
        string texPath=$"{SpriteTexFolder}/{name}.png";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if(tex) return CreateBillboard(name,index,parent,tex);
        if(name.StartsWith("swptree")) return CreatePrefabProxy(name,index,parent,"Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab",6.0f);
        if(name.StartsWith("snotre")) return CreatePrefabProxy(name,index,parent,"Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX",6.5f);
        if(name.Contains("rock")) return CreatePrefabProxy(name,index,parent,"Assets/EnvironmentAssets/Gobkit/Rock001.fbx",1.2f);
        return CreatePrimitiveProxy(name,index,parent);
    }

    static GameObject CreateBillboard(string name,int index,Transform parent,Texture2D tex)
    {
        var go=new GameObject($"SRC_{index:000}_{name}");
        go.transform.SetParent(parent,false);
        var mf=go.AddComponent<MeshFilter>();
        mf.sharedMesh=SourceQuad();
        var mr=go.AddComponent<MeshRenderer>();
        mr.sharedMaterial=SpriteMaterial(name,tex);
        mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows=false;
        float h=Mathf.Clamp(tex.height/32f,0.35f,11f);
        float w=h*Mathf.Clamp(tex.width/(float)Mathf.Max(1,tex.height),0.18f,2.5f);
        go.transform.localScale=new Vector3(w,h,1f);
        go.AddComponent<MMBillboardToCamera>();
        return go;
    }
    static Mesh SourceQuad()
    {
        string path=$"{SharedFolder}/MM6_SourceSpriteQuad.asset";
        var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(m) return m;
        m=new Mesh{name="MM6_SourceSpriteQuad"};
        m.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(.5f,1,0),new Vector3(-.5f,1,0)};
        m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
        m.triangles=new[]{0,2,1,0,3,2};
        m.RecalculateNormals();m.RecalculateBounds();
        AssetDatabase.CreateAsset(m,path);
        return m;
    }

    static Material SpriteMaterial(string name,Texture2D tex)
    {
        string path=$"{SpriteMatFolder}/{name}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader sh=Shader.Find("Unlit/Transparent Cutout");
        if(!sh) sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}
        m.shader=sh;
        if(m.HasProperty("_MainTex")) m.SetTexture("_MainTex",tex);
        if(m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff",0.05f);
        if(m.HasProperty("_Cull")) m.SetInt("_Cull",0);
        EditorUtility.SetDirty(m);
        return m;
    }
    static GameObject CreatePrefabProxy(string name,int index,Transform parent,string assetPath,float targetHeight)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if(!prefab) return CreatePrimitiveProxy(name,index,parent);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name=$"SRC_{index:000}_{name}";
        go.transform.SetParent(parent,false);
        ScaleToHeight(go,targetHeight);
        ApplyCactusMaterials(go);
        KeepOneCactusVariant(go,index);
        ApplyDesertShrubMaterials(go);
        KeepOneDesertShrubVariant(go,index);
        ApplyRealisticTreeMaterials(go.transform);
        return go;
    }

    static GameObject CreatePrimitiveProxy(string name,int index,Transform parent)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name=$"SRC_{index:000}_{name}";
        go.transform.SetParent(parent,false);
        go.transform.localScale=new Vector3(.18f,.35f,.18f);
        var c=go.GetComponent<Collider>(); if(c) UnityEngine.Object.DestroyImmediate(c);
        var r=go.GetComponent<Renderer>();
        if(r) r.sharedMaterial=GenericSpriteProxyMaterial();
        return go;
    }

    static Material GenericSpriteProxyMaterial()
    {
        string path=$"{SpriteMatFolder}/_missing_proxy.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));m.color=new Color(.48f,.40f,.24f);AssetDatabase.CreateAsset(m,path);}
        return m;
    }
    static void ScaleToHeight(GameObject go,float target)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0) return;
        Bounds b=rs[0].bounds;
        for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);
        if(b.size.y<0.001f) return;
        go.transform.localScale*=target/b.size.y;
    }


    static byte GroupAtWorld(byte[] tilemap,byte[] groups,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,N-1);
        int sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);
        return groups[tilemap[sy*N+sx]];
    }

    static string SourceAssetPath(GameObject go)
    {
        var src=PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
        return src?AssetDatabase.GetAssetPath(src):string.Empty;
    }

    static bool BadVegetationAsset(GameObject go)
    {
        string p=SourceAssetPath(go).ToLowerInvariant();
        string n=go.name.ToLowerInvariant();
        return p.Contains("banyantree") || p.Contains("treesmall02") || p.Contains("tree_small_02") || p.Contains("dead_tree_trunk_02") ||
               n.Contains("banyan") || n.Contains("tree_small_02") || n.Contains("treesmall02") || n.Contains("dead_tree_trunk") ||
               p.Contains("treehigh001") || p.Contains("treehigh002") || p.Contains("treehigh003") || p.Contains("pine_005") ||
               n.Contains("treehigh001") || n.Contains("treehigh002") || n.Contains("treehigh003") ||
               p.Contains("desertvegetation/desert_shrubs") || p.Contains("wÃ¼stenstrauch") || p.Contains("wï¿½stenstrauch");
    }

    static int CactusIndex(string s)
    {
        if(string.IsNullOrEmpty(s)) return 0;
        s=s.ToLowerInvariant();
        for(int i=1;i<=9;i++) if(s.Contains("cactus_"+i)) return i;
        return 0;
    }

    static Material CactusMaterial(int i)
    {
        string dir="Assets/Environment/DesertVegetation/Materials";
        Directory.CreateDirectory(dir);
        string path=$"{dir}/Cactus_{i}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} else m.shader=sh;
        string td=$"Assets/Environment/DesertVegetation/Textures/cactus_{i}__pbrs2a_diffuse.png";
        string tn=$"Assets/Environment/DesertVegetation/Textures/cactus_{i}__pbrs2a_normal.png";
        var d=AssetDatabase.LoadAssetAtPath<Texture2D>(td); var n=AssetDatabase.LoadAssetAtPath<Texture2D>(tn);
        if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",d);
        if(n && m.HasProperty("_BumpMap")){m.SetTexture("_BumpMap",n);m.SetFloat("_BumpScale",1f);m.EnableKeyword("_NORMALMAP");}
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.12f);
        EditorUtility.SetDirty(m); return m;
    }

    static void ApplyCactusMaterials(GameObject go)
    {
        if(!go) return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;
            for(int k=0;k<a.Length;k++)
            {
                int i=CactusIndex(a[k]?a[k].name:r.gameObject.name);
                if(i==0)i=CactusIndex(r.gameObject.name);
                if(i>0)a[k]=CactusMaterial(i);
            }
            r.sharedMaterials=a;
        }
    }

    static void KeepOneCactusVariant(GameObject go,int seed)
    {
        if(!go) return;
        var rs=go.GetComponentsInChildren<Renderer>(true)
            .Where(r=>CactusIndex(r.gameObject.name)>0)
            .OrderBy(r=>CactusIndex(r.gameObject.name)).ToArray();
        if(rs.Length==0) return;
        int keep=Mathf.Abs(seed)%rs.Length;
        for(int i=0;i<rs.Length;i++) rs[i].gameObject.SetActive(i==keep);
    }

    static string DesertShrubStem(string name)
    {
        if(string.IsNullOrEmpty(name)) return null; string n=name.ToLowerInvariant().Replace(" ","_");
        if(n.Contains("agave")) return "agave"; if(n.Contains("yacca_leaf_01")||n.Contains("yucca_leaf_01")) return "yacca_leaf_01";
        if(n.Contains("yacca_leaf_02")||n.Contains("yucca_leaf_02")) return "yacca_leaf_02"; if(n.Contains("ocotillo_branch_01")) return "ocotillo_branch_01";
        if(n.Contains("ocotillo_branch_02")) return "ocotillo_branch_02"; if(n.Contains("creosote_branch_02")) return "creosote_branch_02";
        if(n.Contains("cholla_01")) return "cholla_01"; if(n.Contains("cholla_02")) return "cholla_02"; if(n.Contains("bark_02")) return "bark_02";
        if(n.Contains("bark_1")||n=="bark") return "bark"; return null;
    }

    static Texture2D FindDesertTexture(string stem,bool normal)
    {
        string folder="Assets/Environment/DesertVegetation/DesertShrubsTextures";
        foreach(string g in AssetDatabase.FindAssets("t:Texture2D",new[]{folder}))
        {
            string p=AssetDatabase.GUIDToAssetPath(g),n=Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
            if(!n.StartsWith(stem.ToLowerInvariant())) continue; bool isNormal=n.Contains("normal");
            if(normal==isNormal && (!normal || isNormal)) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        return null;
    }

    static Material DesertShrubMaterial(string stem)
    {
        string dir="Assets/Environment/DesertVegetation/Materials";Directory.CreateDirectory(dir);string path=$"{dir}/Desert_{stem}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        var d=FindDesertTexture(stem,false);var n=FindDesertTexture(stem,true);if(d)m.SetTexture("_MainTex",d);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        m.SetFloat("_Glossiness",.08f);if(d && d.format!=TextureFormat.RGB24){m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.35f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
        EditorUtility.SetDirty(m);return m;
    }

    static void KeepOneDesertShrubVariant(GameObject go,int seed)
    {
        if(!go)return;var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>DesertShrubStem(r.gameObject.name)!=null||DesertShrubStem(r.sharedMaterial?r.sharedMaterial.name:null)!=null).OrderBy(r=>r.gameObject.name).ToArray();
        if(rs.Length==0)return;int keep=Mathf.Abs(seed)%rs.Length;for(int i=0;i<rs.Length;i++)rs[i].enabled=(i==keep);
    }

    static void ApplyDesertShrubMaterials(GameObject go)
    {
        if(!go)return;foreach(var r in go.GetComponentsInChildren<Renderer>(true)){string stem=DesertShrubStem(r.gameObject.name);if(stem==null&&r.sharedMaterial)stem=DesertShrubStem(r.sharedMaterial.name);if(stem==null)continue;var a=r.sharedMaterials;for(int i=0;i<a.Length;i++)a[i]=DesertShrubMaterial(stem);r.sharedMaterials=a;}
    }

    static Material WinterTreeMaterial(int id)
    {
        string dir="Assets/Environment/WinterVegetation/Materials";Directory.CreateDirectory(dir);string path=$"{dir}/WinterTree{id}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        string tex=$"Assets/Environment/WinterVegetation/WinterTree{id}/winter-tree{id}.png";m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));m.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(m);return m;
    }

    static void ApplyWinterTreeMaterial(GameObject go,int id)
    {
        if(!go)return;var m=WinterTreeMaterial(id);foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length==0)a=new Material[1];for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;}
    }
    static bool NearArchitecture(Transform arch,float x,float z,float margin)
    {
        if(!arch) return false;
        foreach(var r in arch.GetComponentsInChildren<Renderer>(true))
        {
            Bounds b=r.bounds;
            if(x>=b.min.x-margin&&x<=b.max.x+margin&&z>=b.min.z-margin&&z<=b.max.z+margin) return true;
        }
        return false;
    }

    static GameObject ReplacePlant(GameObject old,GameObject prefab,Transform parent,Terrain terrain)
    {
        if(!prefab) return old;
        Vector3 p=old.transform.position;
        Quaternion q=old.transform.rotation;
        string n=old.name;
        UnityEngine.Object.DestroyImmediate(old);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name=n;
        go.transform.SetParent(parent,true);
        float y=terrain.SampleHeight(new Vector3(p.x,0f,p.z))+terrain.transform.position.y;
        go.transform.position=new Vector3(p.x,y,p.z);
        go.transform.rotation=Quaternion.Euler(0f,q.eulerAngles.y,0f);
        go.transform.localScale=Vector3.one;
        KeepOneCactusVariant(go,n.GetHashCode());
        KeepOneDesertShrubVariant(go,n.GetHashCode());
        ApplyCactusMaterials(go);
        ApplyDesertShrubMaterials(go);
        string srcPath=SourceAssetPath(go).ToLowerInvariant();
        if(srcPath.Contains("winter-tree5"))ApplyWinterTreeMaterial(go,5);
        else if(srcPath.Contains("winter-tree8"))ApplyWinterTreeMaterial(go,8);
        ApplyRealisticTreeMaterials(go.transform);
        return go;
    }

    static int ApplyBiomeVegetation(Zone z,Transform root,Terrain terrain,byte[] tilemap,byte[] groups)
    {
        var veg=root.Find("Vegetation - MM Anchors + Natural Groves");
        if(!veg) return 0;
        var trees=veg.Find("Trees");
        var cactus=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/DesertVegetation/Cactus.fbx");
        var desertShrub=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/DesertVegetation/desert_shrubs.fbx");
        var pine=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab");
        var winter5=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/WinterVegetation/WinterTree5/winter-tree5_LOW_RES.fbx");
        var winter8=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/WinterVegetation/WinterTree8/winter-tree8_LOW_RES.fbx");
        int changed=0;

        // Global cleanup, including preserved/manual vegetation.
        foreach(var t in root.GetComponentsInChildren<Transform>(true).ToList())
        {
            if(!t || t==root || !BadVegetationAsset(t.gameObject)) continue;
            byte g=GroupAtWorld(tilemap,groups,t.position.x,t.position.z);
            Transform parent=t.parent?t.parent:root;
            if(g==2) ReplacePlant(t.gameObject,((changed&1)==0?cactus:desertShrub),parent,terrain);
            else if(g==1 && (winter5||winter8)) ReplacePlant(t.gameObject,((changed&1)==0&&winter5)?winter5:(winter8?winter8:winter5),parent,terrain);
            else if(g==3||g==6) UnityEngine.Object.DestroyImmediate(t.gameObject);
            else ReplacePlant(t.gameObject,pine,parent,terrain);
            changed++;
        }
        if(trees)
        {
            var pineA=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.prefab");
            var pineB=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_5.prefab");
            var pineC=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_02.prefab");
            var pineD=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_007/pine_007_01.prefab");
            var realTrees=new[]{pineA,pineB,pineC,pineD}.Where(x=>x).ToArray();
            foreach(var t in trees.Cast<Transform>().ToList())
            {
                var go=t.gameObject;
                byte g=GroupAtWorld(tilemap,groups,t.position.x,t.position.z);
                int h=Mathf.RoundToInt(t.position.x*31f+t.position.z*17f) ^ z.key.GetHashCode();
                if(g==2)
                {
                    var dp=((h&1)==0?cactus:desertShrub); if(!dp) dp=cactus?cactus:desertShrub;
                    var repl=ReplacePlant(go,dp,trees,terrain);
                    if(repl){KeepOneCactusVariant(repl,h);KeepOneDesertShrubVariant(repl,h);ApplyCactusMaterials(repl);ApplyDesertShrubMaterials(repl);}
                    changed++; continue;
                }
                if(g==1 && (winter5||winter8))
                {
                    var wp=((h&1)==0&&winter5)?winter5:(winter8?winter8:winter5);
                    var repl=ReplacePlant(go,wp,trees,terrain);
                    if(repl){ApplyWinterTreeMaterial(repl,wp==winter8?8:5);ScaleToHeight(repl,Mathf.Lerp(5.5f,10.5f,(Mathf.Abs(h>>7)%1000)/999f));}
                    changed++; continue;
                }
                if(g==3||g==6)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    changed++; continue;
                }
                if(realTrees.Length>0)
                {
                    var repl=ReplacePlant(go,realTrees[Mathf.Abs(h)%realTrees.Length],trees,terrain);
                    if(repl) ScaleToHeight(repl,Mathf.Lerp(4.8f,9.2f,(Mathf.Abs(h>>8)%1000)/999f));
                    changed++;
                }
            }
        }

        var old=root.Find("Biome Vegetation - Source Grid");
        if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        var biomeRoot=new GameObject("Biome Vegetation - Source Grid");
        biomeRoot.transform.SetParent(root,false);
        Transform arch=root.Find("Architecture - MM6 Original Layout Expanded");
        if(cactus||desertShrub)
        {
            for(int sy=1;sy<N-1;sy++) for(int sx=1;sx<N-1;sx++)
            {
                byte raw=tilemap[sy*N+sx]; if(groups[raw]!=2) continue;
                int h=(sx*73856093)^(sy*19349663)^(z.key.Length*83492791);
                if((h&0x7fffffff)%11!=0) continue;
                float jx=(((h>>4)&255)/255f-.5f)*2.4f;
                float jz=(((h>>12)&255)/255f-.5f)*2.4f;
                float x=(sx-64f)*4f+jx, zp=(64f-sy)*4f+jz;
                float nx=(x-terrain.transform.position.x)/terrain.terrainData.size.x;
                float nz=(zp-terrain.transform.position.z)/terrain.terrainData.size.z;
                if(nx<.01f||nz<.01f||nx>.99f||nz>.99f) continue;
                if(terrain.terrainData.GetSteepness(nx,nz)>32f || NearArchitecture(arch,x,zp,3.2f)) continue;
                var prefab=((h&2)==0?cactus:desertShrub); if(!prefab) prefab=cactus?cactus:desertShrub;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name=((prefab==cactus)?"DesertCactus_":"DesertShrub_")+sx+"_"+sy;
                go.transform.SetParent(biomeRoot.transform,true);
                float y=terrain.SampleHeight(new Vector3(x,0f,zp))+terrain.transform.position.y;
                go.transform.position=new Vector3(x,y,zp);
                go.transform.rotation=Quaternion.Euler(0f,(h&1023)*.3519f,0f);
                go.transform.localScale=Vector3.one;
                KeepOneCactusVariant(go,h);
                KeepOneDesertShrubVariant(go,h);
                ApplyCactusMaterials(go);
                ApplyDesertShrubMaterials(go);
                ApplyRealisticTreeMaterials(go.transform);
                changed++;
            }
        }
        if(winter5||winter8)
        {
            for(int sy=1;sy<N-1;sy++) for(int sx=1;sx<N-1;sx++)
            {
                byte raw=tilemap[sy*N+sx]; if(groups[raw]!=1) continue;
                int h=(sx*83492791)^(sy*297121507)^z.key.GetHashCode(); if((h&0x7fffffff)%17!=0) continue;
                float x=(sx-64f)*4f+((((h>>5)&255)/255f-.5f)*2.8f); float zp=(64f-sy)*4f+((((h>>13)&255)/255f-.5f)*2.8f);
                float nx=(x-terrain.transform.position.x)/terrain.terrainData.size.x,nz=(zp-terrain.transform.position.z)/terrain.terrainData.size.z;
                if(nx<.01f||nz<.01f||nx>.99f||nz>.99f||terrain.terrainData.GetSteepness(nx,nz)>38f||NearArchitecture(arch,x,zp,4f))continue;
                var wp=((h&1)==0&&winter5)?winter5:(winter8?winter8:winter5);var go=(GameObject)PrefabUtility.InstantiatePrefab(wp);go.name="WinterTree_"+sx+"_"+sy;go.transform.SetParent(biomeRoot.transform,true);
                float y=terrain.SampleHeight(new Vector3(x,0f,zp))+terrain.transform.position.y;go.transform.position=new Vector3(x,y,zp);go.transform.rotation=Quaternion.Euler(0f,(h&1023)*.3519f,0f);ApplyWinterTreeMaterial(go,wp==winter8?8:5);ScaleToHeight(go,Mathf.Lerp(5.4f,10.8f,(Mathf.Abs(h>>9)%1000)/999f));changed++;
            }
        }
        if(z.key=="MireOfTheDamned")
        {
            var swampA=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_5.prefab");
            var swampB=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_02.prefab");
            var swampC=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_007/pine_007_01.prefab");
            var swampTrees=new[]{swampA,swampB,swampC}.Where(x=>x).ToArray();
            if(swampTrees.Length>0)
            for(int sy=1;sy<N-1;sy++) for(int sx=1;sx<N-1;sx++)
            {
                byte raw=tilemap[sy*N+sx]; if(groups[raw]!=7) continue;
                int h=(sx*73856093)^(sy*19349663)^0x5A17; if((h&0x7fffffff)%9!=0) continue;
                float x=(sx-64f)*4f+((((h>>5)&255)/255f-.5f)*3.0f);
                float zp=(64f-sy)*4f+((((h>>13)&255)/255f-.5f)*3.0f);
                float nx=(x-terrain.transform.position.x)/terrain.terrainData.size.x;
                float nz=(zp-terrain.transform.position.z)/terrain.terrainData.size.z;
                if(nx<.01f||nz<.01f||nx>.99f||nz>.99f) continue;
                if(terrain.terrainData.GetSteepness(nx,nz)>36f||NearArchitecture(arch,x,zp,4.0f)) continue;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(swampTrees[Mathf.Abs(h)%swampTrees.Length]);
                go.name="SwampForest_"+sx+"_"+sy;
                go.transform.SetParent(biomeRoot.transform,true);
                float y=terrain.SampleHeight(new Vector3(x,0f,zp))+terrain.transform.position.y;
                go.transform.position=new Vector3(x,y,zp);
                go.transform.rotation=Quaternion.Euler(0f,(h&1023)*.3519f,0f);
                ScaleToHeight(go,Mathf.Lerp(5.2f,9.5f,(Mathf.Abs(h>>9)%1000)/999f));
                ApplyRealisticTreeMaterials(go.transform);
                changed++;
            }
        }
        return changed;
    }

    static int FixSidewaysVegetation(Transform root,Terrain terrain)
    {
        int changed=0;
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n=t.name.ToLowerInvariant();
            if(!(n.StartsWith("grovetree_")||n.StartsWith("desertcactus_")||n.StartsWith("desertshrub_")||n.StartsWith("src_"))) continue;
            if(!(n.Contains("tree")||n.Contains("pine")||n.Contains("cactus")||n.Contains("shrub"))) continue;
            if(Vector3.Angle(t.up,Vector3.up)<=35f) continue;
            float yaw=t.eulerAngles.y;
            t.rotation=Quaternion.Euler(0f,yaw,0f);
            float y=terrain.SampleHeight(new Vector3(t.position.x,0f,t.position.z))+terrain.transform.position.y;
            t.position=new Vector3(t.position.x,y,t.position.z);
            changed++;
        }
        return changed;
    }

    static Material RealTreeMaterial(bool foliage)
    {
        string key=foliage?"RealPineFoliage":"RealPineBark";
        string path=$"{LayerFolder}/{key}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} else m.shader=sh;
        string baseDir="Assets/Environment/PolyHaven/Models/pine_sapling_small/";
        string d=baseDir+(foliage?"pine_sapling_small_twig_diff_1k.png":"pine_sapling_small_bark_diff_1k.png");
        string n=baseDir+(foliage?"pine_sapling_small_twig_nor_gl_1k.png":"pine_sapling_small_bark_nor_gl_1k.png");
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(d));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(n)); m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_Glossiness",foliage?.08f:.12f);
        if(foliage){m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.35f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
        EditorUtility.SetDirty(m);return m;
    }
    static int ApplyRealisticTreeMaterials(Transform root)
    {
        var bark=RealTreeMaterial(false);var foliage=RealTreeMaterial(true);int changed=0;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;bool dirty=false;
            for(int i=0;i<a.Length;i++)
            {
                var m=a[i];if(!m||m.mainTexture)continue;
                string n=m.name.ToLowerInvariant();
                bool leaf=n.Contains("leaf")||n.Contains("twig")||n.Contains("needle")||n.Contains("billboard")||n.Contains("branch");
                bool wood=n.Contains("bark")||n.Contains("trunk")||n.Contains("wood")||n.Contains("stump");
                if(leaf){a[i]=foliage;dirty=true;changed++;}
                else if(wood){a[i]=bark;dirty=true;changed++;}
            }
            if(dirty)r.sharedMaterials=a;
        }
        return changed;
    }

    static Material SurfaceMaterial(string key,string texturePath,bool water=false)
    {
        string path=$"{LayerFolder}/{key}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader sh=water?Shader.Find("MMUnity/DepthWater"):Shader.Find("Standard");
        if(!sh) sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);} m.shader=sh;
        if(water){m.color=new Color(.04f,.28f,.38f,.88f);m.renderQueue=3000;}
        else {m.color=Color.white;if(m.HasProperty("_MainTex"))m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.05f);}
        EditorUtility.SetDirty(m); return m;
    }

    static Mesh SaveMeshAsset(string path,List<Vector3> verts,List<int> tris,List<Vector2> uvs)
    {
        var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);}m.Clear();
        m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;m.SetVertices(verts);m.SetTriangles(tris,0);m.SetUVs(0,uvs);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);return m;
    }

    enum OuterEdge { North, South, West, East }

    static void EdgeSample(OuterEdge edge,float along,out float x,out float z)
    {
        x=(edge==OuterEdge.West)?-255.5f:(edge==OuterEdge.East)?255.5f:Mathf.Clamp(along,-255.5f,255.5f);
        z=(edge==OuterEdge.South)?-255.5f:(edge==OuterEdge.North)?255.5f:Mathf.Clamp(along,-255.5f,255.5f);
    }

    static void EdgePoint(OuterEdge edge,float along,float d,out float x,out float z)
    {
        if(edge==OuterEdge.North){x=along;z=256f+d;return;}
        if(edge==OuterEdge.South){x=along;z=-256f-d;return;}
        if(edge==OuterEdge.West){x=-256f-d;z=along;return;}
        x=256f+d;z=along;
    }

    static bool EdgeLand(byte[] tilemap,byte[] groups,byte[] sem,OuterEdge edge,float along)
    {
        EdgeSample(edge,along,out float x,out float z);
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,N-1);
        int sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);
        byte raw=tilemap[sy*N+sx],f=sem[raw];
        return (f&1)==0;
    }

    static float CoastNoise(float along)
    {
        return Mathf.Clamp01(Mathf.PerlinNoise(along*.0075f+17.3f,.41f));
    }

    static float EdgeInnerHeight(Terrain terrain,OuterEdge edge,float along,float inward)
    {
        float x,z; EdgeSample(edge,along,out x,out z);
        if(edge==OuterEdge.North) z-=inward;
        else if(edge==OuterEdge.South) z+=inward;
        else if(edge==OuterEdge.West) x+=inward;
        else x-=inward;
        return terrain.SampleHeight(new Vector3(x,0f,z))+terrain.transform.position.y;
    }

    static float BeachWidth(Terrain terrain,OuterEdge edge,float along,bool land)
    {
        if(!land) return 0f;
        EdgeSample(edge,along,out float x,out float z);
        float edgeY=terrain.SampleHeight(new Vector3(x,0f,z))+terrain.transform.position.y;
        float innerY=EdgeInnerHeight(terrain,edge,along,12f);
        float slope=Mathf.Abs(innerY-edgeY)/12f;
        float n=CoastNoise(along);
        if(edgeY>5.5f || slope>.34f) return Mathf.Lerp(3f,7f,n);
        return Mathf.Lerp(8f,18f,n);
    }

    static float ShoreExtensionY(Terrain terrain,OuterEdge edge,float along,float d,bool land)
    {
        EdgeSample(edge,along,out float x,out float z);
        float inner=terrain.SampleHeight(new Vector3(x,0f,z))+terrain.transform.position.y;
        if(!land) return Mathf.Lerp(Mathf.Min(inner,-.35f),-4.2f,Mathf.SmoothStep(0f,1f,d/64f));
        float beach=BeachWidth(terrain,edge,along,true);
        float n=(CoastNoise(along)-.5f)*.28f;
        if(d<=beach)
        {
            float t=Mathf.SmoothStep(0f,1f,d/Mathf.Max(1f,beach));
            return Mathf.Lerp(inner,.18f+n,t);
        }
        float u=Mathf.SmoothStep(0f,1f,(d-beach)/Mathf.Max(1f,64f-beach));
        return Mathf.Lerp(.18f+n,-4.2f,u);
    }

    static void AddQuad(List<Vector3> v,List<int> tr,Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool flip)
    {
        int q=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
        if(!flip){tr.Add(q);tr.Add(q+2);tr.Add(q+1);tr.Add(q);tr.Add(q+3);tr.Add(q+2);}
        else{tr.Add(q);tr.Add(q+1);tr.Add(q+2);tr.Add(q);tr.Add(q+2);tr.Add(q+3);}
    }

    static GameObject MakeEdgeSurfacePart(Zone z,Transform parent,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem,OuterEdge edge,int part,Material mat)
    {
        const int alongN=256,across=32;const float cell=2f;
        var v=new List<Vector3>();var tr=new List<int>();var uv=new List<Vector2>();bool flip=edge==OuterEdge.South||edge==OuterEdge.East;
        for(int j=0;j<across;j++)for(int i=0;i<alongN;i++)
        {
            float a0=-256f+i*cell,a1=a0+cell,am=(a0+a1)*.5f,d0=j*cell,d1=d0+cell,dm=(d0+d1)*.5f;
            bool land=EdgeLand(tilemap,groups,sem,edge,am);float beach=BeachWidth(terrain,edge,am,land);
            EdgeSample(edge,am,out float ex,out float ez);float edgeY=terrain.SampleHeight(new Vector3(ex,0f,ez))+terrain.transform.position.y;
            float innerY=EdgeInnerHeight(terrain,edge,am,12f);float slope=Mathf.Abs(innerY-edgeY)/12f;
            bool rocky=land&&(edgeY>5.5f||slope>.34f);
            if(!land) continue; int cls=dm>beach+5f?3:rocky?2:(dm<Mathf.Max(2f,beach-5f)?0:1);
            if(cls!=part) continue;
            EdgePoint(edge,a0,d0,out float x00,out float z00);EdgePoint(edge,a1,d0,out float x10,out float z10);EdgePoint(edge,a1,d1,out float x11,out float z11);EdgePoint(edge,a0,d1,out float x01,out float z01);
            float y00=ShoreExtensionY(terrain,edge,a0,d0,EdgeLand(tilemap,groups,sem,edge,a0));
            float y10=ShoreExtensionY(terrain,edge,a1,d0,EdgeLand(tilemap,groups,sem,edge,a1));
            float y11=ShoreExtensionY(terrain,edge,a1,d1,EdgeLand(tilemap,groups,sem,edge,a1));
            float y01=ShoreExtensionY(terrain,edge,a0,d1,EdgeLand(tilemap,groups,sem,edge,a0));
            AddQuad(v,tr,new Vector3(x00,y00,z00),new Vector3(x10,y10,z10),new Vector3(x11,y11,z11),new Vector3(x01,y01,z01),flip);
            uv.Add(new Vector2(a0/12f,d0/12f));uv.Add(new Vector2(a1/12f,d0/12f));uv.Add(new Vector2(a1/12f,d1/12f));uv.Add(new Vector2(a0/12f,d1/12f));
        }
        if(tr.Count==0)return null;
        string en=edge.ToString();string pn=part==0?"DryBeach":part==1?"WetBeach":part==2?"RockyShore":"Seabed";string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/Outer_{pn}_{en}.asset",v,tr,uv);var go=new GameObject($"Outer {pn} - {en}");go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }

    static GameObject MakeEdgeWater(Zone z,Transform parent,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem,OuterEdge edge,Material water)
    {
        const int alongN=256,across=32;const float cell=2f; var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>(); bool flip=edge==OuterEdge.South||edge==OuterEdge.East;
        for(int j=0;j<across;j++)for(int i=0;i<alongN;i++)
        {
            float a0=-256f+i*cell,a1=a0+cell,d0=j*cell,d1=d0+cell,am=(a0+a1)*.5f; bool land=EdgeLand(tilemap,groups,sem,edge,am);
            float beach=BeachWidth(terrain,edge,am,land);float waterStart=land?Mathf.Max(4f,beach-1.5f):0f;if(d1<=waterStart)continue;
            EdgePoint(edge,a0,d0,out float x00,out float z00);EdgePoint(edge,a1,d0,out float x10,out float z10);EdgePoint(edge,a1,d1,out float x11,out float z11);EdgePoint(edge,a0,d1,out float x01,out float z01);
            int q=v.Count;v.Add(new Vector3(x00,.12f,z00));v.Add(new Vector3(x10,.12f,z10));v.Add(new Vector3(x11,.12f,z11));v.Add(new Vector3(x01,.12f,z01));uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);
            if(!flip){tr.Add(q);tr.Add(q+2);tr.Add(q+1);tr.Add(q);tr.Add(q+3);tr.Add(q+2);}else{tr.Add(q);tr.Add(q+1);tr.Add(q+2);tr.Add(q);tr.Add(q+2);tr.Add(q+3);}
        }
        string en=edge.ToString();string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/OuterWater_{en}.asset",v,tr,uv);var go=new GameObject("Outer Ocean - "+en);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=water;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
    }

    static float EdgeLandFactor(byte[] tilemap,byte[] groups,byte[] sem,OuterEdge edge,float along)
    {
        float sum=0f,ws=0f;
        for(int k=-4;k<=4;k++)
        {
            float w=Mathf.Exp(-k*k*.32f); sum+=(EdgeLand(tilemap,groups,sem,edge,along+k*3f)?1f:0f)*w; ws+=w;
        }
        return sum/Mathf.Max(.0001f,ws);
    }

    static bool EdgeRocky(Terrain terrain,OuterEdge edge,float along)
    {
        EdgeSample(edge,along,out float x,out float z);
        float e=terrain.SampleHeight(new Vector3(x,0f,z))+terrain.transform.position.y;
        float i=EdgeInnerHeight(terrain,edge,along,12f);
        float slope=Mathf.Abs(i-e)/12f; if(edge==OuterEdge.North) return e>2.5f || slope>.18f; return e>5.5f || slope>.34f;
    }

    static GameObject MakeCurvedShoreBand(Zone z,Transform parent,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem,OuterEdge edge,int part,Material mat)
    {
        const int segs=512; const float step=1f; var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>(); bool flip=edge==OuterEdge.South||edge==OuterEdge.East;
        for(int i=0;i<segs;i++)
        {
            float a0=-256f+i*step,a1=a0+step; float lf0=EdgeLandFactor(tilemap,groups,sem,edge,a0),lf1=EdgeLandFactor(tilemap,groups,sem,edge,a1);
            bool r0=EdgeRocky(terrain,edge,a0),r1=EdgeRocky(terrain,edge,a1); if(Mathf.Max(lf0,lf1)<.18f)continue;
            float b0=BeachWidth(terrain,edge,a0,true)*Mathf.SmoothStep(0f,1f,lf0), b1=BeachWidth(terrain,edge,a1,true)*Mathf.SmoothStep(0f,1f,lf1);
            float inset0=-Mathf.Lerp(2.0f,5.5f,CoastNoise(a0))*lf0, inset1=-Mathf.Lerp(2.0f,5.5f,CoastNoise(a1))*lf1;
            float d00,d01,d10,d11;
            if(part==0){if(r0&&r1)continue; d00=inset0;d01=b0*.62f;d10=inset1;d11=b1*.62f;}
            else if(part==1){if(r0&&r1)continue; d00=b0*.58f;d01=b0+1.2f;d10=b1*.58f;d11=b1+1.2f;}
            else if(part==2){if(!r0&&!r1)continue; d00=inset0;d01=Mathf.Max(4f,b0*.78f);d10=inset1;d11=Mathf.Max(4f,b1*.78f);}
            else {d00=b0;d01=28f;d10=b1;d11=28f;}
            EdgePoint(edge,a0,d00,out float x00,out float z00);EdgePoint(edge,a0,d01,out float x01,out float z01);EdgePoint(edge,a1,d10,out float x10,out float z10);EdgePoint(edge,a1,d11,out float x11,out float z11);
            float y00=ShoreExtensionY(terrain,edge,a0,d00,true),y01=ShoreExtensionY(terrain,edge,a0,d01,true),y10=ShoreExtensionY(terrain,edge,a1,d10,true),y11=ShoreExtensionY(terrain,edge,a1,d11,true);
            AddQuad(v,tr,new Vector3(x00,y00,z00),new Vector3(x10,y10,z10),new Vector3(x11,y11,z11),new Vector3(x01,y01,z01),flip);
            uv.Add(new Vector2(a0/10f,d00/10f));uv.Add(new Vector2(a1/10f,d10/10f));uv.Add(new Vector2(a1/10f,d11/10f));uv.Add(new Vector2(a0/10f,d01/10f));
        }
        if(tr.Count==0)return null; string en=edge.ToString(),pn=part==0?"DryBeach":part==1?"WetBeach":part==2?"RockyShore":"Seabed",dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/Curved_{pn}_{en}.asset",v,tr,uv);var go=new GameObject($"Curved {pn} - {en}");go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
    }

    static GameObject MakeCurvedEdgeWater(Zone z,Transform parent,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem,OuterEdge edge,Material water)
    {
        const int segs=512;const float step=1f;var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();bool flip=edge==OuterEdge.South||edge==OuterEdge.East;
        for(int i=0;i<segs;i++)
        {
            float a0=-256f+i*step,a1=a0+step,lf0=EdgeLandFactor(tilemap,groups,sem,edge,a0),lf1=EdgeLandFactor(tilemap,groups,sem,edge,a1);
            float b0=lf0<.12f?0f:Mathf.Max(0f,BeachWidth(terrain,edge,a0,true)*Mathf.SmoothStep(0f,1f,lf0)-1f);float b1=lf1<.12f?0f:Mathf.Max(0f,BeachWidth(terrain,edge,a1,true)*Mathf.SmoothStep(0f,1f,lf1)-1f);
            EdgePoint(edge,a0,b0,out float x00,out float z00);EdgePoint(edge,a0,64f,out float x01,out float z01);EdgePoint(edge,a1,b1,out float x10,out float z10);EdgePoint(edge,a1,64f,out float x11,out float z11);
            AddQuad(v,tr,new Vector3(x00,.12f,z00),new Vector3(x10,.12f,z10),new Vector3(x11,.12f,z11),new Vector3(x01,.12f,z01),flip);uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);
        }
        string en=edge.ToString(),dir=$"Assets/World/{z.key}/Generated";var mesh=SaveMeshAsset($"{dir}/CurvedWater_{en}.asset",v,tr,uv);var go=new GameObject("Curved Ocean - "+en);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=water;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
    }
    static GameObject MakeOceanQuad(Zone z,Transform parent,string name,float x0,float x1,float z0,float z1,Material water)
    {
        var v=new List<Vector3>{new Vector3(x0,.12f,z0),new Vector3(x1,.12f,z0),new Vector3(x1,.12f,z1),new Vector3(x0,.12f,z1)};var uv=new List<Vector2>{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};var tr=new List<int>{0,2,1,0,3,2};string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/{name.Replace(" ","")}.asset",v,tr,uv);var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=water;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
    }

    static void BuildOuterShoreExtensions(Zone z,Transform root,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem)
    {
        bool north=z.key=="SweetWater"||z.key=="Kriegspire"||z.key=="FrozenHighlands"||z.key=="SilverCove"||z.key=="EelInfestedWaters";
        bool south=z.key=="HermitsIsle"||z.key=="Dragonsand"||z.key=="MireOfTheDamned"||z.key=="CastleIronfist"||z.key=="NewSorpigal";
        bool west=z.key=="SweetWater"||z.key=="ParadiseValley"||z.key=="HermitsIsle";
        bool east=z.key=="EelInfestedWaters"||z.key=="MistyIslands"||z.key=="NewSorpigal";
        var old=root.Find("Outer Ocean + Shore Extension");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);if(!north&&!south&&!west&&!east)return;
        var g=new GameObject("Outer Ocean + Shore Extension");g.transform.SetParent(root,false);
        var dry=SurfaceMaterial("OuterDryBeach","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg");
        var wet=SurfaceMaterial("OuterWetBeach","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_diff_1k.jpg");
        var rock=SurfaceMaterial("OuterRockyShore","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg");
        var seabed=SurfaceMaterial("OuterSeabed","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_diff_1k.jpg");
        var water=SurfaceMaterial("OuterOcean","",true);
        if(z.key=="Kriegspire"){dry=rock;wet=rock;seabed=rock;}
        else if(z.key=="MireOfTheDamned"){dry=wet;seabed=wet;}
        else if(z.key=="SweetWater"||z.key=="FrozenHighlands"){seabed=rock;}
        Action<OuterEdge> edge=(e)=>{MakeCurvedShoreBand(z,g.transform,terrain,tilemap,groups,sem,e,0,dry);MakeCurvedShoreBand(z,g.transform,terrain,tilemap,groups,sem,e,1,wet);MakeCurvedShoreBand(z,g.transform,terrain,tilemap,groups,sem,e,2,rock);MakeCurvedShoreBand(z,g.transform,terrain,tilemap,groups,sem,e,3,seabed);};
        if(north)edge(OuterEdge.North);if(south)edge(OuterEdge.South);if(west)edge(OuterEdge.West);if(east)edge(OuterEdge.East);
        Debug.Log($"OUTER_SHORE_EXTENSION {z.display} N={north} S={south} W={west} E={east} coastAware=true shelfOnly=true localShelf=28m globalOcean=linkedWorld");
    }
    static int CountSourceModels(Zone z)
    {
        string dir=$"Assets/World/{z.key}/Objects";
        if(!Directory.Exists(dir)) return 0;
        return Directory.GetFiles(dir,"*.obj").Count(p=>!Path.GetFileName(p).StartsWith("000_",StringComparison.OrdinalIgnoreCase));
    }

    static int CountSceneModels(Transform root)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded");
        if(!arch) return 0;
        int n=0;
        foreach(Transform c in arch)
        {
            if(c.name.StartsWith("HouseMass_",StringComparison.OrdinalIgnoreCase) ||
               c.name.StartsWith("HouseRoof_",StringComparison.OrdinalIgnoreCase) ||
               c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)) continue;
            n++;
        }
        return n;
    }
}
