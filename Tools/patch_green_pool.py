from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticVegetationPass.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''        "Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx",
        "Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx",
''','')
p.write_text(s,encoding="utf-8")
print("GREEN_POOL_SIMPLIFIED")