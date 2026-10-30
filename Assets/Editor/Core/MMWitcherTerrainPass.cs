using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class MMWitcherTerrainPass
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
    public const int N=128;
    public const int Green=0,Snow=1,Sand=2,Volcanic=3,DryAsh=4,Mud=5,Marsh=6,Road=7,Rock=8;
    const string Dir="Assets/Materials/WitcherWorld";
    public static string BiomeName(int i)
    {
        switch(i){case Green:return "GREEN";case Snow:return "SNOW";case Sand:return "DESERT_SAND";
            case Volcanic:return "DARK_VOLCANIC";case DryAsh:return "LIGHT_DRY_ASH";case Mud:return "MUD";
            case Marsh:return "MARSH";case Road:return "ROAD";case Rock:return "ROCK";default:return "UNKNOWN";}
    }

    static int CanonicalLand(string zone,byte g)
    {
        switch(zone)
        {
            // Canonical GREEN world: all non-water land uses the same green family.
            case "NewSorpigal":
            case "CastleIronfist":
            case "MistyIslands":
            case "BootlegBay":
            case "EelInfestedWaters":
            case "SilverCove":
                return Green;

            // Mire is green/wet forest, with only true swamp source cells staying marshy.
            case "MireOfTheDamned":
                return g==7?Marsh:Green;

            // Dragonsand is the yellow desert, but preserve explicit green source patches/islands.
            case "Dragonsand":
                return g==0?Green:Sand;

            // Canonical grey volcanic/dead zones.
            case "HermitsIsle":
            case "SweetWater":
                return Volcanic;

            // Canonical Witcher-like brown/muddy belt. Preserve only explicit green source patches.
            case "ParadiseValley":
                return Mud;
            case "Blackshire":
            case "FreeHaven":
                return g==0?Green:Mud;
            case "Kriegspire":
                return g==3?Volcanic:Mud;

            // Frozen Highlands is the white snow biome.
            case "FrozenHighlands":
                return Snow;
        }
        if(g==0)return Green;if(g==1)return Snow;if(g==2)return Sand;if(g==3)return Volcanic;
        if(g==4)return Mud;if(g==6)return Volcanic;if(g==7)return Marsh;return Green;
    }

    static int ShoreClass(string zone)
    {
        if(zone=="MireOfTheDamned"||zone=="Blackshire"||zone=="FreeHaven")return Mud;
        if(zone=="SweetWater"||zone=="FrozenHighlands"||zone=="Kriegspire")return Rock;
        return Sand;
    }

    public static int ExpectedLayerForCell(string zone,byte[] tile,byte[] grp,byte[] sem,int sx,int sy)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);
        byte raw=tile[sy*N+sx],f=sem[raw],g=grp[raw];
        if((f&8)!=0||(g>=8&&g<255))return Road;
        if((f&1)!=0)return Rock;
        if((f&2)!=0||g==5)return ShoreClass(zone);
        return CanonicalLand(zone,g);
    }
    [MenuItem("MMUnity/Witcher World - Harmonize Terrain Materials")]
    public static void ApplyAll()
    {
        Directory.CreateDirectory(Dir);
        var layers=BuildLayers();int done=0;
        foreach(var z in Zones)done+=Apply(z,layers);
        AssetDatabase.SaveAssets();
        Debug.Log("WITCHER_TERRAIN_DONE zones="+done+" canonicalFamilies=9 sourceDriven=true");
    }

    static TerrainLayer[] BuildLayers()
    {
        return new[]{
            Layer("Green","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_nor_gl_1k.jpg",8.5f,.025f),
            Layer("Snow","Assets/EnvironmentAssets/Biomes/snow.png","",10f,.045f),
            Layer("DesertSand","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_nor_gl_1k.jpg",8f,.025f),
            Layer("DarkVolcanic","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",7f,.02f),
            Layer("LightDryAsh","Assets/EnvironmentAssets/Biomes/ash_ground.png","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",8f,.025f),
            Layer("Mud","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg",7f,.06f),
            Layer("Marsh","Assets/EnvironmentAssets/Biomes/swamp_ground.png","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg",8f,.08f),
            Layer("Road","Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png","Assets/EnvironmentAssets/UnitySamples/stone_ground_noh.png",6f,.13f),
            Layer("Rock","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",8f,.025f)
        };
    }

    static TerrainLayer Layer(string key,string diff,string normal,float tile,float smooth)
    {
        string path=$"{Dir}/World_{key}.terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
        l.name="World_"+key;l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(diff);
        l.normalMapTexture=string.IsNullOrEmpty(normal)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
        l.tileSize=new Vector2(tile,tile);l.tileOffset=Vector2.zero;l.metallic=0f;l.smoothness=smooth;l.normalScale=1f;
        EditorUtility.SetDirty(l);return l;
    }
    static void PaintTruth(float[,,] a,int y,int x,int expected,float steep,float noise)
    {
        int l=a.GetLength(2);
        for(int k=0;k<l;k++)a[y,x,k]=0f;
        if(expected==Road){a[y,x,Road]=1f;return;}
        if(expected==Rock){a[y,x,Rock]=.76f;a[y,x,Sand]=.24f;return;}
        float rock=Mathf.Clamp01(Mathf.InverseLerp(38f,68f,steep))*.12f;
        float secondary=.025f+.035f*noise;
        a[y,x,expected]=1f-rock-secondary;
        a[y,x,Rock]+=rock;
        if(expected==Green)a[y,x,Mud]+=secondary;
        else if(expected==Mud)a[y,x,Green]+=secondary;
        else if(expected==Marsh)a[y,x,Mud]+=secondary;
        else if(expected==Snow)a[y,x,Rock]+=secondary;
        else if(expected==Sand)a[y,x,Rock]+=secondary;
        else if(expected==DryAsh)a[y,x,Rock]+=secondary;
        else if(expected==Volcanic)a[y,x,Rock]+=secondary;
        else a[y,x,expected]+=secondary;
    }

    static int Apply(Z z,TerrainLayer[] layers)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t){Debug.LogError("WITCHER_TERRAIN_NO_TERRAIN "+z.key);return 0;}
        string d=$"Assets/World/{z.key}/Data";
        byte[] tile=File.ReadAllBytes(d+"/tilemap_u8.bin"),grp=File.ReadAllBytes(d+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var td=t.terrainData;td.terrainLayers=layers;if(td.alphamapResolution!=512)td.alphamapResolution=512;
        int r=td.alphamapResolution,l=layers.Length;var a=new float[r,r,l];
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            int sx=Mathf.Clamp(Mathf.FloorToInt((x+.5f)/r*N),0,N-1);
            int sy=Mathf.Clamp(N-1-Mathf.FloorToInt((y+.5f)/r*N),0,N-1);
            int expected=ExpectedLayerForCell(z.key,tile,grp,sem,sx,sy);
            float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));
            float noise=Mathf.PerlinNoise((sx+17)*.13f,(sy+31)*.13f);
            PaintTruth(a,y,x,expected,steep,noise);
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);t.Flush();
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"WITCHER_TERRAIN {z.key} layers=9 sourceDriven=true");return 1;
    }
}
