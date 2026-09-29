from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticTerrainBiomePass.cs")
s=p.read_text(encoding="utf-8-sig")

# Constants: exactly six biome terrain types + non-biome road overlay. Legacy names are aliases only.
s=s.replace(
'public const int VeryGreen=0,SlightGreen=1,Sand=2,Volcanic=3,DryBrown=4,Snow=5,Rock=6,Road=7,WetMud=8;',
'''public const int Green=0,LightGreen=1,Desert=2,Volcanic=3,Snow=4,Arid=5,RoadOverlay=6;
    // Legacy aliases for helper scripts only; these DO NOT create extra terrain types.
    public const int VeryGreen=Green,SlightGreen=LightGreen,Sand=Desert,DryBrown=Arid,Road=RoadOverlay,Rock=Arid,WetMud=LightGreen;''')

a=s.index('    public static string ClassName')
b=s.index('    static TerrainLayer Layer',a)
s=s[:a]+'''    public static string ClassName(int k)
    {
        string[] n={"GREEN","LIGHT_GREEN","DESERT","VOLCANIC","SNOW","ARID","ROAD_OVERLAY"};
        return k>=0&&k<n.Length?n[k]:"UNKNOWN";
    }

'''+s[b:]

a=s.index('    static TerrainLayer[] Layers()')
b=s.index('    static bool RoadCell',a)
s=s[:a]+'''    static TerrainLayer[] Layers()=>new[]{
        Layer("Green","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_ground/grass_ground_nor_gl_1k.jpg",7.5f,.025f),
        Layer("LightGreen","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_nor_gl_1k.jpg",7.2f,.035f),
        Layer("Desert","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_nor_gl_1k.jpg",7.5f,.025f),
        Layer("Volcanic","Assets/EnvironmentAssets/Biomes/ash_ground.png","Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_nor_gl_1k.jpg",7.5f,.02f),
        Layer("Snow","Assets/EnvironmentAssets/Biomes/snow.png","",9f,.055f),
        Layer("Arid","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png","Assets/Environment/PolyHaven/Textures/damp_sand/damp_sand_nor_gl_1k.jpg",7f,.025f),
        Layer("RoadOverlay","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_diff_1k.jpg","Assets/Environment/PolyHaven/Textures/grass_path_2/grass_path_2_nor_gl_1k.jpg",5.8f,.04f)
    };

'''+s[b:]

a=s.index('    public static int BaseClass(')
b=s.index('    static void Add(',a)
s=s[:a]+'''    public static int BaseClass(string zone,byte g,byte f)
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
                if(g==2)return Desert;
                if(g==3)return Volcanic;
                return Arid;

            case "Blackshire":
                if(g==2)return Desert;
                return LightGreen;

            case "ParadiseValley":
                if(g==2)return Desert;
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
                return sy<64 ? Snow : LightGreen;
        }
        return LightGreen;
    }

'''+s[b:]

a=s.index('    static void SampleSource(')
b=s.index('    static void Normalize',a)
s=s[:a]+'''    static void SampleSource(string zone,byte[] tile,byte[] grp,byte[] sem,float sx,float sy,float[] w,float amount)
    {
        int ix=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),iy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);
        byte raw=tile[iy*N+ix],g=grp[raw],f=sem[raw];
        int c=BaseClassAt(zone,g,f,iy);
        if(c==Green){Add(w,Green,amount*.95f);Add(w,LightGreen,amount*.05f);}
        else if(c==LightGreen){Add(w,LightGreen,amount*.88f);Add(w,Green,amount*.12f);}
        else if(c==Desert){Add(w,Desert,amount*.94f);Add(w,Arid,amount*.06f);}
        else if(c==Volcanic){Add(w,Volcanic,amount*.94f);Add(w,Arid,amount*.06f);}
        else if(c==Snow){Add(w,Snow,amount*.96f);Add(w,Arid,amount*.04f);}
        else if(c==Arid){Add(w,Arid,amount*.92f);Add(w,LightGreen,amount*.08f);}
        else Add(w,c,amount);
    }

'''+s[b:]

s=s.replace('if(s<=.0001f){w[SlightGreen]=1f;return;}','if(s<=.0001f){w[LightGreen]=1f;return;}')
s=s.replace('if(RoadCell(g,f)){a[y,x,Road]=1f;continue;}','if(RoadCell(g,f)){a[y,x,RoadOverlay]=1f;continue;}')

old='''            int bc=BaseClassAt(z.key,g,f,cy);
            if(bc==VeryGreen){w[VeryGreen]*=.94f+.10f*n;w[SlightGreen]+=.05f*(1f-n);}
            else if(bc==SlightGreen){w[SlightGreen]*=.92f+.10f*n;w[VeryGreen]+=.035f*n;}
            else if(bc==DryBrown){w[DryBrown]*=.95f+.08f*n;w[SlightGreen]+=.035f*n;}'''
new='''            int bc=BaseClassAt(z.key,g,f,cy);
            if(bc==Green){w[Green]*=.95f+.08f*n;w[LightGreen]+=.035f*(1f-n);}
            else if(bc==LightGreen){w[LightGreen]*=.94f+.08f*n;w[Green]+=.025f*n;}
            else if(bc==Arid){w[Arid]*=.96f+.05f*n;w[LightGreen]+=.018f*n;}'''
if old not in s: raise SystemExit("noise block missing")
s=s.replace(old,new,1)

p.write_text(s,encoding="utf-8")
print("SIX_TERRAIN_TYPES_PATCHED")
