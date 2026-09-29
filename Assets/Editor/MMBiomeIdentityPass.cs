using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

public static class MMBiomeIdentityPass
{
    sealed class Z
    {
        public string key,scene,baseTex,dirtTex; public int col,row; public bool l,r,u,d; public float edge;
        public Z(string k,string s,int c,int w,bool L,bool R,bool U,bool D,string b=null,string di=null,float e=.28f)
        {key=k;scene=s;col=c;row=w;l=L;r=R;u=U;d=D;baseTex=b;dirtTex=di;edge=e;}
    }
    static readonly Z[] Zones={
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity",0,2,false,true,false,true,"Assets/EnvironmentAssets/Biomes/ash_ground.png","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",.58f),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity",1,2,true,true,false,true,"Assets/EnvironmentAssets/Biomes/ash_ground.png","Assets/EnvironmentAssets/Biomes/cold_rock.png",.56f),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity",2,2,true,true,false,true,"Assets/EnvironmentAssets/Biomes/snow.png","Assets/EnvironmentAssets/Biomes/cold_rock.png",.68f),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity",3,2,true,true,false,true),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity",4,2,true,false,false,true),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity",0,1,false,true,true,true,"Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png","Assets/EnvironmentAssets/Biomes/sand.png",.48f),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity",1,1,true,true,true,true,"Assets/EnvironmentAssets/Biomes/swamp_ground.png","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",.42f),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity",2,1,true,true,true,true),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity",3,1,true,true,true,true),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity",4,1,true,false,true,true),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity",0,0,false,true,true,false,"Assets/EnvironmentAssets/Biomes/ash_ground.png","Assets/EnvironmentAssets/Biomes/cold_rock.png",.45f),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity",1,0,true,true,true,false,"Assets/EnvironmentAssets/Biomes/sand.png","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg",.38f),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity",2,0,true,true,true,false),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity",3,0,true,true,true,false),
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity",4,0,true,false,true,false)
    };
    const int N=128;
    static TerrainLayer Ecotone()
    {
        string p="Assets/Materials/Terrain/EnrothEcotone.terrainlayer";
        var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(p);
        if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,p);}
        l.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg");
        l.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg");
        l.tileSize=new Vector2(11f,11f);l.metallic=0;l.smoothness=.04f;EditorUtility.SetDirty(l);return l;
    }
    static bool Water(byte[] tile,byte[] sem,int sx,int sy)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);return (sem[tile[sy*N+sx]]&1)!=0;
    }
    [MenuItem("MMUnity/Fix Biome Identity + Ecotones")]
    public static void ApplyAll()
    {
        var eco=Ecotone();int changed=0;
        foreach(var z in Zones)changed+=Apply(z,eco);
        AssetDatabase.SaveAssets();Debug.Log("BIOME_IDENTITY_DONE zones=15 changed="+changed+" universalTerrain=false");
    }
    static int Apply(Z z,TerrainLayer eco)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);if(!t)return 0;var td=t.terrainData;
        var layers=td.terrainLayers.ToList();
        if(z.baseTex!=null&&layers.Count>0){layers[0].diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(z.baseTex);EditorUtility.SetDirty(layers[0]);}
        if(z.dirtTex!=null&&layers.Count>1){layers[1].diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(z.dirtTex);EditorUtility.SetDirty(layers[1]);}
        int ei=layers.IndexOf(eco);if(ei<0){layers.Add(eco);td.terrainLayers=layers.ToArray();ei=layers.Count-1;}
        int aw=td.alphamapWidth,ah=td.alphamapHeight,l=td.alphamapLayers;var a=td.GetAlphamaps(0,0,aw,ah);
        byte[] tile=File.ReadAllBytes($"Assets/World/{z.key}/Data/tilemap_u8.bin"),sem=File.ReadAllBytes($"Assets/World/{z.key}/Data/tile_semantics_u8.bin");
        int n=0;
        for(int y=0;y<ah;y++)for(int x=0;x<aw;x++)
        {
            int sx=Mathf.Clamp(Mathf.FloorToInt((x+.5f)/aw*N),0,N-1),sy=Mathf.Clamp(N-1-Mathf.FloorToInt((y+.5f)/ah*N),0,N-1);
            if(Water(tile,sem,sx,sy))continue;
            float mx=(x+.5f)/aw*512f,mz=(y+.5f)/ah*512f,dist=999f;
            if(z.l)dist=Mathf.Min(dist,mx);if(z.r)dist=Mathf.Min(dist,512f-mx);if(z.d)dist=Mathf.Min(dist,mz);if(z.u)dist=Mathf.Min(dist,512f-mz);
            float gx=(z.col-2)*512f-256f+mx,gz=(z.row-1)*512f-256f+mz;
            float width=13f+Mathf.PerlinNoise(gx*.008f+7.2f,gz*.008f+18.4f)*20f;if(dist>=width)continue;
            float w=(1f-Mathf.SmoothStep(0f,1f,dist/width))*z.edge*(.72f+.28f*Mathf.PerlinNoise(gx*.021f+3.1f,gz*.021f+11.8f));
            if(w<=.01f)continue;for(int k=0;k<l;k++)a[y,x,k]*=(1f-w);a[y,x,ei]+=w;n++;
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,z.scene);return n;
    }
}
