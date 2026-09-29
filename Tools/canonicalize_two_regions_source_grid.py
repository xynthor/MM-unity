from pathlib import Path
import re

ROOT=Path(r'C:\MMUnityPort')
for exp in [ROOT/'Tools/rebuild_textured_new_sorpigal_objects_v2.py',ROOT/'Tools/rebuild_textured_castle_ironfist_objects_v2.py']:
    s=exp.read_text(encoding='utf-8-sig')
    s=s.replace('out.append(f"v {x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}")','out.append(f"v {-x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}")')
    exp.write_text(s,encoding='utf-8')

for zone in ['NewSorpigal','CastleIronfist']:
    p=ROOT/f'Assets/Editor/Build{zone}OpenWorld.cs'
    s=p.read_text(encoding='utf-8-sig')

    s=s.replace('return new Vector2(origin+((N-1f)-sx)*step,origin+sy*step);','return new Vector2(origin+sx*step,origin+sy*step);')
    s=s.replace('return new Vector2(origin+(N-1f-sx)*step,origin+sy*step);','return new Vector2(origin+sx*step,origin+sy*step);')
    s=s.replace('float sx=(N-1f)-(x-origin)/step;','float sx=(x-origin)/step;')
    s=s.replace('float sx=(N-1)-x/(float)hmMax*(N-1),sy=y/(float)hmMax*(N-1);','float sx=x/(float)hmMax*(N-1),sy=y/(float)hmMax*(N-1);')
    s=s.replace('float sx=(N-1)-x/511f*(N-1),sy=y/511f*(N-1);','float sx=x/511f*(N-1),sy=y/511f*(N-1);')

    s=s.replace('new Vector2(ox*WorldScale,oz*WorldScale)','new Vector2(-ox*WorldScale,oz*WorldScale)')
    s=s.replace('float x=ox*WorldScale,z=oz*WorldScale;','float x=-ox*WorldScale,z=oz*WorldScale;')
    s=s.replace('float sx = originalSpawn.x * WorldScale;','float sx = -originalSpawn.x * WorldScale;')
    s=s.replace('if(requireGrass&&IsDirt(t))return false;','if(requireGrass&&TileGroup(t)!=0)return false;')

    start=s.index('    static void EnsureHydroRoadMasks(byte[] tiles)')
    end=s.index('    static float SampleBoolMask',start)
    replacement='''    static void EnsureHydroRoadMasks(byte[] tiles)\n    {\n        if(correctedWaterCache!=null && correctedRoadCache!=null) return;\n        correctedWaterCache=new bool[N,N];\n        correctedRoadCache=new bool[N,N];\n        maskHeightCache=File.ReadAllBytes(HeightPath);\n        for(int y=0;y<N;y++) for(int x=0;x<N;x++)\n        {\n            byte t=tiles[y*N+x];\n            correctedWaterCache[y,x]=IsWater(t);\n            correctedRoadCache[y,x]=IsRoad(t) && !correctedWaterCache[y,x];\n        }\n    }\n\n'''
    s=s[:start]+replacement+s[end:]

    if zone=='NewSorpigal':
        start=s.index('    static float SampleNaturalTerrainHeight(byte[] heights,float sx,float sy)')
        end=s.index('    static float CreativeLandHeight',start)
        natural='''    static float SampleNaturalTerrainHeight(byte[] heights,float sx,float sy)\n    {\n        float center=SampleByteBilinear(heights,sx,sy);\n        float sum=0f,weight=0f;\n        for(int oy=-1;oy<=1;oy++) for(int ox=-1;ox<=1;ox++)\n        {\n            float w=(ox==0&&oy==0)?4f:((ox==0||oy==0)?1.5f:.75f);\n            sum+=SampleByteBilinear(heights,sx+ox*.62f,sy+oy*.62f)*w;\n            weight+=w;\n        }\n        float smooth=sum/Mathf.Max(.0001f,weight);\n        float elevated=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(8f,48f,center));\n        return Mathf.Lerp(center,smooth,.32f*elevated);\n    }\n\n    static float SourceFaithfulLandHeight(byte[] heights,float sx,float sy)\n    {\n        return BlueprintBaseHeight(SampleNaturalTerrainHeight(heights,sx,sy)*.25f);\n    }\n\n'''
        s=s[:start]+natural+s[end:]
        s=s.replace('float landY=CreativeLandHeight(rawY,sx,sy);','float landY=SourceFaithfulLandHeight(heights,sx,sy);')
        s=s.replace('float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*.25f,sx,sy),slope=td.GetSteepness(x/511f,y/511f);','float h=SourceFaithfulLandHeight(heights,sx,sy),slope=td.GetSteepness(x/511f,y/511f);')

    p.write_text(s,encoding='utf-8')
    print('patched',zone)
