from pathlib import Path
import csv,numpy as np,shapely
from shapely.geometry import Polygon
from scipy.ndimage import map_coordinates
root=Path(r'C:\MMUnityPort');d=root/'Validation/NorthSq01WestCoveRemesh20261011'
rows=list(csv.DictReader(open(d/'W2_triangles.csv')))
p=shapely.union_all([Polygon([(float(r['x'+str(i)]),float(r['z'+str(i)])) for i in range(3)]) for r in rows])
sea_rows=list(csv.DictReader(open(root/'Validation/NorthSq01WestOceanFootprint20261011/triangles.csv')))
sea=shapely.union_all([Polygon([(float(r['x'+str(i)]),float(r['z'+str(i)])) for i in range(3)]) for r in sea_rows])
center_rows=list(csv.DictReader(open(root/'Tools/NorthSq1F2P20261010/Accepted_CoveGap_V2Safe_triangles.csv')))
center=shapely.union_all([Polygon([(float(r['x'+str(i)]),float(r['z'+str(i)])) for i in range(3)]) for r in center_rows])
sea=shapely.union_all([sea,center])
h0=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq0.f32','<f4').reshape(513,513)
h1=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq1.f32','<f4').reshape(513,513)
h=np.hstack([h0[:,:512],h1])
def height(points):return map_coordinates(h,[points[:,1]-768,points[:,0]+1280],order=1,mode='nearest')
def parts(obj):
 if obj.is_empty:return []
 if obj.geom_type=='Polygon':return [obj]
 if hasattr(obj,'geoms'):return [p for g in obj.geoms for p in parts(g)]
 return []
r=.25
xs=np.arange(-1017,-807.9,r);zs=np.arange(1165,1241,r)
ZZ,XX=np.meshgrid(zs,xs,indexing='ij');terrain=map_coordinates(h,[ZZ-768,XX+1280],order=1,mode='nearest')
weights=np.array([[.5,.5,0],[.5,0,.5],[0,.5,.5],[1/3,1/3,1/3]])
for iteration in range(9):
 pieces=[g for g in parts(p) if g.area>5]
 p=shapely.union_all(pieces)
 ts=[]
 for g in pieces:
  for q in shapely.constrained_delaunay_triangles(g).geoms:
   if q.area>.001:ts.append(list(q.exterior.coords)[:3])
 arr=np.asarray(ts,dtype=float)
 vertices=arr.reshape(-1,2)
 mids=np.einsum('wf,nfc->nwc',weights,arr).reshape(-1,2)
 inc=shapely.intersects_xy(p,XX,ZZ)
 gridloc=np.column_stack((XX[inc],ZZ[inc]));gridh=terrain[inc]
 vertexh=height(vertices);midh=height(mids)
 unsafe=np.vstack([vertices[vertexh>-.3],mids[midh>-.3],gridloc[gridh>-.3]])
 print('ITER',iteration,'area',round(p.area,1),'parts',len(pieces),'triangles',len(ts),'badVertices',int((vertexh>-.3).sum()),'badMid',int((midh>-.3).sum()),'badGrid',int((gridh>-.3).sum()),'maxY',round(float(max(vertexh.max(),midh.max(),gridh.max())),2),'riskPoints',len(unsafe),flush=True)
 if len(unsafe)==0:
  print('SUCCESS',flush=True);break
 # Proactively excise circular zones around unsafe vertex and subgrid points, including high spikes at boundaries
 pts=shapely.points(unsafe)
 buff=shapely.buffer(pts,1.0 if iteration<4 else 1.5,quad_segs=6)
 forbidden=shapely.union_all(buff)
 p=p.difference(forbidden)
else:print('FAILED_TO_CLEAR',flush=True)
chosen=shapely.union_all([g for g in parts(p) if g.area>10])
trilist=[]
for g in parts(chosen):
 for t in shapely.constrained_delaunay_triangles(g).geoms:
  if t.area>.001:trilist.append(np.asarray(t.exterior.coords[:3]))
final=np.asarray(trilist)
assert len(final)>0
print('FINAL',len(final),'area',round(chosen.area,1),'seaOverlap',round(chosen.intersection(sea).area,6),'contact',round(chosen.boundary.intersection(sea.boundary).length,1),'maxV',round(float(height(final.reshape(-1,2)).max()),3),'polys',len(parts(chosen)),flush=True)
with (d/'W5_iterative_safe_triangles.csv').open('w',newline='') as f:
 w=csv.writer(f);w.writerow(['x0','z0','x1','z1','x2','z2'])
 for t in final:w.writerow([*t[0],*t[1],*t[2]])
