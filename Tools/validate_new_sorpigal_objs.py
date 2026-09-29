from pathlib import Path

root=Path(r"C:\MMUnityPort\Assets\World\NewSorpigal\Objects")
files=sorted(root.glob("*.obj"))
triangles=bad_arity=bad_index=degenerate=0
for path in files:
    verts=[]
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("v "):
            _,x,y,z=line.split()[:4]
            verts.append((float(x),float(y),float(z)))
        elif line.startswith("f "):
            parts=line.split()[1:]
            if len(parts)!=3:
                bad_arity+=1
                continue
            ids=[int(p.split("/")[0])-1 for p in parts]
            if any(i<0 or i>=len(verts) for i in ids):
                bad_index+=1
                continue
            a,b,c=(verts[i] for i in ids)
            ab=tuple(b[i]-a[i] for i in range(3))
            ac=tuple(c[i]-a[i] for i in range(3))
            cross=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
            if sum(v*v for v in cross)<1e-14: degenerate+=1
            triangles+=1
print(f"files={len(files)} triangles={triangles} bad_arity={bad_arity} bad_index={bad_index} degenerate={degenerate}")
if len(files)!=85 or bad_arity or bad_index or degenerate:
    raise SystemExit(1)
