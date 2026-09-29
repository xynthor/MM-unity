from pathlib import Path
import re

P=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=P.read_text(encoding='utf-8')

def one(old,new,label):
    global s
    if s.count(old)!=1:
        raise SystemExit(f'{label}: expected 1 literal, got {s.count(old)}')
    s=s.replace(old,new,1)
    print('PATCH',label)

def rex(pattern,repl,label):
    global s
    ns,n=re.subn(pattern,repl,s,count=1,flags=re.S)
    if n!=1:
        raise SystemExit(f'{label}: expected 1 regex, got {n}')
    s=ns
    print('PATCH',label)

one('const int TerrainResolution = 513;','const int TerrainResolution = 1025;','terrain resolution')

coast='''    static float SampleCoastalWaterMask(byte[] tiles,float sx,float sy)
    {
        float warpX=(Mathf.PerlinNoise(sx*.075f+17.3f,sy*.071f+4.9f)-.5f)*.85f;
        float warpY=(Mathf.PerlinNoise(sx*.069f+2.1f,sy*.081f+23.7f)-.5f)*.85f;
        sx+=warpX; sy+=warpY;
        float sum=0f,weight=0f;
        for(int oy=-4;oy<=4;oy++) for(int ox=-4;ox<=4;ox++)
        {
            float d2=ox*ox+oy*oy;
            float w=Mathf.Exp(-d2*.18f);
            sum+=SampleWaterMask(tiles,sx+ox*.42f,sy+oy*.42f)*w;
            weight+=w;
        }
        return sum/Mathf.Max(.0001f,weight);
    }

'''
marker='    static readonly float[] RiverSy='
if marker not in s: raise SystemExit('river marker missing')
s=s.replace(marker,coast+marker,1);print('PATCH coastal mask')
one('float original=SampleSmoothWaterMask(tiles,sx,sy);','float original=SampleCoastalWaterMask(tiles,sx,sy);','sea mask smoother')

old_width='''        float d=RiverDownstream01(sy);
        float w=Mathf.Lerp(1.05f,3.35f,d);
        return w*Mathf.Lerp(.90f,1.10f,Mathf.PerlinNoise(sy*.27f+11.3f,2.2f));'''
new_width='''        float d=RiverDownstream01(sy);
        float w=Mathf.Lerp(1.65f,4.10f,d);
        return w*Mathf.Lerp(.94f,1.06f,Mathf.PerlinNoise(sy*.27f+11.3f,2.2f));'''
one(old_width,new_width,'river width')

old_sea='''            float seaMask=SampleSeaWaterMask(tiles,sx,sy);
            if(seaMask>=.5f)
            {
                float d=SampleDistanceBilinear(waterDist,sx,sy);
                worldY=-Mathf.Clamp(.75f+d*.82f,.75f,13.5f);
            }
            else
            {
                float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;
                worldY=Mathf.Lerp(worldY,BlueprintBaseHeight(rawY),protectedBlend);
                worldY=Mathf.Max(worldY,.38f);
            }'''
new_sea='''            float seaMask=SampleSeaWaterMask(tiles,sx,sy);
            float coastBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.34f,.66f,seaMask));
            float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;
            float landY=Mathf.Lerp(worldY,BlueprintBaseHeight(rawY),protectedBlend);
            landY=Mathf.Max(landY,.38f);
            float dSea=SampleDistanceBilinear(waterDist,sx,sy);
            float seaY=-Mathf.Clamp(.75f+dSea*.82f,.75f,13.5f);
            worldY=Mathf.Lerp(landY,seaY,coastBlend);'''
one(old_sea,new_sea,'continuous coast terrain')
one('float bankExtra=Mathf.Lerp(2.6f,6.8f,RiverDownstream01(sy));','float bankExtra=Mathf.Lerp(5.5f,10.5f,RiverDownstream01(sy));','river banks')
one('float outer=halfW>0f?halfW+Mathf.Lerp(2.6f,6.8f,RiverDownstream01(sy)):0f;','float outer=halfW>0f?halfW+Mathf.Lerp(5.5f,10.5f,RiverDownstream01(sy)):0f;','river texture banks')
one('var mat=CreateSimpleMaterial(name,Shader.Find("MMUnity/RealCoastalWater"),Color.white);','var mat=CreateSimpleMaterial(name,Shader.Find(river?"MMUnity/RiverWater":"MMUnity/RealCoastalWater"),Color.white);','dedicated river shader')
one('const int segs=360;','const int segs=560;','river segments')
one('float hw=RiverHalfWidthWorld(sy),wy=RiverSurfaceY(sy)+.045f;','float hw=RiverHalfWidthWorld(sy),wy=RiverSurfaceY(sy)+.14f;','river surface clearance')
one('mat.SetFloat("_WaveAmp",river?.035f:.18f);','mat.SetFloat("_WaveAmp",river?.012f:.18f);','river wave amplitude')
one('mat.SetFloat("_FoamDepth",river?.38f:1.15f);','mat.SetFloat("_FoamDepth",river?.24f:1.15f);','river foam depth')

P.write_text(s,encoding='utf-8')
print('PATCH_SMOOTH_RIVER_SEA_DONE',len(s.splitlines()))
