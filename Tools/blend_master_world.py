from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorld.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('float oceanFloor=-36f-(Mathf.PerlinNoise(nx*5.1f+2f,nz*5.3f+9f)*4.5f);','float oceanFloor=-44.2f-(Mathf.PerlinNoise(nx*5.1f+2f,nz*5.3f+9f)*0.65f);')
s=s.replace('float wy=Mathf.Lerp(-36f,4f+noise,shore);','float wy=Mathf.Lerp(-44.2f,4f+noise,shore);')
s=s.replace('float local=g-i, band=0.18f;','float local=g-i, band=0.30f;')
start=s.index('        var alpha=new float[1024,1024,9];')
end=s.index('        td.SetAlphamaps(0,0,alpha);',start)
new=r'''        var alpha=new float[1024,1024,9];
        for(int y=0;y<1024;y++) for(int x=0;x<1024;x++)
        {
            float wx=Mathf.Lerp(minX-CoastPad,maxX+CoastPad,x/1023f);
            float wz=Mathf.Lerp(minZ-CoastPad,maxZ+CoastPad,y/1023f);
            float nx=(wx-minX)/spanX,nz=(wz-minZ)/spanZ,mask=ContinentMask(nx,nz);
            float cx=Mathf.Clamp(wx,minX,maxX),cz=Mathf.Clamp(wz,minZ,maxZ);
            float gx=(cx-minX)/RegionSize,gz=(cz-minZ)/RegionSize;
            AxisBlend(gx,Cols,out int c0,out int c1,out float tx);
            AxisBlend(gz,Rows,out int r0,out int r1,out float tz);
            float w00=(1f-tx)*(1f-tz),w10=tx*(1f-tz),w01=(1f-tx)*tz,w11=tx*tz;
            float road=SampleGridAlpha(src,sourceAlpha,cx,cz,minX,minZ,2);
            float rock=SampleGridAlpha(src,sourceAlpha,cx,cz,minX,minZ,3);
            float coast=Mathf.Clamp01((.72f-mask)/.72f);
            float roadW=road*.88f*mask;
            float rockW=Mathf.Clamp01(rock*.70f+coast*.16f)*mask;
            float sandW=coast*.62f*mask;
            float baseW=Mathf.Max(0f,mask-roadW-rockW-sandW*.50f);
            alpha[y,x,BiomeLayer(r0,c0)]+=baseW*w00;
            alpha[y,x,BiomeLayer(r0,c1)]+=baseW*w10;
            alpha[y,x,BiomeLayer(r1,c0)]+=baseW*w01;
            alpha[y,x,BiomeLayer(r1,c1)]+=baseW*w11;
            float desertBlend=(BiomeLayer(r0,c0)==4?w00:0f)+(BiomeLayer(r0,c1)==4?w10:0f)+(BiomeLayer(r1,c0)==4?w01:0f)+(BiomeLayer(r1,c1)==4?w11:0f);
            alpha[y,x,4]+=sandW+desertBlend*baseW*.45f;
            alpha[y,x,6]+=rockW;
            alpha[y,x,8]+=roadW;
            if(mask<.06f) alpha[y,x,4]=1f;
            float sum=0f; for(int l=0;l<9;l++) sum+=alpha[y,x,l];
            if(sum<.001f){alpha[y,x,4]=1f;sum=1f;}
            for(int l=0;l<9;l++) alpha[y,x,l]/=sum;
        }
'''
s=s[:start]+new+s[end:]
p.write_text(s,encoding='utf-8')
print('master biome/height blending patched')