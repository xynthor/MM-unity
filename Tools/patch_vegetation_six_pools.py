from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticVegetationPass.cs")
s=p.read_text(encoding="utf-8-sig")

old='''    static GameObject[] SourcePool(int cls,int seed,GameObject[] green,GameObject[] deadPool,GameObject[] desert)
    {
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return deadPool;
        if(cls==MMRealisticTerrainBiomePass.Sand)return desert;
        if(cls==MMRealisticTerrainBiomePass.DryBrown && (seed&3)==0)return deadPool;
        return green;
    }'''
new='''    static GameObject[] SourcePool(int cls,int seed,GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid)
    {
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return deadPool;
        if(cls==MMRealisticTerrainBiomePass.Desert)return desert;
        if(cls==MMRealisticTerrainBiomePass.Snow)return snow;
        if(cls==MMRealisticTerrainBiomePass.Arid)return arid;
        return green;
    }'''
if old not in s: raise SystemExit("SourcePool block missing")
s=s.replace(old,new,1)

s=s.replace('''                                    GameObject[] green,GameObject[] deadPool,GameObject[] desert,List<string> audit,List<Vector2> occupied)''',
'''                                    GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid,List<string> audit,List<Vector2> occupied)''',1)
s=s.replace('''            var pool=SourcePool(cls,seed,green,deadPool,desert);''',
'''            var pool=SourcePool(cls,seed,green,deadPool,desert,snow,arid);''',1)

old='''    static bool AcceptDensity(int cls,float cluster,float rnd)
    {
        if(cls==MMRealisticTerrainBiomePass.VeryGreen)return cluster>.40f&&rnd<.30f;
        if(cls==MMRealisticTerrainBiomePass.SlightGreen)return cluster>.48f&&rnd<.14f;
        if(cls==MMRealisticTerrainBiomePass.WetMud)return cluster>.45f&&rnd<.18f;
        if(cls==MMRealisticTerrainBiomePass.Snow)return cluster>.43f&&rnd<.20f;
        if(cls==MMRealisticTerrainBiomePass.DryBrown)return cluster>.55f&&rnd<.07f;
        if(cls==MMRealisticTerrainBiomePass.Sand)return cluster>.50f&&rnd<.09f;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return cluster>.58f&&rnd<.065f;
        return false;
    }
    static GameObject[] SupplementPool(int cls,int seed,GameObject[] green,GameObject[] deadPool,GameObject[] desert)
    {
        if(cls==MMRealisticTerrainBiomePass.Sand)return desert;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return deadPool;
        if(cls==MMRealisticTerrainBiomePass.DryBrown)return (seed&1)==0?deadPool:green;
        return green;
    }'''
new='''    static bool AcceptDensity(int cls,float cluster,float rnd)
    {
        if(cls==MMRealisticTerrainBiomePass.Green)return cluster>.38f&&rnd<.32f;
        if(cls==MMRealisticTerrainBiomePass.LightGreen)return cluster>.48f&&rnd<.14f;
        if(cls==MMRealisticTerrainBiomePass.Snow)return cluster>.43f&&rnd<.20f;
        if(cls==MMRealisticTerrainBiomePass.Arid)return cluster>.56f&&rnd<.065f;
        if(cls==MMRealisticTerrainBiomePass.Desert)return cluster>.50f&&rnd<.09f;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return cluster>.60f&&rnd<.055f;
        return false;
    }
    static GameObject[] SupplementPool(int cls,int seed,GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid)
    {
        if(cls==MMRealisticTerrainBiomePass.Desert)return desert;
        if(cls==MMRealisticTerrainBiomePass.Volcanic)return deadPool;
        if(cls==MMRealisticTerrainBiomePass.Snow)return snow;
        if(cls==MMRealisticTerrainBiomePass.Arid)return arid;
        return green;
    }'''
if old not in s: raise SystemExit("density/pool block missing")
s=s.replace(old,new,1)

s=s.replace('''                                  GameObject[] green,GameObject[] deadPool,GameObject[] desert,List<string> audit,List<Vector2> occupied)''',
'''                                  GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid,List<string> audit,List<Vector2> occupied)''',1)
s=s.replace('''            var pool=SupplementPool(cls,seed,green,deadPool,desert);if(pool.Length==0)continue;''',
'''            var pool=SupplementPool(cls,seed,green,deadPool,desert,snow,arid);if(pool.Length==0)continue;''',1)

s=s.replace('''    static int ApplyZone(Z z,GameObject[] green,GameObject[] deadPool,GameObject[] desert,List<string> audit)''',
'''    static int ApplyZone(Z z,GameObject[] green,GameObject[] deadPool,GameObject[] desert,GameObject[] snow,GameObject[] arid,List<string> audit)''',1)
s=s.replace('''        int source=RebuildSourceAnchors(z,regionRoot.transform,terrain,tile,grp,sem,green,deadPool,desert,audit,occupied);
        int extra=BuildSupplementary(z,regionRoot.transform,terrain,tile,grp,sem,green,deadPool,desert,audit,occupied);''',
'''        int source=RebuildSourceAnchors(z,regionRoot.transform,terrain,tile,grp,sem,green,deadPool,desert,snow,arid,audit,occupied);
        int extra=BuildSupplementary(z,regionRoot.transform,terrain,tile,grp,sem,green,deadPool,desert,snow,arid,audit,occupied);''',1)

old='''        var green=GreenPool();var deadPool=DeadPool();var desert=DesertPool();
        Directory.CreateDirectory("Validation");
        var audit=new List<string>{"zone,kind,index,name,x,z,targetX,targetZ,dx,dz,terrainClass,prefab,rootRotX,rootRotZ,verticalRatio,groundGap,roadOrWater,slope,status"};
        int total=0;
        foreach(var z in Zones)total+=ApplyZone(z,green,deadPool,desert,audit);
        File.WriteAllLines("Validation/RealisticTreePlacementAudit.csv",audit);
        AssetDatabase.SaveAssets();
        Debug.Log($"REALISTIC_VEGETATION_ALL_DONE total={total} greenVariants={green.Length} deadVariants={deadPool.Length} desertVariants={desert.Length}");'''
new='''        var green=GreenPool();var deadPool=DeadPool();var desert=DesertPool();var snow=SnowPool();var arid=AridPool();
        Directory.CreateDirectory("Validation");
        var audit=new List<string>{"zone,kind,index,name,x,z,targetX,targetZ,dx,dz,terrainClass,prefab,rootRotX,rootRotZ,verticalRatio,groundGap,roadOrWater,slope,status"};
        int total=0;
        foreach(var z in Zones)total+=ApplyZone(z,green,deadPool,desert,snow,arid,audit);
        File.WriteAllLines("Validation/RealisticTreePlacementAudit.csv",audit);
        AssetDatabase.SaveAssets();
        Debug.Log($"REALISTIC_VEGETATION_ALL_DONE total={total} greenVariants={green.Length} snowVariants={snow.Length} deadVariants={deadPool.Length} desertVariants={desert.Length} aridVariants={arid.Length}");'''
if old not in s: raise SystemExit("ApplyAll block missing")
s=s.replace(old,new,1)

p.write_text(s,encoding="utf-8")
print("VEGETATION_SIX_BIOME_POOLS_PATCHED")
