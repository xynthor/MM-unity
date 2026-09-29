from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMSorpigalTreeUpgrade.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('ratio>=.62f','ratio>=.48f')
s=s.replace('if(vr<.62f)horizontal++;','if(vr<.48f)horizontal++;')
p.write_text(s,encoding="utf-8")
print("TREE_RATIO_PATCHED")