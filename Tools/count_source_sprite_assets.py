import csv,glob,os,collections
texdir=r"C:\MMUnityPort\Assets\Textures\MM6SpritesOriginal"
zones=[os.path.basename(os.path.dirname(os.path.dirname(f))) for f in glob.glob(r"C:\MMUnityPort\Assets\World\*\Data\decorations.csv")]
def istree(n):
 n=n.lower(); return n.startswith("6tree") or n.startswith("tree") or n.startswith("swptree") or n.startswith("snotre")
def isrock(n): return n.lower().startswith("6rock")
def isstart(n):
 n=n.strip().lower(); return n=="party start" or n.endswith(" start")
for f in glob.glob(r"C:\MMUnityPort\Assets\World\*\Data\decorations.csv"):
 z=os.path.basename(os.path.dirname(os.path.dirname(f)))
 rows=list(csv.reader(open(f,encoding="utf-8")))
 sprites=[]
 for i,q in enumerate(rows[1:],1):
  if len(q)<10: continue
  n=q[1].strip().lower()
  if isstart(n) or istree(n) or isrock(n): continue
  sprites.append(n)
 have=sum(os.path.exists(os.path.join(texdir,n+".png")) for n in sprites)
 print(z,len(sprites),have,"texture",len(sprites)-have,"fallback")
