import csv,glob,os,collections
N=128
def cls(z,g,f,sy):
 if (f&8) or (8<=g<255): return "ROAD"
 if (f&1) or (f&2) or g==5:return "ARID"
 if z in ["NewSorpigal","CastleIronfist","MireOfTheDamned","MistyIslands","EelInfestedWaters","BootlegBay"]:return "GREEN"
 if z=="SilverCove":return "GREEN" if g==0 else "LIGHT_GREEN"
 if z=="Dragonsand":
  if g==2:return "DESERT"
  if g==0:return "GREEN"
  return "ARID"
 if z=="HermitsIsle":
  if g==2:return "DESERT"
  if g==3:return "VOLCANIC"
  return "ARID"
 if z=="Blackshire":
  if g==2:return "DESERT"
  return "LIGHT_GREEN"
 if z=="ParadiseValley":
  if g==2:return "DESERT"
  return "ARID"
 if z=="FreeHaven":return "LIGHT_GREEN"
 if z=="SweetWater":
  return "SNOW" if g==1 else "ARID"
 if z=="FrozenHighlands":return "SNOW"
 if z=="Kriegspire":
  if g==3:return "VOLCANIC"
  return "SNOW" if sy<64 else "LIGHT_GREEN"
 return "LIGHT_GREEN"
base=r"C:\MMUnityPort\Validation\SixTerrainAudit"
world=r"C:\MMUnityPort\Assets\World"
for f in glob.glob(base+r"\*.csv"):
 if os.path.basename(f)=="summary.csv":continue
 z=os.path.basename(f)[:-4]
 lows=[r for r in csv.DictReader(open(f,encoding="utf-8")) if r["status"]!="OK" and min(int(r["sx"]),int(r["sy"]),127-int(r["sx"]),127-int(r["sy"]))>6]
 if not lows:continue
 d=os.path.join(world,z,"Data")
 tm=open(os.path.join(d,"tilemap_u8.bin"),"rb").read();gr=open(os.path.join(d,"tile_groups_u8.bin"),"rb").read();se=open(os.path.join(d,"tile_semantics_u8.bin"),"rb").read()
 print("\n"+z)
 for r in lows:
  sx,sy=int(r["sx"]),int(r["sy"]); C=collections.Counter()
  for yy in range(max(0,sy-2),min(127,sy+2)+1):
   for xx in range(max(0,sx-2),min(127,sx+2)+1):
    raw=tm[yy*N+xx];C[cls(z,gr[raw],se[raw],yy)]+=1
  print((sx,sy),"expected",r["expected"],"actual",r["dominant"],"weight",r["expectedWeight"],"5x5",dict(C))
