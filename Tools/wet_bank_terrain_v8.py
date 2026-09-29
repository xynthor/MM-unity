from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
def rep(old,new,n=1):
    global s
    c=s.count(old)
    if c!=n: raise SystemExit(f'expected {n}, got {c}: {old[:100]!r}')
    s=s.replace(old,new,n)
rep('public TerrainLayer grassLayer, dirtLayer, roadLayer, rockLayer;','public TerrainLayer grassLayer, dirtLayer, roadLayer, rockLayer, wetBankLayer;')
rep('''        e.rockLayer = CreateTerrainLayer("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png", new Vector2(8,8), 0.90f);''','''        e.rockLayer = CreateTerrainLayer("Rock", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png", "Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png", new Vector2(8,8), 0.90f);\n        e.wetBankLayer = CreateTerrainLayer("WetBank", "Assets/World/NewSorpigal/Generated/wet_bank.png", "Assets/EnvironmentAssets/UnitySamples/dry_soil_NOH.png", new Vector2(5,5), 0.78f);''')
rep('td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer};','td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer,e.wetBankLayer};')
rep('float[,,] alpha=new float[512,512,4];','float[,,] alpha=new float[512,512,5];')
old='''            if(halfW>0f&&riverD<outer)\n            {\n                float edge=Mathf.InverseLerp(0f,outer,riverD);\n                alpha[y,x,1]=Mathf.Lerp(.42f,.20f,edge);\n                alpha[y,x,3]=Mathf.Lerp(.13f,.035f,edge);\n                alpha[y,x,0]=1f-alpha[y,x,1]-alpha[y,x,3];\n            }'''
new='''            if(halfW>0f&&riverD<outer)\n            {\n                float bankT=Mathf.Clamp01(Mathf.InverseLerp(halfW,outer,riverD));\n                float wet=riverD<=halfW?1f:Mathf.Lerp(.90f,.04f,Mathf.SmoothStep(0f,1f,bankT));\n                alpha[y,x,4]=wet;\n                alpha[y,x,1]=(1f-wet)*.20f;\n                alpha[y,x,3]=(1f-wet)*.025f;\n                alpha[y,x,0]=1f-alpha[y,x,4]-alpha[y,x,1]-alpha[y,x,3];\n            }'''
rep(old,new)
rep('''        var banks=BuildRiverBanks(root.transform,terrain);\n        var river=BuildRealRiver(root.transform);\n        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} banks={(banks?"yes":"no")} source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");''','''        var river=BuildRealRiver(root.transform);\n        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} banks=terrain-wet-layer source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");''')
p.write_text(s,encoding='utf-8')
print('WET_BANK_TERRAIN_V8_PATCHED')
