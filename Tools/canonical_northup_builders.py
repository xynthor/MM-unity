from pathlib import Path
import re
root=Path(r'C:\MMUnityPort\Assets\Editor')
for p in sorted(root.glob('Build*OpenWorld.cs')):
    if p.name=='BuildEnrothLinkedOpenWorld.cs': continue
    s=p.read_text(encoding='utf-8-sig')
    orig=s
    # Canonical MM6 outdoor map: source col 0=west, row 0=north; Unity +X=east,+Z=north.
    s,n1=re.subn(r'float\s+sy\s*=\s*z\s*/\s*Cell\s*\+\s*64f\s*;', 'float sy = 64f - z / Cell;', s)
    s,n2=re.subn(r'float\s+sy\s*=\s*64f\s*\+\s*z\s*/\s*Cell\s*;', 'float sy = 64f - z / Cell;', s)
    s,n3=re.subn(r'return\s+new\s+Vector2\(\(64f-sx\)\*Cell,\(sy-64f\)\*Cell\)\s*;', 'return new Vector2((sx-64f)*Cell,(64f-sy)*Cell);', s)
    s,n4=re.subn(r'float\s+x\s*=\s*ox\s*\*\s*WorldScale\s*,\s*z\s*=\s*oz\s*\*\s*WorldScale\s*;', 'float x=ox*WorldScale,z=-oz*WorldScale;', s)
    if s!=orig:
        p.write_text(s,encoding='utf-8')
    print(p.name,'world',n1+n2,'sourceworld',n3,'decor',n4)
