from pathlib import Path
ROOT=Path(r'C:\MMUnityPort')
marker=ROOT/'Validation/CANONICAL_OBJ_Z_FLIP_APPLIED.txt'
if marker.exists(): raise SystemExit('marker exists; refusing double flip')
count=0; verts=0; faces=0
for zd in sorted((ROOT/'Assets/World').iterdir()):
    od=zd/'Objects'
    if not od.is_dir(): continue
    zc=0
    for p in sorted(od.glob('*.obj')):
        if p.name.startswith('000_'): continue
        lines=p.read_text(encoding='utf-8',errors='ignore').splitlines()
        out=[]; changed=False
        for line in lines:
            if line.startswith('v '):
                q=line.split()
                if len(q)>=4:
                    z=-float(q[3]); q[3]=f'{z:.6f}'; line=' '.join(q); verts+=1; changed=True
            elif line.startswith('f '):
                q=line.split(); line=' '.join([q[0]]+list(reversed(q[1:]))); faces+=1; changed=True
            out.append(line)
        if changed:
            p.write_text('\n'.join(out)+'\n',encoding='utf-8');count+=1;zc+=1
    if zc: print(zd.name,zc)
marker.write_text(f'objects={count} verts={verts} faces={faces}\n',encoding='utf-8')
print('DONE objects',count,'verts',verts,'faces',faces)
