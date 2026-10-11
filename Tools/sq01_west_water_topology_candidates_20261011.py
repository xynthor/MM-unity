from pathlib import Path
import csv,numpy as np,shapely
from scipy.ndimage import map_coordinates
from shapely.geometry import Polygon,box
from PIL import Image,ImageDraw
root=Path(r'C:\MMUnityPort')
out=root/'Validation/NorthSq01WestCoveRemesh20261011';out.mkdir(parents=True,exist_ok=True)
src=list(csv.DictReader(open(root/'Validation/NorthSq01WestOceanFootprint20261011/triangles.csv')))
old=shapely.union_all([Polygon([(float(r['x'+str(i)]),float(r['z'+str(i)])) for i in range(3)]) for r in src])
src2=list(csv.DictReader(open(root/'Tools/NorthSq1F2P20261010/Accepted_CoveGap_V2Safe_triangles.csv')))
mid=shapely.union_all([Polygon([(float(r['x'+str(i)]),float(r['z'+str(i)])) for i in range(3)]) for r in src2])
old=shapely.union_all((old,mid))
h0=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq0.f32','<f4').reshape(513,513)
h1=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq1.f32','<f4').reshape(513,513)
h=np.hstack((h0[:,:512],h1))
xmin,xmax,zmin,zmax=-1060,-795,1150,1260
xx=np.arange(xmin+.5,xmax);zz=np.arange(zmin+.5,zmax)
Z,X=np.meshgrid(zz,xx,indexing='ij')
terrain=map_coordinates(h,[Z-768,X+1280],order=1,mode='nearest')
for name,depth,rad in [('W1',-.65,.8),('W2',-1.2,.7)]:
 m=terrain<depth
 rect=[]
 for i,z in enumerate(zz):
  v=np.pad(m[i].astype(np.int8),(1,1));starts=np.flatnonzero(np.diff(v)==1);ends=np.flatnonzero(np.diff(v)==-1)
  for a,b in zip(starts,ends):rect.append(box(xmin+a,z-.5,xmin+b,z+.5))
 raw=shapely.union_all(rect).buffer(rad,quad_segs=5).buffer(-rad,quad_segs=5).simplify(.35,preserve_topology=True)
 missing=raw.difference(old).intersection(box(-1040,1156,-803,1245))
 polys=[p for p in (missing.geoms if hasattr(missing,'geoms') else [missing]) if p.geom_type=='Polygon' and p.area>200 and p.boundary.intersection(old.boundary).length>100]
 tris=[]
 for p in polys:
  for t in shapely.constrained_delaunay_triangles(p).geoms:
   if t.area>.001:tris.append(list(t.exterior.coords)[:3])
 with (out/(name+'_triangles.csv')).open('w',newline='') as f:
  w=csv.writer(f);w.writerow(['x0','z0','x1','z1','x2','z2'])
  for t in tris:w.writerow([*t[0],*t[1],*t[2]])
 allpoly=shapely.union_all([Polygon(t) for t in tris])
 area=sum(p.area for p in polys)
 img=Image.new('RGB',(1060,440),(207,204,202));dr=ImageDraw.Draw(img)
 def xy(x,z):return ((x+1060)*4,(1260-z)*4)
 for p in (old.geoms if hasattr(old,'geoms') else [old]):
  if p.geom_type=='Polygon':dr.polygon([xy(x,z) for x,z in p.exterior.coords],fill=(20,94,166))
 for p in polys:
  dr.polygon([xy(x,z) for x,z in p.exterior.coords],fill=(35,183,171))
  for hole in p.interiors:dr.polygon([xy(x,z) for x,z in hole.coords],fill=(207,204,202))
 img.save(out/(name+'_topology.png'))
 ar=np.asarray(tris);longs=np.linalg.norm(ar-np.roll(ar,1,axis=1),axis=2) if len(ar) else np.array([0])
 print(name,'polys',len(polys),'triangles',len(tris),'AREA',round(area,2),'OLDOVERLAP',round(allpoly.intersection(old).area,6),'oldBoundary',round(allpoly.boundary.intersection(old.boundary).length,2),'maxEdge',round(float(longs.max()),2),'bounds',[tuple(round(v,1) for v in p.bounds) for p in polys],flush=True)
