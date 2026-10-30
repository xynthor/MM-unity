using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMRealisticTerrainBiomePass
{
    public const int N=128;
    public const int Green=0,LightGreen=1,Desert=2,Volcanic=3,Snow=4,Arid=5,RoadOverlay=6;
    // Legacy aliases for helper scripts only; these DO NOT create extra terrain types.
    public const int VeryGreen=Green,SlightGreen=LightGreen,Sand=Desert,DryBrown=Arid,Road=RoadOverlay,Rock=Arid,WetMud=LightGreen;
    const string LayerDir="Assets/Materials/RealisticWorld";
    sealed class Z{public string key,scene;public Z(string k,string s){key=k;scene=s;}}
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity")};

    public static string ClassName(int k)
    {
        string[] n={"GREEN","LIGHT_GREEN","DESERT","VOLCANIC","SNOW","ARID","ROAD_OVERLAY"};
        return k>=0&&k<n.Length?n[k]:"UNKNOWN";
    }

    static TerrainLayer Layer(string key,string diff,string normal,float tile,float smooth)
    {
        Directory.CreateDirectory(LayerDir);
        string path=$"{LayerDir}/{key}.terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
        l.name="Realistic_"+key;
        l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(diff);
        l.normalMapTexture=string.IsNullOrEmpty(normal)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
        l.tileSize=new Vector2(tile,tile);l.tileOffset=Vector2.zero;l.metallic=0f;l.smoothness=smooth;l.normalScale=1f;
        EditorUtility.SetDirty(l);return l;
    }

    static TerrainLayer[] Layers()=>new[]{
        Layer("Green","Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_CH.png","Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_NOH.png",9f,.035f),
        Layer("LightGreen","Assets/Materials/RealisticWorld/LightGreen_blend.png","Assets/EnvironmentAssets/UnitySamples/ground_grass_fells_mossy_NOH.png",8f,.03f),
        Layer("Desert","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_nor_gl_1k.jpg",7.5f,.025f),
        Layer("Volcanic","Assets/EnvironmentAssets/Biomes/ash_ground.png","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",7.5f,.02f),
        Layer("Snow","Assets/EnvironmentAssets/Biomes/snow.png","",9f,.055f),
        Layer("Arid","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg",7f,.025f),
        Layer("RoadOverlay","Assets/EnvironmentAssets/UnitySamples/stone_ground_CH.png","Assets/EnvironmentAssets/UnitySamples/stone_ground_noh.png",6f,.12f)
    };

    static TerrainLayer[] LayersFor(string zone,TerrainLayer[] baseLayers)
    {
        var a=(TerrainLayer[])baseLayers.Clone();
        if(zone=="Blackshire")
            a[LightGreen]=Layer("BlackshireMud","Assets/EnvironmentAssets/UnitySamples/mud_cracked_dry_c.png",
                                "Assets/EnvironmentAssets/UnitySamples/mud_cracked_dry_n.png",7.5f,.02f);
        return a;
    }

    static bool RoadCell(byte g,byte f)=>((f&8)!=0)||(g>=8&&g<255);

    public static int BaseClass(string zone,byte g,byte f)
    {
        return BaseClassAt(zone,g,f,64);
    }

    public static int BaseClassAt(string zone,byte g,byte f,int sy)
    {
        if(RoadCell(g,f))return RoadOverlay;

        // Water/shore terrain is hidden by the unified seabed pass; keep one neutral underlying class.
        if((f&1)!=0||(f&2)!=0||g==5)return Arid;

        switch(zone)
        {
            case "NewSorpigal":
            case "CastleIronfist":
            case "MireOfTheDamned":
            case "MistyIslands":
            case "EelInfestedWaters":
            case "BootlegBay":
                return Green;

            case "SilverCove":
                return g==0 ? Green : LightGreen;

            case "Dragonsand":
                if(g==2)return Desert;
                if(g==0)return Green;
                return Arid;

            case "HermitsIsle":
                return Volcanic; // user-reference biome: volcanic ash across all non-water, non-road land

            case "Blackshire":
                if(g==2)return Desert;
                return LightGreen;

            case "ParadiseValley":
                // Source desert tiles occupy the south-east; extend that source-indicated band to an approximately 50/50
                // desert vs dry-mud split without introducing any green terrain.
                if(g==2||sy>=60)return Desert;
                return Arid;

            case "FreeHaven":
                return LightGreen;

            case "SweetWater":
                if(g==1)return Snow;
                return Arid;

            case "FrozenHighlands":
                return Snow;

            case "Kriegspire":
                if(g==3)return Volcanic; // volcano always wins over snow
                return sy<64 ? Snow : Arid;
        }
        return LightGreen;
    }

    static void Add(float[] w,int k,float v){if(k>=0&&k<w.Length)w[k]+=v;}
    static void SampleSource(string zone,byte[] tile,byte[] grp,byte[] sem,float sx,float sy,float[] w,float amount)
    {
        int ix=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),iy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        byte raw=tile[iy*N+ix],g=grp[raw],f=sem[raw];
        int c=BaseClassAt(zone,g,f,iy);
        if(c==Green){Add(w,Green,amount*.985f);Add(w,LightGreen,amount*.015f);}
        else if(c==LightGreen)
        {
            if(zone=="Blackshire"){Add(w,LightGreen,amount*.95f);Add(w,Green,amount*.05f);}
            else {Add(w,LightGreen,amount*.82f);Add(w,Green,amount*.18f);}
        }
        else if(c==Desert){Add(w,Desert,amount*.94f);Add(w,Arid,amount*.06f);}
        else if(c==Volcanic)
        {
            if(zone=="HermitsIsle")Add(w,Volcanic,amount);
            else {Add(w,Volcanic,amount*.94f);Add(w,Arid,amount*.06f);}
        }
        else if(c==Snow){Add(w,Snow,amount*.96f);Add(w,Arid,amount*.04f);}
        else if(c==Arid)
        {
            bool noGreen=zone=="SweetWater"||zone=="Kriegspire"||zone=="ParadiseValley"||zone=="HermitsIsle";
            if(noGreen)Add(w,Arid,amount);
            else {Add(w,Arid,amount*.92f);Add(w,LightGreen,amount*.08f);}
        }
        else Add(w,c,amount);
    }

    static void Normalize(float[] w)
    {
        float s=0f;for(int i=0;i<w.Length;i++)s+=Mathf.Max(0f,w[i]);
        if(s<=.0001f){w[LightGreen]=1f;return;}
        for(int i=0;i<w.Length;i++)w[i]=Mathf.Max(0f,w[i])/s;
    }

    static int ApplyZone(Z z,TerrainLayer[] layers)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");
        string d=$"Assets/World/{z.key}/Data";
        var tile=File.ReadAllBytes(d+"/tilemap_u8.bin");var grp=File.ReadAllBytes(d+"/tile_groups_u8.bin");var sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        var td=t.terrainData;var zoneLayers=LayersFor(z.key,layers);td.terrainLayers=zoneLayers;if(td.alphamapResolution!=512)td.alphamapResolution=512;
        int r=td.alphamapResolution,L=zoneLayers.Length;var a=new float[r,r,L];
        for(int y=0;y<r;y++)for(int x=0;x<r;x++)
        {
            float sx=((x+.5f)/r)*N-.5f;
            float sy=N-1-(((y+.5f)/r)*N-.5f);
            int cx=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),cy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
            byte raw=tile[cy*N+cx],g=grp[raw],f=sem[raw];
            if(RoadCell(g,f)){a[y,x,RoadOverlay]=1f;continue;}
            var w=new float[L];
            for(int oy=-2;oy<=2;oy++)for(int ox=-2;ox<=2;ox++)
            {
                float dd=ox*ox+oy*oy;float wt=Mathf.Exp(-dd/2.1f);
                SampleSource(z.key,tile,grp,sem,sx+ox,sy+oy,w,wt);
            }
            float n=Mathf.PerlinNoise((sx+37.1f)*.095f,(sy+11.7f)*.095f);
            int bc=BaseClassAt(z.key,g,f,cy);
            if(bc==Green){w[Green]*=1.00f+.04f*n;w[LightGreen]+=.012f*(1f-n);}
            else if(bc==LightGreen)
            {
                w[LightGreen]*=.96f+.06f*n;
                w[Green]+=(z.key=="Blackshire"?.012f:.055f)*n;
            }
            else if(bc==Arid)
            {
                w[Arid]*=.96f+.05f*n;
                bool noGreen=z.key=="SweetWater"||z.key=="Kriegspire"||z.key=="ParadiseValley"||z.key=="HermitsIsle";
                if(!noGreen)w[LightGreen]+=.018f*n;
            }
            Normalize(w);
            for(int k=0;k<L;k++)a[y,x,k]=w[k];
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);t.Flush();
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"REALISTIC_TERRAIN {z.key} applied smooth=true sourceBlueprint=true");
        return 1;
    }

    [MenuItem("MMUnity/Realistic World/1c Priority Biome Regions")]
    public static void ApplyPriorityBiomeRegions()
    {
        var layers=Layers();int n=0;
        foreach(var key in new[]{"HermitsIsle","ParadiseValley","Blackshire","SweetWater","Kriegspire"})
            n+=ApplyZone(Zones.First(x=>x.key==key),layers);
        AssetDatabase.SaveAssets();
        Debug.Log("REALISTIC_TERRAIN_PRIORITY_DONE zones="+n);
    }

    [MenuItem("MMUnity/Realistic World/1b Kriegspire Terrain Only")]
    public static void ApplyKriegspireOnly()
    {
        var layers=Layers();
        var z=Zones.First(x=>x.key=="Kriegspire");
        ApplyZone(z,layers);
        AssetDatabase.SaveAssets();
        Debug.Log("REALISTIC_TERRAIN_KRIEGSPIRE_DONE");
    }

    [MenuItem("MMUnity/Realistic World/1 Terrain Biomes")]
    public static void ApplyAll()
    {
        var layers=Layers();int n=0;foreach(var z in Zones)n+=ApplyZone(z,layers);
        AssetDatabase.SaveAssets();Debug.Log("REALISTIC_TERRAIN_ALL_DONE zones="+n);
    }
}
