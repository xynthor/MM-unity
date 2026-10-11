from pathlib import Path
import csv,numpy as np
from scipy.ndimage import gaussian_filter,median_filter
root=Path(r'C:\MMUnityPort');d=root/'Validation/NorthSq01CoastSeam20261011'
h0=np.fromfile(d/'sq0_current.f32','<f4').reshape(513,513).astype(float)
h1=np.fromfile(d/'sq1_current.f32','<f4').reshape(513,513).astype(float)
h=np.hstack((h0[:,:-1],h1))
zz,xx=np.mgrid[:513,:1025];X=xx-1280;Z=zz+768
def sm(a,b,v):
 t=np.clip((v-a)/(b-a),0,1);return t*t*(3-2*t)
# Locate the exact abrupt face per column, do NOT use estimated sea geometry; avoid double crossings
shore=np.full(1025,np.nan)
for j in range(375,675):
 a=h[1170-768:1240-768,j]
 jumps=a[:-1]-a[1:]
 k=int(np.argmax(jumps))
 if jumps[k]>2.5:shore[j]=1170+k+.5
active=np.flatnonzero(np.isfinite(shore))
shore[active]=gaussian_filter(median_filter(shore[active],size=13),5)
# Derive gaussian-blurred terrain itself, not a large artificial target basin
for key,sz,sx,span in [('U1',5.,4.,21.),('U2',8.,6.,27.)]:
 smoothed=gaussian_filter(h,sigma=(sz,sx),mode='nearest')
 g=np.zeros_like(h)
 for j in range(390,670):
  if not np.isfinite(shore[j]):continue
  x=j-1280
  xr=sm(-855,-836,x)*(1-sm(-703,-685,x))
  if xr<1e-6:continue
  k=shore[j]
  dz=np.abs(Z[:,j]-k)
  zweight=np.exp(-.5*(dz/span)**4)
  g[:,j]=(smoothed[:,j]-h[:,j])*xr*zweight
 target=h+g
 # exclude cells under original ocean polygons if they were above sea: never worsen an ocean conflict
 from PIL import Image,ImageDraw
 im=Image.new('L',(1025,513),0);dr=ImageDraw.Draw(im)
 for r in csv.DictReader(open(root/'Validation/NorthSq01OceanFootprint20261011/triangles.csv')):
  dr.polygon([(float(r['x'+str(i)])+1280,1280-float(r['z'+str(i)])) for i in range(3)],fill=1)
 sea=np.flipud(np.asarray(im,dtype=bool))
 originalSub=sea&(h<.12)
 target[originalSub]=np.minimum(target[originalSub],-.25)
 previousOver=sea&(h>.12)
 target[previousOver]=np.minimum(target[previousOver],h[previousOver])
 # dry original must remain visibly dry, do not create gaps without water
 dry=(~sea)&(h>.12)
 target[dry]=np.maximum(target[dry],.18)
 assert np.array_equal(target[:,0],h[:,0]) and np.array_equal(target[:,-1],h[:,-1])
 assert np.array_equal(target[0],h[0]) and np.array_equal(target[-1],h[-1])
 a=target[:,:513].astype('<f4');b=target[:,512:].astype('<f4')
 assert np.array_equal(a[:,-1],b[:,0])
 a.tofile(d/(key+'_sq0.f32'));b.tofile(d/(key+'_sq1.f32'))
 delta=target-h
 print(key,'n',int((abs(delta)>.01).sum()),'maxRise',round(float(delta.max()),2),'maxFall',round(float(delta.min()),2),flush=True)
 for x in [-810,-800,-780,-768,-756,-742,-730,-715]:
  j=x+1280
  print(x,[(z,round(float(target[z-768,j]),2)) for z in (1180,1184,1188,1192,1194,1195,1196,1198,1200,1205)],flush=True)
