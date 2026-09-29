from pathlib import Path
root=Path(r"C:\MMUnityPort")
# Ecotone: preserve each side's own dominant biome while adding secondary transition.
p=root/"Assets/Editor/MMEcotoneSeamPass.cs"
s=p.read_text(encoding="utf-8-sig")
old='''    static void MixPixel(float[,,] a,int y,int x,float[] target,float strength)
    {
        int l=a.GetLength(2);float road=a[y,x,RoadLayer];
        if(road>.20f)return;
        float s=0f;
        for(int k=0;k<l;k++)
        {
            float t=k==RoadLayer?0f:target[k];
            a[y,x,k]=Mathf.Lerp(a[y,x,k],t,strength);
            s+=a[y,x,k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)a[y,x,k]/=s;
    }'''
new='''    static void MixPixel(float[,,] a,int y,int x,float[] target,float strength)
    {
        int l=a.GetLength(2);float road=a[y,x,RoadLayer];if(road>.20f)return;
        int dom=0;for(int k=1;k<l;k++)if(a[y,x,k]>a[y,x,dom])dom=k;
        float s=0f;
        for(int k=0;k<l;k++)
        {
            float t=k==RoadLayer?0f:target[k];
            a[y,x,k]=Mathf.Lerp(a[y,x,k],t,strength*.62f);s+=a[y,x,k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)a[y,x,k]/=s;
        if(a[y,x,dom]<.56f)
        {
            float need=.56f-a[y,x,dom],other=Mathf.Max(.0001f,1f-a[y,x,dom]);
            for(int k=0;k<l;k++)if(k!=dom)a[y,x,k]*=Mathf.Max(0f,(other-need)/other);
            a[y,x,dom]=.56f;
        }
    }'''
if old not in s: raise SystemExit("ecotone MixPixel needle missing")
s=s.replace(old,new,1)
p.write_text(s,encoding="utf-8")

# Source-exact audit: use canonical source/zone-aware biome expectation and never index a missing layer.
p=root/"Assets/Editor/AuditWorldSourceExact.cs"
s=p.read_text(encoding="utf-8-sig")
old='''            byte raw=tile[sy*N+sx],g=group[raw],f=sem[raw];int exp=ExpectedLayer(g,f);
            int ax=Mathf.Clamp(sx*4+2,0,td.alphamapWidth-1),ay=Mathf.Clamp((N-1-sy)*4+2,0,td.alphamapHeight-1);int act=0;float best=-1f;
            for(int k=0;k<a.GetLength(2);k++)if(a[ay,ax,k]>best){best=a[ay,ax,k];act=k;}
            float ew=((f&1)!=0&&exp==8)?.62f:1f;bool tm=act!=exp||Mathf.Abs(a[ay,ax,exp]-ew)>.08f;if(tm)tileMismatch++;
            float wx=(sx-64f)*4f,wz=(64f-sy)*4f;float hs=height[sy*N+sx]*.25f;float hc=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;float hd=hc-hs;
            if((f&1)==0){hsum+=Mathf.Abs(hd);hmax=Mathf.Max(hmax,Mathf.Abs(hd));hn++;}
            if(tm||Mathf.Abs(hd)>2.5f)tilesOut.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),raw.ToString(),g.ToString(),f.ToString(),exp.ToString(),act.ToString(),ew.ToString("F2",CultureInfo.InvariantCulture),a[ay,ax,exp].ToString("F2",CultureInfo.InvariantCulture),hs.ToString("F3",CultureInfo.InvariantCulture),hc.ToString("F3",CultureInfo.InvariantCulture),hd.ToString("F3",CultureInfo.InvariantCulture),tm?"TILE_MISMATCH":"HEIGHT_CHECK"}));'''
new='''            byte raw=tile[sy*N+sx],g=group[raw],f=sem[raw];
            int exp=MMWitcherTerrainPass.ExpectedLayerForCell(z.key,tile,group,sem,sx,sy);
            int ax=Mathf.Clamp(sx*4+2,0,td.alphamapWidth-1),ay=Mathf.Clamp((N-1-sy)*4+2,0,td.alphamapHeight-1);int act=0;float best=-1f;
            for(int k=0;k<a.GetLength(2);k++)if(a[ay,ax,k]>best){best=a[ay,ax,k];act=k;}
            bool layerMissing=exp<0||exp>=a.GetLength(2);float actualExpected=layerMissing?0f:a[ay,ax,exp];
            float ew=1f;bool tm=layerMissing||act!=exp;if(tm)tileMismatch++;
            float wx=(sx-64f)*4f,wz=(64f-sy)*4f;float hs=height[sy*N+sx]*.25f;float hc=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;float hd=hc-hs;
            if((f&1)==0){hsum+=Mathf.Abs(hd);hmax=Mathf.Max(hmax,Mathf.Abs(hd));hn++;}
            if(tm||Mathf.Abs(hd)>2.5f)tilesOut.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),raw.ToString(),g.ToString(),f.ToString(),exp.ToString(),act.ToString(),ew.ToString("F2",CultureInfo.InvariantCulture),actualExpected.ToString("F2",CultureInfo.InvariantCulture),hs.ToString("F3",CultureInfo.InvariantCulture),hc.ToString("F3",CultureInfo.InvariantCulture),hd.ToString("F3",CultureInfo.InvariantCulture),layerMissing?"LAYER_MISSING":tm?"TILE_MISMATCH":"HEIGHT_CHECK"}));'''
if old not in s: raise SystemExit("audit tile needle missing")
s=s.replace(old,new,1)
p.write_text(s,encoding="utf-8")
print("PATCHED_ECOTONE_AND_SOURCE_AUDIT")