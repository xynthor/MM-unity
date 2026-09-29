from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMImmediateCommands.cs")
s=p.read_text(encoding="utf-8-sig").replace('''[MenuItem("MMUnity/Restore Old Good Sorpigal Vegetation _F8")]''','''[MenuItem("MMUnity/Restore Old Good Sorpigal Vegetation %#g")]''')
p.write_text(s,encoding="utf-8")
print("CTRL_SHIFT_G_READY")