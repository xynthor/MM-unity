from pathlib import Path
import csv,numpy as np,shapely
from scipy.ndimage import map_coordinates
from shapely.geometry import Polygon
root=Path(r'C:\MMUnityPort');d=root/'Validation/NorthSq01OceanGapFill20261011'
src=list(csv.DictReader(open(root/'Validation/NorthSq01OceanFootprint20261011/triangles.csv')))
oldSea=shapely.union_all([Polygon([(float(r['x'+str(k)]),float(r['z'+str(k)])) for k in range(3)]) for r in src])
h0=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq0.f32','<f4').reshape(513,513)
h1=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq1.f32','<f4').reshape(513,513)
h=np.hstack([h0[:,:512],h1])
for version in ['V1','V2']:
 r=list(csv.DictReader(open(d/(version+'_triangles.csv'))))
 polys=[Polygon([(float(a['x'+str(k)]),float(a['z'+str(k)])) for k in range(3)]) for a in r]
 combined=shapely.union_all(polys)
 overlap=combined.intersection(oldSea).area
 edges=combined.boundary.intersection(oldSea.boundary).length
 samples=[]
 for p in polys:
  minx,minz,maxx,maxz=p.bounds
  xx=np.arange(minx,maxx+.201,.2);zz=np.arange(minz,maxz+.201,.2)
  X,Z=np.meshgrid(xx,zz,indexing='xy')
  valid=shapely.contains_xy(p,X,Z)
  if not valid.any():continue
  S=map_coordinates(h,[Z[valid]-768,X[valid]+1280],order=1,mode='nearest')
  samples.append(S)
 v=np.concatenate(samples)
 bad=(v>.12)
 print(version,'triangles',len(r),'area',round(combined.area,3),'overlapSea_m2',round(overlap,5),'shared_boundary_m',round(edges,3),'ground_max',round(float(v.max()),3),'newBuriedArea_m2',round(float(bad.sum()*.04),3),'newBuriedMaxAboveSea_m',round(float(v[bad].max()-.12),3) if bad.any() else 0,'minimumHeightM',round(float(v.min()),3),'sampledArea_m2',round(len(v)*.04,2),flush=True)
