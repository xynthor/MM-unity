from pathlib import Path
import re
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal2.cs')
s=p.read_text(encoding='utf-8-sig')
helpers=r'''    static void WarpGrid(float wx,float wz,float minX,float minZ,out float gx,out float gz)
    {
        float bx=(wx-minX)/RegionSize,bz=(wz-minZ)/RegionSize;
        float ux=bx/Cols,uz=bz/Rows;
        float n1=Mathf.PerlinNoise(ux*3.15f+4.7f,uz*3.45f+1.9f)-.5f;
        float n2=Mathf.PerlinNoise(ux*5.20f+8.1f,uz*4.75f+6.4f)-.5f;
        float n3=Mathf.PerlinNoise(ux*3.70f+2.2f,uz*5.05f+9.3f)-.5f;
        float n4=Mathf.PerlinNoise(ux*6.10f+7.6f,uz*3.85f+3.5f)-.5f;
        gx=bx+n1*.52f+n2*.18f;
        gz=bz+n3*.46f+n4*.16f;
    }

    static void CullUnderwaterVegetation(Transform regions,Terrain mainland)
    {
        int culled=0;
        foreach(Transform region in regions)
        {
            foreach(var veg in region.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Vegetation -",StringComparison.OrdinalIgnoreCase)))
            {
                foreach(Transform category in veg)
                {
                    var objs=category.Cast<Transform>().ToArray();
                    foreach(var obj in objs)
                    {
                        float y=mainland.SampleHeight(obj.position)+mainland.transform.position.y;
                        if(y<OceanY+.05f){obj.gameObject.SetActive(false);culled++;}
                    }
                }
            }
        }
        Debug.Log("ENROTH_UNDERWATER_NATURE_CULLED "+culled);
    }
'''
s=s.replace('    static Terrain BuildMainlandTerrain',helpers+'\n    static Terrain BuildMainlandTerrain',1)
s=s.replace('        var mainland=BuildMainlandTerrain(world.transform,terrains);','        var mainland=BuildMainlandTerrain(world.transform,terrains);\n        CullUnderwaterVegetation(regionRoot.transform,mainland);',1)
s=s.replace('float gx=(cx-minX)/RegionSize,gz=(cz-minZ)/RegionSize;','WarpGrid(cx,cz,minX,minZ,out float gx,out float gz);',1)
old='''    static float SampleGrid(Terrain[,] src,float wx,float wz,float minX,float minZ)\n    {\n        float gx=(wx-minX)/RegionSize, gz=(wz-minZ)/RegionSize;'''
new='''    static float SampleGrid(Terrain[,] src,float wx,float wz,float minX,float minZ)\n    {\n        WarpGrid(wx,wz,minX,minZ,out float gx,out float gz);'''
if old not in s: print('NO SampleGrid anchor')
s=s.replace(old,new,1)
s=s.replace('float local=g-i, band=0.30f;','float local=g-i, band=0.42f;',1)
anchor='''            alpha[y,x,BiomeLayer(r1,c1)]+=baseW*w11;\n            float desertBlend='''
insert='''            alpha[y,x,BiomeLayer(r1,c1)]+=baseW*w11;\n            float elev=SampleGrid(src,cx,cz,minX,minZ);\n            float macro=Mathf.PerlinNoise(wx*.00085f+12.4f,wz*.00091f+3.7f);\n            float snow=alpha[y,x,5];\n            if(snow>0f){float sf=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(20f,78f,elev+(macro-.5f)*26f));alpha[y,x,5]=snow*sf;alpha[y,x,6]+=snow*(1f-sf)*.55f;alpha[y,x,0]+=snow*(1f-sf)*.45f;}\n            float dark=alpha[y,x,2];\n            if(dark>0f){float df=.58f+.42f*Mathf.SmoothStep(0f,1f,macro);alpha[y,x,2]=dark*df;alpha[y,x,0]+=dark*(1f-df)*.68f;alpha[y,x,6]+=dark*(1f-df)*.32f;}\n            float swamp=alpha[y,x,3];\n            if(swamp>0f){float wet=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(12f,34f,elev+(macro-.5f)*12f));alpha[y,x,3]=swamp*wet;alpha[y,x,0]+=swamp*(1f-wet)*.72f;alpha[y,x,2]+=swamp*(1f-wet)*.28f;}\n            float ash=alpha[y,x,7];\n            if(ash>0f){float af=.48f+.52f*Mathf.PerlinNoise(wx*.00125f+5.1f,wz*.00110f+8.4f);alpha[y,x,7]=ash*af;alpha[y,x,2]+=ash*(1f-af)*.55f;alpha[y,x,6]+=ash*(1f-af)*.45f;}\n            float desertBlend='''
if anchor not in s: print('NO alpha anchor')
s=s.replace(anchor,insert,1)
p.write_text(s,encoding='utf-8')
print('patched final2',len(s.splitlines()))
