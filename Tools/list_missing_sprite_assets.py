import csv,glob,os,collections
texdir=r"C:\MMUnityPort\Assets\Textures\MM6SpritesOriginal"
def istree(n):
 n=n.lower(); return n.startswith("6tree") or n.startswith("tree") or n.startswith("swptree") or n.startswith("snotre")
def isrock(n): return n.lower().startswith("6rock")
def isstart(n):
 n=n.strip().lower(); return n=="party start" or n.endswith(" start")
c=collections.Counter()
loc=[]
for f in glob.glob(r"C:\MMUnityPort\Assets\World\*\Data\decorations.csv"):
 z=os.path.basename(os.path.dirname(os.path.dirname(f)))
 for q in list(csv.reader(open(f,encoding="utf-8")))[1:]:
  if len(q)<10:continue
  n=q[1].strip().lower()
  if isstart(n) or istree(n) or isrock(n):continue
  if not os.path.exists(os.path.join(texdir,n+".png")):
   c[n]+=1;loc.append((z,n,q[0]))
print(c)
print(loc)
