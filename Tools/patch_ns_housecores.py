from pathlib import Path
import re
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
pat=r'''\s*float roofY=b\.min\.y\+Mathf\.Min\(b\.size\.y\*\.82f,sy\+Mathf\.Max\(\.5f,b\.size\.y\*\.16f\)\);.*?\n\s*ncore\+\+;'''
rep='''
            // Keep the original MM6 roof/facade geometry.  The core only closes the hollow shell.
            // Do NOT add synthetic roof slabs here: they can intersect the authentic facets.
            ncore++;'''
s2,n=re.subn(pat,rep,s,flags=re.S)
if n!=1:
    raise SystemExit(f'expected one roof block, replaced {n}')
p.write_text(s2,encoding='utf-8')
print('patched synthetic roofs removed')
