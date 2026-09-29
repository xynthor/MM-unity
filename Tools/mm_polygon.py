# Concave polygon triangulation preserving UV corner indices.
EPS=1e-9

def project_polygon(points):
    nx=ny=nz=0.0
    for i,p in enumerate(points):
        q=points[(i+1)%len(points)]
        nx+=(p[1]-q[1])*(p[2]+q[2])
        ny+=(p[2]-q[2])*(p[0]+q[0])
        nz+=(p[0]-q[0])*(p[1]+q[1])
    ax,ay,az=abs(nx),abs(ny),abs(nz)
    if ax>=ay and ax>=az: return [(p[1],p[2]) for p in points]
    if ay>=az: return [(p[0],p[2]) for p in points]
    return [(p[0],p[1]) for p in points]

def area2(a,b,c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])

def polygon_area(poly):
    s=0.0
    for i,p in enumerate(poly):
        q=poly[(i+1)%len(poly)]
        s += p[0]*q[1]-q[0]*p[1]
    return 0.5*s

def point_in_tri(p,a,b,c,orient):
    ab=area2(a,b,p)*orient
    bc=area2(b,c,p)*orient
    ca=area2(c,a,p)*orient
    return ab>=-EPS and bc>=-EPS and ca>=-EPS

def earclip(points):
    if len(points)<3: return None
    poly=project_polygon(points)
    if abs(polygon_area(poly))<=EPS: return None
    for i in range(len(poly)):
        a,b=poly[i],poly[(i+1)%len(poly)]
        for j in range(i+1,len(poly)):
            if j==i+1 or (i==0 and j==len(poly)-1): continue
            c,d=poly[j],poly[(j+1)%len(poly)]
            if area2(a,b,c)*area2(a,b,d)<-EPS and area2(c,d,a)*area2(c,d,b)<-EPS:
                return None
    if len(points)==3: return [(0,1,2)]
    orient=1.0 if polygon_area(poly)>=0 else -1.0
    idx=list(range(len(poly)))
    tris=[]
    guard=0
    while len(idx)>3 and guard < len(poly)*len(poly)*4:
        guard+=1
        clipped=False
        for ii in range(len(idx)):
            ia,ib,ic=idx[ii-1],idx[ii],idx[(ii+1)%len(idx)]
            a,b,c=poly[ia],poly[ib],poly[ic]
            if area2(a,b,c)*orient <= EPS: continue
            if any(j not in (ia,ib,ic) and point_in_tri(poly[j],a,b,c,orient) for j in idx):
                continue
            tris.append((ia,ib,ic))
            del idx[ii]
            clipped=True
            break
        if not clipped:
            # Drop the least significant nearly-collinear corner and continue.
            best=None
            for ii in range(len(idx)):
                ia,ib,ic=idx[ii-1],idx[ii],idx[(ii+1)%len(idx)]
                score=abs(area2(poly[ia],poly[ib],poly[ic]))
                if best is None or score<best[0]: best=(score,ii)
            if best and best[0] < 1e-6:
                del idx[best[1]]
                continue
            return None
    if len(idx)==3:
        tris.append(tuple(idx))
    return tris

