from pathlib import Path
import numpy as np,csv,shapely
from shapely.geometry import Polygon,box
from scipy.ndimage import map_coordinates
from PIL import Image,ImageDraw
root=Path(r'C:\MMUnityPort');base=root/'Validation/NorthSq01OceanFootprint20261011'
out=root/'Validation/NorthSq01OceanGapFill20261011';out.mkdir(exist_ok=True)
h0=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq0.f32','<f4').reshape(513,513)
h1=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq1.f32','<f4').reshape(513,513)
h=np.hstack((h0[:,:512],h1))
src=list(csv.DictReader(open(base/'triangles.csv')))
original=shapely.union_all([Polygon([(float(r['x'+str(k)]),float(r['z'+str(k)])) for k in range(3)]) for r in src])
xmin,xmax,zmin,zmax=-900,-650,1150,1260
xx=np.arange(xmin+.5,xmax,1.);zz=np.arange(zmin+.5,zmax,1.)
Z,X=np.meshgrid(zz,xx,indexing='ij')
ground=map_coordinates(h,[Z-768,X+1280],order=1,mode='nearest')
for name,threshold,bl in [('V1',-.2,1.1),('V2',-.5,1.6)]:
 wet=ground<threshold
 runs=[]
 for k,z in enumerate(zz):
  ar=np.pad(wet[k].astype(np.int8),(1,1))
  for a,b in zip(np.flatnonzero(np.diff(ar)==1),np.flatnonzero(np.diff(ar)==-1)):
   runs.append(box(xmin+a,z-.5,xmin+b,z+.5))
 area=shapely.union_all(runs).buffer(bl,quad_segs=5).buffer(-bl,quad_segs=5).simplify(.25,preserve_topology=True)
 missing=area.difference(original)
 polys=list(missing.geoms) if hasattr(missing,'geoms') else [missing]
 chosen=[]
 for p in polys:
  if p.geom_type!='Polygon' or p.area<12:continue
  a,b,c,d=p.bounds
  if a< -825 or c> -718 or b<1188 or d>1218:continue
  if p.boundary.distance(original.boundary)>.4:continue
  chosen.append(p)
 print(name,'components',[(round(p.area,1),tuple(round(v,1) for v in p.bounds)) for p in chosen],flush=True)
 triangles=[]
 for poly in chosen:
  for g in shapely.constrained_delaunay_triangles(poly).geoms:
   if g.geom_type!='Polygon' or g.area<.0001:continue
   verts=list(g.exterior.coords)[:3]
   # shapely triangles CCW in XY, Unity XZ goes down Y, reverse in C# with normal up
   triangles.append(verts)
 f=out/(name+'_triangles.csv')
 with f.open('w',newline='') as file:
  w=csv.writer(file);w.writerow(['x0','z0','x1','z1','x2','z2'])
  for a,b,c in triangles:w.writerow([*a,*b,*c])
 print(name,'triangles',len(triangles),'area',round(sum(Polygon(t).area for t in triangles),1),'input_polygon_area',round(sum(p.area for p in chosen),1),flush=True)
 mask=Image.new('RGB',(1000,440),(205,204,199));md=ImageDraw.Draw(mask)
 # original water cyan; wet gaps red; new coverage blue
 def P(x,z):return ((x+900)*4,(1260-z)*4)
 for q in (original.geoms if hasattr(original,'geoms') else [original]):
  if q.geom_type=='Polygon':md.polygon([P(x,z) for x,z in q.exterior.coords],fill=(14,103,178))
 for poly in chosen:
  md.polygon([P(x,z) for x,z in poly.exterior.coords],fill=(56,186,159))
  for h in poly.interiors:md.polygon([P(x,z) for x,z in h.coords],fill=(14,103,178))
 mask.save(out/(name+'_map.png'))
