from pathlib import Path
import csv,numpy as np,shapely
from scipy.ndimage import map_coordinates
from shapely.geometry import Polygon
root=Path(r'C:\MMUnityPort');d=root/'Validation/NorthSq01WestCoveRemesh20261011'
h0=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq0.f32','<f4').reshape(513,513)
h1=np.fromfile(root/'Validation/NorthSq01CoastSeam20261011/U2_sq1.f32','<f4').reshape(513,513)
h=np.hstack((h0[:,:512],h1))
for name in ['W1','W2']:
 rows=list(csv.DictReader(open(d/(name+'_triangles.csv'))))
 array=np.asarray([[[float(row['x'+str(i)]),float(row['z'+str(i)])] for i in range(3)]for row in rows])
 pts=[array.reshape(-1,2)]
 weights=np.array([[.5,.5,0],[.5,0,.5],[0,.5,.5],[1/3,1/3,1/3]])
 pts.append(np.einsum('af,nfc->nac',weights,array).reshape(-1,2))
 samples=np.vstack(pts)
 y=map_coordinates(h,[samples[:,1]-768,samples[:,0]+1280],order=1,mode='nearest')
 print(name,'critical 7-point samples',len(samples),'max y',round(float(y.max()),4),'above sea',int((y>.12).sum()), 'closest gap',round(float(.12-y.max()),4),flush=True)
 polys=[Polygon(t) for t in array]
 xs=np.arange(-1016, -809.9,.5);zs=np.arange(1165.5,1240.6,.5);Z,X=np.meshgrid(zs,xs,indexing='ij')
 selected=np.zeros(X.shape,bool)
 for p in polys:
  a,b,c,d1=p.bounds
  ix=(xs>=a)&(xs<=c);iz=(zs>=b)&(zs<=d1)
  if not(ix.any() and iz.any()):continue
  selected[np.ix_(iz,ix)]|=shapely.intersects_xy(p,X[np.ix_(iz,ix)],Z[np.ix_(iz,ix)])
 pos=np.column_stack([X[selected],Z[selected]])
 val=map_coordinates(h,[pos[:,1]-768,pos[:,0]+1280],order=1,mode='nearest')
 print(name,'dense sample',len(pos),'max y',round(float(val.max()),4),'above water',int((val>.12).sum()),'closest gap',round(float(.12-val.max()),4),'min y',round(float(val.min()),4),flush=True)
