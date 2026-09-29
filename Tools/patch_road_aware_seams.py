from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticSeamBlendPass.cs")
s=p.read_text(encoding="utf-8-sig")
old='''    static float[] Avg(float[] a,float[] b)
    {
        int l=Mathf.Min(a.Length,b.Length);var v=new float[l];float s=0f;
        for(int k=0;k<l;k++){v[k]=(a[k]+b[k])*.5f;s+=v[k];}
        if(s>.0001f)for(int k=0;k<l;k++)v[k]/=s;
        return v;
    }
    static float[] Smooth(float[][] src,int i)
    {
        int l=src[i].Length;var o=new float[l];float ws=0f;
        for(int q=Mathf.Max(0,i-4);q<=Mathf.Min(src.Length-1,i+4);q++)
        {
            float d=Mathf.Abs(q-i);float w=Mathf.Exp(-(d*d)/5f);
            for(int k=0;k<l;k++)o[k]+=src[q][k]*w;ws+=w;
        }
        if(ws>.0001f)for(int k=0;k<l;k++)o[k]/=ws;
        return o;
    }'''
new='''    static float[] Avg(float[] a,float[] b)
    {
        int l=Mathf.Min(a.Length,b.Length);var v=new float[l];float s=0f;
        int road=MMRealisticTerrainBiomePass.RoadOverlay;
        for(int k=0;k<l;k++)
        {
            if(k==road){v[k]=0f;continue;}
            v[k]=(a[k]+b[k])*.5f;s+=v[k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)v[k]/=s;
        return v;
    }
    static float[] Smooth(float[][] src,int i)
    {
        int l=src[i].Length;var o=new float[l];float ws=0f;
        int road=MMRealisticTerrainBiomePass.RoadOverlay;
        for(int q=Mathf.Max(0,i-4);q<=Mathf.Min(src.Length-1,i+4);q++)
        {
            float biome=0f;for(int k=0;k<l;k++)if(k!=road)biome+=src[q][k];
            if(biome<.0001f)continue; // never propagate road-only samples into biome ecotones
            float d=Mathf.Abs(q-i);float w=Mathf.Exp(-(d*d)/5f);
            for(int k=0;k<l;k++)if(k!=road)o[k]+=src[q][k]*w;
            ws+=w;
        }
        if(ws>.0001f)for(int k=0;k<l;k++)o[k]/=ws;
        o[road]=0f;
        return o;
    }'''
if old not in s: raise SystemExit("avg/smooth block not found")
s=s.replace(old,new,1)
# Also make BlendPixel explicitly preserve road and never inject it.
old2='''        if(a[y,x,MMRealisticTerrainBiomePass.Road]>.45f)return;
        int l=a.GetLength(2);float s=0f;
        for(int k=0;k<l;k++){a[y,x,k]=Mathf.Lerp(a[y,x,k],target[k],strength);s+=a[y,x,k];}'''
new2='''        int road=MMRealisticTerrainBiomePass.RoadOverlay;
        if(a[y,x,road]>.45f)return;
        int l=a.GetLength(2);float s=0f;
        for(int k=0;k<l;k++)
        {
            if(k==road){a[y,x,k]=0f;continue;}
            a[y,x,k]=Mathf.Lerp(a[y,x,k],target[k],strength);s+=a[y,x,k];
        }'''
if old2 not in s: raise SystemExit("blendpixel block not found")
s=s.replace(old2,new2,1)
p.write_text(s,encoding="utf-8")
print("ROAD_AWARE_SEAM_BLEND_PATCHED")
