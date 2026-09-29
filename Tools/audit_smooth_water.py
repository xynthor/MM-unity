from pathlib import Path
import math
root=Path(r'C:\MMUnityPort')
for zone in ['NewSorpigal','CastleIronfist','MistyIslands']:
    tiles=(root/f'Assets/World/{zone}/Data/tilemap_u8.bin').read_bytes()
    sem=(root/f'Assets/World/{zone}/Data/tile_semantics_u8.bin').read_bytes(); N=128
    def wat(t): return (sem[t]&1)!=0
    def samp(sx,sy):
        sx=max(0,min(127,sx)); sy=max(0,min(127,sy)); x0=int(math.floor(sx)); y0=int(math.floor(sy)); x1=min(x0+1,127); y1=min(y0+1,127); tx=sx-x0; ty=sy-y0
        f=lambda x,y:1.0 if wat(tiles[y*N+x]) else 0.0
        a=f(x0,y0)*(1-tx)+f(x1,y0)*tx; b=f(x0,y1)*(1-tx)+f(x1,y1)*tx
        return a*(1-ty)+b*ty
    def smooth(sx,sy):
        su=we=0.0
        for oy in range(-2,3):
            for ox in range(-2,3):
                w=1.0/(1+ox*ox+oy*oy); su+=samp(sx+ox*.65,sy+oy*.65)*w; we+=w
        return su/we
    bad=[]
    for p in sorted((root/f'Assets/World/{zone}/Objects').glob('*.obj')):
        if p.name.startswith('000_'): continue
        xs=[]; zs=[]
        for line in p.read_text(errors='ignore').splitlines():
            if line.startswith('v '):
                _,x,y,z=line.split()[:4]; xs.append(float(x)); zs.append(float(z))
        if not xs: continue
        x=(min(xs)+max(xs))/2; z=(min(zs)+max(zs))/2; sx=x/4+64; sy=z/4+64; sm=smooth(sx,sy)
        if sm>=.5 and not any(k in p.name.lower() for k in ['bridge','pier','dock']): bad.append((p.name,round(sm,2),round(sx,1),round(sy,1)))
    print(zone,'smooth_bad',len(bad),bad[:20])
