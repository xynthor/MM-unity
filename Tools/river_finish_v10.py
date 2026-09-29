from pathlib import Path
b=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=b.read_text(encoding='utf-8')
def rep(old,new,n=1):
    global s
    c=s.count(old)
    if c!=n: raise SystemExit(f'expected {n}, got {c}: {old[:100]!r}')
    s=s.replace(old,new,n)
rep('mat.SetFloat("_DepthMax",river?3.5f:16f);mat.SetFloat("_FoamDepth",river?.24f:1.15f);','mat.SetFloat("_DepthMax",river?3.5f:16f);mat.SetFloat("_FoamDepth",river?.075f:1.15f);')
rep('Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidthWorld(sy)+.62f,wy=RiverSurfaceY(sy)+.14f;','Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidthWorld(sy)+.80f,wy=RiverSurfaceY(sy)+.16f;')
rep('float wetOuter=halfW+Mathf.Lerp(2.0f,4.0f,RiverDownstream01(sy));','float wetOuter=halfW+Mathf.Lerp(1.25f,2.65f,RiverDownstream01(sy));')
b.write_text(s,encoding='utf-8')
sh=Path(r'C:\MMUnityPort\Assets\Shaders\MMRiverWater.shader')
t=sh.read_text(encoding='utf-8')
t=t.replace('float shore=1-smoothstep(0,_FoamDepth,depth);','float shore=1-smoothstep(0,_FoamDepth,depth);')
t=t.replace('float edge=smoothstep(.34,.48,abs(i.uv.x-.5));','float edge=smoothstep(.465,.499,abs(i.uv.x-.5));')
t=t.replace('float foam=saturate(shore*.45+edge*ripple*.10);','float foam=saturate(shore*.09+edge*ripple*.012);')
sh.write_text(t,encoding='utf-8')
print('RIVER_FINISH_V10_PATCHED')
