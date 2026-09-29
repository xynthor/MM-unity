using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class BuildFrozenHighlandsOpenWorld
{
    const string ScenePath = "Assets/Scenes/Regions/FrozenHighlands.unity";
    const float WorldScale = 1f;
    const float Cell = 4f * WorldScale;
    const float TerrainSize = 128f * Cell;
    const float TerrainBaseY = -24f;
    const float TerrainHeight = 320f;
    const int N = 128;
    const int TerrainResolution = 513;
    const int WaterGrid = 512;
    const float ArchitectureScale = 1f;

    static readonly string HeightPath = "Assets/World/FrozenHighlands/Data/heightmap_u8.bin";
    static readonly string TilePath = "Assets/World/FrozenHighlands/Data/tilemap_u8.bin";
    static readonly string SemPath = "Assets/World/FrozenHighlands/Data/tile_semantics_u8.bin";
    static readonly string DecorPath = "Assets/World/FrozenHighlands/Data/decorations.csv";
    static readonly string ObjFolder = "Assets/World/FrozenHighlands/Objects";
    static readonly string MatFolder = "Assets/Materials/FrozenHighlands";
    static readonly string PlacementPath = "Assets/World/FrozenHighlands/Data/model_placement_audit.csv";
    static readonly string GroupPath = "Assets/World/FrozenHighlands/Data/tile_groups_u8.bin";

    [MenuItem("MMUnity/Build Frozen Highlands Open World")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Materials/FrozenHighlands/Buildings");
        Directory.CreateDirectory("Assets/Materials/FrozenHighlands/Environment");
        Directory.CreateDirectory("Assets/World/FrozenHighlands/Generated");
        Directory.CreateDirectory("Assets/Prefabs/FrozenHighlands");
        Directory.CreateDirectory("Preview");

        var buildingMats = CreateBuildingMaterials();
        var env = CreateEnvironmentAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FrozenHighlands";

        var root = new GameObject("Frozen Highlands - Open World 1x1");
        var terrain = BuildTerrain(root.transform, env);
        Bounds cityBounds = BuildBuildings(root.transform, buildingMats, terrain);
        var water = BuildWater(root.transform, terrain, env.waterMaterial);
        BuildVegetation(root.transform, terrain, env);
        var player = BuildPlayer(root.transform, terrain);
        SetupLighting(root.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        RenderPreview(scene, cityBounds, terrain, water, player);
        AssetDatabase.SaveAssets();
        Debug.Log($"FH_OPENWORLD_DONE scene={ScenePath} scale={WorldScale} terrain={TerrainSize}m player={player.transform.position}");
    }

    sealed class EnvAssets
    {
        public TerrainLayer grassLayer, dirtLayer, roadLayer, rockLayer;
        public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, treeSmallBark, treeSmallLeaves, pineBark, pineLeaves, shrubMaterial, fernMaterial;
        public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, treeSmallPrefab, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;
    }

    static Dictionary<string, Material> CreateBuildingMaterials()
    {
        var result = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        string texFolder = "Assets/Textures/FrozenHighlands/Buildings";
        foreach (string file in Directory.GetFiles(texFolder, "*_albedo.png"))
        {
            string assetPath = file.Replace('\\','/');
            string key = Path.GetFileNameWithoutExtension(file).Replace("_albedo", "").ToLowerInvariant();
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            string normalPath = assetPath.Replace("_albedo.png", "_normal.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            SetNormalImporter(normalPath);

            string matPath = $"{MatFolder}/Buildings/{key}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = Shader.Find("Standard");
            if(mat.HasProperty("_Cull")) mat.SetInt("_Cull",0);
            mat.name = key;
            mat.SetTexture("_MainTex", albedo);
            mat.SetFloat("_Glossiness", 0.22f);
            mat.SetFloat("_Metallic", 0.02f);
            if (normal)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 0.55f);
                mat.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(mat);
            result[key] = mat;
        }

        var fallbackPath = $"{MatFolder}/Buildings/pending.mat";
        var fallback = AssetDatabase.LoadAssetAtPath<Material>(fallbackPath);
        if (!fallback)
        {
            fallback = new Material(Shader.Find("Standard"));
            fallback.color = new Color(0.55f, 0.5f, 0.42f);
            fallback.SetFloat("_Glossiness", 0.15f);
            AssetDatabase.CreateAsset(fallback, fallbackPath);
        }
        result["pending"] = fallback;
        return result;
    }

    static void SetNormalImporter(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    static EnvAssets CreateEnvironmentAssets()
    {
        var e = new EnvAssets();
        e.grassLayer = CreateTerrainLayer("Grass", "Assets/EnvironmentAssets/Biomes/snow.png", new Vector2(14,14));
        e.dirtLayer = CreateTerrainLayer("Dirt", "Assets/EnvironmentAssets/Biomes/cold_rock.png", new Vector2(11,11));
        e.roadLayer = CreateTerrainLayer("Road", "Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png", new Vector2(8,8));
        e.rockLayer = CreateTerrainLayer("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", new Vector2(10,10));

        e.waterMaterial = CreateSimpleMaterial("WaterDepth", Shader.Find("MMUnity/DepthWater"), new Color(0.08f,0.35f,0.44f,0.72f));
        e.rockMaterial = CreateTexturedMaterial("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", 0.18f);
        e.grassMaterial = CreateSimpleMaterial("GrassDetail", Shader.Find("Standard"), new Color(0.22f,0.46f,0.15f));
        e.treeBark = CreateSimpleMaterial("TreeBark", Shader.Find("Standard"), new Color(0.31f,0.18f,0.09f));
        e.treeLeaves = CreateSimpleMaterial("TreeLeaves", Shader.Find("Standard"), new Color(0.18f,0.48f,0.13f));
        e.treeAtlas = CreateTexturedMaterial("TreeAtlas", "Assets/EnvironmentAssets/Gobkit/TreeAtlas.png", 0.03f);
        e.pineBark = CreateTexturedMaterial("PineBark", "Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_bark_diff_1k.png", 0.05f);
        e.treeSmallBark = CreateTexturedMaterial("TreeSmallBark", "Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbm/tree_small_02_branch_diff_1k.png", 0.06f);
        e.treeSmallLeaves = CreateCutoutMaterial("TreeSmallLeaves", "Assets/EnvironmentAssets/Biomes/tree_small_leaves_rgba.png", 0.42f);
        e.pineLeaves = CreateCutoutMaterial("PineLeaves", "Assets/EnvironmentAssets/Biomes/pine_twig_rgba.png", 0.42f);
        e.shrubMaterial = CreateCutoutMaterial("Shrub", "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_rgba_1k.png", 0.42f);
        e.fernMaterial = CreateCutoutMaterial("Fern", "Assets/Environment/PolyHaven/Models/fern_02/fern_02_rgba_1k.png", 0.38f);

        e.treePrefab = null;
        e.treePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_1.FBX");
        e.treePrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004/Pine_004_01.FBX");
        e.treePrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_01.FBX");
        e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX");
        e.deadTreePrefab = null; // giant dead trunk removed globally
        e.treeSmallPrefab = null;
        e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/BroadleafShrub_01/Broadleaf_Shrub_01_Var1.FBX");
        e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Fern_var01.FBX");
        e.rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/Rock001.fbx");
        e.rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/Rock002.fbx");
        e.grassPrefab = MakeGrassPrefab(e.grassMaterial);
        return e;
    }

    static TerrainLayer CreateTerrainLayer(string name, string texturePath, Vector2 tileSize)
    {
        string path = $"{MatFolder}/Environment/Terrain_{name}.terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (!layer)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, path);
        }
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        layer.tileSize = tileSize;
        layer.tileOffset = Vector2.zero;
        layer.metallic = 0f;
        layer.smoothness = name == "Road" ? 0.18f : 0.08f;
        EditorUtility.SetDirty(layer);
        return layer;
    }

    static Material CreateSimpleMaterial(string name, Shader shader, Color color)
    {
        string path = $"{MatFolder}/Environment/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(shader ? shader : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader ? shader : Shader.Find("Standard");
        mat.color = color;
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.18f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateTexturedMaterial(string name, string texturePath, float smoothness)
    {
        var mat = CreateSimpleMaterial(name, Shader.Find("Standard"), Color.white);
        mat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        mat.SetFloat("_Glossiness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material CreateCutoutMaterial(string name, string texturePath, float cutoff)
    {
        var mat=CreateSimpleMaterial(name,Shader.Find("Standard"),Color.white);
        mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        mat.SetFloat("_Mode",1f); mat.SetFloat("_Cutoff",cutoff); mat.SetFloat("_Glossiness",0.05f);
        mat.SetOverrideTag("RenderType","TransparentCutout"); mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON"); mat.DisableKeyword("_ALPHAPREMULTIPLY_ON"); mat.renderQueue=2450;
        EditorUtility.SetDirty(mat); return mat;
    }

    static GameObject MakeGrassPrefab(Material mat)
    {
        string prefabPath = "Assets/Prefabs/FrozenHighlands/GrassDetail.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing) return existing;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/grass_bladeNoLOD10.FBX");
        if (!source) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name = "GrassDetail";
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static byte[] semanticCache;
    static byte Sem(byte tile)
    {
        if (semanticCache == null) semanticCache = File.ReadAllBytes(SemPath);
        return semanticCache[tile];
    }
    static bool IsWater(byte tile) => (Sem(tile) & 1) != 0;
    static bool IsShore(byte tile) => (Sem(tile) & 2) != 0;
    static bool IsRoad(byte tile) => (Sem(tile) & 8) != 0;
    static bool IsDirt(byte tile) => (Sem(tile) & 16) != 0 && !IsWater(tile) && !IsRoad(tile);
    static byte[] groupCache;
    static byte TileGroup(byte tile){if(groupCache==null)groupCache=File.ReadAllBytes(GroupPath);return groupCache[tile];}
    static bool IsHarsh(byte tile){byte g=TileGroup(tile);return g==3||g==6;}

    static int[,] ComputeWaterDistance(byte[] tiles)
    {
        var dist = new int[N,N];
        var q = new Queue<Vector2Int>();
        const int inf = 9999;
        for (int y=0; y<N; y++)
        for (int x=0; x<N; x++)
        {
            if (!IsWater(tiles[y*N+x]))
            {
                dist[y,x] = 0;
                q.Enqueue(new Vector2Int(x,y));
            }
            else dist[y,x] = inf;
        }
        int[] dx = {1,-1,0,0};
        int[] dy = {0,0,1,-1};
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int nd = dist[p.y,p.x] + 1;
            for (int k=0;k<4;k++)
            {
                int nx=p.x+dx[k], ny=p.y+dy[k];
                if (nx<0||ny<0||nx>=N||ny>=N||nd>=dist[ny,nx]) continue;
                dist[ny,nx]=nd;
                q.Enqueue(new Vector2Int(nx,ny));
            }
        }
        return dist;
    }

    static float SampleByteBilinear(byte[] data, float sx, float sy)
    {
        sx = Mathf.Clamp(sx, 0f, N-1f);
        sy = Mathf.Clamp(sy, 0f, N-1f);
        int x0 = Mathf.FloorToInt(sx), y0 = Mathf.FloorToInt(sy);
        int x1 = Mathf.Min(x0+1,N-1), y1 = Mathf.Min(y0+1,N-1);
        float tx=sx-x0, ty=sy-y0;
        float a=Mathf.Lerp(data[y0*N+x0],data[y0*N+x1],tx);
        float b=Mathf.Lerp(data[y1*N+x0],data[y1*N+x1],tx);
        return Mathf.Lerp(a,b,ty);
    }

    static float SampleWaterMask(byte[] tiles, float sx, float sy)
    {
        sx = Mathf.Clamp(sx, 0f, N-1f);
        sy = Mathf.Clamp(sy, 0f, N-1f);
        int x0=Mathf.FloorToInt(sx), y0=Mathf.FloorToInt(sy);
        int x1=Mathf.Min(x0+1,N-1), y1=Mathf.Min(y0+1,N-1);
        float tx=sx-x0, ty=sy-y0;
        float a=Mathf.Lerp(IsWater(tiles[y0*N+x0])?1f:0f, IsWater(tiles[y0*N+x1])?1f:0f, tx);
        float b=Mathf.Lerp(IsWater(tiles[y1*N+x0])?1f:0f, IsWater(tiles[y1*N+x1])?1f:0f, tx);
        return Mathf.Lerp(a,b,ty);
    }

    static float SampleSmoothWaterMask(byte[] tiles, float sx, float sy)
    {
        float sum=0f, weight=0f;
        for (int oy=-2;oy<=2;oy++)
        for (int ox=-2;ox<=2;ox++)
        {
            float d2=ox*ox+oy*oy;
            float w=1f/(1f+d2);
            sum+=SampleWaterMask(tiles,sx+ox*0.65f,sy+oy*0.65f)*w;
            weight+=w;
        }
        return sum/weight;
    }

    static float SampleDistanceBilinear(int[,] dist, float sx, float sy)
    {
        sx=Mathf.Clamp(sx,0f,N-1f); sy=Mathf.Clamp(sy,0f,N-1f);
        int x0=Mathf.FloorToInt(sx), y0=Mathf.FloorToInt(sy);
        int x1=Mathf.Min(x0+1,N-1), y1=Mathf.Min(y0+1,N-1);
        float tx=sx-x0, ty=sy-y0;
        float a=Mathf.Lerp(dist[y0,x0],dist[y0,x1],tx);
        float b=Mathf.Lerp(dist[y1,x0],dist[y1,x1],tx);
        return Mathf.Lerp(a,b,ty);
    }

    static Vector2 WorldToSource(float x, float z)
    {
        float sx = x / Cell + 64f;
        float sy = 64f - z / Cell;
        return new Vector2(Mathf.Clamp(sx,0f,N-1f),Mathf.Clamp(sy,0f,N-1f));
    }

    static byte TileAtSource(byte[] tiles, float sx, float sy)
    {
        int ix=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1);
        int iy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        return tiles[iy*N+ix];
    }

    static float CreativeLandHeight(float raw,float sx,float sy)
    {
        float high=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(3f,20f,raw));
        float micro=(Mathf.PerlinNoise(sx*.43f+17.2f,sy*.39f+3.1f)-.5f)*.70f*high;
        return Mathf.Max(.38f,raw+micro);
    }

    static Terrain BuildTerrain(Transform parent, EnvAssets e)
    {
        byte[] heights = File.ReadAllBytes(HeightPath);
        byte[] tiles = File.ReadAllBytes(TilePath);
        int[,] waterDist = ComputeWaterDistance(tiles);

        string dataPath = "Assets/World/FrozenHighlands/Generated/FrozenHighlandsTerrain.asset";
        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if (!td)
        {
            td = new TerrainData();
            AssetDatabase.CreateAsset(td, dataPath);
        }
        td.heightmapResolution = TerrainResolution;
        td.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        td.terrainLayers = new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer};

        int hmMax=TerrainResolution-1;
        float[,] hm = new float[TerrainResolution,TerrainResolution];
        for (int y=0;y<TerrainResolution;y++)
        for (int x=0;x<TerrainResolution;x++)
        {
            float wx=-TerrainSize*.5f+x/(float)hmMax*TerrainSize;
            float wz=-TerrainSize*.5f+y/(float)hmMax*TerrainSize;
            Vector2 src=WorldToSource(wx,wz);
            float sx=src.x, sy=src.y;
            float mask=SampleSmoothWaterMask(tiles,sx,sy);
            float rawY=SampleByteBilinear(heights,sx,sy)*0.25f;
            float worldY=CreativeLandHeight(rawY,sx,sy);
            if (mask>=0.5f)
            {
                float d=SampleDistanceBilinear(waterDist,sx,sy);
                worldY=-Mathf.Clamp(0.65f+d*0.78f,0.65f,11.5f);
            }
            else
            {
                worldY=Mathf.Max(worldY,0.38f); // keep dry land above sea level
            }
            hm[y,x]=Mathf.Clamp01((worldY-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,hm);

        td.alphamapResolution = 512;
        float[,,] alpha = new float[512,512,4];
        for (int y=0;y<512;y++)
        for (int x=0;x<512;x++)
        {
            float wx=-TerrainSize*.5f+(x+.5f)/512f*TerrainSize;
            float wz=-TerrainSize*.5f+(y+.5f)/512f*TerrainSize;
            Vector2 src=WorldToSource(wx,wz);
            float sx=src.x, sy=src.y;
            byte tile=TileAtSource(tiles,sx,sy);
            float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*0.25f,sx,sy);
            if (IsWater(tile)) { alpha[y,x,1]=0.35f; alpha[y,x,3]=0.65f; }
            else if (IsRoad(tile)) { alpha[y,x,2]=0.88f; alpha[y,x,1]=0.12f; }
            else if (IsDirt(tile)) { alpha[y,x,1]=0.72f; alpha[y,x,0]=0.18f; alpha[y,x,3]=0.10f; }
            else if (tile < 90 && h <= 0.75f) { alpha[y,x,1]=0.58f; alpha[y,x,0]=0.42f; }
            else { alpha[y,x,0]=0.90f; alpha[y,x,1]=0.10f; }
        }
        td.SetAlphamaps(0,0,alpha);

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terrain_MM_SourceGrid_1x1";
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(-TerrainSize/2f, TerrainBaseY, -TerrainSize/2f);
        var terrain = go.GetComponent<Terrain>();
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 2f;
        terrain.basemapDistance = 1000f;
        terrain.detailObjectDistance = 150f;
        terrain.detailObjectDensity = 0.75f;
        BuildGrassDetails(terrain, tiles, e.grassPrefab);
        EditorUtility.SetDirty(td);
        return terrain;
    }

    static bool HasNearbyForbiddenTile(byte[] tiles, float sx, float sy, int radius, bool forbidDirt)
    {
        int cx=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1);
        int cy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        for (int oy=-radius;oy<=radius;oy++)
        for (int ox=-radius;ox<=radius;ox++)
        {
            int x=cx+ox, y=cy+oy;
            if (x<0||y<0||x>=N||y>=N) return true;
            byte t=tiles[y*N+x];
            if (IsWater(t) || IsRoad(t) || (forbidDirt && IsDirt(t))) return true;
        }
        return false;
    }

    static void BuildGrassDetails(Terrain terrain, byte[] tiles, GameObject grassPrefab)
    {
        if (!grassPrefab) return;
        var td = terrain.terrainData;
        td.SetDetailResolution(512, 16);
        var proto = new DetailPrototype
        {
            prototype = grassPrefab,
            usePrototypeMesh = true,
            renderMode = DetailRenderMode.VertexLit,
            minWidth = 0.45f,
            maxWidth = 1.0f,
            minHeight = 0.45f,
            maxHeight = 1.0f,
            noiseSpread = 0.38f,
            healthyColor = new Color(0.46f,0.66f,0.28f),
            dryColor = new Color(0.55f,0.47f,0.24f)
        };
        td.detailPrototypes = new[]{proto};
        int[,] density = new int[512,512];
        for (int y=0;y<512;y++)
        for (int x=0;x<512;x++)
        {
            float wx=-TerrainSize*.5f+(x+.5f)/512f*TerrainSize;
            float wz=-TerrainSize*.5f+(y+.5f)/512f*TerrainSize;
            Vector2 src=WorldToSource(wx,wz);
            float sx=src.x, sy=src.y;
            byte t=TileAtSource(tiles,sx,sy);
            bool grass=!IsWater(t)&&!IsRoad(t)&&!IsDirt(t)&&!HasNearbyForbiddenTile(tiles,sx,sy,1,false);
            if (grass) density[y,x]=((x*17+y*31)%7==0)?1:2;
        }
        td.SetDetailLayer(0,0,0,density);
    }

    static bool[,] BuildArchitectureProtectionMask(Transform parent,int grid,float step,float origin)
    {
        var mask=new bool[grid,grid]; var arch=parent.Find("Architecture - MM6 Original Layout Expanded"); if(!arch) return mask;
        foreach(Transform child in arch)
        {
            if(WaterAffiliated(child.name)) continue; var rs=child.GetComponentsInChildren<Renderer>(true); if(rs.Length==0) continue;
            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); b.Expand(new Vector3(4f,0f,4f));
            int x0=Mathf.Clamp(Mathf.FloorToInt((b.min.x-origin)/step),0,grid-1), x1=Mathf.Clamp(Mathf.CeilToInt((b.max.x-origin)/step),0,grid-1);
            int y0=Mathf.Clamp(Mathf.FloorToInt((b.min.z-origin)/step),0,grid-1), y1=Mathf.Clamp(Mathf.CeilToInt((b.max.z-origin)/step),0,grid-1);
            for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++) mask[y,x]=true;
        }
        return mask;
    }

    static GameObject BuildWater(Transform parent, Terrain terrain, Material material)
    {
        byte[] tiles=File.ReadAllBytes(TilePath);
        int grid=WaterGrid;
        int row=grid+1;
        float step=TerrainSize/grid;
        float origin=-TerrainSize/2f;
        bool[,] protectedWater=BuildArchitectureProtectionMask(parent,grid,step,origin);
        var verts=new List<Vector3>(row*row);
        var uvs=new List<Vector2>(row*row);
        var tris=new List<int>(grid*grid*4);

        for (int y=0;y<=grid;y++)
        for (int x=0;x<=grid;x++)
        {
            float wx=origin+x*step;
            float wz=origin+y*step;
            verts.Add(new Vector3(wx,0.12f,wz));
            uvs.Add(new Vector2(x*0.055f,y*0.055f));
        }

        for (int y=0;y<grid;y++)
        for (int x=0;x<grid;x++)
        {
            float wx=origin+(x+0.5f)*step;
            float wz=origin+(y+0.5f)*step;
            Vector2 src=WorldToSource(wx,wz);
            if (protectedWater[y,x]) continue;
            if (SampleSmoothWaterMask(tiles,src.x,src.y)<0.5f) continue;
            int a=y*row+x, b=a+1, d=(y+1)*row+x, c=d+1;
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(a); tris.Add(d); tris.Add(c);
        }

        string meshPath="Assets/World/FrozenHighlands/Generated/WaterSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!mesh)
        {
            mesh=new Mesh();
            AssetDatabase.CreateAsset(mesh,meshPath);
        }
        mesh.Clear();
        mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris,0);
        mesh.SetUVs(0,uvs);
        mesh.SetNormals(Enumerable.Repeat(Vector3.up,verts.Count).ToList());
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);

        if (material)
        {
            material.renderQueue=3000;
            if (material.HasProperty("_ShallowColor")) material.SetColor("_ShallowColor",new Color(0.08f,0.58f,0.67f,0.50f));
            if (material.HasProperty("_DeepColor")) material.SetColor("_DeepColor",new Color(0.02f,0.16f,0.30f,0.84f));
            if (material.HasProperty("_DepthRange")) material.SetFloat("_DepthRange",11f);
            if (material.HasProperty("_FoamDepth")) material.SetFloat("_FoamDepth",0.55f);
        }

        var go=new GameObject("Water - Smoothed Sea Rivers Pond");
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();
        mr.sharedMaterial=material;
        mr.shadowCastingMode=ShadowCastingMode.Off;
        mr.receiveShadows=false;
        Debug.Log($"FH_WATER grid={grid} verts={verts.Count} tris={tris.Count/3}");
        return go;
    }

    static bool IsSpecialPlacement(string name)
    {
        string n=name.ToLowerInvariant();
        return n.Contains("bridge") || n.Contains("fountain") || n.Contains("create") || n.Contains("antifly");
    }

    sealed class PlacementInfo
    {
        public string kind; public float sourceBaseOffset; public float waterRatio;
    }
    static Dictionary<string,PlacementInfo> placementCache;
    static PlacementInfo PlacementFor(string name)
    {
        if(placementCache==null)
        {
            placementCache=new Dictionary<string,PlacementInfo>(StringComparer.OrdinalIgnoreCase);
            if(File.Exists(PlacementPath)) foreach(string line in File.ReadAllLines(PlacementPath).Skip(1))
            {
                var q=line.Split(','); if(q.Length<12) continue;
                string key=Path.GetFileNameWithoutExtension(q[0]);
                float.TryParse(q[8],NumberStyles.Float,CultureInfo.InvariantCulture,out float wr);
                float.TryParse(q[11],NumberStyles.Float,CultureInfo.InvariantCulture,out float bo);
                placementCache[key]=new PlacementInfo{kind=q[1],waterRatio=wr,sourceBaseOffset=bo};
            }
        }
        placementCache.TryGetValue(Path.GetFileNameWithoutExtension(name),out var pi); return pi;
    }
    static bool WaterAffiliated(string name){var p=PlacementFor(name); return p!=null && p.kind!="LAND";}

    static Bounds BuildBuildings(Transform parent, Dictionary<string, Material> mats, Terrain terrain)
    {
        var group=new GameObject("Architecture - MM6 Original Layout Expanded");
        group.transform.SetParent(parent);
        var paths=AssetDatabase.FindAssets("t:GameObject",new[]{ObjFolder})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.EndsWith(".obj",StringComparison.OrdinalIgnoreCase))
            .Where(p=>!Path.GetFileName(p).StartsWith("000_"))
            .OrderBy(p=>p).ToArray();

        Bounds all=new Bounds(Vector3.zero,Vector3.zero);
        bool hasBounds=false;
        int grounded=0;
        foreach (string path in paths)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name=Path.GetFileNameWithoutExtension(path);
            go.transform.SetParent(group.transform,true);

            Bounds original=GetRendererBounds(go);
            float objectScale=1f;
            go.transform.localScale=Vector3.one;

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var src=r.sharedMaterials;
                var dst=new Material[src.Length];
                for (int i=0;i<src.Length;i++)
                {
                    string key=src[i]?src[i].name.ToLowerInvariant():"pending";
                    dst[i]=mats.TryGetValue(key,out var m)?m:mats["pending"];
                }
                r.sharedMaterials=dst;
                r.shadowCastingMode=ShadowCastingMode.On;
                r.receiveShadows=true;
            }

            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue;
                var mc=mf.GetComponent<MeshCollider>();
                if (!mc) mc=mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh=mf.sharedMesh;
            }

            Bounds b=GetRendererBounds(go);
            var placement=PlacementFor(go.name);
            if (placement!=null && b.size.y>0.15f)
            {
                // Same 1:1 source placement rule as New Sorpigal / Castle Ironfist.
                // Preserve baked source X/Z exactly; only ground Y from the source base offset.
                float surface=WaterAffiliated(go.name)?0.12f:SampleTerrainY(terrain,b.center.x,b.center.z);
                float desiredBase=surface+placement.sourceBaseOffset*objectScale;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);
                grounded++;
                b=GetRendererBounds(go);
            }

            if (!hasBounds) { all=b; hasBounds=true; }
            else all.Encapsulate(b);
        }
        // Do not cluster-shift buildings or stamp terrain. Both change the source map.
        AddArchitectureSolidCores(group.transform);
        Physics.SyncTransforms();
        Debug.Log($"FH_BUILDINGS count={paths.Length} grounded={grounded} architectureScale={ArchitectureScale} bounds={all}");
        return all;
    }


    static bool OverlapXZ(Bounds a, Bounds b, float margin)
    {
        return a.min.x-margin<=b.max.x && a.max.x+margin>=b.min.x &&
               a.min.z-margin<=b.max.z && a.max.z+margin>=b.min.z;
    }

    static List<List<GameObject>> ArchitectureClusters(Transform root)
    {
        var items=root.Cast<Transform>().Where(t=>!WaterAffiliated(t.name) && !t.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)).Select(t=>t.gameObject).ToList();
        var result=new List<List<GameObject>>(); var used=new bool[items.Count];
        for(int i=0;i<items.Count;i++) if(!used[i])
        {
            var group=new List<GameObject>(); var q=new Queue<int>(); q.Enqueue(i); used[i]=true;
            while(q.Count>0)
            {
                int k=q.Dequeue(); group.Add(items[k]); var a=GetRendererBounds(items[k]);
                for(int j=0;j<items.Count;j++) if(!used[j])
                {
                    var b=GetRendererBounds(items[j]);
                    float d=Vector2.Distance(new Vector2(a.center.x,a.center.z),new Vector2(b.center.x,b.center.z));
                    if(OverlapXZ(a,b,5f)||d<30f){used[j]=true;q.Enqueue(j);}
                }
            }
            result.Add(group);
        }
        return result;
    }

    static int GroundArchitectureClusters(Terrain terrain, Transform root)
    {
        int moved=0;
        foreach(var cluster in ArchitectureClusters(root))
        {
            Bounds b=GetRendererBounds(cluster[0]); for(int i=1;i<cluster.Count;i++) b.Encapsulate(GetRendererBounds(cluster[i]));
            float[] ys={SampleTerrainY(terrain,b.center.x,b.center.z),SampleTerrainY(terrain,b.min.x,b.min.z),SampleTerrainY(terrain,b.max.x,b.min.z),SampleTerrainY(terrain,b.min.x,b.max.z),SampleTerrainY(terrain,b.max.x,b.max.z)};
            Array.Sort(ys); float surface=ys[2]; float delta=surface-b.min.y;
            foreach(var go in cluster){go.transform.position+=Vector3.up*delta;moved++;}
        }
        return moved;
    }

    static bool IsBuildingObject(string n)
    {
        n=n.ToLowerInvariant();
        if(n.Contains("sign")||n.Contains("sgn")) return false;
        return n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("blacksm")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
    }

    static Material PickBuildingMaterial(GameObject go,bool roof)
    {
        Material fallback=null;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)) foreach(var m in r.sharedMaterials)
        {
            if(!m) continue; if(!fallback) fallback=m;
            string n=m.name.ToLowerInvariant(); bool isRoof=n.Contains("roof")||n.Contains("ruf")||n.Contains("shing")||n.Contains("tile");
            if(roof && isRoof) return m;
            if(!roof && !isRoof && (n.Contains("wal")||n.Contains("wall")||n.Contains("brick")||n.Contains("brck")||n.Contains("wood")||n.Contains("str")||n.Contains("beam"))) return m;
        }
        return fallback;
    }
    static void AddArchitectureSolidCores(Transform root)
    {
        // Preserve the exact MM6 exterior mesh at 1:1. Only add an inset solid core
        // so one-sided/open source shells no longer read as hollow buildings.
        int ncore=0;
        var originals=root.Cast<Transform>().Where(t=>
            !t.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase) &&
            !t.name.StartsWith("HouseMass_",StringComparison.OrdinalIgnoreCase) &&
            !t.name.StartsWith("HouseRoof_",StringComparison.OrdinalIgnoreCase)).ToList();
        foreach(var child in originals)
        {
            string n=child.name.ToLowerInvariant();
            if(!IsBuildingObject(n)) continue;
            Bounds b=GetRendererBounds(child.gameObject);
            if(b.size.x<1.8f||b.size.z<1.8f||b.size.x>60f||b.size.z>60f||b.size.y<1.8f||b.size.y>45f) continue;
            Material wall=PickBuildingMaterial(child.gameObject,false);
            if(!wall) continue;
            float inset=Mathf.Clamp(Mathf.Min(b.size.x,b.size.z)*0.07f,0.15f,0.55f);
            float sx=Mathf.Max(.35f,b.size.x-inset*2f);
            float sz=Mathf.Max(.35f,b.size.z-inset*2f);
            float sy=Mathf.Max(.8f,b.size.y*.90f);
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name="SolidCore_"+child.name;
            core.transform.SetParent(root,true);
            core.transform.position=new Vector3(b.center.x,b.min.y+sy*.5f+0.03f,b.center.z);
            core.transform.localScale=new Vector3(sx,sy,sz);
            var coreRenderer=core.GetComponent<MeshRenderer>(); coreRenderer.sharedMaterial=wall; coreRenderer.enabled=false; // collider-only solid core
            // Keep the box collider: MM6 exterior houses are not walk-in meshes.
            ncore++;
        }
        Debug.Log("ARCHITECTURE_SOLID_1X1 "+ncore);
    }


    static float ArchitectureScaleFactor(string name, Bounds raw)
    {
        string n=name.ToLowerInvariant();
        if(n.Contains("bridge")||n.Contains("dock")||n.Contains("pier")||n.Contains("wall")||n.Contains("gate")||n.Contains("tower")||n.Contains("castle")||n.Contains("trigger")||n.Contains("antifly")||n.Contains("stairs")) return 1f;
        bool explicitSmall=n.Contains("house")||n.Contains("hse")||n.Contains("fountain")||n.Contains("well");
        return explicitSmall?1.20f:1f;
    }

    static Bounds GetRendererBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static float SampleTerrainY(Terrain terrain, float x, float z)
    {
        Vector3 p = new Vector3(x,0f,z);
        return terrain.SampleHeight(p) + terrain.transform.position.y;
    }

    static void StampDryGroundUnderBuildings(Terrain terrain, Transform architectureRoot)
    {
        var td=terrain.terrainData; int r=td.heightmapResolution; var hm=td.GetHeights(0,0,r,r);
        foreach(Transform child in architectureRoot)
        {
            if(WaterAffiliated(child.name)) continue; var rs=child.GetComponentsInChildren<Renderer>(true); if(rs.Length==0) continue;
            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds); b.Expand(new Vector3(5f,0f,5f));
            float target=Mathf.Clamp01((0.38f-terrain.transform.position.y)/td.size.y);
            int x0=Mathf.Clamp(Mathf.FloorToInt((b.min.x-terrain.transform.position.x)/td.size.x*(r-1)),0,r-1);
            int x1=Mathf.Clamp(Mathf.CeilToInt((b.max.x-terrain.transform.position.x)/td.size.x*(r-1)),0,r-1);
            int y0=Mathf.Clamp(Mathf.FloorToInt((b.min.z-terrain.transform.position.z)/td.size.z*(r-1)),0,r-1);
            int y1=Mathf.Clamp(Mathf.CeilToInt((b.max.z-terrain.transform.position.z)/td.size.z*(r-1)),0,r-1);
            for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++) hm[y,x]=Mathf.Max(hm[y,x],target);
        }
        td.SetHeights(0,0,hm); EditorUtility.SetDirty(td);
    }

    static float Deterministic01(int seed)
    {
        unchecked
        {
            uint x = (uint)seed * 747796405u + 2891336453u;
            x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return (x & 0x00FFFFFF) / 16777215f;
        }
    }

    static void ScaleToHeight(GameObject go, float targetHeight)
    {
        Bounds b = GetRendererBounds(go);
        if (b.size.y < 0.01f) return;
        float k = targetHeight / b.size.y;
        go.transform.localScale *= k;
    }

    static bool NearArchitecture(Transform architectureRoot, float x, float z, float margin)
    {
        if (!architectureRoot) return false;
        foreach (var r in architectureRoot.GetComponentsInChildren<Renderer>(true))
        {
            Bounds b=r.bounds;
            if (x>=b.min.x-margin && x<=b.max.x+margin && z>=b.min.z-margin && z<=b.max.z+margin)
                return true;
        }
        return false;
    }

    static bool IsSuitableNaturalSpot(Terrain terrain, Transform architectureRoot, float x, float z, bool tree, bool requireGrass)
    {
        if (tileCache==null) tileCache=File.ReadAllBytes(TilePath);
        Vector3 tp=terrain.transform.position;
        float nx=(x-tp.x)/terrain.terrainData.size.x;
        float nz=(z-tp.z)/terrain.terrainData.size.z;
        if (nx<0.01f||nz<0.01f||nx>0.99f||nz>0.99f) return false;

        Vector2 src=WorldToSource(x,z);
        byte t=TileAtSource(tileCache,src.x,src.y);
        if (IsWater(t)||IsRoad(t)) return false;
        if (requireGrass && IsDirt(t)) return false;
        if (HasNearbyForbiddenTile(tileCache,src.x,src.y,tree?1:0,false)) return false;

        float steep=terrain.terrainData.GetSteepness(nx,nz);
        if (steep>(tree?31f:43f)) return false;
        if (NearArchitecture(architectureRoot,x,z,tree?3.5f:1.4f)) return false;
        return true;
    }

    static GameObject PickTreePrefab(EnvAssets e,int seed)
    {
        int k=Mathf.Abs(seed)%4;
        if(k==0 && e.treePrefab2) return e.treePrefab2;
        if(k==1 && e.treePrefab3) return e.treePrefab3;
        if(k==2 && e.treePrefab4) return e.treePrefab4;
        if(e.pinePrefab) return e.pinePrefab;
        return e.treePrefab;
    }

    static bool KeepTreeAnchor(int seed)
    { return Mathf.Abs(seed)%6!=0; }

    static void BuildVegetation(Transform parent, Terrain terrain, EnvAssets e)
    {
        var root=new GameObject("Vegetation - MM Anchors + Natural Groves"); root.transform.SetParent(parent);
        var trees=new GameObject("Trees"); trees.transform.SetParent(root.transform);
        var rocks=new GameObject("Rocks"); rocks.transform.SetParent(root.transform);
        var understory=new GameObject("Understory"); understory.transform.SetParent(root.transform);
        Transform architectureRoot=parent.Find("Architecture - MM6 Original Layout Expanded");
        string[] lines=File.ReadAllLines(DecorPath);byte[] decorTiles=File.ReadAllBytes(TilePath);
        int treeCount=0,extraTrees=0,shrubs=0,ferns=0,rockCount=0,skippedTrees=0,skippedRocks=0;
        for(int i=1;i<lines.Length;i++)
        {
            string[] q=lines[i].Split(','); if(q.Length<10) continue;
            string name=q[1].Trim().ToLowerInvariant();
            if(!float.TryParse(q[4],NumberStyles.Float,CultureInfo.InvariantCulture,out float ox))continue;
            if(!float.TryParse(q[6],NumberStyles.Float,CultureInfo.InvariantCulture,out float oz))continue;
            float x=ox*WorldScale,z=-oz*WorldScale; float yaw=Deterministic01(i*13+7)*360f;
            if(name.StartsWith("6tree")||name.StartsWith("tree"))
            {
                Vector2 aSrc=WorldToSource(x,z);byte aTile=TileAtSource(decorTiles,aSrc.x,aSrc.y);
                var anchorPrefab=(IsHarsh(aTile)&&e.deadTreePrefab)?e.deadTreePrefab:PickTreePrefab(e,i); if(!anchorPrefab || !KeepTreeAnchor(i)){skippedTrees++;continue;}
                // Exact MM6 source anchor: keep it at its recorded X/Z and ground it vertically.
                SpawnTree(anchorPrefab,trees.transform,e,x,z,SampleTerrainY(terrain,x,z),yaw,Mathf.Lerp(7.5f,11.5f,Deterministic01(i*19+3)),name+"_"+i.ToString("000")); treeCount++;
                int extras=1+(i%2);
                for(int k=0;k<extras;k++)
                {
                    float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float rad=Mathf.Lerp(7f,27f,Deterministic01(i*107+k*23+9));
                    float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;
                    if(!IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,true,true))continue;
                    SpawnTree(PickTreePrefab(e,i*109+k),trees.transform,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(i*109+k)*360f,Mathf.Lerp(6.8f,10.8f,Deterministic01(i*113+k)),"GroveTree_"+i.ToString("000")+"_"+k); extraTrees++;
                }
                if(e.shrubPrefab && i%2==0)
                {
                    int n=1+(i%2); for(int k=0;k<n;k++){float a=Deterministic01(i*127+k)*Mathf.PI*2f,rad=Mathf.Lerp(3f,12f,Deterministic01(i*131+k));float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,false,true)){SpawnPlant(e.shrubPrefab,understory.transform,e.shrubMaterial,ex,ez,SampleTerrainY(terrain,ex,ez),i*17+k,Mathf.Lerp(0.75f,1.55f,Deterministic01(i*137+k)),"Shrub");shrubs++;}}
                }
                if(e.fernPrefab && i%3==0)
                {
                    for(int k=0;k<2;k++){float a=Deterministic01(i*139+k)*Mathf.PI*2f,rad=Mathf.Lerp(2.5f,10f,Deterministic01(i*149+k));float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,false,true)){SpawnPlant(e.fernPrefab,understory.transform,e.fernMaterial,ex,ez,SampleTerrainY(terrain,ex,ez),i*29+k,Mathf.Lerp(0.35f,0.75f,Deterministic01(i*151+k)),"Fern");ferns++;}}
                }
            }
            else if(name.StartsWith("6rock")&&(e.rockA||e.rockB))
            {
                // Exact MM6 source rock anchor: keep it at its recorded X/Z and ground it vertically.
                var src=(i%2==0&&e.rockB)?e.rockB:e.rockA;if(!src)src=e.rockB;var go=(GameObject)PrefabUtility.InstantiatePrefab(src);go.name=name+"_"+i.ToString("000");go.transform.SetParent(rocks.transform);go.transform.position=new Vector3(x,SampleTerrainY(terrain,x,z),z);go.transform.rotation=Quaternion.Euler(0,yaw,0);
                float target=(i%11==0)?Mathf.Lerp(1.5f,2.6f,Deterministic01(i*53)):Mathf.Lerp(0.45f,1.35f,Deterministic01(i*53));ScaleToHeight(go,target);foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=e.rockMaterial;rockCount++;
            }
        }
        Debug.Log($"VEGETATION_REALISTIC anchorTrees={treeCount} groveTrees={extraTrees} shrubs={shrubs} ferns={ferns} rocks={rockCount} skippedTrees={skippedTrees} skippedRocks={skippedRocks}");
    }

    static void NormalizeTreeUpright(GameObject go)
    {
        Quaternion baseRot=go.transform.rotation,bestRot=baseRot; float best=-999f;
        Quaternion[] tests={baseRot,baseRot*Quaternion.Euler(90f,0,0),baseRot*Quaternion.Euler(-90f,0,0),baseRot*Quaternion.Euler(0,0,90f),baseRot*Quaternion.Euler(0,0,-90f)};
        foreach(var q in tests){go.transform.rotation=q;Bounds b=GetRendererBounds(go);float score=b.size.y-.12f*Mathf.Max(b.size.x,b.size.z);if(score>best){best=score;bestRot=q;}}
        go.transform.rotation=bestRot;
    }

    static void SpawnTree(GameObject prefab,Transform parent,EnvAssets e,float x,float z,float y,float yaw,float h,string name)
    {
        if(!prefab)return; var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);
        NormalizeTreeUpright(go);ScaleToHeight(go,h);Bounds b=GetRendererBounds(go);go.transform.position+=Vector3.up*(y-b.min.y);
        var cc=go.AddComponent<CapsuleCollider>();cc.radius=.38f;cc.height=Mathf.Min(5.2f,h*.48f);cc.center=new Vector3(0,cc.height*.5f,0);
    }

    static void SpawnPlant(GameObject prefab,Transform parent,Material mat,float x,float z,float y,int seed,float h,string prefix)
    { var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=prefix+"_"+seed;go.transform.SetParent(parent);KeepOneVariant(go,seed);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,Deterministic01(seed*43+7)*360f,0);ScaleToHeight(go,h);foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;} }

    static void KeepOneVariant(GameObject go,int seed)
    { var rs=go.GetComponentsInChildren<Renderer>(true); if(rs.Length<=1)return; int keep=Mathf.Abs(seed)%rs.Length; for(int i=rs.Length-1;i>=0;i--)if(i!=keep)UnityEngine.Object.DestroyImmediate(rs[i].gameObject); }

    static void AssignTreeMaterials(GameObject go, EnvAssets e)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterials;
            int n = (src == null || src.Length == 0) ? 1 : src.Length;
            var dst = new Material[n];
            for (int i=0;i<n;i++)
            {
                string mn=(src!=null && i<src.Length && src[i])?src[i].name.ToLowerInvariant():"";
                if(mn.Contains("atlas")) dst[i]=e.treeAtlas; else if(mn.Contains("twig")||mn.Contains("needle")) dst[i]=e.pineLeaves; else if(mn.Contains("pine")&&mn.Contains("bark")) dst[i]=e.pineBark; else if(mn.Contains("small")&&mn.Contains("leaf")) dst[i]=e.treeSmallLeaves; else if(mn.Contains("small")&&mn.Contains("branch")) dst[i]=e.treeSmallBark; else dst[i]=(mn.Contains("leaf")||mn.Contains("canopy")||mn.Contains("branches"))?e.treeLeaves:e.treeBark;
            }
            r.sharedMaterials = dst;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;
        }
    }

    static byte[] tileCache;

    static GameObject BuildPlayer(Transform parent, Terrain terrain)
    {
        var player = new GameObject("Player - Third Person");
        player.transform.SetParent(parent);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.08f;
        cc.radius = 0.192f;
        cc.center = new Vector3(0,0.54f,0);
        cc.stepOffset = 0.228f;
        cc.slopeLimit = 52f;

        var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/Hero/Heraklios.fbx");
        GameObject visual = null;
        Animator animator = null;
        if (baseAsset)
        {
            visual = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);
            visual.name = "Warrior Visual";
            visual.transform.SetParent(player.transform,false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            ScaleToHeight(visual,1.08f);
            Bounds vb = GetRendererBounds(visual);
            visual.transform.position += Vector3.up * (-vb.min.y);
            animator = visual.GetComponentInChildren<Animator>();
            if (!animator) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Player/Hero/MMHero.controller");
            animator.applyRootMotion = false;
        }

        Vector2 originalSpawn = FindPartyStart();
        float sx = originalSpawn.x * WorldScale;
        float sz = originalSpawn.y * WorldScale;
        player.transform.position = new Vector3(sx,SampleTerrainY(terrain,sx,sz)+0.15f,sz);

        var camGO = new GameObject("Player Camera");
        camGO.transform.SetParent(parent);
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 67f;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 2500f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = player.transform.position + new Vector3(0,3.2f,-6.5f);
        camGO.transform.LookAt(player.transform.position + Vector3.up*1.5f);

        var ctrl = player.AddComponent<MMThirdPersonController>();
        ctrl.visualRoot = visual ? visual.transform : null;
        ctrl.animator = animator;
        var combat = player.AddComponent<MMHeroCombatController>(); combat.animator = animator;
        ctrl.playerCamera = cam;
        ctrl.walkSpeed = 3.4f;
        ctrl.runSpeed = 5.8f;
        ctrl.sprintSpeed = 8.2f;
        ctrl.cameraDistance = 3.48f;
        ctrl.cameraHeight = 1.23f;
        Debug.Log($"FH_PLAYER spawn={player.transform.position} visual={(visual?visual.name:"none")}");
        return player;
    }

    static Vector2 FindPartyStart()
    {
        foreach (string line in File.ReadAllLines(DecorPath).Skip(1))
        {
            string[] p=line.Split(',');
            if (p.Length<7 || !p[1].Trim().Equals("Party Start",StringComparison.OrdinalIgnoreCase)) continue;
            if (float.TryParse(p[4],NumberStyles.Float,CultureInfo.InvariantCulture,out float x) &&
                float.TryParse(p[6],NumberStyles.Float,CultureInfo.InvariantCulture,out float z))
                return new Vector2(x,-z);
        }
        return Vector2.zero;
    }

    static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__",StringComparison.OrdinalIgnoreCase));
    }

    static RuntimeAnimatorController CreatePlayerAnimatorController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Player/Hero/MMHero.controller");
        if (!controller) throw new Exception("HERAKLIOS Animator Controller is missing; run MMHeroSetup first.");
        return controller;
    }

    static void SetupLighting(Transform parent)
    {
        var sunGO=new GameObject("Sun - Directional Light");
        sunGO.transform.SetParent(parent);
        sunGO.transform.rotation=Quaternion.Euler(48f,-32f,0f);
        var sun=sunGO.AddComponent<Light>();
        sun.type=LightType.Directional;
        sun.intensity=1.15f;
        sun.color=new Color(1f,0.94f,0.82f);
        sun.shadows=LightShadows.Soft;
        RenderSettings.sun=sun;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(0.48f,0.62f,0.78f);
        RenderSettings.ambientEquatorColor=new Color(0.33f,0.42f,0.46f);
        RenderSettings.ambientGroundColor=new Color(0.16f,0.17f,0.14f);
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(0.62f,0.72f,0.75f);
        RenderSettings.fogStartDistance=520f;
        RenderSettings.fogEndDistance=1450f;
        QualitySettings.shadowDistance=350f;
        QualitySettings.shadowCascades=4;
    }

    static void RenderPreview(Scene scene, Bounds cityBounds, Terrain terrain, GameObject water, GameObject player)
    {
        var go=new GameObject("__PreviewCamera");
        var cam=go.AddComponent<Camera>();
        cam.fieldOfView=58f;
        cam.nearClipPlane=0.1f;
        cam.farClipPlane=2600f;
        cam.allowHDR=true;
        float d=72f;
        Vector3 target=(player ? player.transform.position : cityBounds.center)+Vector3.up*5.5f;
        go.transform.position=target+new Vector3(-d*0.62f,d*0.34f,-d*0.78f);
        go.transform.rotation=Quaternion.LookRotation(target-go.transform.position,Vector3.up);

        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
        cam.targetTexture=rt;
        RenderTexture.active=rt;
        cam.Render();
        var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,1280,720),0,0);
        tex.Apply();
        string previewPath="C:/MMUnityPort/Preview/FrozenHighlands.png";
        File.WriteAllBytes(previewPath,tex.EncodeToPNG());
        RenderTexture.active=null;
        cam.targetTexture=null;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log($"FH_PREVIEW {previewPath}");
    }
}
