from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticVegetationPass.cs")
s=p.read_text(encoding="utf-8-sig").replace('''        "Assets/Environment/DesertVegetation/desert_shrubs.fbx",
''','')
p.write_text(s,encoding="utf-8")
print("DESERT_POOL_SIMPLIFIED")