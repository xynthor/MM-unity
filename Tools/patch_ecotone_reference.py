from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMEcotoneSeamPass.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMEcotoneSeamPass_pre_reference_blend_20260918.cs"))
s=s.replace("const float BlendBand=24f;","const float BlendBand=48f;")
start=s.index("    static void MixPixel")
end=s.index("    static int Blend",start)
new='''    static void MixPixel(float[,,] a,int y,int x,float[] target,float strength)
    {
        int l=a.GetLength(2);float road=a[y,x,RoadLayer];if(road>.20f)return;
        float s=0f;
        for(int k=0;k<l;k++)
        {
            float t=k==RoadLayer?0f:target[k];
            a[y,x,k]=Mathf.Lerp(a[y,x,k],t,strength);
            s+=a[y,x,k];
        }
        if(s>.0001f)for(int k=0;k<l;k++)a[y,x,k]/=s;
    }

'''
s=s[:start]+new+s[end:]
p.write_text(s,encoding="utf-8")
print("ECOTONE_REFERENCE_BLEND_PATCHED")
