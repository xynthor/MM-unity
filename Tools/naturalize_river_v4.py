from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
def rep(old,new,n=1):
    global s
    c=s.count(old)
    if c!=n: raise SystemExit(f'expected {n} matches, got {c}: {old[:80]!r}')
    s=s.replace(old,new,n)
old='''        float anchorDist=Mathf.Min(Mathf.Min(Mathf.Abs(sy-58f),Mathf.Abs(sy-73.2f)),Mathf.Min(Mathf.Abs(sy-84.5f),Mathf.Abs(sy-103f)));\n        float free=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(anchorDist/1.8f));\n        float meander=((Mathf.PerlinNoise(sy*.145f+7.2f,4.1f)-.5f)*.72f + Mathf.Sin(sy*.53f+1.7f)*.16f)*free;\n        return x+meander;'''
new='''        float anchorDist=Mathf.Min(Mathf.Min(Mathf.Abs(sy-58f),Mathf.Abs(sy-73.2f)),Mathf.Min(Mathf.Abs(sy-84.5f),Mathf.Abs(sy-103f)));\n        float free=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(anchorDist/2.15f));\n        float broad=(Mathf.PerlinNoise(sy*.082f+7.2f,4.1f)-.5f)*2.05f;\n        float secondary=Mathf.Sin(sy*.305f+.9f)*.44f+Mathf.Sin(sy*.665f+2.4f)*.18f;\n        float meander=(broad+secondary)*free;\n        return x+meander;'''
rep(old,new)
old='''        float d=RiverDownstream01(sy);\n        float w=Mathf.Lerp(1.65f,4.10f,d);\n        return w*Mathf.Lerp(.94f,1.06f,Mathf.PerlinNoise(sy*.27f+11.3f,2.2f));'''
new='''        float d=RiverDownstream01(sy);\n        float w=Mathf.Lerp(1.65f,4.10f,d);\n        float broad=Mathf.Lerp(.84f,1.18f,Mathf.PerlinNoise(sy*.118f+3.4f,9.7f));\n        float detail=Mathf.Lerp(.94f,1.06f,Mathf.PerlinNoise(sy*.31f+11.3f,2.2f));\n        float mouth=1f+.68f*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(58f,64.5f,sy)));\n        return w*broad*detail*mouth;'''
rep(old,new)
old='''                alpha[y,x,1]=Mathf.Lerp(.56f,.30f,edge);\n                alpha[y,x,3]=Mathf.Lerp(.36f,.12f,edge);\n                alpha[y,x,0]=1f-alpha[y,x,1]-alpha[y,x,3];'''
new='''                alpha[y,x,1]=Mathf.Lerp(.42f,.20f,edge);\n                alpha[y,x,3]=Mathf.Lerp(.13f,.035f,edge);\n                alpha[y,x,0]=1f-alpha[y,x,1]-alpha[y,x,3];'''
rep(old,new)
old='''        if(tileCache==null) tileCache=File.ReadAllBytes(TilePath);\n        float[] center=GetRiverCenterline(tileCache); int added=0;\n        for(int i=0;i<64;i++)\n        {\n            float sy=Mathf.Lerp(60f,91f,i/63f); if(!TryRiverCenter(center,sy,out float cx)) continue;'''
new='''        if(tileCache==null) tileCache=File.ReadAllBytes(TilePath);\n        float[] center=GetRiverCenterline(tileCache); int added=0;\n        var roots=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Roots/Roots_System_01_Prefab.prefab");\n        var debris=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Sticks_debris/sticks_debris_00_prefab.prefab");\n        for(int i=0;i<96;i++)\n        {\n            float sy=Mathf.Lerp(59f,98f,i/95f); if(!TryRiverCenter(center,sy,out float cx)) continue;'''
rep(old,new)
old='''                float bank=RiverHalfWidth(sy)+.52f+Deterministic01(seed*11)*.48f;\n                float sx=cx+side*bank+(Deterministic01(seed*13)-.5f)*.16f;'''
new='''                float bank=RiverHalfWidth(sy)+.42f+Deterministic01(seed*11)*.72f;\n                float sx=cx+side*bank+(Deterministic01(seed*13)-.5f)*.24f;'''
rep(old,new)
old='''                if(i%9==0 && e.botdRockPrefab)\n                {\n                    float rx=wx+side*Mathf.Lerp(1.2f,3.5f,Deterministic01(seed*23));\n                    if(IsSuitableNaturalSpot(terrain,architectureRoot,rx,wz,false,false)){SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,rx,wz,SampleTerrainY(terrain,rx,wz),seed+1,Mathf.Lerp(.45f,1.25f,Deterministic01(seed*29)),"RiverRock");added++;}\n                }'''
new='''                if(i%6==0 && e.botdRockPrefab)\n                {\n                    float rx=wx+side*Mathf.Lerp(.7f,3.2f,Deterministic01(seed*23));\n                    if(IsSuitableNaturalSpot(terrain,architectureRoot,rx,wz,false,false)){SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,rx,wz,SampleTerrainY(terrain,rx,wz),seed+1,Mathf.Lerp(.38f,1.45f,Deterministic01(seed*29)),"RiverRock");added++;}\n                }\n                if(i%12==3 && roots)\n                {\n                    float rx=wx+side*Mathf.Lerp(1f,2.8f,Deterministic01(seed*31));\n                    if(IsSuitableNaturalSpot(terrain,architectureRoot,rx,wz,false,false)){SpawnPlant(roots,understory,null,rx,wz,SampleTerrainY(terrain,rx,wz),seed+2,Mathf.Lerp(.45f,.85f,Deterministic01(seed*37)),"RiverRoots");added++;}\n                }\n                if(i%10==5 && debris)\n                {\n                    float dx=wx+side*Mathf.Lerp(.4f,2.1f,Deterministic01(seed*41));\n                    if(IsSuitableNaturalSpot(terrain,architectureRoot,dx,wz,false,false)){SpawnPlant(debris,understory,null,dx,wz,SampleTerrainY(terrain,dx,wz),seed+3,Mathf.Lerp(.35f,.72f,Deterministic01(seed*43)),"RiverDebris");added++;}\n                }\n                if(i%16==7 && e.botdLogPrefab)\n                {\n                    float lx=wx+side*Mathf.Lerp(1.1f,3.8f,Deterministic01(seed*47));\n                    if(IsSuitableNaturalSpot(terrain,architectureRoot,lx,wz,false,false)){SpawnPlant(e.botdLogPrefab,understory,null,lx,wz,SampleTerrainY(terrain,lx,wz),seed+4,Mathf.Lerp(.45f,.9f,Deterministic01(seed*53)),"RiverLog");added++;}\n                }'''
rep(old,new)
p.write_text(s,encoding='utf-8')
print('NATURAL_RIVER_V4_PATCHED')
