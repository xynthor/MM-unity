from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticSeamBlendPass.cs")
s=p.read_text(encoding="utf-8-sig")
old='''        return new I{td=AssetDatabase.LoadAssetAtPath<TerrainData>(AssetDatabase.GetAssetPath(t.terrainData))};'''
new='''        return new I{td=t.terrainData};'''
if old not in s: raise SystemExit("load line missing")
p.write_text(s.replace(old,new,1),encoding="utf-8")
print("SEAM_LOAD_DIRECT_TERRAINDATA")