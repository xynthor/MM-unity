from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMTreeOrientationVisualAudit.cs")
s=p.read_text()
s=s.replace('[MenuItem("MMUnity/Debug/Tree Orientation Audit")]','[MenuItem("MMUnity/Debug/Tree Orientation Audit _F2")]')
p.write_text(s)
print("HOTKEY_SET")
