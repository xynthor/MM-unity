import csv,glob,os
for f in glob.glob(r"C:\MMUnityPort\Assets\World\*\Data\decorations.csv"):
    z=os.path.basename(os.path.dirname(os.path.dirname(f)))
    for q in list(csv.reader(open(f,encoding="utf-8")))[1:]:
        if len(q)>1 and q[1].strip().lower() in ("shp","dec25","snd_brook",""):
            print(z,q)
