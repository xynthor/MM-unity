from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')

def rep(old,new,label):
    global s
    n=s.count(old)
    if n!=1:
        raise SystemExit(f'{label}: expected 1 match, got {n}')
    s=s.replace(old,new,1)
    print('PATCH',label)

old='''    static float BlueprintBaseHeight(float raw)\n    {\n        float n=Mathf.Clamp01(raw/31.750f);\n        return Mathf.Max(0.55f,0.9f+Mathf.Pow(n,1.22f)*76f);\n    }\n'''
new='''    static float BlueprintBaseHeight(float raw)\n    {\n        float n=Mathf.Clamp01(raw/31.750f);\n        return Mathf.Max(0.55f,0.9f+Mathf.Pow(n,1.22f)*76f);\n    }\n\n    // MM6 outdoor heights are strongly quantized. Smooth only elevated terrain so\n    // hills keep their footprint but lose artificial terraces/flat mesa tops.\n    static float SampleNaturalTerrainHeight(byte[] heights,float sx,float sy)\n    {\n        float center=SampleByteBilinear(heights,sx,sy);\n        float sum=0f,weight=0f;\n        for(int oy=-3;oy<=3;oy++) for(int ox=-3;ox<=3;ox++)\n        {\n            float d2=ox*ox+oy*oy;\n            float w=Mathf.Exp(-d2*.34f);\n            sum+=SampleByteBilinear(heights,sx+ox*.72f,sy+oy*.72f)*w;\n            weight+=w;\n        }\n        float smooth=sum/Mathf.Max(.0001f,weight);\n        float high=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(18f,92f,center));\n        float shaped=Mathf.Lerp(center,smooth,.82f*high);\n        if(center>92f) shaped=Mathf.Lerp(shaped,center,.24f);\n        return shaped;\n    }\n'''
rep(old,new,'natural terrain sampler')
old='''            float rawY=SampleByteBilinear(heights,sx,sy)*.25f;\n            float worldY=CreativeLandHeight(rawY,sx,sy);\n            float seaMask=SampleSeaWaterMask(tiles,sx,sy);\n            float coastBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.34f,.66f,seaMask));\n            float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;\n            float landY=Mathf.Lerp(worldY,BlueprintBaseHeight(rawY),protectedBlend);'''
new='''            float sourceRawY=SampleByteBilinear(heights,sx,sy)*.25f;\n            float rawY=SampleNaturalTerrainHeight(heights,sx,sy)*.25f;\n            float worldY=CreativeLandHeight(rawY,sx,sy);\n            float seaMask=SampleSeaWaterMask(tiles,sx,sy);\n            float coastBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.34f,.66f,seaMask));\n            float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;\n            float landY=Mathf.Lerp(worldY,BlueprintBaseHeight(sourceRawY),protectedBlend);'''
rep(old,new,'terrain anti-terrace heights')

old='''            float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*.25f,sx,sy);'''
new='''            float h=CreativeLandHeight(SampleNaturalTerrainHeight(heights,sx,sy)*.25f,sx,sy);'''
rep(old,new,'alphamap natural hill height')

old='''    static readonly float[] RiverSy={58f,61.5f,65f,68.5f,73.2f,76.5f,80f,84.5f,87.5f,90.5f,94f,97f,99f,100f,101f,102f,103f};\n    static readonly float[] RiverX={89f,92.3f,89.3f,93.7f,95.5f,98.5f,100.2f,103.5f,101.9f,104.4f,103.0f,104.7f,104f,103.8f,103.5f,103.2f,103f};\n    static readonly float[] RiverY={.14f,.17f,.21f,.27f,.34f,.39f,.47f,.58f,.70f,.86f,1.02f,1.75f,3.0f,10f,27f,48f,66f};'''
new='''    // One continuous drainage: mountain spring -> cascade -> bridges -> estuary -> sea.\n    static readonly float[] RiverSy={58f,61.5f,65f,68.5f,73.2f,76.5f,80f,84.5f,87.5f,90.5f,94f,97f,99f,100f,101f,102f,102.6f,103f};\n    static readonly float[] RiverX={89f,92.3f,89.3f,93.7f,95.5f,98.5f,100.2f,103.5f,101.9f,104.4f,103.0f,104.7f,104f,103.8f,103.5f,103.2f,103.08f,103f};\n    static readonly float[] RiverY={.14f,.17f,.21f,.27f,.34f,.39f,.47f,.58f,.70f,.86f,1.02f,1.75f,3.0f,8.5f,20f,37f,51f,61.5f};'''
rep(old,new,'river source profile')
old='''        float d=RiverDownstream01(sy);\n        float w=Mathf.Lerp(1.65f,4.10f,d);\n        float broad=Mathf.Lerp(.84f,1.18f,Mathf.PerlinNoise(sy*.118f+3.4f,9.7f));'''
new='''        float d=RiverDownstream01(sy);\n        // A spring is a trickle, not a pond. Width grows progressively downstream.\n        float w=Mathf.Lerp(.42f,4.10f,Mathf.Pow(d,.72f));\n        float broad=Mathf.Lerp(.90f,1.12f,Mathf.PerlinNoise(sy*.118f+3.4f,9.7f));'''
rep(old,new,'spring to river width growth')

s=s.replace('Mathf.Lerp(5.5f,10.5f,RiverDownstream01(sy))','Mathf.Lerp(2.4f,10.5f,RiverDownstream01(sy))')
print('PATCH narrow source banks',s.count('Mathf.Lerp(2.4f,10.5f,RiverDownstream01(sy))'))

old='''        var water = BuildWater(root.transform, terrain, env.waterMaterial);'''
new='''        var water = BuildWater(root.transform, terrain, env);'''
rep(old,new,'pass environment to water')

old='''    static GameObject BuildWater(Transform parent,Terrain terrain,Material unused)\n    {'''
new='''    static GameObject BuildWater(Transform parent,Terrain terrain,EnvAssets e)\n    {'''
rep(old,new,'water signature')
old='''            Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidthWorld(sy)+.80f,wy=RiverSurfaceY(sy)+.16f;'''
new='''            float downstream=RiverDownstream01(sy);\n            Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);\n            float hw=RiverHalfWidthWorld(sy)+Mathf.Lerp(.10f,.42f,downstream),wy=RiverSurfaceY(sy)+.14f;'''
rep(old,new,'remove source water bulb')

old='''    static GameObject BuildWater(Transform parent,Terrain terrain,EnvAssets e)\n    {'''
new='''    static void BuildRiverSourceRocks(Transform parent,Terrain terrain,EnvAssets e)\n    {\n        if(!e.botdRockPrefab)return;\n        var root=new GameObject("Mountain Spring Source");root.transform.SetParent(parent);\n        float sy=103f,cx=RiverCenterX(sy);\n        float wx=(64f-cx)*Cell,wz=(sy-64f)*Cell;\n        Vector2[] offsets={new Vector2(-1.35f,.65f),new Vector2(1.25f,.55f),new Vector2(-.82f,1.55f),new Vector2(.92f,1.75f),new Vector2(0f,2.35f)};\n        float[] heights={1.8f,1.55f,1.35f,1.65f,2.15f};\n        for(int i=0;i<offsets.Length;i++)\n        {\n            var go=(GameObject)PrefabUtility.InstantiatePrefab(e.botdRockPrefab);\n            go.name="SpringRock_"+i;go.transform.SetParent(root.transform);\n            float x=wx+offsets[i].x,z=wz+offsets[i].y;\n            go.transform.position=new Vector3(x,SampleTerrainY(terrain,x,z)-.18f,z);\n            go.transform.rotation=Quaternion.Euler(Deterministic01(17100+i*7)*12f-6f,Deterministic01(17200+i*11)*360f,Deterministic01(17300+i*13)*10f-5f);\n            ScaleToHeight(go,heights[i]);\n            foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=e.rockMaterial;\n        }\n    }\n\n    static GameObject BuildWater(Transform parent,Terrain terrain,EnvAssets e)\n    {'''
rep(old,new,'spring source rocks helper')

old='''        var river=BuildRealRiver(root.transform);\n        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} banks=terrain-wet-layer source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");'''
new='''        var river=BuildRealRiver(root.transform);\n        BuildRiverSourceRocks(root.transform,terrain,e);\n        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} spring=rocks source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");'''
rep(old,new,'spring source placement')

p.write_text(s,encoding='utf-8')
print('PATCH_HILL_SPRING_DONE',len(s.splitlines()))
