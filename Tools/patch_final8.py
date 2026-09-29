from pathlib import Path
src=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal7.cs')
dst=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorldFinal8.cs')
s=src.read_text(encoding='utf-8-sig').replace('BuildEnrothFullWorldFinal7','BuildEnrothFullWorldFinal8')
s=s.replace('for(int pass=0;pass<2;pass++)','for(int pass=0;pass<4;pass++)',1)
s=s.replace('float w=TransitionStrength(gx,gz)*.58f;','float w=TransitionStrength(gx,gz)*.72f;',1)
s=s.replace('"Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx"','"Assets/Environment/Generated/pine_sapling_game.fbx"',1)
s=s.replace('terrain.detailObjectDistance=115f; terrain.detailObjectDensity=.55f;','terrain.detailObjectDistance=90f; terrain.detailObjectDensity=.45f;',1)
s=s.replace('terrain.treeDistance=1400f; terrain.treeBillboardDistance=220f; terrain.treeCrossFadeLength=20f; terrain.treeMaximumFullLODCount=100;','terrain.treeDistance=1100f; terrain.treeBillboardDistance=180f; terrain.treeCrossFadeLength=18f; terrain.treeMaximumFullLODCount=60;',1)
s=s.replace('streamer.player=player.transform; streamer.loadDistance=2250f; streamer.unloadDistance=2900f; streamer.checkInterval=.55f;','streamer.player=player.transform; streamer.loadDistance=1900f; streamer.unloadDistance=2500f; streamer.checkInterval=.55f;',1)
s=s.replace('regions="+streamer.regions.Length+" load=2250 unload=2900','regions="+streamer.regions.Length+" load=1900 unload=2500',1)
old='''            float wh=SampleWorld(src,wx,wz);
            bool dry=wh>OceanY+.40f;
            if(dry) land++;
            else wh=-38f-(Mathf.PerlinNoise(u*6.1f+3f,v*5.7f+8f)*2f);'''
new='''            float wh=SampleWorld(src,wx,wz);
            float edge=Mathf.Min(Mathf.Min(u,1f-u),Mathf.Min(v,1f-v));
            float edgeGate=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.025f,.135f,edge));
            wh=Mathf.Lerp(-38f,wh,edgeGate);
            bool dry=wh>OceanY+.40f;
            if(dry) land++;
            else wh=-38f-(Mathf.PerlinNoise(u*6.1f+3f,v*5.7f+8f)*2f);'''
if old not in s: raise SystemExit('offshore height block missing')
s=s.replace(old,new,1)
old2='''            float wh=SampleWorld(src,wx,wz);
            if(wh<=OceanY+.40f){a[y,x,4]=1f;continue;}'''
new2='''            float wh=SampleWorld(src,wx,wz);
            float edge=Mathf.Min(Mathf.Min(u,1f-u),Mathf.Min(v,1f-v));
            float edgeGate=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.025f,.135f,edge));
            wh=Mathf.Lerp(-38f,wh,edgeGate);
            if(wh<=OceanY+.40f){a[y,x,4]=1f;continue;}'''
if old2 not in s: raise SystemExit('offshore alpha block missing')
s=s.replace(old2,new2,1)
helper=r'''
    static void SoftenLegacyHeightBands(float[,] h,float worldMinX,float worldMinZ,float worldMaxX,float worldMaxZ,float gridMinX,float gridMinZ,float spanX,float spanZ)
    {
        int n=h.GetLength(0); var tmp=new float[n,n];
        for(int pass=0;pass<2;pass++)
        {
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                if(x==0||y==0||x==n-1||y==n-1){tmp[y,x]=h[y,x];continue;}
                float wy=TerrainBaseY+h[y,x]*TerrainHeight;
                if(wy<=OceanY+.45f){tmp[y,x]=h[y,x];continue;}
                float wx=Mathf.Lerp(worldMinX-CoastPad,worldMaxX+CoastPad,x/(float)(n-1));
                float wz=Mathf.Lerp(worldMinZ-CoastPad,worldMaxZ+CoastPad,y/(float)(n-1));
                float nx=(wx-gridMinX)/spanX,nz=(wz-gridMinZ)/spanZ;
                float mask=ContinentMask(nx,nz);
                if(mask<.32f){tmp[y,x]=h[y,x];continue;}
                float blur=(h[y,x]*4f+h[y-1,x]+h[y+1,x]+h[y,x-1]+h[y,x+1]+h[y-1,x-1]+h[y-1,x+1]+h[y+1,x-1]+h[y+1,x+1])/12f;
                WarpGrid(wx,wz,gridMinX,gridMinZ,out float gx,out float gz);
                float w=.18f+TransitionStrength(gx,gz)*.22f;
                tmp[y,x]=Mathf.Lerp(h[y,x],blur,w);
            }
            var swap=h; h=tmp; tmp=swap;
        }
    }
'''
anchor='    static Material MasterDetailMaterial(string name,Color color)'
if anchor not in s: raise SystemExit('detail material anchor missing')
s=s.replace(anchor,helper+'\n'+anchor,1)
call='''        SmoothTransitionHeights(heights,minX,minZ,maxX,maxZ,gridMinX,gridMinZ);
        td.SetHeights(0,0,heights);'''
newcall='''        SmoothTransitionHeights(heights,minX,minZ,maxX,maxZ,gridMinX,gridMinZ);
        SoftenLegacyHeightBands(heights,minX,minZ,maxX,maxZ,gridMinX,gridMinZ,spanX,spanZ);
        td.SetHeights(0,0,heights);'''
if call not in s: raise SystemExit('smoothing call missing')
s=s.replace(call,newcall,1)
dst.write_text(s,encoding='utf-8')
print('patched final8',len(s.splitlines()))
