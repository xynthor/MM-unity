from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMInternalCoastPass.cs")
s=p.read_text(encoding="utf-8-sig")

old='''        Material mat=null;var old=root.transform.Find("Water");
        if(old){var r=old.GetComponentInChildren<MeshRenderer>(true);if(r)mat=r.sharedMaterial;UnityEngine.Object.DestroyImmediate(old.gameObject);}
        var old2=root.transform.Find("Internal Water - Smooth");if(old2)UnityEngine.Object.DestroyImmediate(old2.gameObject);
        if(!mat)mat=WaterMaterial();'''
new='''        Material mat=MMUnifiedWorldWaterPass.SharedWaterMaterial();
        var old=root.transform.Find("Water");
        if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var old2=root.transform.Find("Internal Water - Smooth");if(old2)UnityEngine.Object.DestroyImmediate(old2.gameObject);'''
if old not in s: raise SystemExit("material block missing")
s=s.replace(old,new,1)

needle='''    static float SoftWorld(byte[] tile,byte[] sem,float x,float z)=>Soft(tile,sem,x/4f+64f,64f-z/4f);
    static Material WaterMaterial()'''
repl='''    static float SoftWorld(byte[] tile,byte[] sem,float x,float z)=>Soft(tile,sem,x/4f+64f,64f-z/4f);
    static bool SourceWaterOrShore(byte[] tile,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.FloorToInt(x/4f+64f),0,N-1);
        int sy=Mathf.Clamp(Mathf.FloorToInt(64f-z/4f),0,N-1);
        byte f=sem[tile[sy*N+sx]];
        return (f&1)!=0||(f&2)!=0;
    }
    static Material WaterMaterial()'''
if needle not in s: raise SystemExit("source mask insert missing")
s=s.replace(needle,repl,1)

old='''                w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);
                float gy=terrain.SampleHeight(new Vector3(p[i].x,0f,p[i].y))+terrain.transform.position.y;
                if(gy>WaterY+.08f)w[i]=0f;
                if(w[i]>=th)bits|=1<<i;'''
new='''                w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);
                // Never smooth water into source land cells. Shore cells are the transition band.
                if(!SourceWaterOrShore(tile,sem,p[i].x,p[i].y))w[i]=0f;
                float gy=terrain.SampleHeight(new Vector3(p[i].x,0f,p[i].y))+terrain.transform.position.y;
                if(gy>WaterY+.08f)w[i]=0f;
                if(w[i]>=th)bits|=1<<i;'''
if old not in s: raise SystemExit("corner mask block missing")
s=s.replace(old,new,1)
p.write_text(s,encoding="utf-8")
print("WATER_SURFACE_SOURCE_MASK_PATCHED")
