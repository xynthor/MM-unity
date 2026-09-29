from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal3.cs')
s=p.read_text(encoding='utf-8-sig')
marker='    static Terrain BuildMainlandTerrain(Transform parent,Terrain[,] src)\n'
helpers=r'''    static float EllipseField(float nx,float nz,float cx,float cz,float rx,float rz)
    {
        float dx=(nx-cx)/rx,dz=(nz-cz)/rz;
        return Mathf.Clamp01(1f-(dx*dx+dz*dz));
    }
    static float TransitionStrength(float gx,float gz)
    {
        float fx=Mathf.Abs((gx-Mathf.Floor(gx))-.5f)*2f;
        float fz=Mathf.Abs((gz-Mathf.Floor(gz))-.5f)*2f;
        float sx=1f-Mathf.SmoothStep(.18f,.72f,fx);
        float sz=1f-Mathf.SmoothStep(.18f,.72f,fz);
        return Mathf.Max(sx,sz);
    }
    static void SmoothTransitionHeights(float[,] h,float minX,float minZ,float spanX,float spanZ)
    {
        int n=h.GetLength(0); var tmp=new float[n,n];
        for(int pass=0;pass<2;pass++)
        {
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float wx=Mathf.Lerp(minX-CoastPad,minX+spanX+CoastPad,x/(float)(n-1));
                float wz=Mathf.Lerp(minZ-CoastPad,minZ+spanZ+CoastPad,y/(float)(n-1));
                WarpGrid(Mathf.Clamp(wx,minX,minX+spanX),Mathf.Clamp(wz,minZ,minZ+spanZ),minX,minZ,out float gx,out float gz);
                float w=TransitionStrength(gx,gz)*.58f;
                if(w<.01f||x==0||y==0||x==n-1||y==n-1){tmp[y,x]=h[y,x];continue;}
                float blur=(h[y,x]*4f+h[y-1,x]+h[y+1,x]+h[y,x-1]+h[y,x+1]+h[y-1,x-1]*.5f+h[y-1,x+1]*.5f+h[y+1,x-1]*.5f+h[y+1,x+1]*.5f)/10f;
                tmp[y,x]=Mathf.Lerp(h[y,x],blur,w);
            }
            var swap=h; h=tmp; tmp=swap;
        }
    }
'''
if helpers not in s:
    s=s.replace(marker,helpers+'\n'+marker,1)
old='        td.SetHeights(0,0,heights);\n        td.alphamapResolution=1024;'
new='        SmoothTransitionHeights(heights,minX,minZ,spanX,spanZ);\n        td.SetHeights(0,0,heights);\n        td.alphamapResolution=1024;'
if old not in s: raise SystemExit('height marker not found')
s=s.replace(old,new,1)
start=s.index('        var alpha=new float[1024,1024,9];')
end=s.index('        td.SetAlphamaps(0,0,alpha);',start)
block=r'''        var alpha=new float[1024,1024,9];
        for(int y=0;y<1024;y++) for(int x=0;x<1024;x++)
        {
            float wx=Mathf.Lerp(minX-CoastPad,maxX+CoastPad,x/1023f);
            float wz=Mathf.Lerp(minZ-CoastPad,maxZ+CoastPad,y/1023f);
            float nx=(wx-minX)/spanX,nz=(wz-minZ)/spanZ,mask=ContinentMask(nx,nz);
            float cx=Mathf.Clamp(wx,minX,maxX),cz=Mathf.Clamp(wz,minZ,maxZ);
            float elev=SampleGrid(src,cx,cz,minX,minZ);
            float macro=Mathf.PerlinNoise(wx*.00072f+11.7f,wz*.00078f+4.6f);
            float detail=Mathf.PerlinNoise(wx*.00155f+2.1f,wz*.00143f+8.9f);
            float road=SampleGridAlpha(src,sourceAlpha,cx,cz,minX,minZ,2);
            float rock=SampleGridAlpha(src,sourceAlpha,cx,cz,minX,minZ,3);
            float coast=Mathf.Clamp01((.74f-mask)/.74f);
            float roadW=road*.86f*mask;
            float rockW=Mathf.Clamp01(rock*.66f+coast*.14f)*mask;
            float coastSand=coast*.58f*mask;
            float usable=Mathf.Max(0f,mask-roadW-rockW-coastSand*.42f);
'''
block+=r'''            float snowGeo=EllipseField(nx,nz,.49f,.90f,.28f,.25f);
            float snowElev=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(34f,88f,elev+(macro-.5f)*28f));
            float snow=snowGeo*snowElev*(.78f+.22f*detail);
            float desert=EllipseField(nx,nz,.31f,.13f,.27f,.23f)*(.72f+.28f*macro);
            float swamp=EllipseField(nx,nz,.53f,.17f,.24f,.23f)*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(14f,46f,elev)))*(.70f+.30f*detail);
            float ash=EllipseField(nx,nz,.14f,.78f,.19f,.25f)*(.62f+.38f*detail);
            float dark=Mathf.Max(EllipseField(nx,nz,.28f,.68f,.24f,.30f),EllipseField(nx,nz,.28f,.47f,.22f,.25f))*(.68f+.32f*macro);
            float tropical=Mathf.Max(EllipseField(nx,nz,.82f,.56f,.31f,.38f),EllipseField(nx,nz,.93f,.72f,.17f,.27f))*(.70f+.30f*detail);
            float specialized=Mathf.Clamp01(Mathf.Max(Mathf.Max(snow,desert),Mathf.Max(swamp,Mathf.Max(ash,Mathf.Max(dark,tropical)))));
            float temperate=.34f+.66f*(1f-specialized);
            float wt=temperate,wtr=tropical*1.35f,wd=dark*1.45f,wsw=swamp*1.70f,wsa=desert*1.75f,wsn=snow*2.05f,wa=ash*1.55f;
            float baseSum=wt+wtr+wd+wsw+wsa+wsn+wa;
            alpha[y,x,0]+=usable*wt/baseSum;
            alpha[y,x,1]+=usable*wtr/baseSum;
            alpha[y,x,2]+=usable*wd/baseSum;
            alpha[y,x,3]+=usable*wsw/baseSum;
            alpha[y,x,4]+=usable*wsa/baseSum+coastSand;
            alpha[y,x,5]+=usable*wsn/baseSum;
            alpha[y,x,7]+=usable*wa/baseSum;
            alpha[y,x,6]+=rockW; alpha[y,x,8]+=roadW;
            if(mask<.06f) alpha[y,x,4]=1f;
            float sum=0f; for(int l=0;l<9;l++) sum+=alpha[y,x,l];
            if(sum<.001f){alpha[y,x,0]=1f;sum=1f;}
            for(int l=0;l<9;l++) alpha[y,x,l]/=sum;
        }
'''
s=s[:start]+block+s[end:]
pos=s.index('    static float SampleGridAlpha')
needle='        float gx=(wx-minX)/RegionSize, gz=(wz-minZ)/RegionSize;\n        AxisBlend(gx,Cols,out int c0,out int c1,out float tx);\n        AxisBlend(gz,Rows,out int r0,out int r1,out float tz);'
repl='        WarpGrid(wx,wz,minX,minZ,out float gx,out float gz);\n        AxisBlend(gx,Cols,out int c0,out int c1,out float tx);\n        AxisBlend(gz,Rows,out int r0,out int r1,out float tz);'
head=s[:pos]; tail=s[pos:]
if needle not in tail: raise SystemExit('alpha warp marker not found')
s=head+tail.replace(needle,repl,1)
p.write_text(s,encoding='utf-8')
print('patched final3',len(s.splitlines()))
