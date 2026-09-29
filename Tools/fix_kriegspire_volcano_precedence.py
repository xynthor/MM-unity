from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticTerrainBiomePass.cs")
s=p.read_text(encoding="utf-8-sig")
old='''        if(baseClass==Road||baseClass==Rock||baseClass==Sand||baseClass==WetMud)return baseClass;'''
new='''        if(baseClass==Road||baseClass==Rock||baseClass==Sand||baseClass==WetMud||baseClass==Volcanic)return baseClass;'''
if old not in s: raise SystemExit("precedence needle missing")
s=s.replace(old,new,1)
p.write_text(s,encoding="utf-8")
print("KRIEGSPIRE_VOLCANO_PRECEDENCE_FIXED")
