from pathlib import Path
p=Path(r'C:\MMUnityPort\Tools\prepare_mm6_zone.py')
s=p.read_text()
needle='    "MistyIslands": ("Misty Islands", ROOT / "Extract/MistyIslands/oute2.odm"),\n}'
repl='    "MistyIslands": ("Misty Islands", ROOT / "Extract/MistyIslands/oute2.odm"),\n    "BootlegBay": ("Bootleg Bay", ROOT / "Extract/BootlegBay/outd2.odm"),\n}'
assert needle in s
p.write_text(s.replace(needle,repl))
print('patched')