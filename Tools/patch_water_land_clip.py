from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMInternalCoastPass.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMInternalCoastPass_pre_land_clip_20260918.cs"))
s=s.replace('int tris=BuildWater(z,root.transform,tile,sem,mat);','int tris=BuildWater(z,root.transform,tile,sem,mat,terrain);')
s=s.replace('static int BuildWater(Z z,Transform root,byte[] tile,byte[] sem,Material mat)',
            'static int BuildWater(Z z,Transform root,byte[] tile,byte[] sem,Material mat,Terrain terrain)')
old='''            for(int i=0;i<4;i++){w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);if(w[i]>=th)bits|=1<<i;}'''
new='''            for(int i=0;i<4;i++)
            {
                w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);
                float ground=terrain.SampleHeight(new Vector3(p[i].x,0f,p[i].y))+terrain.transform.position.y;
                // Never let shoreline smoothing paint water across real land/building ground.
                if(ground>WaterY+.18f) w[i]=0f;
                if(w[i]>=th)bits|=1<<i;
            }'''
if old not in s: raise SystemExit("water loop needle missing")
s=s.replace(old,new,1)
p.write_text(s,encoding="utf-8")
print("INTERNAL_WATER_LAND_CLIP_PATCHED")
