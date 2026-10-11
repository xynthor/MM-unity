import csv,numpy as np,shapely
from pathlib import Path
from shapely.geometry import Polygon
from scipy.ndimage import map_coordinates
root=Path(r'C:\MMUnityPort')
oldpath=root/'Tools/NorthSq1F2P20261010/Accepted_CoveGap_V2_triangles.csv'
newpath=root/'Tools/NorthSq1F2P20261010/Accepted_CoveGap_V2Safe_triangles.csv'
d=list(csv.DictReader(open(oldpath)))
tri=[np.array([[float(row['x'+str(k)]),float(row['z'+str(k)])] for k in range(3)]) for row in d]
h0=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq0.f32','<f4').reshape(513,513)
h1=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq1.f32','<f4').reshape(513,513)
h=np.hstack([h0[:,:512],h1])
def height(p):return float(map_coordinates(h,[[p[1]-768],[p[0]+1280]],order=1,mode='nearest')[0])
for idx,a,b,depth in [(5,0,1,.8),(11,1,2,.7)]:
 t=tri[idx];mid=(t[a]+t[b])/2;mid[1]+=depth
 print('move mid',idx,tuple(mid),'height',height(mid),'inside',Polygon(t).contains(shapely.Point(mid)),flush=True)
 new1=np.array([t[a],mid,t[3-a-b]])
 new2=np.array([mid,t[b],t[3-a-b]])
 tri[idx]=new1;tri.append(new2)
weights=np.array([[1,0,0],[0,1,0],[0,0,1],[1/3,1/3,1/3],[.5,.5,0],[0,.5,.5],[.5,0,.5]])
violations=[]
for idx,t in enumerate(tri):
 for q,wt in enumerate(weights):
  p=wt@t;y=height(p)
  if y>.12:violations.append((idx,q,tuple(p),round(y,4)))
print('TOTAL',len(tri),'violations',violations,flush=True)
src=list(csv.DictReader(open(root/'Validation/NorthSq01OceanFootprint20261011/triangles.csv')))
sea=shapely.union_all([Polygon([(float(r['x'+str(k)]),float(r['z'+str(k)])) for k in range(3)]) for r in src])
polygon=shapely.union_all([Polygon(t) for t in tri])
print('AREA',polygon.area,'overlap',polygon.intersection(sea).area,'shared boundary m',polygon.boundary.intersection(sea.boundary).length,'components',polygon.geom_type,flush=True)
with newpath.open('w',newline='') as f:
 w=csv.writer(f);w.writerow(['x0','z0','x1','z1','x2','z2'])
 for t in tri:w.writerow([*t[0],*t[1],*t[2]])
print('SAVED',newpath)
