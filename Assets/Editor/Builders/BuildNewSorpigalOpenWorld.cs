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

public static class BuildNewSorpigalOpenWorld
{
    const string ScenePath = "Assets/Scenes/Regions/NewSorpigal.unity";
    const float WorldScale = 1f;
    const float Cell = 4f * WorldScale;
    const float TerrainSize = 128f * Cell;
    const float TerrainBaseY = -24f;
    const float TerrainHeight = 320f;
    const int N = 128;
    const int TerrainResolution = 513;
    const int WaterGrid = 512;
    const float ArchitectureScale = 1f;
    // Shared blueprint reconstruction rules: terrain and visible water use one contour.
    const float WaterSurfaceY = .12f;
    const float WaterShoreThreshold = .22f;
    const float WaterBankOuterThreshold = .08f;
    const float RoadCoreRadiusSource = .18f;
    const float RoadOuterRadiusSource = .36f;

    static readonly string HeightPath = "Assets/World/NewSorpigal/Data/heightmap_u8.bin";
    static readonly string TilePath = "Assets/World/NewSorpigal/Data/tilemap_u8.bin";
    static readonly string SemPath = "Assets/World/NewSorpigal/Data/tile_semantics_u8.bin";
    static readonly string DecorPath = "Assets/World/NewSorpigal/Data/decorations.csv";
    static readonly string ObjFolder = "Assets/World/NewSorpigal/Objects";
    static readonly string MatFolder = "Assets/Materials/NewSorpigal";
    static readonly string PlacementPath = "Assets/World/NewSorpigal/Data/model_placement_audit.csv";
    static readonly string GroupPath = "Assets/World/NewSorpigal/Data/tile_groups_u8.bin";

    [MenuItem("MMUnity/Build New Sorpigal Open World")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        Directory.CreateDirectory("Assets/Materials/NewSorpigal/Buildings");
        Directory.CreateDirectory("Assets/Materials/NewSorpigal/Environment");
        Directory.CreateDirectory("Assets/World/NewSorpigal/Generated");
        Directory.CreateDirectory("Assets/Prefabs/NewSorpigal");
        Directory.CreateDirectory("Preview");

        var buildingMats = CreateBuildingMaterials();
        var env = CreateEnvironmentAssets();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "NewSorpigal";

        var root = new GameObject("New Sorpigal - Open World 1x1");
        var terrain = BuildTerrain(root.transform, env);
        Bounds cityBounds = BuildBuildings(root.transform, buildingMats, terrain);
        var water = BuildWater(root.transform, terrain, env.waterMaterial);
        BuildVegetation(root.transform, terrain, env);
        RestorePreservedBiomeFill(root.transform, terrain);
        var player = BuildPlayer(root.transform, terrain);
        SetupLighting(root.transform);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new IOException("Unity failed to save " + ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Vector3 playerPos=player.transform.position;
        RenderPreview(scene, cityBounds, terrain, water, player);
        Debug.Log($"NS_OPENWORLD_DONE scene={ScenePath} scale={WorldScale} terrain={TerrainSize}m player={playerPos}");
    }

    sealed class EnvAssets
    {
        public TerrainLayer grassLayer, dirtLayer, roadLayer, rockLayer, wetBankLayer, volcanicLayer, swampLayer;
        public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas, treeSmallBark, treeSmallLeaves, pineBark, pineLeaves, shrubMaterial, fernMaterial;
        public Material botdGrassMaterial, botdFernMaterial, botdShrubMaterial, botdCloverMaterial, barrelMaterial;
        public GameObject grassPrefab, treePrefab, treePrefab2, treePrefab3, treePrefab4, treeSmallPrefab, pinePrefab, deadTreePrefab, shrubPrefab, fernPrefab, rockA, rockB;
        public GameObject botdGrassPrefab, botdFernPrefab, botdShrubPrefab, botdCloverPrefab, botdPineGroundPrefab, botdRockPrefab, botdLogPrefab, barrelPrefab;
    }

    static Dictionary<string, Material> CreateBuildingMaterials()
    {
        var result = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        string texFolder = "Assets/Textures/NewSorpigal/Buildings";
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
        e.grassLayer = CreateTerrainLayer("Grass", "Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_CH.png", "Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_NOH.png", new Vector2(9,9), 0.78f);
        e.dirtLayer = CreateTerrainLayer("Dirt", "Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(7,7), 0.72f);
        e.roadLayer = CreateTerrainLayer("Road", "Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png", "Assets/EnvironmentAssets/UnitySamples/stone_ground_noh.png", new Vector2(6,6), 0.62f);
        e.rockLayer = CreateTerrainLayer("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png", new Vector2(8,8), 0.90f);
        e.wetBankLayer = CreateTerrainLayer("WetBank", "Assets/World/NewSorpigal/Generated/wet_bank.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(5,5), 0.78f);
        e.volcanicLayer = CreateTerrainLayer("Volcanic", "Assets/EnvironmentAssets/Biomes/ash_ground.png", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png", new Vector2(8,8), 0.72f);
        e.swampLayer = CreateTerrainLayer("Swamp", "Assets/EnvironmentAssets/Biomes/swamp_ground.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(9,9), 0.55f);

        e.waterMaterial = CreateSimpleMaterial("WaterDepth", Shader.Find("MMUnity/DepthWater"), new Color(0.08f,0.35f,0.44f,0.72f));
        e.rockMaterial = CreateTexturedMaterial("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", 0.18f);
        e.grassMaterial = CreateSimpleMaterial("GrassDetail", Shader.Find("Standard"), new Color(0.31f,0.43f,0.18f));
        e.treeBark = CreateSimpleMaterial("TreeBark", Shader.Find("Standard"), new Color(0.31f,0.18f,0.09f));
        e.treeLeaves = CreateSimpleMaterial("TreeLeaves", Shader.Find("Standard"), new Color(0.18f,0.48f,0.13f));
        e.treeAtlas = CreateTexturedMaterial("TreeAtlas", "Assets/EnvironmentAssets/Gobkit/TreeAtlas.png", 0.03f);
        e.pineBark = CreateSimpleMaterial("PineBark", Shader.Find("Standard"), new Color(0.29f,0.19f,0.11f));
        e.treeSmallBark = CreateTexturedMaterial("TreeSmallBark", "Assets/EnvironmentAssets/PolyHaven/TreeSmall02/GameLOD2/tree_small_02_LOD1.fbm/tree_small_02_branch_diff_1k.png", 0.06f);
        e.treeSmallLeaves = CreateCutoutMaterial("TreeSmallLeaves", "Assets/EnvironmentAssets/Biomes/tree_small_leaves_rgba.png", 0.42f);
        e.pineLeaves = CreateCutoutMaterial("PineLeaves", "Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/Materials/branches_albedo.tif", 0.34f);
        e.shrubMaterial = CreateCutoutMaterial("Shrub", "Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_rgba_1k.png", 0.42f);
        e.fernMaterial = CreateCutoutMaterial("Fern", "Assets/Environment/PolyHaven/Models/fern_02/fern_02_rgba_1k.png", 0.38f);
        e.botdGrassMaterial = CreateCutoutMaterial("BOTD_MeadowGrass", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/MeadowGrass_01/Materials/Meadow_Grass_01_Albedo_Opacity.tif", 0.30f);
        e.botdFernMaterial = CreateCutoutMaterial("BOTD_Fern", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Materials/Ferns_Albedo_Opacity.tif", 0.30f);
        e.botdShrubMaterial = CreateCutoutMaterial("BOTD_Broadleaf", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/BroadleafShrub_01/Materials/Broadleaf_Shrub_01_Albedo_Opacity.tif", 0.30f);
        e.botdCloverMaterial = CreateCutoutMaterial("BOTD_Clover", "Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Materials/Clover_01_Albedo_Opacity.tif", 0.28f);
        e.shrubMaterial = e.botdShrubMaterial;
        e.fernMaterial = e.botdFernMaterial;

        // Banyan and TreeSmall02 are intentionally not used in New Sorpigal.
        e.treePrefab = null;
        e.treePrefab2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_1.FBX");
        e.treePrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004/Pine_004_01.FBX");
        e.treePrefab4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_006/pine_006_01.FBX");
        e.pinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Vegetation/Trees/Pines/Pine_004_new/pine_004_4.FBX");
        e.deadTreePrefab = null; // giant dead trunk removed globally
        e.treeSmallPrefab = null;
        e.shrubPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/BroadleafShrub_01/Broadleaf_Shrub_01_Var1.FBX");
        e.fernPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Ferns/Fern_var01.FBX");
        e.rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx");
        e.rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TerrainDemoScene_HDRP/Prefabs/Rocks/Models/Rock_A_02.fbx");
        e.botdGrassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/MeadowGrass_01/Meadow_Grass_01_Var1.FBX");
        e.botdFernPrefab = e.fernPrefab;
        e.botdShrubPrefab = e.shrubPrefab;
        e.botdCloverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/Clover_01/Clover_01_Var1.FBX");
        e.botdPineGroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Forest-quixel/PineGroundScatter_01/PineGroundScatter01_Var1.FBX");
        e.botdRockPrefab = e.rockA;
        e.botdLogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Wood/Wood_Log_qdhxa/Wood_Log_qdhxa.fbx");
        e.barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/PolyHaven/Models/wine_barrel_01/wine_barrel_01_1k.fbx");
        e.barrelMaterial = CreateTexturedMaterial("TownBarrel", "Assets/Environment/PolyHaven/Models/wine_barrel_01/textures/wine_barrel_01_diff_1k.jpg", 0.18f);
        e.grassPrefab = MakeGrassPrefab(e.grassMaterial);
        return e;
    }

    static TerrainLayer CreateTerrainLayer(string name, string texturePath, string normalPath, Vector2 tileSize, float normalScale)
    {
        string path = $"{MatFolder}/Environment/Terrain_{name}.terrainlayer";
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if (!layer)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, path);
        }
        SetNormalImporter(normalPath);
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        layer.normalScale = normalScale;
        layer.tileSize = tileSize;
        layer.tileOffset = Vector2.zero;
        layer.metallic = 0f;
        layer.smoothness = name == "Road" ? 0.12f : (name == "Rock" ? 0.06f : 0.035f);
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
        string prefabPath = "Assets/Prefabs/NewSorpigal/GrassDetail.prefab";
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
    static byte TileGroup(byte tile)
    {
        if (groupCache == null) groupCache = File.ReadAllBytes(GroupPath);
        return groupCache[tile];
    }
    static bool IsVolcanic(byte tile) => TileGroup(tile) == 3;
    static bool IsSwamp(byte tile) => TileGroup(tile) == 7;


    static bool[,] correctedWaterCache;
    static bool[,] correctedRoadCache;
    static byte[] maskHeightCache;

    static float SourceSlope(byte[] heights,int x,int y)
    {
        int xl=Mathf.Max(0,x-1),xr=Mathf.Min(N-1,x+1),yd=Mathf.Max(0,y-1),yu=Mathf.Min(N-1,y+1);
        float dx=(heights[y*N+xr]-heights[y*N+xl])*.125f;
        float dy=(heights[yu*N+x]-heights[yd*N+x])*.125f;
        return Mathf.Sqrt(dx*dx+dy*dy);
    }

    static float SourceRelief(byte[] heights,int x,int y)
    {
        int min=255,max=0;
        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
        {
            int xx=Mathf.Clamp(x+ox,0,N-1),yy=Mathf.Clamp(y+oy,0,N-1),v=heights[yy*N+xx];
            if(v<min)min=v;if(v>max)max=v;
        }
        return (max-min)*.25f;
    }

    static void MarkGridLine(bool[,] mask,Vector2Int a,Vector2Int b)
    {
        int steps=Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));
        if(steps==0){mask[a.y,a.x]=true;return;}
        for(int i=0;i<=steps;i++)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.x,b.x,i/(float)steps)),0,N-1);
            int y=Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.y,b.y,i/(float)steps)),0,N-1);
            if(!correctedWaterCache[y,x]) mask[y,x]=true;
        }
    }

    static void EnsureHydroRoadMasks(byte[] tiles)
    {
        if(correctedWaterCache!=null && correctedRoadCache!=null) return;
        correctedWaterCache=new bool[N,N];
        correctedRoadCache=new bool[N,N];
        maskHeightCache=File.ReadAllBytes(HeightPath);
        for(int y=0;y<N;y++) for(int x=0;x<N;x++)
        {
            byte t=tiles[y*N+x];
            correctedWaterCache[y,x]=IsWater(t);
            correctedRoadCache[y,x]=IsRoad(t) && !correctedWaterCache[y,x];
        }
    }

    static float SampleBoolMask(bool[,] mask,float sx,float sy)
    {
        sx=Mathf.Clamp(sx,0f,N-1f);sy=Mathf.Clamp(sy,0f,N-1f);
        int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy),x1=Mathf.Min(x0+1,N-1),y1=Mathf.Min(y0+1,N-1);
        float tx=sx-x0,ty=sy-y0;
        float a=Mathf.Lerp(mask[y0,x0]?1f:0f,mask[y0,x1]?1f:0f,tx);
        float b=Mathf.Lerp(mask[y1,x0]?1f:0f,mask[y1,x1]?1f:0f,tx);
        return Mathf.Lerp(a,b,ty);
    }

    static float SampleImprovedWaterMask(byte[] tiles,float sx,float sy)
    {
        EnsureHydroRoadMasks(tiles);float baseV=SampleBoolMask(correctedWaterCache,sx,sy),sum=0f,wSum=0f;
        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
        {
            float w=(ox==0&&oy==0)?2.4f:((ox==0||oy==0)?1f:.55f);
            sum+=SampleBoolMask(correctedWaterCache,sx+ox*.34f,sy+oy*.34f)*w;wSum+=w;
        }
        return Mathf.Clamp01(Mathf.Lerp(baseV,sum/wSum,.46f));
    }

    static float SampleCorrectedRoadMask(byte[] tiles,float sx,float sy)
    {
        EnsureHydroRoadMasks(tiles);float baseV=SampleBoolMask(correctedRoadCache,sx,sy),sum=0f,wSum=0f;
        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
        { float w=(ox==0&&oy==0)?2f:1f;sum+=SampleBoolMask(correctedRoadCache,sx+ox*.28f,sy+oy*.28f)*w;wSum+=w; }
        return Mathf.Clamp01(Mathf.Lerp(baseV,sum/wSum,.30f));
    }

    static float PointSegmentDistance(float px,float py,float ax,float ay,float bx,float by)
    {
        float vx=bx-ax,vy=by-ay,wx=px-ax,wy=py-ay;
        float vv=vx*vx+vy*vy;if(vv<.0001f)return Mathf.Sqrt(wx*wx+wy*wy);
        float t=Mathf.Clamp01((wx*vx+wy*vy)/vv);float dx=px-(ax+vx*t),dy=py-(ay+vy*t);
        return Mathf.Sqrt(dx*dx+dy*dy);
    }

    static float RoadDistanceSource(byte[] tiles,float sx,float sy)
    {
        EnsureHydroRoadMasks(tiles);int cx=Mathf.RoundToInt(sx),cy=Mathf.RoundToInt(sy);float best=99f;
        int[] nx4={1,0,1,1},ny4={0,1,1,-1};
        for(int oy=-4;oy<=4;oy++)for(int ox=-4;ox<=4;ox++)
        {
            int x=cx+ox,y=cy+oy;if(x<0||y<0||x>=N||y>=N||!correctedRoadCache[y,x])continue;
            float dx=sx-x,dy=sy-y;best=Mathf.Min(best,Mathf.Sqrt(dx*dx+dy*dy));
            for(int k=0;k<4;k++){int xx=x+nx4[k],yy=y+ny4[k];if(xx<0||yy<0||xx>=N||yy>=N||!correctedRoadCache[yy,xx])continue;best=Mathf.Min(best,PointSegmentDistance(sx,sy,x,y,xx,yy));}
        }
        return best;
    }

    static float SampleRoadCorridor(byte[] tiles,float sx,float sy)
    {
        float d=RoadDistanceSource(tiles,sx,sy);
        return 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(RoadCoreRadiusSource,RoadOuterRadiusSource,d));
    }

    static float NearestRoadBlueprintY(float sx,float sy,float fallbackRaw)
    {
        // Smooth the road grade from nearby blueprint road cells so adjoining cells do not
        // create artificial steps/cliffs.  This preserves the MM6 route while giving it a
        // physically plausible continuous bed.
        int cx=Mathf.RoundToInt(sx),cy=Mathf.RoundToInt(sy);float sum=0f,wSum=0f;
        for(int oy=-3;oy<=3;oy++)for(int ox=-3;ox<=3;ox++)
        {
            int x=cx+ox,y=cy+oy;if(x<0||y<0||x>=N||y>=N||!correctedRoadCache[y,x])continue;
            float d=Mathf.Sqrt((sx-x)*(sx-x)+(sy-y)*(sy-y));if(d>3.25f)continue;
            float w=Mathf.Exp(-d*d*.70f);
            sum+=BlueprintBaseHeight(maskHeightCache[y*N+x]*.25f)*w;wSum+=w;
        }
        return wSum>.0001f?sum/wSum:BlueprintBaseHeight(fallbackRaw);
    }

    static Vector2 SourceToWorld(float sx,float sy)
    {
        // MM6 -> Unity handedness used by the OBJ exporter: X is mirrored, Z is not.
        // One source square is exactly Cell (4m) in the Unity world.
        return new Vector2((sx-64f)*Cell,(64f-sy)*Cell);
    }

    static int[,] ComputeWaterDistance(byte[] tiles)
    {
        EnsureHydroRoadMasks(tiles);var dist=new int[N,N];var q=new Queue<Vector2Int>();const int inf=9999;
        for(int y=0;y<N;y++)for(int x=0;x<N;x++)
        {
            if(!correctedWaterCache[y,x]){dist[y,x]=0;q.Enqueue(new Vector2Int(x,y));}else dist[y,x]=inf;
        }
        int[] dx={1,-1,0,0},dy={0,0,1,-1};
        while(q.Count>0)
        {
            var p=q.Dequeue();int nd=dist[p.y,p.x]+1;
            for(int k=0;k<4;k++){int nx=p.x+dx[k],ny=p.y+dy[k];if(nx<0||ny<0||nx>=N||ny>=N||nd>=dist[ny,nx])continue;dist[ny,nx]=nd;q.Enqueue(new Vector2Int(nx,ny));}
        }
        return dist;
    }

    static float SampleByteBilinear(byte[] data, float sx, float sy)
    {
        sx=Mathf.Clamp(sx,0f,N-1f);sy=Mathf.Clamp(sy,0f,N-1f);
        int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy),x1=Mathf.Min(x0+1,N-1),y1=Mathf.Min(y0+1,N-1);
        float tx=sx-x0,ty=sy-y0;
        float a=Mathf.Lerp(data[y0*N+x0],data[y0*N+x1],tx);
        float b=Mathf.Lerp(data[y1*N+x0],data[y1*N+x1],tx);
        return Mathf.Lerp(a,b,ty);
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
        float sx=x/Cell+64f;
        float sy = 64f - z / Cell;
        return new Vector2(Mathf.Clamp(sx,0f,N-1f),Mathf.Clamp(sy,0f,N-1f));
    }

    static byte TileAtSource(byte[] tiles, float sx, float sy)
    {
        int ix=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1);
        int iy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        return tiles[iy*N+ix];
    }

    static float BlueprintBaseHeight(float raw)
    {
        // True MM6 1:1 vertical scale: source outdoor height is byte * 0.25.
        // Do not amplify terrain height relative to source architecture.
        return raw;
    }

    // MM6 outdoor heights are strongly quantized. Smooth only elevated terrain so
    // hills keep their footprint but lose artificial terraces/flat mesa tops.
    static float SampleNaturalTerrainHeight(byte[] heights,float sx,float sy)
    {
        float center=SampleByteBilinear(heights,sx,sy);
        float sum=0f,weight=0f;
        for(int oy=-1;oy<=1;oy++) for(int ox=-1;ox<=1;ox++)
        {
            float w=(ox==0&&oy==0)?4f:((ox==0||oy==0)?1.5f:.75f);
            sum+=SampleByteBilinear(heights,sx+ox*.62f,sy+oy*.62f)*w;
            weight+=w;
        }
        float smooth=sum/Mathf.Max(.0001f,weight);
        float elevated=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(8f,48f,center));
        return Mathf.Lerp(center,smooth,.32f*elevated);
    }

    static float SourceFaithfulLandHeight(byte[] heights,float sx,float sy)
    {
        float sourceByte=SampleNaturalTerrainHeight(heights,sx,sy);
        float baseY=BlueprintBaseHeight(sourceByte*.25f);
        // Sub-metre relief only: breaks quantized flat crowns without changing source-square layout.
        float high=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(14f,70f,sourceByte));
        float micro=(Mathf.PerlinNoise(sx*.43f+17.2f,sy*.39f+3.1f)-.5f)*1.4f*high;
        return baseY+micro;
    }

    static float CreativeLandHeight(float raw,float sx,float sy)
    {
        float n=Mathf.Clamp01(raw/31.750f);
        float high=Mathf.SmoothStep(0.10f,1f,n);
        float h=BlueprintBaseHeight(raw);
        float macro=(Mathf.PerlinNoise(sx*.0155f+24.22f,sy*.0168f+29.54f)-.5f)*18f*high;
        float ridge=1f-Mathf.Abs(Mathf.PerlinNoise(sx*.036f+7.4f,sy*.032f+13.8f)*2f-1f);
        ridge=Mathf.Pow(Mathf.Clamp01(ridge),2.35f)*18f*Mathf.SmoothStep(.18f,1f,n);
        float detail=(Mathf.PerlinNoise(sx*.082f+3.7f,sy*.089f+17.2f)-.5f)*7.0f*(.24f+.76f*high);
        float micro=(Mathf.PerlinNoise(sx*.171f+31.1f,sy*.163f+4.6f)-.5f)*2.4f*high;
        h+=macro+ridge+detail+micro;
        return Mathf.Max(0.55f,h);
    }


    static Terrain BuildTerrain(Transform parent, EnvAssets e)
    {
        byte[] heights=File.ReadAllBytes(HeightPath),tiles=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tiles);
        int[,] waterDist=ComputeWaterDistance(tiles);string dataPath="Assets/World/NewSorpigal/Generated/NewSorpigalTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,dataPath);}
        td.heightmapResolution=TerrainResolution;td.size=new Vector3(TerrainSize,TerrainHeight,TerrainSize);td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer,e.volcanicLayer,e.swampLayer};
        int hmMax=TerrainResolution-1;float[,] hm=new float[TerrainResolution,TerrainResolution];
        for(int y=0;y<TerrainResolution;y++)for(int x=0;x<TerrainResolution;x++)
        {
            float wx=-TerrainSize*.5f+x/(float)hmMax*TerrainSize;
            float wz=-TerrainSize*.5f+y/(float)hmMax*TerrainSize;
            Vector2 src=WorldToSource(wx,wz); float sx=src.x,sy=src.y;
            float rawY=SampleByteBilinear(heights,sx,sy)*.25f;
            float landY=SourceFaithfulLandHeight(heights,sx,sy);
            float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;
            // Roads use their own nearest blueprint elevation, not neighboring hill height/noise.
            float roadDistTerrain=RoadDistanceSource(tiles,sx,sy);
            float roadTerrainBlend=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.20f,1.55f,roadDistTerrain));
            float blueprintY=BlueprintBaseHeight(rawY);
            if(roadTerrainBlend>.001f) blueprintY=Mathf.Lerp(blueprintY,NearestRoadBlueprintY(sx,sy,rawY),roadTerrainBlend);
            protectedBlend=Mathf.Max(protectedBlend,roadTerrainBlend);
            landY=Mathf.Max(Mathf.Lerp(landY,blueprintY,protectedBlend),.38f);
            float mask=SampleImprovedWaterMask(tiles,sx,sy),d=SampleDistanceBilinear(waterDist,sx,sy);
            float bed=-Mathf.Clamp(.65f+d*.78f,.65f,11.5f);
            float waterBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(WaterBankOuterThreshold,WaterShoreThreshold,mask));
            float worldY=Mathf.Lerp(landY,bed,waterBlend);
            hm[y,x]=Mathf.Clamp01((worldY-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,hm);td.alphamapResolution=512;float[,,] alpha=new float[512,512,6];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float wx=-TerrainSize*.5f+(x+.5f)/512f*TerrainSize;
            float wz=-TerrainSize*.5f+(y+.5f)/512f*TerrainSize;
            Vector2 src=WorldToSource(wx,wz);float sx=src.x,sy=src.y;byte tile=TileAtSource(tiles,sx,sy);
            float h=SourceFaithfulLandHeight(heights,sx,sy),slope=td.GetSteepness(x/511f,y/511f);
            float macro=Mathf.PerlinNoise(sx*.071f+2.8f,sy*.067f+11.4f),water=SampleImprovedWaterMask(tiles,sx,sy);
            float roadWeight=SampleRoadCorridor(tiles,sx,sy);
            if(water>WaterShoreThreshold){alpha[y,x,1]=.58f;alpha[y,x,3]=.42f;}

            else if(roadWeight>.01f)
            {
                alpha[y,x,2]=.95f*roadWeight;alpha[y,x,1]=.05f*roadWeight;alpha[y,x,0]=1f-roadWeight;
            }
            else if(IsVolcanic(tile)){alpha[y,x,4]=.84f;alpha[y,x,3]=.12f;alpha[y,x,1]=.04f;}
            else if(IsSwamp(tile)){alpha[y,x,5]=.82f;alpha[y,x,0]=.12f;alpha[y,x,1]=.06f;}
            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}
            else
            {
                float rock=Mathf.Clamp01(Mathf.InverseLerp(19f,43f,slope)*.92f+Mathf.InverseLerp(66f,118f,h)*.30f);
                float dirt=(.045f+macro*.14f)*(1f-rock),grass=Mathf.Max(0f,1f-rock-dirt);
                alpha[y,x,0]=grass;alpha[y,x,1]=dirt;alpha[y,x,3]=rock;
            }
        }
        td.SetAlphamaps(0,0,alpha);
        var go=Terrain.CreateTerrainGameObject(td);go.name="Terrain_MM_SourceGrid_1x1";go.transform.SetParent(parent);
        go.transform.position=new Vector3(-TerrainSize/2f,TerrainBaseY,-TerrainSize/2f);
        var terrain=go.GetComponent<Terrain>();terrain.drawInstanced=true;terrain.heightmapPixelError=1.5f;terrain.basemapDistance=1600f;
        terrain.detailObjectDistance=185f;terrain.detailObjectDensity=1f;terrain.treeDistance=1650f;terrain.treeBillboardDistance=260f;
        terrain.treeCrossFadeLength=22f;terrain.treeMaximumFullLODCount=180;
        BuildGrassDetails(terrain,tiles,e.grassPrefab);EditorUtility.SetDirty(td);return terrain;
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
        if(!grassPrefab)return;EnsureHydroRoadMasks(tiles);var td=terrain.terrainData;td.SetDetailResolution(512,16);
        var lush=new DetailPrototype{prototype=grassPrefab,usePrototypeMesh=true,renderMode=DetailRenderMode.VertexLit,minWidth=.44f,maxWidth=.94f,minHeight=.49f,maxHeight=1.20f,noiseSpread=.31f,healthyColor=new Color(.39f,.58f,.24f),dryColor=new Color(.48f,.43f,.21f)};
        var meadow=new DetailPrototype{prototype=grassPrefab,usePrototypeMesh=true,renderMode=DetailRenderMode.VertexLit,minWidth=.31f,maxWidth=.68f,minHeight=.75f,maxHeight=1.53f,noiseSpread=.47f,healthyColor=new Color(.31f,.50f,.20f),dryColor=new Color(.57f,.50f,.27f)};
        td.detailPrototypes=new[]{lush,meadow};int[,] dense=new int[512,512],tall=new int[512,512];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float wx=-TerrainSize*.5f+(x+.5f)/512f*TerrainSize;
            float wz=-TerrainSize*.5f+(y+.5f)/512f*TerrainSize;
            Vector2 src=WorldToSource(wx,wz);float sx=src.x,sy=src.y;byte t=TileAtSource(tiles,sx,sy);float slope=td.GetSteepness(x/511f,y/511f);
            float water=SampleImprovedWaterMask(tiles,sx,sy),roadDist=RoadDistanceSource(tiles,sx,sy);
            bool ok=water<.12f&&roadDist>1.05f&&!IsDirt(t)&&!IsVolcanic(t)&&!IsSwamp(t)&&slope<34f;if(!ok)continue;
            float n=Mathf.PerlinNoise(sx*.093f+5.7f,sy*.087f+1.9f),patch=Mathf.PerlinNoise(sx*.037f+18.4f,sy*.041f+3.2f);
            dense[y,x]=Mathf.Clamp(Mathf.RoundToInt(6f+n*5.5f+patch*2f),5,13);
            if(n>.30f&&slope<24f)tall[y,x]=n>.72f?5:(n>.50f?3:2);
        }
        td.SetDetailLayer(0,0,0,dense);td.SetDetailLayer(0,0,1,tall);
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

    static GameObject BuildWater(Transform parent,Terrain terrain,Material material)
    {
        byte[] tiles=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tiles);int grid=WaterGrid;float step=TerrainSize/grid,origin=-TerrainSize*.5f;
        var verts=new List<Vector3>(900000);var uvs=new List<Vector2>(900000);var tris=new List<int>(1200000);const float th=WaterShoreThreshold;
        Vector2 Edge(Vector2 a,Vector2 b,float va,float vb){float u=Mathf.Abs(vb-va)<.0001f?.5f:Mathf.Clamp01((th-va)/(vb-va));return Vector2.Lerp(a,b,u);}
        void Emit(List<Vector2> poly)
        {
            if(poly.Count<3)return;int start=verts.Count;
            foreach(var q in poly){verts.Add(new Vector3(q.x,WaterSurfaceY,q.y));uvs.Add(new Vector2((q.x-origin)*.018f,(q.y-origin)*.018f));}
            for(int k=1;k<poly.Count-1;k++){tris.Add(start);tris.Add(start+k+1);tris.Add(start+k);}
        }
        for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
        {
            float x0=origin+x*step,x1=x0+step,z0=origin+y*step,z1=z0+step;
            Vector2[] p4={new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)};float[] v=new float[4];int bits=0;
            for(int i=0;i<4;i++){Vector2 src=WorldToSource(p4[i].x,p4[i].y);v[i]=SampleImprovedWaterMask(tiles,src.x,src.y);if(v[i]>=th)bits|=1<<i;}
            if(bits==0)continue;if(bits==15){Emit(new List<Vector2>(p4));continue;}
            if((bits==5||bits==10)&&((v[0]+v[1]+v[2]+v[3])*.25f)<th)
            {
                for(int i=0;i<4;i++)if((bits&(1<<i))!=0){int prev=(i+3)%4,next=(i+1)%4;Emit(new List<Vector2>{p4[i],Edge(p4[i],p4[next],v[i],v[next]),Edge(p4[prev],p4[i],v[prev],v[i])});}
                continue;
            }
            var poly=new List<Vector2>(8);
            for(int i=0;i<4;i++){int j=(i+1)%4;bool a=v[i]>=th,b=v[j]>=th;if(a)poly.Add(p4[i]);if(a!=b)poly.Add(Edge(p4[i],p4[j],v[i],v[j]));}
            Emit(poly);
        }

        string meshPath="Assets/World/NewSorpigal/Generated/WaterSurface.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        if(material)
        {
            material.renderQueue=3000;if(material.HasProperty("_ShallowColor"))material.SetColor("_ShallowColor",new Color(.08f,.58f,.67f,.50f));
            if(material.HasProperty("_DeepColor"))material.SetColor("_DeepColor",new Color(.02f,.16f,.30f,.84f));
            if(material.HasProperty("_DepthRange"))material.SetFloat("_DepthRange",11f);if(material.HasProperty("_FoamDepth"))material.SetFloat("_FoamDepth",.55f);
        }
        var go=new GameObject("Water - Smoothed Original River Pond Shore");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
        Debug.Log($"NS_WATER_SMOOTH verts={verts.Count} tris={tris.Count/3}");return go;
    }

    static void RestorePreservedBiomeFill(Transform parent, Terrain terrain)
    {
        const string path="Assets/World/NewSorpigal/Generated/PreservedBiomeFill.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(!prefab)return;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab); go.name="Vegetation - Preserved User Additions"; go.transform.SetParent(parent,false);
        var stale=go.transform.Find("Volcanic Dead Trees"); if(stale) UnityEngine.Object.DestroyImmediate(stale.gameObject);
        int grounded=0;
        foreach(Transform group in go.transform) foreach(Transform item in group)
        {
            // PreservedBiomeFill was captured from the old x3 scene; convert positions back to the current 1:1 map.
            Vector3 lp=item.localPosition; lp.x/=3f; lp.z/=3f; item.localPosition=lp;
            var rs=item.GetComponentsInChildren<Renderer>(true); if(rs.Length==0)continue;
            Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
            float y=SampleTerrainY(terrain,b.center.x,b.center.z); item.position+=Vector3.up*(y-b.min.y); grounded++;
        }
        Debug.Log("NS_PRESERVED_USER_VEGETATION grounded="+grounded);
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
        string lookup=Path.GetFileNameWithoutExtension(name);
        int seg=lookup.IndexOf("__SEG_",StringComparison.OrdinalIgnoreCase);
        if(seg>=0) lookup=lookup.Substring(0,seg);
        placementCache.TryGetValue(lookup,out var pi); return pi;
    }
    static bool WaterAffiliated(string name){var p=PlacementFor(name); return p!=null && p.kind!="LAND";}

    static string CompositeStructureKey(string name)
    {
        string n=Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
        int seg=n.IndexOf("__seg_",StringComparison.OrdinalIgnoreCase);
        if(seg>=0) return "Wall_"+n.Substring(0,seg);
        if(n.StartsWith("064_m063_sgntavernw")||n.StartsWith("080_m079_tavbckw")||n.StartsWith("081_m080_tavfrntw")) return "TavernW";
        if(n.StartsWith("083_m082_smkeepbck")||n.StartsWith("084_m083_smkeepfrnt")) return "SmallKeep";
        if(n.StartsWith("071_m070_evilkc")||n.StartsWith("072_m071_evilkl")||n.StartsWith("073_m072_evilkr")) return "EvilKeep";
        if(n.StartsWith("044_m043_sgnstble")||n.StartsWith("050_m049_stablese")) return "StablesE";
        return null;
    }

    static bool IsFusedWallSourcePath(string path)
    {
        string n=Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        // The raw MM6 wall objects are the authoritative placement. The old __SEG_* exports
        // were generated with mirrored X/Z and are the source of the walls appearing far away.
        return n.Contains("__seg_") && (n.Contains("_wopr") || n.Contains("_wopl") || n.Contains("_wer"));
    }

    static Bounds BuildBuildings(Transform parent, Dictionary<string, Material> mats, Terrain terrain)
    {
        var group=new GameObject("Architecture - MM6 Original Layout Expanded");
        group.transform.SetParent(parent);
        var paths=AssetDatabase.FindAssets("t:GameObject",new[]{ObjFolder})
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p=>p.EndsWith(".obj",StringComparison.OrdinalIgnoreCase))
            .Where(p=>!Path.GetFileName(p).StartsWith("000_"))
            .Where(p=>!IsFusedWallSourcePath(p))
            .OrderBy(p=>p).ToArray();
        var instances=new List<GameObject>();
        foreach(string path in paths)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(!asset) continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name=Path.GetFileNameWithoutExtension(path);
            go.transform.SetParent(group.transform,true);
            instances.Add(go);
        }
        var compositeBounds=new Dictionary<string,Bounds>();
        var compositeMembers=new Dictionary<string,List<GameObject>>();
        foreach(var go in instances)
        {
            string key=CompositeStructureKey(go.name); if(key==null) continue;
            Bounds b=GetRendererBounds(go);
            if(!compositeBounds.ContainsKey(key)){compositeBounds[key]=b;compositeMembers[key]=new List<GameObject>();}
            else {Bounds cb=compositeBounds[key];cb.Encapsulate(b);compositeBounds[key]=cb;}
            compositeMembers[key].Add(go);
        }
        foreach(var go in instances)
        {
            Bounds original=GetRendererBounds(go);
            // Exact MM6 1:1 geometry. Never scale or move source architecture in X/Z.
            go.transform.localScale=Vector3.one;
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var src=r.sharedMaterials;var dst=new Material[src.Length];
                for(int i=0;i<src.Length;i++){string mk=src[i]?src[i].name.ToLowerInvariant():"pending";dst[i]=mats.TryGetValue(mk,out var m)?m:mats["pending"];}
                r.sharedMaterials=dst;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
            }
            foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
            { if(!mf.sharedMesh)continue;var mc=mf.GetComponent<MeshCollider>();if(!mc)mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh; }
        }
        int grounded=0;
        foreach(var go in instances)
        {
            if(CompositeStructureKey(go.name)!=null) continue;
            Bounds b=GetRendererBounds(go);var placement=PlacementFor(go.name);
            if(placement!=null&&b.size.y>0.15f)
            {
                float surface=WaterAffiliated(go.name)?WaterSurfaceY:SampleTerrainY(terrain,b.center.x,b.center.z);
                float desiredBase=surface+placement.sourceBaseOffset*go.transform.localScale.x;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);grounded++;
            }
        }
        foreach(var kv in compositeMembers)
        {
            var members=kv.Value;Bounds b=GetRendererBounds(members[0]);
            for(int i=1;i<members.Count;i++)b.Encapsulate(GetRendererBounds(members[i]));
            float surface=SampleTerrainY(terrain,b.center.x,b.center.z);
            float baseOffset=0f;bool have=false;
            foreach(var m in members){var pl=PlacementFor(m.name);if(pl!=null){baseOffset=have?Mathf.Min(baseOffset,pl.sourceBaseOffset):pl.sourceBaseOffset;have=true;}}
            float desiredBase=surface+baseOffset*members[0].transform.localScale.x;
            float dy=desiredBase-b.min.y;
            foreach(var m in members)m.transform.position+=Vector3.up*dy;
            grounded+=members.Count;
            Debug.Log($"NS_COMPOSITE {kv.Key} parts={members.Count} pivot={compositeBounds[kv.Key].center} dy={dy:F3}");
        }
        Bounds all=new Bounds(Vector3.zero,Vector3.zero);bool hasBounds=false;
        foreach(var go in instances){Bounds b=GetRendererBounds(go);if(!hasBounds){all=b;hasBounds=true;}else all.Encapsulate(b);}
        if(group.transform.childCount!=paths.Length)throw new InvalidOperationException($"Architecture import incomplete: expected {paths.Length}, instantiated {group.transform.childCount}");
        AddArchitectureSolidCores(group.transform);
        Physics.SyncTransforms();
        Debug.Log($"NS_BUILDINGS count={paths.Length} grounded={grounded} architectureScale={ArchitectureScale} composites={compositeMembers.Count} bounds={all}");
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
            var coreRenderer=core.GetComponent<MeshRenderer>(); coreRenderer.sharedMaterial=wall; coreRenderer.enabled=false; // collider-only core: never show axis-aligned box through irregular/rotated buildings
            // Keep the box collider: MM6 exterior houses are not walk-in meshes.
            ncore++;
        }
        Debug.Log("ARCHITECTURE_SOLID_1X1 "+ncore);
    }


    static float ArchitectureScaleFactor(string name, Bounds raw)
    {
        return 1f;
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


    static bool IsSuitableNaturalSpot(Terrain terrain,Transform architectureRoot,float x,float z,bool tree,bool requireGrass)
    {
        if(tileCache==null)tileCache=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tileCache);
        Vector3 tp=terrain.transform.position;float nx=(x-tp.x)/terrain.terrainData.size.x,nz=(z-tp.z)/terrain.terrainData.size.z;
        if(nx<.01f||nz<.01f||nx>.99f||nz>.99f)return false;
        Vector2 src=WorldToSource(x,z);byte t=TileAtSource(tileCache,src.x,src.y);float water=SampleImprovedWaterMask(tileCache,src.x,src.y);
        float roadClear=RoadDistanceSource(tileCache,src.x,src.y);if(water>.16f||roadClear<(tree?1.35f:.78f))return false;
        if(requireGrass&&TileGroup(t)!=0)return false;
        if(tree)
        {
            float edge=Mathf.Max(Mathf.Max(SampleImprovedWaterMask(tileCache,src.x+.45f,src.y),SampleImprovedWaterMask(tileCache,src.x-.45f,src.y)),Mathf.Max(SampleImprovedWaterMask(tileCache,src.x,src.y+.45f),SampleImprovedWaterMask(tileCache,src.x,src.y-.45f)));
            if(edge>.28f)return false;
        }
        float steep=terrain.terrainData.GetSteepness(nx,nz);if(steep>(tree?31f:43f))return false;
        if(NearArchitecture(architectureRoot,x,z,tree?3.5f:1.4f))return false;return true;
    }

    static GameObject PickTreePrefab(EnvAssets e,int seed)
    {
        // New Sorpigal: never use Banyan or the sideways TreeSmall02 prefab.
        // Use only upright temperate trees until the imported realistic tree packs are wired in.
        int k=Mathf.Abs(seed)%4;
        if(k==0 && e.pinePrefab) return e.pinePrefab;
        if(k==1 && e.treePrefab2) return e.treePrefab2;
        if(k==2 && e.treePrefab3) return e.treePrefab3;
        if(e.treePrefab4) return e.treePrefab4;
        return e.treePrefab2 ? e.treePrefab2 : (e.pinePrefab ? e.pinePrefab : e.treePrefab3);
    }

    static bool KeepTreeAnchor(int seed)
    { return true; }

    static int NearbyTreeAnchorCount(string[] lines,float x,float z)
    {
        int count=0;
        for(int j=1;j<lines.Length;j++)
        {
            string[] q=lines[j].Split(',');if(q.Length<10)continue;
            string name=q[1].Trim().ToLowerInvariant();if(!(name.StartsWith("6tree")||name.StartsWith("tree")))continue;
            if(!float.TryParse(q[4],NumberStyles.Float,CultureInfo.InvariantCulture,out float ox))continue;
            if(!float.TryParse(q[6],NumberStyles.Float,CultureInfo.InvariantCulture,out float oz))continue;
            float d=Vector2.Distance(new Vector2(x,z),new Vector2(ox*WorldScale,-oz*WorldScale));
            if(d>1f&&d<90f)count++;
        }
        return count;
    }

    static void BuildVegetation(Transform parent, Terrain terrain, EnvAssets e)
    {
        var root=new GameObject("Vegetation - MM Anchors + Natural Groves"); root.transform.SetParent(parent);
        var trees=new GameObject("Trees"); trees.transform.SetParent(root.transform);
        var rocks=new GameObject("Rocks"); rocks.transform.SetParent(root.transform);
        var understory=new GameObject("Understory"); understory.transform.SetParent(root.transform);
        var props=new GameObject("Props - MM Anchors"); props.transform.SetParent(root.transform);
        Transform architectureRoot=parent.Find("Architecture - MM6 Original Layout Expanded");
        string[] lines=File.ReadAllLines(DecorPath);byte[] decorTiles=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(decorTiles);
        int treeCount=0,extraTrees=0,shrubs=0,ferns=0,rockCount=0,flowerCount=0,barrelCount=0,skippedTrees=0,skippedRocks=0;
        for(int i=1;i<lines.Length;i++)
        {
            string[] q=lines[i].Split(','); if(q.Length<10) continue;
            string name=q[1].Trim().ToLowerInvariant();
            if(!float.TryParse(q[4],NumberStyles.Float,CultureInfo.InvariantCulture,out float ox))continue;
            if(!float.TryParse(q[6],NumberStyles.Float,CultureInfo.InvariantCulture,out float oz))continue;
            float x=ox*WorldScale,z=-oz*WorldScale; float yaw=Deterministic01(i*13+7)*360f;
            if(name.StartsWith("6tree")||name.StartsWith("tree"))
            {
                Vector2 anchorSrc=WorldToSource(x,z); byte anchorTile=TileAtSource(decorTiles,anchorSrc.x,anchorSrc.y);
                bool volcanicAnchor=IsVolcanic(anchorTile);
                var anchorPrefab=(volcanicAnchor && e.deadTreePrefab)?e.deadTreePrefab:PickTreePrefab(e,i); if(!anchorPrefab || !KeepTreeAnchor(i)){skippedTrees++;continue;}
                // Exact MM6 source anchor: keep it at its recorded X/Z and ground it vertically.
                SpawnTree(anchorPrefab,trees.transform,e,x,z,SampleTerrainY(terrain,x,z),yaw,Mathf.Lerp(7.5f,11.5f,Deterministic01(i*19+3)),name+"_"+i.ToString("000")); treeCount++;
                int neighbours=NearbyTreeAnchorCount(lines,x,z); int extras=0; // Synthetic GroveTree fill disabled: source anchors + preserved/manual vegetation are authoritative.
                for(int k=0;k<extras;k++)
                {
                    float a=Deterministic01(i*101+k*17+5)*Mathf.PI*2f; float maxRad=neighbours>=3?72f:(neighbours>=1?48f:24f); float rad=Mathf.Lerp(5f,maxRad,Mathf.Sqrt(Deterministic01(i*107+k*23+9)));
                    float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;
                    if(!IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,true,true))continue;
                    SpawnTree(PickTreePrefab(e,i*109+k),trees.transform,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(i*109+k)*360f,Mathf.Lerp(6.8f,10.8f,Deterministic01(i*113+k)),"GroveTree_"+i.ToString("000")+"_"+k); extraTrees++;
                }
                if(e.shrubPrefab && (i%2==0 || NearbyTreeAnchorCount(lines,x,z)>=2))
                {
                    int n=1+(i%2); for(int k=0;k<n;k++){float a=Deterministic01(i*127+k)*Mathf.PI*2f,rad=Mathf.Lerp(3f,12f,Deterministic01(i*131+k));float ex=x+Mathf.Cos(a)*rad,ez=z+Mathf.Sin(a)*rad;if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,false,true)){SpawnPlant(e.shrubPrefab,understory.transform,e.shrubMaterial,ex,ez,SampleTerrainY(terrain,ex,ez),i*17+k,Mathf.Lerp(0.75f,1.55f,Deterministic01(i*137+k)),"Shrub");shrubs++;}}
                }
                if(e.fernPrefab && (i%3==0 || NearbyTreeAnchorCount(lines,x,z)>=3))
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
            else if(name.StartsWith("6flower") && e.botdCloverPrefab)
            {
                // Preserve MM6 flower anchors, but use a modern ground-cover mesh at source 1:1 map scale.
                Vector2 fs=WorldToSource(x,z);float fw=SampleImprovedWaterMask(decorTiles,fs.x,fs.y),fr=RoadDistanceSource(decorTiles,fs.x,fs.y);
                if(fw<.14f&&fr>.65f){SpawnPlant(e.botdCloverPrefab,understory.transform,e.botdCloverMaterial,x,z,SampleTerrainY(terrain,x,z),i*211,Mathf.Lerp(.42f,.62f,Deterministic01(i*59)),"MMFlower");flowerCount++;}
            }
            else if(name.Contains("bigbarel") && e.barrelPrefab)
            {
                // Original MM6 barrel anchors become real 3D barrels rather than tiny/missing sprites.
                SpawnTownProp(e.barrelPrefab,props.transform,e.barrelMaterial,x,z,SampleTerrainY(terrain,x,z),yaw,1.30f,"MMBarrel_"+i.ToString("000"));barrelCount++;
            }
        }
        int fillObjects=0; // random forest fill disabled
        int riverObjects=0; // random riverside fill disabled
        int volcanicDead=0; // random dead-tree fill disabled
        Debug.Log($"VEGETATION_REALISTIC anchorTrees={treeCount} groveTrees={extraTrees} shrubs={shrubs} ferns={ferns} rocks={rockCount} flowers={flowerCount} barrels={barrelCount} fillObjects={fillObjects} riverObjects={riverObjects} volcanicDead={volcanicDead} skippedTrees={skippedTrees} skippedRocks={skippedRocks}");
    }

    static int BuildNaturalFill(Terrain terrain,Transform architectureRoot,Transform trees,Transform rocks,Transform understory,EnvAssets e)
    {
        int added=0; const int grid=34; float step=TerrainSize/grid;
        for(int gy=0;gy<grid;gy++) for(int gx=0;gx<grid;gx++)
        {
            int seed=90000+gy*grid+gx;
            float x=-TerrainSize*.5f+(gx+.5f)*step+(Deterministic01(seed*11+3)-.5f)*step*.70f;
            float z=-TerrainSize*.5f+(gy+.5f)*step+(Deterministic01(seed*13+7)-.5f)*step*.70f;
            Vector2 src=WorldToSource(x,z);
            float forest=Mathf.PerlinNoise(src.x*.047f+21.4f,src.y*.052f+6.8f);
            if(forest>.64f && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,true,true))
            {
                SpawnTree(PickTreePrefab(e,seed),trees,e,x,z,SampleTerrainY(terrain,x,z),Deterministic01(seed*17)*360f,Mathf.Lerp(8.0f,14.5f,Deterministic01(seed*19)),"ForestFill_"+seed); added++;
                if(forest>.80f)
                {
                    float a=Deterministic01(seed*23)*Mathf.PI*2f, r=Mathf.Lerp(6f,18f,Deterministic01(seed*29));
                    float ex=x+Mathf.Cos(a)*r,ez=z+Mathf.Sin(a)*r;
                    if(IsSuitableNaturalSpot(terrain,architectureRoot,ex,ez,true,true)){SpawnTree(PickTreePrefab(e,seed+31),trees,e,ex,ez,SampleTerrainY(terrain,ex,ez),Deterministic01(seed*31)*360f,Mathf.Lerp(7.0f,12.5f,Deterministic01(seed*37)),"ForestFillCompanion_"+seed);added++;}
                }
            }
            else if(forest>.50f && e.botdShrubPrefab && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,true))
            { SpawnPlant(e.botdShrubPrefab,understory,e.botdShrubMaterial,x,z,SampleTerrainY(terrain,x,z),seed,Mathf.Lerp(.75f,1.65f,Deterministic01(seed*41)),"ForestShrub"); added++; }
            float ground=Mathf.PerlinNoise(src.x*.089f+4.2f,src.y*.083f+33.7f);
            if(ground>.77f && e.botdGrassPrefab && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,true))
            { SpawnPlant(e.botdGrassPrefab,understory,e.botdGrassMaterial,x,z,SampleTerrainY(terrain,x,z),seed+1,Mathf.Lerp(.35f,.85f,Deterministic01(seed*43)),"MeadowClump"); added++; }
            float rockN=Mathf.PerlinNoise(src.x*.071f+71.1f,src.y*.067f+12.6f);
            if(rockN>.88f && e.botdRockPrefab && IsSuitableNaturalSpot(terrain,architectureRoot,x,z,false,false))
            { SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,x,z,SampleTerrainY(terrain,x,z),seed+2,Mathf.Lerp(.55f,1.9f,Deterministic01(seed*47)),"NaturalRock"); added++; }
        }
        return added;
    }


    static int BuildVolcanicDeadFill(Terrain terrain,Transform architectureRoot,Transform trees,EnvAssets e)
    {
        if(!e.deadTreePrefab)return 0; byte[] tiles=File.ReadAllBytes(TilePath); int added=0;
        for(int sy=1;sy<N-1;sy++) for(int sx=1;sx<N-1;sx++)
        {
            byte t=tiles[sy*N+sx]; if(!IsVolcanic(t))continue; int seed=260000+sy*257+sx*17;
            if(Mathf.Abs(seed)%13!=0)continue;
            float jx=(Deterministic01(seed*7)-.5f)*.62f, jy=(Deterministic01(seed*11)-.5f)*.62f;
            Vector2 w=SourceToWorld(sx+jx,sy+jy);
            if(!IsSuitableNaturalSpot(terrain,architectureRoot,w.x,w.y,true,false))continue;
            SpawnTree(e.deadTreePrefab,trees,e,w.x,w.y,SampleTerrainY(terrain,w.x,w.y),Deterministic01(seed*13)*360f,Mathf.Lerp(5.5f,9.5f,Deterministic01(seed*19)),"VolcanicDead_"+sx+"_"+sy); added++;
        }
        return added;
    }

    static int BuildRiverside(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e)
    {
        if(tileCache==null)tileCache=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tileCache);int added=0;
        var roots=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Roots/Roots_System_01_Prefab.prefab");
        var debris=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Sticks_debris/sticks_debris_00_prefab.prefab");
        int[] dx={1,-1,0,0},dy={0,0,1,-1};
        for(int y=1;y<N-1&&added<260;y++)for(int x=1;x<N-1&&added<260;x++) if(correctedWaterCache[y,x])
        {
            for(int k=0;k<4&&added<260;k++)
            {
                int lx=x+dx[k],ly=y+dy[k];if(correctedWaterCache[ly,lx])continue;
                int seed=150000+y*521+x*31+k*7;if(Mathf.Abs(seed)%6!=0)continue;
                float sx=x+dx[k]*.82f+(Deterministic01(seed*13)-.5f)*.28f,sy=y+dy[k]*.82f+(Deterministic01(seed*17)-.5f)*.28f;
                Vector2 w=SourceToWorld(sx,sy);if(!IsSuitableNaturalSpot(terrain,architectureRoot,w.x,w.y,false,false))continue;
                int choice=Mathf.Abs(seed)%4;GameObject pf=null;Material mat=null;float ph=.6f;
                if(choice==0){pf=e.botdFernPrefab;mat=e.botdFernMaterial;ph=Mathf.Lerp(.45f,.95f,Deterministic01(seed*19));}
                else if(choice==1){pf=e.botdShrubPrefab;mat=e.botdShrubMaterial;ph=Mathf.Lerp(.85f,1.7f,Deterministic01(seed*19));}
                else if(choice==2){pf=e.botdCloverPrefab;mat=e.botdCloverMaterial;ph=Mathf.Lerp(.22f,.48f,Deterministic01(seed*19));}
                else {pf=e.botdGrassPrefab;mat=e.botdGrassMaterial;ph=Mathf.Lerp(.35f,.78f,Deterministic01(seed*19));}
                if(pf){SpawnPlant(pf,understory,mat,w.x,w.y,SampleTerrainY(terrain,w.x,w.y),seed,ph,"WaterEdge");added++;}
                if(seed%29==0&&e.botdRockPrefab){Vector2 rw=SourceToWorld(sx+dx[k]*.35f,sy+dy[k]*.35f);if(IsSuitableNaturalSpot(terrain,architectureRoot,rw.x,rw.y,false,false)){SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,rw.x,rw.y,SampleTerrainY(terrain,rw.x,rw.y),seed+1,Mathf.Lerp(.4f,1.25f,Deterministic01(seed*23)),"WaterRock");added++;}}
            }
        }
        return added;
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
        if(prefab!=e.deadTreePrefab)AssignTreeMaterials(go,e);
        var cc=go.AddComponent<CapsuleCollider>();cc.radius=.38f;cc.height=Mathf.Min(5.2f,h*.48f);cc.center=new Vector3(0,cc.height*.5f,0);
    }

    static void SpawnTownProp(GameObject prefab,Transform parent,Material mat,float x,float z,float y,float yaw,float targetHeight,string name)
    {
        if(!prefab)return;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,yaw,0);ScaleToHeight(go,targetHeight);Bounds b=GetRendererBounds(go);go.transform.position+=Vector3.up*(y-b.min.y);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){if(mat)r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
    }

    static void SpawnPlant(GameObject prefab,Transform parent,Material mat,float x,float z,float y,int seed,float h,string prefix)
    {
        if(!prefab)return;var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=prefix+"_"+seed;go.transform.SetParent(parent);go.transform.position=new Vector3(x,y,z);go.transform.rotation=Quaternion.Euler(0,Deterministic01(seed*43+7)*360f,0);
        string p=prefix.ToLowerInvariant();float smallSpriteBoost=(p.Contains("grass")||p.Contains("shrub")||p.Contains("fern")||p.Contains("clover")||p.Contains("flower")||p.Contains("wateredge"))?1.30f:1f;
        ScaleToHeight(go,h*smallSpriteBoost);Bounds b=GetRendererBounds(go);go.transform.position+=Vector3.up*(y-b.min.y);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){if(mat)r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
    }

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
                if(mn.Contains("atlas")) dst[i]=e.treeAtlas;
                else if(mn.Contains("branch")||mn.Contains("twig")||mn.Contains("needle")||mn.Contains("leaf")||mn.Contains("canopy")) dst[i]=e.pineLeaves;
                else if(mn.Contains("trunk")||mn.Contains("stump")||mn.Contains("bark")) dst[i]=e.pineBark;
                else dst[i]=e.treeBark;
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
            visual.name = "Runner Visual";
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
        cam.nearClipPlane = 0.025f;
        cam.farClipPlane = 2500f;
        cam.depthTextureMode |= DepthTextureMode.Depth;
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = player.transform.position + new Vector3(0,1.07f,-2.17f);
        camGO.transform.LookAt(player.transform.position + Vector3.up*0.50f);

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
        Debug.Log($"NS_PLAYER spawn={player.transform.position} visual={(visual?visual.name:"none")}");
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
        return new Vector2(-76f,88.43f);
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
        string previewPath="C:/MMUnityPort/Preview/NewSorpigal.png";
        File.WriteAllBytes(previewPath,tex.EncodeToPNG());
        RenderTexture.active=null;
        cam.targetTexture=null;
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log($"NS_PREVIEW {previewPath}");
    }
}
