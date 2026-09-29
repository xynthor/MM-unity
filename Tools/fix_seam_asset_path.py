from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticSeamAudit.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('string p=$"Assets/World/{z}/Generated/{z}_Terrain.asset";','string p=$"Assets/World/{z}/Generated/{z}Terrain.asset";')
p.write_text(s,encoding="utf-8")
print("SEAM_ASSET_PATH_FIXED")