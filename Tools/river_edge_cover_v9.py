from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
def rep(old,new):
    global s
    c=s.count(old)
    if c!=1: raise SystemExit(f'expected 1 got {c}: {old[:100]!r}')
    s=s.replace(old,new,1)
rep('Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidthWorld(sy),wy=RiverSurfaceY(sy)+.14f;','Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidthWorld(sy)+.62f,wy=RiverSurfaceY(sy)+.14f;')
old='''                float bankT=Mathf.Clamp01(Mathf.InverseLerp(halfW,outer,riverD));\n                float wet=riverD<=halfW?1f:Mathf.Lerp(.90f,.04f,Mathf.SmoothStep(0f,1f,bankT));\n                alpha[y,x,4]=wet;\n                alpha[y,x,1]=(1f-wet)*.20f;\n                alpha[y,x,3]=(1f-wet)*.025f;\n                alpha[y,x,0]=1f-alpha[y,x,4]-alpha[y,x,1]-alpha[y,x,3];'''
new='''                float wetOuter=halfW+Mathf.Lerp(2.0f,4.0f,RiverDownstream01(sy));\n                float wet=riverD<=halfW?1f:(riverD<wetOuter?Mathf.Lerp(.84f,.03f,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(halfW,wetOuter,riverD))):0f);\n                alpha[y,x,4]=wet;\n                alpha[y,x,1]=(1f-wet)*.14f;\n                alpha[y,x,3]=(1f-wet)*.015f;\n                alpha[y,x,0]=1f-alpha[y,x,4]-alpha[y,x,1]-alpha[y,x,3];'''
rep(old,new)
p.write_text(s,encoding='utf-8')
print('RIVER_EDGE_COVER_V9_PATCHED')
